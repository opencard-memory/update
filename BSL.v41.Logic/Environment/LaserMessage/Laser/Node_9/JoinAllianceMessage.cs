using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class JoinAllianceMessage : PiranhaMessage
{
    public JoinAllianceMessage() { Helper.Skip(); }
    public long AllianceId { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AllianceId = ByteStream.ReadLong();
    }
    public override int GetMessageType() => 14305;
    public override int GetServiceNodeType() => 9;
}
