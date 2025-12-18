using UnityEngine;

/// <summary>
/// Handles the visual rotation of drone rotors based on flight input.
/// Adds realism without complicating the physics/training loop.
/// </summary>
public class DroneAnimator : MonoBehaviour
{
    [Header("Rotor Setup")]
    public Transform[] rotors; // Drag your 3 rotor meshes here (Blade_1, Blade_2, Blade_3)
    public float baseSpeed = 1000f; // Idle spin speed
    public float maxSpeedMultiplier = 2000f; // Additional speed when thrusting

    [Header("Fan Animation")]
    public Transform fanBlade; // Optional: If you have a separate cooling fan
    public Vector3 fanAxis = Vector3.forward;

    [Header("Reference")]
    public DroneAgent agent; // Reference to the main agent to read inputs

    [Header("Visual Tilt")]
    public Transform visualModel; // Assign the "AI_Drone" child object here
    public float maxTiltAngle = 30f;
    public float tiltSpeed = 5f;

    private void Update()
    {
        if (agent == null) return;
        var rb = agent.GetComponent<Rigidbody>();
        if (rb == null) return;

        // 1. Calculate Rotation Speed based on Agent's effort
        float velocityMagnitude = rb.linearVelocity.magnitude;
        float currentSpeed = baseSpeed + (velocityMagnitude * 50f);

        // 2. Rotate Main Rotors
        foreach (var rotor in rotors)
        {
            if (rotor != null) rotor.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
        }

        // 3. Rotate Cooling Fan
        if (fanBlade != null)
        {
            fanBlade.Rotate(fanAxis, currentSpeed * 2f * Time.deltaTime);
        }

        // 4. Visual Tilting (The "Fake" Aerodynamics)
        if (visualModel != null)
        {
            // Convert global velocity to local space to know "forward" vs "right" speed
            Vector3 localVel = agent.transform.InverseTransformDirection(rb.linearVelocity);

            // Calculate target tilt angles
            // Moving Forward (+) -> Tilt Forward (Positive X rotation? No, usually +X is pitch down/up depends on axes)
            // Let's assume: Forward = Pitch Down (+X), Right = Roll Right (-Z) or similar.
            // Standard Unity: +Z is forward. To dip nose, rotate around X. 
            // Forward Speed (+Z) -> needs to rotate X positive (nose down).
            // Right Speed (+X) -> needs to rotate Z negative (bank right).

            float targetPitch = localVel.z * 2f; // Scale factor
            float targetRoll = -localVel.x * 2f;

            // Clamp angles
            targetPitch = Mathf.Clamp(targetPitch, -maxTiltAngle, maxTiltAngle);
            targetRoll = Mathf.Clamp(targetRoll, -maxTiltAngle, maxTiltAngle);

            // Apply smooth rotation to the visual model ONLY
            Quaternion targetRotation = Quaternion.Euler(targetPitch, 0, targetRoll);
            // Note: We keep Y=0 because Y rotation (Yaw) is handled by the Root Agent

            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
        }
    }
}
