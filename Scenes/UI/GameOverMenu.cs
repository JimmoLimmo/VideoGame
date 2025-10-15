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

	private async void OnRetryPressed() {
		var tree = GetTree();
		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

		if (fader != null)
			await fader.FadeOut(0.4f);

		string targetRoom;
		Vector2 spawnPos;

		if (GlobalRoomChange.HasCheckpoint) {
			targetRoom = GlobalRoomChange.CheckpointRoom;
			spawnPos = GlobalRoomChange.CheckpointPos;
			GD.Print($"[Retry] Respawning at checkpoint {targetRoom} → {spawnPos}");
		}
		else {
			// fallback to current scene
			targetRoom = tree.CurrentScene.SceneFilePath;
			spawnPos = Vector2.Zero; // or your default spawn
			GD.Print("[Retry] No checkpoint found, restarting current room.");
		}

		GlobalRoomChange.Activate = true;
		GlobalRoomChange.PlayerPos = spawnPos;
		GlobalRoomChange.PlayerJumpOnEnter = false;

		tree.ChangeSceneToFile(targetRoom);

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		GlobalRoomChange.ForceUpdate();

		if (fader != null)
			await fader.FadeIn(0.4f, true);
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
