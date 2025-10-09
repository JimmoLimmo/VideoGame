using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class HUD : CanvasLayer {
	[Export] public int MaxMasks { get; set; } = 5;
	[Export] public int size = 150;

	[ExportCategory("Textures")]
	[Export] public Texture2D MaskFull { get; set; }
	[Export] public Texture2D MaskEmpty { get; set; }

	[ExportCategory("Nodes")]
	[Export] public NodePath HealthBoxPath { get; set; } = "Root/TopLeft/Health";

	private HBoxContainer _healthBox;
	private readonly List<TextureRect> _maskIcons = new();

	// --------------------------------------------------------------------
	// Node lifecycle
	// --------------------------------------------------------------------
	public override async void _Ready() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_healthBox = GetNodeOrNull<HBoxContainer>(HealthBoxPath)
			?? FindChild("Health", true, false) as HBoxContainer;

		if (_healthBox == null) {
			GD.PushError("[HUD] ERROR: Health container not found.");
			return;
		}

		GD.Print($"[HUD] Initialized. Found {_healthBox.GetPath()}");
		BuildMaskRow(MaxMasks);
		SetHealth(MaxMasks);

		// Connect to room group changes
		GlobalRoomChange.RoomGroupChanged += OnRoomGroupChanged;

		// Apply correct visibility for the starting scene
		UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
	}

	public override void _ExitTree() {
		// Clean up event subscription to prevent leaks
		GlobalRoomChange.RoomGroupChanged -= OnRoomGroupChanged;
	}

	// --------------------------------------------------------------------
	// Visibility
	// --------------------------------------------------------------------
	private void OnRoomGroupChanged(RoomGroup group) {
		UpdateVisibilityFromGroup(group);
	}

	public void UpdateVisibilityFromGroup(RoomGroup group) {
		switch (group) {
			case RoomGroup.Title:
				Visible = false;
				break;
			case RoomGroup.Overworld:
			case RoomGroup.Boss:
				Visible = true;
				break;
		}
		GD.Print($"[HUD] Visibility updated for group: {group} -> Visible={Visible}");
	}

	// --------------------------------------------------------------------
	// Health Display
	// --------------------------------------------------------------------
	private void BuildMaskRow(int count) {
		if (_healthBox == null) return;

		foreach (var child in _healthBox.GetChildren())
			(child as Node)?.QueueFree();
		_maskIcons.Clear();

		for (int i = 0; i < count; i++) {
			var icon = new TextureRect {
				StretchMode = TextureRect.StretchModeEnum.Scale,
				CustomMinimumSize = new Vector2(28, 28),
				Size = new Vector2(size, size),
				Texture = MaskEmpty,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_healthBox.AddChild(icon);
			_maskIcons.Add(icon);
		}
	}

	public void SetHealth(int current) {
		if (_healthBox == null) return;

		current = Mathf.Clamp(current, 0, MaxMasks);
		if (_maskIcons.Count != MaxMasks)
			BuildMaskRow(MaxMasks);

		for (int i = 0; i < MaxMasks; i++) {
			_maskIcons[i].Texture = (i < current) ? MaskFull : MaskEmpty;
			_maskIcons[i].Size = new Vector2(size, size);
		}
	}

	public async void FlashDamage() {
		if (_healthBox == null) return;

		var tween = CreateTween();
		var baseMod = _healthBox.Modulate;
		tween.TweenProperty(_healthBox, "modulate", new Color(1, 0.4f, 0.4f, 1), 0.05f);
		tween.TweenProperty(_healthBox, "modulate", baseMod, 0.15f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public void RebuildAndSet(int max, int current) {
		MaxMasks = max;
		BuildMaskRow(MaxMasks);
		SetHealth(current);
	}

	// --------------------------------------------------------------------
	// Manual refresh helper (optional)
	// --------------------------------------------------------------------
	public void RefreshVisibility() {
		UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
	}
}
