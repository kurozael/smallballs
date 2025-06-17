using System;
using Sandbox.Diagnostics;

public sealed class PlayerBall : Component
{
	[Property] public float JumpPower { get; set; } = 1f;
	[Property] public float Acceleration { get; set; } = 1f;
	[Property] public float MaxSpeed { get; set; } = 800f;
	[Property] public Color PlayerColor { get; set; }
	
	[Sync] private Connection Controller { get; set; }
	
	private RealTimeUntil NextInputTime { get; set; }

	struct PlayerInput
	{
		public Vector3 MoveDir { get; set; }
	}

	public void SetController( Connection controller )
	{
		Assert.True( Networking.IsHost );
		Controller = controller;
	}

	private TimeUntil _nextJumpTime;
	private PlayerInput _input;

	public bool IsLocallyControlled => Connection.Local == Controller;

	private readonly float cameraDistance = 1000f;
	private float _cameraYaw = 0f;
	private float _cameraPitch = 20f;

	protected override void OnStart()
	{
		var mr = GetComponent<ModelRenderer>();
		var hl = GetComponent<HighlightOutline>();
		hl.Color = PlayerColor;
		mr.Tint = PlayerColor;
	}

	protected override void OnUpdate()
	{
		if ( !IsLocallyControlled )
			return;
		
		var mouseX = Input.MouseDelta.x;
		var mouseY = Input.MouseDelta.y;

		_cameraYaw -= mouseX * 0.1f;
		_cameraPitch += mouseY * 0.1f;
		_cameraPitch = Math.Clamp( _cameraPitch, -80f, 80f );
		
		var orbitRotation = Rotation.From( _cameraPitch, _cameraYaw, 0 );
		var desiredOffset = orbitRotation.Forward * -cameraDistance;
		
		var desiredCameraPos = WorldPosition + Vector3.Up * 100f + desiredOffset;
		
		var trace = Scene.Trace.Ray( WorldPosition + Vector3.Up * 100f, desiredCameraPos )
			.IgnoreGameObjectHierarchy( GameObject )
			.Size( 10f )
			.Run();
		
		if ( trace.Hit )
			Scene.Camera.WorldPosition = trace.EndPosition + trace.Direction * 10f;
		else
			Scene.Camera.WorldPosition = desiredCameraPos;
		
		Scene.Camera.WorldRotation = Rotation.LookAt( WorldPosition - Scene.Camera.WorldPosition );

		if ( Input.Pressed( "Jump" ) )
		{
			Jump();
		}
			
		if ( !NextInputTime )
			return;

		var cameraRotation = Scene.Camera.WorldRotation;
		var moveDir = Vector3.Zero;

		if ( Input.Down( "Forward" ) )
			moveDir += cameraRotation.Forward;
		
		if ( Input.Down( "Backward" ) )
			moveDir += cameraRotation.Backward;
		
		if ( Input.Down( "Left" ) )
			moveDir += cameraRotation.Left;
		
		if ( Input.Down( "Right" ) )
			moveDir += cameraRotation.Right;
		
		moveDir = moveDir.Normal;
				
		var input = new PlayerInput
		{
			MoveDir = moveDir
		};

		SendInput( input );
		NextInputTime = 1f / 30f;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		var rb = GetComponent<Rigidbody>();
		
		if ( !_input.MoveDir.IsNearZeroLength )
		{
			rb.ApplyTorque( Vector3.Cross( Vector3.Up, _input.MoveDir ) * rb.Mass * 50000f * Acceleration );
		}
		
		rb.Velocity = rb.Velocity.ClampLength( MaxSpeed );
	}

	private bool IsOnGround()
	{
		var trace = Scene.Trace.Ray( WorldPosition, WorldPosition + Vector3.Down * 50f )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		return trace.Hit;
	}

	[Rpc.Owner]
	private void Jump()
	{
		if ( !_nextJumpTime || !IsOnGround() ) return;

		var rb = GetComponent<Rigidbody>();
		var force = rb.Mass * 50000f * JumpPower;
		rb.ApplyForce( Vector3.Up * force );

		_nextJumpTime = 0.1f;
	}

	[Rpc.Owner( NetFlags.UnreliableNoDelay )]
	private void SendInput( PlayerInput input )
	{
		_input = input;
	}
}
