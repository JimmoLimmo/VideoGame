using Godot;
using System;

public partial class MovingPlatform : Node2D {
    [Export] public float leftX = -200f;      // how far left from start
    [Export] public float rightX = 200f;      // how far right from start
    [Export] public float moveTime = 2.5f;    // time to move between ends
    [Export] public bool startMovingRight = true;
    [Export] public bool waitAtEnds = false;
    [Export] public float waitTime = 0.5f;    // only used if waitAtEnds is true

    private StaticBody2D _platform;
    private Timer _cycleTimer;
    private Tween _tween;
    private bool _movingRight;

    private Vector2 _startPos;
    private Vector2 _leftTarget;
    private Vector2 _rightTarget;
    private Vector2 _prevPos;
    public Vector2 LastMotion { get; private set; } = Vector2.Zero;

    public override void _Ready() {
        _platform = GetNode<StaticBody2D>("Platform");
        _cycleTimer = GetNodeOrNull<Timer>("CycleTimer") ?? new Timer();

        if (_cycleTimer.GetParent() == null)
            AddChild(_cycleTimer);

        _cycleTimer.OneShot = true;
        _cycleTimer.Timeout += OnCycleTimeout;

        _startPos = _platform.Position;
        _leftTarget = _startPos + new Vector2(leftX, 0);
        _rightTarget = _startPos + new Vector2(rightX, 0);

        _movingRight = startMovingRight;

        StartNextMove();
    }

    public override void _PhysicsProcess(double delta) {
        if (_platform != null) {
            LastMotion = _platform.GlobalPosition - _prevPos;
            _prevPos = _platform.GlobalPosition;
        }
    }
    private async void StartNextMove() {
        _tween?.Kill(); // stop any existing tween
        _tween = GetTree().CreateTween();
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.SetEase(Tween.EaseType.InOut);

        Vector2 from = _platform.Position;
        Vector2 to = _movingRight ? _rightTarget : _leftTarget;

        _tween.TweenProperty(_platform, "position", to, moveTime);
        await ToSignal(_tween, Tween.SignalName.Finished);

        _movingRight = !_movingRight;

        if (waitAtEnds)
            _cycleTimer.Start(waitTime);
        else
            StartNextMove();
    }

    private void OnCycleTimeout() {
        StartNextMove();
    }
}
