using Godot;
using System;

public partial class CameraController : Camera2D {
	[Export] public NodePath playerPath;
	[Export] public NodePath boundsPath;
	[Export] public int cameraYOffset = -300;

	private CharacterBody2D player;

	private CollisionShape2D boundsShape;
	private RectangleShape2D rectShape;

	public override void _Ready() {
		player = GetNode<CharacterBody2D>(playerPath);

		boundsShape = GetNode<CollisionShape2D>(boundsPath);
		rectShape = boundsShape.Shape as RectangleShape2D;

		Enabled = true;
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
	}
}
