using Godot;
using System.Threading.Tasks;

public partial class StartGameButton : Button {
	[Export] private PackedScene sceneToSwitchTo; // Assign your first gameplay / overworld scene in the Inspector
	private bool _isTransitioning = false;

	public override void _Ready() {
		Pressed += OnStartGameButtonPressed;
	}

	private async void OnStartGameButtonPressed() {
		if (_isTransitioning) return;
		_isTransitioning = true;

		var tree = GetTree();

		// ------------------------------------------------------------
		// PREVENT REPEAT TRIGGER
		// ------------------------------------------------------------
		ReleaseFocus();           // stop receiving further ui_accept input
		Disabled = true;          // ignore any more presses
		FocusMode = FocusModeEnum.None;
		GetViewport().SetInputAsHandled();
		// --------------------------------------------
		// 1) Fade out
		// --------------------------------------------
		var fader = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (fader != null)
			await fader.FadeOut(0.5f);

		// --------------------------------------------
		// 2) Remove leftover menus (MainMenu, OptionsMenu, InputMenu, etc.)
		// --------------------------------------------
		// Defer freeing menus to the next idle frame (prevents freeing this script's parent mid-call)
		var menusToFree = new Godot.Collections.Array<Node>();
		foreach (Node child in tree.Root.GetChildren()) {
			string lower = child.Name.ToString().ToLowerInvariant();
			if (lower.Contains("menu")) {
				menusToFree.Add(child);
				GD.Print($"[StartGameButton] Queuing menu for removal: {child.Name}");
			}
		}
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		foreach (var menu in menusToFree)
			if (menu.IsInsideTree())
				menu.QueueFree();


		// --------------------------------------------
		// 3) Load gameplay scene
		// --------------------------------------------
		if (sceneToSwitchTo == null) {
			GD.PushError("[StartGameButton] No gameplay scene assigned!");
			_isTransitioning = false;
			return;
		}

		tree.Paused = false; // Just in case menu paused the tree
		tree.ChangeSceneToPacked(sceneToSwitchTo);

		// Wait one frame so the new scene is ready
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		// --------------------------------------------
		// 4) Immediately sync HUD and music
		// --------------------------------------------
		GlobalRoomChange.ForceUpdate(); // calls EnterRoom and triggers HUD/music setup

		// --------------------------------------------
		// 5) Fade in after loading
		// --------------------------------------------
		var newFader = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (newFader != null)
			await newFader.FadeIn(0.5f, true);

		GD.Print("[StartGameButton] Game started successfully.");
		_isTransitioning = false;
	}
}
