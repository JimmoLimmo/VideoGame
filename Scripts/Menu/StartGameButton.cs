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

<<<<<<< HEAD
=======
		// Reset save data for new game
		SaveManager.ResetToNewGame(true);
		
		// Reset GlobalRoomChange state for new game
		GlobalRoomChange.Activate = false;
		GlobalRoomChange.hasSword = false;
		GlobalRoomChange.hasDash = false;
		GlobalRoomChange.hasWalljump = false;
		GlobalRoomChange.hasClawTeleport = false;
		GlobalRoomChange.health = 5;
		GlobalRoomChange.mana = 0;

>>>>>>> Aidan
		var tree = GetTree();


		ReleaseFocus();           // stop receiving further ui_accept input
		Disabled = true;          // ignore any more presses
		FocusMode = FocusModeEnum.None;
		GetViewport().SetInputAsHandled();

		var fader = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (fader != null)
			await fader.FadeOut(0.5f);

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



		if (sceneToSwitchTo == null) {
			GD.PushError("[StartGameButton] No gameplay scene assigned!");
			_isTransitioning = false;
			return;
		}

		tree.Paused = false; // Just in case menu paused the tree
		tree.ChangeSceneToPacked(sceneToSwitchTo);

		// Wait one frame so the new scene is ready
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);


		GlobalRoomChange.ForceUpdate(); // calls EnterRoom and triggers HUD/music setup

		var newFader = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (newFader != null)
			await newFader.FadeIn(0.5f, true);

		GD.Print("[StartGameButton] Game started successfully.");
		_isTransitioning = false;
	}
}
