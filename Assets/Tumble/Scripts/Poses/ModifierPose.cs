using UnityEngine;


public class ModifierPose : Pose {
    public BoxCollider modifierRegion;

    public AudioSource onApplySound;

    private void Start() { modifierRegion.enabled = false; }

    public override void OnPoseEnter() {
        var i              = 0;
        var alreadyApplied = new Structure[100];
        var overlap        = Physics.OverlapBox(modifierRegion.transform.position, modifierRegion.size / 2, modifierRegion.transform.rotation);
        var any            = false;
        foreach (var collider in overlap) {
            if(collider == null) continue;
            
            var structure = collider.GetComponentInParent<Structure>();
            if (structure == null) continue;
            
            var alreadyExists = false;
            for(var x = 0; x < i; x++) {
                if (alreadyApplied[x] == structure) {
                    alreadyExists = true;
                    break;
                }
            }
            if (alreadyExists) continue;
            
            OnApplyModifier(structure);
            alreadyApplied[i] = structure;
            i++;
            any = true;
        }
        
        if(any) onApplySound.Play();
    }
    
    public virtual void OnApplyModifier(Structure structure) {
        
    }
}