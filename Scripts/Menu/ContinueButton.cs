using Godot;
using System;

public partial class ContinueButton : Button
{
    [Export] PackedScene defaultScene;

    public override void _Ready()
    {
        Pressed += OnPressed;

        // Enable only if a save exists (use cached API)
        var save = SaveManager.GetCurrentSave();
        Disabled = save == null;
    }

    private void OnPressed()
    {
        var save = SaveManager.GetCurrentSave();
        if (save == null)
        {
            return;
        }

        // Stop any currently playing audio before scene transition
        AudioManager.StopAllAudio();

        // Load save data and change to the correct scene
        string targetScene = save.CurrentScene;
        
        // If no scene is saved or it's empty, default to room_01
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "res://Levels/room_01.tscn";
            GD.Print("[ContinueButton] No scene in save data, defaulting to room_01");
        }

        GD.Print($"[ContinueButton] Loading game and changing to scene: {targetScene}");
        
        // Change to the saved scene - player will auto-load from save data in their _Ready method
        GetTree().ChangeSceneToFile(targetScene);
    }
}