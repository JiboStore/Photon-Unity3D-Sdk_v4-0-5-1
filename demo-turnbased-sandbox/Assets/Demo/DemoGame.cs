using System.Collections;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

public class DemoGame : LoadBalancingClient
{
    public string ErrorMessageToShow { get; set; }

    // some props a player might have. using random values as showcase
    internal protected string[] playerProps = new string[] { "skill", "rank", "guild" };
    // some props a room might have. using random values as showcase. more room props are used in this demo!
    internal protected string[] roomProps = new string[] { "map", "skill", "mode" };
    // room properties we want to get in the lobby room list and in the save-game list with WebRpc "GetGameList"
    private static string[] RoomPropsInLobby = new string[]{ "t#" };

    private const byte MaxPlayers = 2;

    public int turnNumber = 1;
    public int lastTurnPlayer;

    public const byte EvTileClick = 1;
    public List<List<int>> lastTilesClicked = new List<List<int>>();
    public int evCount = 0;

    public Dictionary<string, int> SavedGames = new Dictionary<string, int>();


    // overriding the CreatePlayer "factory" provides us with custom DemoPlayers (that also know their position)
    protected internal override Player CreatePlayer(string actorName, int actorNumber, bool isLocal, Hashtable actorProperties)
    {
        return new DemoPlayer(actorName, actorNumber, isLocal, actorProperties);
    }

    public override void OnOperationResponse(OperationResponse operationResponse)
    {
        base.OnOperationResponse(operationResponse);
        // this.DebugReturn(DebugLevel.ERROR, operationResponse.ToStringFull());    // log as ERROR to make sure it's not filtered out due to log level

        switch (operationResponse.OperationCode)
        {
            case (byte)OperationCode.WebRpc:
                Debug.Log("WebRpc-Response: " + operationResponse.ToStringFull());
                if (operationResponse.ReturnCode == 0)
                {
                    this.OnWebRpcResponse(new WebRpcResponse(operationResponse));
                }
                break;
            case (byte)OperationCode.JoinGame:
            case (byte)OperationCode.CreateGame:
                if (this.Server == ServerConnection.GameServer)
                {
                    if (operationResponse.ReturnCode == 0)
                    {
                        this.UpdateBoard();
                    }
                }
                break;
            case (byte)OperationCode.JoinRandomGame:
                if (operationResponse.ReturnCode == ErrorCode.NoRandomMatchFound)
                {
                    // no room found: we create one!
                    this.CreateTurnbasedRoom();
                }
                break;
        }
    }

    private void OnWebRpcResponse(WebRpcResponse response)
    {
        if (response.ReturnCode != 0)
        {
            Debug.Log(response.ToStringFull());     // in an error case, it's often helpful to see the full response
            return;
        }

        if (response.Name.Equals("GetGameList"))
        {
            this.SavedGames.Clear();

            if (response.Parameters == null)
            {
                Debug.Log("WebRpcResponse for GetGameList contains no rooms: " + response.ToStringFull());
                return;
            }

            // the response for GetGameList contains a Room's name as Key and another Dictionary<string,object> with the values the web service sends
            foreach (KeyValuePair<string, object> pair in response.Parameters)
            {
                // per key (room name), we send
                // "ActorNr" which is the PlayerId/ActorNumber this user had in the room
                // "Properties" which is another Dictionary<string,object> with the properties that the lobby sees
                Dictionary<string, object> roomValues = pair.Value as Dictionary<string, object>;

                int savedActorNumber = (int)roomValues["ActorNr"];
                Dictionary<string, object> savedRoomProps = roomValues["Properties"] as Dictionary<string, object>; // we are not yet using these in this demo

                this.SavedGames.Add(pair.Key, savedActorNumber);
                Debug.Log(pair.Key + " actorNr: " + savedActorNumber + " props: " + SupportClass.DictionaryToString(savedRoomProps));
            }
        }
    }

    public void SendTileClickEv(int index)
    {
        Debug.Log("Send Tile Click");
        Hashtable content = new Hashtable();
        content[(byte)1] = this.turnNumber;
        content[(byte)2] = index;
        this.loadBalancingPeer.OpRaiseEvent(EvTileClick, content, true, new RaiseEventOptions() { CachingOption = EventCaching.AddToRoomCache });

        while (turnNumber >= this.lastTilesClicked.Count)
        {
            this.lastTilesClicked.Add(new List<int>());
        }
        this.lastTilesClicked[turnNumber].Add(index);
    }

    public void ClearTileClickEvForTurn(int turnToDelete)
    {
        Debug.Log("Clean Tile Click for Turn " + turnToDelete);
        Hashtable content = new Hashtable();
        content[(byte)1] = turnToDelete;
        this.loadBalancingPeer.OpRaiseEvent(EvTileClick, content, true, new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache });
        this.lastTilesClicked[turnToDelete].Clear();
    }

    public void ClearAllTileClickEv()
    {
        Debug.Log("Clean All Tile Click");
        this.loadBalancingPeer.OpRaiseEvent(EvTileClick, null, true, new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache });
        this.lastTilesClicked.Clear();
    }

    public override void OnEvent(EventData photonEvent)
    {
        base.OnEvent(photonEvent);

        switch (photonEvent.Code)
        {
            case (byte)EvTileClick:
				object content = photonEvent.Parameters[ParameterCode.CustomEventContent];
                Hashtable turnClick = content as Hashtable;
                if (turnClick != null)
                {
                    int turnNumber = (int)turnClick[(byte)1];
                    int clickedTile = (int)turnClick[(byte)2];


                    while (turnNumber >= this.lastTilesClicked.Count)
                    {
                        this.lastTilesClicked.Add(new List<int>());
                    }
                    this.lastTilesClicked[turnNumber].Add(clickedTile);
                    this.evCount++;
                    Debug.Log("got click ev. tile: " + clickedTile + " turn: " + turnNumber);
                }
                break;

			case EventCode.PropertiesChanged:
				DebugReturn(DebugLevel.ALL, "Got Properties via Event. Update Board by room props.");
                this.UpdateBoard();
				break;
        }
    }

    public override void DebugReturn(DebugLevel level, string message)
    {
        //base.DebugReturn(level, message);
        Debug.Log(message);
    }

    public override void OnStatusChanged(StatusCode statusCode)
    {
        base.OnStatusChanged(statusCode);

        switch (statusCode)
        {
            case StatusCode.Exception:
            case StatusCode.ExceptionOnConnect:
                Debug.LogWarning("Exception on connection level. Is the server running? Is the address (" + this.MasterServerAddress+ ") reachable?");
                break;
            case StatusCode.Disconnect:
                HideBoard();
                break;
        }
    }

    public void SaveBoardAsProperty()
    {
        CubeBoard board = GameObject.FindObjectOfType(typeof(CubeBoard)) as CubeBoard;
        this.turnNumber = this.turnNumber + 1;
        this.lastTurnPlayer = this.LocalPlayer.ID;

        Hashtable boardProps = board.GetBoardAsCustomProperties();
        boardProps.Add("lt", this.lastTurnPlayer);  // "lt" is for "last turn" and contains the ID/actorNumber of the player who did the last one
        boardProps.Add("t#", this.turnNumber);

        this.OpSetCustomPropertiesOfRoom(boardProps);

        Debug.Log("saved board to props " + SupportClass.DictionaryToString(boardProps));
    }

    public void UpdateBoard()
    {
        // we set properties "lt" (last turn) and "t#" (turn number). those props might have changed
        // it's easier to use a variable in gui, so read the latter property now
        if (this.CurrentRoom.CustomProperties.ContainsKey("t#"))
        {
            this.turnNumber = (int) this.CurrentRoom.CustomProperties["t#"];
        }
        else
        {
            this.turnNumber = 1;
        }
        if (this.CurrentRoom.CustomProperties.ContainsKey("lt"))
        {
            this.lastTurnPlayer = (int) this.CurrentRoom.CustomProperties["lt"];
        }
        else
        {
            this.lastTurnPlayer = 0;    // unknown
        }

        CubeBoard board = GameObject.FindObjectOfType(typeof(CubeBoard)) as CubeBoard;
        if (!board.enabled)
        {
            board.enabled = true;
            board.ResetTileValues();
        }

        Hashtable roomProps = this.CurrentRoom.CustomProperties;
        bool success = board.SetBoardByCustomProperties(roomProps);
        Debug.Log("loaded board from room props. Success: " + success);

        board.ShowCubes();
    }

    public void HideBoard()
    {
        CubeBoard board = GameObject.FindObjectOfType(typeof(CubeBoard)) as CubeBoard;
        if (board.enabled) board.enabled = false;
        this.lastTilesClicked.Clear();
    }

    /// <summary>
    /// Demo method that makes up a roomname and sets up the room in general.
    /// </summary>
    public void CreateTurnbasedRoom()
    {
        string newRoomName = this.NickName + "-" +Random.Range(0,1000).ToString("D4");    // for int, Random.Range is max-exclusive!
        Debug.Log("CreateTurnbasedRoom() will create: " + newRoomName);

        RoomOptions demoRoomOptions = new RoomOptions()
                                          {
                                              MaxPlayers = DemoGame.MaxPlayers,
                                              CustomRoomPropertiesForLobby = DemoGame.RoomPropsInLobby,
                                              EmptyRoomTtl = 5000,
                                              PlayerTtl = int.MaxValue
                                          };
        this.OpCreateRoom(newRoomName, demoRoomOptions, TypedLobby.Default);
    }
}
