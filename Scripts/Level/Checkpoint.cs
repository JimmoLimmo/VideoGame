using Godot;
using System;

public partial class Checkpoint : Area2D {
	private Marker2D marker;
	
	public override void _Ready() {
		marker = GetNode<Marker2D>("RespawnPoint");
		
		BodyEntered += OnBodyEntered;
	}
	
	private void OnBodyEntered(Node2D body) {
		if(body is Player player) {
			player.SetCheckpoint(marker.GlobalPosition);
		}
	}
}
