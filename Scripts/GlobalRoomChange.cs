using Godot;
using System.Collections.Generic;

public enum RoomGroup { Title, Overworld, Boss }

public partial class GlobalRoomChange : Node {
	// --------------------------------------------------------------------
	// Persistent Global Data
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

	// Room / Group tracking
	public static string CurrentRoom { get; private set; } = "";
	public static RoomGroup CurrentGroup { get; private set; } = RoomGroup.Title;
	private Node _lastScene;

	public static string LastExitName = "";
	public static string LastExitRoom = "";

	// ✅ New: Checkpoint system
	public static string CheckpointRoom = "";
	public static Vector2 CheckpointPos = Vector2.Zero;
	public static bool HasCheckpoint => !string.IsNullOrEmpty(CheckpointRoom);

	// --------------------------------------------------------------------
	// Event
	// --------------------------------------------------------------------
	public delegate void RoomGroupChangedEventHandler(RoomGroup group);
	public static event RoomGroupChangedEventHandler RoomGroupChanged;

	// --------------------------------------------------------------------
	// Lifecycle
	// --------------------------------------------------------------------
	public override void _Ready() => ProcessMode = Node.ProcessModeEnum.Always;

	public override void _Process(double delta) {
		var scene = GetTree().CurrentScene;
		if (scene == null) return;

		string sceneName = scene.Name ?? "";
		if (scene != _lastScene && sceneName != "") {
			GD.Print($"[GlobalRoomChange] Scene object changed → {sceneName}");
			_lastScene = scene;
			UpdateScene(scene);
		}
	}

	// --------------------------------------------------------------------
	// Helpers
	// --------------------------------------------------------------------
	public static async void ForceUpdate() {
		var tree = Engine.GetMainLoop() as SceneTree;
		if (tree == null) return;

		while (tree.CurrentScene == null)
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		UpdateScene(tree.CurrentScene);
	}

	private static void UpdateScene(Node scene) {
		var group = DetectGroup(scene);

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

		RoomGroupChanged?.Invoke(group);

		var mm = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull<MusicManager>("MusicManager");
		if (mm == null) {
			GD.PushWarning("[GlobalRoomChange] MusicManager not found.");
			return;
		}

		GD.Print($"[GlobalRoomChange] Entered room: {roomName} (Group: {group})");

		switch (group) {
			case RoomGroup.Title:
				if (!mm.IsPlaying(BgmTrack.Title)) mm.Play(BgmTrack.Title, 0.8);
				break;
			case RoomGroup.Overworld:
				if (!mm.IsPlaying(BgmTrack.Overworld)) mm.Play(BgmTrack.Overworld, 0.8);
				break;
			case RoomGroup.Boss:
				mm.StartBoss(0.8);
				break;
		}
	}

	public static void SetRespawnToLastExit(Node scene) {
		if (string.IsNullOrEmpty(LastExitName))
			return;

		var door = scene.GetNodeOrNull<DoorArea2D>(LastExitName);
		if (door != null)
			PlayerPos = door.PlayerPos;
	}

	// --------------------------------------------------------------------
	// Door / Respawn Helpers
	// --------------------------------------------------------------------
	/// <summary>
	/// Finds the nearest DoorArea2D in a scene to the given position.
	/// Works even if doors are nested inside containers (recursive search).
	/// </summary>
	public static Vector2 FindNearestDoor(Node scene, Vector2 refPos) {
		float minDist = float.MaxValue;
		Vector2 closest = refPos;

		void Search(Node node) {
			foreach (Node child in node.GetChildren()) {
				if (child is DoorArea2D door) {
					float dist = door.GlobalPosition.DistanceTo(refPos);
					if (dist < minDist) {
						minDist = dist;
						closest = door.PlayerPos;
					}
				}
				else if (child.GetChildCount() > 0)
					Search(child);
			}
		}

		Search(scene);
		return closest;
	}

	// ---- Checkpoint respawn helpers ----
	public const string DefaultRoomPath = "res://Scenes/room_01.tscn";

	public static string GetRespawnRoomPath() {
		// Use the last collectable's room if we have one, else default to room_01
		return HasCheckpoint && !string.IsNullOrEmpty(CheckpointRoom)
			? CheckpointRoom
			: DefaultRoomPath;
	}
	// public static Vector2 GetRespawnPositionForLoadedScene(Node loadedScene) {
	// 	if (loadedScene == null) return PlayerPos;
	// 	var refPos = HasCheckpoint ? CheckpointPos : Vector2.Zero;
	// 	return FindNearestDoor(loadedScene, refPos);
	// }
	public static Vector2 GetRespawnPositionForLoadedScene(Node loadedScene) {
		if (loadedScene == null)
			return PlayerPos;

		// Try to find a door that matches our last exit or checkpoint.
		DoorArea2D bestDoor = null;

		// 1. If we came from a door in another room, prefer the connected one.
		if (!string.IsNullOrEmpty(LastExitRoom)) {
			foreach (Node child in loadedScene.GetChildren()) {
				if (child is DoorArea2D door) {
					// Check if this door links back to the room we exited from
					if (!string.IsNullOrEmpty(door.ConnectedRoom) &&
						door.ConnectedRoom == LastExitRoom) {
						bestDoor = door;
						break;
					}
				}
			}
		}

		// 2. If no matching door found, find the *nearest* door to checkpoint position
		if (bestDoor == null) {
			var refPos = HasCheckpoint ? CheckpointPos : Vector2.Zero;
			float bestDist = float.MaxValue;

			void Search(Node node) {
				foreach (Node child in node.GetChildren()) {
					if (child is DoorArea2D door) {
						float dist = door.GlobalPosition.DistanceTo(refPos);
						if (dist < bestDist) {
							bestDist = dist;
							bestDoor = door;
						}
					}
					else if (child.GetChildCount() > 0)
						Search(child);
				}
			}

			Search(loadedScene);
		}

		// 3. If still none found, default to first door in scene
		if (bestDoor == null) {
			bestDoor = loadedScene.GetNodeOrNull<DoorArea2D>("DoorArea2D");
		}

		// 4. If no door exists at all, just use (0,0)
		if (bestDoor == null) {
			GD.PushWarning("[Respawn] No DoorArea2D found — falling back to origin (0,0).");
			return Vector2.Zero;
		}

		// Use the door’s assigned player position
		GD.Print($"[Respawn] Using door '{bestDoor.Name}' @ {bestDoor.PlayerPos}");
		return bestDoor.PlayerPos;
	}


}
