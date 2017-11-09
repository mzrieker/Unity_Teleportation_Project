//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using System.Collections.Generic;
using Valve.VR;

public class SteamVR_ControllerManager : MonoBehaviour
{
	public GameObject left, right;
	public GameObject[] objects; // populate with objects you want to assign to additional controllers

	public bool assignAllBeforeIdentified; // set to true if you want objects arbitrarily assigned to controllers before their role (left vs right) is identified

	uint[] indices; // assigned
	bool[] connected = new bool[OpenVR.k_unMaxTrackedDeviceCount]; // controllers only

	// cached roles - may or may not be connected
	uint leftIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
	uint rightIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

	// This needs to be called if you update left, right or objects at runtime (e.g. when dyanmically spawned).
	public void UpdateTargets()
	{
		// Add left and right entries to the head of the list so we only have to operate on the list itself.
		var additional = (this.objects != null) ? this.objects.Length : 0;
		var objects = new GameObject[2 + additional];
		indices = new uint[2 + additional];
		objects[0] = right;
		indices[0] = OpenVR.k_unTrackedDeviceIndexInvalid;
		objects[1] = left;
		indices[1] = OpenVR.k_unTrackedDeviceIndexInvalid;
		for (int i = 0; i < additional; i++)
		{
			objects[2 + i] = this.objects[i];
			indices[2 + i] = OpenVR.k_unTrackedDeviceIndexInvalid;
		}
		this.objects = objects;
	}

	SteamVR_Events.Action inputFocusAction, deviceConnectedAction, trackedDeviceRoleChangedAction;

	void Awake()
	{
		UpdateTargets();
		inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);
		deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
		trackedDeviceRoleChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_TrackedDeviceRoleChanged, OnTrackedDeviceRoleChanged);
	}

	void OnEnable()
	{
		for (int i = 0; i < objects.Length; i++)
		{
			var obj = objects[i];
			if (obj != null)
				obj.SetActive(false);
		}

		Refresh();

		for (int i = 0; i < SteamVR.connected.Length; i++)
			if (SteamVR.connected[i])
				OnDeviceConnected(i, true);

		inputFocusAction.enabled = true;
		deviceConnectedAction.enabled = true;
		trackedDeviceRoleChangedAction.enabled = true;
	}

	void OnDisable()
	{
		inputFocusAction.enabled = false;
		deviceConnectedAction.enabled = false;
		trackedDeviceRoleChangedAction.enabled = false;
	}

	static string[] labels = { "left", "right" };

	// Hide controllers when the dashboard is up.
	private void OnInputFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				var obj = objects[i];
				if (obj != null)
				{
					var label = (i < 2) ? labels[i] : (i - 1).ToString();
					ShowObject(obj.transform, "hidden (" + label + ")");
				}
			}
		}
		else
		{
			for (int i = 0; i < objects.Length; i++)
			{
				var obj = objects[i];
				if (obj != null)
				{
					var label = (i < 2) ? labels[i] : (i - 1).ToString();
					HideObject(obj.transform, "hidden (" + label + ")");
				}
			}
		}
	}

	// Reparents to a new object and deactivates that object (this allows
	// us to call SetActive in OnDeviceConnected independently.
	private void HideObject(Transform t, string name)
	{
		var hidden = new GameObject(name).transform;
		hidden.parent = t.parent;
		t.parent = hidden;
		hidden.gameObject.SetActive(false);
	}
	private void ShowObject(Transform t, string name)
	{
		var hidden = t.parent;
		if (hidden.gameObject.name != name)
			return;
		t.parent = hidden.parent;
		Destroy(hidden.gameObject);
	}

	private void SetTrackedDeviceIndex(int objectIndex, uint trackedDeviceIndex)
	{
		// First make sure no one else is already using this index.
		if (trackedDeviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				if (i != objectIndex && indices[i] == trackedDeviceIndex)
				{
					var obj = objects[i];
					if (obj != null)
						obj.SetActive(false);

					indices[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
				}
			}
		}

		// Only set when changed.
		if (trackedDeviceIndex != indices[objectIndex])
		{
			indices[objectIndex] = trackedDeviceIndex;

			var obj = objects[objectIndex];
			if (obj != null)
			{
				if (trackedDeviceIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
					obj.SetActive(false);
				else
				{
					obj.SetActive(true);
					obj.BroadcastMessage("SetDeviceIndex", (int)trackedDeviceIndex, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	// Keep track of assigned roles.
	private void OnTrackedDeviceRoleChanged(VREvent_t vrEvent)
	{
		Refresh();
	}

	// Keep track of connected controller indices.
	private void OnDeviceConnected(int index, bool connected)
	{
		bool changed = this.connected[index];
		this.connected[index] = false;

		if (connected)
		{
			var system = OpenVR.System;
			if (system != null)
			{
				var deviceClass = system.GetTrackedDeviceClass((uint)index);
				if (deviceClass == ETrackedDeviceClass.Controller ||
					deviceClass == ETrackedDeviceClass.GenericTracker)
				{
					this.connected[index] = true;
					changed = !changed; // if we clear and set the same index, nothing has changed
				}
			}
		}

		if (changed)
			Refresh();
	}

	public void Refresh()
	{
		int objectIndex = 0;

		var system = OpenVR.System;
		if (system != null)
		{
			leftIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			rightIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
		}

		// If neither role has been assigned yet, try hooking up at least the right controller.
		if (leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
		{
			for (uint deviceIndex = 0; deviceIndex < connected.Length; deviceIndex++)
			{
				if (objectIndex >= objects.Length)
					break;

				if (!connected[deviceIndex])
					continue;

				SetTrackedDeviceIndex(objectIndex++, deviceIndex);

				if (!assignAllBeforeIdentified)
					break;
			}
		}
		else
		{
			SetTrackedDeviceIndex(objectIndex++, (rightIndex < connected.Length && connected[rightIndex]) ? rightIndex : OpenVR.k_unTrackedDeviceIndexInvalid);
			SetTrackedDeviceIndex(objectIndex++, (leftIndex < connected.Length && connected[leftIndex]) ? leftIndex : OpenVR.k_unTrackedDeviceIndexInvalid);

			// Assign out any additional controllers only after both left and right have been assigned.
			if (leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
			{
				for (uint deviceIndex = 0; deviceIndex < connected.Length; deviceIndex++)
				{
					if (objectIndex >= objects.Length)
						break;

					if (!connected[deviceIndex])
                    {
                        continue;
                    }

					if (deviceIndex != leftIndex && deviceIndex != rightIndex)
					{
						SetTrackedDeviceIndex(objectIndex++, deviceIndex);
					}
				}
			}
		}

		// Reset the rest.
		while (objectIndex < objects.Length)
		{
			SetTrackedDeviceIndex(objectIndex++, OpenVR.k_unTrackedDeviceIndexInvalid);
		}
	}

	public LineRenderer lineRenderer;
	public Vector3[] lineRendererVertices;

	public Material hoveredObjectMaterial;
	public Material selectedObjectMaterial;

	private GameObject lastHoveredObject = null;
	private List<Material> originalHoveredObjectMaterials = new List<Material>();

	private GameObject selectedObject = null;
	private List<Material> originalSelectedObejectMaterials = new List<Material>();

	private bool triggerPressed = false;
	private bool trigger2Pressed = false;

	private float timeSinceLastPort = 0.0f;
		
    void Start()
    {
		lineRenderer = gameObject.AddComponent<LineRenderer>();
		lineRenderer.startWidth = 0.03f;
		lineRenderer.endWidth = 0.02f;
		lineRenderer.numPositions = 2;
		lineRenderer.startColor = Color.red;
		lineRenderer.endColor = Color.red;
		lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
		lineRendererVertices = new Vector3[2];
    }

    void Update()
	{
		Vector3 startPos = transform.position + transform.up * -0.035f;

		lineRendererVertices[0] = transform.position + new Vector3(0.1f,0.1f,0f);

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


		// TRIGGER
		ulong trigger = (Input.GetKeyDown(KeyCode.Q)) ? 1UL : 0L;
		if (trigger > 0L && !triggerPressed)
		{
			onTrigger ();
		}

		else if (trigger == 0L && triggerPressed)
		{
			triggerPressed = false;
		}

		timeSinceLastPort += Time.deltaTime;
		ulong trigger2 = (Input.GetKeyDown(KeyCode.E)) ? 1UL : 0L;
		if (trigger2 > 0L && !trigger2Pressed)
		{
			onTrigger2 ();
		}

		else if (trigger2 == 0L && trigger2Pressed)
		{
			trigger2Pressed = false;
		}

    }

	public void onTrigger(){ // indicate selection and acquire the transform of selected object

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

	public void onTrigger2(){ // indicate selection and acquire the transform of selected object


		if (timeSinceLastPort <= 2)
			return;

		timeSinceLastPort = 0.0f;

		RaycastHit hit;

		Vector3 newPosition = transform.forward * 10f;
		newPosition.y = 0;

		if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
		{
			print ("teleport to hit: " + hit.transform.position);
			newPosition = hit.transform.position;
		}
		else
			print ("teleport +10: " + hit.transform.position);

		transform.parent.transform.position = newPosition;

	}
}

