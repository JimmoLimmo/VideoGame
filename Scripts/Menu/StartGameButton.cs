using Godot;

public partial class StartGameButton : Button
{
    [Export] private PackedScene sceneToSwtichTo;

    public override void _Ready()
    {
        Pressed += OnStartGameButtonPressed;
    }

    private void OnStartGameButtonPressed()
    {
        if (sceneToSwtichTo == null) return;

        var tree = GetTree();
        tree.Paused = false;                      // make sure we leave the title unpaused
        tree.ChangeSceneToPacked(sceneToSwtichTo); // REPLACE the scene instead of AddChild
    }
}
