using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;


public class LeaderboardListDisplay : UdonSharpBehaviour {
    public GameObject leaderboardEntryPrefab;
    public Transform  leaderboardList;
    public int        itemsPerPage = 33;
    public int        currentPage;
    public int        levelIndex;

    public                 LeaderboardScope        scope;
    public                 LeaderboardSortCriteria sortCriteria;
    public                 LeaderboardMode         mode;
    [NonSerialized] public ModifiersEnum           ModifiersFilter;

    public TumbleLevel Level => _universe.GetLevel(levelIndex);

    private Universe           _universe;
    private LeaderboardManager _leaderboardManager;
    private float              _lastUpdateTime;
    private DataList           _times = new DataList();

    private void Start() {
        _universe           = GetComponentInParent<Universe>();
        _leaderboardManager = _universe.leaderboardManager;
    }

    private void FixedUpdate() {
        var localLeaderboard = _universe.leaderboardManager.LocalLeaderboard;
        if (localLeaderboard == null) return;
        if (localLeaderboard.isRunning) return; // Don't update the leaderboard while the player is running to avoid lag spikes

        if (Time.time - _lastUpdateTime < 10) return;

        UpdateData();
    }

    private void FirstPage() {
        currentPage = 0;
        Refresh();
    }

    private void LastPage() {
        currentPage = Mathf.CeilToInt(_times.Count / (float)itemsPerPage) - 1;
        Refresh();
    }

    private void NextPage() {
        if (currentPage * itemsPerPage >= _times.Count) return;

        currentPage++;
        Refresh();
    }

    private void PreviousPage() {
        if (currentPage == 0) return;

        currentPage--;
        Refresh();
    }

    public void UpdateData() {
        _lastUpdateTime = Time.time;

        _times = GetTimes();

        // Sort the times using bubble sort 💪🫧
        while (true) {
            var swapped = false;

            for (var i = 1; i < _times.Count; i++) {
                var previousTime = _times[i - 1];
                var time         = _times[i];

                if (PlayerLeaderboard.GetTimeFromData(previousTime) > PlayerLeaderboard.GetTimeFromData(time)) {
                    var tempTime = _times[i - 1];
                    _times[i - 1] = _times[i];
                    _times[i]     = tempTime;
                    swapped       = true;
                }
            }

            if (!swapped) break;
        }

        Refresh();
    }

    private DataList GetTimes() {
        var times = new DataList();

        // Filter by scope
        var level = Level;

        if (scope == LeaderboardScope.All || scope == LeaderboardScope.InWorld) {
            foreach (var leaderboard in _leaderboardManager.Leaderboards) {
                var time = leaderboard.GetBestLevelTime(level, ModifiersFilter);

                if (time != null) {
                    time                                  = time.ShallowClone();
                    time[PlayerLeaderboard.PlayerNameKey] = leaderboard.playerName;
                    times.Add(time);
                }
            }

            if (scope == LeaderboardScope.All) times.AddRange(_leaderboardManager.GetVerifiedTimesForLevel(level));
        }

        if (scope == LeaderboardScope.MyTimes) {
            var localLeaderboard = _leaderboardManager.LocalLeaderboard;

            if (localLeaderboard != null) {
                foreach (var time in localLeaderboard.GetTimesForLevel(level).ToArray()) {
                    var timeData = time.DataDictionary.ShallowClone();
                    timeData[PlayerLeaderboard.PlayerNameKey] = localLeaderboard.playerName;
                    times.Add(timeData);
                }
            }
        }

        // Filter cheats
        foreach (var time in times.ToArray()) {
            if (ModifiersFilter == ModifiersEnum.All) continue;

            var timeCheats = (int)PlayerLeaderboard.GetModifiersFromData(time);
            var filter     = (int)ModifiersFilter;

            if ((timeCheats | filter) != filter) times.Remove(time);
        }

        return times;
    }

    private void Refresh() {
        while (leaderboardList.childCount > 0) DestroyImmediate(leaderboardList.GetChild(0).gameObject);

        var placement = 1;

        var start = currentPage * itemsPerPage;
        var end   = Mathf.Min(start + itemsPerPage, _times.Count);

        for (var i = start; i < end; i++) {
            var time  = _times[i].DataDictionary;
            var name  = PlayerLeaderboard.GetPlayerNameFromData(time);
            var entry = Instantiate(leaderboardEntryPrefab, leaderboardList).GetComponent<LeaderboardListEntry>();
            entry.universe = _universe;
            entry.SetData(placement, time, PlayerLeaderboard.GetVerifiedFromData(time));
            placement++;
        }
    }
}

public enum LeaderboardScope {
    All,
    InWorld,
    MyTimes,
}

public enum LeaderboardSortCriteria {
    Time,
    Date,
}

public enum LeaderboardMode {
    Normal,
    AroundMe,
}