using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using PhosphorusNET.Ipc.Common;

namespace PhosphorusNET.Ipc;

public abstract class IpcContainer
{
    private readonly Dictionary<string, IpcInstance> _instanceCache = new();
    public List<IpcInstance> Instances { get; } = new();

    public void RegisterHttp(string instanceName = "http", HttpClient? httpClient = null) => Register(instanceName, new Http(httpClient));

    public IpcContainer Register(string instanceName, object target)
    {
        if (Instances.Any(instance => instance.InstanceName == instanceName))
        {
            throw new InvalidOperationException("This instance name already exists in the container");
        }

        var ipcInstance = new IpcInstance(instanceName, target);

        IEnumerable<MethodInfo> methods;
        if (target.GetType().GetCustomAttribute<IpcExposeAttribute>() is not null)
        {
            methods = target.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }
        else
        {
            methods = target.GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttribute<IpcExposeAttribute>() is not null);
        }

        ipcInstance.Methods = methods
            .Select(m => new IpcMethod(m.GetCustomAttribute<IpcExposeAttribute>()?.InstanceName ?? m.Name, m))
            .ToList();

        Instances.Add(ipcInstance);

        return this;
    }


    public IpcInstance GetInstance(string instanceName)
    {
        if (_instanceCache.TryGetValue(instanceName, out var instance)) return instance;
        instance = Instances.FirstOrDefault(i => i.InstanceName == instanceName);
        _instanceCache[instanceName] = instance ?? throw new ArgumentException($"Instance '{instanceName}' not found.");
        return instance;
    }
}