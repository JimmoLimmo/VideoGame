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

	private int _currentHealth;
	private Vector2 _leftLimit;
	private Vector2 _rightLimit;
	private bool _movingRight = true;
	private bool _inHitBox = false;

	// --- NEW stun fields ---
	private bool _isStunned = false;
	private float _stunTimer = 0f;
	// -----------------------

	private CpuParticles2D bloodEmitter;
	private CpuParticles2D sparkEmitter;

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
	}

	public override void _PhysicsProcess(double delta) {
		// ---- STUN HANDLING ----
		if (_isStunned) {
			_stunTimer -= (float)delta;
			if (_stunTimer <= 0f)
				_isStunned = false;

			// Slide with reduced momentum while stunned
			Velocity = new Vector2(Velocity.X * 0.9f, Velocity.Y);
			MoveAndSlide();
			return;
		}
		// -----------------------

		Vector2 velocity = Velocity;

		if (_movingRight) {
			velocity.X = Speed;
			if (GlobalPosition.X >= _rightLimit.X)
				_movingRight = false;

			_sprite.FlipH = false;
			_anim.Play("Walk");
		}
		else {
			velocity.X = -Speed;
			if (GlobalPosition.X <= _leftLimit.X)
				_movingRight = true;

			_sprite.FlipH = true;
			_anim.Play("Walk");
		}

		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;
		Velocity = velocity;
		MoveAndSlide();
	}

	// --- Optional public accessors for helper reflection ---
	public void SetStun(float time) {
		_isStunned = true;
		_stunTimer = time;
	}
	// --------------------------------------------------------

	public void TakeDamage(int amount) {
		_currentHealth -= amount;
		bloodEmitter.Restart();
		sparkEmitter.Restart();
		CheckDeath();
	}

	private async void CheckDeath() {
		if (_currentHealth <= 0) {
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
}
