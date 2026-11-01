using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamCreateMessage : PiranhaMessage
{
    private int _mapSlot;
    private int _mapId;
    private int _roomType;
    public TeamCreateMessage() { Helper.Skip(); }
    public override void Decode()
    {
        base.Decode();
        _mapSlot = ByteStream.ReadVInt();
        _mapId = ByteStream.ReadVInt();
        _roomType = ByteStream.ReadVInt();
    }
    public int GetMapSlot() => _mapSlot;
    public int GetMapId() => _mapId;
    public int GetRoomType() => _roomType;
    public override int GetMessageType() => 14350;
    public override int GetServiceNodeType() => 9;
}
