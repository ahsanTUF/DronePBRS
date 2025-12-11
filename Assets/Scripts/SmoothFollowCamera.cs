using UnityEngine;

/// <summary>
/// A robust Chase Camera that follows the target's rotation.
/// Keeps the view behind the drone ("Need for Speed" style).
/// </summary>
public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    [Tooltip("Height above the drone.")]
    public float height = 3.0f;
    [Tooltip("Distance behind the drone.")]
    public float distance = 6.0f;

    [Tooltip("How fast the camera moves to its spot (Higher = Snappier).")]
    public float followSpeed = 10f;
    [Tooltip("How fast the camera rotates to match drone (Lower = Smoother).")]
    public float rotationSpeed = 5f;

    // Internal smoothing velocity
    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (!target) return;

        // 1. Calculate Desired Rotation
        // We look at the drone's Yaw (Y-axis), but ignore its Pitch/Roll to stay level.
        float wantedRotationAngle = target.eulerAngles.y;
        float currentRotationAngle = transform.eulerAngles.y;

        // Smoothly interpolate the angle
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationSpeed * Time.deltaTime);
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // 2. Calculate Desired Position
        // Start at target, move back by 'distance' at the calculated angle, then add height.
        Vector3 desiredPosition = target.position;
        desiredPosition -= currentRotation * Vector3.forward * distance;
        desiredPosition.y = target.position.y + height;

        // 3. Apply Position Smoothing (Removes Jitter)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1.0f / followSpeed);

        // 4. Look at Target
        // Look slightly above the drone to keep it centered
        transform.LookAt(target.position + Vector3.up * 1.0f);
    }
}