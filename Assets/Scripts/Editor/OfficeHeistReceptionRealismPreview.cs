#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Reversible realism pass for the main entrance reception.
/// It replaces only Furniture/Reception while the preview is active.
/// Gameplay markers, architecture, doors, mission objects and guard routes are untouched.
/// </summary>
public static class OfficeHeistReceptionRealismPreview
{
    private const string SceneName = "Office_Heist";
    private const string BuildingRootName = "OfficeHeist_Generated";
    private const string PreviewRootName = "Reception_Realism_PREVIEW";
    private const string BackupName = "OriginalReceptionFurniture_BACKUP";
    private const string MaterialFolder = "Assets/Generated/ReceptionRealism/Materials";

    private static Material walnut;
    private static Material walnutDark;
    private static Material stone;
    private static Material brass;
    private static Material charcoal;
    private static Material fabric;
    private static Material carpet;
    private static Material plaster;
    private static Material glass;
    private static Material paper;
    private static Material foliage;
    private static Material ceramic;
    private static Material warmEmission;
    private static Material screenEmission;
    private static Material redPaint;

    private enum SurfaceStyle
    {
        Wood,
        Marble,
        BrushedMetal,
        Fabric,
        Carpet,
        Plaster,
        Paper,
        Ceramic,
        Flat
    }

    [MenuItem("Tools/Gardien Furtif/Reception Realism/1. Build Reception Preview", false, 10)]
    public static void BuildPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, SceneName, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Reception Realism",
                "Open the Office_Heist scene before building the reception preview.",
                "OK");
            return;
        }

        GameObject building = GameObject.Find(BuildingRootName);
        if (building == null)
        {
            EditorUtility.DisplayDialog(
                "Reception Realism",
                "OfficeHeist_Generated was not found. Build the Office Heist level first.",
                "OK");
            return;
        }

        Transform furniture = building.transform.Find("Furniture");
        if (furniture == null)
        {
            EditorUtility.DisplayDialog(
                "Reception Realism",
                "The expected Furniture group was not found. No scene objects were changed.",
                "OK");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Reception Realism Preview");

        // Restore the original group first so repeated builds remain safe and idempotent.
        RemovePreviewInternal(building.transform, furniture);
        Transform originalReception = furniture.Find("Reception");
        if (originalReception == null)
        {
            EditorUtility.DisplayDialog(
                "Reception Realism",
                "The original Furniture/Reception group was not found. No scene objects were changed.",
                "OK");
            return;
        }

        EnsureMaterials();

        GameObject preview = new GameObject(PreviewRootName);
        preview.transform.SetParent(building.transform, false);
        Undo.RegisterCreatedObjectUndo(preview, "Create Reception Realism Preview");

        Transform backup = CreateGroup(BackupName, preview.transform);
        Undo.SetTransformParent(originalReception, backup, "Back up original reception furniture");
        Undo.RecordObject(originalReception.gameObject, "Hide original reception furniture");
        originalReception.gameObject.SetActive(false);

        Transform architecture = CreateGroup("01_ArchitecturalDetails", preview.transform);
        Transform desk = CreateGroup("02_ReceptionCounter", preview.transform);
        Transform waiting = CreateGroup("03_WaitingLounge", preview.transform);
        Transform decor = CreateGroup("04_OfficeDetails", preview.transform);
        Transform ceiling = CreateGroup("05_ReceptionCeiling", preview.transform);
        Transform lighting = CreateGroup("06_ReceptionLighting", preview.transform);

        BuildWallFinishes(architecture);
        BuildCounter(desk);
        BuildWaitingArea(waiting);
        BuildOfficeDetails(decor);
        BuildReceptionCeiling(ceiling);
        BuildReceptionLighting(lighting);

        Selection.activeGameObject = preview;
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[ReceptionRealismPreview] Built a reversible reception realism preview. " +
            "Use Tools > Gardien Furtif > Reception Realism > 2. Remove Preview to restore the original.");
        EditorUtility.DisplayDialog(
            "Reception Preview Built",
            "The upgraded reception is now in the scene. The original reception furniture is safely backed up inside the preview root. Use Remove Preview to restore it.",
            "OK");
    }

    [MenuItem("Tools/Gardien Furtif/Reception Realism/2. Remove Preview & Restore Original", false, 20)]
    public static void RemovePreview()
    {
        GameObject building = GameObject.Find(BuildingRootName);
        if (building == null)
        {
            EditorUtility.DisplayDialog("Reception Realism", "OfficeHeist_Generated was not found.", "OK");
            return;
        }

        Transform furniture = building.transform.Find("Furniture");
        Transform preview = building.transform.Find(PreviewRootName);
        if (preview == null)
        {
            EditorUtility.DisplayDialog("Reception Realism", "No reception preview was found.", "OK");
            return;
        }

        RemovePreviewInternal(building.transform, furniture);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[ReceptionRealismPreview] Removed the preview and restored the original reception furniture.");
    }

    [MenuItem("Tools/Gardien Furtif/Reception Realism/3. Select Preview Root", false, 30)]
    public static void SelectPreviewRoot()
    {
        GameObject building = GameObject.Find(BuildingRootName);
        Transform preview = building != null ? building.transform.Find(PreviewRootName) : null;
        if (preview == null)
        {
            EditorUtility.DisplayDialog("Reception Realism", "Build the reception preview first.", "OK");
            return;
        }

        Selection.activeGameObject = preview.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.LookAt(
                new Vector3(-9.0f, 1.15f, -6.0f),
                Quaternion.Euler(24f, 0f, 0f),
                10.5f);
        }
    }

    private static void RemovePreviewInternal(Transform buildingRoot, Transform furniture)
    {
        Transform preview = buildingRoot.Find(PreviewRootName);
        if (preview == null) return;

        Transform backup = preview.Find(BackupName);
        Transform original = backup != null ? backup.Find("Reception") : null;
        if (original != null && furniture != null)
        {
            Undo.SetTransformParent(original, furniture, "Restore original reception furniture");
            Undo.RecordObject(original.gameObject, "Restore original reception furniture");
            original.name = "Reception";
            original.gameObject.SetActive(true);
        }

        Undo.DestroyObjectImmediate(preview.gameObject);
    }

    // ---------------------------------------------------------------------
    // MATERIALS: procedural albedo + tangent-space normal maps
    // ---------------------------------------------------------------------

    private static void EnsureMaterials()
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder("Assets/Generated/ReceptionRealism");
        EnsureFolder(MaterialFolder);

        walnut = MakeMaterial("Reception_Walnut_Veneer", new Color(0.34f, 0.20f, 0.12f), SurfaceStyle.Wood, 0.36f, 0.0f);
        walnutDark = MakeMaterial("Reception_Dark_Walnut", new Color(0.16f, 0.09f, 0.055f), SurfaceStyle.Wood, 0.42f, 0.0f);
        stone = MakeMaterial("Reception_Charcoal_Marble", new Color(0.18f, 0.20f, 0.21f), SurfaceStyle.Marble, 0.78f, 0.08f);
        brass = MakeMaterial("Reception_Brushed_Brass", new Color(0.62f, 0.40f, 0.16f), SurfaceStyle.BrushedMetal, 0.68f, 0.72f);
        charcoal = MakeMaterial("Reception_Matte_Charcoal", new Color(0.055f, 0.065f, 0.075f), SurfaceStyle.Flat, 0.30f, 0.18f);
        fabric = MakeMaterial("Reception_Taupe_Fabric", new Color(0.40f, 0.35f, 0.30f), SurfaceStyle.Fabric, 0.16f, 0.0f);
        carpet = MakeMaterial("Reception_Deep_Green_Carpet", new Color(0.075f, 0.105f, 0.09f), SurfaceStyle.Carpet, 0.12f, 0.0f);
        plaster = MakeMaterial("Reception_Wall_Panel", new Color(0.72f, 0.68f, 0.59f), SurfaceStyle.Plaster, 0.18f, 0.0f);
        glass = MakeMaterial("Reception_Smoked_Glass", new Color(0.30f, 0.39f, 0.43f), SurfaceStyle.Flat, 0.88f, 0.15f);
        paper = MakeMaterial("Reception_Paper", new Color(0.88f, 0.85f, 0.76f), SurfaceStyle.Paper, 0.10f, 0.0f);
        foliage = MakeMaterial("Reception_Leaf_Green", new Color(0.13f, 0.28f, 0.15f), SurfaceStyle.Flat, 0.12f, 0.0f);
        ceramic = MakeMaterial("Reception_Ceramic", new Color(0.30f, 0.34f, 0.33f), SurfaceStyle.Ceramic, 0.55f, 0.0f);
        redPaint = MakeMaterial("Reception_FireExtinguisher_Red", new Color(0.62f, 0.045f, 0.025f), SurfaceStyle.Flat, 0.28f, 0.0f);
        warmEmission = MakeEmission("Reception_Warm_LED", new Color(1.0f, 0.68f, 0.30f) * 2.1f);
        screenEmission = MakeEmission("Reception_Screen_Glow", new Color(0.08f, 0.42f, 0.52f) * 1.0f);

        AssetDatabase.SaveAssets();
    }

    private static Material MakeMaterial(string name, Color tone, SurfaceStyle style, float smoothness, float metallic)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = ResolveLitShader();

        if (material == null)
        {
            material = new Material(shader);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        material.color = Color.white;
        SetColorProperty(material, "_BaseColor", Color.white);
        SetColorProperty(material, "_Color", Color.white);
        SetFloatProperty(material, "_Metallic", metallic);
        SetFloatProperty(material, "_Smoothness", smoothness);
        SetFloatProperty(material, "_Glossiness", smoothness);

        Texture2D albedo = FindTexture(path, name + "_Albedo");
        if (albedo == null)
        {
            albedo = GenerateTexture(name + "_Albedo", tone, style, false);
            AssetDatabase.AddObjectToAsset(albedo, material);
        }

        Texture2D normal = FindTexture(path, name + "_Normal");
        if (normal == null)
        {
            normal = GenerateTexture(name + "_Normal", tone, style, true);
            AssetDatabase.AddObjectToAsset(normal, material);
        }

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            SetFloatProperty(material, "_BumpScale", 0.45f);
        }

        albedo.wrapMode = TextureWrapMode.Repeat;
        normal.wrapMode = TextureWrapMode.Repeat;
        albedo.anisoLevel = 8;
        normal.anisoLevel = 8;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeEmission(string name, Color emission)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(ResolveLitShader());
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = Color.white;
        SetColorProperty(material, "_BaseColor", Color.white);
        SetColorProperty(material, "_Color", Color.white);
        material.EnableKeyword("_EMISSION");
        SetColorProperty(material, "_EmissionColor", emission);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader ResolveLitShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) return urp;
            Shader hdrp = Shader.Find("HDRP/Lit");
            if (hdrp != null) return hdrp;
        }

        Shader standard = Shader.Find("Standard");
        if (standard != null) return standard;
        return Shader.Find("Diffuse");
    }

    private static Texture2D FindTexture(string assetPath, string expectedName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            Texture2D texture = asset as Texture2D;
            if (texture != null && texture.name == expectedName) return texture;
        }
        return null;
    }

    private static Texture2D GenerateTexture(string textureName, Color tone, SurfaceStyle style, bool makeNormal)
    {
        const int size = 256;
        const float strength = 1.8f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, makeNormal);
        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float h = SurfaceHeight(u, v, style);

                if (makeNormal)
                {
                    float left = SurfaceHeight(u - 1f / size, v, style);
                    float right = SurfaceHeight(u + 1f / size, v, style);
                    float down = SurfaceHeight(u, v - 1f / size, style);
                    float up = SurfaceHeight(u, v + 1f / size, style);
                    Vector3 n = new Vector3((left - right) * strength, (down - up) * strength, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
                else
                {
                    float variation = Mathf.Clamp01(h * 0.20f + 0.43f);
                    Color tint = Color.Lerp(tone * 0.82f, tone * 1.16f, variation);
                    if (style == SurfaceStyle.Marble)
                    {
                        float vein = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 8f + Mathf.PerlinNoise(u * 4f, v * 7f)) * 21f))), 12f);
                        tint = Color.Lerp(tint, new Color(0.58f, 0.54f, 0.46f), vein * 0.42f);
                    }
                    else if (style == SurfaceStyle.BrushedMetal)
                    {
                        float scratch = Mathf.PerlinNoise(u * 150f, v * 7f) * 0.10f;
                        tint = Color.Lerp(tint, tone * 1.28f, scratch);
                    }
                    else if (style == SurfaceStyle.Fabric)
                    {
                        float weave = ((Mathf.Sin(u * 620f) + Mathf.Sin(v * 620f)) * 0.5f + 0.5f) * 0.045f;
                        tint *= 0.94f + weave;
                    }
                    else if (style == SurfaceStyle.Carpet)
                    {
                        float fibre = Mathf.PerlinNoise(u * 180f, v * 180f) * 0.20f;
                        tint = Color.Lerp(tone * 0.72f, tone * 1.20f, fibre);
                    }
                    else if (style == SurfaceStyle.Paper)
                    {
                        float fibres = Mathf.PerlinNoise(u * 100f, v * 140f) * 0.06f;
                        tint *= 0.94f + fibres;
                    }

                    pixels[y * size + x] = new Color(tint.r, tint.g, tint.b, 1f);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private static float SurfaceHeight(float u, float v, SurfaceStyle style)
    {
        float broad = Mathf.PerlinNoise(u * 5.7f + 1.8f, v * 6.3f + 2.7f);
        float fine = Mathf.PerlinNoise(u * 35.0f + 4.1f, v * 39.0f + 7.6f);
        float micro = Mathf.PerlinNoise(u * 110.0f + 3.0f, v * 105.0f + 8.0f);

        switch (style)
        {
            case SurfaceStyle.Wood:
                return broad * 0.12f + Mathf.PerlinNoise(u * 14f, v * 2f) * 0.24f + fine * 0.025f;
            case SurfaceStyle.Marble:
                return broad * 0.10f + fine * 0.035f + Mathf.Sin((u * 8f + broad) * 20f) * 0.018f;
            case SurfaceStyle.BrushedMetal:
                return broad * 0.025f + Mathf.PerlinNoise(u * 180f, v * 4f) * 0.045f;
            case SurfaceStyle.Fabric:
                return broad * 0.08f + (Mathf.Sin(u * 620f) + Mathf.Sin(v * 620f)) * 0.008f;
            case SurfaceStyle.Carpet:
                return broad * 0.12f + fine * 0.07f + micro * 0.035f;
            case SurfaceStyle.Plaster:
                return broad * 0.13f + fine * 0.045f + micro * 0.012f;
            case SurfaceStyle.Paper:
                return fine * 0.035f + micro * 0.018f;
            case SurfaceStyle.Ceramic:
                return broad * 0.08f + fine * 0.025f;
            default:
                return broad * 0.015f;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetColorProperty(Material material, string property, Color color)
    {
        if (material.HasProperty(property)) material.SetColor(property, color);
    }

    private static void SetFloatProperty(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }

    // ---------------------------------------------------------------------
    // ARCHITECTURAL FINISHES
    // ---------------------------------------------------------------------

    private static void BuildWallFinishes(Transform parent)
    {
        // The lobby's north wall has a 2.2m doorway at X=-9. The panels occupy
        // only the solid section on its east side and never narrow the opening.
        CreateBox(parent, "Reception_Backdrop_Main", new Vector3(-5.45f, 1.60f, -2.20f), new Vector3(4.65f, 2.65f, 0.10f), walnutDark, false);
        CreateBox(parent, "Reception_Backdrop_Inlay", new Vector3(-5.45f, 1.60f, -2.26f), new Vector3(4.35f, 2.38f, 0.025f), plaster, false);
        CreateBox(parent, "Reception_Backdrop_Header", new Vector3(-5.45f, 2.78f, -2.285f), new Vector3(3.55f, 0.035f, 0.018f), brass, false);
        CreateBox(parent, "Reception_Backdrop_Footer", new Vector3(-5.45f, 0.43f, -2.285f), new Vector3(3.55f, 0.035f, 0.018f), brass, false);

        for (int i = 0; i < 17; i++)
        {
            float x = -7.40f + i * 0.245f;
            CreateBox(parent, "Reception_Backdrop_Slat_" + i, new Vector3(x, 1.63f, -2.282f), new Vector3(0.028f, 2.15f, 0.022f), walnut, false);
        }

        CreateBox(parent, "Reception_Backdrop_Plaque", new Vector3(-5.45f, 2.10f, -2.31f), new Vector3(2.70f, 0.56f, 0.055f), charcoal, false);
        CreateWorldText(parent, "Reception_Backdrop_Title", "RECEPTION", new Vector3(-5.45f, 2.17f, -2.345f), Quaternion.Euler(0f, 180f, 0f), 0.0060f, new Color(0.86f, 0.70f, 0.43f));
        CreateWorldText(parent, "Reception_Backdrop_Subtitle", "VISITOR SERVICES", new Vector3(-5.45f, 1.98f, -2.345f), Quaternion.Euler(0f, 180f, 0f), 0.0022f, new Color(0.82f, 0.82f, 0.77f));

        // Wainscot and warm skirting: placed only along solid wall segments.
        CreateBox(parent, "Reception_West_Baseboard", new Vector3(-14.82f, 0.11f, -6.0f), new Vector3(0.12f, 0.20f, 7.55f), walnutDark, false);
        CreateBox(parent, "Reception_West_Wainscot", new Vector3(-14.82f, 0.55f, -6.0f), new Vector3(0.10f, 0.62f, 7.35f), plaster, false);
        CreateBox(parent, "Reception_West_Wainscot_Cap", new Vector3(-14.75f, 0.88f, -6.0f), new Vector3(0.16f, 0.045f, 7.35f), brass, false);

        CreateWallBaseSegment(parent, "Reception_East_Base_A", new Vector3(-3.13f, 0.11f, -8.40f), new Vector3(0.12f, 0.20f, 3.20f));
        CreateWallBaseSegment(parent, "Reception_East_Base_B", new Vector3(-3.13f, 0.11f, -4.85f), new Vector3(0.12f, 0.20f, 0.60f));
        CreateWallBaseSegment(parent, "Reception_East_Base_C", new Vector3(-3.13f, 0.11f, -2.55f), new Vector3(0.12f, 0.20f, 0.90f));

        CreateWallBaseSegment(parent, "Reception_North_Base_West", new Vector3(-12.55f, 0.11f, -2.13f), new Vector3(4.70f, 0.20f, 0.10f));
        CreateWallBaseSegment(parent, "Reception_North_Base_East", new Vector3(-5.30f, 0.11f, -2.13f), new Vector3(4.85f, 0.20f, 0.10f));

        // Narrow vertical blinds over the existing west window, without a collider.
        for (int i = 0; i < 13; i++)
        {
            float z = -7.18f + i * 0.197f;
            GameObject slat = CreatePrimitive(parent, "Reception_Window_Blind_" + i, PrimitiveType.Cube,
                new Vector3(-14.76f, 1.70f, z), new Vector3(0.035f, 1.42f, 0.095f), ceramic, false);
            slat.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0) ? -7f : 7f, 0f);
        }
        CreateBox(parent, "Reception_Window_Blind_Rail", new Vector3(-14.73f, 2.46f, -6.0f), new Vector3(0.09f, 0.055f, 2.65f), charcoal, false);

        // Discreet framed artwork on the west wall beside the waiting area.
        CreateBox(parent, "Reception_West_Art_Frame", new Vector3(-14.72f, 2.10f, -8.55f), new Vector3(0.10f, 0.78f, 1.20f), walnutDark, false);
        CreateBox(parent, "Reception_West_Art_Print", new Vector3(-14.65f, 2.10f, -8.55f), new Vector3(0.025f, 0.64f, 1.05f), plaster, false);
        CreateBox(parent, "Reception_West_Art_Line_1", new Vector3(-14.62f, 2.14f, -8.55f), new Vector3(0.014f, 0.035f, 0.74f), brass, false);
        CreateBox(parent, "Reception_West_Art_Line_2", new Vector3(-14.62f, 1.98f, -8.55f), new Vector3(0.014f, 0.02f, 0.42f), walnut, false);

        // Direction plate above the corridor doorway; it does not cover the opening.
        CreateBox(parent, "Reception_MainHall_Sign", new Vector3(-9.0f, 2.72f, -2.56f), new Vector3(1.55f, 0.18f, 0.38f), charcoal, false);
        CreateWorldText(parent, "Reception_MainHall_Sign_Text", "MAIN HALL", new Vector3(-9.0f, 2.70f, -2.77f), Quaternion.Euler(0f, 180f, 0f), 0.0035f, new Color(0.88f, 0.73f, 0.48f));
    }

    private static void CreateWallBaseSegment(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        CreateBox(parent, name, pos, size, walnutDark, false);
    }

    // ---------------------------------------------------------------------
    // RECEPTION COUNTER: textured timber, stone top and brass detailing
    // ---------------------------------------------------------------------

    private static void BuildCounter(Transform parent)
    {
        Transform group = CreateGroup("Reception_ArchitecturalCounter", parent);
        group.position = new Vector3(-9.0f, 0f, -5.2f);

        // Main body retains the approximate footprint and height of the original stealth-cover counter.
        CreateLocalBox(group, "Counter_Main_ColliderBody", new Vector3(0f, 0.56f, 0f),
            new Vector3(4.20f, 1.12f, 0.90f), walnutDark, true);
        CreateLocalBox(group, "Counter_Front_Veneer", new Vector3(0f, 0.57f, -0.465f),
            new Vector3(4.12f, 0.98f, 0.035f), walnut, false);
        CreateLocalBox(group, "Counter_ToeKick", new Vector3(0f, 0.075f, -0.47f),
            new Vector3(4.10f, 0.12f, 0.055f), charcoal, false);
        CreateLocalBox(group, "Counter_Brass_Lower_Inlay", new Vector3(0f, 0.17f, -0.505f),
            new Vector3(4.04f, 0.018f, 0.018f), brass, false);
        CreateLocalBox(group, "Counter_Brass_Upper_Inlay", new Vector3(0f, 0.91f, -0.505f),
            new Vector3(4.04f, 0.018f, 0.018f), brass, false);

        // Vertical fluting adds depth to the large front plane.
        for (int i = 0; i < 29; i++)
        {
            float x = -1.94f + i * 0.138f;
            CreateLocalBox(group, "Counter_Front_Flute_" + i, new Vector3(x, 0.55f, -0.493f),
                new Vector3(0.026f, 0.86f, 0.025f), walnutDark, false);
        }

        // Brushed-brass logo medallion, not a gameplay object.
        CreateLocalBox(group, "Counter_Logo_Plaque", new Vector3(0f, 0.57f, -0.525f),
            new Vector3(0.60f, 0.36f, 0.028f), charcoal, false);
        CreateLocalBox(group, "Counter_Logo_LineTop", new Vector3(0f, 0.72f, -0.546f),
            new Vector3(0.32f, 0.018f, 0.012f), brass, false);
        CreateLocalBox(group, "Counter_Logo_LineBottom", new Vector3(0f, 0.42f, -0.546f),
            new Vector3(0.32f, 0.018f, 0.012f), brass, false);
        CreateLocalCylinder(group, "Counter_Logo_Seal", new Vector3(0f, 0.57f, -0.555f),
            new Vector3(0.13f, 0.025f, 0.13f), brass, false, Quaternion.Euler(90f, 0f, 0f));

        // Polished stone transaction counter with a softened layered edge.
        CreateLocalBox(group, "Counter_StoneTop", new Vector3(0f, 1.15f, 0f),
            new Vector3(4.52f, 0.12f, 1.05f), stone, false);
        CreateLocalBox(group, "Counter_StoneTop_Edge", new Vector3(0f, 1.085f, -0.005f),
            new Vector3(4.48f, 0.035f, 1.015f), charcoal, false);
        CreateLocalBox(group, "Counter_Staff_Lip", new Vector3(0f, 1.255f, 0.34f),
            new Vector3(2.45f, 0.15f, 0.10f), walnut, false);
        CreateLocalBox(group, "Counter_Staff_Lip_Brass", new Vector3(0f, 1.33f, 0.34f),
            new Vector3(2.48f, 0.018f, 0.105f), brass, false);

        // Side return: keep the original L-shape silhouette and collision volume.
        Transform wing = CreateGroup("Reception_Counter_SideReturn", parent);
        wing.position = new Vector3(-6.90f, 0f, -4.10f);
        CreateLocalBox(wing, "SideReturn_ColliderBody", new Vector3(0f, 0.56f, 0f),
            new Vector3(0.82f, 1.12f, 1.90f), walnutDark, true);
        CreateLocalBox(wing, "SideReturn_FrontVeneer", new Vector3(0.43f, 0.57f, 0f),
            new Vector3(0.035f, 0.98f, 1.82f), walnut, false);
        CreateLocalBox(wing, "SideReturn_StoneTop", new Vector3(0f, 1.15f, 0f),
            new Vector3(0.98f, 0.12f, 2.04f), stone, false);
        CreateLocalBox(wing, "SideReturn_BrassEdge", new Vector3(0f, 1.225f, 0f),
            new Vector3(0.99f, 0.018f, 2.05f), brass, false);

        BuildReceptionComputer(group, "Reception_PC_Left", new Vector3(-0.82f, 1.21f, -0.12f));
        BuildReceptionComputer(group, "Reception_PC_Right", new Vector3(0.82f, 1.21f, -0.12f));

        BuildKeyboard(group, "Reception_Keyboard_Left", new Vector3(-0.82f, 1.225f, -0.39f));
        BuildKeyboard(group, "Reception_Keyboard_Right", new Vector3(0.82f, 1.225f, -0.39f));
        BuildDeskPhone(group, "Reception_DeskPhone", new Vector3(1.48f, 1.23f, 0.22f));
        BuildPaperwork(group, "Reception_VisitorForms", new Vector3(-1.48f, 1.225f, 0.18f));
        BuildPenCup(group, "Reception_PenHolder", new Vector3(-1.75f, 1.23f, 0.18f));
        BuildTaskLamp(group, "Reception_DeskLamp", new Vector3(1.78f, 1.22f, 0.12f));

        BuildStaffChair(parent, "Reception_StaffChair_Left", new Vector3(-9.82f, 0f, -4.05f), 180f);
        BuildStaffChair(parent, "Reception_StaffChair_Right", new Vector3(-8.18f, 0f, -4.05f), 180f);

        // Low-level LED glow beneath the transaction top.
        CreateLocalBox(group, "Counter_UnderTop_LED", new Vector3(0f, 1.065f, -0.505f),
            new Vector3(3.80f, 0.025f, 0.018f), warmEmission, false);
    }

    private static void BuildReceptionComputer(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        CreateLocalBox(group, "Monitor_Base", new Vector3(0f, 0.015f, 0f), new Vector3(0.34f, 0.035f, 0.23f), charcoal, false);
        CreateLocalBox(group, "Monitor_Stand", new Vector3(0f, 0.12f, 0.03f), new Vector3(0.08f, 0.20f, 0.07f), brass, false);
        CreateLocalBox(group, "Monitor_Bezel", new Vector3(0f, 0.39f, 0f), new Vector3(0.66f, 0.43f, 0.055f), charcoal, false);
        CreateLocalBox(group, "Monitor_Display", new Vector3(0f, 0.39f, -0.032f), new Vector3(0.60f, 0.37f, 0.012f), screenEmission, false);
        CreateLocalBox(group, "Monitor_StatusLight", new Vector3(0.28f, 0.20f, -0.043f), new Vector3(0.018f, 0.018f, 0.008f), warmEmission, false);
    }

    private static void BuildKeyboard(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        CreateLocalBox(group, "Keyboard_Base", Vector3.zero, new Vector3(0.52f, 0.035f, 0.19f), charcoal, false);
        for (int row = 0; row < 3; row++)
        {
            for (int key = 0; key < 12; key++)
            {
                CreateLocalBox(group, "Key_" + row + "_" + key,
                    new Vector3(-0.235f + key * 0.043f, 0.022f, -0.062f + row * 0.055f),
                    new Vector3(0.032f, 0.012f, 0.038f), ceramic, false);
            }
        }

        GameObject mouse = CreatePrimitive(group, "Mouse", PrimitiveType.Sphere,
            new Vector3(0.34f, 0.025f, -0.01f), new Vector3(0.075f, 0.045f, 0.12f), charcoal, false);
    }

    private static void BuildDeskPhone(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        CreateLocalBox(group, "Phone_Base", Vector3.zero, new Vector3(0.30f, 0.055f, 0.23f), charcoal, false);
        CreateLocalBox(group, "Phone_Display", new Vector3(0f, 0.031f, -0.025f), new Vector3(0.18f, 0.012f, 0.07f), screenEmission, false);
        CreateLocalBox(group, "Phone_Handset", new Vector3(0f, 0.09f, 0.085f), new Vector3(0.30f, 0.045f, 0.055f), charcoal, false);
        for (int i = 0; i < 9; i++)
        {
            CreateLocalCylinder(group, "Phone_Key_" + i,
                new Vector3(-0.085f + (i % 3) * 0.085f, 0.043f, -0.065f + (i / 3) * 0.045f),
                new Vector3(0.025f, 0.01f, 0.025f), brass, false, Quaternion.identity);
        }
    }

    private static void BuildPaperwork(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        for (int i = 0; i < 4; i++)
        {
            GameObject sheet = CreateLocalBox(group, "Form_" + i,
                new Vector3(i * 0.012f, i * 0.008f, i * 0.012f),
                new Vector3(0.28f, 0.008f, 0.36f), paper, false);
            sheet.transform.localRotation = Quaternion.Euler(0f, (i - 1.5f) * 2.2f, 0f);
        }
        CreateLocalBox(group, "Folder_Cover", new Vector3(-0.02f, 0.05f, 0.02f),
            new Vector3(0.30f, 0.012f, 0.37f), walnut, false);
    }

    private static void BuildPenCup(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        CreateLocalCylinder(group, "Cup", new Vector3(0f, 0.09f, 0f),
            new Vector3(0.10f, 0.09f, 0.10f), ceramic, false, Quaternion.identity);
        for (int i = 0; i < 5; i++)
        {
            GameObject pen = CreatePrimitive(group, "Pen_" + i, PrimitiveType.Cylinder,
                new Vector3(-0.05f + i * 0.024f, 0.23f, (i % 2 == 0) ? 0.01f : -0.01f),
                new Vector3(0.008f, 0.11f, 0.008f), brass, false);
            pen.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 2) * 5f);
        }
    }

    private static void BuildTaskLamp(Transform parent, string name, Vector3 localPos)
    {
        Transform group = CreateGroup(name, parent);
        group.localPosition = localPos;
        CreateLocalCylinder(group, "Lamp_Base", new Vector3(0f, 0.025f, 0f),
            new Vector3(0.16f, 0.025f, 0.16f), brass, false, Quaternion.identity);
        GameObject stem = CreatePrimitive(group, "Lamp_Stem", PrimitiveType.Cylinder,
            new Vector3(0f, 0.17f, 0f), new Vector3(0.018f, 0.15f, 0.018f), charcoal, false);
        stem.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        GameObject shade = CreatePrimitive(group, "Lamp_Shade", PrimitiveType.Cylinder,
            new Vector3(0f, 0.32f, -0.06f), new Vector3(0.11f, 0.045f, 0.11f), warmEmission, false);
    }

    private static void BuildStaffChair(Transform parent, string name, Vector3 pos, float yaw)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateLocalBox(group, "Seat_Pan", new Vector3(0f, 0.43f, 0f), new Vector3(0.62f, 0.12f, 0.58f), charcoal, true);
        CreateLocalBox(group, "Seat_Cushion", new Vector3(0f, 0.52f, 0f), new Vector3(0.59f, 0.10f, 0.55f), fabric, false);
        CreateLocalBox(group, "Back_Frame", new Vector3(0f, 0.91f, -0.25f), new Vector3(0.60f, 0.70f, 0.10f), charcoal, false);
        CreateLocalBox(group, "Back_Cushion", new Vector3(0f, 0.92f, -0.18f), new Vector3(0.55f, 0.62f, 0.12f), fabric, false);
        CreateLocalCylinder(group, "Chair_GasLift", new Vector3(0f, 0.22f, 0f),
            new Vector3(0.055f, 0.20f, 0.055f), brass, false, Quaternion.identity);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f;
            Vector3 legPos = new Vector3(Mathf.Cos(a) * 0.22f, 0.055f, Mathf.Sin(a) * 0.22f);
            GameObject leg = CreatePrimitive(group, "Chair_BaseLeg_" + i, PrimitiveType.Cube,
                legPos, new Vector3(0.34f, 0.055f, 0.07f), charcoal, false);
            leg.transform.localRotation = Quaternion.Euler(0f, -i * 72f, 0f);
            CreateLocalCylinder(group, "Chair_Caster_" + i,
                legPos + new Vector3(Mathf.Cos(a) * 0.15f, -0.02f, Mathf.Sin(a) * 0.15f),
                new Vector3(0.045f, 0.04f, 0.045f), charcoal, false, Quaternion.identity);
        }
        CreateLocalBox(group, "Chair_Arm_Left", new Vector3(-0.34f, 0.72f, 0f), new Vector3(0.07f, 0.35f, 0.48f), charcoal, false);
        CreateLocalBox(group, "Chair_Arm_Right", new Vector3(0.34f, 0.72f, 0f), new Vector3(0.07f, 0.35f, 0.48f), charcoal, false);
    }

    // ---------------------------------------------------------------------
    // WAITING LOUNGE
    // ---------------------------------------------------------------------

    private static void BuildWaitingArea(Transform parent)
    {
        // A carpeted island differentiates the visitor lounge from the main walk route.
        CreateBox(parent, "Reception_Waiting_Carpet", new Vector3(-12.70f, 0.018f, -7.32f),
            new Vector3(2.95f, 0.025f, 3.55f), carpet, false);
        CreateBox(parent, "Reception_Waiting_Carpet_Border", new Vector3(-12.70f, 0.035f, -7.32f),
            new Vector3(2.82f, 0.012f, 3.42f), brass, false);
        CreateBox(parent, "Reception_Waiting_Carpet_Inset", new Vector3(-12.70f, 0.045f, -7.32f),
            new Vector3(2.76f, 0.012f, 3.36f), carpet, false);

        BuildSofa(parent, "Reception_WaitingSofa_West", new Vector3(-13.60f, 0f, -6.45f), 90f);
        BuildSofa(parent, "Reception_WaitingSofa_South", new Vector3(-12.20f, 0f, -8.70f), 0f);
        BuildCoffeeTable(parent, "Reception_Waiting_CoffeeTable", new Vector3(-12.20f, 0f, -7.18f));

        BuildPlanter(parent, "Reception_Planter_SW", new Vector3(-14.20f, 0f, -9.28f), 1.0f);
        BuildPlanter(parent, "Reception_Planter_NW", new Vector3(-14.20f, 0f, -2.85f), 0.9f);
        BuildPlanter(parent, "Reception_Planter_NE", new Vector3(-3.78f, 0f, -2.85f), 0.88f);
    }

    private static void BuildSofa(Transform parent, string name, Vector3 pos, float yaw)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateLocalBox(group, "Sofa_Base_Collider", new Vector3(0f, 0.28f, 0f),
            new Vector3(2.12f, 0.50f, 0.88f), charcoal, true);
        CreateLocalBox(group, "Sofa_Seat_Cushion", new Vector3(0f, 0.55f, -0.08f),
            new Vector3(1.98f, 0.22f, 0.72f), fabric, false);
        CreateLocalBox(group, "Sofa_Back_Cushion", new Vector3(0f, 0.79f, 0.32f),
            new Vector3(2.02f, 0.60f, 0.20f), fabric, false);
        CreateLocalBox(group, "Sofa_Arm_Left", new Vector3(-0.98f, 0.56f, 0f),
            new Vector3(0.18f, 0.58f, 0.88f), walnutDark, false);
        CreateLocalBox(group, "Sofa_Arm_Right", new Vector3(0.98f, 0.56f, 0f),
            new Vector3(0.18f, 0.58f, 0.88f), walnutDark, false);

        for (int i = 0; i < 3; i++)
        {
            float x = -0.64f + i * 0.64f;
            CreateLocalBox(group, "Sofa_Seat_Stitch_" + i, new Vector3(x, 0.666f, -0.08f),
                new Vector3(0.012f, 0.008f, 0.55f), walnutDark, false);
            CreateLocalBox(group, "Sofa_Back_Seam_" + i, new Vector3(x, 0.80f, 0.205f),
                new Vector3(0.012f, 0.44f, 0.012f), walnutDark, false);
        }

        CreateLocalBox(group, "Sofa_Front_Plinth", new Vector3(0f, 0.045f, -0.02f),
            new Vector3(1.86f, 0.09f, 0.72f), walnutDark, false);
    }

    private static void BuildCoffeeTable(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        CreateLocalBox(group, "CoffeeTable_Frame", new Vector3(0f, 0.39f, 0f),
            new Vector3(1.38f, 0.10f, 0.92f), walnutDark, true);
        CreateLocalBox(group, "CoffeeTable_GlassTop", new Vector3(0f, 0.47f, 0f),
            new Vector3(1.32f, 0.035f, 0.86f), glass, false);
        CreateLocalBox(group, "CoffeeTable_Brass_Inlay", new Vector3(0f, 0.495f, 0f),
            new Vector3(1.22f, 0.012f, 0.76f), brass, false);
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CreateLocalCylinder(group, "CoffeeTable_Leg_" + x + "_" + z,
                    new Vector3(x * 0.53f, 0.20f, z * 0.32f),
                    new Vector3(0.035f, 0.19f, 0.035f), brass, false, Quaternion.identity);
            }
        }
        CreateLocalBox(group, "CoffeeTable_Magazine", new Vector3(-0.25f, 0.505f, 0.06f),
            new Vector3(0.34f, 0.012f, 0.24f), paper, false);
        CreateLocalBox(group, "CoffeeTable_Magazine_Band", new Vector3(-0.25f, 0.514f, 0.06f),
            new Vector3(0.30f, 0.006f, 0.02f), walnut, false);
        CreateLocalCylinder(group, "CoffeeTable_Vase", new Vector3(0.40f, 0.61f, -0.12f),
            new Vector3(0.09f, 0.11f, 0.09f), ceramic, false, Quaternion.identity);
    }

    private static void BuildPlanter(Transform parent, string name, Vector3 pos, float size)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        CreateLocalCylinder(group, "Planter_Pot", new Vector3(0f, 0.28f * size, 0f),
            new Vector3(0.30f * size, 0.28f * size, 0.30f * size), ceramic, true, Quaternion.identity);
        CreateLocalCylinder(group, "Planter_Brass_Rim", new Vector3(0f, 0.50f * size, 0f),
            new Vector3(0.31f * size, 0.025f * size, 0.31f * size), brass, false, Quaternion.identity);
        CreateLocalCylinder(group, "Planter_Soil", new Vector3(0f, 0.515f * size, 0f),
            new Vector3(0.25f * size, 0.012f * size, 0.25f * size), walnutDark, false, Quaternion.identity);

        for (int i = 0; i < 9; i++)
        {
            float angle = i * 40f * Mathf.Deg2Rad;
            Vector3 leafPos = new Vector3(Mathf.Cos(angle) * 0.12f * size, 0.78f * size, Mathf.Sin(angle) * 0.12f * size);
            GameObject leaf = CreatePrimitive(group, "Plant_Leaf_" + i, PrimitiveType.Capsule,
                leafPos, new Vector3(0.10f * size, 0.43f * size, 0.07f * size), foliage, false);
            leaf.transform.localRotation = Quaternion.Euler(0f, i * 40f, (i % 2 == 0) ? -22f : 22f);
        }
        GameObject center = CreatePrimitive(group, "Plant_Center", PrimitiveType.Sphere,
            new Vector3(0f, 0.70f * size, 0f), new Vector3(0.24f * size, 0.38f * size, 0.24f * size), foliage, false);
    }

    // ---------------------------------------------------------------------
    // ADDITIONAL OFFICE DETAILS
    // ---------------------------------------------------------------------

    private static void BuildOfficeDetails(Transform parent)
    {
        // Reception room wall clock on the east partition.
        CreateWallClock(parent, "Reception_WallClock", new Vector3(-3.18f, 2.25f, -8.25f), Quaternion.Euler(0f, -90f, 0f));

        // A mounted safety extinguisher and small waste bin beside the information kiosk.
        BuildFireExtinguisher(parent, "Reception_FireExtinguisher", new Vector3(-3.55f, 0.0f, -8.75f));
        BuildWasteBin(parent, "Reception_WasteBin", new Vector3(-4.25f, 0.0f, -9.20f));

        // Brochure stand, positioned clear of the main entrance-to-hall route.
        Transform stand = CreateGroup("Reception_BrochureStand", parent);
        stand.position = new Vector3(-4.45f, 0f, -7.00f);
        CreateLocalCylinder(stand, "Stand_Base", new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.08f, 0.22f), charcoal, true, Quaternion.identity);
        CreateLocalCylinder(stand, "Stand_Post", new Vector3(0f, 0.55f, 0f), new Vector3(0.035f, 0.48f, 0.035f), brass, false, Quaternion.identity);
        CreateLocalBox(stand, "Stand_Brochure_Holder", new Vector3(0f, 1.02f, -0.02f), new Vector3(0.48f, 0.62f, 0.12f), charcoal, false);
        for (int i = 0; i < 3; i++)
        {
            CreateLocalBox(stand, "Brochure_" + i, new Vector3(-0.12f + i * 0.12f, 1.06f, -0.10f),
                new Vector3(0.10f, 0.40f, 0.018f), paper, false);
        }

        // Small wall-mounted visitor information board on the east wall.
        CreateBox(parent, "Reception_VisitorNotice_Frame", new Vector3(-3.17f, 1.72f, -2.62f),
            new Vector3(0.08f, 0.86f, 0.98f), walnutDark, false);
        CreateBox(parent, "Reception_VisitorNotice_Surface", new Vector3(-3.115f, 1.72f, -2.62f),
            new Vector3(0.025f, 0.76f, 0.88f), plaster, false);
        for (int i = 0; i < 4; i++)
        {
            CreateBox(parent, "Reception_Notice_Line_" + i,
                new Vector3(-3.145f, 1.94f - i * 0.15f, -2.62f),
                new Vector3(0.012f, 0.018f, 0.62f - i * 0.05f), walnut, false);
        }

        // Low discreet power outlets and a wall air-return grille.
        CreateBox(parent, "Reception_Wall_Outlet", new Vector3(-3.11f, 0.34f, -3.0f),
            new Vector3(0.025f, 0.12f, 0.18f), ceramic, false);
        for (int i = 0; i < 7; i++)
        {
            CreateBox(parent, "Reception_AirGrille_Slat_" + i, new Vector3(-3.12f, 2.72f, -4.25f + i * 0.07f),
                new Vector3(0.025f, 0.018f, 0.045f), charcoal, false);
        }

        // A couple of small floor indicators and queue studs; they stay out of the central clear path.
        CreateBox(parent, "Reception_Queue_Inlay_West", new Vector3(-14.12f, 0.026f, -5.15f),
            new Vector3(0.06f, 0.018f, 1.10f), brass, false);
        CreateBox(parent, "Reception_Queue_Inlay_East", new Vector3(-4.05f, 0.026f, -5.15f),
            new Vector3(0.06f, 0.018f, 1.10f), brass, false);
    }

    private static void CreateWallClock(Transform parent, string name, Vector3 pos, Quaternion rotation)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        group.rotation = rotation;
        CreateLocalCylinder(group, "Clock_Body", Vector3.zero, new Vector3(0.43f, 0.055f, 0.43f), walnutDark, false, Quaternion.Euler(90f, 0f, 0f));
        CreateLocalCylinder(group, "Clock_Face", new Vector3(0f, 0f, -0.045f), new Vector3(0.36f, 0.018f, 0.36f), paper, false, Quaternion.Euler(90f, 0f, 0f));
        CreateLocalCylinder(group, "Clock_Center", new Vector3(0f, 0f, -0.065f), new Vector3(0.035f, 0.012f, 0.035f), brass, false, Quaternion.Euler(90f, 0f, 0f));
        GameObject hour = CreateLocalBox(group, "Clock_HourHand", new Vector3(-0.045f, 0f, -0.075f),
            new Vector3(0.11f, 0.012f, 0.025f), charcoal, false);
        hour.transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
        GameObject minute = CreateLocalBox(group, "Clock_MinuteHand", new Vector3(0.045f, 0f, -0.078f),
            new Vector3(0.20f, 0.010f, 0.018f), charcoal, false);
        minute.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            CreateLocalBox(group, "Clock_Tick_" + i,
                new Vector3(Mathf.Cos(a) * 0.29f, 0f, Mathf.Sin(a) * 0.29f - 0.07f),
                new Vector3(0.025f, 0.01f, 0.012f), brass, false);
        }
    }

    private static void BuildFireExtinguisher(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        CreateLocalCylinder(group, "Extinguisher_Body", new Vector3(0f, 0.45f, 0f),
            new Vector3(0.13f, 0.42f, 0.13f), redPaint, true, Quaternion.identity);
        CreateLocalCylinder(group, "Extinguisher_Neck", new Vector3(0f, 0.88f, 0f),
            new Vector3(0.07f, 0.05f, 0.07f), charcoal, false, Quaternion.identity);
        CreateLocalBox(group, "Extinguisher_Handle", new Vector3(0.07f, 0.97f, 0f),
            new Vector3(0.18f, 0.04f, 0.06f), charcoal, false);
        CreateLocalCylinder(group, "Extinguisher_Hose", new Vector3(-0.13f, 0.75f, 0.02f),
            new Vector3(0.018f, 0.22f, 0.018f), charcoal, false, Quaternion.Euler(0f, 0f, 18f));
        CreateLocalBox(group, "Extinguisher_Label", new Vector3(0f, 0.51f, -0.132f),
            new Vector3(0.12f, 0.23f, 0.009f), paper, false);
    }

    private static void BuildWasteBin(Transform parent, string name, Vector3 pos)
    {
        Transform group = CreateGroup(name, parent);
        group.position = pos;
        CreateLocalCylinder(group, "Bin_Body", new Vector3(0f, 0.24f, 0f),
            new Vector3(0.19f, 0.24f, 0.19f), charcoal, true, Quaternion.identity);
        CreateLocalCylinder(group, "Bin_Rim", new Vector3(0f, 0.49f, 0f),
            new Vector3(0.20f, 0.025f, 0.20f), brass, false, Quaternion.identity);
        CreateLocalCylinder(group, "Bin_Opening", new Vector3(0f, 0.505f, 0f),
            new Vector3(0.13f, 0.008f, 0.13f), walnutDark, false, Quaternion.identity);
    }

    // ---------------------------------------------------------------------
    // CEILING AND LIGHTING
    // ---------------------------------------------------------------------

    private static void BuildReceptionCeiling(Transform parent)
    {
        // This independent ceiling remains visible while the original building ceiling groups are hidden.
        CreateBox(parent, "Reception_Ceiling_Main", new Vector3(-9.0f, 3.115f, -6.0f),
            new Vector3(11.55f, 0.075f, 7.55f), plaster, false);

        // Suspended ceiling grid: subtle seams, not a dense object-heavy grid.
        for (int i = 0; i < 10; i++)
        {
            float x = -14.65f + i * 1.18f;
            CreateBox(parent, "Reception_Ceiling_Grid_X_" + i, new Vector3(x, 3.065f, -6.0f),
                new Vector3(0.018f, 0.014f, 7.42f), ceramic, false);
        }
        for (int i = 0; i < 7; i++)
        {
            float z = -9.68f + i * 1.22f;
            CreateBox(parent, "Reception_Ceiling_Grid_Z_" + i, new Vector3(-9.0f, 3.065f, z),
                new Vector3(11.40f, 0.014f, 0.018f), ceramic, false);
        }

        // Perimeter cornice and recessed air-conditioning slot.
        CreateBox(parent, "Reception_Ceiling_Cornice_South", new Vector3(-9.0f, 3.00f, -9.78f),
            new Vector3(11.55f, 0.13f, 0.08f), walnutDark, false);
        CreateBox(parent, "Reception_Ceiling_Cornice_North", new Vector3(-9.0f, 3.00f, -2.22f),
            new Vector3(11.55f, 0.13f, 0.08f), walnutDark, false);
        CreateBox(parent, "Reception_Ceiling_Cornice_West", new Vector3(-14.78f, 3.00f, -6.0f),
            new Vector3(0.08f, 0.13f, 7.55f), walnutDark, false);
        CreateBox(parent, "Reception_Ceiling_Cornice_East", new Vector3(-3.22f, 3.00f, -6.0f),
            new Vector3(0.08f, 0.13f, 7.55f), walnutDark, false);

        for (int i = 0; i < 3; i++)
        {
            float x = -12.2f + i * 3.2f;
            CreateBox(parent, "Reception_Ceiling_LightLens_" + i, new Vector3(x, 3.055f, -6.0f),
                new Vector3(1.45f, 0.025f, 0.34f), warmEmission, false);
        }
    }

    private static void BuildReceptionLighting(Transform parent)
    {
        CreateLight(parent, "Reception_FillLight_West", new Vector3(-12.0f, 2.78f, -6.0f),
            new Color(1.0f, 0.82f, 0.64f), 0.85f, 5.2f);
        CreateLight(parent, "Reception_FillLight_East", new Vector3(-5.0f, 2.72f, -6.0f),
            new Color(0.82f, 0.90f, 1.0f), 0.55f, 4.6f);
        CreateLight(parent, "Reception_CounterAccent", new Vector3(-9.0f, 2.55f, -4.9f),
            new Color(1.0f, 0.75f, 0.48f), 0.42f, 3.2f);
    }

    private static void CreateLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    // ---------------------------------------------------------------------
    // GENERIC PRIMITIVE HELPERS
    // ---------------------------------------------------------------------

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material material, bool withCollider)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, pos, size, material, withCollider);
    }

    private static GameObject CreatePrimitive(
        Transform parent, string name, PrimitiveType type, Vector3 worldPos, Vector3 worldScale, Material material, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.localScale = worldScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static GameObject CreateLocalBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material material, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static GameObject CreateLocalCylinder(
        Transform parent, string name, Vector3 localPos, Vector3 size, Material material, bool withCollider, Quaternion rotation)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.transform.localRotation = rotation;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static void CreateWorldText(
        Transform parent, string name, string value, Vector3 pos, Quaternion rotation, float characterSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = rotation;

        TextMesh text = go.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = 72;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.richText = false;
        text.lineSpacing = 1.0f;
    }
}
#endif
