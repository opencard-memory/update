using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class SearchAlliancesMessage : PiranhaMessage
{
    public SearchAlliancesMessage() { Helper.Skip(); }
    public string SearchText { get; private set; } = string.Empty;
    public override void Decode()
    {
        base.Decode();
        SearchText = ByteStream.ReadString(1024);
    }
    public override int GetMessageType() => 14324;
    public override int GetServiceNodeType() => 9;
}
