using UdonSharp;

using VRC.SDKBase;
using VRC.Udon.Common;


public class TumbleBehaviour : UdonSharpBehaviour {
    private Universe _universe;

    public Universe Universe {
        get {
            if (_universe == null) _universe = GetComponentInParent<Universe>();
            return _universe;
        }
    }

    protected TumbleRoom LocalRoom => Universe.playerRoomManager.LocalTrackerRoom;

    protected TumbleLevel LocalLevel => LocalRoom.level;
}