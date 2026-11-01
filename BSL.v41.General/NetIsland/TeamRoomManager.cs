using BSL.v41.General.Networking;
using BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;
using BSL.v41.Logic.Environment.LaserListener;
using BSL.v41.Logic.Database;
using BSL.v41.Logic.Database.Account;

namespace BSL.v41.General.NetIsland;

public static class TeamRoomManager
{
    private sealed class Member(ServerConnection connection)
    {
        public ServerConnection Connection { get; } = connection;
        public bool Ready { get; set; }
    }

    private sealed class Room(int id, int roomType, int mapId)
    {
        public int Id { get; } = id;
        public int RoomType { get; } = roomType;
        public int MapId { get; } = mapId;
        public long LeaderId { get; set; }
        public int StreamTick { get; set; }
        public List<Member> Members { get; } = new();
    }

    private static readonly object Sync = new();
    private static readonly Dictionary<int, Room> Rooms = new();
    private static readonly Dictionary<long, int> PlayerRooms = new();
    private static readonly Dictionary<long, HashSet<int>> PendingInvites = new();
    private static readonly HashSet<long> InvitesBlocked = new();
    private static readonly HashSet<long> TeamChatMuted = new();
    private static readonly Dictionary<int, HashSet<long>> PendingJoinRequests = new();
    private static int _nextRoomId = 1000;

    public static bool Create(ServerConnection connection, int roomType, int mapId)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (PlayerRooms.ContainsKey(accountId)) return false;

            var id = Interlocked.Increment(ref _nextRoomId);
            var room = new Room(id, roomType, mapId) { LeaderId = accountId };
            room.Members.Add(new Member(connection));
            Rooms.Add(id, room);
            PlayerRooms.Add(accountId, id);
            Broadcast(room);
            return true;
        }
    }

    public static bool Invite(ServerConnection inviterConnection, long targetAccountId)
    {
        lock (Sync)
        {
            var inviterId = inviterConnection.GetAccountModel().GetAccountId();
            if (InvitesBlocked.Contains(targetAccountId) ||
                !PlayerRooms.TryGetValue(inviterId, out var roomId) ||
                !Rooms.ContainsKey(roomId) ||
                !inviterConnection.GetAccountModel().GetFriendIds().Contains(targetAccountId) ||
                Databases.AccountDatabase.LoadAccount(targetAccountId) == null! ||
                !IdentifierListener.GetV2LogicGameListenerByAccountId(targetAccountId))
                return false;

            if (!PendingInvites.TryGetValue(targetAccountId, out var invites))
                PendingInvites[targetAccountId] = invites = new HashSet<int>();
            invites.Add(roomId);

            try
            {
                IdentifierListener.GetLogicGameListenerByAccountId(targetAccountId).Send(
                    new TeamInvitationMessage(roomId, inviterConnection.GetAccountModel()));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static bool RespondToInvite(ServerConnection connection, int roomId, bool accept)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (!PendingInvites.TryGetValue(accountId, out var invites) || !invites.Remove(roomId))
                return false;
            if (invites.Count == 0) PendingInvites.Remove(accountId);
        }

        return !accept || Join(connection, roomId);
    }

    public static bool Join(ServerConnection connection, int roomId)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (PlayerRooms.ContainsKey(accountId) || !Rooms.TryGetValue(roomId, out var room) ||
                room.Members.Count >= 3) return false;

            room.Members.Add(new Member(connection));
            PlayerRooms.Add(accountId, roomId);
            if (PendingJoinRequests.TryGetValue(roomId, out var requests)) { requests.Remove(accountId); if (requests.Count == 0) PendingJoinRequests.Remove(roomId); }
            Broadcast(room);
            return true;
        }
    }

    public static void Leave(ServerConnection connection, int reason = 0)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (!PlayerRooms.TryGetValue(accountId, out var roomId) ||
                !Rooms.TryGetValue(roomId, out var room)) return;

            room.Members.RemoveAll(m => m.Connection.GetAccountModel().GetAccountId() == accountId);
            PlayerRooms.Remove(accountId);
            if (room.LeaderId == accountId && room.Members.Count > 0)
                room.LeaderId = room.Members[0].Connection.GetAccountModel().GetAccountId();
            if (PendingJoinRequests.TryGetValue(roomId, out var requests))
            {
                requests.Remove(accountId);
                if (requests.Count == 0) PendingJoinRequests.Remove(roomId);
            }
            PendingInvites.Remove(accountId);
            try { connection.GetMessaging()!.Send(new TeamLeftServerMessage(reason)); } catch { }

            if (room.Members.Count == 0)
            {
                Rooms.Remove(roomId);
                PendingJoinRequests.Remove(roomId);
            };
            else
                Broadcast(room);
        }
    }

    public static void ToggleReady(ServerConnection connection)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (!PlayerRooms.TryGetValue(accountId, out var roomId) ||
                !Rooms.TryGetValue(roomId, out var room)) return;

            var member = room.Members.FirstOrDefault(m =>
                m.Connection.GetAccountModel().GetAccountId() == accountId);
            if (member == null) return;
            member.Ready = !member.Ready;
            Broadcast(room);
        }
    }

    public static IReadOnlyList<ServerConnection> GetMembers(long accountId)
    {
        lock (Sync)
        {
            if (!PlayerRooms.TryGetValue(accountId, out var roomId) ||
                !Rooms.TryGetValue(roomId, out var room))
                return Array.Empty<ServerConnection>();
            return room.Members.Select(member => member.Connection).ToArray();
        }
    }

    public static bool IsInRoom(long accountId)
    {
        lock (Sync) return PlayerRooms.ContainsKey(accountId);
    }

    public static int GetRoomId(long accountId)
    {
        lock (Sync) return PlayerRooms.GetValueOrDefault(accountId);
    }


    public static bool Kick(ServerConnection leaderConnection, long targetAccountId)
    {
        lock (Sync)
        {
            var leaderId = leaderConnection.GetAccountModel().GetAccountId();
            if (!PlayerRooms.TryGetValue(leaderId, out var roomId) ||
                !Rooms.TryGetValue(roomId, out var room) || room.LeaderId != leaderId ||
                targetAccountId == leaderId) return false;
            var target = room.Members.FirstOrDefault(m =>
                m.Connection.GetAccountModel().GetAccountId() == targetAccountId);
            if (target == null) return false;
            room.Members.Remove(target);
            PlayerRooms.Remove(targetAccountId);
            try { target.Connection.GetMessaging()!.Send(new TeamLeftServerMessage(1)); } catch { }
            Broadcast(room);
            return true;
        }
    }

    public static bool SendChat(ServerConnection sender, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        message = message.Trim();
        if (message.Length > 200) message = message[..200];
        lock (Sync)
        {
            var senderId = sender.GetAccountModel().GetAccountId();
            if (!PlayerRooms.TryGetValue(senderId, out var roomId) ||
                !Rooms.TryGetValue(roomId, out var room)) return false;
            var name = sender.GetAccountModel()
                .GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName)?.ToString() ?? "Brawler";
            room.StreamTick++;
            foreach (var member in room.Members)
            {
                if (TeamChatMuted.Contains(member.Connection.GetAccountModel().GetAccountId())) continue;
                try { member.Connection.GetMessaging()!.Send(new TeamStreamMessage(roomId, room.StreamTick, senderId, name, message)); } catch { }
            }
            return true;
        }
    }

    public static void SetInvitesBlocked(ServerConnection connection, bool blocked)
    {
        lock (Sync)
        {
            var id = connection.GetAccountModel().GetAccountId();
            if (blocked) InvitesBlocked.Add(id); else InvitesBlocked.Remove(id);
            if (blocked) PendingInvites.Remove(id);
        }
    }

    public static void SetChatMuted(ServerConnection connection, bool muted)
    {
        lock (Sync)
        {
            var id = connection.GetAccountModel().GetAccountId();
            if (muted) TeamChatMuted.Add(id); else TeamChatMuted.Remove(id);
        }
    }

    public static IReadOnlyList<(int RoomId, int RoomType, int MapId, int Members)> FindOpenRooms()
    {
        lock (Sync)
            return Rooms.Values.Where(r => r.Members.Count < 3)
                .Select(r => (r.Id, r.RoomType, r.MapId, r.Members.Count)).ToArray();
    }

    public static bool RequestJoin(ServerConnection requester, int roomId)
    {
        lock (Sync)
        {
            var id = requester.GetAccountModel().GetAccountId();
            if (PlayerRooms.ContainsKey(id) || !Rooms.TryGetValue(roomId, out var room) || room.Members.Count >= 3)
                return false;
            if (!PendingJoinRequests.TryGetValue(roomId, out var requests))
                PendingJoinRequests[roomId] = requests = new HashSet<long>();
            requests.Add(id);
            return true;
        }
    }

    private static void Broadcast(Room room)
    {
        var members = room.Members.Select(m =>
            (m.Connection.GetAccountModel(), m.Ready)).ToList();
        foreach (var member in room.Members)
        {
            try { member.Connection.GetMessaging()!.Send(new TeamRoomMessage(room.Id, room.RoomType, room.MapId, members)); }
            catch { /* disconnected member is removed by connection cleanup */ }
        }
    }
}
