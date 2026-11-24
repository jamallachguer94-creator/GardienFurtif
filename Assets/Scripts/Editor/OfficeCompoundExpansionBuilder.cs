#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GardienFurtif.EditorTools
{
    /// <summary>
    /// Adds an outdoor low-rise compound around the existing OfficeEnvironment_Generated.
    /// It does not replace or modify the existing office interior.
    /// Run: Gardien Furtif > Environment > Complete Existing Office Compound.
    /// </summary>
    public static class OfficeCompoundExpansionBuilder
    {
        private const string RootName = "OfficeCompoundExpansion_Generated";
        private const string MaterialsFolder = "Assets/Materials/GeneratedOfficeExpansion";
        private const float WallHeight = 3.35f;
        private const float WallThickness = 0.22f;
        private const float RoadY = 0.045f;

        private enum DoorSide { North, South, East, West }

        private sealed class Palette
        {
            public Material Ground, Road, RoadMark, Path, Wall, WallLight, WallAccent;
            public Material Roof, Door, Wood, Metal, DarkMetal, Glass, WindowFrame;
            public Material Grass, Tree, Bush, Concrete, White, Sign, InteriorFloor;
        }

        [MenuItem("Gardien Furtif/Environment/Complete Existing Office Compound")]
        public static void BuildExpansion()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Rebuild Office Compound Expansion",
                    "The outdoor expansion already exists. Replace only '" + RootName + "'? The original office and all other scene objects will remain untouched.",
                    "Replace expansion", "Cancel");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing);
            }

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "GeneratedOfficeExpansion");
            Palette p = CreatePalette();

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Complete Gardien Furtif Office Compound");

            Transform groundAndRoads = Group("Ground and Internal Roads", root.transform);
            Transform buildings = Group("10 Additional Low-Rise Buildings", root.transform);
            Transform optionalRoofs = Group("Optional Roofs - enable for aerial overview", root.transform);
            Transform perimeter = Group("Perimeter Fence and Main Gate", root.transform);
            Transform landscape = Group("Trees and Courtyards", root.transform);
            Transform streetFurniture = Group("Street Furniture and Parking", root.transform);
            Transform lighting = Group("Outdoor Lighting", root.transform);

            BuildGroundAndRoads(groundAndRoads, p);
            BuildAdditionalBuildings(buildings, optionalRoofs, groundAndRoads, p);
            BuildPerimeter(perimeter, p);
            BuildCourtyards(landscape, p);
            BuildParkingAndVehicles(streetFurniture, p);
            BuildOutdoorLighting(lighting, p);
            optionalRoofs.gameObject.SetActive(false);

            root.transform.position = Vector3.zero;
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Office Compound Expansion Created",
                "The outdoor expansion is ready: 10 additional low-rise buildings, roads, paths, two gatehouses, a perimeter fence, parking, trees and lighting.\n\nThe original office hierarchy was not changed. Save the scene with Ctrl+S. Optional roofs are disabled so you can inspect the buildings from above.",
                "OK");
        }

        private static Palette CreatePalette()
        {
            return new Palette
            {
                Ground = Mat("Expansion_Ground", new Color(0.34f, 0.36f, 0.29f), 0f, 0.08f),
                Road = Mat("Expansion_Road", new Color(0.22f, 0.23f, 0.22f), 0f, 0.16f),
                RoadMark = Mat("Expansion_Road_Marking", new Color(0.82f, 0.78f, 0.65f), 0f, 0.1f),
                Path = Mat("Expansion_Path", new Color(0.58f, 0.55f, 0.48f), 0f, 0.1f),
                Wall = Mat("Expansion_Wall", new Color(0.75f, 0.69f, 0.57f), 0f, 0.15f),
                WallLight = Mat("Expansion_Wall_Light", new Color(0.86f, 0.81f, 0.70f), 0f, 0.12f),
                WallAccent = Mat("Expansion_Wall_Accent", new Color(0.34f, 0.38f, 0.36f), 0f, 0.18f),
                Roof = Mat("Expansion_Roof", new Color(0.42f, 0.23f, 0.18f), 0f, 0.15f),
                Door = Mat("Expansion_Door", new Color(0.25f, 0.17f, 0.12f), 0f, 0.18f),
                Wood = Mat("Expansion_Wood", new Color(0.43f, 0.30f, 0.19f), 0f, 0.18f),
                Metal = Mat("Expansion_Metal", new Color(0.38f, 0.41f, 0.40f), 0.35f, 0.25f),
                DarkMetal = Mat("Expansion_Dark_Metal", new Color(0.10f, 0.13f, 0.13f), 0.3f, 0.22f),
                Glass = Mat("Expansion_Glass", new Color(0.16f, 0.31f, 0.36f), 0.15f, 0.45f),
                WindowFrame = Mat("Expansion_Window_Frame", new Color(0.15f, 0.17f, 0.16f), 0.2f, 0.2f),
                Grass = Mat("Expansion_Grass", new Color(0.26f, 0.34f, 0.20f), 0f, 0.06f),
                Tree = Mat("Expansion_Tree", new Color(0.13f, 0.27f, 0.13f), 0f, 0.08f),
                Bush = Mat("Expansion_Bush", new Color(0.19f, 0.31f, 0.16f), 0f, 0.06f),
                Concrete = Mat("Expansion_Concrete", new Color(0.49f, 0.48f, 0.43f), 0f, 0.12f),
                White = Mat("Expansion_White", new Color(0.88f, 0.85f, 0.77f), 0f, 0.12f),
                Sign = Mat("Expansion_Sign", new Color(0.14f, 0.20f, 0.19f), 0.05f, 0.18f),
                InteriorFloor = Mat("Expansion_Interior_Floor", new Color(0.57f, 0.51f, 0.42f), 0f, 0.1f)
            };
        }

        private static Material Mat(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("HDRP/Lit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("Unity could not find a supported shader for the outdoor environment materials.");
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

        private static void BuildGroundAndRoads(Transform parent, Palette p)
        {
            // The current office footprint is approximately 30 x 22 m, centered at (0,0,0).
            // This ground lies below the office floor, leaving its interior geometry untouched.
            Cube("Compound Ground - 132 x 104 m", parent, new Vector3(0f, -0.35f, 0f), new Vector3(132f, 0.50f, 104f), p.Ground);

            // A simple ring-like road layout around, not through, the existing central office.
            Cube("North Cross Road", parent, new Vector3(0f, RoadY, 20f), new Vector3(108f, 0.09f, 5.2f), p.Road);
            Cube("South Cross Road", parent, new Vector3(0f, RoadY, -20f), new Vector3(108f, 0.09f, 5.2f), p.Road);
            Cube("West Side Road", parent, new Vector3(-31f, RoadY, 0f), new Vector3(5.2f, 0.09f, 86f), p.Road);
            Cube("East Side Road", parent, new Vector3(31f, RoadY, 0f), new Vector3(5.2f, 0.09f, 86f), p.Road);
            Cube("Main Entrance Access Road", parent, new Vector3(0f, RoadY, -34f), new Vector3(8f, 0.09f, 28f), p.Road);

            // Entrance walkway connects the existing office's south doorway to the south cross road.
            Path(parent, "Main Office Entrance Walk", new Vector3(-10f, 0.10f, -16.8f), new Vector3(2.5f, 0.12f, 11.6f), p.Path);

            // A few painted lane markings give the entrance and junctions scale.
            for (int i = 0; i < 6; i++)
                Cube("Entrance Lane Mark " + (i + 1), parent, new Vector3(0f, 0.10f, -45f + i * 2.2f), new Vector3(0.18f, 0.025f, 1.0f), p.RoadMark);
            Cube("North Junction Marking", parent, new Vector3(0f, 0.102f, 20f), new Vector3(1.0f, 0.025f, 4.6f), p.RoadMark);
            Cube("South Junction Marking", parent, new Vector3(0f, 0.102f, -20f), new Vector3(1.0f, 0.025f, 4.6f), p.RoadMark);
        }

        private static void BuildAdditionalBuildings(Transform buildings, Transform roofs, Transform paths, Palette p)
        {
            // North row. Doors face south toward the north cross road.
            SmallBuilding(buildings, roofs, paths, "01 - Records Annex", new Vector2(-43f, 35f), 12f, 9f, DoorSide.South, p, 0);
            SmallBuilding(buildings, roofs, paths, "02 - Training Block", new Vector2(-17f, 35f), 12f, 9f, DoorSide.South, p, 1);
            SmallBuilding(buildings, roofs, paths, "03 - Operations Annex", new Vector2(17f, 35f), 12f, 9f, DoorSide.South, p, 2);
            SmallBuilding(buildings, roofs, paths, "04 - Research Block", new Vector2(43f, 35f), 12f, 9f, DoorSide.South, p, 3);

            // West and east clusters. Their entries face the side roads and central office.
            SmallBuilding(buildings, roofs, paths, "05 - Workshop", new Vector2(-43f, 8f), 12f, 10f, DoorSide.East, p, 4);
            SmallBuilding(buildings, roofs, paths, "06 - Storage Building", new Vector2(-43f, -9f), 12f, 10f, DoorSide.East, p, 5);
            SmallBuilding(buildings, roofs, paths, "07 - Staff Building", new Vector2(43f, 8f), 12f, 10f, DoorSide.West, p, 6);
            SmallBuilding(buildings, roofs, paths, "08 - Utility Building", new Vector2(43f, -9f), 12f, 10f, DoorSide.West, p, 7);

            // South row. Doors face north toward the south cross road.
            SmallBuilding(buildings, roofs, paths, "09 - Maintenance Block", new Vector2(-17f, -35f), 12f, 9f, DoorSide.North, p, 8);
            SmallBuilding(buildings, roofs, paths, "10 - Logistics Block", new Vector2(17f, -35f), 12f, 9f, DoorSide.North, p, 9);

            // Two small gatehouses near the main entrance.
            SmallBuilding(buildings, roofs, paths, "11 - West Gatehouse", new Vector2(-10.5f, -44f), 6.5f, 5.5f, DoorSide.North, p, 10);
            SmallBuilding(buildings, roofs, paths, "12 - East Gatehouse", new Vector2(10.5f, -44f), 6.5f, 5.5f, DoorSide.North, p, 11);
        }

        private static void SmallBuilding(Transform parent, Transform roofs, Transform paths, string name,
            Vector2 center, float width, float depth, DoorSide doorSide, Palette p, int variant)
        {
            Transform building = Group(name, parent);
            float left = center.x - width * 0.5f;
            float right = center.x + width * 0.5f;
            float front = center.y - depth * 0.5f;
            float back = center.y + depth * 0.5f;

            Cube("Floor", building, new Vector3(center.x, -0.045f, center.y), new Vector3(width, 0.12f, depth), p.InteriorFloor);

            if (doorSide == DoorSide.South)
            {
                WallX(building, "Front Wall Left", left, center.x - 0.95f, front, p);
                WallX(building, "Front Wall Right", center.x + 0.95f, right, front, p);
                DoorHeaderX(building, center.x, front, p);
                WallX(building, "Rear Wall", left, right, back, p);
                WallZ(building, "West Wall", left, front, back, p);
                WallZ(building, "East Wall", right, front, back, p);
                DoorFrameX(building, center.x, front, p);
                ConnectPath(paths, name + " Entrance Path", new Vector3(center.x, 0.10f, front - 3.5f), new Vector3(2.2f, 0.12f, 7f), p.Path);
            }
            else if (doorSide == DoorSide.North)
            {
                WallX(building, "Front Wall Left", left, center.x - 0.95f, back, p);
                WallX(building, "Front Wall Right", center.x + 0.95f, right, back, p);
                DoorHeaderX(building, center.x, back, p);
                WallX(building, "Rear Wall", left, right, front, p);
                WallZ(building, "West Wall", left, front, back, p);
                WallZ(building, "East Wall", right, front, back, p);
                DoorFrameX(building, center.x, back, p);
                ConnectPath(paths, name + " Entrance Path", new Vector3(center.x, 0.10f, back + 3.5f), new Vector3(2.2f, 0.12f, 7f), p.Path);
            }
            else if (doorSide == DoorSide.East)
            {
                WallZ(building, "Front Wall Lower", right, front, center.y - 0.95f, p);
                WallZ(building, "Front Wall Upper", right, center.y + 0.95f, back, p);
                DoorHeaderZ(building, right, center.y, p);
                WallZ(building, "Rear Wall", left, front, back, p);
                WallX(building, "North Wall", left, right, back, p);
                WallX(building, "South Wall", left, right, front, p);
                DoorFrameZ(building, right, center.y, p);
                ConnectPath(paths, name + " Entrance Path", new Vector3(right + 3.5f, 0.10f, center.y), new Vector3(7f, 0.12f, 2.2f), p.Path);
            }
            else // West
            {
                WallZ(building, "Front Wall Lower", left, front, center.y - 0.95f, p);
                WallZ(building, "Front Wall Upper", left, center.y + 0.95f, back, p);
                DoorHeaderZ(building, left, center.y, p);
                WallZ(building, "Rear Wall", right, front, back, p);
                WallX(building, "North Wall", left, right, back, p);
                WallX(building, "South Wall", left, right, front, p);
                DoorFrameZ(building, left, center.y, p);
                ConnectPath(paths, name + " Entrance Path", new Vector3(left - 3.5f, 0.10f, center.y), new Vector3(7f, 0.12f, 2.2f), p.Path);
            }

            // Each annex has simple window blocks and an interior prop, with clear floor space for future gameplay.
            AddWindows(building, center, width, depth, doorSide, p);
            AddInteriorProp(building, center, p, variant);
            AddSign(building, center, doorSide, p);

            GameObject roof = Cube(name + " Roof", roofs, new Vector3(center.x, WallHeight + 0.12f, center.y),
                new Vector3(width + 0.45f, 0.24f, depth + 0.45f), p.Roof);
            roof.isStatic = true;
        }

        private static void AddWindows(Transform parent, Vector2 c, float width, float depth, DoorSide doorSide, Palette p)
        {
            float y = 2.0f;
            float winW = Mathf.Min(2.1f, width * 0.22f);
            float winH = 1.0f;
            // Windows are decorative overlays; the low-rise blockout walls remain solid for now.
            if (doorSide == DoorSide.North || doorSide == DoorSide.South)
            {
                float z = doorSide == DoorSide.North ? c.y + depth * 0.5f + 0.125f : c.y - depth * 0.5f - 0.125f;
                Window(parent, "Front Window Left", new Vector3(c.x - width * 0.30f, y, z), new Vector3(winW, winH, 0.08f), p);
                Window(parent, "Front Window Right", new Vector3(c.x + width * 0.30f, y, z), new Vector3(winW, winH, 0.08f), p);
            }
            else
            {
                float x = doorSide == DoorSide.East ? c.x + width * 0.5f + 0.125f : c.x - width * 0.5f - 0.125f;
                Window(parent, "Side Window Front", new Vector3(x, y, c.y - depth * 0.25f), new Vector3(0.08f, winH, Mathf.Min(2.2f, depth * 0.22f)), p);
                Window(parent, "Side Window Rear", new Vector3(x, y, c.y + depth * 0.25f), new Vector3(0.08f, winH, Mathf.Min(2.2f, depth * 0.22f)), p);
            }
        }

        private static void Window(Transform parent, string name, Vector3 position, Vector3 size, Palette p)
        {
            Cube(name + " Frame", parent, position, size, p.WindowFrame);
            Vector3 glassSize = new Vector3(Mathf.Max(0.05f, size.x - 0.18f), Mathf.Max(0.05f, size.y - 0.18f), Mathf.Max(0.035f, size.z - 0.04f));
            Cube(name + " Glass", parent, position + new Vector3(0f, 0f, -0.015f), glassSize, p.Glass);
        }

        private static void AddInteriorProp(Transform parent, Vector2 c, Palette p, int variant)
        {
            Vector3 pos = new Vector3(c.x, 0f, c.y + 0.8f);
            if (variant % 4 == 0)
            {
                Cube("Storage Cabinet", parent, pos + new Vector3(0f, 1.0f, 0f), new Vector3(2.0f, 2.0f, 0.7f), p.Metal);
                Cube("Work Table", parent, pos + new Vector3(2.5f, 0.78f, -1.0f), new Vector3(2.0f, 0.12f, 1.0f), p.Wood);
            }
            else if (variant % 4 == 1)
            {
                Cube("Meeting Table", parent, pos + new Vector3(0f, 0.78f, 0f), new Vector3(3.2f, 0.12f, 1.4f), p.Wood);
                Cube("Display Panel", parent, pos + new Vector3(0f, 1.9f, 3.6f), new Vector3(2.5f, 1.1f, 0.12f), p.DarkMetal);
            }
            else if (variant % 4 == 2)
            {
                for (int i = 0; i < 3; i++)
                    Cube("Work Desk " + (i + 1), parent, pos + new Vector3(-3f + i * 2.7f, 0.78f, 0f), new Vector3(1.8f, 0.12f, 0.9f), p.Wood);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    Cube("Equipment Rack " + (i + 1), parent, pos + new Vector3(-3f + i * 2.7f, 1.0f, 2.2f), new Vector3(1.0f, 2.0f, 0.65f), p.DarkMetal);
            }
        }

        private static void AddSign(Transform parent, Vector2 c, DoorSide side, Palette p)
        {
            Vector3 position;
            Vector3 size;
            if (side == DoorSide.North || side == DoorSide.South)
            {
                float z = side == DoorSide.North ? c.y + 4.65f : c.y - 4.65f;
                position = new Vector3(c.x, 2.85f, z);
                size = new Vector3(2.6f, 0.45f, 0.12f);
            }
            else
            {
                float x = side == DoorSide.East ? c.x + 6.2f : c.x - 6.2f;
                position = new Vector3(x, 2.85f, c.y);
                size = new Vector3(0.12f, 0.45f, 2.6f);
            }
            Cube("Building Sign Panel", parent, position, size, p.Sign);
        }

        private static void BuildPerimeter(Transform parent, Palette p)
        {
            const float x = 62f, z = 48f, h = 2.25f, t = 0.18f;
            Cube("West Perimeter Wall", parent, new Vector3(-x, h * 0.5f, 0f), new Vector3(t, h, 96f), p.WallAccent);
            Cube("East Perimeter Wall", parent, new Vector3(x, h * 0.5f, 0f), new Vector3(t, h, 96f), p.WallAccent);
            Cube("North Perimeter Wall", parent, new Vector3(0f, h * 0.5f, z), new Vector3(124f, h, t), p.WallAccent);
            Cube("South Perimeter Wall Left", parent, new Vector3(-34f, h * 0.5f, -z), new Vector3(56f, h, t), p.WallAccent);
            Cube("South Perimeter Wall Right", parent, new Vector3(34f, h * 0.5f, -z), new Vector3(56f, h, t), p.WallAccent);

            for (int i = 0; i <= 20; i++)
            {
                float px = -62f + i * 6.2f;
                Cube("North Perimeter Post " + i, parent, new Vector3(px, 1.15f, z), new Vector3(0.28f, 2.3f, 0.28f), p.Metal);
                if (Mathf.Abs(px) > 6.5f)
                    Cube("South Perimeter Post " + i, parent, new Vector3(px, 1.15f, -z), new Vector3(0.28f, 2.3f, 0.28f), p.Metal);
            }
            for (int i = 0; i <= 15; i++)
            {
                float pz = -48f + i * 6.4f;
                Cube("West Perimeter Post " + i, parent, new Vector3(-x, 1.15f, pz), new Vector3(0.28f, 2.3f, 0.28f), p.Metal);
                Cube("East Perimeter Post " + i, parent, new Vector3(x, 1.15f, pz), new Vector3(0.28f, 2.3f, 0.28f), p.Metal);
            }

            Cube("Entrance Gate Pillar Left", parent, new Vector3(-6.5f, 1.6f, -48f), new Vector3(0.8f, 3.2f, 0.8f), p.WallLight);
            Cube("Entrance Gate Pillar Right", parent, new Vector3(6.5f, 1.6f, -48f), new Vector3(0.8f, 3.2f, 0.8f), p.WallLight);
            Cube("Entrance Gate Header", parent, new Vector3(0f, 3.15f, -48f), new Vector3(13.2f, 0.35f, 0.5f), p.Roof);
            Cube("Gate Leaf West", parent, new Vector3(-3.5f, 1.05f, -48f), new Vector3(0.12f, 2.1f, 0.25f), p.Metal);
            Cube("Gate Leaf East", parent, new Vector3(3.5f, 1.05f, -48f), new Vector3(0.12f, 2.1f, 0.25f), p.Metal);
            Cube("Main Entrance Sign", parent, new Vector3(0f, 2.35f, -47.65f), new Vector3(4.6f, 0.65f, 0.16f), p.Sign);
        }

        private static void BuildCourtyards(Transform parent, Palette p)
        {
            // Small planted courtyards between clusters; none of these props block the central roads.
            Lawn(parent, "North West Courtyard", new Vector3(-30f, 0.025f, 36f), new Vector3(7f, 0.05f, 7f), p.Grass);
            Lawn(parent, "North East Courtyard", new Vector3(30f, 0.025f, 36f), new Vector3(7f, 0.05f, 7f), p.Grass);
            Lawn(parent, "South West Courtyard", new Vector3(-30f, 0.025f, -35f), new Vector3(7f, 0.05f, 7f), p.Grass);
            Lawn(parent, "South East Courtyard", new Vector3(30f, 0.025f, -35f), new Vector3(7f, 0.05f, 7f), p.Grass);

            Vector3[] trees =
            {
                new Vector3(-54f,0f,34f), new Vector3(-54f,0f,16f), new Vector3(-54f,0f,-16f), new Vector3(-54f,0f,-34f),
                new Vector3(54f,0f,34f), new Vector3(54f,0f,16f), new Vector3(54f,0f,-16f), new Vector3(54f,0f,-34f),
                new Vector3(-27f,0f,34f), new Vector3(27f,0f,34f), new Vector3(-27f,0f,-34f), new Vector3(27f,0f,-34f),
                new Vector3(-18f,0f,15f), new Vector3(18f,0f,15f), new Vector3(-18f,0f,-15f), new Vector3(18f,0f,-15f)
            };
            for (int i = 0; i < trees.Length; i++) Tree(parent, "Campus Tree " + (i + 1), trees[i], p);

            Vector3[] bushes =
            {
                new Vector3(-30f,0f,31f), new Vector3(30f,0f,31f), new Vector3(-30f,0f,-30f), new Vector3(30f,0f,-30f),
                new Vector3(-54f,0f,0f), new Vector3(54f,0f,0f)
            };
            for (int i = 0; i < bushes.Length; i++)
                Sphere(parent, "Landscaping Bush " + (i + 1), bushes[i] + Vector3.up * 0.65f, new Vector3(1.8f, 1.3f, 1.8f), p.Bush);
        }

        private static void BuildParkingAndVehicles(Transform parent, Palette p)
        {
            Cube("Parking Lot Surface", parent, new Vector3(43f, 0.075f, -34f), new Vector3(17f, 0.12f, 12f), p.Concrete);
            for (int i = 0; i < 5; i++)
                Cube("Parking Bay Line " + (i + 1), parent, new Vector3(36.5f + i * 3.2f, 0.15f, -34f), new Vector3(0.08f, 0.025f, 9.5f), p.White);
            Car(parent, "Parked Car A", new Vector3(38.2f, 0.16f, -34f), p);
            Car(parent, "Parked Car B", new Vector3(44.6f, 0.16f, -34f), p);
            Car(parent, "Parked Car C", new Vector3(51.0f, 0.16f, -34f), p);
        }

        private static void BuildOutdoorLighting(Transform parent, Palette p)
        {
            Vector3[] positions =
            {
                new Vector3(-55f,0f,24f), new Vector3(-55f,0f,0f), new Vector3(-55f,0f,-24f),
                new Vector3(55f,0f,24f), new Vector3(55f,0f,0f), new Vector3(55f,0f,-24f),
                new Vector3(-36f,0f,24f), new Vector3(36f,0f,24f), new Vector3(-36f,0f,-24f), new Vector3(36f,0f,-24f),
                new Vector3(15f,0f,-42f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 pos = positions[i];
                Cylinder(parent, "Street Lamp Pole " + (i + 1), pos + new Vector3(0f, 2.0f, 0f), new Vector3(0.16f, 2f, 0.16f), p.DarkMetal);
                Cube("Street Lamp Head " + (i + 1), parent, pos + new Vector3(0f, 4.0f, 0f), new Vector3(0.75f, 0.18f, 0.35f), p.White);
                GameObject lightObject = new GameObject("Street Light " + (i + 1));
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.position = pos + new Vector3(0f, 3.8f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 10f;
                light.intensity = 0.8f;
                light.color = new Color(1f, 0.87f, 0.68f);
                light.shadows = LightShadows.None;
            }
        }

        private static void ConnectPath(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            Cube(name, parent, center, size, material);
        }

        private static void Path(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            Cube(name, parent, center, size, material);
        }

        private static void Lawn(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            Cube(name, parent, center, size, material);
        }

        private static void Tree(Transform parent, string name, Vector3 position, Palette p)
        {
            Cylinder(parent, name + " Trunk", position + new Vector3(0f, 1.25f, 0f), new Vector3(0.32f, 1.25f, 0.32f), p.Wood);
            Sphere(parent, name + " Crown", position + new Vector3(0f, 3.2f, 0f), new Vector3(2.1f, 2.3f, 2.1f), p.Tree);
        }

        private static void Car(Transform parent, string name, Vector3 position, Palette p)
        {
            Cube(name + " Body", parent, position + new Vector3(0f, 0.65f, 0f), new Vector3(2.4f, 0.65f, 4.4f), p.WallAccent);
            Cube(name + " Cabin", parent, position + new Vector3(0f, 1.18f, 0.15f), new Vector3(1.75f, 0.72f, 2.25f), p.Glass);
            for (int side = -1; side <= 1; side += 2)
            for (int axle = -1; axle <= 1; axle += 2)
                Cylinder(parent, name + " Wheel", position + new Vector3(side * 1.18f, 0.42f, axle * 1.35f), new Vector3(0.38f, 0.18f, 0.38f), p.DarkMetal, Quaternion.Euler(0f, 0f, 90f));
        }

        private static void DoorFrameX(Transform parent, float x, float z, Palette p)
        {
            Cube("Door Left Post", parent, new Vector3(x - 0.9f, WallHeight * 0.5f, z), new Vector3(0.14f, WallHeight, 0.22f), p.WindowFrame);
            Cube("Door Right Post", parent, new Vector3(x + 0.9f, WallHeight * 0.5f, z), new Vector3(0.14f, WallHeight, 0.22f), p.WindowFrame);
            Cube("Door Header", parent, new Vector3(x, WallHeight - 0.08f, z), new Vector3(1.95f, 0.16f, 0.24f), p.WindowFrame);
        }

        private static void DoorFrameZ(Transform parent, float x, float z, Palette p)
        {
            Cube("Door Lower Post", parent, new Vector3(x, WallHeight * 0.5f, z - 0.9f), new Vector3(0.22f, WallHeight, 0.14f), p.WindowFrame);
            Cube("Door Upper Post", parent, new Vector3(x, WallHeight * 0.5f, z + 0.9f), new Vector3(0.22f, WallHeight, 0.14f), p.WindowFrame);
            Cube("Door Header", parent, new Vector3(x, WallHeight - 0.08f, z), new Vector3(0.24f, 0.16f, 1.95f), p.WindowFrame);
        }

        private static void DoorHeaderX(Transform parent, float x, float z, Palette p)
        {
            Cube("Door Header Fill", parent, new Vector3(x, WallHeight - 0.08f, z), new Vector3(1.9f, 0.16f, WallThickness), p.WallLight);
        }

        private static void DoorHeaderZ(Transform parent, float x, float z, Palette p)
        {
            Cube("Door Header Fill", parent, new Vector3(x, WallHeight - 0.08f, z), new Vector3(WallThickness, 0.16f, 1.9f), p.WallLight);
        }

        private static void WallX(Transform parent, string name, float xMin, float xMax, float z, Palette p)
        {
            if (xMax <= xMin) return;
            Cube(name, parent, new Vector3((xMin + xMax) * 0.5f, WallHeight * 0.5f, z), new Vector3(xMax - xMin, WallHeight, WallThickness), p.Wall);
        }

        private static void WallZ(Transform parent, string name, float x, float zMin, float zMax, Palette p)
        {
            if (zMax <= zMin) return;
            Cube(name, parent, new Vector3(x, WallHeight * 0.5f, (zMin + zMax) * 0.5f), new Vector3(WallThickness, WallHeight, zMax - zMin), p.Wall);
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = scale;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            obj.isStatic = true;
            return obj;
        }

        private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            obj.isStatic = true;
            return obj;
        }

        private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.transform.localRotation = rotation ?? Quaternion.identity;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            obj.isStatic = true;
            return obj;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
