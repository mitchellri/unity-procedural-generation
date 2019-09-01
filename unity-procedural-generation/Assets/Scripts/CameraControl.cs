using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour
{

    /*
    Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
    Converted to C# 27-02-13 - no credit wanted.
    Simple flycam I made, since I couldn't find any others made public.  
    Made simple to use (drag and drop, done) for regular keyboard layout  
    wasd : basic movement
    shift : Makes camera accelerate
    space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/

    /*
    Edited by MitchellRi.
    Removed mouse movement
        space feature
    Changed keyboard controls to include arrows
        keyboard controls to 2D
    scroll: Zooms camera*/

    float moveSpeed = 100.0f; //regular speed
    float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    float maxShift = 1000.0f; //Maximum speed when holdin gshift
    private float totalRun = 1.0f;
    float zoomSize;
    float zoomSpeed = 10;

    void Start()
    {
        zoomSize = GetComponent<Camera>().orthographicSize;
    }

    void Update()
    {
        //Keyboard commands
        Vector3 p = GetBaseInput();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            totalRun += Time.deltaTime;
            p = p * totalRun * shiftAdd;
            p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
            p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
            p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
        }
        else
        {
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
            p = p * moveSpeed;
        }

        p = p * Time.deltaTime;
        transform.Translate(p);

        //Scroll wheel commands
        //Mouse wheel moving forward
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && zoomSize - zoomSpeed > 0) GetComponent<Camera>().orthographicSize = zoomSize -= zoomSpeed;

        //Mouse wheel moving backward
        else if (Input.GetAxis("Mouse ScrollWheel") < 0) GetComponent<Camera>().orthographicSize = zoomSize += zoomSpeed;
    }

    private Vector3 GetBaseInput()
    { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, -1, 0);
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 1, 0);
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        return p_Velocity;
    }
}