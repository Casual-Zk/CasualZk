using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private GameObject main_camera;
	public float smoothSpeed = 0.25f;
	public Vector3 offset;

    void Start(){
		main_camera = GameObject.Find("Main Camera");
    }
	void FixedUpdate ()
	{
		Vector3 desiredPosition = transform.position + offset;
		desiredPosition.z = -10;
		Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
		main_camera.transform.position = smoothedPosition;
	}
}
