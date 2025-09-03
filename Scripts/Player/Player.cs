using Godot;
using System;

public partial class Player : CharacterBody2D
{
    public const float Speed = 150.0f;
    public const float JumpVelocity = -350.0f;

    private AnimationPlayer _anim;
    private Sprite2D _sprite;

    [Export] public NodePath SwordPath { get; set; }  // assign in Inspector
    private Sword _sword;

    // simple cooldown to prevent spam
    [Export] public float AttackCooldown = 0.25f;
    private float _attackTimer = 0f;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _sword = GetNode<Sword>(SwordPath); // e.g. "Sword"
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity += GetGravity() * (float)delta;

        Vector2 dir = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");

        if (Mathf.Abs(dir.X) > 0.01f)
        {
            velocity.X = dir.X * Speed;
            bool facingLeft = dir.X < 0f;
            _sprite.FlipH = facingLeft;
            _sword?.SetFacingLeft(facingLeft);
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Attack input
        _attackTimer -= (float)delta;
        if (Input.IsActionJustPressed("attack") && _attackTimer <= 0f)
        {
            _attackTimer = AttackCooldown;
            _anim.Play("Sword");          // animation controls hitbox window
        }

        // Movement anims if not attacking
        if (_anim.CurrentAnimation != "Sword" || !_anim.IsPlaying())
        {
            string nextAnim =
                !IsOnFloor() ? (velocity.Y < 0f ? "Jump" : "Fall") :
                Mathf.Abs(velocity.X) > 1f ? "Walk" : "Idle";

            if (_anim.CurrentAnimation != nextAnim)
                _anim.Play(nextAnim);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    // These are called by AnimationPlayer via Call Method tracks:
    public void AttackStart()  => _sword?.EnableHitbox();
    public void AttackEnd()    => _sword?.DisableHitbox();
}
