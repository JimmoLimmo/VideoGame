using Godot;
using System;

public partial class BreakableWall : Node
{
	[Export] public int health = 30;
	[Export] public string WallId = "room_xx_wall_xx";
	
	public override void _Ready() {
		if(GlobalRoomChange.IsWallBroken(WallId)) QueueFree();
	}
	
	public void TakeDamage(int damage, Vector2 temp) {
		health -= damage;
		
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
