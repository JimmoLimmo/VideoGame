using Godot;

public partial class RebindButton : Button {
    [Export] public string ActionName { get; set; } = "";

    private bool _waitingForInput = false;

    public override void _Ready() {
        UpdateButtonLabel();
        Pressed += OnPressed;
    }

    private void OnPressed() {
        GD.Print($"[RebindButton] Waiting for new input for '{ActionName}'");
        Text = $"Press any key/button for {ActionName}...";
        _waitingForInput = true;
    }

    public override void _Input(InputEvent @event) {
        if (!_waitingForInput) return;

        // Handle keyboard keys
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.Escape) { // cancel
                GD.Print("[RebindButton] Rebinding canceled.");
                UpdateButtonLabel();
                _waitingForInput = false;
                return;
            }

            ApplyNewBinding(keyEvent, OS.GetKeycodeString(keyEvent.Keycode));
        }

        // Handle gamepad buttons
        else if (@event is InputEventJoypadButton joyButton && joyButton.Pressed) {
            ApplyNewBinding(joyButton, $"JoyBtn {joyButton.ButtonIndex}");
        }

        // Handle joystick axes (e.g. triggers)
        else if (@event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.5f) {
            ApplyNewBinding(motion, $"JoyAxis {motion.Axis} {(motion.AxisValue > 0 ? "+" : "-")}");
        }
    }

    private void ApplyNewBinding(InputEvent evt, string label) {
        InputMap.ActionEraseEvents(ActionName);
        InputMap.ActionAddEvent(ActionName, evt);
        Text = $"{ActionName}: {label}";
        _waitingForInput = false;

        // Automatically save via InputManager autoload
        var im = GetNodeOrNull<InputManager>("/root/InputManager");
        im?.SaveBindings();

        GD.Print($"[RebindButton] '{ActionName}' rebound to {label} and saved.");
    }


    private void UpdateButtonLabel() {
        var events = InputMap.ActionGetEvents(ActionName);
        foreach (var e in events) {
            if (e is InputEventKey key)
                Text = $"{ActionName}: {OS.GetKeycodeString(key.Keycode)}";
            else if (e is InputEventJoypadButton joy)
                Text = $"{ActionName}: JoyBtn {joy.ButtonIndex}";
            else if (e is InputEventJoypadMotion motion)
                Text = $"{ActionName}: JoyAxis {motion.Axis}";
        }

        if (events.Count == 0)
            Text = $"{ActionName}: None";
    }
}
