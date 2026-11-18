using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Birinci şahıs: WASD ile yürür, fare ile bakar. Space zıplar, Esc imleci serbest bırakır.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4.5f;
    [SerializeField] float lookSensitivity = 0.12f;
    [SerializeField] float jumpHeight = 1.1f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float eyeHeight = 0.7f;
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch = 85f;

    CharacterController controller;
    Camera playerCamera;
    float yaw;
    float pitch;
    float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            controller.height = capsule.height;
            controller.radius = capsule.radius;
            controller.center = capsule.center;
            capsule.enabled = false;
        }

        controller.slopeLimit = 50f;
        controller.stepOffset = 0.35f;
        controller.skinWidth = 0.08f;

        MeshRenderer body = GetComponent<MeshRenderer>();
        if (body != null)
            body.enabled = false;

        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(transform, false);
            playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        yaw = transform.eulerAngles.y;
        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();
        Look();
        Move();
    }

    void HandleCursor()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            LockCursor(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);
    }

    void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * lookSensitivity;
        pitch -= delta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Move()
    {
        Vector2 input = ReadMoveInput();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        Keyboard keyboard = Keyboard.current;
        if (controller.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        return input;
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
