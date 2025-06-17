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

	void INetworkListener.OnActive( Connection channel )
	{
		var playerObject = PlayerPrefab.Clone();
		var player = playerObject.GetComponent<PlayerBall>();
		player.WorldPosition = Vector3.Up * 100f;
		player.SetController( channel );
		playerObject.NetworkSpawn();
	}
}
