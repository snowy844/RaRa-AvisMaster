using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSeek : BAgent {

    void Start()
    {
        Avoid = GetComponent<BAvoidance>();
        //player = GameObject.FindGameObjectWithTag("Player");
        //target = player.transform.TransformPoint(0, 0, -10);
    }

    // Update is called once per frame
    void Update () {
        //Seek();
        //target = player.transform.TransformPoint(0, 0, -10);
    }

    public void Seek(Vector3 target, float radius)
    {
        Vector3 position = transform.position;
        Vector3 velocity = Vector3.Normalize(target - position);
        float distance = Vector3.Distance(target, position);
        Vector3 desiredVelocity = Vector3.zero;
        Vector3 steering = Vector3.zero;

        desiredVelocity = target - position;

        if (distance < radius)
        {
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxVelocity * (distance / radius);
        }
        else
        {
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxVelocity;
        }

        steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, maxForce);
        
        steering = steering + Avoid.collisionAvoidance();
        steering = steering / mass;
        velocity = Vector3.ClampMagnitude(velocity + steering, maxSpeed);

        transform.position += velocity * Time.deltaTime * speed;
        transform.LookAt(target);
    }
}
