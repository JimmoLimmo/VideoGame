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

		// Find the menu root (MainMenu / OptionsMenu / InputMenu)
		while (current != null) {
			if (current is Control c) {
				string n = c.Name.ToString().ToLowerInvariant();
				if (n.Contains("optionsmenu") || n.Contains("inputmenu") || n.Contains("mainmenu")) {
					foundMenu = c;
					break;
				}
			}
			current = current.GetParent();
		}

		string target = "res://Scenes/UI/MainMenu.tscn";
		if (foundMenu != null) {
			string n = foundMenu.Name.ToString().ToLowerInvariant();
			if (n.Contains("optionsmenu")) target = "res://Scenes/UI/MainMenu.tscn";
			else if (n.Contains("inputmenu")) target = "res://Scenes/UI/OptionsMenu.tscn";
		}

		await LoadScene(tree, target, foundMenu);
	}

	private async Task LoadScene(SceneTree tree, string path, Node toRemove = null) {
		// Clean up any lingering ConfirmationPopup anywhere
		foreach (Node node in tree.Root.GetChildren()) {
			if (node is Control c && c.HasNode("ConfirmationPopup")) {
				var popup = c.GetNode<Popup>("ConfirmationPopup");
				if (popup != null && popup.IsInsideTree()) {
					GD.Print($"[BackButton] Closing lingering popup in {c.Name}");
					popup.QueueFree();
				}
			}
		}

		var scene = GD.Load<PackedScene>(path);
		if (scene == null) {
			GD.PushError($"[BackButton] Failed to load scene: {path}");
			return;
		}

		var newScene = scene.Instantiate<Control>();
		tree.Root.AddChild(newScene);
		GD.Print($"[BackButton] Loaded {path}");

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		if (newScene.HasMethod("RegrabFocus"))
			newScene.CallDeferred("RegrabFocus");

		if (toRemove != null && toRemove.IsInsideTree()) {
			GD.Print($"[BackButton] Removing {toRemove.Name}");
			toRemove.QueueFree();
		}
	}
}
