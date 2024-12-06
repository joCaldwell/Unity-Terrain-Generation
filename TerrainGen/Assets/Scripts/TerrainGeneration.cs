using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TerrainMeshGenerator : MonoBehaviour
{
    public static TerrainMeshGenerator Instance;
    public TerrainMeshVariables meshVariables;
    public TerrainHeightmapVariables heightmapVariables;
    public ChunkParameters chunkParameters;

    private Dictionary<int2, Chunk> loadedChunks = new Dictionary<int2, Chunk>();
    private Dictionary<int, float[,]> heightMaps;

    private NativeArray<float> heightMap;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnValidate()
    {
        GenerateHeightMaps();
    }

    private void Start()
    {
        GenerateHeightMaps();
    }

    private void Update()
    {
        UpdateChunks();
    }

    private void UpdateChunks()
    {
        float3 camPos = Camera.main.transform.position;
        float2 camPos2D = new Vector2(camPos.x, camPos.z);
        int2 cameraChunk = WorldToChunk(camPos2D);
        int2 maxChunk = 1 << (meshVariables.terrainDetailFactor - chunkParameters.chunkSizeFactor);

        int chunksToCalc = chunkParameters.chunksToCalculate;
        int levelWidth = chunkParameters.levelWidth;
        for (int x = -chunksToCalc; x <= chunksToCalc; x++)
        {
            for (int y = -chunksToCalc; y <= chunksToCalc; y++)
            {
                int2 currentChunk = new int2(cameraChunk.x + x, cameraChunk.y + y);
                if (currentChunk.x < 0 || currentChunk.y < 0) continue;
                if (currentChunk.x >= maxChunk.x || currentChunk.y >= maxChunk.y) continue;
                int xFact = Mathf.FloorToInt(Mathf.Abs(x) / levelWidth);
                int yFact = Mathf.FloorToInt(Mathf.Abs(y) / levelWidth);
                int dist = Mathf.FloorToInt(Mathf.Sqrt((xFact * xFact) + (yFact * yFact)));
                int level = Mathf.Min(dist, meshVariables.levelsOfDetail - 1);

                if (!loadedChunks.ContainsKey(currentChunk))
                {
                    loadedChunks.Add(currentChunk, new Chunk(currentChunk, level, chunkParameters, meshVariables, heightMaps));
                }
                else
                {
                    Chunk chunk = loadedChunks[currentChunk];
                    if (chunk.lod != level)
                    {
                        chunk.D();
                        loadedChunks.Remove(currentChunk);
                        loadedChunks.Add(currentChunk, new Chunk(currentChunk, level, chunkParameters, meshVariables, heightMaps));
                    }
                }
            }
        }
    }

    private int2 WorldToChunk(float2 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / (chunkParameters.chunkWidth * (meshVariables.terrainWidth / meshVariables.terrainMeshDetail)));
        int y = Mathf.FloorToInt(worldPos.y / (chunkParameters.chunkWidth * (meshVariables.terrainWidth / meshVariables.terrainMeshDetail)));
        return new int2(x,y);
    }

    private void GenerateHeightMaps()
    {
        HeightMapGenerator heightmapGenerator = new HeightMapGenerator(meshVariables, heightmapVariables);
        heightMap = heightmapGenerator.GetHeightMap();
        heightMaps = heightmapGenerator.GetHeightMaps();
    }
}

[Serializable]
public struct TerrainMeshVariables
{
    public Material material;

    [Range(1, 16)]
    public int terrainDetailFactor;

    // The number of subdivisions along one axis of the terrain grid
    [Range(1, 5)]
    public int levelsOfDetail;

    public float terrainWidth;

    public float maxHeight;

    // Factor of 2 to the power of terrainDetailFactor
    public int terrainMeshDetail => 1 << terrainDetailFactor;

    // The total number of vertices in the whole heightmap (all levels of detail)
    public int TotalVerts
    {
        get
        {
            int count = 0;
            for (int i = 0; i < levelsOfDetail; i++)
            {
                int detail = (terrainMeshDetail / (1 << i)) + 1;
                count += detail * detail;
            }
            return count;
        }
    }

    public float[] levelsWidthMultiplier
    {
        get
        {
            float[] widths = new float[levelsOfDetail];
            for (int i = 0; i < levelsOfDetail; i++)
            {
                widths[i] = (terrainWidth / terrainMeshDetail) * (1 << i);
            }
            return widths;
        }
    }

    public int[] levelsVerts
    {
        get
        {
            int[] verts = new int[levelsOfDetail];
            for (int i = 0; i < levelsOfDetail; i++)
            {
                verts[i] = (terrainMeshDetail / (1 << i) + 1) * (terrainMeshDetail / (1 << i) + 1);
            }
            return verts;
        }
    }

    public int[] levelsIndex
    {
        get
        {
            int[] levels = new int[levelsOfDetail];
            levels[0] = 0;
            for (int i = 0; i < levelsOfDetail-1; i++)
            {
                levels[i+1] = levels[i] + (int)Mathf.Pow(terrainMeshDetail / (1 << i) + 1, 2);
            }
            return levels;
        }
    }
}