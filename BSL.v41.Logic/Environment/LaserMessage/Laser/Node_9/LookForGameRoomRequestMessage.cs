using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class LookForGameRoomRequestMessage : PiranhaMessage
{
    private int _eventId;
    private int _eventSlot;
    public LookForGameRoomRequestMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _eventId = ByteStream.ReadVInt();
        _eventSlot = ByteStream.ReadVInt();
    }
    public int GetEventId() => _eventId;
    public int GetEventSlot() => _eventSlot;
    public override int GetMessageType() => 14199;
    public override int GetServiceNodeType() => 9;
}
