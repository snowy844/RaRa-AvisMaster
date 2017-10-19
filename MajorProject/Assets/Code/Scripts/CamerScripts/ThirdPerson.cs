using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPerson : MonoBehaviour {

    public float speed = 15;
    public Transform target;
    public Camera cam;
	

    void Update() {
        Move();
    }

    public void Move() {

        float rightH = Input.GetAxis("RightThumb");
        float rightV = Input.GetAxis("RightThumbV");

        Vector3 lookhere = new Vector3(/*(3 * Time.deltaTime) **/ rightV * 3, /*(3*Time.deltaTime)**/rightH * 3, 0);
        transform.eulerAngles += lookhere; 

        Vector3 direction = (target.position - cam.transform.position).normalized;

        //Quaternion lookRot = Quaternion.LookRotation(direction);

        //lookRot.x = transform.rotation.x;
        //lookRot.z = transform.rotation.z;

        //transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 100);
        transform.position = Vector3.Slerp(transform.position, target.position, Time.deltaTime * speed);
    }
}
