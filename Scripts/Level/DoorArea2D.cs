using Godot;
using System;

public partial class DoorArea2D : Area2D {
	[Export] public string ConnectedRoom;
	[Export] public Vector2 PlayerPos;
	[Export] public bool PlayerJumpOnEnter = false;

	private bool _busy;

	public override void _Ready() {
		BodyEntered += OnBodyEntered;
	}

	private async void OnBodyEntered(Node body) {
		if (_busy) return;
		// if (!body.IsInGroup("player")) 
		// 	return;

		if (body is not Player player)
			return;

		if (!player.CanUseDoors)
			return;
		_busy = true;

		var tree = GetTree();

		GlobalRoomChange.LastExitName = Name;
		GlobalRoomChange.LastExitRoom = GetTree().CurrentScene.SceneFilePath;

		// Set spawn params for the next scene
		GlobalRoomChange.Activate = true;
		GlobalRoomChange.PlayerPos = PlayerPos;
		GlobalRoomChange.PlayerJumpOnEnter = PlayerJumpOnEnter;

		// Fade OUT (if this scene has a ScreenFade)
		var fade = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (fade != null)
			await fade.FadeOut(0.25f);

		// Switch scenes
		tree.ChangeSceneToFile(ConnectedRoom);

		// Wait one frame so new scene is ready
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		// Fade IN (if the new scene has a ScreenFade)
		fade = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
		if (fade != null)
			await fade.FadeIn(0.25f);

		_busy = false;
	}
}
