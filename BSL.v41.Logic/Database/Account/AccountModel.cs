using System.Reflection;
using Newtonsoft.Json;
using BSL.v41.Logic.Database.Alliance;
using BSL.v41.Logic.Environment.LaserMessage.Sepo.Alliance;
using BSL.v41.Logic.Environment.LaserMessage.Sepo.Home.Laser.Laser;
using BSL.v41.Tools.LaserFactory;
using BSL.v41.Titan.Utilities;

namespace BSL.v41.Logic.Database.Account;

public class AccountModel(string jsonAccountModel = null!)
{
    private readonly string? _jsonAccountModel = jsonAccountModel;

    [JsonProperty] private string? _accountCreatedDate; // private. Not use!
    [JsonProperty] private long _accountId; // private. Not use!
    [JsonProperty] private long _allianceId; // private. Not use!
    [JsonProperty] private AllianceMemberEntry? _allianceMemberEntry; // private. Not use!
    [JsonProperty] private string? _avatarName; // private. Not use!
    [JsonProperty] private int _homeBrawlerGlobalId; // private. Not use!
    [JsonProperty] private long _homeId; // private. Not use!
    [JsonProperty] private Dictionary<int, IntValueEntry>? _intValueEntryX; // private. Not use!
    [JsonProperty] private Dictionary<int, IntValueEntry>? _intValueEntryY; // private. Not use!
    [JsonProperty] private LanguageFactory _languageLaserFactory; // private. Not use!
    [JsonProperty] private int _lastCreateAllianceTime; // private. Not use!
    [JsonProperty] private int _lastOnlineTime; // private. Not use!
    [JsonProperty] private List<LogicOfferBundle>? _logicOfferBundleList; // private. Not use!
    [JsonProperty] private int _maxTrophies; // private. Not use!
    [JsonProperty] private int _nameChangeCount; // private. Not use!
    [JsonProperty] private int _nameChangeEndTime; // private. Not use!
    [JsonProperty] private int _nameColorGlobalId; // private. Not use!
    [JsonProperty] private bool _nameSetByUser; // private. Not use!
    [JsonProperty] private long _notificationFactoryIntraSignedId; // private. Not use!
    [JsonProperty] private string? _passToken; // private. Not use!
    [JsonProperty] private int _playerAge; // private. Not use!
    [JsonProperty] private int _playerStatus; // private. Not use!
    [JsonProperty] private int _playerThumbnailGlobalId; // private. Not use!
    [JsonProperty] private int _playTimeSeconds; // private. Not use!
    [JsonProperty] private int _sessionCount; // private. Not use!
    [JsonProperty] private int _startPlayTimeSeconds; // private. Not use!
    [JsonProperty] private string? _supportedCreator; // private. Not use!
    [JsonProperty] private int _trophies; // private. Not use!
    [JsonProperty] private HashSet<long> _friendIds = new(); // persistent friend relationships
    [JsonProperty] private int _trophyRoadProgress; // private. Not use!
    [JsonProperty] private bool _userIsAuthed; // private. Not use!

    // ---- admin console fields ----
    [JsonProperty] private Dictionary<int, int>? _resourceAmounts; // resources.csv instance id -> amount.
    [JsonProperty] private int _banType; // 0 = none, 1 = temporary, 2 = permanent.
    [JsonProperty] private int _banEndTime; // unix time (temporary ban).
    [JsonProperty] private string? _banReason;

    public int GetResource(int resourceId)
    {
        if (_resourceAmounts != null && _resourceAmounts.TryGetValue(resourceId, out var value)) return value;
        return resourceId == 8 ? 100 : 0; // 8 = power points (previous hard-coded default).
    }

    public void SetResource(int resourceId, int value)
    {
        _resourceAmounts ??= new Dictionary<int, int>();
        _resourceAmounts[resourceId] = Math.Clamp(value, 0, 999_999_999);
    }

    public Dictionary<int, int> GetResourcesForAvatar()
    {
        var result = new Dictionary<int, int>
        {
            { 0, GetResource(0) }, { 1, GetResource(1) }, { 8, GetResource(8) }, { 10, GetResource(10) }
        };
        if (_resourceAmounts != null)
            foreach (var pair in _resourceAmounts)
                result[pair.Key] = pair.Value;
        return result;
    }

    public int GetBanType()
    {
        return _banType;
    }

    public int GetBanEndTime()
    {
        return _banEndTime;
    }

    public string GetBanReason()
    {
        return _banReason ?? "";
    }

    public bool IsBannedNow()
    {
        return _banType == 2 || (_banType == 1 && _banEndTime > LogicTimeUtil.GetTimestamp());
    }

    public void SetBan(int banType, int endTime, string? reason)
    {
        _banType = banType;
        _banEndTime = endTime;
        _banReason = reason;
    }

    public void ClearBan()
    {
        _banType = 0;
        _banEndTime = 0;
        _banReason = null;
    }


    public IReadOnlyCollection<long> GetFriendIds()
    {
        _friendIds ??= new HashSet<long>();
        return _friendIds.ToArray();
    }

    public bool AddFriend(long accountId)
    {
        if (accountId <= 0 || accountId == _accountId) return false;
        _friendIds ??= new HashSet<long>();
        var added = _friendIds.Add(accountId);
        if (added) Persist();
        return added;
    }

    public bool RemoveFriend(long accountId)
    {
        _friendIds ??= new HashSet<long>();
        var removed = _friendIds.Remove(accountId);
        if (removed) Persist();
        return removed;
    }

    public void Persist()
    {
        Databases.AccountDatabase.OnAssistByAccountInFile(this, true, _accountId);
    }

    public object GetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure accountStructure)
    {
        object fieldValue = null!;
        {
            var fieldName = AccountHelper.GetNormalParameterNameFromAccountStructure(accountStructure);
            {
                var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                {
                    foreach (var field in fields)
                        try
                        {
                            if (field.Name == fieldName) fieldValue = field.GetValue(this)!;
                        }
                        catch (Exception)
                        {
                            // ignored.
                        }
                }
            }
        }

        return fieldValue;
    }

    public void SetFieldValueByAccountStructureParameterFromAccountModel(AccountStructure accountStructure,
        object newValue)
    {
        var fieldName = AccountHelper.GetNormalParameterNameFromAccountStructure(accountStructure);
        {
            var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            {
                foreach (var field in fields)
                    try
                    {
                        if (field.Name != fieldName) continue;

                        if (field.FieldType == typeof(int))
                            if (newValue is long)
                                newValue = (int)newValue;

                        if (field.FieldType == typeof(long))
                            if (newValue is int)
                                newValue = (long)newValue;

                        field.SetValue(this, newValue);
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
            }
        }

        Databases.AccountDatabase.OnAssistByAccountInFile(this, true, _accountId);
        if (_allianceId <= 0) return;

        Databases.AllianceDatabase.LoadAlliance(_allianceId).SetFieldValueByAllianceStructureParameterFromAllianceModel(
            AllianceStructure.AllianceMemberEntryList, (Dictionary<long, AllianceMemberEntry>)Databases.AllianceDatabase
                .LoadAlliance(_allianceId)
                .GetFieldValueByAllianceStructureParameterFromAllianceModel(AllianceStructure.AllianceMemberEntryList));
    }

    protected void ReplaceFieldValueByAccountStructureParameter(AccountModel accountModel,
        AccountStructure accountStructure, object newValue)
    {
        var fieldName = AccountHelper.GetNormalParameterNameFromAccountStructure(accountStructure);
        {
            var fields = accountModel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            {
                foreach (var field in fields)
                    try
                    {
                        if (field.Name == fieldName) field.SetValue(accountModel, newValue);
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
            }
        }
    }

    protected bool ReplaceFieldValueByFieldName(AccountModel accountModel, string fieldName, object newValue)
    {
        {
            var fields = accountModel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            {
                foreach (var field in fields)
                    try
                    {
                        if (field.Name != fieldName) continue;
                        field.SetValue(accountModel, newValue);

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
            }
        }

        return false;
    }

    protected AccountModel AbstractingAccountModelFromRootAccountModel(AccountModel accountModel,
        AccountModel rootAccountModel, bool thisClassAbstracted = true)
    {
        if (thisClassAbstracted)
        {
            var fieldsThis = accountModel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            {
                foreach (var field in fieldsThis)
                    try
                    {
                        object objectValue = null!;

                        foreach (var fieldRoot in rootAccountModel.GetType()
                                     .GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            try
                            {
                                if (field.Name == fieldRoot.Name) objectValue = fieldRoot.GetValue(rootAccountModel)!;
                            }
                            catch (Exception)
                            {
                                // ignored.
                            }

                        field.SetValue(accountModel, objectValue);
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
            }
        }

        var newAccountModel = new AccountModel();
        {
            var fields = newAccountModel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            {
                foreach (var field in fields)
                    try
                    {
                        object objectValue = null!;

                        foreach (var fieldRoot in rootAccountModel.GetType()
                                     .GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            try
                            {
                                if (field.Name == fieldRoot.Name) objectValue = fieldRoot.GetValue(rootAccountModel)!;
                            }
                            catch (Exception)
                            {
                                // ignored.
                            }

                        field.SetValue(accountModel, objectValue);
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
            }
        }

        return newAccountModel;
    }

    public AccountModel GetAccountModel()
    {
        return this;
    }

    public long GetAccountId()
    {
        return _accountId;
    }

    public string AccountModelToJson()
    {
        return JsonConvert.SerializeObject(GetAccountModel());
    }

    public AccountModel JsonToAccountModel(string jsonAccountModel = null!)
    {
        return JsonConvert.DeserializeObject<AccountModel>(_jsonAccountModel ?? jsonAccountModel)!;
    }
}