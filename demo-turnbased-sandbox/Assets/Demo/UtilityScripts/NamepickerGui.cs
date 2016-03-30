using UnityEngine;


public class NamepickerGui : MonoBehaviour
{
    public Vector2 GuiSize = new Vector2(200, 300);
    public static string NickName = string.Empty;

    private Rect guiCenteredRect;
    public MonoBehaviour componentToEnable;
    public string helpText = "Welcome to this Photon demo.\nEnter a nickname to start. This demo does not require users to authenticate.";
    private const string UserNamePlayerPref = "NamePickUserName";


    public void Awake()
    {
        this.guiCenteredRect = new Rect(Screen.width/2-GuiSize.x/2, Screen.height/2-GuiSize.y/2, GuiSize.x, GuiSize.y);

        if (this.componentToEnable == null || this.componentToEnable.enabled)
        {
            Debug.LogError("To use the NamepickerGui, the ComponentToEnable should be defined in inspector and disabled initially.");
        }

        string prefsName = PlayerPrefs.GetString(NamepickerGui.UserNamePlayerPref);
        if (!string.IsNullOrEmpty(prefsName))
        {
            NamepickerGui.NickName = prefsName;
        }
    }
    
    public void OnGUI()
    {
        // Enter-Key handling:
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
        {
            if (!string.IsNullOrEmpty(NamepickerGui.NickName))
            {
                this.StartChat();
                return;
            }
        }
        GUI.skin.label.wordWrap = true;

        GUILayout.BeginArea(guiCenteredRect);

        GUILayout.Label(this.helpText);
        
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("NameInput");
        NamepickerGui.NickName = GUILayout.TextField(NamepickerGui.NickName);
        if (GUILayout.Button("Connect", GUILayout.Width(80)))
        {
            this.StartChat();
        }
        GUI.FocusControl("NameInput");
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void StartChat()
    {
        PlayerPrefs.SetString(NamepickerGui.UserNamePlayerPref, NamepickerGui.NickName);
        this.componentToEnable.enabled = true;
        this.enabled = false;
    }
}
