using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
#if RELEASE
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app", "wwwroot",
            CoreWebView2HostResourceAccessKind.Allow);
        webView.CoreWebView2.Navigate("https://app/index.html");
#else
            webView.CoreWebView2.Navigate("http://localhost:5173");
#endif
        };

        await webView.EnsureCoreWebView2Async();
        webView.RegisterIpcHandler(new Container());
    }
}