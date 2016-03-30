using UnityEngine;


/// <summary>Taken from another demo component, this is used for username input and storage.</summary>
/// <remarks>
/// This component evolved from an even simpler script. The idea is that a name saved once entered 
/// and loaded automatically into NamePicker.NickName. Any other script can access this name and
/// use it to authenticate on Photon.
/// 
/// 
/// For Photon Turnbased, the user's name plays an important role: It does not make sense to save
/// a game, unless you can identify the user who played it (maybe with the exception of a game 
/// that only runs on a single device per user). 
/// You need to know the user, to save and load the games she plays asynchronously.
/// 
/// 
/// Important classes in this demo: MemoryGui, MemoryBoard, MemoryGameClient and NamePickerGui.
/// </remarks>
public class NamepickerGui : MonoBehaviour
{
    /// <summary>This is the single most important value we want from this class/GUI: The nickname of this player for in-game.</summary>
    /// <remarks>Static to guarantee easy access from any component / class!</remarks>
    public static string NickName = string.Empty;

    public MonoBehaviour ComponentToEnable;     // set in inspector
    public string HelpText;                     // set in inspector
    public bool StartWithLoadedName;            // set in inspector

    public Vector2 WidthAndHeight = new Vector2(300, 120);  // set in inspector
    public GUISkin Skin;                                    // set in inspector
    
    private static string nickNameInPrefs = string.Empty;
    private const string UserNamePlayerPref = "NamePickUserName";
    

    public void OnEnable()
    {
        if (this.ComponentToEnable == null)
        {
            Debug.LogError("To use the NamepickerGui, the ComponentToEnable should be defined in inspector and disabled initially.");
        }

        // try to load a username from the player preferences
        nickNameInPrefs = PlayerPrefs.GetString(NamepickerGui.UserNamePlayerPref);
        if (!string.IsNullOrEmpty(nickNameInPrefs))
        {
            NamepickerGui.NickName = nickNameInPrefs;
            if (StartWithLoadedName)
            {
                EndNamePicker();
                return;
            }
        }

        GameObject title = GameObject.Find("TitleQuad");
        if (title != null)
        {
            title.GetComponent<TileMono>().ToFront();
        }
    }

    public void OnGUI()
    {
        // enter-key handling (a bit awkward in Unity gui)
        if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
        {
            if (!string.IsNullOrEmpty(NamepickerGui.NickName))
            {
                this.EndNamePicker();
                return;
            }
        }

        // apply a some skinning
        GUI.skin.label.wordWrap = true;
        if (this.Skin != null)
        {
            GUI.skin = this.Skin;
        }
        
        // find the position for the input
        float xp = Screen.width*0.5f - WidthAndHeight.x/2;
        float yp = Screen.height*0.75f - WidthAndHeight.y/2;
        #if UNITY_WP8
        if (Application.platform == RuntimePlatform.WP8Player)
        {
            if (TouchScreenKeyboard.visible)
            {
                // if the keyboard covers the lower end of the screen, show the input field in a higher place.
                int newY = (int)(Screen.height - WidthAndHeight.y - TouchScreenKeyboard.area.height);
                if (newY < yp)
                {
                    yp = newY;
                }
            }
        }
        #endif
        Rect guiCenteredRect = new Rect(xp, yp, WidthAndHeight.x, WidthAndHeight.y);

        
        // actually create the gui on screen
        GUILayout.BeginArea(guiCenteredRect);

        GUILayout.Label(this.HelpText); // text is defined in inspector

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("NameInput");
        NamepickerGui.NickName = GUILayout.TextField(NamepickerGui.NickName);
        GUI.FocusControl("NameInput");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        

        if (GUILayout.Button("login"))
        {
            EndNamePicker();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

    }

    private void EndNamePicker()
    {
        // end namepicker only if name is set (if so, save it)
        if (string.IsNullOrEmpty(NamepickerGui.NickName))
        {
            return;
        }

        bool changedName = !NamepickerGui.NickName.Equals(nickNameInPrefs);
        PlayerPrefs.SetString(NamepickerGui.UserNamePlayerPref, NamepickerGui.NickName);
        nickNameInPrefs = NamepickerGui.NickName;


        // for push notifications, we check if the name changed on this device and update our tag if needed
        if (changedName)
        {
            MemoryNotification pushMsg = GetComponent<MemoryNotification>();
            if (pushMsg != null)
            {
                pushMsg.RegisterPushTags();
            }
        }


        // we use OnEnable to enable and show some items but it's not called if the component is already enabled!
        if (this.ComponentToEnable.enabled)
        {
            Debug.Log("Component already enabled");
            this.ComponentToEnable.SendMessage("OnEnable");
        }
        else
        {
            this.ComponentToEnable.enabled = true;
        }

        this.enabled = false;
    }
}
