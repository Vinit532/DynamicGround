using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoTerrainSculptor : MonoBehaviour
{
    public Terrain targetTerrain;            // Reference to the Terrain object
    public float maxBrushBaseSize = 20f;     // Maximum brush size for base width
    public float sculptSpeed = 0.003f;       // Height increase per stroke
    public float directionChangeInterval = 2f; // Interval in seconds to change direction

    private TerrainData terrainData;
    private int terrainWidth;
    private int terrainHeight;
    private float[,] heights;                // Stores the heightmap data

    private Vector2 brushPosition;           // Current brush position on the terrain
    private Vector2 brushDirection;          // Current direction of the brush
    private float currentBrushBaseSize;      // Current brush size (radius) for mountain base
    private float timeSinceDirectionChange;  // Timer for direction changes

    private void Start()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No Terrain assigned! Please assign a Terrain object.");
            return;
        }

        // Initialize TerrainData and heightmap dimensions
        terrainData = targetTerrain.terrainData;
        terrainWidth = terrainData.heightmapResolution;
        terrainHeight = terrainData.heightmapResolution;
        heights = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        // Start the brush in the center and initialize direction and sculpt properties
        brushPosition = new Vector2(terrainWidth / 2, terrainHeight / 2);
        SetRandomDirection();
        SetRandomBrushProperties();
    }

    private void Update()
    {
        if (targetTerrain != null)
        {
            MoveBrush();
            SculptMountain();
            ApplyHeights();
            UpdateDirectionAndBrush();
        }
    }

    private void MoveBrush()
    {
        // Move the brush position based on direction
        brushPosition += brushDirection * 0.05f * 100;

        // If the brush hits terrain boundaries, reverse direction
        if (brushPosition.x < 0 || brushPosition.x >= terrainWidth) brushDirection.x *= -1;
        if (brushPosition.y < 0 || brushPosition.y >= terrainHeight) brushDirection.y *= -1;
    }

    private void SculptMountain()
    {
        // Calculate the current brush radius for the mountain base
        int brushRadius = Mathf.RoundToInt(currentBrushBaseSize * terrainWidth / terrainData.size.x);
        int xCenter = Mathf.RoundToInt(brushPosition.x);
        int yCenter = Mathf.RoundToInt(brushPosition.y);

        // Loop through the area within the brush radius
        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                int xPos = xCenter + x;
                int yPos = yCenter + y;

                // Check if position is within bounds
                if (xPos >= 0 && xPos < terrainWidth && yPos >= 0 && yPos < terrainHeight)
                {
                    // Calculate distance from the center for a smooth mountain slope
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance < brushRadius)
                    {
                        // Determine height increment with a smooth falloff to form a mountain shape
                        float heightIncrement = Mathf.Lerp(sculptSpeed, 0, distance / brushRadius);

                        // Add height increment to the terrain for a smooth mountain slope
                        heights[yPos, xPos] += heightIncrement;
                    }
                }
            }
        }
    }

    private void UpdateDirectionAndBrush()
    {
        // Update the timer for direction and brush property changes
        timeSinceDirectionChange += Time.deltaTime;

        if (timeSinceDirectionChange >= directionChangeInterval)
        {
            // Change direction and brush properties randomly
            SetRandomDirection();
            SetRandomBrushProperties();
            timeSinceDirectionChange = 0;
        }
    }

    private void SetRandomDirection()
    {
        // Generate a random direction angle (in radians) and calculate the direction vector
        float angle = Random.Range(0f, Mathf.PI * 2f);
        brushDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    private void SetRandomBrushProperties()
    {
        // Randomize brush base size for each new mountain base
        currentBrushBaseSize = Random.Range(0, maxBrushBaseSize);
    }

    private void ApplyHeights()
    {
        // Apply modified heightmap to the terrain
        terrainData.SetHeights(0, 0, heights);
    }
}
