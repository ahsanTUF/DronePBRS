using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
public class DroneAgent : Agent
{
    [Header("References")]
    public Transform targetTransform;

    [Header("Flight Dynamics")]
    public float forceMultiplier = 50f;
    public float yawSpeed = 3f;
    public float yawResponse = 5f;

    [Header("Movement Efficiency")]
    [Range(0f, 1f)] public float reverseEfficiency = 1.0f;
    [Range(0f, 1f)] public float strafeEfficiency = 1.0f;

    [Header("Training Constraints")]
    public float maxDistance = 50.0f;
    public float maxEpisodeLength = 1000f;
    public bool useEasyMode = true;
    public bool useRandomRotation = false;

    [Header("Auto-Hover")]
    public bool useAutoHover = true;
    public float hoverHeight = 2.0f;
    public float ceilingHeight = 500.0f;
    public float hoverSpring = 20f;
    public float hoverDamp = 5f;

    [Header("PBRS Settings")]
    public float discountFactor = 0.99f;

    [Header("Rewards & Penalties")]
    public float successReward = 1.0f;
    public float crashPenalty = -1.0f;
    public float groundPenalty = -1f;
    public float boundaryPenalty = -1.0f;

    private Rigidbody rBody;
    private BehaviorParameters behaviorParameters;
    private DronePhysics physics;
    private DroneRewards rewards;
    private DroneObservations observations;
    private Vector3 startPos;
    private Vector3 targetStartPos;
    private float stepCount;
    private int groundHitCount;
    private ActionBuffers heuristicActions;
    private float moveStrafe, moveUp, moveForward, moveYaw;
    private Quaternion startRot;

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        physics = new DronePhysics(rBody, transform);
        rewards = new DroneRewards();
        observations = new DroneObservations(transform, rBody);
        SyncPhysicsSettings();
        SyncRewardSettings();
        startPos = transform.position;
        startRot = transform.rotation;
        heuristicActions = new ActionBuffers(behaviorParameters.BrainParameters.ActionSpec);
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (targetTransform != null) targetStartPos = targetTransform.position;
    }

    [Header("Spawn Scheduler")]
    public bool useSpawnScheduler = false;
    public float spawnDistance = 10f;
    private int spawnIndex = 0;

    public override void OnEpisodeBegin()
    {
        stepCount = 0f;
        groundHitCount = 0;
        physics.ResetPhysics();
        moveStrafe = 0f;
        moveUp = 0f;
        moveForward = 0f;
        moveYaw = 0f;

        if (useSpawnScheduler && targetTransform != null)
        {
            // Deterministic spawn pattern: Behind -> Front -> Left -> Right (Relative to Target Facing)
            Vector3 spawnOffset = Vector3.zero;
            switch (spawnIndex)
            {
                case 0: spawnOffset = -targetTransform.forward * spawnDistance; break; // Behind (-Z relative to target)
                case 1: spawnOffset = targetTransform.forward * spawnDistance; break;  // Front (+Z)
                case 2: spawnOffset = -targetTransform.right * spawnDistance; break;   // Left (-X)
                case 3: spawnOffset = targetTransform.right * spawnDistance; break;    // Right (+X)
            }

            transform.position = targetTransform.position + spawnOffset + Vector3.up * 2f; // Ensure height
            transform.rotation = startRot; // Maintain scene rotation
            spawnIndex = (spawnIndex + 1) % 4;
        }
        else
        {
            transform.position = startPos;
            float initialYaw = useRandomRotation ? Random.Range(0f, 360f) : 0f;
            transform.rotation = Quaternion.Euler(0f, initialYaw, 0f);

            if (targetTransform != null)
            {
                if (useEasyMode)
                {
                    // Target stays where dragged (no reset)
                }
                else
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-10f, 10f), 0.5f, Random.Range(10f, 30f));
                    targetTransform.position = startPos + randomOffset;
                }
            }
        }

        float initialPotential = CalculateCurrentPotential();
        rewards.ResetRewards(initialPotential);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observations.CollectObservations(sensor, targetTransform, maxDistance, forceMultiplier);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount += 1f;
        var cont = actions.ContinuousActions;
        moveStrafe = cont[0];
        moveUp = cont[1];
        moveForward = cont[2];
        moveYaw = cont[3];
        float currentPotential = CalculateCurrentPotential();
        float pbrsReward = rewards.CalculatePBRSReward(currentPotential);
        AddReward(pbrsReward);
        CheckTerminalConditions();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        if (Keyboard.current == null) return;
        cont[0] = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        cont[1] = (Keyboard.current.spaceKey.isPressed ? 1 : 0) - (Keyboard.current.leftCtrlKey.isPressed ? 1 : 0);
        cont[2] = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
        cont[3] = (Keyboard.current.eKey.isPressed ? 1 : 0) - (Keyboard.current.qKey.isPressed ? 1 : 0);
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            useAutoHover = !useAutoHover;
            physics.UseAutoHover = useAutoHover;
        }
    }

    private void FixedUpdate()
    {
        if (behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
        {
            Heuristic(heuristicActions);
            var cont = heuristicActions.ContinuousActions;
            moveStrafe = cont[0];
            moveUp = cont[1];
            moveForward = cont[2];
            moveYaw = cont[3];
        }
        SyncPhysicsSettings();
        physics.ApplyMovement(moveStrafe, moveUp, moveForward, moveYaw);
    }

    private void CheckTerminalConditions()
    {
        if (targetTransform != null)
        {
            float distance = Vector3.Distance(transform.position, targetTransform.position);
            if (distance < 2.0f)
            {
                RegisterSuccess("Proximity");
                return;
            }
            if (distance > maxDistance)
            {
                EndEpisodeWithPenalty(boundaryPenalty);
                return;
            }
        }
        if (stepCount >= maxEpisodeLength)
        {
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetTransform != null && collision.gameObject == targetTransform.gameObject)
        {
            RegisterSuccess("Collision");
            return;
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            groundHitCount++;
            if (groundHitCount >= 10)
            {
                EndEpisodeWithPenalty(groundPenalty);
            }
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            SetReward(crashPenalty);
            EndEpisode();
        }
    }

    private void RegisterSuccess(string method)
    {
        rewards.IncrementSuccessCount();
        SetReward(successReward);
        Debug.Log($"SUCCESS ({method})! Total: {rewards.GetSuccessCount()}");
        EndEpisode();
    }

    private void EndEpisodeWithPenalty(float penalty)
    {
        AddReward(penalty);
        EndEpisode();
    }

    private float CalculateCurrentPotential()
    {
        if (targetTransform == null) return 0f;
        return rewards.CalculatePotential(transform.position, targetTransform.position, maxDistance);
    }

    private void SyncPhysicsSettings()
    {
        physics.ForceMultiplier = forceMultiplier;
        physics.YawSpeed = yawSpeed;
        physics.YawResponse = yawResponse;
        physics.ReverseEfficiency = reverseEfficiency;
        physics.StrafeEfficiency = strafeEfficiency;
        physics.UseAutoHover = useAutoHover;
        physics.HoverHeight = hoverHeight;
        physics.CeilingHeight = ceilingHeight;
        physics.HoverSpring = hoverSpring;
        physics.HoverDamp = hoverDamp;
    }

    private void SyncRewardSettings()
    {
        rewards.DiscountFactor = discountFactor;
        rewards.SuccessReward = successReward;
        rewards.CrashPenalty = crashPenalty;
        rewards.GroundPenalty = groundPenalty;
        rewards.BoundaryPenalty = boundaryPenalty;
    }

    private void OnDrawGizmos()
    {
        if (targetTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetTransform.position, maxDistance);
        }
    }
}