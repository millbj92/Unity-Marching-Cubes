using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCamera : MonoBehaviour
{
    public Camera cam;
    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, transform.position + (cam.transform.forward * Input.GetAxis("Vertical")) + (transform.right * Input.GetAxis("Horizontal")), Time.deltaTime * 10);
        cam.transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0));
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0));
    }
}
