using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingCubeManager))]
[ExecuteInEditMode]
public class MarchingCubesEditor : Editor
{

    private float sectionSpacing = 10f;

    bool showWorldSettings = true;
    bool showMarchingCubeSettings = true;
    bool showCellSettings = true;
    bool showChunkGeneration = true;
    bool showDebugSettings = true;

    bool showAdvancedWorldSettings = false;
    string advWorldSettingsText = "+";
    float originalLabelWidth = 0;
    int totalWorldSizeX = 0;
    int totalWorldSizeY = 0;
    int totalWorldSizeZ = 0;
    static int currentChunkSize = 32;

    ComputeShader MarchingCubesGenerator;
    ComputeShader NoiseDensity;
    ComputeShader SphereDensity;


    public static List<Line> lines;
    public static MarchingCubeManager script;

    private void OnEnable()
    {
        script = target as MarchingCubeManager;

        if (MarchingCubesGenerator == null)
            MarchingCubesGenerator = (ComputeShader)Resources.Load("Compute/MarchingCubes");

        if (MarchingCubesGenerator == null)
            Debug.LogError("Could not load Marching Cubes Compute Shader!");

        if (NoiseDensity == null)
            NoiseDensity = (ComputeShader)Resources.Load("Compute/NoiseDensity");

        if (NoiseDensity == null)
            Debug.LogError("Could not load NoiseDensity Compute Shader!");

        if (SphereDensity == null)
            SphereDensity = (ComputeShader)Resources.Load("Compute/SphereDensity");

        if (SphereDensity == null)
            Debug.LogError("Could not load SphereDensity Compute Shader!");


        SceneView.duringSceneGui += OnSceneGUI;

        script.MarchingCubesShader = MarchingCubesGenerator;
        script.NoiseDensity = NoiseDensity;
        script.SphereDensity = SphereDensity;

        lines = new List<Line>();
        originalLabelWidth = EditorGUIUtility.labelWidth;
        totalWorldSizeX = script.XChunks * currentChunkSize;
        totalWorldSizeZ = script.ZChunks * currentChunkSize;
        //GenerateLines();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        #region World Settings
        EditorGUILayout.BeginVertical();
        showWorldSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showWorldSettings, "World Settings");
        if (showWorldSettings)
        {
            EditorGUI.BeginChangeCheck();
            script.WorldType = (WorldTypes)EditorGUILayout.EnumPopup("World Type", script.WorldType);
            if (EditorGUI.EndChangeCheck())
            {
                script.UpdateWorldType();
            }
            EditorGUI.indentLevel++;
            if (script.WorldType == WorldTypes.Finite)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                script.WorldSize = (WorldSize)EditorGUILayout.EnumPopup("World Size", script.WorldSize);
                if (EditorGUI.EndChangeCheck())
                {
                    if (script.WorldSize == WorldSize.Miniscule)
                    {
                        script.ChunkSize = ChunkSize.Is32MetersCu;
                        script.XChunks = 1;
                        script.YChunks = 1;
                        script.ZChunks = 1;
                        script.SetChunkSize();
                    }
                    if (script.WorldSize == WorldSize.Tiny)
                    {
                        script.ChunkSize = ChunkSize.Is256MetersCu;
                        script.XChunks = 1;
                        script.YChunks = 1;
                        script.ZChunks = 1;
                        script.SetChunkSize();
                    }
                    if (script.WorldSize == WorldSize.Small)
                    {
                        script.ChunkSize = ChunkSize.Is512MetersCu;
                        script.XChunks = 1;
                        script.YChunks = 1;
                        script.ZChunks = 1;
                        script.SetChunkSize();
                    }
                    if (script.WorldSize == WorldSize.Medium)
                    {
                        script.ChunkSize = ChunkSize.Is1024MetersCu;
                        script.XChunks = 1;
                        script.YChunks = 1;
                        script.ZChunks = 1;
                        script.SetChunkSize();
                    }
                    if (script.WorldSize == WorldSize.Large)
                    {
                        script.ChunkSize = ChunkSize.Is2048MetersCu;
                        script.XChunks = 1;
                        script.YChunks = 1;
                        script.ZChunks = 1;
                        script.SetChunkSize();
                    }
                    totalWorldSizeX = script.XChunks * currentChunkSize;
                    totalWorldSizeY = script.YChunks * currentChunkSize;
                    totalWorldSizeZ = script.ZChunks * currentChunkSize;
                    //GenerateLines();
                }
                if (GUILayout.Button(advWorldSettingsText, GUILayout.Width(25f)))
                {
                    showAdvancedWorldSettings = !showAdvancedWorldSettings;

                    advWorldSettingsText = showAdvancedWorldSettings ? "-" : "+";
                }
                EditorGUILayout.EndHorizontal();

                if (showAdvancedWorldSettings)
                {
                    EditorGUILayout.LabelField("Advanced World Settings: ", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 80f;
                    EditorGUI.BeginChangeCheck();
                    script.XChunks = EditorGUILayout.IntField("X Chunks", script.XChunks, GUILayout.ExpandWidth(true));
                    script.YChunks = EditorGUILayout.IntField("Y Chunks", script.YChunks, GUILayout.ExpandWidth(true));
                    script.ZChunks = EditorGUILayout.IntField("Z Chunks", script.ZChunks, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = originalLabelWidth;
                    EditorGUILayout.Space();
                    script.ChunkSize = (ChunkSize)EditorGUILayout.EnumPopup("Chunk Size", script.ChunkSize);
                    if (EditorGUI.EndChangeCheck())
                    {
                        currentChunkSize = script.SetChunkSize();

                        if (script.XChunks > 1 || script.ZChunks > 1 || currentChunkSize > 2048)
                        {
                            script.WorldSize = WorldSize.Custom;
                        }
                        else if (currentChunkSize == 32)
                            script.WorldSize = WorldSize.Miniscule;
                        else if (currentChunkSize == 256)
                            script.WorldSize = WorldSize.Tiny;
                        else if (currentChunkSize == 512)
                            script.WorldSize = WorldSize.Small;
                        else if (currentChunkSize == 1024)
                            script.WorldSize = WorldSize.Medium;
                        else if (currentChunkSize == 2048)
                            script.WorldSize = WorldSize.Large;


                        script.XChunks = Mathf.Clamp(script.XChunks, 1, 512);
                        script.YChunks = Mathf.Clamp(script.YChunks, 1, 512);
                        script.ZChunks = Mathf.Clamp(script.ZChunks, 1, 512);

                        totalWorldSizeX = script.XChunks * currentChunkSize;
                        totalWorldSizeZ = script.ZChunks * currentChunkSize;



                        script.InitChunks();
                        // GenerateLines();
                    }
                    EditorGUILayout.Space();
                    script.TerrainHeight = EditorGUILayout.Slider("Terrain Height", script.TerrainHeight, 16f, currentChunkSize);
                    EditorGUILayout.LabelField($"Total World Size: {totalWorldSizeX}m x {totalWorldSizeY}m {totalWorldSizeZ}m, Chunks: {script.XChunks * script.ZChunks * script.YChunks}", EditorStyles.boldLabel);

                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Generate Chunks"))
                {
                    script.InitChunks();
                    //GenerateLines();
                }
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(sectionSpacing);
        #endregion

        #region Marching Cube Settings
        showMarchingCubeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showMarchingCubeSettings, "Marching Cube Settings");
        if (showMarchingCubeSettings)
        {
            EditorGUI.indentLevel++;

            script.AutoUpdateInEditor = EditorGUILayout.Toggle("Auto Update In Editor", script.AutoUpdateInEditor);
            script.AutoUpdateInGame = EditorGUILayout.Toggle("Auto Update In Game", script.AutoUpdateInGame);
            script.SmoothTerrain = EditorGUILayout.Toggle("Smooth Terrain", script.SmoothTerrain);
            script.FlatShaded = EditorGUILayout.Toggle("Flat Shaded", script.FlatShaded);
            script.GenerateCollider = EditorGUILayout.Toggle("Generate Colliders", script.GenerateCollider);
            script.NoiseThreshold = EditorGUILayout.Slider("Noise Threshold", script.NoiseThreshold, 0f, 1f);
            script.IsoLevel = EditorGUILayout.FloatField("Iso Level", script.IsoLevel);
            script.Density = EditorGUILayout.Slider("Density", script.Density, 1f, 10f);
            script.NumPointsPerAxis = EditorGUILayout.IntSlider("Num Points Per Axis", script.NumPointsPerAxis, 2, 100);
            script.seed = EditorGUILayout.IntField("Seed", script.seed);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainMaterial"));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Generate Random Seed"))
            {
                script.GenerateRandomSeed();
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(sectionSpacing);
        #endregion

        #region Cell Settings
        showCellSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showCellSettings, "Cell Settings");
        if (showCellSettings)
        {
            EditorGUI.indentLevel++;

            script.CellSize = EditorGUILayout.IntSlider("Cell Size", script.CellSize, 1, 10);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(sectionSpacing);
        #endregion

        #region Chunk Generation
        showChunkGeneration = EditorGUILayout.BeginFoldoutHeaderGroup(showChunkGeneration, "Chunk Generation");
        if (showChunkGeneration)
        {
            EditorGUI.indentLevel++;

            script.ChunkEnableDistance = EditorGUILayout.FloatField("Chunk Enable Distance", script.ChunkEnableDistance);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(sectionSpacing);
        #endregion

        #region Debug
        showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugSettings, "Debug");
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            script.drawChunkOutline = EditorGUILayout.Toggle("Draw Chunk Outline", script.drawChunkOutline);
            if (EditorGUI.EndChangeCheck() && script.drawChunkOutline)
            {
                //GenerateLines();
            }
            script.chunkOutlineThickness = EditorGUILayout.Slider("Outline Thickness", script.chunkOutlineThickness, 0.1f, 5f);
            script.chunkOutlineColor = EditorGUILayout.ColorField("Outline Color", script.chunkOutlineColor);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(sectionSpacing);
        #endregion

        if (GUILayout.Button("Create Terrain"))
        {
            script.CreateTerrain();
        }

        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            script.settingsUpdated = true;
            script.Update();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }




    private static void OnSceneGUI(SceneView sceneView)
    {
        if (script.drawChunkOutline && script.chunks.Count > 0)
        {
            foreach (Chunk c in script.chunks)
            {
                if (c.drawChunkOutline)
                {
                    Handles.DrawBezier(c.points.TopOne, c.points.TopTwo, c.points.TopOne, c.points.TopTwo, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopTwo, c.points.TopThree, c.points.TopTwo, c.points.TopThree, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopThree, c.points.TopFour, c.points.TopThree, c.points.TopFour, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopFour, c.points.TopOne, c.points.TopFour, c.points.TopOne, script.chunkOutlineColor, null, script.chunkOutlineThickness);

                    Handles.DrawBezier(c.points.BottomOne, c.points.BottomTwo, c.points.BottomOne, c.points.BottomTwo, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.BottomTwo, c.points.BottomThree, c.points.BottomTwo, c.points.BottomThree, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.BottomThree, c.points.BottomFour, c.points.BottomThree, c.points.BottomFour, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.BottomFour, c.points.BottomOne, c.points.BottomFour, c.points.BottomOne, script.chunkOutlineColor, null, script.chunkOutlineThickness);

                    Handles.DrawBezier(c.points.TopOne, c.points.BottomOne, c.points.TopOne, c.points.BottomOne, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopTwo, c.points.BottomTwo, c.points.TopTwo, c.points.BottomTwo, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopThree, c.points.BottomThree, c.points.TopThree, c.points.BottomThree, script.chunkOutlineColor, null, script.chunkOutlineThickness);
                    Handles.DrawBezier(c.points.TopFour, c.points.BottomFour, c.points.TopFour, c.points.BottomFour, script.chunkOutlineColor, null, script.chunkOutlineThickness);

                }
            }

        }

        //void GenerateLines()
        //{
        //    if (!script.drawChunkOutline)
        //        return;

        //    lines.Clear();
        //    lines.TrimExcess();
        //    List<Chunk> mChunks = new List<Chunk> (FindObjectsOfType<Chunk>());
        //    Chunk c = mChunks[0];

        //    Vector3[,,] startPoints = new Vector3[1,1,1];
        //    Vector3[,,] endPoints = new Vector3[1,1,1];

        //    for (int x = 0; x < 5; x++)
        //    {
        //        for (int y = 0; y < 9; y++)
        //        {
        //            for(int z = 0; z < 9; z++)
        //            {
        //                startPoints[x,y,z] = new Vector3((c.coord.x + x) - currentChunkSize, (c.coord.y + y), (c.coord.z + z));
        //                endPoints[x, y, z] = new Vector3((c.coord.x + x), (c.coord.y + y), (c.coord.z + z));
        //            }
        //        }

        //    }

        //    /*Vector3 Zero = c.coord;
        //    Vector3 X1End = new Vector3(c.coord.x - currentChunkSize, c.coord.y, c.coord.z);
        //    if (!lines.Any(x => x.Start == Zero && x.End == X1End))
        //        lines.Add(new Line(Zero, X1End));
        //    Vector3 X2Start = new Vector3(c.coord.x, c.coord.y, c.coord.z - currentChunkSize);
        //    Vector3 X2End = new Vector3(c.coord.x - currentChunkSize, c.coord.y, c.coord.z - currentChunkSize);
        //    if (!lines.Any(x => x.Start == X2Start && x.End == X2End))
        //        lines.Add(new Line(X2Start, X2End));
        //    Vector3 X3Start = new Vector3(c.coord.x - currentChunkSize, c.coord.y, c.coord.z);
        //    Vector3 X3End = new Vector3(c.coord.x - currentChunkSize, c.coord.y, c.coord.z - currentChunkSize);
        //    if (!lines.Any(x => x.Start == X3Start && x.End == X3End))
        //        lines.Add(new Line(X3Start, X3End));
        //    Vector3 L4Start = new Vector3(c.coord.x, c.coord.y, c.coord.z);
        //    Vector3 L4End = new Vector3(c.coord.x, c.coord.y, c.coord.z - currentChunkSize);
        //    if (!lines.Any(x => x.Start == L4Start && x.End == L4End))
        //        lines.Add(new Line(L4Start, L4End));



        //    Debug.Log(lines.Count);
        //    /*foreach (Chunk c in mChunks)
        //    {
        //        Vector3 mCoord = script.CentreFromCoord(c.coord);
        //        Vector3 Zero = c.coord;
        //        //Length Values
        //        Vector3 L1End = new Vector3(mCoord.x, mCoord.y, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == Zero && x.End == L1End))
        //            lines.Add(new Line(Zero, L1End));
        //        Vector3 L2Start = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z);
        //        Vector3 L2End = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize));
        //        if (!lines.Any(x => x.Start == L2Start && x.End == L2End))
        //            lines.Add(new Line(L2Start, L2End));
        //        Vector3 L3Start = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z);
        //        Vector3 L3End = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == L3Start && x.End == L3End))
        //            lines.Add(new Line(L3Start, L3End));
        //        Vector3 L4Start = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z);
        //        Vector3 L4End = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == L4Start && x.End == L4End))
        //            lines.Add(new Line(L4Start, L4End));




        //        //Width Values
        //        Vector3 W1End = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z);
        //        if (!lines.Any(x => x.Start == Zero && x.End == W1End))
        //            lines.Add(new Line(Zero, W1End));
        //        Vector3 W2Start = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z);
        //        Vector3 W2End = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z);
        //        if (!lines.Any(x => x.Start == W2Start && x.End == W2End))
        //            lines.Add(new Line(W2Start, W2End));
        //        Vector3 W3Start = new Vector3(mCoord.x, mCoord.y, mCoord.z + currentChunkSize);
        //        Vector3 W3End = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == W3Start && x.End == W3End))
        //            lines.Add(new Line(W3Start, W3End));
        //        Vector3 W4Start = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize);
        //        Vector3 W4End = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == W4Start && x.End == W4End))
        //            lines.Add(new Line(W4Start, W4End));

        //        //Depth Values
        //        Vector3 H1End = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z);
        //        if (!lines.Any(x => x.Start == Zero && x.End == H1End))
        //            lines.Add(new Line(Zero, H1End));
        //        Vector3 H2Start = new Vector3(mCoord.x, mCoord.y, mCoord.z + currentChunkSize);
        //        Vector3 H2End = new Vector3(mCoord.x, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == H2Start && x.End == H2End))
        //            lines.Add(new Line(H2Start, H2End));
        //        Vector3 H3Start = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z);
        //        Vector3 H3End = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z);
        //        if (!lines.Any(x => x.Start == H3Start && x.End == H3End))
        //            lines.Add(new Line(H3Start, H3End));
        //        Vector3 H4Start = new Vector3(mCoord.x + currentChunkSize, mCoord.y, mCoord.z + currentChunkSize);
        //        Vector3 H4End = new Vector3(mCoord.x + currentChunkSize, mCoord.y + currentChunkSize, mCoord.z + currentChunkSize);
        //        if (!lines.Any(x => x.Start == H4Start && x.End == H4End))
        //            lines.Add(new Line(H4Start, H4End));
        //}*/
        //}
    }

    public class Line
    {
        public Vector3 Start;
        public Vector3 End;

        public Line(Vector3 _start, Vector3 _end)
        {
            Start = _start;
            End = _end;
        }
    }
}
