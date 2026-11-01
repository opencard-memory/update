using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class RespondToAllianceJoinRequestMessage : PiranhaMessage
{
    public RespondToAllianceJoinRequestMessage() { Helper.Skip(); }
    public long AccountId { get; private set; }
    public bool Accept { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AccountId = ByteStream.ReadLong(); Accept = ByteStream.ReadBoolean();
    }
    public override int GetMessageType() => 14321;
    public override int GetServiceNodeType() => 9;
}
