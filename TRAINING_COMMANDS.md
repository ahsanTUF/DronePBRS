# ML-Agents Training Commands Reference

## Training Commands

### Start New Training
```bash
mlagents-learn pbrs_config.yaml --run-id=pbrs_v1
```

**Note:** Curriculum learning is configured in the `pbrs_config.yaml` file. To disable it, comment out the `curriculum:` section in the config.



### Resume Training (Continue from Checkpoint)
```bash
mlagents-learn pbrs_config.yaml --run-id=pbrs_v1 --resume
```

### Force Overwrite Existing Training
```bash
mlagents-learn pbrs_config.yaml --run-id=pbrs_v1 --force
```

### Training with Inference (See Agent in Unity While Training)
```bash
mlagents-learn pbrs_config.yaml --run-id=pbrs_v1 --inference
```

---

## TensorBoard Monitoring

### View Training Graphs (Default Port 6006)
```bash
tensorboard --logdir results/
```

### View Training on Custom Port
```bash
tensorboard --logdir results/ --port 6007
```

### View Specific Run Only
```bash
tensorboard --logdir results/pbrs_v1
```

**Open in browser:** `http://localhost:6006`

---

## File Management

### List All Training Runs
```bash
# Windows PowerShell
dir results/

# Windows CMD
dir results\
```

### Delete a Specific Run
```bash
# Windows PowerShell
Remove-Item -Recurse -Force results/pbrs_v1

# Windows CMD
rmdir /s /q results\pbrs_v1
```

### Backup a Training Run
```bash
# Windows PowerShell
Copy-Item -Recurse results/pbrs_v1 results/pbrs_v1_backup

# Windows CMD
xcopy /E /I results\pbrs_v1 results\pbrs_v1_backup
```

---

## Model Export & Testing

### Export Model (Automatic)
Models are automatically saved to:
```
results/pbrs_v1/DroneBehaviour/DroneBehaviour.onnx
```

### Copy Model to Unity Project
```bash
# Windows PowerShell
Copy-Item results/pbrs_v1/DroneBehaviour/DroneBehaviour.onnx "Assets/Models/"

# Windows CMD
copy results\pbrs_v1\DroneBehaviour\DroneBehaviour.onnx Assets\Models\
```

### Test Model in Unity
1. In Unity, select your drone GameObject
2. In **Behavior Parameters** component:
   - Set **Behavior Type** to "Inference Only"
   - Drag the `.onnx` file into the **Model** field
3. Press Play to see the trained agent

---

## Training Progress Tracking

### Key TensorBoard Metrics

| Metric | What to Watch | Good Trend |
|--------|---------------|------------|
| **Environment/Cumulative Reward** | Overall episode performance | ↑ Upward (toward +1.0) |
| **Policy/Extrinsic Reward** | PBRS signal strength | Visible (0.01-0.05 range) |
| **Environment/Episode Length** | Time to reach target | ↓ Downward (faster) |
| **Losses/Policy Loss** | Network learning stability | ↓ Decrease then stabilize |
| **Policy/Entropy** | Exploration vs exploitation | High early, decrease later |
| **Policy/Learning Rate** | Current learning rate | Linear decay to 0 |

### Expected Performance Milestones

| Steps | Expected Success Rate | Notes |
|-------|----------------------|-------|
| 0-50k | 0-10% | Random exploration → basic navigation |
| 50-200k | 10-50% | Learning efficient paths |
| 200-500k | 50-80% | Optimizing movement |
| 500k+ | 80%+ | Mastery |

---

## Common Workflows

### Workflow 1: Start Training from Scratch
```bash
# Terminal 1: Start training
mlagents-learn pbrs_config.yaml --curriculum=pbrs_curriculum.yaml --run-id=pbrs_v1

# Terminal 2: Monitor with TensorBoard
tensorboard --logdir results/
```

### Workflow 2: Resume After Crash
```bash
mlagents-learn pbrs_config.yaml --run-id=pbrs_v1 --resume
```

### Workflow 3: Experiment with New Config
```bash
# Copy old run for comparison
Copy-Item -Recurse results/pbrs_v1 results/pbrs_v1_old

# Start new experiment
mlagents-learn pbrs_config_v2.yaml --run-id=pbrs_v2

# Compare in TensorBoard
tensorboard --logdir results/
```

---

## Troubleshooting Commands

### Check ML-Agents Installation
```bash
mlagents-learn --help
```

### Check TensorBoard Installation
```bash
tensorboard --version
```

### Kill Stuck TensorBoard Process
```bash
# Windows: Find and kill process on port 6006
netstat -ano | findstr :6006
taskkill /PID <PID_NUMBER> /F
```

### Clear Unity ML-Agents Ports (If Training Won't Start)
```bash
# Unity uses port 5004 by default
netstat -ano | findstr :5004
taskkill /PID <PID_NUMBER> /F
```

---

## Training Stages

### Stage 1: Initial Training (0-200k steps)
**Settings:**
- `useEasyMode = true` (fixed target)
- `useRandomRotation = false` (fixed spawn direction)
- `reverseEfficiency = 1.0`
- `strafeEfficiency = 1.0`

**Command:**
```bash
mlagents-learn pbrs_config.yaml --run-id=stage1_basic
```

### Stage 2: Add Rotation Randomness (200-400k steps)
**Settings:**
- `useEasyMode = true`
- `useRandomRotation = true` ← Enable this
- `reverseEfficiency = 1.0`
- `strafeEfficiency = 1.0`

**Command:**
```bash
mlagents-learn pbrs_config.yaml --run-id=stage2_rotation --resume
```

### Stage 3: Random Target + Efficient Movement (400k+ steps)
**Settings:**
- `useEasyMode = false` ← Enable hard mode
- `useRandomRotation = true`
- `reverseEfficiency = 0.2` ← Reduce to 20%
- `strafeEfficiency = 0.5` ← Reduce to 50%

**Command:**
```bash
mlagents-learn pbrs_config.yaml --run-id=stage3_mastery --resume
```

---

## Quick Reference Card

```bash
# START TRAINING
mlagents-learn pbrs_config.yaml --run-id=NAME

# RESUME TRAINING
mlagents-learn pbrs_config.yaml --run-id=NAME --resume

# VIEW GRAPHS
tensorboard --logdir results/

# EXPORT MODEL (copy from results to Unity)
Copy-Item results/NAME/DroneBehaviour/DroneBehaviour.onnx Assets/Models/
```
