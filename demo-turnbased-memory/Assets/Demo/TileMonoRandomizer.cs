using UnityEngine;
using System.Collections;

public class TileMonoRandomizer : TileMono
{
    public int MaxTilesInX;
    public int MinSecondsBetweenFlips;
    public int MaxSecondsBetweenFlips;
    private Vector3 startPos = Vector3.zero;
    private float timeToMoveAgain;


    protected override void SwapContentOnEdge()
    {
        if (InRotation && MaxTilesInX > 0)
        {
            if (startPos == Vector3.zero)
            {
                startPos = this.transform.position;
            }

            Vector3 newPos = startPos;
            newPos.x += Random.Range(0, MaxTilesInX);
            this.transform.position = newPos;
            this.UpdateTargetVectors();
            if (CurrentSide == Side.Edge)
            {
                this.transform.forward = -sideways;
            }
        }

        timeToMoveAgain = Time.time + Random.Range(MinSecondsBetweenFlips, MaxSecondsBetweenFlips);
        base.SwapContentOnEdge();
    }



    protected override void Update()
    {
        base.Update();

        if (!InRotation && this.TargetSide != Side.Edge && Time.time > timeToMoveAgain)
        {
            ToFront();
        }
    }
    
}
