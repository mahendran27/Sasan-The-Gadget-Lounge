using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using SasanERP.Server;

namespace SasanERP.UI.Views;

public partial class WebView2HostView : UserControl
{
    private bool _isInitialized;

    public WebView2HostView()
    {
        InitializeComponent();
        Loaded += WebView2HostView_Loaded;
    }

    private async void WebView2HostView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            // Ensure local embedded server is active on 5000
            await ServerHost.StartAsync(url: "http://127.0.0.1:5000");

            // Setup WebView2 environment
            string userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SasanTheGadgetLounge",
                "WebView2Data");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await MainWebView.EnsureCoreWebView2Async(env);

            MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MainWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            MainWebView.NavigationCompleted += (s, args) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            };

            MainWebView.Source = new Uri("http://127.0.0.1:5000");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-LOAD-ERROR] {ex}");
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
