using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class AskForJoinableAlliancesListMessage : PiranhaMessage
{
    public AskForJoinableAlliancesListMessage() { Helper.Skip(); }
    
    public override void Decode()
    {
        base.Decode();
        
    }
    public override int GetMessageType() => 14303;
    public override int GetServiceNodeType() => 9;
}
