using BSL.v41.Logic.Database.Account;
using BSL.v41.Supercell.Titan.CommonUtils;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

public sealed class TeamInvitationMessage : PiranhaMessage
{
    private long _roomId;
    private AccountModel? _inviter;
    public TeamInvitationMessage() { Helper.Skip(); }
    public TeamInvitationMessage(long roomId, AccountModel inviter)
    {
        _roomId = roomId;
        _inviter = inviter;
    }
    public override void Encode()
    {
        base.Encode();
        ByteStream.WriteVInt(0);
        ByteStream.WriteLong(_roomId);
        var inviter = _inviter;
        ByteStream.WriteLong(inviter?.GetAccountId() ?? 0);
        ByteStream.WriteBoolean(false);
        ByteStream.WriteString(inviter?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName)?.ToString() ?? "Brawler");
        ByteStream.WriteString("");
        ByteStream.WriteString("");
        ByteStream.WriteString("");
        ByteStream.WriteInt(0);
        ByteStream.WriteInt(1);
        ByteStream.WriteInt(0);
        ByteStream.WriteInt(Convert.ToInt32(inviter?.GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.Trophies) ?? 0));
        ByteStream.WriteInt(0);
        ByteStream.WriteInt(0);
        ByteStream.WriteInt(0);
        ByteStream.WriteBoolean(false);
        ByteStream.WriteBoolean(false);
    }
    public override int GetMessageType() => 24589;
    public override int GetServiceNodeType() => 9;
}
