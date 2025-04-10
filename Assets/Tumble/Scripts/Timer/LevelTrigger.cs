using Tumble.Scripts.Level;

using UdonSharp;

using VRC.SDKBase;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelTrigger : TumbleBehaviour {
    public LevelTriggerType triggerType;

    private PlayerLeaderboard LocalLeaderboard => GetComponentInParent<Universe>().leaderboardManager.LocalLeaderboard;

    private TumbleLevel Level => GetComponentInParent<TumbleLevel>();

    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if (!player.isLocal) return;

        if (triggerType == LevelTriggerType.Start && Level.IsStarted) Level.EndLevel();

        if (triggerType == LevelTriggerType.Checkpoint) Level.SetCheckpoint(this);

        if (triggerType == LevelTriggerType.End && Level.IsLevelCompletable()) Level.EndLevel();

        if (triggerType == LevelTriggerType.Respawn && Level.IsStarted) Level.RespawnAtCheckpoint();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player) {
        if (!player.isLocal) return;
        if (Level != LocalRoom.level) return;
        if (LocalRoom.roomType == RoomType.Editor && Universe.levelEditor.tool.mode != LevelEditorToolMode.Play) return;

        if (triggerType == LevelTriggerType.Start) Level.StartLevel(this);
    }
}

public enum LevelTriggerType {
    Start,
    Checkpoint,
    End,
    Respawn,
}