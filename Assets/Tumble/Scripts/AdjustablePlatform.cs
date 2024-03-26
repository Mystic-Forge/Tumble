using TMPro;

using UdonSharp;
using UnityEngine;

public class AdjustablePlatform : UdonSharpBehaviour {
    public Vector3 increment;
    public int     distance;
    
    public TextMeshProUGUI distanceText;
    
    public void UpClick() {
        distance++;
        transform.localPosition = increment * distance;
        UpdateText();
    }
    
    public void DownClick() {
        distance--;
        distance = Mathf.Max(0, distance);
        transform.localPosition = increment * distance;
        UpdateText();
    }
    
    private void UpdateText() {
        distanceText.text = $"{(int)(increment * distance).magnitude}m";
    }
}
