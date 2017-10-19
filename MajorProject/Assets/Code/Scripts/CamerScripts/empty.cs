using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class empty : MonoBehaviour {

    public Transform player;
    PlayerMovement vel;
    
	// Use this for initialization
	void Start () {
        vel = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }
	
	// Update is called once per frame
	void Update () {
       
        transform.position = player.transform.position;
        //if (Input.GetAxis("Horizontal")!= 0) {
        //    transform.rotation = Quaternion.Euler(0, player.eulerAngles.y, 0);
        //}
        
	}
}
