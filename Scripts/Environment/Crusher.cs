using Godot;
using System;
using System.Collections.Generic;

public partial class Crusher : Node {
	
	[Export] public float upDuration = 1f;
	[Export] public float downDuration = 0.5f;
	[Export] public float movementTime = 0.5f;
	[Export] public float offset = 0f;
	[Export] public bool isDown = false;
	[Export] public bool halt = false;
	[Export] public bool doesDamage = true;
	
	private StaticBody2D top;
	private Area2D deathZone;
	private Timer cycleTimer;
	private AnimationPlayer animate;
	private AudioStreamPlayer2D boomSound;
	private HashSet<PhysicsBody2D> bodiesHit = new HashSet<PhysicsBody2D>();
	private bool hasPlayedBoom = false;

	public override void _Ready() {
		top = GetNode<StaticBody2D>("Top");
		deathZone = GetNode<Area2D>("DeathZone");
		cycleTimer = GetNode<Timer>("CycleTimer");
		animate = GetNode<AnimationPlayer>("AnimationPlayer");
		boomSound = GetNode<AudioStreamPlayer2D>("BoomSound");

		top.Position = new Vector2(0, 50);
		
		if(!halt) {
			cycleTimer.OneShot = true;
			cycleTimer.Timeout += OnCycleTimeout;
			cycleTimer.Start(upDuration + offset);
		}
	}

	public override void _Process(double delta) {
		if (top.Position.Y > 150 && !isDown && doesDamage) {
			deathZone.Monitoring = true;

			if (!hasPlayedBoom && top.Position.Y >= 370 && top.Position.Y <= 385) {
				hasPlayedBoom = true;
				boomSound.PitchScale = (float)GD.RandRange(0.95, 1.05);
				boomSound.Play();
			}
			var overlaps = deathZone.GetOverlappingBodies();

			foreach (PhysicsBody2D body in overlaps) {
				if (body.IsInGroup("enemies")) {
					if (body is TechTick tick) {
						if (bodiesHit.Add(tick)) {
							tick.Kill();
						}
					} else if (body is MechMosquito mosq) {
						if (bodiesHit.Add(mosq)) {
							mosq.Kill();
						}
					}
				} else if (body is Player player) {
					if (bodiesHit.Add(player)) {
						player.Position = new Vector2(top.GlobalPosition.X, player.Position.Y);
						player.areaHazard(deathZone);
					}
				}
			}
		}
		else {
			deathZone.Monitoring = false;
			bodiesHit.Clear();
			if (hasPlayedBoom && top.Position.Y < 100)
				hasPlayedBoom = false;
		}
	}

	private async void OnCycleTimeout() {
		float nextStateDuration;

		if (isDown) {
			Vector2 midPosition = new Vector2(0, 50);
			Vector2 downPosition = new Vector2(0, 384);
			nextStateDuration = downDuration;
			isDown = !isDown;

			animate.Play("DropPrep");
			await ToSignal(animate, AnimationPlayer.SignalName.AnimationFinished);

			animate.Play("Drop");
			await ToSignal(animate, AnimationPlayer.SignalName.AnimationFinished);

		}
		else {
			Vector2 position = new Vector2(0, 50);
			nextStateDuration = upDuration;
			isDown = !isDown;

			var tween = GetTree().CreateTween();
			tween.TweenProperty(top, "position", position, movementTime)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);

			top.CollisionLayer = 1;
			
			await ToSignal(tween, Tween.SignalName.Finished);
		}

		cycleTimer.Start(nextStateDuration);
	}
}
