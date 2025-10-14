using Godot;
using System;

public partial class MechMosquito : CharacterBody2D {
	private AnimationPlayer animator;

	[Export] public int maxHealth = 30;
	[Export] public float speed = 300;
	[Export] public float angleVariationDegrees = 15f;
	[Export] public float noiseSpeed = 200f;
	[Export] public float dashRadius = 400;
	[Export] public float dashDistance = 700;
	[Export] public float dashSpeed = 700;
	[Export] public float dashBreak = 1;
	[Export] public int contactDamage = 1;
	[Export] public float knockbackVelocity = 1500f;
	[Export] public float knockbackTime = 0.2f;

	private int currentHealth;
	private bool isActive = false;
	private bool dashing = false;
	private float dashTracker = 0;
	private float dashTimer;
	private float noiseOffset;
	private Vector2 lastPos;
	private Vector2 breakPos;
	private Vector2 startPos;
	private Vector2 dashDirection;
	private FastNoiseLite noise = new FastNoiseLite();
	private bool hasKnockback = false;
	private float knockbackTimer = 0f;

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
		noise.Seed = (int)GD.Randi();
		noiseOffset = GD.Randf() * 1000f;

		sprite = GetNode<Sprite2D>("Sprite2D");
		hitBox = GetNode<Area2D>("HitBox");
		aggroArea = GetNode<Area2D>("AggroArea");
		bloodEmitter = GetNode<CpuParticles2D>("BloodEmitter");
		sparkEmitter = GetNode<CpuParticles2D>("SparkEmitter");

		hitBox.BodyEntered += OnBodyEntered;
		aggroArea.BodyEntered += EnteredAggroArea;
		aggroArea.BodyExited += ExitAggroArea;
	}

	public override void _PhysicsProcess(double delta) {
		if(!hasKnockback) {
			Vector2 velocity = new Vector2(0, 0);

			if (dashTimer < dashBreak) dashTimer += 1 * (float)delta;

			float t = (float)Time.GetTicksMsec() / 1000f;
			float noiseValue = noise.GetNoise1D(t * noiseSpeed + noiseOffset);
			float angleOffset = Mathf.DegToRad(noiseValue * angleVariationDegrees);

			if (isActive && dashTimer >= dashBreak) {
				CharacterBody2D player = GetNode<CharacterBody2D>("../Player");

				Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
				Vector2 direction = toPlayer.Normalized();

				if (toPlayer.Length() > dashRadius && !dashing) {
					velocity = direction * speed * (float)delta * 50;
				}
				else if (dashing) {
					if (dashTracker > dashDistance || IsOnFloor() || IsOnWall() || IsOnCeiling()) {
						dashing = false;
						Rotation = 0;
						dashTracker = 0;
						dashTimer = 0;
					}
					else {
						velocity = dashDirection * dashSpeed * (float)delta * 50;
						
						dashTracker += (Position - lastPos).Length();
					}
				}
				else {
					dashing = true;
					dashDirection = direction;
					Rotation = direction.Angle();
				}
			}
			else {
				Vector2 movePos = startPos;
				Vector2 toPos = movePos - GlobalPosition;
				Vector2 direction = toPos.Normalized();
				
				velocity = direction * speed * (float)delta * 50;
			}

			Vector2 randomizedVelocity = velocity.Rotated(angleOffset);
			
			lastPos = Position;
			Velocity = randomizedVelocity;
		} else {
			if(knockbackTimer < knockbackTime) {
				knockbackTimer += 1 * (float)delta;
			} else {
				hasKnockback = false;
			}
		}
		
		MoveAndSlide();
	}

	public void TakeDamage(int amount, Vector2 source) {
		currentHealth -= amount;
		
		bloodEmitter.Restart();
		sparkEmitter.Restart();
		
		ApplyKnockback(source);
		CheckDeath();
	}
	
	private void ApplyKnockback(Vector2 source) {
		hasKnockback = true;
		knockbackTimer = 0f;
		
		Vector2 velocity = new Vector2(0, 0);
		
		Vector2 dir = (GlobalPosition - source).Normalized();
		
		if(dir.X > 0) velocity = new Vector2(knockbackVelocity, 0);
		else velocity = new Vector2(-knockbackVelocity, 0);
		
		Velocity = velocity;
	}

	private async void CheckDeath() {
		if (currentHealth <= 0) {
			sprite.Visible = false;
			hitBox.Monitoring = false;
			await ToSignal(bloodEmitter, "finished");
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body) {
		if (body is Player player)
			player.ApplyHit(contactDamage, GlobalPosition);
	}

	private void EnteredAggroArea(Node2D body) {
		if (body is Player player) isActive = true;
	}

	private void ExitAggroArea(Node2D body) {
		if (body is Player player) isActive = false;
	}
}
