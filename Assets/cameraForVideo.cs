using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraForVideo : MonoBehaviour {
    Rigidbody rbody;
	// Use this for initialization
	void Start () {
        rbody = this.GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        if(Input.GetKey(KeyCode.Space))
            rbody.AddRelativeForce(3000 * Vector3.forward * Time.deltaTime);
        this.transform.eulerAngles += new Vector3(-Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), 0);

	}
}
