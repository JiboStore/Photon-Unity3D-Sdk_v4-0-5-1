using System.Collections;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


/// <summary>Main class of the Memory Demo, handling GUI, state and the connection.</summary>
/// <remarks>
/// Important classes in this demo: MemoryGui, MemoryBoard, MemoryGameClient and NamePickerGui.
/// </remarks>
[RequireComponent(typeof(MemoryBoard))]
public class MemoryGui : MonoBehaviour
{
    public MemoryGameClient GameClientInstance; // keeps part of the state and connection
    public string AppId;            // set in inspector

    public TileMonoGroup MainMenuRoot;
    public TileMono MainMenuInfo;
    public GameObject[] LoadGameButtons;

    public GameObject InGameTextRoot;   // root of ingame texts, actually only used by MemoryBoard
    public GameObject InGameInfoText;
    public GameObject InGameScoresText;
    

    public enum GameState { MainMenu, SwitchToGame, InGame, SwitchToMenu, Login }
    public GameState CurrentState = GameState.Login;    // set in inspector
    

    private MemoryBoard board;
    private int savegameListStartIndex = 0;     // paging for saved-game list
    private const bool OnGuiShortcut = true;    // during development OnGUI was used for debugging


    private bool visible;
    public bool Visible
    {
        get { return visible; }
        set
        {
            visible = value;
            this.OnVisibleChanged();
        }
    }

    public void Awake()
    {
        if (string.IsNullOrEmpty(this.AppId))
        {
            Debug.LogError("You must enter your AppId from the Dashboard in the component: Scripts, MemoryGui, AppId before you can use this demo.");
            Debug.Break();
        }

        Application.runInBackground = true;
        CustomTypes.Register();
        
        this.GameClientInstance = new MemoryGameClient();
        
        this.GameClientInstance.memoryGui = this;
        this.GameClientInstance.AppId = this.AppId;       // set in Inspector
        this.GameClientInstance.AppVersion = "1.0";
        

        board = this.GetComponentInChildren<MemoryBoard>();
        board.GameClientInstance = this.GameClientInstance;
        GameClientInstance.board = board;
        board.MemoryGui = this;
        this.DisableButtons();
    }

    public void OnEnable()
    {
        //Debug.Log(string.Format("Awake GameClientState: {0}", this.GameClientInstance.State));
        if (this.GameClientInstance.IsConnected)
        {
            Debug.Log("Already connected.");
            return;
        }

        this.GameClientInstance.NickName = !string.IsNullOrEmpty(NamepickerGui.NickName) ? NamepickerGui.NickName : "unityPlayer";
        // For sake of simplicity, this demo sets the UserID to the name entered. 
        // In your released game, you would want proper UserIDs, accounts and authentication. Rooms are persisted per UserID.
        this.GameClientInstance.UserId = this.GameClientInstance.NickName;
        this.GameClientInstance.OnStateChangeAction += this.OnStateChanged;

        this.GameClientInstance.ConnectToRegionMaster("EU");
        this.MainMenuInfo.Back.Text = "Connecting";
        this.MainMenuInfo.ToBack();
    }

    /// <summary>Attempt a disconnect, if still connected.</summary>
    public void OnApplicationQuit()
    {
        if (this.GameClientInstance != null && this.GameClientInstance.loadBalancingPeer != null)
        {
            this.GameClientInstance.Disconnect();
            this.GameClientInstance.loadBalancingPeer.StopThread();
        }
        this.GameClientInstance = null;
    }

    void onPushNotificationsReceived(string payload)
    {
        if (this.GameClientInstance.Server == LoadBalancingClient.ServerConnection.MasterServer)
        {
            this.GetRoomsList();    // refresh the room list if we got a push notification while on master server (not playing)
        }
    }

    private void OnStateChanged(ClientState state)
    {
        switch (state)
        {
            case ClientState.ConnectedToMaster:
                this.Visible = true;
                this.GetRoomsList();
                break;
            case ClientState.Joining:
                // hidemenu should be called by a button?
                break;
        }
    }

    void OnVisibleChanged()
    {
        //Debug.Log("MemoryGui.OnVisibleChanged. now visible: " + visible);

        if (visible)
        {
            this.MainMenuRoot.gameObject.SetActive(true);
            this.MainMenuRoot.GroupToFront();
            this.CurrentState = GameState.MainMenu;
            this.board.DestroyAllTiles();
        }
        else
        {
            MainMenuRoot.GroupToSide();
        }
    }

    IEnumerator Send()
    {
        while (true)
        {
            this.GameClientInstance.SaveBoardToProperties();
            yield return new WaitForSeconds(0.2f);
        }
    }


    public float serviceInterval = 0.1f;
    public float timeSinceService;
    public void Update()
    {

        if (Input.GetKeyUp(KeyCode.P))
        {
            this.GameClientInstance.loadBalancingPeer.DisconnectTimeout = 500;

            this.GameClientInstance.loadBalancingPeer.IsSimulationEnabled = true;
            this.GameClientInstance.loadBalancingPeer.NetworkSimulationSettings.IncomingLossPercentage = 10;
            this.GameClientInstance.loadBalancingPeer.NetworkSimulationSettings.OutgoingLossPercentage = 10;

            this.GameClientInstance.loadBalancingPeer.DebugOut = DebugLevel.WARNING;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            StartCoroutine("Send");
        }

        
        timeSinceService += Time.deltaTime;
        if (timeSinceService > serviceInterval)
        {
            this.GameClientInstance.Service();
            timeSinceService = 0;
        }
        

        if (this.CurrentState == GameState.SwitchToGame)
        {
            if (!this.MainMenuRoot.InStateTransition && this.GameClientInstance.GameIsLoaded)
            {
                this.CurrentState = GameState.InGame;
                this.MainMenuRoot.gameObject.SetActive(false);
                board.Visible = true;
            }
        }

        if (this.CurrentState == GameState.InGame)
        {
            this.UpdateInGameTexts();
        }
    }
    
    private void UpdateInGameTexts()
    {
        if (this.GameClientInstance == null || this.GameClientInstance.State != ClientState.Joined)
        {
            return;
        }

        if (this.GameClientInstance.CurrentRoom != null)
        {
            GUIText pointsGuiText = this.InGameScoresText.guiText;
            
            Player other = this.GameClientInstance.Opponent;
            if (other == null)
            {
                pointsGuiText.text = string.Format("");
            }
            else
            {
                pointsGuiText.text = string.Format("you: {0} {2}: {1}", this.GameClientInstance.MyPoints, this.GameClientInstance.OthersPoints, other.NickName);
            }
        }


        string theText = null;
        GameObject txt = this.InGameInfoText;

        if (this.GameClientInstance.GameWasAbandoned)
        {
            theText = "Your opponent left for good.";
        }
        else if (!this.GameClientInstance.GameCanStart)
        {
            theText = "Please wait for opponent.";
        }
        else if (board.IsBoardEmpty)
        {
            if (this.GameClientInstance.IsMyScoreHigher)
            {
                theText = "Game completed. You won!!";
            }
            else if (this.GameClientInstance.IsScoreTheSame)
            {
                theText = "Game completed. It's a draw!";
            }
            else
            {
                theText = "Game completed. You lost.";
            }
        }
        else if (this.GameClientInstance.IsMyTurn)
        {
            if (!board.IsOneTileFlipped())
                theText = "Your Turn. Pick 1st Tile.";

            if (board.AreTwoTilesFlipped())
            {
                if (board.AreTheSameTilesFlipped())
                    theText = "Yay! Matching Tiles!";
                else theText = "Sorry. No Match. End of Turn.";
            }
            else if (board.IsOneTileFlipped())
                theText = "Your Turn. Pick 2nd Tile.";
        }
        else theText = "Other's Turn. Please wait.";

        if (string.IsNullOrEmpty(theText))
        {
            txt.guiText.enabled = false;
        }
        else
        {
            txt.guiText.enabled = true;
            txt.guiText.text = theText;
        }
    }

    private void GetRoomsList()
    {
        this.savegameListStartIndex = 0;
        this.GameClientInstance.OpWebRpc("GetGameList", null);
        //this.GameClientInstance.OpWebRpc("GetGameList", new Dictionary<string, object>());
    }

    public void GameListUpdate()
    {
        Debug.Log(string.Format("GameListUpdate() Saved Games: {0}", this.GameClientInstance.SavedGames.Count));
        for (int index = 0; index < this.LoadGameButtons.Length; index++)
        {
            GameObject loadGameButton = this.LoadGameButtons[index];
            loadGameButton.SetActive(false);
        }

        if (this.GameClientInstance.SavedGames == null || this.GameClientInstance.SavedGames.Count == 0)
        {
            this.MainMenuInfo.Front.Text = string.Format("{0}\nno saves", this.GameClientInstance.NickName);
            this.MainMenuInfo.ToFront();
            return;
        }

        this.MainMenuInfo.Front.Text = string.Format("{0}\n{1} saves", this.GameClientInstance.NickName, this.GameClientInstance.SavedGames.Count);
        this.MainMenuInfo.ToFront();
        
        int saveGameCount = this.GameClientInstance.SavedGames.Count;

        int buttonNr = 0;// apply to button 0 and up
        int lastSaveGameBtn = this.LoadGameButtons.Length - 1;
        bool moreSavesThanButtons = (saveGameCount > this.LoadGameButtons.Length);

        for (int saveGameIndex = savegameListStartIndex; saveGameIndex < saveGameCount; saveGameIndex++)
        {
            if (buttonNr == lastSaveGameBtn && moreSavesThanButtons)
            {
                break;
            }

            SaveGameInfo saveGame = this.GameClientInstance.SavedGames[saveGameIndex];  // save to access this by index, as the for-loop only goes up to < saveGameCount
            this.ReapplyButton(this.LoadGameButtons[buttonNr++], saveGame.DisplayName + ((saveGame.MyTurn)?"\nyour turn":"\nwaiting"), "LoadGameMsg", new object[] { saveGame.RoomName, saveGame.MyPlayerId });
        }

        if (moreSavesThanButtons)
        {
            this.ReapplyButton(this.LoadGameButtons[lastSaveGameBtn], ">>", "PageSaveGameListMsg", null);
        }
    }

    public void PageSaveGameListMsg()
    {
        Debug.Log("PageSaveGameListMsg");
        this.savegameListStartIndex += this.LoadGameButtons.Length - 1;
        if (savegameListStartIndex > this.GameClientInstance.SavedGames.Count)
        {
            this.savegameListStartIndex = 0;
        }

        this.GameListUpdate();
    }

    private void DisableButtons()
    {
        for (int i = 0; i < LoadGameButtons.Length; i++)
        {
            GameObject button = LoadGameButtons[i];
            button.SetActive(false); // deactivate the individual buttons, so they can be enabled individually (not with their root)
        }
    }

    public GameObject ReapplyButton(GameObject button, string txt, string msgToCall, object parameter)
    {
        button.SetActive(true);
        TileMono tileMono = button.GetComponent<TileMono>();

        tileMono.Front = new TileContent() { Text = txt, CallMessage =  msgToCall, CallParameter = parameter};
        tileMono.ToFront();

        return button;
    }

    #region Messages called by SendMessage

    /// <summary>Switches to NamePickerGui and disables main menu.</summary>
    /// <remarks>Called via SendMessage in OnClickCall. Most likely hard to find where it's called from.</remarks>
    public void ToNameInputMsg()
    {
        if (this.CurrentState == GameState.Login)
        {
            // we could react with something like a rotation of the tile or so. right now, just skip doing anything if we're not logged in
            return;
        }

        this.Visible = false;
        this.CurrentState = GameState.Login;
        this.GameClientInstance.Disconnect();
        
        this.GetComponent<NamepickerGui>().enabled = true;
    }

    /// <summary>Leaves the game to come back later. You become inactive in the room.</summary>
    /// <remarks>Called via SendMessage in OnClickCall. Most likely hard to find where it's called from.</remarks>
    public void LeaveGameMsg()
    {
        this.LeaveGameMsg(false);
    }

    /// <summary>Abandons a game/room. Your player is no longer in the player list if that room.</summary>
    /// <remarks>Called via SendMessage in OnClickCall. Most likely hard to find where it's called from.</remarks>
    public void AbandonGameMsg()
    {
        this.LeaveGameMsg(true);
    }

    /// <summary>Called only indirectly by LeaveGameMsg and AbandonGameMsg to actually make the Photon call needed.</summary>
    /// <remarks>Only if the application is setup as "IsPersistent" via WebHooks in the Dashboard, the app can have inactive players.</remarks>
    public void LeaveGameMsg(bool doAbandon)
    {
        this.InGameInfoText.guiText.enabled = false;
        
        this.GameClientInstance.OpLeaveRoom(!doAbandon);    // creates the actual call to leave/abandon the room

        this.CurrentState = GameState.SwitchToMenu;
        this.board.Visible = false;
    }

    /// <summary>Actually tries to join a random game before creating one if needed.</summary>
    /// <remarks>
    /// First doing a OpJoinRandomRoom before creating a new game implements a really simple matchmaking!
    /// See:
    /// http://doc.exitgames.com/en/realtime/current/reference/matchmaking-and-lobby
    /// 
    /// Called via SendMessage in OnClickCall. Most likely hard to find where it's called from.
    /// </remarks>
    public void NewGameMsg()
    {
        this.Visible = false;
        this.board.Visible = false;
        this.CurrentState = GameState.SwitchToGame;
        this.GameClientInstance.OpJoinRandomRoom(null, 0);
    }

    /// <summary>Attempts to re-join an existing game. The server will try to load this, which could fail, of course.</summary>
    /// <remarks>
    /// To check this operation's success and react to errors, implement OnOperationResponse.
    /// See MemoryGameClient.OnOperationResponse how this is demo handles the error.
    /// 
    /// Called via SendMessage in OnClickCall. Most likely hard to find where it's called from.
    /// </remarks>
    public void LoadGameMsg(object[] parameters)
    {
        string name = parameters[0] as string;
        int actorNumber = (int)parameters[1];

        Debug.Log(string.Format("LoadGameMsg: {0} #{1}", name, actorNumber));
        this.Visible = false;
        this.board.Visible = false;
        this.CurrentState = GameState.SwitchToGame;
        this.GameClientInstance.OpJoinRoom(name, actorNumber);
    }

    #endregion

    public void OnGUI()
    {
//        GUILayout.Label("this.GameClientInstance.loadBalancingPeer.DebugOut: " + this.GameClientInstance.loadBalancingPeer.DebugOut);

        // workaround for a known unity issue:
        // http://issuetracker.unity3d.com/issues/dest-dot-m-multiframeguistate-dot-m-namedkeycontrollist-when-pressing-any-key
    }
}