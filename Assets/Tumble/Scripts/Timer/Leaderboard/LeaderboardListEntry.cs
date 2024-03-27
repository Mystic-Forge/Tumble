using TMPro;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

using VRC.SDK3.Data;
using VRC.SDKBase;


public class LeaderboardListEntry : UdonSharpBehaviour {
    public TextMeshProUGUI playerName;
    public GameObject      verifiedBadge;
    public TextMeshProUGUI time;
    public TextMeshProUGUI placement;
    public TextMeshProUGUI date;
    public InputField      inputField;
    public Transform       modifiersContainer;
    public GameObject      modifierIconPrefab;
    public Color           localColor;
    public Color           alternatingColorA;
    public Color           alternatingColorB;

    public Universe universe;

    public void SetData(int placement, DataDictionary timeToken, bool verified) {
        verifiedBadge.SetActive(verified);
        this.placement.text = GetOrdinal(placement);
        time.text           = PlayerLeaderboard.GetFormattedTime(PlayerLeaderboard.GetTimeFromData(timeToken));
        var name = PlayerLeaderboard.GetPlayerNameFromData(timeToken);
        playerName.text = name;
        date.text       = PlayerLeaderboard.GetFormattedDate(PlayerLeaderboard.GetDateFromData(timeToken));

        var modifiers = PlayerLeaderboard.GetModifiersFromData(timeToken);

        foreach (var modifier in universe.modifiers.ExtractModifiers(modifiers)) {
            var icon = Instantiate(modifierIconPrefab, modifiersContainer);
            icon.GetComponentInChildren<Image>().sprite = universe.modifiers.GetIcon(modifier);
        }

        var image = GetComponent<Image>();

        var local = Networking.LocalPlayer.displayName == name;
        if (local && GetComponentInParent<LeaderboardListDisplay>().scope != LeaderboardScope.MyTimes)
            image.color = localColor;
        else
            image.color = placement % 2 == 0 ? alternatingColorA : alternatingColorB;

        if (!local) {
            inputField.gameObject.SetActive(false);
        } else {
            inputField.gameObject.SetActive(true);
            if (VRCJson.TrySerializeToJson(timeToken, JsonExportType.Minify, out var json)) inputField.text = json.String;
        }
    }

    private string GetOrdinal(int number) {
        if (number <= 0) return number.ToString();

        switch (number % 100) {
            case 11:
            case 12:
            case 13: return number + "th";
        }

        switch (number % 10) {
            case 1:  return number + "st";
            case 2:  return number + "nd";
            case 3:  return number + "rd";
            default: return number + "th";
        }
    }
}