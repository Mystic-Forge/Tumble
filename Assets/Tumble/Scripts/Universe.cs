using Nessie.Udon.Movement;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;


public class Universe : UdonSharpBehaviour {
    public const int Version = 1;
    
    public                                  PoseManager        poseManager;
    public                                  StructureManager   structureManager;
    public                                  Transform          levelsHolder;
    public                                  LeaderboardManager leaderboardManager;
    public                                  NUMovement         movement;
    [FormerlySerializedAs("cheats")] public Modifiers          modifiers;
    public                                  PWIManager         pwiManager;

    public TumbleLevel[] AllLevels => levelsHolder.GetComponentsInChildren<TumbleLevel>();
    
    public TumbleLevel GetLevel(int levelIndex) {
        foreach (var level in AllLevels) if (level.levelIndex == levelIndex) return level;
        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player) { player.SetVoiceDistanceFar(250); }
}