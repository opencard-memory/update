using System.Text.Json;

namespace BSL.v41.Logic.Admin;

public sealed class MaintenanceSnapshot
{
    public bool Enabled { get; init; }
    public bool ManualEnabled { get; init; }
    public bool ScheduledActive { get; init; }
    public string Message { get; init; } = "";
    public string StartUtc { get; init; } = "";
    public string EndUtc { get; init; } = "";
}

// Shared by the game login handler and the admin web console.
public static class MaintenanceControl
{
    private const string MaintenancePath = "SaveBase/maintenance_mode.json";
    private const string AdminsPath = "SaveBase/admin_users.json";
    private static readonly object Gate = new();
    private static bool _manualEnabled;
    private static string _message = "서버 점검 중입니다. 잠시 후 다시 접속해 주세요.";
    private static DateTimeOffset? _startUtc;
    private static DateTimeOffset? _endUtc;
    private static readonly HashSet<long> AdminIds = LoadAdmins();

    static MaintenanceControl() => LoadMaintenance();

    public static MaintenanceSnapshot GetSnapshot()
    {
        lock (Gate)
        {
            var now = DateTimeOffset.UtcNow;
            var scheduled = _startUtc.HasValue && now >= _startUtc.Value &&
                            (!_endUtc.HasValue || now < _endUtc.Value);
            return new MaintenanceSnapshot
            {
                Enabled = _manualEnabled || scheduled,
                ManualEnabled = _manualEnabled,
                ScheduledActive = scheduled,
                Message = _message,
                StartUtc = _startUtc?.ToString("O") ?? "",
                EndUtc = _endUtc?.ToString("O") ?? ""
            };
        }
    }

    public static void Configure(bool manualEnabled, string message,
        DateTimeOffset? startUtc, DateTimeOffset? endUtc)
    {
        if (endUtc.HasValue && (!startUtc.HasValue || endUtc.Value <= startUtc.Value))
            throw new ArgumentException("점검 종료 시각은 시작 시각보다 뒤여야 합니다.");

        lock (Gate)
        {
            _manualEnabled = manualEnabled;
            if (!string.IsNullOrWhiteSpace(message)) _message = message.Trim();
            if (_message.Length > 300) _message = _message[..300];
            _startUtc = startUtc?.ToUniversalTime();
            _endUtc = endUtc?.ToUniversalTime();
            SaveMaintenance();
        }
    }

    public static bool IsAdmin(long accountId)
    {
        lock (AdminIds) return accountId > 0 && AdminIds.Contains(accountId);
    }

    public static void SetAdmin(long accountId, bool enabled)
    {
        if (accountId <= 0) throw new ArgumentOutOfRangeException(nameof(accountId));
        lock (AdminIds)
        {
            if (enabled) AdminIds.Add(accountId);
            else AdminIds.Remove(accountId);
            Directory.CreateDirectory("SaveBase");
            File.WriteAllText(AdminsPath, JsonSerializer.Serialize(AdminIds.OrderBy(x => x).ToArray(),
                new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void LoadMaintenance()
    {
        try
        {
            if (!File.Exists(MaintenancePath)) return;
            var state = JsonSerializer.Deserialize<MaintenanceConfig>(File.ReadAllText(MaintenancePath));
            if (state == null) return;
            _manualEnabled = state.ManualEnabled;
            if (!string.IsNullOrWhiteSpace(state.Message)) _message = state.Message.Length > 300 ? state.Message[..300] : state.Message;
            _startUtc = state.StartUtc;
            _endUtc = state.EndUtc;
        }
        catch
        {
            // Keep safe defaults if the config is missing or malformed.
        }
    }

    private static HashSet<long> LoadAdmins()
    {
        try
        {
            if (!File.Exists(AdminsPath)) return new HashSet<long>();
            var ids = JsonSerializer.Deserialize<long[]>(File.ReadAllText(AdminsPath));
            return ids == null ? new HashSet<long>() : ids.Where(x => x > 0).ToHashSet();
        }
        catch
        {
            return new HashSet<long>();
        }
    }

    private static void SaveMaintenance()
    {
        Directory.CreateDirectory("SaveBase");
        var state = new MaintenanceConfig
        {
            ManualEnabled = _manualEnabled,
            Message = _message,
            StartUtc = _startUtc,
            EndUtc = _endUtc
        };
        File.WriteAllText(MaintenancePath, JsonSerializer.Serialize(state,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class MaintenanceConfig
    {
        public bool ManualEnabled { get; set; }
        public string Message { get; set; } = "";
        public DateTimeOffset? StartUtc { get; set; }
        public DateTimeOffset? EndUtc { get; set; }
    }
}