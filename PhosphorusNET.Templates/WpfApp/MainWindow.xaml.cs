using System.Windows;
using Microsoft.Web.WebView2.Core;
using PhosphorusNET.Ipc;
using WpfApp.Ipc;

namespace WpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        OnLoad();
    }

    private async void OnLoad()
    {
        webView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            //-:cnd:noEmit
#if RELEASE
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app", "wwwroot",
                CoreWebView2HostResourceAccessKind.Allow);
            webView.CoreWebView2.Navigate("https://app/index.html");
#else
            webView.CoreWebView2.Navigate("http://localhost:5173");
#endif
            //+:cnd:noEmit
        };

        await webView.EnsureCoreWebView2Async();
        webView.RegisterIpcHandler(new Container());
    }
}