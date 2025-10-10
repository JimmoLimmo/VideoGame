using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class HUD : CanvasLayer {
	[Export] public int MaxMasks { get; set; } = 5;
	[Export(PropertyHint.Range, "16,256,1")] public int MaskSize { get; set; } = 64;

	[ExportCategory("Textures")]
	[Export] public Texture2D MaskFull { get; set; }
	[Export] public Texture2D MaskEmpty { get; set; }

	[ExportCategory("Nodes")]
	[Export] public NodePath HealthBoxPath { get; set; } = "Root/TopLeft/Health";

	private HBoxContainer _healthBox;
	private readonly List<TextureRect> _maskIcons = new();

	private bool _initialized = false;

	public override void _Ready() {
		InitializeHUD(); // call once
		GlobalRoomChange.RoomGroupChanged += OnRoomGroupChanged;
		UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
	}

	public override void _ExitTree() {
		GlobalRoomChange.RoomGroupChanged -= OnRoomGroupChanged;
	}

	// --------------------------------------------------------------
	// Initialization (handles reparenting when rooms reload)
	// --------------------------------------------------------------
	private void InitializeHUD() {
		// Skip if we already did a successful build
		if (_initialized && IsInstanceValid(_healthBox)) return;

		_healthBox = GetNodeOrNull<HBoxContainer>(HealthBoxPath)
			?? FindChild("Health", true, false) as HBoxContainer;

		if (_healthBox == null) {
			GD.PushWarning("[HUD] HealthBox not found yet, will retry on next frame...");
			// Try again next frame in case the new scene tree hasn't loaded
			CallDeferred(nameof(InitializeHUD));
			return;
		}

		GD.Print($"[HUD] Found {_healthBox.GetPath()}");
		BuildMaskRow(MaxMasks);
		SetHealth(MaxMasks);
		_initialized = true;
	}

	// --------------------------------------------------------------
	// Visibility
	// --------------------------------------------------------------
	private void OnRoomGroupChanged(RoomGroup group) => UpdateVisibilityFromGroup(group);

	public void UpdateVisibilityFromGroup(RoomGroup group) {
		Visible = group == RoomGroup.Overworld || group == RoomGroup.Boss;
	}

	// --------------------------------------------------------------
	// Health mask management
	// --------------------------------------------------------------
	private void BuildMaskRow(int count) {
		if (_healthBox == null) return;

		foreach (var c in _healthBox.GetChildren())
			c.QueueFree();
		_maskIcons.Clear();

		for (int i = 0; i < count; i++) {
			var icon = new TextureRect {
				StretchMode = TextureRect.StretchModeEnum.KeepCentered,
				CustomMinimumSize = new Vector2(MaskSize, MaskSize),
				Texture = MaskEmpty,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_healthBox.AddChild(icon);
			_maskIcons.Add(icon);
		}
	}

	public void SetHealth(int current) {
		InitializeHUD(); // ensure the box is valid even after scene changes

		if (_healthBox == null || _maskIcons.Count == 0)
			return;

		current = Mathf.Clamp(current, 0, MaxMasks);

		for (int i = 0; i < MaxMasks; i++) {
			var icon = _maskIcons[i];
			icon.Texture = (i < current) ? MaskFull : MaskEmpty;
		}
	}

	// --------------------------------------------------------------
	// Utilities
	// --------------------------------------------------------------
	public async Task FlashDamage() {
		if (_healthBox == null) return;
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_healthBox, "modulate", new Color(1, 0.4f, 0.4f, 1), 0.05f);
		tween.TweenProperty(_healthBox, "modulate", Colors.White, 0.15f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public void RebuildAndSet(int max, int current) {
		MaxMasks = max;
		BuildMaskRow(MaxMasks);
		SetHealth(current);
	}

	public void RefreshVisibility() => UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
}
