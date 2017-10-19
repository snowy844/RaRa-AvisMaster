using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BFlee : BAgent {

    public Vector3 target;


    // Update is called once per frame
    void Update()
    {
        target = player.transform.position;
        Flee();
    }

    void Flee()
    {
        Vector3 velocity = Vector3.Normalize(transform.position - target);
        Vector3 desiredVelocity = Vector3.Normalize(transform.position - target) * maxVelocity;
        Vector3 steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, maxForce);
        steering = steering / mass;
        velocity = Vector3.ClampMagnitude(velocity + steering, maxSpeed);
        transform.position += velocity * Time.deltaTime * speed;
        transform.LookAt(-player.transform.position);
    }
}
