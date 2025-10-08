using Godot;
using System;
using System.Collections.Generic;

public partial class Crusher : Node {
	[Export] public float upDuration = 1.0f;
	[Export] public float downDuration = 1.0f;
	[Export] public float movementTime = 0.5f;
	
	private StaticBody2D top;
	private Area2D deathZone;
	private Timer cycleTimer;
	private AnimationPlayer animate;
	
	private bool isDown = false;
	
	private HashSet<PhysicsBody2D> bodiesHit = new HashSet<PhysicsBody2D>();
	
	public override void _Ready() {
		top = GetNode<StaticBody2D>("Top");
		deathZone = GetNode<Area2D>("DeathZone");
		cycleTimer = GetNode<Timer>("CycleTimer");
		animate = GetNode<AnimationPlayer>("AnimationPlayer");
		
		top.Position = new Vector2(0, 50);
		
		cycleTimer.OneShot = true;
		cycleTimer.Timeout += OnCycleTimeout;
		cycleTimer.Start(upDuration);
	}
	
	public override void _Process(double delta) {
		if(top.Position.Y > 200 && !isDown) {
			deathZone.Monitoring = true;
			
			var overlaps = deathZone.GetOverlappingBodies();
			
			foreach(PhysicsBody2D body in overlaps) {
				if(body is Player player) {
					if(bodiesHit.Add(player)) {
						player.areaHazard(deathZone);
					}
				}
			}
		} else {
			deathZone.Monitoring = false;
			bodiesHit.Clear();
		}
	}
	
	private async void OnCycleTimeout() {
		float nextStateDuration;
		
		if(isDown) {
			Vector2 midPosition = new Vector2(0, 50);
			Vector2 downPosition = new Vector2(0, 384);
			nextStateDuration = upDuration;
			isDown = !isDown;
			
			animate.Play("DropPrep");
			await ToSignal(animate, AnimationPlayer.SignalName.AnimationFinished);
			
			animate.Play("Drop");
			await ToSignal(animate, AnimationPlayer.SignalName.AnimationFinished);
		} else {
			Vector2 position = new Vector2(0, 50);
			nextStateDuration = downDuration;
			isDown = !isDown;
			
			var tween = GetTree().CreateTween();
			tween.TweenProperty(top, "position", position, movementTime)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);
		}
		
		cycleTimer.Start(nextStateDuration);
	}
}
