using UnityEngine;
using System.Collections;

public class BgColorChanger : MonoBehaviour
{
    public float Speed = 0.5f;
    public Material MatToChange;
    public Color[] Colors;
    

    private Color currentTarget;
    private float timeToChange;

	
	void Start ()
	{
	    SwitchTartgetColor();
	}

    private void SwitchTartgetColor()
    {
        currentTarget = Colors[Random.Range(0, Colors.Length)];
        timeToChange = Time.time + Random.Range(2, 5);
    }

    // Update is called once per frame
	void Update ()
	{
	    Color current = MatToChange.color;
	    current = Color.Lerp(current, currentTarget, Time.deltaTime*Speed);
	    MatToChange.color = current;

        if (current == currentTarget || Time.time > timeToChange)
        {
            SwitchTartgetColor();
        }
	}
}
