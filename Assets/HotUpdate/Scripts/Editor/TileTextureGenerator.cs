using UnityEngine;
using UnityEditor;
using System.IO;

public class TileTextureGenerator
{
    private static readonly Color[] baseColors = new Color[]
    {
        new Color(0.96f, 0.26f, 0.42f),
        new Color(0.36f, 0.25f, 0.80f),
        new Color(1.00f, 0.84f, 0.10f),
        new Color(0.20f, 0.80f, 0.60f),
        new Color(1.00f, 0.60f, 0.10f),
        new Color(0.60f, 0.16f, 0.62f),
        new Color(1.00f, 0.70f, 0.60f),
        new Color(0.42f, 0.78f, 0.22f),
    };

    private static readonly string[] colorNames = new string[]
    {
        "Strawberry", "Blueberry", "Lemon", "Mint", "Orange", "Grape", "Peach", "Apple"
    };

    [MenuItem("Cake Home/Generate Candy Tile Textures")]
    private static void Generate()
    {
        const int size = 64;
        const int cornerRadius = 8;
        const int padding = 4;
        const int bodySize = 56;

        string outputPath = Path.Combine(Application.dataPath, "Textures", "CandyTiles");
        Directory.CreateDirectory(outputPath);

        int totalGenerated = 0;

        for (int i = 0; i < baseColors.Length; i++)
        {
            Color baseColor = baseColors[i];
            string name = colorNames[i];

            // Generate base candy
            Texture2D tex = GenerateCandyTexture(size, bodySize, cornerRadius, padding, baseColor, 1.0f);
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outputPath, name + ".png"), png);
            Object.DestroyImmediate(tex);
            totalGenerated++;

            // Generate light variant (7 total, for 15 total files)
            if (i < 7)
            {
                Color lightColor = Color.Lerp(baseColor, Color.white, 0.3f);
                Texture2D lightTex = GenerateCandyTexture(size, bodySize, cornerRadius, padding, lightColor, 0.85f);
                byte[] lightPng = lightTex.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(outputPath, name + "_Light.png"), lightPng);
                Object.DestroyImmediate(lightTex);
                totalGenerated++;
            }
        }

        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Generated " + totalGenerated + " candy tile textures to " + outputPath);
    }

    private static Texture2D GenerateCandyTexture(int size, int bodySize, int cornerRadius, int padding, Color fillColor, float alpha)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        int halfBody = bodySize / 2;
        int cx = size / 2;
        int cy = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int ax = Mathf.Abs(dx);
                int ay = Mathf.Abs(dy);

                bool inside = false;

                if (ax <= halfBody && ay <= halfBody)
                {
                    if (ax <= halfBody - cornerRadius || ay <= halfBody - cornerRadius)
                    {
                        inside = true;
                    }
                    else
                    {
                        int cornerDistX = ax - (halfBody - cornerRadius);
                        int cornerDistY = ay - (halfBody - cornerRadius);
                        float cornerDist = Mathf.Sqrt(cornerDistX * cornerDistX + cornerDistY * cornerDistY);
                        inside = cornerDist <= cornerRadius;
                    }
                }

                if (inside)
                {
                    float nx = (x - padding) / (float)bodySize;
                    float ny = (y - padding) / (float)bodySize;

                    float edgeX = Mathf.Min(nx, 1.0f - nx);
                    float edgeY = Mathf.Min(ny, 1.0f - ny);
                    float edgeDist = Mathf.Min(edgeX, edgeY);

                    float edgeFactor = Mathf.Clamp01(edgeDist * 6.0f);
                    Color edgeColor = Color.Lerp(fillColor * 0.6f, fillColor, edgeFactor);

                    float shineX = 1.0f - nx;
                    float shineY = ny;
                    float shineDist = Mathf.Min(shineX, shineY);
                    float shine = Mathf.Clamp01((0.4f - shineDist) * 4.0f);

                    Color finalColor = edgeColor;
                    finalColor.r += shine * 0.3f;
                    finalColor.g += shine * 0.3f;
                    finalColor.b += shine * 0.3f;
                    finalColor.a = alpha;

                    tex.SetPixel(x, y, finalColor);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
