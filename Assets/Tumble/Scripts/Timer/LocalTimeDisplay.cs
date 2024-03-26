using System;

using TMPro;

using UdonSharp;


public class LocalTimeDisplay : UdonSharpBehaviour {
    public TextMeshProUGUI currentTime;
    public TextMeshProUGUI bestTime;
    
    private LeaderboardManager _leaderboardManager;
    private PlayerLeaderboard LocalLeaderboard => _leaderboardManager.LocalLeaderboard;
    private float CurrentTime => LocalLeaderboard.currentTime;
    private float BestTime => LocalLeaderboard.GetBestLevelTime(LocalLeaderboard.currentLevel);

    private void Start() {
        _leaderboardManager = GetComponentInParent<Universe>().leaderboardManager;
    }

    private void FixedUpdate() {
        if(LocalLeaderboard == null) return;
        
        if (LocalLeaderboard.isRunning) {
            var time = TimeSpan.FromSeconds(CurrentTime);
            currentTime.text   = $"{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
        }
        
        var best = TimeSpan.FromSeconds(BestTime);
        bestTime.text = $"{best.Minutes:00}:{best.Seconds:00}.{best.Milliseconds:000}";
    }
}
