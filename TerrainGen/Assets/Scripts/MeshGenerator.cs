using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using System;
using Unity.Mathematics;

[BurstCompile]
public struct MeshGenerator
{
    private TerrainMeshVariables _meshVariables;
    private ChunkParameters _chunkParameters;
    [NativeDisableParallelForRestriction] private NativeArray<Vector3> _verticies;
    [NativeDisableParallelForRestriction] private NativeArray<int> _triangleIndicies;

    public MeshGenerator(TerrainMeshVariables mv, ChunkParameters cp, int lod)
    {
        _meshVariables = mv;
        _chunkParameters = cp;

        int vertsPerSide = _chunkParameters.chunkWidth / (1 << lod) + 1;
        int totalVertices = vertsPerSide * vertsPerSide;
        int totalTris = (vertsPerSide-1) * (vertsPerSide-1) * 6;

        _verticies = new NativeArray<Vector3>(totalVertices, Allocator.TempJob);
        _triangleIndicies = new NativeArray<int>(totalTris, Allocator.TempJob);
    }

    public Mesh GenerateMesh(int levelOfDetail, int2 chunkCoord, NativeArray<float> heightMap)
    {
        int level = levelOfDetail;
        int k = (_chunkParameters.chunkWidth / (1 << level));
        float initX = (_chunkParameters.chunkWidth * chunkCoord.x) * _meshVariables.levelsWidthMultiplier[0];
        float initY = (_chunkParameters.chunkWidth * chunkCoord.y) * _meshVariables.levelsWidthMultiplier[0];
        float xTranslation = (initX / (1 << level)) - (initX);
        float yTranslation = (initY / (1 << level)) - (initY);

        Vector2 translation = new Vector2(xTranslation, yTranslation) * (1 << level);
        for (int j = 0; j < Mathf.Pow(k, 2); j++)
        {
            int x = (j / k) + _chunkParameters.chunkWidth * chunkCoord.x;
            int y = (j % k) + _chunkParameters.chunkWidth * chunkCoord.y;

            int a = j + Mathf.FloorToInt(j / (_chunkParameters.chunkWidth / (1 << level)));
            int b = a + 1;
            int c = b + _chunkParameters.chunkWidth / (1 << level);
            int d = c + 1;

            Vector2 tl = (new Vector2(x, y) * _meshVariables.levelsWidthMultiplier[level]) + translation;
            Vector2 tr = (new Vector2(x + 1, y) * _meshVariables.levelsWidthMultiplier[level]) + translation;
            Vector2 bl = (new Vector2(x, y + 1) * _meshVariables.levelsWidthMultiplier[level]) + translation;
            Vector2 br = (new Vector2(x + 1, y + 1) * _meshVariables.levelsWidthMultiplier[level]) + translation;

            _verticies[a] = new Vector3(tl.x, heightMap[a], tl.y);
            _verticies[b] = new Vector3(bl.x, heightMap[b], bl.y);
            _verticies[c] = new Vector3(tr.x, heightMap[c], tr.y);
            _verticies[d] = new Vector3(br.x, heightMap[d], br.y);

            _triangleIndicies[j * 6 + 0] = a;
            _triangleIndicies[j * 6 + 1] = b;
            _triangleIndicies[j * 6 + 2] = c;
            _triangleIndicies[j * 6 + 3] = b;
            _triangleIndicies[j * 6 + 4] = d;
            _triangleIndicies[j * 6 + 5] = c;
        }

        return DisposeAndGetMesh();
    }

    private readonly string float2string(NativeArray<float> f)
    {
        string s = "";
        foreach (float v in f)
        {
            s += v.ToString() + ", ";
        }
        return s;
    }

    public Mesh DisposeAndGetMesh()
    {
        // create and assign values to mesh
        var m = new Mesh();

        m.SetVertices(_verticies);
        m.triangles = _triangleIndicies.ToArray();
        m.RecalculateNormals();

        // Away with the memory hoarding!! (dispose the native arrays from memory)
        _verticies.Dispose();
        _triangleIndicies.Dispose();

        return m;
    }
}
