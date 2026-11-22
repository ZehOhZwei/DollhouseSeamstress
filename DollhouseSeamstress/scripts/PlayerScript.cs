using System;
using System.Threading.Tasks;
using Godot;

public partial class PlayerScript : Node3D
{
    public override void _PhysicsProcess(double delta)
    {
        if (_controlMode != ControlMode.Play)
        {
            HandleControlMode();
        }
        else
        {
            HandleCameraMovement();
        }

        if (_controlMode == ControlMode.Movement)
        {
            HandleCameraMovement();
        }
        if (_controlMode == ControlMode.DragAndDrop)
        {
            HandleDragAndDrop();
        }
        if (_controlMode == ControlMode.Sewing)
        {
            HandleSewingObjects();
        }
    }

    #region ControlMode
    public enum ControlMode { Movement, DragAndDrop, Sewing, Play }
    private ControlMode _controlMode;

    private void HandleControlMode()
    {
        if (Input.IsActionJustPressed("movement_mode"))
        {
            _controlMode = ControlMode.Movement;
        }
        if (Input.IsActionJustPressed("drag_and_drop_mode"))
        {
            _controlMode = ControlMode.DragAndDrop;
        }
        if (Input.IsActionJustPressed("sewing_mode"))
        {
            _controlMode = ControlMode.Sewing;
        }
        if (Input.IsActionJustPressed("pulling_mode"))
        {
            _controlMode = ControlMode.Play;
            PlayLevel();
        }
    }
    #endregion

    #region Camera
    [Export] public float rotationSpeed;
    [Export] public float maxDepth = 30f;
    [Export] public float minDepth = 10f;
    [Export] public float maxAngle = 0.8f;
    [Export] public float minAngle = 0.7f;
    [Export] public Node3D CameraPivotX;
    [Export] public Node3D CameraPivotY;
    [Export] public Camera3D Camera;

    private void HandleCameraMovement()
    {
        var horizontalRotationAngle = 0f;
        var verticalRotationAngle = 0f;
        var depth = 0f;
        var currentDepth = Camera.Position.Z;
        var currentAngle = CameraPivotX.Rotation.X;

        if (Input.IsActionPressed("rotate_right"))
        {
            horizontalRotationAngle = 1.0f;
        }
        if (Input.IsActionPressed("rotate_left"))
        {
            horizontalRotationAngle = -1.0f;
        }
        if (Input.IsActionPressed("rotate_down") && currentAngle <= 0.5f)
        {
            verticalRotationAngle = 1.0f;
        }
        if (Input.IsActionPressed("rotate_up") && currentAngle >= -0.5f)
        {
            verticalRotationAngle = -1.0f;
        }
        if (Input.IsActionJustReleased("move_in") && currentDepth >= minDepth)
        {
            depth = -1f;
        }
        if (Input.IsActionJustReleased("move_out") && currentDepth <= maxDepth)
        {
            depth = 1f;
        }

        Camera.Translate(new Vector3(0, 0, depth));
        CameraPivotY.RotateObjectLocal(Vector3.Up, horizontalRotationAngle * rotationSpeed);
        CameraPivotX.RotateObjectLocal(Vector3.Right, verticalRotationAngle * rotationSpeed);
    }

    private void GetCameraVectors(out Vector3 origin, out Vector3 direction)
    {
        var mousePosition = GetViewport().GetMousePosition();
        origin = Camera.ProjectRayOrigin(mousePosition);
        direction = Camera.ProjectRayNormal(mousePosition);
    }

    #endregion

    #region Object
    [Export] public float DragForce = 30.0f;
    [Export] public float Damping = 5.0f;
    [Export] public float MaxForce = 100.0f;
    private RigidBody3D _draggedBody;
    private float _dragDistance;
    private void HandleDragAndDrop()
    {
        if (Input.IsActionJustPressed("interact"))
        {
            if (_draggedBody == null)
            {
                TryPickObject();
            }
            else
            {
                DropObject();
            }
        }

        if (_draggedBody != null)
        {
            HandleMoveObject();
            HandleRotateObject();
        }
    }

    private void TryPickObject()
    {
        GetCameraVectors(out var origin, out var direction);

        var end = origin + direction * 1000.0f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (result.Count == 0) return;

        var collider = result["collider"].AsGodotObject();

        if (collider is RigidBody3D body && body.IsInGroup("draggable"))
        {
            _draggedBody = body;
            _draggedBody.Freeze = false;
            _draggedBody.LockRotation = true;
            _dragDistance = origin.DistanceTo(_draggedBody.Position);
        }
    }

    private void DropObject()
    {
        _draggedBody.Freeze = true;
        _draggedBody = null;
    }

    private Vector3 GetTargetPosition()
    {
        GetCameraVectors(out var origin, out var direction);

        if (Input.IsActionJustReleased("move_out") && _dragDistance > 3f)
        {
            _dragDistance -= 1f;
        }
        if (Input.IsActionJustReleased("move_in"))
        {
            _dragDistance += 1f;
        }


        var mouseTarget = origin + direction * _dragDistance;

        return mouseTarget;
    }

    private void HandleMoveObject()
    {
        var targetPosition = GetTargetPosition();

        var displacement = targetPosition - _draggedBody.GlobalPosition;

        var force = displacement * DragForce - _draggedBody.LinearVelocity * Damping;

        force = force.LimitLength(MaxForce);

        _draggedBody.ApplyCentralForce(force);
    }

    private void HandleRotateObject()
    {
        Vector3 newRotation = new Vector3();
        var objectRotationSpeed = 0.05f;

        if (Input.IsActionPressed("rotate_right"))
        {
            newRotation.Y = 1;
        }
        if (Input.IsActionPressed("rotate_left"))
        {
            newRotation.Y = -1;
        }
        if (Input.IsActionPressed("rotate_up"))
        {
            newRotation.X = 1;
        }
        if (Input.IsActionPressed("rotate_down"))
        {
            newRotation.X = -1;
        }
        if (Input.IsActionPressed("rotate_around_right"))
        {
            newRotation.Z = 1;
        }
        if (Input.IsActionPressed("rotate_around_left"))
        {
            newRotation.Z = -1;
        }
        newRotation *= objectRotationSpeed;
        newRotation = newRotation.Rotated(Vector3.Up, CameraPivotY.Rotation.Y);

        _draggedBody.RotateX(newRotation.X);
        _draggedBody.RotateY(newRotation.Y);
        _draggedBody.RotateZ(newRotation.Z);
    }
    #endregion

    #region Sewing

    private Godot.Collections.Array<Node3D> stitches = [];

    private void HandleSewingObjects()
    {
        if (Input.IsActionJustPressed("interact"))
        {
            TryPlaceStitch();
        }
    }

    private void TryPlaceStitch()
    {
        var stitchScene = ResourceLoader.Load<PackedScene>("res://scenes/stitch.tscn");

        GetCameraVectors(out var origin, out var direction);

        var end = origin + direction * 1000.0f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (result.Count == 0) return;

        var collider = result["collider"].AsGodotObject();
        var hitGlobal = result["position"].AsVector3();

        if (collider is RigidBody3D body && body.IsInGroup("draggable"))
        {
            var stitch = stitchScene.Instantiate<Node3D>();
            body.AddChild(stitch);
            var hitLocal = body.ToLocal(hitGlobal);

            stitch.Position = hitLocal;

            if (stitches.Count > 0)
            {
                var previous = stitches[^1];
                stitch.Call("AddPrevious", previous);
                previous.Call("AddNext", stitch);
            }
            stitches.Add(stitch);
        }

    }

    #endregion

    #region Play

    private void PlayLevel()
    {
        var level = GetTree().GetFirstNodeInGroup("level");

        level.Call("PlayLevel");
    }

    #endregion
}
