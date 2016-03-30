// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2015
// </copyright>
// <summary>
// A simple UI for this demo.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading;
using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;


public class DemoGUI : MonoBehaviour
{
    public string AppId;                // edited in Inspector!
    public DemoGame GameInstance;


    public DemoGUI()
    {

    }

    void Start()
    {
        if (string.IsNullOrEmpty(this.AppId))
        {
            Debug.LogError("AppId not set! Edit GameObject 'Scripts', 'Demo GUI', AppId first.");
            Debug.Break();
        }

        Application.runInBackground = true;
        CustomTypes.Register();

        this.GameInstance = new DemoGame();
        this.GameInstance.AppId = this.AppId;   // edited in Inspector!
        this.GameInstance.AppVersion = "1.0";
        this.GameInstance.NickName = "unityPlayer";
        this.GameInstance.ConnectToRegionMaster("eu");
    }

    private Thread thread;
    private LoadBalancingClient threadClient;
    private object threadClientLock = new object();

    public void OnApplicationPause(bool willPause)
    {
        if (willPause)
        {
            lock (threadClientLock)
            {
                threadClient = this.GameInstance;
            }
            thread = new Thread(this.SendAcks);
            thread.IsBackground = true;
            thread.Start();
        }
        else
        {
            lock (threadClientLock)
            {
                threadClient = null;
            }
        }

        if (this.GameInstance == null || this.GameInstance.loadBalancingPeer == null)
        {
            return;
        }

        LoadBalancingPeer lbPeer = this.GameInstance.loadBalancingPeer;
        Debug.Log("OnApplicationPause " + (willPause ? "SLEEP":"WAKE") + " time: "+ DateTime.Now.ToLongTimeString() + " " +lbPeer.BytesIn +"in "+ lbPeer.BytesOut + "out");


        if (!willPause)
        {
            lbPeer.Service();
            int timeSinceLastReceive = Environment.TickCount - lbPeer.TimestampOfLastSocketReceive;
            Debug.Log("IsConnectedAndReady: " + this.GameInstance.IsConnectedAndReady + " timeSinceLastReceive: " + timeSinceLastReceive);
        }
    }

    private void SendAcks()
    {
        while (threadClient != null)
        {
            lock (threadClientLock)
            {
                if (threadClient != null)
                {
                    threadClient.loadBalancingPeer.SendAcksOnly();
                }
                else
                {
                    return;
                }
            }
            Thread.Sleep(100);
        }
    }

    void OnApplicationQuit()
    {
        this.GameInstance.Disconnect();     // let's try to do a regular disconnect on app quit

        LoadBalancingPeer lbPeer = this.GameInstance.loadBalancingPeer;
        lbPeer.StopThread();   // for the Editor it's better stop any connection immediately
    }

    void Update()
    {
        this.GameInstance.Service();

        // "back" button of phone will quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void OnGUI()
    {
        GUILayout.Label("State: " + GameInstance.State.ToString());

        if (!string.IsNullOrEmpty(this.GameInstance.ErrorMessageToShow))
        {
            GUILayout.Label(this.GameInstance.ErrorMessageToShow);
        }

        switch (GameInstance.State)
        {
            case ClientState.JoinedLobby:
                this.OnGUILobby();
                break;
            case ClientState.Joined:
                this.OnGUIJoined();
                break;
        }
    }

    private void OnGUILobby()
    {
        GUILayout.Label("Lobby Screen");
        GUILayout.Label(string.Format("Players in rooms: {0} looking for rooms: {1}  rooms: {2}", this.GameInstance.PlayersInRoomsCount, this.GameInstance.PlayersOnMasterCount, this.GameInstance.RoomsCount));

        if (GUILayout.Button("Create", GUILayout.Width(150)))
        {
            this.GameInstance.OpJoinRandomRoom(null, 0);
        }

        GUILayout.Label("Rooms to choose from: " + this.GameInstance.RoomInfoList.Count);
        foreach (RoomInfo roomInfo in this.GameInstance.RoomInfoList.Values)
        {
            if (GUILayout.Button(roomInfo.Name))
            {
                this.GameInstance.OpJoinRoom(roomInfo.Name);
            }
        }
    }

    private void OnGUIJoined()
    {
        // we are in a room, so we can access CurrentRoom and it's Players

        GUILayout.Label("Room Screen. Players: " + this.GameInstance.CurrentRoom.Players.Count);
        GUILayout.Label("Room: " + this.GameInstance.CurrentRoom + " Server: " + this.GameInstance.CurrentServerAddress);
        GUILayout.Label("last move: " + this.GameInstance.lastMoveEv + " ev count: " + this.GameInstance.evCount);

        foreach (Player player in this.GameInstance.CurrentRoom.Players.Values)
        {
            GUILayout.Label("Player: " + player);
        }

        if (GUILayout.Button("Send Move Event", GUILayout.Width(150)))
        {
            this.GameInstance.SendMove();
        }

        if (GUILayout.Button("Rename Self", GUILayout.Width(150)))
        {
            this.GameInstance.LocalPlayer.NickName = string.Format("unityPlayer{0:00}", "unityPlayer" + Random.Range(0, 100));
        }

        // creates a random property of this room with a random value to set
        if (GUILayout.Button("Set Room Property", GUILayout.Width(150)))
        {
            Hashtable randomProps = new Hashtable();
            randomProps[Random.Range(0, 2).ToString()] = Random.Range(0, 2).ToString();
            this.GameInstance.CurrentRoom.SetCustomProperties(randomProps);
        }

        // creates a random property of this player with a random value to set
        if (GUILayout.Button("Set Player Property", GUILayout.Width(150)))
        {
            Hashtable randomProps = new Hashtable();
            randomProps[Random.Range(0, 2).ToString()] = Random.Range(0, 2).ToString();
            this.GameInstance.LocalPlayer.SetCustomProperties(randomProps);
        }

        if (GUILayout.Button("Leave", GUILayout.Width(150)))
        {
            this.GameInstance.OpLeaveRoom();
        }
    }
}
