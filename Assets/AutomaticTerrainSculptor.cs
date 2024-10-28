using UnityEngine;

public class DynamicTerrainSculptor : MonoBehaviour
{
    public Terrain targetTerrain;  // Reference to the Terrain object
    public float maxSculptSpeed = 0.005f;  // Maximum sculpt speed (height increase) for randomization
    public float maxBrushSize = 10f;  // Maximum brush size for randomization
    public float maxHeight = 0.5f;  // Maximum terrain height allowed
    public float directionChangeInterval = 1f;  // Interval to change direction in seconds

    private TerrainData terrainData;
    private int terrainWidth;
    private int terrainHeight;
    private float[,] heights;  // Stores the heightmap data

    private Vector2 brushPosition;  // Current brush position on the terrain
    private Vector2 brushDirection;  // Current direction of the brush
    private float currentSculptSpeed;  // Variable sculpting speed
    private float currentBrushSize;  // Variable brush size
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
            SculptTerrain();
            ApplyHeights();
            UpdateDirectionAndBrush();
        }
    }

    private void MoveBrush()
    {
        // Move the brush position based on direction
        brushPosition += brushDirection * currentSculptSpeed * 100;

        // If the brush hits terrain boundaries, reverse direction
        if (brushPosition.x < 0 || brushPosition.x >= terrainWidth) brushDirection.x *= -1;
        if (brushPosition.y < 0 || brushPosition.y >= terrainHeight) brushDirection.y *= -1;
    }

    private void SculptTerrain()
    {
        // Calculate the current brush radius
        int brushRadius = Mathf.RoundToInt(currentBrushSize * terrainWidth / terrainData.size.x);
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
                    // Calculate distance from the center for a smooth falloff effect
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance < brushRadius)
                    {
                        // Determine the increment based on distance for smooth falloff
                        float heightIncrement = Mathf.Lerp(currentSculptSpeed, 0, distance / brushRadius);

                        // Add the height increment, allowing for accumulation
                        heights[yPos, xPos] = Mathf.Clamp(heights[yPos, xPos] + heightIncrement, 0, maxHeight);
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
        // Randomize sculpt speed and brush size within the assigned maximum ranges
        currentSculptSpeed = Random.Range(0, maxSculptSpeed);
        currentBrushSize = Random.Range(0, maxBrushSize);
    }

    private void ApplyHeights()
    {
        // Apply modified heightmap to the terrain
        terrainData.SetHeights(0, 0, heights);
    }
}
