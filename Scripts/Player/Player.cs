using Godot;
using System;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D {
	// -------------------------------
	// Tunables
	// -------------------------------
	[Export] public float Speed = 700.0f;
	[Export] public float JumpVelocity = -1250.0f;
	[Export] public float fallAcceleration = 2.5f;

	// Heal Settings (Hollow Knight style)
	private float _healTimer = 0f;
	private const float HealDuration = 1.8f;
	private const int HealAmount = 1;
	private const int HealManaCost = 3;
	private bool _isHealing = false;

	// Movement variables
	private Vector2 _dashDirection = Vector2.Zero;
	private bool _isDashing = false;
	private bool _hasAirDashed = false;
	private bool _isWallSliding = false;
	private float _dashTimer = 0f;
	private float _dashCooldownTimer = 0f;
	[Export] public float WallJumpLockTime = 0.1f;
	private float _wallJumpLockTimer = 0f;
	private bool holdPlayer = false;

	// Wall slide
	[Export] public float WallSlideSpeed = 400.0f;
	[Export] public float WallJumpForce = 1000.0f;
	[Export] private bool hasWalljump = false;

	// Dash
	[Export] public float DashSpeed = 2000.0f;
	[Export] public float DashDuration = 0.2f;
	[Export] public float DashCooldown = 0.5f;
	[Export] private bool hasDash = false;

	// Attack
	[Export] public float AttackCooldown = 0.25f;
	private float _attackTimer = 0f;
	[Export] private bool hasSword = false;
	[Export] private bool hasClawTeleport = false;
	private Node2D clawSprites;
	
	// Claw Teleport Variables
	[Export] public PackedScene ThrowableClawScene { get; set; }
	private ThrowableClaw _activeThrownClaw;
	private bool _clawIsThrown = false;

	// Health & mana
	private int _hp;
	private int _mana;
	private bool _isDead = false;

	// Node paths
	[Export] public NodePath SwordPath { get; set; }

	// Animation vars
	private string nextAnimation = "";
	private string lastAnimation;

	// Nodes
	private AnimationPlayer _anim;
	private AnimationPlayer swordAnimator;
	private Node2D sprites;
	private Sword _sword;
	private HUD _hud;
	private ScreenFader fade;

	// Damage & invuln
	[Export] public float InvulnTime = 0.6f;
	[Export] public float HitstunTime = 0.15f;
	[Export] public float KnockbackForce = 260f;
	[Export] public float KnockbackUpward = 120f;
	private bool _invulnerable = false;
	private float _invulnTimer = 0f;
	private float _hitstunTimer = 0f;
	private bool lockPlayer = false;
	private Vector2 respawnPoint;
	private AudioStreamPlayer2D _footstepPlayer;
	private AudioStreamPlayer2D _jumpPlayer;
	private AudioStreamPlayer2D _dashPlayer;
	private AudioStreamPlayer2D _swingPlayer;
	private AudioStreamPlayer2D _wallJumpPlayer;
	private AudioStreamPlayer2D _landPlayer;
	private bool _wasOnFloor = false;
	private bool hasSwung = false;



	// -------------------------------
	// Initialization
	// -------------------------------
	public override void _Ready() {
		if (GlobalRoomChange.Activate) {
			GlobalPosition = GlobalRoomChange.PlayerPos;
			if (GlobalRoomChange.PlayerJumpOnEnter)
				Velocity = new Vector2(0, JumpVelocity);

			hasSword = GlobalRoomChange.hasSword;
			hasDash = GlobalRoomChange.hasDash;
			hasWalljump = GlobalRoomChange.hasWalljump;
			hasClawTeleport = GlobalRoomChange.hasClawTeleport; // Add claw teleport to room changes

			GlobalRoomChange.Activate = false;
			_hp = GlobalRoomChange.health;
			_mana = GlobalRoomChange.mana;
		}
		else {
			// Starting fresh or loading from save
			// Check if this is a new game vs loading from existing save
			if (!SaveManager.IsNewGame()) {
				// Loading from existing save (Continue button was pressed)
				var saveData = SaveManager.GetCurrentSave();
				ApplySaveData(saveData, true);
				
				// IMPORTANT: Sync loaded abilities to GlobalRoomChange so room transitions work correctly
				GlobalRoomChange.hasSword = hasSword;
				GlobalRoomChange.hasDash = hasDash;
				GlobalRoomChange.hasWalljump = hasWalljump;
				GlobalRoomChange.hasClawTeleport = hasClawTeleport;
				GlobalRoomChange.health = _hp;
				GlobalRoomChange.mana = _mana;
				
				// Make sure the room group is set correctly for gameplay scenes
				// This ensures the HUD shows up properly
				string currentSceneName = GetTree().CurrentScene.Name;
				string lowerSceneName = currentSceneName.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				if (lowerSceneName.Contains("room") || lowerSceneName.Contains("level")) {
					GlobalRoomChange.EnterRoom(currentSceneName, RoomGroup.Overworld);
				}
			}
			else {
				// New game - use default starting values, don't load from save
				_hp = 5; // Default starting health
				hasSword = false;
				hasDash = false;
				hasWalljump = false;
				hasClawTeleport = false;
				
				// IMPORTANT: Update GlobalRoomChange to match new game state
				GlobalRoomChange.health = _hp;
				GlobalRoomChange.hasSword = false;
				GlobalRoomChange.hasDash = false;
				GlobalRoomChange.hasWalljump = false;
				GlobalRoomChange.hasClawTeleport = false;
				
				// Make sure the room group is set for new game
				string currentSceneName = GetTree().CurrentScene.Name;
				string lowerSceneName = currentSceneName.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				if (lowerSceneName.Contains("room") || lowerSceneName.Contains("level")) {
					GlobalRoomChange.EnterRoom(currentSceneName, RoomGroup.Overworld);
				}
			}
		}

		respawnPoint = Position;

		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		swordAnimator = GetNode<AnimationPlayer>("SwordAnimation");
		sprites = GetNode<Node2D>("Sprites");
		clawSprites = GetNode<Node2D>("Sprites/ClawSprites");
		_sword = GetNodeOrNull<Sword>(SwordPath);
		if (hasSword) {
			_sword.Visible = true;
			clawSprites.Visible = true;
		}

		// Get HUD from autoload - defer this to ensure autoloads are ready
		CallDeferred(nameof(InitializeHUD));
		
		fade = GetNodeOrNull<ScreenFader>("../ScreenFade");

		var hazardBox = GetNode<Area2D>("HitBox");
		hazardBox.BodyEntered += areaHazard;

		_footstepPlayer = GetNode<AudioStreamPlayer2D>("Audio/FootstepPlayer");
		_jumpPlayer = GetNode<AudioStreamPlayer2D>("Audio/JumpPlayer");
		_dashPlayer = GetNode<AudioStreamPlayer2D>("Audio/DashPlayer");
		_swingPlayer = GetNode<AudioStreamPlayer2D>("Audio/SwordSwingPlayer");
		_wallJumpPlayer = GetNode<AudioStreamPlayer2D>("Audio/WallJumpPlayer");
		_landPlayer = GetNode<AudioStreamPlayer2D>("Audio/LandPlayer");

		AddToGroup("player");
	}

	private void InitializeHUD()
	{
		// Get HUD from autoload
		_hud = GetNodeOrNull<HUD>("/root/HUD");
		if (_hud != null)
		{
			// Sync initial HUD state
			SyncHud();
		}
	}

	private void SyncHud() {
		// Try to reconnect to HUD if it's missing
		if (_hud == null)
		{
			_hud = GetNodeOrNull<HUD>("/root/HUD");
			if (_hud == null) return;
		}
		_hud.SetHealth(_hp);
		_hud.SetMana(_mana);
	}

	// -------------------------------
	// Physics Loop
	// -------------------------------
	public override void _PhysicsProcess(double delta) {
		if (_isDead || lockPlayer) return;

		_wallJumpLockTimer = Mathf.Max(0f, _wallJumpLockTimer - (float)delta);
		HandleDashCooldown(delta);

		if (_isDashing) HandleDash(delta);
		else HandleMovement(delta);

		if (!_isDashing) HandleWallSlide(delta);

		if (!holdPlayer) {
			HandleJump();
			HandleDashInput();
			HandleAttack(delta);
			HandleAnimations();
		}
		// --- Landing detection ---
		if (!_wasOnFloor && IsOnFloor()) {
			// Just landed this frame
			_landPlayer.PitchScale = (float)GD.RandRange(0.95, 1.05);
			_landPlayer.Play();
		}
		_wasOnFloor = IsOnFloor();


		HandleHeal(delta);
		UpdateInvulnerability(delta);
		UpdateHitstun(delta);
	}

	private void UpdateInvulnerability(double delta) {
		if (!_invulnerable) return;
		_invulnTimer -= (float)delta;
		if (_invulnTimer <= 0f) {
			_invulnerable = false;
			sprites.SelfModulate = Colors.White;
		}
		else {
			bool flash = ((int)(Time.GetTicksMsec() / 100) % 2) == 0;
			sprites.SelfModulate = new Color(1, 1, 1, flash ? 0.5f : 1f);
		}
	}

	private void UpdateHitstun(double delta) {
		if (_hitstunTimer <= 0f) return;
		_hitstunTimer -= (float)delta;
		Vector2 v = Velocity;
		if (!IsOnFloor()) v += GetGravity() * (float)delta;
		Velocity = v;
		MoveAndSlide();
	}

	// -------------------------------
	// Movement & Jump
	// -------------------------------
	private void HandleMovement(double delta) {
		float inputX = (_wallJumpLockTimer > 0f || holdPlayer)
			? 0f
			: Input.GetAxis("move_left", "move_right");

		Vector2 velocity = Velocity;
		int div = 1;

		if (!IsOnFloor() && !_isWallSliding && !_isDashing) {
			velocity += Velocity.Y > 0
				? GetGravity() * (float)delta * fallAcceleration
				: GetGravity() * (float)delta;
			div = 7;
		}

		if (Mathf.Abs(inputX) > 0.01f) {
			velocity.X = inputX * Speed;
			bool facingLeft = inputX < 0f;
			sprites.Scale = new Vector2(facingLeft ? -1 : 1, 1);
			_sword?.SetFacingLeft(facingLeft);
		}
		else velocity.X = Mathf.MoveToward(velocity.X, 0, Speed / div);

		// --- Footstep Sound ---
		if (IsOnFloor() && Mathf.Abs(Velocity.X) > 50f) {
			// Play only when not already playing, to prevent spam
			if (!_footstepPlayer.Playing) {
				_footstepPlayer.PitchScale = (float)GD.RandRange(0.95f, 1.05f); // adds variation
				_footstepPlayer.Play();
			}
		}
		else {
			// Stop footsteps midair or idle
			if (_footstepPlayer.Playing)
				_footstepPlayer.Stop();
		}


		Velocity = velocity;
		MoveAndSlide();
	}

	private void HandleJump() {
		if (IsOnFloor()) {
			if (Input.IsActionJustPressed("jump")) {
				Velocity = new Vector2(Velocity.X, JumpVelocity);
				_jumpPlayer.Play();
			}
		}
		else if (Input.IsActionJustReleased("jump") && Velocity.Y < 0)
			Velocity = new Vector2(Velocity.X, Velocity.Y * 0.5f);

		if (_isWallSliding && hasWalljump && Input.IsActionJustPressed("jump")) {
			int dir = sprites.Scale.X < 0 ? 1 : -1;
			Velocity = new Vector2(dir * WallJumpForce, JumpVelocity);
			_wallJumpPlayer.PitchScale = (float)GD.RandRange(0.9f, 1.1f);
			_wallJumpPlayer.Play();
			_isWallSliding = false;
			_wallJumpLockTimer = WallJumpLockTime;
		}
		// else if (_isDashing && Input.IsActionJustPressed("jump")) {
		// 	Velocity = new Vector2(_dashDirection.X * DashSpeed, JumpVelocity);
		// 	_isDashing = false;
		// 	_dashTimer = 0f;
		// 	_jumpPlayer.Play();
		// }
	}

	// -------------------------------
	// Dash
	// -------------------------------
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
				Vector2 upIntoWall = new Vector2((sprites.Scale.X < 0 ? -1 : 1) * 0.2f, -1f).Normalized();
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
			direction = sprites.Scale.X < 0 ? Vector2.Left : Vector2.Right;

		_isDashing = true;
		_dashTimer = DashDuration;
		_dashCooldownTimer = DashCooldown;
		_dashDirection = direction.Normalized();
		Velocity = _dashDirection * DashSpeed * (IsOnWall() ? 0.7f : 1f);
		_dashPlayer.Play();
	}

	// -------------------------------
	// Wall Slide
	// -------------------------------
	private void HandleWallSlide(double delta) {
		if (IsOnWall() && !IsOnFloor() && hasWalljump && Input.GetAxis("move_left", "move_right") != 0) {
			_isWallSliding = true;
			Velocity = new Vector2(0, Mathf.Min(Velocity.Y + GetGravity().Y * (float)delta, WallSlideSpeed));
		}
		else _isWallSliding = false;
	}

	// -------------------------------
	// Attack
	// -------------------------------
	private void HandleAttack(double delta) {
		_attackTimer -= (float)delta;
		if (Input.IsActionJustPressed("attack") && _attackTimer <= 0f && hasSword) {
			_attackTimer = AttackCooldown;
			swordAnimator.Play("Swing");
			_swingPlayer.Play();
		}
		
		// Handle claw throwing (only if teleport upgrade is available)
		if (Input.IsActionJustPressed("sword_throw") && _attackTimer <= 0f && hasSword && hasClawTeleport && !_clawIsThrown) {
			ThrowClaw();
		}
	}

	public void AttackStart() => _sword?.EnableHitbox();
	public void AttackEnd() => _sword?.DisableHitbox();
	
	private void ThrowClaw()
	{
		if (ThrowableClawScene == null || _clawIsThrown) return;
		
		_attackTimer = AttackCooldown;
		_clawIsThrown = true;
		
		// Hide the regular sword (claw is still attached to sword)
		if (_sword != null)
		{
			_sword.Visible = false;
		}
		
		// Create throwable claw
		_activeThrownClaw = ThrowableClawScene.Instantiate<ThrowableClaw>();
		GetTree().CurrentScene.AddChild(_activeThrownClaw);
		
		// Set the player reference in the claw
		_activeThrownClaw.SetPlayer(this);
		
		// Position it at the player's claw position
		Vector2 clawOffset = new Vector2(sprites.Scale.X < 0 ? -30 : 30, -10);
		_activeThrownClaw.GlobalPosition = GlobalPosition + clawOffset;
		
		// Determine throw direction based on player facing
		Vector2 throwDirection = sprites.Scale.X < 0 ? Vector2.Left : Vector2.Right;
		_activeThrownClaw.ThrowInDirection(throwDirection);
		
		// Connect to the claw's destruction to know when to show our sword again
		_activeThrownClaw.TreeExiting += OnThrownClawDestroyed;
	}
	
	private void OnThrownClawDestroyed()
	{
		_clawIsThrown = false;
		_activeThrownClaw = null;
		
		// Show the regular sword again (claw is part of the sword weapon)
		if (_sword != null && hasSword)
		{
			_sword.Visible = true;
		}
	}

	// -------------------------------
	// Animation Control
	// -------------------------------
	private void HandleAnimations() {
		if (hasSword && !_clawIsThrown) {
			_sword.Visible = true;
			clawSprites.Visible = true;
		}
		else {
			_sword.Visible = false;
			clawSprites.Visible = false;
		}

		if (holdPlayer) {
			nextAnimation = ("Stagger");
		}
		else if (_isDashing) {
			if (Velocity.Y < 0) nextAnimation = "Jump";
			else nextAnimation = "Dash";
		}
		else if (_isWallSliding) {
			if (lastAnimation != "IntoWallslide" && lastAnimation != "Wallslide") {
				nextAnimation = ("IntoWallslide");
			}
			else {
				nextAnimation = ("Wallslide");
			}
		}
		else if (_wallJumpLockTimer > 0f && (lastAnimation == "IntoWallslide" || lastAnimation == "Wallslide")) {
			GD.Print(lastAnimation);
			nextAnimation = "Jump";
		}
		else if (Velocity.Y < -10f && Velocity.Y > -200f) {
			nextAnimation = "Peak";
		}
		else if (Velocity.Y < -200f) {
			nextAnimation = "Jump";
		}
		else if (Velocity.Y > 100f) {
			nextAnimation = "Fall";
		}
		else if (Velocity.Y == 0 && Velocity.X != 0) {
			if (lastAnimation == "Fall") {
				nextAnimation = "IntoRun";
			}
			else {
				nextAnimation = "Run";
			}
		}
		else if (Velocity.Y == 0 && Velocity.X == 0) {
			if (lastAnimation == "Fall") {
				nextAnimation = "IntoIdle";
			}
			else {
				nextAnimation = "Idle";
			}
		}

		if (nextAnimation == "IntoWallslide" || nextAnimation == "Wallslide") {
			sprites.Scale = new Vector2(sprites.Scale.X * -1, 1);
		}

		if (_anim.CurrentAnimation != nextAnimation && (!_anim.IsPlaying() || _anim.CurrentAnimation == "Run" || _anim.CurrentAnimation == "Wallslide" || _anim.CurrentAnimation == "IntoWallslide")) {
			_anim.Play(nextAnimation);
			lastAnimation = nextAnimation;
		}
	}

	// -------------------------------
	// Health & Heal
	// -------------------------------
	public void TakeDamage(int dmg) {
		if (_isDead) return;

		_hp = Mathf.Max(0, _hp - dmg);
		_hud?.SetHealth(_hp);
		_hud?.FlashDamage();
		GlobalRoomChange.health = _hp;

		if (_hp <= 0) Die();
	}

	public async void Heal(int amt) {
		GD.Print($"[Heal] Attempting heal: hp={_hp}, mana={_mana}");
		if (!SpendMana(HealManaCost)) {
			GD.Print("[Heal] Not enough mana!");
			return;
		}
		int before = _hp;
		int max = _hud?.MaxMasks ?? _hp;
		_hp = Mathf.Min(_hp + amt, max);
		GlobalRoomChange.health = _hp;
		_hud?.SetHealth(_hp);
		await _hud?.DrainManaForHeal(HealManaCost, 1.0f);
		GD.Print($"[Heal] Success — {before} → {_hp}, mana now {_mana}");
	}

	private void HandleHeal(double delta) {
		if (_isDead || lockPlayer || _isDashing || _hitstunTimer > 0f)
			return;

		// --------------------------
		// Begin Healing (button pressed)
		// --------------------------
		if (Input.IsActionPressed("heal")) {
			if (!_isHealing && _mana >= HealManaCost) {
				_isHealing = true;
				_healTimer = 0f;
				holdPlayer = true;
				_invulnerable = true;
				GD.Print("[Heal] Started");
				if (_anim.HasAnimation("FocusStart"))
					_anim.Play("FocusStart");
			}

			// Always advance timer while healing
			if (_isHealing) {
				_healTimer += (float)delta;

				if (_healTimer >= HealDuration) {
					GD.Print("[Heal] Finished");
					Heal(HealAmount);
					StopHealing();
					return;
				}

				// Small debug print
				if (((int)(Time.GetTicksMsec() / 250)) % 5 == 0)
					DebugHealState();
			}
		}
		// --------------------------
		// Heal Cancelled (button released)
		// --------------------------
		else if (_isHealing) {
			GD.Print("[Heal] Cancelled");
			StopHealing();
		}
	}


	private void StopHealing() {
		GD.Print("[Heal] StopHealing called");
		_isHealing = false;
		holdPlayer = false;
		_invulnerable = false;
		_healTimer = 0f;

		if (_anim.HasAnimation("FocusEnd"))
			_anim.Play("FocusEnd");
		else
			_anim.Play("Idle");
	}

	private void DebugHealState() {
		GD.Print($"[HealDebug] isHealing={_isHealing}, holdPlayer={holdPlayer}, mana={_mana}, healTimer={_healTimer:F2}/{HealDuration}, anim={_anim?.CurrentAnimation}");
	}

	private void _OnFocusStartFinished() {
		if (_isHealing && _anim.HasAnimation("FocusHold")) {
			_anim.Play("FocusHold");
			GD.Print("[Heal] FocusStart → FocusHold");
		}
	}

	// -------------------------------
	// Death & Respawn
	// -------------------------------
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
		if (IsOnFloor()) _hasAirDashed = false;
		if (IsOnWall() && !IsOnFloor() && _wallJumpLockTimer <= 0f && !_isDashing && hasWalljump) {
			_hasAirDashed = false;
		}
	}

	// -------------------------------
	// Misc
	// -------------------------------
	public async void areaHazard(Node2D body) {
		lockPlayer = true;
		TakeDamage(1);
		await fade.FadeOut(0.5f);
		Position = respawnPoint;
		lockPlayer = false;
		await fade.FadeIn(0.5f);
	}

	public void OnCollect(CollectableType type) {
		if (type == CollectableType.Sword) hasSword = true;
		else if (type == CollectableType.Dash) hasDash = true;
		else if (type == CollectableType.Walljump) hasWalljump = true;
		else if (type == CollectableType.Throw) hasClawTeleport = true;
		GlobalRoomChange.hasSword = hasSword;
		GlobalRoomChange.hasDash = hasDash;
		GlobalRoomChange.hasWalljump = hasWalljump;
		GlobalRoomChange.hasClawTeleport = hasClawTeleport;
	}

	public void ApplyHit(int dmg, Vector2 src) {
		if (_isDead || _invulnerable) return;
		TakeDamage(dmg);
		_invulnerable = true;
		_invulnTimer = InvulnTime;
		_hitstunTimer = HitstunTime;
		Vector2 dir = (GlobalPosition - src).Normalized();
		Vector2 kb = new Vector2(dir.X, 0).Normalized() * KnockbackForce;
		kb.Y = -Mathf.Abs(KnockbackUpward);
		Velocity = kb;
		MoveAndSlide();
	}

	public void SetCheckpoint(Vector2 globalPos) => respawnPoint = globalPos;

	public async void HoldPlayer(float time) {
		holdPlayer = true;

		await ToSignal(GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
		holdPlayer = false;
	}

	public int MaxMana => GlobalRoomChange.maxMana;

	public void AddMana(int amount) {
		_mana = Mathf.Min(_mana + amount, MaxMana);
		_hud?.SetMana(_mana);
		GlobalRoomChange.mana = _mana;
	}

	public bool SpendMana(int amount) {
		if (_mana < amount) return false;
		_mana -= amount;
		_hud?.SetMana(_mana);
		GlobalRoomChange.mana = _mana;
		return true;
	}

	// --- Save / Load helpers -------------------------------------------------

	public SaveManager.SaveData ToSaveData()
	{
		// Get current scene file path
		string currentScenePath = GetTree().CurrentScene.SceneFilePath;
		
		return new SaveManager.SaveData
		{
			Hp = _hp,
			HasSword = hasSword,
			HasDash = hasDash,
			HasWalljump = hasWalljump,
			HasClawTeleport = hasClawTeleport,
			CurrentScene = currentScenePath,
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
		hasClawTeleport = data.HasClawTeleport;

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

	// Helper used for deferred application from external code (reads cached save)
	public void ApplySaveDataFromManager(bool setPosition = true)
	{
		var data = SaveManager.GetCurrentSave();
		if (data != null)
			ApplySaveData(data, setPosition);
	}
}
