using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSteeringManager : MonoBehaviour {

    public GameObject player;
    public float maxVelocity = 1.0f;
    public float maxForce = 1.0f;
    public float maxSpeed = 1.0f;
    public float speed = 0.1f;
    public float mass = 1.0f;
    public float slowRadius;
    public float maxSeeAhead;
    public float maxAvoidanceForce;
    public float maxSeperation;


    //Vector3 position;
    Vector3 target;
    Vector3 ahead;
    Vector3 ahead2;
    Vector3 avoidanceForce;
    Vector3 velocity;

    public GameObject threat;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        target = player.transform.TransformPoint(0, 5, -10);
    }

    // Update is called once per frame
    void Update()
    {
        target = player.transform.TransformPoint(0, 0, -10);
        avoidanceForce = collisionAvoidance();
        threat = findMostThreateningObstacle();

        Ahead(target);
        if (Random.Range(0, 5) < 1)
        {
            Seek(target, slowRadius);
        }
        transform.Translate(Time.deltaTime * speed, Time.deltaTime * speed, Time.deltaTime * speed);

    }

    void Seek(Vector3 target, float radius)
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
        steering = steering + Seperation() + collisionAvoidance();
        steering = steering / mass;
        velocity = Vector3.ClampMagnitude(velocity + steering, maxSpeed);

        transform.position += velocity * Time.deltaTime * speed;
        transform.LookAt(player.transform.position);
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

    void calcAvoidanceForce(GameObject go)
    {
        avoidanceForce = ahead - go.transform.position;
        avoidanceForce = Vector3.Normalize(avoidanceForce) * maxAvoidanceForce;
    }

    void Ahead(Vector3 target)
    {
        Vector3 position;
        position = transform.position;
        velocity = Vector3.Normalize(target - position);
        ahead = position + Vector3.Normalize(velocity) * maxSeeAhead;
        ahead2 = position + Vector3.Normalize(velocity) * maxSeeAhead * 0.5f;
    }

    Vector3 AvoidTerrain()
    {
        //this shit still experimental
        Vector3 avoidance = Vector3.zero;

        RaycastHit hit;
        float distance;
        if (Physics.Raycast(this.transform.position, Vector3.down, out hit))
        {
            distance = hit.distance;
            avoidance += new Vector3(0,distance,0);
            avoidance.Normalize();
            avoidance.Scale(Vector3.one * maxAvoidanceForce);
            Debug.DrawLine(this.transform.position, hit.point, Color.cyan);
        }
        else
        {
            avoidance.Scale(Vector3.zero);
        }

        return avoidance;
    }

    Vector3 collisionAvoidance()
    {
        GameObject mostThreatening = findMostThreateningObstacle();
        Vector3 avoidance = Vector3.zero;

        if (mostThreatening != null)
        {
            avoidance += ahead - mostThreatening.transform.position;
            avoidance.Normalize();
            avoidance.Scale(Vector3.one * maxAvoidanceForce);
        }
        else
        {
            avoidance.Scale(Vector3.zero);
        }

        return avoidance;
    }

    GameObject findMostThreateningObstacle()
    {
        GameObject mostThreatening = null;
        GameObject[] objects = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obstacle = objects[i];

            bool collision = lineIntersectsCircle(ahead, ahead2, obstacle);
            if (collision && (mostThreatening == null || Vector3.Distance(transform.position, obstacle.transform.position) < Vector3.Distance(transform.position, mostThreatening.transform.position)))
            {
                mostThreatening = obstacle;
            }
        }

        return mostThreatening;
    }

    bool lineIntersectsCircle(Vector3 a, Vector3 b, GameObject go)
    {
        if (go.GetComponent<Collider>() != null)
        {
            Collider col = go.GetComponent<Collider>();
            float radius = col.bounds.extents.magnitude;
            return Vector3.Distance(go.transform.position, a) <= radius || Vector3.Distance(go.transform.position, b) <= radius;
        }
        else
            return false;
        
    }

    Vector3 Seperation()
    {
        Vector3 force = Vector3.zero;
        Vector3 avoid = Vector3.zero;

        int neighbourCount = 0;

        GameObject[] objects = GameObject.FindGameObjectsWithTag("Bird");

        float dist;

        for(int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != this)
            {
                dist = Vector3.Distance(objects[i].transform.position, this.transform.position);
                if (dist <= maxSeperation)
                {
                    //force = force + (this.transform.position - objects[i].transform.position);
                    //force = objects[i].transform.position - this.transform.position;

                    force += objects[i].transform.position;


                    neighbourCount++;

                    if(dist < 2.0f)
                    {
                        avoid = avoid + (this.transform.position - objects[i].transform.position);
                    }

                }
            }
        }

        if(neighbourCount != 0)
        {
            force /= neighbourCount;

            force = (force + avoid) - this.transform.position;

            force = Vector3.ClampMagnitude(force, -1);
        }

        force.Normalize();
        force = Vector3.ClampMagnitude(force, maxSeperation);


        return force;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, ahead2);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(ahead2, ahead);
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, target);
    }



    public float getAngle(Vector3 vector)
    {
        return Mathf.Atan2(vector.y, vector.x);
    }

    public void setAngle(Vector3 vector, float value)
    {
        float len = vector.magnitude;
        vector.x = Mathf.Cos(value) * len;
        vector.y = Mathf.Sin(value) * len;
    }

    public void truncate(Vector3 vector, float max)
    {
        float i;

        i = max / vector.magnitude;
        i = i < 1.0f ? i : 1.0f;

        scaleBy(vector, i);
    }

    void scaleBy(Vector3 vector ,float k)
    {
        vector.x *= k;
        vector.y *= k;
    }
}
