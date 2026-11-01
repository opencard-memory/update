using BSL.v41.Supercell.Titan.CommonUtils;
using BSL.v41.Titan.Mathematical;

namespace BSL.v41.Logic.Environment.LaserMessage.Laser.Node_10;

public class FriendAvatarBaseMessage : PiranhaMessage
{
    private LogicLong _avatarId = new();

    public FriendAvatarBaseMessage() { Helper.Skip(); }

    public override void Decode()
    {
        base.Decode();
        _avatarId = new LogicLong().Decode(ByteStream);
    }

    public override int GetMessageType() => 0;
    public long GetAvatarId() => (long)_avatarId;
    public override int GetServiceNodeType() => 3;
}
