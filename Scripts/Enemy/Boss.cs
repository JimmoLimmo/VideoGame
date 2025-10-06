using Godot;
using System;

public partial class Boss : CharacterBody2D
{
    // ---------- Tunables ----------
    [Export] public int MaxHealth = 1000;
    [Export] public float WalkSpeed = 120f;
    [Export] public float JumpVy = -900f;
    [Export] public float JumpVx = 220f;
    [Export] public float SlashSlideSpeed = 320f;
    [Export] public float UppercutHoriSpeed = 100f;
    [Export] public float DashSpeed = 420f;
    [Export] public float DashStopAccel = 2500f;
    [Export] public float HurtKnockback = 420f;

    [Export] public float ArenaLeftX = 0f;
    [Export] public float ArenaRightX = 700f;
    [Export] public int ContactDamage = 1;
    [Export] public NodePath PlayerPath;

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
    [Export] public float WatchdogSeconds = 6.0f; // increased
    private float _watchdog = 0f;

    // ---------- Internal timers ----------
    private float _debugTimer = 0f;
    private float _readyTimer = 0f;
    private float _idleDecisionTimer = 0f;
    private float _stateTimer = 0f;

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
    private enum State {
        Ready, ReadyDrop, RoarPrep, Roar, Idle,
        Move, Jump, Fall, Dash, DashStop,
        Hurt, Die_1, Die_2
    }

    private State _state = State.Ready;
    private bool _stateNew = true;
    private Vector2 _playerPos = Vector2.Zero;
    private int _hp;
    private readonly RandomNumberGenerator _rng = new();

    // ---------- Jump detach fix ----------
    private CollisionShape2D _col;
    private float _defaultSnap = 10f;
    private int _detachFrames = 0;
    private bool _snapSuppressed = false;

    private Vector2 GetBossGravity() => GetGravity();

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
        // fixed condition
        return !_anim.IsPlaying() || _anim.CurrentAnimation != name;
    }

    public override void _Ready()
    {
        _hp = MaxHealth;
        UpDirection = Vector2.Up;
        GD.Print($"[Boss READY] JumpVy = {JumpVy}");

        _anim = GetNodeOrNull<AnimationPlayer>("SpriteRoot/AnimationPlayer");
        _spriteRoot = GetNode<Node2D>("SpriteRoot");
        _sprite = GetNode<Sprite2D>("SpriteRoot/Sprite2D");
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
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player != null) _playerPos = _player.GlobalPosition;

        // Hard detach
        if (_detachFrames > 0)
        {
            FloorSnapLength = 0f;
            _snapSuppressed = true;
            if (_col != null) _col.Disabled = true;
            _detachFrames--;
        }
        else if (_snapSuppressed)
        {
            if (_col != null) _col.Disabled = false;
            FloorSnapLength = _defaultSnap;
            _snapSuppressed = false;
        }

        if (WatchdogEnabled)
        {
            _watchdog += (float)delta;
            if (_watchdog > WatchdogSeconds)
            {
                GD.PushWarning($"[Boss] Watchdog tripped in state: {_state}. Forcing Idle.");
                Velocity = Vector2.Zero;
                Change(State.Idle);
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
        }

        _stateNew = false;

        _debugTimer -= (float)delta;
        if (_debugTimer <= 0f)
        {
            GD.Print($"[Boss] State={_state} Pos={GlobalPosition} Vel={Velocity} OnFloor={IsOnFloor()}");
            _debugTimer = 0.25f;
        }
    }

    [Export] public bool LogStateChanges = true;
    private void Change(State s)
    {
        if (LogStateChanges && s != _state)
            GD.Print($"[Boss] {_state} → {s}");
        _state = s;
        _stateNew = true;
        _watchdog = 0f;
    }

    // ---------- Physics helpers ----------
    private void ApplyGravity(double dt)
    {
        if (!IsOnFloor() || Velocity.Y < 0f)
            Velocity += GetBossGravity() * (float)dt;
    }

    private void FacePlayer()
    {
        if (_player == null) return;
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
        ApplyGravity(dt);
        MoveAndSlide();
        if (IsOnFloor()) Change(State.Idle);
    }

    private void S_RoarPrep(double dt)
    {
        if (_stateNew) SafePlay(ARoarPrep);
        if (AnimFinished(ARoarPrep)) Change(State.Roar);
    }

    private void S_Roar(double dt)
    {
        if (_stateNew) SafePlay(ARoar);
        if (AnimFinished(ARoar)) Change(State.Idle);
    }

    private void S_Idle(double dt)
    {
        if (_stateNew)
        {
            SafePlay(AIdle);
            _idleDecisionTimer = 0.5f;
        }

        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 2000f * (float)dt), Velocity.Y);
        ApplyGravity(dt);
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
        }

        float dir = _playerPos.X > GlobalPosition.X ? 1f : -1f;
        float target = dir * WalkSpeed;
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, target, 2500f * (float)dt), Velocity.Y);

        ApplyGravity(dt);
        MoveAndSlide();

        _stateTimer -= (float)dt;
        if (_stateTimer <= 0f) Change(State.Idle);
    }

    // ---------- Jump + Fall (fixed) ----------
private void S_Jump(double dt)
{
    if (_stateNew)
    {
        // === setup ===
        _defaultSnap = FloorSnapLength;
        FloorSnapLength = 0f;
        _snapSuppressed = true;
        _detachFrames = 10;

        // ensure we're not still flagged "on floor"
        MoveAndSlide(); // run one frame of movement to clear contact

        // temporarily disable collider so floor won't instantly reattach
        if (_col == null)
            _col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_col != null)
            _col.Disabled = true;

        // small lift
        GlobalPosition += new Vector2(0, -2f);

        // determine jump direction
        float dir = Mathf.Sign(_playerPos.X - GlobalPosition.X);
        if (Mathf.IsZeroApprox(dir)) dir = 1f;

        // apply launch velocity
        Velocity = new Vector2(dir * JumpVx, JumpVy);
        GD.Print($"[Boss JUMP INIT] Velocity={Velocity}");

        SafePlay(AJump);
    }

    // === airborne motion ===
    if (!_stateNew)
        ApplyGravity(dt);

    MoveAndSlide();

    // only allow landing after we've been clearly airborne
    bool airborne = !IsOnFloor() || Velocity.Y < -10f || _detachFrames > 0;

    if (!airborne && IsOnFloor())
    {
        // restore collider and snap
        if (_col != null) _col.Disabled = false;
        FloorSnapLength = _defaultSnap;
        _snapSuppressed = false;
        _detachFrames = 0;

        Change(State.Idle);
        return;
    }

    // transition to fall after rising
    if (!IsOnFloor() && Velocity.Y > 0f)
        Change(State.Fall);
}

    private float _landTimer = 0f;

    private void S_Fall(double dt)
    {
        if (_stateNew)
        {
            SafePlay(AFall);
            _landTimer = 0.12f;
        }

        ApplyGravity(dt);
        MoveAndSlide();

        if (IsOnFloor())
        {
            _landTimer -= (float)dt;
            if (_landTimer <= 0f)
            {
                if (_col != null) _col.Disabled = false;
                FloorSnapLength = _defaultSnap;
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
            // fixed direction logic
            float dir = _spriteRoot.Scale.X == 1 ? 1 : -1;
            Velocity = new Vector2(dir * DashSpeed, 0);
            SafePlay(ADash);
        }

        ApplyGravity(dt);
        MoveAndSlide();

        bool hitLeftBound = GlobalPosition.X < ArenaLeftX;
        bool hitRightBound = GlobalPosition.X > ArenaRightX;
        if (hitLeftBound || hitRightBound)
            Change(State.DashStop);
    }

    private void S_DashStop(double dt)
    {
        if (_stateNew) SafePlay(ADashStop);
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, DashStopAccel * (float)dt), Velocity.Y);
        ApplyGravity(dt);
        MoveAndSlide();
        if (AnimFinished(ADashStop)) Change(State.Idle);
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

    private void OnMateriaTimeout() => _sprite.SelfModulate = Colors.White;
}
