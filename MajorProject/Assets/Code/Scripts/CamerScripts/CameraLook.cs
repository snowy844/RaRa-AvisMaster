using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour {

  
    
    public float turnSpeed = 10f;
    public Transform player;
    public float radius;
   
    private Vector3 offsetY;
    
    // Use this for initialization
    void Start () {

        offsetY = new Vector3(0, 2, -5);
    }

    void FixedUpdate() {
       
    }
    void LateUpdate() {

        offsetY = Quaternion.AngleAxis(/*Input.GetAxis("Mouse Y") ||*/ Input.GetAxisRaw("RightThumbV") * turnSpeed, -Vector3.right) * offsetY;
        //print(offsetY);

        //clamp the y and z values of camera
        offsetY.y = Mathf.Clamp(offsetY.y, 0.2f, 5.3f);
        offsetY.z = Mathf.Clamp(offsetY.z, -5.3f, -.3f);

        transform.position = player.TransformPoint( offsetY);
        transform.LookAt(player.position);
       

    }
    
}
