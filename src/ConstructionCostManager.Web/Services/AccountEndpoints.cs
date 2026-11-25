using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConstructionCostManager.Web.Services;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/login", (HttpRequest request, IAdminAuthenticationService auth) =>
        {
            var returnUrl = GetSafeReturnUrl(request.Query["returnUrl"]);
            return Results.Content(BuildLoginPage(returnUrl, auth.IsConfigured), "text/html; charset=utf-8");
        }).AllowAnonymous();

        endpoints.MapPost("/account/login", async (HttpContext context, IAdminAuthenticationService auth) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var rememberMe = string.Equals(form["rememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase);
            var returnUrl = GetSafeReturnUrl(form["returnUrl"]);

            if (!auth.IsConfigured)
                return Results.Problem(
                    "لم تتم تهيئة حساب المدير. يجب ضبط Admin:Username وAdmin:PasswordHash خارج المستودع.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!await auth.SignInAsync(context, username, password, rememberMe))
                return Results.Unauthorized();

            return Results.Redirect(returnUrl);
        }).AllowAnonymous().DisableAntiforgery();

        endpoints.MapPost("/account/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/account/login");
        }).RequireAuthorization().DisableAntiforgery();

        endpoints.MapGet("/account/denied", () =>
            Results.Content(BuildDeniedPage(), "text/html; charset=utf-8"))
            .AllowAnonymous();

        return endpoints;
    }

    private static string GetSafeReturnUrl(string? value)
    {
        var returnUrl = value?.Trim();
        return !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl
            : "/";
    }

    private static string BuildLoginPage(string returnUrl, bool isConfigured)
    {
        var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);
        var configurationMessage = isConfigured
            ? string.Empty
            : "<div class=\"alert alert-warning\">لم يتم إعداد حساب المدير بعد. راجع تعليمات الأمان في README.</div>";

        return $$"""
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>تسجيل الدخول - مدير تكاليف البناء</title>
            <link rel="stylesheet" href="/app.css" />
            <style>body{min-height:100vh;display:grid;place-items:center;background:#f5f7fb}.login-card{width:min(420px,calc(100% - 2rem));background:#fff;border-radius:1rem;padding:2rem;box-shadow:0 10px 30px #0001}</style>
        </head>
        <body>
            <main class="login-card">
                <h1 class="h4 mb-4">تسجيل الدخول</h1>
                {{configurationMessage}}
                <form method="post" action="/account/login" autocomplete="on">
                    <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
                    <div class="mb-3"><label class="form-label" for="username">اسم المستخدم</label><input class="form-control" id="username" name="username" required autocomplete="username" /></div>
                    <div class="mb-3"><label class="form-label" for="password">كلمة المرور</label><input class="form-control" id="password" name="password" type="password" required autocomplete="current-password" /></div>
                    <div class="form-check mb-3"><input class="form-check-input" id="rememberMe" name="rememberMe" type="checkbox" /><label class="form-check-label" for="rememberMe">تذكرني على هذا الجهاز</label></div>
                    <button class="btn btn-primary w-100" type="submit" {{(isConfigured ? "" : "disabled")}}>دخول</button>
                </form>
            </main>
        </body>
        </html>
        """;
    }

    private static string BuildDeniedPage() => """
        <!doctype html><html lang="ar" dir="rtl"><head><meta charset="utf-8" /><title>غير مصرح</title></head>
        <body style="font-family:system-ui;padding:3rem"><h1>ليس لديك صلاحية للوصول إلى هذه الصفحة.</h1><a href="/">العودة</a></body></html>
        """;
}
