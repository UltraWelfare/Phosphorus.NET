namespace PhosphorusNET.Ipc.Messages;

internal class InvokationRequestError : Message
{
    public string Error { get; set; }

    public InvokationRequestError(string uuid, string error) : base(uuid, MessageType.InvokationRequest)
    {
        Error = error;
    }
}