using Godot;
using System.Collections.Generic;

public enum RoomGroup {
	Title,
	Overworld,
	Boss
}

public partial class GlobalRoomChange : Node {
	public static bool Activate { get; set; } = false;
	public static Vector2 PlayerPos { get; set; } = new Vector2();
	public static bool PlayerJumpOnEnter { get; set; } = false;

	public static bool hasSword;
	public static bool hasDash;
	public static bool hasWalljump;

	public static Dictionary<string, bool> destroyedWalls = new();

	public static void MarkWallBroken(string wallId) => destroyedWalls[wallId] = true;
	public static bool IsWallBroken(string wallId) => destroyedWalls.ContainsKey(wallId) && destroyedWalls[wallId];

	public static string CurrentRoom { get; private set; } = "";
	public static RoomGroup CurrentGroup { get; private set; } = RoomGroup.Title;

	private Node _lastScene;

	public override void _Ready() {
		ProcessMode = Node.ProcessModeEnum.Always;
	}

	public override void _Process(double delta) {
		var scene = GetTree().CurrentScene;
		if (scene == _lastScene || scene == null) return;
		_lastScene = scene;

		RoomGroup group = DetectGroup(scene);

		string roomName = !string.IsNullOrEmpty(scene.SceneFilePath)
			? System.IO.Path.GetFileNameWithoutExtension(scene.SceneFilePath)
			: scene.Name;

		EnterRoom(roomName, group);
	}

	private static RoomGroup DetectGroup(Node scene) {
		// Prefer explicit groups if present
		if (scene.IsInGroup("music_title")) return RoomGroup.Title;
		if (scene.IsInGroup("music_overworld")) return RoomGroup.Overworld;
		if (scene.IsInGroup("music_boss")) return RoomGroup.Boss;

		// --- Automatic fallback detection ---
		string path = scene.SceneFilePath?.ToLower() ?? "";
		string name = (string)scene.Name; // Convert StringName → string
		name = name.ToLowerInvariant();

		// Recognize menus/titles automatically
		if (path.Contains("title") || path.Contains("menu") || name.Contains("menu") || name.Contains("title"))
			return RoomGroup.Title;

		// Recognize bosses
		if (path.Contains("boss") || name.Contains("boss"))
			return RoomGroup.Boss;

		// Everything else defaults to Overworld
		return RoomGroup.Overworld;
	}


	public static async void EnterRoom(string roomName, RoomGroup group) {
		bool isFirstRoom = string.IsNullOrEmpty(CurrentRoom);

		// Only trigger when changing groups (prevents re-fires each frame)
		if (group == CurrentGroup && !isFirstRoom)
			return;

		CurrentRoom = roomName;
		CurrentGroup = group;

		var mm = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull<MusicManager>("MusicManager");
		if (mm == null) return;

		GD.Print($"[GlobalRoomChange] Entered room: {roomName} (Group: {group})");

		switch (group) {
			case RoomGroup.Title:
				if (!mm.IsPlaying(BgmTrack.Title))
					mm.Play(BgmTrack.Title, 0.8);
				break;

			case RoomGroup.Overworld:
				if (!mm.IsPlaying(BgmTrack.Overworld))
					mm.Play(BgmTrack.Overworld, 0.8);
				break;

			case RoomGroup.Boss:
				mm.StartBoss(0.8);
				break;
		}
	}
}
