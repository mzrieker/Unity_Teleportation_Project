using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManipulation : SteamVR_TrackedController {

	public float scalefactor = 2.0f;

	public ControllerSelection selection;

	private GameObject selectedObject;
	private RigidbodyConstraints oldConstraints;

	private bool doRotation = false;
	private Quaternion startControllerRotation;
	private Quaternion startObjectRotation;

	private bool doScaling = false;
	private GameObject firstController;
	private GameObject secondController;
	private Vector3 startScale;
	private float startDistance;

	// Use this for initialization
	void Start () {
		base.Start ();

		firstController = GameObject.Find ("/player/[CameraRig]/Controller (left)");
		secondController = GameObject.Find ("/player/[CameraRig]/Controller (right)");

		if (firstController == null || secondController == null) {
			print ("One of the controller objects wasn't found.");
			return;
		}
	}
	
	// Update is called once per frame
	void Update () {
		base.Update ();

		// Selected a different object mid manipulation? Stop it.
		if (selection.selectedObject == null || selectedObject != selection.selectedObject) {
			ResetSelectionState ();
			return;
		}

		if (doRotation) {
			// Rotate it!
			Quaternion targetRotation = startObjectRotation * transform.rotation * Quaternion.Inverse (startControllerRotation);
			//selectedObject.transform.rotation = Quaternion.Slerp(startObjectRotation, targetRotation, 0.1f);
			selectedObject.transform.rotation = targetRotation;
		}

		// Scale the object based on the difference between the distance of the two controllers
		// compared to how far apart they were when the button was pressed.
		if (doScaling) {
			float distance = (firstController.transform.position - secondController.transform.position).sqrMagnitude;
			distance = Mathf.Abs (distance);

			float scale = (distance - startDistance) * scalefactor;
			selectedObject.transform.localScale = startScale + new Vector3 (scale, scale, scale);
		}
	}

	public override void OnGripped(ClickedEventArgs e) {
		if (selection.selectedObject == null || doScaling)
			return;

		SelectObject (selection.selectedObject);

		// Remember how the object and controller was rotated when we started pressing the button.
		startObjectRotation = selection.transform.rotation;
		startControllerRotation = transform.rotation;

		doRotation = true;
	}

	public override void OnUngripped(ClickedEventArgs e) {
		ResetSelectionState ();
	}

	public override void OnPadClicked(ClickedEventArgs e) {
		if (selection.selectedObject == null || doRotation)
			return;

		// Make sure we have the references to the controllers.
		if (firstController == null || secondController == null)
			return;

		SelectObject(selection.selectedObject);

		// See how far apart the two controllers are right now.
		startDistance = (firstController.transform.position - secondController.transform.position).sqrMagnitude;
		startDistance = Mathf.Abs (startDistance);

		// Save the current object scale as a reference.
		startScale = selectedObject.transform.localScale;

		doScaling = true;
	}

	public override void OnPadUnclicked(ClickedEventArgs e) {
		ResetSelectionState ();
	}

	private void SelectObject(GameObject obj) {
		selectedObject = selection.selectedObject;

		// Disable physics on it.
		Rigidbody rigidbody = selectedObject.GetComponent<Rigidbody>();
		if (rigidbody) {
			oldConstraints = rigidbody.constraints;

			rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			//rigidbody.isKinematic = true;
		}
	}

	// Put the object back into the scene and enable physics
	private void ResetSelectionState() {
		if (selectedObject) {

			// Enable physics again.
			Rigidbody rigidbody = selectedObject.GetComponent<Rigidbody>();
			if (rigidbody) {
				rigidbody.constraints = oldConstraints;
				//rigidbody.isKinematic = false;
			}
		}

		selectedObject = null;
		doScaling = false;
		doRotation = false;
	}
}
