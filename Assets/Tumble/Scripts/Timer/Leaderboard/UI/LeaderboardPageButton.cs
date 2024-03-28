
using System;

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LeaderboardPageButton : UdonSharpBehaviour
{
    public PageButtonMode mode;
    
    private Button _button;
    private LeaderboardListDisplay _leaderboardListDisplay;
    
    private void Start()
    {
        _button = GetComponent<Button>();
        _leaderboardListDisplay = GetComponentInParent<LeaderboardListDisplay>();
    }

    private void Update() {
        switch (mode)
        {
            case PageButtonMode.First:
            case PageButtonMode.Previous:
                _button.interactable = _leaderboardListDisplay.currentPage > 0;
                break;
            case PageButtonMode.Next:
            case PageButtonMode.Last:
                _button.interactable = _leaderboardListDisplay.currentPage < _leaderboardListDisplay.PageCount - 1;
                break;
        }
    }

    public void OnClick()
    {
        switch (mode)
        {
            case PageButtonMode.First:
                _leaderboardListDisplay.FirstPage();
                break;
            case PageButtonMode.Previous:
                _leaderboardListDisplay.PreviousPage();
                break;
            case PageButtonMode.Next:
                _leaderboardListDisplay.NextPage();
                break;
            case PageButtonMode.Last:
                _leaderboardListDisplay.LastPage();
                break;
        }
    }
}
public enum PageButtonMode {
    First,
    Previous,
    Next,
    Last,
}
