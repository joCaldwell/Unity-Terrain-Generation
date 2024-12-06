using UnityEngine;

public class CameraMove : MonoBehaviour
{
        public float moveSpeed = 10f; // Movement speed
    public float fastMoveMultiplier = 2f; // Speed multiplier when holding Shift
    public float lookSpeed = 2f; // Mouse look sensitivity

    private float pitch = 0f; // Vertical rotation
    private float yaw = 0f; // Horizontal rotation

    private void Start()
    {
        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation
        Vector3 initialRotation = transform.rotation.eulerAngles;
        pitch = initialRotation.x;
        yaw = initialRotation.y;
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCursorToggle();
    }

    private void HandleMovement()
    {
        float moveMultiplier = Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f;

        // Get input for movement
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow
        float moveY = 0f;

        // Ascend/Descend with Q/E keys
        if (Input.GetKey(KeyCode.Q)) moveY = -1f;
        if (Input.GetKey(KeyCode.E)) moveY = 1f;

        // Move the camera
        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * moveMultiplier * Time.deltaTime;
        transform.Translate(move, Space.Self);
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        // Update pitch and yaw
        yaw += mouseX;
        pitch -= mouseY;

        // Clamp pitch to avoid flipping the camera
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleCursorToggle()
    {
        // Toggle cursor visibility with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
