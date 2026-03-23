using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Smooth Camera Follow - Network lag için optimize edilmiş
/// CameraPivot yerine bu script'i kullanın (opsiyonel)
/// </summary>
public class SmoothCameraFollow : NetworkBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target; // Player root
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, -3.5f);
    
    [Header("Smooth Settings")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Header("Prediction (Multiplayer)")]
    [SerializeField] private bool usePositionPrediction = true;
    [SerializeField] private float predictionMultiplier = 1.5f;
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 lastTargetPosition;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        
        // Target'ı otomatik bul (eğer atanmadıysa)
        if (target == null)
        {
            target = transform.parent;
        }
        
        if (target != null)
        {
            lastTargetPosition = target.position;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Hedef pozisyon hesapla
        Vector3 targetPosition = CalculateTargetPosition();
        
        // Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            1f / positionSmoothSpeed
        );
        
        // Rotation (opsiyonel - eğer camera parent rotation takip etmiyorsa)
        if (rotationSmoothSpeed > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target.forward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
    }
    
    private Vector3 CalculateTargetPosition()
    {
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        
        // Position prediction (network lag compensation)
        if (usePositionPrediction)
        {
            Vector3 targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
            desiredPosition += targetVelocity * Time.deltaTime * predictionMultiplier;
        }
        
        lastTargetPosition = target.position;
        
        return desiredPosition;
    }
    
    // Gizmos
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        Gizmos.color = Color.yellow;
        Vector3 targetPos = target.position + target.TransformDirection(offset);
        Gizmos.DrawWireSphere(targetPos, 0.3f);
        Gizmos.DrawLine(target.position, targetPos);
    }
}
