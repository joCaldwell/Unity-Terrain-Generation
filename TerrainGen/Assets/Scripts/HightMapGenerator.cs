using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public struct HeightMapGenerator
{
    [NativeDisableParallelForRestriction,] private NativeArray<float> _heightMap;
    private Dictionary<int, float[,]> _heightMaps;
    private readonly TerrainMeshVariables _meshVariables;
    private readonly TerrainHeightmapVariables _heightmapVariables;

    private string float2tostring(float[,] data)
    {
        string s = string.Empty;
        for (int i = 0; i < data.GetLength(0); i++)
        {
            for (int j = 0; j < data.GetLength(1); j++)
            {
                s += $"{data[i,j]}, ";
            }
            s += "\n";
        }
        return s;
    }


    public HeightMapGenerator(TerrainMeshVariables mv, TerrainHeightmapVariables hv)
    {
        _meshVariables = mv;
        _heightmapVariables = hv;
        _heightMap = new NativeArray<float>(_meshVariables.TotalVerts, Allocator.Persistent);
        _heightMaps = null;

        // Generate the heightmap once
        _heightMaps = GenerateHeightMap(_heightmapVariables.randomSeed);

        for (int i = 0; i < _meshVariables.levelsOfDetail; i++)
        {
            int levelAdder = _meshVariables.levelsIndex[i];
            int k = (_meshVariables.terrainMeshDetail / (1 << i)) + 1;

            for (int j = 0; j < Mathf.Pow(_heightMaps[i].GetLength(0), 2); j++)
            {
                int x = j / k;
                int y = j % k;
                _heightMap[j + levelAdder] = _heightMaps[i][x, y];
            }
        }
    }

    public NativeArray<float> GetHeightMap() => _heightMap;

    public Dictionary<int, float[,]> GetHeightMaps() => _heightMaps;

    void DiamondSquareRecursive(float[,] heightMap, bool[,] processed, int x, int y, int width, float roughness, System.Random random)
    {
        if (width <= 1)
            return;

        int halfSize = (width) / 2;

        // Diamond step
        int centerX = x + halfSize;
        int centerY = y + halfSize;
        if (!processed[centerX, centerY])
        {
            float diamondAvg = (heightMap[x, y] +
                                heightMap[x + width, y] +
                                heightMap[x, y + width] +
                                heightMap[x + width, y + width]) / 4.0f;
            heightMap[centerX, centerY] = diamondAvg + RandomOffset(roughness, random);
            processed[centerX, centerY] = true;
        }

        // Square step
        if (!processed[x + halfSize, y])
        {
            heightMap[x + halfSize, y] = AverageWithRandom(heightMap[x, y], heightMap[x + width, y], heightMap[centerX, centerY], roughness, random);
            processed[x + halfSize, y] = true;
        }
        if (!processed[x, y + halfSize])
        {
            heightMap[x, y + halfSize] = AverageWithRandom(heightMap[x, y], heightMap[x, y + width], heightMap[centerX, centerY], roughness, random);
            processed[x, y + halfSize] = true;
        }
        if (!processed[x + width, y + halfSize])
        {
            heightMap[x + width, y + halfSize] = AverageWithRandom(heightMap[x + width, y], heightMap[x + width, y + width], heightMap[centerX, centerY], roughness, random);
            processed[x + width, y + halfSize] = true;
        }
        if (!processed[x + halfSize, y + width])
        {
            heightMap[x + halfSize, y + width] = AverageWithRandom(heightMap[x, y + width], heightMap[x + width, y + width], heightMap[centerX, centerY], roughness, random);
            processed[x + halfSize, y + width] = true;
        }

        // Recursive calls
        DiamondSquareRecursive(heightMap, processed, x, y, halfSize, RandomRoughness(roughness, random) / 2.0f, random);
        DiamondSquareRecursive(heightMap, processed, centerX, y, halfSize, RandomRoughness(roughness, random) / 2.0f, random);
        DiamondSquareRecursive(heightMap, processed, x, centerY, halfSize, RandomRoughness(roughness, random) / 2.0f, random);
        DiamondSquareRecursive(heightMap, processed, centerX, centerY, halfSize, RandomRoughness(roughness, random) / 2.0f, random);
    }

    private float RandomRoughness(float roughness, System.Random random)
    {
        return (float)(random.NextDouble()*2-1)*(roughness/8) + roughness;
    }

    private float AverageWithRandom(float a, float b, float c, float roughness, System.Random random)
    {
        return (a + b + c) / 3.0f + RandomOffset(roughness, random);
    }

    private float RandomOffset(float roughness, System.Random random)
    {
        return Mathf.Clamp((float)(random.NextDouble() * 2 - 1) * roughness, 0.0f, _meshVariables.maxHeight);
    }

    private Dictionary<int, float[,]> GenerateHeightMap(int randomSeed)
    {        
        int bigSize = _meshVariables.terrainMeshDetail+1;
        Dictionary<int, float[,]> heightMaps = new Dictionary<int, float[,]>();
        for (int i = 0; i < _meshVariables.levelsOfDetail; i++)
        {
            float[,] map = new float[(bigSize - 1) / (1 << i) + 1, (bigSize - 1) / (1 << i) + 1];
            heightMaps.Add(i, map);
        }

        System.Random random = new System.Random(randomSeed);

        // Initialize corners
        float[] corners = {
        (float)(random.NextDouble() * _meshVariables.maxHeight),
        (float)(random.NextDouble() * _meshVariables.maxHeight),
        (float)(random.NextDouble() * _meshVariables.maxHeight),
        (float)(random.NextDouble() * _meshVariables.maxHeight),
        };
        

        bool[,] processed = new bool[bigSize, bigSize];
        float[,] heightMap = new float[bigSize, bigSize];
        heightMap[0, 0] = corners[0];
        heightMap[0, bigSize-1] = corners[1];
        heightMap[bigSize-1, 0] = corners[2];
        heightMap[bigSize-1, bigSize-1] = corners[3];
        DiamondSquareRecursive(heightMap, processed, 0, 0, bigSize-1, _heightmapVariables.baseNoise, random);

        // Copy the heightmap to the different levels of detail
        for (int i = 0; i < _meshVariables.levelsOfDetail; i++)
        {
            int size = (int)((bigSize - 1) / Mathf.Pow(2, i) + 1);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    heightMaps[i][x, y] = heightMap[(int)(x * Mathf.Pow(2, i)), (int)(y * Mathf.Pow(2, i))];
                }
            }
        }
        return heightMaps;
    }
}


[Serializable]
public struct TerrainHeightmapVariables
{
    [Header("Noise")]
    public float baseNoise;
    public int randomSeed;
}
