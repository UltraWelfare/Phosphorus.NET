using System;

namespace PhosphorusNET.Ipc;

/// <summary>
/// The attribute is used to identify which methods should be exposed via IPC. <br/>
/// It can be placed directly on a Method, or globally in the Class to avoid repetition. <br/>
/// When placed on a Method there is a constructor with a string parameter to rename the Method on IPC calls. <br/>
/// When placed on a Method without an instanceName (parameterless constructor) it will take the name of the method <br/>
/// </summary>
/// <remarks>
/// When using the class attribute, the instanceName is ignored.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IpcExposeAttribute : Attribute
{
    public string? InstanceName { get; }

    public IpcExposeAttribute(string instanceName)
    {
        InstanceName = instanceName;
    }

    public IpcExposeAttribute()
    {
        InstanceName = null;
    }
}