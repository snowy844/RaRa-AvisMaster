using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BAvoidance : BAgent {

    public float maxSeeAhead;
    //public Vector3 target;
    public float maxAvoidanceForce;

    Vector3 ahead;
    Vector3 ahead2;
    Vector3 position;
    Vector3 velocity;
    Vector3 avoidanceForce;
    //Vector3 target;

    public GameObject threat;

	// Use this for initialization
	void Start () {
        //player = GameObject.FindGameObjectWithTag("Player");
        //target = player.transform.TransformPoint(0, 0, -10);
    }

    // Update is called once per frame
    void Update () {
        //target = player.transform.TransformPoint(0, 0, -10);
        //Ahead();
        avoidanceForce = collisionAvoidance();
        threat = findMostThreateningObstacle();
	}

    void calcAvoidanceForce(GameObject go)
    {
        avoidanceForce = ahead - go.transform.position;
        avoidanceForce = Vector3.Normalize(avoidanceForce) * maxAvoidanceForce;
    }

    public void Ahead(Vector3 target)
    {
        position = transform.position;
        velocity = Vector3.Normalize(target - position);
        ahead = position + Vector3.Normalize(velocity) * maxSeeAhead;
        ahead2 = position + Vector3.Normalize(velocity) * maxSeeAhead * 0.5f;
    }

    public Vector3 collisionAvoidance()
    {
        GameObject mostThreatening = findMostThreateningObstacle();
        Vector3 avoidance = Vector3.zero;

        if(mostThreatening != null)
        {
            avoidance.x += ahead.x - mostThreatening.transform.position.x;
            avoidance.y += ahead.y - mostThreatening.transform.position.y;
            avoidance.z += ahead.z - mostThreatening.transform.position.z;

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
        Collider col = go.GetComponent<Collider>();
        float radius = col.bounds.extents.magnitude;
        return Vector3.Distance(go.transform.position, a) <= radius || Vector3.Distance(go.transform.position, b) <= radius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, ahead2);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(ahead2, ahead);
    }
}
