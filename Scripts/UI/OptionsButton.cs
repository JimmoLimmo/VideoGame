using Godot;
using System.Threading.Tasks;

public partial class OptionsButton : Button {
	public override void _Ready() {
		Pressed += OnPressed;
	}

	private async void OnPressed() {
		var tree = GetTree();

		// --- Load OptionsMenu scene ---
		var optionsScene = GD.Load<PackedScene>("res://Scenes/UI/OptionsMenu.tscn");
		if (optionsScene == null) {
			GD.PushError("[OptionsButton] Failed to load OptionsMenu scene!");
			return;
		}

		var optionsMenu = optionsScene.Instantiate<Control>();
		tree.Root.AddChild(optionsMenu);
		GD.Print("[OptionsButton] OptionsMenu instantiated successfully!");

		// --- Wait one frame so it renders ---
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		// --- Remove old MainMenu ---
		var mainMenu = GetParent().GetParent();
		if (mainMenu != null && mainMenu.IsInsideTree()) {
			GD.Print("[OptionsButton] Removing old MainMenu.");
			mainMenu.QueueFree();
		}
	}
}
