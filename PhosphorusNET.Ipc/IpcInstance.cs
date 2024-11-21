using System.Collections.Generic;

namespace PhosphorusNET.Ipc;

/// <summary>
/// Holds an instance and its name.
/// </summary>
public class IpcInstance
{
    public string InstanceName { get; set; }
    public object Instance { get; set; }

    public IpcInstance(string instanceName, object instance)
    {
        InstanceName = instanceName;
        Instance = instance;
    }

    public List<IpcMethod> Methods { get; set; } = new();
}