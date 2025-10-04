using Godot;

public partial class GlobalRoomChange : Node {
	public static bool Activate {get; set;} = false;
	public static Vector2 PlayerPos {get; set;} = new Vector2();
	public static bool PlayerJumpOnEnter {get; set;} = false;
	
	public static bool hasSword;
	public static bool hasDash;
	public static bool hasWalljump;
}
