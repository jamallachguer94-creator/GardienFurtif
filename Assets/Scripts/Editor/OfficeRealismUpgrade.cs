#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GardienFurtif.EditorTools
{
    /// <summary>
    /// Adds a restrained, more realistic architectural material palette to the generated
    /// Gardien Furtif office and (if present) the outdoor compound expansion.
    /// Safe to re-run: it only updates generated material assets and renderer assignments
    /// under the two named generated roots.
    /// </summary>
    public static class OfficeRealismUpgrade
    {
        private const string MaterialFolder = "Assets/Materials/GardienFurtifRealism";
        private const string TextureFolder = MaterialFolder + "/Textures";
        private const string OfficeRootName = "OfficeEnvironment_Generated";
        private const string ExpansionRootName = "OfficeCompoundExpansion_Generated";
        private const int TextureSize = 512;

        private enum SurfaceKind { Plaster, Concrete, Asphalt, RoofTile, Wood, Soil, Grass }

        private static readonly Dictionary<string, Material> MaterialMap = new Dictionary<string, Material>();

        [MenuItem("Gardien Furtif/Environment/Apply Realistic Materials & Colors")]
        public static void ApplyRealisticMaterials()
        {
            GameObject officeRoot = GameObject.Find(OfficeRootName);
            GameObject expansionRoot = GameObject.Find(ExpansionRootName);

            if (officeRoot == null && expansionRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Generated Environment Not Found",
                    "I could not find OfficeEnvironment_Generated or OfficeCompoundExpansion_Generated in the open scene. Build the office first, then run this command again.",
                    "OK");
                return;
            }

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "GardienFurtifRealism");
            EnsureFolder(MaterialFolder, "Textures");

            MaterialMap.Clear();
            BuildMaterials();

            int changed = 0;
            if (officeRoot != null) changed += ApplyToHierarchy(officeRoot.transform);
            if (expansionRoot != null) changed += ApplyToHierarchy(expansionRoot.transform);
            if (expansionRoot != null) ApplyBuildingFacadeVariants(expansionRoot.transform);

            ApplySceneLighting(officeRoot != null ? officeRoot.transform : null,
                               expansionRoot != null ? expansionRoot.transform : null);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string roots = (officeRoot != null ? "Main office" : "") +
                           (officeRoot != null && expansionRoot != null ? " + " : "") +
                           (expansionRoot != null ? "outdoor compound" : "");

            EditorUtility.DisplayDialog(
                "Realistic Materials Applied",
                "Applied a natural architectural palette to " + roots + ".\n\n" +
                "Updated renderers: " + changed + "\n" +
                "Materials and procedural PBR-style surface maps were saved under " + MaterialFolder + ".\n\n" +
                "Save the scene with Ctrl+S. Inspect the result in Scene view and use a low, angled camera view to judge the wall and roof materials.",
                "OK");
        }

        private static void BuildMaterials()
        {
            Texture2D plaster = CreateSurfaceTexture("Plaster_Albedo", SurfaceKind.Plaster, false);
            Texture2D plasterNormal = CreateSurfaceTexture("Plaster_Normal", SurfaceKind.Plaster, true);
            Texture2D concrete = CreateSurfaceTexture("Concrete_Albedo", SurfaceKind.Concrete, false);
            Texture2D concreteNormal = CreateSurfaceTexture("Concrete_Normal", SurfaceKind.Concrete, true);
            Texture2D asphalt = CreateSurfaceTexture("Asphalt_Albedo", SurfaceKind.Asphalt, false);
            Texture2D asphaltNormal = CreateSurfaceTexture("Asphalt_Normal", SurfaceKind.Asphalt, true);
            Texture2D roof = CreateSurfaceTexture("Terracotta_Roof_Albedo", SurfaceKind.RoofTile, false);
            Texture2D roofNormal = CreateSurfaceTexture("Terracotta_Roof_Normal", SurfaceKind.RoofTile, true);
            Texture2D wood = CreateSurfaceTexture("Natural_Wood_Albedo", SurfaceKind.Wood, false);
            Texture2D woodNormal = CreateSurfaceTexture("Natural_Wood_Normal", SurfaceKind.Wood, true);
            Texture2D soil = CreateSurfaceTexture("Dry_Ground_Albedo", SurfaceKind.Soil, false);
            Texture2D grass = CreateSurfaceTexture("Olive_Grass_Albedo", SurfaceKind.Grass, false);

            // Main office: warm plaster, stone/concrete flooring, walnut and muted fabrics.
            Add("Office_Walls", MakeMaterial("GF_Office_Warm_Plaster", Hex("D8D0BE"), plaster, plasterNormal, 0.12f, 0f, new Vector2(2f, 2f)));
            Add("Office_Wall_Accent", MakeMaterial("GF_Office_Sage_Accent", Hex("59665B"), plaster, plasterNormal, 0.14f, 0f, new Vector2(2f, 2f)));
            Add("Office_Floor", MakeMaterial("GF_Office_Stone_Floor", Hex("B8B2A5"), concrete, concreteNormal, 0.22f, 0f, new Vector2(4f, 4f)));
            Add("Office_Corridor", MakeMaterial("GF_Office_Corridor_Tile", Hex("777A76"), concrete, concreteNormal, 0.20f, 0f, new Vector2(5f, 5f)));
            Add("Office_Trim", MakeMaterial("GF_Office_Charcoal_Trim", Hex("343A3A"), null, null, 0.20f, 0.12f, Vector2.one));
            Add("Office_Wood", MakeMaterial("GF_Office_Walnut", Hex("725039"), wood, woodNormal, 0.24f, 0f, new Vector2(2f, 1.5f)));
            Add("Office_Wood_Light", MakeMaterial("GF_Office_Oak", Hex("B18A5E"), wood, woodNormal, 0.25f, 0f, new Vector2(2f, 1.5f)));
            Add("Office_Metal", MakeMaterial("GF_Office_Brushed_Metal", Hex("858B89"), concrete, concreteNormal, 0.34f, 0.45f, new Vector2(2f, 2f)));
            Add("Office_Dark_Metal", MakeMaterial("GF_Office_Dark_Metal", Hex("303638"), null, null, 0.26f, 0.35f, Vector2.one));
            Add("Office_Fabric", MakeMaterial("GF_Office_Deep_Charcoal_Fabric", Hex("394B50"), null, null, 0.42f, 0f, Vector2.one));
            Add("Office_Screen", MakeMaterial("GF_Office_Screen", Hex("10252D"), null, null, 0.34f, 0.08f, Vector2.one));
            Add("Office_Plant", MakeMaterial("GF_Office_Plant", Hex("465B3D"), grass, null, 0.16f, 0f, new Vector2(2f, 2f)));
            Add("Office_White", MakeMaterial("GF_Office_Warm_White", Hex("E8E2D4"), null, null, 0.26f, 0f, Vector2.one));
            Add("Office_Glass_Blue", MakeMaterial("GF_Office_Glass", Hex("52747A"), null, null, 0.72f, 0.12f, Vector2.one));

            // Outdoor compound: varied plaster facades, terracotta roofs, asphalt and paving.
            Material outdoorWall = MakeMaterial("GF_Compound_Plaster_Base", Hex("D8CBB2"), plaster, plasterNormal, 0.12f, 0f, new Vector2(2.4f, 2.4f));
            Add("Expansion_Wall", outdoorWall);
            Add("Expansion_Wall_Light", MakeMaterial("GF_Compound_Light_Stone", Hex("E4DCCB"), plaster, plasterNormal, 0.12f, 0f, new Vector2(2.4f, 2.4f)));
            Add("Expansion_Wall_Accent", MakeMaterial("GF_Compound_Slate_Accent", Hex("626B66"), concrete, concreteNormal, 0.20f, 0.05f, new Vector2(2f, 2f)));
            Add("Expansion_Ground", MakeMaterial("GF_Compound_Dry_Earth", Hex("9B8D6E"), soil, null, 0.08f, 0f, new Vector2(18f, 18f)));
            Add("Expansion_Road", MakeMaterial("GF_Compound_Asphalt", Hex("484B4B"), asphalt, asphaltNormal, 0.12f, 0f, new Vector2(16f, 16f)));
            Add("Expansion_Road_Marking", MakeMaterial("GF_Compound_Road_Paint", Hex("D6D0B9"), concrete, null, 0.12f, 0f, new Vector2(2f, 2f)));
            Add("Expansion_Path", MakeMaterial("GF_Compound_Paving", Hex("B9B09D"), concrete, concreteNormal, 0.16f, 0f, new Vector2(5f, 5f)));
            Add("Expansion_Roof", MakeMaterial("GF_Compound_Terracotta_Roof", Hex("A95F43"), roof, roofNormal, 0.18f, 0f, new Vector2(3f, 3f)));
            Add("Expansion_Door", MakeMaterial("GF_Compound_Dark_Wood_Door", Hex("503729"), wood, woodNormal, 0.18f, 0f, new Vector2(2f, 1.5f)));
            Add("Expansion_Wood", MakeMaterial("GF_Compound_Natural_Wood", Hex("94704A"), wood, woodNormal, 0.24f, 0f, new Vector2(2f, 1.5f)));
            Add("Expansion_Metal", MakeMaterial("GF_Compound_Painted_Metal", Hex("626A68"), concrete, concreteNormal, 0.28f, 0.3f, new Vector2(2f, 2f)));
            Add("Expansion_Dark_Metal", MakeMaterial("GF_Compound_Dark_Metal", Hex("303638"), null, null, 0.24f, 0.35f, Vector2.one));
            Add("Expansion_Glass", MakeMaterial("GF_Compound_Tinted_Glass", Hex("526F76"), null, null, 0.72f, 0.10f, Vector2.one));
            Add("Expansion_Window_Frame", MakeMaterial("GF_Compound_Window_Frame", Hex("343B3B"), null, null, 0.24f, 0.18f, Vector2.one));
            Add("Expansion_Grass", MakeMaterial("GF_Compound_Olive_Grass", Hex("687044"), grass, null, 0.08f, 0f, new Vector2(10f, 10f)));
            Add("Expansion_Tree", MakeMaterial("GF_Compound_Tree_Foliage", Hex("40573A"), grass, null, 0.10f, 0f, new Vector2(3f, 3f)));
            Add("Expansion_Bush", MakeMaterial("GF_Compound_Bush_Foliage", Hex("536742"), grass, null, 0.10f, 0f, new Vector2(3f, 3f)));
            Add("Expansion_Concrete", MakeMaterial("GF_Compound_Concrete", Hex("A7A294"), concrete, concreteNormal, 0.18f, 0f, new Vector2(6f, 6f)));
            Add("Expansion_White", MakeMaterial("GF_Compound_Warm_White", Hex("E5DFD1"), null, null, 0.20f, 0f, Vector2.one));
            Add("Expansion_Sign", MakeMaterial("GF_Compound_Sign_Dark_Green", Hex("34483F"), null, null, 0.20f, 0f, Vector2.one));
            Add("Expansion_Interior_Floor", MakeMaterial("GF_Compound_Interior_Terrazzo", Hex("B6AA94"), concrete, concreteNormal, 0.18f, 0f, new Vector2(4f, 4f)));
        }

        private static int ApplyToHierarchy(Transform root)
        {
            int count = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null) continue;
                string sourceName = renderer.sharedMaterial.name.Replace(" (Instance)", "");
                Material replacement;
                if (MaterialMap.TryGetValue(sourceName, out replacement))
                {
                    renderer.sharedMaterial = replacement;
                    EditorUtility.SetDirty(renderer);
                    count++;
                }
            }
            return count;
        }

        private static void ApplyBuildingFacadeVariants(Transform expansionRoot)
        {
            Transform buildings = expansionRoot.Find("10 Additional Low-Rise Buildings");
            if (buildings == null) return;

            Color[] facadeColors =
            {
                Hex("D8CBB2"), // warm sand
                Hex("CFC5B1"), // pale limestone
                Hex("C5C5B6"), // soft grey-beige
                Hex("D9D0C0"), // ivory
                Hex("C8B69B"), // muted sandstone
                Hex("BFC0AE"), // pale sage-grey
                Hex("D4C4B3"), // muted clay beige
                Hex("C9C1B2")  // warm grey
            };

            Material[] variants = new Material[facadeColors.Length];
            Texture2D plaster = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Plaster_Albedo.png");
            Texture2D plasterNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Plaster_Normal.png");
            for (int i = 0; i < variants.Length; i++)
                variants[i] = MakeMaterial("GF_Compound_Facade_Variant_" + (i + 1).ToString("00"), facadeColors[i], plaster, plasterNormal, 0.12f, 0f, new Vector2(2.4f, 2.4f));

            for (int b = 0; b < buildings.childCount; b++)
            {
                Transform building = buildings.GetChild(b);
                Material variant = variants[b % variants.Length];
                Renderer[] renderers = building.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    if (renderers[r] != null && renderers[r].sharedMaterial != null &&
                        renderers[r].sharedMaterial.name == "GF_Compound_Plaster_Base")
                    {
                        renderers[r].sharedMaterial = variant;
                        EditorUtility.SetDirty(renderers[r]);
                    }
                }
            }
        }

        private static void ApplySceneLighting(Transform officeRoot, Transform expansionRoot)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("DCE2E4");
            RenderSettings.ambientEquatorColor = Hex("B8B8AD");
            RenderSettings.ambientGroundColor = Hex("777568");
            RenderSettings.ambientIntensity = 1.0f;

            if (officeRoot != null) TuneLights(officeRoot);
            if (expansionRoot != null) TuneLights(expansionRoot);
        }

        private static void TuneLights(Transform root)
        {
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null) continue;
                if (light.type == LightType.Directional)
                {
                    light.color = Hex("FFF1DC");
                    light.intensity = Mathf.Clamp(light.intensity, 0.75f, 1.0f);
                    light.shadows = LightShadows.Soft;
                }
                else if (light.type == LightType.Point)
                {
                    light.color = Hex("FFE6C5");
                    light.intensity = Mathf.Clamp(light.intensity, 0.65f, 1.0f);
                }
                EditorUtility.SetDirty(light);
            }
        }

        private static Material MakeMaterial(string name, Color color, Texture2D albedo, Texture2D normal,
            float smoothness, float metallic, Vector2 tiling)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("HDRP/Lit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("Unity could not find a supported material shader.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);

            if (albedo != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
                if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", tiling);
                if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", tiling);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.22f);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateSurfaceTexture(string fileName, SurfaceKind kind, bool normalMap)
        {
            string path = TextureFolder + "/" + fileName + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, normalMap);
            texture.name = fileName;
            Color[] pixels = new Color[TextureSize * TextureSize];
            float du = 1f / (TextureSize - 1);

            for (int y = 0; y < TextureSize; y++)
            {
                float v = y / (float)(TextureSize - 1);
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)(TextureSize - 1);
                    Color pixel;
                    if (normalMap)
                    {
                        float left = SurfaceHeight(kind, u - du, v);
                        float right = SurfaceHeight(kind, u + du, v);
                        float down = SurfaceHeight(kind, u, v - du);
                        float up = SurfaceHeight(kind, u, v + du);
                        Vector3 normal = new Vector3(-(right - left) * 24f, -(up - down) * 24f, 1f).normalized;
                        pixel = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                    }
                    else
                    {
                        float h = SurfaceHeight(kind, u, v);
                        float value;
                        if (kind == SurfaceKind.RoofTile) value = Mathf.Clamp(0.55f + h * 0.48f, 0f, 1f);
                        else value = Mathf.Clamp(0.78f + (h - 0.5f) * 0.36f, 0f, 1f);
                        pixel = new Color(value, value, value, 1f);
                    }
                    pixels[y * TextureSize + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            string absolutePath = Path.GetFullPath(path);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normalMap;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.maxTextureSize = TextureSize;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Periodic value noise: the texture edges tile cleanly without obvious seams.
        private static float SurfaceHeight(SurfaceKind kind, float u, float v)
        {
            u = Repeat01(u);
            v = Repeat01(v);
            float broad = TileNoise(u, v, 5, 11);
            float medium = TileNoise(u, v, 13, 23);
            float fine = TileNoise(u, v, 31, 37);
            float micro = TileNoise(u, v, 67, 41);

            switch (kind)
            {
                case SurfaceKind.Plaster:
                    return broad * 0.42f + medium * 0.34f + fine * 0.18f + micro * 0.06f;
                case SurfaceKind.Concrete:
                    return broad * 0.20f + medium * 0.34f + fine * 0.30f + micro * 0.16f;
                case SurfaceKind.Asphalt:
                    return broad * 0.08f + medium * 0.22f + fine * 0.36f + micro * 0.34f;
                case SurfaceKind.Soil:
                    return broad * 0.48f + medium * 0.28f + fine * 0.18f + micro * 0.06f;
                case SurfaceKind.Grass:
                    return broad * 0.18f + medium * 0.28f + fine * 0.30f + micro * 0.24f;
                case SurfaceKind.Wood:
                    float grain = 0.5f + 0.5f * Mathf.Sin((v * 42f + TileNoise(u, v, 7, 9) * 1.4f) * Mathf.PI * 2f);
                    return Mathf.Clamp01(grain * 0.16f + broad * 0.20f + medium * 0.28f + fine * 0.24f + micro * 0.12f);
                case SurfaceKind.RoofTile:
                    float row = v * 14f;
                    int rowIndex = Mathf.FloorToInt(row);
                    float localY = row - Mathf.Floor(row);
                    float shift = (rowIndex % 2 == 0) ? 0f : 0.5f;
                    float localX = Repeat01(u * 10f + shift);
                    float edge = Mathf.Min(Mathf.Min(localX, 1f - localX), Mathf.Min(localY, 1f - localY));
                    float tileFace = SmoothStep(0.015f, 0.11f, edge);
                    float tileVariation = TileNoise(u, v, 10, 71) * 0.12f;
                    return 0.18f + tileFace * (0.58f + tileVariation) + medium * 0.08f;
                default:
                    return medium;
            }
        }

        private static float TileNoise(float u, float v, int cells, int seed)
        {
            float x = Repeat01(u) * cells;
            float y = Repeat01(v) * cells;
            int x0 = Mathf.FloorToInt(x) % cells;
            int y0 = Mathf.FloorToInt(y) % cells;
            int x1 = (x0 + 1) % cells;
            int y1 = (y0 + 1) % cells;
            float tx = Smooth01(x - Mathf.Floor(x));
            float ty = Smooth01(y - Mathf.Floor(y));
            float a = Hash01(x0, y0, seed);
            float b = Hash01(x1, y0, seed);
            float c = Hash01(x0, y1, seed);
            float d = Hash01(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263 + seed * 1442695041;
                n = (n ^ (n >> 13)) * 1274126177;
                n ^= n >> 16;
                return (n & 0x7fffffff) / 2147483647f;
            }
        }

        private static float Smooth01(float t) { return t * t * (3f - 2f * t); }
        private static float SmoothStep(float min, float max, float value)
        {
            float t = Mathf.Clamp01((value - min) / (max - min));
            return t * t * (3f - 2f * t);
        }
        private static float Repeat01(float value) { return value - Mathf.Floor(value); }

        private static void Add(string sourceName, Material replacement)
        {
            MaterialMap[sourceName] = replacement;
        }

        private static Color Hex(string hex)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString("#" + hex, out color)) return Color.white;
            return color;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
