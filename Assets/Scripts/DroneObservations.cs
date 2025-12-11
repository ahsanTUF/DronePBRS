using UnityEngine;
using Unity.MLAgents.Sensors;

/// <summary>
/// Handles observation collection for the drone agent.
/// Provides directional guidance and normalized state information to the neural network.
/// </summary>
public class DroneObservations
{
    private Transform droneTransform;
    private Rigidbody rBody;

    public DroneObservations(Transform transform, Rigidbody rigidbody)
    {
        droneTransform = transform;
        rBody = rigidbody;
    }

    /// <summary>
    /// Collects 8 normalized observations for the neural network:
    /// - Direction to target (3 values, local space)
    /// - Distance to target (1 value, normalized)
    /// - Velocity (3 values, normalized, local space)
    /// - Height (1 value, normalized)
    /// </summary>
    public void CollectObservations(VectorSensor sensor, Transform target, float maxDistance, float maxForce)
    {
        if (target == null)
        {
            sensor.AddObservation(Vector3.zero); // Direction
            sensor.AddObservation(0f);            // Distance
            sensor.AddObservation(Vector3.zero); // Velocity
            sensor.AddObservation(0f);            // Height
            return;
        }

        // Direction to target (normalized vector in drone's local space)
        // This acts as a "compass" pointing toward the target
        Vector3 directionToTarget = (target.position - droneTransform.position).normalized;
        sensor.AddObservation(droneTransform.InverseTransformDirection(directionToTarget));

        // Distance to target (normalized 0-1)
        float distance = Vector3.Distance(droneTransform.position, target.position);
        sensor.AddObservation(Mathf.Clamp01(distance / maxDistance));

        // Velocity (normalized, in drone's local space)
        float maxSpeed = maxForce * 0.2f; // Estimated max speed
        sensor.AddObservation(droneTransform.InverseTransformVector(rBody.linearVelocity) / maxSpeed);

        // Height (normalized)
        sensor.AddObservation(droneTransform.position.y / maxDistance);
    }
}
