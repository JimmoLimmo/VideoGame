using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] public int MaxHp = 3;
	[Export] public int Damage = 1;      // damage to player on contact
	[Export] public float Speed = 50f;   // patrol speed

	private int _hp;
	private Vector2 _direction = Vector2.Left;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_hp = MaxHp;

		// Get the AnimatedSprite2D
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Setup DamageArea to hurt the player
		var damageArea = GetNode<Area2D>("DamageArea");
		if (damageArea != null)
			damageArea.BodyEntered += OnDamageAreaEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Patrol movement
		Vector2 velocity = _direction * Speed;
		Velocity = velocity;
		MoveAndSlide();

		// Flip sprite based on direction
		_sprite.FlipH = _direction.X < 0;

		// Play animations
		if (velocity.Length() > 0)
		{
			if (_sprite.Animation != "Walk")
				_sprite.Play("Walk");
		}
		else
		{
			if (_sprite.Animation != "Idle")
				_sprite.Play("Idle");
		}

		// Reverse direction on wall collision
		if (IsOnWall())
			_direction = -_direction;
	}

	private void OnDamageAreaEntered(Node body)
	{
		if (body is Player player)
		{
			player.TakeDamage(Damage);
		}
	}

	public void TakeDamage(int dmg)
	{
		_hp -= dmg;
		GD.Print($"Enemy took {dmg} damage, HP: {_hp}");

		if (_hp <= 0)
			QueueFree(); // enemy dies
	}
}
