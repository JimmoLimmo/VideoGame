using Godot;
using System.Collections.Generic;

public static class InputLabelFormatter {
	// Xbox-style pretty names (these work great for most controllers)
	private static readonly Dictionary<int, string> PrettyButtonMap = new() {
		{ 0, "A" },
		{ 1, "B" },
		{ 2, "X" },
		{ 3, "Y" },
		{ 4, "LB" },
		{ 5, "RB" },
		{ 6, "LT" },
		{ 7, "RT" },
		{ 8, "Select" },
		{ 9, "Start" },
		{ 10, "L3" },
		{ 11, "R3" },
		{ 12, "D-Up" },
		{ 13, "D-Down" },
		{ 14, "D-Left" },
		{ 15, "D-Right" }
	};

	private static readonly Dictionary<int, string> PrettyAxisMap = new() {
		{ 0, "Left Stick X" },
		{ 1, "Left Stick Y" },
		{ 2, "Right Stick X" },
		{ 3, "Right Stick Y" },
		{ 4, "LT Axis" },
		{ 5, "RT Axis" }
	};

	// Convert a JoyButton ID to a readable label
	public static string PrettyGamepadButton(int index) {
		return PrettyButtonMap.TryGetValue(index, out string name)
			? name
			: $"Button {index}";
	}

	// Convert a JoyAxis ID to a readable label
	public static string PrettyGamepadAxis(int index) {
		return PrettyAxisMap.TryGetValue(index, out string name)
			? name
			: $"Axis {index}";
	}

	// Optionally map keyboard keycodes to shorter, UI-friendly labels
	public static string PrettyKey(Key keycode) {
		return keycode switch {
			Key.Space => "Space",
			Key.Shift => "Shift",
			Key.Ctrl => "Ctrl",
			Key.Alt => "Alt",
			Key.Up => "↑",
			Key.Down => "↓",
			Key.Left => "←",
			Key.Right => "→",
			Key.Enter => "Enter",
			Key.Escape => "Esc",
			Key.Z => "Z",
			Key.X => "X",
			_ => OS.GetKeycodeString(keycode)
		};
	}
}
