// using Godot;
// using System;

// public enum CollectableType {
// 	Sword,
// 	Dash,
// 	Walljump,
// 	Throw
// }

// public partial class Collectable : Area2D {
// 	[Export] public CollectableType Type { get; set; } = CollectableType.Sword;
// 	[Export] public Texture2D PickupTexture;
// 	[Export] public string ItemName = "Item";
// 	[Export] public string keybindDesc = "Press <Button> to use";
// 	[Export] public string Description = "You Got an Item!";
// 	[Export] public double WaitTime = 2;

// 	private bool canDismiss = false;

// 	public override void _Ready() {
// 		BodyEntered += OnBodyEntered;
// 		ProcessMode = Node.ProcessModeEnum.Always;
// 		if (GlobalRoomChange.hasSword == true && Type == CollectableType.Sword) QueueFree();
// 		else if (GlobalRoomChange.hasDash == true && Type == CollectableType.Dash) QueueFree();
// 		else if (GlobalRoomChange.hasWalljump == true && Type == CollectableType.Walljump) QueueFree();
// 	}

// 	private void OnBodyEntered(Node2D body) {
// 		if (body is Player player) {
// 			player.OnCollect(Type);

// 			ShowPickupOverlay();
// 		}
// 	}

// 	private void ShowPickupOverlay() {
// 		var currentScene = GetTree().CurrentScene;
// 		var musicPlayer = currentScene.GetNode<AudioStreamPlayer>("AudioStreamPlayer");
// 		var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
// 		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
// 		var nameLabel = ui.GetNode<Label>("Control/ItemName");
// 		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
// 		var descLabel = ui.GetNode<Label>("Control/Description");

// 		var image = ui.GetNode<TextureRect>("Control/ItemImage");

// 		nameLabel.Text = ItemName;
// 		keybindLabel.Text = keybindDesc;
// 		descLabel.Text = Description;

// 		if (PickupTexture != null) {
// 			image.Texture = PickupTexture;
// 		}

// 		overlay.Visible = true;
// 		nameLabel.Visible = true;
// 		keybindLabel.Visible = true;
// 		descLabel.Visible = true;
// 		image.Visible = true;

// 		GetTree().Paused = true;
// 		musicPlayer.ProcessMode = Node.ProcessModeEnum.Always;
// 		ui.ProcessMode = Node.ProcessModeEnum.Always;

// 		var timer = new Timer();
// 		ui.AddChild(timer);
// 		timer.WaitTime = 1.5;
// 		timer.OneShot = true;
// 		timer.Timeout += () => {
// 			var currentScene = GetTree().CurrentScene;
// 			var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
// 			var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");

// 			spaceIndicator.Visible = true;

// 			canDismiss = true;
// 		};
// 		timer.Start();
// 	}

// 	public override void _Input(InputEvent @event) {
// 		if (GetTree().Paused && canDismiss) {
// 			if (@event.IsActionPressed("ui_accept")) ClearPickupOverlay();
// 		}
// 	}

// 	private void ClearPickupOverlay() {
// 		var currentScene = GetTree().CurrentScene;
// 		var ui = currentScene.GetNode<CanvasLayer>("CollectionOverlay");
// 		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
// 		var nameLabel = ui.GetNode<Label>("Control/ItemName");
// 		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
// 		var descLabel = ui.GetNode<Label>("Control/Description");

// 		var image = ui.GetNode<TextureRect>("Control/ItemImage");
// 		var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");

// 		overlay.Visible = false;
// 		nameLabel.Visible = false;
// 		keybindLabel.Visible = false;
// 		descLabel.Visible = false;
// 		image.Visible = false;
// 		spaceIndicator.Visible = false;

// 		GetTree().Paused = false;
// 		canDismiss = false;
// 		QueueFree();
// 	}
// }
using Godot;
using System;

public enum CollectableType { Sword, Dash, Walljump, Throw }

public partial class Collectable : Area2D {
	[Export] public CollectableType Type { get; set; } = CollectableType.Sword;
	[Export] public Texture2D PickupTexture;
	[Export] public string ItemName = "Item";
	[Export] public string keybindDesc = "Press <Button> to use";
	[Export] public string Description = "You Got an Item!";
	[Export] public double WaitTime = 2;

	private bool canDismiss = false;

	public override void _Ready() {
		// IMPORTANT: this script must keep receiving input while paused
		PauseMode = Node.PauseModeEnum.Process;     // <— add this
		ProcessMode = Node.ProcessModeEnum.Always;  // <— and this (keeps signals/timers consistent)

		BodyEntered += OnBodyEntered;

		if (GlobalRoomChange.hasSword && Type == CollectableType.Sword) QueueFree();
		else if (GlobalRoomChange.hasDash && Type == CollectableType.Dash) QueueFree();
		else if (GlobalRoomChange.hasWalljump && Type == CollectableType.Walljump) QueueFree();
	}

	private void OnBodyEntered(Node2D body) {
		if (body is Player player) {
			player.OnCollect(Type);
			ShowPickupOverlay();
		}
	}

	private async void ShowPickupOverlay() {
		var currentScene = GetTree().CurrentScene;

		// Grab UI nodes
		var musicPlayer = currentScene.GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");
		var ui = currentScene.GetNodeOrNull<CanvasLayer>("CollectionOverlay");
		if (ui == null) {
			GD.PushError("[Collectable] CollectionOverlay not found in scene.");
			return;
		}

		// Ensure the overlay also processes while paused (belt + suspenders)
		ui.PauseMode = Node.PauseModeEnum.Process;     // <— important
		ui.ProcessMode = Node.ProcessModeEnum.Always;  // <— important

		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
		var nameLabel = ui.GetNode<Label>("Control/ItemName");
		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
		var descLabel = ui.GetNode<Label>("Control/Description");
		var image = ui.GetNode<TextureRect>("Control/ItemImage");

		nameLabel.Text = ItemName;
		keybindLabel.Text = keybindDesc;
		descLabel.Text = Description;
		if (PickupTexture != null)
			image.Texture = PickupTexture;

		overlay.Visible = true;
		nameLabel.Visible = true;
		keybindLabel.Visible = true;
		descLabel.Visible = true;
		image.Visible = true;

		// Let all UI get ready this frame, then pause
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		GetTree().Paused = true;

		if (musicPlayer != null)
			musicPlayer.ProcessMode = Node.ProcessModeEnum.Always;

		// Small timer before allowing dismissal (so the prompt appears)
		var timer = new Timer { WaitTime = 1.5, OneShot = true };
		ui.AddChild(timer);
		timer.Timeout += () => {
			var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");
			spaceIndicator.Visible = true;
			canDismiss = true;
		};
		timer.Start();
	}

	public override void _Input(InputEvent @event) {
		// Because PauseMode=Process, this will run even while paused
		if (!GetTree().Paused || !canDismiss) return;

		// Accept with ui_accept OR common alternates (Enter/Space/Z)
		if (@event.IsActionPressed("ui_accept")
			|| (@event is InputEventKey k && k.Pressed &&
				(k.Keycode == Key.Enter || k.Keycode == Key.Space || k.Keycode == Key.Z))) {
			ClearPickupOverlay();
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

		canDismiss = false;
		GetTree().Paused = false;
		QueueFree();
	}
}
