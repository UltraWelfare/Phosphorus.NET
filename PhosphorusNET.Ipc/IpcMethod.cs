using System.Reflection;

namespace PhosphorusNET.Ipc;

/// <summary>
/// Holds a method and its name.
/// </summary>
public class IpcMethod
{
    public string MethodName { get; set; }
    public MethodInfo MethodInfo { get; set; }

    public IpcMethod(string methodName, MethodInfo methodInfo)
    {
        MethodName = methodName;
        MethodInfo = methodInfo;
    }
}