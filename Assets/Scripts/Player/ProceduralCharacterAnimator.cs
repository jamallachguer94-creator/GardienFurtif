using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight procedural locomotion for a Mixamo-style GLB skeleton.
/// Keeps the GLB mesh/materials and does not require Animator, FBX or extra clips.
/// Attach to the Player root, or let the runtime installer add it automatically.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class ProceduralCharacterAnimator : MonoBehaviour
{
    [Header("Automatic setup")]
    [SerializeField] private bool disableImportedAnimationComponents = true;
    [SerializeField] private bool logBoneReport = true;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float runSpeedThreshold = 4.0f;

    [Header("Motion feel")]
    [SerializeField, Min(0.1f)] private float blendSpeed = 8f;
    [SerializeField, Min(0f)] private float walkLegSwing = 27f;
    [SerializeField, Min(0f)] private float runLegSwing = 43f;
    [SerializeField, Min(0f)] private float crouchLegSwing = 13f;
    [SerializeField, Min(0f)] private float walkKneeBend = 25f;
    [SerializeField, Min(0f)] private float runKneeBend = 55f;
    [SerializeField, Min(0f)] private float crouchKneeBend = 62f;
    [SerializeField, Min(0f)] private float crouchHipDrop = 0.12f;
    [SerializeField, Range(0f, 35f)] private float crouchTorsoLean = 17f;

    private PlayerMovement movement;
    private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Quaternion> restRotations = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector3> restPositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

    private float moveBlend;
    private float crouchBlend;
    private float runBlend;
    private float phase;
    private bool initialized;

    // Automatically installs the animator on the GameObject that already has PlayerMovement.
    // The user only needs to add this .cs file to the Unity project.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnPlayerAfterSceneLoad()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        if (player.GetComponent<ProceduralCharacterAnimator>() == null)
            player.gameObject.AddComponent<ProceduralCharacterAnimator>();
    }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (disableImportedAnimationComponents)
            DisableImportedAnimationDrivers();

        FindMixamoBones();
        initialized = bones.ContainsKey("Hips") && bones.ContainsKey("LeftUpLeg") && bones.ContainsKey("RightUpLeg");

        if (!initialized)
        {
            Debug.LogWarning("ProceduralCharacterAnimator: Could not find the essential Mixamo bones (Hips, LeftUpLeg, RightUpLeg). The GLB may use different bone names or the model may not be under Player.", this);
            enabled = false;
            return;
        }

        if (logBoneReport)
            Debug.Log($"ProceduralCharacterAnimator: found {bones.Count} Mixamo bones. Procedural idle/walk/run/crouch animation is active.", this);
    }

    private void FindMixamoBones()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        string[] wanted =
        {
            "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
            "RightShoulder", "RightArm", "RightForeArm", "RightHand",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "RightUpLeg", "RightLeg", "RightFoot"
        };

        foreach (Transform t in all)
        {
            if (t == transform) continue;
            string simpleName = GetSimpleBoneName(t.name);
            for (int i = 0; i < wanted.Length; i++)
            {
                if (string.Equals(simpleName, wanted[i], StringComparison.OrdinalIgnoreCase) && !bones.ContainsKey(wanted[i]))
                {
                    bones.Add(wanted[i], t);
                    restRotations.Add(wanted[i], t.localRotation);
                    restPositions.Add(wanted[i], t.localPosition);
                    break;
                }
            }
        }
    }

    private static string GetSimpleBoneName(string name)
    {
        int colon = name.LastIndexOf(':');
        return colon >= 0 && colon < name.Length - 1 ? name.Substring(colon + 1) : name;
    }

    private void DisableImportedAnimationDrivers()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            if (animator != null) animator.enabled = false;

        Animation[] legacyAnimations = GetComponentsInChildren<Animation>(true);
        foreach (Animation animation in legacyAnimations)
            if (animation != null) animation.enabled = false;
    }

    private void LateUpdate()
    {
        if (!initialized || movement == null) return;

        float speed = movement.PlanarSpeed;
        bool moving = speed > 0.12f;
        bool crouching = movement.IsCrouching;
        bool running = moving && !crouching && speed >= runSpeedThreshold;

        moveBlend = Mathf.MoveTowards(moveBlend, moving ? 1f : 0f, blendSpeed * Time.deltaTime);
        crouchBlend = Mathf.MoveTowards(crouchBlend, crouching ? 1f : 0f, blendSpeed * Time.deltaTime);
        runBlend = Mathf.MoveTowards(runBlend, running ? 1f : 0f, blendSpeed * Time.deltaTime);

        float frequency = crouching ? 5.0f : Mathf.Lerp(6.8f, 10.5f, runBlend);
        if (moving) phase += Time.deltaTime * frequency * Mathf.PI * 2f;

        ApplyPose();
    }

    private void ApplyPose()
    {
        float leftCycle = Mathf.Sin(phase);
        float rightCycle = Mathf.Sin(phase + Mathf.PI);

        float normalLegSwing = Mathf.Lerp(walkLegSwing, runLegSwing, runBlend);
        float legSwing = Mathf.Lerp(normalLegSwing, crouchLegSwing, crouchBlend) * moveBlend;
        float normalKnee = Mathf.Lerp(walkKneeBend, runKneeBend, runBlend);
        float kneeBase = crouchKneeBend * crouchBlend;

        float leftKnee = kneeBase + Mathf.Max(0f, -leftCycle) * normalKnee * moveBlend;
        float rightKnee = kneeBase + Mathf.Max(0f, -rightCycle) * normalKnee * moveBlend;

        // Mixamo leg bones swing around their local X axis. Crouch bends both knees.
        SetRotation("LeftUpLeg", leftCycle * legSwing - 38f * crouchBlend, 0f, 0f);
        SetRotation("RightUpLeg", rightCycle * legSwing - 38f * crouchBlend, 0f, 0f);
        SetRotation("LeftLeg", -leftKnee, 0f, 0f);
        SetRotation("RightLeg", -rightKnee, 0f, 0f);
        SetRotation("LeftFoot", 8f * crouchBlend - leftCycle * legSwing * 0.18f, 0f, 0f);
        SetRotation("RightFoot", 8f * crouchBlend - rightCycle * legSwing * 0.18f, 0f, 0f);

        // Arms counter-swing against the legs. In crouch, arms move slightly forward.
        float armSwing = Mathf.Lerp(0.58f, 0.68f, runBlend) * legSwing;
        SetRotation("LeftArm", -leftCycle * armSwing - 12f * crouchBlend, 0f, -4f * crouchBlend);
        SetRotation("RightArm", -rightCycle * armSwing - 12f * crouchBlend, 0f, 4f * crouchBlend);
        SetRotation("LeftForeArm", -8f - 18f * runBlend - 8f * crouchBlend, 0f, 0f);
        SetRotation("RightForeArm", -8f - 18f * runBlend - 8f * crouchBlend, 0f, 0f);
        SetRotation("LeftShoulder", 0f, 0f, -3f * crouchBlend);
        SetRotation("RightShoulder", 0f, 0f, 3f * crouchBlend);

        // Slight torso lean while crouching and subtle breathing while idle.
        float breathing = (1f - moveBlend) * Mathf.Sin(Time.time * 2.0f) * 1.1f;
        SetRotation("Spine", -crouchTorsoLean * 0.25f * crouchBlend, 0f, 0f);
        SetRotation("Spine1", -crouchTorsoLean * 0.35f * crouchBlend + breathing * 0.3f, 0f, 0f);
        SetRotation("Spine2", -crouchTorsoLean * 0.40f * crouchBlend + breathing, 0f, 0f);
        SetRotation("Neck", crouchTorsoLean * 0.12f * crouchBlend - breathing * 0.25f, 0f, 0f);
        SetRotation("Head", crouchTorsoLean * 0.10f * crouchBlend, 0f, 0f);

        // Lower the pelvis a little during crouch without translating the Player root.
        if (bones.TryGetValue("Hips", out Transform hips))
        {
            hips.localRotation = restRotations["Hips"];
            hips.localPosition = restPositions["Hips"] + Vector3.down * (crouchHipDrop * crouchBlend);
        }
    }

    private void SetRotation(string boneName, float x, float y, float z)
    {
        if (!bones.TryGetValue(boneName, out Transform bone)) return;
        bone.localRotation = restRotations[boneName] * Quaternion.Euler(x, y, z);
    }

    private void OnDisable()
    {
        if (!initialized) return;
        foreach (KeyValuePair<string, Transform> pair in bones)
        {
            if (pair.Value == null) continue;
            pair.Value.localRotation = restRotations[pair.Key];
            pair.Value.localPosition = restPositions[pair.Key];
        }
    }
}
