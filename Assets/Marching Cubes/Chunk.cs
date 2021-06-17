using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public class Chunk : MonoBehaviour
{
    [HideInInspector]
    public Mesh mesh;

    public Vector3Int coord;
    public bool drawChunkOutline = true;

    public Points points;



    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    bool generateCollider;

    public void PrintName()
    {
        Debug.Log(this.gameObject.name);
    }

    public void DestroyOrDisable()
    {
        if (Application.isPlaying)
        {
            mesh.Clear();
            gameObject.SetActive(false);
        }
        else
        {
            DestroyImmediate(gameObject, false);
        }
    }

    public void SetUp(Material mat, bool generateCollider)
    {
        this.generateCollider = generateCollider;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (meshCollider == null && generateCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        if (meshCollider != null && !generateCollider)
        {
            DestroyImmediate(meshCollider);
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        if (generateCollider)
        {
            if (meshCollider.sharedMesh == null)
            {
                meshCollider.sharedMesh = mesh;
            }
            // force update
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }

        meshRenderer.material = mat;
    }

    public void UpdateChunkSize(ChunkSize mSize)
    {
        int size = (int)mSize;

        this.points = new Points();
        int x = this.coord.x * size;
        int y = this.coord.y * size;
        int z = this.coord.z * size;

        points.TopOne = new Vector3(x, y, z);
        points.TopTwo = new Vector3(x - size, y, z);
        points.TopThree = new Vector3(x - size, y, z - size);
        points.TopFour = new Vector3(x, y, z - size);

        points.BottomOne = new Vector3(x, y - size, z);
        points.BottomTwo = new Vector3(x - size, y - size, z);
        points.BottomThree = new Vector3(x - size, y - size, z - size);
        points.BottomFour = new Vector3(x, y - size, z - size);
    }

}

[System.Serializable]
public class Points
{
    public Vector3 TopOne;
    public Vector3 TopTwo;
    public Vector3 TopThree;
    public Vector3 TopFour;
    public Vector3 BottomOne;
    public Vector3 BottomTwo;
    public Vector3 BottomThree;
    public Vector3 BottomFour;
    
}
