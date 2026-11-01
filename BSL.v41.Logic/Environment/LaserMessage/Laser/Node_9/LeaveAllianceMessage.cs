using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class LeaveAllianceMessage : PiranhaMessage
{
    public LeaveAllianceMessage() { Helper.Skip(); }
    
    public override void Decode()
    {
        base.Decode();
        
    }
    public override int GetMessageType() => 14308;
    public override int GetServiceNodeType() => 9;
}
