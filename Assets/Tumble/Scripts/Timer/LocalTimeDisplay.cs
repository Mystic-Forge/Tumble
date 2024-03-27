using System;

using TMPro;

using UdonSharp;


public class LocalTimeDisplay : UdonSharpBehaviour {
    public TextMeshProUGUI currentTime;
    public TextMeshProUGUI bestTime;

    private Universe _universe;

    private LeaderboardManager _leaderboardManager;

    private PlayerLeaderboard LocalLeaderboard => _leaderboardManager.LocalLeaderboard;

    private float CurrentTime => LocalLeaderboard.currentTime;

    private float BestTime {
        get {
            var time = LocalLeaderboard.GetBestLevelTime(LocalLeaderboard.currentLevel, _universe.modifiers.EnabledModifiers);
            if (time == null) return 0;
            return PlayerLeaderboard.GetTimeFromData(time);
        }
    }

    private void Start() {
        _universe           = GetComponentInParent<Universe>();
        _leaderboardManager = _universe.leaderboardManager;
    }

    private void FixedUpdate() {
        if (LocalLeaderboard == null) return;

        if (LocalLeaderboard.isRunning) currentTime.text = PlayerLeaderboard.GetFormattedTime(CurrentTime);

        bestTime.text = PlayerLeaderboard.GetFormattedTime(BestTime);
    }
}