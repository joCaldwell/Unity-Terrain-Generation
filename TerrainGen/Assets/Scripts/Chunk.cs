using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public struct Chunk
{
    public bool isLoaded;
    public int lod;
    public int2 chunkCoord;
    public ChunkParameters chunkParameters;
    public TerrainMeshVariables meshVariables;
    private MeshGenerator mg;
    public Dictionary<int, float[,]> heightMaps;
    public NativeArray<float> heightMap;
    public Mesh mesh;
    public GameObject chunkObj;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public Chunk(int2 coord, int lod, ChunkParameters parameters, TerrainMeshVariables mv, Dictionary<int, float[,]> heightMaps)
    {
        isLoaded = false;
        this.lod = lod;
        chunkCoord = coord;
        chunkParameters = parameters;
        meshVariables = mv;
        mg = new MeshGenerator(mv, chunkParameters, lod);
        this.heightMaps = heightMaps;
        heightMap = new();
        mesh = null;

        chunkObj = new GameObject("Chunk");

        meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshRenderer = chunkObj.AddComponent<MeshRenderer>();

        float[,] levelMap = heightMaps[lod];
        heightMap = ExtractCorrectMap(levelMap);

        meshRenderer.material = meshVariables.material;

        mesh = mg.GenerateMesh(lod, chunkCoord, heightMap);
        meshFilter.mesh = mesh;
        isLoaded = true;
    }

    public void ReDraw(int lod)
    {
        this.lod = lod;
        mesh.Clear();

        // Destroy the mesh
        if (mesh != null)
        {
            //meshFilter.mesh = null;
            //UnityEngine.Object.Destroy(mesh);
            mesh = null;
        }
        isLoaded = false;

        mesh = mg.GenerateMesh(lod, chunkCoord, heightMap);
        meshFilter.mesh = mesh;
        isLoaded = true;
    }

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

    private NativeArray<float> ExtractCorrectMap(float[,] levelMap)
    {
        int levelWidth = chunkParameters.chunkWidth / (1 << lod);
        int levelVertsWidth = levelWidth + 1;

        NativeArray<float> map = new NativeArray<float>((int)Mathf.Pow(levelVertsWidth,2), Allocator.Persistent);
        for (int x = 0; x < levelVertsWidth; x++)
        {
            for (int y = 0; y < levelVertsWidth; y++)
            {
                map[x * levelVertsWidth + y] = levelMap[x+(levelWidth*chunkCoord.x), y+(levelWidth*chunkCoord.y)];
            }
        }
        return map;
    }

    public void D()
    {
        // Destroy the mesh
        if (mesh != null)
        {
            UnityEngine.Object.Destroy(mesh);
            mesh = null;
        }

        // Destroy the GameObject
        if (chunkObj != null)
        {
            UnityEngine.Object.Destroy(chunkObj);
            chunkObj = null;
        }

    }
}

[Serializable]
public struct ChunkParameters
{
    [Range(1,8)]
    public int chunkSizeFactor;

    [Range(1, 50)]
    public int chunksToCalculate;

    [Range(1, 6)]
    public int levelWidth;

    public int chunkWidth => 1 << chunkSizeFactor;
}