namespace PhosphorusNET.Ipc.Messages;

public class InvokationRequestMessage : Message
{
    public object? Data { get; set; }

    public InvokationRequestMessage(string uuid, object? data) : base(uuid, MessageType.InvokationRequest)
    {
        Data = data;
    }
}