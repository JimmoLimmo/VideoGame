using Godot;
using System.Collections.Generic;

public partial class BossSword : Area2D {
	[Export] public int Damage { get; set; } = 1;
	private HashSet<Node2D> _hitTargets = new();

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
		var bodies = GetOverlappingBodies();
		foreach (var body in bodies) {
			if (!_hitTargets.Add(body)) continue;
			if (body is Player player) {
				if (player != null) {
					player.ApplyHit(Damage, GlobalPosition);
					GD.Print($"[BossSword] Hit player for {Damage}");
				}
			}
		}
	}
}
