namespace PhosphorusNET.Ipc.Messages;

internal class InvokationRequestMessage : Message
{
    public object? Data { get; set; }

    public InvokationRequestMessage(string uuid, object? data) : base(uuid, MessageType.InvokationRequest)
    {
        Data = data;
    }
}