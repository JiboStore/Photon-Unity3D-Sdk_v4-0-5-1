using UnityEngine;
using ExitGames.Client.Photon.LoadBalancing;
using Hashtable = ExitGames.Client.Photon.Hashtable;


/// <summary>Works closely with MemoryGameClient to present the current game state visually.</summary>
/// <remarks>
/// Important classes in this demo: MemoryGui, MemoryBoard, MemoryGameClient and NamePickerGui.
/// </remarks>
public class MemoryBoard : MonoBehaviour
{
    protected internal MemoryGameClient GameClientInstance { get; set; }
    public MemoryGui MemoryGui { get; set; }


    public int TilesX = 4;      // set in inspector
    public int TilesY = 4;      // set in inspector
    public byte[] FlippedTiles = new byte[] { NotFlippedTile, NotFlippedTile };     // up to 2 tiles can be flipped

    public Texture2D[] Textures;                // set in Inspector. Graphics of Tiles (must be same count as TilesTypeCount)
    public Material TileBackMaterial;           // set in inspector

    public float EndOfTurnDelay;                // set in inspector. seconds to delay the end of a turn (hide picked tiles again)
    public GameObject TilePrefab;               // inspector. prefab to instantiate as tile
    public GameObject InGameRoot;               // inspector. root GameObject 


    protected internal Tile[] Tiles;            // tiles of a board
    private readonly Color[] colors = new Color[] { Color.blue, Color.cyan, Color.green, Color.magenta, Color.red, Color.white, Color.yellow };
    private const byte TileTypeEmpty = 0;
    private const byte NotFlippedTile = byte.MaxValue;              // value of FlippedTile[i] when not flipped

    private int TileCount { get { return TilesY * TilesX; } }
    private int TilesTypeCount { get { return TileCount / 2; } }    // tile type 0 means: no tile (anymore)
    
    protected internal bool IsBoardEmpty;       // set by UpdateIsBoardEmpty() by certain events
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


    /// <summary>Is at least one tile flipped?!</summary>
    /// <returns>True if one (or more) tiles are flipped.</returns>
    public bool IsOneTileFlipped()
    {
        return FlippedTiles[0] != NotFlippedTile;
    }

    /// <summary>Checks if two tiles are flipped (not none and not one).</summary>
    public bool AreTwoTilesFlipped()
    {
        return FlippedTiles[0] != NotFlippedTile && FlippedTiles[1] != NotFlippedTile;
    }

    /// <summary>Checks if two tiles are flipped and same.</summary>
    public bool AreTheSameTilesFlipped()
    {
        int tileA = FlippedTiles[0];
        int tileB = FlippedTiles[1];
        return AreTwoTilesFlipped() && Tiles[tileA].Type == Tiles[tileB].Type;
    }

    /// <summary>Checks if board is empty and stores value in IsBoardEmpty.</summary>
    private bool UpdateIsBoardEmpty()
    {
        // Debug.Log("Tiles: " + SupportClass.ByteArrayToString(Tiles));

        this.IsBoardEmpty = false;
        for (int i = 0; i < Tiles.Length; i++)
        {
            Tile tile = Tiles[i];
            if (tile.Type != TileTypeEmpty)
            {
                return false;
            }
        }

        Debug.LogWarning("Board is empty");
        this.IsBoardEmpty = true;
        return true;
    }


    /// <summary>To be called by "utility" component InputToEvent, which must be on the camera to work.</summary>
    public void OnClick()
    {
        if (this.GameClientInstance == null)
        {
            Debug.Log("No Clicking! GameInstance is null.");
            return;
        }
        if (this.GameClientInstance.Server != LoadBalancingClient.ServerConnection.GameServer || InputToEvent.goPointedAt == null)
        {
            return;
        }


        if (!this.GameClientInstance.GameCanStart)
        {
            Debug.Log("Sorry. Wait for Another Player.");
            return;
        }

        if (this.GameClientInstance != null && !this.GameClientInstance.IsMyTurn)
        {
            Debug.Log("Sorry. Not Your Turn.");
            return;
        }

        if (this.GameClientInstance != null && AreTwoTilesFlipped())
        {
            Debug.Log("TODO: show a note to wait a moment"); // TODO: show a note to wait a moment
            return;
        }
        

        int index = GetCubeTileIndex(InputToEvent.goPointedAt);
        Tiles[index].TileMono.ToFront();

        if (IsOneTileFlipped())
        {
            // check if 2nd tile not the same as 1st
            if (FlippedTiles[0] == (byte)index)
            {
                Debug.Log("Can't click same twice.");
                return;
            }

            // show second tile flip
            FlippedTiles[1] = (byte)index;

            if (this.AreTheSameTilesFlipped())
            {
                if (this.GameClientInstance != null) this.GameClientInstance.MyPoints++;
            }
            else
            {
                // not the same tiles means it's the next player's turn.
                // we handle this case in SaveBoardToProperties()!
            }
            this.EndTurnDelayed();
        }
        else
        {
            FlippedTiles[0] = (byte) index;
        }

        if (this.GameClientInstance != null)
        {
            this.GameClientInstance.SaveBoardToProperties();
        }
    }

    
    void OnVisibleChanged()
    {
        Debug.Log(string.Format("MemoryBoard.OnVisibleChanged. now visible: {0}", visible));
        if (this.Tiles != null)
        {
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tile tile = Tiles[i];
                if (tile.Go == null || tile.TileMono == null)
                {
                    continue;
                }
                TileMono tm = tile.TileMono;
                if (this.visible) tm.ToBack();
                else tm.ToSide();
            }

            if (this.visible)
            {
                this.ShowFlippedTiles();
            }

            this.MemoryGui.InGameTextRoot.SetActive(this.visible);
        }
    }

    public void ShowFlippedTiles()
    {
        for (int i = 0; i < FlippedTiles.Length; i++)
        {
            byte tileId = FlippedTiles[i];
            if (tileId != NotFlippedTile && tileId < Tiles.Length)
            {
                Tile flippedTile = Tiles[tileId];
                if (flippedTile.TileMono.CurrentSide != TileMono.Side.Front)
                {
                    flippedTile.TileMono.ToFront();
                }
            }
        }

        if (AreTwoTilesFlipped())
        {
            EndTurnDelayed();
        }
    }

    public void InitializeBoard()
    {
        ResetFlippedTiles();
        DestroyAllTiles();

        this.Tiles = new Tile[TileCount];
        int y = 0;
        int x = 0;
        float xOffset = (TilesX / 2.0f) - 0.5f;
        float yOffset = (TilesY / 2.0f) - 0.5f;

        for (int i = 0; i < TileCount; i++)
        {
            this.Tiles[i] = new Tile() { Id = i };

            GameObject cube = Instantiate(TilePrefab) as GameObject;
            cube.name = string.Format("Tile.{0:00}", i);
            cube.transform.parent = this.InGameRoot.transform;
            cube.transform.localPosition = new Vector3(x - xOffset, y - yOffset, 0);
            cube.transform.localScale = cube.transform.localScale * 0.95f;

            this.Tiles[i].Go = cube;
            TileMono tm = cube.GetComponent<TileMono>();
            this.Tiles[i].TileMono = tm;

            x++;
            if (x >= TilesX)
            {
                x = 0;
                y++;
            }
        }
    }

    public void RandomBoard()
    {
        Debug.Log("Randomizing board");
        for (int i = 0; i < Tiles.Length; i++)
        {
            Tile tile = Tiles[i];
            tile.Type = (byte)((i%TilesTypeCount) + 1);
        }

        // randomize the tiles
        for (int i = 0; i < TileCount * 10; i++)
        {
            int tileA = Random.Range(0, TileCount);
            int tileB = Random.Range(0, TileCount);

            byte valueA = this.Tiles[tileA].Type;
            this.Tiles[tileA].Type = this.Tiles[tileB].Type;
            this.Tiles[tileB].Type = valueA;
        }
    }

    public void DestroyAllTiles()
    {
        if (Tiles == null)
        {
            return;
        }

        for (int i = 0; i < Tiles.Length; i++)
        {
            Tile tile = Tiles[i];
            Destroy(tile.Go); // destroy the GO
            tile.Go = null;
            tile.Type = TileTypeEmpty;
        }

        this.Tiles = null;
    }

    private void RemoveTileAndQuad(byte tileToRemove)
    {
        Tile t = Tiles[tileToRemove];
        Destroy(t.Go);    // destroy the GO
        t.Go = null;
        t.Type = TileTypeEmpty;
    }


    protected internal void MarkFlippedTilesToRemove()
    {
        if (Tiles != null)
        {
            for (int i = 0; i < FlippedTiles.Length; i++)
            {
                byte tileId = FlippedTiles[i];
                if (tileId == NotFlippedTile) continue;
                Tiles[tileId].Type = TileTypeEmpty;
                Tiles[tileId].TileMono.Visible = false;
            }
        }
    }

    protected internal void ResetFlippedTiles()
    {
        if (Tiles != null)
        {
            for (int i = 0; i < FlippedTiles.Length; i++)
            {
                byte tileId = FlippedTiles[i];
                if (tileId == NotFlippedTile)
                {
                    continue;
                }

                Tile t = Tiles[tileId];
                if (t.TileMono.Visible && t.TileMono.TargetSide != TileMono.Side.Back)
                {
                    t.TileMono.ToBack();
                }
            }
        }

        FlippedTiles[0] = NotFlippedTile;
        FlippedTiles[1] = NotFlippedTile;
    }

    private string FlippedTilesToString()
    {
        return string.Format("{0}, {1}", FlippedTiles[0], FlippedTiles[1]);
    }

    public void EndTurnDelayed()
    {
        Invoke("EndTurn", this.EndOfTurnDelay); // after a while, end turn
    }

    public void EndTurn()
    {
        Debug.Log(string.Format("EndTurn. Flipped Tiles: {0}", this.FlippedTilesToString()));

        if (this.GameClientInstance.CurrentRoom == null || this.Tiles == null)
        {
            Debug.Log("Left room while waiting for end of turn. Skip end of turn.");
            return;
        }

        if (!AreTwoTilesFlipped())
        {
            Debug.LogError("End of turn triggered but not 2 tiles flipped?!");
            return;
        }

        this.GameClientInstance.TurnNumber += 1;
        if (AreTheSameTilesFlipped())
        {
            Debug.Log("Same Tiles Flipped! Yay!");
            
            // take tiles from board (as they are the same)
            MarkFlippedTilesToRemove();

            this.UpdateIsBoardEmpty();
        }
        else
        {
            Debug.Log("Nope. Those are not the same. Handing over turn. Ending turn for player: " + this.GameClientInstance.PlayerIdToMakeThisTurn);
            this.GameClientInstance.HandoverTurnToNextPlayer();

            if (this.GameClientInstance.PlayerIdToMakeThisTurn > 0)
            {
                Player opponent = this.GameClientInstance.CurrentRoom.Players[GameClientInstance.PlayerIdToMakeThisTurn];
                if (opponent.IsInactive)
                {
                    // TODO: show some hint that the other is not active and wait time will be longer!
                }
            }
        }

        ResetFlippedTiles();
        //UpdateVisuals();
    }

    public int GetCubeTileIndex(GameObject cube)
    {
        for (int i = 0; i < Tiles.Length; i++)
        {
            Tile tile = Tiles[i];
            if (cube.Equals(tile.Go))
            {
                return tile.Id;
            }
        }

        Debug.LogError("Could not find Cube in Tiles.");
        return -1;
    }

    protected internal Hashtable GetBoardAsCustomProperties()
    {
        Hashtable customProps = new Hashtable();
        for (int i = 0; i < TileCount; i++)
        {
            customProps[i.ToString()] = Tiles[i].Type;
        }

        if (this.IsOneTileFlipped())
        {
            customProps.Add("flips", this.FlippedTiles);
        }
        return customProps;
    }


    protected internal bool SetBoardByCustomProperties(Hashtable customProps, bool calledByEvent)
    {
        if (!calledByEvent)
        {
            this.TilesX = 4;    // original game had 4x4
            this.TilesY = 4;
            if (customProps.ContainsKey("tx#"))
            {
                this.TilesX = (int)customProps["tx#"];
                this.TilesY = (int)customProps["ty#"];
            }

            this.InitializeBoard();
        }

        int readTiles = 0;
        for (int i = 0; i < TileCount; i++)
        {
            if (customProps.ContainsKey(i.ToString()))
            {
                byte type = (byte)customProps[i.ToString()];
                if (type == TileTypeEmpty)
                {
                    RemoveTileAndQuad((byte) i);
                }
                else
                {
                    // the type defines the front of a tile and it's material
                    Tiles[i].Type = type;

                    int colorIndex = type % colors.Length;
                    int textureIndex = type % Textures.Length;

                    Material m = new Material(this.TileBackMaterial);
                    m.SetColor("_Color", colors[colorIndex]);
                    m.SetTexture("_MainTex", Textures[textureIndex]);

                    Tiles[i].TileMono.Front.Material = m;
                }
                
                readTiles++;
            }
        }
        
        
        // init "flipped tiles"
        this.FlippedTiles[0] = NotFlippedTile;
        this.FlippedTiles[1] = NotFlippedTile;
        
        // read the tiles that are actually, currently flipped (if any)
        if (customProps.ContainsKey("flips"))
        {
            byte[] othersFlippedTiles = (byte[]) customProps["flips"];

            for (int i = 0; i < othersFlippedTiles.Length; i++)
            {
                byte tileId = othersFlippedTiles[i];
                if (tileId == NotFlippedTile) continue;
            }

            this.FlippedTiles = othersFlippedTiles;
        }

        this.UpdateIsBoardEmpty();
        return readTiles == TileCount;
    }
}
