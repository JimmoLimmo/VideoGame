using Godot;
using System.Threading.Tasks;

public partial class BackButton : Button {
	public override void _Ready() {
		Pressed += OnPressed;
	}

	private async void OnPressed() {
		var tree = GetTree();

		// --- Load MainMenu scene ---
		var mainMenuScene = GD.Load<PackedScene>("res://Scenes/UI/MainMenu.tscn");
		if (mainMenuScene == null) {
			GD.PushError("[BackButton] Failed to load MainMenu scene!");
			return;
		}

		var mainMenu = mainMenuScene.Instantiate<Control>();
		tree.Root.AddChild(mainMenu);
		GD.Print("[BackButton] MainMenu instantiated successfully!");

		// --- Wait a frame for rendering ---
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		// --- Remove old OptionsMenu ---
		var optionsMenu = GetParent().GetParent();
		if (optionsMenu != null && optionsMenu.IsInsideTree()) {
			GD.Print("[BackButton] Removing old OptionsMenu.");
			optionsMenu.QueueFree();
		}
	}
}
