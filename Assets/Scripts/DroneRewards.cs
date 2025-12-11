using UnityEngine;

/// <summary>
/// Manages reward calculations including PBRS (Potential-Based Reward Shaping) for the drone agent.
/// </summary>
public class DroneRewards
{
    #region Configuration

    public float DiscountFactor { get; set; } = 0.99f;
    public float SuccessReward { get; set; } = 1.0f;
    public float CrashPenalty { get; set; } = -1.0f;
    public float GroundPenalty { get; set; } = -0.1f;
    public float BoundaryPenalty { get; set; } = -1.0f;

    #endregion

    #region State

    private float lastPotential;
    private int successCount;

    #endregion

    #region Public Methods

    /// <summary>
    /// Resets the reward state for a new episode.
    /// </summary>
    public void ResetRewards(float initialPotential)
    {
        lastPotential = initialPotential;
    }

    /// <summary>
    /// Calculates the PBRS reward: F(s,s') = γΦ(s') - Φ(s)
    /// </summary>
    public float CalculatePBRSReward(float currentPotential)
    {
        float pbrsReward = (DiscountFactor * currentPotential) - lastPotential;
        lastPotential = currentPotential;
        return pbrsReward;
    }

    /// <summary>
    /// Calculates potential using exponential decay.
    /// Provides stronger gradient at all distances compared to linear.
    /// At 0m: potential = 1.0
    /// At 50% max distance: potential = ~0.37
    /// At max distance: potential = ~0.14
    /// </summary>
    public float CalculatePotential(Vector3 dronePosition, Vector3 targetPosition, float maxDistance)
    {
        float distance = Vector3.Distance(dronePosition, targetPosition);
        float normalizedDistance = distance / maxDistance;

        // Exponential potential: exp(-2 * normalized_distance)
        return Mathf.Exp(-2f * normalizedDistance);
    }

    public void IncrementSuccessCount()
    {
        successCount++;
    }

    public int GetSuccessCount()
    {
        return successCount;
    }

    #endregion
}
