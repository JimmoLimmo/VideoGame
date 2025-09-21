using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	private AnimationPlayer _anim;
	private Sprite2D _sprite;

	[Export] public int MaxHealth = 3;
	[Export] public float Speed = 100f;
	[Export] public NodePath LeftLimitPath;
	[Export] public NodePath RightLimitPath;

	private int _currentHealth;
	private Vector2 _leftLimit;
	private Vector2 _rightLimit;
	private bool _movingRight = true;


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
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void TakeDamage(int amount)
	{
		_currentHealth -= amount;
		if (_currentHealth <= 0)
			Die();
	}

	private void Die()
	{
		QueueFree(); // Remove enemy from scene
	}
	

}
