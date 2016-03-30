using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;


/// <summary>
/// This class is an add-on to the CubeBoard, sending clicks as cached-event and showing the click path.
/// </summary>
public class CubeClicks : MonoBehaviour
{
    private CubeBoard board;
    private DemoGame gameInstance;
    private GameObject clickedGo;
    public GameObject highlighterGo;
    public float zPos;
    public Vector3 defaultScale = Vector3.zero;

    public Rect rightToolBar;

	// Use this for initialization
	void Start()
    {
        rightToolBar = new Rect(Screen.width - rightToolBar.width - rightToolBar.x, rightToolBar.y, rightToolBar.width, Screen.height - rightToolBar.y);
        board = GameObject.FindObjectOfType(typeof(CubeBoard)) as CubeBoard;
        Debug.Log(board);
	}
    
    public void OnGUI()
    {
        GUI.skin.button.stretchWidth = true;
        GUI.skin.button.fixedWidth = 0;

        if (gameInstance == null)
        {
            DemoGUI gui = GameObject.FindObjectOfType(typeof(DemoGUI)) as DemoGUI;
            gameInstance = gui.GameInstance;
        }
        if (gameInstance == null || this.gameInstance.State != ClientState.Joined)
        {
            return;
        }

        GUILayout.BeginArea(rightToolBar);
        GUILayout.Label("Clicks of");
        int turnToShow = this.gameInstance.turnNumber;
        GUILayout.Label("turn " + turnToShow);
        string allTiles = "";

        if (turnToShow > 0 && turnToShow < this.gameInstance.lastTilesClicked.Count && this.gameInstance.lastTilesClicked[turnToShow] != null)
            foreach (int i in this.gameInstance.lastTilesClicked[turnToShow])
            {
                allTiles += i + ", ";
            }
        GUILayout.Label(allTiles);
        if (GUILayout.Button("Clear " + turnToShow))
        {
            this.gameInstance.ClearTileClickEvForTurn(turnToShow);
        }

        turnToShow = turnToShow - 1;
        GUILayout.Label("turn " + turnToShow);
        allTiles = "";

        if (turnToShow > 0 && turnToShow < this.gameInstance.lastTilesClicked.Count && this.gameInstance.lastTilesClicked[turnToShow] != null)
            foreach (int i in this.gameInstance.lastTilesClicked[turnToShow])
            {
                allTiles += i + ", ";
            }
        GUILayout.Label(allTiles);
        if (GUILayout.Button("Clear " + turnToShow))
        {
            this.gameInstance.ClearTileClickEvForTurn(turnToShow);
        }
        GUILayout.EndArea();
    }

    public void OnClick()
    {
        if (InputToEvent.goPointedAt != null && this.gameInstance != null)
        {
            int index = board.GetCubeTileIndex(InputToEvent.goPointedAt);
            this.gameInstance.SendTileClickEv(index);
        }
    }
}
