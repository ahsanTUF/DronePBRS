# DronePBRS: Project Presentation Guide

This document is a comprehensive guide for presenting the Drone Navigation project. It covers the technical architecture, the "Why" behind key decisions, the challenges faced during development, and the specific code implementations that solved them.

---

## 1. Project Overview & History

### The Goal
To create an autonomous drone agent capable of navigating a 3D environment to reach a moving target while avoiding obstacles, using **Reinforcement Learning (RL)**.

### Evolution
1.  **V1: Basic Flight**: The drone could fly but struggled with stability. It used naive standard rewards (Hit Target = +1).
2.  **V2: PBRS Implementation**: We introduced **Potential-Based Reward Shaping** to give the drone "hints" (dense rewards) without biasing the optimal policy.
3.  **V3: The "Lazy Drone" (Bias Problem)**: We discovered the drone optimized for flying backwards/sideways because it was easier than turning. We fixed this by introducing **Physics Penalties**.
4.  **V4: The "Blind Drone" (Obstacle Problem)**: The drone could find the target but crashed into walls because it only knew coordinates (GPS) but had no vision.
5.  **V5: Sighted Agent (Current)**: We added **Ray Perception Sensors** (Lidar-like vision), allowing the drone to "see" walls and navigate complex environments.

---

## 2. Technical Architecture: The "Brain" vs. The "Body"

The project uses a standard Robotics pattern: Separation of Control (AI) and Dynamics (Physics).

### The Body: [DronePhysics.cs](file:///c:/Users/user/Documents/Unity%20Projects/DronePBRS/Assets/Scripts/DronePhysics.cs)
This script acts as the "Flight Controller". It doesn't know *where* to go, it only knows *how* to fly.

**Key Feature 1: Auto-Hover (PID Loop)**
This function acts like a "Flight Assist" (similar to a DJI drone), fighting gravity automatically so the AI can focus on steering.

```csharp
// DronePhysics.cs
private void ApplyAutoHover()
{
    float currentY = transform.position.y;
    // Damping prevents oscillation (bouncing)
    float velocityDamping = rigidbody.linearVelocity.y * HoverDamp; 
    
    if (currentY < HoverHeight)
    {
        // Spring force: The lower we are, the harder we push up
        float heightError = HoverHeight - currentY;
        float correction = 9.81f + (heightError * HoverSpring) - velocityDamping;
        rigidbody.AddForce(Vector3.up * correction, ForceMode.Acceleration);
    }
}
```

**Key Feature 2: Anti-Cheese (Efficiency)**
We discovered the "Lazy Drone" bias where flying sideways was optimal. We fixed it by penalizing [StrafeEfficiency](file:///c:/Users/user/Documents/Unity%20Projects/DronePBRS/Assets/Scripts/DronePhysics.cs#94-101) to 0.5 (50%), forcing the drone to turn and fly forward (100% efficiency).

```csharp
// DronePhysics.cs
private float ApplyStrafeEfficiency(float strafeInput)
{
    // Penalty: Sideways movement provides 50% less thrust than forward movement
    return strafeInput * StrafeEfficiency; // StrafeEfficiency = 0.5f
}
```

### The Brain: [DroneAgent.cs](file:///c:/Users/user/Documents/Unity%20Projects/DronePBRS/Assets/Scripts/DroneAgent.cs)
This is the ML-Agents interface. It connects Unity to the Python PPO trainer.

**Key Feature: The Spawn Scheduler (Curriculum)**
To prevent the drone from memorizing "Go North", we force it to spawn in 4 relative quadrants. This builds a robust general policy.

```csharp
// DroneAgent.cs
if (useSpawnScheduler)
{
    // Deterministic spawn pattern: Behind -> Front -> Left -> Right
    // We use targetTransform.forward/right to rotate the spawn logic WITH the target
    switch (spawnIndex)
    {
        case 0: spawnOffset = -targetTransform.forward * spawnDistance; break; // Behind
        case 1: spawnOffset = targetTransform.forward * spawnDistance; break;  // Front
        case 2: spawnOffset = -targetTransform.right * spawnDistance; break;   // Left
        case 3: spawnOffset = targetTransform.right * spawnDistance; break;    // Right
    }
    transform.position = targetTransform.position + spawnOffset + Vector3.up * 2f;
    transform.rotation = startRot; // Keep drone rotation fixed to force it to learn turning
}
```

---

## 3. The "AI" Side: PBRS & Observations

### Why Potential-Based Reward Shaping (PBRS)?
Standard rewards are "Sparse" (you only get +1 after 1000 steps). The drone spends 99% of its time guessing.
PBRS gives a reward *every single step* based on:
> "Are you closer to the target than you were last frame?"

**The Math:** $F = \gamma \Phi(s') - \Phi(s)$
We used an **Exponential Potential** function. Unlike linear distance, this provides a strong gradient even when the drone is far away.

```csharp
// DroneRewards.cs
public float CalculatePotential(Vector3 dronePos, Vector3 targetPos, float maxDist)
{
    float distance = Vector3.Distance(dronePos, targetPos);
    
    // Normalized 0 to 1
    float normalizedDist = distance / maxDist;
    
    // Exponential decay: exp(-2 * x)
    // 0 distance = 1.0 potential (High Reward)
    // Max distance = 0.14 potential (Low Reward)
    return Mathf.Exp(-2f * normalizedDist);
}

public float CalculatePBRSReward(float currentPotential)
{
    // The "Difference" Reward
    // DiscountFactor (gamma) ensures mathematical invariance
    return (DiscountFactor * currentPotential) - lastPotential;
}
```

### The Eyes: Observations
The Neural Network Input Layer (Size 44) consists of:

**1. Proprioception (8 inputs): "Where am I?"**
We calculate direction in **Local Space** so the drone knows "Target is to my Left", regardless of global North/South.

```csharp
// DroneObservations.cs
public void CollectObservations(VectorSensor sensor, Transform target, ...)
{
    // 1. "Compass": Direction to target relative to drone's nose
    Vector3 directionToTarget = (target.position - droneTransform.position).normalized;
    sensor.AddObservation(droneTransform.InverseTransformDirection(directionToTarget)); // 3 floats

    // 2. Distance (Normalized 0-1)
    sensor.AddObservation(distance / maxDistance); // 1 float

    // 3. Local Velocity (How fast am I moving relative to my facing?)
    sensor.AddObservation(droneTransform.InverseTransformVector(rBody.linearVelocity) / maxSpeed); // 3 floats
    
    // 4. Height
    sensor.AddObservation(height / maxHeight); // 1 float
}
```

**2. Exteroception (36 inputs): "What is around me?"**
*   **Ray Perception Sensor 3D**: Throws invisible laser beams to detect warnings ("Obstacle") vs safety ("Target").
*   *Note: This is handled by a Unity Component, not C# code.*

---

## 4. Challenges & Critical Problem Solving

### Problem A: The "Brain Transplant" Crash
**Symptom**: `RuntimeError: Size of tensor a (8) must match tensor b (44)`
**Context**: We tried to transfer the "Smart Navigation" brain (Inputs: 8) to the new "Sighted" drone (Inputs: 44).
**Reason**: Neural Networks have fixed input layers. Adding "Eyes" changed the architecture.
**Solution**: We had to start a **Fresh Training Run** (`pbrs_v5_eyes`), abandoning the old weights but applying the "Flight School" curriculum to relearn quickly.

### Problem B: The "biased" Drone
**Symptom**: The drone flew sideways or backwards to the target.
**Reason**: Our physics allowed 100% speed in all directions. The AI found it "cheaper" to just strafe than to spend energy turning.
**Solution**: We adjusted [DronePhysics.cs](file:///c:/Users/user/Documents/Unity%20Projects/DronePBRS/Assets/Scripts/DronePhysics.cs) to penalize strafing (0.5 efficiency). This aligned the *Physical* optimal policy with our *Desired* visual behavior.

---

## 5. Q&A: Thinking Like an ML Expert

**Q: Why PPO (Proximal Policy Optimization)?**
A: PPO is the standard for continuous control tasks like robotics. Unlike DQN (which outputs discrete buttons like A, B, X), PPO outputs continuous values (-1.0 to 1.0) which maps perfectly to our joystick-like flight controls (Throttle, Yaw, Pitch).

**Q: How do you prevent overfitting?**
A: Two ways:
1.  **Spawn Scheduler**: We force the drone to start in different relative positions.
2.  **Dynamic Target**: In "Hard Mode", we drag the target around the arena so the drone can't memorize a single location.

**Q: Why use Ray Sensors instead of a Camera (CNN)?**
A: Cameras require massive compute (Pixels -> ConvNet -> Action). Ray Sensors give us the "Depth" information we need (Distance to wall) with a fraction of the computational cost, allowing for faster training (millions of steps in hours vs days).

---

## 6. Project Stats & Config
*   **Algorithm**: PPO
*   **Framework**: Unity ML-Agents Release 21
*   **Max Steps**: 5,000,000
*   **Hidden Units**: 256 (beefy brain for 3D physics)
*   **Time Horizon**: 256 (5+ seconds of planning)
*   *Config File Reference:* [pbrs_config.yaml](file:///c:/Users/user/Documents/Unity%20Projects/DronePBRS/pbrs_config.yaml)
