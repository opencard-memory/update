using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamInviteMessage : PiranhaMessage
{
    private long _targetAccountId;
    public TeamInviteMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _targetAccountId = ByteStream.ReadVLong();
        _ = ByteStream.ReadVInt();
    }
    public long GetTargetAccountId() => _targetAccountId;
    public override int GetMessageType() => 14365;
    public override int GetServiceNodeType() => 9;
}
