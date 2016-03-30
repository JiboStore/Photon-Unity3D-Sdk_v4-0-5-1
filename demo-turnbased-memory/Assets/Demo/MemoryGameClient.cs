using System.Collections;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;


public class SaveGameInfo
{
    public int MyPlayerId;
    public string RoomName;
    public string DisplayName;
    public bool MyTurn;
    public Dictionary<string, object> AvailableProperties;

    public string ToStringFull()
    {
        return string.Format("\"{0}\"[{1}] {2} ({3})", RoomName, MyPlayerId, MyTurn, SupportClass.DictionaryToString(AvailableProperties));
    }
}

/// <summary>The network/connection handling class of the Memory Demo.</summary>
/// <remarks>
/// This class extends the LoadBalancingClient, which implements the methods to communicate 
/// with a Photon Server (and the Photon Cloud) and handles the state for server changes.
/// 
/// This class keeps the general game state and is able to write and read it as "Room Properties".
/// Photon's room properties can be saved between sessions with Photon Turnbased.
/// 
/// This class also adds some properties which are useful to keep track of the game's state.
/// 
/// Important classes in this demo: MemoryGui, MemoryBoard, MemoryGameClient and NamePickerGui.
/// 
/// 
/// This demo uses a fair amount of room properties to save the state plus two room properties
/// that are made available in the save-game list (and the lobby):
/// "turn"     is the id of the player who's turn is next. not necessarily "who's turn was done last".
/// "players"  is a colon-separated list of the 2 player names.

/// </remarks>
public class MemoryGameClient : LoadBalancingClient
{
    public MemoryBoard board;
    public MemoryGui memoryGui;

    public const string PropTurn = "turn";
    public const string PropNames = "names";

    private const byte MaxPlayers = 2;
     
    public int TurnNumber = 1;
    
    public int PlayerIdToMakeThisTurn;  // who's turn this is. when "done", set the other player's actorNumber and save

    public bool IsMyTurn
    {
        get
        {
            //Debug.Log(PlayerIdToMakeThisTurn + "'s turn. You are: " + this.LocalPlayer.ID); 
            return this.PlayerIdToMakeThisTurn == this.LocalPlayer.ID;
        }
    }
    
    public byte MyPoints = 0;
    public byte OthersPoints = 0;
    public List<SaveGameInfo> SavedGames = new List<SaveGameInfo>();

    public bool GameIsLoaded
    {
        get
        {
            return this.CurrentRoom != null && this.CurrentRoom.CustomProperties != null && this.CurrentRoom.CustomProperties.ContainsKey("pt");
        }
    }

    public bool GameCanStart 
    {
        get { return this.CurrentRoom != null && this.CurrentRoom.Players.Count == 2; }
    }

    public bool GameWasAbandoned
    {
        get { return this.CurrentRoom != null && this.CurrentRoom.Players.Count < 2 && this.CurrentRoom.CustomProperties.ContainsKey("flips"); }
    }

    public bool IsMyScoreHigher
    {
        get { return this.MyPoints > this.OthersPoints; }
    }

    public bool IsScoreTheSame
    {
        get { return this.MyPoints == this.OthersPoints; }
    }


    /// <summary>Returns the Player instance for the remote player (not LocalPlayer.ID) in a two-player game.</summary>
    /// <returns>Might be null if there is no other player yet or anymore.</returns>
    public Player Opponent
    {
        get
        {
            
            Player opp = this.LocalPlayer.GetNext();
            //Debug.Log("you: " + this.LocalPlayer.ToString() + " other: " + opp.ToString());
            return opp;
        }
    }

    public override void OnOperationResponse(OperationResponse operationResponse)
    {
        base.OnOperationResponse(operationResponse);

        switch (operationResponse.OperationCode)
        {
            case (byte)OperationCode.WebRpc:
                if (operationResponse.ReturnCode == 0)
                {
                    this.OnWebRpcResponse(new WebRpcResponse(operationResponse));
                }
                break;
            case (byte)OperationCode.JoinGame:
            case (byte)OperationCode.CreateGame:
                if (operationResponse.ReturnCode != 0)
                {
                    Debug.Log(string.Format("Join or Create failed for: '{2}' Code: {0} Msg: {1}", operationResponse.ReturnCode, operationResponse.DebugMessage, this.CurrentRoom));
                }
                if (this.Server == ServerConnection.GameServer)
                {
                    if (operationResponse.ReturnCode == 0)
                    {
                        this.LoadBoardFromProperties(false);
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


    public override void OnEvent(EventData photonEvent)
    {
        base.OnEvent(photonEvent);

        switch (photonEvent.Code)
        {
            case EventCode.PropertiesChanged:
				//Debug.Log("Got Properties via Event. Update board by room props.");
                this.LoadBoardFromProperties(true);
                this.board.ShowFlippedTiles();
				break;
            case EventCode.Join:
                if (this.CurrentRoom.Players.Count == 2 && this.CurrentRoom.IsOpen)
                {
                    this.CurrentRoom.IsOpen = false;
                    this.CurrentRoom.IsVisible = false;
                    this.SavePlayersInProps();
                }
                break;
            case EventCode.Leave:
                if (this.CurrentRoom.Players.Count == 1 && !this.GameWasAbandoned)
                {
                    this.CurrentRoom.IsOpen = true;
                    this.CurrentRoom.IsVisible = true;
                }
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
            case StatusCode.Exception:
            case StatusCode.ExceptionOnReceive:
            case StatusCode.TimeoutDisconnect:
            case StatusCode.DisconnectByServer:
            case StatusCode.DisconnectByServerLogic:
                Debug.LogError(string.Format("Error on connection level. StatusCode: {0}", statusCode));
                break;
            case StatusCode.ExceptionOnConnect:
                Debug.LogWarning(string.Format("Exception on connection level. Is the server running? Is the address ({0}) reachable?", this.CurrentServerAddress));
                break;
            case StatusCode.Disconnect:
                this.SavedGames.Clear();
                break;
        }
    }

    private void OnWebRpcResponse(WebRpcResponse response)
    {
        Debug.Log(string.Format("OnWebRpcResponse. Code: {0} Content: {1}", response.ReturnCode, SupportClass.DictionaryToString(response.Parameters)));
        if (response.ReturnCode == 0)
        {
            if (response.Parameters == null)
            {
                Debug.Log("WebRpc executed ok but didn't get content back. This happens for empty save-game lists.");
                memoryGui.GameListUpdate();
                return;
            }

            if (response.Name.Equals("GetGameList"))
            {
                this.SavedGames.Clear();

                // the response for GetGameList contains a Room's name as Key and another Dictionary<string,object> with the values the web service sends
                foreach (KeyValuePair<string, object> pair in response.Parameters)
                {
                    // per key (room name), we send 
                    // "ActorNr" which is the PlayerId/ActorNumber this user had in the room
                    // "Properties" which is another Dictionary<string,object> with the properties that the lobby sees
                    Dictionary<string, object> roomValues = pair.Value as Dictionary<string, object>;

                    SaveGameInfo si = new SaveGameInfo();
                    si.RoomName = pair.Key;
                    si.DisplayName = pair.Key;  // we might have a better display name for this room. see below.
                    si.MyPlayerId = (int)roomValues["ActorNr"];
                    si.AvailableProperties = roomValues["Properties"] as Dictionary<string, object>;
                    
                    // let's find out of it's our turn to play and if we know the opponent's name (which we will display as game name). 
                    if (si.AvailableProperties != null)
                    {
                        // PropTurn is a value per room that gets set to the player who's turn is next.
                        if (si.AvailableProperties.ContainsKey(PropTurn))
                        {
                            int nextPlayer = (int) si.AvailableProperties[PropTurn];
                            si.MyTurn = nextPlayer == si.MyPlayerId;
                        }

                        // PropNames is set to a list of the player names. this can easily be turned into a name for the game to display
                        if (si.AvailableProperties.ContainsKey(PropNames))
                        {
                            string display = (string)si.AvailableProperties[PropNames];
                            display = display.ToLower();
                            display = display.Replace(this.NickName.ToLower(), "");
                            display = display.Replace(";", "");
                            si.DisplayName = "vs. " + display;
                        }
                    }

                    //Debug.Log(si.ToStringFull());
                    this.SavedGames.Add(si);
                }
                memoryGui.GameListUpdate();
            }
        }

    }

    public void SaveBoardToProperties()
    {
        Hashtable boardProps = board.GetBoardAsCustomProperties();
        boardProps.Add("pt", this.PlayerIdToMakeThisTurn);  // "pt" is for "player turn" and contains the ID/actorNumber of the player who's turn it is
        boardProps.Add("t#", this.TurnNumber);
        boardProps.Add("tx#", board.TilesX);
        boardProps.Add("ty#", board.TilesY);
        boardProps.Add(GetPlayerPointsPropKey(this.LocalPlayer.ID), this.MyPoints); // we always only save "our" points. this will not affect the opponent's score.

        
        // our turn will be over if 2 tiles are clicked/flipped but not the same. in that case, we update the other player if inactive
        bool webForwardToPush = false;
        if (board.AreTwoTilesFlipped() && !board.AreTheSameTilesFlipped())
        {
            Player otherPlayer = this.Opponent;
            if (otherPlayer != null)
            {
                boardProps.Add(PropTurn, otherPlayer.ID); // used to identify which player's turn the NEXT is. the WebHooks might send a PushMessage to that user.
                if (otherPlayer.IsInactive) 
                {
                    webForwardToPush = true;            // this will send the props to the WebHooks, which in turn will push a message to the other player.
                }
            }
        }

        
        //Debug.Log(string.Format("saved board to room-props {0}", SupportClass.DictionaryToString(boardProps)));
        this.OpSetCustomPropertiesOfRoom(boardProps, null, webForwardToPush);
    }

    public void SavePlayersInProps()
    {
        if (this.CurrentRoom == null || this.CurrentRoom.CustomProperties == null || this.CurrentRoom.CustomProperties.ContainsKey(PropNames))
        {
            Debug.Log("Skipped saving names. They are already saved.");
            return;
        }

        Debug.Log("Saving names.");
        Hashtable boardProps = new Hashtable();
        boardProps[PropNames] = string.Format("{0};{1}", this.LocalPlayer.NickName, this.Opponent.NickName);
        this.OpSetCustomPropertiesOfRoom(boardProps, null, false);
    }

    public void LoadBoardFromProperties(bool calledByEvent)
    {
        //board.InitializeBoard();
        
        Hashtable roomProps = this.CurrentRoom.CustomProperties;
        Debug.Log(string.Format("Board Properties: {0}", SupportClass.DictionaryToString(roomProps)));

        if (roomProps.Count == 0)
        {
            // we are in a fresh room with no saved board.
            board.InitializeBoard();
            board.RandomBoard();
            this.SaveBoardToProperties();
        }


        // we are in a game that has props (a board). read those (as update or as init, depending on calledByEvent)
        bool success = board.SetBoardByCustomProperties(roomProps, calledByEvent);
        if (!success)
        {
            Debug.LogError("Not loaded board from props?");
        }

        
        // we set properties "pt" (player turn) and "t#" (turn number). those props might have changed
        // it's easier to use a variable in gui, so read the latter property now
        if (this.CurrentRoom.CustomProperties.ContainsKey("t#"))
        {
            this.TurnNumber = (int) this.CurrentRoom.CustomProperties["t#"];
        }
        else
        {
            this.TurnNumber = 1;
        }

        if (this.CurrentRoom.CustomProperties.ContainsKey("pt"))
        {
            this.PlayerIdToMakeThisTurn = (int) this.CurrentRoom.CustomProperties["pt"];
            //Debug.Log("This turn was played by player.ID: " + this.PlayerIdToMakeThisTurn);
        }
        else
        {
            this.PlayerIdToMakeThisTurn = 0;
        }

        // if the game didn't save a player's turn yet (it is 0): use master
        if (this.PlayerIdToMakeThisTurn == 0)
        {
            this.PlayerIdToMakeThisTurn = this.CurrentRoom.MasterClientId;
        }

        this.MyPoints = GetPlayerPointsFromProps(this.LocalPlayer);
        this.OthersPoints = GetPlayerPointsFromProps(this.Opponent);
    }

    private string GetPlayerPointsPropKey(int id)
    {
        return string.Format("pt{0}", id);
    }

    byte GetPlayerPointsFromProps(Player player)
    {
        if (player == null || player.ID < 1)
        {
            return 0;
        }

        string pointsKey = GetPlayerPointsPropKey(player.ID);
        if (this.CurrentRoom.CustomProperties.ContainsKey(pointsKey))
        {
            return (byte)this.CurrentRoom.CustomProperties[pointsKey];
        }

        return 0;
    }


    public void CreateTurnbasedRoom()
    {
        string newRoomName = string.Format("{0}-{1}", this.NickName, Random.Range(0,1000).ToString("D4"));    // for int, Random.Range is max-exclusive!
        Debug.Log(string.Format("CreateTurnbasedRoom(): {0}", newRoomName));

        RoomOptions roomOptions = new RoomOptions()
                                      {
                                          MaxPlayers = 2,
                                          CustomRoomPropertiesForLobby = new string[] { PropTurn, PropNames },
                                          PlayerTtl = int.MaxValue,
                                          EmptyRoomTtl = 5000
                                      };
        this.OpCreateRoom(newRoomName, roomOptions, TypedLobby.Default);
    }

    public void HandoverTurnToNextPlayer()
    {
        if (this.LocalPlayer != null)
        {
            Player nextPlayer = this.LocalPlayer.GetNextFor(this.PlayerIdToMakeThisTurn);
            if (nextPlayer != null)
            {
                this.PlayerIdToMakeThisTurn = nextPlayer.ID;
                return;
            }
        }

        this.PlayerIdToMakeThisTurn = 0;
    }
}
