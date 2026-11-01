using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamChatMessage : PiranhaMessage
{
    private string _text = string.Empty;
    public TeamChatMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _text = ByteStream.ReadString();
    }
    public string GetText() => _text;
    public override int GetMessageType() => 14359;
    public override int GetServiceNodeType() => 9;
}
