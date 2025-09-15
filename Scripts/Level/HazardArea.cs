using Godot;
using System.Collections.Generic;

public enum HazardMode
{
	OnEnter,   // deal damage once on contact
	Tick,      // deal damage repeatedly while inside, at TickInterval
	PulseTick  // same as Tick, but hazard automatically toggles on/off
}

public partial class HazardArea : Area2D
{
	[Export] public int Damage { get; set; } = 1;

	// What this hazard can hit. Use Godot groups like "player", "enemies".
	// Leave empty to hit anything that has TakeDamage(int).
	[Export] public StringName[] TargetGroups { get; set; } = new StringName[] { "player" };

	// Behavior mode
	[Export] public HazardMode Mode { get; set; } = HazardMode.OnEnter;

	// For Tick / PulseTick
	[Export] public float TickInterval { get; set; } = 0.4f;

	// For PulseTick
	[Export] public float PulseOnTime { get; set; } = 1.2f;
	[Export] public float PulseOffTime { get; set; } = 1.0f;

	// Optional knockback to apply when damage is dealt
	[Export] public Vector2 Knockback { get; set; } = Vector2.Zero;

	// Whether the hazard is currently active (affects Monitoring)
	[Export] public bool Active { get; set; } = true;

	// If true in OnEnter mode, don't re-damage until body leaves and re-enters
	[Export] public bool OneShotUntilExit { get; set; } = true;

	// Internal
	private readonly HashSet<Node2D> _inside = new();
	private float _tickTimer = 0f;
	private float _pulseTimer = 0f;
	private bool _pulsePhaseOn = true;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited  += OnBodyExited;
		UpdateMonitoring();

		_tickTimer  = TickInterval;
		_pulseTimer = PulseOnTime;
	}

	public override void _Process(double delta)
	{
		// Pulse logic
		if (Mode == HazardMode.PulseTick)
		{
			_pulseTimer -= (float)delta;
			if (_pulseTimer <= 0f)
			{
				_pulsePhaseOn = !_pulsePhaseOn;
				_pulseTimer = _pulsePhaseOn ? PulseOnTime : PulseOffTime;
				UpdateMonitoring(); // turn hitbox on/off with the pulse
			}
		}

		// Tick logic
		if ((Mode == HazardMode.Tick) || (Mode == HazardMode.PulseTick && _pulsePhaseOn))
		{
			_tickTimer -= (float)delta;
			if (_tickTimer <= 0f)
			{
				_tickTimer = TickInterval;
				// Deal damage to everything inside
				foreach (var body in _inside)
					TryDealDamage(body);
			}
		}
	}

	private void UpdateMonitoring()
	{
		// Active means the hitbox is "live" and can register overlaps.
		// For pulsing, disable Monitoring during off phase.
		Monitoring = Active && (Mode != HazardMode.PulseTick || _pulsePhaseOn);
		Monitorable = false; // hazards usually shouldn't be harmed by other hitboxes
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!IsValidTarget(body)) return;

		_inside.Add(body);

		if (Mode == HazardMode.OnEnter)
		{
			// Burst damage once
			if (TryDealDamage(body) && OneShotUntilExit)
			{
				// Do nothing else; waiting for exit to re-arm
			}
		}
	}

	private void OnBodyExited(Node2D body)
	{
		_inside.Remove(body);
	}

	private bool IsValidTarget(Node2D body)
	{
		// If specific groups are set, require membership in at least one
		if (TargetGroups != null && TargetGroups.Length > 0)
		{
			bool inAny = false;
			foreach (var g in TargetGroups)
			{
				if (!g.IsEmpty && body.IsInGroup(g))
				{
					inAny = true;
					break;
				}
			}
			if (!inAny) return false;
		}

		// Require the damage receiver to have TakeDamage(int)
		return body.HasMethod("TakeDamage");
	}

	private bool TryDealDamage(Node2D body)
	{
		if (!IsInstanceValid(body)) return false;

		// Call TakeDamage on the target
		body.Call("TakeDamage", Damage);

		// Optional knockback if the body can accept it
		if (Knockback != Vector2.Zero && body is CharacterBody2D cb)
		{
			// Additive push; customize to taste (set, add, or lerp)
			cb.Velocity += Knockback;
		}

		return true;
	}

	// You can toggle hazards via AnimationPlayer or code:
	public void SetActive(bool on)
	{
		Active = on;
		UpdateMonitoring();
	}
}
