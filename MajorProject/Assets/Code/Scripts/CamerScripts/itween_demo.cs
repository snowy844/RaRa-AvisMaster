using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class itween_demo : MonoBehaviour {
  //  Vector3[] path;
   
    public Transform[] grind;
    public GameObject player;
    public Transform grindEnd;
    public Transform rotate;
    PlayerMovement grindDetect;
   
    float minDistance = float.PositiveInfinity;
    float minPercent = 0;
    bool onPath;
    float moveby;
    float current;
    float dist;
    float t;
    RaycastHit hitForLanding;
    // Use this for initialization
    void Start () {
      
        grindDetect = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update() {
        float v = Input.GetAxis("Vertical");
        detectGrind();
        moveAlongPath(v);
        endGrind();
       // iTween.RotateTo(player, new Vector3(0, 0, -40), 1f);
    }

    void detectGrind() {

        if (Physics.Raycast(player.transform.position, -player.transform.up, out hitForLanding)) {
          
            if (grindDetect.grind) {
                for ( t = 0; t <= 1; t += 0.02f) {
                     dist = Vector3.Distance(player.transform.position, iTween.PointOnPath(grind, t));
                    if (dist < minDistance) {
                        minDistance = dist;
                        minPercent = t;
                    }

                }
                iTween.PutOnPath(player, grind, minPercent);
                onPath = true;

            }

        }
    }


    void moveAlongPath(float v) {
        
        if (onPath) {
            if (v > 0) {
                minPercent += v * Time.deltaTime;
                if (minPercent > current)
                    current = minPercent;
                iTween.PutOnPath(player, grind, current);
            }
              //grindDetect.grind = false;
            }
        }


    void endGrind() {

        if (Input.GetButtonDown("Jump") || current >= 1) {
            onPath = false;
            grindDetect.grind = false;
            iTween.Stop(player);
            minPercent = 0;
            current = 0;
            minDistance = float.PositiveInfinity;
            //dist = 0;
            //t = 0;
            //v = 0;
        }
    
    }

     void OnDrawGizmos() {
       
        iTween.DrawPath(grind, Color.magenta);
    }
  

}
