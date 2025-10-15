using Godot;
using System.Collections.Generic;

public enum RoomGroup { Title, Overworld, Boss }

public partial class GlobalRoomChange : Node {
	// --------------------------------------------------------------------
	// Public static fields / properties
	// --------------------------------------------------------------------
	public static bool Activate { get; set; } = false;
	public static Vector2 PlayerPos { get; set; } = new Vector2();
	public static bool PlayerJumpOnEnter { get; set; } = false;

	public static bool hasSword;
	public static bool hasDash;
	public static bool hasWalljump;

	public static int health = 5;
	public static int mana = 0;
	public static int maxMana = 9;
	public static Dictionary<string, bool> destroyedWalls = new();

	public static string CurrentRoom { get; private set; } = "";
	public static RoomGroup CurrentGroup { get; private set; } = RoomGroup.Title;

	private Node _lastScene;

	public static string LastExitName = "";
	public static string LastExitRoom = "";
	public static string CheckpointRoom = "";
	public static Vector2 CheckpointPos = Vector2.Zero;
	public static bool HasCheckpoint => !string.IsNullOrEmpty(CheckpointRoom);

	// --------------------------------------------------------------------
	// C# event (replaces [Signal])
	// --------------------------------------------------------------------
	public delegate void RoomGroupChangedEventHandler(RoomGroup group);
	public static event RoomGroupChangedEventHandler RoomGroupChanged;

	// --------------------------------------------------------------------
	// Node lifecycle
	// --------------------------------------------------------------------
	public override void _Ready() {
		ProcessMode = Node.ProcessModeEnum.Always;
	}

	public override void _Process(double delta) {
		var scene = GetTree().CurrentScene;

		// Scene temporarily missing?  Wait until next valid one
		if (scene == null) return;

		string sceneName = scene.Name ?? "";
		if (scene != _lastScene && sceneName != "") {
			GD.Print($"[GlobalRoomChange] Scene object changed → {sceneName}");
			_lastScene = scene;
			UpdateScene(scene);
		}
	}


	// --------------------------------------------------------------------
	// ForceUpdate — call this after ChangeSceneToPacked() to sync HUD/music
	// --------------------------------------------------------------------
	// public static async void ForceUpdate() {
	// 	var tree = Engine.GetMainLoop() as SceneTree;
	// 	if (tree == null) {
	// 		GD.PushWarning("[GlobalRoomChange] ForceUpdate() called but SceneTree missing.");
	// 		return;
	// 	}

	// 	// Wait a frame if the current scene isn't ready yet
	// 	if (tree.CurrentScene == null) {
	// 		await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
	// 	}

	// 	var scene = tree.CurrentScene;
	// 	if (scene == null) {
	// 		GD.PushWarning("[GlobalRoomChange] ForceUpdate() still no scene after waiting — aborting.");
	// 		return;
	// 	}

	// 	UpdateScene(scene);
	// }
	public static async void ForceUpdate() {
		var tree = Engine.GetMainLoop() as SceneTree;
		if (tree == null) return;

		// Wait until CurrentScene is non-null
		while (tree.CurrentScene == null)
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		var scene = tree.CurrentScene;
		UpdateScene(scene);
	}


	// --------------------------------------------------------------------
	// Internal helpers
	// --------------------------------------------------------------------
	private static void UpdateScene(Node scene) {
		RoomGroup group = DetectGroup(scene);

		string roomName = !string.IsNullOrEmpty(scene.SceneFilePath)
			? System.IO.Path.GetFileNameWithoutExtension(scene.SceneFilePath)
			: scene.Name;

		EnterRoom(roomName, group);
	}

	private static RoomGroup DetectGroup(Node scene) {
		if (scene.IsInGroup("music_title")) return RoomGroup.Title;
		if (scene.IsInGroup("music_overworld")) return RoomGroup.Overworld;
		if (scene.IsInGroup("music_boss")) return RoomGroup.Boss;

		string path = scene.SceneFilePath?.ToLower() ?? "";
		string name = scene.Name.ToString().ToLowerInvariant();

		if (path.Contains("title") || path.Contains("menu") || name.Contains("menu") || name.Contains("title"))
			return RoomGroup.Title;
		if (path.Contains("boss") || name.Contains("boss"))
			return RoomGroup.Boss;

		return RoomGroup.Overworld;
	}

	// --------------------------------------------------------------------
	// Public API
	// --------------------------------------------------------------------
	public static void MarkWallBroken(string wallId) => destroyedWalls[wallId] = true;
	public static bool IsWallBroken(string wallId) => destroyedWalls.ContainsKey(wallId) && destroyedWalls[wallId];

	public static void EnterRoom(string roomName, RoomGroup group) {
		bool isFirstRoom = string.IsNullOrEmpty(CurrentRoom);
		if (group == CurrentGroup && !isFirstRoom)
			return;

		CurrentRoom = roomName;
		CurrentGroup = group;

		// Notify listeners (HUD, etc.)
		RoomGroupChanged?.Invoke(group);

		var mm = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull<MusicManager>("MusicManager");
		if (mm == null) {
			GD.PushWarning("[GlobalRoomChange] MusicManager not found.");
			return;
		}

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
	// public static void SetRespawnToLastExit(Node scene) {
	// 	if (string.IsNullOrEmpty(LastExitName))
	// 		return;

	// 	var door = scene.GetNodeOrNull<DoorArea2D>(LastExitName);
	// 	if (door != null)
	// 		PlayerPos = door.PlayerPos;
	// }

	public static Vector2 FindNearestDoor(Node scene, Vector2 deathPos) {
		float minDist = float.MaxValue;
		Vector2 closest = deathPos;

		foreach (Node child in scene.GetChildren()) {
			if (child is DoorArea2D door) {
				float dist = door.GlobalPosition.DistanceTo(deathPos);
				if (dist < minDist) {
					minDist = dist;
					closest = door.PlayerPos;
				}
			}
		}
		return closest;
	}

}
