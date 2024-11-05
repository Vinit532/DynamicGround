using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicBattleGround : MonoBehaviour
{
    public Terrain terrain;  // Shared Terrain reference
    public float mountainSpeed = 1.0f;  // Speed at which mountains are generated
    public float maxMountainHeight = 25f;  // Max height for generated mountains
    public int minBrushSize = 30;
    public int maxBrushSize = 100;
    public float minBrushOpacity = 1f;
    public float maxBrushOpacity = 8f;
    public int mountainDetailLayers = 20;
    public Texture2D[] brushTextures;  // Textures for Dynamic Terrain
    public float maxBrushBaseSize = 20f;  // For AutoTerrainSculptor
    public float sculptSpeed = 0.003f;  // AutoTerrain height increment
    public float directionChangeInterval = 2f;  // Direction change interval for AutoTerrain

    private TerrainData terrainData;
    private int terrainWidth;
    private int terrainHeight;
    private float[,] heights;  // Base heightmap data
    private float[,] tempHeights;  // Temporary heightmap for modifications
    private bool isGenerating = false;  // Flag for DynamicTerrain operation
    private Vector2 brushPosition;  // Brush position for AutoTerrain
    private Vector2 brushDirection;  // Direction for brush movement in AutoTerrain
    private float currentBrushBaseSize;  // Current brush size in AutoTerrain
    private float timeSinceDirectionChange;  // Timer for AutoTerrain direction change

    private object heightmapLock = new object();  // Lock for heightmap modifications

    void Start()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned! Please assign a Terrain object.");
            return;
        }

        // Initialize heightmap data
        terrainData = terrain.terrainData;
        terrainWidth = terrainData.heightmapResolution;
        terrainHeight = terrainData.heightmapResolution;
        heights = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);
        tempHeights = (float[,])heights.Clone();

        brushPosition = new Vector2(terrainWidth / 2, terrainHeight / 2);
        SetRandomDirection();
        SetRandomBrushProperties();

        StartCoroutine(GenerateMountains());  // Start DynamicTerrain coroutine
    }

    void Update()
    {
        if (terrain != null)
        {
            lock (heightmapLock)
            {
                MoveBrush();
                SculptMountain();
                UpdateTerrainHeights();
                UpdateDirectionAndBrush();
            }
        }
    }

    IEnumerator GenerateMountains()
    {
        while (true)
        {
            if (!isGenerating)
            {
                isGenerating = true;
                int randomX = Random.Range(0, terrainWidth);
                int randomY = Random.Range(0, terrainHeight);
                int brushSize = Random.Range(minBrushSize, maxBrushSize);
                int strokeCount = Random.Range(3, mountainDetailLayers);
                yield return StartCoroutine(BuildMountain(randomX, randomY, brushSize, strokeCount));
                isGenerating = false;
            }
            yield return new WaitForSeconds(mountainSpeed);
        }
    }

    IEnumerator BuildMountain(int centerX, int centerY, int brushSize, int strokeCount)
    {
        for (int i = 0; i < strokeCount; i++)
        {
            float brushOpacity = Random.Range(minBrushOpacity, maxBrushOpacity) / 10f;
            int randomOffsetX = Random.Range(-brushSize / 2, brushSize / 2);
            int randomOffsetY = Random.Range(-brushSize / 2, brushSize / 2);
            int brushIndex = Random.Range(0, brushTextures.Length);
            yield return StartCoroutine(ApplyBrush(centerX + randomOffsetX, centerY + randomOffsetY, brushSize, brushOpacity, brushTextures[brushIndex]));
        }
        yield return null;
    }

    IEnumerator ApplyBrush(int centerX, int centerY, int brushSize, float brushOpacity, Texture2D brushTexture)
    {
        lock (heightmapLock)
        {
            for (int x = -brushSize; x <= brushSize; x++)
            {
                for (int y = -brushSize; y <= brushSize; y++)
                {
                    int posX = centerX + x;
                    int posY = centerY + y;

                    if (posX >= 0 && posX < terrainWidth && posY >= 0 && posY < terrainHeight)
                    {
                        float distance = Mathf.Sqrt(x * x + y * y) / brushSize;
                        if (distance <= 1.0f)
                        {
                            float brushValue = GetBrushValueFromTexture(brushTexture, x, y, brushSize);
                            float newHeight = tempHeights[posY, posX] + Mathf.Clamp01(brushOpacity * brushValue * (1f - distance));
                            tempHeights[posY, posX] = Mathf.Clamp(newHeight, 0f, maxMountainHeight);
                        }
                    }
                }
            }
        }
        yield return null;
    }

    float GetBrushValueFromTexture(Texture2D brushTexture, int x, int y, int brushSize)
    {
        float u = (x + brushSize) / (2f * brushSize);
        float v = (y + brushSize) / (2f * brushSize);
        return brushTexture.GetPixelBilinear(u, v).grayscale;
    }

    private void MoveBrush()
    {
        brushPosition += brushDirection * 0.05f * 100;
        if (brushPosition.x < 0 || brushPosition.x >= terrainWidth) brushDirection.x *= -1;
        if (brushPosition.y < 0 || brushPosition.y >= terrainHeight) brushDirection.y *= -1;
    }

    private void SculptMountain()
    {
        int brushRadius = Mathf.RoundToInt(currentBrushBaseSize * terrainWidth / terrainData.size.x);
        int xCenter = Mathf.RoundToInt(brushPosition.x);
        int yCenter = Mathf.RoundToInt(brushPosition.y);

        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                int xPos = xCenter + x;
                int yPos = yCenter + y;
                if (xPos >= 0 && xPos < terrainWidth && yPos >= 0 && yPos < terrainHeight)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance < brushRadius)
                    {
                        float heightIncrement = Mathf.Lerp(sculptSpeed, 0, distance / brushRadius);
                        tempHeights[yPos, xPos] += heightIncrement;
                    }
                }
            }
        }
    }

    private void UpdateDirectionAndBrush()
    {
        timeSinceDirectionChange += Time.deltaTime;
        if (timeSinceDirectionChange >= directionChangeInterval)
        {
            SetRandomDirection();
            SetRandomBrushProperties();
            timeSinceDirectionChange = 0;
        }
    }

    private void SetRandomDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        brushDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    private void SetRandomBrushProperties()
    {
        currentBrushBaseSize = Random.Range(0, maxBrushBaseSize);
    }

    private void UpdateTerrainHeights()
    {
        lock (heightmapLock)
        {
            terrainData.SetHeights(0, 0, tempHeights);
        }
    }
}
