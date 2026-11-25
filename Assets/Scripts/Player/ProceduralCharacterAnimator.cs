using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Production-quality Humanoid Stealth Animation System for "Gardien Furtif" (Unity 6.3 LTS).
/// Designed specifically for glTFast-imported Mixamo GLB characters in T-Pose.
///
/// Key Features:
/// 1. Inspects bone hierarchy and measures real bind-pose bone vectors (never guesses local Euler axes).
/// 2. Eliminates T-Pose via Quaternion.FromToRotation calibration in Character Anatomical Space.
/// 3. Computes guaranteed anatomical hinge axes (hips, knees, ankles, shoulders, elbows, spine, neck, head).
/// 4. Supports both Pure Calibrated Biomechanical Locomotion AND Hybrid In-Place Embedded GLB Clip playback
///    (stripping the 5-metre Mixamo root translation drift on mixamorig:Hips).
/// 5. Includes an Automatic Foot-Ground Pelvis Solver so feet never float or sink during Crouch/Walk/Run.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class ProceduralCharacterAnimator : MonoBehaviour
{
    public enum LocomotionSourceMode
    {
        /// <summary>
        /// Uses 100% anatomically calibrated humanoid locomotion for Idle, Walk, Run, Crouch Idle, and Crouch Walk.
        /// Guaranteed natural posture and gait regardless of what clip is inside the GLB.
        /// </summary>
        CalibratedBiomechanical = 0,

        /// <summary>
        /// Uses the embedded GLB clip ("Mixamo.com") during Walk/Run with horizontal Hips root translation stripped
        /// (fixing the 5m drift bug), and smoothly blends into Calibrated Standing Idle and Crouch poses.
        /// </summary>
        HybridEmbeddedClipInPlace = 1
    }

    [Header("Mode & Diagnostics")]
    [SerializeField] private LocomotionSourceMode sourceMode = LocomotionSourceMode.CalibratedBiomechanical;
    [SerializeField] private bool logInspectionReportOnStart = true;

    [Header("State Thresholds & Blending")]
    [SerializeField, Min(0.05f)] private float walkStartSpeed = 0.12f;
    [SerializeField, Min(0.5f)] private float sprintSpeedThreshold = 4.0f;
    [SerializeField, Min(1f)] private float stateBlendSpeed = 7.5f;

    [Header("Step Cadence (Synchronized with NoiseEmitter)")]
    [SerializeField, Min(0.1f)] private float walkStepInterval = 0.52f;
    [SerializeField, Min(0.1f)] private float sprintStepInterval = 0.32f;
    [SerializeField, Min(0.1f)] private float crouchStepInterval = 0.72f;

    [Header("Standing Idle Posture (T-Pose Correction)")]
    [SerializeField, Range(0.05f, 0.35f)] private float idleArmOutwardSpread = 0.15f;
    [SerializeField, Range(0f, 35f)] private float idleElbowBend = 14f;
    [SerializeField, Range(0f, 8f)] private float idleKneeUnlock = 3.0f;
    [SerializeField, Range(0f, 4f)] private float idleBreathingDegrees = 1.4f;

    [Header("Walk Gait Tuning")]
    [SerializeField, Range(10f, 40f)] private float walkHipSwing = 24f;
    [SerializeField, Range(20f, 70f)] private float walkKneeFlexion = 48f;
    [SerializeField, Range(5f, 35f)] private float walkArmSwing = 20f;
    [SerializeField, Range(10f, 45f)] private float walkElbowBend = 22f;
    [SerializeField, Range(0f, 12f)] private float walkTorsoLean = 4.5f;
    [SerializeField, Range(0f, 10f)] private float walkPelvisYaw = 4.5f;
    [SerializeField, Range(0f, 0.06f)] private float walkVerticalBounce = 0.022f;

    [Header("Sprint / Run Gait Tuning")]
    [SerializeField, Range(20f, 60f)] private float runHipSwing = 42f;
    [SerializeField, Range(40f, 95f)] private float runKneeFlexion = 76f;
    [SerializeField, Range(15f, 55f)] private float runArmSwing = 36f;
    [SerializeField, Range(45f, 95f)] private float runElbowBend = 72f;
    [SerializeField, Range(4f, 22f)] private float runTorsoLean = 13f;
    [SerializeField, Range(0f, 14f)] private float runPelvisYaw = 7.0f;
    [SerializeField, Range(0f, 0.09f)] private float runVerticalBounce = 0.042f;

    [Header("Stealth Crouch Tuning")]
    [SerializeField, Range(20f, 60f)] private float crouchHipFlexion = 42f;
    [SerializeField, Range(35f, 85f)] private float crouchKneeFlexion = 68f;
    [SerializeField, Range(10f, 35f)] private float crouchHipSwingWalk = 16f;
    [SerializeField, Range(8f, 32f)] private float crouchTorsoLean = 22f;
    [SerializeField, Range(25f, 75f)] private float crouchElbowBend = 48f;
    [SerializeField, Range(0.05f, 0.45f)] private float maxCrouchPelvisDrop = 0.36f;

    // Runtime references
    private PlayerMovement movement;
    private Animation embeddedLegacyAnimation;
    private string embeddedClipName;
    private float embeddedClipLength;
    private float embeddedClipDriftMetres;
    private bool hasUsableEmbeddedClip;

    // Calibrated bone data
    private sealed class CalibratedBone
    {
        public string Key;
        public Transform Transform;
        public Vector3 BindLocalPosition;
        public Quaternion BindLocalRotation;
        public Quaternion BindInCharFrame;      // Inverse(charBindWorldRot) * boneBindWorldRot
        public Quaternion InvBindInCharFrame;   // Inverse(BindInCharFrame)
        public Vector3 BindDirInCharFrame;      // Normalized vector to primary child in Character Anatomical Space
    }

    private readonly Dictionary<string, CalibratedBone> rig = new Dictionary<string, CalibratedBone>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Quaternion> embeddedSampledRotations = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);

    // Character anatomical frame relative to Player root
    private Quaternion charFrameLocalToPlayer = Quaternion.identity;
    private Vector3 hipsBindOffsetInPlayer;
    private float bindFootHeightAbovePlayer;

    // Pre-calibrated T-Pose -> Neutral Relaxed Arm rotations in Character Anatomical Space
    private Quaternion leftArmToNeutralChar = Quaternion.identity;
    private Quaternion rightArmToNeutralChar = Quaternion.identity;
    private Vector3 leftElbowHingeChar = Vector3.up;
    private Vector3 rightElbowHingeChar = Vector3.down;

    // Animation state variables
    private float moveBlend;
    private float runBlend;
    private float crouchBlend;
    private float gaitPhase;
    private float embeddedClipTimer;
    private float smoothedPelvisOffsetY;
    private bool initialized;

    private static readonly string[] CanonicalBoneNames =
    {
        "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
        "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
        "RightShoulder", "RightArm", "RightForeArm", "RightHand",
        "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
        "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"
    };

    // Automatically attaches to Player if not already attached
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnPlayerAfterSceneLoad()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        if (player.GetComponent<ProceduralCharacterAnimator>() == null)
        {
            player.gameObject.AddComponent<ProceduralCharacterAnimator>();
        }
    }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        InitializeAndCalibrateRig(logInspectionReportOnStart);
    }

    /// <summary>
    /// Inspects the GLB skeleton, stops automatic root-drifting clip playback,
    /// measures the bind pose in Character Anatomical Space, and calibrates all joint axes.
    /// </summary>
    public void InitializeAndCalibrateRig(bool logReport)
    {
        rig.Clear();
        embeddedSampledRotations.Clear();
        initialized = false;

        // 1. Inspect and stop any automatic Animator / Legacy Animation components first
        // so they do not translate mixamorig:Hips 5 metres away while idle.
        InspectAndControlEmbeddedAnimationDrivers(out StringBuilder clipReport);

        // 2. Discover Mixamo bones in children
        DiscoverMixamoBones();

        if (!rig.ContainsKey("Hips") || !rig.ContainsKey("LeftUpLeg") || !rig.ContainsKey("RightUpLeg"))
        {
            Debug.LogWarning(
                "[ProceduralCharacterAnimator] Essential Mixamo bones (Hips, LeftUpLeg, RightUpLeg) were not found under Player. " +
                "Verify that the GLB visual hierarchy is a child of the Player GameObject.",
                this);
            enabled = false;
            return;
        }

        // 3. Establish the Character Anatomical Frame (Right = +X, Up = +Y, Forward = +Z)
        CalibrateAnatomicalFrameAndAxes(out StringBuilder rigReport);

        // 4. If an embedded GLB clip exists, sample it non-destructively to measure its 5m root drift
        MeasureEmbeddedClipRootDrift(clipReport);

        initialized = true;

        if (logReport)
        {
            Debug.Log(rigReport.ToString() + "\n" + clipReport.ToString(), this);
        }
    }

    private void InspectAndControlEmbeddedAnimationDrivers(out StringBuilder report)
    {
        report = new StringBuilder();
        report.AppendLine("=== [GLB Animation Drivers Inspection] ===");

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in animators)
        {
            if (anim == null) continue;
            anim.applyRootMotion = false;
            anim.enabled = false;
            report.AppendLine($"- Disabled Animator on '{anim.name}' (applyRootMotion=false).");
        }

        Animation[] legacyAnims = GetComponentsInChildren<Animation>(true);
        hasUsableEmbeddedClip = false;
        embeddedLegacyAnimation = null;

        foreach (Animation legacy in legacyAnims)
        {
            if (legacy == null) continue;
            legacy.playAutomatically = false;
            legacy.Stop();

            AnimationClip clip = legacy.clip;
            if (clip != null)
            {
                embeddedLegacyAnimation = legacy;
                embeddedClipName = clip.name;
                embeddedClipLength = clip.length;
                hasUsableEmbeddedClip = clip.length > 0.1f;
                report.AppendLine($"- Found Legacy Animation on '{legacy.name}' with clip '{clip.name}' (length: {clip.length:F2}s, wrapMode: {clip.wrapMode}).");
            }

            // Keep disabled unless actively sampling in Hybrid mode
            legacy.enabled = false;
        }

        if (legacyAnims.Length == 0 && animators.Length == 0)
        {
            report.AppendLine("- No Animator or Legacy Animation components found on character children.");
        }
    }

    private void DiscoverMixamoBones()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            if (t == transform) continue;
            string stripped = StripBonePrefix(t.name);

            for (int i = 0; i < CanonicalBoneNames.Length; i++)
            {
                string canonical = CanonicalBoneNames[i];
                if (string.Equals(stripped, canonical, StringComparison.OrdinalIgnoreCase) && !rig.ContainsKey(canonical))
                {
                    rig.Add(canonical, new CalibratedBone
                    {
                        Key = canonical,
                        Transform = t,
                        BindLocalPosition = t.localPosition,
                        BindLocalRotation = t.localRotation
                    });
                    break;
                }
            }
        }
    }

    private static string StripBonePrefix(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return string.Empty;

        int colonIdx = rawName.LastIndexOf(':');
        if (colonIdx >= 0 && colonIdx < rawName.Length - 1)
        {
            return rawName.Substring(colonIdx + 1);
        }

        const string underscorePrefix = "mixamorig_";
        if (rawName.StartsWith(underscorePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return rawName.Substring(underscorePrefix.Length);
        }

        const string plainPrefix = "mixamorig";
        if (rawName.StartsWith(plainPrefix, StringComparison.OrdinalIgnoreCase) && rawName.Length > plainPrefix.Length)
        {
            return rawName.Substring(plainPrefix.Length);
        }

        return rawName;
    }

    private void CalibrateAnatomicalFrameAndAxes(out StringBuilder report)
    {
        report = new StringBuilder();
        report.AppendLine("=== [Mixamo GLB Skeleton & Bind Pose Calibration] ===");
        report.AppendLine($"- Discovered {rig.Count}/{CanonicalBoneNames.Length} canonical Mixamo bones.");

        Transform hips = rig["Hips"].Transform;
        Transform leftUpLeg = rig["LeftUpLeg"].Transform;
        Transform rightUpLeg = rig["RightUpLeg"].Transform;
        Transform head = rig.ContainsKey("Head") ? rig["Head"].Transform : (rig.ContainsKey("Spine2") ? rig["Spine2"].Transform : hips);

        // Compute true anatomical axes in World Space from bind bone positions
        Vector3 upWorld = (head.position - hips.position);
        upWorld = upWorld.sqrMagnitude > 0.001f ? upWorld.normalized : transform.up;

        Vector3 rightWorld = (rightUpLeg.position - leftUpLeg.position);
        rightWorld = Vector3.ProjectOnPlane(rightWorld, upWorld);
        if (rightWorld.sqrMagnitude < 0.0001f && rig.ContainsKey("LeftArm") && rig.ContainsKey("RightArm"))
        {
            rightWorld = Vector3.ProjectOnPlane(rig["RightArm"].Transform.position - rig["LeftArm"].Transform.position, upWorld);
        }
        rightWorld = rightWorld.sqrMagnitude > 0.0001f ? rightWorld.normalized : transform.right;

        // In Unity's left-handed coordinate system: Cross(Right, Up) == Forward
        Vector3 forwardWorld = Vector3.Cross(rightWorld, upWorld).normalized;
        upWorld = Vector3.Cross(forwardWorld, rightWorld).normalized;

        Quaternion charBindWorldRot = Quaternion.LookRotation(forwardWorld, upWorld);
        Quaternion invCharBindWorldRot = Quaternion.Inverse(charBindWorldRot);
        charFrameLocalToPlayer = Quaternion.Inverse(transform.rotation) * charBindWorldRot;

        // Record Hips bind position in Player space & Foot bind height above Player root
        hipsBindOffsetInPlayer = transform.InverseTransformPoint(hips.position);

        float leftFootY = rig.ContainsKey("LeftFoot") ? rig["LeftFoot"].Transform.position.y : transform.position.y;
        float rightFootY = rig.ContainsKey("RightFoot") ? rig["RightFoot"].Transform.position.y : transform.position.y;
        bindFootHeightAbovePlayer = Mathf.Min(leftFootY, rightFootY) - transform.position.y;

        // Cache every bone's orientation in the Character Anatomical Frame
        foreach (KeyValuePair<string, CalibratedBone> kvp in rig)
        {
            CalibratedBone bone = kvp.Value;
            bone.BindInCharFrame = invCharBindWorldRot * bone.Transform.rotation;
            bone.InvBindInCharFrame = Quaternion.Inverse(bone.BindInCharFrame);
        }

        // Measure child direction vectors in Character Anatomical Space to verify T-Pose and calibrate arms/elbows
        MeasureChildDirection("LeftArm", "LeftForeArm", invCharBindWorldRot);
        MeasureChildDirection("RightArm", "RightForeArm", invCharBindWorldRot);
        MeasureChildDirection("LeftForeArm", "LeftHand", invCharBindWorldRot);
        MeasureChildDirection("RightForeArm", "RightHand", invCharBindWorldRot);
        MeasureChildDirection("LeftUpLeg", "LeftLeg", invCharBindWorldRot);
        MeasureChildDirection("RightUpLeg", "RightLeg", invCharBindWorldRot);

        // Calibrate T-Pose -> Neutral Relaxed Arm rotations in Character Anatomical Space
        if (rig.TryGetValue("LeftArm", out CalibratedBone leftArm))
        {
            Vector3 bindDir = leftArm.BindDirInCharFrame.sqrMagnitude > 0.1f ? leftArm.BindDirInCharFrame : Vector3.left;
            Vector3 desiredNeutralDir = new Vector3(-idleArmOutwardSpread, -0.985f, -0.02f).normalized;
            leftArmToNeutralChar = Quaternion.FromToRotation(bindDir, desiredNeutralDir);
            report.AppendLine($"- LeftArm Bind Direction (Char Space): {bindDir:F2} -> Calibrated T-Pose Adduction Angle: {Quaternion.Angle(Quaternion.identity, leftArmToNeutralChar):F1}°");
        }

        if (rig.TryGetValue("RightArm", out CalibratedBone rightArm))
        {
            Vector3 bindDir = rightArm.BindDirInCharFrame.sqrMagnitude > 0.1f ? rightArm.BindDirInCharFrame : Vector3.right;
            Vector3 desiredNeutralDir = new Vector3(idleArmOutwardSpread, -0.985f, -0.02f).normalized;
            rightArmToNeutralChar = Quaternion.FromToRotation(bindDir, desiredNeutralDir);
            report.AppendLine($"- RightArm Bind Direction (Char Space): {bindDir:F2} -> Calibrated T-Pose Adduction Angle: {Quaternion.Angle(Quaternion.identity, rightArmToNeutralChar):F1}°");
        }

        // Calibrate Left & Right Elbow hinge axes in Character Anatomical Space so positive angle always bends forearm forward & slightly inward
        if (rig.TryGetValue("LeftForeArm", out CalibratedBone leftForeArm))
        {
            Vector3 bindDir = leftForeArm.BindDirInCharFrame.sqrMagnitude > 0.1f ? leftForeArm.BindDirInCharFrame : Vector3.left;
            Vector3 targetBendDir = new Vector3(0.25f, 0.05f, 0.96f).normalized;
            Vector3 axis = Vector3.Cross(bindDir, targetBendDir);
            leftElbowHingeChar = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
        }

        if (rig.TryGetValue("RightForeArm", out CalibratedBone rightForeArm))
        {
            Vector3 bindDir = rightForeArm.BindDirInCharFrame.sqrMagnitude > 0.1f ? rightForeArm.BindDirInCharFrame : Vector3.right;
            Vector3 targetBendDir = new Vector3(-0.25f, 0.05f, 0.96f).normalized;
            Vector3 axis = Vector3.Cross(bindDir, targetBendDir);
            rightElbowHingeChar = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.down;
        }
    }

    private void MeasureChildDirection(string parentKey, string childKey, Quaternion invCharBindWorldRot)
    {
        if (!rig.TryGetValue(parentKey, out CalibratedBone parentBone) || !rig.TryGetValue(childKey, out CalibratedBone childBone))
            return;

        Vector3 worldDelta = childBone.Transform.position - parentBone.Transform.position;
        if (worldDelta.sqrMagnitude > 0.00001f)
        {
            parentBone.BindDirInCharFrame = (invCharBindWorldRot * worldDelta).normalized;
        }
    }

    private void MeasureEmbeddedClipRootDrift(StringBuilder report)
    {
        if (!hasUsableEmbeddedClip || embeddedLegacyAnimation == null || !rig.TryGetValue("Hips", out CalibratedBone hips))
            return;

        AnimationState state = embeddedLegacyAnimation[embeddedClipName];
        if (state == null) return;

        embeddedLegacyAnimation.enabled = true;
        state.enabled = true;
        state.weight = 1f;

        // Sample start of clip
        state.time = 0f;
        embeddedLegacyAnimation.Sample();
        Vector3 startHipsWorld = hips.Transform.position;

        // Sample end of clip
        state.time = Mathf.Max(0f, embeddedClipLength - 0.01f);
        embeddedLegacyAnimation.Sample();
        Vector3 endHipsWorld = hips.Transform.position;

        state.enabled = false;
        embeddedLegacyAnimation.Stop();
        embeddedLegacyAnimation.enabled = false;

        // Restore exact bind pose immediately after sampling
        RestoreBindPose();

        Vector3 planarDrift = new Vector3(endHipsWorld.x - startHipsWorld.x, 0f, endHipsWorld.z - startHipsWorld.z);
        embeddedClipDriftMetres = planarDrift.magnitude;

        report.AppendLine(
            $"- Inspected embedded clip '{embeddedClipName}': detected {embeddedClipDriftMetres:F2}m of horizontal Hips translation. " +
            "Horizontal root drift stripping is active.");
    }

    private void LateUpdate()
    {
        if (!initialized || movement == null) return;

        float planarSpeed = movement.PlanarSpeed;
        bool isGrounded = movement.IsGrounded;
        bool isMoving = isGrounded && planarSpeed > walkStartSpeed;
        bool isCrouching = movement.IsCrouching;
        bool isRunning = isMoving && !isCrouching && planarSpeed >= sprintSpeedThreshold;

        // Smooth state blend weights
        float dt = Time.deltaTime;
        moveBlend = Mathf.MoveTowards(moveBlend, isMoving ? 1f : 0f, stateBlendSpeed * dt);
        runBlend = Mathf.MoveTowards(runBlend, isRunning ? 1f : 0f, stateBlendSpeed * dt);
        crouchBlend = Mathf.MoveTowards(crouchBlend, isCrouching ? 1f : 0f, stateBlendSpeed * dt);

        // Compute gait cycle frequency (in Hz = full 2-step cycles per second) synchronized with NoiseEmitter intervals
        float activeStepInterval = isCrouching
            ? crouchStepInterval
            : Mathf.Lerp(walkStepInterval, sprintStepInterval, runBlend);

        float cycleFrequencyHz = 1.0f / Mathf.Max(0.2f, activeStepInterval * 2.0f);

        // Scale cadence slightly with actual movement speed so feet never slide
        float referenceSpeed = isCrouching ? 1.65f : Mathf.Lerp(3.2f, 5.2f, runBlend);
        float speedRatio = isMoving ? Mathf.Clamp(planarSpeed / Mathf.Max(0.5f, referenceSpeed), 0.65f, 1.35f) : 1.0f;

        if (moveBlend > 0.001f)
        {
            gaitPhase += dt * cycleFrequencyHz * speedRatio * Mathf.PI * 2.0f;
            if (gaitPhase > Mathf.PI * 200f) gaitPhase -= Mathf.PI * 200f;
        }

        // Optional: Sample embedded GLB clip in-place if Hybrid mode is selected
        bool blendEmbeddedClip = (sourceMode == LocomotionSourceMode.HybridEmbeddedClipInPlace)
                                 && hasUsableEmbeddedClip
                                 && moveBlend > 0.001f;

        if (blendEmbeddedClip)
        {
            SampleEmbeddedClipInPlace(dt * speedRatio);
        }

        // Apply calibrated humanoid pose
        ApplyCalibratedHumanoidPose(blendEmbeddedClip);
    }

    private void SampleEmbeddedClipInPlace(float scaledDeltaTime)
    {
        if (embeddedLegacyAnimation == null) return;
        AnimationState state = embeddedLegacyAnimation[embeddedClipName];
        if (state == null) return;

        embeddedClipTimer = (embeddedClipTimer + scaledDeltaTime) % Mathf.Max(0.1f, embeddedClipLength);

        embeddedLegacyAnimation.enabled = true;
        state.enabled = true;
        state.weight = 1f;
        state.time = embeddedClipTimer;
        embeddedLegacyAnimation.Sample();
        state.enabled = false;
        embeddedLegacyAnimation.enabled = false;

        foreach (KeyValuePair<string, CalibratedBone> kvp in rig)
        {
            embeddedSampledRotations[kvp.Key] = kvp.Value.Transform.localRotation;
        }
    }

    private void ApplyCalibratedHumanoidPose(bool blendWithEmbeddedClip)
    {
        // Phase angles for Left and Right legs (180 degrees out of phase)
        float leftPhase = gaitPhase;
        float rightPhase = gaitPhase + Mathf.PI;

        float sinL = Mathf.Sin(leftPhase);
        float cosL = Mathf.Cos(leftPhase);
        float sinR = Mathf.Sin(rightPhase);
        float cosR = Mathf.Cos(rightPhase);

        // 1. LEGS: Hip Flexion/Extension, Knee Flexion (strictly backward), and Ankle Articulation
        float strideHipSwing = Mathf.Lerp( Mathf.Lerp(walkHipSwing, runHipSwing, runBlend), crouchHipSwingWalk, crouchBlend ) * moveBlend;
        float strideKneeBend = Mathf.Lerp(walkKneeFlexion, runKneeFlexion, runBlend) * moveBlend;

        // Asymmetric human hip swing: slightly more forward flexion than backward extension
        float leftHipDynamic = (sinL * 0.92f + cosL * 0.16f) * strideHipSwing;
        float rightHipDynamic = (sinR * 0.92f + cosR * 0.16f) * strideHipSwing;

        float baseHipCrouch = crouchHipFlexion * crouchBlend;
        float leftHipFlex = baseHipCrouch + leftHipDynamic;
        float rightHipFlex = baseHipCrouch + rightHipDynamic;

        // Knee flexion curve: peaks during forward leg swing (when cos > 0) to clear the ground, plus crouch base bend
        float leftSwingPeak = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(leftPhase - 0.55f)), 1.35f);
        float rightSwingPeak = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(rightPhase - 0.55f)), 1.35f);

        float baseKneePose = Mathf.Lerp(idleKneeUnlock * (1f - moveBlend), crouchKneeFlexion, crouchBlend);
        float leftKneeFlex = baseKneePose + leftSwingPeak * strideKneeBend * (1f - 0.45f * crouchBlend);
        float rightKneeFlex = baseKneePose + rightSwingPeak * strideKneeBend * (1f - 0.45f * crouchBlend);

        // Ankle pitch: dorsiflexion during crouch and forward swing, plantarflexion push-off at toe-off
        float leftFootPitch = -16f * crouchBlend - leftSwingPeak * 12f * moveBlend + Mathf.Max(0f, -sinL) * 10f * moveBlend;
        float rightFootPitch = -16f * crouchBlend - rightSwingPeak * 12f * moveBlend + Mathf.Max(0f, -sinR) * 10f * moveBlend;

        // Slight natural stance abduction on thighs (wider during crouch for stealth balance)
        float hipSpread = Mathf.Lerp(1.8f, 5.5f, crouchBlend);

        // In Character Anatomical Space:
        // Rotating a downward-pointing leg around Character Right (+X) by a NEGATIVE angle swings it FORWARD (+Z).
        // Rotating a downward-pointing shin around Character Right (+X) by a POSITIVE angle bends it BACKWARD (-Z).
        ApplyCharSpaceRotation("LeftUpLeg", Quaternion.Euler(-leftHipFlex, -hipSpread * 0.5f, -hipSpread));
        ApplyCharSpaceRotation("RightUpLeg", Quaternion.Euler(-rightHipFlex, hipSpread * 0.5f, hipSpread));

        ApplyCharSpaceRotation("LeftLeg", Quaternion.Euler(leftKneeFlex, 0f, 0f));
        ApplyCharSpaceRotation("RightLeg", Quaternion.Euler(rightKneeFlex, 0f, 0f));

        ApplyCharSpaceRotation("LeftFoot", Quaternion.Euler(leftFootPitch, 0f, 0f));
        ApplyCharSpaceRotation("RightFoot", Quaternion.Euler(rightFootPitch, 0f, 0f));

        // 2. PELVIS & TORSO: Breathing, Counter-Rotation, Forward Lean & Crouch Posture
        float breathWave = Mathf.Sin(Time.time * 2.1f) * idleBreathingDegrees * (1f - moveBlend * 0.6f);
        float idleWeightShift = Mathf.Sin(Time.time * 1.05f) * 0.8f * (1f - moveBlend);

        float pelvisYaw = sinL * Mathf.Lerp(walkPelvisYaw, runPelvisYaw, runBlend) * moveBlend * (1f - 0.4f * crouchBlend);
        float pelvisRoll = cosL * 2.2f * moveBlend * (1f - 0.4f * crouchBlend) + idleWeightShift;

        ApplyCharSpaceRotation("Hips", Quaternion.Euler(0f, pelvisYaw, pelvisRoll));

        // Forward torso lean (pointing UP, so POSITIVE X angle in Character Space leans torso FORWARD +Z)
        float locomotionLean = Mathf.Lerp(walkTorsoLean, runTorsoLean, runBlend) * moveBlend;
        float totalForwardLean = Mathf.Lerp(locomotionLean, crouchTorsoLean, crouchBlend);
        float torsoCounterYaw = -pelvisYaw * 1.15f;

        ApplyCharSpaceRotation("Spine", Quaternion.Euler(totalForwardLean * 0.35f, torsoCounterYaw * 0.35f, -pelvisRoll * 0.4f));
        ApplyCharSpaceRotation("Spine1", Quaternion.Euler(totalForwardLean * 0.35f + breathWave * 0.4f, torsoCounterYaw * 0.35f, 0f));
        ApplyCharSpaceRotation("Spine2", Quaternion.Euler(totalForwardLean * 0.30f + breathWave * 0.6f, torsoCounterYaw * 0.30f, 0f));

        // Counter-pitch Neck and Head upward during crouch/run so the character looks straight ahead
        float headCounterPitch = -totalForwardLean * 0.78f - breathWave * 0.3f;
        ApplyCharSpaceRotation("Neck", Quaternion.Euler(headCounterPitch * 0.45f, -torsoCounterYaw * 0.25f, 0f));
        ApplyCharSpaceRotation("Head", Quaternion.Euler(headCounterPitch * 0.55f, -torsoCounterYaw * 0.25f, 0f));

        // 3. SHOULDERS, ARMS & FOREARMS: Lowered from T-Pose + Natural Sagittal Counter-Swing
        float shoulderBreath = breathWave * 0.6f;
        ApplyCharSpaceRotation("LeftShoulder", Quaternion.Euler(0f, 0f, -shoulderBreath - 2f * crouchBlend));
        ApplyCharSpaceRotation("RightShoulder", Quaternion.Euler(0f, 0f, shoulderBreath + 2f * crouchBlend));

        // Left arm swings forward with Right leg (sinR); Right arm swings forward with Left leg (sinL)
        float activeArmSwing = Mathf.Lerp(walkArmSwing, runArmSwing, runBlend) * moveBlend * (1f - 0.45f * crouchBlend);
        float crouchArmForwardOffset = 15f * crouchBlend;

        float leftArmPitch = -sinR * activeArmSwing - crouchArmForwardOffset;
        float rightArmPitch = -sinL * activeArmSwing - crouchArmForwardOffset;

        // Compose in Character Space: FIRST lower arm from T-Pose to relaxed side (leftArmToNeutralChar),
        // THEN swing the lowered arm forward/backward around Character Right (+X).
        Quaternion leftArmDeltaChar = Quaternion.Euler(leftArmPitch, 0f, 0f) * leftArmToNeutralChar;
        Quaternion rightArmDeltaChar = Quaternion.Euler(rightArmPitch, 0f, 0f) * rightArmToNeutralChar;

        ApplyCharSpaceRotation("LeftArm", leftArmDeltaChar);
        ApplyCharSpaceRotation("RightArm", rightArmDeltaChar);

        // Elbows bend strictly forward/inward around their calibrated elbow hinge axes
        float baseElbow = Mathf.Lerp(
            Mathf.Lerp(idleElbowBend, Mathf.Lerp(walkElbowBend, runElbowBend, runBlend), moveBlend),
            crouchElbowBend,
            crouchBlend);

        float leftDynamicElbow = baseElbow + Mathf.Max(0f, sinR) * 14f * moveBlend;
        float rightDynamicElbow = baseElbow + Mathf.Max(0f, sinL) * 14f * moveBlend;

        ApplyCharSpaceRotation("LeftForeArm", Quaternion.AngleAxis(leftDynamicElbow, leftElbowHingeChar));
        ApplyCharSpaceRotation("RightForeArm", Quaternion.AngleAxis(rightDynamicElbow, rightElbowHingeChar));

        // 4. OPTIONAL HYBRID BLEND WITH EMBEDDED GLB CLIP (during standing Walk/Run)
        if (blendWithEmbeddedClip)
        {
            float clipWeight = moveBlend * (1f - crouchBlend * 0.85f);
            if (clipWeight > 0.001f)
            {
                foreach (KeyValuePair<string, CalibratedBone> kvp in rig)
                {
                    if (embeddedSampledRotations.TryGetValue(kvp.Key, out Quaternion clipRot))
                    {
                        kvp.Value.Transform.localRotation = Quaternion.Slerp(
                            kvp.Value.Transform.localRotation,
                            clipRot,
                            clipWeight);
                    }
                }
            }
        }

        // 5. AUTOMATIC FOOT-GROUND PELVIS SOLVER (prevents floating in Crouch and locks horizontal X/Z in-place)
        SolvePelvisPositionAndFootGrounding();
    }

    private void ApplyCharSpaceRotation(string boneKey, Quaternion deltaInCharSpace)
    {
        if (!rig.TryGetValue(boneKey, out CalibratedBone bone)) return;

        // Similarity transform: converts Character Anatomical Space rotation into the bone's exact local bind space
        Quaternion localDelta = bone.InvBindInCharFrame * deltaInCharSpace * bone.BindInCharFrame;
        bone.Transform.localRotation = bone.BindLocalRotation * localDelta;
    }

    private void SolvePelvisPositionAndFootGrounding()
    {
        if (!rig.TryGetValue("Hips", out CalibratedBone hips)) return;

        // 1. Always lock Hips horizontal X/Z to the Player root's bind offset (eliminates the 5m GLB clip drift)
        Vector3 bindWorldHips = transform.TransformPoint(hipsBindOffsetInPlayer);
        hips.Transform.position = bindWorldHips;

        // 2. Measure current lowest ankle height above Player root after leg rotations
        float targetYOffset = 0f;
        if (rig.TryGetValue("LeftFoot", out CalibratedBone leftFoot) && rig.TryGetValue("RightFoot", out CalibratedBone rightFoot))
        {
            float currentLowestFootY = Mathf.Min(leftFoot.Transform.position.y, rightFoot.Transform.position.y) - transform.position.y;
            float footLiftDelta = currentLowestFootY - bindFootHeightAbovePlayer;

            // Lower the pelvis so the lowest foot stays planted on the floor (clamped for safety)
            float groundingDrop = Mathf.Clamp(footLiftDelta, -0.05f, maxCrouchPelvisDrop);

            // Add subtle vertical gait bounce during walk/run
            float bounceAmp = Mathf.Lerp(walkVerticalBounce, runVerticalBounce, runBlend) * moveBlend * (1f - 0.5f * crouchBlend);
            float gaitBounce = Mathf.Abs(Mathf.Cos(gaitPhase)) * bounceAmp;

            targetYOffset = -groundingDrop + gaitBounce;
        }

        smoothedPelvisOffsetY = Mathf.Lerp(smoothedPelvisOffsetY, targetYOffset, 18f * Time.deltaTime);
        hips.Transform.position = bindWorldHips + Vector3.up * smoothedPelvisOffsetY;
    }

    public void RestoreBindPose()
    {
        foreach (KeyValuePair<string, CalibratedBone> kvp in rig)
        {
            CalibratedBone b = kvp.Value;
            if (b.Transform == null) continue;
            b.Transform.localPosition = b.BindLocalPosition;
            b.Transform.localRotation = b.BindLocalRotation;
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;
        RestoreBindPose();
    }

#if UNITY_EDITOR
    // =========================================================================
    // UNITY EDITOR DIAGNOSTIC & ONE-CLICK SETUP TOOLS
    // =========================================================================

    [MenuItem("Tools/Gardien Furtif/Character Animation/Inspect Player Rig & GLB Clips", false, 10)]
    public static void EditorInspectPlayerRig()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Character Animation Inspector", "No GameObject with PlayerMovement was found in the active scene.", "OK");
            return;
        }

        ProceduralCharacterAnimator animator = player.GetComponent<ProceduralCharacterAnimator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<ProceduralCharacterAnimator>(player.gameObject);
        }

        animator.InitializeAndCalibrateRig(true);
        Selection.activeGameObject = player.gameObject;
        EditorUtility.SetDirty(animator);
    }

    [MenuItem("Tools/Gardien Furtif/Character Animation/Preview Calibrated Standing Idle Pose", false, 30)]
    public static void EditorPreviewStandingIdlePose()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;

        ProceduralCharacterAnimator animator = player.GetComponent<ProceduralCharacterAnimator>();
        if (animator == null) animator = Undo.AddComponent<ProceduralCharacterAnimator>(player.gameObject);

        animator.InitializeAndCalibrateRig(false);
        animator.moveBlend = 0f;
        animator.runBlend = 0f;
        animator.crouchBlend = 0f;
        animator.ApplyCalibratedHumanoidPose(false);
        SceneView.RepaintAll();
        Debug.Log("[ProceduralCharacterAnimator] Previewing Calibrated Standing Idle Pose (T-Pose arms lowered naturally).");
    }

    [MenuItem("Tools/Gardien Furtif/Character Animation/Preview Calibrated Stealth Crouch Pose", false, 31)]
    public static void EditorPreviewCrouchPose()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;

        ProceduralCharacterAnimator animator = player.GetComponent<ProceduralCharacterAnimator>();
        if (animator == null) animator = Undo.AddComponent<ProceduralCharacterAnimator>(player.gameObject);

        animator.InitializeAndCalibrateRig(false);
        animator.moveBlend = 0f;
        animator.runBlend = 0f;
        animator.crouchBlend = 1f;
        animator.ApplyCalibratedHumanoidPose(false);
        SceneView.RepaintAll();
        Debug.Log("[ProceduralCharacterAnimator] Previewing Calibrated Stealth Crouch Pose.");
    }

    [MenuItem("Tools/Gardien Furtif/Character Animation/Restore Original T-Pose (Bind Pose)", false, 32)]
    public static void EditorRestoreBindPose()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;

        ProceduralCharacterAnimator animator = player.GetComponent<ProceduralCharacterAnimator>();
        if (animator == null) return;

        animator.InitializeAndCalibrateRig(false);
        animator.RestoreBindPose();
        SceneView.RepaintAll();
        Debug.Log("[ProceduralCharacterAnimator] Restored original GLB T-Pose (Bind Pose).");
    }
#endif
}