using Godot;
using System;

public enum CollectableType {
	Sword,
	Dash,
	Walljump,
	Throw
}

public partial class Collectable : Area2D {
	[Export]
	public CollectableType Type {get; set;} = CollectableType.Sword;
	
	public override void _Ready() {
		BodyEntered += OnBodyEntered;
	}
	
	private void OnBodyEntered(Node2D body) {
		if(body is Player player) {
			player.OnCollect(Type);
			QueueFree();
		}
	}
}
