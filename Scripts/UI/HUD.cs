using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class HUD : CanvasLayer {
	// ------------------------------------------------------------
	// CONFIG
	// ------------------------------------------------------------
	[Export] public int MaxMasks { get; set; } = 5;
	[Export(PropertyHint.Range, "16,256,1")] public int MaskSize { get; set; } = 64;

	[ExportCategory("Health Textures")]
	[Export] public Texture2D MaskFull { get; set; }
	[Export] public Texture2D MaskEmpty { get; set; }

	[ExportCategory("Soul Textures")]
	[Export] public Texture2D ManaVesselBG { get; set; }
	[Export] public Texture2D ManaVesselFill { get; set; }

	[ExportCategory("Nodes")]
	[Export] public NodePath HealthBoxPath { get; set; } = "Root/TopLeft/Health";
	[Export] public NodePath ManaContainerPath { get; set; } = "Root/TopLeft/ManaContainer/ManaFill";

	// ------------------------------------------------------------
	// STATE
	// ------------------------------------------------------------
	private HBoxContainer _healthBox;
	private readonly List<TextureRect> _maskIcons = new();
	private TextureRect _manaFill;
	private float _currentManaRatio = 1f;
	private float _targetManaRatio = 1f;

	private bool _built = false;
	private bool _initPass = true;

	// ------------------------------------------------------------
	// GODOT
	// ------------------------------------------------------------
	public override void _Ready() {
		GlobalRoomChange.RoomGroupChanged += OnRoomGroupChanged;
		UpdateVisibilityFromGroup(GlobalRoomChange.CurrentGroup);
	}

	public override void _Process(double delta) {
		if (!_built)
			TryBuildHUD();
		else if (_initPass)
			_initPass = false;

		// Smooth mana fill animation
		if (_manaFill != null) {
			_currentManaRatio = Mathf.Lerp(_currentManaRatio, _targetManaRatio, (float)delta * 5f);
			if (_manaFill.Material is ShaderMaterial shaderMat)
				shaderMat.SetShaderParameter("fill_ratio", _currentManaRatio);

		}
	}

	public override void _ExitTree() {
		GlobalRoomChange.RoomGroupChanged -= OnRoomGroupChanged;
	}

	// ------------------------------------------------------------
	// BUILD
	// ------------------------------------------------------------
	private void TryBuildHUD() {
		if (_built) return;

		_healthBox = GetNodeOrNull<HBoxContainer>(HealthBoxPath);
		_manaFill = GetNodeOrNull<TextureRect>(ManaContainerPath);
		if (_healthBox == null || _manaFill == null) return;

		BuildMaskRow(_healthBox, _maskIcons, MaxMasks, MaskEmpty);
		SetHealth(GlobalRoomChange.health);
		SetMana(GlobalRoomChange.mana, instant: true);

		GD.Print("[HUD] Initialized (Hollow Knight style)");
		_built = true;
	}

	private void BuildMaskRow(HBoxContainer box, List<TextureRect> list, int count, Texture2D tex) {
		foreach (var c in box.GetChildren()) c.QueueFree();
		list.Clear();
		for (int i = 0; i < count; i++) {
			var icon = new TextureRect {
				Texture = tex,
				StretchMode = TextureRect.StretchModeEnum.KeepCentered,
				CustomMinimumSize = new Vector2(MaskSize, MaskSize),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			box.AddChild(icon);
			list.Add(icon);
		}
	}

	// ------------------------------------------------------------
	// HEALTH
	// ------------------------------------------------------------
	private async void AnimateIcons(List<TextureRect> icons, int current, Texture2D full, Texture2D empty) {
		current = Mathf.Clamp(current, 0, icons.Count);

		for (int i = 0; i < icons.Count; i++) {
			var icon = icons[i];
			var shouldBeFull = i < current;
			var targetTex = shouldBeFull ? full : empty;
			if (icon.Texture == targetTex) continue;

			icon.Texture = targetTex;
			if (_initPass) continue;

			var tween = GetTree().CreateTween();
			tween.TweenProperty(icon, "scale", new Vector2(1.25f, 1.25f), 0.1f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(icon, "scale", Vector2.One, 0.1f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	public void SetHealth(int current) {
		if (!_built) return;
		AnimateIcons(_maskIcons, current, MaskFull, MaskEmpty);
	}

	// ------------------------------------------------------------
	// MANA (Soul Vessel)
	// ------------------------------------------------------------
	public void SetMana(int current, bool instant = false) {
		if (!_built || _manaFill == null) return;
		int max = Mathf.Max(1, GlobalRoomChange.maxMana);
		_targetManaRatio = Mathf.Clamp((float)current / max, 0, 1);
		if (instant) _currentManaRatio = _targetManaRatio;
	}

	// Gradual soul drain when healing (focus)
	public async Task DrainManaForHeal(int cost, float duration = 1.0f) {
		if (!_built) return;
		float startRatio = _currentManaRatio;
		float endRatio = Mathf.Clamp(startRatio - ((float)cost / GlobalRoomChange.maxMana), 0f, 1f);

		var timer = GetTree().CreateTimer(duration);
		while (timer.TimeLeft > 0) {
			float t = 1f - (float)(timer.TimeLeft / duration);
			_targetManaRatio = Mathf.Lerp(startRatio, endRatio, t);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_targetManaRatio = endRatio;
		GlobalRoomChange.mana = Mathf.RoundToInt(endRatio * GlobalRoomChange.maxMana);
	}

	// ------------------------------------------------------------
	// VISIBILITY / HELPERS
	// ------------------------------------------------------------
	public async Task FlashDamage() {
		if (!_built || _healthBox == null) return;
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_healthBox, "modulate", new Color(1, 0.4f, 0.4f, 1), 0.05f);
		tween.TweenProperty(_healthBox, "modulate", Colors.White, 0.15f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private void OnRoomGroupChanged(RoomGroup group) => UpdateVisibilityFromGroup(group);

	public void UpdateVisibilityFromGroup(RoomGroup group) {
		bool shouldBeVisible = group == RoomGroup.Overworld || group == RoomGroup.Boss;
		Visible = shouldBeVisible;
		
		// Force a rebuild if the HUD should be visible but isn't built
		if (shouldBeVisible && !_built) {
			TryBuildHUD();
		}
	}
}
