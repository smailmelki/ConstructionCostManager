using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConstructionCostManager.Web.Services;

public interface IAdminAuthenticationService
{
    bool IsConfigured { get; }
    string? Username { get; }
    Task<bool> SignInAsync(HttpContext httpContext, string username, string password, bool rememberMe = false);
}

/// <summary>
/// Authenticates the single local administrator configured outside source control.
/// The password is stored as a PBKDF2 hash in Admin:PasswordHash.
/// </summary>
public sealed class AdminAuthenticationService : IAdminAuthenticationService
{
    private const int MinimumIterations = 100_000;
    private readonly ILogger<AdminAuthenticationService> _logger;
    private readonly string? _username;
    private readonly string? _passwordHash;

    public AdminAuthenticationService(IConfiguration configuration, ILogger<AdminAuthenticationService> logger)
    {
        _logger = logger;
        _username = configuration["Admin:Username"]?.Trim();
        _passwordHash = configuration["Admin:PasswordHash"]?.Trim();

        if (!IsConfigured)
        {
            _logger.LogWarning(
                "Administrator authentication is not configured. Set Admin:Username and Admin:PasswordHash outside source control before deployment.");
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_passwordHash);
    public string? Username => _username;

    public async Task<bool> SignInAsync(
        HttpContext httpContext,
        string username,
        string password,
        bool rememberMe = false)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return false;

        var submittedUsername = System.Text.Encoding.UTF8.GetBytes(username.Trim());
        var configuredUsername = System.Text.Encoding.UTF8.GetBytes(_username!);
        if (submittedUsername.Length != configuredUsername.Length ||
            !CryptographicOperations.FixedTimeEquals(submittedUsername, configuredUsername))
            return false;

        if (!VerifyPassword(password, _passwordHash!))
            return false;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, _username!),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

        return true;
    }

    private static bool VerifyPassword(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('$', StringSplitOptions.None);
            if (parts.Length != 4 || parts[0] != "v1" ||
                !int.TryParse(parts[1], out var iterations) || iterations < MinimumIterations)
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length < 16 || expected.Length < 32)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
