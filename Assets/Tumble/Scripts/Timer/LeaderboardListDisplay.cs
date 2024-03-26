using System;

using UdonSharp;

using UnityEngine;


public class LeaderboardListDisplay : UdonSharpBehaviour
{
    public GameObject leaderboardEntryPrefab;
    public Transform  leaderboardList;
    public int        level;
    
    private LeaderboardManager _leaderboardManager;
    private float _lastUpdateTime;
    
    void Start()
    {
        _leaderboardManager = GetComponentInParent<Universe>().leaderboardManager;
    }

    private void FixedUpdate() {
        if (Time.time - _lastUpdateTime < 5) return;
        
        _lastUpdateTime = Time.time;
        
        while (leaderboardList.childCount > 0) DestroyImmediate(leaderboardList.GetChild(0).gameObject);
        
        var leaderboards = _leaderboardManager.Leaderboards;
        var hasScore     = 0;
        foreach (var leaderboard in leaderboards)
            if (leaderboard.GetBestLevelTime(level) > 0) hasScore++;
        
        var newLeaderboards = new PlayerLeaderboard[hasScore];
        hasScore = 0;
        foreach (var leaderboard in leaderboards)
            if (leaderboard.GetBestLevelTime(level) > 0) newLeaderboards[hasScore++] = leaderboard;
        
        leaderboards = newLeaderboards;
        
        // Sort manually
        while (true) {
            var swapped = false;
            for (var i = 1; i < leaderboards.Length; i++) {
                if (leaderboards[i - 1].GetBestLevelTime(level) > leaderboards[i].GetBestLevelTime(level)) {
                    var temp = leaderboards[i - 1];
                    leaderboards[i - 1] = leaderboards[i];
                    leaderboards[i] = temp;
                    swapped = true;
                }
            }

            if (!swapped) break;
        }
        
        var placement = 1;
        foreach (var leaderboard in leaderboards) {
            var entry = Instantiate(leaderboardEntryPrefab, leaderboardList).GetComponent<LeaderboardListEntry>();
            entry.placement.text  = $"[{placement++:000}]";
            entry.playerName.text = leaderboard.playerName;
            var levelTime = leaderboard.GetBestLevelTime(level);
            entry.time.text       = TimeSpan.FromSeconds(levelTime).ToString(@"mm\:ss\:fff");
        }
    }
}
