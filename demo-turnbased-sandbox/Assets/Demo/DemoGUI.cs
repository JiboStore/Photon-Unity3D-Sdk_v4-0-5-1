using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class DemoGUI : MonoBehaviour
{
    public DemoGame GameInstance;
    public string AppId;            //set this in the editor in a scene!
    public Rect LobbyRect;  // set in inspector to position the lobby screen
    public Rect leftToolbar;  // set in inspector to position the lobby screen

    public void Start()
    {
        Application.runInBackground = true;
        CustomTypes.Register();

        leftToolbar = new Rect(leftToolbar.x, leftToolbar.y, leftToolbar.width, Screen.height - leftToolbar.y);

        this.GameInstance = new DemoGame();
        this.GameInstance.AppId = this.AppId;       // set in Inspector
        this.GameInstance.AppVersion = "1.11";

        this.GameInstance.NickName = !string.IsNullOrEmpty(NamepickerGui.NickName) ? NamepickerGui.NickName : "unityPlayer";
        this.GameInstance.UserId = this.GameInstance.NickName;      // this sandbox uses NickName == UserId

        this.GameInstance.OnStateChangeAction += this.OnStateChanged;
        //this.GameInstance.loadBalancingPeer.DebugOut = DebugLevel.ALL;
        this.GameInstance.ConnectToRegionMaster("EU");  // Turnbased games have to use this connect via Name Server
    }

    private void OnStateChanged(ClientState state)
    {
        if (state == ClientState.ConnectedToMaster)
        {
            this.GetRoomsList();
        }
    }

    private void GetRoomsList()
    {
        this.GameInstance.OpWebRpc("GetGameList", new Dictionary<string, object>());
    }

    public void OnApplicationQuit()
    {
        if (this.GameInstance != null && this.GameInstance.loadBalancingPeer != null)
        {
            this.GameInstance.Disconnect();
            this.GameInstance.loadBalancingPeer.StopThread();
        }
        this.GameInstance = null;
    }


    public bool SkipUpdate;
    public bool SetRandomRoomPropsAuto;
    private float timeToSend;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SkipUpdate = !SkipUpdate;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            this.SetRandomRoomPropsAuto = !this.SetRandomRoomPropsAuto;
        }

        if (SkipUpdate) return;
        if (this.SetRandomRoomPropsAuto && timeToSend < Time.time)
        {
            timeToSend = Time.time + 0.1f;
            this.GameInstance.OpSetCustomPropertiesOfRoom(new Hashtable() {{"t", Time.time}});
        }

        this.GameInstance.Service();

        // "back" button of phone will quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void OnGUI()
    {
        if (string.IsNullOrEmpty(this.AppId))
        {
            GUILayout.Label("You must enter your AppId from the Dashboard in the component: Scripts, DemoGUI.AppId before you can use this demo.");
            return;
        }

        GUI.skin.button.stretchWidth = true;
        GUI.skin.button.fixedWidth = 0;

        GUILayout.Label("name: " + this.GameInstance.NickName + " state: " + GameInstance.State.ToString());

        if (!string.IsNullOrEmpty(this.GameInstance.ErrorMessageToShow))
        {
            GUILayout.Label(this.GameInstance.ErrorMessageToShow);
        }

        switch (GameInstance.State)
        {
            case ClientState.JoinedLobby:
                this.GuiInLobby();
                break;
            case ClientState.Joined:
                this.GuiInGame();
                break;
            case ClientState.Disconnected:
                if (GUILayout.Button("Connect"))
                {
                    this.GameInstance.ConnectToRegionMaster("EU");  // Turnbased games have to use this connect via Name Server
                }
                break;
        }
    }


    private string roomToJoinName = "tobi-0795";
    private void GuiInLobby()
    {
        GUILayout.BeginArea(LobbyRect);
        GUILayout.Label("Lobby Screen");
        GUILayout.Label(string.Format("Players in rooms: {0} looking for rooms: {1}  rooms: {2}", this.GameInstance.PlayersInRoomsCount, this.GameInstance.PlayersOnMasterCount, this.GameInstance.RoomsCount));

        if (GUILayout.Button("Join Random (or create)"))
        {
            this.GameInstance.OpJoinRandomRoom(null, 0);
        }

        this.roomToJoinName = GUILayout.TextField(this.roomToJoinName);
        if (GUILayout.Button("Join"))
        {
            this.GameInstance.OpJoinRoom(this.roomToJoinName, -1);
        }
        if (GUILayout.Button("Create New Game"))
        {
            this.GameInstance.CreateTurnbasedRoom();
        }
        GUILayout.Space(20);

        GUILayout.Label("Saved Games: " + this.GameInstance.SavedGames.Count);
        foreach (KeyValuePair<string, int> savedRoom in this.GameInstance.SavedGames)
        {
            string roomName = savedRoom.Key;
            int actorNumber = savedRoom.Value;
            if (GUILayout.Button("ReJoin: " + roomName + " #" + actorNumber))
            {
                this.GameInstance.OpJoinRoom(roomName, actorNumber);
            }
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(150)))
        {
            this.GetRoomsList();
        }
        GUILayout.Space(20);

        GUILayout.Label("Rooms in lobby: " + this.GameInstance.RoomInfoList.Count);
        foreach (RoomInfo roomInfo in this.GameInstance.RoomInfoList.Values)
        {
            if (GUILayout.Button(roomInfo.Name + " turn: " + roomInfo.CustomProperties["t#"]))
            {
                this.GameInstance.OpJoinRoom(roomInfo.Name);
            }
        }

        GUILayout.EndArea();
    }

    private void GuiInGame()
    {
        GUILayout.BeginArea(leftToolbar);
        GUI.skin.button.stretchWidth = false;
        GUI.skin.button.fixedWidth = 150;

        // we are in a room, so we can access CurrentRoom and it's Players
        GUILayout.Label("In Room: " + this.GameInstance.CurrentRoom.Name);
        string interestingPropsAsString = FormatRoomProps();
        if (!string.IsNullOrEmpty(interestingPropsAsString))
        {
            GUILayout.Label("Props: " + interestingPropsAsString);
        }

        foreach (Player player in this.GameInstance.CurrentRoom.Players.Values)
        {
            if (player.ID == this.GameInstance.lastTurnPlayer)
            {
                GUILayout.Label(player.ToString() + " (played last)");
            }
            else
            {
                GUILayout.Label(player.ToString());
            }
        }

        GUILayout.Space(15);

        GUILayout.Label("Save the board by ending the turn.");
        if (GUILayout.Button("End Turn " + this.GameInstance.turnNumber + " (Save)"))
        {
            this.GameInstance.SaveBoardAsProperty();
        }

        GUILayout.Space(15);
        if (this.GameInstance.CurrentRoom.IsVisible)
        {
            GUILayout.Label("Start game to avoid more joins.");
            if (GUILayout.Button("Start (hide in lobby)"))
            {
                this.GameInstance.CurrentRoom.IsVisible = false;
            }
        }
        else
        {
            GUILayout.Label("Accept more players by matchmaking.");
            if (GUILayout.Button("Show in Lobby"))
            {
                this.GameInstance.CurrentRoom.IsVisible = true;
            }
        }

        GUILayout.Space(15);

        GUILayout.Label("To test, set some room/user props.");
        // creates a random property of this room with a random value to set
        if (GUILayout.Button("Set Room Property"))
        {
            Hashtable randomProps = new Hashtable();
            randomProps[RandomCustomRoomProp()] = Random.Range(0, 99);
            this.GameInstance.CurrentRoom.SetCustomProperties(randomProps);
        }

        // creates a random property of this player with a random value to set
        if (GUILayout.Button("Set Player Property"))
        {
            Hashtable randomProps = new Hashtable();
            randomProps[RandomCustomPlayerProp()] = Random.Range(0, 99);
            this.GameInstance.LocalPlayer.SetCustomProperties(randomProps);
        }

        if (GUILayout.Button("Clear Ev Cache (all)"))
        {
            this.GameInstance.ClearAllTileClickEv();
        }

        if (GUILayout.Button("Clear Ev Cache (turn)  " + (this.GameInstance.turnNumber-1)))
        {
            this.GameInstance.ClearTileClickEvForTurn(this.GameInstance.turnNumber-1);
        }

        if (GUILayout.Button("hide+close"))
        {
            this.GameInstance.CurrentRoom.IsOpen = false;
            this.GameInstance.CurrentRoom.IsVisible = false;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Leave (return later)"))
        {
            this.GameInstance.OpLeaveRoom(true);
        }
        if (GUILayout.Button("Abandon"))
        {
            this.GameInstance.OpLeaveRoom(false);
        }
        GUILayout.EndArea();
    }

    private string FormatRoomProps()
    {
        Hashtable customRoomProps = this.GameInstance.CurrentRoom.CustomProperties;
        string interestingProps = "";
        foreach (string propName in GameInstance.roomProps)
        {
            if (customRoomProps.ContainsKey(propName))
            {
                if (!string.IsNullOrEmpty(interestingProps)) interestingProps += " ";
                interestingProps += propName + ":" + customRoomProps[propName];
            }
        }
        return interestingProps;
    }

    private string RandomCustomRoomProp()
    {
        string[] roomProps = GameInstance.roomProps;
        return roomProps[Random.Range(0, roomProps.Length)];
    }

    private string RandomCustomPlayerProp()
    {
        string[] playerProps = GameInstance.playerProps;
        return playerProps[Random.Range(0, playerProps.Length)];
    }
}
