using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BSL.v41.Logic.Admin;
using BSL.v41.Logic.Database;
using BSL.v41.Logic.Dynamic;
using BSL.v41.Logic.Environment.LaserListener;
using BSL.v41.Supercell.Titan.CommonUtils.Utils;
using BSL.v41.Titan.Graphic;
using BSL.v41.Titan.Utilities;
using BSL.v41.Tools.LaserCsv;
using BSL.v41.Tools.LaserCsv.Manufacturer.Laser;

namespace BSL.v41.Manage.Admin;

public class AdminConfig
{
    public int Port { get; set; } = 9120;
    public string BindAddress { get; set; } = "0.0.0.0";
    public string Password { get; set; } = "";
}

internal sealed class NoLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Web based admin console (default port 9120, reachable from outside).
public static class AdminServer
{
    private const string CookieName = "bsl_admin";
    private const string ConfigPath = "SaveBase/admin_config.json";
    private const string AuditPath = "SaveBase/admin_audit.log";

    private static readonly int[] ValidSlots =
        [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 21, 22, 23, 24, 30, 31, 32];

    private static readonly int[] ValidShopTypes = [1, 16, 6, 14, 10, 8, 17, 28, 7, 9, 3, 4];

    private static readonly ConcurrentDictionary<string, DateTime> Sessions = new();
    private static readonly ConcurrentDictionary<string, List<DateTime>> LoginFailures = new();
    private static readonly object AuditGate = new();
    private static readonly DateTime StartedAt = DateTime.Now;

    private static AdminConfig _config = new();
    private static byte[] _html = [];

    public static void Start()
    {
        _config = LoadConfig();

        using (var stream = typeof(AdminServer).Assembly.GetManifestResourceStream("admin.html"))
        {
            if (stream != null)
            {
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                _html = memory.ToArray();
            }
        }

        Task.Run(async () =>
        {
            try
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.Logging.ClearProviders();
                builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoLifetime>());
                builder.WebHost.UseUrls($"http://{_config.BindAddress}:{_config.Port}");

                var app = builder.Build();
                app.Run(HandleAsync);

                ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Start,
                    $"Admin console started! http://{_config.BindAddress}:{_config.Port}/ (password: {_config.Password})");

                await app.RunAsync();
            }
            catch (Exception exception)
            {
                ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Error,
                    "Admin console failed to start: " + exception.Message);
            }
        });
    }

    private static AdminConfig LoadConfig()
    {
        AdminConfig? config = null;
        try
        {
            if (File.Exists(ConfigPath))
                config = JsonConvert.DeserializeObject<AdminConfig>(File.ReadAllText(ConfigPath));
        }
        catch
        {
            // ignored.
        }

        config ??= new AdminConfig();
        if (string.IsNullOrWhiteSpace(config.Password))
        {
            const string alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[14];
            for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            config.Password = new string(chars);
        }

        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch
        {
            // ignored.
        }

        return config;
    }

    // ---------------------------------------------------------------- http plumbing

    private static async Task HandleAsync(HttpContext context)
    {
        var path = (context.Request.Path.Value ?? "/").TrimEnd('/');
        if (path.Length == 0) path = "/";
        var method = context.Request.Method;

        try
        {
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            if (method == "GET" && (path == "/" || path == "/index.html"))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.Body.WriteAsync(_html);
                return;
            }

            if (!path.StartsWith("/api/"))
            {
                context.Response.StatusCode = 404;
                return;
            }

            JObject body = new();
            if (method == "POST")
            {
                if (context.Request.Headers["X-Requested-With"] != "bsl-admin")
                {
                    await Reply(context, 403, Err("잘못된 요청입니다."));
                    return;
                }

                if ((context.Request.ContentLength ?? 0) > 200_000)
                {
                    await Reply(context, 413, Err("요청이 너무 큽니다."));
                    return;
                }

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                var text = await reader.ReadToEndAsync();
                if (text.Length > 0) body = JObject.Parse(text);
            }

            if (path == "/api/login" && method == "POST")
            {
                await Login(context, body);
                return;
            }

            if (path == "/api/logout" && method == "POST")
            {
                if (context.Request.Cookies.TryGetValue(CookieName, out var oldToken)) Sessions.TryRemove(oldToken, out _);
                context.Response.Cookies.Delete(CookieName);
                await Reply(context, 200, Ok());
                return;
            }

            if (!IsAuthorized(context))
            {
                await Reply(context, 401, Err("로그인이 필요합니다."));
                return;
            }

            var query = context.Request.Query;
            object result;

            switch (method + " " + path)
            {
                case "GET /api/me":
                    result = Ok();
                    break;
                case "GET /api/status":
                    result = GetStatus();
                    break;
                case "POST /api/maintenance":
                    result = Audit(context, "maintenance", body, ConfigureMaintenance(body));
                    break;
                case "POST /api/user/admin":
                    result = Audit(context, "user.admin", body, SetUserAdmin(body));
                    break;
                case "GET /api/users":
                    result = GetUsers(query["q"].ToString(), query["filter"].ToString(), query["sort"].ToString());
                    break;
                case "GET /api/user":
                    result = GetUser(ParseLong(query["id"].ToString()));
                    break;
                case "POST /api/user/resource":
                    result = Audit(context, "resource", body, UserResource(body));
                    break;
                case "POST /api/user/trophies":
                    result = Audit(context, "trophies", body,
                        AdminOps.SetTrophies(Long(body, "id"), Int(body, "trophies"), Bool(body, "refresh")));
                    break;
                case "POST /api/user/rename":
                    result = Audit(context, "rename", body, AdminOps.Rename(Long(body, "id"), Str(body, "name")));
                    break;
                case "POST /api/user/ban":
                    result = Audit(context, "ban", body,
                        AdminOps.Ban(Long(body, "id"), Int(body, "type"), Int(body, "hours"), Str(body, "reason")));
                    break;
                case "POST /api/user/unban":
                    result = Audit(context, "unban", body, AdminOps.Unban(Long(body, "id")));
                    break;
                case "POST /api/user/kick":
                    result = Audit(context, "kick", body, AdminOps.Kick(Long(body, "id")));
                    break;
                case "GET /api/catalog":
                    result = GetCatalog();
                    break;
                case "GET /api/events":
                    result = GetEvents();
                    break;
                case "POST /api/events/save":
                    result = Audit(context, "events.save", body, SaveEvents(body));
                    break;
                case "POST /api/events/reroll":
                    result = Audit(context, "events.reroll", body, RerollEvents());
                    break;
                case "GET /api/shop":
                    result = ShopCatalog.Load();
                    break;
                case "POST /api/shop/save":
                    result = Audit(context, "shop.save", body, SaveShop(body));
                    break;
                case "POST /api/shop/delete":
                    result = Audit(context, "shop.delete", body, DeleteShop(body));
                    break;
                case "POST /api/mail":
                    result = Audit(context, "mail", body, SendMail(body));
                    break;
                case "GET /api/audit":
                    result = GetAudit();
                    break;
                default:
                    await Reply(context, 404, Err("알 수 없는 요청입니다."));
                    return;
            }

            if (result is AdminResult adminResult)
            {
                await Reply(context, adminResult.Ok ? 200 : 400,
                    new { ok = adminResult.Ok, message = adminResult.Message });
                return;
            }

            await Reply(context, 200, result);
        }
        catch (Exception exception)
        {
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Error, "Admin console: " + exception.Message);
            await Reply(context, 500, Err("서버 오류: " + exception.Message));
        }
    }

    private static async Task Reply(HttpContext context, int status, object value)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(value));
    }

    private static object Ok()
    {
        return new { ok = true };
    }

    private static object Err(string message)
    {
        return new { ok = false, message };
    }

    private static bool IsAuthorized(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var token)) return false;
        if (!Sessions.TryGetValue(token, out var expires)) return false;
        if (expires < DateTime.UtcNow)
        {
            Sessions.TryRemove(token, out _);
            return false;
        }

        Sessions[token] = DateTime.UtcNow.AddHours(12);
        return true;
    }

    private static async Task Login(HttpContext context, JObject body)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "?";
        var failures = LoginFailures.GetOrAdd(ip, _ => new List<DateTime>());

        lock (failures)
        {
            failures.RemoveAll(x => x < DateTime.UtcNow.AddMinutes(-10));
            if (failures.Count >= 5)
            {
                context.Response.StatusCode = 429;
            }
        }

        if (context.Response.StatusCode == 429)
        {
            await Reply(context, 429, Err("로그인 시도가 너무 많습니다. 10분 뒤 다시 시도하세요."));
            return;
        }

        var given = SHA256.HashData(Encoding.UTF8.GetBytes(Str(body, "password")));
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_config.Password));

        if (!CryptographicOperations.FixedTimeEquals(given, expected))
        {
            lock (failures)
            {
                failures.Add(DateTime.UtcNow);
            }

            WriteAudit(ip, "login.fail", "");
            await Reply(context, 401, Err("비밀번호가 올바르지 않습니다."));
            return;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        Sessions[token] = DateTime.UtcNow.AddHours(12);
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromHours(12)
        });

        WriteAudit(ip, "login.ok", "");
        await Reply(context, 200, Ok());
    }

    // ---------------------------------------------------------------- audit

    private static AdminResult Audit(HttpContext context, string action, JObject body, AdminResult result)
    {
        var detail = body.ToString(Formatting.None);
        if (detail.Length > 300) detail = detail[..300];
        WriteAudit(context.Connection.RemoteIpAddress?.ToString() ?? "?", action,
            (result.Ok ? "OK " : "FAIL ") + detail + " -> " + result.Message);
        return result;
    }

    private static void WriteAudit(string ip, string action, string detail)
    {
        try
        {
            lock (AuditGate)
            {
                File.AppendAllText(AuditPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{ip}\t{action}\t{detail.Replace('\n', ' ')}{System.Environment.NewLine}");
            }
        }
        catch
        {
            // ignored.
        }
    }

    private static object GetAudit()
    {
        try
        {
            lock (AuditGate)
            {
                if (!File.Exists(AuditPath)) return new { lines = Array.Empty<string>() };
                var lines = File.ReadAllLines(AuditPath);
                return new { lines = lines.Skip(Math.Max(0, lines.Length - 200)).Reverse().ToArray() };
            }
        }
        catch
        {
            return new { lines = Array.Empty<string>() };
        }
    }

    // ---------------------------------------------------------------- body helpers

    private static string Str(JObject o, string key)
    {
        return o.TryGetValue(key, out var token) && token.Type != JTokenType.Null ? token.ToString() : "";
    }

    private static int Int(JObject o, string key, int fallback = 0)
    {
        return o.TryGetValue(key, out var token) && int.TryParse(token.ToString(), out var value) ? value : fallback;
    }

    private static long Long(JObject o, string key)
    {
        return o.TryGetValue(key, out var token) && long.TryParse(token.ToString(), out var value) ? value : -1;
    }

    private static bool Bool(JObject o, string key)
    {
        return o.TryGetValue(key, out var token) && token.Type == JTokenType.Boolean && token.Value<bool>();
    }

    private static long ParseLong(string text)
    {
        return long.TryParse(text, out var value) ? value : -1;
    }

    private static AdminResult ConfigureMaintenance(JObject body)
    {
        var message = Str(body, "message");
        if (message.Length > 300) return AdminResult.Fail("점검 안내 문구는 300자 이내로 입력하세요.");

        DateTimeOffset? start = null;
        DateTimeOffset? end = null;
        var startText = Str(body, "startUtc");
        var endText = Str(body, "endUtc");
        if (!string.IsNullOrWhiteSpace(startText))
        {
            if (!DateTimeOffset.TryParse(startText, out var parsedStart))
                return AdminResult.Fail("점검 시작 시각이 올바르지 않습니다.");
            start = parsedStart.ToUniversalTime();
        }
        if (!string.IsNullOrWhiteSpace(endText))
        {
            if (!DateTimeOffset.TryParse(endText, out var parsedEnd))
                return AdminResult.Fail("점검 종료 시각이 올바르지 않습니다.");
            end = parsedEnd.ToUniversalTime();
        }
        if (end.HasValue && (!start.HasValue || end.Value <= start.Value))
            return AdminResult.Fail("점검 종료 시각은 시작 시각보다 뒤여야 합니다.");

        MaintenanceControl.Configure(Bool(body, "enabled"), message, start, end);
        return AdminResult.Success("점검 설정을 저장했습니다.");
    }

    private static AdminResult SetUserAdmin(JObject body)
    {
        var id = Long(body, "id");
        if (id <= 0 || AdminOps.Resolve(id) == null)
            return AdminResult.Fail("유저를 찾을 수 없습니다.");
        var enabled = Bool(body, "enabled");
        MaintenanceControl.SetAdmin(id, enabled);
        return AdminResult.Success(enabled ? "관리자로 지정했습니다." : "관리자 권한을 해제했습니다.");
    }

    // ---------------------------------------------------------------- status / users

    private static object GetStatus()
    {
        var maintenance = MaintenanceControl.GetSnapshot();
        var accounts = AdminOps.GetAllAccounts();
        var now = LogicTimeUtil.GetTimestamp();
        var online = accounts.Where(x => AdminOps.IsOnline(x.GetAccountId())).ToList();
        var banned = accounts.Where(x => x.IsBannedNow()).ToList();

        var locations = LocationLookup();
        var events = DynamicServerParameters.Event1DataMassive.Values.OrderBy(x => x.GetSlot()).Select(x =>
        {
            var instance = x.GetLocation() - 15 * 1000000;
            locations.TryGetValue(instance, out var location);
            return new
            {
                slot = x.GetSlot(),
                map = location?.GetMap() ?? "?",
                mode = location?.GetGameModeVariation() ?? "?",
                endsIn = Math.Max(x.GetEndTime() - now, 0)
            };
        }).ToList();

        return new
        {
            serverTime = now,
            uptimeSeconds = (long)(DateTime.Now - StartedAt).TotalSeconds,
            memoryMb = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024,
            totalAccounts = accounts.Count,
            online = online.Count,
            banned = banned.Count,
            permanentBanned = banned.Count(x => x.GetBanType() == 2),
            totalGems = accounts.Sum(x => (long)x.GetResource(AdminOps.Gems)),
            totalGold = accounts.Sum(x => (long)x.GetResource(AdminOps.Gold)),
            onlineUsers = online.Take(50).Select(AdminOps.Summary).ToList(),
            topTrophies = accounts.Select(AdminOps.Summary)
                .OrderByDescending(x => Convert.ToInt32(x["trophies"])).Take(5).ToList(),
            events,
            maintenanceEnabled = maintenance.Enabled,
            maintenanceManualEnabled = maintenance.ManualEnabled,
            maintenanceScheduledActive = maintenance.ScheduledActive,
            maintenanceMessage = maintenance.Message,
            maintenanceStartUtc = maintenance.StartUtc,
            maintenanceEndUtc = maintenance.EndUtc,
            shopOffers = ShopCatalog.Load().Count(x => x.Enabled && !ShopCatalog.IsExpired(x)),
            nextRotateIn = Math.Max(DynamicServerParameters.NextRotateTime - now, 0)
        };
    }

    private static object GetUsers(string q, string filter, string sort)
    {
        var accounts = AdminOps.GetAllAccounts().Select(AdminOps.Summary).ToList();
        var total = accounts.Count;

        q = (q ?? "").Trim();
        if (q.Length > 0)
        {
            var id = AdminOps.ParseTagOrId(q);
            accounts = accounts.Where(x =>
                Convert.ToString(x["name"])!.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                Convert.ToString(x["tag"])!.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (id >= 0 && Convert.ToInt64(x["id"]) == id)).ToList();
        }

        if (filter == "online") accounts = accounts.Where(x => (bool)x["online"]!).ToList();
        else if (filter == "banned") accounts = accounts.Where(x => Convert.ToInt32(x["banType"]) != 0).ToList();

        accounts = sort switch
        {
            "trophies" => accounts.OrderByDescending(x => Convert.ToInt32(x["trophies"])).ToList(),
            "recent" => accounts.OrderByDescending(x => Convert.ToInt32(x["lastOnline"])).ToList(),
            _ => accounts.OrderBy(x => Convert.ToInt64(x["id"])).ToList()
        };

        return new { total, matched = accounts.Count, users = accounts.Take(200).ToList() };
    }

    private static object GetUser(long id)
    {
        var account = AdminOps.Resolve(id);
        if (account == null) return Err("유저를 찾을 수 없습니다.");
        return new { ok = true, user = AdminOps.Summary(account), mails = AdminOps.GetMails(id) };
    }

    private static AdminResult UserResource(JObject body)
    {
        var id = Long(body, "id");
        var refresh = Bool(body, "refresh");
        var resourceId = Str(body, "resource") switch
        {
            "gems" => AdminOps.Gems,
            "gold" => AdminOps.Gold,
            "power" => AdminOps.PowerPoints,
            _ => -1
        };
        if (resourceId < 0) return AdminResult.Fail("알 수 없는 재화입니다.");

        var amount = Int(body, "amount");
        return Str(body, "mode") == "set"
            ? AdminOps.SetResource(id, resourceId, amount, refresh)
            : AdminOps.AddResource(id, resourceId, amount, refresh);
    }

    // ---------------------------------------------------------------- game data (events / shop pickers)

    private static Dictionary<int, LogicLocationData> LocationLookup()
    {
        var result = new Dictionary<int, LogicLocationData>();
        var all = LogicDataTables.GetAllDataFromCsvById(CsvHelperTable.Locations.GetId());
        if (all == null!) return result;

        foreach (var data in all)
            if (data is LogicLocationData location)
                result[location.GetInstanceId()] = location;
        return result;
    }

    private static object GetCatalog()
    {
        var locations = LocationLookup().Values.OrderBy(x => x.GetInstanceId()).ToList();

        var modes = locations.GroupBy(x => x.GetGameModeVariation()).Select(g => new
        {
            id = g.Key,
            total = g.Count(),
            enabled = g.Count(x => !x.GetDisabled())
        }).OrderByDescending(x => x.enabled).ThenBy(x => x.id).ToList();

        var brawlers = new List<object>();
        var characters = LogicDataTables.GetAllDataFromCsvById(CsvHelperTable.Characters.GetId());
        if (characters != null!)
            foreach (var data in characters)
                if (data is LogicCharacterData character && !character.GetDisabled() &&
                    !string.IsNullOrWhiteSpace(character.GetItemName()))
                    brawlers.Add(new { id = character.GetInstanceId(), name = character.GetItemName() });

        var skins = new List<object>();
        var skinData = LogicDataTables.GetAllDataFromCsvById(CsvHelperTable.Skins.GetId());
        if (skinData != null!)
            foreach (var data in skinData)
                skins.Add(new { id = data.GetInstanceId(), name = data.GetName() });

        return new
        {
            slots = ValidSlots,
            modes,
            locations = locations.Select(x => new
            {
                id = x.GetInstanceId(),
                name = x.GetName(),
                map = x.GetMap(),
                mode = x.GetGameModeVariation(),
                disabled = x.GetDisabled()
            }).ToList(),
            brawlers,
            skins
        };
    }

    // ---------------------------------------------------------------- events

    private static object GetEvents()
    {
        var config = DynamicServerParameters.LoadConfig();
        var now = LogicTimeUtil.GetTimestamp();
        var locations = LocationLookup();

        return new
        {
            config,
            intervalSeconds = DynamicServerParameters.GetIntervalSeconds(config),
            nextRotateIn = Math.Max(DynamicServerParameters.NextRotateTime - now, 0),
            current = DynamicServerParameters.Event1DataMassive.Values.OrderBy(x => x.GetSlot()).Select(x =>
            {
                var instance = x.GetLocation() - 15 * 1000000;
                locations.TryGetValue(instance, out var location);
                return new
                {
                    slot = x.GetSlot(),
                    location = instance,
                    map = location?.GetMap() ?? "?",
                    mode = location?.GetGameModeVariation() ?? "?",
                    endsIn = Math.Max(x.GetEndTime() - now, 0)
                };
            }).ToList()
        };
    }

    private static AdminResult SaveEvents(JObject body)
    {
        var locations = LocationLookup();
        var knownModes = locations.Values.Select(x => x.GetGameModeVariation()).ToHashSet();
        var config = new EventStructureFile();

        var rotate = Int(body, "rotate_seconds");
        if (rotate != 0 && (rotate < 10 || rotate > 7 * 24 * 3600))
            return AdminResult.Fail("교체 주기가 올바르지 않습니다.");
        config.RotateSeconds = rotate;

        if (body["slots"] is not JArray slots) return AdminResult.Fail("슬롯 정보가 없습니다.");

        var used = new HashSet<int>();
        foreach (var token in slots.OfType<JObject>())
        {
            var slot = Int(token, "slot");
            if (!ValidSlots.Contains(slot)) return AdminResult.Fail($"사용할 수 없는 슬롯 번호입니다: {slot}");
            if (!used.Add(slot)) return AdminResult.Fail($"슬롯 {slot} 이(가) 중복되었습니다.");

            var entry = new EventSlotConfig
            {
                Slot = slot,
                Category = Str(token, "category").Trim(),
                Enabled = !token.TryGetValue("enabled", out var enabled) || enabled.Type != JTokenType.Boolean ||
                          enabled.Value<bool>()
            };
            if (entry.Category.Length > 30) entry.Category = entry.Category[..30];

            if (token["game_modes"] is JArray modes)
                foreach (var mode in modes.Select(x => x.ToString()).Distinct())
                    if (knownModes.Contains(mode))
                        entry.GameModes.Add(mode);

            if (token["locations"] is JArray pinned)
                foreach (var location in pinned.Select(x => (int)x).Distinct())
                    if (locations.ContainsKey(location))
                        entry.Locations.Add(location);

            if (entry.Enabled && entry.GameModes.Count == 0 && entry.Locations.Count == 0)
                return AdminResult.Fail($"슬롯 {slot}: 이벤트 종류(모드) 또는 맵을 선택하세요.");

            config.Slots.Add(entry);
        }

        lock (AdminOps.Gate)
        {
            DynamicServerParameters.SaveConfig(config);
            DynamicServerParameters.RebuildEvents();
            DynamicServerParameters.BroadcastConf();
        }

        return AdminResult.Success("이벤트를 저장하고 적용했습니다.");
    }

    private static AdminResult RerollEvents()
    {
        lock (AdminOps.Gate)
        {
            DynamicServerParameters.RebuildEvents();
            DynamicServerParameters.BroadcastConf();
        }

        return AdminResult.Success("이벤트 맵을 새로 뽑았습니다.");
    }

    // ---------------------------------------------------------------- shop

    private static AdminResult SaveShop(JObject body)
    {
        if (body["offer"] is not JObject raw) return AdminResult.Fail("상품 정보가 없습니다.");

        var offer = new ShopOfferEntry
        {
            Id = Int(raw, "Id"),
            Title = Str(raw, "Title").Trim(),
            Enabled = !raw.TryGetValue("Enabled", out var enabled) || enabled.Type != JTokenType.Boolean ||
                      enabled.Value<bool>(),
            PriceType = Int(raw, "PriceType"),
            Price = Int(raw, "Price", 1),
            OldPrice = Int(raw, "OldPrice", 0),
            DurationHours = Int(raw, "DurationHours"),
            Daily = Bool(raw, "Daily"),
            Confirm = Bool(raw, "Confirm")
        };

        if (offer.Title.Length is < 1 or > 40) return AdminResult.Fail("상품 이름은 1~40자여야 합니다.");
        if (offer.PriceType is < 0 or > 3) return AdminResult.Fail("가격 종류가 올바르지 않습니다.");
        if (offer.Price is < 1 or > 999_999) return AdminResult.Fail("가격은 1~999,999 사이여야 합니다.");
        if (offer.OldPrice < offer.Price) offer.OldPrice = offer.Price;
        if (offer.DurationHours is < 0 or > 24 * 90) return AdminResult.Fail("판매 기간은 0~2160시간입니다.");

        var brawlers = LogicDataTables.GetDataFromTable(CsvHelperTable.Characters.GetId());
        var skins = LogicDataTables.GetDataFromTable(CsvHelperTable.Skins.GetId());

        if (raw["Items"] is not JArray items || items.Count == 0) return AdminResult.Fail("구성품을 1개 이상 추가하세요.");
        if (items.Count > 6) return AdminResult.Fail("구성품은 최대 6개입니다.");

        foreach (var token in items.OfType<JObject>())
        {
            var item = new ShopItemEntry
            {
                Type = Int(token, "Type"),
                Count = Math.Clamp(Int(token, "Count", 1), 1, 999_999),
                BrawlerId = Int(token, "BrawlerId"),
                SkinId = Int(token, "SkinId")
            };

            if (!ValidShopTypes.Contains(item.Type)) return AdminResult.Fail("지원하지 않는 구성품 종류입니다.");
            if (item.Type == (int)ShopItemHelperTable.GuaranteedHero &&
                (brawlers == null! || item.BrawlerId < 0 || item.BrawlerId >= brawlers.GetItemCount()))
                return AdminResult.Fail("브롤러를 선택하세요.");
            if (item.Type == (int)ShopItemHelperTable.Skin &&
                (skins == null! || item.SkinId < 0 || item.SkinId >= skins.GetItemCount()))
                return AdminResult.Fail("스킨을 선택하세요.");

            offer.Items.Add(item);
        }

        lock (AdminOps.Gate)
        {
            var offers = ShopCatalog.Load();
            var existing = offers.FirstOrDefault(x => x.Id == offer.Id && offer.Id > 0);

            if (existing == null)
            {
                offer.Id = offers.Count == 0 ? 1 : offers.Max(x => x.Id) + 1;
                offer.StartTime = LogicTimeUtil.GetTimestamp();
                offers.Add(offer);
            }
            else
            {
                offer.StartTime = Bool(body, "restart") ? LogicTimeUtil.GetTimestamp() : existing.StartTime;
                offers[offers.IndexOf(existing)] = offer;
            }

            ShopCatalog.Save(offers);
        }

        if (Bool(body, "push")) PushHomeToOnline();
        return AdminResult.Success("상품을 저장했습니다.");
    }

    private static AdminResult DeleteShop(JObject body)
    {
        lock (AdminOps.Gate)
        {
            var offers = ShopCatalog.Load();
            var removed = offers.RemoveAll(x => x.Id == Int(body, "id"));
            if (removed == 0) return AdminResult.Fail("상품을 찾을 수 없습니다.");
            ShopCatalog.Save(offers);
        }

        if (Bool(body, "push")) PushHomeToOnline();
        return AdminResult.Success("상품을 삭제했습니다.");
    }

    private static void PushHomeToOnline()
    {
        try
        {
            foreach (var id in IdentifierListener.GetLogicGameListeners().Keys.ToList()) AdminOps.RefreshHome(id);
        }
        catch
        {
            // ignored.
        }
    }

    // ---------------------------------------------------------------- mail

    private static AdminResult SendMail(JObject body)
    {
        var target = Str(body, "target");
        var id = target == "all" ? -1 : ParseLong(target);
        if (target != "all" && id < 0) return AdminResult.Fail("받는 유저를 선택하세요.");

        return AdminOps.SendMail(id, Str(body, "text"), Math.Clamp(Int(body, "gems"), 0, 999_999),
            Math.Clamp(Int(body, "gold"), 0, 999_999), Bool(body, "refresh"));
    }
}
