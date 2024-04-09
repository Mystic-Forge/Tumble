using System.Collections;
using System.Collections.Generic;

using UdonSharp;

using UnityEngine;

[RequireComponent(typeof(TumbleButton)),UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleButton_ToggleMenu : UdonSharpBehaviour
{
    public  string               menuName;
    private TumbleMenuController _menuController;
    
    private void Start()
    {
        _menuController = GetComponentInParent<TumbleMenuController>();
    }
    
    public void OnClick()
    {
        _menuController.ToggleMenu(menuName);
    }
}
