using Godot;
using System;

public partial class Boss : Node2D {
	[Export] public Node2D player {get; set;}
	
	[Export] public float speed {get; set;} = 100f;
	[Export] public int health {get; set;} = 30;
	
	[Export] public int idleTime {get; set;} = 3;
	
	[Export] public int dashPrepDistance {get; set;} = 150;
	
	[Export] public int dashSpeed {get; set;} = 350;
	[Export] public int dashLength {get; set;} = 400;
	private Vector2 dashStart;
	private bool playerToRight;
	
	private AnimationPlayer animator;
	
	private enum BossState {
		Idle,
		ChargeDash,
		Dash,
		Swipe
	}
	
	BossState currState = BossState.Idle;
	private Timer idleTimer;
	
	public override void _Ready() {
		animator = GetNode<AnimationPlayer>("AnimationPlayer");
		
		idleTimer = GetNode<Timer>("IdleTimer");
		idleTimer.Timeout += OnIdleTimeout;
		EnterIdle(idleTime);
	}
	
	public override void _PhysicsProcess(double delta) {
		GD.Print(currState);
		switch(currState) {
			case BossState.Idle:
				animator.Play("Idle");
				break;
			case BossState.ChargeDash:
				ChargeDash(delta);
				break;
			case BossState.Dash:
				animator.Play("Attack");
				Dash(delta);
				break;
			case BossState.Swipe:
				break;
		}
	}
	
	private void EnterIdle(float waitTime) {
		currState = BossState.Idle;
		idleTimer.Start(waitTime);
	}
	
	private void OnIdleTimeout() {
		currState = BossState.ChargeDash;
	}
	
	private void ChargeDash(double delta) {
		Vector2 currPos = GlobalPosition;
		Vector2 playerPos = player.GlobalPosition;
		
		Vector2 diffPos = playerPos - currPos;
		
		int yDirection = 1;
		if(diffPos.Y > 0) { //Boss is above
			yDirection = 1;
		} else if(diffPos.Y < 0) { //Boss is below
			yDirection = -1;
		}
		
		int xDirection = 1;
		if(diffPos.X < 0) { //Right side
			xDirection = -1;
			playerToRight = false;
		} else if(diffPos.X > 0) { //Left side
			xDirection = 1;
			playerToRight = true;
		}
		
		if(Math.Abs(diffPos.X) < dashPrepDistance) { //Flips to maintain distance
			xDirection *= -1;
		}
		
		Position += new Vector2(speed * xDirection * (float)delta, speed * yDirection  * (float)delta);
		
		currPos = GlobalPosition;
		playerPos = player.GlobalPosition;
		
		diffPos = playerPos - currPos;
		
		if(Math.Abs(diffPos.Y) < 15) {
			currState = BossState.Dash;
			dashStart = currPos;
		}
	}
	
	private void Dash(double delta) {		
		Vector2 currPos = GlobalPosition;
		Vector2 diffPos = currPos - dashStart;
		
		int xDirection = playerToRight ? 1 : -1;
		
		Position += new Vector2(dashSpeed * xDirection * (float)delta, 0);
		
		if(Math.Abs(diffPos.X) > dashLength) EnterIdle(idleTime);
	}
	
	public void TakeDamage(int damage) {
		health -= damage;
		
		GD.Print("Enemy Hit, HP: " + health);
		
		if(health <= 0) {
			DeathSequence();
		}
	}
	
	public void DeathSequence() {
		QueueFree();
	}
}
