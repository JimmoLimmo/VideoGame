using Godot;
using System.Collections.Generic;

public enum RoomGroup
{
	Title,
	Overworld,
	Boss
}

public partial class GlobalRoomChange : Node
{
	public static bool Activate { get; set; } = false;
	public static Vector2 PlayerPos { get; set; } = new Vector2();
	public static bool PlayerJumpOnEnter { get; set; } = false;

	public static bool hasSword;
	public static bool hasDash;
	public static bool hasWalljump;

	public static Dictionary<string, bool> destroyedWalls = new();

	public static void MarkWallBroken(string wallId)
	{
		destroyedWalls[wallId] = true;
	}

	public static bool IsWallBroken(string wallId)
	{
		return destroyedWalls.ContainsKey(wallId) && destroyedWalls[wallId];
	}

	public static string CurrentRoom { get; private set; } = "";
	public static RoomGroup CurrentGroup { get; private set; } = RoomGroup.Title;

	private Node _lastScene; // track scene changes

	public override void _Ready()
	{
		// keep running even if the tree is paused
		this.ProcessMode = Node.ProcessModeEnum.Always;
	}

	public override void _Process(double delta)
	{
		var scene = GetTree().CurrentScene;
		if (scene == _lastScene || scene == null) return;
		_lastScene = scene;

		// Determine group by editor tags 
		RoomGroup group = DetectGroup(scene);

		// A readable room name 
		string roomName = !string.IsNullOrEmpty(scene.SceneFilePath)
			? System.IO.Path.GetFileNameWithoutExtension(scene.SceneFilePath)
			: scene.Name;

		EnterRoom(roomName, group);
	}

	private static RoomGroup DetectGroup(Node scene)
	{
		if (scene.IsInGroup("music_title")) return RoomGroup.Title;
		if (scene.IsInGroup("music_overworld")) return RoomGroup.Overworld;
		if (scene.IsInGroup("music_boss")) return RoomGroup.Boss;

		// Fallback heuristic if noo tag:
		var path = scene.SceneFilePath ?? "";
		if (path.Contains("room_")) return RoomGroup.Overworld;
		if (path.ToLower().Contains("boss")) return RoomGroup.Boss;
		if (path.ToLower().Contains("title")) return RoomGroup.Title;

		return RoomGroup.Overworld; // sensible default
	}

	public static void EnterRoom(string roomName, RoomGroup group)
	{
		CurrentRoom = roomName;
		CurrentGroup = group;

		var mm = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull<MusicManager>("MusicManager");
		if (mm == null) return;

		switch (group)
		{
			case RoomGroup.Title:
				if (!mm.IsPlaying(BgmTrack.Title)) mm.Play(BgmTrack.Title);
				break;

			case RoomGroup.Overworld:
				if (!mm.IsPlaying(BgmTrack.Overworld)) mm.Play(BgmTrack.Overworld);
				break;

			case RoomGroup.Boss:
				mm.StartBoss(); // override; call EndBoss() when done
				break;
		}
	}


}
