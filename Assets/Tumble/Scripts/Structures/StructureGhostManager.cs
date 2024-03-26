using UdonSharp;

using VRC.SDKBase;


public class StructureGhostManager : UdonSharpBehaviour {
    private void FixedUpdate() {
        if (!Networking.LocalPlayer.isMaster) return;

        for (var i = 0; i < transform.childCount; i++) {
            var playerStructureGhost = transform.GetChild(i).GetComponent<PlayerStructureGhost>();
            if (playerStructureGhost.playerId == -1) continue;

            var playerApi                                        = VRCPlayerApi.GetPlayerById(playerStructureGhost.playerId);
            if (playerApi == null) playerStructureGhost.playerId = -1;
        }

        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (var player in players) {
            var playerStructureGhost = GetPlayerStructureGhost(player);

            if (playerStructureGhost == null) {
                var newGhost = GetNextAvailablePlayerStructureGhost();
                newGhost.playerId = player.playerId;
                Networking.SetOwner(player, newGhost.gameObject);
            }
        }
    }

    private PlayerStructureGhost GetPlayerStructureGhost(VRCPlayerApi player) {
        for (var i = 0; i < transform.childCount; i++) {
            var playerStructureGhost = transform.GetChild(i).GetComponent<PlayerStructureGhost>();
            if (playerStructureGhost.playerId == player.playerId) return playerStructureGhost;
        }

        return null;
    }

    private PlayerStructureGhost GetNextAvailablePlayerStructureGhost() {
        for (var i = 0; i < transform.childCount; i++) {
            var playerStructureGhost = transform.GetChild(i).GetComponent<PlayerStructureGhost>();
            if (playerStructureGhost.playerId == -1) return playerStructureGhost;
        }

        return null;
    }
}