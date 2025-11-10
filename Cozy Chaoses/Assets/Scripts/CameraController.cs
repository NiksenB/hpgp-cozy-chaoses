using UnityEngine.InputSystem;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public InputAction cameraMoveRotate;
    public InputAction cameraRotateModifier;
    public InputAction cameraZoomModifier;

    public float speed = 100f;

    private Vector2 input;

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

    void Update()
    {
        input = cameraMoveRotate.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        Transform camera = Camera.main.transform;
        bool rotateMod = cameraRotateModifier.IsPressed();
        bool zoomMod = cameraZoomModifier.IsPressed();

        if (zoomMod)
        {
            Vector3 zoomDirection = camera.forward * input.y;
            camera.position += speed * Time.deltaTime * zoomDirection;      }
        else if (rotateMod)
        {
            float pitch = -input.y * speed * Time.deltaTime;
            float yaw = input.x * speed * Time.deltaTime;
            camera.Rotate(pitch, yaw, 0f);
        }
        else
        {
            Vector3 moveDirection = camera.rotation * input.normalized;
            camera.position += speed * Time.deltaTime * moveDirection;
        }
    }
}
