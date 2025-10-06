using Godot;
using System;

public partial class DoorArea2D : Area2D {
	[Export] public string ConnectedRoom;
	[Export] public Vector2 PlayerPos;
	[Export] public bool PlayerJumpOnEnter = false;
	
	public override void _Ready() {
		this.BodyEntered += OnBodyEntered;
	}
	
	private void OnBodyEntered(Node body) {
		if(body.IsInGroup("player")) {
			GlobalRoomChange.Activate = true;
			GlobalRoomChange.PlayerPos = PlayerPos;
			GlobalRoomChange.PlayerJumpOnEnter = PlayerJumpOnEnter;
			GetTree().CallDeferred("change_scene_to_file", ConnectedRoom);
		}
	}
}
