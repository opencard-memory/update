using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamRequestJoinApproveMessage : PiranhaMessage
{
    private long _joinerId;
    private bool _state;
    private bool _mutePlayer;
    public TeamRequestJoinApproveMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _joinerId = ByteStream.ReadLong();
        _state = ByteStream.ReadBoolean();
        _mutePlayer = ByteStream.ReadBoolean();
    }
    public long GetJoinerId() => _joinerId;
    public bool GetState() => _state;
    public bool GetMutePlayer() => _mutePlayer;
    public override int GetMessageType() => 14882;
    public override int GetServiceNodeType() => 23;
}
