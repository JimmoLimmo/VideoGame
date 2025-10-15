using Godot;
using System.Threading.Tasks;

public partial class BossDoor : Node2D {
    [Export] public NodePath BossPath;
    [Export] public Vector2 ClosedOffset = new(0, 500);
    [Export] public float CloseSpeed = 1200f;
    [Export] public float BossActivationDelay = 1.0f;

    private Boss _boss;
    private StaticBody2D _doorBody;
    private Sprite2D _doorSprite;
    private AudioStreamPlayer2D _closeSound;
    private Vector2 _openPos;
    private Vector2 _closedPos;
    private bool _isClosed = false;
    private bool _triggered = false;


    public override void _Ready() {
        _doorBody = GetNode<StaticBody2D>("DoorBody");
        _doorSprite = _doorBody.GetNode<Sprite2D>("Sprite2D");
        _closeSound = GetNode<AudioStreamPlayer2D>("CloseSound");

        if (BossPath != null && !BossPath.IsEmpty)
            _boss = GetNodeOrNull<Boss>(BossPath);

        if (_boss == null) {
            var bosses = GetTree().GetNodesInGroup("boss");
            if (bosses.Count > 0)
                _boss = bosses[0] as Boss;
        }

        _openPos = _doorBody.Position;
        _closedPos = _openPos + ClosedOffset;

        var trigger = GetNode<Area2D>("Trigger");
        trigger.BodyEntered += OnBodyEntered;

        MusicManager.Instance?.Play(BgmTrack.Ambiance, 1.0);

        if (_boss != null) {
            _boss.SetPhysicsProcess(false);
            _boss.ProcessMode = ProcessModeEnum.Disabled;
        }

        GD.Print("[BossDoor] Initialized: ambient playing, boss idle.");
    }

    private async void OnBodyEntered(Node body) {
        if (_triggered) return; //  prevent re-entry
        if (body is not Player player) return;

        _triggered = true; //  mark as used
        var trigger = GetNode<Area2D>("Trigger");
        // trigger.Monitoring = false; //  stop future signals
        trigger.SetDeferred("monitoring", false);

        GD.Print("[BossDoor] Player entered boss arena — closing door and starting intro.");

        await CloseDoor();
        _closeSound?.Play(); // slam
        GD.Print("[BossDoor] Closed.");

        MusicManager.Instance?.StartBoss(0.8);

        await ToSignal(GetTree().CreateTimer(BossActivationDelay), Timer.SignalName.Timeout);

        if (_boss != null) {
            _boss.ProcessMode = ProcessModeEnum.Inherit;
            _boss.SetPhysicsProcess(true);
            _boss.SetProcess(true);
            GD.Print("[BossDoor] Boss activated!");
        }
        else {
            GD.PrintErr("[BossDoor] ERROR: Boss not assigned!");
        }
    }


    public async Task CloseDoor() {
        if (_isClosed) return;
        _isClosed = true;

        //  Enable collision before closing
        foreach (var shape in _doorBody.GetChildren()) {
            if (shape is CollisionShape2D cs)
                cs.SetDeferred("disabled", false);
        }

        var tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_doorBody, "position", _closedPos, 0.5);
        await ToSignal(tween, Tween.SignalName.Finished);

        GD.Print("[BossDoor] Closed and collision enabled.");
    }


    public async void OpenDoor() {
        if (!_isClosed) return;
        _isClosed = false;

        var tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_doorBody, "position", _openPos, 0.8);
        await ToSignal(tween, Tween.SignalName.Finished);

        //  Disable collision once open
        foreach (var shape in _doorBody.GetChildren()) {
            if (shape is CollisionShape2D cs)
                cs.SetDeferred("disabled", true);
        }

        GD.Print("[BossDoor] Door reopened and collision disabled.");
    }
}
