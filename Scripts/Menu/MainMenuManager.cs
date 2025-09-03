using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenuManager : Control
{
    List<int> goBackList = new();

    public void swapMenu(int menuIndex, int returnIndex)
    {
        if (GetChild(menuIndex) is MenuTab menuTab)
        {
            menuTab.Visible = true;
        }

        if (returnIndex < 0) return;
        goBackList.Add(returnIndex);
    }
    public void swapMenuToPrevious()
    {
        if (!goBackList.Any()) return;
        swapMenu(goBackList[goBackList.Count - 1], -1);
        goBackList.RemoveAt(goBackList.Count - 1);
    }

    public void onSwapScene(PackedScene loadScene)
    {
        GetTree().Root.AddChild(loadScene.Instantiate());
        QueueFree();
    }

}


