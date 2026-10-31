using Newtonsoft.Json;
using BSL.v41.Logic.Database;
using BSL.v41.Logic.Environment.LaserCommand.Laser;
using BSL.v41.Logic.Environment.LaserListener;
using BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;
using BSL.v41.Logic.Environment.LaserMessage.Sepo.Event;
using BSL.v41.Logic.Environment.LaserMessage.Sepo.Home.Laser;
using BSL.v41.Supercell.Titan.CommonUtils.Utils;
using BSL.v41.Titan.Graphic;
using BSL.v41.Titan.Utilities;
using BSL.v41.Tools.LaserCsv;
using BSL.v41.Tools.LaserCsv.Manufacturer.Laser;

namespace BSL.v41.Logic.Dynamic;

public class EventSlotConfig
{
    [JsonProperty("slot")] public int Slot { get; set; }
    [JsonProperty("category")] public string Category { get; set; } = "";
    [JsonProperty("game_modes")] public List<string> GameModes { get; set; } = new();
    [JsonProperty("locations")] public List<int> Locations { get; set; } = new(); // pinned maps (locations.csv index).
    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
}

public class EventStructureFile
{
    [JsonProperty("slots")] public List<EventSlotConfig> Slots { get; set; } = new();
    [JsonProperty("rotate_seconds")] public int RotateSeconds { get; set; } // 0 = DefaultSecTimeForUpdate.
}

public static class DynamicServerParameters
{
    private static readonly object Gate = new();
    private static readonly Random Rnd = new();
    private static long _nextRotate;

    public static Dictionary<int, EventData> Event1DataMassive { get; private set; } = new();
    public static Dictionary<int, EventData> Event2DataMassive { get; private set; } = new();

    public static long DefaultSecTimeForUpdate { get; set; }
    public static string PathToJson { get; set; } = null!;

    public static long NextRotateTime => Interlocked.Read(ref _nextRotate);

    public static EventStructureFile LoadConfig()
    {
        lock (Gate)
        {
            try
            {
                return JsonConvert.DeserializeObject<EventStructureFile>(File.ReadAllText(PathToJson)) ??
                       new EventStructureFile();
            }
            catch
            {
                return new EventStructureFile();
            }
        }
    }

    public static void SaveConfig(EventStructureFile config)
    {
        lock (Gate)
        {
            File.WriteAllText(PathToJson, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }

    public static int GetIntervalSeconds(EventStructureFile? config = null)
    {
        config ??= LoadConfig();
        return config.RotateSeconds > 0 ? config.RotateSeconds : (int)Math.Max(DefaultSecTimeForUpdate, 5);
    }

    public static void InitializeRotateEvents()
    {
        RebuildEvents();
    }

    // Rebuilds every event slot from the config. Safe to call from any thread (admin console / rotator).
    public static void RebuildEvents()
    {
        lock (Gate)
        {
            var config = LoadConfig();
            var interval = GetIntervalSeconds(config);
            var now = LogicTimeUtil.GetTimestamp();

            var all = LogicDataTables.GetAllDataFromCsvById(CsvHelperTable.Locations.GetId());
            var locations = all == null! ? new List<LogicLocationData>() : all.Cast<LogicLocationData>().ToList();

            var current = new Dictionary<int, EventData>();
            var upcoming = new Dictionary<int, EventData>();
            var old = Event1DataMassive;

            foreach (var slot in config.Slots.Where(x => x.Enabled && x.Slot > 0).OrderBy(x => x.Slot))
            {
                var previous = old.TryGetValue(slot.Slot - 1, out var prev) ? prev.GetLocation() - 15 * 1000000 : -1;
                var instance = PickLocation(slot, locations, previous);
                if (instance < 0) continue;

                current[slot.Slot - 1] = new EventData(now + interval, slot.Slot, instance, 0);
                upcoming[slot.Slot - 1] = new EventData(now + interval, slot.Slot, instance, 0, true);
            }

            Event1DataMassive = current;
            Event2DataMassive = upcoming;
            Interlocked.Exchange(ref _nextRotate, now + interval);
        }
    }

    private static int PickLocation(EventSlotConfig slot, List<LogicLocationData> locations, int previous)
    {
        if (slot.Locations.Count > 0)
        {
            var pinned = slot.Locations.Where(x => x != previous).ToList();
            if (pinned.Count == 0) pinned = slot.Locations;
            return pinned[Rnd.Next(pinned.Count)];
        }

        if (slot.GameModes.Count == 0) return -1;

        var mode = slot.GameModes[Rnd.Next(slot.GameModes.Count)];
        var pool = locations.Where(x => !x.GetDisabled() && x.GetGameModeVariation() == mode).ToList();
        if (pool.Count == 0)
            pool = locations.Where(x => !x.GetDisabled() && slot.GameModes.Contains(x.GetGameModeVariation()))
                .ToList();
        if (pool.Count == 0) return -1;

        var fresh = pool.Where(x => x.GetInstanceId() != previous).ToList();
        if (fresh.Count > 0) pool = fresh;

        return pool[Rnd.Next(pool.Count)].GetInstanceId();
    }

    public static void BroadcastConf()
    {
        List<KeyValuePair<long, LogicGameListener>> listeners;
        try
        {
            listeners = IdentifierListener.GetLogicGameListeners().ToList();
        }
        catch
        {
            return;
        }

        foreach (var gameListener in listeners)
            try
            {
                var account = Databases.AccountDatabase.LoadAccount(gameListener.Key);
                if (account == null!) continue;

                var availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.SetServerCommand(new LogicDayChangedCommand(new LogicConfData(account)));
                gameListener.Value.Send(availableServerCommandMessage);
            }
            catch
            {
                // ignored.
            }
    }

    public static void EventRotator()
    {
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000);

                try
                {
                    if (LogicTimeUtil.GetTimestamp() < Interlocked.Read(ref _nextRotate)) continue;

                    RebuildEvents();
                    BroadcastConf();
                }
                catch (Exception exception)
                {
                    ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Error, exception);
                }
            }
        }) { IsBackground = true }.Start();
    }

    public static void ShopRotator()
    {
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000);

                var moscowTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));

                if (moscowTime.Hour != 11) continue;

                for (var i = 1; i <= Databases.AccountDatabase.GetMaxAccountId(); i++)
                {
                    var accountModel = Databases.AccountDatabase.LoadAccount(i);
                    // todo.
                }
            }
        }) { IsBackground = true }.Start();
    }
}
