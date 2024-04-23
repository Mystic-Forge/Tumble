using TMPro;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UserUIElement : UdonSharpBehaviour {
    public TextMeshProUGUI usernameText;
    public GameObject      promoteButton;
    public GameObject      promoteGold;
    public GameObject      kickButton;

    public VRCPlayerApi User;
    
    private TumbleRoom _room;
    private TumbleRoomConfigurationUI _configUI;

    public void SetUser(VRCPlayerApi user, TumbleRoom room, TumbleRoomConfigurationUI configUI) {
        User              = user;
        _room             = room;
        _configUI         = configUI;
        
        usernameText.text = user.displayName;
        kickButton.SetActive(false); // Will implement kicking later
        promoteButton.SetActive(room.LocalIsRoomOwner);
        promoteGold.SetActive(room.roomOwner == user.displayName);
    }
    
    public void PromoteUser() {
        if(User.isLocal) return; // We can only press this button if we are already the room owner
        _room.SetRoomOwner(User);
        _configUI.UpdateUserList();
    }
    
    public void KickUser() {
        // _room.
    }
}