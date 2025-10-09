using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenuManager : Control {
    private readonly List<int> _goBack = new();

    public override void _Ready() {
        GetViewport().GuiDisableInput = false;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        SetProcessUnhandledInput(true);
        SetProcessUnhandledKeyInput(true);

        CallDeferred(nameof(RegrabFocus));
    }

    private void RegrabFocus() {
        // Prefer NewGame, fall back to first button under VBox
        var first = GetNodeOrNull<Button>("CenterContainer/VBoxContainer/NewGameBtn")
                 ?? GetNodeOrNull<Button>("CenterContainer/VBoxContainer/ContinueBtn")
                 ?? GetNodeOrNull<Button>("CenterContainer/VBoxContainer/OptionsBtn")
                 ?? GetNodeOrNull<Button>("CenterContainer/VBoxContainer/QuitBtn");

        if (first != null) {
            first.GrabFocus();
            GD.Print("[MainMenuManager] Focus set to ", first.Name);
        }
        else {
            GD.PushWarning("[MainMenuManager] No focusable button found.");
        }
    }

    // Keeping your API for completeness
    public void swapMenu(int menuIndex, int returnIndex) {
        if (GetChild(menuIndex) is MenuTab tab) tab.Visible = true;
        if (returnIndex >= 0) _goBack.Add(returnIndex);
    }

    public void swapMenuToPrevious() {
        if (!_goBack.Any()) return;
        swapMenu(_goBack[^1], -1);
        _goBack.RemoveAt(_goBack.Count - 1);
    }

    public void onSwapScene(PackedScene loadScene) {
        GetTree().ChangeSceneToPacked(loadScene);
    }
}
