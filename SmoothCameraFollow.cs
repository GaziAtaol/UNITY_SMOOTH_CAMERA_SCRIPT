using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Smooth Camera Follow script optimized for network lag compensation.
/// Attach to a camera GameObject that is a child of (or separate from) the player.
/// Works in both networked (Unity Netcode) and single-player contexts.
/// </summary>
public class SmoothCameraFollow : NetworkBehaviour
{
    [Header("Follow Settings")]
    /// <summary>The transform to follow. Auto-assigned to the parent if left empty.</summary>
    [SerializeField] private Transform target;
    /// <summary>Camera offset relative to the target's local space.</summary>
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, -3.5f);
    /// <summary>When true, the camera rotates to look directly at the target instead of matching its forward direction.</summary>
    [SerializeField] private bool lookAtTarget = false;

    [Header("Smooth Settings")]
    /// <summary>Higher values = snappier position follow.</summary>
    [Min(0.1f)]
    [SerializeField] private float positionSmoothSpeed = 10f;
    /// <summary>Higher values = snappier rotation follow. Set to 0 to skip rotation entirely.</summary>
    [Min(0f)]
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Collision Detection")]
    /// <summary>When true, the camera moves closer to the target to avoid clipping through geometry.</summary>
    [SerializeField] private bool useCollisionDetection = true;
    /// <summary>Layers the collision sphere-cast tests against.</summary>
    [SerializeField] private LayerMask collisionLayers = ~0;
    /// <summary>Radius of the sphere used for collision detection.</summary>
    [Min(0.01f)]
    [SerializeField] private float collisionRadius = 0.2f;
    /// <summary>Minimum allowed distance from the target when collision pushes the camera in.</summary>
    [Min(0f)]
    [SerializeField] private float minDistance = 0.5f;

    [Header("Prediction (Multiplayer)")]
    /// <summary>Extrapolates the target's next position to compensate for network lag.</summary>
    [SerializeField] private bool usePositionPrediction = true;
    /// <summary>How aggressively to extrapolate. 1.0 = one frame ahead, 2.0 = two frames ahead.</summary>
    [Min(0f)]
    [SerializeField] private float predictionMultiplier = 1.5f;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _lastTargetPosition;
    private bool _initialized;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void OnNetworkSpawn()
    {
        // In multiplayer, only the owning client should drive its own camera.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        Initialize();
    }

    private void Start()
    {
        // Fallback for single-player or editor preview (no NetworkManager present).
        if (!IsSpawned)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (target == null)
        {
            target = transform.parent;
        }

        if (target != null)
        {
            _lastTargetPosition = target.position;
        }

        _initialized = true;
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    private void LateUpdate()
    {
        if (!_initialized || target == null) return;

        Vector3 desiredPosition = CalculateDesiredPosition();

        Vector3 finalPosition = useCollisionDetection
            ? ApplyCollisionDetection(desiredPosition)
            : desiredPosition;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalPosition,
            ref _velocity,
            1f / positionSmoothSpeed
        );

        ApplyRotation();
    }

    // -------------------------------------------------------------------------
    // Position helpers
    // -------------------------------------------------------------------------

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 basePosition = target.position + target.TransformDirection(offset);

        // Extrapolate ahead by the movement delta from the last frame to compensate
        // for network lag. movementDelta already equals velocity * deltaTime, so no
        // divide-then-multiply is needed.
        if (usePositionPrediction)
        {
            Vector3 movementDelta = target.position - _lastTargetPosition;
            basePosition += movementDelta * predictionMultiplier;
        }

        _lastTargetPosition = target.position;
        return basePosition;
    }

    private Vector3 ApplyCollisionDetection(Vector3 desiredPosition)
    {
        Vector3 directionToCamera = desiredPosition - target.position;
        float desiredDistance = directionToCamera.magnitude;

        if (desiredDistance < 0.001f) return desiredPosition;

        if (Physics.SphereCast(
            target.position,
            collisionRadius,
            directionToCamera.normalized,
            out RaycastHit hit,
            desiredDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            float clampedDistance = Mathf.Max(hit.distance, minDistance);
            return target.position + directionToCamera.normalized * clampedDistance;
        }

        return desiredPosition;
    }

    // -------------------------------------------------------------------------
    // Rotation helpers
    // -------------------------------------------------------------------------

    private void ApplyRotation()
    {
        if (rotationSmoothSpeed <= 0f) return;

        Quaternion targetRotation;

        if (lookAtTarget)
        {
            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude < 0.001f) return;
            targetRotation = Quaternion.LookRotation(direction);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(target.forward);
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    private void OnValidate()
    {
        positionSmoothSpeed = Mathf.Max(0.1f, positionSmoothSpeed);
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
        predictionMultiplier = Mathf.Max(0f, predictionMultiplier);
        collisionRadius = Mathf.Max(0.01f, collisionRadius);
        minDistance = Mathf.Max(0f, minDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + target.TransformDirection(offset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.3f);
        Gizmos.DrawLine(target.position, targetPos);

        if (useCollisionDetection)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f); // orange
            Gizmos.DrawWireSphere(targetPos, collisionRadius);
        }
    }
}
