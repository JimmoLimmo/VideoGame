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
	private bool _hasAirDashed = false; // Tracks if the player has dashed in the air
	private bool _isWallSliding = false; // Tracks if the player is sliding on a wall
	private float _dashTimer = 0f; // Tracks remaining dash time
	private float _dashCooldownTimer = 0f; // Tracks cooldown time
	[Export] public float WallJumpLockTime = 0.1f; // lock duration in seconds
	private float _wallJumpLockTimer = 0f;


	// Wall Slide Settings
	[Export] public float WallSlideSpeed = 100.0f; // Speed of sliding down a wall
	[Export] public float WallJumpForce = 600.0f; // Force applied when jumping off a wall
	private float CurrentWallSlideSpeed = 980.0f; //
	private bool hasWalljump = false;

	// Dash Settings
	[Export] public float DashSpeed = 400.0f; // Speed during dash
	[Export] public float DashDuration = 0.2f; // Duration of the dash in seconds
	[Export] public float DashCooldown = 0.5f; // Cooldown time between dashes
	private bool hasDash = false;

	// Attack Variables
	[Export] public float AttackCooldown = 0.25f;
	private float _attackTimer = 0f;
	private bool hasSword = false;

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

	// Damage Handling Crap
	[Export] public float InvulnTime = 0.6f;     // seconds of invulnerability
	[Export] public float HitstunTime = 0.15f;   // time you can't control the player
	[Export] public float KnockbackForce = 260f; // pixels/sec knockback speed
	[Export] public float KnockbackUpward = 120f;// upward component
	private bool _invulnerable = false;
	private float _invulnTimer = 0f;
	private float _hitstunTimer = 0f;

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

		var hazardBox = GetNode<Area2D>("player");
		hazardBox.BodyEntered += areaHazard;

		AddToGroup("player");
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

		_wallJumpLockTimer = Mathf.Max(0f, _wallJumpLockTimer - (float)delta);

		HandleDashCooldown(delta);

		if (_isDashing)
		{
			HandleDash(delta);
		}
		else
		{
			HandleMovement(delta);
		}

		if (_isWallSliding && !_isDashing)
			HandleWallSlide(delta);

		HandleJump();         // Allow jumping regardless of dash
		HandleDashInput();    // Still process new dash input
		HandleAttack(delta);  // Allow attacking mid-air or mid-dash
		HandleAnimations();   // Update animation

		// decrement timers
		if (_invulnerable)
		{
			_invulnTimer -= (float)delta;
			if (_invulnTimer <= 0f)
			{
				_invulnerable = false;
				_sprite.SelfModulate = Colors.White; // stop flicker
			}
			else
			{
				// simple flicker (toggle alpha)
				// 10 Hz blink:
				bool on = ((int)(Time.GetTicksMsec() / 100) % 2) == 0;
				_sprite.SelfModulate = new Color(1, 1, 1, on ? 0.5f : 1f);
			}
		}

		if (_hitstunTimer > 0f)
		{
			_hitstunTimer -= (float)delta;

			// During hitstun: no input, just apply gravity and keep current Velocity
			Vector2 v = Velocity;
			if (!IsOnFloor()) v += GetGravity() * (float)delta;
			Velocity = v;
			MoveAndSlide();
			return; // skip normal controls this frame
		}

	}

	// Movement Logic
	private void HandleMovement(double delta)
	{
		// Respect wall-jump input lock
		float inputX = (_wallJumpLockTimer > 0f)
			? 0f
			: Input.GetAxis("move_left", "move_right");

		Vector2 velocity = Velocity;

		if (!IsOnFloor() && !_isWallSliding)
			velocity += GetGravity() * (float)delta;

		if (Mathf.Abs(inputX) > 0.01f)
		{
			velocity.X = inputX * Speed;
			bool facingLeft = inputX < 0f;
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


	// Jump Logic
	private void HandleJump()
	{
		if (Input.IsActionJustPressed("jump"))
		{
			if (IsOnFloor())
			{
				// Normal jump
				Velocity = new Vector2(Velocity.X, JumpVelocity);
			}
			else if (_isWallSliding && hasWalljump)
			{
				// Jump away from the wall with a strong horizontal push
				int dir = _sprite.FlipH ? 1 : -1; // facing left => wall on left, push right
				Velocity = new Vector2(dir * WallJumpForce, JumpVelocity);

				// Start input lock so holding into wall doesn’t cancel this
				_isWallSliding = false;
				_wallJumpLockTimer = WallJumpLockTime;
			}
			else if (_isDashing)
			{
				// Preserve momentum when jumping during a dash
				Velocity = new Vector2(_dashDirection.X * DashSpeed, JumpVelocity);
				_isDashing = false; // End the dash
				_dashTimer = 0f; // Reset the dash timer
			}
		}
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
		if (_dashTimer <= 0f || !Input.IsActionPressed("dash"))
		{
			_isDashing = false; // End the dash
		}
		else
		{
			// Only apply dash velocity if the player is still dashing
			Velocity = _dashDirection * DashSpeed;
			MoveAndSlide();
		}
	}

	private void HandleDashInput()
	{
		if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0f && !_isDashing && hasDash)
		{
			if (IsOnWall() && !IsOnFloor())
			{
				// Dash up the wall at a controlled speed
				Vector2 upIntoWall = new Vector2((_sprite.FlipH ? -1 : 1) * 0.2f, -1f).Normalized();
				StartDash(upIntoWall);

			}
			else if (IsOnFloor() || !_hasAirDashed)
			{
				StartDash(Input.GetVector("move_left", "move_right", "ui_up", "ui_down"));
				if (!IsOnFloor())
					_hasAirDashed = true; // Mark air dash as used
			}
		}
	}

	private void StartDash(Vector2 direction)
	{
		if (direction == Vector2.Zero)
			direction = _sprite.FlipH ? Vector2.Left : Vector2.Right; // Default to facing direction

		_isDashing = true;
		_dashTimer = DashDuration;
		_dashCooldownTimer = DashCooldown;

		// Scale the dash speed for wall dashing
		_dashDirection = direction.Normalized();
		Velocity = _dashDirection * DashSpeed * (IsOnWall() ? 0.7f : 1f); // Reduce speed for wall dashing

		// _anim.Play("Dash"); // Play dash animation
	}

	// Wall Slide Logic
	private void HandleWallSlide(double delta)
	{
		if (IsOnWall() && !IsOnFloor())
		{
			_isWallSliding = true;

			// Stick to the wall and slide down slowly
			Velocity = new Vector2(0, Mathf.Min(Velocity.Y + GetGravity().Y * (float)delta, CurrentWallSlideSpeed));
		}
		else
		{
			_isWallSliding = false;
		}
	}

	// Attack Logic
	private void HandleAttack(double delta)
	{
		_attackTimer -= (float)delta;
		if (Input.IsActionJustPressed("attack") && _attackTimer <= 0f && hasSword)
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

	// Reset States on Ground or Wall Contact
	public override void _Process(double delta)
	{
		if (IsOnFloor())
		{
			_hasAirDashed = false; // Reset air dash when touching the ground
		}

		if (IsOnWall() && !IsOnFloor() && _wallJumpLockTimer <= 0f && !_isDashing)
		{
			_isWallSliding = true;  // Enable wall sliding when touching a wall
			_hasAirDashed = false;  // Reset air dash when touching a wall
		}
		else if (!IsOnWall() || IsOnFloor())
		{
			_isWallSliding = false;
		}
	}

	public void areaHazard(Node2D body)
	{
		TakeDamage(1);
	}

	public void OnCollect(CollectableType type)
	{
		if (type == CollectableType.Sword)
		{
			hasSword = true;
		}
		else if (type == CollectableType.Dash)
		{
			hasDash = true;
		}
		else if (type == CollectableType.Walljump)
		{
			CurrentWallSlideSpeed = WallSlideSpeed;
			hasWalljump = true;
		}
	}

	public void ApplyHit(int dmg, Vector2 sourceGlobalPos)
	{
		if (_isDead || _invulnerable) return;

		// 1) take damage
		TakeDamage(dmg);

		// 2) start i-frames + hitstun
		_invulnerable = true;
		_invulnTimer = InvulnTime;
		_hitstunTimer = HitstunTime;

		// 3) compute knockback (away from source, with a bit of upward kick)
		Vector2 dir = (GlobalPosition - sourceGlobalPos).Normalized();
		Vector2 kb = new Vector2(dir.X, 0).Normalized() * KnockbackForce;
		kb.Y = -Mathf.Abs(KnockbackUpward); // negative = up
		Velocity = kb;

		// optional: immediately slide once so it feels snappy
		MoveAndSlide();
	}


	// --- Save / Load helpers -------------------------------------------------

	public SaveManager.SaveData ToSaveData()
	{
		return new SaveManager.SaveData
		{
			Hp = _hp,
			HasSword = hasSword,
			HasDash = hasDash,
			HasWalljump = hasWalljump,
			PlayerPosition = GlobalPosition
		};
	}

	public void ApplySaveData(SaveManager.SaveData data, bool setPosition = true)
	{
		if (data == null) return;

		_hp = data.Hp;
		hasSword = data.HasSword;
		hasDash = data.HasDash;
		hasWalljump = data.HasWalljump;

		// Update relevant nodes/UI
		_hud?.SetHealth(_hp);
		if (_sword != null) {
			_sword.Show();
			_sword.Visible = hasSword;
		}

		if (setPosition)
		{
			// Teleport player to saved position
			GlobalPosition = data.PlayerPosition;
		}
	}

	public void SaveToFile()
	{
		SaveManager.Save(ToSaveData());
	}

	public void LoadFromFile()
	{
		var data = SaveManager.Load();
		if (data != null)
			ApplySaveData(data);
	}


}
