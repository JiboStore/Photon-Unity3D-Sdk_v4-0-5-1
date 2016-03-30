using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class TileMonoGroup : MonoBehaviour
{
    private TileMono[] tiles;
    public bool InRotation;
    public bool InStateTransition;

    void Start()
    {
        tiles = GetComponentsInChildren<TileMono>(true);
    }

    public void GroupToFront()
    {
        foreach (TileMono tile in this.tiles)
        {
            tile.ToFront();
        }
    }

    public void GroupToBack()
    {
        foreach (TileMono tile in this.tiles)
        {
            tile.ToBack();
        }
    }

    public void GroupToSide()
    {
        foreach (TileMono tile in this.tiles)
        {
            tile.ToSide();
        }
    }

    public void GroupVisibility(bool visible)
    {
        foreach (TileMono tile in this.tiles)
        {
            tile.Visible = visible;
        }
    }
    
    void Update()
    {
        bool rotates = false;
        bool stateChanging = false;
        foreach (TileMono tile in this.tiles)
        {
            if (!rotates && tile.InRotation)
            {
                rotates = true;
            }
            if (!stateChanging && tile.gameObject.activeInHierarchy && tile.InStateTransition)
            {
                stateChanging = true;
                if (rotates && stateChanging)
                {
                    break;
                }
            }
        }

        bool stoppedRotating = rotates == false && this.InRotation != rotates;
        this.InRotation = rotates;
        this.InStateTransition = stateChanging;
        if (stoppedRotating)
        {
            //SendMessageUpwards("EndOfRotation");
        }
        

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyUp("s"))
            {
                GroupToSide();
            }
            if (Input.GetKeyUp("f"))
            {
                GroupToFront();
            }
            if (Input.GetKeyUp("b"))
            {
                GroupToBack();
            }
        }
    }
}
