//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// High level interface for the ZED's Spatial Mapping features. Allows you to scan your environment into a 3D mesh.
/// The scan will appear as a set of "chunk" meshes under a GameObject called "[ZED Mesh Holder] created at runtime. 
/// The mesh can be used once scanning is finished after a brief finalizing/filtering/texturing period, and saved into 
/// an .obj, .ply or .bin if desired. It will also get a MeshCollider cadded to it, so that virtual objects can collide with it. 
/// Saving a scan made with this class also saves an .area file in the same location that can be used by the ZED's Spatial Memory
/// feature for better tracking localization. 
/// Most of the spatial mapping implementation is handled in ContinuousSpatialMapping.cs, but this class simplifies its use. 
/// For more information and a tutorial, see our documentation: https://docs.stereolabs.com/mixed-reality/unity/spatial-mapping-unity/
/// </summary>
[DisallowMultipleComponent]
public class ContinuousSpatialMappingManager : MonoBehaviour
{
    /// <summary>
    /// Resolution setting for the scan. A higher resolution creates more submeshes and uses more memory, but is more accurate.
    /// </summary>
	public ContinuousSpatialMapping.RESOLUTION resolution_preset = ContinuousSpatialMapping.RESOLUTION.MEDIUM;

    /// <summary>
    /// Maximum distance geometry can be from the camera to be scanned. Geometry scanned from farther away will be less accurate. 
    /// </summary>
	public ContinuousSpatialMapping.RANGE range_preset = ContinuousSpatialMapping.RANGE.MEDIUM;

    public bool startScanOnAwake;

    public Material wireframeMaterial;

    [Range(0, 1)]
    public float renderDistance = 0.3f;

    [Range(1f, 30f)]
    public float timeout;

    /// <summary>
    /// Instance of the ContinuousSpatialMapping class that handles the actual spatial mapping implementation within Unity. 
    /// </summary>
    private ContinuousSpatialMapping spatialMapping;

    /// <summary>
    /// The scene's ZEDManager instance. Usually attached to the ZED rig root object (ZED_Rig_Mono or ZED_Rig_Stereo). 
    /// </summary>
    private ZEDManager manager;

    /// <summary>
    /// Fills the manager reference and instantiates ContinuousSpatialMapping.
    /// </summary>
    private void Start()
    {
        manager = ZEDManager.Instance;
        spatialMapping = new ContinuousSpatialMapping(transform, sl.ZEDCamera.GetInstance(), manager, wireframeMaterial, timeout, renderDistance);
    }

    /// <summary>
    /// Whether the spatial mapping is currently scanning. 
    /// </summary>
    public bool IsRunning {get { return spatialMapping!= null ? spatialMapping.IsRunning(): false; }}

    /// <summary>
    /// List of the processed submeshes. This list isn't filled until StopSpatialMapping() is called. 
    /// </summary>
    public List<ContinuousSpatialMapping.Chunk> ChunkList { get { return spatialMapping != null ? spatialMapping.ChunkList : null; } }

    /// <summary>
    /// Whether the mesh update thread is running. 
    /// </summary>
    public bool IsUpdateThreadRunning { get { return spatialMapping != null ? spatialMapping.IsUpdateThreadRunning: false; } }

    /// <summary>
    /// Whether the spatial mapping was running but has been paused (not stopped) by the user. 
    /// </summary>
    public bool IsPaused { get { return spatialMapping != null ? spatialMapping.IsPaused :false; } }

    /// <summary>
    /// Whether the mesh is in the texturing stage of finalization. 
    /// </summary>
    public bool IsTexturingRunning { get { return spatialMapping != null ? spatialMapping.IsTexturingRunning : false; } }

    private void OnEnable()
    {
        ContinuousSpatialMapping.OnMeshReady += SpatialMappingHasStopped;
        ZEDManager.OnZEDReady += OnZedManagerReady;
    }

    private void OnDisable()
    {
        ContinuousSpatialMapping.OnMeshReady -= SpatialMappingHasStopped;
        ZEDManager.OnZEDReady -= OnZedManagerReady;
    }

    void OnZedManagerReady()
    {
        if (startScanOnAwake)
        {
            if (!ContinuousSpatialMapping.display)
            {
                SwitchDisplayMeshState(true);
            }
            StartSpatialMapping();
        }
    }

    /// <summary>
    /// Saves the mesh once it's finished, if saveWhenOver is set to true. 
    /// </summary>
    void SpatialMappingHasStopped()
    {
    }

    /// <summary>
    /// Tells ContinuousSpatialMapping to begin a new scan. This clears the previous scan from the scene if there is one. 
    /// </summary>
    public void StartSpatialMapping()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        spatialMapping.StartStatialMapping(resolution_preset, range_preset, false);
    }

    /// <summary>
    /// Ends the current spatial mapping. Once called, the current mesh will be filtered, textured (if enabled) and saved (if enabled), 
    /// and a mesh collider will be added. 
    /// </summary>
    public void StopSpatialMapping()
    {
        spatialMapping.StopStatialMapping();
    }

    /// <summary>
    /// Updates the filtering parameters and call the ContinuousSpatialMapping instance's Update() function. 
    /// </summary>
    private void Update()
    {
        if (spatialMapping != null)
        {
            spatialMapping.Update(); //As ContinuousSpatialMapping doesn't inherit from Monobehaviour, this doesn't happen automatically. 
        }
    }

    /// <summary>
    /// Properly clears existing scan data when the application is closed. 
    /// </summary>
    private void OnApplicationQuit()
    {
        spatialMapping.Dispose();
    }

    /// <summary>
    /// Toggles whether to display the mesh or not. 
    /// </summary>
    /// <param name="state"><c>True</c> to make the mesh visible, <c>false</c> to make it invisible. </param>
    public void SwitchDisplayMeshState(bool state)
    {
        spatialMapping.SwitchDisplayMeshState(state);
    }

    /// <summary>
    /// Pauses the current scan. 
    /// </summary>
    /// <param name="state"><c>True</c> to pause the scanning, <c>false</c> to unpause it.</param>
    public void SwitchPauseState(bool state)
    {
        spatialMapping.SwitchPauseState(state);
    }

}
#if UNITY_EDITOR

/// <summary>
/// Custom Inspector screen for ContinuousSpatialMappingManager. 
/// Displays values in an organized manner, and adds buttons for starting/stopping spatial
/// mapping and hiding/displaying the resulting mesh. 
/// </summary>
[CustomEditor(typeof(ContinuousSpatialMappingManager))]
public class ContinuousSpatialMappingEditor : Editor
{
    /// <summary>
    /// The ContinuousSpatialMappingManager component this editor instance is editing. 
    /// </summary>
    private ContinuousSpatialMappingManager spatialMapping;

    /// <summary>
    /// Layout option used to draw the '...' button for opening a File Explorer window to find a mesh file. 
    /// </summary>
    // private GUILayoutOption[] optionsButtonBrowse = { GUILayout.MaxWidth(30) };

    /// <summary>
    /// Text on the mesh visibility button. Switches between 'Hide Mesh' and 'Display Mesh'.
    /// </summary>
    // private string displayText = "Hide Mesh";

    ///Serialized properties used to apply and save changes. 

    /// <summary>
    /// Serialized version of ContinuousSpatialMappingManager's range_preset property. 
    /// </summary>
    private SerializedProperty range;
    /// <summary>
    /// Serialized version of ContinuousSpatialMappingManager's resolution_preset property. 
    /// </summary>
    private SerializedProperty resolution;

    private SerializedProperty startScanOnAwake;

    private SerializedProperty wireframeMaterial;

    private SerializedProperty renderDistance;

    private SerializedProperty timeout;

    /// <summary>
    /// Public accessor to the ContinuousSpatialMappingManager component this editor instance is editing. 
    /// </summary>
    private ContinuousSpatialMappingManager Target
    {
        get { return (ContinuousSpatialMappingManager)target; }
    }

    public void OnEnable()
    {
        //Bind the serialized properties to their respective properties in ContinuousSpatialMappingManager.
        spatialMapping = (ContinuousSpatialMappingManager)target;
        range = serializedObject.FindProperty("range_preset");
        resolution = serializedObject.FindProperty("resolution_preset");
        startScanOnAwake = serializedObject.FindProperty("startScanOnAwake");
        wireframeMaterial = serializedObject.FindProperty("wireframeMaterial");
        renderDistance = serializedObject.FindProperty("renderDistance");
        timeout = serializedObject.FindProperty("timeout");
    }

    public override bool RequiresConstantRepaint()
    {
        return true;
    }

    public override void OnInspectorGUI()
    {

		bool cameraIsReady = sl.ZEDCamera.GetInstance().IsCameraReady; 
        // displayText = ContinuousSpatialMapping.display ? "Hide Mesh" : "Display Mesh";
        serializedObject.Update();
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Mesh Parameters", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUIContent resolutionlabel = new GUIContent("Resolution", "Resolution setting for the scan. " +
            "A higher resolution creates more submeshes and uses more memory, but is more accurate.");
        ContinuousSpatialMapping.RESOLUTION newResolution = (ContinuousSpatialMapping.RESOLUTION)EditorGUILayout.EnumPopup(resolutionlabel, spatialMapping.resolution_preset);
        if (newResolution != spatialMapping.resolution_preset)
        {
            resolution.enumValueIndex = (int)newResolution;
            serializedObject.ApplyModifiedProperties();
        }

        GUIContent rangelabel = new GUIContent("Range", "Maximum distance geometry can be from the camera to be scanned. " + 
            "Geometry scanned from farther away will be less accurate.");
        ContinuousSpatialMapping.RANGE newRange = (ContinuousSpatialMapping.RANGE)EditorGUILayout.EnumPopup(rangelabel, spatialMapping.range_preset);
        if (newRange != spatialMapping.range_preset)
        {
            range.enumValueIndex = (int)newRange;
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.PropertyField(startScanOnAwake);
        EditorGUILayout.PropertyField(wireframeMaterial);
        EditorGUILayout.PropertyField(renderDistance);
        EditorGUILayout.PropertyField(timeout);

        // GUI.enabled = cameraIsReady; //Gray out below elements if the ZED hasn't been initialized as you can't yet start a scan. 
        
        // EditorGUILayout.BeginHorizontal();
        // if (!spatialMapping.IsRunning)
        // {
        //     GUIContent startmappinglabel = new GUIContent("Start Spatial Mapping", "Begin the spatial mapping process.");
        //     if (GUILayout.Button(startmappinglabel))
        //     {
        //         if (!ContinuousSpatialMapping.display)
        //         {
        //             spatialMapping.SwitchDisplayMeshState(true);
        //         }
        //         spatialMapping.StartSpatialMapping();
        //     }
        // }
        // else
        // {
        //     if (spatialMapping.IsRunning && !spatialMapping.IsUpdateThreadRunning || spatialMapping.IsRunning && spatialMapping.IsTexturingRunning)

        //     {
        //         GUILayout.FlexibleSpace();
        //         GUIContent finishinglabel = new GUIContent("Spatial mapping is finishing", "Please wait - the mesh is being processed.");
        //         GUILayout.Label(finishinglabel);
        //         Repaint();
        //         GUILayout.FlexibleSpace();
        //     }
        //     else
        //     {
        //         GUIContent stopmappinglabel = new GUIContent("Stop Spatial Mapping", "Ends spatial mapping and begins processing the final mesh.");
        //         if (GUILayout.Button(stopmappinglabel))
        //         {
        //             spatialMapping.StopSpatialMapping();
        //         }
        //     }
        // }

        // EditorGUILayout.EndHorizontal();

        // GUI.enabled = cameraIsReady;
        // string displaytooltip = ContinuousSpatialMapping.display ? "Hide the mesh from view." : "Display the hidden mesh.";
        // GUIContent displaylabel = new GUIContent(displayText, displaytooltip);
        // if (GUILayout.Button(displayText))
        // {
        //     spatialMapping.SwitchDisplayMeshState(!ContinuousSpatialMapping.display);
        // }
        // GUI.enabled = true;

        serializedObject.ApplyModifiedProperties();

        if (!cameraIsReady) Repaint();
    }


}

#endif
