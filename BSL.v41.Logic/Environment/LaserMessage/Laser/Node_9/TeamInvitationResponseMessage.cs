using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamInvitationResponseMessage : PiranhaMessage
{
    private int _response;
    private long _roomId;
    public TeamInvitationResponseMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _response = ByteStream.ReadVInt();
        _roomId = (long)new BSL.v41.Titan.Mathematical.LogicLong().Decode(ByteStream);
        _ = ByteStream.ReadBoolean();
    }
    public int GetResponse() => _response;
    public long GetRoomId() => _roomId;
    public override int GetMessageType() => 14479;
    public override int GetServiceNodeType() => 9;
}
