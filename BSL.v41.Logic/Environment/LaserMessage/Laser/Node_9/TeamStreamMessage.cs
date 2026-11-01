using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamStreamMessage : PiranhaMessage
{
    private readonly int _roomId;
    private readonly int _tick;
    private readonly long _senderId;
    private readonly string _senderName;
    private readonly string _text;

    public TeamStreamMessage() { Helper.Skip(); }
    public TeamStreamMessage(int roomId, int tick, long senderId, string senderName, string text)
    {
        _roomId = roomId;
        _tick = tick;
        _senderId = senderId;
        _senderName = senderName;
        _text = text;
    }

    public override void Encode()
    {
        base.Encode();
        ByteStream.WriteVInt(0);
        ByteStream.WriteVInt(_roomId);
        ByteStream.WriteVInt(1);
        ByteStream.WriteVInt(2);
        ByteStream.WriteVInt(0);
        ByteStream.WriteVInt(_tick);
        ByteStream.WriteVInt(0);
        ByteStream.WriteLong(_senderId);
        ByteStream.WriteString(_senderName);
        ByteStream.WriteVInt(0);
        ByteStream.WriteVInt(0);
        ByteStream.WriteBoolean(false);
        ByteStream.WriteString(_text);
    }

    public override int GetMessageType() => 24131;
    public override int GetServiceNodeType() => 9;
}
