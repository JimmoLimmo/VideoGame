using Godot;
using System;

public partial class MechMosquito : CharacterBody2D {
	private AnimationPlayer animator;
	
	[Export] public int maxHealth = 15;
	[Export] public float speed = 300;
	[Export] public float dashRadius = 400;
	[Export] public float dashDistance = 700;
	[Export] public float dashSpeed = 700;
	[Export] public float dashBreak = 1;
	[Export] public int contactDamage = 1;
	
	private int currentHealth;
	private bool isActive = false;
	private bool dashing = false;
	private float dashTracker = 0;
	private float dashTimer;
	private Vector2 lastPos;
	private Vector2 breakPos;
	private Vector2 startPos;
	private Vector2 dashDirection;
	
	private Sprite2D sprite;
	private Area2D hitBox;
	private Area2D aggroArea;
	private CpuParticles2D bloodEmitter;
	private CpuParticles2D sparkEmitter;
	
	public override void _Ready() {
		AddToGroup("enemies");
		
		currentHealth = maxHealth;
		dashTimer = dashBreak;
		lastPos = Position;
		startPos = Position;
		
		hitBox = GetNode<Area2D>("HitBox");
		aggroArea = GetNode<Area2D>("AggroArea");
		bloodEmitter = GetNode<CpuParticles2D>("BloodEmitter");
		sparkEmitter = GetNode<CpuParticles2D>("SparkEmitter");
		
		hitBox.BodyEntered += OnBodyEntered;
		aggroArea.BodyEntered += EnteredAggroArea;
		aggroArea.BodyExited += ExitAggroArea;
	}
	
	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = new Vector2(0, 0);
		
		if(dashTimer < dashBreak) dashTimer += 1 * (float)delta;
		
		GD.Print(dashTimer);
		
		if(isActive && dashTimer >= dashBreak) {
			GD.Print("Dashing: " + dashing + ", Dash Length: " + dashTracker);
			CharacterBody2D player = GetNode<CharacterBody2D>("../Player");
			
			Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
			Vector2 direction = toPlayer.Normalized();
			
			if(toPlayer.Length() > dashRadius && !dashing) {
				velocity = direction * speed * (float)delta * 50;
			} else if(dashing) {
				if(dashTracker > dashDistance || IsOnFloor() || IsOnWall() || IsOnCeiling()) {
					dashing = false;
					Rotation = 0;
					dashTracker = 0;
					dashTimer = 0;
				} else {
					velocity = dashDirection * dashSpeed * (float)delta * 50;
					dashTracker += (Position - lastPos).Length();
				}
			} else {
				dashing = true;
				dashDirection = direction;
				Rotation = direction.Angle();
			}
		} else {
		}
		
		lastPos = Position;
		Velocity = velocity;
		MoveAndSlide();
	}
	
	public void TakeDamage(int amount) {
		currentHealth -= amount;
		bloodEmitter.Restart();
		sparkEmitter.Restart();
		CheckDeath();
	}
	
	private async void CheckDeath() {
		if(currentHealth <= 0) {
			sprite.Visible = false;
			hitBox.Monitoring = false;
			await ToSignal(bloodEmitter, "finished");
			QueueFree();
		}
	}
	
	private void OnBodyEntered(Node2D body) {
		// If the Player body (CharacterBody2D) enters, allow damage
		if(body is Player player) {
			player.ApplyHit(contactDamage, GlobalPosition);
		}
	}
	
	private void EnteredAggroArea(Node2D body) {
		if(body is Player player) isActive = true;
	}
	
	private void ExitAggroArea(Node2D body) {
		if(body is Player player) isActive = false;
	}
}
