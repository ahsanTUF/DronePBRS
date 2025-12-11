# DronePBRS - Semester Project

## Project Overview

**DronePBRS** is a semester project focused on training an autonomous drone agent to navigate towards a target using **Deep Reinforcement Learning (DRL)**. The project utilizes **Unity ML-Agents** and implements **Potential-Based Reward Shaping (PBRS)** to improve training efficiency and convergence speed.

The goal of the agent is to fly from a starting position to a target position while avoiding obstacles and the ground, maintaining stability, and optimizing its flight path.

## Key Features

*   **ML-Agents Integration**: Uses Unity's ML-Agents toolkit for training via Proximal Policy Optimization (PPO).
*   **Potential-Based Reward Shaping (PBRS)**: Implements PBRS with an exponential potential function to provide dense, policy-invariant rewards that guide the agent effectively.
*   **Physics-Based Movement**: The drone operates using realistic physics forces (thrust, torque) rather than kinematic translation.
*   **Modular Architecture**: Code is separated into distinct responsibilities (Agent, Physics, Rewards, Observations).
*   **Curriculum Learning Support**: Includes an "Easy Mode" for initial training phases.

## Project Structure & Scripts

The core logic is located in `Assets/Scripts/`. Here is a detailed breakdown of the components:

### 1. `DroneAgent.cs`
The main entry point inheriting from `Agent`. It coordinates the interaction between the environment and the neural network.

*   **`OnEpisodeBegin()`**: Resets the drone and target positions. Initializes PBRS potential.
*   **`CollectObservations(VectorSensor sensor)`**: Delegates observation collection to `DroneObservations`.
*   **`OnActionReceived(ActionBuffers actions)`**: Receives actions from the NN, applies them via `DronePhysics`, calculates PBRS rewards via `DroneRewards`, and checks for terminal conditions (success/crash).
*   **`Heuristic(in ActionBuffers actionsOut)`**: Provides manual keyboard control for testing (WASD + Space/Ctrl + Q/E).

### 2. `DronePhysics.cs`
Handles the rigid-body physics and movement dynamics.

*   **`ApplyMovement(...)`**: Main method to apply forces based on neural network outputs.
*   **`ApplyThrust(...)`**: Applies directional force (Forward/Right/Up).
*   **`ApplyRotation(...)`**: Applies yaw torque.
*   **`ApplyAutoHover()`**: A stabilizing force to keep the drone within a specific height range (anti-gravity).
*   **`ApplyStrafeEfficiency` / `ApplyReverseEfficiency`**: Penalties to encourage forward flight over strafing or flying backward.

### 3. `DroneRewards.cs`
Encapsulates the reward logic, specifically the PBRS implementation.

*   **`CalculatePBRSReward(float currentPotential)`**: Computes the shaping reward $F = \gamma \Phi(s') - \Phi(s)$.
*   **`CalculatePotential(...)`**: Defines the potential function $\Phi(s)$. This project uses an exponential decay function based on distance to the target, providing a stronger gradient than simple linear distance.
*   **`ResetRewards(...)`**: Resets state for the new episode.

### 4. `DroneObservations.cs`
Manages the data fed into the neural network. Collects 8 normalized observations:
1.  **Direction to Target** (3 floats): Vector pointing to target (local space).
2.  **Distance to Target** (1 float): Normalized distance.
3.  **Velocity** (3 floats): Drone's linear velocity (local space).
4.  **Height** (1 float): Normalized height.

### 5. `SmoothFollowCamera.cs`
A utility script attached to the Main Camera to follow the drone smoothly.
*   **`LateUpdate()`**: Calculates the desired position behind the drone and smoothly interpolates the camera's position and rotation to avoid jitter.

### 6. `pbrs_config.yaml`
The configuration file for the ML-Agents trainer.
*   **Trainer**: PPO (Proximal Policy Optimization).
*   **Hyperparameters**: Tuned for stability (Batch Size: 1024, Learning Rate: 2e-4).
*   **Network**: 3 layers, 256 hidden units each.
*   **Time Horizon**: 256 steps.

## How It Works

1.  **Initialization**: The `DroneAgent` initializes helper classes (`Physics`, `Rewards`, `Observations`).
2.  **Observation**: Every step, the agent observes its relative position and velocity regarding the target.
3.  **Decision**: The PPO policy (or Heuristic) outputs continuous actions for movement (Strafe, Up, Forward, Yaw).
4.  **Action**: `DronePhysics` translates these actions into physical forces applied to the drone's Rigidbody.
5.  **Reward**:
    *   **Extrinsic**: +1 for reaching the target, -1 for crashing/boundary.
    *   **Shaping (PBRS)**: A continuous reward based on the change in potential (getting closer/further from target).
6.  **Loop**: This cycle repeats until the episode ends (Success, Crash, or Max Steps).

## Getting Started

### Prerequisites
*   Unity 2022.3 or later.
*   ML-Agents package installed in Unity.
*   Python environment with `mlagents` installed.

### Training
To start training the agent, open a terminal in the project root and run:

```bash
mlagents-learn pbrs_config.yaml --run-id=DronePBRS_01
```

Then press **Play** in the Unity Editor.

### Inference (Testing)
1.  Locate the trained `.onnx` file in the `results/` folder.
2.  Assign it to the **Model** field in the `Behavior Parameters` component on the Drone.
3.  Set **Behavior Type** to `Inference Only`.
4.  Press **Play**.

### Manual Control
Set **Behavior Type** to `Heuristic Only` to fly the drone manually:
*   **W/S**: Forward/Backward
*   **A/D**: Strafe Left/Right
*   **Space/Ctrl**: Up/Down
*   **Q/E**: Yaw Left/Right
*   **T**: Toggle Auto-Hover
