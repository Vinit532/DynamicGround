using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    public Terrain terrain;                    // Reference to the 3D Terrain
    public float timerValue = 10f;             // Countdown timer for path generation
    public int minWidthOfRoad = 3;             // Minimum road width
    public int maxWidthOfRoad = 8;             // Maximum road width

    private float currentTimer;                // Countdown timer
    private TerrainData terrainData;           // Holds terrain data
    private int terrainWidth;                  // Width of the terrain heightmap
    private int terrainHeight;                 // Height of the terrain heightmap
    private int currentRoadWidth;              // Width of the current road
    private bool isWaiting = false;            // Track the 10-second wait
    private Vector2Int currentPosition;        // Current position of the road segment
    private Vector2Int direction;              // Direction for path extension
    private HashSet<Vector2Int> visitedPoints; // Track points to prevent self-crossing

    void Start()
    {
        // Initialize timer and terrain data
        currentTimer = timerValue;
        terrainData = terrain.terrainData;
        terrainWidth = terrainData.heightmapResolution;
        terrainHeight = terrainData.heightmapResolution;
        visitedPoints = new HashSet<Vector2Int>();
        SetNewRoadPath(); // Start a new path immediately
    }

    void Update()
    {
        if (!isWaiting)
        {
            // Decrement the timer
            currentTimer -= Time.deltaTime;

            // Check if timer has reached 0
            if (currentTimer <= 0f)
            {
                currentTimer = timerValue;
                StartCoroutine(WaitBeforeRestart());
            }
            else
            {
                // Generate and flatten road path segment
                GenerateRoadSegment();
                FlattenRoadSegment();
            }
        }
    }

    // Coroutine to handle the 10-second wait before restarting the timer
    private IEnumerator WaitBeforeRestart()
    {
        isWaiting = true;
        yield return new WaitForSeconds(10f);  // 10-second wait
        SetNewRoadPath();                      // Start from a new random position
        isWaiting = false;                     // Restart the timer
    }

    private void SetNewRoadPath()
    {
        // Select a random starting point within the terrain bounds
        currentPosition = new Vector2Int(
            Random.Range(maxWidthOfRoad, terrainWidth - maxWidthOfRoad),
            Random.Range(maxWidthOfRoad, terrainHeight - maxWidthOfRoad)
        );
        SetRandomDirection();
        visitedPoints.Clear();
        currentRoadWidth = Random.Range(minWidthOfRoad, maxWidthOfRoad); // Randomize road width for each path
    }

    private void SetRandomDirection()
    {
        // Choose an initial random direction (up, down, left, or right)
        int randomDirection = Random.Range(0, 4);
        switch (randomDirection)
        {
            case 0: direction = Vector2Int.up; break;
            case 1: direction = Vector2Int.down; break;
            case 2: direction = Vector2Int.left; break;
            case 3: direction = Vector2Int.right; break;
        }
    }

    private void GenerateRoadSegment()
    {
        // Add current position to visited points
        visitedPoints.Add(currentPosition);

        // Occasionally make a curve (to prevent constant straight lines)
        if (Random.Range(0, 100) < 20) // 20% chance to curve
        {
            float angle = Random.Range(20f, 60f) * (Random.value < 0.5f ? 1 : -1);
            direction = RotateDirection(direction, angle);
        }

        // Calculate the new position and prevent self-crossing
        Vector2Int nextPosition = currentPosition + direction;
        if (visitedPoints.Contains(nextPosition) || !IsWithinBounds(nextPosition))
        {
            // If a self-crossing or out-of-bounds position is detected, change direction
            SetRandomDirection();
        }
        else
        {
            // Move to the next valid position
            currentPosition = nextPosition;
        }
    }

    private Vector2Int RotateDirection(Vector2Int dir, float angleDegrees)
    {
        // Rotate direction vector by the specified angle (in degrees)
        float radians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        int newX = Mathf.RoundToInt(dir.x * cos - dir.y * sin);
        int newY = Mathf.RoundToInt(dir.x * sin + dir.y * cos);
        return new Vector2Int(newX, newY);
    }

    private bool IsWithinBounds(Vector2Int position)
    {
        // Check if the position is within terrain bounds
        return position.x >= maxWidthOfRoad && position.x < terrainWidth - maxWidthOfRoad &&
               position.y >= maxWidthOfRoad && position.y < terrainHeight - maxWidthOfRoad;
    }

    private void FlattenRoadSegment()
    {
        // Get the heightmap data
        float[,] heights = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        // Flatten a segment around the current position with the randomized width
        for (int x = currentPosition.x - currentRoadWidth / 2; x < currentPosition.x + currentRoadWidth / 2; x++)
        {
            for (int y = currentPosition.y - currentRoadWidth / 2; y < currentPosition.y + currentRoadWidth / 2; y++)
            {
                if (x >= 0 && x < terrainWidth && y >= 0 && y < terrainHeight)
                {
                    heights[x, y] = 0f; // Set height to 0 to create a flat road segment
                }
            }
        }

        // Apply the modified heightmap back to the terrain
        terrainData.SetHeights(0, 0, heights);
    }
}
