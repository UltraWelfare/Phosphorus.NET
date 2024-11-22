using System.Text.Json;
using PhosphorusNET.Ipc.Messages;
using Message = PhosphorusNET.Ipc.Messages.Message;
using WebView2Wpf = Microsoft.Web.WebView2.Wpf.WebView2;
using WebView2WinForms = Microsoft.Web.WebView2.WinForms.WebView2;

namespace PhosphorusNET.Ipc;

public static class Ipc
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Adds a listener to the `WebMessageReceived` event of the webView core.
    /// It passes the web message as a json to <see cref="Invoke"/> which then
    /// serializes and posts it back to the webView core.
    /// </summary>
    public static void RegisterIpcHandler(this WebView2Wpf webView2, IpcContainer ipcContainer)
    {
        var coreWebView2 = webView2.CoreWebView2;
        coreWebView2.WebMessageReceived += async (o, e) =>
        {
            var message = await Invoke(ipcContainer, e.WebMessageAsJson);
            var serialized = JsonSerializer.Serialize<object>(message, JsonSerializerOptions);
            coreWebView2.PostWebMessageAsJson(serialized);
        };
    }

    /// <inheritdoc cref="RegisterIpcHandler(WebView2Wpf,PhosphorusNET.Ipc.IpcContainer)"/>
    public static void RegisterIpcHandler(this WebView2WinForms webView2, IpcContainer ipcContainer)
    {
        var coreWebView2 = webView2.CoreWebView2;
        coreWebView2.WebMessageReceived += async (o, e) =>
        {
            var message = await Invoke(ipcContainer, e.WebMessageAsJson);
            coreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonSerializerOptions));
        };
    }


    /// <summary>
    /// This is the essential entry point that coordinates the invokation.
    /// The `json` parameter gets parsed and validated to contain the appropriate fields (uuid, instance, method, args)
    ///
    /// <br/><br/>
    /// UUID is used to track the invocation request from the browser.<br/>
    /// Instance is used to identify the registered instance in the container.<br/>
    /// Method is used to identify the method of the instance.<br/>
    /// Args is a list `[1, "a", [2,3]]` which denotes the positional arguments of the method.
    /// <br/><br/>
    ///
    /// The instance is grabbed from the ipc container and then the appropriate method is searched.<br/>
    /// The tricky part is deserializing the list of the arguments. The current solution involves
    /// first deserializing the args into `IEnumerable&lt;JsonElement&gt;` then depending on the positional argument
    /// on the method it gets mapped accordingly.
    ///
    /// <br/><br/>
    ///
    /// Finally, <see cref="InvokeMethodAsync"/> is called to get the result of the method which is the return of this function.
    /// </summary>
    /// <returns>
    /// Either a <see cref="InvokationRequestMessage"/> for successful invokations or a <see cref="InvokationRequestError"/> for failed invokations.
    /// </returns>
    /// <exception cref="InvalidOperationException">the uuid field in the json is null</exception>
    /// <exception cref="JsonException">the json string cannot be parsed</exception>
    public static async Task<Message> Invoke(IpcContainer ipcContainer, string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        var uuid = jsonDocument.RootElement.GetProperty("uuid").GetString();
        if (uuid is null)
        {
            throw new InvalidOperationException("Invalid message format.");
        }

        var instanceName = jsonDocument.RootElement.GetProperty("instance").GetString();
        var method = jsonDocument.RootElement.GetProperty("method").GetString();
        if (instanceName == null || method == null)
        {
            return new InvokationRequestError(uuid, "Invalid message format.");
        }

        try
        {
            var instance = ipcContainer.GetInstance(instanceName);
            var ipcMethod = instance.Methods.SingleOrDefault(m => m.MethodName == method);
            if (ipcMethod == null)
            {
                throw new InvalidOperationException($"Method '{method}' not found on instance '{instanceName}'.");
            }

            var methodInfo = ipcMethod.MethodInfo;
            var parameters = methodInfo.GetParameters();
            var typedParameters = jsonDocument.RootElement.GetProperty("args")
                .EnumerateArray()
                .Select<JsonElement, object?>((jsonElement, index) =>
                {
                    var targetType = parameters[index].ParameterType;

                    return jsonElement.ValueKind switch
                    {
                        JsonValueKind.Number when targetType == typeof(int) => jsonElement.GetInt32(),
                        JsonValueKind.Number when targetType == typeof(long) => jsonElement.GetInt64(),
                        JsonValueKind.Number when targetType == typeof(double) => jsonElement.GetDouble(),
                        JsonValueKind.String when targetType == typeof(string) => jsonElement.GetString(),
                        JsonValueKind.True when targetType == typeof(bool) => true,
                        JsonValueKind.False when targetType == typeof(bool) => false,
                        JsonValueKind.Null when targetType == typeof(object) => null,
                        JsonValueKind.Object when targetType.IsClass || targetType.IsInterface =>
                            JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType),
                        JsonValueKind.Array when targetType.IsArray =>
                            JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType),
                        JsonValueKind.Undefined => null,
                        _ => throw new InvalidOperationException(
                            $"Unsupported conversion from {jsonElement.ValueKind} to {targetType}")
                    };
                })
                .ToArray();

            var result = await InvokeMethodAsync(instance, ipcMethod, typedParameters);
            return new InvokationRequestMessage(uuid, result);
        }
        catch (Exception ex)
        {
            return new InvokationRequestError(uuid, ex.Message);
        }
    }


    /// <summary>
    /// Tries to invoke either a sync or an async method of an ipc instance - method and return the result.
    /// </summary>
    /// <param name="ipcInstance">The IPC Instance</param>
    /// <param name="ipcMethod">The IPC Method</param>
    /// <param name="args">The positional arguments that will be passed to the method.</param>
    /// <returns>The result of the method or the awaited result of a Task</returns>
    private static async Task<object?> InvokeMethodAsync(IpcInstance ipcInstance, IpcMethod ipcMethod,
        object?[]? args = null)
    {
        var method = ipcMethod.MethodInfo;

        var returnType = method.ReturnType;
        var isAsync = returnType == typeof(Task) ||
                      (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));

        var result = method.Invoke(ipcInstance.Instance, args);

        if (!isAsync) return result;

        var task = (Task)result!;
        await task;

        if (!returnType.IsGenericType) return null;

        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
}