using Godot;
using System;
using Godot.Collections;

public partial class SettingsManager : Node {
	private const string SettingsPath = "user://settings.json";

	public bool Fullscreen = false;
	public float MasterVolume = 1.0f;
	public float MusicVolume = 1.0f;
	public float SfxVolume = 1.0f;
	public int ResolutionIndex = 0;

	public override void _Ready() {
		LoadSettings();
	}

	public void SaveSettings() {
		var data = new Dictionary<string, Variant> {
			{ "fullscreen", Fullscreen },
			{ "master_volume", MasterVolume },
			{ "music_volume", MusicVolume },
			{ "sfx_volume", SfxVolume },
			{ "resolution_index", ResolutionIndex }
		};

		string json = Json.Stringify(data, "\t");
		using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
		file.StoreString(json);
		GD.Print("[Settings] Saved to: ", SettingsPath);
	}

	public void LoadSettings() {
		if (!FileAccess.FileExists(SettingsPath)) {
			GD.Print("[Settings] File not found — creating defaults.");
			SaveSettings();
			return;
		}

		using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
		var parsed = Json.ParseString(file.GetAsText());
		if (parsed.VariantType != Variant.Type.Dictionary) return;

		var dict = parsed.AsGodotDictionary();

		Fullscreen = dict.TryGetValue("fullscreen", out Variant f) && (bool)f;
		MasterVolume = dict.TryGetValue("master_volume", out Variant mv) ? (float)mv : 1f;
		MusicVolume = dict.TryGetValue("music_volume", out Variant mu) ? (float)mu : 1f;
		SfxVolume = dict.TryGetValue("sfx_volume", out Variant sv) ? (float)sv : 1f;
		ResolutionIndex = dict.TryGetValue("resolution_index", out Variant ri) ? (int)ri : 0;

		ApplySettings();
	}

	public void ApplySettings() {
		// --- Apply fullscreen/window mode ---
		DisplayServer.WindowSetMode(
			Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
		);

		// --- Apply resolution ---
		Vector2I resolution = ResolutionIndex switch {
			0 => new Vector2I(1280, 720),
			1 => new Vector2I(1920, 1080),
			2 => new Vector2I(2560, 1440),
			_ => DisplayServer.WindowGetSize()
		};
		DisplayServer.WindowSetSize(resolution);

		// --- Apply volume levels ---
		SetBusVolume("Master", MasterVolume);
		SetBusVolume("Music", MusicVolume);
		SetBusVolume("SFX", SfxVolume);
	}

	private void SetBusVolume(string busName, float linearVolume) {
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex == -1) {
			GD.PushWarning($"[Settings] Audio bus '{busName}' not found!");
			return;
		}
		AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(linearVolume));
	}
}
