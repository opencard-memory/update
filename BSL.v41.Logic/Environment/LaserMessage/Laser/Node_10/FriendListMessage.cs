using BSL.v41.Logic.Database.Account;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_10;

public sealed class FriendListMessage : PiranhaMessage
{
    private readonly List<long> _friends;
    private readonly AccountModel _owner;

    public FriendListMessage(AccountModel owner, IEnumerable<long> friends)
    {
        _owner = owner;
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
            ByteStream.WriteBoolean(false); // home id is not present in this v41 entry variant
            ByteStream.WriteString(friend?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName)?.ToString() ?? "Unknown");
            ByteStream.WriteString("");
            ByteStream.WriteString("");
            ByteStream.WriteString("");
            ByteStream.WriteInt(0); // protection duration
            ByteStream.WriteInt(1); // experience level
            ByteStream.WriteInt(0); // league type
            ByteStream.WriteInt(Convert.ToInt32(friend?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.Trophies) ?? 0));
            ByteStream.WriteInt(0); // duel score
            ByteStream.WriteInt(-1); // friend state
            ByteStream.WriteInt(0);
            ByteStream.WriteBoolean(false); // alliance
            ByteStream.WriteBoolean(false); // live replay
        }
    }

    public override int GetMessageType() => 20105;
    public override int GetServiceNodeType() => 3;
}
