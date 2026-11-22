using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class LevelScript : Node3D
{
    [Export] public Godot.Collections.Array<Area3D> TargetAreas;
    [Export] public Godot.Collections.Array<RigidBody3D> BuildingObjects;
    [Export] public int GridResolution = 5;


    private Godot.Collections.Array<MeshInstance3D> LevelMeshes = [];

    public override void _Ready()
    {
        base._Ready();

        foreach (var buildingObject in BuildingObjects)
        {
            var childMesh = buildingObject.GetNode<MeshInstance3D>("MeshInstance3D");
            LevelMeshes.Add(childMesh);
        }
    }


    [Export] public float MaxForce = 100.0f;
    private bool playing = false;
    private Godot.Collections.Array<Node> stitches = [];
    public override void _PhysicsProcess(double delta)
    {
        if (playing)
        {
            foreach (Node3D stitch in stitches)
            {
                var parent = stitch.GetParent<RigidBody3D>();

                Vector3 force = (Vector3)stitch.Call("GetCurrentForce");

                force = force.LimitLength(MaxForce);

                parent.ApplyForce(-force, stitch.Position);
            }
        }
    }


    public void PlayLevel()
    {
        foreach (var buildingObject in BuildingObjects)
        {
            buildingObject.Freeze = false;
            buildingObject.Sleeping = false;
            buildingObject.LockRotation = false;
        }

        stitches = GetTree().GetNodesInGroup("stitch");



        Task.Delay(10000);

        var score = GetLevelScore();


        GD.Print(score);
    }

    public float GetLevelScore()
    {
        var totalScore = 0f;
        foreach (var mesh in LevelMeshes)
        {
            var highest = 0f;
            foreach (var area in TargetAreas)
            {
                var score = CalculateOverlapScore(mesh, area);
                if (score > highest) highest = score;
            }
            totalScore += highest;
        }

        totalScore /= TargetAreas.Count;

        return totalScore;
    }

    public float CalculateOverlapScore(MeshInstance3D targetMesh, Area3D targetArea)
    {

        Aabb localAabb = targetMesh.GetAabb();
        var spaceState = GetWorld3D().DirectSpaceState;

        var query = new PhysicsPointQueryParameters3D
        {
            CollideWithAreas = true,
            CollideWithBodies = false,
            CollisionMask = targetArea.CollisionLayer
        };

        int pointsInsideArea = 0;
        int totalPoints = GridResolution * GridResolution * GridResolution;

        for (int x = 0; x < GridResolution; x++)
        {
            for (int y = 0; y < GridResolution; y++)
            {
                for (int z = 0; z < GridResolution; z++)
                {
                    Vector3 step = new Vector3(
                        (x + 0.5f) / GridResolution,
                        (y + 0.5f) / GridResolution,
                        (z + 0.5f) / GridResolution
                    );

                    Vector3 localPoint = localAabb.Position + (localAabb.Size * step);
                    query.Position = targetMesh.GlobalTransform * localPoint;

                    // Check if the point is inside this specific Area3D
                    var results = spaceState.IntersectPoint(query);
                    foreach (var hit in results)
                    {
                        if ((GodotObject)hit["collider"] == targetArea)
                        {
                            pointsInsideArea++;
                            break;
                        }
                    }
                }
            }
        }

        float score = (float)pointsInsideArea / totalPoints * 100f;
        return score;
    }

}
