
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleMenuController : UdonSharpBehaviour {
    public GameObject[] menus;
    
    public void EnableMenu(string name) {
        foreach (var menu in menus) if(menu.name == name) menu.SetActive(true);
    }
    
    public void DisableMenu(string name) {
        foreach (var menu in menus) if(menu.name == name) menu.SetActive(false);
    }
    
    public void ToggleMenu(string name) {
        foreach (var menu in menus) if(menu.name == name) menu.SetActive(!menu.activeSelf);
    }
    
    public void EnableMenuExclusive(string name) {
        foreach (var menu in menus) menu.SetActive(menu.name == name);
    }
    
    public void DisableAllMenus() {
        foreach (var menu in menus) menu.SetActive(false);
    }
}
