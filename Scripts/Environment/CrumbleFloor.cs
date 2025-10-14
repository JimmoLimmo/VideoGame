using Godot;
using System;

public partial class CrumbleFloor : Node2D {
	private Area2D collider;
	private AnimationPlayer animator;
	private Timer breakTimer;
	private AudioStreamPlayer2D breakSound;

	[Export] public float breakTime = 1.0f;
	[Export] public string FloorID = "room_xx_floor_xx";

	public override void _Ready() {
		collider = GetNode<Area2D>("PlayerCollider");
		animator = GetNode<AnimationPlayer>("AnimationPlayer");
		breakTimer = GetNode<Timer>("BreakTime");
		breakSound = GetNode<AudioStreamPlayer2D>("Audio/BreakSound");
		
		if(GlobalRoomChange.IsWallBroken(FloorID)) QueueFree();

		collider.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body) {
		if (body is Player player) {
			animator.Play("Shake");
			handleBreak(player);
		}
	}

	private async void handleBreak(Player player) {
		GD.Print(player);
		breakTimer.Start(breakTime);
		player.HoldPlayer(breakTime);
		breakSound.Play();
		GlobalRoomChange.MarkWallBroken(FloorID);
		await ToSignal(breakTimer, Timer.SignalName.Timeout);
		QueueFree();
	}

	public void _PlayBreakSound() => breakSound.Play();
}
