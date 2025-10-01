using Godot;
using System;

public partial class BreakableWall : Node
{
	[Export] public int health = 30;
	
	public override void _Ready() {
	}
	
	public void TakeDamage(int damage) {
		health -= damage;
		
		if(health <= 0) {
			BreakWall();
		}
	}
	
	private void BreakWall() {
		QueueFree();
	}
}
