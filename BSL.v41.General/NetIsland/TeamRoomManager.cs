using BSL.v41.General.Networking;
using BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;

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
        public List<Member> Members { get; } = new();
    }

    private static readonly object Sync = new();
    private static readonly Dictionary<int, Room> Rooms = new();
    private static readonly Dictionary<long, int> PlayerRooms = new();
    private static int _nextRoomId = 1000;

    public static bool Create(ServerConnection connection, int roomType, int mapId)
    {
        lock (Sync)
        {
            var accountId = connection.GetAccountModel().GetAccountId();
            if (PlayerRooms.ContainsKey(accountId)) return false;

            var id = Interlocked.Increment(ref _nextRoomId);
            var room = new Room(id, roomType, mapId);
            room.Members.Add(new Member(connection));
            Rooms.Add(id, room);
            PlayerRooms.Add(accountId, id);
            Broadcast(room);
            return true;
        }
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
            try { connection.GetMessaging()!.Send(new TeamLeftServerMessage(reason)); } catch { }

            if (room.Members.Count == 0)
                Rooms.Remove(roomId);
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

    public static bool IsInRoom(long accountId)
    {
        lock (Sync) return PlayerRooms.ContainsKey(accountId);
    }

    public static int GetRoomId(long accountId)
    {
        lock (Sync) return PlayerRooms.GetValueOrDefault(accountId);
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
