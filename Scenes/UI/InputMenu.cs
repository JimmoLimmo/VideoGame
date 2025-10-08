using Godot;
using System.Threading.Tasks;

public partial class InputMenu : Control {
	private Button _backButton;

	public override void _Ready() {
		_backButton = GetNode<Button>("CenterContainer/VBoxContainer/BackButton");
		_backButton.Pressed += OnBackPressed;
	}

	private async void OnBackPressed() {
		var tree = GetTree();

		// --- Load OptionsMenu ---
		var scene = GD.Load<PackedScene>("res://Scenes/UI/OptionsMenu.tscn");
		if (scene == null) {
			GD.PushError("[InputMenu] Failed to load OptionsMenu.tscn");
			return;
		}

		// --- Remove current InputMenu first (prevents overlap) ---
		QueueFree();

		// --- Add new OptionsMenu after one frame ---
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		var optionsMenu = scene.Instantiate<Control>();
		tree.Root.AddChild(optionsMenu);

		GD.Print("[InputMenu] Switched back to OptionsMenu.");
	}
}
