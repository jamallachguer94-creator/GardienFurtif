#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissionSetupEditor1
{
    private const string FileMarkerName = "Mission_FileSpawnPoint";
    private const string ExitMarkerName = "Mission_ExitPoint";
    private const string ManagerName = "_MissionManager";
    private const string DossierName = "Mission_Dossier";
    private const string ExitZoneName = "Mission_ExitZone";
    private const string MaterialFolder = "Assets/Materials/GardienFurtifMission";

    [MenuItem("Tools/Gardien Furtif/Mission/Build or Rebuild Mission System")]
    public static void BuildMissionSystem()
    {
        Transform fileMarker = FindSceneTransform(FileMarkerName);
        Transform exitMarker = FindSceneTransform(ExitMarkerName);
        GameObject player = FindPlayer();

        if (fileMarker == null || exitMarker == null || player == null)
        {
            string missing = "";
            if (fileMarker == null) missing += "\n- " + FileMarkerName;
            if (exitMarker == null) missing += "\n- " + ExitMarkerName;
            if (player == null) missing += "\n- Player";
            EditorUtility.DisplayDialog("Mission setup: missing references",
                "The following scene objects could not be found:" + missing +
                "\n\nCheck that the objects are active and named exactly as shown.", "OK");
            return;
        }

        GameObject managerObject = FindSceneObject(ManagerName);
        if (managerObject == null)
        {
            managerObject = new GameObject(ManagerName);
            Undo.RegisterCreatedObjectUndo(managerObject, "Create Mission Manager");
        }

        MissionManager manager = managerObject.GetComponent<MissionManager>();
        if (manager == null) manager = Undo.AddComponent<MissionManager>(managerObject);
        if (managerObject.GetComponent<MissionHUD>() == null)
            Undo.AddComponent<MissionHUD>(managerObject);

        GameObject dossier = FindSceneObject(DossierName);
        if (dossier == null)
        {
            dossier = new GameObject(DossierName);
            Undo.RegisterCreatedObjectUndo(dossier, "Create Mission Dossier");
        }
        dossier.transform.SetParent(null);
        dossier.transform.SetPositionAndRotation(fileMarker.position, fileMarker.rotation);

        BoxCollider dossierTrigger = dossier.GetComponent<BoxCollider>();
        if (dossierTrigger == null) dossierTrigger = Undo.AddComponent<BoxCollider>(dossier);
        dossierTrigger.isTrigger = true;
        dossierTrigger.center = new Vector3(0f, 0.55f, 0f);
        dossierTrigger.size = new Vector3(1.6f, 1.5f, 1.6f);

        Transform visualTransform = dossier.transform.Find("DossierVisual");
        GameObject visualRoot;
        if (visualTransform == null)
        {
            visualRoot = new GameObject("DossierVisual");
            Undo.RegisterCreatedObjectUndo(visualRoot, "Create Dossier Visual");
            visualRoot.transform.SetParent(dossier.transform, false);
            BuildDossierVisual(visualRoot.transform);
        }
        else
        {
            visualRoot = visualTransform.gameObject;
        }

        MissionFilePickup pickup = dossier.GetComponent<MissionFilePickup>();
        if (pickup == null) pickup = Undo.AddComponent<MissionFilePickup>(dossier);
        pickup.interactKey = KeyCode.E;
        pickup.prompt = "Appuyez sur E pour récupérer le dossier";
        pickup.visualRoot = visualRoot;

        GameObject exitObject = FindSceneObject(ExitZoneName);
        if (exitObject == null)
        {
            exitObject = new GameObject(ExitZoneName);
            Undo.RegisterCreatedObjectUndo(exitObject, "Create Mission Exit Zone");
        }
        exitObject.transform.SetParent(null);
        exitObject.transform.SetPositionAndRotation(exitMarker.position, exitMarker.rotation);

        BoxCollider exitCollider = exitObject.GetComponent<BoxCollider>();
        if (exitCollider == null) exitCollider = Undo.AddComponent<BoxCollider>(exitObject);
        exitCollider.isTrigger = true;
        exitCollider.center = new Vector3(0f, 1.15f, 0f);
        exitCollider.size = new Vector3(2.6f, 2.3f, 2.6f);

        MissionExitZone exitZone = exitObject.GetComponent<MissionExitZone>();
        if (exitZone == null) exitZone = Undo.AddComponent<MissionExitZone>(exitObject);

        Undo.RecordObject(manager, "Assign Mission References");
        manager.player = player;
        manager.filePickup = pickup;
        manager.exitZone = exitZone;
        manager.interactKey = KeyCode.E;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(pickup);
        EditorUtility.SetDirty(exitZone);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = managerObject;

        EditorUtility.DisplayDialog("Mission system ready",
            "Mission objects have been created and linked.\n\n" +
            "1. Save the scene (Ctrl+S).\n" +
            "2. Press Play.\n" +
            "3. Approach the dossier and press E.\n" +
            "4. Reach the exit marker.\n\n" +
            "The dossier and exit use trigger volumes. The Player object must keep its CharacterController.", "OK");
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Object[] all = Resources.FindObjectsOfTypeAll(typeof(Transform));
        foreach (Object item in all)
        {
            Transform t = item as Transform;
            if (t == null || t.name != objectName) continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;
            return t;
        }
        return null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform t = FindSceneTransform(objectName);
        return t != null ? t.gameObject : null;
    }

    private static GameObject FindPlayer()
    {
        GameObject player = FindSceneObject("Player");
        if (player != null) return player;

        try { return GameObject.FindGameObjectWithTag("Player"); }
        catch (UnityException) { return null; }
    }

    private static void BuildDossierVisual(Transform parent)
    {
        Material cover = GetOrCreateMaterial("DossierCover.mat", new Color(0.22f, 0.12f, 0.055f));
        Material paper = GetOrCreateMaterial("DossierPages.mat", new Color(0.88f, 0.84f, 0.72f));
        Material band = GetOrCreateMaterial("DossierBand.mat", new Color(0.55f, 0.12f, 0.08f));

        CreateVisualCube(parent, "Pages", new Vector3(0f, 0.025f, 0f), new Vector3(0.34f, 0.045f, 0.24f), paper);
        CreateVisualCube(parent, "BottomCover", new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.018f, 0.26f), cover);
        CreateVisualCube(parent, "TopCover", new Vector3(0f, 0.052f, 0f), new Vector3(0.36f, 0.018f, 0.26f), cover);
        CreateVisualCube(parent, "SecurityBand", new Vector3(0f, 0.064f, 0f), new Vector3(0.055f, 0.006f, 0.265f), band);
    }

    private static void CreateVisualCube(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        Transform existing = parent.Find(objectName);
        GameObject cube;
        if (existing != null) cube = existing.gameObject;
        else
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Create Dossier Part");
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
        }

        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;
        Collider col = cube.GetComponent<Collider>();
        if (col != null) Undo.DestroyObjectImmediate(col);
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static Material GetOrCreateMaterial(string fileName, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "GardienFurtifMission");

        string path = MaterialFolder + "/" + fileName;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");
        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        return material;
    }
}
#endif
