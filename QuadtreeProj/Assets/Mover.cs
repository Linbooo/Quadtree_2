//using System.Collections;
using UnityEngine;

public class Mover : MonoBehaviour {

	Material recentCubeMaterial;
	Transform recentCubeTranform;

	void Start () {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update () {
		transform.Translate( Input.GetAxis("Horizontal") * Time.deltaTime  * 15f, 0f, Input.GetAxis("Vertical") * Time.deltaTime * 15f, Space.Self);
		transform.Rotate(0f, Input.GetAxis("Mouse X") * Time.deltaTime * 50f, 0f, Space.World);
		transform.Rotate(-Input.GetAxis("Mouse Y") * Time.deltaTime * 50f, 0f, 0f, Space.Self);

		if(Input.GetKeyDown(KeyCode.Mouse0)){
			GameObject newCube = (GameObject) GameObject.Instantiate(Resources.Load("Cube"), transform.position + transform.forward * 6, transform.rotation);
		}

		RaycastHit hit;

		if(Physics.Raycast(transform.position, transform.forward, out hit, 100f)){ // "out" makes changes to the "hit" variable.
				if(hit.collider.tag == "OctCube"){

					if(recentCubeMaterial != null)
						recentCubeMaterial.color = Color.white;

					GameObject caughtCube = hit.collider.gameObject;
					Rigidbody caughtRigid = caughtCube.GetComponent<Rigidbody>();

					recentCubeMaterial = caughtCube.GetComponent<Renderer>().material;
					recentCubeMaterial.color = Color.cyan;

					if(Input.GetKeyDown(KeyCode.Mouse1)){
						caughtRigid.isKinematic = true;
						recentCubeTranform = caughtCube.transform;
						recentCubeTranform.parent = transform;

					}
					if(Input.GetKeyUp(KeyCode.Mouse1)){
						caughtRigid.isKinematic = false;
						if(recentCubeTranform != null){
							recentCubeTranform.parent = null;
						}
					}

					if(Input.GetKeyUp(KeyCode.E)){
						GameObject.Destroy(caughtCube);
					}

					if(Input.GetKeyDown(KeyCode.R)){
						caughtRigid.AddForce(transform.forward * 150f);
					}
				}
		}
		else{
			if(recentCubeMaterial != null)
				recentCubeMaterial.color = Color.white;
		}

		if(Input.GetKeyDown(KeyCode.Escape)){
			Application.Quit();
		}



	}
}
