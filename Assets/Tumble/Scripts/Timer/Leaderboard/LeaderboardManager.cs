using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;
using VRC.SDKBase;


public class LeaderboardManager : UdonSharpBehaviour {
    [SerializeField]                                    private string[] verifiedTimesJson;
    [SerializeField] public  VRCUrl[] replayUrls;
    public                                                      DataList verifiedTimes;

    public PlayerLeaderboard LocalLeaderboard => GetLeaderboard(Networking.LocalPlayer);

    public PlayerLeaderboard[] Leaderboards {
        get {
            var count = 0;

            for (var i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).GetComponent<PlayerLeaderboard>().HasOwner)
                    count++;

            var leaderboards = new PlayerLeaderboard[count];
            count = 0;

            for (var i = 0; i < transform.childCount; i++) {
                var leaderboard                                 = transform.GetChild(i).GetComponent<PlayerLeaderboard>();
                if (leaderboard.HasOwner) leaderboards[count++] = leaderboard;
            }

            return leaderboards;
        }
    }

    private PlayerLeaderboard GetLeaderboard(VRCPlayerApi player) {
        for (var i = 0; i < transform.childCount; i++) {
            var leaderboard = transform.GetChild(i).GetComponent<PlayerLeaderboard>();
            if (leaderboard.playerName == player.displayName) return leaderboard;
        }

        return null;
    }

    private PlayerLeaderboard FirstAvailableLeaderboard {
        get {
            for (var i = 0; i < transform.childCount; i++) {
                var leaderboard = transform.GetChild(i).GetComponent<PlayerLeaderboard>();
                if (!leaderboard.HasOwner) return leaderboard;
            }

            return null;
        }
    }

    private void Start() {
        verifiedTimes = new DataList();

        for (var i = 0; i < verifiedTimesJson.Length; i++) {
            if (VRCJson.TryDeserializeFromJson(verifiedTimesJson[i], out var data)) {
                data.DataDictionary[PlayerLeaderboard.VerifiedTimeKey] = i;
                verifiedTimes.Add(data.DataDictionary);
            }
        }
    }

    private void FixedUpdate() {
        if (!Networking.LocalPlayer.isMaster) return;

        var allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(allPlayers);

        foreach (var player in allPlayers) {
            var leaderboard = GetLeaderboard(player);

            if (leaderboard != null) {
                if (Networking.GetOwner(leaderboard.gameObject) != player) Networking.SetOwner(player, leaderboard.gameObject); // Rejoin failsafe
                continue;
            }

            leaderboard = FirstAvailableLeaderboard;
            if (leaderboard == null) return;

            leaderboard.playerName = player.displayName;
            Networking.SetOwner(player, leaderboard.gameObject);
        }
    }

    public DataList GetVerifiedTimesForLevel(TumbleLevel level) {
        var times = new DataList();

        foreach (var time in verifiedTimes.ToArray())
            if (PlayerLeaderboard.GetLevelIndexFromData(time) == level.levelIndex)
                times.Add(time);

        return times;
    }
}