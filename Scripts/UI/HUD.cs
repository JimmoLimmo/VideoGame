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
	private bool _built = false;

	// ------------------------------------------------------------
	// Lifecycle
	// ------------------------------------------------------------
	public override void _Ready() {
		// Autoloads start before most scenes exist — so we don’t build here.
		GlobalRoomChange.RoomGroupChanged += OnRoomGroupChanged;
		UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
	}

	public override void _Process(double delta) {
		// Try once per frame until built successfully
		if (!_built)
			TryBuildHUD();
	}

	public override void _ExitTree() {
		GlobalRoomChange.RoomGroupChanged -= OnRoomGroupChanged;
	}

	// ------------------------------------------------------------
	// Initialization (autoload-safe)
	// ------------------------------------------------------------
	private void TryBuildHUD() {
		if (_built)
			return;

		_healthBox = GetNodeOrNull<HBoxContainer>(HealthBoxPath)
			?? FindChild("Health", true, false) as HBoxContainer;

		if (_healthBox == null)
			return; // still waiting for a valid scene that has it

		GD.Print($"[HUD] Initialized: {_healthBox.GetPath()}");
		BuildMaskRow(MaxMasks);
		SetHealth(MaxMasks);
		_built = true;
	}

	// ------------------------------------------------------------
	// Visibility
	// ------------------------------------------------------------
	private void OnRoomGroupChanged(RoomGroup group) => UpdateVisibilityFromGroup(group);

	public void UpdateVisibilityFromGroup(RoomGroup group) {
		Visible = group == RoomGroup.Overworld || group == RoomGroup.Boss;
	}

	// ------------------------------------------------------------
	// Health mask management
	// ------------------------------------------------------------
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

	public async void SetHealth(int current) {
		if (!_built || _maskIcons.Count == 0) return;

		current = Mathf.Clamp(current, 0, MaxMasks);

		for (int i = 0; i < _maskIcons.Count; i++) {
			var icon = _maskIcons[i];
			var shouldBeFull = i < current;
			var targetTex = shouldBeFull ? MaskFull : MaskEmpty;

			if (icon.Texture == targetTex)
				continue;

			icon.Texture = targetTex;

			// Animate the change — little pop/pulse
			var tween = GetTree().CreateTween();
			tween.TweenProperty(icon, "scale", new Vector2(1.25f, 1.25f), 0.1f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(icon, "scale", Vector2.One, 0.1f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.In);

			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	// ------------------------------------------------------------
	// Flash / helper
	// ------------------------------------------------------------
	public async Task FlashDamage() {
		if (!_built || _healthBox == null) return;

		var tween = GetTree().CreateTween();
		tween.TweenProperty(_healthBox, "modulate", new Color(1, 0.4f, 0.4f, 1), 0.05f);
		tween.TweenProperty(_healthBox, "modulate", Colors.White, 0.15f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public void RebuildAndSet(int max, int current) {
		MaxMasks = max;
		if (_built) {
			BuildMaskRow(MaxMasks);
			SetHealth(current);
		}
	}

	public void RefreshVisibility() => UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
}
