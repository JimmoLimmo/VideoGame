using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	private AnimationPlayer _anim;
	private Sprite2D _sprite;

	[Export] public int MaxHealth = 15;
	[Export] public float Speed = 500f;
	[Export] public NodePath LeftLimitPath;
	[Export] public NodePath RightLimitPath;
	[Export] public int ContactDamage = 1;

	private int _currentHealth;
	private Vector2 _leftLimit;
	private Vector2 _rightLimit;
	private bool _movingRight = true;
	private bool _inHitBox = false;
	
	private CpuParticles2D bloodEmitter;
	private CpuParticles2D sparkEmitter;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
		var leftLimit = GetNode<Node2D>(LeftLimitPath);
		var rightLimit = GetNode<Node2D>(RightLimitPath);
		AddToGroup("enemies");

		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_leftLimit = leftLimit.GlobalPosition;
		_rightLimit = rightLimit.GlobalPosition;

		// Connect Area2D hitbox signal 
		var hitBox = GetNode<Area2D>("HitBox");
		hitBox.BodyEntered += OnHitBoxBodyEntered;   // Physics bodies
		hitBox.AreaEntered += OnHitBoxAreaEntered;   // Areas
		
		bloodEmitter = GetNode<CpuParticles2D>("BloodEmitter");
		sparkEmitter = GetNode<CpuParticles2D>("SparkEmitter");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (_movingRight)
		{
			velocity.X = Speed;
			if (GlobalPosition.X >= _rightLimit.X)
				_movingRight = false;

			_sprite.FlipH = false;
			_anim.Play("Walk");
		}
		else
		{
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

	public void TakeDamage(int amount)
	{
		_currentHealth -= amount;
		GD.Print($"Health: {_currentHealth}");
		bloodEmitter.Restart();
		sparkEmitter.Restart();
		CheckDeath();
	}

	private async void CheckDeath() {
		if (_currentHealth <= 0) {
			_sprite.Visible = false;
			await ToSignal(bloodEmitter, "finished");
			QueueFree(); // Remove enemy from scene
		}
	}

	// Called when the player enters the enemy's hitbox
	// 	private void OnHitBoxEntered(Node body)
	// 	{
	// 		GD.Print($"Hitbox entered by: {body.Name}");
	// 		// if (body.IsInGroup("player") && body is Player player)
	// 		if (body is Player player)
	// 		{
	// 			GD.Print("Player detected! Applying damage.");
	// 			player.TakeDamage(ContactDamage);
	// 		}
	// 	}
	// }
	private void OnHitBoxBodyEntered(Node2D body)
	{
		// If the Player body (CharacterBody2D) enters, allow damage
		if (body is Player p)
			p.ApplyHit(ContactDamage, GlobalPosition);
	}

	private void OnHitBoxAreaEntered(Area2D area)
	{
		// Ignore swords and other areas; only hurt the player’s HURTBOX
		if (!area.IsInGroup("player_hurtbox")) return;

		if (area.GetParent() is Player p)
			p.ApplyHit(ContactDamage, GlobalPosition);
	}


}
