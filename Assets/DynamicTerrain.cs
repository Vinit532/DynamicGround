using System.Collections;
using UnityEngine;

public class DynamicTerrain : MonoBehaviour
{
    public Terrain terrain;  // Reference to the terrain
    public float mountainSpeed = 1.0f;  // Speed at which mountains are generated
    public float maxMountainHeight = 25f;  // Maximum mountain height
    public int minBrushSize = 30;  // Minimum brush size
    public int maxBrushSize = 100;  // Maximum brush size
    public float minBrushOpacity = 1f;  // Minimum brush opacity
    public float maxBrushOpacity = 8f;  // Maximum brush opacity
    public int mountainDetailLayers = 20;  // Number of strokes to simulate "holding" the brush
    public Texture2D[] brushTextures;  // Assign built-in brushes from Unity here

    private TerrainData terrainData;
    private int terrainWidth;
    private int terrainHeight;
    private bool isGenerating = false;

    void Start()
    {
        // Initialize terrain data
        terrainData = terrain.terrainData;
        terrainWidth = terrainData.heightmapResolution;
        terrainHeight = terrainData.heightmapResolution;

        // Start the mountain generation process
        StartCoroutine(GenerateMountains());
    }

    IEnumerator GenerateMountains()
    {
        while (true)
        {
            if (!isGenerating)
            {
                isGenerating = true;

                // Randomly choose a position on the terrain for the mountain
                int randomX = Random.Range(0, terrainWidth);
                int randomY = Random.Range(0, terrainHeight);

                // Randomize the brush size for this mountain
                int brushSize = Random.Range(minBrushSize, maxBrushSize);

                // Randomly determine the number of strokes to simulate "scrubbing"
                int strokeCount = Random.Range(3, mountainDetailLayers);

                // Start generating the mountain
                yield return StartCoroutine(BuildMountain(randomX, randomY, brushSize, strokeCount));

                isGenerating = false;
            }

            yield return new WaitForSeconds(mountainSpeed);
        }
    }

    IEnumerator BuildMountain(int centerX, int centerY, int brushSize, int strokeCount)
    {
        float[,] heights = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        // Apply multiple strokes to simulate the "click and hold" painting effect
        for (int i = 0; i < strokeCount; i++)
        {
            // Randomize the brush opacity
            float brushOpacity = Random.Range(minBrushOpacity, maxBrushOpacity) / 10f;

            // Randomize the brush position within a certain range around the center point
            int randomOffsetX = Random.Range(-brushSize / 2, brushSize / 2);
            int randomOffsetY = Random.Range(-brushSize / 2, brushSize / 2);

            // Select a random built-in Unity terrain brush
            int brushIndex = Random.Range(0, brushTextures.Length);

            // Apply the brush stroke using different built-in brushes
            yield return StartCoroutine(ApplyBrush(heights, centerX + randomOffsetX, centerY + randomOffsetY, brushSize, brushOpacity, brushTextures[brushIndex]));
        }

        // Apply the modified heights back to the terrain
        terrainData.SetHeights(0, 0, heights);
    }

    IEnumerator ApplyBrush(float[,] heights, int centerX, int centerY, int brushSize, float brushOpacity, Texture2D brushTexture)
    {
        // Get the terrain size
        int terrainWidth = heights.GetLength(0);
        int terrainHeight = heights.GetLength(1);

        // Iterate over the brush area
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                int posX = centerX + x;
                int posY = centerY + y;

                // Ensure the position is within the terrain bounds
                if (posX < 0 || posX >= terrainWidth || posY < 0 || posY >= terrainHeight)
                    continue; // Skip if out of bounds

                // Calculate the distance from the brush center
                float distance = Mathf.Sqrt(x * x + y * y) / brushSize;

                if (distance <= 1.0f)
                {
                    // Use the selected built-in brush texture to modify the terrain's height
                    float brushValue = GetBrushValueFromTexture(brushTexture, x, y, brushSize);

                    // Apply height modification based on the brush value and opacity
                    float newHeight = heights[posY, posX] + Mathf.Clamp01(brushOpacity * brushValue * (1f - distance));

                    // Clamp the new height to prevent spikes or walls
                    heights[posY, posX] = Mathf.Clamp(newHeight, 0f, maxMountainHeight); // Ensure new height stays within bounds
                }
            }
        }

        yield return null;
    }

    float GetBrushValueFromTexture(Texture2D brushTexture, int x, int y, int brushSize)
    {
        // Get the normalized pixel coordinates based on the brush size
        float u = (x + brushSize) / (2f * brushSize);
        float v = (y + brushSize) / (2f * brushSize);

        // Sample the brush texture and return the grayscale value
        return brushTexture.GetPixelBilinear(u, v).grayscale;
    }
}
