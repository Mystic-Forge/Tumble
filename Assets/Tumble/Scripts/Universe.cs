using Nessie.Udon.Movement;

using UdonSharp;

using VRC.SDKBase;


public class Universe : UdonSharpBehaviour {
    public PoseManager        poseManager;
    public StructureManager   structureManager;
    public LeaderboardManager leaderboardManager;
    public NUMovement         movement;
    public Cheats             cheats;

    public override void OnPlayerJoined(VRCPlayerApi player) {
        player.SetVoiceDistanceFar(250); 
    }
}