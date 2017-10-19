using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSpawn : MonoBehaviour {

    public GameObject BirdPrefab;
    public GameObject Player;
    public Vector3 BirdSpawnPos = Vector3.zero;
    public float MaxBirdsSpawn = 25.0f;

    private Vector3 SpawnPos;
    private float PlayerSpeed;

	// Use this for initialization
	void Start () {
        Player = GameObject.FindGameObjectWithTag("Player");
        SpawnPos = Player.transform.TransformPoint(BirdSpawnPos);
        PlayerSpeed = Player.GetComponent<PlayerMovement>().speed;
	}
	
	// Update is called once per frame
	void Update () {

        SpawnPos = Player.transform.TransformPoint(BirdSpawnPos);
        PlayerSpeed = Player.GetComponent<PlayerMovement>().speed;
        //test spawn code
        if (/*Input.GetButtonDown("Fire1")*/ PlayerSpeed > 0.5f)
        {
            Spawn();
        }



	}

    void Spawn()
    {
        Instantiate(BirdPrefab, SpawnPos, Quaternion.LookRotation(Player.transform.position));
    }
}
