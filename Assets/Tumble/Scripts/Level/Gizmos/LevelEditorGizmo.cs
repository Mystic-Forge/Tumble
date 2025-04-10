using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelEditorGizmo : TumbleBehaviour {
    public GameObject[] blacklist;
    public GameObject[] whitelist;

    public virtual void UpdateGizmo(Ray ray, Vector3 point, Vector3Int cell, GameObject element) { transform.position = Universe.levelEditor.level.GetWorldPosition(cell); }

    public virtual bool IsWhitelisted(GameObject obj, int elementId) {
        if(blacklist.Length != 0)
            foreach (var go in blacklist) {
                var index = Universe.levelLoader.GetIdOfElement(go);
                if (index == elementId) return false;
                if (go == obj) return false;
            }

        if (whitelist.Length != 0) {
            foreach (var go in whitelist) {
                var index = Universe.levelLoader.GetIdOfElement(go);
                if (index == elementId) return true;
                if (go == obj)
                    return true;
            }
            
            return false;
        }

        return true;
    }

    public virtual bool ShouldShowGizmo(LevelEditorTool tool) => true;

    public virtual void OnClickGizmo(Ray ray, Vector3 point) {
        
    }
    
    public virtual void OnDragGizmo(Ray ray) {
        
    }
    
    public virtual void OnReleaseGizmo(Ray ray) {
        
    }
}