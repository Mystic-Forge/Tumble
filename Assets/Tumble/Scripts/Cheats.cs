using UdonSharp;

using UnityEngine.UI;


public class Cheats : UdonSharpBehaviour {
    public bool AllGroundIsDirt => allGroundIsDirtToggle.isOn;
    public bool InfiniteAirActions => infiniteAirActionsToggle.isOn;
    public bool NoCooldowns => noCooldownsToggle.isOn;
    
    public bool AnyEnabled => AllGroundIsDirt || InfiniteAirActions || NoCooldowns;
    
    public Toggle allGroundIsDirtToggle;
    public Toggle infiniteAirActionsToggle;
    public Toggle noCooldownsToggle;
}
