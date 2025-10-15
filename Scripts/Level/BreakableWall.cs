using Godot;
using System;

public partial class BreakableWall : Node
{
	[Export] public int health = 30;
	[Export] public string WallId = "room_xx_wall_xx";
	
	private AnimationPlayer animate;
	
	public override void _Ready() {
		if(GlobalRoomChange.IsWallBroken(WallId)) QueueFree();
		else {
			animate = GetNode<AnimationPlayer>("AnimationPlayer");
		}
	}
	
	public void TakeDamage(int damage, Vector2 temp) {
		health -= damage;
		animate.Play("Shake");
		
		GD.Print("Wall Hit, HP: " + health);
		
		if(health <= 0) {
			BreakWall();
		}
	}
	
	private void BreakWall() {
		GlobalRoomChange.MarkWallBroken(WallId);
		QueueFree();
	}
}
