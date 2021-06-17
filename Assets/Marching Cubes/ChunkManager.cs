using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance;

    public List<Chunk> chunks;

    private void Awake()
    {
        instance = this;
        chunks = new List<Chunk>();
    }


}
