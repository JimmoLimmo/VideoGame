using Godot;
using System;
using Godot.Collections;

public partial class SettingsManager : Node {
	private const string SettingsPath = "user://settings.json";

	// Example settings
	public bool Fullscreen = false;
	public float MasterVolume = 1.0f;
	public int ResolutionIndex = 0;

	public override void _Ready() {
		LoadSettings();
	}

	public void SaveSettings() {
		var data = new Godot.Collections.Dictionary<string, Variant>
		{
			{ "fullscreen", Fullscreen },
			{ "master_volume", MasterVolume },
			{ "resolution_index", ResolutionIndex }
		};

		// Convert to JSON string
		string jsonString = Json.Stringify(data, indent: "\t");

		// Save to file
		using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
		file.StoreString(jsonString);

		GD.Print("[Settings] Saved to: ", SettingsPath);
	}

	public void LoadSettings() {
		if (!FileAccess.FileExists(SettingsPath)) {
			GD.Print("[Settings] File not found — creating defaults.");
			SaveSettings();
			return;
		}

		using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();

		var parsed = Json.ParseString(jsonText);
		if (parsed.VariantType == Variant.Type.Dictionary) {
			var dict = parsed.AsGodotDictionary();

			Fullscreen = dict.TryGetValue("fullscreen", out Variant f) && (bool)f;
			MasterVolume = dict.TryGetValue("master_volume", out Variant mv) ? (float)mv : 1.0f;
			ResolutionIndex = dict.TryGetValue("resolution_index", out Variant ri) ? (int)ri : 0;
		}

		ApplySettings();
	}

	public void ApplySettings() {
		DisplayServer.WindowSetMode(
			Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
		);

		AudioServer.SetBusVolumeDb(
			AudioServer.GetBusIndex("Master"),
			Mathf.LinearToDb(MasterVolume)
		);
	}
}
