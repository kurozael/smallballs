using System.Threading.Tasks;
using Sandbox.Network;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	
	protected override Task OnLoad()
	{
		if ( !Scene.IsEditor && !Networking.IsActive )
		{
			Networking.CreateLobby( new LobbyConfig() );
		}
		
		return base.OnLoad();
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		var balls = Scene.GetAll<PlayerBall>().Where( x => x.ControllerId == channel.Id );

		foreach ( var ball in balls )
		{
			ball.Destroy();
		}
	}
	
	void INetworkListener.OnActive( Connection channel )
	{
		var playerObject = PlayerPrefab.Clone();
		var player = playerObject.GetComponent<PlayerBall>();
		player.WorldPosition = Vector3.Up * 100f;
		player.PlayerColor = Color.Random;
		player.SetController( channel );
		playerObject.NetworkSpawn();
	}
}
