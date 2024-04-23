
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MenuAnchor : UdonSharpBehaviour
{
    private Universe _universe;
    
    private void Start() {
        _universe = GetComponentInParent<Universe>();
    }
    
    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if(player.isLocal)
            _universe.mainMenu.transform.SetPositionAndRotation(transform.position, transform.rotation);
    }
}
