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
        // Stop any currently playing audio before scene transition
        AudioManager.StopAllAudio();

        // Starting a new game should clear any existing cached save so we don't resume previous progress.
        SaveManager.ResetToNewGame(deleteFile: true);

        if (GetParent().GetParent() is MenuTab menuTab)
        {
            menuTab.loadSceneRequest(sceneToSwtichTo);
        }
    }
}
