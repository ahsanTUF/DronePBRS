using UnityEngine;

/// <summary>
/// Handles all physics-related functionality for the drone including movement, rotation, and auto-hover.
/// </summary>
public class DronePhysics
{
    #region -- Configuration --

    private readonly Rigidbody rigidbody;
    private readonly Transform transform;

    // Flight dynamics parameters
    public float ForceMultiplier { get; set; } = 50f;
    public float YawSpeed { get; set; } = 3f;
    public float YawResponse { get; set; } = 5f;

    // Asymmetric thrust (anti-cheese) - Lowered to force forward flight preference
    public float ReverseEfficiency { get; set; } = 0.5f;
    public float StrafeEfficiency { get; set; } = 0.5f;

    // Auto-hover settings
    public bool UseAutoHover { get; set; } = true;
    public float HoverHeight { get; set; } = 2.0f;
    public float CeilingHeight { get; set; } = 500.0f;
    public float HoverSpring { get; set; } = 20f;
    public float HoverDamp { get; set; } = 5f;

    #endregion

    #region -- Internal State --

    private float currentYawVelocity;

    #endregion

    #region -- Initialization --

    /// <summary>
    /// Initializes the DronePhysics system with required Unity components.
    /// </summary>
    /// <param name="rb">The Rigidbody component of the drone</param>
    /// <param name="trans">The Transform component of the drone</param>
    public DronePhysics(Rigidbody rb, Transform trans)
    {
        rigidbody = rb;
        transform = trans;
    }

    #endregion

    #region -- Public Methods --

    /// <summary>
    /// Resets the physics state for a new episode.
    /// </summary>
    public void ResetPhysics()
    {
        currentYawVelocity = 0f;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Applies movement and rotation forces based on input actions.
    /// </summary>
    /// <param name="strafeInput">Sideways movement input (-1 to 1)</param>
    /// <param name="upInput">Vertical movement input (-1 to 1)</param>
    /// <param name="forwardInput">Forward/backward movement input (-1 to 1)</param>
    /// <param name="yawInput">Rotation input (-1 to 1)</param>
    public void ApplyMovement(float strafeInput, float upInput, float forwardInput, float yawInput)
    {
        // Apply asymmetric thrust penalties
        strafeInput = ApplyStrafeEfficiency(strafeInput);
        forwardInput = ApplyReverseEfficiency(forwardInput);

        // Apply rotation
        ApplyRotation(yawInput);

        // Apply thrust forces
        ApplyThrust(strafeInput, upInput, forwardInput);

        // Apply auto-hover if enabled
        if (UseAutoHover)
        {
            ApplyAutoHover();
        }
    }

    #endregion

    #region -- Private Methods --

    /// <summary>
    /// Applies strafe efficiency penalty to discourage sideways movement.
    /// </summary>
    private float ApplyStrafeEfficiency(float strafeInput)
    {
        return strafeInput * StrafeEfficiency;
    }

    /// <summary>
    /// Applies reverse efficiency penalty to discourage backward movement.
    /// </summary>
    private float ApplyReverseEfficiency(float forwardInput)
    {
        if (forwardInput < 0)
        {
            return forwardInput * ReverseEfficiency;
        }
        return forwardInput;
    }

    /// <summary>
    /// Applies yaw rotation to the drone.
    /// </summary>
    private void ApplyRotation(float yawInput)
    {
        float targetYaw = yawInput * YawSpeed;
        currentYawVelocity = Mathf.Lerp(currentYawVelocity, targetYaw, YawResponse * Time.fixedDeltaTime);
        rigidbody.angularVelocity = new Vector3(0, currentYawVelocity, 0);
    }

    /// <summary>
    /// Applies thrust forces in all directions.
    /// </summary>
    private void ApplyThrust(float strafeInput, float upInput, float forwardInput)
    {
        Vector3 forces = (transform.forward * forwardInput) +
                         (transform.right * strafeInput) +
                         (Vector3.up * upInput);

        // DEBUG: Print inputs if they are non-zero
        if (forces.magnitude > 0.01f)
        {
            // Debug.Log($"Thrust Applied: {forces * ForceMultiplier} (Inputs: F={forwardInput:F2}, S={strafeInput:F2}, U={upInput:F2})");
        }
        else if (Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(upInput) > 0.1f)
        {
            Debug.LogWarning("Thrust is ZERO despite inputs! Check ForceMultiplier or Vectors.");
        }

        rigidbody.AddForce(forces * ForceMultiplier, ForceMode.Acceleration);
    }

    /// <summary>
    /// Applies automatic hover correction to maintain altitude.
    /// </summary>
    private void ApplyAutoHover()
    {
        float currentY = transform.position.y;
        float velocityDamping = rigidbody.linearVelocity.y * HoverDamp;
        float correction = 0f;
        bool applyForce = false;

        if (currentY < HoverHeight)
        {
            // Below hover height - push up
            float heightError = HoverHeight - currentY;
            correction = 9.81f + (heightError * HoverSpring) - velocityDamping;
            applyForce = true;
        }
        else if (currentY > CeilingHeight)
        {
            // Above ceiling - push down
            float heightExcess = currentY - CeilingHeight;
            correction = (-heightExcess * HoverSpring) - velocityDamping;
            applyForce = true;
        }

        if (applyForce)
        {
            rigidbody.AddForce(Vector3.up * correction, ForceMode.Acceleration);
        }
    }

    #endregion
}
