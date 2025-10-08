using Godot;
using System.Threading.Tasks;

public partial class BackButton : Button {
	public override void _Ready() {
		Pressed += OnPressed;
	}

	private async void OnPressed() {
		var tree = GetTree();
		Node current = this;
		Control foundMenu = null;

		// Walk up the tree until we find a node with one of the known menu names
		while (current != null) {
			if (current is Control control) {
				string name = control.Name.ToString().ToLowerInvariant();

				if (name.Contains("optionsmenu") || name.Contains("inputmenu") || name.Contains("mainmenu")) {
					foundMenu = control;
					break;
				}
			}
			current = current.GetParent();
		}

		string targetScenePath = "res://Scenes/UI/MainMenu.tscn";

		if (foundMenu != null) {
			string name = foundMenu.Name.ToString().ToLowerInvariant();

			if (name.Contains("optionsmenu"))
				targetScenePath = "res://Scenes/UI/MainMenu.tscn";
			else if (name.Contains("inputmenu"))
				targetScenePath = "res://Scenes/UI/OptionsMenu.tscn";
		}
		else {
			GD.PushWarning("[BackButton] Could not find menu root — defaulting to MainMenu.");
		}

		await LoadScene(tree, targetScenePath, foundMenu);
	}

	private async Task LoadScene(SceneTree tree, string path, Node toRemove = null) {
		var scene = GD.Load<PackedScene>(path);
		if (scene == null) {
			GD.PushError($"[BackButton] Failed to load scene: {path}");
			return;
		}

		var newScene = scene.Instantiate<Control>();
		tree.Root.AddChild(newScene);
		GD.Print($"[BackButton] Loaded {path}");

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		if (toRemove != null && toRemove.IsInsideTree()) {
			GD.Print($"[BackButton] Removing {toRemove.Name}");
			toRemove.QueueFree();
		}
	}
}
