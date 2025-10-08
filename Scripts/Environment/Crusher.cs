using Godot;
using System;
using System.Collections.Generic;

public partial class Crusher : Node {
	[Export] public float upDuration = 1.0f;
	[Export] public float downDuration = 1.0f;
	[Export] public float movementTime = 0.25f;
	
	private StaticBody2D top;
	private Area2D deathZone;
	private Timer cycleTimer;
	
	private bool isDown = false;
	
	private HashSet<PhysicsBody2D> bodiesHit = new HashSet<PhysicsBody2D>();
	
	public override void _Ready() {
		top = GetNode<StaticBody2D>("Top");
		deathZone = GetNode<Area2D>("DeathZone");
		cycleTimer = GetNode<Timer>("CycleTimer");
		
		cycleTimer.OneShot = true;
		cycleTimer.Timeout += OnCycleTimeout;
		cycleTimer.Start(upDuration);
	}
	
	public override void _Process(double delta) {
		if(top.Position.Y > 100 && !isDown) {
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
	
	private void OnCycleTimeout() {
		Vector2 position;
		float nextStateDuration;
		
		if(isDown) {
			position = new Vector2(0, 384);
			nextStateDuration = upDuration;
		} else {
			position = new Vector2(0, 0);
			nextStateDuration = downDuration;
		}
		
		var tween = GetTree().CreateTween();
		tween.TweenProperty(top, "position", position, movementTime)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		
		cycleTimer.Start(nextStateDuration);
		isDown = !isDown;
	}
}
