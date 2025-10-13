// using Godot;

// public static class KnockbackHelper {
//     public static void ApplyKnockback(Node node, Vector2 sourcePos, float force = 1200f, float upForce = 400f) {
//         // Works for any CharacterBody2D-based enemy
//         if (node is CharacterBody2D body) {
//             Vector2 dir = (body.GlobalPosition - sourcePos).Normalized();
//             Vector2 kb = new Vector2(dir.X, 0).Normalized() * force;
//             kb.Y = -Mathf.Abs(upForce);
//             body.Velocity = kb;
//             body.MoveAndSlide();
//         }
//         // Optional: log ignored cases (for debugging)
//         else {
//             GD.Print($"[KnockbackHelper] Node {node.Name} not a CharacterBody2D — skipping knockback.");
//         }
//     }
// }
using Godot;

public static class KnockbackHelper {
    public static void ApplyImpulse(CharacterBody2D body, Vector2 sourcePos, float force = 700f) {
        if (body == null) return;
        if (body.IsInGroup("boss")) return; // Skip bosses

        float dirX = Mathf.Sign((body.GlobalPosition - sourcePos).X);
        Vector2 impulse = new Vector2(dirX * force, -100f); // Slight upward knock

        body.Velocity = impulse;

        // Temporarily freeze their AI so velocity isn’t immediately overwritten
        body.SetPhysicsProcess(false);

        //  use SceneTree.CreateTimer
        var tree = body.GetTree();
        if (tree != null) {
            var timer = tree.CreateTimer(0.08f);
            timer.Timeout += () => {
                if (GodotObject.IsInstanceValid(body))
                    body.SetPhysicsProcess(true);
            };
        }

        GD.Print($"[Knockback] Applied impulse to {body.Name}, dir={dirX}, force={force}");
    }
}
