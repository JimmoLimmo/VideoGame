using Godot;
using System;

public enum CollectableType {
	Sword,
	Dash,
	Walljump,
	Throw
}

public partial class Collectable : Area2D {
	[Export] public CollectableType Type { get; set; } = CollectableType.Sword;
	[Export] public Texture2D PickupTexture;
	[Export] public string ItemName = "Item";
	[Export] public string keybindDesc = "Press <button> to use";
	[Export] public string Description = "You Got an Item!";
	[Export] public double WaitTime = 2;

	private bool canDismiss = false;

	public override void _Ready() {
		BodyEntered += OnBodyEntered;
		ProcessMode = Node.ProcessModeEnum.Always;
		if (GlobalRoomChange.hasSword == true && Type == CollectableType.Sword) QueueFree();
		else if (GlobalRoomChange.hasDash == true && Type == CollectableType.Dash) QueueFree();
		else if (GlobalRoomChange.hasWalljump == true && Type == CollectableType.Walljump) QueueFree();
	}

	private void OnBodyEntered(Node2D body) {
		if (body is Player player) {
			player.OnCollect(Type);

			ShowPickupOverlay();
		}
	}

	private void ShowPickupOverlay() {
		var currentScene = GetTree().CurrentScene;
		var musicPlayer = currentScene.GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
		var nameLabel = ui.GetNode<Label>("Control/ItemName");
		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
		var descLabel = ui.GetNode<Label>("Control/Description");

		var image = ui.GetNode<TextureRect>("Control/ItemImage");
		
		string actionType = "";
			
		switch(Type) {
			case CollectableType.Sword:
				actionType = "attack";
				break;
			case CollectableType.Dash:
				actionType = "dash";
				break;
			case CollectableType.Walljump:
				actionType = "jump";
				break;
			case CollectableType.Throw:
				actionType = "sword_throw";
				break;
		}
		
		var binds = InputMap.ActionGetEvents(actionType);
		string bindText = "";
		
		if(binds.Count == 0) {
			bindText = "Missing Bind";
		} else {
			var firstEvent = binds[0];
			
			bindText = firstEvent switch {
				InputEventKey keyEvent => keyEvent.AsText(), 
				InputEventMouseButton mouseEvent => mouseEvent.AsText(),
				InputEventJoypadButton joyEvent => joyEvent.AsText(),
				_ => firstEvent.AsText()
			};
		}
		
		string useString = keybindDesc.Replace("<button>", bindText);

		nameLabel.Text = ItemName;
		keybindLabel.Text = useString;
		descLabel.Text = Description;

		if (PickupTexture != null) {
			image.Texture = PickupTexture;
		}

		overlay.Visible = true;
		nameLabel.Visible = true;
		keybindLabel.Visible = true;
		descLabel.Visible = true;
		image.Visible = true;

		GetTree().Paused = true;
		musicPlayer.ProcessMode = Node.ProcessModeEnum.Always;
		ui.ProcessMode = Node.ProcessModeEnum.Always;

		var timer = new Timer();
		ui.AddChild(timer);
		timer.WaitTime = 1.5;
		timer.OneShot = true;
		timer.Timeout += () => {
			var currentScene = GetTree().CurrentScene;
			var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
			var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");

			spaceIndicator.Visible = true;

			canDismiss = true;
		};
		timer.Start();
	}

	public override void _Input(InputEvent @event) {
		if (GetTree().Paused && canDismiss) {
			if (@event.IsActionPressed("ui_accept")) ClearPickupOverlay();
		}
	}

	private void ClearPickupOverlay() {
		var currentScene = GetTree().CurrentScene;
		var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
		var nameLabel = ui.GetNode<Label>("Control/ItemName");
		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
		var descLabel = ui.GetNode<Label>("Control/Description");

		var image = ui.GetNode<TextureRect>("Control/ItemImage");
		var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");

		overlay.Visible = false;
		nameLabel.Visible = false;
		keybindLabel.Visible = false;
		descLabel.Visible = false;
		image.Visible = false;
		spaceIndicator.Visible = false;

		GetTree().Paused = false;
		canDismiss = false;
		QueueFree();
	}
}
