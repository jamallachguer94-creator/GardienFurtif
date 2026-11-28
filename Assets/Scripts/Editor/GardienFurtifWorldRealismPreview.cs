#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Reversible realism pass for the full Gardien Furtif world:
/// - the main generated office,
/// - OfficeCompoundExpansion_Generated,
/// - outdoor-only traffic, pedestrians and animals.
/// Existing door materials are restored from the original builder assets and are never
/// included in the general material remapping pass.
/// </summary>
public static class GardienFurtifWorldRealismPreview
{
    private const string PreviewRootName = "WorldRealism_PREVIEW";
    private const string MainHeistRoot = "OfficeHeist_Generated";
    private const string LegacyOfficeRoot = "OfficeEnvironment_Generated";
    private const string CompoundRootName = "OfficeCompoundExpansion_Generated";
    private const string MaterialFolder = "Assets/Materials/GardienFurtifWorldPreview";
    private const string TextureFolder = MaterialFolder + "/Textures";

    private static readonly Dictionary<string, Material> MaterialMap = new Dictionary<string, Material>();

    private static Material plaster;
    private static Material paleStone;
    private static Material slate;
    private static Material terrazzo;
    private static Material walnut;
    private static Material lightOak;
    private static Material brushedMetal;
    private static Material darkMetal;
    private static Material fabric;
    private static Material roof;
    private static Material asphalt;
    private static Material paving;
    private static Material grass;
    private static Material foliage;
    private static Material glass;
    private static Material signDark;
    private static Material signLight;
    private static Material warmEmission;
    private static Material carPaintBlue;
    private static Material carPaintSilver;
    private static Material carPaintRed;
    private static Material tyre;
    private static Material skinWarm;
    private static Material skinDeep;
    private static Material dogFur;
    private static Material catFur;

    private enum TextureStyle { Plaster, Stone, Concrete, Wood, Metal, Fabric, Asphalt, Roof, Grass, Flat }

    private sealed class Annex
    {
        public string name;
        public Vector2 center;
        public float width;
        public float depth;
        public string side;
        public string displayName;
    }

    private static readonly Annex[] Annexes =
    {
        new Annex { name = "01 - Records Annex", center = new Vector2(-43f, 35f), width = 12f, depth = 9f, side = "South", displayName = "RECORDS" },
        new Annex { name = "02 - Training Block", center = new Vector2(-17f, 35f), width = 12f, depth = 9f, side = "South", displayName = "TRAINING" },
        new Annex { name = "03 - Operations Annex", center = new Vector2(17f, 35f), width = 12f, depth = 9f, side = "South", displayName = "OPERATIONS" },
        new Annex { name = "04 - Research Block", center = new Vector2(43f, 35f), width = 12f, depth = 9f, side = "South", displayName = "RESEARCH" },
        new Annex { name = "05 - Workshop", center = new Vector2(-43f, 8f), width = 12f, depth = 10f, side = "East", displayName = "WORKSHOP" },
        new Annex { name = "06 - Storage Building", center = new Vector2(-43f, -9f), width = 12f, depth = 10f, side = "East", displayName = "STORAGE" },
        new Annex { name = "07 - Staff Building", center = new Vector2(43f, 8f), width = 12f, depth = 10f, side = "West", displayName = "STAFF" },
        new Annex { name = "08 - Utility Building", center = new Vector2(43f, -9f), width = 12f, depth = 10f, side = "West", displayName = "UTILITY" },
        new Annex { name = "09 - Maintenance Block", center = new Vector2(-17f, -35f), width = 12f, depth = 9f, side = "North", displayName = "MAINTENANCE" },
        new Annex { name = "10 - Logistics Block", center = new Vector2(17f, -35f), width = 12f, depth = 9f, side = "North", displayName = "LOGISTICS" },
        new Annex { name = "11 - West Gatehouse", center = new Vector2(-10.5f, -44f), width = 6.5f, depth = 5.5f, side = "North", displayName = "WEST GATE" },
        new Annex { name = "12 - East Gatehouse", center = new Vector2(10.5f, -44f), width = 6.5f, depth = 5.5f, side = "North", displayName = "EAST GATE" }
    };

    // ---------------------------------------------------------------------
    // MENU
    // ---------------------------------------------------------------------

    [MenuItem("Tools/Gardien Furtif/World Realism/1. Restore Original Door Colors", false, 10)]
    public static void RestoreOriginalDoorColors()
    {
        int restored = RestoreDoorMaterialsInScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[WorldRealism] Restored original door materials on " + restored + " renderers.");
        EditorUtility.DisplayDialog(
            "Original Door Colors Restored",
            "Restored the original door/door-frame materials wherever the original builder material assets were available. Save the scene with Ctrl+S.",
            "OK");
    }

    [MenuItem("Tools/Gardien Furtif/World Realism/2. Build Whole World Realism Preview", false, 20)]
    public static void BuildPreview()
    {
        GameObject mainRoot = FindMainOfficeRoot();
        GameObject compoundRoot = GameObject.Find(CompoundRootName);

        if (mainRoot == null && compoundRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Generated Environment Not Found",
                "Open the Office_Heist scene and make sure the generated main office and/or OfficeCompoundExpansion_Generated exists. Build the original environment first if necessary.",
                "OK");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Whole World Realism Preview");

        RemovePreviewInternal(mainRoot, compoundRoot);
        int restoredDoors = RestoreDoorMaterials(mainRoot, compoundRoot);
        EnsureMaterials();

        GameObject preview = new GameObject(PreviewRootName);
        Undo.RegisterCreatedObjectUndo(preview, "Create World Realism Preview");

        Transform architecture = Group("01_MainOffice_Architecture_Details", preview.transform);
        Transform interior = Group("02_MainOffice_Interior_Signs_And_Art", preview.transform);
        Transform compound = Group("03_Compound_Facades_And_Signs", preview.transform);
        Transform street = Group("04_Street_Props_And_Landscaping", preview.transform);
        Transform vehicles = Group("05_Moving_Outdoor_Vehicles", preview.transform);
        Transform pedestrians = Group("06_Outdoor_Pedestrians", preview.transform);
        Transform animals = Group("07_Outdoor_Animals", preview.transform);

        if (mainRoot != null)
        {
            BuildMainOfficeFacade(architecture, mainRoot.name == MainHeistRoot);
            BuildMainOfficeInteriorDetails(interior, mainRoot.name == MainHeistRoot);
            ApplyRealisticMaterials(mainRoot.transform);
        }

        if (compoundRoot != null)
        {
            BuildCompoundFacadeDetails(compound, compoundRoot.transform);
            BuildCompoundStreetProps(street);
            BuildMovingVehicles(vehicles);
            BuildPedestrians(pedestrians);
            BuildAnimals(animals);
            ApplyRealisticMaterials(compoundRoot.transform);
        }

        Selection.activeGameObject = preview;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "[WorldRealism] World realism preview generated. Door-material renderers restored: " + restoredDoors +
            ". The interior gameplay roots, Player, Mission System and Guard AI were not intentionally modified.");

        EditorUtility.DisplayDialog(
            "Whole World Realism Preview Built",
            "Created a reversible world preview. Door materials were restored from the original builder assets where available. " +
            "The main office receives architectural details and room signs; the outdoor compound receives building signs, street props, moving cars, pedestrians and animals. " +
            "The new ambient actors have no colliders and stay outside the main office. Save the scene with Ctrl+S.",
            "OK");
    }

    [MenuItem("Tools/Gardien Furtif/World Realism/3. Remove Preview & Restore Materials", false, 30)]
    public static void RemovePreview()
    {
        GameObject mainRoot = FindMainOfficeRoot();
        GameObject compoundRoot = GameObject.Find(CompoundRootName);
        if (mainRoot == null && compoundRoot == null)
        {
            EditorUtility.DisplayDialog("World Realism", "No generated main office or compound root was found.", "OK");
            return;
        }

        RemovePreviewInternal(mainRoot, compoundRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[WorldRealism] Preview removed and original shared materials restored.");
        EditorUtility.DisplayDialog(
            "World Preview Removed",
            "Removed the preview objects and restored the materials captured before the preview. The original door-color correction is intentionally kept. Save the scene with Ctrl+S.",
            "OK");
    }

    private static GameObject FindMainOfficeRoot()
    {
        GameObject root = GameObject.Find(MainHeistRoot);
        if (root != null) return root;
        return GameObject.Find(LegacyOfficeRoot);
    }

    private static void RemovePreviewInternal(GameObject mainRoot, GameObject compoundRoot)
    {
        RestoreMaterialSnapshots(mainRoot != null ? mainRoot.transform : null);
        RestoreMaterialSnapshots(compoundRoot != null ? compoundRoot.transform : null);

        GameObject preview = GameObject.Find(PreviewRootName);
        if (preview != null) Undo.DestroyObjectImmediate(preview);
    }

    private static void RestoreMaterialSnapshots(Transform root)
    {
        if (root == null) return;
        WorldRealismMaterialBackup[] backups = root.GetComponentsInChildren<WorldRealismMaterialBackup>(true);
        for (int i = 0; i < backups.Length; i++)
        {
            WorldRealismMaterialBackup backup = backups[i];
            if (backup == null) continue;
            backup.Restore();
            Undo.DestroyObjectImmediate(backup);
        }
    }

    // ---------------------------------------------------------------------
    // DOOR RESTORATION
    // ---------------------------------------------------------------------

    private static int RestoreDoorMaterialsInScene()
    {
        return RestoreDoorMaterials(FindMainOfficeRoot(), GameObject.Find(CompoundRootName));
    }

    private static int RestoreDoorMaterials(GameObject mainRoot, GameObject compoundRoot)
    {
        int count = 0;
        if (mainRoot != null && mainRoot.name == MainHeistRoot)
        {
            Material originalLeaf = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Generated/OfficeHeist/Materials/Door_DarkWalnut.mat");

            Transform architecture = mainRoot.transform.Find("Building_Architecture");
            if (architecture != null)
            {
                string[] floorPaths =
                {
                    "GroundFloor/DoorsAndFrames",
                    "UpperFloor/DoorsAndFrames"
                };

                for (int p = 0; p < floorPaths.Length; p++)
                {
                    Transform doors = architecture.Find(floorPaths[p]);
                    if (doors == null) continue;
                    Renderer[] renderers = doors.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] == null || originalLeaf == null) continue;
                        renderers[i].sharedMaterial = originalLeaf;
                        EditorUtility.SetDirty(renderers[i]);
                        count++;
                    }
                }
            }
        }
        else if (mainRoot != null && mainRoot.name == LegacyOfficeRoot)
        {
            Material originalTrim = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedOffice/Office_Trim.mat");
            Material originalThreshold = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedOffice/Office_Wood_Light.mat");

            Renderer[] renderers = mainRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.name.ToLowerInvariant().Contains("door")) continue;

                bool threshold = renderer.name.ToLowerInvariant().Contains("threshold");
                Material target = threshold ? originalThreshold : originalTrim;
                if (target == null) continue;
                renderer.sharedMaterial = target;
                EditorUtility.SetDirty(renderer);
                count++;
            }
        }

        if (compoundRoot != null)
        {
            Material originalExpansionFrame = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedOfficeExpansion/Expansion_Window_Frame.mat");
            Material originalExpansionWall = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedOfficeExpansion/Expansion_Wall_Light.mat");
            Material originalExpansionDoor = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedOfficeExpansion/Expansion_Door.mat");
            Material originalCompoundDoor = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GeneratedCompound/Compound_Door.mat");

            Renderer[] renderers = compoundRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                string name = renderer.name.ToLowerInvariant();
                if (!name.Contains("door")) continue;

                Material target = null;
                if (name.Contains("post") || name.Contains("header") || name.Contains("frame"))
                    target = originalExpansionFrame;
                else if (name.Contains("leaf") || name.Contains("panel"))
                    target = originalExpansionDoor != null ? originalExpansionDoor : originalCompoundDoor;
                else if (name.Contains("fill"))
                    target = originalExpansionWall;

                if (target == null) continue;
                renderer.sharedMaterial = target;
                EditorUtility.SetDirty(renderer);
                count++;
            }
        }

        return count;
    }

    private static bool IsDoorRenderer(Renderer renderer)
    {
        if (renderer == null) return true;
        string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : string.Empty;
        if (materialName.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        Transform current = renderer.transform;
        while (current != null)
        {
            string n = current.name;
            if (n.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("doorsandframes", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }
        return false;
    }

    // ---------------------------------------------------------------------
    // PROCEDURAL MATERIALS
    // ---------------------------------------------------------------------

    private static void EnsureMaterials()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "GardienFurtifWorldPreview");
        EnsureFolder(MaterialFolder, "Textures");

        plaster = CreateMaterial("World_Warm_Plaster", new Color(0.72f, 0.68f, 0.59f), TextureStyle.Plaster, 0.14f, 0f);
        paleStone = CreateMaterial("World_Pale_Limestone", new Color(0.70f, 0.67f, 0.59f), TextureStyle.Stone, 0.24f, 0f);
        slate = CreateMaterial("World_Slate_Accent", new Color(0.25f, 0.29f, 0.29f), TextureStyle.Stone, 0.24f, 0.05f);
        terrazzo = CreateMaterial("World_Terrazzo_Floor", new Color(0.58f, 0.56f, 0.50f), TextureStyle.Concrete, 0.25f, 0f);
        walnut = CreateMaterial("World_Walnut", new Color(0.28f, 0.16f, 0.09f), TextureStyle.Wood, 0.31f, 0f);
        lightOak = CreateMaterial("World_Light_Oak", new Color(0.55f, 0.40f, 0.25f), TextureStyle.Wood, 0.28f, 0f);
        brushedMetal = CreateMaterial("World_Brushed_Metal", new Color(0.42f, 0.45f, 0.44f), TextureStyle.Metal, 0.58f, 0.46f);
        darkMetal = CreateMaterial("World_Dark_Metal", new Color(0.10f, 0.12f, 0.12f), TextureStyle.Metal, 0.34f, 0.35f);
        fabric = CreateMaterial("World_Fabric", new Color(0.34f, 0.35f, 0.32f), TextureStyle.Fabric, 0.16f, 0f);
        roof = CreateMaterial("World_Terracotta_Roof", new Color(0.45f, 0.23f, 0.16f), TextureStyle.Roof, 0.20f, 0f);
        asphalt = CreateMaterial("World_Asphalt", new Color(0.18f, 0.19f, 0.19f), TextureStyle.Asphalt, 0.12f, 0f);
        paving = CreateMaterial("World_Paving", new Color(0.55f, 0.52f, 0.45f), TextureStyle.Stone, 0.16f, 0f);
        grass = CreateMaterial("World_Grass", new Color(0.24f, 0.31f, 0.19f), TextureStyle.Grass, 0.08f, 0f);
        foliage = CreateMaterial("World_Foliage", new Color(0.15f, 0.26f, 0.14f), TextureStyle.Grass, 0.10f, 0f);
        glass = CreateMaterial("World_Tinted_Glass", new Color(0.22f, 0.34f, 0.37f), TextureStyle.Flat, 0.72f, 0.12f);
        signDark = CreateMaterial("World_Sign_Dark", new Color(0.09f, 0.15f, 0.14f), TextureStyle.Flat, 0.18f, 0.12f);
        signLight = CreateMaterial("World_Sign_Light", new Color(0.76f, 0.69f, 0.53f), TextureStyle.Flat, 0.25f, 0f);
        warmEmission = CreateEmission("World_Warm_Sign_Emission", new Color(1f, 0.72f, 0.40f) * 1.8f);
        carPaintBlue = CreateMaterial("World_Car_Paint_Blue", new Color(0.035f, 0.12f, 0.21f), TextureStyle.Flat, 0.70f, 0.52f);
        carPaintSilver = CreateMaterial("World_Car_Paint_Silver", new Color(0.48f, 0.52f, 0.53f), TextureStyle.Flat, 0.70f, 0.60f);
        carPaintRed = CreateMaterial("World_Car_Paint_Red", new Color(0.36f, 0.045f, 0.035f), TextureStyle.Flat, 0.70f, 0.45f);
        tyre = CreateMaterial("World_Tyre_Rubber", new Color(0.035f, 0.038f, 0.04f), TextureStyle.Flat, 0.08f, 0f);
        skinWarm = CreateMaterial("World_Skin_Warm", new Color(0.66f, 0.43f, 0.30f), TextureStyle.Flat, 0.18f, 0f);
        skinDeep = CreateMaterial("World_Skin_Deep", new Color(0.34f, 0.20f, 0.15f), TextureStyle.Flat, 0.18f, 0f);
        dogFur = CreateMaterial("World_Dog_Fur", new Color(0.45f, 0.30f, 0.18f), TextureStyle.Flat, 0.14f, 0f);
        catFur = CreateMaterial("World_Cat_Fur", new Color(0.36f, 0.38f, 0.36f), TextureStyle.Flat, 0.18f, 0f);

        MaterialMap.Clear();
        Map(new[] { "Ext_Plaster_WarmSandstone", "Ext_Plaster_CreamBeige", "Int_Wall_WarmIvory", "Int_Wall_NeutralGrey",
            "Office_Walls", "Office_Wall_Accent", "Expansion_Wall", "Expansion_Wall_Light",
            "GF_Office_Warm_Plaster", "GF_Office_Sage_Accent", "GF_Compound_Plaster_Base", "GF_Compound_Light_Stone" }, plaster);
        Map(new[] { "Floor_PolishedConcrete", "Floor_ReceptionStoneTile", "Office_Floor", "GF_Office_Stone_Floor",
            "Expansion_Concrete", "Expansion_Interior_Floor", "GF_Compound_Concrete", "GF_Compound_Interior_Terrazzo" }, paleStone);
        Map(new[] { "Floor_CorridorSlate", "Office_Corridor", "GF_Office_Corridor_Tile", "Expansion_Wall_Accent",
            "GF_Compound_Slate_Accent", "Roof_ModernSlate" }, slate);
        Map(new[] { "Floor_ExecutiveHardwood", "Furniture_WalnutWood", "Furniture_WarmOak", "Office_Wood",
            "Office_Wood_Light", "GF_Office_Walnut", "GF_Office_Oak", "Expansion_Wood", "GF_Compound_Natural_Wood" }, walnut);
        Map(new[] { "Metal_BrushedSteel", "Metal_CharcoalFrame", "Office_Metal", "Office_Dark_Metal",
            "GF_Office_Brushed_Metal", "GF_Office_Dark_Metal", "Expansion_Metal", "Expansion_Dark_Metal",
            "GF_Compound_Painted_Metal", "GF_Compound_Dark_Metal", "GF_Compound_Window_Frame" }, brushedMetal);
        Map(new[] { "Furniture_FabricMutedGrey", "Furniture_FabricWarmTaupe", "Furniture_LeatherCharcoal",
            "Office_Fabric", "GF_Office_Deep_Charcoal_Fabric" }, fabric);
        Map(new[] { "Ext_AsphaltRoad", "Expansion_Road", "GF_Compound_Asphalt" }, asphalt);
        Map(new[] { "Ext_SidewalkConcrete", "Ext_CourtyardStonePaving", "Expansion_Path", "Expansion_Ground",
            "GF_Compound_Paving", "GF_Compound_Dry_Earth" }, paving);
        Map(new[] { "Ext_GrassOlive", "Ext_FoliageNaturalGreen", "Ext_TreeBarkBrown", "Office_Plant",
            "GF_Office_Plant", "Expansion_Grass", "Expansion_Tree", "Expansion_Bush",
            "GF_Compound_Olive_Grass", "GF_Compound_Tree_Foliage", "GF_Compound_Bush_Foliage" }, grass);
        Map(new[] { "Roof_ModernSlate", "Expansion_Roof", "GF_Compound_Terracotta_Roof" }, roof);
        Map(new[] { "Glass_TintedBlueGrey", "Office_Glass_Blue", "GF_Office_Glass",
            "Expansion_Glass", "GF_Compound_Tinted_Glass" }, glass);
        Map(new[] { "Expansion_Sign", "GF_Compound_Sign_Dark_Green" }, signDark);
        Map(new[] { "Expansion_Road_Marking", "GF_Compound_Road_Paint" }, paleStone);
        Map(new[] { "Ceiling_AcousticTile" }, plaster);
    }

    private static void Map(string[] names, Material material)
    {
        for (int i = 0; i < names.Length; i++) MaterialMap[names[i]] = material;
    }

    private static Material CreateMaterial(string name, Color tint, TextureStyle style, float smoothness, float metallic)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = ResolveLitShader();
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        SetColor(material, "_BaseColor", Color.white);
        SetColor(material, "_Color", Color.white);
        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_Glossiness", smoothness);

        Texture2D albedo = LoadTexture(path, name + "_Albedo");
        if (albedo == null)
        {
            albedo = GenerateTexture(name + "_Albedo", tint, style, false);
            AssetDatabase.AddObjectToAsset(albedo, material);
        }
        Texture2D normal = LoadTexture(path, name + "_Normal");
        if (normal == null)
        {
            normal = GenerateTexture(name + "_Normal", tint, style, true);
            AssetDatabase.AddObjectToAsset(normal, material);
        }

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            SetFloat(material, "_BumpScale", 0.32f);
        }

        if (albedo != null) { albedo.wrapMode = TextureWrapMode.Repeat; albedo.filterMode = FilterMode.Trilinear; albedo.anisoLevel = 8; }
        if (normal != null) { normal.wrapMode = TextureWrapMode.Repeat; normal.filterMode = FilterMode.Trilinear; normal.anisoLevel = 8; }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateEmission(string name, Color emission)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(ResolveLitShader()) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        SetColor(material, "_BaseColor", Color.white);
        SetColor(material, "_Color", Color.white);
        material.EnableKeyword("_EMISSION");
        SetColor(material, "_EmissionColor", emission);
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
        return Shader.Find("Sprites/Default");
    }

    private static Texture2D LoadTexture(string path, string name)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Texture2D texture = assets[i] as Texture2D;
            if (texture != null && texture.name == name) return texture;
        }
        return null;
    }

    private static Texture2D GenerateTexture(string name, Color tint, TextureStyle style, bool normalMap)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, normalMap)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                if (normalMap)
                {
                    float l = Height(u - 1f / size, v, style);
                    float r = Height(u + 1f / size, v, style);
                    float d = Height(u, v - 1f / size, style);
                    float up = Height(u, v + 1f / size, style);
                    Vector3 n = new Vector3((l - r) * 1.8f, (d - up) * 1.8f, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1f);
                }
                else
                {
                    float h = Height(u, v, style);
                    float noise = Mathf.Clamp01(0.45f + h * 0.34f);
                    Color c = Color.Lerp(tint * 0.78f, tint * 1.14f, noise);
                    if (style == TextureStyle.Wood)
                    {
                        float grain = Mathf.Sin((u * 42f + Mathf.PerlinNoise(u * 6f, v * 2f) * 2f) * 6.283f) * 0.05f;
                        c *= 1f + grain;
                    }
                    if (style == TextureStyle.Asphalt || style == TextureStyle.Grass)
                    {
                        float speckle = Mathf.PerlinNoise(u * 100f, v * 100f) * 0.12f;
                        c *= 0.90f + speckle;
                    }
                    pixels[y * size + x] = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private static float Height(float u, float v, TextureStyle style)
    {
        float broad = Mathf.PerlinNoise(u * 4.7f + 2.1f, v * 5.3f + 1.4f);
        float medium = Mathf.PerlinNoise(u * 18f + 3.3f, v * 19f + 4.1f);
        float fine = Mathf.PerlinNoise(u * 72f + 5.2f, v * 68f + 2.6f);
        switch (style)
        {
            case TextureStyle.Plaster: return broad * .55f + medium * .34f + fine * .11f;
            case TextureStyle.Stone: return broad * .30f + medium * .45f + fine * .25f;
            case TextureStyle.Concrete: return broad * .18f + medium * .35f + fine * .47f;
            case TextureStyle.Wood: return broad * .10f + Mathf.PerlinNoise(u * 12f, v * 2f) * .72f + fine * .18f;
            case TextureStyle.Metal: return broad * .05f + Mathf.PerlinNoise(u * 160f, v * 5f) * .95f;
            case TextureStyle.Fabric: return broad * .25f + (Mathf.Sin(u * 480f) + Mathf.Sin(v * 480f)) * .02f + fine * .35f;
            case TextureStyle.Asphalt: return broad * .15f + medium * .35f + fine * .50f;
            case TextureStyle.Roof: return broad * .45f + medium * .45f + fine * .10f;
            case TextureStyle.Grass: return broad * .18f + medium * .36f + fine * .46f;
            default: return .5f;
        }
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, value);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (AssetDatabase.IsValidFolder(path)) return;
        if (!AssetDatabase.IsValidFolder(parent))
        {
            string grand = System.IO.Path.GetDirectoryName(parent).Replace('\\', '/');
            string parentName = System.IO.Path.GetFileName(parent);
            EnsureFolder(grand, parentName);
        }
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void ApplyRealisticMaterials(Transform root)
    {
        if (root == null) return;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsDoorRenderer(renderer) || renderer.sharedMaterial == null) continue;

            Material replacement;
            if (!MaterialMap.TryGetValue(renderer.sharedMaterial.name.Replace(" (Instance)", ""), out replacement))
                continue;

            WorldRealismMaterialBackup backup = renderer.GetComponent<WorldRealismMaterialBackup>();
            if (backup == null)
            {
                backup = Undo.AddComponent<WorldRealismMaterialBackup>(renderer.gameObject);
                backup.Capture(renderer);
            }

            renderer.sharedMaterial = replacement;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void BuildMainOfficeFacade(Transform parent, bool modernHeist)
    {
        if (modernHeist)
        {
            // The main entrance is on the south facade at X=-9, Z=-10.
            CreateBox(parent, "MainOffice_IdentitySign_Back", new Vector3(-9f, 2.48f, -10.22f),
                new Vector3(3.55f, 0.40f, 0.13f), signDark, false);
            CreateText(parent, "MainOffice_IdentitySign_Text", "GARDIEN FURTIF",
                new Vector3(-9f, 2.48f, -10.305f), Quaternion.Euler(0f, 180f, 0f), 0.0042f, signLight.color);

            // Slim stone reveals and brass-like bands on solid wall portions only.
            float[] pilasters = { -11.1f, -7.0f, -0.8f, 7.4f, 13.8f };
            for (int i = 0; i < pilasters.Length; i++)
            {
                CreateBox(parent, "MainFacade_Pilaster_" + i,
                    new Vector3(pilasters[i], 1.60f, -10.205f), new Vector3(0.10f, 3.02f, 0.035f), paleStone, false);
                CreateBox(parent, "MainFacade_Band_" + i,
                    new Vector3(pilasters[i], 0.38f, -10.232f), new Vector3(0.24f, 0.045f, 0.045f), brushedMetal, false);
            }

            // Low-profile sills under the south facade windows; these do not close openings.
            float[] windowCenters = { -13f, -5.2f, 2.5f, 11.5f };
            float[] windowWidths = { 2f, 2f, 3f, 2.6f };
            for (int i = 0; i < windowCenters.Length; i++)
                CreateBox(parent, "MainFacade_WindowSill_" + i,
                    new Vector3(windowCenters[i], 0.91f, -10.205f), new Vector3(windowWidths[i] + 0.12f, 0.055f, 0.15f), paleStone, false);
        }
        else
        {
            // Legacy OfficeEnvironment_Generated is a 30 x 22 m single-storey block.
            CreateBox(parent, "LegacyOffice_IdentitySign_Back", new Vector3(-10f, 2.58f, -11.18f),
                new Vector3(3.6f, 0.42f, 0.12f), signDark, false);
            CreateText(parent, "LegacyOffice_IdentitySign_Text", "GARDIEN FURTIF",
                new Vector3(-10f, 2.58f, -11.26f), Quaternion.Euler(0f, 180f, 0f), 0.0041f, signLight.color);
        }
    }

    private static void BuildMainOfficeInteriorDetails(Transform parent, bool modernHeist)
    {
        if (!modernHeist) return;

        // Ground-floor room plaques mounted above the doorway openings on the corridor side.
        AddRoomPlaque(parent, "GF_Sign_Security", "SECURITY", new Vector3(-10.5f, 2.58f, 1.82f), 0f);
        AddRoomPlaque(parent, "GF_Sign_Archive", "ARCHIVE", new Vector3(-2f, 2.58f, 1.82f), 0f);
        AddRoomPlaque(parent, "GF_Sign_Service", "SERVICE", new Vector3(5.5f, 2.58f, 1.82f), 0f);
        AddRoomPlaque(parent, "GF_Sign_Stairwell", "STAIRWELL", new Vector3(12f, 2.58f, 1.82f), 0f);
        AddRoomPlaque(parent, "GF_Sign_Reception", "RECEPTION", new Vector3(-9f, 2.58f, -1.82f), 180f);
        AddRoomPlaque(parent, "GF_Sign_Workspace", "WORKSPACE", new Vector3(2.5f, 2.58f, -1.82f), 180f);
        AddRoomPlaque(parent, "GF_Sign_Meeting", "MEETING ROOM", new Vector3(11.5f, 2.58f, -1.82f), 180f);

        // Upper-floor plaques.
        AddRoomPlaque(parent, "UF_Sign_Director", "DIRECTOR", new Vector3(-9f, 5.98f, 1.82f), 0f);
        AddRoomPlaque(parent, "UF_Sign_Archive", "DOCUMENT ARCHIVE", new Vector3(3f, 5.98f, 1.82f), 0f);
        AddRoomPlaque(parent, "UF_Sign_Server", "SERVER ROOM", new Vector3(-10f, 5.98f, -1.82f), 180f);
        AddRoomPlaque(parent, "UF_Sign_Office", "OFFICES", new Vector3(0.5f, 5.98f, -1.82f), 180f);

        // A few framed pictures on blank, safe wall spans. All renderers are decorative and collider-free.
        AddWallArt(parent, "Security_WallArt", new Vector3(-7.35f, 1.78f, 9.82f), Quaternion.Euler(0f, 180f, 0f));
        AddWallArt(parent, "Meeting_WallArt", new Vector3(14.78f, 1.82f, -6.4f), Quaternion.Euler(0f, -90f, 0f));
        AddWallArt(parent, "Director_WallArt", new Vector3(-4.35f, 5.18f, 9.82f), Quaternion.Euler(0f, 180f, 0f));
        AddWallArt(parent, "Server_WallArt", new Vector3(-14.78f, 5.18f, -8.5f), Quaternion.Euler(0f, 90f, 0f));
    }

    private static void AddRoomPlaque(Transform parent, string name, string label, Vector3 position, float yaw)
    {
        Transform group = Group(name, parent);
        group.position = position;
        group.rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateLocalBox(group, "Plaque_Body", Vector3.zero, new Vector3(1.55f, 0.24f, 0.045f), signDark, false);
        CreateLocalBox(group, "Plaque_TopLine", new Vector3(0f, 0.095f, -0.026f),
            new Vector3(1.35f, 0.012f, 0.012f), brushedMetal, false);
        CreateText(group, "Plaque_Text", label, new Vector3(0f, -0.01f, -0.028f),
            Quaternion.identity, 0.0017f, signLight.color, true);
    }

    private static void AddWallArt(Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        Transform group = Group(name, parent);
        group.position = position;
        group.rotation = rotation;
        CreateLocalBox(group, "Art_Frame", Vector3.zero, new Vector3(1.25f, 0.85f, 0.075f), walnut, false);
        CreateLocalBox(group, "Art_Mat", new Vector3(0f, 0f, -0.041f), new Vector3(1.12f, 0.72f, 0.02f), paleStone, false);
        CreateLocalBox(group, "Art_Panel", new Vector3(0f, 0f, -0.054f), new Vector3(0.99f, 0.59f, 0.012f), slate, false);
        CreateLocalBox(group, "Art_Horizon", new Vector3(0f, -0.08f, -0.063f), new Vector3(0.80f, 0.018f, 0.008f), signLight, false);
        CreateLocalBox(group, "Art_Sun", new Vector3(0.23f, 0.12f, -0.066f), new Vector3(0.17f, 0.17f, 0.012f), warmEmission, false);
    }

    private static void BuildCompoundFacadeDetails(Transform parent, Transform compoundRoot)
    {
        Transform buildings = compoundRoot.Find("10 Additional Low-Rise Buildings");
        if (buildings == null) return;

        for (int i = 0; i < Annexes.Length; i++)
        {
            Annex annex = Annexes[i];
            Transform building = buildings.Find(annex.name);
            if (building == null) continue;

            Vector3 signPosition;
            Quaternion facing;
            if (annex.side == "South")
            {
                signPosition = new Vector3(annex.center.x, 2.83f, annex.center.y - annex.depth * 0.5f - 0.16f);
                facing = Quaternion.Euler(0f, 180f, 0f);
            }
            else if (annex.side == "North")
            {
                signPosition = new Vector3(annex.center.x, 2.83f, annex.center.y + annex.depth * 0.5f + 0.16f);
                facing = Quaternion.identity;
            }
            else if (annex.side == "East")
            {
                signPosition = new Vector3(annex.center.x + annex.width * 0.5f + 0.16f, 2.83f, annex.center.y);
                facing = Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                signPosition = new Vector3(annex.center.x - annex.width * 0.5f - 0.16f, 2.83f, annex.center.y);
                facing = Quaternion.Euler(0f, -90f, 0f);
            }

            Transform signGroup = Group("RealisticSign_" + (i + 1).ToString("00"), parent);
            signGroup.position = signPosition;
            signGroup.rotation = facing;
            CreateLocalBox(signGroup, "Sign_Back", Vector3.zero, new Vector3(2.65f, 0.54f, 0.10f), signDark, false);
            CreateLocalBox(signGroup, "Sign_TopTrim", new Vector3(0f, 0.245f, -0.056f),
                new Vector3(2.45f, 0.018f, 0.018f), brushedMetal, false);
            CreateText(signGroup, "Sign_Title", annex.displayName,
                new Vector3(0f, 0.04f, -0.059f), Quaternion.identity, 0.0022f, signLight.color, true);
            CreateText(signGroup, "Sign_Code", "GF / " + (i + 1).ToString("00"),
                new Vector3(0f, -0.15f, -0.059f), Quaternion.identity, 0.0013f, signLight.color, true);

            // A small awning lip and downlight above the entrance, with no collider.
            Vector3 awningPosition = signPosition + (annex.side == "South" ? Vector3.forward * 0.18f :
                annex.side == "North" ? Vector3.back * 0.18f :
                annex.side == "East" ? Vector3.left * 0.18f : Vector3.right * 0.18f);
            Quaternion awningRotation = facing;
            GameObject awning = CreateBox(parent, annex.name + "_Entry_Awning", awningPosition + Vector3.up * 0.38f,
                new Vector3(2.1f, 0.07f, 0.65f), darkMetal, false);
            awning.transform.rotation = awningRotation;
            GameObject lightGo = new GameObject(annex.name + "_Entry_Downlight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = awningPosition + Vector3.up * 0.30f;
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.intensity = 0.24f;
            light.color = new Color(1f, 0.80f, 0.58f);
            light.shadows = LightShadows.None;
        }

        // Exterior louvers on the smaller annexes add depth to the blank facade panels.
        for (int i = 0; i < Annexes.Length; i++)
        {
            Annex a = Annexes[i];
            if (a.width < 8f) continue;
            for (int j = 0; j < 5; j++)
            {
                float x = a.center.x - 1.2f + j * 0.6f;
                float z = a.center.y - a.depth * 0.5f - 0.14f;
                CreateBox(parent, "Annex_" + (i + 1) + "_South_Louver_" + j,
                    new Vector3(x, 1.42f, z), new Vector3(0.08f, 0.92f, 0.09f), walnut, false);
            }
        }
    }

    private static void BuildCompoundStreetProps(Transform parent)
    {
        // Wayfinding totems outside the main office and at the cross-road junctions.
        AddDirectionalTotem(parent, "Main_Office_Wayfinding", new Vector3(-13.2f, 0f, -16.4f),
            new[] { "MAIN OFFICE", "VISITOR PARKING", "SERVICE ACCESS" });
        AddDirectionalTotem(parent, "North_Compound_Wayfinding", new Vector3(0f, 0f, 24.5f),
            new[] { "OPERATIONS", "RESEARCH", "RECORDS" });
        AddDirectionalTotem(parent, "South_Compound_Wayfinding", new Vector3(0f, 0f, -26.0f),
            new[] { "LOGISTICS", "MAINTENANCE", "MAIN GATE" });

        // Benches, bollards, planters and litter bins in peripheral courtyards only.
        Vector3[] benches = { new Vector3(-30f, 0f, 36f), new Vector3(30f, 0f, 36f), new Vector3(-30f, 0f, -35f), new Vector3(30f, 0f, -35f) };
        for (int i = 0; i < benches.Length; i++) BuildOutdoorBench(parent, "Compound_Bench_" + i, benches[i]);

        Vector3[] bins = { new Vector3(-25.8f, 0f, 32.8f), new Vector3(34.2f, 0f, 32.8f), new Vector3(-25.8f, 0f, -31.8f), new Vector3(34.2f, 0f, -31.8f) };
        for (int i = 0; i < bins.Length; i++) BuildOutdoorBin(parent, "Compound_Bin_" + i, bins[i]);

        // Low planted borders; they stay within courtyards and avoid the road lanes.
        Vector3[] planters = { new Vector3(-34.5f, 0f, 36f), new Vector3(34.5f, 0f, 36f), new Vector3(-34.5f, 0f, -35f), new Vector3(34.5f, 0f, -35f) };
        for (int i = 0; i < planters.Length; i++) BuildPlanter(parent, "Compound_Planter_" + i, planters[i]);
    }

    private static void AddDirectionalTotem(Transform parent, string name, Vector3 pos, string[] labels)
    {
        Transform group = Group(name, parent);
        group.position = pos;
        CreateLocalBox(group, "Totem_Base", new Vector3(0f, 0.18f, 0f), new Vector3(0.56f, 0.36f, 0.56f), darkMetal, false);
        CreateLocalBox(group, "Totem_Post", new Vector3(0f, 1.50f, 0f), new Vector3(0.15f, 2.65f, 0.15f), brushedMetal, false);
        CreateLocalBox(group, "Totem_Board", new Vector3(0f, 2.28f, -0.06f), new Vector3(1.40f, 1.10f, 0.16f), signDark, false);
        for (int i = 0; i < labels.Length; i++)
            CreateText(group, "Totem_Label_" + i, labels[i],
                new Vector3(0f, 2.58f - i * 0.30f, -0.15f), Quaternion.identity, 0.0015f, signLight.color, true);
    }

    private static void BuildOutdoorBench(Transform parent, string name, Vector3 pos)
    {
        Transform group = Group(name, parent);
        group.position = pos;
        CreateLocalBox(group, "Bench_Seat", new Vector3(0f, 0.47f, 0f), new Vector3(1.7f, 0.10f, 0.52f), walnut, false);
        CreateLocalBox(group, "Bench_Back", new Vector3(0f, 0.82f, 0.23f), new Vector3(1.7f, 0.54f, 0.10f), walnut, false);
        CreateLocalBox(group, "Bench_Support_Left", new Vector3(-0.66f, 0.24f, 0f), new Vector3(0.12f, 0.48f, 0.45f), darkMetal, false);
        CreateLocalBox(group, "Bench_Support_Right", new Vector3(0.66f, 0.24f, 0f), new Vector3(0.12f, 0.48f, 0.45f), darkMetal, false);
    }

    private static void BuildOutdoorBin(Transform parent, string name, Vector3 pos)
    {
        Transform group = Group(name, parent);
        group.position = pos;
        CreateLocalCylinder(group, "Bin_Body", new Vector3(0f, 0.42f, 0f), new Vector3(0.27f, 0.42f, 0.27f), darkMetal, false);
        CreateLocalCylinder(group, "Bin_Lid", new Vector3(0f, 0.86f, 0f), new Vector3(0.29f, 0.035f, 0.29f), brushedMetal, false);
    }

    private static void BuildPlanter(Transform parent, string name, Vector3 pos)
    {
        Transform group = Group(name, parent);
        group.position = pos;
        CreateLocalCylinder(group, "Planter_Pot", new Vector3(0f, 0.27f, 0f), new Vector3(0.48f, 0.27f, 0.48f), paleStone, false);
        CreateLocalSphere(group, "Planter_Foliage", new Vector3(0f, 0.85f, 0f), new Vector3(0.62f, 0.95f, 0.62f), foliage);
    }

    // ---------------------------------------------------------------------
    // OUTDOOR VEHICLES
    // ---------------------------------------------------------------------

    private static void BuildMovingVehicles(Transform parent)
    {
        Vector3[][] routes =
        {
            new[] { new Vector3(-50f,0.06f,18.5f), new Vector3(50f,0.06f,18.5f), new Vector3(50f,0.06f,21.5f), new Vector3(-50f,0.06f,21.5f) },
            new[] { new Vector3(50f,0.06f,-18.5f), new Vector3(-50f,0.06f,-18.5f), new Vector3(-50f,0.06f,-21.5f), new Vector3(50f,0.06f,-21.5f) },
            new[] { new Vector3(-33.5f,0.06f,-38f), new Vector3(-33.5f,0.06f,38f), new Vector3(-30.5f,0.06f,38f), new Vector3(-30.5f,0.06f,-38f) },
            new[] { new Vector3(30.5f,0.06f,-38f), new Vector3(30.5f,0.06f,38f), new Vector3(33.5f,0.06f,38f), new Vector3(33.5f,0.06f,-38f) }
        };

        Material[] paints = { carPaintBlue, carPaintSilver, carPaintRed, carPaintBlue };
        float[] speeds = { 5.4f, 4.7f, 4.2f, 4.9f };
        for (int i = 0; i < routes.Length; i++)
            BuildCar(parent, "Ambient_Car_" + (i + 1), routes[i], paints[i], speeds[i]);
    }

    private static void BuildCar(Transform parent, string name, Vector3[] route, Material paint, float speed)
    {
        Transform car = Group(name, parent);
        car.position = route[0];
        Transform visual = Group("Vehicle_Visuals", car);
        CreateLocalBox(visual, "Car_Body_Lower", new Vector3(0f, 0.47f, 0f), new Vector3(1.88f, 0.55f, 4.0f), paint, false);
        CreateLocalBox(visual, "Car_Hood", new Vector3(0f, 0.72f, -1.35f), new Vector3(1.78f, 0.20f, 1.20f), paint, false);
        CreateLocalBox(visual, "Car_Trunk", new Vector3(0f, 0.70f, 1.48f), new Vector3(1.78f, 0.20f, 0.95f), paint, false);
        CreateLocalBox(visual, "Car_Cabin", new Vector3(0f, 1.05f, 0.18f), new Vector3(1.48f, 0.70f, 2.08f), glass, false);
        CreateLocalBox(visual, "Car_Roof", new Vector3(0f, 1.43f, 0.25f), new Vector3(1.38f, 0.12f, 1.52f), paint, false);
        CreateLocalBox(visual, "Car_Front_Bumper", new Vector3(0f, 0.36f, -2.02f), new Vector3(1.82f, 0.16f, 0.12f), darkMetal, false);
        CreateLocalBox(visual, "Car_Rear_Bumper", new Vector3(0f, 0.36f, 2.02f), new Vector3(1.82f, 0.16f, 0.12f), darkMetal, false);
        CreateLocalBox(visual, "Car_Front_Grille", new Vector3(0f, 0.56f, -2.055f), new Vector3(0.62f, 0.18f, 0.025f), darkMetal, false);
        CreateLocalBox(visual, "Car_Headlight_Left", new Vector3(-0.62f, 0.68f, -2.07f), new Vector3(0.32f, 0.14f, 0.035f), signLight, false);
        CreateLocalBox(visual, "Car_Headlight_Right", new Vector3(0.62f, 0.68f, -2.07f), new Vector3(0.32f, 0.14f, 0.035f), signLight, false);
        CreateLocalBox(visual, "Car_Tail_Light_Left", new Vector3(-0.66f, 0.68f, 2.05f), new Vector3(0.26f, 0.13f, 0.035f), carPaintRed, false);
        CreateLocalBox(visual, "Car_Tail_Light_Right", new Vector3(0.66f, 0.68f, 2.05f), new Vector3(0.26f, 0.13f, 0.035f), carPaintRed, false);
        CreateLocalBox(visual, "Car_WingMirror_Left", new Vector3(-0.83f, 1.03f, -0.48f), new Vector3(0.18f, 0.12f, 0.24f), darkMetal, false);
        CreateLocalBox(visual, "Car_WingMirror_Right", new Vector3(0.83f, 1.03f, -0.48f), new Vector3(0.18f, 0.12f, 0.24f), darkMetal, false);

        Transform[] wheels = new Transform[4];
        int wheelIndex = 0;
        for (int side = -1; side <= 1; side += 2)
        for (int axle = -1; axle <= 1; axle += 2)
        {
            Transform wheel = Group("Wheel_" + wheelIndex, visual);
            wheel.localPosition = new Vector3(side * 0.96f, 0.38f, axle * 1.27f);
            wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreateLocalCylinder(wheel, "Rubber", Vector3.zero, new Vector3(0.34f, 0.18f, 0.34f), tyre, false, Quaternion.identity);
            CreateLocalCylinder(wheel, "Rim", new Vector3(0f, 0f, 0f), new Vector3(0.16f, 0.185f, 0.16f), brushedMetal, false, Quaternion.identity);
            wheels[wheelIndex++] = wheel;
        }

        CompoundAmbientMover mover = car.gameObject.AddComponent<CompoundAmbientMover>();
        mover.Configure(route, speed, true, null, null, wheels, 0.33f);
    }

    // ---------------------------------------------------------------------
    // PEDESTRIANS
    // ---------------------------------------------------------------------

    private static void BuildPedestrians(Transform parent)
    {
        Vector3[][] routes =
        {
            new[] { new Vector3(-10.9f,0f,-21.0f), new Vector3(-9.1f,0f,-21.0f), new Vector3(-9.1f,0f,-12.4f), new Vector3(-10.9f,0f,-12.4f) },
            new[] { new Vector3(-32f,0f,34f), new Vector3(-28f,0f,34f), new Vector3(-28f,0f,38f), new Vector3(-32f,0f,38f) },
            new[] { new Vector3(28f,0f,34f), new Vector3(32f,0f,34f), new Vector3(32f,0f,38f), new Vector3(28f,0f,38f) },
            new[] { new Vector3(28f,0f,-35f), new Vector3(32f,0f,-35f), new Vector3(32f,0f,-31f), new Vector3(28f,0f,-31f) }
        };

        Material[] shirts = { carPaintBlue, fabric, carPaintRed, slate };
        Material[] trousers = { darkMetal, walnut, darkMetal, walnut };
        Material[] skins = { skinWarm, skinDeep, skinWarm, skinDeep };
        for (int i = 0; i < routes.Length; i++)
            BuildPerson(parent, "Ambient_Pedestrian_" + (i + 1), routes[i], shirts[i], trousers[i], skins[i], 0.95f + i * 0.08f);
    }

    private static void BuildPerson(Transform parent, string name, Vector3[] route, Material shirt, Material trousers, Material skin, float speed)
    {
        Transform actor = Group(name, parent);
        actor.position = route[0];
        Transform visual = Group("Person_Visuals", actor);

        CreateLocalSphere(visual, "Head", new Vector3(0f, 1.59f, 0f), new Vector3(0.22f, 0.27f, 0.22f), skin);
        CreateLocalCylinder(visual, "Neck", new Vector3(0f, 1.38f, 0f), new Vector3(0.08f, 0.10f, 0.08f), skin, false, Quaternion.identity);
        CreateLocalBox(visual, "Torso", new Vector3(0f, 1.03f, 0f), new Vector3(0.46f, 0.63f, 0.28f), shirt, false);
        CreateLocalBox(visual, "Pelvis", new Vector3(0f, 0.68f, 0f), new Vector3(0.40f, 0.20f, 0.25f), trousers, false);

        Transform leftLeg = Group("Left_Leg_Pivot", visual);
        leftLeg.localPosition = new Vector3(-0.13f, 0.63f, 0f);
        CreateLocalCapsule(leftLeg, "Leg", new Vector3(0f, -0.21f, 0f), new Vector3(0.12f, 0.24f, 0.12f), trousers, false);
        CreateLocalBox(leftLeg, "Shoe", new Vector3(0f, -0.42f, -0.04f), new Vector3(0.15f, 0.07f, 0.25f), darkMetal, false);

        Transform rightLeg = Group("Right_Leg_Pivot", visual);
        rightLeg.localPosition = new Vector3(0.13f, 0.63f, 0f);
        CreateLocalCapsule(rightLeg, "Leg", new Vector3(0f, -0.21f, 0f), new Vector3(0.12f, 0.24f, 0.12f), trousers, false);
        CreateLocalBox(rightLeg, "Shoe", new Vector3(0f, -0.42f, -0.04f), new Vector3(0.15f, 0.07f, 0.25f), darkMetal, false);

        Transform leftArm = Group("Left_Arm_Pivot", visual);
        leftArm.localPosition = new Vector3(-0.28f, 1.24f, 0f);
        CreateLocalCapsule(leftArm, "Arm", new Vector3(0f, -0.22f, 0f), new Vector3(0.095f, 0.22f, 0.095f), shirt, false);
        Transform rightArm = Group("Right_Arm_Pivot", visual);
        rightArm.localPosition = new Vector3(0.28f, 1.24f, 0f);
        CreateLocalCapsule(rightArm, "Arm", new Vector3(0f, -0.22f, 0f), new Vector3(0.095f, 0.22f, 0.095f), shirt, false);

        CompoundAmbientMover mover = actor.gameObject.AddComponent<CompoundAmbientMover>();
        mover.Configure(route, speed, true,
            new[] { leftLeg, rightLeg, leftArm, rightArm },
            new[] { 0f, Mathf.PI, Mathf.PI, 0f },
            null, 0.34f, 7.5f, 20f, 0.012f);
    }

    // ---------------------------------------------------------------------
    // ANIMALS
    // ---------------------------------------------------------------------

    private static void BuildAnimals(Transform parent)
    {
        Vector3[] dogRoute =
        {
            new Vector3(-28.7f,0f,-33.4f),
            new Vector3(-27.4f,0f,-33.4f),
            new Vector3(-27.4f,0f,-32.3f),
            new Vector3(-28.7f,0f,-32.3f)
        };
        BuildDog(parent, "Ambient_Dog_01", dogRoute);

        Vector3[] catRoute =
        {
            new Vector3(28.1f,0f,32.8f),
            new Vector3(29.2f,0f,32.8f),
            new Vector3(29.2f,0f,33.6f),
            new Vector3(28.1f,0f,33.6f)
        };
        BuildCat(parent, "Ambient_Cat_01", catRoute);
    }

    private static void BuildDog(Transform parent, string name, Vector3[] route)
    {
        Transform actor = Group(name, parent);
        actor.position = route[0];
        Transform visual = Group("Dog_Visuals", actor);

        CreateLocalBox(visual, "Body", new Vector3(0f, 0.54f, 0.05f), new Vector3(0.34f, 0.40f, 0.86f), dogFur, false);
        CreateLocalSphere(visual, "Chest", new Vector3(0f, 0.58f, -0.33f), new Vector3(0.34f, 0.40f, 0.36f), dogFur);
        CreateLocalSphere(visual, "Head", new Vector3(0f, 0.77f, -0.47f), new Vector3(0.30f, 0.31f, 0.31f), dogFur);
        CreateLocalBox(visual, "Muzzle", new Vector3(0f, 0.66f, -0.72f), new Vector3(0.22f, 0.14f, 0.24f), paleStone, false);
        CreateLocalBox(visual, "Ear_Left", new Vector3(-0.19f, 0.98f, -0.43f), new Vector3(0.10f, 0.22f, 0.12f), walnut, false);
        CreateLocalBox(visual, "Ear_Right", new Vector3(0.19f, 0.98f, -0.43f), new Vector3(0.10f, 0.22f, 0.12f), walnut, false);
        Transform tail = Group("Tail_Pivot", visual);
        tail.localPosition = new Vector3(0f, 0.68f, 0.44f);
        CreateLocalCapsule(tail, "Tail", new Vector3(0f, 0.12f, 0.18f), new Vector3(0.08f, 0.18f, 0.08f), dogFur, false);

        Transform[] legs = new Transform[4];
        int k = 0;
        for (int side = -1; side <= 1; side += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Transform leg = Group("Leg_Pivot_" + k, visual);
            leg.localPosition = new Vector3(side * 0.12f, 0.40f, z * 0.26f);
            CreateLocalCapsule(leg, "Leg", new Vector3(0f, -0.17f, 0f), new Vector3(0.09f, 0.19f, 0.09f), dogFur, false);
            legs[k++] = leg;
        }

        Transform[] gait = { legs[0], legs[3], legs[1], legs[2], tail };
        CompoundAmbientMover mover = actor.gameObject.AddComponent<CompoundAmbientMover>();
        mover.Configure(route, 1.15f, true, gait,
            new[] { 0f, Mathf.PI, Mathf.PI, 0f, 0.5f * Mathf.PI });
    }

    private static void BuildCat(Transform parent, string name, Vector3[] route)
    {
        Transform actor = Group(name, parent);
        actor.position = route[0];
        Transform visual = Group("Cat_Visuals", actor);

        CreateLocalSphere(visual, "Body", new Vector3(0f, 0.36f, 0.06f), new Vector3(0.24f, 0.28f, 0.60f), catFur);
        CreateLocalSphere(visual, "Head", new Vector3(0f, 0.48f, -0.31f), new Vector3(0.22f, 0.23f, 0.22f), catFur);
        CreateLocalSphere(visual, "Ear_Left", new Vector3(-0.13f, 0.66f, -0.36f), new Vector3(0.09f, 0.16f, 0.08f), catFur);
        CreateLocalSphere(visual, "Ear_Right", new Vector3(0.13f, 0.66f, -0.36f), new Vector3(0.09f, 0.16f, 0.08f), catFur);
        Transform tail = Group("Tail_Pivot", visual);
        tail.localPosition = new Vector3(0f, 0.44f, 0.34f);
        CreateLocalCapsule(tail, "Tail", new Vector3(0f, 0.14f, 0.13f), new Vector3(0.045f, 0.25f, 0.045f), catFur, false);

        Transform[] legs = new Transform[4];
        int k = 0;
        for (int side = -1; side <= 1; side += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Transform leg = Group("Leg_Pivot_" + k, visual);
            leg.localPosition = new Vector3(side * 0.11f, 0.27f, z * 0.18f);
            CreateLocalCapsule(leg, "Leg", new Vector3(0f, -0.10f, 0f), new Vector3(0.07f, 0.13f, 0.07f), catFur, false);
            legs[k++] = leg;
        }

        Transform[] gait = { legs[0], legs[3], legs[1], legs[2], tail };
        CompoundAmbientMover mover = actor.gameObject.AddComponent<CompoundAmbientMover>();
        mover.Configure(route, 0.70f, true, gait,
            new[] { 0f, Mathf.PI, Mathf.PI, 0f, 0.8f * Mathf.PI });
    }

    // ---------------------------------------------------------------------
    // LOW-LEVEL HELPERS
    // ---------------------------------------------------------------------

    private static Transform Group(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 worldPos, Vector3 size, Material material, bool withCollider)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, worldPos, size, material, withCollider);
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 worldPos,
        Vector3 worldScale, Material material, bool withCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = worldPos;
        obj.transform.localScale = worldScale;
        SetRenderer(obj, material, withCollider);
        return obj;
    }

    private static GameObject CreateLocalBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material material, bool withCollider)
    {
        return CreateLocalPrimitive(parent, name, PrimitiveType.Cube, localPos, size, material, withCollider, Quaternion.identity);
    }

    private static GameObject CreateLocalSphere(Transform parent, string name, Vector3 localPos, Vector3 size, Material material)
    {
        return CreateLocalPrimitive(parent, name, PrimitiveType.Sphere, localPos, size, material, false, Quaternion.identity);
    }

    private static GameObject CreateLocalCapsule(Transform parent, string name, Vector3 localPos, Vector3 size, Material material,
        bool withCollider, Quaternion? rotation = null)
    {
        return CreateLocalPrimitive(parent, name, PrimitiveType.Capsule, localPos, size, material, withCollider,
            rotation ?? Quaternion.identity);
    }

    private static GameObject CreateLocalCylinder(Transform parent, string name, Vector3 localPos, Vector3 size, Material material,
        bool withCollider, Quaternion rotation)
    {
        return CreateLocalPrimitive(parent, name, PrimitiveType.Cylinder, localPos, size, material, withCollider, rotation);
    }

    private static GameObject CreateLocalPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPos,
        Vector3 size, Material material, bool withCollider, Quaternion rotation)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = size;
        obj.transform.localRotation = rotation;
        SetRenderer(obj, material, withCollider);
        return obj;
    }

    private static void SetRenderer(GameObject obj, Material material, bool withCollider)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        if (!withCollider)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
    }

    private static void CreateText(Transform parent, string name, string value, Vector3 position,
        Quaternion rotation, float characterSize, Color color, bool localSpace = false)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        if (localSpace)
        {
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
        }
        else
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        TextMesh text = obj.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.richText = false;
    }
}
#endif
