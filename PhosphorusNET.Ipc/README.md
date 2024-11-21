# Phosphorus.NET

This is a library more than a framework that allows you to create native C# WPF - Winforms applications that host a
web-app with interoperability using IPC between C# and JS.

## But there is Blazor Hybrid...

Having one more tool in your pockets is always better than having one. The aim of this library is to allow you to embed
any web-app of your choice whether that's native JS, React, Vue, Angular etc.

## Why ?

1. Why not?
2. WebView2 ! Low RAM consumption compared to a whole chromium instance.
3. You get to write your native code in C# instead of JS :)
4. You have full control over your WPF / WinForms project.

## Doesn't WebView2 already have Interop anyways?

Yes and no. Technically yes it does, however whilst the C# -> JS communication is simple, the JS -> C# communication not
so much.
How WebView2 achieves JS -> C# communication is with two ways:

1. One way - Using the `WebMessageReceived` event
2. Two ways - Using Host Objects
   Addressing "2"
   first, [adding Host Objects](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.addhostobjecttoscript")
   have limitations around passing parameters. Even having a
   `public Task<CustomClass> Calculate(AnotherClass A, List<AnotherClass> B)` will not work easily. I'm not even sure if
   it's somehow possible.
   The "1"st point is interesting, however it's a global channel. On the javascript side you just add an event listener
   for "message" and everything comes there! Also there's no way to easily "return" a value, since it's one-way...

# Goal of the library

The goal is to fix "2" so that it provides better DX for your project. Ideally you should just be able to register
objects from the C# side and call them from JS. How does the Phosphorus.NET API look ? Simple!

1. Create the wrapper classes that you wanna "expose" to IPC. This can be anything, REST Calls, Database Calls, reading
   from the FS, displaying notifications etc...
   Use the `IpcExpose` attribute on the class to mark all public methods to be exposed, or you can also use it on
   individual methods.

```csharp
[IpcExpose]
public class Calculator
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

2. Create a class that extends `IpcContainer` and call `Register(string, object)` to attach your C# instances. The first
   parameter is important it's the "instance name", you will need to remember it to access the object from JS-land.

```csharp
class Container : IpcContainer
{
    private Calculator _calculator = new();
    public Container()
    {
        Register("calculator", _calculator);
    }
}
```

3. After initializing CoreWebView2, attach your Container using the extension method `RegisterIpcHandler(IpcContainer)`

```csharp
await webView.EnsureCoreWebView2Async();
var container = new Container();
webView.RegisterIpcHandler(container);
```

4. Install the `@ultrawelfare/phosphorus.net-ipc` npm package on your webapp and in the initial entry-point of your
   app (main.jsx for React, for others it depends on your framework) import it

```js
import "@ultrawelfare/phosphorus.net-ipc";
```

That's it! You've attached your container to the webview, which in turn attaches the objects to JS-land and you
installed the npm package to allow you to communicate!
The script attaches a global `ipc` object to window which you can call like this:

```js
// General syntax:
// ipc.{instanceName}.{methodName}({args});

// or for a better example using the calculator:
const result = await ipc.calculator.Multiply(2, 5);
console.log(result); // 10
```

Note: you will always need to `await`, even if the C# method is not a Task.

## Requirements
Minimum .NET 6.0 version is required.

Only windows supported at the time, although I'd like to explore the possibility to make MAUI work with this.
## Limitations

* **NOT PRODUCTION READY** - There's no guarantee about the stability of the library (although it works on my machine ¯\\_(ツ)_/¯).
* Uses reflection, so it's not the fastest thing in the world.
* Due to how the IPC works (using JSON) you're not "permitted" to pass **everything** as arguments or return
  *everything*.
  Unfortunately it will have to stay within the bounds of the JSON Serialization rules, so it is not allowed to pass
  functions (Func, Action) with the IPC.

So far what is allowed is:

1. Double, Float, Int (as number)
2. String
3. Boolean
4. Null
5. Classes
6. Lists
7. Objects

* There's currently no support to invoke Generic methods.

## Quick Start

## Manual Installation

Refer to the [manual-installation.md](docs/manual-installation.md) for a step-by-step guide on how to install the
library to your project.
Note that this is for more advanced users.

## Security

As with all things in life, be careful with what you expose to the web.
The library does not provide any security mechanisms, so it's up to you to ensure that you're not exposing anything
sensitive.

At any moment, your web application can be injected with a malicious script (especially when the application is communicating with the Web) that can call your exposed methods and
potentially
damage your application or the user's computer.

You take **full** responsibility for the security of your application.

## How it works

A small synopsis:

* The library uses a custom `IpcExpose` attribute to mark classes that you want to expose to the IPC.
* The `IpcContainer` class is used to register the instances of the classes that you want to expose.
* The `IpcContainer` class is then attached to the `CoreWebView2` instance using the `RegisterIpcHandler` extension
  method.
* The `@ultrawelfare/phosphorus.net-ipc` npm package is used to communicate with the C# instances from the web app.

The whole idea is passing JSON back'n'forth.
At the root of `ipc` in JS-Land, it's just a proxy which gets the instance name, method name and the arguments.
Using `window.chrome.webview.postMessage` it sends the data to the C# side. This message is a JSON object which looks
like:

```json 
{
  "uuid": "3a97d12a-6b76-4ce5-8e3e-37cfe9629a8e",
  "instance": "calculator",
  "method": "Multiply",
  "args": [
    2,
    5
  ]
}
```

The `uuid` is used to track the response while going between C# and JS. The C# side will respond with the same `uuid` so
that the JS-land can match the response.
Once the C# side receives a message from JS (that's what `RegisterIpcHandler` does), it will look inside the user's
Container for the instance name and try to deserialize the arguments according to the method (using Reflection)
The response is then sent back to JS-land using the same `uuid` so that the JS-land can match the response.

At the JS-land once the user tries to "invoke" an ipc method and the message is sent it returns a Promise that will
resolve once the response is received (hence why you need to `await` even sync functions).
