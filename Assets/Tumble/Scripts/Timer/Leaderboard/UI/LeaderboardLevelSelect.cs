using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


[RequireComponent(typeof(Dropdown))]
public class LeaderboardLevelSelect : UdonSharpBehaviour {
    public void UpdateLevel() {
        var dropdown    = GetComponent<Dropdown>();
        var leaderboard = GetComponentInParent<LeaderboardListDisplay>();
        leaderboard.levelId = dropdown.value;
        leaderboard.UpdateData();
    }
}