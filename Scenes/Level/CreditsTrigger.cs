using Godot;
using System;

public partial class CreditsTrigger : Area2D {
	[Export] public string CreditsScenePath = "res://Levels/credits.tscn";
	[Export] public AudioStreamPlayer2D PickupSound;

	private bool _activated = false;
	private AnimationPlayer animator;

	public override void _Ready() {
		animator = GetNode<AnimationPlayer>("AnimationPlayer");
		BodyEntered += OnBodyEntered;
		animator.Play("Float");
	}

	private async void OnBodyEntered(Node body) {
		if (_activated) return;
		if (body is not Player player) return;
		_activated = true;

		GD.Print("[CreditsTrigger] Player touched credits trigger.");

		PickupSound?.Play();

		// Optional: small delay to let sound play
		await ToSignal(GetTree().CreateTimer(0.8), Timer.SignalName.Timeout);

		var fade = GetTree().Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");
		if (fade != null)
			await fade.FadeOut(0.6f);

		GetTree().ChangeSceneToFile(CreditsScenePath);
	}
}
