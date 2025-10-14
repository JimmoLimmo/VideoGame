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

		HideAll();
		tree.Paused = false;

		tree.ReloadCurrentScene();

		// Wait a few frames for scene load
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		// Now reposition player to last exit
		var currentScene = tree.CurrentScene;
		GlobalRoomChange.SetRespawnToLastExit(currentScene);

		var player = currentScene.GetNodeOrNull<Player>("Player");
		if (player != null) {
			player.GlobalPosition = GlobalRoomChange.PlayerPos;
			player.BeginDoorGrace(0.6f);
		}

		if (fader != null)
			await fader.FadeIn(0.4f);

		if (string.IsNullOrEmpty(GlobalRoomChange.LastExitName)) {
			GlobalRoomChange.PlayerPos = GlobalRoomChange.FindNearestDoor(currentScene, player.GlobalPosition);
		}
		player.GlobalPosition = GlobalRoomChange.PlayerPos;
		player.BeginDoorGrace(0.6f);


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
