using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamLeftServerMessage : PiranhaMessage
{
    private int _reason;
    public TeamLeftServerMessage() { Helper.Skip(); }
    public TeamLeftServerMessage(int reason) { _reason = reason; }
    public override void Encode()
    {
        base.Encode();
        ByteStream.WriteInt(_reason);
    }
    public override int GetMessageType() => 24125;
    public override int GetServiceNodeType() => 9;
}
