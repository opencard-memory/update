using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamLeaveMessage : PiranhaMessage
{
    public TeamLeaveMessage() { Helper.Skip(); }
    public override int GetMessageType() => 14353;
    public override int GetServiceNodeType() => 9;
}
