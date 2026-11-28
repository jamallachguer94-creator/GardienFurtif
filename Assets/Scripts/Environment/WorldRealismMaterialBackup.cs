using UnityEngine;

/// <summary>
/// Stores the exact shared-material assignment before a reversible World Realism preview changes it.
/// This component is removed together with the preview after restoration.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldRealismMaterialBackup : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material[] originalMaterials;

    public void Capture(Renderer renderer)
    {
        if (renderer == null || targetRenderer != null) return;
        targetRenderer = renderer;
        originalMaterials = renderer.sharedMaterials;
    }

    public void Restore()
    {
        if (targetRenderer != null && originalMaterials != null)
            targetRenderer.sharedMaterials = originalMaterials;
    }
}
