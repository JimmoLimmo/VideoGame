using Godot;
using System;

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Stops all AudioStreamPlayer nodes in the current scene tree
    /// </summary>
    public static void StopAllAudio()
    {
        if (Instance?.GetTree() == null) return;

        var root = Instance.GetTree().Root;
        StopAudioInNode(root);
    }

    /// <summary>
    /// Stops audio specifically from menu-related nodes
    /// </summary>
    public static void StopMenuAudio()
    {
        if (Instance?.GetTree() == null) return;

        var root = Instance.GetTree().Root;
        
        // Look for nodes that might contain menu audio
        var mainMenu = root.GetNodeOrNull("MainMenu");
        if (mainMenu != null)
        {
            StopAudioInNode(mainMenu);
        }

        // Also check for any MainMenuManager nodes
        var nodes = Instance.GetTree().GetNodesInGroup("main_menu");
        foreach (var node in nodes)
        {
            if (node is Node menuNode)
            {
                StopAudioInNode(menuNode);
            }
        }
    }

    /// <summary>
    /// Removes any lingering menu-related UI elements that might persist between scenes
    /// </summary>
    public static void CleanupMenuUI()
    {
        if (Instance?.GetTree() == null) return;

        var root = Instance.GetTree().Root;
        
        // Remove any nodes that might be leftover menu elements
        for (int i = root.GetChildCount() - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            
            // Skip essential nodes
            if (child.Name == "AudioManager") continue;
            if (child == Instance.GetTree().CurrentScene) continue;
            
            // Remove potential menu leftovers
            string childName = child.Name.ToString();
            if (childName.Contains("Menu") || 
                childName.Contains("PauseCanvas") ||
                (child is CanvasLayer canvasLayer && canvasLayer.Layer > 50))
            {
                try 
                { 
                    child.QueueFree(); 
                    GD.Print($"AudioManager: Cleaned up lingering UI element: {child.Name}");
                } 
                catch { }
            }
        }
    }

    /// <summary>
    /// Stops AudioStreamPlayer nodes in a specific node and its children
    /// </summary>
    public static void StopAudioInNode(Node node)
    {
        if (node == null) return;

        // Stop AudioStreamPlayer in this node
        if (node is AudioStreamPlayer audioPlayer)
        {
            audioPlayer.Stop();
        }

        // Recursively stop audio in children
        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                StopAudioInNode(childNode);
            }
        }
    }

    /// <summary>
    /// Stops audio specifically in the current scene (but not autoloads/UI overlays)
    /// </summary>
    public static void StopCurrentSceneAudio()
    {
        if (Instance?.GetTree()?.CurrentScene == null) return;

        StopAudioInNode(Instance.GetTree().CurrentScene);
    }
}