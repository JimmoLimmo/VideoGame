using Godot;
using System.Threading.Tasks;

public partial class BossDoor : Node2D {
    [Export] public float CloseDelay = 0.3f;
    [Export] public float SlideDuration = 0.4f;
    [Export] public Vector2 ClosedOffset = new Vector2(0, 500); // how far the door slides when closing
    [Export] public AudioStreamPlayer2D CloseSound;

    private StaticBody2D _doorBody;
    private Sprite2D _doorSprite;
    private Area2D _trigger;
    private bool _closed = false;
    private Vector2 _openPos;
    private Vector2 _closedPos;

    public override void _Ready() {
        _doorBody = GetNode<StaticBody2D>("DoorBody");
        _doorSprite = _doorBody.GetNodeOrNull<Sprite2D>("Sprite2D");
        _trigger = GetNode<Area2D>("Trigger");

        _openPos = _doorBody.Position;
        _closedPos = _openPos + ClosedOffset;

        _trigger.BodyEntered += OnBodyEntered;

        // Start open
        _doorBody.Position = _openPos;
        _doorBody.CollisionLayer = 0;
        _doorBody.CollisionMask = 0;
    }

    private async void OnBodyEntered(Node body) {
        if (_closed) return;
        if (!body.IsInGroup("player")) return;

        // small delay so player is inside
        await ToSignal(GetTree().CreateTimer(CloseDelay), Timer.SignalName.Timeout);
        CloseDoor();
    }

    private void CloseDoor() {
        if (_closed) return;
        _closed = true;

        // Enable collisions so it’s solid
        _doorBody.CollisionLayer = 1;
        _doorBody.CollisionMask = 1;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_doorBody, "position", _closedPos, SlideDuration);

        CloseSound?.Play();
        GD.Print("[BossDoor] Closed.");
    }

    public void OpenDoor() {
        if (!_closed) return;
        _closed = false;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_doorBody, "position", _openPos, SlideDuration);

        // Disable collisions so player can pass through again
        _doorBody.CollisionLayer = 0;
        _doorBody.CollisionMask = 0;

        GD.Print("[BossDoor] Opened.");
    }
}
