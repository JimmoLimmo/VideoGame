using Godot;
using System;

public enum CollectableType {
	Sword,
	Dash,
	Walljump,
	SwordTeleport
}

public partial class Collectable : Area2D {
	// Editor-assigned unique identifier for saving whether this item was picked up.
	[Export] public string SaveID { get; set; } = "";
	[Export] public CollectableType Type {get; set;} = CollectableType.Sword;
	[Export] public Texture2D PickupTexture;
	[Export] public string ItemName = "Item";
	[Export] public string keybindDesc = "Press <Button> to use";
	[Export] public string Description = "You Got an Item!";
	[Export] public double WaitTime = 2;
	
	private bool canDismiss = false;
	
	public override void _Ready() {
		// Check if SaveID is set on parent node (for scene instances where SaveID is set on the root node)
		if (string.IsNullOrEmpty(SaveID) && GetParent() != null)
		{
			// Try to get SaveID from parent if it has the property
			try
			{
				var parent = GetParent();
				// Check if parent has a SaveID property (common in scene instances)
				if (parent.HasMethod("Get"))
				{
					var parentSaveIDVariant = parent.Get("SaveID");
					if (parentSaveIDVariant.VariantType == Variant.Type.String)
					{
						string parentSaveID = parentSaveIDVariant.AsString();
						if (!string.IsNullOrEmpty(parentSaveID))
						{
							SaveID = parentSaveID;
						}
					}
				}
			}
			catch
			{
				// Parent doesn't have SaveID property, continue with auto-generation
			}
		}

		// For specific items, use hardcoded SaveIDs based on parent name
		if (string.IsNullOrEmpty(SaveID) && GetParent() != null)
		{
			string parentName = GetParent().Name;
			switch (parentName)
			{
				case "SwordUpgrade":
					SaveID = "level_1_sword";
					break;
				case "DashUpgrade":
					SaveID = "level_1_dash";
					break;
				case "WallJumpUpgrade":
					SaveID = "level_1_walljump";
					break;
				case "SwordTeleportUpgrade":
					SaveID = "level_1_sword_teleport";
					break;
			}
		}

		// Ensure this collectable has a stable SaveID. If none provided in the editor, auto-generate one
		if (string.IsNullOrEmpty(SaveID))
		{
			string sceneName = GetTree().CurrentScene?.Name ?? "unknown_scene";
			string nodePath = GetPath().ToString();
			SaveID = $"auto:{sceneName}:{nodePath}";
		}

		// If this collectable has already been picked up in the save, remove it now
		bool isCollected = SaveManager.HasCollectedItem(SaveID);
		if (isCollected)
		{
			QueueFree();
			return;
		}
		BodyEntered += OnBodyEntered;
		ProcessMode = Node.ProcessModeEnum.Always;
	}
	
	private void OnBodyEntered(Node2D body) {
		if(body is Player player) {
			player.OnCollect(Type);
			// Record collection in the cached save (if SaveID provided) but do NOT persist to disk here.
			if (!string.IsNullOrEmpty(SaveID))
			{
				var save = SaveManager.GetCurrentSave();
				if (save.CollectedItems == null) save.CollectedItems = new System.Collections.Generic.List<string>();
				if (!save.CollectedItems.Contains(SaveID))
					save.CollectedItems.Add(SaveID);
				// Also flip equipment flags based on collectable type so Continue restores correctly
				switch (Type)
				{
					case CollectableType.Sword:
						save.HasSword = true;
						break;
					case CollectableType.Dash:
						save.HasDash = true;
						break;
					case CollectableType.Walljump:
					save.HasWalljump = true;
					break;
				case CollectableType.SwordTeleport:
					save.HasSwordTeleport = true;
					break;
				default:
					break;
				}
				// Do NOT write to disk here; SaveStation will flush the cached save when the player explicitly saves.
			}
			
			ShowPickupOverlay();
		}
	}
	
	private void ShowPickupOverlay() {
		var ui = GetTree().Root.GetNode<CanvasLayer>("level_1/CollectionOverlay");
		var overlay = ui.GetNode<ColorRect>("Control/Overlay");
		var nameLabel = ui.GetNode<Label>("Control/ItemName");
		var keybindLabel = ui.GetNode<Label>("Control/Keybind");
		var descLabel = ui.GetNode<Label>("Control/Description");
		
		var image = ui.GetNode<TextureRect>("Control/ItemImage");
		
		nameLabel.Text = ItemName;
		keybindLabel.Text = keybindDesc;
		descLabel.Text = Description;
		
		if(PickupTexture != null) {
			image.Texture = PickupTexture;
		}
		
		overlay.Visible = true;
		nameLabel.Visible = true;
		keybindLabel.Visible = true;
		descLabel.Visible = true;
		image.Visible = true;
		
		GetTree().Paused = true;
		ui.ProcessMode = Node.ProcessModeEnum.Always;
		
		var timer = new Timer();
		ui.AddChild(timer);
		timer.WaitTime = 1.5;
		timer.OneShot = true;
		timer.Timeout += () => {
			var ui = GetTree().Root.GetNode<CanvasLayer>("level_1/CollectionOverlay");
			var spaceIndicator = ui.GetNode<ColorRect>("Control/SpaceIndicator");
			
			spaceIndicator.Visible = true;
			
			canDismiss = true;
		};
		timer.Start();
	}
	
	public override void _Input(InputEvent @event) {
		if(GetTree().Paused && canDismiss) {
			if(@event.IsActionPressed("ui_accept")) ClearPickupOverlay();
		}
	}
	
	private void ClearPickupOverlay() {
		var ui = GetTree().Root.GetNode<CanvasLayer>("level_1/CollectionOverlay");
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
