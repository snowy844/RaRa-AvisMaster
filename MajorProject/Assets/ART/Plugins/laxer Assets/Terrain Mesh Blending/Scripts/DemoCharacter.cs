using UnityEngine;
using System.Collections;

public class DemoCharacter : MonoBehaviour {
	public float speed;
	public float mouse_speed;
	public Terrain terr;
	public Transform cam;
	public GameObject[] demo_objs;
	int toggle = 0;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyUp (KeyCode.E)) {
			demo_objs[toggle].SetActive(false);
			toggle++;		
			if(toggle>= demo_objs.Length){
				toggle = 0;
			}
			demo_objs[toggle].SetActive(true);
		}
		transform.Rotate (new Vector3 (0, mouse_speed*Input.GetAxis("Mouse X")*Time.deltaTime, 0));
		cam.localRotation *=  Quaternion.Euler(new Vector3 (-mouse_speed*Input.GetAxis("Mouse Y")*Time.deltaTime,0, 0));
		Vector2 dir = new Vector2 ();
		if(Input.GetKey(KeyCode.A)){
			dir.x--;
		}
		if(Input.GetKey(KeyCode.W)){
			dir.y++;
		}
		if(Input.GetKey(KeyCode.S)){
			dir.y--;
		}
		if(Input.GetKey(KeyCode.D)){
			dir.x++;
		}
		transform.position += speed * transform.forward*Time.deltaTime * dir.y;
		transform.position += speed * transform.right*Time.deltaTime * dir.x;
		transform.position = new Vector3 (transform.position.x, terr.terrainData.GetInterpolatedHeight((transform.position.x-terr.GetPosition().x)/terr.terrainData.size.x,(transform.position.z-terr.GetPosition().z)/terr.terrainData.size.z)+terr.GetPosition().y, transform.position.z);
	}
}
