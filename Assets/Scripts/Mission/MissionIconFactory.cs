using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates clean, original procedural fallback Sprites at runtime and allows the Editor tool
/// to bake them into physical PNG assets under Assets/Sprites/Mission/Icons/.
/// </summary>
public static class MissionIconFactory
{
    public enum IconKind
    {
        GuidePortrait,
        ThiefPortrait,
        VillainPortrait,
        ResultsClock,
        ResultsMoneyBag,
        Dossier,
        ExitDoor,
        Checkmark,
        Lock,
        LootBag,
        CashBundle,
        Laptop,
        Painting,
        Briefcase,
        JewelryBox,
        GoldObject,
        ValuableDocuments,
        StarFilled,
        StarEmpty,
        CircleFill,
        RingOutline,
        ArrowPointer
    }

    private static readonly Dictionary<IconKind, Sprite> Cache = new Dictionary<IconKind, Sprite>();

    public static Sprite GetIcon(IconKind kind)
    {
        if (Cache.TryGetValue(kind, out Sprite cached) && cached != null)
            return cached;

        // Full-resolution, original character art is stored in Resources so it also
        // loads in Play Mode without assigning scene references or creating UI prefabs.
        if (kind == IconKind.ThiefPortrait || kind == IconKind.VillainPortrait)
        {
            Texture2D portraitTexture = Resources.Load<Texture2D>("GardienFurtif/UI/" + kind);
            if (portraitTexture != null)
            {
                Sprite portraitSprite = Sprite.Create(
                    portraitTexture,
                    new Rect(0f, 0f, portraitTexture.width, portraitTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                Cache[kind] = portraitSprite;
                return portraitSprite;
            }
        }

        Sprite created = CreateIconSprite(kind, 64);
        Cache[kind] = created;
        return created;
    }

    public static Texture2D CreateIconTexture(IconKind kind, int size = 128)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = $"Icon_{kind}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        DrawIconPixels(kind, pixels, size);
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Sprite CreateIconSprite(IconKind kind, int size)
    {
        Texture2D tex = CreateIconTexture(kind, size);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void DrawIconPixels(IconKind kind, Color[] p, int s)
    {
        float center = (s - 1) * 0.5f;

        switch (kind)
        {
            case IconKind.CircleFill:
                FillCircle(p, s, center, center, s * 0.46f, Color.white);
                break;

            case IconKind.RingOutline:
                FillRing(p, s, center, center, s * 0.34f, s * 0.46f, Color.white);
                break;

            case IconKind.ArrowPointer:
                FillCircle(p, s, center, center, s * 0.44f, new Color(0.1f, 0.14f, 0.2f, 0.92f));
                FillTriangle(p, s,
                    new Vector2(center, s * 0.78f),
                    new Vector2(s * 0.26f, s * 0.28f),
                    new Vector2(s * 0.74f, s * 0.28f),
                    Color.white);
                break;

            case IconKind.GuidePortrait:
                FillCircle(p, s, center, center, s * 0.47f, new Color(0.11f, 0.22f, 0.34f, 1f));
                FillRing(p, s, center, center, s * 0.43f, s * 0.47f, new Color(0.35f, 0.78f, 1f, 1f));
                FillEllipse(p, s, center, s * 0.18f, s * 0.30f, s * 0.18f, new Color(0.18f, 0.32f, 0.48f, 1f));
                FillCircle(p, s, center, s * 0.54f, s * 0.18f, new Color(0.92f, 0.82f, 0.72f, 1f));
                FillRect(p, s, s * 0.33f, s * 0.50f, s * 0.34f, s * 0.09f, new Color(0.12f, 0.16f, 0.22f, 1f));
                FillCircle(p, s, s * 0.42f, s * 0.545f, s * 0.04f, new Color(0.3f, 0.95f, 1f, 1f));
                FillCircle(p, s, s * 0.58f, s * 0.545f, s * 0.04f, new Color(0.3f, 0.95f, 1f, 1f));
                FillRect(p, s, s * 0.24f, s * 0.66f, s * 0.52f, s * 0.06f, new Color(0.14f, 0.2f, 0.28f, 1f));
                FillRect(p, s, s * 0.32f, s * 0.71f, s * 0.36f, s * 0.12f, new Color(0.18f, 0.26f, 0.36f, 1f));
                break;

            case IconKind.Dossier:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.12f, 0.42f, 0.85f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(0.55f, 0.82f, 1f, 1f));
                FillRect(p, s, s * 0.24f, s * 0.26f, s * 0.52f, s * 0.42f, new Color(0.95f, 0.98f, 1f, 1f));
                FillRect(p, s, s * 0.24f, s * 0.66f, s * 0.24f, s * 0.08f, new Color(0.82f, 0.92f, 1f, 1f));
                FillRect(p, s, s * 0.31f, s * 0.54f, s * 0.38f, s * 0.04f, new Color(0.15f, 0.45f, 0.88f, 1f));
                FillRect(p, s, s * 0.31f, s * 0.44f, s * 0.38f, s * 0.04f, new Color(0.15f, 0.45f, 0.88f, 1f));
                FillRect(p, s, s * 0.31f, s * 0.34f, s * 0.26f, s * 0.04f, new Color(0.9f, 0.25f, 0.25f, 1f));
                break;

            case IconKind.ExitDoor:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.14f, 0.68f, 0.36f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(0.62f, 0.98f, 0.75f, 1f));
                FillRect(p, s, s * 0.26f, s * 0.22f, s * 0.34f, s * 0.54f, Color.white);
                FillRect(p, s, s * 0.30f, s * 0.26f, s * 0.26f, s * 0.46f, new Color(0.08f, 0.45f, 0.22f, 1f));
                FillRect(p, s, s * 0.42f, s * 0.45f, s * 0.24f, s * 0.09f, Color.white);
                FillTriangle(p, s,
                    new Vector2(s * 0.78f, s * 0.495f),
                    new Vector2(s * 0.62f, s * 0.63f),
                    new Vector2(s * 0.62f, s * 0.36f),
                    Color.white);
                break;

            case IconKind.Checkmark:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.16f, 0.76f, 0.40f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(0.7f, 1f, 0.8f, 1f));
                DrawLine(p, s, new Vector2(s * 0.28f, s * 0.48f), new Vector2(s * 0.44f, s * 0.32f), s * 0.065f, Color.white);
                DrawLine(p, s, new Vector2(s * 0.44f, s * 0.32f), new Vector2(s * 0.73f, s * 0.66f), s * 0.065f, Color.white);
                break;

            case IconKind.Lock:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.85f, 0.52f, 0.12f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.84f, 0.48f, 1f));
                FillRing(p, s, center, s * 0.56f, s * 0.10f, s * 0.16f, Color.white);
                FillRect(p, s, s * 0.30f, s * 0.24f, s * 0.40f, s * 0.32f, Color.white);
                FillCircle(p, s, center, s * 0.40f, s * 0.045f, new Color(0.85f, 0.52f, 0.12f, 1f));
                break;

            case IconKind.CashBundle:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.88f, 0.66f, 0.12f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.92f, 0.55f, 1f));
                FillRect(p, s, s * 0.22f, s * 0.32f, s * 0.56f, s * 0.34f, new Color(0.32f, 0.82f, 0.42f, 1f));
                FillRect(p, s, s * 0.26f, s * 0.36f, s * 0.48f, s * 0.26f, new Color(0.18f, 0.58f, 0.28f, 1f));
                FillRect(p, s, s * 0.44f, s * 0.32f, s * 0.12f, s * 0.34f, new Color(0.98f, 0.92f, 0.72f, 1f));
                FillCircle(p, s, center, s * 0.49f, s * 0.07f, new Color(0.95f, 0.85f, 0.35f, 1f));
                break;

            case IconKind.Laptop:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.85f, 0.62f, 0.10f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.9f, 0.5f, 1f));
                FillRect(p, s, s * 0.26f, s * 0.38f, s * 0.48f, s * 0.32f, Color.white);
                FillRect(p, s, s * 0.30f, s * 0.42f, s * 0.40f, s * 0.24f, new Color(0.20f, 0.68f, 0.92f, 1f));
                FillRect(p, s, s * 0.18f, s * 0.28f, s * 0.64f, s * 0.08f, Color.white);
                break;

            case IconKind.Painting:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.85f, 0.60f, 0.12f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.9f, 0.5f, 1f));
                FillRect(p, s, s * 0.23f, s * 0.25f, s * 0.54f, s * 0.46f, new Color(1f, 0.95f, 0.78f, 1f));
                FillRect(p, s, s * 0.28f, s * 0.30f, s * 0.44f, s * 0.36f, new Color(0.22f, 0.38f, 0.62f, 1f));
                FillCircle(p, s, s * 0.60f, s * 0.56f, s * 0.06f, new Color(1f, 0.85f, 0.25f, 1f));
                FillTriangle(p, s,
                    new Vector2(s * 0.44f, s * 0.56f),
                    new Vector2(s * 0.29f, s * 0.30f),
                    new Vector2(s * 0.60f, s * 0.30f),
                    new Color(0.38f, 0.76f, 0.52f, 1f));
                break;

            case IconKind.Briefcase:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.84f, 0.58f, 0.12f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.9f, 0.5f, 1f));
                FillRect(p, s, s * 0.24f, s * 0.26f, s * 0.52f, s * 0.36f, Color.white);
                FillRect(p, s, s * 0.40f, s * 0.62f, s * 0.20f, s * 0.08f, Color.white);
                FillRect(p, s, s * 0.24f, s * 0.43f, s * 0.52f, s * 0.04f, new Color(0.84f, 0.58f, 0.12f, 1f));
                break;

            case IconKind.JewelryBox:
            case IconKind.GoldObject:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.90f, 0.66f, 0.10f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.94f, 0.60f, 1f));
                FillTriangle(p, s,
                    new Vector2(center, s * 0.22f),
                    new Vector2(s * 0.24f, s * 0.52f),
                    new Vector2(s * 0.76f, s * 0.52f),
                    Color.white);
                FillTriangle(p, s,
                    new Vector2(center, s * 0.74f),
                    new Vector2(s * 0.24f, s * 0.52f),
                    new Vector2(s * 0.76f, s * 0.52f),
                    new Color(0.82f, 0.96f, 1f, 1f));
                break;

            case IconKind.ValuableDocuments:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.86f, 0.62f, 0.14f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.9f, 0.5f, 1f));
                FillRect(p, s, s * 0.28f, s * 0.24f, s * 0.44f, s * 0.52f, Color.white);
                FillRect(p, s, s * 0.34f, s * 0.58f, s * 0.32f, s * 0.05f, new Color(0.86f, 0.62f, 0.14f, 1f));
                FillRect(p, s, s * 0.34f, s * 0.46f, s * 0.32f, s * 0.05f, new Color(0.86f, 0.62f, 0.14f, 1f));
                FillRect(p, s, s * 0.34f, s * 0.34f, s * 0.22f, s * 0.05f, new Color(0.86f, 0.62f, 0.14f, 1f));
                break;

            case IconKind.StarFilled:
                FillStar(p, s, center, center, s * 0.44f, s * 0.19f, new Color(1f, 0.84f, 0.18f, 1f));
                break;

            case IconKind.StarEmpty:
                FillStar(p, s, center, center, s * 0.44f, s * 0.19f, new Color(0.28f, 0.32f, 0.38f, 0.85f));
                break;

            case IconKind.ThiefPortrait:
                FillCircle(p, s, center, center, s * 0.47f, new Color(0.98f, 0.52f, 0.08f, 1f));
                FillRing(p, s, center, center, s * 0.41f, s * 0.47f, new Color(0.14f, 0.10f, 0.08f, 1f));
                FillEllipse(p, s, center, s * 0.48f, s * 0.28f, s * 0.34f, new Color(0.66f, 0.43f, 0.30f, 1f));
                FillEllipse(p, s, center, s * 0.76f, s * 0.34f, s * 0.18f, new Color(0.08f, 0.09f, 0.11f, 1f));
                FillRect(p, s, s * 0.19f, s * 0.49f, s * 0.62f, s * 0.17f, new Color(0.045f, 0.05f, 0.06f, 1f));
                FillRect(p, s, s * 0.26f, s * 0.55f, s * 0.19f, s * 0.045f, Color.white);
                FillRect(p, s, s * 0.55f, s * 0.55f, s * 0.19f, s * 0.045f, Color.white);
                FillEllipse(p, s, center, s * 0.25f, s * 0.20f, s * 0.07f, new Color(0.16f, 0.09f, 0.07f, 1f));
                FillRect(p, s, s * 0.18f, s * 0.17f, s * 0.64f, s * 0.07f, new Color(0.16f, 0.10f, 0.07f, 1f));
                FillRect(p, s, s * 0.35f, s * 0.20f, s * 0.30f, s * 0.04f, new Color(0.96f, 0.50f, 0.08f, 1f));
                break;

            case IconKind.VillainPortrait:
                FillCircle(p, s, center, center, s * 0.47f, new Color(0.28f, 0.43f, 0.20f, 1f));
                FillRing(p, s, center, center, s * 0.41f, s * 0.47f, new Color(0.13f, 0.10f, 0.08f, 1f));
                FillEllipse(p, s, center, s * 0.48f, s * 0.29f, s * 0.34f, new Color(0.78f, 0.69f, 0.52f, 1f));
                FillEllipse(p, s, s * 0.19f, s * 0.72f, s * 0.17f, s * 0.25f, new Color(0.92f, 0.91f, 0.84f, 1f));
                FillEllipse(p, s, s * 0.81f, s * 0.72f, s * 0.17f, s * 0.25f, new Color(0.92f, 0.91f, 0.84f, 1f));
                FillRect(p, s, s * 0.18f, s * 0.51f, s * 0.64f, s * 0.13f, new Color(0.18f, 0.85f, 0.29f, 1f));
                FillRect(p, s, s * 0.22f, s * 0.55f, s * 0.22f, s * 0.045f, Color.white);
                FillRect(p, s, s * 0.56f, s * 0.55f, s * 0.22f, s * 0.045f, Color.white);
                FillRect(p, s, s * 0.37f, s * 0.31f, s * 0.26f, s * 0.055f, new Color(0.22f, 0.12f, 0.08f, 1f));
                FillRect(p, s, s * 0.31f, s * 0.20f, s * 0.38f, s * 0.06f, new Color(0.90f, 0.90f, 0.84f, 1f));
                break;

            case IconKind.ResultsClock:
                FillCircle(p, s, center, center, s * 0.46f, new Color(1f, 0.72f, 0.08f, 1f));
                FillRing(p, s, center, center, s * 0.37f, s * 0.46f, new Color(0.18f, 0.12f, 0.07f, 1f));
                FillCircle(p, s, center, center, s * 0.34f, new Color(1f, 0.86f, 0.28f, 1f));
                DrawLine(p, s, new Vector2(center, center), new Vector2(center, s * 0.68f), s * 0.035f, new Color(0.22f, 0.15f, 0.08f, 1f));
                DrawLine(p, s, new Vector2(center, center), new Vector2(s * 0.68f, s * 0.42f), s * 0.035f, new Color(0.22f, 0.15f, 0.08f, 1f));
                FillCircle(p, s, center, center, s * 0.05f, new Color(0.22f, 0.15f, 0.08f, 1f));
                break;

            case IconKind.ResultsMoneyBag:
                FillCircle(p, s, center, center, s * 0.46f, new Color(1f, 0.72f, 0.08f, 1f));
                FillRing(p, s, center, center, s * 0.40f, s * 0.46f, new Color(0.18f, 0.12f, 0.07f, 1f));
                FillEllipse(p, s, center, s * 0.43f, s * 0.25f, s * 0.30f, new Color(0.89f, 0.59f, 0.08f, 1f));
                FillRect(p, s, s * 0.34f, s * 0.64f, s * 0.32f, s * 0.10f, new Color(0.72f, 0.40f, 0.04f, 1f));
                FillTriangle(p, s, new Vector2(center, s * 0.86f), new Vector2(s * 0.37f, s * 0.70f), new Vector2(s * 0.63f, s * 0.70f), new Color(0.95f, 0.70f, 0.15f, 1f));
                FillRect(p, s, s * 0.46f, s * 0.35f, s * 0.08f, s * 0.22f, new Color(1f, 0.88f, 0.40f, 1f));
                FillCircle(p, s, center, s * 0.46f, s * 0.065f, new Color(1f, 0.88f, 0.40f, 1f));
                break;

            case IconKind.LootBag:
            default:
                FillCircle(p, s, center, center, s * 0.46f, new Color(0.90f, 0.66f, 0.10f, 1f));
                FillRing(p, s, center, center, s * 0.42f, s * 0.46f, new Color(1f, 0.92f, 0.54f, 1f));
                FillEllipse(p, s, center, s * 0.38f, s * 0.24f, s * 0.20f, Color.white);
                FillTriangle(p, s,
                    new Vector2(center, s * 0.52f),
                    new Vector2(s * 0.36f, s * 0.72f),
                    new Vector2(s * 0.64f, s * 0.72f),
                    Color.white);
                FillCircle(p, s, center, s * 0.38f, s * 0.07f, new Color(0.90f, 0.66f, 0.10f, 1f));
                break;
        }
    }

    private static void FillCircle(Color[] p, int s, float cx, float cy, float r, Color col)
    {
        float rSq = r * r;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dSq = dx * dx + dy * dy;
                if (dSq <= rSq)
                {
                    float dist = Mathf.Sqrt(dSq);
                    float aa = Mathf.Clamp01(r - dist + 0.5f);
                    BlendPixel(p, y * s + x, new Color(col.r, col.g, col.b, col.a * aa));
                }
            }
        }
    }

    private static void FillRing(Color[] p, int s, float cx, float cy, float innerR, float outerR, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (dist >= innerR - 0.5f && dist <= outerR + 0.5f)
                {
                    float a1 = Mathf.Clamp01(dist - innerR + 0.5f);
                    float a2 = Mathf.Clamp01(outerR - dist + 0.5f);
                    BlendPixel(p, y * s + x, new Color(col.r, col.g, col.b, col.a * Mathf.Min(a1, a2)));
                }
            }
        }
    }

    private static void FillEllipse(Color[] p, int s, float cx, float cy, float rx, float ry, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x - cx) / Mathf.Max(0.001f, rx);
                float ny = (y - cy) / Mathf.Max(0.001f, ry);
                if (nx * nx + ny * ny <= 1f)
                    BlendPixel(p, y * s + x, col);
            }
        }
    }

    private static void FillRect(Color[] p, int s, float xMin, float yMin, float w, float h, Color col)
    {
        int x0 = Mathf.Clamp(Mathf.RoundToInt(xMin), 0, s - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(xMin + w), 0, s - 1);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(yMin), 0, s - 1);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(yMin + h), 0, s - 1);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                BlendPixel(p, y * s + x, col);
    }

    private static void FillTriangle(Color[] p, int s, Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, s - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, s - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, s - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, s - 1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (PointInTriangle(new Vector2(x, y), a, b, c))
                    BlendPixel(p, y * s + x, col);
    }

    private static void DrawLine(Color[] p, int s, Vector2 a, Vector2 b, float radius, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = DistancePointToSegment(new Vector2(x, y), a, b);
                if (d <= radius)
                {
                    float aa = Mathf.Clamp01(radius - d + 0.5f);
                    BlendPixel(p, y * s + x, new Color(col.r, col.g, col.b, col.a * aa));
                }
            }
        }
    }

    private static void FillStar(Color[] p, int s, float cx, float cy, float outerR, float innerR, Color col)
    {
        Vector2 center = new Vector2(cx, cy);
        Vector2[] pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = (i * 36f + 90f) * Mathf.Deg2Rad;
            float r = (i % 2 == 0) ? outerR : innerR;
            pts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        for (int i = 0; i < 10; i++)
            FillTriangle(p, s, center, pts[i], pts[(i + 1) % 10], col);
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + ab * t);
    }

    private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = Sign(pt, v1, v2);
        float d2 = Sign(pt, v2, v3);
        float d3 = Sign(pt, v3, v1);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static void BlendPixel(Color[] p, int idx, Color src)
    {
        Color dst = p[idx];
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0.0001f) return;
        Color outCol = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
        outCol.a = outA;
        p[idx] = outCol;
    }
}
