using Godot;
using System;

public partial class CollideRight : Area3D
{
	[Export]
	public Node3D cannon;

	[Export]
	public AnimationPlayer animation;
	
	[Export]
	public CollisionShape3D collider;
    [Export]
	public AudioStreamPlayer3D player;

	public bool isinArea = false;
	public bool is_played = false;

	public override void _Ready()
	{
	}


	public override void _Process(double delta)
	{
		if(collider.Shape is BoxShape3D box)
		{
			Transform3D transform = collider.GlobalTransform;
			Vector3 localpoint = transform.Basis.Inverse()*(cannon.GlobalPosition - transform.Origin);
			isinArea = Mathf.Abs(localpoint.X) <= box.Size.X / 2
                && Mathf.Abs(localpoint.Y) <= box.Size.Y / 2
                && Mathf.Abs(localpoint.Z) <= box.Size.Z / 2;
		}

		if(isinArea && !is_played)
		{
			animation.Play("swing2");
			player.Play();
			is_played = true;
		}
		else if (!isinArea)
		{
			is_played = false;
		}
	}
}
