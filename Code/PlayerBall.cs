using Sandbox.Diagnostics;

public sealed class PlayerBall : Component
{
	[Sync] private Connection Controller { get; set; }

	public void SetController( Connection controller )
	{
		Assert.True( Networking.IsHost );
		Controller = controller;
	}

	public bool IsLocallyControlled => Connection.Local == Controller;
	
	protected override void OnUpdate()
	{
		if ( IsLocallyControlled )
		{
			Scene.Camera.WorldPosition = WorldPosition + Vector3.Up * 300f + Vector3.Backward * 500f;
			Scene.Camera.WorldRotation = Rotation.LookAt( (WorldPosition - Scene.Camera.WorldPosition).Normal );
		}
	}
}
