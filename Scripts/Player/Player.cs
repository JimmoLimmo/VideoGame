using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 150.0f;
	public const float JumpVelocity = -350.0f;

	private AnimationPlayer _anim;
	private Sprite2D _sprite;        // for FlipH

	public override void _Ready()
	{
		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Gravity
		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;

		// Horizontal input
		Vector2 dir = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");

		if (Mathf.Abs(dir.X) > 0.01f)
		{
			velocity.X = dir.X * Speed;
			_sprite.FlipH = dir.X < 0f; // face movement
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}

		// Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Decide animation
		string nextAnim =
			!IsOnFloor() ? (velocity.Y < 0f ? "Jump" : "Fall") :
			Mathf.Abs(velocity.X) > 1f ? "Walk" : "Idle";

		if (_anim.CurrentAnimation != nextAnim)
			_anim.Play(nextAnim);

		Velocity = velocity;
		MoveAndSlide();
	}
}
