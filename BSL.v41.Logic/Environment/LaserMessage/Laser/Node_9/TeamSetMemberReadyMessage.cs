using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamSetMemberReadyMessage : PiranhaMessage
{
    public TeamSetMemberReadyMessage() { Helper.Skip(); }
    public override int GetMessageType() => 14355;
    public override int GetServiceNodeType() => 9;
}
