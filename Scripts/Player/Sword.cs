using Godot;
using System.Collections.Generic;

public partial class Sword : Area2D {
	[Export] public int Damage { get; set; } = 10;
	[Export] public Node2D VisualAnchor { get; set; } // Optional anchor

	private HashSet<Area2D> _hitEnemies = new HashSet<Area2D>();

	public override void _Ready() {
		Monitoring = false;       // OFF by default
		Monitorable = true;
	}

	public void EnableHitbox() {
		Monitoring = true;
		_hitEnemies.Clear();
	}

	public void DisableHitbox() {
		Monitoring = false;
	}

	// Mirror sword hitbox when player flips
	public void SetFacingLeft(bool left) {
		// Mirror around local Y-axis; use anchor to avoid shape drifting
		// Scale = new Vector2(left ? -1 : 1, 1);
		if (VisualAnchor != null) VisualAnchor.Scale = new Vector2(left ? -1 : 1, 1);
	}

	public void HitCheck() {
		Monitoring = true;
		var areas = GetOverlappingAreas();

		foreach (Area2D area in areas) {
			if (_hitEnemies.Add(area)) {
				GD.Print("Hit " + area.Name);

				Node current = area;
				while (current != null && !current.HasMethod("TakeDamage")) {
					current = current.GetParent();
					GD.Print("Check next");
				}

				if (current == null) {
					GD.Print("null");
					continue;
				}

				GD.Print("Hit Node: " + current.Name);

				if (current.HasMethod("TakeDamage")) {
					current.CallDeferred("TakeDamage", Damage);
				}
				var player = GetParentOrNull<Player>();
				player?.AddMana(1);
				GD.Print($"[ManaGain] Enemy hit → mana={GlobalRoomChange.mana}/{GlobalRoomChange.maxMana}");
			}
		}
	}
}
