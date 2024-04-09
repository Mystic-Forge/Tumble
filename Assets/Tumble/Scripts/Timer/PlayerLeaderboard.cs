using System;
using System.Linq;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;
using VRC.SDKBase;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerLeaderboard : UdonSharpBehaviour {
    public const string LevelIdKey       = "li";
    public const string LevelVersionKey  = "lv";
    public const string TumbleVersionKey = "v";
    public const string TimeKey          = "t";
    public const string UsedModifiersKey = "m";
    public const string DateKey          = "d";
    public const string PlayerNameKey    = "pn";
    public const string VerifiedTimeKey  = "vt";
    public const string PlatformKey      = "p";

    public const string LevelDataKey = "times";

    [UdonSynced] public string      playerName;
    [UdonSynced] public int         currentLevelIndex;
    public              TumbleLevel currentLevel;
    [UdonSynced] public string      serializedTimes;
    public              float       currentTime;
    public              bool        isRunning;
    [UdonSynced] public bool        syncedWithVrcx = false;

    public bool HasOwner => !string.IsNullOrWhiteSpace(playerName);

    public bool IsLocal => playerName == Networking.LocalPlayer.displayName;

    private LeaderboardManager Manager => GetComponentInParent<LeaderboardManager>();

    private Universe Universe => GetComponentInParent<Universe>();

    private DataList _times = new DataList();
    private bool     _vrcxInitialized;

    private void FixedUpdate() {
        if (!Networking.GetOwner(gameObject).isLocal) return;

        if (!Networking.LocalPlayer.isMaster && !HasOwner) playerName =  Networking.LocalPlayer.displayName;
        if (isRunning) currentTime                                    += Time.fixedDeltaTime;

        if (HasOwner && IsLocal && !_vrcxInitialized && Universe.pwiManager.IsReady()) {
            if (syncedWithVrcx) _times.Clear();

            if (Universe.pwiManager.TryGetString(LevelDataKey, out var savedTimesString))
                if (VRCJson.TryDeserializeFromJson(savedTimesString, out var savedTimes))
                    AddSavedTimes(savedTimes.DataList);

            // Scan for duplicates and remove them
            var jsonArray = new DataList();

            for (var index = 0; index < _times.Count; index++) {
                var timeToken = _times[index];

                if (VRCJson.TrySerializeToJson(timeToken, JsonExportType.Minify, out var json)) {
                    if (jsonArray.Contains(json)) {
                        _times.RemoveAt(index);
                        continue;
                    }

                    jsonArray.Add(json);
                }
            }

            for (var index = 0; index < _times.Count; index++) {
                var json = _times[index].ToString();
            }

            if (!syncedWithVrcx) SyncWithVrcx();
            _vrcxInitialized = true;
            RequestSerialization();
        }
    }

    private void SyncWithVrcx() {
        if (Universe.pwiManager.IsReady()) {
            Universe.pwiManager.StoreData(LevelDataKey, _times);
            syncedWithVrcx = true;
        }
    }

    public void StartLevel(TumbleLevel level) {
        currentLevel      = level;
        currentLevelIndex = level.levelIndex;
        currentTime       = 0;
        isRunning         = true;
    }

    public void SaveTime() {
        if (!isRunning) return;

        isRunning = false;

        var cheats = Universe.modifiers.EnabledModifiers;

        var timeToken = GetTimeToken(
            currentLevel,
            currentTime,
            cheats
        );

        AddTime(timeToken);

        SubmitTimeToLog(timeToken);
    }

    private void SubmitTimeToLog(DataDictionary timeToken) {
        var cheated = Universe.modifiers.AnyEnabled;

        if (cheated)
            Debug.Log($"[TUMBLE] New time (Cheated with {Universe.modifiers.EnabledModifiers}): "
                + $"{playerName} completed level {currentLevelIndex + 1} in {GetFormattedTime(GetTimeFromData(timeToken))}");
        else
            Debug.Log($"[TUMBLE] New Time: "
                + $"{playerName} completed level {currentLevelIndex + 1} in {GetFormattedTime(GetTimeFromData(timeToken))}");
    }

    public DataDictionary GetBestLevelTime(TumbleLevel level, ModifiersEnum modifierMask, int platformMask) {
        if (level == null) return null;

        var            bestTime  = float.MaxValue;
        DataDictionary bestToken = null;

        foreach (var timeToken in GetTimesForLevel(level).ToArray()) {
            var modifiers = (int)GetModifiersFromData(timeToken);
            var filter    = (int)modifierMask;

            if ((modifiers | filter) != filter) continue;

            var platform = 1 << (int)GetPlatformFromData(timeToken);
            if ((platform | platformMask) != platformMask) continue;

            var time = GetTimeFromData(timeToken);
            if (time >= bestTime) continue;

            bestTime  = time;
            bestToken = timeToken.DataDictionary;
        }

        return bestToken;
    }

    public DataList GetTimesForLevel(TumbleLevel level) {
        if (level == null) return new DataList();

        var times = new DataList();

        foreach (var timeToken in _times.ToArray()) {
            if (GetLevelIndexFromData(timeToken) != level.levelIndex) continue;
            if (GetLevelVersionFromData(timeToken) != level.version) continue;
            if (GetTumbleVersionFromData(timeToken) != Universe.Version) continue;

            times.Add(timeToken);
        }

        return times;
    }

    private static DataDictionary GetTimeToken(TumbleLevel level, float time, ModifiersEnum modifiers) {
        var token = new DataDictionary();

        token[LevelIdKey]       = new DataToken(level.levelIndex);
        token[LevelVersionKey]  = new DataToken(level.version);
        token[TumbleVersionKey] = new DataToken(Universe.Version);
        token[TimeKey]          = new DataToken(time);
        token[UsedModifiersKey] = new DataToken((int)modifiers);
        token[DateKey]          = new DataToken(DateTime.UtcNow.Ticks);
        token[PlatformKey]      = new DataToken(Networking.LocalPlayer.IsUserInVR() ? (int)Platform.VR : (int)Platform.Desktop);

        return token;
    }

    public static int GetLevelIndexFromData(DataToken token) {
        var data = token.DataDictionary[LevelIdKey];
        if (data.TokenType == TokenType.Double) return (int)data.Double;

        return token.DataDictionary[LevelIdKey].Int;
    }

    public static int GetLevelVersionFromData(DataToken token) {
        var data = token.DataDictionary[LevelVersionKey];
        if (data.TokenType == TokenType.Double) return (int)data.Double;

        return token.DataDictionary[LevelVersionKey].Int;
    }

    public static int GetTumbleVersionFromData(DataToken token) {
        var data = token.DataDictionary[TumbleVersionKey];
        if (data.TokenType == TokenType.Double) return (int)data.Double;

        return token.DataDictionary[TumbleVersionKey].Int;
    }

    public static float GetTimeFromData(DataToken token) {
        var data = token.DataDictionary[TimeKey];
        if (data.TokenType == TokenType.Float) return data.Float;

        return (float)data.Double;
    }

    public static ModifiersEnum GetModifiersFromData(DataToken token) {
        var data = token.DataDictionary[UsedModifiersKey];
        if (data.TokenType == TokenType.Double) return (ModifiersEnum)(int)data.Double;

        return (ModifiersEnum)data.Int;
    }

    public static DateTime GetDateFromData(DataToken token) {
        var data = token.DataDictionary[DateKey];
        if (data.TokenType == TokenType.Double) return new DateTime((long)data.Double);

        return new DateTime((long)data.Long);
    }

    public static string GetPlayerNameFromData(DataToken token) => token.DataDictionary[PlayerNameKey].String;

    public static int GetVerifiedFromData(DataToken token) {
        if (token.DataDictionary.TryGetValue(VerifiedTimeKey, out var verified)) return verified.Int;

        return -1;
    }

    public static Platform GetPlatformFromData(DataToken token) {
        if (token.DataDictionary.TryGetValue(PlatformKey, out var platform)) {
            if (platform.TokenType == TokenType.Double) return (Platform)(int)platform.Double;

            return (Platform)platform.Int;
        }

        return Platform.Unknown;
    }

    public void AddTime(DataDictionary timeToken, bool syncWithVrcx = true) {
        _times.Add(timeToken);

        if (syncWithVrcx) SyncWithVrcx();

        RequestSerialization();
    }

    private void AddSavedTimes(DataList savedTimes) {
        Debug.Log($"[TUMBLE] Loading {savedTimes.Count} saved times from VRCX...");
        foreach (var timeToken in savedTimes.ToArray()) AddTime(timeToken.DataDictionary, syncWithVrcx: false);
    }

    public override void OnPreSerialization() {
        if (VRCJson.TrySerializeToJson(_times, JsonExportType.Minify, out var data)) serializedTimes = data.String;
    }

    public override void OnDeserialization() {
        if (VRCJson.TryDeserializeFromJson(serializedTimes, out var data)) _times = data.DataList;
    }

    public static string GetFormattedTime(float time) => TimeSpan.FromSeconds(time).ToString(@"mm\:ss\.fff");

    public static string GetFormattedDate(DateTime date) {
        var relative = DateTime.UtcNow - date;
        if (relative.TotalMinutes < 1) return $"{(int)relative.TotalSeconds}s ago";
        if (relative.TotalHours < 1) return $"{(int)relative.TotalMinutes}m ago";
        if (relative.TotalDays < 1) return $"{(int)relative.TotalHours}h ago";

        return $"{(int)relative.TotalDays}d ago";
    }
}

public enum Platform {
    Unknown = 0,
    Desktop = 1,
    VR      = 2,
}