using PhosphorusNET.Ipc;

namespace WpfApp.Ipc;

public class Container : IpcContainer
{
    public Container()
    {
        Register("calculator", new Calculator());
    }
}