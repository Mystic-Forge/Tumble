using Tumble.Scripts.Level;

using UnityEngine;


public class AreaGizmo : LevelEditorGizmo {
    public AxisGizmo north;
    public AxisGizmo east;
    public AxisGizmo south;
    public AxisGizmo west;
    public AxisGizmo up;
    public AxisGizmo down;

    public override bool ShouldShowGizmo(LevelEditorTool tool) => tool.mode == LevelEditorToolMode.Select;

    public override void UpdateGizmo(Ray ray, Vector3 point, Vector3Int cell, GameObject element) {
        transform.position = element.transform.position;
        var areaComponent = element.GetComponent<AreaElement>();

        var size = areaComponent.endOffset;
        var half = (Vector3)size / 2f;

        if (south.grabbing) {
            size.z                          += south.lastValue;
            element.transform.localPosition -= new Vector3(0, 0, south.lastValue);
            transform.position              =  element.transform.position;
            south.transform.localPosition   =  new Vector3(half.x, half.y, -0.5f);
            south.lastValue                 -= south.lastValue;
        }
        else {
            south.transform.localPosition = new Vector3(half.x, half.y, -0.5f);
        }

        if (north.grabbing) {
            var northDist = north.transform.localPosition.z - 0.5f;
            size.z                        = Mathf.RoundToInt(northDist);
            north.transform.localPosition = new Vector3(half.x, half.y, size.z + 0.5f);
        }
        else {
            north.transform.localPosition = new Vector3(half.x, half.y, size.z + 0.5f);
        }

        if (west.grabbing) {
            size.x                          += west.lastValue;
            element.transform.localPosition -= new Vector3(west.lastValue, 0, 0);
            transform.position              =  element.transform.position;
            west.transform.localPosition    =  new Vector3(-0.5f, half.y, half.z);
            west.lastValue                  -= west.lastValue;
        }
        else {
            west.transform.localPosition = new Vector3(-0.5f, half.y, half.z);
        }

        if (east.grabbing) {
            var eastDist = east.transform.localPosition.x - 0.5f;
            size.x                       = Mathf.RoundToInt(eastDist);
            east.transform.localPosition = new Vector3(size.x + 0.5f, half.y, half.z);
        }
        else {
            east.transform.localPosition = new Vector3(size.x + 0.5f, half.y, half.z);
        }

        if (down.grabbing) {
            size.y                          += down.lastValue;
            element.transform.localPosition -= new Vector3(0, down.lastValue, 0);
            transform.position              =  element.transform.position;
            down.transform.localPosition    =  new Vector3(half.x, -0.5f, half.z);
            down.lastValue                  -= down.lastValue;
        }
        else {
            down.transform.localPosition = new Vector3(half.x, -0.5f, half.z);
        }

        if (up.grabbing) {
            var upDist = up.transform.localPosition.y - 0.5f;
            size.y                     = Mathf.RoundToInt(upDist);
            up.transform.localPosition = new Vector3(half.x, size.y + 0.5f, half.z);
        }
        else {
            up.transform.localPosition = new Vector3(half.x, size.y + 0.5f, half.z);
        }

        if(size.x < 0) size.x = 0;
        if(size.y < 0) size.y = 0;
        if(size.z < 0) size.z = 0;
        
        areaComponent.endOffset = size;
        areaComponent.UpdateState();
    }
}