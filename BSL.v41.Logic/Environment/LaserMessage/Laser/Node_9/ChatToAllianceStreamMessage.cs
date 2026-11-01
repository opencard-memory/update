using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class ChatToAllianceStreamMessage : PiranhaMessage
{
    public ChatToAllianceStreamMessage() { Helper.Skip(); }
    public string Message { get; private set; } = string.Empty;
    public override void Decode()
    {
        base.Decode();
        Message = ByteStream.ReadString(2048);
    }
    public override int GetMessageType() => 14315;
    public override int GetServiceNodeType() => 9;
}
