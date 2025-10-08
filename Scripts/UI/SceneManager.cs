using Godot;
using System.Threading.Tasks;

public partial class SceneManager : Node {
	private Node _currentMenu;

	public async void SwitchMenu(string scenePath) {
		if (!ResourceLoader.Exists(scenePath)) {
			GD.PushError($"[SceneManager] Scene not found: {scenePath}");
			return;
		}

		// Remove current menu if any
		_currentMenu?.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// Load and add new menu
		var scene = GD.Load<PackedScene>(scenePath);
		if (scene == null) {
			GD.PushError($"[SceneManager] Failed to load: {scenePath}");
			return;
		}

		var newMenu = scene.Instantiate<Control>();
		GetTree().Root.AddChild(newMenu);
		_currentMenu = newMenu;

		GD.Print($"[SceneManager] Switched to {scenePath}");
	}
}
