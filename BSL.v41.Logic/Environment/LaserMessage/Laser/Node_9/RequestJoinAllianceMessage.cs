using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class RequestJoinAllianceMessage : PiranhaMessage
{
    public RequestJoinAllianceMessage() { Helper.Skip(); }
    public long AllianceId { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AllianceId = ByteStream.ReadLong();
    }
    public override int GetMessageType() => 14317;
    public override int GetServiceNodeType() => 9;
}
