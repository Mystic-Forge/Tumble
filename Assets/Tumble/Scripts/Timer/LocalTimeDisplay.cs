using System;

using TMPro;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class LocalTimeDisplay : TumbleBehaviour {
    public TextMeshProUGUI currentTime;
    public TextMeshProUGUI bestTime;
    public Canvas          canvas;
    public bool            forVR;

    private PlayerLeaderboard LocalLeaderboard => Universe.leaderboardManager.LocalLeaderboard;

    private float CurrentTime => LocalLeaderboard.currentTime;

    private float _lastBestTimeUpdate;

    private float BestTime {
        get {
            if(LocalLeaderboard == null) return 0;
            if(LocalLeaderboard.currentLevel == null) return 0;
            
            var platform = 1 << (int)(Networking.LocalPlayer.IsUserInVR() ? Platform.VR : Platform.Desktop);
            var time     = LocalLeaderboard.GetBestLevelTime(LocalLeaderboard.currentLevel.levelId, Universe.modifiers.EnabledModifiers, platform);
            if (time == null) return 0;
            return PlayerLeaderboard.GetTimeFromData(time);
        }
    }

    private void Start() {
        if(forVR && !Networking.LocalPlayer.IsUserInVR()) gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        if (LocalLeaderboard == null) return;

        if (LocalLeaderboard.isRunning) currentTime.text = PlayerLeaderboard.GetFormattedTime(CurrentTime);

        if(Time.time - _lastBestTimeUpdate < 5) return;
        bestTime.text = PlayerLeaderboard.GetFormattedTime(BestTime);
    }
}