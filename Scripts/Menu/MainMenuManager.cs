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
        // Stop any currently playing audio before scene transition
        AudioManager.StopAllAudio();

        // Instantiate the requested scene and replace the current scene cleanly.
        var newScene = loadScene.Instantiate() as Node;
        if (newScene == null)
        {
            GD.PrintErr("MainMenuManager: failed to instantiate scene in onSwapScene");
            return;
        }

        var current = GetTree().CurrentScene;
        GetTree().Root.AddChild(newScene);
        try { GetTree().CurrentScene = newScene; } catch { }

        // Free the previous current scene if present to avoid stacking
        try { if (current != null) current.QueueFree(); } catch { }

        // Free the main menu manager's scene (this control) so the new scene becomes the only visible scene
        try { QueueFree(); } catch { }
    }

}


