
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleButton_EnableMenuExclusive : UdonSharpBehaviour
{
    public string menuName;
    
    private TumbleMenuController _menuController;
    
    private void Start()
    {
        _menuController = GetComponentInParent<TumbleMenuController>();
    }
    
    public void OnClick()
    {
        _menuController.EnableMenuExclusive(menuName);
    }
}
