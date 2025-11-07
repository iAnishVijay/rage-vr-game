using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XRArmSwingMoveProvider : ActionBasedContinuousMoveProvider
{
    [Header("Arm Swing Settings")]
    public XRNode leftHandNode = XRNode.LeftHand;
    public XRNode rightHandNode = XRNode.RightHand;

    [Tooltip("Speed (m/s) reached when hand swing speed hits controllerSpeedForMaxSpeed.")]
    public float maxSpeed = 3f;
    public float controllerSpeedForMaxSpeed = 2f;

    [Tooltip("How much to smooth the detected swing motion.")]
    [Range(0f, 1f)] public float smoothing = 0.15f;

    private Vector3 leftPrevPos, rightPrevPos;
    private bool leftHasPrev, rightHasPrev;
    private float currentMoveStrength;

    protected override Vector2 ReadInput()
    {
        // --- 1. Measure how much both controllers moved this frame ---
        float swingStrength = ComputeArmSwingStrength();

        // --- 2. Map swing to a forward-only input (z-axis) ---
        Vector2 input = new Vector2(0, swingStrength);

        return input;
    }

    private float ComputeArmSwingStrength()
    {
        float leftChange = 0f, rightChange = 0f;

        // Left hand
        if (TryGetDevicePosition(leftHandNode, out Vector3 leftPos))
        {
            if (leftHasPrev)
                leftChange = Vector3.Distance(leftPrevPos, leftPos);
            leftPrevPos = leftPos;
            leftHasPrev = true;
        }

        // Right hand
        if (TryGetDevicePosition(rightHandNode, out Vector3 rightPos))
        {
            if (rightHasPrev)
                rightChange = Vector3.Distance(rightPrevPos, rightPos);
            rightPrevPos = rightPos;
            rightHasPrev = true;
        }

        // Combine both hands’ movement
        float avgChange = (leftChange + rightChange) * 0.5f;

        // Convert to “speed per second”
        float speedPerSec = avgChange / Time.deltaTime;
        float t = Mathf.Clamp01(speedPerSec / controllerSpeedForMaxSpeed);

        // Curve → forward input
        float target = Mathf.Lerp(0, 1, t);

        // Smooth so it feels natural
        currentMoveStrength = Mathf.Lerp(currentMoveStrength, target, 1 - Mathf.Exp(-10f * smoothing * Time.deltaTime));

        return currentMoveStrength;
    }

    private bool TryGetDevicePosition(XRNode node, out Vector3 pos)
    {
        InputDevice dev = InputDevices.GetDeviceAtXRNode(node);
        if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.devicePosition, out pos))
            return true;
        pos = Vector3.zero;
        return false;
    }
}
