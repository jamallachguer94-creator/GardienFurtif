#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reversible add-on for the existing Office_Heist scene.
/// Adds a basement hideout for the thief, extends the current stairwell,
/// creates functional doors, custom room dressing and collectible loot.
/// Menu: Tools > Gardien Furtif > Secret Basement.
/// </summary>
public static class OfficeHeistSecretBasementBuilder
{
    private const string OfficeRootName = "OfficeHeist_Generated";
    private const string PreviewRootName = "OfficeHeist_SecretBasement_PREVIEW";
    private const string ChangesName = "BasementStairOpening_Changes";
    private const string SlabBackupName = "GF_Structural_Slab__PreviewBackup";
    private const string CorridorBackupName = "GF_Finish_MainCorridor__PreviewBackup";
    private const string StairLandingBackupName = "Stair_Bottom_LandingPad__PreviewBackup";
    private const string MaterialFolder = "Assets/Generated/SecretBasement/Materials";

    private const float FloorY = -3.55f;
    private const float FloorThickness = 0.30f;
    private const float WallHeight = 3.05f;
    private const float WallCenterY = FloorY + WallHeight * 0.5f;
    private const float WallThickness = 0.24f;

    private static Material floorMat;
    private static Material floorAccentMat;
    private static Material wallMat;
    private static Material wallAccentMat;
    private static Material trimMat;
    private static Material metalMat;
    private static Material darkMetalMat;
    private static Material woodMat;
    private static Material darkWoodMat;
    private static Material fabricMat;
    private static Material screenMat;
    private static Material amberMat;
    private static Material tealMat;
    private static Material goldMat;
    private static Material paperMat;
    private static Material doorMat;

    [MenuItem("Tools/Gardien Furtif/Secret Basement/1. Build Preview", false, 10)]
    public static void BuildPreview()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!string.Equals(scene.name, "Office_Heist", System.StringComparison.OrdinalIgnoreCase))
        {
            bool continueHere = EditorUtility.DisplayDialog(
                "Secret Basement Preview",
                $"The active scene is '{scene.name}'. Open 'Office_Heist' for the intended preview.\n\nBuild here anyway?",
                "Build Here", "Cancel");
            if (!continueHere) return;
        }

        GameObject officeObject = GameObject.Find(OfficeRootName);
        if (officeObject == null)
        {
            EditorUtility.DisplayDialog(
                "Secret Basement Preview",
                "OfficeHeist_Generated was not found. Open Office_Heist and build the existing office environment first.",
                "OK");
            return;
        }

        Transform floors = officeObject.transform.Find("Building_Architecture/GroundFloor/Floors");
        if (floors == null || floors.Find("GF_Structural_Slab") == null &&
            floors.Find(SlabBackupName) == null)
        {
            EditorUtility.DisplayDialog(
                "Secret Basement Preview",
                "The expected GroundFloor/Floors hierarchy or structural slab was not found. No changes were made.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Build Secret Thief Basement",
                "This creates a reversible underground level and extends the existing stairwell downwards.\n\n" +
                "The original floor pieces are kept as disabled backups. Use 'Remove Preview & Restore Office' to undo the preview.",
                "Build Preview", "Cancel"))
            return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Secret Thief Basement Preview");

        RemovePreviewRoot(officeObject.transform);
        RestoreGroundFloorOpening(floors);
        SetOriginalStairLanding(officeObject.transform, true);
        EnsureMaterials();

        if (!CreateGroundFloorOpening(floors))
        {
            EditorUtility.DisplayDialog("Secret Basement Preview", "Could not create the stairwell opening. The original floor was restored.", "OK");
            return;
        }
        SetOriginalStairLanding(officeObject.transform, false);

        GameObject preview = new GameObject(PreviewRootName);
        Undo.RegisterCreatedObjectUndo(preview, "Create Secret Basement Preview");
        preview.transform.SetParent(officeObject.transform, false);

        Transform architecture = CreateGroup("Architecture", preview.transform);
        Transform stairs = CreateGroup("SecretStaircase", preview.transform);
        Transform rooms = CreateGroup("RoomsAndFurniture", preview.transform);
        Transform props = CreateGroup("PropsAndLoot", preview.transform);
        Transform lights = CreateGroup("Lighting", preview.transform);
        Transform tunnel = CreateGroup("EscapeTunnel", preview.transform);

        BuildBasementArchitecture(architecture);
        BuildStairExtension(stairs);
        BuildRoomsAndFurniture(rooms);
        BuildLootAndProps(props);
        BuildLighting(lights);
        BuildEscapeTunnel(tunnel);

        Selection.activeGameObject = preview;
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("[Secret Basement] Preview built. Doors use E while aimed at them; loot uses the existing Mission interaction system.");
    }

    [MenuItem("Tools/Gardien Furtif/Secret Basement/2. Remove Preview & Restore Office", false, 20)]
    public static void RemovePreviewAndRestoreOffice()
    {
        GameObject officeObject = GameObject.Find(OfficeRootName);
        if (officeObject == null)
        {
            EditorUtility.DisplayDialog("Secret Basement Preview", "OfficeHeist_Generated was not found in the active scene.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Remove Secret Basement Preview",
                "Delete the basement preview, restore the original floor and corridor finish, and remove its generated materials?",
                "Remove & Restore", "Cancel"))
            return;

        Transform floors = officeObject.transform.Find("Building_Architecture/GroundFloor/Floors");
        RemovePreviewRoot(officeObject.transform);
        if (floors != null) RestoreGroundFloorOpening(floors);
        SetOriginalStairLanding(officeObject.transform, true);

        AssetDatabase.DeleteAsset(MaterialFolder);
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Secret Basement] Preview removed; original ground-floor objects restored.");
    }

    private static void RemovePreviewRoot(Transform officeRoot)
    {
        Transform preview = officeRoot.Find(PreviewRootName);
        if (preview != null) Undo.DestroyObjectImmediate(preview.gameObject);
    }

    private static void SetOriginalStairLanding(Transform officeRoot, bool restore)
    {
        Transform stairRoot = officeRoot.Find("Building_Architecture/Staircase/Stair_Steps");
        if (stairRoot == null) return;

        Transform landing = stairRoot.Find(restore ? StairLandingBackupName : "Stair_Bottom_LandingPad");
        if (landing == null) return;

        if (restore)
        {
            landing.name = "Stair_Bottom_LandingPad";
            landing.gameObject.SetActive(true);
        }
        else
        {
            landing.name = StairLandingBackupName;
            landing.gameObject.SetActive(false);
        }
    }

    private static bool CreateGroundFloorOpening(Transform floors)
    {
        Transform slab = floors.Find("GF_Structural_Slab");
        if (slab != null)
        {
            slab.name = SlabBackupName;
            slab.gameObject.SetActive(false);
        }
        else if (floors.Find(SlabBackupName) == null)
        {
            Debug.LogError("[Secret Basement] GF_Structural_Slab was not found.");
            return false;
        }

        Transform corridor = floors.Find("GF_Finish_MainCorridor");
        if (corridor != null)
        {
            corridor.name = CorridorBackupName;
            corridor.gameObject.SetActive(false);
        }

        Transform changes = CreateGroup(ChangesName, floors);

        // A 2m x 6m opening at X 11..13 / Z -3.8..2.2, connected to the current stair landing.
        CreateBox(changes, "GF_Slab_Basement_West",
            new Vector3(-2f, -0.15f, 0f), new Vector3(26f, 0.30f, 20f), floorMat, true);
        CreateBox(changes, "GF_Slab_Basement_East",
            new Vector3(14f, -0.15f, 0f), new Vector3(2f, 0.30f, 20f), floorMat, true);
        CreateBox(changes, "GF_Slab_Basement_South",
            new Vector3(12f, -0.15f, -6.9f), new Vector3(2f, 0.30f, 6.2f), floorMat, true);
        CreateBox(changes, "GF_Slab_Basement_North",
            new Vector3(12f, -0.15f, 6.1f), new Vector3(2f, 0.30f, 7.8f), floorMat, true);

        Material corridorMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Generated/OfficeHeist/Materials/Floor_CorridorSlate.mat");
        if (corridorMat == null) corridorMat = floorAccentMat;

        if (floors.Find(CorridorBackupName) != null)
        {
            CreateBox(changes, "GF_CorridorFinish_West",
                new Vector3(-1.9f, 0.005f, 0f), new Vector3(25.8f, 0.01f, 3.9f), corridorMat, false);
            CreateBox(changes, "GF_CorridorFinish_East",
                new Vector3(13.9f, 0.005f, 0f), new Vector3(1.8f, 0.01f, 3.9f), corridorMat, false);
        }

        return true;
    }

    private static void RestoreGroundFloorOpening(Transform floors)
    {
        Transform changes = floors.Find(ChangesName);
        if (changes != null) Undo.DestroyObjectImmediate(changes.gameObject);

        Transform slabBackup = floors.Find(SlabBackupName);
        if (slabBackup != null)
        {
            slabBackup.name = "GF_Structural_Slab";
            slabBackup.gameObject.SetActive(true);
        }

        Transform corridorBackup = floors.Find(CorridorBackupName);
        if (corridorBackup != null)
        {
            corridorBackup.name = "GF_Finish_MainCorridor";
            corridorBackup.gameObject.SetActive(true);
        }
    }

    private static void BuildBasementArchitecture(Transform parent)
    {
        Transform floor = CreateGroup("BasementFloors", parent);
        Transform walls = CreateGroup("BasementWalls", parent);
        Transform details = CreateGroup("ArchitecturalDetails", parent);

        CreateBox(floor, "Basement_StructuralFloor",
            new Vector3(0f, FloorY - FloorThickness * 0.5f, 0f),
            new Vector3(30f, FloorThickness, 20f), floorMat, true);
        CreateBox(floor, "Basement_MainFloorFinish",
            new Vector3(0f, FloorY + 0.01f, 0f),
            new Vector3(29.7f, 0.02f, 19.7f), floorAccentMat, false);
        CreateBox(floor, "Basement_PlanningRoom_Inlay",
            new Vector3(0f, FloorY + 0.025f, -0.4f),
            new Vector3(13.8f, 0.018f, 10.4f), wallAccentMat, false);
        CreateBox(floor, "Basement_GearRoom_Inlay",
            new Vector3(-11.1f, FloorY + 0.026f, 0f),
            new Vector3(6.6f, 0.018f, 15.8f), floorMat, false);
        CreateBox(floor, "Basement_Vault_Inlay",
            new Vector3(11f, FloorY + 0.026f, 4.5f),
            new Vector3(6.4f, 0.018f, 7.5f), trimMat, false);

        CreateBox(walls, "Basement_Wall_South",
            new Vector3(0f, WallCenterY, -9.85f),
            new Vector3(30f, WallHeight, 0.30f), wallMat, true);
        CreateBox(walls, "Basement_Wall_North",
            new Vector3(0f, WallCenterY, 9.85f),
            new Vector3(30f, WallHeight, 0.30f), wallMat, true);
        CreateBox(walls, "Basement_Wall_West",
            new Vector3(-14.85f, WallCenterY, 0f),
            new Vector3(0.30f, WallHeight, 20f), wallMat, true);

        // East wall has a functional door into the concealed escape tunnel.
        BuildWallAlongX(walls, "Basement_EastWall", 14.85f, -9.7f, 9.7f, -1.5f, 2.0f, true);

        // Three interior doors divide equipment, planning and secure-loot zones.
        BuildWallAlongX(walls, "Basement_GearPartition", -7.35f, -8.9f, 8.9f, 0f, 1.75f, false);
        BuildWallAlongX(walls, "Basement_VaultPartition", 7.35f, -8.9f, 8.9f, -1f, 1.80f, false);
        BuildWallAlongZ(walls, "Basement_PlanningNorthPartition", 5.2f, -7.2f, 7.2f, 0f, 1.85f);

        // Door at the bottom of the new stair flight.
        BuildWallAlongZ(walls, "Basement_StairBulkhead", -4.48f, 10.85f, 13.15f, 12f, 1.65f);

        for (int i = 0; i < 15; i++)
        {
            float x = -13.5f + i * 1.9f;
            CreateBox(details, $"SouthWall_CopperAccent_{i + 1}",
                new Vector3(x, FloorY + 2.45f, -9.68f),
                new Vector3(1.2f, 0.10f, 0.05f), trimMat, false);
        }

        for (int i = 0; i < 9; i++)
        {
            float z = -7.8f + i * 1.9f;
            CreateBox(details, $"GearWall_AcousticPanel_{i + 1}",
                new Vector3(-14.67f, FloorY + 1.45f, z),
                new Vector3(0.05f, 1.1f, 1.25f), wallAccentMat, false);
        }

        CreateBox(details, "Basement_ServiceBeam_A",
            new Vector3(-2f, -0.42f, -1.5f), new Vector3(24f, 0.18f, 0.22f), darkMetalMat, false);
        CreateBox(details, "Basement_ServiceBeam_B",
            new Vector3(-2f, -0.42f, 4f), new Vector3(24f, 0.18f, 0.22f), darkMetalMat, false);
    }

    // Wall plane parallel to YZ; doorway width runs along world Z.
    private static void BuildWallAlongX(
        Transform parent, string name, float x, float minZ, float maxZ,
        float doorCenterZ, float doorWidth, bool exterior)
    {
        float doorMin = doorCenterZ - doorWidth * 0.5f;
        float doorMax = doorCenterZ + doorWidth * 0.5f;
        float doorHeight = 2.20f;
        Material material = exterior ? wallMat : wallAccentMat;

        if (doorMin > minZ)
            CreateBox(parent, name + "_SegmentA",
                new Vector3(x, WallCenterY, (minZ + doorMin) * 0.5f),
                new Vector3(WallThickness, WallHeight, doorMin - minZ), material, true);
        if (maxZ > doorMax)
            CreateBox(parent, name + "_SegmentB",
                new Vector3(x, WallCenterY, (doorMax + maxZ) * 0.5f),
                new Vector3(WallThickness, WallHeight, maxZ - doorMax), material, true);

        CreateBox(parent, name + "_Lintel",
            new Vector3(x, FloorY + doorHeight + (WallHeight - doorHeight) * 0.5f, doorCenterZ),
            new Vector3(WallThickness, WallHeight - doorHeight, doorWidth), material, true);

        CreateDoorFrameAlongX(parent, name + "_Frame", x, FloorY, doorCenterZ, doorWidth, doorHeight);
        CreateSwingDoor(parent, name + "_Door",
            new Vector3(x, FloorY, doorMin), Quaternion.Euler(0f, -90f, 0f),
            doorWidth, doorHeight, exterior ? "Escape Tunnel Door" : name, exterior ? 110f : 100f);
    }

    // Wall plane parallel to XY; doorway width runs along world X.
    private static void BuildWallAlongZ(
        Transform parent, string name, float z, float minX, float maxX,
        float doorCenterX, float doorWidth)
    {
        float doorMin = doorCenterX - doorWidth * 0.5f;
        float doorMax = doorCenterX + doorWidth * 0.5f;
        float doorHeight = 2.20f;

        if (doorMin > minX)
            CreateBox(parent, name + "_SegmentA",
                new Vector3((minX + doorMin) * 0.5f, WallCenterY, z),
                new Vector3(doorMin - minX, WallHeight, WallThickness), wallAccentMat, true);
        if (maxX > doorMax)
            CreateBox(parent, name + "_SegmentB",
                new Vector3((doorMax + maxX) * 0.5f, WallCenterY, z),
                new Vector3(maxX - doorMax, WallHeight, WallThickness), wallAccentMat, true);

        CreateBox(parent, name + "_Lintel",
            new Vector3(doorCenterX, FloorY + doorHeight + (WallHeight - doorHeight) * 0.5f, z),
            new Vector3(doorWidth, WallHeight - doorHeight, WallThickness), wallAccentMat, true);

        CreateDoorFrameAlongZ(parent, name + "_Frame", z, FloorY, doorCenterX, doorWidth, doorHeight);
        CreateSwingDoor(parent, name + "_Door",
            new Vector3(doorMin, FloorY, z), Quaternion.identity,
            doorWidth, doorHeight, name, 100f);
    }

    private static void CreateDoorFrameAlongX(
        Transform parent, string name, float x, float floorY, float centerZ, float width, float height)
    {
        CreateBox(parent, name + "_JambA",
            new Vector3(x, floorY + height * 0.5f, centerZ - width * 0.5f),
            new Vector3(0.34f, height, 0.10f), trimMat, false);
        CreateBox(parent, name + "_JambB",
            new Vector3(x, floorY + height * 0.5f, centerZ + width * 0.5f),
            new Vector3(0.34f, height, 0.10f), trimMat, false);
        CreateBox(parent, name + "_Header",
            new Vector3(x, floorY + height - 0.04f, centerZ),
            new Vector3(0.34f, 0.08f, width + 0.10f), trimMat, false);
    }

    private static void CreateDoorFrameAlongZ(
        Transform parent, string name, float z, float floorY, float centerX, float width, float height)
    {
        CreateBox(parent, name + "_JambA",
            new Vector3(centerX - width * 0.5f, floorY + height * 0.5f, z),
            new Vector3(0.10f, height, 0.34f), trimMat, false);
        CreateBox(parent, name + "_JambB",
            new Vector3(centerX + width * 0.5f, floorY + height * 0.5f, z),
            new Vector3(0.10f, height, 0.34f), trimMat, false);
        CreateBox(parent, name + "_Header",
            new Vector3(centerX, floorY + height - 0.04f, z),
            new Vector3(width + 0.10f, 0.08f, 0.34f), trimMat, false);
    }

    private static void CreateSwingDoor(
        Transform parent, string name, Vector3 hingePosition, Quaternion hingeRotation,
        float width, float height, string label, float openAngle)
    {
        GameObject hinge = new GameObject(name + "_Hinge");
        Undo.RegisterCreatedObjectUndo(hinge, "Create Interactive Door");
        hinge.transform.SetParent(parent, false);
        hinge.transform.position = hingePosition;
        hinge.transform.rotation = hingeRotation;

        GameObject panel = CreateLocalBox(hinge.transform, "DoorPanel",
            new Vector3(width * 0.5f, height * 0.5f, 0f),
            new Vector3(width - 0.08f, height - 0.08f, 0.12f), doorMat, true);

        CreateLocalBox(hinge.transform, "DoorInset",
            new Vector3(width * 0.5f, height * 0.55f, -0.066f),
            new Vector3(width * 0.72f, height * 0.50f, 0.018f), woodMat, false);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(handle, "Create Door Handle");
        handle.name = "DoorHandle";
        handle.transform.SetParent(hinge.transform, false);
        handle.transform.localPosition = new Vector3(width * 0.84f, height * 0.48f, -0.095f);
        handle.transform.localScale = Vector3.one * 0.09f;
        handle.GetComponent<Renderer>().sharedMaterial = goldMat;
        Collider handleCollider = handle.GetComponent<Collider>();
        if (handleCollider != null) Undo.DestroyObjectImmediate(handleCollider);

        BoxCollider interaction = hinge.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(width * 0.5f, height * 0.5f, 0f);
        interaction.size = new Vector3(width + 1.3f, height + 0.6f, 2.8f);

        Rigidbody body = hinge.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        OfficeDoorController controller = hinge.AddComponent<OfficeDoorController>();
        controller.Configure(panel.transform, label, openAngle, 5.5f);
    }

    private static void BuildStairExtension(Transform parent)
    {
        Transform steps = CreateGroup("BasementStairSteps", parent);
        Transform rails = CreateGroup("BasementStairRails", parent);

        Material landingMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Generated/OfficeHeist/Materials/Floor_CorridorSlate.mat");
        if (landingMaterial == null) landingMaterial = floorAccentMat;
        CreateBox(steps, "Basement_TopLandingPad", new Vector3(12f, 0.01f, 2.55f),
            new Vector3(2.2f, 0.02f, 0.72f), landingMaterial, false);

        const int count = 18;
        const float startZ = 2.2f;
        const float endZ = -3.8f;
        const float totalDrop = 3.35f;
        float tread = (startZ - endZ) / count;
        float rise = totalDrop / count;

        for (int i = 0; i < count; i++)
        {
            float z = startZ - (i + 0.5f) * tread;
            float topY = -(i + 1) * rise;
            CreateBox(steps, $"BasementStep_{i + 1:00}",
                new Vector3(12f, topY - 0.105f, z),
                new Vector3(1.48f, 0.21f, tread + 0.035f), floorAccentMat, true);
            CreateBox(steps, $"BasementStep_Nosing_{i + 1:00}",
                new Vector3(12f, topY - 0.015f, z - tread * 0.34f),
                new Vector3(1.48f, 0.035f, 0.045f), trimMat, false);
        }

        CreateBox(steps, "BasementStair_BottomLanding",
            new Vector3(12f, FloorY + 0.06f, -4.30f),
            new Vector3(2.0f, 0.12f, 1.1f), floorAccentMat, true);

        Vector3 leftStart = new Vector3(11.13f, 0.92f, 2.0f);
        Vector3 leftEnd = new Vector3(11.13f, -2.42f, -3.75f);
        Vector3 rightStart = new Vector3(12.87f, 0.92f, 2.0f);
        Vector3 rightEnd = new Vector3(12.87f, -2.42f, -3.75f);
        CreateBeam(rails, "LeftStairHandrail", leftStart, leftEnd, 0.075f, metalMat);
        CreateBeam(rails, "RightStairHandrail", rightStart, rightEnd, 0.075f, metalMat);

        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            Vector3 left = Vector3.Lerp(leftStart, leftEnd, t);
            Vector3 right = Vector3.Lerp(rightStart, rightEnd, t);
            CreateBeam(rails, $"LeftRailPost_{i}", left, left - Vector3.up * 0.78f, 0.045f, darkMetalMat);
            CreateBeam(rails, $"RightRailPost_{i}", right, right - Vector3.up * 0.78f, 0.045f, darkMetalMat);
        }

        for (int i = 0; i < 6; i++)
        {
            float z = 1.4f - i * 0.86f;
            float y = -0.15f - i * 0.47f;
            CreateBox(rails, $"StairGuideLight_L_{i + 1}",
                new Vector3(11.03f, y, z), new Vector3(0.06f, 0.10f, 0.30f), amberMat, false);
            CreateBox(rails, $"StairGuideLight_R_{i + 1}",
                new Vector3(12.97f, y, z), new Vector3(0.06f, 0.10f, 0.30f), amberMat, false);
        }
    }

    private static void BuildRoomsAndFurniture(Transform parent)
    {
        Transform planning = CreateGroup("01_ThiefPlanningRoom", parent);
        Transform gear = CreateGroup("02_GearAndWorkshop", parent);
        Transform vault = CreateGroup("03_StolenGoodsVault", parent);
        Transform passage = CreateGroup("04_StairAndEscapePassage", parent);

        // Planning table, map and surveillance equipment.
        CreateBox(planning, "PlanningTable_Top", new Vector3(-0.2f, -2.76f, -0.9f),
            new Vector3(3.8f, 0.12f, 1.75f), woodMat, true);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
            CreateBox(planning, $"PlanningTable_Leg_{x}_{z}",
                new Vector3(-0.2f + x * 1.62f, -3.15f, -0.9f + z * 0.68f),
                new Vector3(0.12f, 0.78f, 0.12f), darkMetalMat, true);

        CreateBox(planning, "HeistMap_Board", new Vector3(-0.2f, -1.45f, 9.68f),
            new Vector3(5.0f, 1.45f, 0.10f), wallAccentMat, false);
        CreateBox(planning, "HeistMap_Paper", new Vector3(-0.2f, -1.42f, 9.60f),
            new Vector3(3.8f, 0.98f, 0.025f), paperMat, false);
        CreateBox(planning, "HeistMap_Route_A", new Vector3(-0.75f, -1.40f, 9.57f),
            new Vector3(2.3f, 0.035f, 0.035f), amberMat, false);
        CreateBox(planning, "HeistMap_Route_B", new Vector3(0.35f, -1.10f, 9.57f),
            new Vector3(1.45f, 0.035f, 0.035f), tealMat, false);

        for (int i = 0; i < 3; i++)
        {
            CreateBox(planning, $"ControlMonitor_{i + 1}_Body",
                new Vector3(-1.2f + i * 1.05f, -2.25f, -1.45f),
                new Vector3(0.78f, 0.48f, 0.07f), darkMetalMat, false);
            CreateBox(planning, $"ControlMonitor_{i + 1}_Screen",
                new Vector3(-1.2f + i * 1.05f, -2.25f, -1.495f),
                new Vector3(0.69f, 0.38f, 0.018f), i == 1 ? amberMat : screenMat, false);
        }

        CreateBox(planning, "Safehouse_Laptop_Base", new Vector3(0.4f, -2.685f, -0.45f),
            new Vector3(0.72f, 0.045f, 0.48f), metalMat, false);
        CreateBox(planning, "Safehouse_Laptop_Screen", new Vector3(0.4f, -2.40f, -0.67f),
            new Vector3(0.70f, 0.48f, 0.045f), screenMat, false);
        CreateBox(planning, "Planning_Documents", new Vector3(1.35f, -2.68f, -0.35f),
            new Vector3(0.42f, 0.07f, 0.32f), paperMat, false);

        CreateSeat(planning, "PlanningChair_A", new Vector3(-1.1f, FloorY, -2.25f));
        CreateSeat(planning, "PlanningChair_B", new Vector3(1.1f, FloorY, -2.25f));
        CreateBox(planning, "Safehouse_SofaBase", new Vector3(2.6f, -3.18f, 3.15f),
            new Vector3(2.25f, 0.55f, 0.82f), fabricMat, true);
        CreateBox(planning, "Safehouse_SofaBack", new Vector3(2.6f, -2.75f, 3.50f),
            new Vector3(2.25f, 0.65f, 0.18f), fabricMat, false);
        CreateBox(planning, "Safehouse_Locker", new Vector3(-5.75f, -2.45f, 3.4f),
            new Vector3(0.95f, 2.20f, 0.72f), darkMetalMat, true);

        // Gear workshop: workbench, lockers, crates and a tool board.
        CreateBox(gear, "Gear_WorkbenchTop", new Vector3(-11.0f, -2.78f, -3.4f),
            new Vector3(4.2f, 0.12f, 1.1f), darkWoodMat, true);
        for (int i = 0; i < 4; i++)
        {
            float x = -13.4f + i * 1.5f;
            CreateBox(gear, $"Gear_Locker_{i + 1}", new Vector3(x, -2.35f, 4.8f),
                new Vector3(1.15f, 2.30f, 0.75f), darkMetalMat, true);
            CreateBox(gear, $"Locker_Handle_{i + 1}", new Vector3(x, -2.35f, 4.39f),
                new Vector3(0.12f, 0.24f, 0.04f), goldMat, false);
        }
        CreateStorageCrate(gear, "GearCrate_A", new Vector3(-12.6f, FloorY + 0.40f, -5.5f));
        CreateStorageCrate(gear, "GearCrate_B", new Vector3(-10.2f, FloorY + 0.40f, -5.5f));
        CreateBox(gear, "ToolRack_Board", new Vector3(-14.64f, -1.70f, -1.0f),
            new Vector3(0.08f, 1.45f, 3.8f), wallAccentMat, false);
        for (int i = 0; i < 5; i++)
            CreateBox(gear, $"ToolRack_Tool_{i + 1}", new Vector3(-14.55f, -1.8f, -2.25f + i * 0.60f),
                new Vector3(0.08f, 0.55f, 0.10f), trimMat, false);

        // Loot vault: shelves and a safe.
        CreateBox(vault, "Vault_Shelf_Back", new Vector3(11.65f, -2.30f, 7.6f),
            new Vector3(3.7f, 2.45f, 0.28f), darkWoodMat, true);
        for (int i = 0; i < 3; i++)
            CreateBox(vault, $"Vault_Shelf_{i + 1}", new Vector3(11.65f, -3.0f + i * 0.72f, 7.40f),
                new Vector3(3.5f, 0.09f, 0.55f), trimMat, false);
        CreateBox(vault, "Vault_Safe_Body", new Vector3(9.4f, -2.5f, 5.2f),
            new Vector3(1.25f, 1.95f, 1.0f), darkMetalMat, true);
        CreateBox(vault, "Vault_Safe_Dial", new Vector3(9.4f, -2.45f, 4.67f),
            new Vector3(0.32f, 0.32f, 0.04f), goldMat, false);

        CreateBox(passage, "StairLanding_WallSign", new Vector3(12f, -1.10f, -4.30f),
            new Vector3(1.65f, 0.34f, 0.08f), wallAccentMat, false);
        CreateBox(passage, "StairLanding_SignAccent", new Vector3(12f, -1.10f, -4.36f),
            new Vector3(0.95f, 0.045f, 0.02f), amberMat, false);
        CreateBox(passage, "VentilationDuct_Main", new Vector3(-2f, -0.52f, 0f),
            new Vector3(18f, 0.22f, 0.42f), darkMetalMat, false);
        CreateBox(passage, "VentilationDuct_Branch", new Vector3(-10.5f, -0.52f, 1f),
            new Vector3(0.42f, 0.22f, 9f), darkMetalMat, false);
    }

    private static void BuildLootAndProps(Transform parent)
    {
        CreateLoot(parent, "Loot_Thief_Cash", new Vector3(-1.2f, FloorY + 0.18f, -0.75f),
            LootCollectible.LootPresetType.CashBundle, "Stashed Cash", "Collect the hidden cash", 350, 0);
        CreateLoot(parent, "Loot_Thief_Laptop", new Vector3(-11.1f, FloorY + 0.30f, -3.4f),
            LootCollectible.LootPresetType.Laptop, "Encrypted Laptop", "Collect the encrypted laptop", 650, 1);
        CreateLoot(parent, "Loot_Thief_Gold", new Vector3(11.65f, FloorY + 0.18f, 7.6f),
            LootCollectible.LootPresetType.GoldObject, "Gold Bars", "Collect the gold bars", 900, 2);
        CreateLoot(parent, "Loot_Thief_Briefcase", new Vector3(9.4f, FloorY + 0.25f, 5.2f),
            LootCollectible.LootPresetType.Briefcase, "Black Briefcase", "Collect the black briefcase", 500, 3);
    }

    private static void CreateLoot(
        Transform parent, string name, Vector3 position,
        LootCollectible.LootPresetType preset, string displayName, string objective, int value, int visualKind)
    {
        GameObject loot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(loot, "Create Basement Loot");
        loot.transform.SetParent(parent, false);
        loot.transform.position = position;

        SphereCollider trigger = loot.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.15f;

        LootCollectible collectible = loot.AddComponent<LootCollectible>();
        collectible.ConfigurePreset(name.ToLowerInvariant(), preset, displayName, objective, value, false);

        Transform visual = CreateGroup("Visuals", loot.transform);
        switch (visualKind)
        {
            case 0:
                for (int i = 0; i < 4; i++)
                {
                    float x = (i % 2) * 0.18f;
                    float z = (i / 2) * 0.13f;
                    CreateLocalBox(visual, $"CashStack_{i + 1}", new Vector3(x, 0.08f + i * 0.025f, z),
                        new Vector3(0.34f, 0.10f, 0.24f), paperMat, false);
                    CreateLocalBox(visual, $"CashBand_{i + 1}", new Vector3(x, 0.13f + i * 0.025f, z),
                        new Vector3(0.05f, 0.012f, 0.25f), amberMat, false);
                }
                break;

            case 1:
                CreateLocalBox(visual, "Laptop_Base", new Vector3(0f, 0.06f, 0f),
                    new Vector3(0.72f, 0.055f, 0.50f), metalMat, false);
                CreateLocalBox(visual, "Laptop_Keyboard", new Vector3(0f, 0.092f, -0.05f),
                    new Vector3(0.52f, 0.012f, 0.28f), darkMetalMat, false);
                GameObject laptopScreen = CreateLocalBox(visual, "Laptop_Screen",
                    new Vector3(0f, 0.31f, 0.20f), new Vector3(0.72f, 0.45f, 0.055f), screenMat, false);
                laptopScreen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                break;

            case 2:
                for (int i = 0; i < 5; i++)
                {
                    float x = (i % 3) * 0.23f - 0.23f;
                    float y = (i / 3) * 0.12f + 0.08f;
                    GameObject bar = CreateLocalBox(visual, $"GoldBar_{i + 1}",
                        new Vector3(x, y, (i % 2) * 0.12f), new Vector3(0.28f, 0.12f, 0.15f), goldMat, false);
                    bar.transform.localRotation = Quaternion.Euler(0f, (i % 2) * 12f, 0f);
                }
                break;

            case 3:
                CreateLocalBox(visual, "Briefcase_Body", new Vector3(0f, 0.18f, 0f),
                    new Vector3(0.72f, 0.35f, 0.48f), doorMat, false);
                CreateLocalBox(visual, "Briefcase_FrontBand", new Vector3(0f, 0.18f, -0.25f),
                    new Vector3(0.58f, 0.24f, 0.025f), goldMat, false);
                CreateLocalBox(visual, "Briefcase_Handle", new Vector3(0f, 0.42f, 0f),
                    new Vector3(0.30f, 0.06f, 0.07f), metalMat, false);
                CreateLocalBox(visual, "Briefcase_Lock", new Vector3(0f, 0.18f, -0.27f),
                    new Vector3(0.11f, 0.12f, 0.025f), goldMat, false);
                break;
        }
    }

    private static void CreateSeat(Transform parent, string name, Vector3 position)
    {
        CreateBox(parent, name + "_Seat", position + new Vector3(0f, 0.48f, 0f),
            new Vector3(0.62f, 0.12f, 0.62f), fabricMat, true);
        CreateBox(parent, name + "_Back", position + new Vector3(0f, 0.82f, 0.25f),
            new Vector3(0.62f, 0.64f, 0.10f), fabricMat, false);
        CreateBox(parent, name + "_Stem", position + new Vector3(0f, 0.22f, 0f),
            new Vector3(0.10f, 0.42f, 0.10f), metalMat, false);
    }

    private static void CreateStorageCrate(Transform parent, string name, Vector3 position)
    {
        CreateBox(parent, name + "_Body", position, new Vector3(0.95f, 0.78f, 0.82f), darkWoodMat, true);
        CreateBox(parent, name + "_Lid", position + new Vector3(0f, 0.41f, 0f),
            new Vector3(0.99f, 0.08f, 0.86f), woodMat, false);
        for (int i = -1; i <= 1; i++)
            CreateBox(parent, name + "_Band_" + i, position + new Vector3(i * 0.30f, 0f, -0.42f),
                new Vector3(0.07f, 0.72f, 0.035f), trimMat, false);
    }

    private static void BuildLighting(Transform parent)
    {
        CreatePointLight(parent, "PlanningRoom_WarmLight", new Vector3(0f, -0.60f, 0f),
            new Color(1f, 0.72f, 0.48f), 2.5f, 8.5f);
        CreatePointLight(parent, "GearRoom_CoolLight", new Vector3(-11f, -0.62f, 0f),
            new Color(0.42f, 0.72f, 0.90f), 2.2f, 7f);
        CreatePointLight(parent, "Vault_AmberLight", new Vector3(11f, -0.62f, 5f),
            new Color(1f, 0.64f, 0.28f), 2.8f, 7.5f);
        CreatePointLight(parent, "Stair_GuideLight", new Vector3(12f, -0.65f, -3.5f),
            new Color(0.25f, 0.82f, 0.88f), 1.8f, 5f);
        CreatePointLight(parent, "EscapeTunnel_Light", new Vector3(18.5f, -0.90f, -1.5f),
            new Color(0.30f, 0.78f, 0.80f), 2f, 6f);

        CreateBox(parent, "Planning_CeilingEmissive",
            new Vector3(0f, -0.47f, 0f), new Vector3(3f, 0.045f, 0.18f), amberMat, false);
        CreateBox(parent, "Gear_CeilingEmissive",
            new Vector3(-11f, -0.47f, 0f), new Vector3(2.8f, 0.045f, 0.16f), tealMat, false);
    }

    private static void CreatePointLight(
        Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create Basement Light");
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private static void BuildEscapeTunnel(Transform parent)
    {
        const float centerZ = -1.5f;
        const float length = 8f;
        float centerX = 15f + length * 0.5f;

        CreateBox(parent, "EscapeTunnel_Floor",
            new Vector3(centerX, FloorY - 0.02f, centerZ),
            new Vector3(length, FloorThickness, 2.35f), floorAccentMat, true);
        CreateBox(parent, "EscapeTunnel_NorthWall",
            new Vector3(centerX, -2.25f, centerZ + 1.25f),
            new Vector3(length, 2.6f, 0.20f), wallMat, true);
        CreateBox(parent, "EscapeTunnel_SouthWall",
            new Vector3(centerX, -2.25f, centerZ - 1.25f),
            new Vector3(length, 2.6f, 0.20f), wallMat, true);
        CreateBox(parent, "EscapeTunnel_Roof",
            new Vector3(18.75f, -0.95f, centerZ),
            new Vector3(7.5f, 0.18f, 2.35f), darkMetalMat, true);

        GameObject ramp = CreateBox(parent, "EscapeTunnel_SurfaceRamp",
            new Vector3(22f, -1.78f, centerZ),
            new Vector3(5.2f, 0.22f, 1.85f), floorMat, true);
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, 34f);

        CreateBox(parent, "SurfaceExit_Frame_Left",
            new Vector3(24.3f, -0.15f, centerZ - 1.05f),
            new Vector3(0.22f, 0.55f, 0.22f), trimMat, false);
        CreateBox(parent, "SurfaceExit_Frame_Right",
            new Vector3(24.3f, -0.15f, centerZ + 1.05f),
            new Vector3(0.22f, 0.55f, 0.22f), trimMat, false);
        CreateBox(parent, "SurfaceExit_Sign",
            new Vector3(24.3f, -0.05f, centerZ),
            new Vector3(1.2f, 0.30f, 0.10f), wallAccentMat, false);
        CreateBox(parent, "SurfaceExit_SignLight",
            new Vector3(24.3f, -0.05f, centerZ - 0.06f),
            new Vector3(0.48f, 0.06f, 0.025f), tealMat, false);
    }

    private static void CreateBeam(
        Transform parent, string name, Vector3 start, Vector3 end, float thickness, Material material)
    {
        Vector3 delta = end - start;
        GameObject beam = CreateBox(parent, name, (start + end) * 0.5f,
            new Vector3(thickness, delta.magnitude, thickness), material, false);
        beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
    }

    private static void EnsureMaterials()
    {
        floorMat = GetOrCreateMaterial("Basement_Floor_DeepSlate", new Color(0.15f, 0.19f, 0.23f), 0.20f, 0.42f);
        floorAccentMat = GetOrCreateMaterial("Basement_Floor_Charcoal", new Color(0.23f, 0.27f, 0.30f), 0.10f, 0.32f);
        wallMat = GetOrCreateMaterial("Basement_Wall_MidnightBlue", new Color(0.12f, 0.19f, 0.25f), 0.05f, 0.22f);
        wallAccentMat = GetOrCreateMaterial("Basement_Wall_SlateBlue", new Color(0.23f, 0.31f, 0.36f), 0.08f, 0.28f);
        trimMat = GetOrCreateMaterial("Basement_Trim_BurnishedCopper", new Color(0.62f, 0.31f, 0.13f), 0.55f, 0.58f);
        metalMat = GetOrCreateMaterial("Basement_Metal_BrushedSteel", new Color(0.48f, 0.55f, 0.60f), 0.78f, 0.62f);
        darkMetalMat = GetOrCreateMaterial("Basement_Metal_Charcoal", new Color(0.075f, 0.09f, 0.11f), 0.72f, 0.48f);
        woodMat = GetOrCreateMaterial("Basement_Wood_WarmOak", new Color(0.42f, 0.27f, 0.16f), 0.02f, 0.36f);
        darkWoodMat = GetOrCreateMaterial("Basement_Wood_DarkWalnut", new Color(0.18f, 0.11f, 0.075f), 0.02f, 0.38f);
        fabricMat = GetOrCreateMaterial("Basement_Fabric_OliveGrey", new Color(0.26f, 0.31f, 0.30f), 0f, 0.16f);
        screenMat = GetOrCreateMaterial("Basement_Screen_Cyan", new Color(0.04f, 0.38f, 0.49f), 0.12f, 0.65f, true);
        amberMat = GetOrCreateMaterial("Basement_Emissive_Amber", new Color(0.95f, 0.40f, 0.10f), 0.1f, 0.50f, true);
        tealMat = GetOrCreateMaterial("Basement_Emissive_Teal", new Color(0.05f, 0.68f, 0.72f), 0.1f, 0.50f, true);
        goldMat = GetOrCreateMaterial("Basement_Gold_Collectible", new Color(0.88f, 0.62f, 0.16f), 0.72f, 0.62f);
        paperMat = GetOrCreateMaterial("Basement_Paper_OffWhite", new Color(0.82f, 0.79f, 0.68f), 0f, 0.12f);
        doorMat = GetOrCreateMaterial("Basement_Door_Mahogany", new Color(0.22f, 0.12f, 0.08f), 0.08f, 0.46f);
        AssetDatabase.SaveAssets();
    }

    private static Material GetOrCreateMaterial(
        string materialName, Color color, float metallic, float smoothness, bool emission = false)
    {
        EnsureFolderExists(MaterialFolder);
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.6f);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolderExists(parent);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, "Create " + name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateBox(
        Transform parent, string name, Vector3 position, Vector3 size, Material material, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = size;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
        }
        return go;
    }

    private static GameObject CreateLocalBox(
        Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, bool withCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = size;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
        }
        return go;
    }
}
#endif
