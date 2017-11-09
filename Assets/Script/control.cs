using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class control : SteamVR_TrackedController
{


    public float speed = 5;
    public float RotationSpeed = 5;
    public ClickedEventArgs e;
    
    Rigidbody rb;
    
    // Use this for initialization
    public override void Start () {
        base.Start();
        rb = GetComponent<Rigidbody>();
        transform.position = new Vector3(transform.position.x, 1, transform.position.z);
    }

    // Update is called once per frame
    public override void Update () {
        //transform.Rotate((Input.GetAxis("Mouse X") * RotationSpeed * Time.deltaTime), (Input.GetAxis("Mouse Y") * RotationSpeed * Time.deltaTime), 0, Space.World);

        if (Input.GetKeyDown("space"))
            teleport();

        transform.Rotate(Input.GetAxis("Mouse Y") * -RotationSpeed, Input.GetAxis("Mouse X") * RotationSpeed, 0);

		transform.eulerAngles = new Vector3(transform.eulerAngles.x, 
			transform.eulerAngles.y, 
			0f);

        rb.angularVelocity = new Vector3(0,0,0);
        rb.AddForce(transform.forward * Input.GetAxis("Vertical") * speed);
        rb.AddForce(transform.right * Input.GetAxis("Horizontal") * speed);

        //transform.position += transform.forward * Input.GetAxis("Vertical") * speed;
        if (Input.GetKeyDown(KeyCode.W))
        {
            print("Hit w");
            transform.position += transform.forward * speed;
        }

        if (Input.GetKeyDown("a"))
            transform.position += transform.right * -speed;

        if (Input.GetKeyDown("d"))
            transform.position += transform.right * speed;

        transform.position = new Vector3(transform.position.x, 1, transform.position.z);

        print("this");

        base.Update();
    }

    public void teleport()
    {
        base.OnTriggerClicked(e);

        RaycastHit hit;

        Vector3 newPosition = transform.forward * 10f;
        newPosition.y = 0;

        if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
        {
            print("teleport to hit: " + hit.point);
            newPosition = hit.point;
        }
        else
            print("teleport +10: " + newPosition);

        transform.parent.parent.transform.position = newPosition;
    }
}
