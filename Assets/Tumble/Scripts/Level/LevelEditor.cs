using System;

using TMPro;

using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


public class LevelEditor : UdonSharpBehaviour {
    public TumbleLevel level;
    public Transform   synchronizerHolder;

    public LevelEditorTool tool;
    
    private LevelEditorSynchronizer[] _synchronizers;

    public LevelEditorSynchronizer LocalSynchronizer => GetSynchronizer(Networking.LocalPlayer);

    private TumbleLevelLoader64 _loader;

    private LevelEditorSynchronizer GetFirstAvailableSynchronizer() {
        for (var i = 0; i < _synchronizers.Length; i++) {
            var synchronizer = _synchronizers[i];
            
            if (!synchronizer.HasOwner) return synchronizer;
            
            // Check if this synchronizer is owned by a player that is no longer in the session
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            var passed = true;
            foreach (var p in players) {
                if (p.displayName == synchronizer.playerName) {
                    passed = false;
                    break;
                }
            }
            
            if (passed) return synchronizer;
        }

        return null;
    }
    
    private LevelEditorSynchronizer GetSynchronizer(VRCPlayerApi player) {
        var name = player.displayName;
        for (var i = 0; i < _synchronizers.Length; i++) {
            var synchronizer = _synchronizers[i];
            if (synchronizer.playerName == name) return synchronizer;
        }

        return null;
    }

    private void Start() {
        _synchronizers = synchronizerHolder.GetComponentsInChildren<LevelEditorSynchronizer>();
        _loader        = GetComponentInParent<Universe>().levelLoader;
    }

    private void FixedUpdate() {
        if(!Networking.LocalPlayer.isMaster) return; 
        
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (var p in players) {
            var synchronizer = GetSynchronizer(p);
            if(synchronizer != null) continue;
            
            var firstAvailableSynchronizer = GetFirstAvailableSynchronizer();
            if (firstAvailableSynchronizer == null) continue;
            
            firstAvailableSynchronizer.playerName = p.displayName;
            Networking.SetOwner(p, firstAvailableSynchronizer.gameObject);
        }
    }

    public void AddElement(int elementId, Vector3 position, Quaternion rotation) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        if (elementId < 0 || elementId >= _loader.levelElements.Length) return;

        var cell         = new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
        var positionArray = new DataList();
        positionArray.Add(cell.x);
        positionArray.Add(cell.y);
        positionArray.Add(cell.z);

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Add);
        change["e"] = new DataToken(elementId);
        change["p"] = new DataToken(positionArray);
        change["r"] = new DataToken(TumbleLevelLoader64.EncodeRotation(rotation));

        synchronizer.SubmitChange(change);

        level.AddElement(elementId, cell, rotation);
    }
    
    public void RemoveElement(GameObject element) => RemoveElement(element.transform.localPosition);
    
    public void RemoveElement(Vector3 position) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        var cell          = new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
        var positionArray = new DataList();
        positionArray.Add(cell.x);
        positionArray.Add(cell.y);
        positionArray.Add(cell.z);

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Remove);
        change["p"] = new DataToken(positionArray);

        synchronizer.SubmitChange(change);

        level.RemoveElementAt(cell);
    }

    public void ReceiveChange(DataDictionary change) {
        var changeType = (LevelEditorChangeType)(int)change["t"].Number;

        switch (changeType) {
            case LevelEditorChangeType.Add:
                var elementId     = (int)change["e"].Number;
                var positionArray = change["p"].DataList;
                var position      = new Vector3Int((int)positionArray[0].Number, (int)positionArray[1].Number, (int)positionArray[2].Number);
                var rotation = TumbleLevelLoader64.DecodeRotation((int)change["r"].Number);
                level.AddElement(elementId, position, rotation);
                break;
            case LevelEditorChangeType.Remove:
                var removePositionArray = change["p"].DataList;
                var removePosition      = new Vector3Int((int)removePositionArray[0].Number, (int)removePositionArray[1].Number, (int)removePositionArray[2].Number);
                level.RemoveElementAt(removePosition);
                break;
        }
    }
}