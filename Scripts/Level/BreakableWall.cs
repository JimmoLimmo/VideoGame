using Godot;
using System;

public partial class BreakableWall : Node
{
	[Export] public int health = 30;
	[Export] public string SaveID { get; set; } = "";
	
	public override void _Ready() {
		// Ensure this wall has a stable SaveID. If none provided in the editor, auto-generate one
		if (string.IsNullOrEmpty(SaveID))
		{
			string sceneName = GetTree().CurrentScene?.Name ?? "unknown_scene";
			string nodePath = GetPath().ToString();
			SaveID = $"auto:{sceneName}:{nodePath}";
		}

		// If this wall has already been destroyed in the save, remove it now
		bool isDestroyed = SaveManager.HasCollectedItem(SaveID);
		if (isDestroyed)
		{
			QueueFree();
			return;
		}
	}
	
	public void TakeDamage(int damage) {
		health -= damage;
		
		GD.Print("Wall Hit, HP: " + health);
		
		if(health <= 0) {
			BreakWall();
		}
	}
	
	private void BreakWall() {
		// Record destruction in the cached save (but do NOT persist to disk here)
		if (!string.IsNullOrEmpty(SaveID))
		{
			var save = SaveManager.GetCurrentSave();
			if (save.CollectedItems == null) save.CollectedItems = new System.Collections.Generic.List<string>();
			if (!save.CollectedItems.Contains(SaveID))
				save.CollectedItems.Add(SaveID);
			// SaveStation will flush the cached save when the player explicitly saves
		}
		
		QueueFree();
	}
}
