using Godot;
using System;

public partial class Player : CharacterBody2D
{
    public const float Speed = 150.0f;
    public const float JumpVelocity = -350.0f;

    private AnimationPlayer _anim;
    private Sprite2D _sprite;

    [Export] public NodePath SwordPath { get; set; }  // assign in Inspector
    [Export] public NodePath HudPath   { get; set; }  // assign in Inspector

    private Sword _sword;
    private HUD _hud;

    [Export] public float AttackCooldown = 0.25f;
    private float _attackTimer = 0f;

    private int _hp = 5;

    public override void _Ready()
    {
        _anim  = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");

        _sword = GetNodeOrNull<Sword>(SwordPath);
        if (_sword == null)
            GD.PushError($"Sword not found at '{SwordPath}' from {GetPath()}.");

        _hud = GetNodeOrNull<HUD>(HudPath);
        if (_hud == null)
            GD.PushError($"HUD not found at '{HudPath}' from {GetPath()}.");

        // 🔧 Defer HUD sync so HUD._Ready() has time to build its UI
        CallDeferred(nameof(SyncHud));
    }

    private void SyncHud()
    {
        // Try to resolve again in case the node was instanced after Player._Ready()
        _hud ??= GetNodeOrNull<HUD>(HudPath);
        if (_hud == null)
        {
            GD.PushError($"[Player] Could not sync HUD; node still null at '{HudPath}'.");
            return;
        }

        // Clamp to HUD capacity and apply initial value
        _hp = Mathf.Min(_hp, _hud.MaxMasks);
        _hud.SetHealth(_hp);
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
            _anim.Play("Sword"); // animation calls AttackStart/AttackEnd
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

    // Called by AnimationPlayer
    public void AttackStart() => _sword?.EnableHitbox();
    public void AttackEnd()   => _sword?.DisableHitbox();

    public void TakeDamage(int dmg)
    {
        _hp = Mathf.Max(0, _hp - dmg);
        _hud?.SetHealth(_hp);
        _hud?.FlashDamage();
    }

    public void Heal(int amt)
    {
        int max = _hud?.MaxMasks ?? _hp;
        _hp = Mathf.Min(_hp + amt, max);
        _hud?.SetHealth(_hp);
    }
}
