using Godot;
using System.Threading.Tasks;

public partial class InputMenuButton : Button {
    public override void _Ready() {
        Pressed += OnPressed;
    }

    private async void OnPressed() {
        var tree = GetTree();

        // Load InputMenu scene
        var scene = GD.Load<PackedScene>("res://Scenes/UI/InputMenu.tscn");
        if (scene == null) {
            GD.PushError("[InputMenuButton] Failed to load InputMenu!");
            return;
        }

        var inputMenu = scene.Instantiate<Control>();
        tree.Root.AddChild(inputMenu);
        GD.Print("[InputMenuButton] InputMenu instantiated successfully!");

        // Wait one frame, then remove OptionsMenu
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var optionsMenu = GetParent().GetParent();
        if (optionsMenu != null && optionsMenu.IsInsideTree()) {
            GD.Print("[InputMenuButton] Removing old OptionsMenu...");
            optionsMenu.QueueFree();
        }
    }
}
