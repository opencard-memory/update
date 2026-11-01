using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class SendAllianceInvitationMessage : PiranhaMessage
{
    public SendAllianceInvitationMessage() { Helper.Skip(); }
    public long AccountId { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AccountId = ByteStream.ReadLong();
    }
    public override int GetMessageType() => 14322;
    public override int GetServiceNodeType() => 9;
}
