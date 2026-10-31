using BSL.v41.Logic.Database;
using BSL.v41.Logic.Database.Account;
using BSL.v41.Logic.Environment.LaserCommand.Laser;
using BSL.v41.Logic.Environment.LaserListener;
using BSL.v41.Logic.Environment.LaserMessage.Laser.Node_9;
using BSL.v41.Logic.Environment.LaserNotification;
using BSL.v41.Logic.Environment.LaserNotification.Laser;
using BSL.v41.Supercell.Titan.CommonUtils.Utils.Own;
using BSL.v41.Titan.Mathematical;
using BSL.v41.Titan.Utilities;

namespace BSL.v41.Logic.Admin;

public class AdminResult(bool ok, string message)
{
    public bool Ok { get; } = ok;
    public string Message { get; } = message;

    public static AdminResult Success(string message = "완료")
    {
        return new AdminResult(true, message);
    }

    public static AdminResult Fail(string message)
    {
        return new AdminResult(false, message);
    }
}

// All state changing operations used by the admin console live here.
public static class AdminOps
{
    public const int Gems = 0;
    public const int Gold = 1;
    public const int PowerPoints = 8;

    public static readonly object Gate = new();

    private static readonly LogicLongToCodeConverterUtil TagConverter =
        new(Table.Hashtag, Table.ConversionTagCharacters);

    // ---------- lookup ----------

    public static string GetTag(long accountId)
    {
        try
        {
            return TagConverter.ToCode(new LogicLong(0, (int)accountId)) ?? "#" + accountId;
        }
        catch
        {
            return "#" + accountId;
        }
    }

    public static long ParseTagOrId(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return -1;

        if (long.TryParse(text, out var id)) return id;

        try
        {
            if (!text.StartsWith('#')) text = "#" + text;
            var logicLong = TagConverter.ToId(text.ToUpperInvariant());
            return logicLong.GetLowerInt();
        }
        catch
        {
            return -1;
        }
    }

    public static bool IsOnline(long accountId)
    {
        return IdentifierListener.GetV2LogicGameListenerByAccountId(accountId);
    }

    public static int GetOnlineCount()
    {
        try
        {
            return IdentifierListener.GetLogicGameListeners().Count;
        }
        catch
        {
            return 0;
        }
    }

    // When a player is online the connection owns the "live" AccountModel and keeps saving it,
    // so every change has to be applied to that object (otherwise it is overwritten on the next keep-alive).
    public static AccountModel? Resolve(long accountId)
    {
        try
        {
            if (IsOnline(accountId))
            {
                var live = IdentifierListener.GetLogicGameListenerByAccountId(accountId).GetLogicHomeMode()
                    .AccountModel;
                if (live != null) return live;
            }
        }
        catch
        {
            // ignored.
        }

        var account = Databases.AccountDatabase.LoadAccount(accountId);
        return account == null! ? null : account;
    }

    public static List<AccountModel> GetAllAccounts()
    {
        var result = new List<AccountModel>();
        foreach (var account in Databases.AccountDatabase.GetAllAccounts())
            result.Add(Resolve(account.GetAccountId()) ?? account);
        return result;
    }

    private static string Str(AccountModel account, AccountStructure structure)
    {
        return account.GetFieldValueByAccountStructureParameterFromAccountModel(structure)?.ToString() ?? "";
    }

    private static int Int(AccountModel account, AccountStructure structure)
    {
        try
        {
            return Convert.ToInt32(account.GetFieldValueByAccountStructureParameterFromAccountModel(structure) ?? 0);
        }
        catch
        {
            return 0;
        }
    }

    public static Dictionary<string, object?> Summary(AccountModel account)
    {
        var id = account.GetAccountId();
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["tag"] = GetTag(id),
            ["name"] = Str(account, AccountStructure.AvatarName),
            ["trophies"] = Int(account, AccountStructure.Trophies),
            ["maxTrophies"] = Int(account, AccountStructure.MaxTrophies),
            ["gems"] = account.GetResource(Gems),
            ["gold"] = account.GetResource(Gold),
            ["powerPoints"] = account.GetResource(PowerPoints),
            ["online"] = IsOnline(id),
            ["banType"] = account.IsBannedNow() ? account.GetBanType() : 0,
            ["banEnd"] = account.GetBanEndTime(),
            ["banReason"] = account.GetBanReason(),
            ["lastOnline"] = Int(account, AccountStructure.LastOnlineTime),
            ["created"] = Str(account, AccountStructure.AccountCreatedDate),
            ["playTime"] = Int(account, AccountStructure.PlayTimeSeconds),
            ["sessions"] = Int(account, AccountStructure.SessionCount),
            ["allianceId"] = Convert.ToInt64(account.GetFieldValueByAccountStructureParameterFromAccountModel(
                AccountStructure.AllianceId) ?? 0L)
        };
    }

    // ---------- helpers ----------

    private static void Save(AccountModel account)
    {
        account.Persist();
    }

    public static void RefreshHome(long accountId)
    {
        try
        {
            var account = Resolve(accountId);
            if (account == null || !IsOnline(accountId)) return;
            IdentifierListener.GetLogicGameListenerByAccountId(accountId).Send(new OwnHomeDataMessage(account));
        }
        catch
        {
            // ignored.
        }
    }

    // ---------- currency / stats ----------

    public static AdminResult SetResource(long accountId, int resourceId, int amount, bool refresh)
    {
        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            account.SetResource(resourceId, amount);
            Save(account);
            if (refresh) RefreshHome(accountId);
            return AdminResult.Success("재화를 수정했습니다.");
        }
    }

    public static AdminResult AddResource(long accountId, int resourceId, int delta, bool refresh)
    {
        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            account.SetResource(resourceId, (int)Math.Clamp((long)account.GetResource(resourceId) + delta, 0, 999_999_999));
            Save(account);
            if (refresh) RefreshHome(accountId);
            return AdminResult.Success("재화를 수정했습니다.");
        }
    }

    public static AdminResult SetTrophies(long accountId, int trophies, bool refresh)
    {
        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            trophies = Math.Clamp(trophies, 0, 999_999);
            account.SetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.Trophies, trophies);
            if (Int(account, AccountStructure.MaxTrophies) < trophies)
                account.SetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.MaxTrophies,
                    trophies);
            if (refresh) RefreshHome(accountId);
            return AdminResult.Success("트로피를 수정했습니다.");
        }
    }

    // ---------- nickname ----------

    public static AdminResult Rename(long accountId, string name)
    {
        name = (name ?? "").Trim();
        if (name.Length is < 1 or > 15) return AdminResult.Fail("닉네임은 1~15자여야 합니다.");

        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            account.SetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.AvatarName, name);
            account.SetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure.NameSetByUser, true);

            try
            {
                if (IsOnline(accountId))
                {
                    var message = new AvailableServerCommandMessage();
                    message.SetServerCommand(new LogicChangeAvatarNameCommand(name, 0));
                    IdentifierListener.GetLogicGameListenerByAccountId(accountId).Send(message);
                }
            }
            catch
            {
                // ignored.
            }

            return AdminResult.Success("닉네임을 변경했습니다.");
        }
    }

    // ---------- kick / ban ----------

    public static AdminResult Kick(long accountId)
    {
        try
        {
            if (!IsOnline(accountId)) return AdminResult.Fail("접속 중이 아닙니다.");

            var listener = IdentifierListener.GetLogicGameListenerByAccountId(accountId);
            listener.Disconnect();
            IdentifierListener.RemoveLogicGameListenerByAccountId(accountId);
            return AdminResult.Success("접속을 종료했습니다.");
        }
        catch (Exception exception)
        {
            return AdminResult.Fail(exception.Message);
        }
    }

    // banType: 1 = temporary (hours), 2 = permanent.
    public static AdminResult Ban(long accountId, int banType, int hours, string? reason)
    {
        if (banType is not (1 or 2)) return AdminResult.Fail("잘못된 정지 종류입니다.");
        if (banType == 1 && hours < 1) return AdminResult.Fail("정지 시간을 선택하세요.");

        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            var end = banType == 1 ? LogicTimeUtil.GetTimestamp() + hours * 3600 : 0;
            account.SetBan(banType, end, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());
            Save(account);
        }

        Kick(accountId);
        return AdminResult.Success(banType == 2 ? "영구 정지했습니다." : $"{hours}시간 일시 정지했습니다.");
    }

    public static AdminResult Unban(long accountId)
    {
        lock (Gate)
        {
            var account = Resolve(accountId);
            if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");

            account.ClearBan();
            Save(account);
            return AdminResult.Success("정지를 해제했습니다.");
        }
    }

    // ---------- inbox (mail) ----------

    // targetAccountId < 0 = every account.
    public static AdminResult SendMail(long targetAccountId, string text, int gems, int gold, bool refresh)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return AdminResult.Fail("내용을 입력하세요.");
        if (text.Length > 500) return AdminResult.Fail("내용은 500자 이내여야 합니다.");

        lock (Gate)
        {
            List<AccountModel> targets;
            if (targetAccountId < 0)
            {
                targets = GetAllAccounts();
            }
            else
            {
                var account = Resolve(targetAccountId);
                if (account == null) return AdminResult.Fail("유저를 찾을 수 없습니다.");
                targets = [account];
            }

            if (targets.Count == 0) return AdminResult.Fail("받을 유저가 없습니다.");

            var database = Databases.NotificationIntraSignedDatabase;
            var factories = database.LoadAll();
            var now = LogicTimeUtil.GetTimestamp();
            var pushes = new List<KeyValuePair<long, FreeTextNotification>>();

            foreach (var account in targets)
            {
                var key = Convert.ToInt64(
                    account.GetFieldValueByAccountStructureParameterFromAccountModel(
                        AccountStructure.NotificationFactoryIntraSignedId) ?? 0L);
                if (key <= 0) continue;

                if (!factories.TryGetValue(key, out var factory))
                {
                    factory = new NotificationFactory { AccountModel = account };
                    factories[key] = factory;
                }

                var notification = new FreeTextNotification(text, now);
                factory.AddWithoutSave(notification);
                pushes.Add(new KeyValuePair<long, FreeTextNotification>(account.GetAccountId(), notification));
            }

            database.SaveAll(factories);

            foreach (var push in pushes)
                try
                {
                    if (!IsOnline(push.Key)) continue;
                    var message = new AvailableServerCommandMessage();
                    message.SetServerCommand(new LogicAddNotificationCommand(push.Value));
                    IdentifierListener.GetLogicGameListenerByAccountId(push.Key).Send(message);
                }
                catch
                {
                    // ignored.
                }

            if (gems > 0 || gold > 0)
                foreach (var push in pushes)
                {
                    var account = Resolve(push.Key);
                    if (account == null) continue;

                    if (gems > 0) account.SetResource(Gems, (int)Math.Min((long)account.GetResource(Gems) + gems, 999_999_999));
                    if (gold > 0) account.SetResource(Gold, (int)Math.Min((long)account.GetResource(Gold) + gold, 999_999_999));
                    Save(account);
                    if (refresh) RefreshHome(push.Key);
                }

            return AdminResult.Success($"{pushes.Count}명에게 우편을 보냈습니다.");
        }
    }

    // ---------- inbox history of one player ----------

    public static List<Dictionary<string, object?>> GetMails(long accountId)
    {
        var result = new List<Dictionary<string, object?>>();
        var account = Resolve(accountId);
        if (account == null) return result;

        try
        {
            var key = Convert.ToInt64(account.GetFieldValueByAccountStructureParameterFromAccountModel(
                AccountStructure.NotificationFactoryIntraSignedId) ?? 0L);
            var factory = Databases.NotificationIntraSignedDatabase.GetAppendix(key);
            foreach (var mail in factory.FreeTextNotifications.OrderByDescending(x => x.Key).Take(30))
                result.Add(new Dictionary<string, object?>
                    { ["text"] = mail.Value.Text, ["time"] = mail.Value.Time, ["read"] = mail.Value.IsRead });
        }
        catch
        {
            // ignored.
        }

        return result;
    }
}
