using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DynamicMountains : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;

    public float maxMountainHeight = 50f; // Maximum height for mountains
    public float minMountainHeight = 10f; // Minimum height for mountains
    public float maxMountainWidth = 100f; // Maximum width for mountains (double height)
    public float maxMountainPercentage = 40f; // Maximum percentage of terrain area for mountains

    public float growDuration = 5f; // How fast the mountains grow
    public float flattenDuration = 5f; // How fast they flatten
    public float pauseBetweenMountains = 2f; // Pause between new mountain creation
    public float flattenPauseDuration = 5f; // Pause after max mountains are built before flattening

    private List<Vector2Int> activeMountainPositions = new List<Vector2Int>();
    private List<int> activeMountainRadii = new List<int>();
    private List<float> activeMountainHeights = new List<float>();

    private float totalTerrainArea;
    private float currentMountainArea;
    private float maxAllowedMountainArea;

    void Start()
    {
        terrain = Terrain.activeTerrain;
        terrainData = terrain.terrainData;

        // Calculate the total terrain area based on terrain resolution (mesh size)
        totalTerrainArea = terrainData.heightmapResolution * terrainData.heightmapResolution;
        // Calculate the maximum allowed area for mountains based on the maxMountainPercentage
        maxAllowedMountainArea = (maxMountainPercentage / 100f) * totalTerrainArea;

        // Start the process
        StartCoroutine(ManageTerrainCycle());
    }

    IEnumerator ManageTerrainCycle()
    {
        while (true)
        {
            // Check if mountains cover more area than allowed percentage
            if (currentMountainArea >= maxAllowedMountainArea)
            {
                yield return StartCoroutine(FlattenAllMountains());
            }
            else
            {
                // Generate a new mountain
                GenerateRandomMountain();
                yield return new WaitForSeconds(pauseBetweenMountains);
            }
        }
    }

    void GenerateRandomMountain()
    {
        // Randomly generate position and mountain properties
        int randomX = Random.Range((int)maxMountainWidth, terrainData.heightmapResolution - (int)maxMountainWidth);
        int randomY = Random.Range((int)maxMountainWidth, terrainData.heightmapResolution - (int)maxMountainWidth);
        float mountainHeight = Random.Range(minMountainHeight, maxMountainHeight); // Random height
        int mountainWidth = (int)Mathf.Max(mountainHeight * 2, maxMountainWidth); // Width must be at least double the height

        // Calculate the area of the current mountain
        float mountainArea = Mathf.PI * (mountainWidth / 2f) * (mountainWidth / 2f);

        // Check if adding this mountain would exceed the allowed area
        if (currentMountainArea + mountainArea > maxAllowedMountainArea)
        {
            return; // Skip adding the mountain if it exceeds the limit
        }

        // Store the mountain's data
        activeMountainPositions.Add(new Vector2Int(randomX, randomY));
        activeMountainRadii.Add(mountainWidth / 2);
        activeMountainHeights.Add(mountainHeight);
        currentMountainArea += mountainArea;

        // Gradually grow the mountain
        StartCoroutine(GrowMountain(randomX, randomY, mountainWidth, mountainHeight, growDuration));
    }

    IEnumerator GrowMountain(int xPos, int yPos, int diameter, float targetHeight, float duration)
    {
        int radius = diameter / 2;
        float[,] heights = terrainData.GetHeights(xPos - radius, yPos - radius, diameter, diameter);
        float[,] originalHeights = (float[,])heights.Clone();

        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float growthFactor = elapsedTime / duration;

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    float distanceToCenter = Mathf.Sqrt((x - radius) * (x - radius) + (y - radius) * (y - radius));
                    float heightMultiplier = Mathf.Clamp01(1 - distanceToCenter / radius);

                    heights[x, y] = Mathf.Lerp(originalHeights[x, y], targetHeight * heightMultiplier, growthFactor);
                }
            }

            terrainData.SetHeights(xPos - radius, yPos - radius, heights);
            yield return null;
        }
    }

    IEnumerator FlattenAllMountains()
    {
        for (int i = 0; i < activeMountainPositions.Count; i++)
        {
            Vector2Int mountainPos = activeMountainPositions[i];
            int radius = activeMountainRadii[i];

            // Gradually flatten the mountain
            yield return StartCoroutine(FlattenMountain(mountainPos.x, mountainPos.y, radius, flattenDuration));
        }

        // Reset the mountain data after flattening
        activeMountainPositions.Clear();
        activeMountainRadii.Clear();
        activeMountainHeights.Clear();
        currentMountainArea = 0;
    }

    IEnumerator FlattenMountain(int xPos, int yPos, int radius, float duration)
    {
        int diameter = radius * 2;
        float[,] heights = terrainData.GetHeights(xPos - radius, yPos - radius, diameter, diameter);
        float[,] originalHeights = (float[,])heights.Clone();

        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float flattenFactor = elapsedTime / duration;

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    heights[x, y] = Mathf.Lerp(heights[x, y], originalHeights[x, y], flattenFactor);
                }
            }

            terrainData.SetHeights(xPos - radius, yPos - radius, heights);
            yield return null;
        }
    }
}
