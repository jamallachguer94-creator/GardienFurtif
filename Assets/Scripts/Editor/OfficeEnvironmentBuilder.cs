#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GardienFurtif.EditorTools
{
    /// <summary>
    /// Generates a first playable blockout of the Gardien Furtif office.
    /// Run from: Gardien Furtif > Environment > Build Office Environment
    /// Safe to run again: only the OfficeEnvironment_Generated hierarchy is replaced.
    /// </summary>
    public static class OfficeEnvironmentBuilder
    {
        private const string GeneratedRootName = "OfficeEnvironment_Generated";
        private const string MaterialsFolder = "Assets/Materials/GeneratedOffice";
        private const float WallHeight = 3.2f;
        private const float WallThickness = 0.25f;

        private sealed class Palette
        {
            public Material Floor;
            public Material Corridor;
            public Material Wall;
            public Material WallAccent;
            public Material Trim;
            public Material Wood;
            public Material WoodLight;
            public Material Metal;
            public Material DarkMetal;
            public Material Fabric;
            public Material Screen;
            public Material Plant;
            public Material White;
            public Material GlassBlue;
        }

        [MenuItem("Gardien Furtif/Environment/Build Office Environment")]
        public static void BuildOfficeEnvironment()
        {
            GameObject existing = GameObject.Find(GeneratedRootName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Rebuild Office Environment",
                    "A generated office already exists. Replace only '" + GeneratedRootName + "'? Your other scene objects will not be changed.",
                    "Replace generated office",
                    "Cancel");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing);
            }

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "GeneratedOffice");
            Palette palette = CreatePalette();

            GameObject root = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Gardien Furtif Office");

            Transform architecture = CreateGroup("Architecture", root.transform);
            Transform furniture = CreateGroup("Furniture", root.transform);
            Transform lighting = CreateGroup("Lighting", root.transform);
            Transform details = CreateGroup("Details", root.transform);

            BuildShell(architecture, palette);
            BuildInteriorPartitions(architecture, palette);
            BuildRoomFloorAccents(details, palette);
            BuildReception(furniture, palette);
            BuildMeetingRoom(furniture, palette);
            BuildArchive(furniture, palette);
            BuildOpenOffice(furniture, palette);
            BuildDirectorOffice(furniture, palette);
            BuildServerRoom(furniture, palette);
            BuildCorridorDetails(furniture, details, palette);
            BuildLighting(lighting, palette);

            root.transform.position = Vector3.zero;
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Office Environment Created",
                "The office blockout has been generated. Save the scene (Ctrl+S), then inspect it from the Scene view.\n\nGenerated hierarchy: " + GeneratedRootName,
                "OK");
        }

        private static Palette CreatePalette()
        {
            return new Palette
            {
                Floor = GetOrCreateMaterial("Office_Floor", new Color(0.34f, 0.37f, 0.39f), 0.05f, 0.25f),
                Corridor = GetOrCreateMaterial("Office_Corridor", new Color(0.24f, 0.29f, 0.32f), 0.05f, 0.28f),
                Wall = GetOrCreateMaterial("Office_Walls", new Color(0.76f, 0.77f, 0.73f), 0f, 0.18f),
                WallAccent = GetOrCreateMaterial("Office_Wall_Accent", new Color(0.16f, 0.22f, 0.27f), 0.05f, 0.25f),
                Trim = GetOrCreateMaterial("Office_Trim", new Color(0.09f, 0.12f, 0.14f), 0.12f, 0.32f),
                Wood = GetOrCreateMaterial("Office_Wood", new Color(0.36f, 0.22f, 0.13f), 0.02f, 0.28f),
                WoodLight = GetOrCreateMaterial("Office_Wood_Light", new Color(0.58f, 0.42f, 0.27f), 0f, 0.25f),
                Metal = GetOrCreateMaterial("Office_Metal", new Color(0.35f, 0.40f, 0.43f), 0.55f, 0.45f),
                DarkMetal = GetOrCreateMaterial("Office_Dark_Metal", new Color(0.07f, 0.09f, 0.11f), 0.45f, 0.38f),
                Fabric = GetOrCreateMaterial("Office_Fabric", new Color(0.12f, 0.22f, 0.28f), 0f, 0.45f),
                Screen = GetOrCreateMaterial("Office_Screen", new Color(0.035f, 0.10f, 0.13f), 0.15f, 0.25f),
                Plant = GetOrCreateMaterial("Office_Plant", new Color(0.12f, 0.30f, 0.16f), 0f, 0.5f),
                White = GetOrCreateMaterial("Office_White", new Color(0.88f, 0.88f, 0.82f), 0f, 0.2f),
                GlassBlue = GetOrCreateMaterial("Office_Glass_Blue", new Color(0.18f, 0.34f, 0.39f), 0.25f, 0.3f)
            };
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("HDRP/Lit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    throw new InvalidOperationException("Unity could not find a supported shader for generated office materials.");

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildShell(Transform parent, Palette p)
        {
            // Building footprint: 30 m wide (X) x 22 m deep (Z).
            CreateCube("Main Floor", parent, new Vector3(0f, -0.16f, 0f), new Vector3(30f, 0.32f, 22f), p.Floor);
            CreateCube("Central Corridor Floor", parent, new Vector3(0f, 0.012f, 0f), new Vector3(29.5f, 0.025f, 2.8f), p.Corridor);

            AddHorizontalWall(parent, "North Exterior Wall", -15f, 15f, 11f, WallHeight, WallThickness, p.Wall);
            AddVerticalWall(parent, "West Exterior Wall", -15f, -11f, 11f, WallHeight, WallThickness, p.Wall);
            AddVerticalWall(parent, "East Exterior Wall", 15f, -11f, 11f, WallHeight, WallThickness, p.Wall);

            // South entrance opening, 2.2 m wide, near the reception side.
            AddHorizontalWall(parent, "South Exterior Wall - Left", -15f, -11.1f, -11f, WallHeight, WallThickness, p.Wall);
            AddHorizontalWall(parent, "South Exterior Wall - Right", -8.9f, 15f, -11f, WallHeight, WallThickness, p.Wall);
            AddDoorFrameHorizontal(parent, "Main Entrance Frame", -10f, -11f, 2.2f, p.Trim, p.WoodLight);

            // Dark skirting strips make the room boundaries easier to read in Scene view.
            AddHorizontalWall(parent, "North Wall Skirting", -14.85f, 14.85f, 10.84f, 0.18f, 0.08f, p.Trim);
            AddHorizontalWall(parent, "South Wall Skirting Left", -14.85f, -11.12f, -10.84f, 0.18f, 0.08f, p.Trim);
            AddHorizontalWall(parent, "South Wall Skirting Right", -8.88f, 14.85f, -10.84f, 0.18f, 0.08f, p.Trim);
        }

        private static void BuildInteriorPartitions(Transform parent, Palette p)
        {
            // North wing: Reception / Meeting Room / Archive, all entered from the main corridor.
            AddHorizontalWallWithOpenings(parent, "North Wing Corridor Wall", -14.85f, 14.85f, 1.4f,
                new[] { -10f, 0.5f, 10f }, 1.6f, p.Wall);
            AddVerticalWallWithOpening(parent, "North Partition - Reception and Meeting", -4.5f, 1.52f, 10.85f, 6.1f, 1.6f, p.Wall);
            AddVerticalWallWithOpening(parent, "North Partition - Meeting and Archive", 5.5f, 1.52f, 10.85f, 6.1f, 1.6f, p.Wall);

            AddDoorFrameHorizontal(parent, "Reception Door Frame", -10f, 1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameHorizontal(parent, "Meeting Door Frame", 0.5f, 1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameHorizontal(parent, "Archive Door Frame", 10f, 1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameVertical(parent, "Reception Meeting Door Frame", -4.5f, 6.1f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameVertical(parent, "Meeting Archive Door Frame", 5.5f, 6.1f, 1.6f, p.Trim, p.WoodLight);

            // South wing: Open Workspace / Director Office / Server Room.
            AddHorizontalWallWithOpenings(parent, "South Wing Corridor Wall", -14.85f, 14.85f, -1.4f,
                new[] { -10f, 0.5f, 10f }, 1.6f, p.Wall);
            AddVerticalWallWithOpening(parent, "South Partition - Workspace and Director", -4.5f, -10.85f, -1.52f, -6.1f, 1.6f, p.Wall);
            AddVerticalWallWithOpening(parent, "South Partition - Director and Server", 5.5f, -10.85f, -1.52f, -6.1f, 1.6f, p.Wall);

            AddDoorFrameHorizontal(parent, "Workspace Door Frame", -10f, -1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameHorizontal(parent, "Director Door Frame", 0.5f, -1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameHorizontal(parent, "Server Room Door Frame", 10f, -1.4f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameVertical(parent, "Workspace Director Door Frame", -4.5f, -6.1f, 1.6f, p.Trim, p.WoodLight);
            AddDoorFrameVertical(parent, "Director Server Door Frame", 5.5f, -6.1f, 1.6f, p.Trim, p.WoodLight);

            // Short visual dividers inside the office areas; kept low enough to preserve sightlines.
            CreateCube("Reception Low Divider", parent, new Vector3(-12.8f, 0.65f, 5.4f), new Vector3(0.18f, 1.3f, 3.2f), p.WallAccent);
            CreateCube("Workspace Low Divider", parent, new Vector3(-12.7f, 0.62f, -5.8f), new Vector3(0.18f, 1.24f, 4.2f), p.WallAccent);
        }

        private static void BuildRoomFloorAccents(Transform parent, Palette p)
        {
            // Thin floor inlays identify each room without blocking navigation.
            CreateCube("Reception Floor Zone", parent, new Vector3(-9.7f, 0.012f, 6.1f), new Vector3(9.8f, 0.025f, 9.1f), p.WoodLight);
            CreateCube("Meeting Floor Zone", parent, new Vector3(0.5f, 0.014f, 6.1f), new Vector3(9.7f, 0.025f, 9.1f), p.Corridor);
            CreateCube("Archive Floor Zone", parent, new Vector3(10.1f, 0.016f, 6.1f), new Vector3(8.9f, 0.025f, 9.1f), p.WallAccent);
            CreateCube("Workspace Floor Zone", parent, new Vector3(-9.7f, 0.014f, -6.1f), new Vector3(9.8f, 0.025f, 9.1f), p.Corridor);
            CreateCube("Director Floor Zone", parent, new Vector3(0.5f, 0.016f, -6.1f), new Vector3(9.7f, 0.025f, 9.1f), p.WoodLight);
            CreateCube("Server Floor Zone", parent, new Vector3(10.1f, 0.018f, -6.1f), new Vector3(8.9f, 0.025f, 9.1f), p.DarkMetal);
        }

        private static void BuildReception(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Reception", parent);
            CreateDesk(room, "Reception Counter", new Vector3(-11.5f, 0f, 8.4f), new Vector3(3.5f, 0.95f, 0.9f), p.Wood, p.WoodLight);
            CreateCube("Reception Counter Front", room, new Vector3(-11.5f, 0.45f, 7.92f), new Vector3(3.5f, 0.8f, 0.08f), p.Wood);
            CreateChair(room, "Reception Chair", new Vector3(-11.5f, 0f, 9.65f), p.Fabric, p.DarkMetal);
            CreatePlant(room, "Reception Plant", new Vector3(-13.6f, 0f, 3.2f), p);
            CreateCube("Reception Filing Cabinet", room, new Vector3(-6.3f, 0.95f, 8.8f), new Vector3(0.8f, 1.9f, 0.65f), p.Metal);
            CreateCube("Reception Wall Notice Board", room, new Vector3(-8.6f, 2.05f, 10.63f), new Vector3(2.4f, 0.9f, 0.08f), p.Wood);
        }

        private static void BuildMeetingRoom(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Meeting Room", parent);
            CreateDesk(room, "Meeting Table", new Vector3(0.5f, 0f, 6.4f), new Vector3(4.2f, 0.82f, 1.65f), p.Wood, p.WoodLight);
            for (int i = 0; i < 3; i++)
            {
                float x = -1.0f + i * 1.5f;
                CreateChair(room, "Meeting Chair North " + (i + 1), new Vector3(x, 0f, 8.0f), p.Fabric, p.DarkMetal);
                CreateChair(room, "Meeting Chair South " + (i + 1), new Vector3(x, 0f, 4.8f), p.Fabric, p.DarkMetal);
            }
            CreateCube("Meeting Display", room, new Vector3(0.5f, 1.8f, 10.55f), new Vector3(2.8f, 1.45f, 0.12f), p.DarkMetal);
            CreateCube("Meeting Display Screen", room, new Vector3(0.5f, 1.82f, 10.475f), new Vector3(2.55f, 1.2f, 0.025f), p.Screen);
            CreatePlant(room, "Meeting Room Plant", new Vector3(4.2f, 0f, 9.5f), p);
        }

        private static void BuildArchive(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Archive", parent);
            for (int row = 0; row < 2; row++)
            {
                float z = 4.3f + row * 3.5f;
                for (int bay = 0; bay < 3; bay++)
                {
                    float x = 7.4f + bay * 2.5f;
                    CreateArchiveShelf(room, "Archive Shelf " + row + "-" + bay, new Vector3(x, 0f, z), p);
                }
            }
            CreateCube("Archive Work Table", room, new Vector3(12.6f, 0.78f, 9.35f), new Vector3(2.3f, 0.12f, 0.9f), p.Wood);
            CreateCube("Archive Cart Base", room, new Vector3(7.1f, 0.28f, 9.4f), new Vector3(1.0f, 0.12f, 0.65f), p.Metal);
            CreateCube("Archive Cart Upper Shelf", room, new Vector3(7.1f, 0.78f, 9.4f), new Vector3(1.0f, 0.1f, 0.65f), p.Metal);
            CreatePlant(room, "Archive Plant", new Vector3(14.0f, 0f, 3.0f), p);
        }

        private static void BuildOpenOffice(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Open Workspace", parent);
            Vector3[] deskPositions =
            {
                new Vector3(-12.2f, 0f, -3.6f), new Vector3(-8.0f, 0f, -3.6f),
                new Vector3(-12.2f, 0f, -8.0f), new Vector3(-8.0f, 0f, -8.0f)
            };
            for (int i = 0; i < deskPositions.Length; i++)
            {
                Vector3 pos = deskPositions[i];
                CreateDesk(room, "Workstation Desk " + (i + 1), pos, new Vector3(2.2f, 0.76f, 1.0f), p.Wood, p.WoodLight);
                CreateMonitor(room, "Workstation Monitor " + (i + 1), pos + new Vector3(0f, 1.0f, 0.28f), p);
                CreateChair(room, "Workstation Chair " + (i + 1), pos + new Vector3(0f, 0f, -0.95f), p.Fabric, p.DarkMetal);
            }
            CreateCube("Workspace Storage Cabinet", room, new Vector3(-5.7f, 1.0f, -9.3f), new Vector3(1.0f, 2.0f, 0.65f), p.Metal);
            CreatePlant(room, "Workspace Plant", new Vector3(-14.0f, 0f, -2.5f), p);
        }

        private static void BuildDirectorOffice(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Director Office", parent);
            CreateDesk(room, "Director Desk", new Vector3(0.5f, 0f, -7.1f), new Vector3(3.0f, 0.82f, 1.35f), p.Wood, p.WoodLight);
            CreateMonitor(room, "Director Monitor", new Vector3(0.5f, 1.05f, -6.72f), p);
            CreateChair(room, "Director Chair", new Vector3(0.5f, 0f, -5.65f), p.Fabric, p.DarkMetal);
            CreateCube("Director Bookcase", room, new Vector3(-3.35f, 1.15f, -9.8f), new Vector3(0.75f, 2.3f, 1.4f), p.Wood);
            CreateCube("Director Side Cabinet", room, new Vector3(4.1f, 0.65f, -9.5f), new Vector3(1.0f, 1.3f, 0.65f), p.Metal);
            CreatePlant(room, "Director Office Plant", new Vector3(4.1f, 0f, -3.0f), p);
        }

        private static void BuildServerRoom(Transform parent, Palette p)
        {
            Transform room = CreateGroup("Server Room", parent);
            for (int i = 0; i < 3; i++)
            {
                float x = 7.8f + i * 2.3f;
                CreateServerRack(room, "Server Rack " + (i + 1), new Vector3(x, 0f, -8.7f), p);
            }
            CreateCube("Server Room Control Desk", room, new Vector3(12.7f, 0.78f, -4.0f), new Vector3(2.3f, 0.12f, 0.9f), p.Metal);
            CreateMonitor(room, "Server Control Monitor", new Vector3(12.7f, 1.05f, -3.7f), p);
            CreateCube("Server Room Wall Panel", room, new Vector3(14.55f, 1.65f, -5.2f), new Vector3(0.12f, 1.4f, 1.6f), p.DarkMetal);
        }

        private static void BuildCorridorDetails(Transform furniture, Transform details, Palette p)
        {
            CreatePlant(furniture, "Corridor Plant West", new Vector3(-13.8f, 0f, 0f), p);
            CreatePlant(furniture, "Corridor Plant East", new Vector3(13.8f, 0f, 0f), p);
            CreateCube("Corridor Wall Cabinet", furniture, new Vector3(3.0f, 1.0f, -1.12f), new Vector3(1.2f, 1.8f, 0.35f), p.Metal);
            CreateCube("Corridor Direction Sign", details, new Vector3(0f, 2.75f, 1.19f), new Vector3(3.0f, 0.32f, 0.08f), p.WallAccent);
            CreateCube("Entrance Welcome Mat", details, new Vector3(-10f, 0.035f, -9.7f), new Vector3(2.7f, 0.05f, 1.1f), p.Fabric);
            CreateCube("Archive Safety Cabinet", furniture, new Vector3(14.2f, 0.65f, 7.0f), new Vector3(0.55f, 1.3f, 0.55f), p.Trim);
        }

        private static void BuildLighting(Transform parent, Palette p)
        {
            GameObject sun = new GameObject("Office Directional Light");
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.65f;
            directional.color = new Color(1f, 0.94f, 0.82f);
            directional.shadows = LightShadows.Soft;

            Vector3[] positions =
            {
                new Vector3(-10f, 2.95f, 6f), new Vector3(0.5f, 2.95f, 6f), new Vector3(10f, 2.95f, 6f),
                new Vector3(-10f, 2.95f, -6f), new Vector3(0.5f, 2.95f, -6f), new Vector3(10f, 2.95f, -6f),
                new Vector3(0f, 2.95f, 0f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                CreateCube("Ceiling Light Fixture " + (i + 1), parent, positions[i], new Vector3(1.5f, 0.08f, 0.35f), p.White);
                Light pointObject = new GameObject("Room Point Light " + (i + 1)).AddComponent<Light>();
                pointObject.transform.SetParent(parent, false);
                pointObject.transform.position = positions[i] + new Vector3(0f, -0.2f, 0f);
                pointObject.type = LightType.Point;
                pointObject.range = 9f;
                pointObject.intensity = 1.25f;
                pointObject.color = new Color(1f, 0.91f, 0.76f);
                pointObject.shadows = LightShadows.None;
            }
        }

        private static void AddHorizontalWallWithOpenings(Transform parent, string name, float xMin, float xMax, float z,
            float[] centers, float openingWidth, Material material)
        {
            Array.Sort(centers);
            float cursor = xMin;
            for (int i = 0; i < centers.Length; i++)
            {
                float left = centers[i] - openingWidth * 0.5f;
                float right = centers[i] + openingWidth * 0.5f;
                if (left > cursor) AddHorizontalWall(parent, name + " Segment " + (i + 1), cursor, left, z, WallHeight, WallThickness, material);
                cursor = right;
            }
            if (cursor < xMax) AddHorizontalWall(parent, name + " Final Segment", cursor, xMax, z, WallHeight, WallThickness, material);
        }

        private static void AddVerticalWallWithOpening(Transform parent, string name, float x, float zMin, float zMax,
            float openingCenter, float openingWidth, Material material)
        {
            float lowerEnd = openingCenter - openingWidth * 0.5f;
            float upperStart = openingCenter + openingWidth * 0.5f;
            if (lowerEnd > zMin) AddVerticalWall(parent, name + " Lower Segment", x, zMin, lowerEnd, WallHeight, WallThickness, material);
            if (upperStart < zMax) AddVerticalWall(parent, name + " Upper Segment", x, upperStart, zMax, WallHeight, WallThickness, material);
        }

        private static void AddHorizontalWall(Transform parent, string name, float xMin, float xMax, float z,
            float height, float thickness, Material material)
        {
            if (xMax - xMin <= 0.02f) return;
            CreateCube(name, parent, new Vector3((xMin + xMax) * 0.5f, height * 0.5f, z),
                new Vector3(xMax - xMin, height, thickness), material);
        }

        private static void AddVerticalWall(Transform parent, string name, float x, float zMin, float zMax,
            float height, float thickness, Material material)
        {
            if (zMax - zMin <= 0.02f) return;
            CreateCube(name, parent, new Vector3(x, height * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(thickness, height, zMax - zMin), material);
        }

        private static void AddDoorFrameHorizontal(Transform parent, string name, float centerX, float z, float width,
            Material frameMaterial, Material doorAccent)
        {
            float postWidth = 0.14f;
            float postHeight = WallHeight;
            CreateCube(name + " Left Post", parent, new Vector3(centerX - width * 0.5f, postHeight * 0.5f, z),
                new Vector3(postWidth, postHeight, 0.18f), frameMaterial);
            CreateCube(name + " Right Post", parent, new Vector3(centerX + width * 0.5f, postHeight * 0.5f, z),
                new Vector3(postWidth, postHeight, 0.18f), frameMaterial);
            CreateCube(name + " Header", parent, new Vector3(centerX, postHeight - 0.08f, z),
                new Vector3(width + postWidth, 0.16f, 0.2f), frameMaterial);
            CreateCube(name + " Threshold", parent, new Vector3(centerX, 0.035f, z),
                new Vector3(width - 0.08f, 0.07f, 0.28f), doorAccent);
        }

        private static void AddDoorFrameVertical(Transform parent, string name, float x, float centerZ, float width,
            Material frameMaterial, Material doorAccent)
        {
            float postWidth = 0.14f;
            float postHeight = WallHeight;
            CreateCube(name + " Lower Post", parent, new Vector3(x, postHeight * 0.5f, centerZ - width * 0.5f),
                new Vector3(0.18f, postHeight, postWidth), frameMaterial);
            CreateCube(name + " Upper Post", parent, new Vector3(x, postHeight * 0.5f, centerZ + width * 0.5f),
                new Vector3(0.18f, postHeight, postWidth), frameMaterial);
            CreateCube(name + " Header", parent, new Vector3(x, postHeight - 0.08f, centerZ),
                new Vector3(0.2f, 0.16f, width + postWidth), frameMaterial);
            CreateCube(name + " Threshold", parent, new Vector3(x, 0.035f, centerZ),
                new Vector3(0.28f, 0.07f, width - 0.08f), doorAccent);
        }

        private static void CreateDesk(Transform parent, string name, Vector3 position, Vector3 size,
            Material bodyMaterial, Material topMaterial)
        {
            float topThickness = 0.12f;
            float legHeight = Mathf.Max(0.5f, size.y - topThickness);
            CreateCube(name + " Top", parent, position + new Vector3(0f, size.y - topThickness * 0.5f, 0f),
                new Vector3(size.x, topThickness, size.z), topMaterial);
            float legX = size.x * 0.42f;
            float legZ = size.z * 0.36f;
            foreach (float x in new[] { -legX, legX })
            foreach (float z in new[] { -legZ, legZ })
                CreateCube(name + " Leg", parent, position + new Vector3(x, legHeight * 0.5f, z),
                    new Vector3(0.10f, legHeight, 0.10f), bodyMaterial);
        }

        private static void CreateChair(Transform parent, string name, Vector3 position, Material fabric, Material metal)
        {
            CreateCube(name + " Seat", parent, position + new Vector3(0f, 0.52f, 0f), new Vector3(0.62f, 0.16f, 0.62f), fabric);
            CreateCube(name + " Back", parent, position + new Vector3(0f, 0.95f, -0.24f), new Vector3(0.62f, 0.75f, 0.13f), fabric);
            CreateCube(name + " Stem", parent, position + new Vector3(0f, 0.27f, 0f), new Vector3(0.12f, 0.48f, 0.12f), metal);
            CreateCube(name + " Base", parent, position + new Vector3(0f, 0.06f, 0f), new Vector3(0.72f, 0.08f, 0.72f), metal);
        }

        private static void CreateMonitor(Transform parent, string name, Vector3 position, Palette p)
        {
            CreateCube(name + " Screen Frame", parent, position, new Vector3(0.95f, 0.62f, 0.12f), p.DarkMetal);
            CreateCube(name + " Screen", parent, position + new Vector3(0f, 0f, 0.068f), new Vector3(0.83f, 0.49f, 0.018f), p.Screen);
            CreateCube(name + " Stand", parent, position + new Vector3(0f, -0.4f, 0f), new Vector3(0.12f, 0.22f, 0.12f), p.Metal);
        }

        private static void CreateArchiveShelf(Transform parent, string name, Vector3 position, Palette p)
        {
            CreateCube(name + " Back", parent, position + new Vector3(0f, 1.15f, 0.0f), new Vector3(1.9f, 2.3f, 0.12f), p.Metal);
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.35f + shelf * 0.56f;
                CreateCube(name + " Shelf " + shelf, parent, position + new Vector3(0f, y, 0.28f), new Vector3(1.9f, 0.08f, 0.62f), p.Metal);
                for (int box = 0; box < 4; box++)
                {
                    float x = -0.68f + box * 0.45f;
                    CreateCube(name + " Archive Box " + shelf + "-" + box, parent,
                        position + new Vector3(x, y + 0.25f, 0.28f), new Vector3(0.34f, 0.38f, 0.42f),
                        (box % 2 == 0) ? p.WoodLight : p.White);
                }
            }
            CreateCube(name + " Left Upright", parent, position + new Vector3(-0.9f, 1.15f, 0.3f), new Vector3(0.08f, 2.3f, 0.65f), p.DarkMetal);
            CreateCube(name + " Right Upright", parent, position + new Vector3(0.9f, 1.15f, 0.3f), new Vector3(0.08f, 2.3f, 0.65f), p.DarkMetal);
        }

        private static void CreateServerRack(Transform parent, string name, Vector3 position, Palette p)
        {
            CreateCube(name + " Cabinet", parent, position + new Vector3(0f, 1.25f, 0f), new Vector3(1.25f, 2.5f, 0.85f), p.DarkMetal);
            for (int i = 0; i < 5; i++)
            {
                float y = 0.35f + i * 0.44f;
                CreateCube(name + " Server Unit " + i, parent, position + new Vector3(0f, y, 0.44f), new Vector3(1.0f, 0.32f, 0.06f), p.Metal);
                CreateCube(name + " Status Light " + i, parent, position + new Vector3(0.39f, y, 0.48f), new Vector3(0.08f, 0.07f, 0.025f), p.Plant);
            }
        }

        private static void CreatePlant(Transform parent, string name, Vector3 position, Palette p)
        {
            CreateCube(name + " Pot", parent, position + new Vector3(0f, 0.28f, 0f), new Vector3(0.62f, 0.56f, 0.62f), p.Wood);
            CreateCube(name + " Foliage Core", parent, position + new Vector3(0f, 1.05f, 0f), new Vector3(0.75f, 1.0f, 0.75f), p.Plant);
            CreateCube(name + " Foliage Left", parent, position + new Vector3(-0.32f, 1.0f, 0.05f), new Vector3(0.42f, 0.75f, 0.42f), p.Plant);
            CreateCube(name + " Foliage Right", parent, position + new Vector3(0.3f, 1.15f, -0.05f), new Vector3(0.42f, 0.8f, 0.42f), p.Plant);
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = scale;
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            cube.isStatic = true;
            return cube;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
