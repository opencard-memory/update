using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class SetTeamChatMutedMessage : PiranhaMessage
{
    private bool _muted;
    public SetTeamChatMutedMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _muted = ByteStream.ReadBoolean();
    }
    public bool GetMuted() => _muted;
    public override int GetMessageType() => 14778;
    public override int GetServiceNodeType() => 9;
}
