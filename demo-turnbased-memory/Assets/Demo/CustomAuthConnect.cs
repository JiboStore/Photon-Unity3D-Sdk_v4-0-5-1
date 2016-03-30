using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;
using System.Collections;

public class CustomAuthConnect : MonoBehaviour
{
    private LoadBalancingClient loadBalancingClient;
    public string TurnbasedAppId;    // set in inspector! must be a turnbased app
    public string UserName;
    public string UserToken;

	// Use this for initialization
	void Start () 
    {
        loadBalancingClient = new LoadBalancingClient(null, TurnbasedAppId, "1.0"); // the master server address is not used when connecting via nameserver
	    
        AuthenticationValues customAuth = new AuthenticationValues();
        customAuth.AddAuthParameter("username", UserName);  // expected by the demo custom auth service
        customAuth.AddAuthParameter("token", UserToken);    // expected by the demo custom auth service
        loadBalancingClient.AuthValues = customAuth;

        loadBalancingClient.AutoJoinLobby = false;
	    loadBalancingClient.ConnectToRegionMaster("eu");
	}

    void OnApplicationQuit()
    {
        if (loadBalancingClient != null) loadBalancingClient.Disconnect();  // don't forget to disconnect. unity doesn't like open sockets
    }
	
	void Update () 
    {
	    if (loadBalancingClient != null)
	    {
	        loadBalancingClient.Service();  // easy but ineffective. should be refined to using dispatch every frame and sendoutgoing on demand
	    }
	}

    void OnGUI()
    {
        if (loadBalancingClient != null)
        {
            GUILayout.Label(loadBalancingClient.State + " " + loadBalancingClient.Server);
        }
    }
}
