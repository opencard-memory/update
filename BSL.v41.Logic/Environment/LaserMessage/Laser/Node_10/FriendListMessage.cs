using BSL.v41.Logic.Database;
using BSL.v41.Logic.Database.Account;
using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_10;

public sealed class FriendListMessage : PiranhaMessage
{
    private readonly List<long> _friends;

    public FriendListMessage() { Helper.Skip(); _friends = new List<long>(); }

    public FriendListMessage(AccountModel owner, IEnumerable<long> friends)
    {
        _friends = friends.Distinct().ToList();
    }

    public override void Encode()
    {
        base.Encode();
        ByteStream.WriteInt(0);
        ByteStream.WriteInt(_friends.Count);
        foreach (var friendId in _friends)
        {
            var friend = Databases.AccountDatabase.LoadAccount(friendId);
            ByteStream.WriteLong(friendId);
            ByteStream.WriteBoolean(false);
            ByteStream.WriteString(friend?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName)?.ToString() ?? "Unknown");
            ByteStream.WriteString("");
            ByteStream.WriteString("");
            ByteStream.WriteString("");
            ByteStream.WriteInt(0);
            ByteStream.WriteInt(1);
            ByteStream.WriteInt(0);
            ByteStream.WriteInt(Convert.ToInt32(friend?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.Trophies) ?? 0));
            ByteStream.WriteInt(0);
            ByteStream.WriteInt(-1);
            ByteStream.WriteInt(0);
            ByteStream.WriteBoolean(false);
            ByteStream.WriteBoolean(false);
        }
    }

    public override int GetMessageType() => 20105;
    public override int GetServiceNodeType() => 3;
}
