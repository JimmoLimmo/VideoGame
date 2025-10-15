using Godot;
using System;

public partial class TechTick : CharacterBody2D {
	private AnimationPlayer _anim;
	private Sprite2D _sprite;
	private Area2D hitBox;

	[Export] public int MaxHealth = 15;
	[Export] public float Speed = 400;
	[Export] public NodePath LeftLimitPath;
	[Export] public NodePath RightLimitPath;
	[Export] public int ContactDamage = 1;
	[Export] public float knockbackVelocity = 1500f;
	[Export] public float knockbackDecceleration = 100f;

	private int _currentHealth;
	private Vector2 _leftLimit;
	private Vector2 _rightLimit;
	private bool _movingRight = true;
	private bool _inHitBox = false;
	private bool isDead = false;

	private CpuParticles2D bloodEmitter;
	private CpuParticles2D sparkEmitter;
	private AudioStreamPlayer2D hitSound;


	public override void _Ready() {
		_currentHealth = MaxHealth;
		var leftLimit = GetNode<Node2D>(LeftLimitPath);
		var rightLimit = GetNode<Node2D>(RightLimitPath);
		AddToGroup("enemies");

		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_leftLimit = leftLimit.GlobalPosition;
		_rightLimit = rightLimit.GlobalPosition;

		hitBox = GetNode<Area2D>("HitBox");
		hitBox.BodyEntered += OnHitBoxBodyEntered;

		bloodEmitter = GetNode<CpuParticles2D>("BloodEmitter");
		sparkEmitter = GetNode<CpuParticles2D>("SparkEmitter");
		hitSound = GetNode<AudioStreamPlayer2D>("Hit");

	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		float goalSpeed = 0f;

		if (GlobalPosition.X >= _rightLimit.X) _movingRight = false;
		else if (GlobalPosition.X < _leftLimit.X) _movingRight = true;

		if (_movingRight) {
			goalSpeed = Speed;
			_sprite.FlipH = false;
		}
		else {
			goalSpeed = -Speed;
			_sprite.FlipH = true;
		}

		_anim.Play("Walk");

		velocity.X = Mathf.MoveToward(velocity.X, goalSpeed, knockbackDecceleration);

		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;
		Velocity = velocity;

		if (!isDead) MoveAndSlide();
	}

	public void TakeDamage(int amount, Vector2 source) {
		_currentHealth -= amount;
		bloodEmitter.Restart();
		sparkEmitter.Restart();

		hitSound.Play();

		ApplyKnockback(source);
		CheckDeath();
	}

	private void ApplyKnockback(Vector2 source) {
		Vector2 velocity = new Vector2(0, 0);

		Vector2 dir = (GlobalPosition - source).Normalized();

		if (dir.X > 0) velocity = new Vector2(knockbackVelocity, 0);
		else velocity = new Vector2(-knockbackVelocity, 0);

		Velocity = velocity;
	}

	private async void CheckDeath() {
		if (_currentHealth <= 0) {
			isDead = true;
			_sprite.Visible = false;
			hitBox.Monitoring = false;
			await ToSignal(bloodEmitter, "finished");
			QueueFree();
		}
	}

	private void OnHitBoxBodyEntered(Node2D body) {
		if (body is Player p)
			p.ApplyHit(ContactDamage, GlobalPosition);
	}
	
	public void Kill() {
		_currentHealth = 0;
		CheckDeath();
	}
}
