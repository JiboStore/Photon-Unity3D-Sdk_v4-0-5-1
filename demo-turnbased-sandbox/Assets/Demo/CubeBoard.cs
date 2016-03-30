using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CubeBoard : MonoBehaviour
{
    private const int TilesPerRowAndColum = 4;
    public const int TileCount = TilesPerRowAndColum * TilesPerRowAndColum;
    private const int MaximumValueOfTile = 2;
    private readonly Color[] colors = new Color[] { Color.white, Color.red, Color.blue };
    private const float SizeFactor = 2f;

    protected internal Dictionary<int, GameObject> TileGameObjects;
    protected internal byte[] TileValues = new byte[TileCount];


    public void OnEnable()
    {
        this.ShowCubes();
    }

    public void OnDisable()
    {
        this.ClearCubes();
    }

    public void ResetTileValues()
    {
        TileValues = new byte[TileCount];
    }

    public void ClearCubes()
    {
        if (this.TileGameObjects != null)
        {
            foreach (GameObject go in TileGameObjects.Values)
            {
                GameObject.Destroy(go);
            }

            this.TileGameObjects.Clear();
        }
        ResetTileValues();
    }

    public void ShowCubes()
    {
        if (this.TileGameObjects == null || this.TileGameObjects.Count < TileCount)
        {
            CreateCubes();
        }

        for (int i = 0; i < TileCount; i++)
        {
            int cubeColor = TileValues[i];
            GameObject cube = this.TileGameObjects[i];
            if (cubeColor > colors.Length)
            {
                cube.renderer.material.SetColor("_Color", Color.magenta);
            }
            else
            {
                cube.renderer.material.SetColor("_Color", colors[cubeColor]);
            }
        }
    }

    public void CreateCubes()
	{
        ClearCubes();

        this.TileGameObjects = new Dictionary<int, GameObject>(TileCount);
	    int y = 0;
        int x = 0;
        for (int i = 0; i < TileCount; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = this.transform;
            cube.transform.localPosition = new Vector3(x, y)*SizeFactor;
            cube.transform.localScale = Vector3.one*SizeFactor*0.9f;
            cube.name = cube.name + i;

            int cubeColor = TileValues[i];
            cube.renderer.material.SetColor("_Color", colors[cubeColor]);

            this.TileGameObjects.Add(i, cube);

            // adjust x and y for next tile
            x++;
            if (x >= TilesPerRowAndColum)
            {
                x = 0; 
                y++;
            }
        }
	}

    private void OnClick()
    {
        if (InputToEvent.goPointedAt != null)
        {
            int index = GetCubeTileIndex(InputToEvent.goPointedAt);
            //Debug.Log(index);

            if (index >= 0 && index < TileCount)
            {
                int value = TileValues[index];
                value = value + 1;
                if (value > MaximumValueOfTile) value = 0;
                TileValues[index] = (byte)value;
                ShowCubes();
            }
            //Debug.Log(index + "=" + TileValues[index]);
        }
    }

    public int GetCubeTileIndex(GameObject cube)
    {
        foreach (KeyValuePair<int, GameObject> pair in TileGameObjects)
        {
            if (pair.Value.Equals(cube))
            {
                return pair.Key;
            }
        }

        Debug.LogError("Could not find Cube in Dict.");
        return -1;
    }

    protected internal Hashtable GetBoardAsCustomProperties()
    {
        Hashtable customProps = new Hashtable();
        for (int i = 0; i < TileCount; i++)
        {
            customProps[i.ToString()] = TileValues[i];
        }
        return customProps;
    }

    protected internal bool SetBoardByCustomProperties(Hashtable customProps)
    {
        int readTiles = 0;
        for (int i = 0; i < TileCount; i++)
        {
            if (customProps.ContainsKey(i.ToString()))
            {
                TileValues[i] = (byte)customProps[i.ToString()];
                readTiles++;
            }
        }
        return readTiles == TileCount;
    }
}
