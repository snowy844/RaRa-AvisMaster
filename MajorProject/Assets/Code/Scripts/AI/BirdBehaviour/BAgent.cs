using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BAgent : MonoBehaviour {

    public GameObject player;
    public float maxVelocity = 1.0f;
    public float maxForce = 1.0f;
    public float maxSpeed = 1.0f;
    public float speed = 0.1f;
    public float mass = 1.0f;

    public float slowRadius;
    Vector3 target;
    public BAvoidance Avoid;
    BSeek bseek;

    // Use this for initialization
    void Start () {
        
        player = GameObject.FindGameObjectWithTag("Player");
        target = player.transform.TransformPoint(0, 0, -10);
        bseek = GetComponent<BSeek>();
        Avoid = GetComponent<BAvoidance>();
    }

    // Update is called once per frame
    void Update () {
        target = player.transform.TransformPoint(0, 0, -10);
        Avoid.Ahead(target);
        bseek.Seek(target, slowRadius);
    }

    //void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.black;
    //    Gizmos.DrawLine(transform.position, target);
    //}
}
