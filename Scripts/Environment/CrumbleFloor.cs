using Godot;
using System;

public partial class CrumbleFloor : Node2D {
	private Area2D collider;
	
	public override void _Ready() {
		collider = GetNode<Area2D>("PlayerCollider");
		
		collider.BodyEntered += OnBodyEntered;
	}
	
	private void OnBodyEntered(Node body) {
		
	}
}
