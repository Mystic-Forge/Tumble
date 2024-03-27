
using TMPro;

using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;

public class LeaderboardChip : UdonSharpBehaviour
{
    public LeaderboardChipType leaderboardChipType;
    
    public void ChipUpdated() {
        Debug.Log("Chip updated");
        var leaderboard = GetComponentInParent<LeaderboardListDisplay>();
        var dropdown = GetComponent<Dropdown>();
        switch(leaderboardChipType) {
            case LeaderboardChipType.Scope:
                leaderboard.scope = (LeaderboardScope) dropdown.value;
                break;
        }
        
        leaderboard.UpdateData();
    }
}

public enum LeaderboardChipType {
    Scope,
}