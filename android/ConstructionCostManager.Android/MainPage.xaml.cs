namespace ConstructionCostManager.Android;

public partial class MainPage : ContentPage
{
    private const string ServerPreferenceKey = "construction_cost_manager_server";

    public MainPage()
    {
        InitializeComponent();
        ServerAddressEntry.Text = Preferences.Default.Get(ServerPreferenceKey, string.Empty);
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        var address = ServerAddressEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(address) ||
            !Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await DisplayAlert(
                "عنوان غير صحيح",
                "أدخل عنواناً كاملاً يبدأ بـ http:// أو https://",
                "حسناً");
            return;
        }

        address = address.TrimEnd('/');
        Preferences.Default.Set(ServerPreferenceKey, address);

        StatusLabel.Text = "جارٍ الاتصال بالخادم...";
        AppWebView.IsVisible = true;
        AppWebView.Source = new UrlWebViewSource { Url = address };
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        StatusLabel.Text = e.Result == WebNavigationResult.Success
            ? "تم الاتصال."
            : "تعذر الاتصال. تحقق من العنوان والشبكة وأن الخادم يعمل.";
    }

    protected override bool OnBackButtonPressed()
    {
        if (AppWebView.IsVisible && AppWebView.CanGoBack)
        {
            AppWebView.GoBack();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
