# Visual Upgrade Guide: Night Mode & Neon

To transform your simulator from a basic lab into a "Cyberpunk Drone Arena", follow these steps in the Unity Editor.

## 1. Creating "Night Mode" 🌑
1.  **Select the Directional Light** (The Sun) in your Hierarchy.
2.  **Inspector:**
    *   **Color:** Change from White/Yellow to a dark Blue/Purple (e.g., `#181840`).
    *   **Intensity:** Lower it to `0.2` or `0.5`. (Don't make it pitch black, or the Ray Sensors might look weird in debug).
3.  **Lighting Settings:**
    *   Go to **Window > Rendering > Lighting**.
    *   **Environment Tab**: Change "Source" to **Color**.
    *   **Ambient Color:** Set to a very dark gray/blue (`#050510`).

## 2. Making the Target GLOW (Emission) ✨
1.  **Create Material:** Right-Click in Project -> Create -> Material. Name it `Mat_NeonTarget`.
2.  **Inspector:**
    *   **Albedo:** Black.
    *   **Emission:** CHECK this box.
    *   **Emission Color:** Click the color box. Choose a bright **Cyan**, **Magenta**, or **Lime Green**.
    *   **Intensity:** You will see an "Intensity" slider or an input field next to the color. Set it to **3.0** or higher (HDR).
3.  **Apply:** Drag this material onto your **Target Sphere**.

## 3. Adding the "Bloom" (The Secret Sauce) 🌟
The "Glow" won't look blurry/soft unless you add **Bloom**.
*   *Note: Assuming you are using the Built-in Pipeline or URP (default usually).*

1.  **Hierarchy:** Right-Click -> Volume -> **Global Volume**.
2.  **Inspector:**
    *   **Profile:** Click "New".
    *   Click **Add Override** -> **Post-processing** -> **Bloom**.
3.  **Bloom Settings:**
    *   **Threshold:** `0.8` (Only bright things glow).
    *   **Intensity:** `1.5` to `2.0` (Adjust until it looks cool).
    *   **Scatter:** `0.7`.
4.  **Main Camera:**
    *   Select your Main Camera.
    *   Ensure **Post Processing** checkbox is TICKED.

## 4. Building the "Arena" (Ambient Lights) 💡
1.  **Create Empty Object:** Name it `NeonLights`.
2.  **Add Point Lights:**
    *   Right-Click `NeonLights` -> Light -> **Point Light**.
    *   Color: Hot Pink or Electric Blue.
    *   Range: `10`.
    *   Intensity: `2` or `3`.
3.  **Duplicate:** Move them around the corners of your room/obstacles.

Now your training session will look like a sci-fi movie! 🎥
