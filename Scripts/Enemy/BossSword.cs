using Godot;
using System.Collections.Generic;

public partial class BossSword : Area2D {
	[Export] public int Damage { get; set; } = 1;
	private HashSet<Area2D> _hitTargets = new();

	public override void _Ready() {
		Monitoring = false;
		Monitorable = true;
	}

	public void EnableHitbox() {
		Monitoring = true;
		_hitTargets.Clear();
	}

	public void DisableHitbox() {
		Monitoring = false;
	}

	public void SetFacingLeft(bool left) {
		Scale = new Vector2(left ? -1 : 1, 1);
	}

	public void HitCheck() {
		foreach (Area2D area in GetOverlappingAreas()) {
			if (!_hitTargets.Add(area)) continue;
			if (area.IsInGroup("player_hitbox")) {
				var player = area.GetParentOrNull<Player>();
				if (player != null) {
					player.ApplyHit(Damage, GlobalPosition);
					GD.Print($"[BossSword] Hit player for {Damage}");
				}
			}
		}
	}
}
