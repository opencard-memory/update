using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamKickMessage : PiranhaMessage
{
    private long _targetAccountId;
    public TeamKickMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        var high = ByteStream.ReadVInt();
        var low = ByteStream.ReadVInt();
        _targetAccountId = ((long)high << 32) | (uint)low;
    }
    public long GetTargetAccountId() => _targetAccountId;
    public override int GetMessageType() => 14352;
    public override int GetServiceNodeType() => 9;
}
