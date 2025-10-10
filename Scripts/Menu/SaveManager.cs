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
        public bool HasSwordTeleport { get; set; }
        public Vector2 PlayerPosition { get; set; }
        // List of collected item IDs (editor-assigned SaveID on Collectable nodes)
        public System.Collections.Generic.List<string> CollectedItems { get; set; }
    }

    public static void Save(SaveData data)
    {
        if (data == null) return;

        // If the caller didn't provide CollectedItems, try to preserve any already-known list
        if (data.CollectedItems == null)
        {
            // Prefer the cached save
            if (_cachedSave != null && _cachedSave.CollectedItems != null)
            {
                data.CollectedItems = new System.Collections.Generic.List<string>(_cachedSave.CollectedItems);
            }
            else
            {
                // Fallback: try to load from disk and preserve
                try
                {
                    var existing = Load();
                    if (existing != null && existing.CollectedItems != null)
                        data.CollectedItems = new System.Collections.Generic.List<string>(existing.CollectedItems);
                }
                catch { }
            }
        }
        // Convert to a Godot-friendly Dictionary so JSON round-trips predictably
        var dict = new Godot.Collections.Dictionary
        {
            ["Hp"] = data.Hp,
            ["HasSword"] = data.HasSword,
            ["HasDash"] = data.HasDash,
            ["HasWalljump"] = data.HasWalljump,
            ["HasSwordTeleport"] = data.HasSwordTeleport,
            ["PlayerPosition"] = new Godot.Collections.Dictionary
            {
                ["x"] = data.PlayerPosition.X,
                ["y"] = data.PlayerPosition.Y
            }
        };

            // Collected items: serialize as an array of strings if present
            if (data.CollectedItems != null)
            {
                var arr = new Godot.Collections.Array();
                foreach (var id in data.CollectedItems)
                    arr.Add(id);
                dict["CollectedItems"] = arr;
            }

        var json = Json.Stringify(dict);
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file.StoreString(json);

        // Update cached save reference so subsequent operations see the new state
        _cachedSave = data;
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

            bool hasSwordTeleport = false;
            if (root.TryGetProperty("HasSwordTeleport", out var swordTeleportEl) && swordTeleportEl.ValueKind != JsonValueKind.Null)
                hasSwordTeleport = swordTeleportEl.GetBoolean();

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

            var save = new SaveData
            {
                Hp = hp,
                HasSword = hasSword,
                HasDash = hasDash,
                HasWalljump = hasWalljump,
                HasSwordTeleport = hasSwordTeleport,
                PlayerPosition = playerPos,
                CollectedItems = new System.Collections.Generic.List<string>()
            };

            // Read collected items array if present
            if (root.TryGetProperty("CollectedItems", out var colEl) && colEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var itemEl in colEl.EnumerateArray())
                {
                    if (itemEl.ValueKind == JsonValueKind.String)
                    {
                        save.CollectedItems.Add(itemEl.GetString());
                    }
                }
            }

            return save;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to parse save JSON with System.Text.Json: {e.Message}");
            return null;
        }
    }

    // --- Convenience API (cached save + helpers) -------------------------
    private static SaveData _cachedSave = null;

    // Returns the in-memory save (loads from disk if necessary). Never null.
    public static SaveData GetCurrentSave()
    {
        if (_cachedSave != null) return _cachedSave;
    _cachedSave = Load() ?? new SaveData { CollectedItems = new System.Collections.Generic.List<string>() };
        if (_cachedSave.CollectedItems == null)
            _cachedSave.CollectedItems = new System.Collections.Generic.List<string>();
        return _cachedSave;
    }

    public static bool HasCollectedItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var s = GetCurrentSave();
        return s.CollectedItems != null && s.CollectedItems.Contains(id);
    }

    // Adds an item to the collected list. If persistNow is true, the file is written immediately.
    public static void AddCollectedItem(string id, bool persistNow = true)
    {
        if (string.IsNullOrEmpty(id)) return;
        var s = GetCurrentSave();
        if (s.CollectedItems == null) s.CollectedItems = new System.Collections.Generic.List<string>();
        if (!s.CollectedItems.Contains(id))
            s.CollectedItems.Add(id);
        if (persistNow)
            Save(s);
    }

    // Force-write the cached save to disk (no-op if nothing cached)
    public static void SaveNow()
    {
        if (_cachedSave != null)
            Save(_cachedSave);
    }

    /// <summary>
    /// Clear the in-memory cached save and optionally delete the save file on disk.
    /// Use this to start a fresh game state (e.g. when the player presses Start New Game).
    /// </summary>
    public static void ResetToNewGame(bool deleteFile = true)
    {
        _cachedSave = new SaveData
        {
            Hp = 5,
            HasSword = false,
            HasDash = false,
            HasWalljump = false,
            HasSwordTeleport = false,
            PlayerPosition = Vector2.Zero,
            CollectedItems = new System.Collections.Generic.List<string>()
        };

        if (deleteFile)
        {
            try
            {
                var abs = ProjectSettings.GlobalizePath(SavePath);
                if (System.IO.File.Exists(abs))
                    System.IO.File.Delete(abs);
            }
            catch (Exception e)
            {
                GD.PrintErr($"SaveManager: ResetToNewGame failed to delete save file: {e.Message}");
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// Debug method to print current save state
    /// </summary>
    public static void DebugPrintSaveState()
    {
        var save = GetCurrentSave();
        GD.Print("=== Current Save State ===");
        GD.Print($"HP: {save.Hp}");
        GD.Print($"Has Sword: {save.HasSword}");
        GD.Print($"Has Dash: {save.HasDash}");
        GD.Print($"Has Wall Jump: {save.HasWalljump}");
        GD.Print($"Player Position: {save.PlayerPosition}");
        GD.Print($"Collected Items ({save.CollectedItems?.Count ?? 0}):");
        if (save.CollectedItems != null)
        {
            foreach (var item in save.CollectedItems)
            {
                GD.Print($"  - {item}");
            }
        }
        GD.Print("========================");
    }
}
