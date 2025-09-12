using Godot;
using System;

public partial class FollowCamera : Camera2D
{
	private float _shakeStrength = 0f;
	private Random _rand = new Random();

	[Export] public float ShakeDecay = 5f;       // how quickly shake fades out
	[Export] public float ShakeMagnitude = 6f;   // base shake per 1 dmg

	private Vector2 _offsetBase;

	public override void _Ready()
	{
		_offsetBase = Offset;

		// Connect to player
		var player = GetParent<Player>();
		if (player != null)
			player.Hit += OnPlayerHit;
	}

	public override void _Process(double delta)
	{
		if (_shakeStrength > 0)
		{
			_shakeStrength = Mathf.Max(_shakeStrength - ShakeDecay * (float)delta, 0);

			float offsetX = ((float)_rand.NextDouble() * 2f - 1f) * _shakeStrength;
			float offsetY = ((float)_rand.NextDouble() * 2f - 1f) * _shakeStrength;

			Offset = _offsetBase + new Vector2(offsetX, offsetY);
		}
		else
		{
			Offset = _offsetBase;
		}
	}

	private void OnPlayerHit(int dmg, int newHp)
	{
		// Shake strength scales with how much damage was taken
		_shakeStrength = ShakeMagnitude * dmg;
	}
}
