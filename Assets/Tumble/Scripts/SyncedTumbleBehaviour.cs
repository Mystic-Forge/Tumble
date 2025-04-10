using UdonSharp;

using VRC.SDKBase;
using VRC.Udon.Common;


public abstract class SyncedTumbleBehaviour : TumbleBehaviour {
    [UdonSynced] public int ownerId = -1;
    
    public bool LocalIsOwner => ownerId == Networking.LocalPlayer.playerId;
    
    public void SetOwner(VRCPlayerApi player) {
        ownerId = player.playerId;
        RequestSerialization();
        if(Networking.GetOwner(gameObject).isLocal && player.isLocal) OnBecameOwner();
    }
    
    public VRCPlayerApi GetOwner() => VRCPlayerApi.GetPlayerById(ownerId) ?? Networking.GetOwner(gameObject);

    public virtual void OnBecameOwner() { }

    public override void OnDeserialization(DeserializationResult result) {
        if (
            LocalIsOwner
            && !Networking.GetOwner(gameObject).isLocal
        ) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            OnBecameOwner();
        }
        
        OnDeserialized(result);
    }

    public virtual void OnDeserialized(DeserializationResult result) {
        
    }
}