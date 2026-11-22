using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;

public partial class StitchScript : Node3D
{
    public Node3D previousStitch;
    public Node3D nextStitch;

    public MeshInstance3D lineToNext;
    private Material lineMaterial;


    public override void _PhysicsProcess(double delta)
    {
        if (nextStitch != null)
        {
            PlaceCylinderBetween(lineToNext, GlobalPosition, nextStitch.GlobalPosition);
        }
    }


    public void AddPrevious(Node3D previous)
    {
        previousStitch = previous;
    }
    public void AddNext(Node3D next)
    {
        nextStitch = next;
        lineToNext = GetNode<MeshInstance3D>("Line");
        lineMaterial = ResourceLoader.Load<StandardMaterial3D>("res://resources/yellow.tres");

    }

    private void PlaceCylinderBetween(MeshInstance3D meshInstance, Vector3 start, Vector3 end, float radius = 0.05f)
    {
        var direction = end - start;
        var length = direction.Length();
        if (length <= 0.0001f)
        {
            meshInstance.Visible = false;
            return;
        }
        meshInstance.Visible = true;
        meshInstance.MaterialOverride = lineMaterial;

        var cylinder = meshInstance.Mesh as CylinderMesh;
        if (cylinder == null)
        {
            cylinder = new CylinderMesh();
            meshInstance.Mesh = cylinder;
        }

        cylinder.TopRadius = radius;
        cylinder.BottomRadius = radius;
        cylinder.Height = length;

        meshInstance.GlobalPosition = (start + end) * 0.5f;

        meshInstance.LookAt(end, Vector3.Up);
        meshInstance.RotateObjectLocal(Vector3.Right, -(float)Math.PI * 0.5f);

    }


    public Vector3 GetDirectionToPrevious()
    {

        return new Vector3();
    }

    public Vector3 GetCurrentForce()
    {
        var force = new Vector3();
        var forceMultiplier = 5;

        if (previousStitch != null)
        {
            var distance = GlobalPosition.DistanceTo(previousStitch.GlobalPosition);

            force += (GlobalPosition - previousStitch.GlobalPosition).Normalized() * distance;
        }

        if (nextStitch != null)
        {
            var distance = GlobalPosition.DistanceTo(nextStitch.GlobalPosition);

            force += (GlobalPosition - nextStitch.GlobalPosition).Normalized() * distance;
        }

        return force * forceMultiplier;
    }
}
