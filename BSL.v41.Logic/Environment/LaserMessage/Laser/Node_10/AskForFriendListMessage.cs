using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_10;

public sealed class AskForFriendListMessage : PiranhaMessage
{
    public AskForFriendListMessage() { Helper.Skip(); }
    public override int GetMessageType() => 10504;
    public override int GetServiceNodeType() => 3;
}
