
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelCollectable : TumbleBehaviour
{
    public GameObject collectable;
    
    public bool IsCollected => !collectable.activeSelf;
    
    private TumbleLevel Level => GetComponentInParent<TumbleLevel>();

    public void Collect() {
        collectable.SetActive(false);
    }
    
    public void ResetCollectable() {
        collectable.SetActive(true);
    }
    
    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if(Level.IsStarted && player.isLocal)
            Collect();
    }
}
