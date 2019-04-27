using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

	[SerializeField]
	public float sensitivity = 5.0f;
	[SerializeField]
	public float smoothing = 2.0f;
	public GameObject player;

    public bool canRotate = true;

	public Vector2 mouseLook;
    public Vector2 xAngleRange = new Vector2(-20, 20);
    public Vector2 yAngleRange = new Vector2(-40, 20);

    private Vector2 smoothV;

	void Start () {
		Cursor.lockState = CursorLockMode.Locked;

		player = transform.parent.gameObject;
        mouseLook.x = -player.transform.localRotation.eulerAngles.y;
        mouseLook.y = -player.transform.localRotation.eulerAngles.x;
    }
	
	void Update () {
		var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
		mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity * smoothing, sensitivity * smoothing));

		smoothV.x = Mathf.Lerp(smoothV.x, mouseDelta.x, 1f / smoothing);
		smoothV.y = Mathf.Lerp(smoothV.y, mouseDelta.y, 1f / smoothing);

        mouseLook += smoothV;
        if (!canRotate) {
            mouseLook.x = Mathf.Clamp(mouseLook.x, xAngleRange.x, xAngleRange.y);
        }
        mouseLook.y = Mathf.Clamp(mouseLook.y, yAngleRange.x, yAngleRange.y);

        transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        player.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, Vector3.up);

		if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0)) {
			Cursor.lockState = CursorLockMode.Locked;
		}

		if (Input.GetKeyDown("escape")) {
			Cursor.lockState = CursorLockMode.None;
		}
	}
}