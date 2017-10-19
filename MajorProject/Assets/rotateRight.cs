using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateRight : MonoBehaviour {
    public GameObject player;
    RaycastHit hit;
    float rotate;
	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        Debug.DrawRay(player.transform.position, -player.transform.up);
        if (Physics.Raycast(player.transform.position, -player.transform.up, out hit))
            rotate = hit.point.z;
    }

     void OnTriggerEnter(Collider other) {

        if (other.gameObject.tag == "Player") {
            print("hello");
        }
           iTween.RotateTo(player,new Vector3(0,0,-40), 1f);
    }
}
