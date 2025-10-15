using Godot;
using System.Threading.Tasks;

public partial class GameOverMenu : Control {
	private Button _retryBtn;
	private Button _quitBtn;
	private ColorRect _overlay;
	private CanvasLayer _layer;

	public override void _Ready() {
		Visible = false;
		_layer = GetNode<CanvasLayer>("GameOverLayer");
		_overlay = GetNode<ColorRect>("GameOverLayer/ColorRect");
		_retryBtn = GetNode<Button>("GameOverLayer/CenterContainer/VBoxContainer/RetryBtn");
		_quitBtn = GetNode<Button>("GameOverLayer/CenterContainer/VBoxContainer/QuitBtn");

		_retryBtn.Pressed += OnRetryPressed;
		_quitBtn.Pressed += OnQuitPressed;

		_layer.Layer = 60; // above pause menu
		HideAll();

		ProcessMode = ProcessModeEnum.Always;
	}

	public void ShowGameOver() {
		GetTree().Paused = true;
		Visible = true;
		_layer.Visible = true;
		_retryBtn.GrabFocus();

		var tween = CreateTween();
		_overlay.Modulate = new Color(0, 0, 0, 0);
		Modulate = new Color(1, 1, 1, 0);
		tween.TweenProperty(_overlay, "modulate:a", 0.7f, 0.4);
		tween.TweenProperty(this, "modulate:a", 1.0f, 0.4);
	}

	// private async void OnRetryPressed() {
	// 	var tree = GetTree();
	// 	var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

	// 	if (fader != null)
	// 		await fader.FadeOut(0.4f);

	// 	HideAll();
	// 	tree.Paused = false;

	// 	// 1) Decide which room to load (checkpoint room if available, else room_01)
	// 	string roomPath = GlobalRoomChange.GetRespawnRoomPath();

	// 	// 2) Change scene to that room
	// 	tree.ChangeSceneToFile(roomPath);

	// 	// 3) Wait for the scene to be fully ready
	// 	await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
	// 	await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

	// 	// 4) Compute a good spawn (nearest door to the checkpoint position)
	// 	var scene = tree.CurrentScene;
	// 	var targetPos = GlobalRoomChange.GetRespawnPositionForLoadedScene(scene);

	// 	// 5) Place player and give a brief door grace
	// 	var player = scene?.GetNodeOrNull<Player>("Player");
	// 	if (player != null) {
	// 		GlobalRoomChange.PlayerPos = targetPos; // keep global in sync
	// 		player.GlobalPosition = targetPos;
	// 		player.BeginDoorGrace(0.6f);
	// 	}
	// 	GD.Print($"[Respawn Debug] Player found? {player != null}, position={player?.GlobalPosition}");

	// 	if (fader != null)
	// 		await fader.FadeIn(0.4f);
	// }
	private async void OnRetryPressed() {
		var tree = GetTree();
		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

		if (fader != null)
			await fader.FadeOut(0.4f);

		HideAll();
		tree.Paused = false;


		GlobalRoomChange.Activate = true;
		GlobalRoomChange.PlayerJumpOnEnter = false;
		GlobalRoomChange.PlayerPos = Vector2.Zero;   // will override below

		// 1  Decide which room to load
		string roomPath = GlobalRoomChange.GetRespawnRoomPath();
		tree.ChangeSceneToFile(roomPath);

		// 2️  Wait for scene to be ready
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		var scene = tree.CurrentScene;
		if (scene == null) {
			GD.PushError("[GameOver] Scene failed to load!");
			return;
		}

		// 3️ Find a valid door spawn point in that scene
		Vector2 targetPos = GlobalRoomChange.GetRespawnPositionForLoadedScene(scene);

		// 4  Get the Player node
		// Use %Player so it finds by unique name anywhere in the scene
		var player = scene.GetNodeOrNull<Player>("%Player");
		if (player == null) {
			// GD.PushError("[GameOver] Player not found in scene!");
			return;
		}

		// 5️  Apply position and door grace
		player.GlobalPosition = targetPos;
		player.BeginDoorGrace(0.6f);

		// 6️  Force the camera to follow again
		var cam = player.GetNodeOrNull<Camera2D>("Camera2D");
		if (cam != null)
			cam.MakeCurrent();

		// 7️ (Optional) visualize spawn point for debugging
		var marker = new ColorRect {
			Color = new Color(1, 0, 0, 0.6f),
			Size = new Vector2(16, 16),
			Position = targetPos - new Vector2(8, 8)
		};
		scene.AddChild(marker);
		marker.ZIndex = 1000;
		GD.Print($"[GameOver] Respawn marker created at {targetPos}");

		// 8️  Fade back in
		if (fader != null)
			await fader.FadeIn(0.4f);

		GD.Print($"[GameOver] Player respawned at {targetPos}");
	}



	private async void OnQuitPressed() {
		var tree = GetTree();
		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

		if (fader != null)
			await fader.FadeOut(0.4f);

		HideAll();
		tree.Paused = false;

		// Reset global state
		GlobalRoomChange.hasSword = false;
		GlobalRoomChange.hasDash = false;
		GlobalRoomChange.hasWalljump = false;
		GlobalRoomChange.health = 5;
		GlobalRoomChange.mana = 0;
		GlobalRoomChange.Activate = false;

		GlobalRoomChange.CheckpointRoom = "";
		GlobalRoomChange.CheckpointPos = Vector2.Zero;


		tree.ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");

		if (fader != null)
			await fader.FadeIn(0.4f);
	}

	private void HideAll() {
		Visible = false;
		if (_layer != null)
			_layer.Visible = false;
	}

}
