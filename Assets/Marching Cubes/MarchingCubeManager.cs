using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldTypes
{
    Infinite = 0,
    Finite = 1,
    Spherical = 2,
    Cavernous = 3
}

public enum WorldSize
{
    Miniscule,
    Tiny,
    Small,
    Medium,
    Large,
    Custom
}

public enum ChunkSize
{
    Is32MetersCu = 32,
    Is40MetersSq = 40,
    Is256MetersCu = 256,
    Is512MetersCu = 512,
    Is1024MetersCu = 1024,
    Is2048MetersCu = 2048,
    Is4096MetersCu = 4096,
    Is8192MetersCu = 8192,
    Is16384MetersCu = 16384
}


[ExecuteInEditMode]
public class MarchingCubeManager : MonoBehaviour
{
    public ComputeShader MarchingCubesShader;
    public ComputeShader NoiseDensity;
    public ComputeShader SphereDensity;

    public WorldTypes WorldType = WorldTypes.Infinite;
    public WorldSize WorldSize = WorldSize.Tiny;

    public int XChunks = 1;
    public int YChunks = 1;
    public int ZChunks = 1;

    public ChunkSize ChunkSize = ChunkSize.Is32MetersCu;
    public int chunkSize = 32;

    public bool AutoUpdateInEditor = true;
    public bool AutoUpdateInGame = true;
    public bool SmoothTerrain = false;
    public bool FlatShaded = false;
    public bool GenerateCollider = false;


    public float TerrainHeight = 8f;
    public float NoiseThreshold = 0.6f;
    public float IsoLevel = 2.2f;
    public float Density = 4f;
    public int NumPointsPerAxis = 45;
    public int seed = 651615;
    public int CellSize = 1;
    public float ChunkEnableDistance = 100f;
    GameObject chunkHolder;
    string chunkHolderName = "Terrains";


     
    public Material terrainMaterial;
    public static MarchingCubeManager instance;

    public bool drawChunkOutline = false;
    public float chunkOutlineThickness = 5f;
    public Color chunkOutlineColor = Color.black;

    public List<Chunk> chunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    Queue<Chunk> recycleableChunks;

    public List<MeshGenerator> generators;
    public DensityGenerator densityGenerator;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;

    const int threadGroupSize = 8;
    public bool settingsUpdated;

    private void OnEnable()
    {
        densityGenerator = this.GetComponent<DensityGenerator>();
        instance = this;
    }

    private void Awake()
    {
        instance = this;
        if (Application.isPlaying && WorldType != WorldTypes.Finite)
        {
            InitVariableChunkStructures();

            var oldChunks = FindObjectsOfType<Chunk>();
            for (int i = oldChunks.Length - 1; i >= 0; i--)
            {
                Destroy(oldChunks[i].gameObject);
            }
        }
    }

    void InitVariableChunkStructures()
    {
        recycleableChunks = new Queue<Chunk>();
        chunks = new List<Chunk>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
    }

    public void Update()
    { 
        // Update endless terrain
        if ((Application.isPlaying && WorldType != WorldTypes.Finite))
        {
            Run();
        }

        if (settingsUpdated)
        {
            RequestMeshUpdate();
            settingsUpdated = false;
        }
    }

    public void Run()
    {
        CreateBuffers();
        if (WorldType == WorldTypes.Finite)
        {
            InitChunks();
            UpdateAllChunks();
        }
        else
        {
            if (Application.isPlaying)
            {
                //InitVisibleChunks();
            }
        }

        // Release buffers immediately in editor
        if (!Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    public void UpdateAllChunks()
    {

        // Create mesh for each chunk
        foreach (Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk);
        }

    }

    public void RequestMeshUpdate()
    {
        if ((Application.isPlaying && AutoUpdateInGame) || (!Application.isPlaying && AutoUpdateInEditor))
        {
            Run();
        }
    }

    public void InitChunks()
    {
        CreateChunkHolder();
        chunks = new List<Chunk>();
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());
     

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < XChunks; x++)
        {
            for (int y = 0; y < YChunks; y++)
            {
                for (int z = 0; z < ZChunks; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    bool chunkAlreadyExists = false;

                    // If chunk already exists, add it to the chunks list, and remove from the old list.
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if (oldChunks[i].coord == coord)
                        { 
                            chunks.Add(oldChunks[i]);
                            oldChunks.RemoveAt(i);
                            chunkAlreadyExists = true;
                            break;
                        }
                    }

                    // Create new chunk
                    if (!chunkAlreadyExists)
                    {
                        var newChunk = CreateChunk(coord);
                        chunks.Add(newChunk);
                    }

                    chunks[chunks.Count - 1].SetUp(terrainMaterial, GenerateCollider);
                }
            }
        }

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestroyOrDisable();
        }
    }

    void CreateChunkHolder()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null)
        {
            if (GameObject.Find(chunkHolderName))
            {
                chunkHolder = GameObject.Find(chunkHolderName);
            }
            else
            {
                chunkHolder = new GameObject(chunkHolderName);
            }
        }
    }

    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();
        newChunk.coord = coord;
        newChunk.UpdateChunkSize(this.ChunkSize);
        return newChunk;
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    public Vector3 CentreFromCoord(Vector3Int coord)
    {
        // Centre entire map at origin
        if (WorldType == WorldTypes.Finite)
        {
            Vector3 totalBounds = new Vector3(XChunks, YChunks, ZChunks) * chunkSize;
            return -totalBounds / 2 + (Vector3)coord * chunkSize + Vector3.one * chunkSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * chunkSize;
    }

    public void UpdateChunkMesh(Chunk chunk)
    {
        int numVoxelsPerAxis = NumPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        int chunkSize = (int)ChunkSize;
        float boundSize = (float)chunkSize;
        float pointSpacing = boundSize / (NumPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(XChunks, YChunks, ZChunks) * boundSize;

        densityGenerator.Generate(pointsBuffer, NumPointsPerAxis, boundSize, worldBounds, centre, new Vector3(-0.64f,0,0), pointSpacing);
        

        triangleBuffer.SetCounterValue(0);
        MarchingCubesShader.SetBuffer(0, "points", pointsBuffer);
        MarchingCubesShader.SetBuffer(0, "triangles", triangleBuffer);
        MarchingCubesShader.SetInt("numPointsPerAxis", NumPointsPerAxis);
        MarchingCubesShader.SetFloat("isoLevel", IsoLevel);

        MarchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }

    void CreateBuffers()
    {
        int numPoints = NumPointsPerAxis * NumPointsPerAxis * NumPointsPerAxis;
        int numVoxelsPerAxis = NumPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || (pointsBuffer == null || numPoints != pointsBuffer.count))
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        }
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }
    }


    public void GenerateRandomSeed()
    {
        seed = Random.Range(0, int.MaxValue);
        if (this.WorldType != WorldTypes.Spherical)
            this.GetComponent<NoiseDensity>().seed = seed;
    }

    public void UpdateWorldType()
    {
        DestroyImmediate(densityGenerator, false);
        if(WorldType == WorldTypes.Spherical)
        {
            this.densityGenerator = this.gameObject.AddComponent<SphereDensity>();
            this.densityGenerator.densityShader = SphereDensity;
        }
        else
        {
            this.densityGenerator = this.gameObject.AddComponent<NoiseDensity>();
            this.densityGenerator.densityShader = NoiseDensity;
        }
    }

    public int SetChunkSize()
    {
        this.chunkSize = (int)ChunkSize;
        foreach(Chunk c in this.chunks)
        {
            c.UpdateChunkSize(this.ChunkSize);
        }
        return this.chunkSize;
    }

    public void CreateTerrain()
    {
        Run();
    }

    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
}
