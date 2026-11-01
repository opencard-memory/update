using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamRequestJoinMessage : PiranhaMessage
{
    private long _playerId;
    private long _teamId;
    public TeamRequestJoinMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _playerId = ByteStream.ReadLong();
        _teamId = ByteStream.ReadLong();
    }
    public long GetPlayerId() => _playerId;
    public long GetTeamId() => _teamId;
    public override int GetMessageType() => 14881;
    public override int GetServiceNodeType() => 9;
}
