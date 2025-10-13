using Godot;
using System.Collections.Generic;

public partial class ThrowableSword : RigidBody2D
{
	[Export] public int Damage { get; set; } = 10;
	[Export] public float MaxRange { get; set; } = 600f;
	[Export] public float ThrowSpeed { get; set; } = 300f;
	
	private Vector2 _startPosition;
	private Player _player;
	private HashSet<Area2D> _hitEnemies = new HashSet<Area2D>();
	private bool _hasHitWall = false;
	private float _timeAlive = 0f;
	
	public override void _Ready()
	{
		_startPosition = GlobalPosition;
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		
		// Set up the Area2D but don't enable collision detection yet
		Area2D swordArea = GetNode<Area2D>("Area2D");
		if (swordArea != null)
		{
			swordArea.Monitoring = false; // Start with monitoring disabled
			swordArea.Monitorable = false;
			// Don't connect collision signal yet
		}
		
		// Set up physics
		GravityScale = 0; // Sword flies straight
		LinearDamp = 0; // No air resistance
		
		// Disable contact monitoring initially to prevent self-collision
		ContactMonitor = false;
		MaxContactsReported = 0;
		// Don't connect RigidBody collision immediately
		
		// Use a timer to enable collision detection after a short delay
		var delayTimer = new Timer();
		AddChild(delayTimer);
		delayTimer.WaitTime = 0.2; // Wait 0.2 seconds before enabling collision
		delayTimer.OneShot = true;
		delayTimer.Timeout += EnableCollisionDetection;
		delayTimer.Start();
		
		// Auto-destroy after a reasonable time if nothing happens
		var timer = new Timer();
		AddChild(timer);
		timer.WaitTime = 5.0;
		timer.OneShot = true;
		timer.Timeout += () => TeleportPlayerAndDestroy();
		timer.Start();
	}
	
	private void EnableCollisionDetection()
	{
		// Enable contact monitoring after the delay
		ContactMonitor = true;
		MaxContactsReported = 10;
		BodyEntered += OnRigidBodyCollision;
		
		// Enable Area2D collision detection
		Area2D swordArea = GetNode<Area2D>("Area2D");
		if (swordArea != null)
		{
			swordArea.Monitoring = true;
			swordArea.BodyEntered += OnBodyEntered;
		}
		
		GD.Print("Collision detection enabled for ThrowableSword");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		_timeAlive += (float)delta;
		
		// Check if sword has traveled too far
		float distanceFromStart = GlobalPosition.DistanceTo(_startPosition);
		if (distanceFromStart >= MaxRange && !_hasHitWall)
		{
			// Stop the sword and teleport immediately when max range is reached
			LinearVelocity = Vector2.Zero;
			_hasHitWall = true;
			TeleportPlayerAndDestroy();
			return; // Exit early to avoid further processing
		}
		
		// Removed CheckRigidBodyCollisions() - using proper collision detection instead
		
		// Check for collision with areas (enemies) - only if monitoring is enabled
		Area2D swordArea = GetNode<Area2D>("Area2D");
		if (swordArea != null && swordArea.Monitoring)
		{
			CheckAreaCollisions();
		}
	}
	
	// Commented out - this was causing premature teleportation
	// private void CheckRigidBodyCollisions()
	// {
		// Check if the sword has stopped moving due to collision
		// Use a lower threshold and ensure we've traveled some minimum distance
		// float currentSpeed = LinearVelocity.Length();
		// float distanceTraveled = GlobalPosition.DistanceTo(_startPosition);
		
		// if (currentSpeed < 5f && distanceTraveled > 20f && !_hasHitWall)
		// {
			// _hasHitWall = true;
			// LinearVelocity = Vector2.Zero;
			// TeleportPlayerAndDestroy();
			// return;
		// }
	// }
	
	public override void _Input(InputEvent @event)
	{
		// Check if player presses 's' again to teleport
		if (@event.IsActionPressed("sword_throw") && _player != null)
		{
			TeleportPlayerAndDestroy();
		}
	}
	
	private void OnBodyEntered(Node body)
	{
		// Skip if already hit a wall to prevent spam
		if (_hasHitWall) return;
		
		// Debug: print what we're colliding with
		GD.Print($"Sword collided with: {body.Name} (Type: {body.GetType()}) - ShouldIgnore: {ShouldIgnoreCollision(body)}");
		
		// Hit a wall or solid object (Area2D collision)
		if (!ShouldIgnoreCollision(body))
		{
			GD.Print($"Teleporting due to collision with: {body.Name}");
			LinearVelocity = Vector2.Zero;
			_hasHitWall = true;
			// Teleport immediately on collision
			TeleportPlayerAndDestroy();
		}
		else
		{
			GD.Print($"Ignoring collision with: {body.Name}");
		}
	}
	
	private void OnRigidBodyCollision(Node body)
	{
		// Skip if already hit a wall to prevent spam
		if (_hasHitWall) return;
		
		// Debug: print what we're colliding with  
		GD.Print($"Sword RigidBody collision with: {body.Name} (Type: {body.GetType()}) - ShouldIgnore: {ShouldIgnoreCollision(body)}");
		
		// Hit a wall or solid object (RigidBody2D collision)
		if (!ShouldIgnoreCollision(body))
		{
			GD.Print($"Teleporting due to RigidBody collision with: {body.Name}");
			LinearVelocity = Vector2.Zero;
			_hasHitWall = true;
			// Teleport immediately on collision
			TeleportPlayerAndDestroy();
		}
		else
		{
			GD.Print($"Ignoring RigidBody collision with: {body.Name}");
		}
	}
	
	private bool IsHazardArea(Node body)
	{
		// Check if this is a hazard area (which should not stop the sword)
		
		// Check for Area2D hazards
		if (body is Area2D area)
		{
			// Check if it's in the "player" target group (hazards typically target players)
			if (area.IsInGroup("hazard") || area.IsInGroup("damage"))
			{
				return true;
			}
			
			// If the area doesn't have TakeDamage method, it's likely a hazard (not an enemy)
			if (!area.HasMethod("TakeDamage"))
			{
				return true;
			}
			
			// Check if it has the HazardArea script by checking for specific properties
			if (area.HasMethod("SetActive") && area.Get("Damage").VariantType != Variant.Type.Nil)
			{
				return true;
			}
		}
		
		// Check for StaticBody2D hazards (damage tiles are often StaticBody2D)
		if (body is StaticBody2D staticBody)
		{
			// Check if it's in hazard groups
			if (staticBody.IsInGroup("hazard") || staticBody.IsInGroup("damage"))
			{
				GD.Print($"Detected StaticBody2D hazard by group: {staticBody.Name}");
				return true;
			}
			
			// Check for common hazard naming patterns
			string nodeName = staticBody.Name.ToString().ToLower();
			if (nodeName.Contains("hazard") || nodeName.Contains("damage") || nodeName.Contains("spike") || nodeName.Contains("lava"))
			{
				GD.Print($"Detected StaticBody2D hazard by name pattern: {staticBody.Name}");
				return true;
			}
			
			// Check for HazardArea script
			var script = staticBody.GetScript();
			if (script.VariantType != Variant.Type.Nil && script.AsGodotObject() is Script scriptResource)
			{
				string scriptPath = scriptResource.ResourcePath;
				if (scriptPath.Contains("HazardArea") || scriptPath.Contains("Hazard"))
				{
					GD.Print($"Detected StaticBody2D hazard by script: {staticBody.Name}");
					return true;
				}
			}
			
			// Check if it has Area2D children with hazard properties (common pattern for damage tiles)
			foreach (Node child in staticBody.GetChildren())
			{
				if (child is Area2D childArea)
				{
					if (childArea.IsInGroup("hazard") || childArea.IsInGroup("damage"))
					{
						GD.Print($"Detected StaticBody2D hazard by child Area2D: {staticBody.Name}");
						return true;
					}
					
					// Check if the child area has hazard-like properties
					if (childArea.HasMethod("SetActive") || childArea.Get("Damage").VariantType != Variant.Type.Nil)
					{
						GD.Print($"Detected StaticBody2D hazard by child properties: {staticBody.Name}");
						return true;
					}
				}
			}
		}
		
		return false;
	}
	
	private bool ShouldIgnoreCollision(Node body)
	{
		// Ignore self-collision (the sword colliding with itself or its components)
		if (body == this || body is ThrowableSword)
		{
			GD.Print($"Ignoring self-collision with: {body.Name}");
			return true;
		}
		
		// Check if the body is a child of this sword (Area2D, CollisionShape2D, etc.)
		Node parent = body.GetParent();
		while (parent != null)
		{
			if (parent == this)
			{
				GD.Print($"Ignoring collision with own child component: {body.Name}");
				return true;
			}
			parent = parent.GetParent();
		}
			
		// Ignore player
		if (body == _player)
			return true;
			
		// Ignore hazard areas
		if (IsHazardArea(body))
			return true;
			
		// For TileMapLayer, we need to check what type of collision this is
		if (body is TileMapLayer tilemap)
		{
			// The problem might be that we're hitting the tilemap at the spawn position
			// Let's check if we've traveled a reasonable distance AND time before allowing tilemap collision
			float distanceTraveled = GlobalPosition.DistanceTo(_startPosition);
			if (distanceTraveled < 200f || _timeAlive < 0.5f) // Need significant distance AND time
			{
				GD.Print($"Ignoring TileMapLayer collision - distance: {distanceTraveled}, time: {_timeAlive}");
				return true;
			}
		}
		
		return false;
	}
	
	private void CheckAreaCollisions()
	{
		// Get the Area2D child node to check for overlaps
		Area2D swordArea = GetNode<Area2D>("Area2D");
		if (swordArea == null) return;
		
		var overlappingAreas = swordArea.GetOverlappingAreas();
		
		foreach (Area2D area in overlappingAreas)
		{
			if (_hitEnemies.Add(area))
			{
				// Find the enemy node
				Node current = area;
				while (current != null && !current.HasMethod("TakeDamage"))
				{
					current = current.GetParent();
				}
				
				if (current != null && current.HasMethod("TakeDamage"))
				{
					current.CallDeferred("TakeDamage", Damage);
				}
			}
		}
	}
	
	private void TeleportPlayerAndDestroy()
	{
		if (_player != null)
		{
			// Teleport player to sword position (slightly offset to avoid getting stuck in walls)
			Vector2 teleportPos = GlobalPosition;
			
			// Try to find a safe position near the sword
			var spaceState = GetWorld2D().DirectSpaceState;
			var query = PhysicsRayQueryParameters2D.Create(teleportPos, teleportPos + Vector2.Down * 50);
			query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
			
			var result = spaceState.IntersectRay(query);
			if (result.Count > 0)
			{
				// There's a floor below, teleport slightly above it
				teleportPos = result["position"].AsVector2() + Vector2.Up * 20;
			}
			
			_player.GlobalPosition = teleportPos;
			
			// Reset player velocity to prevent weird physics
			_player.Velocity = Vector2.Zero;
		}
		
		QueueFree();
	}
	
	public void ThrowInDirection(Vector2 direction)
	{
		LinearVelocity = direction.Normalized() * ThrowSpeed;
		
		// Adjust sprite rotation based on direction
		var sprite = GetNode<Sprite2D>("Sprite2D");
		if (sprite != null)
		{
			if (direction.X < 0) // Going left
			{
				sprite.Rotation = 1.5708f + Mathf.Pi; // 90 degrees + 180 degrees
			}
			else // Going right
			{
				sprite.Rotation = 1.5708f; // 90 degrees
			}
		}
	}
}