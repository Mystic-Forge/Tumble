using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class PlayerStructureGhost : UdonSharpBehaviour {
    public int          playerId;
    public float        updateRate = 0.2f;
    public GameObject[] structureGhosts;

    [UdonSynced] public byte[] data;

    private Universe         _universe;
    private StructureManager _structureManager;
    private float            _lastUpdate;

    private GameObject[][] _spawnedGhosts;
    private bool[][]       _ghostActive;

    private void Start() {
        _universe         = GetComponentInParent<Universe>();
        _structureManager = _universe.structureManager;
        _spawnedGhosts    = new GameObject[structureGhosts.Length][];
        _ghostActive      = new bool[structureGhosts.Length][];

        for (var i = 0; i < structureGhosts.Length; i++) {
            _spawnedGhosts[i] = new GameObject[16];
            _ghostActive[i]   = new bool[16];
        }
    }

    private void FixedUpdate() {
        if (!Networking.IsOwner(gameObject)) return;

        if (playerId == -1) {
            if (Networking.LocalPlayer.isMaster) return;

            playerId = Networking.LocalPlayer.playerId;
        }

        if (Time.time - _lastUpdate < updateRate) return;

        RequestSerialization();
        _lastUpdate = Time.time;
    }

    public override void OnPreSerialization() { SerializeData(); }

    public override void OnDeserialization() { DeserializeData(); }

    private void SerializeData() {
        var structures = _structureManager.GetSpawnedStructures();
        data = new byte[structures.Length * 8];
        var offset = 0;

        var count = 0;
        foreach (var s in structures) {
            var type = s.transform.parent.GetSiblingIndex();
            data[offset] |= (byte)type;
            var id = s.transform.GetSiblingIndex();
            data[offset] |= (byte)(id << 4);

            var position = s.transform.position;

            var x = (ushort)Mathf.RoundToInt((position.x / 1000 + 0.5f) * 65536);
            var y = (ushort)Mathf.RoundToInt((position.y / 1000 + 0.5f) * 65536);
            var z = (ushort)Mathf.RoundToInt((position.z / 1000 + 0.5f) * 65536);

            data[offset + 1] = (byte)(x >> 8);
            data[offset + 2] = (byte)(x & 0xFF);
            data[offset + 3] = (byte)(y >> 8);
            data[offset + 4] = (byte)(y & 0xFF);
            data[offset + 5] = (byte)(z >> 8);
            data[offset + 6] = (byte)(z & 0xFF);

            var rotation          = Mathf.Atan2(s.transform.forward.x, s.transform.forward.z);
            var quantizedRotation = (byte)(Mathf.RoundToInt(rotation / (2 * Mathf.PI) * 256) & 0xFF);
            data[offset + 7] =  quantizedRotation;
            offset           += 8;
            
            count++;
            if (count >= 16) break;
        }
    }

    private void DeserializeData() {
        var structureCount = data.Length / 8;
        var offset         = 0;
        var toDeactivate = new bool[structureGhosts.Length][];
        for (var s = 0; s < _spawnedGhosts.Length; s++) {
            toDeactivate[s] = new bool[16];
            for (var i = 0; i < 16; i++) {
                if (_spawnedGhosts[s][i] == null) continue;
                toDeactivate[s][i] = true;
            }
        }

        for (var i = 0; i < structureCount; i++) {
            var type = data[offset] & 0b00001111;
            var id   = (data[offset] & 0b11110000) >> 4;

            var position = new Vector3(
                ((data[offset + 1] << 8) | data[offset + 2]) / 65536f * 1000 - 500,
                ((data[offset + 3] << 8) | data[offset + 4]) / 65536f * 1000 - 500,
                ((data[offset + 5] << 8) | data[offset + 6]) / 65536f * 1000 - 500
            );

            var rotation = data[offset + 7] / 256f * 2 * Mathf.PI;
            UpdateGhost(type, id, position, rotation);
            toDeactivate[type][id] = false;
            offset += 8;
        }
        
        for (var s = 0; s < _spawnedGhosts.Length; s++) {
            for (var i = 0; i < _spawnedGhosts[s].Length; i++) {
                if (toDeactivate[s][i])
                    if (_spawnedGhosts[s][i] != null) _spawnedGhosts[s][i].SetActive(false);
            }
        }
    }

    private void UpdateGhost(int type, int id, Vector3 position, float rotation) {
        var ghost = GetOrCreateGhost(type, id);
        ghost.transform.position = position;
        ghost.transform.rotation = Quaternion.Euler(0, rotation * Mathf.Rad2Deg, 0);

        if (!ghost.activeSelf) {
            ghost.SetActive(true);
            ghost.GetComponentInChildren<InterpolatedTransform>().Teleport();
        }
    }

    private GameObject GetOrCreateGhost(int type, int id) {
        if (_spawnedGhosts[type][id] == null) _spawnedGhosts[type][id] = Instantiate(structureGhosts[type], transform);
        return _spawnedGhosts[type][id];
    }
}