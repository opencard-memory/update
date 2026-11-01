using BSL.v41.Logic.Database.Account;
using BSL.v41.Supercell.Titan.CommonUtils;
using BSL.v41.Supercell.Titan.CommonUtils.Utils;
using BSL.v41.Titan.Mathematical.Data;
using BSL.v41.Tools.LaserCsv;
using BSL.v41.Titan.DataStream.Helps;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamRoomMessage : PiranhaMessage
{
    private int _roomId;
    private int _roomType;
    private int _mapId;
    private IReadOnlyList<(AccountModel Account, bool Ready)> _members = Array.Empty<(AccountModel, bool)>();

    public TeamRoomMessage() { Helper.Skip(); }

    public TeamRoomMessage(int roomId, int roomType, int mapId,
        IReadOnlyList<(AccountModel Account, bool Ready)> members)
    {
        _roomId = roomId;
        _roomType = roomType;
        _mapId = mapId;
        _members = members;
    }

    public override void Encode()
    {
        base.Encode();
        ByteStream.WriteVInt(_roomType);
        ByteStream.WriteBoolean(false);
        ByteStream.WriteVInt(3);
        ByteStream.WriteLong(_roomId);
        ByteStream.WriteVInt(1);
        ByteStream.WriteLong(0);
        ByteStream.WriteLong(0);
        ByteStreamHelper.WriteDataReference(ByteStream, GlobalId.CreateGlobalId(CsvHelperTable.Locations.GetId(), _mapId));
        ByteStream.WriteVInt(_members.Count);

        for (var i = 0; i < _members.Count; i++)
        {
            var (account, ready) = _members[i];
            var id = account.GetAccountId();
            var name = account.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName)?.ToString() ?? "Brawler";
            var brawler = Convert.ToInt32(account.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.HomeBrawlerGlobalId));
            var trophies = Convert.ToInt32(account.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.Trophies));

            ByteStream.WriteLong(id);
            ByteStream.WriteString(name);
            ByteStream.WriteVInt(1);
            ByteStreamHelper.WriteDataReference(ByteStream, brawler);
            ByteStreamHelper.WriteDataReference(ByteStream, 0);
            ByteStream.WriteVInt(trophies);
            ByteStream.WriteVInt(trophies);
            ByteStream.WriteVInt(1);
            ByteStream.WriteVInt(0);
            ByteStream.WriteVInt(1);
            ByteStream.WriteBoolean(ready);
            ByteStream.WriteVInt(0);
        }
    }

    public override int GetMessageType() => 24124;
    public override int GetServiceNodeType() => 9;
}
