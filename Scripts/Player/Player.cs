using Godot;
using System;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D {
	// Constants
	public const float Speed = 700.0f;
	public const float JumpVelocity = -1250.0f;

	// Movement Variables
	private Vector2 _dashDirection = Vector2.Zero;
	private bool _isDashing = false;
	private bool _hasAirDashed = false;
	private bool _isWallSliding = false;
	private float _dashTimer = 0f;
	private float _dashCooldownTimer = 0f;
	[Export] public float WallJumpLockTime = 0.1f;
	private float _wallJumpLockTimer = 0f;
	private bool holdPlayer = false;

	// Wall Slide Settings
	[Export] public float WallSlideSpeed = 100.0f;
	[Export] public float WallJumpForce = 600.0f;
	private float CurrentWallSlideSpeed = 980.0f;
	[Export] private bool hasWalljump = false;

	// Dash Settings
	[Export] public float DashSpeed = 1200.0f;
	[Export] public float DashDuration = 0.2f;
	[Export] public float DashCooldown = 0.5f;
	[Export] private bool hasDash = false;

	// Attack Variables
	[Export] public float AttackCooldown = 0.25f;
	private float _attackTimer = 0f;
	[Export] private bool hasSword = false;

	// Health Variables
	private int _hp = 5;
	private bool _isDead = false;

	private int _mana = 3;

	// Node Paths
	[Export] public NodePath SwordPath { get; set; }
	
	//Animation Variables
	string nextAnimation = "";
	string lastAnimation;

	// Node References
	private AnimationPlayer _anim;
	private Sprite2D _sprite;
	private Sword _sword;
	private HUD _hud;
	private ScreenFader fade;

	// Damage Handling
	[Export] public float InvulnTime = 0.6f;
	[Export] public float HitstunTime = 0.15f;
	[Export] public float KnockbackForce = 260f;
	[Export] public float KnockbackUpward = 120f;
	private bool _invulnerable = false;
	private float _invulnTimer = 0f;
	private float _hitstunTimer = 0f;
	private Vector2 respawnPoint;
	private bool lockPlayer = false;

	// Initialization
	public override void _Ready() {
		if (GlobalRoomChange.Activate) {
			GlobalPosition = GlobalRoomChange.PlayerPos;
			if (GlobalRoomChange.PlayerJumpOnEnter) Velocity = new Vector2(0, JumpVelocity);
			hasSword = GlobalRoomChange.hasSword;
			hasDash = GlobalRoomChange.hasDash;
			hasWalljump = GlobalRoomChange.hasWalljump;
			GlobalRoomChange.Activate = false;
		}

		respawnPoint = Position;

		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");

		_sword = GetNodeOrNull<Sword>(SwordPath);
		if (_sword == null)
			GD.PushError($"Sword not found at '{SwordPath}' from {GetPath()}.");

		_hud = GetNode<HUD>("/root/HUD");

		fade = GetNode<ScreenFader>("../ScreenFade");

		// Defer HUD sync so HUD._Ready() builds first
		CallDeferred(nameof(SyncHud));

		var hazardBox = GetNode<Area2D>("player");
		hazardBox.BodyEntered += areaHazard;

		AddToGroup("player");
	}

	private void SyncHud() {
		if (_hud == null) {
			GD.PushError("[Player] Could not find HUD autoload.");
			return;
		}

		_hp = Mathf.Min(_hp, _hud.MaxMasks);
		_hud.SetHealth(_hp);
	}

	// Physics Process
	public override void _PhysicsProcess(double delta) {
		if (_isDead || lockPlayer) return; // Disable controls if dead

		_wallJumpLockTimer = Mathf.Max(0f, _wallJumpLockTimer - (float)delta);
		HandleDashCooldown(delta);

		if (_isDashing) {
			HandleDash(delta);
		}
		else {
			HandleMovement(delta);
		}

		if (_isWallSliding && !_isDashing)
			HandleWallSlide(delta);

		if (!holdPlayer) {
			HandleJump();         // Allow jumping regardless of dash
			HandleDashInput();    // Still process new dash input
			HandleAttack(delta);  // Allow attacking mid-air or mid-dash
			HandleAnimations();   // Update animation
		}

		if (_invulnerable) {
			_invulnTimer -= (float)delta;
			if (_invulnTimer <= 0f) {
				_invulnerable = false;
				_sprite.SelfModulate = Colors.White;
			}
			else {
				bool on = ((int)(Time.GetTicksMsec() / 100) % 2) == 0;
				_sprite.SelfModulate = new Color(1, 1, 1, on ? 0.5f : 1f);
			}
		}

		if (_hitstunTimer > 0f) {
			_hitstunTimer -= (float)delta;
			Vector2 v = Velocity;
			if (!IsOnFloor()) v += GetGravity() * (float)delta;
			Velocity = v;
			MoveAndSlide();
			return;
		}
	}

	// Movement
	private void HandleMovement(double delta) {
		// Respect wall-jump input lock
		float inputX = (_wallJumpLockTimer > 0f || holdPlayer)
			? 0f
			: Input.GetAxis("move_left", "move_right");

		Vector2 velocity = Velocity;
		if (!IsOnFloor() && !_isWallSliding)
			velocity += GetGravity() * (float)delta;

		if (Mathf.Abs(inputX) > 0.01f) {
			velocity.X = inputX * Speed;
			bool facingLeft = inputX < 0f;
			_sprite.FlipH = facingLeft;
			_sword?.SetFacingLeft(facingLeft);
		}
		else {
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	// Jump
	private void HandleJump() {
		if (IsOnFloor()) {
			if (Input.IsActionJustPressed("jump"))
				Velocity = new Vector2(Velocity.X, JumpVelocity);
		}
		else {
			if (Input.IsActionJustReleased("jump"))
				Velocity = new Vector2(Velocity.X, Velocity.Y * 0.5f);
		}

		if (_isWallSliding && hasWalljump && Input.IsActionJustPressed("jump")) {
			int dir = _sprite.FlipH ? 1 : -1;
			Velocity = new Vector2(dir * WallJumpForce, JumpVelocity);
			_isWallSliding = false;
			_wallJumpLockTimer = WallJumpLockTime;
		}
		else if (_isDashing && Input.IsActionJustPressed("jump")) {
			Velocity = new Vector2(_dashDirection.X * DashSpeed, JumpVelocity);
			_isDashing = false;
			_dashTimer = 0f;
		}
	}

	// Dash
	private void HandleDashCooldown(double delta) {
		if (_dashCooldownTimer > 0f)
			_dashCooldownTimer -= (float)delta;
	}

	private void HandleDash(double delta) {
		_dashTimer -= (float)delta;
		if (_dashTimer <= 0f || !Input.IsActionPressed("dash")) {
			_isDashing = false;
		}
		else {
			Velocity = _dashDirection * DashSpeed;
			MoveAndSlide();
		}
	}

	private void HandleDashInput() {
		if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0f && !_isDashing && hasDash) {
			if (IsOnWall() && !IsOnFloor()) {
				Vector2 upIntoWall = new Vector2((_sprite.FlipH ? -1 : 1) * 0.2f, -1f).Normalized();
				StartDash(upIntoWall);
			}
			else if (IsOnFloor() || !_hasAirDashed) {
				StartDash(Input.GetVector("move_left", "move_right", "ui_up", "ui_down"));
				if (!IsOnFloor()) _hasAirDashed = true;
			}
		}
	}

	private void StartDash(Vector2 direction) {
		if (direction == Vector2.Zero)
			direction = _sprite.FlipH ? Vector2.Left : Vector2.Right;

		_isDashing = true;
		_dashTimer = DashDuration;
		_dashCooldownTimer = DashCooldown;
		_dashDirection = direction.Normalized();
		Velocity = _dashDirection * DashSpeed * (IsOnWall() ? 0.7f : 1f);
	}

	// Wall Slide
	private void HandleWallSlide(double delta) {
		if (IsOnWall() && !IsOnFloor()) {
			_isWallSliding = true;
			Velocity = new Vector2(0, Mathf.Min(Velocity.Y + GetGravity().Y * (float)delta, CurrentWallSlideSpeed));
		}
		else {
			_isWallSliding = false;
		}
	}

	// Attack
	private void HandleAttack(double delta) {
		_attackTimer -= (float)delta;
		if (Input.IsActionJustPressed("attack") && _attackTimer <= 0f && hasSword) {
			_attackTimer = AttackCooldown;
			_anim.Play("Sword");
		}
	}

	public void AttackStart() => _sword?.EnableHitbox();
	public void AttackEnd() => _sword?.DisableHitbox();

	// Animation
	private void HandleAnimations() {
		if(hasSword) {
			_sword.Visible = true;
		} else {
			_sword.Visible = false;
		}
		
		if(Velocity.Y < -10f && Velocity.Y > -200f) {
			nextAnimation = "Peak";
		} else if(Velocity.Y < -200f) {
			nextAnimation = "Jump";
		} else if(Velocity.Y > 100f) {
			nextAnimation = "Fall";
		}  else if(Velocity.Y == 0 && Velocity.X != 0) {
			if(lastAnimation == "Fall") {
				nextAnimation = "IntoRun";
			} else {
				nextAnimation = "Run";
			}
		} else if(Velocity.Y == 0 && Velocity.X == 0) {
			if(lastAnimation == "Fall") {
				nextAnimation = "IntoIdle";
			} else {
				nextAnimation = "Idle";
			}
		}
		
		if(_anim.CurrentAnimation != nextAnimation && (!_anim.IsPlaying() || _anim.CurrentAnimation == "Run")) {
			_anim.Play(nextAnimation);
			lastAnimation = nextAnimation;
		}
	}

	// Health
	public void TakeDamage(int dmg) {
		if (_isDead) return;

		_hp = Mathf.Max(0, _hp - dmg);
		_hud?.SetHealth(_hp);
		_hud?.FlashDamage();

		if (_hp <= 0)
			Die();
	}

	public void Heal(int amt) {
		int max = _hud?.MaxMasks ?? _hp;
		_hp = Mathf.Min(_hp + amt, max);
		_hud?.SetHealth(_hp);
	}

	public void Die() {
		if (_isDead) return;
		_isDead = true;

		_anim.Play("Dead");
		SetPhysicsProcess(false);
		_anim.Connect("animation_finished", new Callable(this, nameof(OnDeathAnimationFinished)));
	}

	private void OnDeathAnimationFinished(string animName) {
		if (animName == "Dead") {
			GD.Print("Game Over!");
			GetTree().Paused = true;
		}
	}

	public override void _Process(double delta) {
		if (IsOnFloor())
			_hasAirDashed = false;

		if (IsOnWall() && !IsOnFloor() && _wallJumpLockTimer <= 0f && !_isDashing) {
			_isWallSliding = true;
			_hasAirDashed = false;
		}
		else if (!IsOnWall() || IsOnFloor()) {
			_isWallSliding = false;
		}
	}

	public async void areaHazard(Node2D body) {
		lockPlayer = true;
		TakeDamage(1);
		await fade.FadeOut(0.5f);
		Position = respawnPoint;
		lockPlayer = false;
		await fade.FadeIn(0.5f);
	}

	public void OnCollect(CollectableType type) {
		if (type == CollectableType.Sword) {
			hasSword = true;
			GlobalRoomChange.hasSword = hasSword;
		}
		else if (type == CollectableType.Dash) {
			hasDash = true;
			GlobalRoomChange.hasDash = hasDash;
		}
		else if (type == CollectableType.Walljump) {
			CurrentWallSlideSpeed = WallSlideSpeed;
			hasWalljump = true;
			GlobalRoomChange.hasWalljump = hasWalljump;
		}
	}

	public void ApplyHit(int dmg, Vector2 sourceGlobalPos) {
		if (_isDead || _invulnerable) return;

		TakeDamage(dmg);
		_invulnerable = true;
		_invulnTimer = InvulnTime;
		_hitstunTimer = HitstunTime;

		Vector2 dir = (GlobalPosition - sourceGlobalPos).Normalized();
		Vector2 kb = new Vector2(dir.X, 0).Normalized() * KnockbackForce;
		kb.Y = -Mathf.Abs(KnockbackUpward);
		Velocity = kb;

		MoveAndSlide();
	}

	public void SetCheckpoint(Vector2 globalPos) {
		respawnPoint = globalPos;
		GD.Print("Respawn Set");
	}

	public async void HoldPlayer(float time) {
		holdPlayer = true;
		_anim.Play("Idle"); //TODO: Move to handleAnimation
		await ToSignal(GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
		holdPlayer = false;
	}
}
