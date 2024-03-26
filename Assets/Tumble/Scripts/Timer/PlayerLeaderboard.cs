using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class PlayerLeaderboard : UdonSharpBehaviour {
    [UdonSynced] public string  playerName;
    [UdonSynced] public int     currentLevel;
    [UdonSynced] public float[] levelTimes;
    public              float   currentTime;
    public              bool    isRunning;
    public bool HasOwner => !string.IsNullOrWhiteSpace(playerName);
    public bool IsLocal => playerName == Networking.LocalPlayer.displayName;
    
    private LeaderboardManager Manager => GetComponentInParent<LeaderboardManager>();
    private Universe          Universe => GetComponentInParent<Universe>();

    public float GetBestLevelTime(int level) {
        ValidateLevelTimeSize(level);
        return levelTimes[level];
    }

    private void ValidateLevelTimeSize(int level) {
        while (levelTimes.Length <= level) {
            var newLevelTimes                                            = new float[Mathf.Max(1, levelTimes.Length * 2)];
            for (var i = 0; i < levelTimes.Length; i++) newLevelTimes[i] = levelTimes[i];
            levelTimes = newLevelTimes;
        }
    }

    private void FixedUpdate() {
        if(!Networking.GetOwner(gameObject).isLocal) return;
        if (isRunning) currentTime += Time.fixedDeltaTime;
        if (!Networking.LocalPlayer.isMaster && !HasOwner) playerName = Networking.LocalPlayer.displayName;
    }

    public void StartLevel(int levelIndex) {
        currentLevel = levelIndex;
        currentTime  = 0;
        isRunning    = true;
    }

    public void SaveTime() {
        if (Universe.cheats.AnyEnabled) isRunning = false;
        if (!isRunning) return;

        ValidateLevelTimeSize(currentLevel);

        if (currentTime < levelTimes[currentLevel] || levelTimes[currentLevel] == 0) levelTimes[currentLevel] = currentTime;
        isRunning = false;
    }
}