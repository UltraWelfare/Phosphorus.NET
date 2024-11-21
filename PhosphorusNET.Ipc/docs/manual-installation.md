## Manual Installation

Create a new WPF/WinForms project or open an existing one.

Install the `Microsoft.Web.WebView2` NuGet package.

```powershell
Install-Package Microsoft.Web.WebView2
# or
dotnet add package Microsoft.Web.WebView2
```

Install the `PhosphorusNET.Ipc` NuGet package.

```powershell
Install-Package PhosphorusNET.Ipc
# or
dotnet add package PhosphorusNET.Ipc
```

Depending on your project type, you'll have to modify your Window/UserControl to include a `WebView2` control. Here's an
example for WPF:

```xml

<Window x:Class="WpfApp1.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:WpfApp1"
        xmlns:wpf="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <DockPanel>
            <wpf:WebView2 Name="webView"/>
        </DockPanel>
    </Grid>
</Window>
```

Before continuing initializing the `WebView2` control, you'll have to setup your web-app project. You're free to use
what-ever technology you want
as long as it produces static files when built. In this example we will use the popular tool "Vite" which supports
React, Vue, Svelte, vanilla JS and others.

Create a directory called "wwwroot" at the root of your project. This is where your web-app will be built to.

```bash
cd wwwroot
npm create vite@latest . # Follow the prompts
npm install
npm install @ultrawelfare/phosphorus.net-ipc
```

Depending on your framework of choice you'll have to import the `@ultrawelfare/phosphorus.net-ipc` package in the entry-point of your app. For example on 
react apps there is a `main.jsx` or `main.tsx` file which would be the entry-point. 

```js
import "@ultrawelfare/phosphorus.net-ipc";
```

Back to the C# project, depending on your C# project type (WinForms / WPF) you'll have to initialize the `WebView2` control. Here's an example
for WPF:

```csharp
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
}
```

The code above depending on the build configuration will either load the static files from the `wwwroot` directory or
from
a local development server. The development server is started by Vite when you run `npm run dev`. Feel free to modify
the code inside
`CoreWebView2InitializationCompleted` to suit your needs.

One small thing before we continue is that the ```wwwroot``` directory in the code above is referred executable
directory... Which means once your
app is built in `bin/Debug/.../WpfApp.exe` or `bin/Release/.../WpfApp.exe` the `wwwroot` directory will **not** be there.

To fix this, we will have to copy the built web-app files from the `wwwroot` directory to the output directory of the C# project.
Once you run `npm run build` inside your `wwwroot` folder, it will place the built files in the `wwwroot/dist` directory. To automate the process you can add the 
following to your `.csproj` file:
```csharp
    <ItemGroup>
        <WwwrootFiles Include="wwwroot\dist\**\*.*" />
    </ItemGroup>

    <Target Name="CopyFiles" AfterTargets="Build">
        <Copy SourceFiles="@(WwwrootFiles)" DestinationFolder="$(OutDir)\wwwroot\%(RecursiveDir)" />
    </Target>

    <Target Name="CopyFiles" AfterTargets="Publish">
        <Copy SourceFiles="@(WwwrootFiles)" DestinationFolder="$(PublishDir)\wwwroot\%(RecursiveDir)" />
    </Target>
```

Now you can build your project and the web-app files will be copied to the output directory.

Moving on, we will have to create a class that extends `IpcContainer` and register the C# objects that we want to expose.
For example:
```csharp
// Calculator.cs
[IpcExpose]
class Calculator
{
    public int Multiply(int a, int b)
    {
        return a * b;
    }
    
    public async Task<int> MultiplyAsync(int a, int b)
    {
        await Task.Delay(2000);
        return a * b;
    }
}
```
```csharp
// Container.cs
class Container : IpcContainer
{
    private Calculator _calculator = new();
    public Container()
    {
        Register("calculator", _calculator);
    }
}
```

Note: The [IpcExpose] attribute can either be added to a class to mark all methods as exposed or to individual methods.

Finally to register the container with the `WebView2` control, you can use the `RegisterIpcHandler` extension method. On the `OnLoad` method
we created before, add the following code after the `EnsureCoreWebView2Async` call:
```csharp
await webView.EnsureCoreWebView2Async();
var container = new Container();
webView.RegisterIpcHandler(container);
```

Your app is now ready to communicate between C# and JS. You can now call the exposed methods from JS like this:
```js
// General syntax:
ipc.{instanceName}.{methodName}({args});
// or for a better example using the calculator:
const result = await ipc.calculator.Multiply(2, 5);
console.log(result); // 10
```