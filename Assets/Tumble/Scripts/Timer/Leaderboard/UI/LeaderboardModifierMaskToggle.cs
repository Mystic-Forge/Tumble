using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


public class LeaderboardModifierMaskToggle : UdonSharpBehaviour {
    public int maskIndex;

    public void UpdateMask() {
        var toggle        = GetComponent<Toggle>().isOn;
        var leaderboard   = GetComponentInParent<LeaderboardListDisplay>();
        var currentFilter = (int)leaderboard.ModifiersFilter;
        if (toggle) currentFilter |= 1 << maskIndex;
        else currentFilter &= ~(1 << maskIndex);
        leaderboard.ModifiersFilter =  (ModifiersEnum)currentFilter;
        leaderboard.UpdateData();
    }
}