using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public InputAction cameraMoveRotate;
    public InputAction cameraRotateModifier;
    public InputAction cameraZoomModifier;

    public float speed = 100f;

    private Vector2 input;

    private void Update()
    {
        input = cameraMoveRotate.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        var camera = Camera.main.transform;
        var rotateMod = cameraRotateModifier.IsPressed();
        var zoomMod = cameraZoomModifier.IsPressed();

        if (zoomMod)
        {
            var zoomDirection = camera.forward * input.y;
            camera.position += speed * Time.deltaTime * zoomDirection;
        }
        else if (rotateMod)
        {
            var pitch = -input.y * speed * Time.deltaTime;
            var yaw = input.x * speed * Time.deltaTime;
            camera.Rotate(pitch, yaw, 0f);
        }
        else
        {
            var moveDirection = camera.rotation * input.normalized;
            camera.position += speed * Time.deltaTime * moveDirection;
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