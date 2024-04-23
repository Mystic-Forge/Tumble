using System.Text;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelDatabase : UdonSharpBehaviour {
    public  VRCUrl url;

    public DataDictionary levels = new DataDictionary();
    
    private string              _rawData;
    private Universe            _universe;
    private TumbleLevelLoader64 _loader;

    private void Start() {
        Debug.Log($"[TUMBLE] Loading level database");
        _universe = GetComponentInParent<Universe>();
        _loader = _universe.levelLoader;
        VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result) {
        _rawData = result.Result;
        Debug.Log($"[TUMBLE] Loaded level database: {_rawData.Length} bytes");
        
        ParseLevels();
    }

    private void ParseLevels() {
        var splits = _rawData.Split('\n'); // TODO: This copies the entire string, could be optimized, but it's not a big deal,
                                           // so I'm not going to bother, but it's worth noting, I guess, so I'm writing this comment,
                                           // but I'm not going to do anything about it, so, yeah. - GPT

        Debug.Log($"[TUMBLE] Parsing {splits.Length - 1} levels from database");
        
        foreach (var level in splits) {
            if(string.IsNullOrWhiteSpace(level)) continue;
            var data  = level.Trim().Split(',');

            if (data.Length != 2) {
                Debug.LogError($"[TUMBLE] Invalid level data: {level}");
                continue;
            }
            
            var hexId = data[0];
            var levelData  = data[1];
            
            var levelDataDictionary = _loader.DeserializeLevelData(levelData);
            levels.Add(hexId, levelDataDictionary);
        }
        
        Debug.Log($"[TUMBLE] Successfully parsed {levels.Count} levels from database");
        
        _universe.BroadcastCustomEvent("EventLevelDatabaseLoaded");
    }

    public override void OnStringLoadError(IVRCStringDownload result) { Debug.LogError($"[TUMBLE] Error loading level database: {result.ErrorCode} - {result.Error}"); }
    
    public DataDictionary GetLevel(string hexId) {
        if (levels.TryGetValue(hexId, out var level)) return level.DataDictionary;
        Debug.LogError($"[TUMBLE] Level not found: {hexId}");
        return null;
    }
}