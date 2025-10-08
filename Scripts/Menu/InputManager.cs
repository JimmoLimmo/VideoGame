using Godot;
using System.Collections.Generic;

public partial class InputManager : Node {
    private const string BindingsPath = "user://bindings.json";

    public override void _Ready() {
        LoadBindings();
    }

    public void SaveBindings() {
        var data = new Godot.Collections.Dictionary<string, Godot.Collections.Array<Godot.Collections.Dictionary>>();

        foreach (string action in InputMap.GetActions()) {
            var events = InputMap.ActionGetEvents(action);
            var arr = new Godot.Collections.Array<Godot.Collections.Dictionary>();

            foreach (var e in events) {
                if (e is InputEventKey keyEvent) {
                    var dict = new Godot.Collections.Dictionary
                    {
                        { "type", "key" },
                        { "keycode", (int)keyEvent.Keycode },
                        { "keyname", OS.GetKeycodeString(keyEvent.Keycode) } // optional readability
					};
                    arr.Add(dict);
                }
                else if (e is InputEventJoypadButton joyButton) {
                    var dict = new Godot.Collections.Dictionary
                    {
                        { "type", "joy_button" },
                        { "button", (int)joyButton.ButtonIndex } // enum → int
					};
                    arr.Add(dict);
                }
                else if (e is InputEventJoypadMotion motion) {
                    var dict = new Godot.Collections.Dictionary
                    {
                        { "type", "joy_motion" },
                        { "axis", (int)motion.Axis },   // enum → int
						{ "value", motion.AxisValue }
                    };
                    arr.Add(dict);
                }
                else if (e is InputEventMouseButton mouseButton) {
                    var dict = new Godot.Collections.Dictionary
                    {
                        { "type", "mouse_button" },
                        { "button_index", (int)mouseButton.ButtonIndex }
                    };
                    arr.Add(dict);
                }
            }

            data[action] = arr;
        }

        string json = Json.Stringify(data, "\t");
        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);

        GD.Print("[InputManager] Saved bindings to:", BindingsPath);
    }

    public void LoadBindings() {
        if (!FileAccess.FileExists(BindingsPath)) {
            GD.Print("[InputManager] No saved bindings — using defaults.");
            return;
        }

        using var file = FileAccess.Open(BindingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        var parsed = Json.ParseString(json);

        if (parsed.VariantType != Variant.Type.Dictionary) {
            GD.PushWarning("[InputManager] Invalid binding file format — ignoring.");
            return;
        }

        var dict = parsed.AsGodotDictionary<string, Godot.Collections.Array<Godot.Collections.Dictionary>>();

        foreach (var kv in dict) {
            string action = kv.Key;
            var arr = kv.Value;

            if (!InputMap.HasAction(action)) {
                GD.PushWarning($"[InputManager] Skipping unknown action '{action}'.");
                continue;
            }

            InputMap.ActionEraseEvents(action);

            foreach (var eDict in arr) {
                string type = eDict["type"].AsString();

                if (type == "key") {
                    var keyEvt = new InputEventKey {
                        Keycode = (Key)eDict["keycode"].AsInt32(),
                        Pressed = false
                    };
                    InputMap.ActionAddEvent(action, keyEvt);
                }
                else if (type == "joy_button") {
                    var joyEvt = new InputEventJoypadButton {
                        ButtonIndex = (JoyButton)eDict["button"].AsInt32() // int → enum
                    };
                    InputMap.ActionAddEvent(action, joyEvt);
                }
                else if (type == "joy_motion") {
                    var motionEvt = new InputEventJoypadMotion {
                        Axis = (JoyAxis)eDict["axis"].AsInt32(),           // int → enum
                        AxisValue = eDict["value"].AsSingle()
                    };
                    InputMap.ActionAddEvent(action, motionEvt);
                }
                else if (type == "mouse_button") {
                    var mouseEvt = new InputEventMouseButton {
                        ButtonIndex = (MouseButton)eDict["button_index"].AsInt32()
                    };
                    InputMap.ActionAddEvent(action, mouseEvt);
                }
            }
        }

        GD.Print("[InputManager] Loaded custom bindings successfully.");
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

    public override void _ExitTree() {
        SaveBindings(); // auto-save on quit
    }
}
