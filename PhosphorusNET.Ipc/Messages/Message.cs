namespace PhosphorusNET.Ipc.Messages;

public class Message
{
    public string Uuid { get; set; }

    public MessageType Type { get; set; }

    public Message(string uuid, MessageType type)
    {
        Uuid = uuid;
        Type = type;
    }
}