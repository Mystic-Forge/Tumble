using System;

using UdonSharp;

using VRC.SDKBase;


public class LeaderboardManager : UdonSharpBehaviour {
    public PlayerLeaderboard LocalLeaderboard {
        get {
            for (var i = 0; i < transform.childCount; i++) {
                var leaderboard = transform.GetChild(i).GetComponent<PlayerLeaderboard>();
                if (leaderboard.IsLocal) return leaderboard;
            }

            return null;
        }
    }

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
    
    private void FixedUpdate() {
        if(!Networking.LocalPlayer.isMaster) return;

        var allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(allPlayers);
        
        foreach(var player in allPlayers) {
            var leaderboard = GetLeaderboard(player);

            if (leaderboard != null) {
                if(Networking.GetOwner(leaderboard.gameObject) != player) Networking.SetOwner(player, leaderboard.gameObject); // Rejoin failsafe
                continue;
            }
            
            leaderboard = FirstAvailableLeaderboard;
            if (leaderboard == null) return;
            
            leaderboard.playerName = player.displayName;
            Networking.SetOwner(player, leaderboard.gameObject);
        }
    }
}