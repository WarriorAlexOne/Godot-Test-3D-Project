using Godot;
using System;

public partial class CharacterBody3d : CharacterBody3D {
    public bool controls = true;

    public float floorSpeed = 5.0f;
    public float floorFriction = 0.8f;

    public float wallSpeed = 2.0f;
    public float wallSpeedMax = 6.0f;
    public float wallFriction = 0.9f;

    public float airFriction = 0.87f;

    public float jumpPower = 12.0f;
    public float gravity = 24.0f;

    public float wallJumpPower = 5.0f;
    public float wallLaunchPower = 32.0f;

    [Export]
    public float MouseSensitivity = 0.3f;
    [Export]
    public float MinPitch = -90.0f;
    [Export]
    public float MaxPitch = 90.0f;

    private Timer _wallLaunchDelay;
    private Node3D _cameraPivot;
    private Camera3D _camera;
    private float _yaw = 0.0f;
    private float _pitch = 0.0f;

    private Vector3 lastWallNormal = Vector3.Zero;

    public override void _Ready() {
        _wallLaunchDelay = GetNode<Timer>("WallLaunchDelay");
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _camera = GetNode<Camera3D>("CameraPivot/Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion) {
            _yaw -= mouseMotion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - mouseMotion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
            RotationDegrees = new Vector3(0, _yaw, 0);
            _cameraPivot.RotationDegrees = new Vector3(_pitch, 0, 0);
        }

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _PhysicsProcess(double delta) {
        Vector3 velocity = Velocity;
        if (!IsOnFloor()) {
            velocity.Y -= gravity * (float)delta;
        }

        if (!_wallLaunchDelay.IsStopped()) {
            controls = false;
        }
        else if (_wallLaunchDelay.IsStopped()) {
            controls = true;
        }

        if (Input.IsActionJustPressed("Jump") && IsOnFloor() && controls) {
            velocity.Y = jumpPower;
        }
        if (Input.IsActionPressed("Walk Forward") && IsOnWall() && controls) {
            velocity.Y += wallSpeed;
            if (velocity.Y > wallSpeedMax) {
                velocity.Y = wallSpeedMax;
            }
        }
        if (IsOnWall() && !Input.IsActionPressed("Walk Forward") && controls) {
            velocity.Y *= wallFriction;
        }
        
        if (Input.IsActionJustPressed("Jump") && IsOnWall() && controls) {
            Vector3 jumpDirection = lastWallNormal.Normalized();

            velocity = jumpDirection * wallLaunchPower;
            velocity.Y = wallJumpPower;
            _wallLaunchDelay.Start();
        }

        if (IsOnWall()) {
            gravity = 0;
        }
        else {
            gravity = 24;
        }

        Vector2 inputDirection = Input.GetVector("Strafe Left", "Strafe Right", "Walk Forward", "Walk Backward");
        Vector3 direction = new Vector3(inputDirection.X, 0, inputDirection.Y).Rotated(Vector3.Up, Mathf.DegToRad(_yaw)).Normalized();
        if (!controls) {
            inputDirection = Vector2.Zero;
            direction = Vector3.Zero;
        }

        KinematicCollision3D collision = null;
        if (!controls) {
            collision = MoveAndCollide(velocity * (float)delta);
        }
        if (collision != null) {
            if (!collision.GetNormal().IsEqualApprox(Vector3.Up)) {
                lastWallNormal = collision.GetNormal();
            }
        }

        if (direction != Vector3.Zero) {
            if (IsOnFloor()) {
                velocity.X = direction.X * floorSpeed;
                velocity.Z = direction.Z * floorSpeed;
            }
            else {
                velocity.X = direction.X * floorSpeed;
                velocity.Z = direction.Z * floorSpeed;
            }
        }
        else {
            if (IsOnFloor()) {
                velocity.X *= floorFriction;
                velocity.Z *= floorFriction;
            }
            else {
                velocity.X *= airFriction;
                velocity.Z *= airFriction;
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
