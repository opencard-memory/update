using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamJoinMessage : PiranhaMessage
{
    private int _roomId;
    public TeamJoinMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _ = ByteStream.ReadVInt();
        _roomId = ByteStream.ReadVInt();
        _ = ByteStream.ReadVInt();
    }
    public int GetRoomId() => _roomId;
    public override int GetMessageType() => 14351;
    public override int GetServiceNodeType() => 9;
}
