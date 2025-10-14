// using Godot;
// using System;

// public partial class Boss : CharacterBody2D
// {
//     // ---------- Tunables ----------
//     [Export] public int MaxHealth = 1000;
//     [Export] public float WalkSpeed = 120f;
//     [Export] public float JumpVy = -200f;
//     [Export] public float JumpVx = 220f; // keep positive in Inspector
//     [Export] public float SlashSlideSpeed = 320f;
//     [Export] public float UppercutHoriSpeed = 100f;
//     [Export] public float DashSpeed = 420f;
//     [Export] public float DashStopAccel = 2500f;
//     [Export] public float HurtKnockback = 420f;

//     [Export] public float ArenaLeftX = 0f;
//     [Export] public float ArenaRightX = 700f;
//     [Export] public int ContactDamage = 1;
//     [Export] public NodePath PlayerPath;

//     // Optional gravity scale (multiplies world gravity)
//     [Export] public float GravityScale = 1f;

//     // ---------- Animation names ----------
//     [Export] public string AIdle = "Idle";
//     [Export] public string AFall = "Fall";
//     [Export] public string ARoarPrep = "RoarPrep";
//     [Export] public string ARoar = "Roar";
//     [Export] public string ASlashPrep = "SlashPrep";
//     [Export] public string ASlash = "Slash";
//     [Export] public string AUppercutPrep = "UppercutPrep";
//     [Export] public string AUppercut = "Uppercut";
//     [Export] public string AMove = "Move";
//     [Export] public string AJump = "Jump";
//     [Export] public string ADashPrep = "DashPrep";
//     [Export] public string ADash = "Dash";
//     [Export] public string ADashStop = "DashStop";
//     [Export] public string AStagger = "Stagger";
//     [Export] public string ADeath = "Death";

//     [Export] public bool RunWithoutAnimations = true;
//     [Export] public bool WatchdogEnabled = true;
//     [Export] public float WatchdogSeconds = 6.0f;
//     [Export] public bool LogStateChanges = true;

//     private float _watchdog = 0f;

//     // ---------- Internal timers ----------
//     private float _debugTimer = 0f;
//     private float _readyTimer = 0f;
//     private float _idleDecisionTimer = 0f;
//     private float _stateTimer = 0f;
//     private float _landTimer = 0f;
//     private float _airTime = 0f;
//     private bool _colliderReenabledMidair = false;

//     // ---------- Node references ----------
//     private AnimationPlayer _anim;
//     private Sprite2D _sprite;
//     private Node2D _spriteRoot;
//     private CpuParticles2D _blood;
//     private CpuParticles2D _spark;
//     private Timer _matTimer;
//     private Area2D _hurtbox;
//     private Area2D _hitbox;
//     private Player _player;

//     // ---------- FSM ----------
//     private enum State { Ready, ReadyDrop, RoarPrep, Roar, Idle, Move, Jump, Fall, Dash, DashStop, Hurt, Die_1, Die_2 }

//     private State _state = State.Ready;
//     private bool _stateNew = true;
//     private bool _changedThisFrame = false;
//     private Vector2 _playerPos = Vector2.Zero;
//     private int _hp;
//     private readonly RandomNumberGenerator _rng = new();

//     // ---------- Jump / detach ----------
//     private CollisionShape2D _col;
//     private float _defaultSnap = 10f;
//     private int _detachFrames = 0;
//     private bool _snapSuppressed = false;
//     private bool _leftGround = false; // gate landing until truly airborne

//     // Motion mode (Grounded <-> Floating)
//     private MotionModeEnum _defaultMotionMode = MotionModeEnum.Grounded;

//     // ---------- Gravity ----------
//     private Vector2 _worldGravity = Vector2.Zero;

//     private void RefreshWorldGravity()
//     {
//         float g = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
//         Vector2 v = ((Vector2)ProjectSettings.GetSetting("physics/2d/default_gravity_vector")).Normalized();
//         _worldGravity = v * g * Mathf.Max(0f, GravityScale);
//     }

//     private void ApplyGravityGroundAware(double dt)
//     {
//         if (!IsOnFloor() || Velocity.Y < 0f)
//             Velocity += _worldGravity * (float)dt;
//     }

//     private void ApplyGravityAlways(double dt)
//     {
//         Velocity += _worldGravity * (float)dt;
//     }

//     private bool HasAnim(string name) =>
//         _anim != null && !string.IsNullOrEmpty(name) && _anim.HasAnimation(name);

//     private void SafePlay(string name)
//     {
//         if (!RunWithoutAnimations && HasAnim(name))
//             _anim.Play(name);
//     }

//     private bool AnimFinished(string name)
//     {
//         if (RunWithoutAnimations || !HasAnim(name)) return true;
//         return !_anim.IsPlaying() && _anim.CurrentAnimation == name;
//     }

//     public override void _Ready()
//     {
//         _hp = MaxHealth;
//         UpDirection = Vector2.Up;

//         RefreshWorldGravity();
//         GD.Print($"[Boss READY] JumpVy = {JumpVy}, Gravity={_worldGravity}");

//         _anim = GetNodeOrNull<AnimationPlayer>("SpriteRoot/AnimationPlayer")
//              ?? GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

//         _spriteRoot = GetNodeOrNull<Node2D>("SpriteRoot") ?? this;
//         _sprite = GetNodeOrNull<Sprite2D>("SpriteRoot/Sprite2D");
//         _blood = GetNodeOrNull<CpuParticles2D>("BloodEmitter");
//         _spark = GetNodeOrNull<CpuParticles2D>("SparkEmitter");
//         _matTimer = GetNodeOrNull<Timer>("MateriaTimer");

//         _hurtbox = GetNodeOrNull<Area2D>("HurtBox");
//         _hitbox = GetNodeOrNull<Area2D>("Hitbox");

//         if (_hitbox != null)
//         {
//             _hitbox.BodyEntered += OnHitBoxBodyEntered;
//             _hitbox.AreaEntered += OnHitBoxAreaEntered;
//         }

//         if (_matTimer != null)
//             _matTimer.Timeout += OnMateriaTimeout;

//         if (PlayerPath != null && !PlayerPath.IsEmpty)
//             _player = GetNodeOrNull<Player>(PlayerPath);
//         else
//         {
//             var players = GetTree().GetNodesInGroup("player");
//             if (players.Count > 0) _player = players[0] as Player;
//         }

//         _rng.Randomize();

//         _col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
//         _defaultSnap = FloorSnapLength;

//         _defaultMotionMode = MotionMode; // remember editor setting
//     }

//     public override void _PhysicsProcess(double delta)
//     {
//         _changedThisFrame = false; //  reset at frame start

//         if (_player != null) _playerPos = _player.GlobalPosition;

//         if (_detachFrames > 0)
//         {
//             FloorSnapLength = 0f;
//             _snapSuppressed = true;
//             _col?.SetDeferred("disabled", true);
//             _detachFrames--;
//         }
//         // DO NOT auto re-enable here; Jump/Fall restore on landing

//         if (WatchdogEnabled)
//         {
//             bool transient = _state is State.Jump or State.Fall or State.Dash or State.DashStop;
//             if (!transient)
//             {
//                 _watchdog += (float)delta;
//                 if (_watchdog > WatchdogSeconds)
//                 {
//                     GD.PushWarning($"[Boss] Watchdog tripped in state: {_state}. Forcing Idle.");
//                     Velocity = Vector2.Zero;
//                     Change(State.Idle);
//                 }
//             }
//         }

//         switch (_state)
//         {
//             case State.Ready: S_Ready(delta); break;
//             case State.ReadyDrop: S_ReadyDrop(delta); break;
//             case State.RoarPrep: S_RoarPrep(delta); break;
//             case State.Roar: S_Roar(delta); break;
//             case State.Idle: S_Idle(delta); break;
//             case State.Move: S_Move(delta); break;
//             case State.Jump: S_Jump(delta); break;
//             case State.Fall: S_Fall(delta); break;
//             case State.Dash: S_Dash(delta); break;
//             case State.DashStop: S_DashStop(delta); break;
//             case State.Hurt: S_Hurt(delta); break;
//             case State.Die_1: S_Die1(delta); break;
//             case State.Die_2: S_Die2(delta); break;
//         }

//         // only clear _stateNew if no Change() happened this frame
//         if (!_changedThisFrame) _stateNew = false;

//         _debugTimer -= (float)delta;
//         if (_debugTimer <= 0f)
//         {
//             GD.Print($"[Boss] State={_state} Pos={GlobalPosition} Vel={Velocity} OnFloor={IsOnFloor()} Grav={_worldGravity}");
//             _debugTimer = 0.25f;
//         }
//     }

//     private void Change(State s)
//     {
//         if (LogStateChanges && s != _state)
//             GD.Print($"[Boss] {_state} → {s}");
//         _state = s;
//         _stateNew = true;
//         _watchdog = 0f;
//         _changedThisFrame = true; // mark so _stateNew survives to next frame
//     }

//     // ---------- Helpers ----------
//     private void FacePlayer()
//     {
//         if (_player == null || _spriteRoot == null) return;
//         bool faceLeft = _playerPos.X < GlobalPosition.X;
//         _spriteRoot.Scale = new Vector2(faceLeft ? 1 : -1, 1);
//     }

//     // ---------- States ----------
//     private void S_Ready(double dt)
//     {
//         if (_stateNew) { FacePlayer(); SafePlay(AIdle); _readyTimer = 0.5f; }
//         _readyTimer -= (float)dt;
//         if (_readyTimer <= 0f) Change(State.ReadyDrop);
//     }

//     private void S_ReadyDrop(double dt)
//     {
//         if (_stateNew) SafePlay(AFall);
//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();
//         if (IsOnFloor()) Change(State.Idle);
//     }

//     private void S_RoarPrep(double dt)
//     {
//         if (_stateNew) { SafePlay(ARoarPrep); _stateTimer = 0.35f; }
//         _stateTimer -= (float)dt;
//         if (_stateTimer <= 0f || AnimFinished(ARoarPrep)) Change(State.Roar);
//     }

//     private void S_Roar(double dt)
//     {
//         if (_stateNew) { SafePlay(ARoar); _stateTimer = 0.6f; }
//         _stateTimer -= (float)dt;
//         if (_stateTimer <= 0f || AnimFinished(ARoar)) Change(State.Idle);
//     }

//     private void S_Idle(double dt)
//     {
//         if (_stateNew)
//         {
//             FacePlayer();
//             SafePlay(AIdle);
//             _idleDecisionTimer = 0.5f;
//             MotionMode = _defaultMotionMode; // ensure grounded
//         }

//         Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 2000f * (float)dt), Velocity.Y);
//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();

//         _idleDecisionTimer -= (float)dt;
//         if (_idleDecisionTimer <= 0f && IsOnFloor())
//             Change(_rng.Randf() > 0.5f ? State.Move : State.Jump);
//     }

//     private void S_Move(double dt)
//     {
//         if (_stateNew)
//         {
//             FacePlayer();
//             SafePlay(AMove);
//             _stateTimer = 1.5f;
//             MotionMode = _defaultMotionMode; // walking uses grounded
//         }

//         float dir = _playerPos.X > GlobalPosition.X ? 1f : -1f;
//         float target = dir * WalkSpeed;
//         Velocity = new Vector2(Mathf.MoveToward(Velocity.X, target, 2500f * (float)dt), Velocity.Y);

//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();

//         _stateTimer -= (float)dt;
//         if (_stateTimer <= 0f) Change(State.Idle);
//     }

//     private void S_Jump(double dt)
//     {
//         if (_stateNew)
//         {
//             _defaultSnap = FloorSnapLength;
//             FloorSnapLength = 0f;         // detach without switching modes
//             _snapSuppressed = true;
//             _detachFrames = 10;
//             _leftGround = false;
//             _airTime = 0f;
//             _colliderReenabledMidair = false;

//             _col ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
//             if (_col != null) _col.Disabled = true;   // briefly disable so won't re-stick the same frame

//             GlobalPosition += new Vector2(0, -2f);    // tiny lift

//             float dir = Mathf.Sign(_playerPos.X - GlobalPosition.X);
//             if (Mathf.IsZeroApprox(dir)) dir = 1f;
//             float vx = Mathf.Abs(JumpVx);

//             Velocity = new Vector2(dir * vx, JumpVy);
//             GD.Print($"[Boss JUMP INIT] Velocity={Velocity}");

//             SafePlay(AJump);
//             return; // integrate next frame
//         }

//         // Airborne integration
//         ApplyGravityAlways(dt);
//         MoveAndSlide();

//         // Track leaving ground
//         if (!IsOnFloor()) _airTime += (float)dt;
//         if (!_leftGround && !IsOnFloor()) _leftGround = true;

//         // Re-enable collider midair so floor can be detected later
//         if (_leftGround && !_colliderReenabledMidair && (_airTime > 0.06f || Velocity.Y > 0f))
//         {
//             _col?.SetDeferred("disabled", false);
//             _colliderReenabledMidair = true;
//         }

//         // Rising -> Falling
//         if (!IsOnFloor() && Velocity.Y > 0f)
//             Change(State.Fall);

//         // Land early (very low jump)
//         if (_colliderReenabledMidair && IsOnFloor())
//         {
//             FloorSnapLength = _defaultSnap;
//             _snapSuppressed = false;
//             _detachFrames = 0;
//             Change(State.Idle);
//         }
//     }

//     private void S_Fall(double dt)
//     {
//         if (_stateNew)
//         {
//             SafePlay(AFall);
//             _landTimer = 0.08f; // small debounce

//             // ensure collider ON during the fall 
//             if (_col != null && _col.Disabled)
//                 _col.SetDeferred("disabled", false);
//             _colliderReenabledMidair = true;
//         }

//         ApplyGravityAlways(dt);
//         MoveAndSlide();

//         if (IsOnFloor())
//         {
//             _landTimer -= (float)dt;
//             if (_landTimer <= 0f)
//             {
//                 FloorSnapLength = _defaultSnap; // restore snapping
//                 _snapSuppressed = false;
//                 _detachFrames = 0;
//                 Change(State.Idle);
//             }
//         }
//     }

//     private void S_Dash(double dt)
//     {
//         if (_stateNew)
//         {
//             float dir = (_spriteRoot?.Scale.X ?? 1f) == 1f ? -1f : 1f;
//             Velocity = new Vector2(dir * DashSpeed, 0);
//             SafePlay(ADash);
//             MotionMode = _defaultMotionMode; // ground dash
//         }
//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();

//         bool hitLeft = GlobalPosition.X <= ArenaLeftX + 2f;
//         bool hitRight = GlobalPosition.X >= ArenaRightX - 2f;
//         if (hitLeft || hitRight) Change(State.DashStop);
//     }

//     private void S_DashStop(double dt)
//     {
//         if (_stateNew) { SafePlay(ADashStop); _stateTimer = 0.25f; }
//         _stateTimer -= (float)dt;

//         Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, DashStopAccel * (float)dt), Velocity.Y);
//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();

//         if (_stateTimer <= 0f || AnimFinished(ADashStop)) Change(State.Idle);
//     }

//     private void S_Hurt(double dt)
//     {
//         if (_stateNew)
//         {
//             SafePlay(AStagger);
//             float dir = _player != null && _playerPos.X < GlobalPosition.X ? 1f : -1f;
//             Velocity = new Vector2(HurtKnockback * dir, -Mathf.Abs(JumpVy) * 0.2f);
//             _stateTimer = 0.3f;
//             MotionMode = MotionModeEnum.Floating; // brief pop
//         }

//         _stateTimer -= (float)dt;
//         ApplyGravityGroundAware(dt);
//         MoveAndSlide();

//         if (_stateTimer <= 0f || AnimFinished(AStagger))
//         {
//             MotionMode = _defaultMotionMode;
//             Change(State.Idle);
//         }
//     }

//     private void S_Die1(double dt)
//     {
//         if (_stateNew)
//         {
//             Velocity = Vector2.Zero;
//             SafePlay(ADeath);
//             _stateTimer = 1.0f;
//             MotionMode = MotionModeEnum.Floating;
//         }
//         _stateTimer -= (float)dt;
//         if (_stateTimer <= 0f || AnimFinished(ADeath))
//             Change(State.Die_2);
//     }

//     private void S_Die2(double dt)
//     {
//         if (_stateNew)
//         {
//             _col?.SetDeferred("disabled", true);
//             _hurtbox?.SetDeferred("monitoring", false);
//             _hitbox?.SetDeferred("monitoring", false);
//             SetPhysicsProcess(false);
//         }
//     }

//     // ---------- Damage ----------
//     public void TakeDamage(int dmg)
//     {
//         _hp -= dmg;
//         _blood?.Restart();
//         _spark?.Restart();

//         if (_hp <= 0) Change(State.Die_1);
//         else Change(State.Hurt);
//     }

//     private void OnHitBoxBodyEntered(Node2D body)
//     {
//         if (body is Player p)
//             p.ApplyHit(ContactDamage, GlobalPosition);
//     }

//     private void OnHitBoxAreaEntered(Area2D area)
//     {
//         if (!area.IsInGroup("player_hurtbox")) return;
//         if (area.GetParent() is Player p)
//             p.ApplyHit(ContactDamage, GlobalPosition);
//     }

//     private void OnMateriaTimeout() { if (_sprite != null) _sprite.SelfModulate = Colors.White; }
// }

//What happens when chat writes all of this code in the style of hollow knight 

using Godot;
using System;
using System.Collections.Generic;

public partial class Boss : CharacterBody2D {
    // ========================= Tunables =========================
    [Export] public int MaxHealth = 1000;

    [ExportGroup("Movement")]
    [Export] public float WalkSpeed = 140f;
    [Export] public float JumpVy = -700f;          // good for ~128px height at g≈1960
    [Export] public float JumpVx = 220f;           // keep positive in Inspector
    [Export] public float GravityScale = 1f;
    [Export] public float FloorSnapOnGround = 10f; // default snap when grounded

    [ExportGroup("Attacks")]
    [Export] public float SlashSlideSpeed = 360f;
    [Export] public float DashSpeed = 520f;
    [Export] public float DashStopAccel = 2500f;
    [Export] public float UppercutHoriSpeed = 140f;
    [Export] public float SlamRecover = 0.18f;

    [ExportGroup("Arena")]
    [Export] public float ArenaLeftX = 0f;
    [Export] public float ArenaRightX = 700f;

    [ExportGroup("Combat")]
    [Export] public int ContactDamage = 1;
    [Export] public float HurtKnockback = 420f;

    [ExportGroup("Node Paths")]
    [Export] public NodePath PlayerPath;

    [ExportGroup("Animation Names")]
    [Export] public string AIdle = "Idle";
    [Export] public string AFall = "Fall";
    [Export] public string ARoarPrep = "RoarPrep";
    [Export] public string ARoar = "Roar";
    [Export] public string ASlashPrep = "SlashPrep";
    [Export] public string ASlash = "Slash";
    [Export] public string AUppercutPrep = "UppercutPrep";
    [Export] public string AUppercut = "Uppercut";
    [Export] public string AMove = "Move";
    [Export] public string AJump = "Jump";
    [Export] public string ADashPrep = "DashPrep";
    [Export] public string ADash = "Dash";
    [Export] public string ADashStop = "DashStop";
    [Export] public string AStagger = "Stagger";
    [Export] public string ADeath = "Death";

    [ExportGroup("Debug")]
    [Export] public bool RunWithoutAnimations = true;
    [Export] public bool LogStateChanges = true;
    [Export] public bool WatchdogEnabled = true;
    [Export] public float WatchdogSeconds = 6.0f;
    // --- Shockwave Integration ---
    [ExportGroup("Projectiles")]
    [Export] public PackedScene ShockwaveScene;

    private Marker2D _spawnLeft;
    private Marker2D _spawnRight;
    private BossSword _sword;



    // ========================= Internals =========================
    private float _time = 0f;
    private float _watchdog = 0f;

    private float _debugTimer = 0f;
    private float _stateTimer = 0f;
    private float _decisionTimer = 0f;   // between actions
    private float _landTimer = 0f;       // coyote/debounce on landing
    private float _airTime = 0f;

    private AnimationPlayer _anim;
    private Sprite2D _sprite;
    private Node2D _spriteRoot;
    private CpuParticles2D _blood;
    private CpuParticles2D _spark;
    private Timer _matTimer;
    private Area2D _hurtbox;
    private Area2D _hitbox;
    private Player _player;
    private CollisionShape2D _col;

    private int _hp;
    private bool _stateNew = true;
    private bool _changedThisFrame = false;

    private Vector2 _worldGravity = Vector2.Zero;
    private float _defaultSnap;
    private bool _snapSuppressed = false;
    private bool _leftGround = false;
    private bool _colliderReenabledMidair = false;

    private readonly RandomNumberGenerator _rng = new();

    private Vector2 _playerPos = Vector2.Zero;

    // ========================= FSM =========================
    private enum State { Ready, ReadyDrop, Idle, Move, Choose, Prep, Slash, Dash, Uppercut, Leap, Fall, Recover, Hurt, Die_1, Die_2 }
    private State _state = State.Ready;

    // High-level “attack verb”
    private enum Attack { None, Slash, Dash, Uppercut, Leap, Roar }
    private Attack _currentAttack = Attack.None;

    // Cooldowns (seconds) and last-used timestamps
    private Dictionary<Attack, float> _cooldown = new()
    {
        { Attack.Slash,   0.8f },
        { Attack.Dash,    2.0f },
        { Attack.Uppercut,1.6f },
        { Attack.Leap,    1.1f },
        { Attack.Roar,    6.0f },
    };
    private Dictionary<Attack, float> _lastUsed = new()
    {
        { Attack.Slash,   -999f },
        { Attack.Dash,    -999f },
        { Attack.Uppercut,-999f },
        { Attack.Leap,    -999f },
        { Attack.Roar,    -999f },
    };

    // Phase (enrage below 50%)
    private bool Enraged => _hp <= MaxHealth * 0.5f;

    // ========================= Helpers =========================
    private void RefreshWorldGravity() {
        float g = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        Vector2 v = ((Vector2)ProjectSettings.GetSetting("physics/2d/default_gravity_vector")).Normalized();
        _worldGravity = v * g * Mathf.Max(0f, GravityScale);
    }

    private void ApplyGravityGroundAware(double dt) {
        if (!IsOnFloor() || Velocity.Y < 0f)
            Velocity += _worldGravity * (float)dt;
    }
    private void ApplyGravityAlways(double dt) {
        Velocity += _worldGravity * (float)dt;
    }

    private bool HasAnim(string name) =>
        _anim != null && !string.IsNullOrEmpty(name) && _anim.HasAnimation(name);

    private void SafePlay(string name) {
        if (!RunWithoutAnimations && HasAnim(name))
            _anim.Play(name);
    }

    private bool AnimDone(string name) {
        if (RunWithoutAnimations || !HasAnim(name)) return true;
        // consider finished when not playing OR switched off that clip
        return !_anim.IsPlaying() || _anim.CurrentAnimation != name;
    }

    private void FacePlayer() {
        if (_player == null || _spriteRoot == null) return;
        bool faceLeft = _playerPos.X < GlobalPosition.X;
        _spriteRoot.Scale = new Vector2(faceLeft ? 1 : -1, 1);
        _sword?.SetFacingLeft(faceLeft);
    }

    private int FacingDir() => (_spriteRoot?.Scale.X ?? 1f) == 1f ? -1 : 1; // 1=right, -1=left mapping

    private void Change(State s) {
        if (LogStateChanges && s != _state)
            GD.Print($"[Boss] {_state} → {s}");
        _state = s;
        _stateNew = true;
        _watchdog = 0f;
        _changedThisFrame = true;
    }

    private void StartDecision(float delay = 0.35f) {
        _decisionTimer = Enraged ? delay * 0.75f : delay; // faster decisions when enraged
        Change(State.Choose);
    }

    private bool OffCooldown(Attack a) => _time - _lastUsed[a] >= (_cooldown[a] * (Enraged ? 0.75f : 1f));
    private void MarkUsed(Attack a) => _lastUsed[a] = _time;

    private float DistXToPlayer() => Mathf.Abs(_playerPos.X - GlobalPosition.X);
    private float DeltaYToPlayer() => (_playerPos.Y - GlobalPosition.Y);

    private bool InsideArena() => GlobalPosition.X >= ArenaLeftX && GlobalPosition.X <= ArenaRightX;
    private void ClampArena() {
        float x = Mathf.Clamp(GlobalPosition.X, ArenaLeftX, ArenaRightX);
        GlobalPosition = new Vector2(x, GlobalPosition.Y);
    }


    // ========================= Audio =========================
    private AudioStreamPlayer2D _sfxSlash;
    private AudioStreamPlayer2D _sfxDash;
    private AudioStreamPlayer2D _sfxUppercut;
    private AudioStreamPlayer2D _sfxLeap;
    private AudioStreamPlayer2D _sfxRoar;
    private AudioStreamPlayer2D _sfxHurt;
    private AudioStreamPlayer2D _sfxDeath;

    private void PlaySFX(AudioStreamPlayer2D sfx) {
        if (sfx != null && sfx.Stream != null)
            sfx.Play();
    }

    // ========================= Lifecycle =========================
    public override void _Ready() {
        _hp = MaxHealth;
        UpDirection = Vector2.Up;

        RefreshWorldGravity();
        GD.Print($"[Boss READY] JumpVy = {JumpVy}, Gravity={_worldGravity}");

        _anim = GetNodeOrNull<AnimationPlayer>("SpriteRoot/AnimationPlayer")
             ?? GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        _sword = GetNode<BossSword>("Sword");

        _spriteRoot = GetNodeOrNull<Node2D>("SpriteRoot") ?? this;
        _sprite = GetNodeOrNull<Sprite2D>("SpriteRoot/Sprite2D");
        _blood = GetNodeOrNull<CpuParticles2D>("BloodEmitter");
        _spark = GetNodeOrNull<CpuParticles2D>("SparkEmitter");
        _matTimer = GetNodeOrNull<Timer>("MateriaTimer");

        _hurtbox = GetNodeOrNull<Area2D>("HurtBox");
        _hitbox = GetNodeOrNull<Area2D>("Hitbox");

        // --- Audio Nodes ---
        _sfxSlash = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Slash");
        _sfxDash = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Dash");
        _sfxUppercut = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Uppercut");
        _sfxLeap = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Leap");
        _sfxRoar = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Roar");
        _sfxHurt = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Hurt");
        _sfxDeath = GetNodeOrNull<AudioStreamPlayer2D>("SFX_Death");


        if (_hitbox != null) {
            _hitbox.BodyEntered += OnHitBoxBodyEntered;
            _hitbox.AreaEntered += OnHitBoxAreaEntered;
        }

        if (_matTimer != null)
            _matTimer.Timeout += OnMateriaTimeout;

        if (PlayerPath != null && !PlayerPath.IsEmpty)
            _player = GetNodeOrNull<Player>(PlayerPath);
        else {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) _player = players[0] as Player;
        }

        _rng.Randomize();
        _col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _defaultSnap = FloorSnapOnGround > 0 ? FloorSnapOnGround : FloorSnapLength;

        _spawnLeft = GetNodeOrNull<Marker2D>("ShockwaveSpawnLeft");
        _spawnRight = GetNodeOrNull<Marker2D>("ShockwaveSpawnRight");

        // DEBUG: Force spawn once on start
        GD.Print("[Boss TEST] Forcing initial shockwave spawn for verification");
        SpawnShockwaves();

        AddToGroup("boss");
    }

    public override void _PhysicsProcess(double delta) {
        _changedThisFrame = false;
        _time += (float)delta;

        if (_player != null) _playerPos = _player.GlobalPosition;

        if (WatchdogEnabled) {
            bool transient = _state is State.Prep or State.Slash or State.Dash or State.Uppercut or State.Leap or State.Fall or State.Recover;
            if (!transient) {
                _watchdog += (float)delta;
                if (_watchdog > WatchdogSeconds) {
                    GD.PushWarning($"[Boss] Watchdog in state: {_state}. Forcing Idle.");
                    Velocity = Vector2.Zero;
                    Change(State.Idle);
                }
            }
        }

        switch (_state) {
            case State.Ready: S_Ready(delta); break;
            case State.ReadyDrop: S_ReadyDrop(delta); break;
            case State.Idle: S_Idle(delta); break;
            case State.Move: S_Move(delta); break;
            case State.Choose: S_Choose(delta); break;
            case State.Prep: S_Prep(delta); break;
            case State.Slash: S_Slash(delta); break;
            case State.Dash: S_Dash(delta); break;
            case State.Uppercut: S_Uppercut(delta); break;
            case State.Leap: S_Leap(delta); break;
            case State.Fall: S_Fall(delta); break;
            case State.Recover: S_Recover(delta); break;
            case State.Hurt: S_Hurt(delta); break;
            case State.Die_1: S_Die1(delta); break;
            case State.Die_2: S_Die2(delta); break;
        }

        if (!_changedThisFrame) _stateNew = false;

        // Debug print (throttled)
        _debugTimer -= (float)delta;
        if (_debugTimer <= 0f) {
            GD.Print($"[Boss] State={_state} Attack={_currentAttack} Pos={GlobalPosition} Vel={Velocity} OnFloor={IsOnFloor()} Grav={_worldGravity}");
            _debugTimer = 0.25f;
        }

        // keep inside arena
        ClampArena();
    }

    // ================================================
    //  Spawn shockwaves on ground slam
    // ================================================
    private void SpawnShockwaves() {
        GD.Print("[Boss DEBUG] SpawnShockwaves() called");

        if (ShockwaveScene == null) {
            GD.PrintErr("[Boss ERROR] ShockwaveScene not assigned! Please drag Shockwave.tscn into the Boss Inspector.");
            return;
        }

        // Verify spawn markers
        if (_spawnLeft == null || _spawnRight == null) {
            GD.PrintErr("[Boss WARNING] Spawn markers not found! Using default positions.");
        }

        Vector2 leftPos = _spawnLeft?.GlobalPosition ?? (GlobalPosition + new Vector2(-150, 0));
        Vector2 rightPos = _spawnRight?.GlobalPosition ?? (GlobalPosition + new Vector2(150, 0));

        // Instantiate shockwaves
        Node2D waveL = ShockwaveScene.Instantiate<Node2D>();
        Node2D waveR = ShockwaveScene.Instantiate<Node2D>();

        if (waveL == null || waveR == null) {
            GD.PrintErr("[Boss ERROR] Failed to instantiate ShockwaveScene!");
            return;
        }

        // Assign positions immediately
        waveL.GlobalPosition = leftPos;
        waveR.GlobalPosition = rightPos;

        // Add safely (deferred)
        GetTree().CurrentScene.CallDeferred("add_child", waveL);
        GetTree().CurrentScene.CallDeferred("add_child", waveR);

        // Initialize waves after they're added
        waveL.CallDeferred("Setup", -1);
        waveR.CallDeferred("Setup", 1);

        // Ensure visible
        waveL.Visible = true;
        waveR.Visible = true;

        GD.Print($"[Boss DEBUG] Deferred shockwave spawn at {leftPos} and {rightPos}");
    }



    // ========================= States =========================
    private void S_Ready(double dt) {
        if (_stateNew) {
            SafePlay(AIdle);
            _stateTimer = 0.5f;
        }
        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f) Change(State.ReadyDrop);
    }

    private void S_ReadyDrop(double dt) {
        if (_stateNew) SafePlay(AFall);
        ApplyGravityGroundAware(dt);
        MoveAndSlide();
        if (IsOnFloor()) Change(State.Idle);
    }

    private void S_Idle(double dt) {
        if (_stateNew) {
            FacePlayer();
            SafePlay(AIdle);
            _decisionTimer = 0.35f;
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 4000f * (float)dt), 0);
            FloorSnapLength = _defaultSnap;
            _snapSuppressed = false;
        }

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _decisionTimer -= (float)dt;
        if (_decisionTimer <= 0f)
            StartDecision(0.2f);
    }

    private void S_Move(double dt) {
        if (_stateNew) {
            FacePlayer();
            SafePlay(AMove);
            _stateTimer = 0.9f;
        }

        float dir = _playerPos.X > GlobalPosition.X ? 1f : -1f;
        float targetX = dir * WalkSpeed * (Enraged ? 1.15f : 1f);
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, targetX, 2500f * (float)dt), Velocity.Y);
        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f) StartDecision();
    }

    // Weighted, distance-aware chooser with cooldowns
    private void S_Choose(double dt) {
        if (_stateNew) {
            FacePlayer();
        }

        _decisionTimer -= (float)dt;
        if (_decisionTimer > 0f) return;

        float dist = DistXToPlayer();
        float dy = DeltaYToPlayer();

        var candidates = new List<(Attack atk, float weight)>();

        // baseline weights
        float wSlash = 1.0f;
        float wDash = 1.0f;
        float wUpper = 0.7f;
        float wLeap = 0.9f;
        float wRoar = 0.25f;

        // distance shaping (HK-style logic)
        if (dist < 160f) { wSlash += 2.2f; wUpper += dy < -120f ? 1.6f : 0f; }
        else if (dist < 320f) { wLeap += 1.2f; }
        else { wDash += 2.0f; wLeap += 0.6f; }

        // vertical shaping
        if (dy < -140f) wUpper += 1.8f; // player above → more uppercuts
        if (dy > 120f) wLeap += 0.8f;   // player below → more leaps/slams

        // Enrage: speed up patterns + reduce roar frequency slightly
        if (Enraged) { wDash *= 1.2f; wSlash *= 1.15f; wRoar *= 0.7f; }

        // Cooldown gates
        void addIfReady(Attack a, float w) { if (OffCooldown(a) && w > 0.05f) candidates.Add((a, w)); }
        addIfReady(Attack.Slash, wSlash);
        addIfReady(Attack.Dash, wDash);
        addIfReady(Attack.Uppercut, wUpper);
        addIfReady(Attack.Leap, wLeap);
        addIfReady(Attack.Roar, wRoar);

        if (candidates.Count == 0) {
            // fallback movement shuffle
            Change(State.Move);
            return;
        }

        // weighted pick
        float total = 0f;
        foreach (var c in candidates) total += c.weight;
        float r = _rng.Randf() * total;
        Attack pick = Attack.Slash;
        foreach (var c in candidates) {
            if ((r -= c.weight) <= 0f) { pick = c.atk; break; }
        }

        _currentAttack = pick;
        MarkUsed(pick);
        Change(State.Prep);
    }

    // Common windup/telegraph
    private void S_Prep(double dt) {
        if (_stateNew) {
            FacePlayer();
            float prep = 0.22f;
            switch (_currentAttack) {
                case Attack.Slash:
                    SafePlay(ASlashPrep);
                    prep = 0.24f;
                    break;
                case Attack.Dash:
                    SafePlay(ADashPrep);
                    prep = 0.26f;
                    break;
                case Attack.Uppercut:
                    SafePlay(AUppercutPrep);
                    prep = 0.25f;
                    break;
                case Attack.Leap:
                    SafePlay(AJump);
                    prep = 0.18f;
                    break;
                case Attack.Roar:
                    SafePlay(ARoarPrep);
                    PlaySFX(_sfxRoar);
                    ShakeCamera(0.6f, Enraged ? 10f : 7f);
                    prep = 0.35f;
                    break;
            }
            if (Enraged) prep *= 0.85f;
            _stateTimer = prep;
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 4000f * (float)dt), Velocity.Y);
        }

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer > 0f) return;

        // Transition to the active part
        switch (_currentAttack) {
            case Attack.Slash: Change(State.Slash); break;
            case Attack.Dash: Change(State.Dash); break;
            case Attack.Uppercut: Change(State.Uppercut); break;
            case Attack.Leap: Change(State.Leap); break;
            case Attack.Roar:
                SafePlay(ARoar);
                _stateTimer = Enraged ? 0.5f : 0.7f;
                // Roar is just a time lock; after it, choose again.
                Change(State.Recover);
                break;
        }
    }

    // Short HK-style sliding slash
    private void S_Slash(double dt) {
        if (_stateNew) {
            SafePlay(ASlash);
            PlaySFX(_sfxSlash);
            float speed = SlashSlideSpeed * (Enraged ? 1.15f : 1f);
            int dir = FacingDir();
            Velocity = new Vector2(dir * speed, 0);
            _sword.EnableHitbox();
            _sword.HitCheck();
            _stateTimer = 0.18f * (Enraged ? 1.05f : 1f); // active slide window
        }

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f) {
            _sword.DisableHitbox();
            Change(State.Recover);
            _stateTimer = 0.18f;
        }
    }

    // Long ground dash, stops at arena edge
    private void S_Dash(double dt) {
        if (_stateNew) {
            SafePlay(ADash);
            PlaySFX(_sfxDash);
            int dir = FacingDir();
            float speed = DashSpeed * (Enraged ? 1.15f : 1f);
            Velocity = new Vector2(dir * speed, 0);
            _stateTimer = 0.75f; // max dash time
        }

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        bool hitLeft = GlobalPosition.X <= ArenaLeftX + 2f;
        bool hitRight = GlobalPosition.X >= ArenaRightX - 2f;
        _stateTimer -= (float)dt;

        if (hitLeft || hitRight || _stateTimer <= 0f) {
            SafePlay(ADashStop);
            Change(State.Recover);
            _stateTimer = 0.25f;
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, DashStopAccel * (float)dt), Velocity.Y);
        }
    }

    // Vertical pop with slight homing
    private void S_Uppercut(double dt) {
        if (_stateNew) {
            SafePlay(AUppercut);
            PlaySFX(_sfxUppercut);
            int dir = _playerPos.X >= GlobalPosition.X ? 1 : -1;
            float vx = UppercutHoriSpeed * (Enraged ? 1.15f : 1f);
            float vy = JumpVy * 1.1f;
            FloorSnapLength = 0f; _snapSuppressed = true;
            _leftGround = false; _airTime = 0f; _colliderReenabledMidair = false;
            if (_col != null) _col.Disabled = true;

            Velocity = new Vector2(dir * vx, vy);
        }

        ApplyGravityAlways(dt);
        MoveAndSlide();

        if (!IsOnFloor()) _airTime += (float)dt;
        if (!_leftGround && !IsOnFloor()) _leftGround = true;

        if (_leftGround && !_colliderReenabledMidair && (_airTime > 0.06f || Velocity.Y > 0f)) {
            _col?.SetDeferred("disabled", false);
            _colliderReenabledMidair = true;
        }

        if (!IsOnFloor() && Velocity.Y > 0f)
            Change(State.Fall);

        if (_colliderReenabledMidair && IsOnFloor()) {
            SpawnShockwaves(); // <<< NEW
            Change(State.Recover);
            _stateTimer = SlamRecover;
            FloorSnapLength = _defaultSnap;
            _snapSuppressed = false;
            Velocity = Vector2.Zero;
        }

    }

    // A forward leap that becomes a fall/slam
    private void S_Leap(double dt) {
        if (_stateNew) {
            SafePlay(AJump);
            PlaySFX(_sfxLeap);
            FloorSnapLength = 0f; _snapSuppressed = true;
            _leftGround = false; _airTime = 0f; _colliderReenabledMidair = false;

            if (_col != null) _col.Disabled = true;

            int dir = _playerPos.X >= GlobalPosition.X ? 1 : -1;
            float vx = Mathf.Abs(JumpVx) * (Enraged ? 1.1f : 1f);
            Velocity = new Vector2(dir * vx, JumpVy);
            GlobalPosition += new Vector2(0, -2f); // anti-stuck lift
        }

        ApplyGravityAlways(dt);
        MoveAndSlide();

        if (!IsOnFloor()) _airTime += (float)dt;
        if (!_leftGround && !IsOnFloor()) _leftGround = true;

        if (_leftGround && !_colliderReenabledMidair && (_airTime > 0.06f || Velocity.Y > 0f)) {
            _col?.SetDeferred("disabled", false);
            _colliderReenabledMidair = true;
        }

        if (!IsOnFloor() && Velocity.Y > 0f)
            Change(State.Fall);

        if (_colliderReenabledMidair && IsOnFloor()) {
            SpawnShockwaves(); // <<< NEW
            Change(State.Recover);
            _stateTimer = SlamRecover;
            FloorSnapLength = _defaultSnap;
            _snapSuppressed = false;
            Velocity = Vector2.Zero;
        }

    }

    // Generic airborne fall; lands into Recover
    private void S_Fall(double dt) {
        if (_stateNew) {
            SafePlay(AFall);
            _landTimer = 0.08f;
            if (_col != null && _col.Disabled) _col.SetDeferred("disabled", false);
            _colliderReenabledMidair = true;
        }

        ApplyGravityAlways(dt);
        MoveAndSlide();

        if (IsOnFloor()) {
            _landTimer -= (float)dt;
            if (_landTimer <= 0f) {
                // Restore normal snap
                FloorSnapLength = _defaultSnap;
                _snapSuppressed = false;

                // >>> Spawn shockwaves on impact if coming from Leap/Uppercut
                if (_currentAttack == Attack.Leap || _currentAttack == Attack.Uppercut) {
                    GD.Print("[Boss DEBUG] Landed after Leap/Uppercut → spawning shockwaves");
                    SpawnShockwaves();
                }

                // Then go to recovery
                Change(State.Recover);
                _stateTimer = SlamRecover;
                Velocity = Vector2.Zero;
            }
        }
    }


    // Brief post-attack delay → Choose next
    private void S_Recover(double dt) {
        if (_stateNew) {
            // keep played anim; just time out
            if (_stateTimer <= 0f) _stateTimer = 0.2f;
        }

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f)
            StartDecision(Enraged ? 0.15f : 0.25f);
    }

    private void S_Hurt(double dt) {
        if (_stateNew) {
            SafePlay(AStagger);
            PlaySFX(_sfxHurt);
            int dir = _player != null && _playerPos.X < GlobalPosition.X ? 1 : -1;
            Velocity = new Vector2(HurtKnockback * dir, -Mathf.Abs(JumpVy) * 0.25f);
            _stateTimer = 0.25f;
            FloorSnapLength = 0f;
        }

        ApplyGravityAlways(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f || AnimDone(AStagger)) {
            FloorSnapLength = _defaultSnap;
            Change(State.Recover);
            _stateTimer = 0.25f;
        }
    }

    private void S_Die1(double dt) {
        if (_stateNew) {
            Velocity = Vector2.Zero;
            SafePlay(ADeath);
            PlaySFX(_sfxDeath);
            _stateTimer = 1.0f;
        }
        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f || AnimDone(ADeath))
            Change(State.Die_2);
    }

    private void S_Die2(double dt) {
        if (_stateNew) {
            _col?.SetDeferred("disabled", true);
            _hurtbox?.SetDeferred("monitoring", false);
            _hitbox?.SetDeferred("monitoring", false);
            SetPhysicsProcess(false);
        }
    }

    // ========================= Damage & Hitboxes =========================
    public void TakeDamage(int dmg) {
        if (_state is State.Die_1 or State.Die_2) return;

        _hp -= dmg;
        _blood?.Restart();
        _spark?.Restart();

        if (_hp <= 0) Change(State.Die_1);
        else Change(State.Hurt);
    }

    private void OnHitBoxBodyEntered(Node2D body) {
        if (body is Player p)
            p.ApplyHit(ContactDamage, GlobalPosition);
    }

    private void OnHitBoxAreaEntered(Area2D area) {
        if (!area.IsInGroup("player_hurtbox")) return;
        if (area.GetParent() is Player p)
            p.ApplyHit(ContactDamage, GlobalPosition);
    }

    private void OnMateriaTimeout() => _sprite?.SetDeferred("self_modulate", Colors.White);
    private void ShakeCamera(float duration = 0.4f, float strength = 6f) {
        var camera = GetTree().GetFirstNodeInGroup("camera") as Node;
        if (camera is CameraController cam)
            cam.Shake(duration, strength);
    }
    // ========================= Blood Helpers =========================
    private void EmitDirectionalBlood(Vector2 hitSource) {
        if (_blood == null) return;

        // Compute direction away from hit source
        Vector2 dir = (GlobalPosition - hitSource).Normalized();
        dir = dir.Rotated(_rng.RandfRange(-0.2f, 0.2f)); // slight randomization

        // Give the emitter a short "kick" rotation
        _blood.Rotation = dir.Angle();
        _blood.Emitting = false; // reset first to retrigger properly
        _blood.Restart();
    }

}
