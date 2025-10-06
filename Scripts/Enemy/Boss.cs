using Godot;
using System;

public partial class Boss : CharacterBody2D
{
    // ---------- Tunables ----------
    [Export] public int MaxHealth = 1000;
    [Export] public float WalkSpeed = 120f;
    [Export] public float JumpVy = -200f;
    [Export] public float JumpVx = 220f; // keep positive in Inspector
    [Export] public float SlashSlideSpeed = 320f;
    [Export] public float UppercutHoriSpeed = 100f;
    [Export] public float DashSpeed = 420f;
    [Export] public float DashStopAccel = 2500f;
    [Export] public float HurtKnockback = 420f;

    [Export] public float ArenaLeftX = 0f;
    [Export] public float ArenaRightX = 700f;
    [Export] public int ContactDamage = 1;
    [Export] public NodePath PlayerPath;

    // Optional gravity scale (multiplies world gravity)
    [Export] public float GravityScale = 1f;

    // ---------- Animation names ----------
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

    [Export] public bool RunWithoutAnimations = true;
    [Export] public bool WatchdogEnabled = true;
    [Export] public float WatchdogSeconds = 6.0f;
    [Export] public bool LogStateChanges = true;

    private float _watchdog = 0f;

    // ---------- Internal timers ----------
    private float _debugTimer = 0f;
    private float _readyTimer = 0f;
    private float _idleDecisionTimer = 0f;
    private float _stateTimer = 0f;
    private float _landTimer = 0f;
    private float _airTime = 0f;
    private bool _colliderReenabledMidair = false;

    // ---------- Node references ----------
    private AnimationPlayer _anim;
    private Sprite2D _sprite;
    private Node2D _spriteRoot;
    private CpuParticles2D _blood;
    private CpuParticles2D _spark;
    private Timer _matTimer;
    private Area2D _hurtbox;
    private Area2D _hitbox;
    private Player _player;

    // ---------- FSM ----------
    private enum State { Ready, ReadyDrop, RoarPrep, Roar, Idle, Move, Jump, Fall, Dash, DashStop, Hurt, Die_1, Die_2 }

    private State _state = State.Ready;
    private bool _stateNew = true;
    private bool _changedThisFrame = false; // <<< NEW
    private Vector2 _playerPos = Vector2.Zero;
    private int _hp;
    private readonly RandomNumberGenerator _rng = new();

    // ---------- Jump / detach ----------
    private CollisionShape2D _col;
    private float _defaultSnap = 10f;
    private int _detachFrames = 0;
    private bool _snapSuppressed = false;
    private bool _leftGround = false; // gate landing until truly airborne

    // Motion mode (Grounded <-> Floating)
    private MotionModeEnum _defaultMotionMode = MotionModeEnum.Grounded;

    // ---------- Gravity ----------
    private Vector2 _worldGravity = Vector2.Zero;

    private void RefreshWorldGravity()
    {
        float g = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        Vector2 v = ((Vector2)ProjectSettings.GetSetting("physics/2d/default_gravity_vector")).Normalized();
        _worldGravity = v * g * Mathf.Max(0f, GravityScale);
    }

    private void ApplyGravityGroundAware(double dt)
    {
        if (!IsOnFloor() || Velocity.Y < 0f)
            Velocity += _worldGravity * (float)dt;
    }

    private void ApplyGravityAlways(double dt)
    {
        Velocity += _worldGravity * (float)dt;
    }

    private bool HasAnim(string name) =>
        _anim != null && !string.IsNullOrEmpty(name) && _anim.HasAnimation(name);

    private void SafePlay(string name)
    {
        if (!RunWithoutAnimations && HasAnim(name))
            _anim.Play(name);
    }

    private bool AnimFinished(string name)
    {
        if (RunWithoutAnimations || !HasAnim(name)) return true;
        return !_anim.IsPlaying() && _anim.CurrentAnimation == name;
    }

    public override void _Ready()
    {
        _hp = MaxHealth;
        UpDirection = Vector2.Up;

        RefreshWorldGravity();
        GD.Print($"[Boss READY] JumpVy = {JumpVy}, Gravity={_worldGravity}");

        _anim = GetNodeOrNull<AnimationPlayer>("SpriteRoot/AnimationPlayer")
             ?? GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        _spriteRoot = GetNodeOrNull<Node2D>("SpriteRoot") ?? this;
        _sprite = GetNodeOrNull<Sprite2D>("SpriteRoot/Sprite2D");
        _blood = GetNodeOrNull<CpuParticles2D>("BloodEmitter");
        _spark = GetNodeOrNull<CpuParticles2D>("SparkEmitter");
        _matTimer = GetNodeOrNull<Timer>("MateriaTimer");

        _hurtbox = GetNodeOrNull<Area2D>("HurtBox");
        _hitbox = GetNodeOrNull<Area2D>("Hitbox");

        if (_hitbox != null)
        {
            _hitbox.BodyEntered += OnHitBoxBodyEntered;
            _hitbox.AreaEntered += OnHitBoxAreaEntered;
        }

        if (_matTimer != null)
            _matTimer.Timeout += OnMateriaTimeout;

        if (PlayerPath != null && !PlayerPath.IsEmpty)
            _player = GetNodeOrNull<Player>(PlayerPath);
        else
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) _player = players[0] as Player;
        }

        _rng.Randomize();

        _col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _defaultSnap = FloorSnapLength;

        _defaultMotionMode = MotionMode; // remember editor setting
    }

    public override void _PhysicsProcess(double delta)
    {
        _changedThisFrame = false; // <<< reset at frame start

        if (_player != null) _playerPos = _player.GlobalPosition;

        if (_detachFrames > 0)
        {
            FloorSnapLength = 0f;
            _snapSuppressed = true;
            _col?.SetDeferred("disabled", true);
            _detachFrames--;
        }
        // DO NOT auto re-enable here; Jump/Fall restore on landing

        if (WatchdogEnabled)
        {
            bool transient = _state is State.Jump or State.Fall or State.Dash or State.DashStop;
            if (!transient)
            {
                _watchdog += (float)delta;
                if (_watchdog > WatchdogSeconds)
                {
                    GD.PushWarning($"[Boss] Watchdog tripped in state: {_state}. Forcing Idle.");
                    Velocity = Vector2.Zero;
                    Change(State.Idle);
                }
            }
        }

        switch (_state)
        {
            case State.Ready: S_Ready(delta); break;
            case State.ReadyDrop: S_ReadyDrop(delta); break;
            case State.RoarPrep: S_RoarPrep(delta); break;
            case State.Roar: S_Roar(delta); break;
            case State.Idle: S_Idle(delta); break;
            case State.Move: S_Move(delta); break;
            case State.Jump: S_Jump(delta); break;
            case State.Fall: S_Fall(delta); break;
            case State.Dash: S_Dash(delta); break;
            case State.DashStop: S_DashStop(delta); break;
            case State.Hurt: S_Hurt(delta); break;
            case State.Die_1: S_Die1(delta); break;
            case State.Die_2: S_Die2(delta); break;
        }

        // <<< only clear _stateNew if no Change() happened this frame
        if (!_changedThisFrame) _stateNew = false;

        _debugTimer -= (float)delta;
        if (_debugTimer <= 0f)
        {
            GD.Print($"[Boss] State={_state} Pos={GlobalPosition} Vel={Velocity} OnFloor={IsOnFloor()} Grav={_worldGravity}");
            _debugTimer = 0.25f;
        }
    }

    private void Change(State s)
    {
        if (LogStateChanges && s != _state)
            GD.Print($"[Boss] {_state} → {s}");
        _state = s;
        _stateNew = true;
        _watchdog = 0f;
        _changedThisFrame = true; // <<< mark so _stateNew survives to next frame
    }

    // ---------- Helpers ----------
    private void FacePlayer()
    {
        if (_player == null || _spriteRoot == null) return;
        bool faceLeft = _playerPos.X < GlobalPosition.X;
        _spriteRoot.Scale = new Vector2(faceLeft ? 1 : -1, 1);
    }

    // ---------- States ----------
    private void S_Ready(double dt)
    {
        if (_stateNew) { FacePlayer(); SafePlay(AIdle); _readyTimer = 0.5f; }
        _readyTimer -= (float)dt;
        if (_readyTimer <= 0f) Change(State.ReadyDrop);
    }

    private void S_ReadyDrop(double dt)
    {
        if (_stateNew) SafePlay(AFall);
        ApplyGravityGroundAware(dt);
        MoveAndSlide();
        if (IsOnFloor()) Change(State.Idle);
    }

    private void S_RoarPrep(double dt)
    {
        if (_stateNew) { SafePlay(ARoarPrep); _stateTimer = 0.35f; }
        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f || AnimFinished(ARoarPrep)) Change(State.Roar);
    }

    private void S_Roar(double dt)
    {
        if (_stateNew) { SafePlay(ARoar); _stateTimer = 0.6f; }
        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f || AnimFinished(ARoar)) Change(State.Idle);
    }

    private void S_Idle(double dt)
    {
        if (_stateNew)
        {
            FacePlayer();
            SafePlay(AIdle);
            _idleDecisionTimer = 0.5f;
            MotionMode = _defaultMotionMode; // ensure grounded
        }

        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 2000f * (float)dt), Velocity.Y);
        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _idleDecisionTimer -= (float)dt;
        if (_idleDecisionTimer <= 0f && IsOnFloor())
            Change(_rng.Randf() > 0.5f ? State.Move : State.Jump);
    }

    private void S_Move(double dt)
    {
        if (_stateNew)
        {
            FacePlayer();
            SafePlay(AMove);
            _stateTimer = 1.5f;
            MotionMode = _defaultMotionMode; // walking uses grounded
        }

        float dir = _playerPos.X > GlobalPosition.X ? 1f : -1f;
        float target = dir * WalkSpeed;
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, target, 2500f * (float)dt), Velocity.Y);

        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f) Change(State.Idle);
    }

    private void S_Jump(double dt)
    {
        if (_stateNew)
        {
            _defaultSnap = FloorSnapLength;
            FloorSnapLength = 0f;         // detach without switching modes
            _snapSuppressed = true;
            _detachFrames = 10;
            _leftGround = false;
            _airTime = 0f;
            _colliderReenabledMidair = false;

            _col ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (_col != null) _col.Disabled = true;   // briefly disable so we don't re-stick the same frame

            GlobalPosition += new Vector2(0, -2f);    // tiny lift

            float dir = Mathf.Sign(_playerPos.X - GlobalPosition.X);
            if (Mathf.IsZeroApprox(dir)) dir = 1f;
            float vx = Mathf.Abs(JumpVx);

            Velocity = new Vector2(dir * vx, JumpVy);
            GD.Print($"[Boss JUMP INIT] Velocity={Velocity}");

            SafePlay(AJump);
            return; // integrate next frame
        }

        // Airborne integration
        ApplyGravityAlways(dt);
        MoveAndSlide();

        // Track leaving ground
        if (!IsOnFloor()) _airTime += (float)dt;
        if (!_leftGround && !IsOnFloor()) _leftGround = true;

        // Re-enable collider midair so floor can be detected later
        if (_leftGround && !_colliderReenabledMidair && (_airTime > 0.06f || Velocity.Y > 0f))
        {
            _col?.SetDeferred("disabled", false);
            _colliderReenabledMidair = true;
        }

        // Rising -> Falling
        if (!IsOnFloor() && Velocity.Y > 0f)
            Change(State.Fall);

        // Land early (very low jump)
        if (_colliderReenabledMidair && IsOnFloor())
        {
            FloorSnapLength = _defaultSnap;
            _snapSuppressed = false;
            _detachFrames = 0;
            Change(State.Idle);
        }
    }

    private void S_Fall(double dt)
    {
        if (_stateNew)
        {
            SafePlay(AFall);
            _landTimer = 0.08f; // small debounce

            // ensure collider ON during the fall so we can detect floor
            if (_col != null && _col.Disabled)
                _col.SetDeferred("disabled", false);
            _colliderReenabledMidair = true;
        }

        ApplyGravityAlways(dt);
        MoveAndSlide();

        if (IsOnFloor())
        {
            _landTimer -= (float)dt;
            if (_landTimer <= 0f)
            {
                FloorSnapLength = _defaultSnap; // restore snapping
                _snapSuppressed = false;
                _detachFrames = 0;
                Change(State.Idle);
            }
        }
    }

    private void S_Dash(double dt)
    {
        if (_stateNew)
        {
            float dir = (_spriteRoot?.Scale.X ?? 1f) == 1f ? -1f : 1f;
            Velocity = new Vector2(dir * DashSpeed, 0);
            SafePlay(ADash);
            MotionMode = _defaultMotionMode; // ground dash
        }
        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        bool hitLeft = GlobalPosition.X <= ArenaLeftX + 2f;
        bool hitRight = GlobalPosition.X >= ArenaRightX - 2f;
        if (hitLeft || hitRight) Change(State.DashStop);
    }

    private void S_DashStop(double dt)
    {
        if (_stateNew) { SafePlay(ADashStop); _stateTimer = 0.25f; }
        _stateTimer -= (float)dt;

        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, DashStopAccel * (float)dt), Velocity.Y);
        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        if (_stateTimer <= 0f || AnimFinished(ADashStop)) Change(State.Idle);
    }

    private void S_Hurt(double dt)
    {
        if (_stateNew)
        {
            SafePlay(AStagger);
            float dir = _player != null && _playerPos.X < GlobalPosition.X ? 1f : -1f;
            Velocity = new Vector2(HurtKnockback * dir, -Mathf.Abs(JumpVy) * 0.2f);
            _stateTimer = 0.3f;
            MotionMode = MotionModeEnum.Floating; // brief pop
        }

        _stateTimer -= (float)dt;
        ApplyGravityGroundAware(dt);
        MoveAndSlide();

        if (_stateTimer <= 0f || AnimFinished(AStagger))
        {
            MotionMode = _defaultMotionMode;
            Change(State.Idle);
        }
    }

    private void S_Die1(double dt)
    {
        if (_stateNew)
        {
            Velocity = Vector2.Zero;
            SafePlay(ADeath);
            _stateTimer = 1.0f;
            MotionMode = MotionModeEnum.Floating;
        }
        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f || AnimFinished(ADeath))
            Change(State.Die_2);
    }

    private void S_Die2(double dt)
    {
        if (_stateNew)
        {
            _col?.SetDeferred("disabled", true);
            _hurtbox?.SetDeferred("monitoring", false);
            _hitbox?.SetDeferred("monitoring", false);
            SetPhysicsProcess(false);
        }
    }

    // ---------- Damage ----------
    public void TakeDamage(int dmg)
    {
        _hp -= dmg;
        _blood?.Restart();
        _spark?.Restart();

        if (_hp <= 0) Change(State.Die_1);
        else Change(State.Hurt);
    }

    private void OnHitBoxBodyEntered(Node2D body)
    {
        if (body is Player p)
            p.ApplyHit(ContactDamage, GlobalPosition);
    }

    private void OnHitBoxAreaEntered(Area2D area)
    {
        if (!area.IsInGroup("player_hurtbox")) return;
        if (area.GetParent() is Player p)
            p.ApplyHit(ContactDamage, GlobalPosition);
    }

    private void OnMateriaTimeout() { if (_sprite != null) _sprite.SelfModulate = Colors.White; }
}
