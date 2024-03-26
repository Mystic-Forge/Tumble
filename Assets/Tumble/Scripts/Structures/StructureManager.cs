using System;

using UdonSharp;


public class StructureManager : UdonSharpBehaviour {
    private void Start() {
        for (int j = 0; j < transform.childCount; j++) {
            var holder = transform.GetChild(j);

            for (var i = 0; i < holder.childCount; i++) {
                var child = holder.GetChild(i);
                child.GetComponent<Structure>().Initialize();
            }
        }
    }

    public Structure SpawnStructure(string structure) {
        var holder = transform.Find(structure);
        if (holder == null) return null;

        for (var i = 0; i < holder.childCount; i++) {
            var child = holder.GetChild(i);
            if (child.gameObject.activeSelf) continue;

            child.gameObject.SetActive(true);
            return child.GetComponent<Structure>();
        }

        return null;
    }

    public Structure[] GetSpawnedStructures() {
        var count      = 0;
        for (var i = 0; i < transform.childCount; i++) {
            var holder = transform.GetChild(i);

            for (var k = 0; k < holder.childCount; k++) {
                var child = holder.GetChild(k).GetComponent<Structure>();
                if (!child.gameObject.activeSelf) continue;
                count++;
            }
        }
        
        var structures = new Structure[count];
        var j          = 0;
        for (var i = 0; i < transform.childCount; i++) {
            var holder = transform.GetChild(i);

            for (var k = 0; k < holder.childCount; k++) {
                var child = holder.GetChild(k).GetComponent<Structure>();
                if (!child.gameObject.activeSelf) continue;
                structures[j] = child;
                j++;
            }
        }

        return structures;
    }
}