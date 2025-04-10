
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MainMenu : UdonSharpBehaviour {
    public void EventRoomLoaded() => gameObject.SetActive(false);
    public void EventRoomUnloaded() => gameObject.SetActive(true);
}
