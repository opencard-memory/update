using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class AskForAllianceStreamMessage : PiranhaMessage
{
    public AskForAllianceStreamMessage() { Helper.Skip(); }
    
    public override void Decode()
    {
        base.Decode();
        
    }
    public override int GetMessageType() => 14304;
    public override int GetServiceNodeType() => 9;
}
