using Godot;
using System;

public partial class CameraController : Camera2D {
	[Export] public NodePath playerPath;
	[Export] public NodePath boundsPath;
	[Export] public int cameraYOffset = -300;

	private CharacterBody2D player;
	private CollisionShape2D boundsShape;
	private RectangleShape2D rectShape;

	// ========================= SHAKE SYSTEM =========================
	private float _shakeTime = 0f;
	private float _shakeStrength = 0f;
	private Vector2 _originalOffset = Vector2.Zero;
	private RandomNumberGenerator _rng = new();

	public override void _Ready() {
		player = GetNode<CharacterBody2D>(playerPath);
		boundsShape = GetNode<CollisionShape2D>(boundsPath);
		rectShape = boundsShape.Shape as RectangleShape2D;

		Enabled = true;
		_originalOffset = Offset;
		_rng.Randomize();

		AddToGroup("camera");
	}

	public override void _Process(double delta) {
		if (player == null || rectShape == null) return;

		Vector2 extents = rectShape.Size / 2;
		Vector2 boundsCenter = boundsShape.GlobalPosition;
		Vector2 min = boundsCenter - extents;
		Vector2 max = boundsCenter + extents;

		Vector2 targetPos = player.Position;
		targetPos.Y += cameraYOffset;

		float clampedX = Mathf.Clamp(targetPos.X, min.X, max.X);
		float clampedY = Mathf.Clamp(targetPos.Y, min.Y, max.Y);

		Position = new Vector2(clampedX, clampedY);

		// ======== apply shake each frame ========
		if (_shakeTime > 0f) {
			_shakeTime -= (float)delta;
			Offset = _originalOffset + new Vector2(
				_rng.RandfRange(-_shakeStrength, _shakeStrength),
				_rng.RandfRange(-_shakeStrength, _shakeStrength)
			);

			if (_shakeTime <= 0f)
				Offset = _originalOffset; // reset
		}
	}

	// Public method so other scripts can trigger shakes
	public void Shake(float duration = 0.4f, float strength = 6f) {
		_shakeTime = duration;
		_shakeStrength = strength;
	}
}
