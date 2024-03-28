using System;

using TMPro;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class LocalTimeDisplay : UdonSharpBehaviour {
    public TextMeshProUGUI currentTime;
    public TextMeshProUGUI bestTime;
    public Canvas          canvas;
    public bool            forVR;

    private Universe _universe;

    private LeaderboardManager _leaderboardManager;

    private PlayerLeaderboard LocalLeaderboard => _leaderboardManager.LocalLeaderboard;

    private float CurrentTime => LocalLeaderboard.currentTime;

    private float _lastBestTimeUpdate;

    private float BestTime {
        get {
            var platform = 1 << (int)(Networking.LocalPlayer.IsUserInVR() ? Platform.VR : Platform.Desktop);
            var time     = LocalLeaderboard.GetBestLevelTime(LocalLeaderboard.currentLevel, _universe.modifiers.EnabledModifiers, platform);
            if (time == null) return 0;
            return PlayerLeaderboard.GetTimeFromData(time);
        }
    }

    private void Start() {
        _universe           = GetComponentInParent<Universe>();
        _leaderboardManager = _universe.leaderboardManager;
        
        if(forVR && !Networking.LocalPlayer.IsUserInVR()) gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        if (LocalLeaderboard == null) return;

        if (LocalLeaderboard.isRunning) currentTime.text = PlayerLeaderboard.GetFormattedTime(CurrentTime);

        if(Time.time - _lastBestTimeUpdate < 5) return;
        bestTime.text = PlayerLeaderboard.GetFormattedTime(BestTime);
    }
}