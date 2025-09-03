using Godot;
using System;

public partial class StartGameButton : Button
{
    [Export] PackedScene sceneToSwtichTo;

    public override void _Ready()
    {
        Pressed += onStartGameButtonPressed;
    }

    private void onStartGameButtonPressed()
    {
        if (GetParent().GetParent() is MenuTab menuTab)
        {
            menuTab.loadSceneRequest(sceneToSwtichTo);
        }
    }
}
