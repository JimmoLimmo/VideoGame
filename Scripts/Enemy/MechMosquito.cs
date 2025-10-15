using Godot;
using System;

public partial class MechMosquito : CharacterBody2D {
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
	[Export] public float idleRadius = 50f;
	[Export] public float TargetChangeInterval = 0.5f;

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
	private float targetTimer = 0f;
	private Random rng = new();
	private Vector2 idlePos;
	private Vector2 centerPoint = Vector2.Zero;

	private Sprite2D sprite;
	private Area2D hitBox;
	private Area2D aggroArea;
	private CpuParticles2D bloodEmitter;
	private CpuParticles2D sparkEmitter;
	private AnimationPlayer animator;
	private AudioStreamPlayer2D hit;
	CharacterBody2D player;

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
		animator = GetNode<AnimationPlayer>("AnimationPlayer");
		hit = GetNode<AudioStreamPlayer2D>("Hit");
		player = GetNode<CharacterBody2D>("../Player");


		hitBox.BodyEntered += OnBodyEntered;
		aggroArea.BodyEntered += EnteredAggroArea;
		aggroArea.BodyExited += ExitAggroArea;
		
		idlePos = GetRandomTarget();
		centerPoint = GlobalPosition;
		GlobalPosition = centerPoint + GetRandomTarget();
	}

	public override void _PhysicsProcess(double delta) {
		if (!hasKnockback) {
			Vector2 velocity = new Vector2(0, 0);

			if (dashTimer < dashBreak) dashTimer += 1 * (float)delta;

			float t = (float)Time.GetTicksMsec() / 1000f;
			float noiseValue = noise.GetNoise1D(t * noiseSpeed + noiseOffset);
			float angleOffset = Mathf.DegToRad(noiseValue * angleVariationDegrees);

			if (isActive && dashTimer >= dashBreak) {
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
					if(direction.X <= 0) {
						Rotation = direction.Angle() - Mathf.DegToRad(120);
					} else if(direction.X > 0) {
						Rotation = direction.Angle() + Mathf.DegToRad(300);
					}
				}
			}
			else {
				Vector2 targetPos = centerPoint + idlePos;
				Vector2 dir = targetPos - GlobalPosition;
				
				velocity = dir.Normalized() * speed;
			}

			Vector2 randomizedVelocity = velocity.Rotated(angleOffset);

			lastPos = Position;
			Velocity = randomizedVelocity;
		}
		else {
			if (knockbackTimer < knockbackTime) {
				knockbackTimer += 1 * (float)delta;
			}
			else {
				hasKnockback = false;
			}
		}
		
		HandleAnimations();
		
		targetTimer += (float)delta;
		if(targetTimer >= TargetChangeInterval) {
			idlePos = GetRandomTarget();
			targetTimer = 0;
		}

		MoveAndSlide();
	}
	
	private void HandleAnimations() {
		string nextAnimation = "";
		
		if(!dashing) {
			nextAnimation = "Fly";
		} else {
			nextAnimation = "Dash";
		}
		
		GD.Print(Velocity.X);
		
		Vector2 dir = player.GlobalPosition - GlobalPosition;
		
		if(dir.X > 0) {
			sprite.FlipH = true;
		} else if(dir.X < 0) {
			sprite.FlipH = false;
		}
		
		if(animator.CurrentAnimation != nextAnimation) {
			animator.Play(nextAnimation);
		}
	}

	public void TakeDamage(int amount, Vector2 source) {
		currentHealth -= amount;

		bloodEmitter.Restart();
		sparkEmitter.Restart();
		hit?.Play();

		ApplyKnockback(source);
		CheckDeath();
	}

	private void ApplyKnockback(Vector2 source) {
		hasKnockback = true;
		knockbackTimer = 0f;

		Vector2 velocity = new Vector2(0, 0);

		Vector2 dir = (GlobalPosition - source).Normalized();

		if (dir.X > 0) velocity = new Vector2(knockbackVelocity, 0);
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
	
	public void Kill() {
		currentHealth = 0;
		CheckDeath();
	}
	
	private Vector2 GetRandomTarget() {
		float angle = (float)(rng.NextDouble() * MathF.Tau);
		float dist = (float)(rng.NextDouble()) * idleRadius;
		return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
	}
}
