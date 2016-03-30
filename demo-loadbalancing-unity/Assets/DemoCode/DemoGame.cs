// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2015
// </copyright>
// <summary>
// Extending the LoadBalancingClient, this class implements game-related logic for the demo.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;


public class DemoGame : LoadBalancingClient
{
    public string ErrorMessageToShow { get; set; }

    public Vector3 lastMoveEv;

    public int evCount = 0;

    // overriding the CreatePlayer "factory" provides us with custom DemoPlayers (that also know their position)
    protected internal override Player CreatePlayer(string actorName, int actorNumber, bool isLocal, Hashtable actorProperties)
    {
        return new Player(actorName, actorNumber, isLocal, actorProperties);
    }

    public void SendMove()
    {
        Hashtable evData = new Hashtable();
        evData[(byte)1] = Random.onUnitSphere;
        this.loadBalancingPeer.OpRaiseEvent(1, evData, true, null);
    }

    public override void OnOperationResponse(OperationResponse operationResponse)
    {
        base.OnOperationResponse(operationResponse);
        this.DebugReturn(DebugLevel.ERROR, operationResponse.ToStringFull());

        switch (operationResponse.OperationCode)
        {
            case (byte)OperationCode.Authenticate:
                if (operationResponse.ReturnCode == ErrorCode.InvalidAuthentication)
                {
                    this.ErrorMessageToShow = string.Format("Authentication failed. Your AppId: {0}.\nMake sure to set the AppId in DemoGUI.cs by replacing \"<insert your app id here>\".\nResponse: {1}", this.AppId, operationResponse.ToStringFull());
                    this.DebugReturn(DebugLevel.ERROR, this.ErrorMessageToShow);
                }
                if (operationResponse.ReturnCode == ErrorCode.InvalidOperation || operationResponse.ReturnCode == ErrorCode.InternalServerError)
                {
                    this.ErrorMessageToShow = string.Format("Authentication failed. You successfully connected but the server ({0}) but it doesn't know the 'authenticate'. Check if it runs the Loadblancing server-logic.\nResponse: {1}", this.MasterServerAddress, operationResponse.ToStringFull());
                    this.DebugReturn(DebugLevel.ERROR, this.ErrorMessageToShow);
                }
                break;

            case (byte)OperationCode.CreateGame:
                string gsAddress = (string)operationResponse[ParameterCode.Address];
                if (!string.IsNullOrEmpty(gsAddress) && gsAddress.StartsWith("127.0.0.1"))
                {
                    this.ErrorMessageToShow = string.Format("The master forwarded you to a gameserver with address: {0}.\nThat address points to 'this computer' anywhere. This might be a configuration error in the game server.", gsAddress);
                    this.DebugReturn(DebugLevel.ERROR, this.ErrorMessageToShow);
                }
                break;

            case (byte)OperationCode.JoinRandomGame:
                string gsAddressJoin = (string)operationResponse[ParameterCode.Address];
                if (!string.IsNullOrEmpty(gsAddressJoin) && gsAddressJoin.StartsWith("127.0.0.1"))
                {
                    this.ErrorMessageToShow = string.Format("The master forwarded you to a gameserver with address: {0}.\nThat address points to 'this computer' anywhere. This might be a configuration error in the game server.", gsAddressJoin);
                    this.DebugReturn(DebugLevel.ERROR, this.ErrorMessageToShow);
                }

                if (operationResponse.ReturnCode != 0)
                {
                    this.OpCreateRoom(null, new RoomOptions() { MaxPlayers = 4 }, null);
                }
                break;
        }
    }

    public override void OnEvent(EventData photonEvent)
    {
        base.OnEvent(photonEvent);

        switch (photonEvent.Code)
        {
            case (byte)1:
                Hashtable content = photonEvent.Parameters[ParameterCode.CustomEventContent] as Hashtable;
                this.lastMoveEv = (Vector3)content[(byte)1];
                this.evCount++;
                break;

            case EventCode.PropertiesChanged:
                var data = photonEvent.Parameters[ParameterCode.Properties] as Hashtable;
                DebugReturn(DebugLevel.ALL, "Got EV PropertiesChanged: " + (data["data"] as string));
                break;
        }
    }

    public override void DebugReturn(DebugLevel level, string message)
    {
        base.DebugReturn(level, message);
        Debug.Log(message);
    }

    public override void OnStatusChanged(StatusCode statusCode)
    {
        base.OnStatusChanged(statusCode);

        switch (statusCode)
        {
            case StatusCode.TimeoutDisconnect:
                Debug.LogError("Timeout by client.");
                break;
            case StatusCode.DisconnectByServer:
                Debug.LogError("Timeout by server received.");
                break;
            case StatusCode.Exception:
            case StatusCode.ExceptionOnConnect:
                Debug.LogWarning("Exception on connection level. Is the server running? Is the address (" + this.MasterServerAddress+ ") reachable?");
                break;
        }
    }
}
