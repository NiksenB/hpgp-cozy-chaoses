using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public InputAction cameraMoveRotate;
    public InputAction cameraRotateModifier;
    public InputAction cameraZoomModifier;

    public float speed = 200f;

    private Vector2 _input;

    private void Update()
    {
        _input = cameraMoveRotate.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        var cameraTransform = Camera.main.transform;
        var rotateMod = cameraRotateModifier.IsPressed();
        var zoomMod = cameraZoomModifier.IsPressed();

        if (zoomMod)
        {
            var zoomDirection = cameraTransform.forward * _input.y;
            cameraTransform.position += speed * Time.deltaTime * zoomDirection;
        }
        else if (rotateMod)
        {
            var pitch = -_input.y * 0.5f * speed * Time.deltaTime;
            var yaw = _input.x * 0.5f * speed * Time.deltaTime;
            cameraTransform.Rotate(pitch, yaw, 0f);
        }
        else
        {
            var moveDirection = cameraTransform.rotation * _input.normalized;
            cameraTransform.position += speed * Time.deltaTime * moveDirection;
        }
    }

    private void OnEnable()
    {
        cameraMoveRotate.Enable();
        cameraRotateModifier.Enable();
        cameraZoomModifier.Enable();
    }

    private void OnDisable()
    {
        cameraMoveRotate.Disable();
        cameraRotateModifier.Disable();
        cameraZoomModifier.Disable();
    }

    public static void AdjustCameraPosition(float planetRadius)
    {
        var pos = Camera.main.transform.position;

        Camera.main.transform.position = new Vector3(pos.x, planetRadius, -0.6f * planetRadius);
        Camera.main.transform.rotation = Quaternion.LookRotation(pos - new Vector3(0f, planetRadius, 0f));
    }
}