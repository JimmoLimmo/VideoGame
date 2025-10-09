using Godot;
using System.Collections.Generic;

public partial class InputManager : Node {
    [Signal] public delegate void BindingsUpdatedEventHandler();

    private const string BindingsPath = "user://bindings.json";

    private readonly Godot.Collections.Dictionary<string, Godot.Collections.Array<InputEvent>> _defaultBindings = new();
    private bool _capturedDefaults = false;

    public override void _Ready() {
        CaptureDefaultsFromProjectSettings(); // capture project defaults first
        LoadBindings(); // then try loading custom bindings
    }

    // ------------------------------------------------------------
    // Capture true defaults from Project Settings
    // ------------------------------------------------------------
    private void CaptureDefaultsFromProjectSettings() {
        if (_capturedDefaults) return;

        foreach (string action in InputMap.GetActions()) {
            var arr = new Godot.Collections.Array<InputEvent>();
            foreach (var e in InputMap.ActionGetEvents(action))
                arr.Add((InputEvent)e.Duplicate());
            _defaultBindings[action] = arr;
        }

        _capturedDefaults = true;
        GD.Print("[InputManager] Captured default bindings from live InputMap (Godot 4.4+ safe).");
    }

    // ------------------------------------------------------------
    // Save, Load, Rebind
    // ------------------------------------------------------------
    public void SaveBindings() {
        var data = new Godot.Collections.Dictionary<string, Godot.Collections.Array<Godot.Collections.Dictionary>>();

        foreach (string action in InputMap.GetActions()) {
            var events = InputMap.ActionGetEvents(action);
            var arr = new Godot.Collections.Array<Godot.Collections.Dictionary>();

            foreach (var e in events) {
                if (e is InputEventKey key) {
                    arr.Add(new Godot.Collections.Dictionary {
                        { "type", "key" },
                        { "keycode", (int)key.Keycode },
                        { "keyname", OS.GetKeycodeString(key.Keycode) }
                    });
                }
                else if (e is InputEventJoypadButton joy) {
                    arr.Add(new Godot.Collections.Dictionary {
                        { "type", "joy_button" },
                        { "button", (int)joy.ButtonIndex }
                    });
                }
                else if (e is InputEventJoypadMotion motion) {
                    arr.Add(new Godot.Collections.Dictionary {
                        { "type", "joy_motion" },
                        { "axis", (int)motion.Axis },
                        { "value", motion.AxisValue }
                    });
                }
                else if (e is InputEventMouseButton mouse) {
                    arr.Add(new Godot.Collections.Dictionary {
                        { "type", "mouse_button" },
                        { "button_index", (int)mouse.ButtonIndex }
                    });
                }
            }
            data[action] = arr;
        }

        string json = Json.Stringify(data, "\t");
        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
        GD.Print("[InputManager] Saved bindings to:", BindingsPath);

        EmitSignal(SignalName.BindingsUpdated);
    }

    public void LoadBindings() {
        if (!FileAccess.FileExists(BindingsPath)) {
            GD.Print("[InputManager] No saved bindings — using captured defaults.");
            RestoreDefaults();
            return;
        }

        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        var parsed = Json.ParseString(json);

        if (parsed.VariantType != Variant.Type.Dictionary) {
            GD.PushWarning("[InputManager] Invalid binding file format — ignoring.");
            RestoreDefaults();
            return;
        }

        var dict = parsed.AsGodotDictionary<string, Godot.Collections.Array<Godot.Collections.Dictionary>>();

        foreach (var kv in dict) {
            string action = kv.Key;
            var arr = kv.Value;
            if (!InputMap.HasAction(action)) continue;

            // If no events were stored for this action, restore defaults
            if (arr.Count == 0) {
                if (action.StartsWith("ui_text_"))
                    continue; // Skip engine/editor text actions

                GD.PushWarning($"[InputManager] Action '{action}' had no bindings, restoring default.");
                if (_defaultBindings.ContainsKey(action)) {
                    foreach (var e in _defaultBindings[action])
                        InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());
                }
            }


            InputMap.ActionEraseEvents(action);

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
                    case "mouse_button":
                        InputMap.ActionAddEvent(action, new InputEventMouseButton {
                            ButtonIndex = (MouseButton)eDict["button_index"].AsInt32()
                        });
                        break;
                }
            }
        }

        GD.Print("[InputManager] Loaded custom bindings successfully.");
        EmitSignal(SignalName.BindingsUpdated);
    }

    public void RebindAction(string actionName, InputEvent evt) {
        if (!InputMap.HasAction(actionName)) {
            GD.PushWarning($"[InputManager] Unknown action '{actionName}' — cannot rebind.");
            return;
        }

        InputMap.ActionEraseEvents(actionName);
        InputMap.ActionAddEvent(actionName, evt);
        SaveBindings();
        GD.Print($"[InputManager] '{actionName}' rebound and saved.");
    }

    // ------------------------------------------------------------
    // Restore defaults correctly
    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // Safe Reset & Restore
    // ------------------------------------------------------------
    public async void ResetToDefaults() {
        GD.Print("[InputManager] Resetting to true project defaults...");

        // 1. Reload all input definitions from project settings
        InputMap.LoadFromProjectSettings();

        // 2. Rebuild the internal cache of defaults for consistency
        _defaultBindings.Clear();
        foreach (string action in InputMap.GetActions()) {
            var arr = new Godot.Collections.Array<InputEvent>();
            foreach (var e in InputMap.ActionGetEvents(action))
                arr.Add((InputEvent)e.Duplicate());
            _defaultBindings[action] = arr;
        }

        // 3. Wait one frame for stability (Godot 4 syntax)
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // 4. Save & emit event so UI refreshes and JSON is accurate
        SaveBindings();
        EmitSignal(SignalName.BindingsUpdated);

        GD.Print("[InputManager] Defaults restored, saved, and synchronized.");
    }


    private void RestoreDefaults() {
        // Clear all existing bindings first
        foreach (string action in InputMap.GetActions())
            InputMap.ActionEraseEvents(action);

        foreach (var kv in _defaultBindings) {
            string action = kv.Key;
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);

            foreach (var e in kv.Value)
                InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());
        }

        GD.Print("[InputManager] Defaults reapplied to InputMap.");
        EmitSignal(SignalName.BindingsUpdated);
    }


    public override void _ExitTree() {
        SaveBindings();
    }
}
