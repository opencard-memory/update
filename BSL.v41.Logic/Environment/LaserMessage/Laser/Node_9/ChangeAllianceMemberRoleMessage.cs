using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class ChangeAllianceMemberRoleMessage : PiranhaMessage
{
    public ChangeAllianceMemberRoleMessage() { Helper.Skip(); }
    public long AccountId { get; private set; }
    public int Role { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AccountId = ByteStream.ReadLong(); Role = ByteStream.ReadVInt();
    }
    public override int GetMessageType() => 14306;
    public override int GetServiceNodeType() => 9;
}
