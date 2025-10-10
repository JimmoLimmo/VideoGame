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

        // For now, load defaultScene (level_1) and apply save after scene instanced
        if (defaultScene == null)
        {
            GD.PrintErr("ContinueButton: defaultScene not set");
            return;
        }

        var newScene = defaultScene.Instantiate() as Node;
        if (newScene == null)
        {
            GD.PrintErr("ContinueButton: failed to instantiate defaultScene");
            return;
        }

        

        var current = GetTree().CurrentScene;

        // Add new scene to root and set it as the current scene
        GetTree().Root.AddChild(newScene);
        
        try { GetTree().CurrentScene = newScene; } catch { }
        

        // Free the previous scene to avoid stacking (if any)
        try { if (current != null) current.QueueFree(); } catch { }

        // Try to find Player node in the newly loaded scene and apply save data (recursive search)
        Player FindPlayerRecursive(Node node)
        {
            if (node is Player p) return p;
            foreach (var child in node.GetChildren())
            {
                if (child is Node cn)
                {
                    var found = FindPlayerRecursive(cn);
                    if (found != null) return found;
                }
            }
            return null;
        }

        var player = FindPlayerRecursive(newScene);
        if (player != null)
        {
            
            // Defer applying save so Player._Ready/_Process doesn't overwrite our restore
            player.CallDeferred(nameof(Player.ApplySaveDataFromManager), true);
        }
        else
        {
            GD.PrintErr("ContinueButton: no Player instance found in new scene");
        }

        // Close menu and free the main menu if appropriate
        var parent = GetParent();
        if (parent is MenuTab menuTab)
        {
            menuTab.Visible = false;
            try { menuTab.QueueFree(); } catch { }
        }
    }
}
