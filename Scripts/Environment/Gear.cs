using Godot;
using System;

public partial class Gear : Node2D {
	[Export] public int gearSize = 1;
	[Export] float gearScale = 1f;
	[Export] bool spinClockwise = true;
	
	private int gearSpeedModifier = 1;
	
	public override void _Ready() {
		Scale = new Vector2(gearScale, gearScale);
	}
	
	public override void _Process(double delta) {
		float gearSpeed = (1/(float)gearSize) * gearSpeedModifier;
		int direction = spinClockwise ? 1 : -1;
		Rotation += gearSpeed * direction * (float)delta;
	}
}
