
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LeaderboardPlatformFilterToggle : UdonSharpBehaviour
{
    public Platform platform;
    
    public void ToggleFilter()
    {
        var toggle        = GetComponent<Toggle>().isOn;
        var leaderboard   = GetComponentInParent<LeaderboardListDisplay>();
        var currentFilter = leaderboard.PlatformFilter;
        if (toggle) currentFilter |= 1 << (int)platform;
        else currentFilter        &= ~(1 << (int)platform);
        leaderboard.PlatformFilter = currentFilter;
        leaderboard.UpdateData();
    }
}
