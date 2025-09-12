using Godot;
using System;

public partial class Player : CharacterBody2D
{
    // Constants
    public const float Speed = 150.0f;
    public const float JumpVelocity = -350.0f;

    // Movement Variables
    private Vector2 _dashDirection = Vector2.Zero; // Direction of the dash
    private bool _isDashing = false; // Whether the player is currently dashing
    private float _dashTimer = 0f; // Tracks remaining dash time
    private float _dashCooldownTimer = 0f; // Tracks cooldown time

    // Dash Settings
    [Export] public float DashSpeed = 400.0f; // Speed during dash
    [Export] public float DashDuration = 0.2f; // Duration of the dash in seconds
    [Export] public float DashCooldown = 0.5f; // Cooldown time between dashes

    // Attack Variables
    [Export] public float AttackCooldown = 0.25f;
    private float _attackTimer = 0f;

    // Health Variables
    private int _hp = 5;
    private bool _isDead = false;

    // Node Paths
    [Export] public NodePath SwordPath { get; set; } // Assign in Inspector
    [Export] public NodePath HudPath { get; set; } // Assign in Inspector

    // Node References
    private AnimationPlayer _anim;
    private Sprite2D _sprite;
    private Sword _sword;
    private HUD _hud;

    // Initialization
    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");

        _sword = GetNodeOrNull<Sword>(SwordPath);
        if (_sword == null)
            GD.PushError($"Sword not found at '{SwordPath}' from {GetPath()}.");

        _hud = GetNodeOrNull<HUD>(HudPath);
        if (_hud == null)
            GD.PushError($"HUD not found at '{HudPath}' from {GetPath()}.");

        // Defer HUD sync so HUD._Ready() has time to build its UI
        CallDeferred(nameof(SyncHud));
    }

    private void SyncHud()
    {
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

    // Physics Process
    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return; // Disable controls if dead

        HandleDashCooldown(delta);
        if (_isDashing)
        {
            HandleDash(delta);
            return; // Skip normal movement while dashing
        }

        HandleMovement(delta);
        HandleJump();
        HandleDashInput();
        HandleAttack(delta);
        HandleAnimations();
    }

    // Movement Logic
    private void HandleMovement(double delta)
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

        Velocity = velocity;
        MoveAndSlide();
    }

    private void HandleJump()
    {
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            Velocity = new Vector2(Velocity.X, JumpVelocity);
    }

    // Dash Logic
    private void HandleDashCooldown(double delta)
    {
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= (float)delta;
    }

    private void HandleDash(double delta)
    {
        _dashTimer -= (float)delta;
        if (_dashTimer <= 0f)
        {
            _isDashing = false; // End the dash
        }
        else
        {
            Velocity = _dashDirection * DashSpeed; // Maintain dash velocity
            MoveAndSlide();
        }
    }

    private void HandleDashInput()
    {
        if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0f && !_isDashing)
        {
            StartDash(Input.GetVector("move_left", "move_right", "ui_up", "ui_down"));
        }
    }

    private void StartDash(Vector2 direction)
    {
        if (direction == Vector2.Zero)
            direction = _sprite.FlipH ? Vector2.Left : Vector2.Right; // Default to facing direction

        _isDashing = true;
        _dashTimer = DashDuration;
        _dashCooldownTimer = DashCooldown;
        _dashDirection = direction.Normalized();

        _anim.Play("Dash"); // Optional: Play a dash animation
    }

    // Attack Logic
    private void HandleAttack(double delta)
    {
        _attackTimer -= (float)delta;
        if (Input.IsActionJustPressed("attack") && _attackTimer <= 0f)
        {
            _attackTimer = AttackCooldown;
            _anim.Play("Sword"); // Animation calls AttackStart/AttackEnd
        }
    }

    public void AttackStart() => _sword?.EnableHitbox();
    public void AttackEnd() => _sword?.DisableHitbox();

    // Animation Logic
    private void HandleAnimations()
    {
        if (_anim.CurrentAnimation != "Sword" || !_anim.IsPlaying())
        {
            string nextAnim =
                !IsOnFloor() ? (Velocity.Y < 0f ? "Jump" : "Fall") :
                Mathf.Abs(Velocity.X) > 1f ? "Walk" : "Idle";

            if (_anim.CurrentAnimation != nextAnim)
                _anim.Play(nextAnim);
        }
    }

    // Health Logic
    public void TakeDamage(int dmg)
    {
        if (_isDead) return; // Ignore damage if already dead

        _hp = Mathf.Max(0, _hp - dmg);
        _hud?.SetHealth(_hp);
        _hud?.FlashDamage();

        if (_hp <= 0)
            Die();
    }

    public void Heal(int amt)
    {
        int max = _hud?.MaxMasks ?? _hp;
        _hp = Mathf.Min(_hp + amt, max);
        _hud?.SetHealth(_hp);
    }

    public void Die()
    {
        if (_isDead) return; // Prevent multiple death triggers
        _isDead = true;

        // Play the "Dead" animation
        _anim.Play("Dead");

        // Disable player controls
        SetPhysicsProcess(false);

        // Optionally, trigger a game-over sequence after the animation finishes
        _anim.Connect("animation_finished", new Callable(this, nameof(OnDeathAnimationFinished)));
    }

    private void OnDeathAnimationFinished(string animName)
    {
        if (animName == "Dead")
        {
            // Trigger game-over logic (e.g., restart level, show game-over screen)
            GD.Print("Game Over!");
            GetTree().Paused = true; // Pause the game
        }
    }
}
