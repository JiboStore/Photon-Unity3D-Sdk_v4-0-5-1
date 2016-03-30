// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2012
// </copyright>
// <summary>
// This script must be added to game objects that describe clients
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using ExitGames.Client.DemoParticle;
using System.Collections;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Logic 
{

    public GameLogic localPlayer { get; private set; }


    // Connection parameters
    public static string ServerAddress { get; set; }
    public static string AppId { get; set; }
    public static string GameVersion { get; set; }

    // Dictionaries for storing references to background games and remote players
    public static Dictionary<string, GameLogic> backgroundGames;
    public static Dictionary<string, ParticlePlayer> remotePlayers;

    // Cube GameObjects that represent players
    public List<GameObject> cubes;

    /// <summary>
    /// CallConnect the game using given connection parameters
    /// </summary>
    /// <param name="serverAddress">Server address</param>
    /// <param name="appId">Application ID</param>
    /// <param name="gameVersion">Game version</param>
    public void StartGame(string serverAddress, string appId, string gameVersion)
    {
        ServerAddress = serverAddress;
        AppId = appId;
        GameVersion = gameVersion;

        backgroundGames = new Dictionary<string, GameLogic>();
        remotePlayers = new Dictionary<string, ParticlePlayer>();
        cubes = new List<GameObject>();

        // Initialize local game
        localPlayer = new GameLogic(appId, gameVersion);
        
        localPlayer.OnEventJoin = this.OnJoinedPlayer;
        localPlayer.OnEventLeave = this.OnLeavedPlayer;
        
        localPlayer.CallConnect();
    }

    /// <summary>
    /// Handler for "Player Joined" Event
    /// </summary>
    /// <param name="particlePlayer">Player that joined the game</param>
    private void OnJoinedPlayer(ParticlePlayer particlePlayer)
    {
        if (!particlePlayer.IsLocal)
        {
            lock (remotePlayers)
            {
                if (!remotePlayers.ContainsKey(particlePlayer.NickName) && !backgroundGames.ContainsKey(particlePlayer.NickName))
                {
                    GameObject playerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    playerCube.name = particlePlayer.NickName;
                    cubes.Add(playerCube);
                    remotePlayers.Add(particlePlayer.NickName, particlePlayer);
                }
            }
        }
        else
        {
            lock (localPlayer)
            {
                foreach (ParticlePlayer p in localPlayer.LocalRoom.Players.Values)
                {
                    foreach (GameObject cube in cubes)
                    {
                        if (cube.name == p.NickName) return;
                    }
                    GameObject playerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    playerCube.name = p.NickName;
                    cubes.Add(playerCube);
                    remotePlayers.Add(p.NickName, p);
                }
            }
        }
    }

    /// <summary>
    /// Handler for "Player Leaved" Event
    /// </summary>
    /// <param name="particlePlayer">Player that leaved the game</param>
    private void OnLeavedPlayer(ParticlePlayer particlePlayer)
    {
        string name = particlePlayer.NickName;

        foreach (GameObject cube in cubes)
        {
            if (cube.name == name)
            {
                remotePlayers.Remove(name);
                cubes.Remove(cube);
                GameObject.Destroy(cube);
                return;
            }
        }
    }

    // Update is called once per frame
    public void UpdateLocal () 
    {
        if (localPlayer != null) 
        {
            localPlayer.UpdateLoop();
            Move();    
        }
    }

    // Update the position of the client
    private void Move()
    {
        if (LocalPlayerJoined())
        {
            foreach (GameLogic logic in backgroundGames.Values)
            {
                logic.UpdateLoop();
            }
        } 
    }

    /// <summary>
    /// Check if local player joined the game
    /// </summary>
    public bool LocalPlayerJoined()
    {
        if (localPlayer != null && localPlayer.State == ClientState.Joined && localPlayer.LocalRoom != null)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add background game
    /// </summary>
    public void AddClient()
    {
        GameLogic addedClient = new GameLogic(AppId, GameVersion);
        addedClient.SetUseInterestGroups(localPlayer.UseInterestGroups);
        addedClient.CallConnect();

        GameObject playerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerCube.name = addedClient.NickName;
        cubes.Add(playerCube);

        backgroundGames.Add(addedClient.NickName, addedClient);
    }

    /// <summary>
    /// Remove background game
    /// </summary>
    public void RemoveClient()
    {
        if (backgroundGames.Count > 0)
        {
            GameLogic logic = null;
            foreach (GameObject cube in cubes)
            {
                if (backgroundGames.TryGetValue(cube.name, out logic))
                {
                    logic.Disconnect();
                    cubes.Remove(cube);
                    backgroundGames.Remove(cube.name);
                    GameObject.Destroy(cube);
                    break;
                }
            }
        } 
    }

    /// <summary>Disconnects all simulated/additional clients that are hosted in this process.</summary>
    public void DisconnectAllClients()
    {
        foreach (GameLogic gl in backgroundGames.Values)
        {
            if (gl != null)
            {
                gl.Disconnect();
            }
        }
    }

    /// <summary>
    /// Turn Interest Groups "On" or "Off" for local and background players
    /// </summary>
    public void SwitchUseInterestGroups()
    {
        localPlayer.SetUseInterestGroups(!localPlayer.UseInterestGroups);

        foreach (GameLogic backgroundGame in backgroundGames.Values)
        {
            backgroundGame.SetUseInterestGroups(localPlayer.UseInterestGroups);
        }
    }

    /// <summary>
    /// Update grid size for local and background players
    /// </summary>
    public void ChangeGridSize()
    {
        localPlayer.ChangeGridSize();

        foreach (GameLogic backgroundGame in backgroundGames.Values)
        {
            backgroundGame.ChangeGridSize();
        }
    }
}