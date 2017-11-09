using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportationController : SteamVR_TrackedController {

	public LineRenderer lineRenderer;
	public Vector3[] lineRendererVertices;
	public RaycastHit hit;

	// Use this for initialization
	public override void Start ()
	{
		base.Start();
		lineRenderer = gameObject.AddComponent<LineRenderer>();
		lineRenderer.startWidth = 0.035f;
		lineRenderer.endWidth = 0.035f;
		lineRenderer.numPositions = 2;
		lineRenderer.startColor = Color.blue;
		lineRenderer.endColor = Color.blue;
		lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
		lineRendererVertices = new Vector3[2];
	}

	public override void OnTriggerClicked(ClickedEventArgs e)
	{
		base.OnTriggerClicked(e);

		RaycastHit hit;

		Vector3 newPosition = transform.forward * 10f;
		newPosition.y = 0;

		if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
		{
			print ("teleport to hit: " + hit.point);
			newPosition = hit.point;
		}
		else
			print ("teleport +10: " + newPosition);

		transform.parent.parent.transform.position = newPosition;
	}

	// Update is called once per frame
	public override void Update ()
	{
		Vector3 startPos = transform.position;

		lineRendererVertices [0] = startPos + transform.up * -.035f;
		if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
		{
			lineRendererVertices[1] = hit.point;
		}
		else
		{
			lineRendererVertices[1] = startPos + transform.forward * 10f;
		}
		lineRenderer.SetPositions(lineRendererVertices);

		base.Update();
	}
}