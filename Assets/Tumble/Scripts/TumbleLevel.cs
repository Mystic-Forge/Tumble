
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TumbleLevel : UdonSharpBehaviour {
    public int version = 0;
    public int levelIndex;
    public string LevelKey => $"l{levelIndex}";
}
