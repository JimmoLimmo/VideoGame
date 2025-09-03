using Godot;

public partial class Sword : Area2D
{
    [Export] public int Damage { get; set; } = 10;
    [Export] public Node2D VisualAnchor { get; set; } // Optional anchor 

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        Monitoring = false;       // OFF by default
        Monitorable = false;      // sword shouldn't be hittable
    }

    public void EnableHitbox()  => Monitoring = true;
    public void DisableHitbox() => Monitoring = false;

    // Mirror sword hitbox when player flips
    public void SetFacingLeft(bool left)
    {
        // Mirror around local Y-axis; use anchor to avoid shape drifting
        // Scale = new Vector2(left ? -1 : 1, 1);
        if (VisualAnchor != null) VisualAnchor.Scale = new Vector2(left ? -1 : 1, 1);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("enemies"))
        {
            if (body.HasMethod("TakeDamage"))
                body.Call("TakeDamage", Damage);
        }
    }
}
