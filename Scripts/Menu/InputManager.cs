using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class InputManager : Node {
    [Signal] public delegate void BindingsUpdatedEventHandler();
    private double _lastDeviceSwitchTime = 0;
    private const double DeviceSwitchCooldown = 0.5; // seconds

    private const string BindingsPath = "user://bindings.json";

    // Default actions from project settings
    private readonly Godot.Collections.Dictionary<string, Godot.Collections.Array<InputEvent>> _defaultBindings = new();
    private bool _capturedDefaults = false;

    // Track last used device
    public static bool UsingGamepad { get; private set; } = false;

    public override void _Ready() {
        CaptureDefaultsFromProjectSettings();
        LoadBindings();
        EnsureUiActions();
    }

    // --------------------------------------------------------------------
    // Capture project defaults from current InputMap
    // --------------------------------------------------------------------
    private void CaptureDefaultsFromProjectSettings() {
        if (_capturedDefaults) return;

        foreach (string action in InputMap.GetActions()) {
            var arr = new Godot.Collections.Array<InputEvent>();
            foreach (var e in InputMap.ActionGetEvents(action))
                arr.Add((InputEvent)e.Duplicate());
            _defaultBindings[action] = arr;
        }

        _capturedDefaults = true;
        GD.Print("[InputManager] Captured default bindings from live InputMap.");
    }

    // --------------------------------------------------------------------
    // Save bindings to JSON (keyboard/gamepad separated)
    // --------------------------------------------------------------------
    public void SaveBindings() {
        // Use non-generic dictionaries everywhere for safety
        var root = new Godot.Collections.Dictionary();
        var keyboard = new Godot.Collections.Dictionary();
        var gamepad = new Godot.Collections.Dictionary();

        foreach (string action in InputMap.GetActions()) {
            var events = InputMap.ActionGetEvents(action);

            var keyArr = new Godot.Collections.Array();
            var joyArr = new Godot.Collections.Array();

            foreach (var e in events) {
                if (e is InputEventKey key) {
                    keyArr.Add(new Godot.Collections.Dictionary {
                    { "type", "key" },
                    { "keycode", (int)key.Keycode },
                    { "keyname", OS.GetKeycodeString(key.Keycode) }
                });
                }
                else if (e is InputEventJoypadButton joy) {
                    joyArr.Add(new Godot.Collections.Dictionary {
                    { "type", "joy_button" },
                    { "button", (int)joy.ButtonIndex }
                });
                }
                else if (e is InputEventJoypadMotion motion) {
                    joyArr.Add(new Godot.Collections.Dictionary {
                    { "type", "joy_motion" },
                    { "axis", (int)motion.Axis },
                    { "value", motion.AxisValue }
                });
                }
            }

            if (keyArr.Count > 0)
                keyboard[action] = keyArr;
            if (joyArr.Count > 0)
                gamepad[action] = joyArr;
        }


        root["keyboard"] = keyboard;
        root["gamepad"] = gamepad;

        string json = Json.Stringify(root, "\t");
        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);

        GD.Print("[InputManager] Saved bindings to:", BindingsPath);
        EmitSignal(SignalName.BindingsUpdated);
    }


    // --------------------------------------------------------------------
    // Load saved bindings
    // --------------------------------------------------------------------
    public void LoadBindings() {
        if (!FileAccess.FileExists(BindingsPath)) {
            GD.Print("[InputManager] No saved bindings — using defaults.");
            RestoreDefaults();
            return;
        }

        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        var parsed = Json.ParseString(json);

        if (parsed.VariantType != Variant.Type.Dictionary) {
            GD.PushWarning("[InputManager] Invalid binding file — restoring defaults.");
            RestoreDefaults();
            return;
        }

        // Root is a Variant -> non-generic Dictionary (string -> Dictionary)
        var root = parsed.AsGodotDictionary(); // Godot.Collections.Dictionary

        // Erase only gameplay/UI actions, keep engine text-edit actions intact
        foreach (string action in InputMap.GetActions()) {
            if (action.StartsWith("ui_text_"))
                continue;
            InputMap.ActionEraseEvents(action);
        }

        foreach (string device in new[] { "keyboard", "gamepad" }) {
            if (!root.ContainsKey(device)) continue;

            // deviceDict: Variant -> non-generic Dictionary (action -> Array<Dictionary>)
            var deviceDict = root[device].AsGodotDictionary(); // Godot.Collections.Dictionary

            // Iterate non-generic dictionary → KeyValuePair<Variant, Variant>
            foreach (var kv in deviceDict) {
                string action = kv.Key.AsString();

                if (!InputMap.HasAction(action))
                    InputMap.AddAction(action);

                // arr: Array of Dictionary entries describing InputEvents
                var arr = kv.Value.AsGodotArray<Godot.Collections.Dictionary>();

                foreach (var eDict in arr) {
                    string type = eDict["type"].AsString();

                    switch (type) {
                        case "key":
                            InputMap.ActionAddEvent(action, new InputEventKey {
                                Keycode = (Key)eDict["keycode"].AsInt32()
                            });
                            break;

                        case "joy_button":
                            InputMap.ActionAddEvent(action, new InputEventJoypadButton {
                                ButtonIndex = (JoyButton)eDict["button"].AsInt32()
                            });
                            break;

                        case "joy_motion":
                            InputMap.ActionAddEvent(action, new InputEventJoypadMotion {
                                Axis = (JoyAxis)eDict["axis"].AsInt32(),
                                AxisValue = eDict["value"].AsSingle()
                            });
                            break;
                    }
                }
            }
        }
        EnsureUiActions();

        GD.Print("[InputManager] Loaded custom bindings successfully.");
        EmitSignal(SignalName.BindingsUpdated);
    }

    // --------------------------------------------------------------------
    // Reset and restore
    // --------------------------------------------------------------------
    public async void ResetToDefaults() {
        GD.Print("[InputManager] Resetting to true project defaults...");

        // Reload from project settings
        InputMap.LoadFromProjectSettings();

        // Rebuild local cache
        _defaultBindings.Clear();
        foreach (string action in InputMap.GetActions()) {
            var arr = new Godot.Collections.Array<InputEvent>();
            foreach (var e in InputMap.ActionGetEvents(action))
                arr.Add((InputEvent)e.Duplicate());
            _defaultBindings[action] = arr;
        }

        // Wait two frames to allow InputMap updates to propagate
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        SaveBindings();
        EmitSignal(SignalName.BindingsUpdated);
        GD.Print("[InputManager] Defaults restored, saved, and synchronized.");
    }

    private void RestoreDefaults() {
        // Fully clear and rebuild using captured defaults
        foreach (string action in InputMap.GetActions())
            InputMap.ActionEraseEvents(action);

        foreach (var kv in _defaultBindings) {
            string action = kv.Key;
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
            foreach (var e in kv.Value)
                InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());
        }
        EnsureUiActions();

        GD.Print("[InputManager] Defaults reapplied to InputMap.");
        EmitSignal(SignalName.BindingsUpdated);
    }

    // --------------------------------------------------------------------
    // Detect active device and notify UI
    // --------------------------------------------------------------------
    // --------------------------------------------------------------------
    // Detect active input device (keyboard vs. gamepad) without breaking UI focus
    // --------------------------------------------------------------------
    [Signal] public delegate void DeviceChangedEventHandler(bool usingGamepad);

    private const float AnalogDeadzone = 0.6f; // prevent stick drift noise

    public override void _Input(InputEvent e) {
        double now = Time.GetTicksMsec() / 1000.0;
        if (now - _lastDeviceSwitchTime < DeviceSwitchCooldown)
            return; // avoid spamming switches from jitter

        bool gamepadSignal = false;
        bool keyboardSignal = false;

        switch (e) {
            case InputEventJoypadButton jb when jb.Pressed:
                gamepadSignal = true;
                break;

            case InputEventJoypadMotion jm:
                if (Mathf.Abs(jm.AxisValue) > AnalogDeadzone)
                    gamepadSignal = true;
                break;

            case InputEventKey key when key.Pressed:
                keyboardSignal = true;
                break;

            case InputEventMouseButton mb when mb.Pressed:
                keyboardSignal = true;
                break;
        }

        // Switch device type only when actually changing
        if (gamepadSignal && !UsingGamepad) {
            UsingGamepad = true;
            _lastDeviceSwitchTime = now;
            GD.Print("[InputManager] Switched to gamepad input.");
            EmitSignal(SignalName.DeviceChanged, true);
        }
        else if (keyboardSignal && UsingGamepad) {
            UsingGamepad = false;
            _lastDeviceSwitchTime = now;
            GD.Print("[InputManager] Switched to keyboard input.");
            EmitSignal(SignalName.DeviceChanged, false);
        }
    }

    // --------------------------------------------------------------------
    // Ensure built-in UI navigation actions always exist
    // --------------------------------------------------------------------
    private void EnsureUiActions() {
        string[] uiActions = {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "ui_accept", "ui_cancel"
    };

        foreach (var action in uiActions) {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        // Assign reasonable defaults if they’re empty
        AddDefaultKey("ui_up", Key.Up);
        AddDefaultKey("ui_down", Key.Down);
        AddDefaultKey("ui_left", Key.Left);
        AddDefaultKey("ui_right", Key.Right);
        AddDefaultKey("ui_accept", Key.Enter);
        AddDefaultKey("ui_cancel", Key.Escape);
    }

    private void AddDefaultKey(string action, Key key) {
        var events = InputMap.ActionGetEvents(action);
        if (events.Count == 0) {
            var e = new InputEventKey { Keycode = key };
            InputMap.ActionAddEvent(action, e);
        }
    }

    // --------------------------------------------------------------------
    // Cleanup and save bindings on exit
    // --------------------------------------------------------------------
    public override void _ExitTree() => SaveBindings();

}
