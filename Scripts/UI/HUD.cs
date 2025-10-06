using Godot;
using System.Collections.Generic;

public partial class HUD : CanvasLayer
{
	[Export] public int MaxMasks { get; set; } = 5;
	[Export] public int size = 150;

	[ExportCategory("Textures")]
	[Export] public Texture2D MaskFull { get; set; }
	[Export] public Texture2D MaskEmpty { get; set; }

	[ExportCategory("Nodes")]
	[Export] public NodePath HealthBoxPath { get; set; } = "Root/TopLeft/Health";

	private HBoxContainer _healthBox;
	private readonly List<TextureRect> _maskIcons = new();

	public override void _Ready()
	{
		_healthBox = GetNode<HBoxContainer>(HealthBoxPath);
		BuildMaskRow(MaxMasks);
		SetHealth(MaxMasks); // start full
	}

	private void BuildMaskRow(int count)
	{
		foreach (var child in _healthBox.GetChildren())
			(child as Node)?.QueueFree();
		_maskIcons.Clear();

		for (int i = 0; i < count; i++)
		{
			var icon = new TextureRect
			{
				StretchMode = TextureRect.StretchModeEnum.Scale,
				CustomMinimumSize = new Vector2(28, 28), // tweak for your art
				Size = new Vector2(size, size),
				Texture = MaskEmpty
			};
			icon.MouseFilter = Control.MouseFilterEnum.Ignore;
			_healthBox.AddChild(icon);
			_maskIcons.Add(icon);
		}
	}

public void SetHealth(int current)
{
	current = Mathf.Clamp(current, 0, MaxMasks);
	if (_maskIcons.Count != MaxMasks)
		BuildMaskRow(MaxMasks);

	for (int i = 0; i < MaxMasks; i++) {
		_maskIcons[i].Texture = (i < current) ? MaskFull : MaskEmpty;
		_maskIcons[i].Size = new Vector2(size, size);
	}
}


	public async void FlashDamage()
	{
		var tween = CreateTween();
		var baseMod = _healthBox.Modulate;
		tween.TweenProperty(_healthBox, "modulate", new Color(1, 0.4f, 0.4f, 1), 0.05f);
		tween.TweenProperty(_healthBox, "modulate", baseMod, 0.15f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
	public void RebuildAndSet(int max, int current)
	{
		MaxMasks = max;
		BuildMaskRow(MaxMasks);
		SetHealth(current);
	}

}
