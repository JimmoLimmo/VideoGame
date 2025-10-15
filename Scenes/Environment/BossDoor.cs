using Godot;
using System.Threading.Tasks;

public partial class BossDoor : Node2D {
    // === Timing & movement ===
    [Export] public float CloseDelay = 0.3f;
    [Export] public float SlideDuration = 0.4f;
    [Export] public Vector2 ClosedOffset = new Vector2(0, 500);
    [Export] public AudioStreamPlayer2D CloseSound;

    // === Cinematic control ===
    [Export] public NodePath BossPath;
    [Export] public NodePath CameraPath;
    [Export] public float DelayBeforeWake = 2.5f;
    [Export] public Rect2 CameraLockArea = new Rect2(1500, 200, 1500, 800);
    [Export] public Vector2 CameraFocusPoint = new Vector2(982, 531);

    private StaticBody2D _doorBody;
    private Area2D _trigger;
    private bool _closed = false;
    private Vector2 _openPos;
    private Vector2 _closedPos;

    private Boss _boss;
    private CameraController _camera;

    public override void _Ready() {
        _doorBody = GetNode<StaticBody2D>("DoorBody");
        _trigger = GetNode<Area2D>("Trigger");

        _openPos = _doorBody.Position;
        _closedPos = _openPos + ClosedOffset;

        _trigger.BodyEntered += OnBodyEntered;

        _boss = GetNodeOrNull<Boss>(BossPath);
        _camera = GetNodeOrNull<CameraController>(CameraPath);

        if (_boss != null)
            _boss.SetProcess(false); // freeze boss until trigger
    }

    private async void OnBodyEntered(Node body) {
        if (_closed) return;
        if (!body.IsInGroup("player")) return;
        _closed = true;

        // === Step 1: Close door ===
        await ToSignal(GetTree().CreateTimer(CloseDelay), Timer.SignalName.Timeout);
        CloseDoor();

        // === Step 2: Fade out overworld music ===
        if (MusicManager.Instance != null)
            MusicManager.Instance.Stop(1.0); // smooth fade-out before boss


        // === Step 4: Dramatic pause ===
        // await ToSignal(GetTree().CreateTimer(DelayBeforeWake), Timer.SignalName.Timeout);

        // === Step 5: Wake boss + roar ===
        if (_boss != null) {
            _boss.Visible = true;
            // _boss.SetPhysicsProcess(true);
            // _boss.SetProcess(true);
            // _boss.RoarIntro();
            _boss.ActivateWithRoar();
        }

        // === Step 6: Start boss music ===
        MusicManager.Instance?.StartBoss(1.2);



        GD.Print("[BossDoor] Boss battle started!");
    }

    private void CloseDoor() {
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_doorBody, "position", _closedPos, SlideDuration);

        // use layer/mask toggles instead of disabling shapes
        _doorBody.CollisionLayer = 1;
        _doorBody.CollisionMask = 1;

        CloseSound?.Play();
        GD.Print("[BossDoor] Closed.");
        _boss.Activate();
    }

    public void OpenDoor() {
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_doorBody, "position", _openPos, SlideDuration);

        _doorBody.CollisionLayer = 0;
        _doorBody.CollisionMask = 0;

        GD.Print("[BossDoor] Opened.");
    }

}
