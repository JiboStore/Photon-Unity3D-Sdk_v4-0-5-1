// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2012
// </copyright>
// <summary>
//  Creates and controls the behaviour of the interface elements
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon.LoadBalancing;
using ExitGames.Client.DemoParticle;
using System.Threading;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class DemoGUI : MonoBehaviour
{

    // Main menu parameters
    public static bool ShowUserInfo { get; private set; }
    public static bool ShowMainMenu { get; private set; }

    public Texture BtnAutoMoveOn;
    public Texture BtnAutoMoveOff;
    public Texture BtnClientInfoOn;
    public Texture BtnClientInfoOff;
    public Texture BtnChangeGridSize;
    public Texture BtnAddClient;
    public Texture BtnRemoveClient;
    public Texture BtnInterestGroups;
    public Texture BtnChangeColor;


    // Logic of the game
    private Logic logic;
    private GameObject playground;
    private Color startAlpha;

    private static int DefaultButtonWidth { get; set; }
    private static int DefaultButtonHeight { get; set; }
    private static int IntervalBetweenButtons { get; set; }

    private static bool AutomoveEnabled { get; set; }
    private static bool MultipleRoomsEnabled { get; set; }
    private static bool IsGameStarted { get; set; }


    // Scale of playground grid
    public static int playgroundScale { get; private set; }
    private float scaleRatio;

    private float inputRepeatTimeout;
    private const float inputRepeatTimeoutSetting = 0.2f;   // every how often do we apply input to movement?


    void Start()
    {
        playground = GameObject.Find("Playground");
        startAlpha = playground.renderer.materials[1].color;
    }


    // Initialization code
    public void Init(string serverAddress, string appId, string gameVersion) {

        ShowMainMenu = true;
        ShowUserInfo = true;

        AutomoveEnabled = true;

        #if UNITY_ANDROID
            DefaultButtonWidth = 100;
            DefaultButtonHeight = 60;
        #else
            DefaultButtonWidth = 45;
            DefaultButtonHeight = 45;
        #endif

        IntervalBetweenButtons = 10;

        // CallConnect the logic of the game
        logic = new Logic();
        logic.StartGame(serverAddress, appId, gameVersion);

        DemoGUI.playgroundScale = (int) GameObject.Find("Playground").transform.localScale.x;
	}


    /// <summary>As precaution in the Unity Editor, you should always do a proper disconnect. Else, the Editor might become unresponsive.</summary>
    public void OnApplicationQuit()
    {
        if (this.logic != null)
        {
            this.logic.localPlayer.Disconnect();
            this.logic.DisconnectAllClients();
        }
    }


    // Update is called once per frame
    void Update()
    {

        // Update logic and render players
        if (logic != null)
        {
            logic.UpdateLocal();

            if (logic.LocalPlayerJoined())
            {
                RenderPlayers();
                InputForControlledCube();
            }
        }

        // Show or hide main menu when 'Tab' is pressed
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            ShowMainMenu = !ShowMainMenu;
        }

        // Exit the application when 'Back' button is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
	}


    // must be connected and in a room (because this uses logic.localPlayer.LocalPlayer).
    private void InputForControlledCube()
    {

        if (inputRepeatTimeout > 0)
        {
            inputRepeatTimeout -= Time.deltaTime;
            return;
        }

        float x = Input.GetAxis("Horizontal");
        if (x < -0.2f)
        {
            this.logic.localPlayer.LocalPlayer.PosX -= 1;
            inputRepeatTimeout += inputRepeatTimeoutSetting;
        }
        else if (x > 0.2f)
        {
            this.logic.localPlayer.LocalPlayer.PosX += 1;
            inputRepeatTimeout += inputRepeatTimeoutSetting;
        }

        float y = Input.GetAxis("Vertical");
        if (y < -0.2f)
        {
            this.logic.localPlayer.LocalPlayer.PosY -= 1;
            inputRepeatTimeout += inputRepeatTimeoutSetting;
        }
        else if (y > 0.2f)
        {
            this.logic.localPlayer.LocalPlayer.PosY += 1;
            inputRepeatTimeout += inputRepeatTimeoutSetting;
        }

        this.logic.localPlayer.LocalPlayer.ClampPosition();
    }


    // Draw the GUI
    void OnGUI()
    {
        if (logic != null && logic.LocalPlayerJoined())
        {
            if (ShowMainMenu)
            {
                #region menu buttons
                int i = 0;
                int buttonSpace = IntervalBetweenButtons + DefaultButtonWidth;
                // Automove button
                if (AutomoveEnabled)
                {
                    if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnAutoMoveOff))
                    {
                        AutomoveEnabled = !AutomoveEnabled;
                        this.logic.localPlayer.MoveInterval.IsEnabled = !this.logic.localPlayer.MoveInterval.IsEnabled;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnAutoMoveOn))
                    {
                        AutomoveEnabled = !AutomoveEnabled;
                        this.logic.localPlayer.MoveInterval.IsEnabled = this.logic.localPlayer.MoveInterval.IsEnabled;
                    }
                }

                // Interest management button
                if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnInterestGroups))
                {
                    logic.SwitchUseInterestGroups();
                    SetPlaygroundTexture();
                }

                // Change color button
                if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnChangeColor))
                {
                    this.logic.localPlayer.ChangeLocalPlayercolor();
                }

                // Show user information button
                if (ShowUserInfo)
                {
                    if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnClientInfoOff))
                    {
                        ShowUserInfo = !ShowUserInfo;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnClientInfoOn))
                    {
                        ShowUserInfo = !ShowUserInfo;
                    }
                }

                // Add client button
                if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnAddClient))
                {
                    logic.AddClient();
                }

                // Remove client button
                if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnRemoveClient))
                {
                    logic.RemoveClient();
                }

                // Change grid size button
                if (GUI.Button(new Rect(buttonSpace * (i++) + IntervalBetweenButtons, 10, DefaultButtonWidth, DefaultButtonHeight), BtnChangeGridSize))
                {
                    logic.ChangeGridSize();
                    SetPlaygroundTexture();
                }
                #endregion
            }
        }
        else
        {
            if (logic != null)
            {
                GUI.Label(new Rect(10, 10, 300, 50), this.logic.localPlayer.State.ToString());
            }
        }

        if (DemoGUI.ShowUserInfo && logic.LocalPlayerJoined())
        {
            // Get 2D coordinates from 3D coordinates of the client

            scaleRatio = DemoGUI.playgroundScale / this.logic.localPlayer.GridSize;
            Vector3 localScale = new Vector3(scaleRatio, scaleRatio, scaleRatio);

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;

            foreach (ParticlePlayer p in logic.localPlayer.LocalRoom.Players.Values)
            {
                Vector3 posVector = Camera.main.WorldToScreenPoint(new Vector3(p.PosX * localScale.x + localScale.x / 2, scaleRatio / 2, p.PosY * localScale.y + localScale.y / 2));

                if (p.IsLocal)
                {
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.textColor = Color.black;
                }
                else
                {
                    labelStyle.fontStyle = FontStyle.Normal;
                    labelStyle.normal.textColor = Color.white;
                }
                // Output the client's name
                GUI.Label(new Rect(posVector.x, Screen.height - posVector.y, 100, 20), p.NickName, labelStyle);
            }
        }

    }



    // Set the playground texture
    // Current texture depends on interest management setting
    public void SetPlaygroundTexture()
    {
        float textureScale;

        if (logic.localPlayer.UseInterestGroups)
        {
            playground.renderer.materials[1].color = startAlpha;
        }
        else
        {
            playground.renderer.materials[1].color = new Color(0,0,0,0);
        }

        textureScale = this.logic.localPlayer.GridSize;
        playground.renderer.material.mainTextureScale = new Vector2(textureScale, textureScale);
    }

    /// <summary>
    /// Convert integer value to Color
    /// </summary>
    public static Color IntToColor(int colorValue)
    {
        float r = (byte)(colorValue >> 16) / 255.0f;
        float g = (byte)(colorValue >> 8) / 255.0f;
        float b = (byte)colorValue / 255.0f;

        return new Color(r, g, b);
    }

    /// <summary>
    /// Render cubes onto the scene
    /// </summary>
    void RenderPlayers()
    {
        float newscale = DemoGUI.playgroundScale / this.logic.localPlayer.GridSize;

        if (newscale != scaleRatio)
        {
            scaleRatio = newscale;
            SetPlaygroundTexture();
        }
        Vector3 localScale = new Vector3(scaleRatio, scaleRatio, scaleRatio);

        lock (logic.localPlayer)
        {
            foreach (ParticlePlayer p in logic.localPlayer.LocalRoom.Players.Values)
            {
                foreach (GameObject cube in logic.cubes)
                {
                    if (cube.name == p.NickName)
                    {
                        float alpha = 1.0f;
                        if (!p.IsLocal && p.UpdateAge > 500)
                        {
                            cube.renderer.material.shader = Shader.Find("Transparent/Diffuse");
                            alpha = (p.UpdateAge > 1000) ? 0.3f : 0.8f;
                        }
                        cube.transform.localScale = localScale;

                        Color cubeColor = DemoGUI.IntToColor(p.Color);
                        cube.renderer.material.color = new Color(cubeColor.r, cubeColor.g, cubeColor.b, alpha);
                        cube.transform.position = new Vector3(p.PosX * localScale.x + localScale.x / 2, scaleRatio / 2, p.PosY * localScale.y + localScale.y / 2);
                        break;
                    }
                }
            }
        }
    }
}