using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class SetInvitesBlockedMessage : PiranhaMessage
{
    private bool _blocked;
    public SetInvitesBlockedMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _blocked = ByteStream.ReadVInt() != 0;
    }
    public bool GetBlocked() => _blocked;
    public override int GetMessageType() => 14777;
    public override int GetServiceNodeType() => 9;
}
