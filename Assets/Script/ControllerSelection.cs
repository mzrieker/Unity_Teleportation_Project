using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerSelection : SteamVR_TrackedController {

    public LineRenderer lineRenderer;
    public Vector3[] lineRendererVertices;

	public Material hoveredObjectMaterial;
	public Material selectedObjectMaterial;

	private GameObject lastHoveredObject = null;
	private List<Material> originalHoveredObjectMaterials = new List<Material>();

	public GameObject selectedObject = null;
	private List<Material> originalSelectedObejectMaterials = new List<Material>();

    // Use this for initialization
    public override void Start ()
    {
        base.Start();
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.03f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.numPositions = 2;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
        lineRendererVertices = new Vector3[2];
    }

    public override void OnTriggerClicked(ClickedEventArgs e)
    {
        base.OnTriggerClicked(e);

		RaycastHit hit;

		// indicate selection and acquire the transform of selected object
		if (Physics.Raycast(transform.position, transform.forward, out hit, 5f))
		{
			if(selectedObject != null)
			{
				RestoreColor(selectedObject, originalSelectedObejectMaterials);
			}
			selectedObject = hit.transform.gameObject;

			if (selectedObject == lastHoveredObject)
				RestoreColor (lastHoveredObject, originalHoveredObjectMaterials);

			lastHoveredObject = null;

			ChangeColor(selectedObject, selectedObjectMaterial, originalSelectedObejectMaterials);
		}
		else // no hit
		{
			if(selectedObject != null)
			{
				RestoreColor(selectedObject, originalSelectedObejectMaterials);
				selectedObject = null;
			}
		}
    }

    // Update is called once per frame
    public override void Update ()
    {
		Vector3 startPos = transform.position + transform.up * -0.035f;

		// Start the line in the middle of the controller ring.
		lineRendererVertices[0] = transform.position + new Vector3(0.0f,-0.035f,0f);

		RaycastHit hit;

		// set the end of the raycast to distance 5 or collision point
		if (Physics.Raycast(transform.position, transform.forward, out hit, 5f))
		{
			lineRendererVertices[1] = hit.point;
			var HoveredObject = hit.transform.gameObject;

			if (lastHoveredObject != null && lastHoveredObject != selectedObject)
				RestoreColor (lastHoveredObject, originalHoveredObjectMaterials);

			if (HoveredObject != selectedObject) 
				ChangeColor (HoveredObject, hoveredObjectMaterial, originalHoveredObjectMaterials);


			if (HoveredObject == selectedObject) 
				lastHoveredObject = null;
			else
				lastHoveredObject = HoveredObject;

		}
		else
		{
			if (lastHoveredObject != null && lastHoveredObject != selectedObject)
			{
				RestoreColor (lastHoveredObject, originalHoveredObjectMaterials);
				lastHoveredObject = null;
			}
			lineRendererVertices[1] = startPos + transform.forward * 5f;
		}
		lineRenderer.SetPositions(lineRendererVertices);
        base.Update();
	}

	public void RestoreColor(GameObject Object, List<Material> originalMaterials)
    {
		MeshRenderer[] meshes = Object.GetComponentsInChildren<MeshRenderer>();

        // change materials of current object to what they were originally
		for (int i = 0; i < meshes.Length; i++)
        {
			meshes[i].material = originalMaterials[i];
		}
	}

	public void ChangeColor(GameObject theObject, Material newMaterial, List<Material> originalMaterials)
    {
		MeshRenderer[] meshes = theObject.GetComponentsInChildren<MeshRenderer>();

        // if the current object is not already composed of the new material
		if (newMaterial != theObject.GetComponentInChildren<MeshRenderer>().material)
        {
			originalMaterials.Clear();

            // record the original materials of the current object
			for (int i = 0; i < meshes.Length; i++)
            {
				originalMaterials.Add(meshes[i].material);
			}

            // change the materials of the current object to the new material
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].material = newMaterial;
            }
        }
	}
}
