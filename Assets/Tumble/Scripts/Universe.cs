using System;

using BobyStar.DualLaser;

using Nessie.Udon.Movement;

using Tumble.Scripts;

using UdonSharp;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

using VRC.SDKBase;


public class Universe : UdonSharpBehaviour {
    public const int Version = 1;

    public                                  PoseManager         poseManager;
    public                                  StructureManager    structureManager;
    public                                  Transform           levelsHolder;
    public                                  LeaderboardManager  leaderboardManager;
    public                                  NUMovement          movement;
    public                                  FlyController       flyMovement;
    [FormerlySerializedAs("cheats")] public Modifiers           modifiers;
    public                                  PWIManager          pwiManager;
    public                                  TumbleLevelLoader64 levelLoader;
    public                                  DualLaser           dualLaser;
    public                                  Transform           spawnPoint;
    public                                  LevelEditor         levelEditor;
    public                                  GameObject          mainMenu;
    public                                  PlayerRoomManager   playerRoomManager;
    public                                  EmojiSupportManager emojiSupportManager;
    public                                  ContextMenu         contextMenu;
    public                                  LevelDatabase       levelDatabase;

    public int blockingInputs = 0;

    public bool BlockInputs => blockingInputs > 0;

    private void LateUpdate() { movement.forceNoMovement = BlockInputs; }

    public TumbleLevel[] AllLevels => levelsHolder.GetComponentsInChildren<TumbleLevel>();

    public TumbleLevel GetLevel(int levelIndex) {
        foreach (var level in AllLevels)
            if (level.levelIndex == levelIndex)
                return level;

        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player) { player.SetVoiceDistanceFar(250); }

    public void BroadcastCustomEvent(string eventName) {
        foreach (var behavior in GetComponentsInChildren<UdonSharpBehaviour>(true)) {
            if(behavior == null || behavior == this) continue;
            behavior.SendCustomEvent(eventName);
        }
    }
}