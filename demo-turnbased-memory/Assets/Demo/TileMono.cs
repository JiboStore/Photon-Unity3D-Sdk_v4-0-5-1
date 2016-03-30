using System;
using UnityEngine;
using System.Collections;

[Serializable]
public class TileContent
{
    public string CallMessage;
    public object CallParameter;
    public string Text;
    public Material Material;
}

public class TileMono : MonoBehaviour
{
    public enum Side
    {
        Edge,
        Front,
        Back, 
        Custom
    }


    public TileContent Front;
    public TileContent Back;
    private TileContent Next;

    public Side CurrentSide;
    public Side TargetSide;
    public bool InRotation;
    public bool InStateTransition { get { return this.CurrentSide != this.TargetSide; }}
    private bool flipFullIfNeeded;

    private bool visible = true;
    public bool Visible {
        get { return this.visible; }
        set 
        { 
            visible = value;
            this.OnVisibleChanged();
        }
    }

    private TextMesh textMesh;
    protected internal TextMesh TextMesh
    {
        get
        {
            if (this.textMesh == null)
            {
                TextMesh[] res = this.GetComponentsInChildren<TextMesh>(true);
                this.textMesh = res[0];
            }
            return textMesh;
        }
    }

    private OnClickCall clickCall;
    protected internal  OnClickCall ClickCall
    {
        get
        {
            if (this.clickCall == null)
            {
                OnClickCall[] res = this.GetComponentsInChildren<OnClickCall>(true);
                if (res.Length > 0)
                {
                    this.clickCall = res[0];
                }
            }
            return clickCall;
        }
    }

    private void OnVisibleChanged()
    {
        if (this.visible)
        {
            this.gameObject.SetActive(this.visible);
        }
        else
        {
            this.TargetSide = Side.Edge;
        }
    }


    protected Vector3 sideways;
    protected readonly Vector3 frontal = Vector3.forward;
    public float Speed = 2.0f;  // inspector


	// Use this for initialization
	void Start() 
    {
        // init as sideways
        this.UpdateTargetVectors();
        this.transform.forward = -sideways;
        //this.SwapContentOnEdge();
    }


    public void ToFront()
    {
        this.TargetSide = Side.Front;
        flipFullIfNeeded = true;
        this.SwapContentOnEdge();
    }

    public void ToBack()
    {
        this.TargetSide = Side.Back;
        flipFullIfNeeded = true;
        this.SwapContentOnEdge();
    }

    public void ToSide()
    {
        this.TargetSide = Side.Edge;
        flipFullIfNeeded = false;
        this.SwapContentOnEdge();
    }

    public void ToNext(TileContent next)
    {
        this.Next = next;

        this.TargetSide = Side.Custom;
        flipFullIfNeeded = true;
        this.SwapContentOnEdge();
    }
    

    // Update is called once per frame
	protected virtual void Update ()
	{
	    if (this.CurrentSide == this.TargetSide && !this.flipFullIfNeeded)
	    {
            InRotation = false;
	        bool reachedForwardOrBack = this.transform.forward == frontal;
	        bool reachedEdge = !reachedForwardOrBack && (this.transform.forward == sideways || this.transform.forward == -sideways);

	        if (this.CurrentSide == Side.Edge)
	        {
                if (!this.visible)
                {
                    this.gameObject.SetActive(false);
                }
	            if (reachedEdge) return;
	        }
	        else
	        {
	            if (reachedForwardOrBack) return;
	        }
	    }
        InRotation = true;

	    bool flipToEdgeFirst = this.CurrentSide != Side.Edge && this.TargetSide != Side.Edge;

        Vector3 targetVector = (this.TargetSide == Side.Edge || flipToEdgeFirst) ? sideways : frontal;
        Vector3 forwardVector = Vector3.RotateTowards(this.transform.forward, targetVector, 1.0f * Time.deltaTime * Speed, 0.0f);

        this.transform.forward = forwardVector;
        if (Vector3.Angle(targetVector, forwardVector) < 1.0f)
        {
            if (flipToEdgeFirst)
            {
                this.CurrentSide = Side.Edge;
                this.SwapContentOnEdge();
            }
            else
            {
                this.CurrentSide = this.TargetSide;
            }

            this.transform.forward = targetVector;
            if (this.CurrentSide == Side.Edge)
            {
                this.transform.forward = -targetVector; // a little trick, to rotate always in the same direction
            }
        }
	}

    protected virtual void SwapContentOnEdge()
    {
        if (!this.gameObject.activeInHierarchy)
        {
            return;
        }

        if (this.CurrentSide != Side.Edge)
        {
            return;
        }

        this.flipFullIfNeeded = false;
        if (this.TargetSide == Side.Edge)
        {
            return;
        }

        TileContent newContent = null;
        if (this.Next != null)
        {
            newContent = this.Next;
            this.TargetSide = Side.Custom;
            this.Next = null;   // applied next.
            Debug.Log("Got next content. Got to go to Custom.");
        }
        else
        {
            newContent = this.TargetSide == Side.Front ? this.Front : this.Back;
        }

        if (newContent == null)
        {
            this.gameObject.SetActive(false);
        }
        // update text
        if (this.TextMesh == null)
        {
            Debug.Log(string.Format("{0} can't swap on edge. textMesh null: {1} activeInHierarchy: {2}", this, (this.TextMesh == null), this.gameObject.activeInHierarchy));
            return;
        }
        this.TextMesh.text = newContent.Text;


        // update material
        if (newContent.Material != null)
        {
            this.renderer.material = newContent.Material;
        }

        if (this.ClickCall != null)
        {
            this.ClickCall.CallMessage = newContent.CallMessage;
            this.ClickCall.Parameter = newContent.CallParameter;
        }
        else
        {
            Debug.Log(string.Format("ClickCall is null on: {0}", this));
        }
        
    }


    protected void UpdateTargetVectors()
    {
        Vector3 toCam = Camera.main.transform.position - this.transform.position;
        toCam.y = 0;
        toCam.Normalize();

        sideways = Vector3.Cross(toCam, Vector3.up);
    }
}