using UdonSharp;

using VRC.SDKBase;


public class LevelTrigger : UdonSharpBehaviour {
    public LevelTriggerType triggerType;

    private PlayerLeaderboard LocalLeaderboard => GetComponentInParent<Universe>().leaderboardManager.LocalLeaderboard;
    private TumbleLevel Level => GetComponentInParent<TumbleLevel>();
    
    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if (!player.isLocal) return;

        if (triggerType == LevelTriggerType.Checkpoint) { } else if (triggerType == LevelTriggerType.End) {
            var leaderboard = LocalLeaderboard;
            if (leaderboard == null) return;

            leaderboard.SaveTime();
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player) {
        if (!player.isLocal) return;

        if (triggerType == LevelTriggerType.Start) {
            var leaderboard = LocalLeaderboard;
            if (leaderboard == null) return;

            leaderboard.StartLevel(Level);
        }
    }
}

public enum LevelTriggerType {
    Start,
    Checkpoint,
    End,
}