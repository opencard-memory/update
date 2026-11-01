using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class SendAllianceInvitationToFriendMessage : PiranhaMessage
{
    public SendAllianceInvitationToFriendMessage() { Helper.Skip(); }
    public long AccountId { get; private set; }
    public override void Decode()
    {
        base.Decode();
        AccountId = ByteStream.ReadLong();
    }
    public override int GetMessageType() => 14326;
    public override int GetServiceNodeType() => 9;
}
