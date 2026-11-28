#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Safe, idempotent Editor workflow for Gardien Furtif.
/// Menu: Tools -> Gardien Furtif -> Mission
/// Generates native Sprite PNGs, Loot Prefabs, MissionUI Prefab, and wires the Office_Heist scene.
/// </summary>
public static class MissionSetupEditor
{
    private const string MenuRoot = "Tools/Gardien Furtif/Mission/";
    private const string IconsFolder = "Assets/Sprites/Mission/Icons";
    private const string LootPrefabsFolder = "Assets/Prefabs/Mission/LootPrefabs";
    private const string UIPrefabsFolder = "Assets/Prefabs/Mission/UI";

    [MenuItem(MenuRoot + "1. Generate Native Sprites & Prefabs", priority = 5)]
    public static void GenerateNativeSpritesAndPrefabs()
    {
        EnsureFolderExists(IconsFolder);
        EnsureFolderExists(LootPrefabsFolder);
        EnsureFolderExists(UIPrefabsFolder);

        MissionIconFactory.IconKind[] kinds =
        {
            MissionIconFactory.IconKind.GuidePortrait,
            MissionIconFactory.IconKind.Dossier,
            MissionIconFactory.IconKind.ExitDoor,
            MissionIconFactory.IconKind.Checkmark,
            MissionIconFactory.IconKind.Lock,
            MissionIconFactory.IconKind.LootBag,
            MissionIconFactory.IconKind.CashBundle,
            MissionIconFactory.IconKind.Laptop,
            MissionIconFactory.IconKind.Painting,
            MissionIconFactory.IconKind.Briefcase,
            MissionIconFactory.IconKind.JewelryBox,
            MissionIconFactory.IconKind.GoldObject,
            MissionIconFactory.IconKind.ValuableDocuments,
            MissionIconFactory.IconKind.StarFilled,
            MissionIconFactory.IconKind.StarEmpty
        };

        foreach (MissionIconFactory.IconKind kind in kinds)
        {
            string assetPath = $"{IconsFolder}/Icon_{kind}.png";
            if (!File.Exists(assetPath))
            {
                Texture2D tex = MissionIconFactory.CreateIconTexture(kind, 128);
                File.WriteAllBytes(assetPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }

        AssetDatabase.Refresh();

        foreach (MissionIconFactory.IconKind kind in kinds)
        {
            string assetPath = $"{IconsFolder}/Icon_{kind}.png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        CreateLootPrefabAsset("Loot_CashBundle", LootCollectible.LootPresetType.CashBundle, "Cash Bundle", "Collect the cash bundle", 250, new Color(0.25f, 0.78f, 0.35f, 1f), new Vector3(0.35f, 0.14f, 0.22f));
        CreateLootPrefabAsset("Loot_OfficeLaptop", LootCollectible.LootPresetType.Laptop, "Office Laptop", "Collect the office laptop", 450, new Color(0.25f, 0.62f, 0.92f, 1f), new Vector3(0.42f, 0.26f, 0.32f));
        CreateLootPrefabAsset("Loot_ValuablePainting", LootCollectible.LootPresetType.ValuablePainting, "Valuable Painting", "Collect the valuable painting", 600, new Color(0.92f, 0.68f, 0.18f, 1f), new Vector3(0.65f, 0.50f, 0.08f));
        CreateLootPrefabAsset("Loot_Briefcase", LootCollectible.LootPresetType.Briefcase, "Executive Briefcase", "Collect the executive briefcase", 500, new Color(0.75f, 0.48f, 0.20f, 1f), new Vector3(0.45f, 0.34f, 0.14f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Gardien Furtif] Native PNG Sprites and Loot Prefabs generated under Assets/Sprites/Mission/Icons and Assets/Prefabs/Mission.");
    }

    [MenuItem(MenuRoot + "2. Build / Update Mission UI", priority = 10)]
    public static void BuildOrUpdateMissionSystem()
    {
        GenerateNativeSpritesAndPrefabs();

        MissionManager missionManager = Object.FindFirstObjectByType<MissionManager>();
        if (missionManager == null)
        {
            GameObject mmObj = GameObject.Find("MissionSystem");
            if (mmObj == null)
            {
                mmObj = new GameObject("MissionSystem");
                Undo.RegisterCreatedObjectUndo(mmObj, "Create MissionSystem");
            }
            missionManager = Undo.AddComponent<MissionManager>(mmObj);
        }

        Undo.RecordObject(missionManager, "Update MissionManager");

        LootManager lootManager = missionManager.GetComponent<LootManager>();
        if (lootManager == null)
            lootManager = Object.FindFirstObjectByType<LootManager>();
        if (lootManager == null)
            lootManager = Undo.AddComponent<LootManager>(missionManager.gameObject);

        missionManager.lootManager = lootManager;

        if (missionManager.player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerObj = pm.gameObject;
            }
            missionManager.player = playerObj;
        }

        MissionFilePickup filePickup = Object.FindFirstObjectByType<MissionFilePickup>();
        GameObject fileSpawnPoint = GameObject.Find("Mission_FileSpawnPoint");
        if (filePickup == null && fileSpawnPoint != null)
        {
            filePickup = fileSpawnPoint.GetComponentInChildren<MissionFilePickup>(true);
            if (filePickup == null)
            {
                SphereCollider col = fileSpawnPoint.GetComponent<SphereCollider>();
                if (col == null)
                {
                    col = Undo.AddComponent<SphereCollider>(fileSpawnPoint);
                    col.isTrigger = true;
                    col.radius = 1.4f;
                }
                filePickup = Undo.AddComponent<MissionFilePickup>(fileSpawnPoint);
            }
        }
        if (filePickup != null)
        {
            missionManager.filePickup = filePickup;
            filePickup.EnsureWorldMarker();
            EditorUtility.SetDirty(filePickup);
        }

        MissionExitZone exitZone = Object.FindFirstObjectByType<MissionExitZone>();
        GameObject exitPoint = GameObject.Find("Mission_ExitPoint");
        if (exitZone == null && exitPoint != null)
        {
            exitZone = exitPoint.GetComponentInChildren<MissionExitZone>(true);
            if (exitZone == null)
            {
                BoxCollider box = exitPoint.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = Undo.AddComponent<BoxCollider>(exitPoint);
                    box.isTrigger = true;
                    box.size = new Vector3(2.2f, 2.5f, 2.2f);
                }
                exitZone = Undo.AddComponent<MissionExitZone>(exitPoint);
            }
        }
        if (exitZone != null)
        {
            missionManager.exitZone = exitZone;
            missionManager.exitObjectiveMarker = exitZone.EnsureExitMarker();
            EditorUtility.SetDirty(exitZone);
        }

        GameObject uiCanvas = MissionManager.BuildOrUpdateMissionUICanvas(missionManager);
        Undo.RegisterCreatedObjectUndo(uiCanvas, "Build or Update MissionUI");

        EnsureFolderExists(UIPrefabsFolder);
        PrefabUtility.SaveAsPrefabAsset(uiCanvas, $"{UIPrefabsFolder}/MissionUI.prefab");

        LootCollectible[] existingLoot = Object.FindObjectsByType<LootCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existingLoot.Length == 0)
            CreateOrUpdateSampleLootSpawnPoints();

        lootManager.DiscoverSceneLoot();
        EditorUtility.SetDirty(missionManager);
        EditorUtility.SetDirty(lootManager);
        EditorUtility.SetDirty(uiCanvas);

        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = missionManager.gameObject;
        Debug.Log($"[Gardien Furtif] Mission UI & Objective System built/updated successfully. Registered Loot Items: {lootManager.TotalLootCount}.");
    }

    [MenuItem(MenuRoot + "3. Create / Update Sample Loot Spawn Points", priority = 20)]
    public static void CreateOrUpdateSampleLootSpawnPoints()
    {
        GameObject gameplayMarkers = GameObject.Find("GameplayMarkers");
        if (gameplayMarkers == null)
        {
            gameplayMarkers = new GameObject("GameplayMarkers");
            Undo.RegisterCreatedObjectUndo(gameplayMarkers, "Create GameplayMarkers");
        }

        Transform lootRootTransform = gameplayMarkers.transform.Find("LootSpawnPoints");
        GameObject lootRoot;
        if (lootRootTransform == null)
        {
            lootRoot = new GameObject("LootSpawnPoints");
            Undo.RegisterCreatedObjectUndo(lootRoot, "Create LootSpawnPoints");
            lootRoot.transform.SetParent(gameplayMarkers.transform, false);
        }
        else
        {
            lootRoot = lootRootTransform.gameObject;
        }

        Vector3 basePos = Vector3.zero;
        GameObject filePoint = GameObject.Find("Mission_FileSpawnPoint");
        if (filePoint != null)
            basePos = filePoint.transform.position;

        CreateOrUpdateSampleLootItem(
            lootRoot.transform,
            "LootPoint_01_CashBundle",
            basePos + new Vector3(-2.5f, 0f, 1.5f),
            "loot_cash_01",
            LootCollectible.LootPresetType.CashBundle,
            "Cash Bundle",
            "Collect the cash bundle",
            250,
            new Color(0.25f, 0.78f, 0.35f, 1f),
            new Vector3(0.35f, 0.14f, 0.22f));

        CreateOrUpdateSampleLootItem(
            lootRoot.transform,
            "LootPoint_02_Laptop",
            basePos + new Vector3(2.5f, 0f, 1.5f),
            "loot_laptop_01",
            LootCollectible.LootPresetType.Laptop,
            "Office Laptop",
            "Collect the office laptop",
            450,
            new Color(0.25f, 0.62f, 0.92f, 1f),
            new Vector3(0.42f, 0.26f, 0.32f));

        CreateOrUpdateSampleLootItem(
            lootRoot.transform,
            "LootPoint_03_ValuablePainting",
            basePos + new Vector3(0f, 0f, -2.5f),
            "loot_painting_01",
            LootCollectible.LootPresetType.ValuablePainting,
            "Valuable Painting",
            "Collect the valuable painting",
            600,
            new Color(0.92f, 0.68f, 0.18f, 1f),
            new Vector3(0.65f, 0.50f, 0.08f));

        LootManager lm = Object.FindFirstObjectByType<LootManager>();
        if (lm != null)
        {
            lm.DiscoverSceneLoot();
            EditorUtility.SetDirty(lm);
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = lootRoot;
        Debug.Log("[Gardien Furtif] Sample LootSpawnPoints created/updated under GameplayMarkers/LootSpawnPoints.");
    }

    [MenuItem(MenuRoot + "4. Validate Mission Setup", priority = 30)]
    public static void ValidateMissionSetup()
    {
        MissionManager mm = Object.FindFirstObjectByType<MissionManager>();
        MissionFilePickup fp = Object.FindFirstObjectByType<MissionFilePickup>();
        MissionExitZone ez = Object.FindFirstObjectByType<MissionExitZone>();
        LootManager lm = Object.FindFirstObjectByType<LootManager>();

        if (mm == null) Debug.LogWarning("[Validate] Missing MissionManager. Run '2. Build / Update Mission UI'.");
        if (fp == null) Debug.LogWarning("[Validate] Missing MissionFilePickup on Mission_FileSpawnPoint.");
        if (ez == null) Debug.LogWarning("[Validate] Missing MissionExitZone on Mission_ExitPoint.");
        if (lm != null) lm.DiscoverSceneLoot();

        int lootCount = lm != null ? lm.TotalLootCount : 0;
        Debug.Log($"[Validate] MissionManager={(mm != null ? "OK" : "MISSING")}, DossierPickup={(fp != null ? "OK" : "MISSING")}, ExitZone={(ez != null ? "OK" : "MISSING")}, RegisteredLoot={lootCount}.");
    }

    private static void CreateLootPrefabAsset(
        string prefabName,
        LootCollectible.LootPresetType preset,
        string displayName,
        string objectiveText,
        int value,
        Color color,
        Vector3 meshScale)
    {
        string prefabPath = $"{LootPrefabsFolder}/{prefabName}.prefab";
        GameObject temp = new GameObject(prefabName);
        SphereCollider trigger = temp.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.35f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "LootVisual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(temp.transform, false);
        visual.transform.localPosition = new Vector3(0f, meshScale.y * 0.5f + 0.1f, 0f);
        visual.transform.localScale = meshScale;

        LootCollectible collectible = temp.AddComponent<LootCollectible>();
        Sprite iconSpr = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsFolder}/Icon_{LootCollectible.GetFallbackIconKind(preset)}.png");
        collectible.ConfigurePreset(prefabName.ToLowerInvariant(), preset, displayName, objectiveText, value, false, iconSpr);

        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);
    }

    private static void CreateOrUpdateSampleLootItem(
        Transform parent,
        string objectName,
        Vector3 defaultWorldPos,
        string id,
        LootCollectible.LootPresetType preset,
        string displayName,
        string objectiveText,
        int value,
        Color placeholderColor,
        Vector3 meshScale)
    {
        Transform existing = parent.Find(objectName);
        GameObject itemObj;
        if (existing != null)
        {
            itemObj = existing.gameObject;
        }
        else
        {
            itemObj = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(itemObj, $"Create {objectName}");
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.position = defaultWorldPos;
        }

        SphereCollider trigger = itemObj.GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = Undo.AddComponent<SphereCollider>(itemObj);
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
        }

        Transform visualTransform = itemObj.transform.Find("LootVisual");
        if (visualTransform == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "LootVisual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(itemObj.transform, false);
            visual.transform.localPosition = new Vector3(0f, meshScale.y * 0.5f + 0.1f, 0f);
            visual.transform.localScale = meshScale;

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Material mat = new Material(rend.sharedMaterial) { color = placeholderColor };
                rend.sharedMaterial = mat;
            }
        }

        LootCollectible collectible = itemObj.GetComponent<LootCollectible>();
        if (collectible == null)
            collectible = Undo.AddComponent<LootCollectible>(itemObj);

        Sprite iconSpr = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsFolder}/Icon_{LootCollectible.GetFallbackIconKind(preset)}.png");
        collectible.ConfigurePreset(id, preset, displayName, objectiveText, value, isRequired: false, assignedIcon: iconSpr);
        EditorUtility.SetDirty(collectible);
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        string[] parts = assetFolderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
