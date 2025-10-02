using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveManager : Node
{
    private static string SavePath = "user://savegame.json";

    public class SaveData
    {
        public int Hp { get; set; }
        public bool HasSword { get; set; }
        public bool HasDash { get; set; }
        public bool HasWalljump { get; set; }
        public Vector2 PlayerPosition { get; set; }
    }

    public static void Save(SaveData data)
    {
        // Convert to a Godot-friendly Dictionary so JSON round-trips predictably
        var dict = new Godot.Collections.Dictionary
        {
            ["Hp"] = data.Hp,
            ["HasSword"] = data.HasSword,
            ["HasDash"] = data.HasDash,
            ["HasWalljump"] = data.HasWalljump,
            ["PlayerPosition"] = new Godot.Collections.Dictionary
            {
                ["x"] = data.PlayerPosition.X,
                ["y"] = data.PlayerPosition.Y
            }
        };

        var json = Json.Stringify(dict);
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static SaveData Load()
    {
        if (!FileAccess.FileExists(SavePath))
            return null; // No save yet

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        var text = file.GetAsText();
        // Use System.Text.Json to parse the JSON text (avoids Godot Variant casting issues)
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            int hp = 0;
            if (root.TryGetProperty("Hp", out var hpEl) && hpEl.ValueKind != JsonValueKind.Null)
                hp = hpEl.GetInt32();

            bool hasSword = false;
            if (root.TryGetProperty("HasSword", out var swordEl) && swordEl.ValueKind != JsonValueKind.Null)
                hasSword = swordEl.GetBoolean();

            bool hasDash = false;
            if (root.TryGetProperty("HasDash", out var dashEl) && dashEl.ValueKind != JsonValueKind.Null)
                hasDash = dashEl.GetBoolean();

            bool hasWalljump = false;
            if (root.TryGetProperty("HasWalljump", out var wallEl) && wallEl.ValueKind != JsonValueKind.Null)
                hasWalljump = wallEl.GetBoolean();

            Vector2 playerPos = Vector2.Zero;
            if (root.TryGetProperty("PlayerPosition", out var posEl) && posEl.ValueKind == JsonValueKind.Object)
            {
                float x = 0f, y = 0f;
                if (posEl.TryGetProperty("x", out var xEl) && xEl.ValueKind != JsonValueKind.Null)
                    x = xEl.GetSingle();
                if (posEl.TryGetProperty("y", out var yEl) && yEl.ValueKind != JsonValueKind.Null)
                    y = yEl.GetSingle();
                playerPos = new Vector2(x, y);
            }

            return new SaveData
            {
                Hp = hp,
                HasSword = hasSword,
                HasDash = hasDash,
                HasWalljump = hasWalljump,
                PlayerPosition = playerPos
            };
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to parse save JSON with System.Text.Json: {e.Message}");
            return null;
        }
    }
}
