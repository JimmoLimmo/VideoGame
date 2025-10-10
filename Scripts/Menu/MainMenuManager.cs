using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenuManager : Control {
<<<<<<< HEAD
    List<int> goBackList = new();

    public override void _Ready() {
        var first = GetNodeOrNull<Button>("CenterContainer/VBoxContainer/NewGameBtn");
        if (first != null)
            first.GrabFocus();
        else
            GD.PushWarning("[MainMenuManager] StartGameBtn not found — check node path.");
    }

    public void swapMenu(int menuIndex, int returnIndex) {
        if (GetChild(menuIndex) is MenuTab menuTab)
            menuTab.Visible = true;

        if (returnIndex < 0) return;
        goBackList.Add(returnIndex);
    }

    public void swapMenuToPrevious() {
        if (!goBackList.Any()) return;
        swapMenu(goBackList[^1], -1);
        goBackList.RemoveAt(goBackList.Count - 1);
    }

    public void onSwapScene(PackedScene loadScene) {
        GetTree().Root.AddChild(loadScene.Instantiate());
        QueueFree();
    }
=======
	List<int> goBackList = new();

	public override void _Ready() {
		var first = GetNodeOrNull<Button>("MenuTab/VBoxContainer/StartGameBtn");
		if (first != null)
			first.GrabFocus();
		else
			GD.PushWarning("[MainMenuManager] StartGameBtn not found — check node path.");
	}

	public void swapMenu(int menuIndex, int returnIndex) {
		if (GetChild(menuIndex) is MenuTab menuTab)
			menuTab.Visible = true;

		if (returnIndex < 0) return;
		goBackList.Add(returnIndex);
	}

	public void swapMenuToPrevious() {
		if (!goBackList.Any()) return;
		swapMenu(goBackList[^1], -1);
		goBackList.RemoveAt(goBackList.Count - 1);
	}

	public void onSwapScene(PackedScene loadScene) {
		GetTree().Root.AddChild(loadScene.Instantiate());
		QueueFree();
	}
>>>>>>> Aidan
}
