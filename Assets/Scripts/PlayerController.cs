using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] float capsuleRadius = 0.18f;
    [SerializeField] float capsuleHeight = 1.2f;
    [SerializeField] float crouchHeight = 0.9f;
    [SerializeField] float cameraNearClip = 0.05f;
    [SerializeField] float cameraWallBuffer = 0.08f;
    [SerializeField] float stepOffset = 0.22f;
    [SerializeField] float cameraSmoothTime = 0.14f;
    [SerializeField] float stairClimbSpeed = 1.6f;

    CharacterController controller;
    Camera playerCamera;
    float yaw;
    float pitch;
    float verticalVelocity;
    float cameraHeightFromFeet;
    float smoothedCameraY;
    float cameraYVelocity;
    bool cameraReady;
    readonly Collider[] overlapBuffer = new Collider[12];
    readonly RaycastHit[] castBuffer = new RaycastHit[12];

    void Awake()
    {
        transform.localScale = Vector3.one;

        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            capsule.enabled = false;

        controller.height = capsuleHeight;
        controller.radius = capsuleRadius;
        controller.center = Vector3.zero;
        controller.slopeLimit = 70f;
        controller.stepOffset = Mathf.Min(stepOffset, capsuleRadius);
        controller.skinWidth = 0.06f;
        controller.minMoveDistance = 0f;

        float maxEye = capsuleHeight * 0.5f - cameraNearClip - 0.05f;
        eyeHeight = Mathf.Clamp(eyeHeight, 0.35f, maxEye);
        cameraHeightFromFeet = eyeHeight + capsuleHeight * 0.5f;

        MeshRenderer body = GetComponent<MeshRenderer>();
        if (body != null)
            body.enabled = false;

        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.nearClipPlane = cameraNearClip;
            playerCamera.transform.SetParent(transform, false);
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
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked && !IsPointerOverUi())
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
        if (playerCamera == null)
            return;

        float feetY = transform.position.y - controller.height * 0.5f;
        Vector3 desiredWorld = transform.position;
        desiredWorld.y = feetY + cameraHeightFromFeet;
        desiredWorld -= transform.forward * 0.08f;
        desiredWorld = PushCameraOutOfWalls(desiredWorld);

        if (!cameraReady)
        {
            smoothedCameraY = desiredWorld.y;
            cameraReady = true;
        }
        else
        {
            smoothedCameraY = Mathf.SmoothDamp(
                smoothedCameraY,
                desiredWorld.y,
                ref cameraYVelocity,
                cameraSmoothTime);
        }

        desiredWorld.y = smoothedCameraY;
        playerCamera.transform.position = desiredWorld;
        playerCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    Vector3 PushCameraOutOfWalls(Vector3 cameraPosition)
    {
        float probeRadius = cameraNearClip + cameraWallBuffer;
        Vector3 look = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        if (Physics.SphereCast(
                cameraPosition,
                cameraNearClip,
                look,
                out RaycastHit lookHit,
                probeRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)
            && !IsSelf(lookHit.collider))
        {
            cameraPosition -= look * (probeRadius - lookHit.distance + 0.02f);
        }

        Vector3 fromBody = cameraPosition - transform.position;
        float bodyDistance = fromBody.magnitude;
        if (bodyDistance > 0.01f
            && Physics.SphereCast(
                transform.position,
                probeRadius,
                fromBody / bodyDistance,
                out RaycastHit bodyHit,
                bodyDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)
            && !IsSelf(bodyHit.collider))
        {
            cameraPosition = transform.position + fromBody / bodyDistance * Mathf.Max(0f, bodyHit.distance - 0.02f);
        }

        return cameraPosition;
    }

    bool IsSelf(Collider hit)
    {
        return hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform));
    }

    void Move()
    {
        FitCapsuleToCeiling();

        Vector2 input = ReadMoveInput();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        bool jumping = false;
        Keyboard keyboard = Keyboard.current;
        if (controller.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumping = true;
        }

        float stepDelta = 0f;
        if (!jumping && direction.sqrMagnitude > 0.01f)
            stepDelta = ProbeStepHeight(direction);

        if (!jumping && stepDelta < -0.02f && stepDelta > -0.5f)
            verticalVelocity = Mathf.Min(verticalVelocity, -2.5f - Mathf.Abs(stepDelta) * 6f);
        else if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontal = direction * moveSpeed;
        Vector3 motion = horizontal + Vector3.up * verticalVelocity;

        if (!jumping && stepDelta > 0.02f && stepDelta <= stepOffset + 0.05f)
            motion.y = Mathf.Max(motion.y, Mathf.Min(stepDelta / Time.deltaTime, stairClimbSpeed));

        controller.Move(motion * Time.deltaTime);
    }

    float ProbeStepHeight(Vector3 direction)
    {
        float radius = controller.radius;
        float feetY = transform.position.y - controller.height * 0.5f;
        Vector3 probe = transform.position + direction.normalized * (radius + 0.06f);
        probe.y = transform.position.y + 0.35f;

        int hits = Physics.SphereCastNonAlloc(
            probe,
            radius * 0.45f,
            Vector3.down,
            castBuffer,
            0.9f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float groundY = float.NegativeInfinity;
        for (int i = 0; i < hits; i++)
        {
            if (IsSelf(castBuffer[i].collider))
                continue;
            if (castBuffer[i].normal.y < 0.4f)
                continue;
            groundY = Mathf.Max(groundY, castBuffer[i].point.y);
        }

        if (groundY > float.NegativeInfinity)
            return groundY - feetY;
        return 0f;
    }

    void FitCapsuleToCeiling()
    {
        float radius = Mathf.Max(0.05f, controller.radius * 0.85f);
        Vector3 origin = transform.position;
        float standHalf = capsuleHeight * 0.5f;
        float targetHeight = capsuleHeight;

        int ceilingHits = Physics.SphereCastNonAlloc(
            origin,
            radius,
            Vector3.up,
            castBuffer,
            standHalf + 0.12f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < ceilingHits; i++)
        {
            if (IsSelf(castBuffer[i].collider) || castBuffer[i].normal.y > -0.35f)
                continue;
            nearest = Mathf.Min(nearest, castBuffer[i].distance);
        }

        if (nearest < standHalf - 0.02f)
        {
            float allowedHalf = nearest - 0.05f;
            targetHeight = Mathf.Clamp(allowedHalf * 2f, crouchHeight, capsuleHeight);
        }

        Vector3 head = origin + Vector3.up * (controller.height * 0.5f - radius);
        int hits = Physics.OverlapSphereNonAlloc(
            head,
            radius,
            overlapBuffer,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++)
        {
            if (!IsSelf(overlapBuffer[i]))
            {
                targetHeight = Mathf.Min(targetHeight, Mathf.Max(crouchHeight, controller.height - 0.08f));
                break;
            }
        }

        float heightSpeed = targetHeight < controller.height - 0.02f ? 1.2f : 0.7f;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, heightSpeed * Time.deltaTime);
        controller.center = Vector3.zero;
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

    static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
