#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Professional two-storey first-person stealth environment builder for "Gardien furtif".
/// Target Engine: Unity 6.3 LTS (6000.3.24f1).
/// Footprint: 30m x 20m, Ground Floor (Y = 0.0m) + Upper Floor (Y = 3.40m).
/// Self-contained Editor script. Does not modify other Editor scripts or unrelated scene objects.
/// </summary>
public static class OfficeHeistLevelBuilder
{
    public const string RootName = "OfficeHeist_Generated";
    private const string LegacyRootName = "OfficeHeist_Level_Generated";
    private const string TargetSceneName = "Office_Heist";
    private const string MaterialFolder = "Assets/Generated/OfficeHeist/Materials";

    // Building core dimensions (30m x 20m footprint)
    private const float HalfWidth = 15.0f;       // X: -15 to +15 (30m)
    private const float HalfDepth = 10.0f;       // Z: -10 to +10 (20m)
    private const float GroundFloorY = 0.0f;
    private const float ClearFloorHeight = 3.20f;
    private const float UpperSlabThickness = 0.20f;
    private const float UpperFloorY = 3.40f;     // GroundFloorY + ClearFloorHeight + UpperSlabThickness
    private const float RoofBaseY = 6.60f;       // UpperFloorY + ClearFloorHeight
    private const float ExtWallThickness = 0.30f;
    private const float IntWallThickness = 0.20f;

    // Stairwell opening coordinates in the upper floor slab
    private const float StairCutoutMinX = 11.0f;
    private const float StairCutoutMaxX = 13.0f;
    private const float StairCutoutMinZ = 2.2f;
    private const float StairCutoutMaxZ = 8.2f;

    // Cached shared materials
    private static Material matExtPlasterMain;
    private static Material matExtPlasterAccent;
    private static Material matIntWallIvory;
    private static Material matIntWallAccent;
    private static Material matFloorConcrete;
    private static Material matFloorReceptionTile;
    private static Material matFloorCorridor;
    private static Material matFloorExecutiveWood;
    private static Material matFloorServerTech;
    private static Material matCeilingTile;
    private static Material matRoofMembrane;
    private static Material matDoorWalnut;
    private static Material matWoodWalnut;
    private static Material matWoodLightOak;
    private static Material matMetalCharcoal;
    private static Material matMetalBrushed;
    private static Material matGlassTinted;
    private static Material matFabricGrey;
    private static Material matFabricExecutive;
    private static Material matLeatherDark;
    private static Material matAsphalt;
    private static Material matSidewalkConcrete;
    private static Material matCourtyardPaving;
    private static Material matGrassOlive;
    private static Material matFoliageGreen;
    private static Material matTreeBark;
    private static Material matLampWarmEmissive;
    private static Material matLampNeutralEmissive;
    private static Material matLampCoolEmissive;
    private static Material matScreenSecurity;
    private static Material matScreenOffice;
    private static Material matServerLedCyan;
    private static Material matConfidentialFolder;
    private static Material matWhiteboard;
    private static Material matPaperStack;

    // =========================================================================
    // EDITOR MENU COMMANDS
    // =========================================================================

    [MenuItem("Tools/Gardien Furtif/Office Heist/Build or Rebuild Level", false, 10)]
    public static void BuildOrRebuildLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, TargetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Scene Verification — Gardien Furtif",
                $"The active scene is '{activeScene.name}', not '{TargetSceneName}'.\n\n" +
                $"It is strongly recommended to open '{TargetSceneName}' before building.\n" +
                "Do you still want to build inside the current scene?",
                "Build Here Anyway",
                "Cancel");
            if (!proceed) return;
        }

        GameObject existingNew = GameObject.Find(RootName);
        GameObject existingLegacy = GameObject.Find(LegacyRootName);

        if (existingNew != null || existingLegacy != null)
        {
            string foundNames = existingNew != null && existingLegacy != null
                ? $"'{RootName}' and legacy '{LegacyRootName}'"
                : (existingNew != null ? $"'{RootName}'" : $"'{LegacyRootName}'");

            bool confirm = EditorUtility.DisplayDialog(
                "Rebuild Office Heist Environment",
                $"Existing generated environment ({foundNames}) was found in this scene.\n\n" +
                "Replace only the generated Office Heist root? All other scene objects, cameras, lights, and scripts will remain untouched.",
                "Replace Generated Level",
                "Cancel");
            if (!confirm) return;
        }

        EnsureMaterials();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Two-Storey Office Heist Environment");

        if (existingNew != null) Undo.DestroyObjectImmediate(existingNew);
        if (existingLegacy != null) Undo.DestroyObjectImmediate(existingLegacy);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create OfficeHeist_Generated");

        // Top-level required hierarchy groups
        Transform archRoot = CreateGroup("Building_Architecture", root.transform);
        Transform groundFloorRoot = CreateGroup("GroundFloor", archRoot);
        Transform upperFloorRoot = CreateGroup("UpperFloor", archRoot);
        Transform staircaseRoot = CreateGroup("Staircase", archRoot);
        Transform roofRoot = CreateGroup("Roof_Generated", archRoot);
        Transform gfCeilingsRoot = CreateGroup("GroundFloor_Ceilings", archRoot);
        Transform ufCeilingsRoot = CreateGroup("UpperFloor_Ceilings", archRoot);

        Transform furnitureRoot = CreateGroup("Furniture", root.transform);
        Transform exteriorRoot = CreateGroup("Exterior", root.transform);
        Transform lightingRoot = CreateGroup("Lighting", root.transform);
        Transform markersRoot = CreateGroup("GameplayMarkers", root.transform);

        // Build Architecture
        BuildGroundFloorArchitecture(groundFloorRoot);
        BuildUpperFloorArchitecture(upperFloorRoot);
        BuildStaircase(staircaseRoot);
        BuildCeilingsAndRoof(gfCeilingsRoot, ufCeilingsRoot, roofRoot);

        // Build Furniture
        BuildAllFurniture(furnitureRoot);

        // Build Exterior
        BuildExteriorEnvironment(exteriorRoot);

        // Build Lighting
        BuildLightingSetup(lightingRoot);

        // Build Gameplay Markers & Patrol Points
        BuildGameplayMarkers(markersRoot);

        // Build Optional Disabled Overview Camera
        BuildOptionalOverviewCamera(root.transform);

        // Initial inspection state: hide ceilings and roof by default so both floors are immediately inspectable
        gfCeilingsRoot.gameObject.SetActive(false);
        ufCeilingsRoot.gameObject.SetActive(false);
        roofRoot.gameObject.SetActive(false);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);

        FrameFullBuilding();

        Debug.Log(
            "[OfficeHeistLevelBuilder] Two-storey 30m x 20m stealth environment built under 'OfficeHeist_Generated'. " +
            "Ceilings and roof are hidden by default for easy inspection. Use 'Tools > Gardien Furtif > Office Heist' to toggle them.");
    }

    [MenuItem("Tools/Gardien Furtif/Office Heist/Frame Full Building", false, 30)]
    public static void FrameFullBuilding()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Office Heist", $"'{RootName}' was not found in the active scene.", "OK");
            return;
        }

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 3.0f, 0f), Quaternion.Euler(34f, -32f, 0f), 32f);
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    [MenuItem("Tools/Gardien Furtif/Office Heist/Show or Hide Ground Floor Ceilings", false, 50)]
    public static void ToggleGroundFloorCeilings()
    {
        ToggleSubGroup("Building_Architecture/GroundFloor_Ceilings", "Ground Floor Ceilings");
    }

    [MenuItem("Tools/Gardien Furtif/Office Heist/Show or Hide Upper Floor Ceilings", false, 51)]
    public static void ToggleUpperFloorCeilings()
    {
        ToggleSubGroup("Building_Architecture/UpperFloor_Ceilings", "Upper Floor Ceilings");
    }

    [MenuItem("Tools/Gardien Furtif/Office Heist/Show or Hide Roof", false, 52)]
    public static void ToggleRoof()
    {
        ToggleSubGroup("Building_Architecture/Roof_Generated", "Roof");
    }

    [MenuItem("Tools/Gardien Furtif/Office Heist/Enable or Disable Overview Camera", false, 70)]
    public static void ToggleOverviewCamera()
    {
        ToggleSubGroup("Optional_OverviewCamera", "Optional Overview Camera");
    }

    private static void ToggleSubGroup(string relativePath, string label)
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Office Heist", $"'{RootName}' was not found in the current scene. Build the level first.", "OK");
            return;
        }

        Transform target = root.transform.Find(relativePath);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Office Heist", $"Could not find '{relativePath}' under '{RootName}'.", "OK");
            return;
        }

        Undo.RecordObject(target.gameObject, $"Toggle {label}");
        bool newState = !target.gameObject.activeSelf;
        target.gameObject.SetActive(newState);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[OfficeHeistLevelBuilder] {label} is now {(newState ? "VISIBLE / ENABLED" : "HIDDEN / DISABLED")}.");
    }

    // =========================================================================
    // MATERIALS & PROCEDURAL TEXTURE GENERATION (URP + BUILT-IN COMPATIBLE)
    // =========================================================================

    private static void EnsureMaterials()
    {
        EnsureFolderExists("Assets/Generated");
        EnsureFolderExists("Assets/Generated/OfficeHeist");
        EnsureFolderExists(MaterialFolder);

        matExtPlasterMain     = GetOrCreateMaterial("Ext_Plaster_WarmSandstone", new Color(0.83f, 0.78f, 0.69f), 0.0f, 0.18f, TexPattern.Plaster, new Color(0.78f, 0.73f, 0.64f), 8f);
        matExtPlasterAccent   = GetOrCreateMaterial("Ext_Plaster_CreamBeige", new Color(0.68f, 0.62f, 0.53f), 0.0f, 0.22f, TexPattern.Plaster, new Color(0.62f, 0.56f, 0.48f), 6f);
        matIntWallIvory       = GetOrCreateMaterial("Int_Wall_WarmIvory", new Color(0.91f, 0.89f, 0.84f), 0.0f, 0.16f, TexPattern.Plaster, new Color(0.87f, 0.85f, 0.80f), 6f);
        matIntWallAccent      = GetOrCreateMaterial("Int_Wall_NeutralGrey", new Color(0.74f, 0.75f, 0.73f), 0.0f, 0.20f, TexPattern.Plaster, new Color(0.69f, 0.70f, 0.68f), 6f);

        matFloorConcrete      = GetOrCreateMaterial("Floor_PolishedConcrete", new Color(0.62f, 0.63f, 0.61f), 0.05f, 0.42f, TexPattern.TileGrid, new Color(0.55f, 0.56f, 0.54f), 10f);
        matFloorReceptionTile = GetOrCreateMaterial("Floor_ReceptionStoneTile", new Color(0.76f, 0.74f, 0.69f), 0.05f, 0.55f, TexPattern.TileGrid, new Color(0.64f, 0.61f, 0.56f), 8f);
        matFloorCorridor      = GetOrCreateMaterial("Floor_CorridorSlate", new Color(0.46f, 0.48f, 0.49f), 0.05f, 0.38f, TexPattern.TileGrid, new Color(0.39f, 0.41f, 0.42f), 12f);
        matFloorExecutiveWood = GetOrCreateMaterial("Floor_ExecutiveHardwood", new Color(0.42f, 0.27f, 0.16f), 0.0f, 0.48f, TexPattern.WoodGrain, new Color(0.33f, 0.20f, 0.11f), 8f);
        matFloorServerTech    = GetOrCreateMaterial("Floor_ServerTechDark", new Color(0.20f, 0.23f, 0.26f), 0.25f, 0.50f, TexPattern.TileGrid, new Color(0.12f, 0.15f, 0.18f), 12f);

        matCeilingTile        = GetOrCreateMaterial("Ceiling_AcousticTile", new Color(0.89f, 0.89f, 0.87f), 0.0f, 0.10f, TexPattern.TileGrid, new Color(0.80f, 0.80f, 0.78f), 12f);
        matRoofMembrane       = GetOrCreateMaterial("Roof_ModernSlate", new Color(0.34f, 0.35f, 0.36f), 0.05f, 0.20f, TexPattern.Plaster, new Color(0.28f, 0.29f, 0.30f), 8f);

        matDoorWalnut         = GetOrCreateMaterial("Door_DarkWalnut", new Color(0.28f, 0.17f, 0.10f), 0.0f, 0.42f, TexPattern.WoodGrain, new Color(0.21f, 0.12f, 0.07f), 4f);
        matWoodWalnut         = GetOrCreateMaterial("Furniture_WalnutWood", new Color(0.35f, 0.22f, 0.13f), 0.0f, 0.45f, TexPattern.WoodGrain, new Color(0.26f, 0.15f, 0.08f), 4f);
        matWoodLightOak       = GetOrCreateMaterial("Furniture_WarmOak", new Color(0.58f, 0.44f, 0.29f), 0.0f, 0.38f, TexPattern.WoodGrain, new Color(0.48f, 0.35f, 0.22f), 4f);

        matMetalCharcoal      = GetOrCreateMaterial("Metal_CharcoalFrame", new Color(0.16f, 0.18f, 0.20f), 0.70f, 0.48f, TexPattern.Flat, Color.black, 1f);
        matMetalBrushed       = GetOrCreateMaterial("Metal_BrushedSteel", new Color(0.56f, 0.59f, 0.61f), 0.80f, 0.62f, TexPattern.Flat, Color.black, 1f);
        matGlassTinted        = GetOrCreateGlassMaterial("Glass_TintedBlueGrey", new Color(0.24f, 0.38f, 0.46f, 0.38f), 0.15f, 0.88f);

        matFabricGrey         = GetOrCreateMaterial("Furniture_FabricMutedGrey", new Color(0.34f, 0.37f, 0.39f), 0.0f, 0.15f, TexPattern.Plaster, new Color(0.28f, 0.31f, 0.33f), 10f);
        matFabricExecutive    = GetOrCreateMaterial("Furniture_FabricWarmTaupe", new Color(0.46f, 0.41f, 0.36f), 0.0f, 0.18f, TexPattern.Plaster, new Color(0.38f, 0.33f, 0.29f), 8f);
        matLeatherDark        = GetOrCreateMaterial("Furniture_LeatherCharcoal", new Color(0.15f, 0.14f, 0.14f), 0.05f, 0.52f, TexPattern.Flat, Color.black, 1f);

        matAsphalt            = GetOrCreateMaterial("Ext_AsphaltRoad", new Color(0.19f, 0.20f, 0.21f), 0.0f, 0.15f, TexPattern.Plaster, new Color(0.14f, 0.15f, 0.16f), 14f);
        matSidewalkConcrete   = GetOrCreateMaterial("Ext_SidewalkConcrete", new Color(0.64f, 0.63f, 0.60f), 0.0f, 0.22f, TexPattern.TileGrid, new Color(0.55f, 0.54f, 0.51f), 10f);
        matCourtyardPaving    = GetOrCreateMaterial("Ext_CourtyardStonePaving", new Color(0.56f, 0.53f, 0.48f), 0.0f, 0.28f, TexPattern.TileGrid, new Color(0.46f, 0.43f, 0.39f), 12f);
        matGrassOlive         = GetOrCreateMaterial("Ext_GrassOlive", new Color(0.28f, 0.38f, 0.23f), 0.0f, 0.08f, TexPattern.Plaster, new Color(0.22f, 0.32f, 0.18f), 12f);
        matFoliageGreen       = GetOrCreateMaterial("Ext_FoliageNaturalGreen", new Color(0.21f, 0.36f, 0.20f), 0.0f, 0.12f, TexPattern.Flat, Color.black, 1f);
        matTreeBark           = GetOrCreateMaterial("Ext_TreeBarkBrown", new Color(0.32f, 0.23f, 0.16f), 0.0f, 0.12f, TexPattern.WoodGrain, new Color(0.24f, 0.17f, 0.11f), 4f);

        matLampWarmEmissive    = GetOrCreateEmissiveMaterial("Light_WarmEmissive", new Color(1.0f, 0.90f, 0.74f), new Color(1.0f, 0.85f, 0.62f) * 1.8f);
        matLampNeutralEmissive = GetOrCreateEmissiveMaterial("Light_NeutralEmissive", new Color(0.95f, 0.97f, 1.0f), new Color(0.90f, 0.94f, 1.0f) * 1.8f);
        matLampCoolEmissive    = GetOrCreateEmissiveMaterial("Light_CoolServerEmissive", new Color(0.55f, 0.84f, 1.0f), new Color(0.35f, 0.75f, 1.0f) * 2.0f);

        matScreenSecurity     = GetOrCreateEmissiveMaterial("Screen_SecuritySurveillance", new Color(0.12f, 0.28f, 0.34f), new Color(0.18f, 0.55f, 0.65f) * 1.2f);
        matScreenOffice       = GetOrCreateEmissiveMaterial("Screen_OfficeMonitor", new Color(0.16f, 0.26f, 0.38f), new Color(0.20f, 0.42f, 0.68f) * 0.9f);
        matServerLedCyan      = GetOrCreateEmissiveMaterial("Server_LED_Cyan", new Color(0.15f, 0.90f, 1.0f), new Color(0.10f, 0.85f, 1.0f) * 2.4f);

        matConfidentialFolder = GetOrCreateMaterial("Prop_ConfidentialFolderRed", new Color(0.68f, 0.11f, 0.09f), 0.0f, 0.30f, TexPattern.Flat, Color.black, 1f);
        matWhiteboard         = GetOrCreateMaterial("Prop_WhiteboardSurface", new Color(0.95f, 0.96f, 0.97f), 0.05f, 0.78f, TexPattern.Flat, Color.black, 1f);
        matPaperStack         = GetOrCreateMaterial("Prop_PaperOffWhite", new Color(0.90f, 0.89f, 0.85f), 0.0f, 0.15f, TexPattern.Flat, Color.black, 1f);

        AssetDatabase.SaveAssets();
    }

    private enum TexPattern
    {
        Flat,
        TileGrid,
        WoodGrain,
        Plaster
    }

    private static Shader ResolveCompatibleLitShader()
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null) return urpLit;
            Shader hdrpLit = Shader.Find("HDRP/Lit");
            if (hdrpLit != null) return hdrpLit;
        }

        Shader standard = Shader.Find("Standard");
        if (standard != null) return standard;

        Shader fallbackUrp = Shader.Find("Universal Render Pipeline/Lit");
        if (fallbackUrp != null) return fallbackUrp;

        return Shader.Find("Sprites/Default");
    }

    private static Material GetOrCreateMaterial(
        string name,
        Color baseColor,
        float metallic,
        float smoothness,
        TexPattern pattern,
        Color secondaryColor,
        float tiling)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader targetShader = ResolveCompatibleLitShader();

        if (mat == null)
        {
            mat = new Material(targetShader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (targetShader != null && mat.shader != targetShader)
        {
            mat.shader = targetShader;
        }

        ApplyColorAndSurfaceProperties(mat, baseColor, metallic, smoothness);

        if (pattern != TexPattern.Flat)
        {
            Texture2D existingTex = GetExistingSubTexture(path);
            if (existingTex == null)
            {
                existingTex = GenerateProceduralTexture(name + "_Tex", pattern, baseColor, secondaryColor);
                AssetDatabase.AddObjectToAsset(existingTex, mat);
            }

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", existingTex);
                mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", existingTex);
                mat.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
            }
            // Set base color multiplier to white when texture already encodes the base tone
            ApplyColorAndSurfaceProperties(mat, Color.white, metallic, smoothness);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateGlassMaterial(string name, Color tintColor, float metallic, float smoothness)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader targetShader = ResolveCompatibleLitShader();

        if (mat == null)
        {
            mat = new Material(targetShader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (targetShader != null && mat.shader != targetShader)
        {
            mat.shader = targetShader;
        }

        ApplyColorAndSurfaceProperties(mat, tintColor, metallic, smoothness);

        // Configure transparency for both URP Lit and Built-in Standard
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1.0f); // Transparent in URP
            mat.SetFloat("_Blend", 0.0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3.0f); // Transparent in Built-in Standard
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateEmissiveMaterial(string name, Color baseColor, Color emissionColor)
    {
        Material mat = GetOrCreateMaterial(name, baseColor, 0.0f, 0.25f, TexPattern.Flat, Color.black, 1f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", emissionColor);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ApplyColorAndSurfaceProperties(Material mat, Color color, float metallic, float smoothness)
    {
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
    }

    private static Texture2D GetExistingSubTexture(string assetPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object obj in assets)
        {
            if (obj is Texture2D tex) return tex;
        }
        return null;
    }

    private static Texture2D GenerateProceduralTexture(string texName, TexPattern pattern, Color primary, Color secondary)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = texName,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = primary;
                if (pattern == TexPattern.TileGrid)
                {
                    bool groutLine = (x <= 1 || y <= 1 || x == size / 2 || y == size / 2);
                    float micro = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.15f;
                    c = groutLine ? secondary : Color.Lerp(primary, secondary, micro);
                }
                else if (pattern == TexPattern.WoodGrain)
                {
                    float grain = Mathf.Sin((x * 0.45f) + Mathf.PerlinNoise(x * 0.12f, y * 0.06f) * 4.0f) * 0.5f + 0.5f;
                    c = Color.Lerp(primary, secondary, grain * 0.55f);
                }
                else if (pattern == TexPattern.Plaster)
                {
                    float n = Mathf.PerlinNoise(x * 0.22f + 1.7f, y * 0.22f + 3.1f);
                    c = Color.Lerp(primary, secondary, n * 0.35f);
                }
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(true, false);
        return tex;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderExists(parent);
        }
        AssetDatabase.CreateFolder(parent, name);
    }

    // =========================================================================
    // GROUND FLOOR ARCHITECTURE (30m x 20m, Y = 0.0m to 3.20m)
    // =========================================================================

    private static void BuildGroundFloorArchitecture(Transform gfRoot)
    {
        Transform floors = CreateGroup("Floors", gfRoot);
        Transform extWalls = CreateGroup("ExteriorWalls", gfRoot);
        Transform intWalls = CreateGroup("InteriorWalls", gfRoot);
        Transform doorsAndFrames = CreateGroup("DoorsAndFrames", gfRoot);
        Transform windows = CreateGroup("Windows", gfRoot);
        Transform rooms = CreateGroup("GroundFloorRooms", gfRoot);

        // Room organizational labels under GroundFloorRooms
        CreateRoomLabel(rooms, "Room_MainEntranceAndReception", new Vector3(-9.0f, 0.0f, -6.0f));
        CreateRoomLabel(rooms, "Room_SecurityRoom", new Vector3(-10.5f, 0.0f, 6.0f));
        CreateRoomLabel(rooms, "Room_ArchiveAndStorage", new Vector3(-2.0f, 0.0f, 6.0f));
        CreateRoomLabel(rooms, "Room_OpenWorkspace", new Vector3(2.5f, 0.0f, -6.0f));
        CreateRoomLabel(rooms, "Room_MeetingRoom", new Vector3(11.5f, 0.0f, -6.0f));
        CreateRoomLabel(rooms, "Room_StaircaseAndServiceHall", new Vector3(8.5f, 0.0f, 6.0f));
        CreateRoomLabel(rooms, "Room_MainCorridor", new Vector3(0.0f, 0.0f, 0.0f));

        // 1. Main Structural Ground Slab (30m x 20m, top surface at Y = 0.0f)
        CreateBox(floors, "GF_Structural_Slab", new Vector3(0f, -0.15f, 0f), new Vector3(30.0f, 0.30f, 20.0f), matFloorConcrete, true);

        // Distinct Floor Finishes (thin non-tripping overlays at Y = 0.005f)
        CreateBox(floors, "GF_Finish_ReceptionStone", new Vector3(-9.0f, 0.005f, -6.0f), new Vector3(11.8f, 0.01f, 7.8f), matFloorReceptionTile, false);
        CreateBox(floors, "GF_Finish_MainCorridor", new Vector3(0.0f, 0.005f, 0.0f), new Vector3(29.6f, 0.01f, 3.9f), matFloorCorridor, false);
        CreateBox(floors, "GF_Finish_SecurityTech", new Vector3(-10.5f, 0.005f, 6.0f), new Vector3(8.8f, 0.01f, 7.8f), matFloorServerTech, false);
        CreateBox(floors, "GF_Finish_MeetingWood", new Vector3(11.5f, 0.005f, -6.0f), new Vector3(6.8f, 0.01f, 7.8f), matFloorExecutiveWood, false);

        // 2. Ground Floor Exterior Walls (30m x 20m perimeter)
        // South Wall (Z = -10.0f): Main Entrance double door at X = -9.0f (width 2.4m), plus 4 windows
        BuildWallAlongX(
            extWalls, windows, doorsAndFrames,
            "GF_South_ExtWall", -HalfDepth, -HalfWidth, HalfWidth, GroundFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            new[] { new OpeningSpec(-9.0f, 2.4f, true) },
            new[] { new WindowSpec(-13.0f, 2.0f, 0.95f, 2.45f), new WindowSpec(-5.2f, 2.0f, 0.95f, 2.45f), new WindowSpec(2.5f, 3.0f, 0.95f, 2.45f), new WindowSpec(11.5f, 2.6f, 0.95f, 2.45f) });

        // North Wall (Z = +10.0f): Rear Service Entrance door at X = 5.5f (width 1.6m), plus windows
        BuildWallAlongX(
            extWalls, windows, doorsAndFrames,
            "GF_North_ExtWall", HalfDepth, -HalfWidth, HalfWidth, GroundFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            new[] { new OpeningSpec(5.5f, 1.6f, false) },
            new[] { new WindowSpec(-10.5f, 2.4f, 1.15f, 2.45f), new WindowSpec(-2.0f, 2.4f, 1.15f, 2.45f), new WindowSpec(12.0f, 2.4f, 0.95f, 2.45f) });

        // West Wall (X = -15.0f): Windows for Reception & Security
        BuildWallAlongZ(
            extWalls, windows, doorsAndFrames,
            "GF_West_ExtWall", -HalfWidth, -HalfDepth, HalfDepth, GroundFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            Array.Empty<OpeningSpec>(),
            new[] { new WindowSpec(-6.0f, 2.6f, 0.95f, 2.45f), new WindowSpec(6.0f, 2.2f, 1.15f, 2.45f) });

        // East Wall (X = +15.0f): East Extraction Exit door at Z = 0.0f (width 1.8m), plus windows
        BuildWallAlongZ(
            extWalls, windows, doorsAndFrames,
            "GF_East_ExtWall", HalfWidth, -HalfDepth, HalfDepth, GroundFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            new[] { new OpeningSpec(0.0f, 1.8f, false) },
            new[] { new WindowSpec(-6.0f, 2.4f, 0.95f, 2.45f), new WindowSpec(6.0f, 2.4f, 0.95f, 2.45f) });

        // Exterior Architectural Band Trim between Ground & Upper Floor
        CreateBox(extWalls, "Facade_MidBand_South", new Vector3(0f, 3.30f, -10.18f), new Vector3(30.4f, 0.24f, 0.12f), matExtPlasterAccent, false);
        CreateBox(extWalls, "Facade_MidBand_North", new Vector3(0f, 3.30f, 10.18f), new Vector3(30.4f, 0.24f, 0.12f), matExtPlasterAccent, false);
        CreateBox(extWalls, "Facade_MidBand_West", new Vector3(-15.18f, 3.30f, 0f), new Vector3(0.12f, 0.24f, 20.4f), matExtPlasterAccent, false);
        CreateBox(extWalls, "Facade_MidBand_East", new Vector3(15.18f, 3.30f, 0f), new Vector3(0.12f, 0.24f, 20.4f), matExtPlasterAccent, false);

        // 3. Ground Floor Interior Walls & Doors
        // South Corridor Wall (Z = -2.0f): Doors into Reception (-9.0), Open Workspace (+2.5), Meeting Room (+11.5)
        BuildWallAlongX(
            intWalls, windows, doorsAndFrames,
            "GF_SouthCorridor_Wall", -2.0f, -HalfWidth, HalfWidth, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-9.0f, 2.2f, true), new OpeningSpec(2.5f, 1.8f, false), new OpeningSpec(11.5f, 1.6f, false) },
            new[] { new WindowSpec(-5.5f, 2.2f, 1.05f, 2.30f), new WindowSpec(-0.5f, 2.2f, 1.05f, 2.30f), new WindowSpec(6.0f, 2.2f, 1.05f, 2.30f) });

        // North Corridor Wall (Z = +2.0f): Doors into Security (-10.5), Archive (-2.0), Service Bay (+5.5), Stair Hall (+12.0)
        BuildWallAlongX(
            intWalls, windows, doorsAndFrames,
            "GF_NorthCorridor_Wall", 2.0f, -HalfWidth, HalfWidth, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-10.5f, 1.5f, false), new OpeningSpec(-2.0f, 1.5f, false), new OpeningSpec(5.5f, 1.6f, false), new OpeningSpec(12.0f, 2.2f, true) },
            new[] { new WindowSpec(-8.0f, 1.6f, 1.15f, 2.25f) });

        // South Zone Room Dividers (Z = -10.0f to -2.0f)
        // Reception | Workspace divider at X = -3.0f (door at Z = -6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "GF_Divider_Reception_Workspace", -3.0f, -10.0f, -2.0f, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-6.0f, 1.6f, false) },
            new[] { new WindowSpec(-3.8f, 1.4f, 1.05f, 2.30f) });

        // Workspace | Meeting Room divider at X = +8.0f (door at Z = -6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "GF_Divider_Workspace_Meeting", 8.0f, -10.0f, -2.0f, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-6.0f, 1.6f, false) },
            new[] { new WindowSpec(-8.3f, 1.6f, 1.05f, 2.30f) });

        // North Zone Room Dividers (Z = +2.0f to +10.0f)
        // Security | Archive divider at X = -6.0f (flanking stealth door at Z = +6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "GF_Divider_Security_Archive", -6.0f, 2.0f, 10.0f, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(6.0f, 1.4f, false) },
            Array.Empty<WindowSpec>());

        // Archive | Service Lobby divider at X = +2.0f (door at Z = +6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "GF_Divider_Archive_Service", 2.0f, 2.0f, 10.0f, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(6.0f, 1.5f, false) },
            Array.Empty<WindowSpec>());

        // Service Lobby | Staircase Hall divider at X = +9.0f (wide walkthrough opening at Z = +4.2f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "GF_Divider_Service_StairHall", 9.0f, 2.0f, 10.0f, GroundFloorY, ClearFloorHeight, IntWallThickness, matIntWallAccent,
            new[] { new OpeningSpec(4.2f, 2.0f, true) },
            Array.Empty<WindowSpec>());

        // Structural Columns along Main Corridor for stealth cover & architectural realism
        float[] columnXs = { -12.0f, -6.0f, 0.0f, 6.0f };
        foreach (float cx in columnXs)
        {
            CreateBox(intWalls, $"GF_Column_North_{cx}", new Vector3(cx, ClearFloorHeight * 0.5f, 1.75f), new Vector3(0.38f, ClearFloorHeight, 0.38f), matIntWallAccent, true);
            CreateBox(intWalls, $"GF_Column_South_{cx}", new Vector3(cx, ClearFloorHeight * 0.5f, -1.75f), new Vector3(0.38f, ClearFloorHeight, 0.38f), matIntWallAccent, true);
        }
    }

    // =========================================================================
    // UPPER FLOOR ARCHITECTURE (30m x 20m, Y = 3.40m to 6.60m)
    // =========================================================================

    private static void BuildUpperFloorArchitecture(Transform ufRoot)
    {
        Transform floors = CreateGroup("Floors", ufRoot);
        Transform extWalls = CreateGroup("ExteriorWalls", ufRoot);
        Transform intWalls = CreateGroup("InteriorWalls", ufRoot);
        Transform doorsAndFrames = CreateGroup("DoorsAndFrames", ufRoot);
        Transform windows = CreateGroup("Windows", ufRoot);
        Transform rooms = CreateGroup("UpperFloorRooms", ufRoot);

        // Room organizational labels under UpperFloorRooms
        CreateRoomLabel(rooms, "Room_DirectorOffice_MainObjective", new Vector3(-9.0f, UpperFloorY, 6.0f));
        CreateRoomLabel(rooms, "Room_UpperDocumentArchive", new Vector3(3.0f, UpperFloorY, 6.0f));
        CreateRoomLabel(rooms, "Room_ServerRoom", new Vector3(-10.0f, UpperFloorY, -6.0f));
        CreateRoomLabel(rooms, "Room_SecondaryOffice", new Vector3(5.0f, UpperFloorY, -6.0f));
        CreateRoomLabel(rooms, "Room_UpperCorridorAndLanding", new Vector3(0.0f, UpperFloorY, 0.0f));

        // 1. Upper Floor Walkable Slabs (Y = 3.20m to 3.40m) WITH Stairwell Cutout at X:[11.0, 13.0], Z:[2.2, 8.2]
        float slabCenterY = UpperFloorY - (UpperSlabThickness * 0.5f); // 3.30f
        // Slab A: Everything West of Stair Cutout (X: -15.0 to +11.0, Z: -10.0 to +10.0) -> Width 26m, Depth 20m
        CreateBox(floors, "UF_Slab_MainWest", new Vector3(-2.0f, slabCenterY, 0.0f), new Vector3(26.0f, UpperSlabThickness, 20.0f), matFloorConcrete, true);
        // Slab B: South-East of Stair Cutout (X: +11.0 to +15.0, Z: -10.0 to +2.2) -> Width 4m, Depth 12.2m
        CreateBox(floors, "UF_Slab_SouthEast", new Vector3(13.0f, slabCenterY, -3.9f), new Vector3(4.0f, UpperSlabThickness, 12.2f), matFloorConcrete, true);
        // Slab C: Top Stair Landing North of Cutout (X: +11.0 to +15.0, Z: +8.2 to +10.0) -> Width 4m, Depth 1.8m
        CreateBox(floors, "UF_Slab_StairTopLanding", new Vector3(13.0f, slabCenterY, 9.1f), new Vector3(4.0f, UpperSlabThickness, 1.8f), matFloorConcrete, true);
        // Slab D: East Gallery Strip beside Cutout (X: +13.0 to +15.0, Z: +2.2 to +8.2) -> Width 2m, Depth 6.0m
        CreateBox(floors, "UF_Slab_EastGallery", new Vector3(14.0f, slabCenterY, 5.2f), new Vector3(2.0f, UpperSlabThickness, 6.0f), matFloorConcrete, true);

        // Distinct Floor Finishes on Upper Floor (at Y = 3.405f)
        CreateBox(floors, "UF_Finish_DirectorHardwood", new Vector3(-9.0f, UpperFloorY + 0.005f, 6.0f), new Vector3(11.8f, 0.01f, 7.8f), matFloorExecutiveWood, false);
        CreateBox(floors, "UF_Finish_ServerTech", new Vector3(-10.0f, UpperFloorY + 0.005f, -6.0f), new Vector3(9.8f, 0.01f, 7.8f), matFloorServerTech, false);
        CreateBox(floors, "UF_Finish_UpperCorridor", new Vector3(0.0f, UpperFloorY + 0.005f, 0.0f), new Vector3(29.6f, 0.01f, 3.9f), matFloorCorridor, false);

        // 2. Upper Floor Exterior Walls (Y = 3.40m to 6.60m)
        // South Wall (Z = -10.0f): Windows for Server Room (high narrow) and Secondary Office (panoramic)
        BuildWallAlongX(
            extWalls, windows, doorsAndFrames,
            "UF_South_ExtWall", -HalfDepth, -HalfWidth, HalfWidth, UpperFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            Array.Empty<OpeningSpec>(),
            new[] { new WindowSpec(-10.0f, 2.2f, 1.35f, 2.45f), new WindowSpec(-1.0f, 2.8f, 0.95f, 2.45f), new WindowSpec(5.5f, 2.8f, 0.95f, 2.45f), new WindowSpec(11.5f, 2.6f, 0.95f, 2.45f) });

        // North Wall (Z = +10.0f): Large Executive Windows for Director's Office, plus Archive & Stairwell windows
        BuildWallAlongX(
            extWalls, windows, doorsAndFrames,
            "UF_North_ExtWall", HalfDepth, -HalfWidth, HalfWidth, UpperFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            Array.Empty<OpeningSpec>(),
            new[] { new WindowSpec(-11.5f, 3.2f, 0.85f, 2.50f), new WindowSpec(-6.2f, 2.8f, 0.85f, 2.50f), new WindowSpec(3.0f, 2.6f, 1.05f, 2.45f), new WindowSpec(12.0f, 2.4f, 0.95f, 2.45f) });

        // West Wall (X = -15.0f): Executive panorama window for Director's Office (North-West)
        BuildWallAlongZ(
            extWalls, windows, doorsAndFrames,
            "UF_West_ExtWall", -HalfWidth, -HalfDepth, HalfDepth, UpperFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            Array.Empty<OpeningSpec>(),
            new[] { new WindowSpec(6.0f, 3.2f, 0.85f, 2.50f), new WindowSpec(-6.0f, 2.0f, 1.35f, 2.45f) });

        // East Wall (X = +15.0f): Windows for Secondary Office, Corridor, and Stair Gallery
        BuildWallAlongZ(
            extWalls, windows, doorsAndFrames,
            "UF_East_ExtWall", HalfWidth, -HalfDepth, HalfDepth, UpperFloorY, ClearFloorHeight, ExtWallThickness, matExtPlasterMain,
            Array.Empty<OpeningSpec>(),
            new[] { new WindowSpec(-6.0f, 2.6f, 0.95f, 2.45f), new WindowSpec(0.0f, 2.0f, 0.95f, 2.45f), new WindowSpec(6.0f, 2.6f, 0.95f, 2.45f) });

        // 3. Upper Floor Interior Walls & Doors
        // South Corridor Wall (Z = -2.0f): Doors into Server Room (-10.0), Secondary Office West (+0.5), Secondary Office East (+10.5)
        BuildWallAlongX(
            intWalls, windows, doorsAndFrames,
            "UF_SouthCorridor_Wall", -2.0f, -HalfWidth, HalfWidth, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-10.0f, 1.6f, false), new OpeningSpec(0.5f, 1.6f, false), new OpeningSpec(10.5f, 1.6f, false) },
            new[] { new WindowSpec(-7.0f, 1.8f, 1.10f, 2.30f), new WindowSpec(4.5f, 2.4f, 1.05f, 2.30f) });

        // North Corridor Wall (Z = +2.0f, from X = -15.0 to +9.0; X = +9.0 to +15.0 opens to Stairwell Gallery):
        // Doors into Director's Office (-9.0, grand double door) and Upper Document Archive (+3.0)
        BuildWallAlongX(
            intWalls, windows, doorsAndFrames,
            "UF_NorthCorridor_Wall", 2.0f, -HalfWidth, 9.0f, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-9.0f, 2.0f, true), new OpeningSpec(3.0f, 1.6f, false) },
            new[] { new WindowSpec(-5.5f, 2.0f, 1.05f, 2.30f) });

        // South Zone Divider: Server Room | Secondary Office at X = -5.0f (door at Z = -6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "UF_Divider_Server_SecondaryOffice", -5.0f, -10.0f, -2.0f, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(-6.0f, 1.5f, false) },
            Array.Empty<WindowSpec>());

        // Partial Partition inside Secondary Office at X = +6.0f (wide opening at Z = -6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "UF_Partition_SecondaryOffice", 6.0f, -10.0f, -2.0f, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallAccent,
            new[] { new OpeningSpec(-6.0f, 2.4f, true) },
            new[] { new WindowSpec(-3.4f, 1.6f, 1.05f, 2.30f) });

        // North Zone Dividers:
        // Director's Office | Upper Archive at X = -3.0f (stealth side entry door at Z = +6.5f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "UF_Divider_Director_Archive", -3.0f, 2.0f, 10.0f, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(6.5f, 1.5f, false) },
            Array.Empty<WindowSpec>());

        // Upper Archive | Stairwell Gallery at X = +9.0f (door at Z = +6.0f)
        BuildWallAlongZ(
            intWalls, windows, doorsAndFrames,
            "UF_Divider_Archive_StairGallery", 9.0f, 2.0f, 10.0f, UpperFloorY, ClearFloorHeight, IntWallThickness, matIntWallIvory,
            new[] { new OpeningSpec(6.0f, 1.6f, false) },
            new[] { new WindowSpec(3.6f, 1.6f, 1.05f, 2.30f) });
    }

    // =========================================================================
    // STAIRCASE (CONNECTING Y = 0.0m TO Y = 3.40m WITH GUARDRAILS & NAV COLLIDER)
    // =========================================================================

    private static void BuildStaircase(Transform stairRoot)
    {
        // Staircase runs inside X: [11.2, 12.8] (width 1.6m) from Z = 2.2f (bottom, Y = 0.0f) to Z = 8.2f (top landing, Y = 3.40f)
        const int stepCount = 20;
        const float stairWidth = 1.60f;
        const float centerX = 12.0f;
        const float startZ = 2.20f;
        const float totalRun = 6.00f; // Z: 2.20 to 8.20
        const float totalRise = UpperFloorY - GroundFloorY; // 3.40m

        float stepRun = totalRun / stepCount;   // 0.30m tread depth
        float stepRise = totalRise / stepCount; // 0.17m riser height

        Transform stepsGroup = CreateGroup("Stair_Steps", stairRoot);
        Transform railingsGroup = CreateGroup("Stair_Railings", stairRoot);

        // Ground floor departure landing pad
        CreateBox(stepsGroup, "Stair_Bottom_LandingPad", new Vector3(centerX, 0.01f, 1.55f), new Vector3(2.2f, 0.02f, 1.3f), matFloorCorridor, false);

        for (int i = 0; i < stepCount; i++)
        {
            float topY = (i + 1) * stepRise;
            float zCenter = startZ + (i + 0.5f) * stepRun;

            // Solid structural step riser/carriage
            CreateBox(
                stepsGroup,
                $"Step_Riser_{i + 1:00}",
                new Vector3(centerX, topY - (stepRise * 0.5f), zCenter),
                new Vector3(stairWidth, stepRise, stepRun),
                matFloorConcrete,
                true);

            // Walnut wood step tread nosing cap
            CreateBox(
                stepsGroup,
                $"Step_TreadCap_{i + 1:00}",
                new Vector3(centerX, topY - 0.015f, zCenter - 0.01f),
                new Vector3(stairWidth + 0.04f, 0.035f, stepRun + 0.03f),
                matWoodWalnut,
                false);
        }

        // Sloped Stringer Beams & Smooth Physics/NavMesh Walkable Ramp
        float slopeLength = Mathf.Sqrt(totalRun * totalRun + totalRise * totalRise);
        float slopeAngle = -Mathf.Atan2(totalRise, totalRun) * Mathf.Rad2Deg;
        Vector3 slopeMidPoint = new Vector3(centerX, totalRise * 0.5f, startZ + totalRun * 0.5f);

        // Left & Right steel stringers
        GameObject leftStringer = CreateBox(stepsGroup, "Stair_Stringer_Left", slopeMidPoint + new Vector3(-stairWidth * 0.5f - 0.04f, -0.08f, 0f), new Vector3(0.08f, 0.24f, slopeLength), matMetalCharcoal, false);
        leftStringer.transform.rotation = Quaternion.Euler(slopeAngle, 0f, 0f);

        GameObject rightStringer = CreateBox(stepsGroup, "Stair_Stringer_Right", slopeMidPoint + new Vector3(stairWidth * 0.5f + 0.04f, -0.08f, 0f), new Vector3(0.08f, 0.24f, slopeLength), matMetalCharcoal, false);
        rightStringer.transform.rotation = Quaternion.Euler(slopeAngle, 0f, 0f);

        // Smooth invisible physics collider flush with step noses so FPS CharacterControllers & NavMeshAgents never snag
        GameObject smoothRamp = new GameObject("Stair_SmoothWalkCollider");
        smoothRamp.transform.SetParent(stairRoot, false);
        smoothRamp.transform.position = slopeMidPoint;
        smoothRamp.transform.rotation = Quaternion.Euler(slopeAngle, 0f, 0f);
        BoxCollider rampCol = smoothRamp.AddComponent<BoxCollider>();
        rampCol.size = new Vector3(stairWidth, 0.05f, slopeLength);

        // Sloped Handrails (Left & Right, 0.95m above stair slope)
        GameObject leftHandrail = CreateBox(railingsGroup, "Stair_Handrail_Left", slopeMidPoint + new Vector3(-stairWidth * 0.5f - 0.04f, 0.95f, 0f), new Vector3(0.06f, 0.06f, slopeLength + 0.2f), matMetalBrushed, true);
        leftHandrail.transform.rotation = Quaternion.Euler(slopeAngle, 0f, 0f);

        GameObject rightHandrail = CreateBox(railingsGroup, "Stair_Handrail_Right", slopeMidPoint + new Vector3(stairWidth * 0.5f + 0.04f, 0.95f, 0f), new Vector3(0.06f, 0.06f, slopeLength + 0.2f), matMetalBrushed, true);
        rightHandrail.transform.rotation = Quaternion.Euler(slopeAngle, 0f, 0f);

        // Vertical Baluster Posts along the stairs
        for (int p = 0; p <= 5; p++)
        {
            float t = p / 5f;
            float pz = startZ + t * totalRun;
            float py = t * totalRise;
            CreateBox(railingsGroup, $"Stair_Post_Left_{p}", new Vector3(centerX - stairWidth * 0.5f - 0.04f, py + 0.48f, pz), new Vector3(0.05f, 0.96f, 0.05f), matMetalCharcoal, false);
            CreateBox(railingsGroup, $"Stair_Post_Right_{p}", new Vector3(centerX + stairWidth * 0.5f + 0.04f, py + 0.48f, pz), new Vector3(0.05f, 0.96f, 0.05f), matMetalCharcoal, false);
        }

        // Upper Floor Stairwell Safety Guardrails around Cutout (X: 11.0..13.0, Z: 2.2..8.2)
        // Left guardrail along X = 10.95f (Z = 2.2 to 8.2)
        BuildGlassBalustradeZ(railingsGroup, "UF_Stairwell_Guardrail_West", 10.95f, StairCutoutMinZ, StairCutoutMaxZ, UpperFloorY);
        // Right guardrail along X = 13.05f (Z = 2.2 to 8.2)
        BuildGlassBalustradeZ(railingsGroup, "UF_Stairwell_Guardrail_East", 13.05f, StairCutoutMinZ, StairCutoutMaxZ, UpperFloorY);
        // South guardrail along Z = 2.15f (X = 10.95 to 13.05) preventing falls from the corridor side into the stairwell
        BuildGlassBalustradeX(railingsGroup, "UF_Stairwell_Guardrail_South", 2.15f, 10.95f, 13.05f, UpperFloorY);
    }

    private static void BuildGlassBalustradeZ(Transform parent, string name, float x, float minZ, float maxZ, float floorY)
    {
        float length = maxZ - minZ;
        float centerZ = (minZ + maxZ) * 0.5f;
        CreateBox(parent, name + "_TopRail", new Vector3(x, floorY + 1.05f, centerZ), new Vector3(0.07f, 0.06f, length), matMetalBrushed, true);
        CreateBox(parent, name + "_GlassPanel", new Vector3(x, floorY + 0.52f, centerZ), new Vector3(0.04f, 0.96f, length - 0.1f), matGlassTinted, true);
        CreateBox(parent, name + "_PostStart", new Vector3(x, floorY + 0.52f, minZ), new Vector3(0.08f, 1.06f, 0.08f), matMetalCharcoal, false);
        CreateBox(parent, name + "_PostEnd", new Vector3(x, floorY + 0.52f, maxZ), new Vector3(0.08f, 1.06f, 0.08f), matMetalCharcoal, false);
    }

    private static void BuildGlassBalustradeX(Transform parent, string name, float z, float minX, float maxX, float floorY)
    {
        float length = maxX - minX;
        float centerX = (minX + maxX) * 0.5f;
        CreateBox(parent, name + "_TopRail", new Vector3(centerX, floorY + 1.05f, z), new Vector3(length, 0.06f, 0.07f), matMetalBrushed, true);
        CreateBox(parent, name + "_GlassPanel", new Vector3(centerX, floorY + 0.52f, z), new Vector3(length - 0.1f, 0.96f, 0.04f), matGlassTinted, true);
    }

    // =========================================================================
    // CEILINGS & ROOF (INDEPENDENTLY TOGGLEABLE)
    // =========================================================================

    private static void BuildCeilingsAndRoof(Transform gfCeilings, Transform ufCeilings, Transform roofRoot)
    {
        // Ground Floor Acoustic Ceilings (at Y = 3.16f, with stairwell cutout kept open)
        CreateBox(gfCeilings, "GF_Ceiling_MainWest", new Vector3(-2.0f, 3.16f, 0.0f), new Vector3(25.8f, 0.06f, 19.6f), matCeilingTile, false);
        CreateBox(gfCeilings, "GF_Ceiling_SouthEast", new Vector3(13.0f, 3.16f, -3.9f), new Vector3(3.8f, 0.06f, 12.0f), matCeilingTile, false);

        // Upper Floor Acoustic Ceilings (at Y = 6.56f)
        CreateBox(ufCeilings, "UF_Ceiling_Full", new Vector3(0.0f, 6.56f, 0.0f), new Vector3(29.6f, 0.06f, 19.6f), matCeilingTile, false);

        // Modern Architectural Roof (at Y = 6.60m to 7.05m)
        CreateBox(roofRoot, "Roof_MainSlab", new Vector3(0.0f, RoofBaseY + 0.12f, 0.0f), new Vector3(30.6f, 0.24f, 20.6f), matRoofMembrane, true);
        // Perimeter Parapet Walls
        CreateBox(roofRoot, "Roof_Parapet_South", new Vector3(0.0f, RoofBaseY + 0.42f, -10.15f), new Vector3(30.6f, 0.45f, 0.35f), matExtPlasterAccent, false);
        CreateBox(roofRoot, "Roof_Parapet_North", new Vector3(0.0f, RoofBaseY + 0.42f, 10.15f), new Vector3(30.6f, 0.45f, 0.35f), matExtPlasterAccent, false);
        CreateBox(roofRoot, "Roof_Parapet_West", new Vector3(-15.15f, RoofBaseY + 0.42f, 0.0f), new Vector3(0.35f, 0.45f, 20.6f), matExtPlasterAccent, false);
        CreateBox(roofRoot, "Roof_Parapet_East", new Vector3(15.15f, RoofBaseY + 0.42f, 0.0f), new Vector3(0.35f, 0.45f, 20.6f), matExtPlasterAccent, false);
        // Rooftop HVAC Units for realistic exterior silhouette
        CreateBox(roofRoot, "Roof_HVAC_Unit_1", new Vector3(-8.5f, RoofBaseY + 0.65f, -4.5f), new Vector3(2.8f, 0.85f, 1.8f), matMetalBrushed, false);
        CreateBox(roofRoot, "Roof_HVAC_Unit_2", new Vector3(-4.8f, RoofBaseY + 0.65f, -4.5f), new Vector3(2.4f, 0.85f, 1.8f), matMetalBrushed, false);
    }

    // =========================================================================
    // INTERIOR FURNITURE & STEALTH COVER (BOTH FLOORS)
    // =========================================================================

    private static void BuildAllFurniture(Transform furnitureRoot)
    {
        Transform reception = CreateGroup("Reception", furnitureRoot);
        Transform security = CreateGroup("SecurityRoom", furnitureRoot);
        Transform workspace = CreateGroup("Workspace", furnitureRoot);
        Transform meeting = CreateGroup("MeetingRoom", furnitureRoot);
        Transform archive = CreateGroup("Archive", furnitureRoot);
        Transform director = CreateGroup("DirectorOffice", furnitureRoot);
        Transform server = CreateGroup("ServerRoom", furnitureRoot);
        Transform secondary = CreateGroup("SecondaryOffice", furnitureRoot);
        Transform corridors = CreateGroup("Corridors", furnitureRoot);

        BuildReceptionFurniture(reception);
        BuildSecurityRoomFurniture(security);
        BuildWorkspaceFurniture(workspace);
        BuildMeetingRoomFurniture(meeting);
        BuildArchiveFurniture(archive);
        BuildDirectorOfficeFurniture(director);
        BuildServerRoomFurniture(server);
        BuildSecondaryOfficeFurniture(secondary);
        BuildCorridorCoverFurniture(corridors);
    }

    private static void BuildReceptionFurniture(Transform parent)
    {
        // Main L-shaped Reception Counter (X: -11.5 to -6.5, Z: -5.2) providing crouch stealth cover
        CreateBox(parent, "Reception_MainDesk_Front", new Vector3(-9.0f, 0.56f, -5.2f), new Vector3(4.2f, 1.12f, 0.85f), matWoodWalnut, true);
        CreateBox(parent, "Reception_MainDesk_TransactionTop", new Vector3(-9.0f, 1.15f, -5.05f), new Vector3(4.3f, 0.08f, 0.45f), matFloorReceptionTile, false);
        CreateBox(parent, "Reception_MainDesk_SideWing", new Vector3(-6.9f, 0.56f, -4.1f), new Vector3(0.80f, 1.12f, 1.9f), matWoodWalnut, true);
        CreateMonitorProp(parent, "Reception_PC_1", new Vector3(-9.8f, 0.85f, -5.1f), 0f, matScreenOffice);
        CreateMonitorProp(parent, "Reception_PC_2", new Vector3(-8.2f, 0.85f, -5.1f), 0f, matScreenOffice);
        CreateOfficeChair(parent, "Reception_StaffChair_1", new Vector3(-9.8f, 0f, -4.1f), 180f, matLeatherDark);
        CreateOfficeChair(parent, "Reception_StaffChair_2", new Vector3(-8.2f, 0f, -4.1f), 180f, matLeatherDark);

        // Information Kiosk Desk near East side of Reception
        CreateBox(parent, "Reception_InfoDesk", new Vector3(-4.6f, 0.52f, -8.2f), new Vector3(1.5f, 1.04f, 0.70f), matWoodLightOak, true);
        CreateMonitorProp(parent, "Reception_InfoKioskScreen", new Vector3(-4.6f, 1.04f, -8.2f), -30f, matScreenOffice);

        // Waiting Area Lounge Sofas & Coffee Table (West side of Reception)
        CreateSofa(parent, "Reception_WaitingSofa_West", new Vector3(-13.6f, 0f, -6.5f), 90f, matFabricExecutive);
        CreateSofa(parent, "Reception_WaitingSofa_South", new Vector3(-12.2f, 0f, -8.7f), 0f, matFabricExecutive);
        CreateBox(parent, "Reception_CoffeeTable", new Vector3(-12.2f, 0.24f, -6.7f), new Vector3(1.3f, 0.44f, 0.85f), matWoodWalnut, true);

        // Decorative Indoor Plants
        CreateIndoorPlant(parent, "Reception_Plant_SW", new Vector3(-14.1f, 0f, -9.1f));
        CreateIndoorPlant(parent, "Reception_Plant_NW", new Vector3(-14.1f, 0f, -2.8f));
        CreateIndoorPlant(parent, "Reception_Plant_NE", new Vector3(-3.8f, 0f, -2.8f));
    }

    private static void BuildSecurityRoomFurniture(Transform parent)
    {
        // Surveillance Command Console (North wall of Security Room, X = -10.5, Z = +8.5)
        CreateBox(parent, "Security_CommandDesk", new Vector3(-10.5f, 0.42f, 8.2f), new Vector3(4.4f, 0.84f, 0.95f), matMetalCharcoal, true);
        CreateOfficeChair(parent, "Security_OperatorChair_1", new Vector3(-11.5f, 0f, 7.1f), 0f, matLeatherDark);
        CreateOfficeChair(parent, "Security_OperatorChair_2", new Vector3(-9.5f, 0f, 7.1f), 0f, matLeatherDark);

        // Multi-monitor Surveillance Wall + Desk Monitors
        for (int i = 0; i < 4; i++)
        {
            float mx = -12.0f + i * 1.0f;
            CreateMonitorProp(parent, $"Security_DeskMonitor_{i + 1}", new Vector3(mx, 0.84f, 8.35f), 0f, matScreenSecurity);
            CreateBox(parent, $"Security_WallDisplay_{i + 1}", new Vector3(mx, 2.05f, 9.75f), new Vector3(0.92f, 0.56f, 0.08f), matScreenSecurity, false);
        }

        // Central Sightline-Blocker Island Desk (allows sneaking between doors)
        CreateOfficeDeskWithPartition(parent, "Security_SupervisorDesk", new Vector3(-12.2f, 0f, 4.8f), 90f, true);

        // Equipment & Keycard Cabinets
        CreateFilingCabinet(parent, "Security_EquipmentCabinet_1", new Vector3(-14.2f, 0f, 8.8f), 90f);
        CreateFilingCabinet(parent, "Security_EquipmentCabinet_2", new Vector3(-7.0f, 0f, 8.8f), -90f);
        CreateFilingCabinet(parent, "Security_KeycardLocker", new Vector3(-6.8f, 0f, 3.2f), 0f);
    }

    private static void BuildWorkspaceFurniture(Transform parent)
    {
        // 4 Workstation Desks with acoustic privacy partitions (ideal crouch cover)
        CreateOfficeDeskWithPartition(parent, "Workspace_Cluster_NW", new Vector3(-0.4f, 0f, -4.3f), 0f, true);
        CreateOfficeDeskWithPartition(parent, "Workspace_Cluster_SW", new Vector3(-0.4f, 0f, -7.7f), 180f, true);
        CreateOfficeDeskWithPartition(parent, "Workspace_Cluster_NE", new Vector3(4.8f, 0f, -4.3f), 0f, true);
        CreateOfficeDeskWithPartition(parent, "Workspace_Cluster_SE", new Vector3(4.8f, 0f, -7.7f), 180f, true);

        // Printer / Copier Cover Island in center
        CreateBox(parent, "Workspace_PrinterIsland", new Vector3(2.2f, 0.52f, -6.0f), new Vector3(1.5f, 1.04f, 0.85f), matMetalBrushed, true);

        // Filing Cabinets & Indoor Plants
        CreateFilingCabinet(parent, "Workspace_Cabinet_1", new Vector3(-2.2f, 0f, -9.1f), 0f);
        CreateFilingCabinet(parent, "Workspace_Cabinet_2", new Vector3(6.9f, 0f, -9.1f), 0f);
        CreateIndoorPlant(parent, "Workspace_Plant_1", new Vector3(-2.2f, 0f, -2.8f));
        CreateIndoorPlant(parent, "Workspace_Plant_2", new Vector3(7.1f, 0f, -2.8f));
    }

    private static void BuildMeetingRoomFurniture(Transform parent)
    {
        // Large Executive Conference Table (X = 11.5, Z = -6.0)
        CreateBox(parent, "Meeting_ConferenceTable_Top", new Vector3(11.5f, 0.76f, -6.0f), new Vector3(2.2f, 0.08f, 4.6f), matWoodWalnut, true);
        CreateBox(parent, "Meeting_ConferenceTable_Base1", new Vector3(11.5f, 0.36f, -4.6f), new Vector3(0.9f, 0.72f, 0.45f), matMetalCharcoal, true);
        CreateBox(parent, "Meeting_ConferenceTable_Base2", new Vector3(11.5f, 0.36f, -7.4f), new Vector3(0.9f, 0.72f, 0.45f), matMetalCharcoal, true);

        // 8 Conference Chairs
        for (int i = 0; i < 3; i++)
        {
            float cz = -7.4f + i * 1.4f;
            CreateOfficeChair(parent, $"Meeting_Chair_West_{i + 1}", new Vector3(9.9f, 0f, cz), 90f, matFabricExecutive);
            CreateOfficeChair(parent, $"Meeting_Chair_East_{i + 1}", new Vector3(13.1f, 0f, cz), -90f, matFabricExecutive);
        }
        CreateOfficeChair(parent, "Meeting_Chair_HeadNorth", new Vector3(11.5f, 0f, -3.2f), 180f, matLeatherDark);
        CreateOfficeChair(parent, "Meeting_Chair_HeadSouth", new Vector3(11.5f, 0f, -8.8f), 0f, matLeatherDark);

        // Wall-Mounted Presentation Display (North wall of Meeting Room) & Whiteboard (West wall)
        CreateBox(parent, "Meeting_WallDisplay_Frame", new Vector3(13.2f, 1.75f, -2.18f), new Vector3(2.1f, 1.15f, 0.08f), matMetalCharcoal, false);
        CreateBox(parent, "Meeting_WallDisplay_Screen", new Vector3(13.2f, 1.75f, -2.23f), new Vector3(1.95f, 1.02f, 0.02f), matScreenOffice, false);
        CreateBox(parent, "Meeting_Whiteboard", new Vector3(8.15f, 1.65f, -4.0f), new Vector3(0.06f, 1.15f, 2.2f), matWhiteboard, false);

        // Side Credenza Cabinet
        CreateBox(parent, "Meeting_Credenza", new Vector3(14.3f, 0.45f, -6.0f), new Vector3(0.65f, 0.90f, 2.6f), matWoodWalnut, true);
        CreateIndoorPlant(parent, "Meeting_Plant_SE", new Vector3(14.1f, 0f, -9.1f));
    }

    private static void BuildArchiveFurniture(Transform parent)
    {
        // 1. Ground Floor Archive Room (X: -6.0 to +2.0, Z: +2.0 to +10.0)
        CreateTallShelfUnit(parent, "GF_Archive_Shelf_Row1_A", new Vector3(-4.2f, 0f, 8.7f), 0f);
        CreateTallShelfUnit(parent, "GF_Archive_Shelf_Row1_B", new Vector3(-0.2f, 0f, 8.7f), 0f);
        CreateTallShelfUnit(parent, "GF_Archive_Shelf_Center_A", new Vector3(-3.8f, 0f, 5.8f), 0f);
        CreateTallShelfUnit(parent, "GF_Archive_Shelf_Center_B", new Vector3(-0.4f, 0f, 5.8f), 0f);
        CreateFilingCabinet(parent, "GF_Archive_Cabinet_1", new Vector3(-5.1f, 0f, 3.1f), 90f);
        CreateFilingCabinet(parent, "GF_Archive_Cabinet_2", new Vector3(1.1f, 0f, 3.1f), -90f);
        CreateBox(parent, "GF_Archive_BoxStack_1", new Vector3(-2.0f, 0.45f, 3.4f), new Vector3(1.1f, 0.90f, 0.85f), matPaperStack, true);

        // Service Bay Storage Crates (X: +2.0 to +9.0, Z: +2.0 to +10.0)
        CreateBox(parent, "GF_Service_CoverCrate_1", new Vector3(4.0f, 0.55f, 7.8f), new Vector3(1.4f, 1.10f, 1.1f), matWoodLightOak, true);
        CreateBox(parent, "GF_Service_CoverCrate_2", new Vector3(7.4f, 0.55f, 7.8f), new Vector3(1.4f, 1.10f, 1.1f), matMetalBrushed, true);

        // 2. Upper Floor Document / Archive Area (X: -3.0 to +9.0, Z: +2.0 to +10.0, Y = 3.40m)
        CreateTallShelfUnit(parent, "UF_Archive_Shelf_North1", new Vector3(-0.5f, UpperFloorY, 8.8f), 0f);
        CreateTallShelfUnit(parent, "UF_Archive_Shelf_North2", new Vector3(3.2f, UpperFloorY, 8.8f), 0f);
        CreateTallShelfUnit(parent, "UF_Archive_Shelf_North3", new Vector3(6.8f, UpperFloorY, 8.8f), 0f);
        // Staggered Stealth Cover Shelves in center of Upper Archive
        CreateTallShelfUnit(parent, "UF_Archive_StealthShelf_West", new Vector3(0.4f, UpperFloorY, 5.6f), 0f);
        CreateTallShelfUnit(parent, "UF_Archive_StealthShelf_East", new Vector3(5.4f, UpperFloorY, 5.6f), 0f);
        // Document Sorting Table
        CreateBox(parent, "UF_Archive_SortingTable", new Vector3(2.9f, UpperFloorY + 0.44f, 4.0f), new Vector3(2.2f, 0.88f, 1.0f), matWoodLightOak, true);
        CreateFilingCabinet(parent, "UF_Archive_Cabinet_West", new Vector3(-2.1f, UpperFloorY, 3.2f), 90f);
        CreateFilingCabinet(parent, "UF_Archive_Cabinet_East", new Vector3(8.1f, UpperFloorY, 3.2f), -90f);
    }

    private static void BuildDirectorOfficeFurniture(Transform parent)
    {
        // MAIN OBJECTIVE ROOM (Upper Floor North-West: X = -15.0 to -3.0, Z = +2.0 to +10.0, Y = 3.40m)
        Vector3 deskCenter = new Vector3(-10.5f, UpperFloorY, 6.5f);

        // Large Executive Desk + Side Return
        CreateBox(parent, "Director_ExecutiveDesk_Main", deskCenter + new Vector3(0f, 0.41f, 0f), new Vector3(2.6f, 0.82f, 1.15f), matWoodWalnut, true);
        CreateBox(parent, "Director_ExecutiveDesk_SideWing", deskCenter + new Vector3(-1.15f, 0.41f, 1.1f), new Vector3(0.85f, 0.82f, 1.4f), matWoodWalnut, true);

        // Executive Chair + 2 Visitor Chairs
        CreateOfficeChair(parent, "Director_ExecutiveChair", deskCenter + new Vector3(0.2f, 0f, 1.15f), 180f, matLeatherDark);
        CreateOfficeChair(parent, "Director_VisitorChair_1", deskCenter + new Vector3(-0.6f, 0f, -1.25f), 0f, matFabricExecutive);
        CreateOfficeChair(parent, "Director_VisitorChair_2", deskCenter + new Vector3(0.6f, 0f, -1.25f), 0f, matFabricExecutive);

        // Director Computer & Desk Lamp
        CreateMonitorProp(parent, "Director_ComputerMonitor", deskCenter + new Vector3(0.75f, 0.82f, 0.15f), 160f, matScreenOffice);

        // Visual Confidential Dossier Prop right at the objective point (non-blocking collider disabled)
        GameObject dossier = CreateBox(parent, "Director_ConfidentialDossier_VisualProp", deskCenter + new Vector3(0.0f, 0.84f, 0.0f), new Vector3(0.42f, 0.04f, 0.32f), matConfidentialFolder, false);
        dossier.transform.rotation = Quaternion.Euler(0f, -14f, 0f);
        CreateBox(parent, "Director_ConfidentialDossier_Papers", deskCenter + new Vector3(0.0f, 0.865f, 0.0f), new Vector3(0.36f, 0.02f, 0.26f), matPaperStack, false);

        // Wall-to-wall Bookshelves & Credenza
        CreateTallShelfUnit(parent, "Director_Bookshelf_North1", new Vector3(-12.6f, UpperFloorY, 9.15f), 0f);
        CreateTallShelfUnit(parent, "Director_Bookshelf_North2", new Vector3(-9.2f, UpperFloorY, 9.15f), 0f);
        CreateFilingCabinet(parent, "Director_SecureSafeCabinet_1", new Vector3(-4.2f, UpperFloorY, 8.9f), -90f);
        CreateFilingCabinet(parent, "Director_SecureSafeCabinet_2", new Vector3(-4.2f, UpperFloorY, 7.8f), -90f);

        // VIP Lounge Area inside Director's Office (provides stealth cover near entrance)
        CreateSofa(parent, "Director_VIP_Sofa", new Vector3(-6.2f, UpperFloorY, 4.5f), -90f, matLeatherDark);
        CreateBox(parent, "Director_VIP_CoffeeTable", new Vector3(-7.6f, UpperFloorY + 0.24f, 4.5f), new Vector3(0.85f, 0.44f, 1.35f), matWoodWalnut, true);

        // Decorative Indoor Plants
        CreateIndoorPlant(parent, "Director_Plant_NW", new Vector3(-14.1f, UpperFloorY, 9.0f));
        CreateIndoorPlant(parent, "Director_Plant_SW", new Vector3(-14.1f, UpperFloorY, 2.9f));
    }

    private static void BuildServerRoomFurniture(Transform parent)
    {
        // Upper Floor South-West (X = -15.0 to -5.0, Z = -10.0 to -2.0, Y = 3.40m)
        for (int i = 0; i < 3; i++)
        {
            float rx = -13.2f + i * 2.4f;
            CreateServerRackUnit(parent, $"Server_Rack_NorthRow_{i + 1}", new Vector3(rx, UpperFloorY, -4.2f));
            CreateServerRackUnit(parent, $"Server_Rack_SouthRow_{i + 1}", new Vector3(rx, UpperFloorY, -7.6f));
        }

        // Overhead Cable Trays connecting the server rows
        CreateBox(parent, "Server_CableTray_North", new Vector3(-10.8f, UpperFloorY + 2.45f, -4.2f), new Vector3(6.2f, 0.08f, 0.45f), matMetalBrushed, false);
        CreateBox(parent, "Server_CableTray_South", new Vector3(-10.8f, UpperFloorY + 2.45f, -7.6f), new Vector3(6.2f, 0.08f, 0.45f), matMetalBrushed, false);
        CreateBox(parent, "Server_CableTray_CrossBridge", new Vector3(-8.4f, UpperFloorY + 2.45f, -5.9f), new Vector3(0.45f, 0.08f, 3.8f), matMetalBrushed, false);

        // Cooling CRAC Unit & Network Console
        CreateBox(parent, "Server_CoolingUnit_CRAC", new Vector3(-14.2f, UpperFloorY + 1.1f, -5.9f), new Vector3(0.75f, 2.2f, 1.8f), matMetalCharcoal, true);
        CreateBox(parent, "Server_NetworkConsoleDesk", new Vector3(-6.4f, UpperFloorY + 0.42f, -8.6f), new Vector3(1.8f, 0.84f, 0.80f), matMetalCharcoal, true);
        CreateMonitorProp(parent, "Server_TerminalScreen", new Vector3(-6.4f, UpperFloorY + 0.84f, -8.6f), 180f, matScreenSecurity);
    }

    private static void BuildSecondaryOfficeFurniture(Transform parent)
    {
        // Upper Floor South-Center & South-East (X = -5.0 to +15.0, Z = -10.0 to -2.0, Y = 3.40m)
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_Desk_1", new Vector3(-2.2f, UpperFloorY, -4.4f), 0f, true);
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_Desk_2", new Vector3(-2.2f, UpperFloorY, -7.6f), 180f, true);
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_Desk_3", new Vector3(2.8f, UpperFloorY, -4.4f), 0f, true);
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_Desk_4", new Vector3(2.8f, UpperFloorY, -7.6f), 180f, true);

        // East Executive Antechamber Desks (X = +6.0 to +15.0)
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_ExecSupport_1", new Vector3(9.2f, UpperFloorY, -4.6f), 0f, true);
        CreateOfficeDeskWithPartition(parent, "SecondaryOffice_ExecSupport_2", new Vector3(12.2f, UpperFloorY, -7.4f), 180f, true);

        CreateFilingCabinet(parent, "SecondaryOffice_Cabinet_1", new Vector3(-4.1f, UpperFloorY, -9.0f), 0f);
        CreateFilingCabinet(parent, "SecondaryOffice_Cabinet_2", new Vector3(14.1f, UpperFloorY, -9.0f), -90f);
        CreateIndoorPlant(parent, "SecondaryOffice_Plant_1", new Vector3(14.1f, UpperFloorY, -2.9f));
    }

    private static void BuildCorridorCoverFurniture(Transform parent)
    {
        // Ground Floor Corridor Cover (waist-high cabinets & planters that break sightlines without blocking navigation)
        CreateBox(parent, "GF_Corridor_CoverCredenza_West", new Vector3(-5.0f, 0.52f, -1.25f), new Vector3(1.8f, 1.04f, 0.55f), matWoodWalnut, true);
        CreateBox(parent, "GF_Corridor_CoverCredenza_Center", new Vector3(3.2f, 0.52f, 1.25f), new Vector3(1.8f, 1.04f, 0.55f), matWoodWalnut, true);
        CreateIndoorPlant(parent, "GF_Corridor_Plant_West", new Vector3(-13.8f, 0f, 1.2f));
        CreateIndoorPlant(parent, "GF_Corridor_Plant_East", new Vector3(13.8f, 0f, -1.2f));

        // Upper Floor Corridor Cover
        CreateBox(parent, "UF_Corridor_CoverCredenza_West", new Vector3(-4.5f, UpperFloorY + 0.52f, 1.25f), new Vector3(1.8f, 1.04f, 0.55f), matWoodWalnut, true);
        CreateBox(parent, "UF_Corridor_CoverCredenza_East", new Vector3(5.0f, UpperFloorY + 0.52f, -1.25f), new Vector3(1.8f, 1.04f, 0.55f), matWoodWalnut, true);
        CreateIndoorPlant(parent, "UF_Corridor_Plant_Landing", new Vector3(14.1f, UpperFloorY, 9.1f));
    }

    // =========================================================================
    // EXTERIOR ENVIRONMENT (PATHS, COURTYARD, VEGETATION, STREET FURNITURE)
    // =========================================================================

    private static void BuildExteriorEnvironment(Transform exteriorRoot)
    {
        Transform paths = CreateGroup("Paths", exteriorRoot);
        Transform courtyard = CreateGroup("Courtyard", exteriorRoot);
        Transform vegetation = CreateGroup("Vegetation", exteriorRoot);
        Transform streetFurniture = CreateGroup("StreetFurniture", exteriorRoot);

        // 1. Exterior Ground Base & Sidewalks around the 30m x 20m building
        CreateBox(courtyard, "Ext_GrassGround_South", new Vector3(0f, -0.18f, -15.5f), new Vector3(44.0f, 0.20f, 11.0f), matGrassOlive, true);
        CreateBox(courtyard, "Ext_GrassGround_North", new Vector3(0f, -0.18f, 14.5f), new Vector3(44.0f, 0.20f, 9.0f), matGrassOlive, true);
        CreateBox(courtyard, "Ext_GrassGround_West", new Vector3(-18.5f, -0.18f, 0f), new Vector3(7.0f, 0.20f, 20.0f), matGrassOlive, true);
        CreateBox(courtyard, "Ext_GrassGround_East", new Vector3(18.5f, -0.18f, 0f), new Vector3(7.0f, 0.20f, 20.0f), matGrassOlive, true);

        // Perimeter Walkways & Approaches
        CreateBox(paths, "Ext_MainEntrance_ApproachPlaza", new Vector3(-9.0f, -0.05f, -14.2f), new Vector3(7.5f, 0.12f, 8.0f), matCourtyardPaving, true);
        CreateBox(paths, "Ext_South_PerimeterWalkway", new Vector3(0f, -0.06f, -11.4f), new Vector3(34.0f, 0.10f, 2.6f), matSidewalkConcrete, true);
        CreateBox(paths, "Ext_North_ServiceWalkway", new Vector3(0f, -0.06f, 11.4f), new Vector3(34.0f, 0.10f, 2.6f), matSidewalkConcrete, true);
        CreateBox(paths, "Ext_West_SideWalkway", new Vector3(-16.4f, -0.06f, 0f), new Vector3(2.6f, 0.10f, 25.0f), matSidewalkConcrete, true);
        CreateBox(paths, "Ext_East_ExtractionWalkway", new Vector3(16.8f, -0.06f, 0f), new Vector3(3.4f, 0.10f, 25.0f), matSidewalkConcrete, true);

        // Small Parking Bay / Asphalt Access Lane on South-East
        CreateBox(courtyard, "Ext_South_AccessRoad", new Vector3(4.0f, -0.07f, -16.8f), new Vector3(30.0f, 0.10f, 5.2f), matAsphalt, true);

        // Entrance Canopy Overhang at Main Entrance (X = -9.0, Z = -10.0)
        CreateBox(courtyard, "Ext_MainEntrance_Canopy", new Vector3(-9.0f, 2.85f, -11.2f), new Vector3(4.6f, 0.18f, 2.2f), matMetalCharcoal, false);

        // 2. Trees & Shrubs
        CreateTree(vegetation, "Ext_Tree_SW", new Vector3(-16.5f, 0f, -15.5f), 1.1f);
        CreateTree(vegetation, "Ext_Tree_SouthCenter", new Vector3(-2.5f, 0f, -14.2f), 1.0f);
        CreateTree(vegetation, "Ext_Tree_SE", new Vector3(17.5f, 0f, -14.5f), 1.15f);
        CreateTree(vegetation, "Ext_Tree_NW", new Vector3(-17.5f, 0f, 14.0f), 1.05f);
        CreateTree(vegetation, "Ext_Tree_NE", new Vector3(17.5f, 0f, 14.0f), 1.1f);

        // Low Shrub Hedges along facade
        CreateBox(vegetation, "Ext_Hedge_South1", new Vector3(-1.5f, 0.40f, -10.55f), new Vector3(4.5f, 0.80f, 0.75f), matFoliageGreen, true);
        CreateBox(vegetation, "Ext_Hedge_South2", new Vector3(7.5f, 0.40f, -10.55f), new Vector3(4.5f, 0.80f, 0.75f), matFoliageGreen, true);

        // 3. Street Furniture (Outdoor Benches & Bollard Lights)
        CreateOutdoorBench(streetFurniture, "Ext_Bench_MainPlaza_West", new Vector3(-12.2f, 0f, -14.0f), 90f);
        CreateOutdoorBench(streetFurniture, "Ext_Bench_MainPlaza_East", new Vector3(-5.8f, 0f, -14.0f), -90f);
        CreateOutdoorBench(streetFurniture, "Ext_Bench_EastWalkway", new Vector3(18.0f, 0f, 3.5f), -90f);

        CreateExteriorLampPost(streetFurniture, "Ext_LampPost_MainEntrance", new Vector3(-12.4f, 0f, -11.8f));
        CreateExteriorLampPost(streetFurniture, "Ext_LampPost_SouthEast", new Vector3(13.5f, 0f, -11.8f));
        CreateExteriorLampPost(streetFurniture, "Ext_LampPost_ServiceRear", new Vector3(7.2f, 0f, 12.0f));
        CreateExteriorLampPost(streetFurniture, "Ext_LampPost_ExtractionEast", new Vector3(17.8f, 0f, -1.8f));
    }

    // =========================================================================
    // LIGHTING SETUP (BALANCED INTERIOR + EXTERIOR)
    // =========================================================================

    private static void BuildLightingSetup(Transform lightingRoot)
    {
        Transform gfLights = CreateGroup("GroundFloor_Lights", lightingRoot);
        Transform ufLights = CreateGroup("UpperFloor_Lights", lightingRoot);
        Transform extLights = CreateGroup("Exterior_Lights", lightingRoot);

        Color warmColor = new Color(1.0f, 0.91f, 0.78f);
        Color neutralColor = new Color(0.94f, 0.96f, 1.0f);
        Color coolServerColor = new Color(0.58f, 0.84f, 1.0f);

        // Ground Floor Room Lights (at Y = 2.95m, fixtures at Y = 3.12m)
        CreateCeilingLight(gfLights, "GF_Light_Reception", new Vector3(-9.0f, 3.12f, -6.0f), warmColor, 1.35f, 11.5f, matLampWarmEmissive);
        CreateCeilingLight(gfLights, "GF_Light_Security", new Vector3(-10.5f, 3.12f, 6.0f), neutralColor, 1.20f, 10.0f, matLampNeutralEmissive);
        CreateCeilingLight(gfLights, "GF_Light_Archive", new Vector3(-2.0f, 3.12f, 6.0f), neutralColor, 1.15f, 9.5f, matLampNeutralEmissive);
        CreateCeilingLight(gfLights, "GF_Light_Workspace", new Vector3(2.5f, 3.12f, -6.0f), warmColor, 1.35f, 11.5f, matLampWarmEmissive);
        CreateCeilingLight(gfLights, "GF_Light_MeetingRoom", new Vector3(11.5f, 3.12f, -6.0f), warmColor, 1.25f, 10.0f, matLampWarmEmissive);
        CreateCeilingLight(gfLights, "GF_Light_StairService", new Vector3(8.5f, 3.12f, 6.0f), neutralColor, 1.25f, 11.0f, matLampNeutralEmissive);
        CreateCeilingLight(gfLights, "GF_Light_CorridorWest", new Vector3(-7.5f, 3.12f, 0.0f), neutralColor, 1.15f, 10.5f, matLampNeutralEmissive);
        CreateCeilingLight(gfLights, "GF_Light_CorridorEast", new Vector3(7.5f, 3.12f, 0.0f), neutralColor, 1.15f, 10.5f, matLampNeutralEmissive);

        // Upper Floor Room Lights (at Y = 6.35m, fixtures at Y = 6.52m)
        CreateCeilingLight(ufLights, "UF_Light_DirectorOffice", new Vector3(-9.0f, 6.52f, 6.0f), warmColor, 1.40f, 12.0f, matLampWarmEmissive);
        CreateCeilingLight(ufLights, "UF_Light_UpperArchive", new Vector3(3.0f, 6.52f, 6.0f), neutralColor, 1.20f, 11.0f, matLampNeutralEmissive);
        CreateCeilingLight(ufLights, "UF_Light_ServerRoom", new Vector3(-10.0f, 6.52f, -6.0f), coolServerColor, 1.25f, 10.5f, matLampCoolEmissive);
        CreateCeilingLight(ufLights, "UF_Light_SecondaryOfficeWest", new Vector3(0.5f, 6.52f, -6.0f), warmColor, 1.30f, 11.0f, matLampWarmEmissive);
        CreateCeilingLight(ufLights, "UF_Light_SecondaryOfficeEast", new Vector3(10.5f, 6.52f, -6.0f), warmColor, 1.30f, 11.0f, matLampWarmEmissive);
        CreateCeilingLight(ufLights, "UF_Light_CorridorCenter", new Vector3(0.0f, 6.52f, 0.0f), neutralColor, 1.15f, 11.5f, matLampNeutralEmissive);
        CreateCeilingLight(ufLights, "UF_Light_StairLanding", new Vector3(12.0f, 6.52f, 7.5f), neutralColor, 1.25f, 10.5f, matLampNeutralEmissive);

        // Exterior Entrance & Extraction Lights
        CreatePointLightNode(extLights, "Ext_Light_MainEntrance", new Vector3(-9.0f, 2.65f, -11.0f), warmColor, 1.3f, 9.0f);
        CreatePointLightNode(extLights, "Ext_Light_ExtractionExit", new Vector3(16.0f, 2.65f, 0.0f), warmColor, 1.2f, 8.0f);
    }

    private static void CreateCeilingLight(Transform parent, string name, Vector3 fixturePos, Color color, float intensity, float range, Material emissiveMat)
    {
        CreateBox(parent, name + "_Fixture", fixturePos, new Vector3(1.30f, 0.05f, 0.36f), emissiveMat, false);
        CreatePointLightNode(parent, name, fixturePos + new Vector3(0f, -0.22f, 0f), color, intensity, range);
    }

    private static void CreatePointLightNode(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject lightGo = new GameObject(name);
        lightGo.transform.SetParent(parent, false);
        lightGo.transform.position = pos;
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    // =========================================================================
    // GAMEPLAY INTEGRATION MARKERS & PATROL WAYPOINTS
    // =========================================================================

    private static void BuildGameplayMarkers(Transform markersRoot)
    {
        // 1. PlayerSpawnPoint (Ground Floor Main Entrance Approach)
        Transform playerSpawn = CreateEmptyMarker(markersRoot, "PlayerSpawnPoint", new Vector3(-9.0f, 0.0f, -13.2f), Quaternion.identity);

        // 2. PlayerCameraAnchor (Recommended 1.70m first-person eye height at player spawn)
        CreateEmptyMarker(markersRoot, "PlayerCameraAnchor", playerSpawn.position + new Vector3(0f, 1.70f, 0f), Quaternion.identity);

        // 3. Mission_FileSpawnPoint (Upper Floor — Director's Office Executive Desk)
        CreateEmptyMarker(markersRoot, "Mission_FileSpawnPoint", new Vector3(-10.5f, UpperFloorY + 0.85f, 6.5f), Quaternion.Euler(0f, 165f, 0f));

        // 4. Mission_ExitPoint (Ground Floor East Extraction Walkway)
        CreateEmptyMarker(markersRoot, "Mission_ExitPoint", new Vector3(16.8f, 0.0f, 0.0f), Quaternion.Euler(0f, 90f, 0f));

        // 5. GroundFloor_GuardPatrolPoints
        Transform gfPatrol = CreateGroup("GroundFloor_GuardPatrolPoints", markersRoot);
        Vector3[] gfPoints =
        {
            new Vector3(-9.0f, 0.0f, -3.2f),  // WP_01_ReceptionEntry
            new Vector3(-10.5f, 0.0f, 0.0f),  // WP_02_WestCorridor
            new Vector3(-10.5f, 0.0f, 5.2f),  // WP_03_SecurityRoom
            new Vector3(-2.0f, 0.0f, 4.5f),   // WP_04_GroundArchive
            new Vector3(0.0f, 0.0f, 0.0f),    // WP_05_CentralCorridor
            new Vector3(2.5f, 0.0f, -5.8f),   // WP_06_OpenWorkspace
            new Vector3(11.5f, 0.0f, -5.8f),  // WP_07_MeetingRoom
            new Vector3(11.5f, 0.0f, 0.0f),   // WP_08_EastCorridorNearStairs
            new Vector3(6.0f, 0.0f, 5.0f)     // WP_09_ServiceAndStairLobby
        };
        for (int i = 0; i < gfPoints.Length; i++)
        {
            CreateEmptyMarker(gfPatrol, $"GF_PatrolPoint_{i + 1:00}", gfPoints[i], Quaternion.identity);
        }

        // 6. UpperFloor_GuardPatrolPoints
        Transform ufPatrol = CreateGroup("UpperFloor_GuardPatrolPoints", markersRoot);
        Vector3[] ufPoints =
        {
            new Vector3(12.0f, UpperFloorY, 8.8f),  // WP_01_StairTopLanding
            new Vector3(9.8f, UpperFloorY, 0.0f),   // WP_02_UpperEastCorridor
            new Vector3(3.0f, UpperFloorY, 4.8f),   // WP_03_UpperArchive
            new Vector3(-6.5f, UpperFloorY, 5.5f),  // WP_04_DirectorOfficeAntechamber
            new Vector3(-9.0f, UpperFloorY, 0.0f),  // WP_05_UpperWestCorridor
            new Vector3(-9.5f, UpperFloorY, -5.8f), // WP_06_ServerRoomAisle
            new Vector3(0.5f, UpperFloorY, -5.8f),  // WP_07_SecondaryOfficeWest
            new Vector3(10.5f, UpperFloorY, -5.8f)  // WP_08_SecondaryOfficeEast
        };
        for (int i = 0; i < ufPoints.Length; i++)
        {
            CreateEmptyMarker(ufPatrol, $"UF_PatrolPoint_{i + 1:00}", ufPoints[i], Quaternion.identity);
        }
    }

    // =========================================================================
    // OPTIONAL DISABLED OVERVIEW CAMERA (UNTAGGED, DISABLED BY DEFAULT)
    // =========================================================================

    private static void BuildOptionalOverviewCamera(Transform root)
    {
        GameObject camGo = new GameObject("Optional_OverviewCamera");
        camGo.transform.SetParent(root, false);
        camGo.tag = "Untagged"; // Explicitly not MainCamera
        camGo.transform.position = new Vector3(-25.0f, 22.5f, -24.0f);
        camGo.transform.LookAt(new Vector3(0.0f, 2.8f, 0.0f));

        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 45.0f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 150.0f;

        camGo.SetActive(false); // Disabled by default so existing Main Camera is untouched
    }

    // =========================================================================
    // MODULAR ARCHITECTURAL WALL & FURNITURE HELPERS
    // =========================================================================

    private struct OpeningSpec
    {
        public readonly float Center;
        public readonly float Width;
        public readonly bool IsDoubleOrOpenArch;

        public OpeningSpec(float center, float width, bool isDoubleOrOpenArch)
        {
            Center = center;
            Width = width;
            IsDoubleOrOpenArch = isDoubleOrOpenArch;
        }
    }

    private struct WindowSpec
    {
        public readonly float Center;
        public readonly float Width;
        public readonly float SillHeight;
        public readonly float TopHeight;

        public WindowSpec(float center, float width, float sillHeight, float topHeight)
        {
            Center = center;
            Width = width;
            SillHeight = sillHeight;
            TopHeight = topHeight;
        }
    }

    private struct SpanItem
    {
        public float Start;
        public float End;
        public bool IsDoor;
        public OpeningSpec Door;
        public WindowSpec Window;
    }

    private static void BuildWallAlongX(
        Transform wallParent,
        Transform windowParent,
        Transform doorParent,
        string baseName,
        float z,
        float minX,
        float maxX,
        float floorY,
        float wallHeight,
        float thickness,
        Material wallMat,
        OpeningSpec[] doors,
        WindowSpec[] windows)
    {
        const float doorHeight = 2.25f;
        List<SpanItem> spans = CollectSortedSpans(doors, windows);

        float cursor = minX;
        int segIdx = 0;

        foreach (SpanItem span in spans)
        {
            float clampedStart = Mathf.Max(cursor, span.Start);
            float solidLen = clampedStart - cursor;
            if (solidLen > 0.04f)
            {
                CreateBox(
                    wallParent,
                    $"{baseName}_Seg_{++segIdx}",
                    new Vector3(cursor + solidLen * 0.5f, floorY + wallHeight * 0.5f, z),
                    new Vector3(solidLen, wallHeight, thickness),
                    wallMat,
                    true);
            }

            float spanWidth = span.End - clampedStart;
            float spanCenter = clampedStart + spanWidth * 0.5f;

            if (span.IsDoor)
            {
                float headerHeight = wallHeight - doorHeight;
                if (headerHeight > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_DoorHeader_{++segIdx}",
                        new Vector3(spanCenter, floorY + doorHeight + headerHeight * 0.5f, z),
                        new Vector3(spanWidth, headerHeight, thickness),
                        wallMat,
                        true);
                }
                BuildDoorFrameAlongX(doorParent, $"{baseName}_Door_{segIdx}", new Vector3(spanCenter, floorY, z), spanWidth, doorHeight, thickness, span.Door.IsDoubleOrOpenArch);
            }
            else
            {
                float sillH = span.Window.SillHeight;
                float topH = span.Window.TopHeight;
                float upperH = wallHeight - topH;

                if (sillH > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_WinSill_{++segIdx}",
                        new Vector3(spanCenter, floorY + sillH * 0.5f, z),
                        new Vector3(spanWidth, sillH, thickness),
                        wallMat,
                        true);
                }
                if (upperH > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_WinLintel_{segIdx}",
                        new Vector3(spanCenter, floorY + topH + upperH * 0.5f, z),
                        new Vector3(spanWidth, upperH, thickness),
                        wallMat,
                        true);
                }
                BuildWindowUnitAlongX(windowParent, $"{baseName}_Window_{segIdx}", new Vector3(spanCenter, floorY, z), spanWidth, sillH, topH, thickness);
            }

            cursor = span.End;
        }

        float tailLen = maxX - cursor;
        if (tailLen > 0.04f)
        {
            CreateBox(
                wallParent,
                $"{baseName}_Seg_{++segIdx}",
                new Vector3(cursor + tailLen * 0.5f, floorY + wallHeight * 0.5f, z),
                new Vector3(tailLen, wallHeight, thickness),
                wallMat,
                true);
        }
    }

    private static void BuildWallAlongZ(
        Transform wallParent,
        Transform windowParent,
        Transform doorParent,
        string baseName,
        float x,
        float minZ,
        float maxZ,
        float floorY,
        float wallHeight,
        float thickness,
        Material wallMat,
        OpeningSpec[] doors,
        WindowSpec[] windows)
    {
        const float doorHeight = 2.25f;
        List<SpanItem> spans = CollectSortedSpans(doors, windows);

        float cursor = minZ;
        int segIdx = 0;

        foreach (SpanItem span in spans)
        {
            float clampedStart = Mathf.Max(cursor, span.Start);
            float solidLen = clampedStart - cursor;
            if (solidLen > 0.04f)
            {
                CreateBox(
                    wallParent,
                    $"{baseName}_Seg_{++segIdx}",
                    new Vector3(x, floorY + wallHeight * 0.5f, cursor + solidLen * 0.5f),
                    new Vector3(thickness, wallHeight, solidLen),
                    wallMat,
                    true);
            }

            float spanWidth = span.End - clampedStart;
            float spanCenter = clampedStart + spanWidth * 0.5f;

            if (span.IsDoor)
            {
                float headerHeight = wallHeight - doorHeight;
                if (headerHeight > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_DoorHeader_{++segIdx}",
                        new Vector3(x, floorY + doorHeight + headerHeight * 0.5f, spanCenter),
                        new Vector3(thickness, headerHeight, spanWidth),
                        wallMat,
                        true);
                }
                BuildDoorFrameAlongZ(doorParent, $"{baseName}_Door_{segIdx}", new Vector3(x, floorY, spanCenter), spanWidth, doorHeight, thickness, span.Door.IsDoubleOrOpenArch);
            }
            else
            {
                float sillH = span.Window.SillHeight;
                float topH = span.Window.TopHeight;
                float upperH = wallHeight - topH;

                if (sillH > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_WinSill_{++segIdx}",
                        new Vector3(x, floorY + sillH * 0.5f, spanCenter),
                        new Vector3(thickness, sillH, spanWidth),
                        wallMat,
                        true);
                }
                if (upperH > 0.04f)
                {
                    CreateBox(
                        wallParent,
                        $"{baseName}_WinLintel_{segIdx}",
                        new Vector3(x, floorY + topH + upperH * 0.5f, spanCenter),
                        new Vector3(thickness, upperH, spanWidth),
                        wallMat,
                        true);
                }
                BuildWindowUnitAlongZ(windowParent, $"{baseName}_Window_{segIdx}", new Vector3(x, floorY, spanCenter), spanWidth, sillH, topH, thickness);
            }

            cursor = span.End;
        }

        float tailLen = maxZ - cursor;
        if (tailLen > 0.04f)
        {
            CreateBox(
                wallParent,
                $"{baseName}_Seg_{++segIdx}",
                new Vector3(x, floorY + wallHeight * 0.5f, cursor + tailLen * 0.5f),
                new Vector3(thickness, wallHeight, tailLen),
                wallMat,
                true);
        }
    }

    private static List<SpanItem> CollectSortedSpans(OpeningSpec[] doors, WindowSpec[] windows)
    {
        List<SpanItem> list = new List<SpanItem>();
        if (doors != null)
        {
            foreach (OpeningSpec d in doors)
            {
                list.Add(new SpanItem
                {
                    Start = d.Center - d.Width * 0.5f,
                    End = d.Center + d.Width * 0.5f,
                    IsDoor = true,
                    Door = d
                });
            }
        }
        if (windows != null)
        {
            foreach (WindowSpec w in windows)
            {
                list.Add(new SpanItem
                {
                    Start = w.Center - w.Width * 0.5f,
                    End = w.Center + w.Width * 0.5f,
                    IsDoor = false,
                    Window = w
                });
            }
        }
        return list.OrderBy(s => s.Start).ToList();
    }

    private static void CreateInteractiveDoorLeaf(
        Transform parent,
        string doorName,
        Vector3 hingePosition,
        Quaternion hingeRotation,
        float width,
        float height,
        float openAngle)
    {
        GameObject hinge = new GameObject(doorName + "_Hinge");
        Undo.RegisterCreatedObjectUndo(hinge, "Create Interactive Office Door");
        hinge.transform.SetParent(parent, false);
        hinge.transform.position = hingePosition;
        hinge.transform.rotation = hingeRotation;

        GameObject pivotObject = new GameObject("SwingPivot");
        Undo.RegisterCreatedObjectUndo(pivotObject, "Create Door Swing Pivot");
        pivotObject.transform.SetParent(hinge.transform, false);
        Transform pivot = pivotObject.transform;

        GameObject panel = CreateLocalBox(
            pivot,
            "DoorPanel",
            new Vector3(width * 0.5f, height * 0.5f, 0f),
            new Vector3(width, height, 0.10f),
            matDoorWalnut,
            true);

        CreateLocalBox(
            pivot,
            "DoorInset",
            new Vector3(width * 0.5f, height * 0.55f, -0.058f),
            new Vector3(width * 0.68f, height * 0.52f, 0.018f),
            matWoodWalnut,
            false);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(handle, "Create Office Door Handle");
        handle.name = "DoorHandle";
        handle.transform.SetParent(pivot, false);
        handle.transform.localPosition = new Vector3(width * 0.84f, height * 0.48f, -0.085f);
        handle.transform.localScale = Vector3.one * 0.085f;
        handle.GetComponent<Renderer>().sharedMaterial = matMetalBrushed;
        Collider handleCollider = handle.GetComponent<Collider>();
        if (handleCollider != null) Undo.DestroyObjectImmediate(handleCollider);

        BoxCollider interaction = hinge.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(width * 0.5f, height * 0.5f, 0f);
        interaction.size = new Vector3(width + 1.25f, height + 0.55f, 2.8f);

        Rigidbody body = hinge.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        OfficeDoorController controller = hinge.AddComponent<OfficeDoorController>();
        controller.Configure(pivot, panel.transform, doorName, openAngle, 5.5f);
    }

    private static void BuildDoorFrameAlongX(Transform parent, string name, Vector3 baseCenter, float width, float height, float thickness, bool isWideArch)
    {
        float frameDepth = thickness + 0.06f;
        CreateBox(parent, name + "_LeftJamb", baseCenter + new Vector3(-width * 0.5f + 0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, frameDepth), matDoorWalnut, false);
        CreateBox(parent, name + "_RightJamb", baseCenter + new Vector3(width * 0.5f - 0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, frameDepth), matDoorWalnut, false);
        CreateBox(parent, name + "_HeaderTrim", baseCenter + new Vector3(0f, height - 0.03f, 0f), new Vector3(width + 0.08f, 0.08f, frameDepth), matDoorWalnut, false);

        if (!isWideArch)
        {
            // Functional hinged leaf: closed across the doorway, opens toward the adjacent room.
            Vector3 hingePosition = new Vector3(
                baseCenter.x - width * 0.5f + 0.04f,
                baseCenter.y,
                baseCenter.z);
            float openingAngle = baseCenter.z < 0f ? -100f : 100f;
            CreateInteractiveDoorLeaf(parent, name, hingePosition, Quaternion.identity,
                width - 0.08f, height - 0.08f, openingAngle);
        }
    }

    private static void BuildDoorFrameAlongZ(Transform parent, string name, Vector3 baseCenter, float width, float height, float thickness, bool isWideArch)
    {
        float frameDepth = thickness + 0.06f;
        CreateBox(parent, name + "_SouthJamb", baseCenter + new Vector3(0f, height * 0.5f, -width * 0.5f + 0.04f), new Vector3(frameDepth, height, 0.08f), matDoorWalnut, false);
        CreateBox(parent, name + "_NorthJamb", baseCenter + new Vector3(0f, height * 0.5f, width * 0.5f - 0.04f), new Vector3(frameDepth, height, 0.08f), matDoorWalnut, false);
        CreateBox(parent, name + "_HeaderTrim", baseCenter + new Vector3(0f, height - 0.03f, 0f), new Vector3(frameDepth, 0.08f, width + 0.08f), matDoorWalnut, false);

        if (!isWideArch)
        {
            // Rotate the hinge so its local X axis follows the doorway along world +Z.
            Vector3 hingePosition = new Vector3(
                baseCenter.x,
                baseCenter.y,
                baseCenter.z - width * 0.5f + 0.04f);
            float openingAngle = baseCenter.x > 0f ? -100f : 100f;
            CreateInteractiveDoorLeaf(parent, name, hingePosition,
                Quaternion.Euler(0f, -90f, 0f), width - 0.08f, height - 0.08f, openingAngle);
        }
    }

    private static void BuildWindowUnitAlongX(Transform parent, string name, Vector3 baseCenter, float width, float sillH, float topH, float thickness)
    {
        float winH = topH - sillH;
        float midY = baseCenter.y + sillH + winH * 0.5f;
        // Glass pane WITH collider so windows never become accidental walkable openings
        CreateBox(parent, name + "_Glass", new Vector3(baseCenter.x, midY, baseCenter.z), new Vector3(width - 0.08f, winH - 0.08f, 0.04f), matGlassTinted, true);
        CreateBox(parent, name + "_FrameTop", new Vector3(baseCenter.x, baseCenter.y + topH - 0.03f, baseCenter.z), new Vector3(width, 0.06f, thickness + 0.04f), matMetalCharcoal, false);
        CreateBox(parent, name + "_FrameBottom", new Vector3(baseCenter.x, baseCenter.y + sillH + 0.03f, baseCenter.z), new Vector3(width, 0.06f, thickness + 0.06f), matMetalCharcoal, false);
        CreateBox(parent, name + "_CenterMullion", new Vector3(baseCenter.x, midY, baseCenter.z), new Vector3(0.05f, winH, 0.07f), matMetalCharcoal, false);
    }

    private static void BuildWindowUnitAlongZ(Transform parent, string name, Vector3 baseCenter, float width, float sillH, float topH, float thickness)
    {
        float winH = topH - sillH;
        float midY = baseCenter.y + sillH + winH * 0.5f;
        CreateBox(parent, name + "_Glass", new Vector3(baseCenter.x, midY, baseCenter.z), new Vector3(0.04f, winH - 0.08f, width - 0.08f), matGlassTinted, true);
        CreateBox(parent, name + "_FrameTop", new Vector3(baseCenter.x, baseCenter.y + topH - 0.03f, baseCenter.z), new Vector3(thickness + 0.04f, 0.06f, width), matMetalCharcoal, false);
        CreateBox(parent, name + "_FrameBottom", new Vector3(baseCenter.x, baseCenter.y + sillH + 0.03f, baseCenter.z), new Vector3(thickness + 0.06f, 0.06f, width), matMetalCharcoal, false);
        CreateBox(parent, name + "_CenterMullion", new Vector3(baseCenter.x, midY, baseCenter.z), new Vector3(0.07f, winH, 0.05f), matMetalCharcoal, false);
    }

    private static void CreateOfficeDeskWithPartition(Transform parent, string name, Vector3 pos, float yaw, bool includePartition)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateLocalBox(group, "Desk_Top", new Vector3(0f, 0.74f, 0f), new Vector3(1.80f, 0.06f, 0.85f), matWoodLightOak, true);
        CreateLocalBox(group, "Desk_LegLeft", new Vector3(-0.84f, 0.36f, 0f), new Vector3(0.06f, 0.72f, 0.78f), matMetalCharcoal, false);
        CreateLocalBox(group, "Desk_LegRight", new Vector3(0.84f, 0.36f, 0f), new Vector3(0.06f, 0.72f, 0.78f), matMetalCharcoal, false);
        CreateLocalBox(group, "Desk_DrawerPedestal", new Vector3(0.55f, 0.36f, 0f), new Vector3(0.42f, 0.70f, 0.74f), matMetalBrushed, true);

        if (includePartition)
        {
            // Acoustic privacy screen (total height 1.25m -> ideal crouch stealth cover)
            CreateLocalBox(group, "Desk_PrivacyPartition", new Vector3(0f, 0.98f, 0.41f), new Vector3(1.78f, 0.52f, 0.05f), matFabricGrey, true);
        }

        // Monitor & Chair
        CreateLocalBox(group, "Monitor_Stand", new Vector3(-0.25f, 0.84f, 0.22f), new Vector3(0.14f, 0.16f, 0.12f), matMetalCharcoal, false);
        CreateLocalBox(group, "Monitor_Screen", new Vector3(-0.25f, 0.98f, 0.20f), new Vector3(0.62f, 0.36f, 0.04f), matScreenOffice, false);
        CreateOfficeChair(group, "Chair", new Vector3(-0.20f, 0f, -0.72f), 0f, matFabricGrey, true);
    }

    private static void CreateOfficeChair(Transform parent, string name, Vector3 pos, float yaw, Material upholsteryMat, bool localSpace = false)
    {
        Transform group = CreateGroup(name, parent);
        if (localSpace)
        {
            group.localPosition = pos;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            group.position = pos;
            group.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        CreateLocalBox(group, "Chair_Seat", new Vector3(0f, 0.46f, 0f), new Vector3(0.54f, 0.08f, 0.52f), upholsteryMat, true);
        CreateLocalBox(group, "Chair_Backrest", new Vector3(0f, 0.80f, -0.23f), new Vector3(0.50f, 0.62f, 0.07f), upholsteryMat, false);
        CreateLocalBox(group, "Chair_Pedestal", new Vector3(0f, 0.22f, 0f), new Vector3(0.10f, 0.42f, 0.10f), matMetalCharcoal, false);
        CreateLocalBox(group, "Chair_BaseCross", new Vector3(0f, 0.04f, 0f), new Vector3(0.48f, 0.05f, 0.48f), matMetalCharcoal, false);
    }

    private static void CreateMonitorProp(Transform parent, string name, Vector3 pos, float yaw, Material screenMat)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateLocalBox(group, "Stand", new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.16f, 0.14f), matMetalCharcoal, false);
        CreateLocalBox(group, "Frame", new Vector3(0f, 0.24f, 0f), new Vector3(0.66f, 0.38f, 0.04f), matMetalCharcoal, false);
        CreateLocalBox(group, "Display", new Vector3(0f, 0.24f, -0.022f), new Vector3(0.62f, 0.34f, 0.01f), screenMat, false);
    }

    private static void CreateTallShelfUnit(Transform parent, string name, Vector3 pos, float yaw)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Single clean collider on the main frame
        CreateLocalBox(group, "Shelf_BackAndCollider", new Vector3(0f, 1.15f, 0f), new Vector3(2.20f, 2.30f, 0.55f), matWoodWalnut, true);
        for (int i = 0; i < 4; i++)
        {
            float y = 0.35f + i * 0.52f;
            CreateLocalBox(group, $"Binders_Row_{i + 1}", new Vector3(-0.35f + (i % 2) * 0.65f, y, -0.04f), new Vector3(0.75f, 0.34f, 0.46f), i % 2 == 0 ? matPaperStack : matIntWallAccent, false);
        }
    }

    private static void CreateFilingCabinet(Transform parent, string name, Vector3 pos, float yaw)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateLocalBox(group, "Cabinet_Body", new Vector3(0f, 0.72f, 0f), new Vector3(0.85f, 1.44f, 0.60f), matMetalBrushed, true);
        for (int i = 0; i < 3; i++)
        {
            CreateLocalBox(group, $"DrawerHandle_{i + 1}", new Vector3(0f, 0.32f + i * 0.42f, -0.31f), new Vector3(0.68f, 0.03f, 0.02f), matMetalCharcoal, false);
        }
    }

    private static void CreateServerRackUnit(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;

        CreateLocalBox(group, "Rack_Cabinet", new Vector3(0f, 1.15f, 0f), new Vector3(1.05f, 2.30f, 0.95f), matMetalCharcoal, true);
        for (int i = 0; i < 5; i++)
        {
            float y = 0.36f + i * 0.40f;
            CreateLocalBox(group, $"Blade_{i + 1}", new Vector3(0f, y, -0.48f), new Vector3(0.90f, 0.26f, 0.03f), matMetalBrushed, false);
            CreateLocalBox(group, $"LED_Strip_{i + 1}", new Vector3(0.34f, y, -0.50f), new Vector3(0.12f, 0.04f, 0.02f), matServerLedCyan, false);
        }
    }

    private static void CreateSofa(Transform parent, string name, Vector3 pos, float yaw, Material fabricMat)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateLocalBox(group, "Sofa_SeatBase", new Vector3(0f, 0.26f, 0f), new Vector3(2.10f, 0.48f, 0.86f), fabricMat, true);
        CreateLocalBox(group, "Sofa_Backrest", new Vector3(0f, 0.66f, 0.34f), new Vector3(2.10f, 0.52f, 0.20f), fabricMat, false);
        CreateLocalBox(group, "Sofa_ArmLeft", new Vector3(-0.98f, 0.48f, 0f), new Vector3(0.16f, 0.56f, 0.86f), fabricMat, false);
        CreateLocalBox(group, "Sofa_ArmRight", new Vector3(0.98f, 0.48f, 0f), new Vector3(0.16f, 0.56f, 0.86f), fabricMat, false);
    }

    private static void CreateIndoorPlant(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;

        CreateLocalBox(group, "Planter_Pot", new Vector3(0f, 0.26f, 0f), new Vector3(0.52f, 0.52f, 0.52f), matExtPlasterAccent, true);
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Plant_Foliage";
        foliage.transform.SetParent(group, false);
        foliage.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        foliage.transform.localScale = new Vector3(0.82f, 1.05f, 0.82f);
        foliage.GetComponent<Renderer>().sharedMaterial = matFoliageGreen;
        Collider c = foliage.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
    }

    private static void CreateTree(Transform parent, string name, Vector3 pos, float scale)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.localScale = Vector3.one * scale;

        CreateLocalBox(group, "Tree_Trunk", new Vector3(0f, 1.35f, 0f), new Vector3(0.36f, 2.70f, 0.36f), matTreeBark, true);
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Tree_Canopy";
        canopy.transform.SetParent(group, false);
        canopy.transform.localPosition = new Vector3(0f, 3.35f, 0f);
        canopy.transform.localScale = new Vector3(2.6f, 2.4f, 2.6f);
        canopy.GetComponent<Renderer>().sharedMaterial = matFoliageGreen;
        Collider c = canopy.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
    }

    private static void CreateOutdoorBench(Transform parent, string name, Vector3 pos, float yaw)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateLocalBox(group, "Bench_Seat", new Vector3(0f, 0.45f, 0f), new Vector3(1.65f, 0.08f, 0.52f), matWoodWalnut, true);
        CreateLocalBox(group, "Bench_Back", new Vector3(0f, 0.78f, -0.22f), new Vector3(1.65f, 0.42f, 0.06f), matWoodWalnut, false);
        CreateLocalBox(group, "Bench_LegL", new Vector3(-0.68f, 0.22f, 0f), new Vector3(0.10f, 0.44f, 0.48f), matMetalCharcoal, false);
        CreateLocalBox(group, "Bench_LegR", new Vector3(0.68f, 0.22f, 0f), new Vector3(0.10f, 0.44f, 0.48f), matMetalCharcoal, false);
    }

    private static void CreateExteriorLampPost(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;

        CreateLocalBox(group, "Pole", new Vector3(0f, 1.75f, 0f), new Vector3(0.14f, 3.50f, 0.14f), matMetalCharcoal, true);
        CreateLocalBox(group, "LampHead", new Vector3(0f, 3.55f, 0f), new Vector3(0.38f, 0.16f, 0.38f), matLampWarmEmissive, false);
    }

    // =========================================================================
    // LOW-LEVEL PRIMITIVE & GROUP UTILITIES
    // =========================================================================

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void CreateRoomLabel(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
    }

    private static Transform CreateEmptyMarker(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        return go.transform;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 worldPos, Vector3 scale, Material mat, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;

        if (!withCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        return go;
    }

    private static GameObject CreateLocalBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;

        if (!withCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        return go;
    }
}
#endif