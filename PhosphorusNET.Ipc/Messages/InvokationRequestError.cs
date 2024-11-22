namespace PhosphorusNET.Ipc.Messages;

public class InvokationRequestError : Message
{
    public string Error { get; set; }

    public InvokationRequestError(string uuid, string error) : base(uuid, MessageType.InvokationRequest)
    {
        Error = error;
    }
}