using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/**
* IMPORTANT NOTE:
* Unity's terrain data co-ordinates are not setup as you might expect.
* Assuming the terrain is not rotated, this is the terrain strides vs axis:
* [0, 0]            = -X, -Z
* [width, 0]        = +X, -Z
* [0, height]       = -X, +Z
* [width, height]   = +X, +Z
* 
* This means that the that X goes out to the 3D Z-Axis, and Y goes out into the 3D X-Axis.
* This also means that a world space position such as the mouse position from a raycast needs 
*   its worldspace X-Axis position mapped to Z, and the worldspace Y-Axis mapped to X
*   
* Another thing of note is that LoadAssetAtPath usages aren't using the generic method - this is simply
* to support older Unity 5.0 releases.
*/

namespace JesseStiller.TerrainFormerExtension {
    [CustomEditor(typeof(TerrainFormer))]
    internal class TerrainFormerEditor : Editor {
        private static int activeInspector = 0;
        internal static TerrainFormerEditor Instance;
        private TerrainFormer terrainFormer;

        internal static readonly Dictionary<string, Type> terrainBrushTypes;
        internal List<TerrainBrush> terrainBrushesOfCurrentType = new List<TerrainBrush>();

        internal static float[,] allTerrainHeights, allUnmodifiedTerrainHeights;
        internal static float[,,] allTextureSamples;
        internal static SplatPrototype[] splatPrototypes;
        internal int totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically;
        internal int totalToolSamplesHorizontally, totalToolSamplesVertically;

        private TerrainMismatchManager terrainMismatchManager;

        /**
        * Caching the terrain brush is especially useful for RotateTemporaryBrushSamples. It would take >500ms when accessing the terrain brush
        * through the property. Using it in when it's been cached makes roughly a 10x speedup and doesn't allocated ~3 MB of garbage.
        */
        private TerrainBrush cachedTerrainBrush;
        private float[,] temporarySamples;

        // Reflection fields
        private List<object> unityTerrainInspectors = new List<object>();
        private static Type unityTerrainInspectorType;
        private static PropertyInfo unityTerrainSelectedTool;
        private static PropertyInfo guiUtilityTextFieldInput;
        private static MethodInfo terrainDataSetBasemapDirtyMethodInfo;
        private static MethodInfo inspectorWindowRepaintAllInspectors;

        // TODO: There is also a Settings.mainDirectory
        private static string mainDirectory;
        internal static string texturesDirectory;
        
        private bool isTerrainGridParentSelected = false;

        // Parameters shared across all terrains
        private int heightmapResolution;
        private int alphamapResolution;
        private int currentToolsResolution;
        internal Vector3 terrainSize; // Size of a single terrain (not a terrain grid)
        
        internal static Settings settings;
        internal static bool exceptionUponLoadingSettings = false;

        private bool neighboursFoldout = false;

        // Flatten fields
        private float flattenHeight = -1f;

        // Heightfield fields
        private Texture2D heightmapTexture;

        // States and Information
        private int lastHeightmapResolultion;
        internal bool isSelectingBrush = false;
        private bool falloffChangeQueued = false;
        private double lastTimeBrushSamplesWereUpdated = -1d;

        internal CommandArea commandArea;

        [Flags]
        private enum SamplesDirty {
            None = 0,
            InspectorTexture = 1,
            ProjectorTexture = 2,
            BrushSamples = 4,
        }
        private SamplesDirty samplesDirty = SamplesDirty.None;

        private float halfBrushSizeInSamples;
        private int brushSizeInSamples;
        private int BrushSizeInSamples {
            get {
                return brushSizeInSamples;
            }
            set {
                if(brushSizeInSamples == value) return;
                brushSizeInSamples = value;
                halfBrushSizeInSamples = brushSizeInSamples * 0.5f;
            }
        }

        // Gizmos
        private GameObject gridPlane;
        private Material gridPlaneMaterial;

        // Projector and cursor fields
        private GameObject brushProjectorGameObject;
        private Projector brushProjector;
        private Material brushProjectorMaterial;
        private GameObject topPlaneGameObject; // Used to show the current height of "Flatten" and "Set Height"
        private Material topPlaneMaterial;

        internal static Texture2D brushProjectorTexture;

        // Brush fields
        private const float minBrushSpeed = 0.01f;
        private const float maxBrushSpeed = 2f;
        private const float minSpacingBounds = 0.1f;
        private const float maxSpacingBounds = 30f;
        private const float minRandomOffset = 0.001f;
        private const float minRandomRotationBounds = -180f;
        private const float maxRandomRotationBounds = 180f;
        private static BrushCollection brushCollection;

        // Terrain fields
        private static readonly int terrainEditorHash = "TerrainFormerEditor".GetHashCode(); // Used for the TerrainEditor windows' events

        // The first tool in order from left to right that is not a scultping tool.
        private const Tool firstNonMouseTool = Tool.Heightmap;

        // Mode fields
        private static GUIContent[] toolsGUIContents;
        
        // Mouse related fields
        private bool mouseIsDown;
        private Vector2 mousePosition = new Vector2(); // The current screen-space position of the mouse. This position is used for raycasting
        private Vector2 lastMousePosition;
        private Vector3 lastWorldspaceMousePosition;
        private Vector3 lastClickPosition; // The point of the terrain the mouse clicked on
        private float mouseSpacingDistance = 0f;
        private float randomSpacing;
        private bool isShowingToolTooltip = false;
        private float currentTotalMouseDelta = 0f;
        internal float CurrentTotalMouseDelta {
            get {
                return currentTotalMouseDelta;
            }
        }

        // Styles
        private static GUIStyle largeBoldLabel;
        private static GUIStyle sceneViewInformationAreaStyle;
        private static GUIStyle brushNameAlwaysShowBrushSelectionStyle;
        private static GUIStyle gridListStyle;
        private static GUIStyle miniBoldLabelCentered;
        private static GUIStyle miniButtonWithoutMargin;
        private static GUIStyle neighboursCellBoxStyle;
        
        // GUI Contents
        private static readonly GUIContent smoothAllTerrainContent = new GUIContent("Smooth All", "Smooths the entirety of the terrain based on the smoothing settings.");
        private static readonly GUIContent boxFilterSizeContent = new GUIContent("Smooth Radius", "Sets the number of adjacent terrain segments that are taken into account when smoothing " +
            "each segment. A higher value will more quickly smooth the area to become almost flat, but it may slow down performance while smoothing.");
        private static readonly GUIContent smoothingIterationsContent = new GUIContent("Smooth Iterations", "Sets how many times the entire terrain will be smoothed. (This setting only " +
            "applies to the Smooth All button.)");
        private static readonly GUIContent flattenModeContent = new GUIContent("Flatten Mode", "Sets the mode of flattening that will be used.\n- Flatten: Terrain higher than the current " +
            "click location height will be set to the click location height.\n- Bridge: The entire terrain will be set to the click location height.\n- Extend: Terrain lower than the current " +
            "click location height wil be set to the click location height.");
        private static readonly GUIContent showSculptingGridPlaneContent = new GUIContent("Show Sculpting Plane Grid", "Sets whether or not a grid plane will be visible while sculpting.");
        private static readonly GUIContent raycastModeLabelContent = new GUIContent("Sculpt Onto", "Sets the way terrain will be sculpted.\n- Plane: Sculpting will be projected onto a plane " +
            "that's located where you initially left-clicked at.\n- Terrain: Sculpting will be projected onto the terrain.");
        private static readonly GUIContent alwaysUpdateTerrainLODsContent = new GUIContent("Update Terrain LODs", "Sets when the terrain(s) being modified will have their LODs updated. This" +
            "option affects sculpting performance depending on how detailed the terrain is, how close it is and your computer.\n" +
            "Always: Only updates the terrain LODs every time they are modified, which can be up to 100 times a second.\nMouse Up: Only updates the LODs when the mouse has been released (" +
            "which is when modifications have stopped)");
        private static readonly GUIContent alwaysShowBrushSelectionContent = new GUIContent("Always Show Brush Selection", "Sets whether or not the brush selection control will be expanded " +
            "in the general brush settings area.");
        private static readonly GUIContent[] heightmapSources = new GUIContent[] { new GUIContent("Greyscale"), new GUIContent("Alpha") };
        private static readonly GUIContent collectDetailPatchesContent = new GUIContent("Collect Detail Patches", "If enabled the detail patches in the Terrain will be removed from memory when not visible. If the property is set to false, the patches are kept in memory until the Terrain object is destroyed or the collectDetailPatches property is set to true.\n\nBy setting the property to false all the detail patches for a given density will be initialized and kept in memory. Changing the density will recreate the patches.");
        private static readonly GUIContent shrinkWrapRaycastOffsetContent = new GUIContent("Raycast Offset", "Units to offset the raycast position. This option is useful for make sure the shrink-wrapped terrain doesn't stick out above objects");

        private static readonly string[] brushSelectionDisplayTypeLabels = { "Image Only", "Image with Type Icon", "Tabbed" };
        private static readonly string[] raycastModes = { "Plane", "Terrain" };
        private static readonly string[] previewSizesContent = new string[] { "32px", "48px", "64px" };
        private static readonly int[] previewSizeValues = new int[] { 32, 48, 64 };

        internal List<TerrainInformation> terrainInformations;
        internal int numberOfTerrainsHorizontally = 1;
        internal int numberOfTerrainsVertically = 1;

        // The first terrain (either the bottom left most one in the grid, or the only terrain).
        private Transform firstTerrainTransform;
        private Terrain firstTerrain;
        private TerrainData firstTerrainData;

        private TerrainCommand currentCommand;
        
        private SavedTool currentTool;
        internal Tool CurrentTool {
            get {
                if(Tools.current == UnityEditor.Tool.None && GetInstanceID() == activeInspector) {
                    return currentTool.Value;
                } else {
                    return Tool.None;
                }
            }
            private set {
                if(value == CurrentTool) return;
                
                if(value != Tool.None) Tools.current = UnityEditor.Tool.None;

                Tool previousTool = currentTool.Value;
                currentTool.Value = value;
                CurrentToolChanged(previousTool);
            }
        }
        
        private TerrainBrush CurrentBrush {
            get {
                if(brushCollection.brushes.ContainsKey(settings.modeSettings[CurrentTool].selectedBrushId) == false) {
                    settings.modeSettings[CurrentTool].selectedBrushId = brushCollection.brushes.Keys.First();
                    SelectedBrushChanged();
                }

                return brushCollection.brushes[settings.modeSettings[CurrentTool].selectedBrushId];
            }
        }

        private float MaxBrushSize {
            get {
                return terrainSize.x;
            }
        }

        // The minimum brush size is set to the total length of five heightmap segments (with one segment being the length from one sample to its neighbour)
        private float MinBrushSize {
            get {
                return Mathf.CeilToInt(terrainSize.x / heightmapResolution / 0.1f) * 0.1f * 5f;
            }
        }

        // Only show the topPlane if the height is more than 1/500th of the heightmap scale
        public float MinHeightDifferenceToShowTopPlane {
            get {
                return firstTerrainData.heightmapScale.y * 0.002f;
            }
        }

        internal AnimationCurve BrushFalloff {
            get {
                return settings.modeSettings[CurrentTool].brushFalloff;
            }
            set {
                settings.modeSettings[CurrentTool].brushFalloff = value;
            }
        }
        
        private class TerrainBrushTypesInfo {
            internal int sortOrder;
            internal string prettyTypeName;
            internal Type type;

            internal TerrainBrushTypesInfo(int sortOrder, string prettyTypeName, Type type) {
                this.sortOrder = sortOrder;
                this.prettyTypeName = prettyTypeName;
                this.type = type;
            }
        }

        static TerrainFormerEditor() {
            terrainBrushTypes = new Dictionary<string, Type>();
            terrainBrushTypes.Add("All", null);

            List<TerrainBrushTypesInfo> terrainBrushTypesInfo = new List<TerrainBrushTypesInfo>();

            Type[] allAssemblyTypes = typeof(TerrainFormerEditor).Assembly.GetTypes();
            // Gather all classes that derrive from TerrainBrush
            foreach(Type type in allAssemblyTypes) {
                if(type.IsSubclassOf(typeof(TerrainBrush)) == false) continue;

                BindingFlags nonPublicStaticBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
                FieldInfo prettyTypeNameFieldInfo = type.GetField("prettyTypeName", nonPublicStaticBindingFlags);
                string prettyTypeName = prettyTypeNameFieldInfo == null ? type.Name : (string)prettyTypeNameFieldInfo.GetValue(null);

                FieldInfo typeSortOrderFieldInfo = type.GetField("typeSortOrder", nonPublicStaticBindingFlags);
                int typeSortOrder = typeSortOrderFieldInfo == null ? 10 : (int)typeSortOrderFieldInfo.GetValue(null);
                
                terrainBrushTypesInfo.Add(new TerrainBrushTypesInfo(typeSortOrder, prettyTypeName, type));
            }

            terrainBrushTypesInfo.Sort(delegate (TerrainBrushTypesInfo x, TerrainBrushTypesInfo y) {
                if(x.sortOrder < y.sortOrder) return x.sortOrder;
                else return y.sortOrder;
            });

            foreach(TerrainBrushTypesInfo t in terrainBrushTypesInfo) {
                terrainBrushTypes.Add(t.prettyTypeName, t.type);
            }
        }
        
        // Simple initialization logic that doesn't rely on any secondary data
        internal void OnEnable() {
            // Sometimes it's possible Terrain Former thinks the mouse is still pressed down as not every event is detected by Terrain Former
            mouseIsDown = false; 
            terrainFormer = (TerrainFormer)target;
            currentTool = new SavedTool("TerrainFormer/CurrentTool", Tool.None);
            // If there is a Unity tool selected, make sure Terrain Former's tool is set to None
            if(Tools.current != UnityEditor.Tool.None) {
                currentTool.Value = Tool.None;
            }
            
            // Forcibly re-initialize just in case variables were lost during an assembly reload
            if(Initialize(true) == false) return;

            if(toolsGUIContents == null) {
                toolsGUIContents = new GUIContent[] {
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/RaiseLower.png"), "Raise/Lower"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/Smooth.png"), "Smooth"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/SetHeight.png"), "Set Height"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/Flatten.png"), "Flatten"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/PaintTexture.psd"), "Paint Texture"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/ShrinkWrap.psd"), "Shrink Wrap"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/Heightmap.psd"), "Heightmap"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/Generate.png"), "Generate"),
                    new GUIContent(null, AssetDatabase.LoadAssetAtPath<Texture2D>(mainDirectory + "Textures/Icons/Settings.png"), "Settings")
                };
            }
            
            // Set the Terrain Former component icon
            Type editorGUIUtilityType = typeof(EditorGUIUtility);
            MethodInfo setIcon = editorGUIUtilityType.GetMethod("SetIconForObject", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]{typeof(UnityEngine.Object), typeof(Texture2D)}, null);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(texturesDirectory + "Icons/Icon.png");
            setIcon.Invoke(null, new object[] { target, icon});
            
            if(GetInstanceID() == activeInspector) {
                if(CurrentTool != Tool.None) {
                    CurrentToolChanged(Tool.None);
                }                
            }

            Undo.undoRedoPerformed += UndoRedoPerformed;
            SceneView.onSceneGUIDelegate += OnSceneGUICallback;
        }
        
        /**
        * Initialize contains logic that is intrinsically tied to this entire terrain tool. If any of these fields and 
        * other things are missing, then the entire editor will break. An attempt will be made every GUI frame to find them.
        * Returns true if the initialization was successful or if everything is already initialized, false otherwise.
        * If the user moves Terrain Former's Editor folder away and brings it back, the brushProjector dissapears. This is why
        * it is checked for on Initialization.
        */
        private bool Initialize(bool forceReinitialize = false) {
            if(forceReinitialize == false && terrainFormer != null && brushProjector != null) {
                return true;
            }
            
            /**
            * If there is more than one object selected, do not even bother initializing. This also fixes a strange 
            * exception occurance when two terrains or more are selected; one with Terrain Former and one without
            */
            if(Selection.objects.Length != 1) return false;

            // Make sure there is only ever one Terrain Former
            TerrainFormer[] terrainFormerInstances = terrainFormer.GetComponents<TerrainFormer>();
            if(terrainFormerInstances.Length > 1) {
                for(int i = terrainFormerInstances.Length - 1; i > 0; i--) {
                    DestroyImmediate(terrainFormerInstances[i]);
                }
                EditorUtility.DisplayDialog("Terrain Former", "You can't add multiple Terrain Former components to a single Terrain object.", "Close");
                return false;
            }
            
            Terrain terrainComponentOfTarget = terrainFormer.GetComponent<Terrain>();
            
            terrainInformations = new List<TerrainInformation>();
            if(terrainComponentOfTarget) {
                // If there is a terrain component attached to this object, check if it's one of many terrains inside of a grid.
                if(terrainFormer.transform.parent != null && terrainFormer.transform.parent.childCount > 0) {
                    Terrain[] terrainComponentsInChildren = terrainFormer.transform.parent.GetComponentsInChildren<Terrain>();
                    foreach(Terrain terrain in terrainComponentsInChildren) {
                        if(terrain.terrainData == null) continue;
                        terrainInformations.Add(new TerrainInformation(terrain));
                    }
                } else if(terrainComponentOfTarget.terrainData != null) {
                    terrainInformations.Add(new TerrainInformation(terrainComponentOfTarget));
                }
            } else {
                // If Terrain Former is attached to a game object with children that contain Terrains, allow Terrain Former to look into the child terrain objects.
                Terrain[] terrainChildren = terrainFormer.GetComponentsInChildren<Terrain>();
                if(terrainChildren != null && terrainChildren.Length > 0) {
                    isTerrainGridParentSelected = true;
                } else {
                    return false;
                }
                
                foreach(Terrain terrain in terrainChildren) {
                    if(terrain.terrainData == null) continue;
                    terrainInformations.Add(new TerrainInformation(terrain));
                }
            }

            if(terrainInformations.Count == 0) return false;

            // Assume the first terrain information has the correct parameters
            terrainSize = terrainInformations[0].terrainData.size;
            heightmapResolution = terrainInformations[0].terrainData.heightmapResolution;
            alphamapResolution = terrainInformations[0].terrainData.alphamapResolution;
            lastHeightmapResolultion = heightmapResolution;

            switch(terrainInformations.Count) {
                case 1:
                    firstTerrainTransform = terrainComponentOfTarget.transform;
                    break;
                default:
                    // If there is more than one terrain, find the top-left most terrain to determine grid coordinates
                    Vector3 bottomLeftMostValue = new Vector3(float.MaxValue, 0f, float.MaxValue);
                    Vector3 currentTerrainPosition;
                    foreach(TerrainInformation ti in terrainInformations) {
                        currentTerrainPosition = ti.transform.position;
                        if(currentTerrainPosition.x <= bottomLeftMostValue.x && currentTerrainPosition.z <= bottomLeftMostValue.z) {
                            bottomLeftMostValue = currentTerrainPosition;
                            firstTerrainTransform = ti.terrain.transform;
                        }
                    }
                    
                    foreach(TerrainInformation terrainInformation in terrainInformations) {
                        terrainInformation.gridXCoordinate = Mathf.RoundToInt((terrainInformation.transform.position.x - bottomLeftMostValue.x) / terrainSize.x);
                        terrainInformation.gridYCoordinate = Mathf.RoundToInt((terrainInformation.transform.position.z - bottomLeftMostValue.z) / terrainSize.z);
                        terrainInformation.alphamapsXOffset = terrainInformation.gridXCoordinate * alphamapResolution;
                        terrainInformation.alphamapsYOffset = terrainInformation.gridYCoordinate * alphamapResolution;
                        terrainInformation.heightmapXOffset = terrainInformation.gridXCoordinate * heightmapResolution - terrainInformation.gridXCoordinate;
                        terrainInformation.heightmapYOffset = terrainInformation.gridYCoordinate * heightmapResolution - terrainInformation.gridYCoordinate;

                        if(terrainInformation.gridXCoordinate + 1 > numberOfTerrainsHorizontally) {
                            numberOfTerrainsHorizontally = terrainInformation.gridXCoordinate + 1;
                        } else if(terrainInformation.gridYCoordinate + 1 > numberOfTerrainsVertically) {
                            numberOfTerrainsVertically = terrainInformation.gridYCoordinate + 1;
                        }
                    }
                    break;
            }
            
            firstTerrain = firstTerrainTransform.GetComponent<Terrain>();
            if(firstTerrain == null) return false;
            firstTerrainData = firstTerrain.terrainData;
            if(firstTerrainData == null) return false;
            
            totalHeightmapSamplesHorizontally = numberOfTerrainsHorizontally * heightmapResolution - (numberOfTerrainsHorizontally - 1);
            totalHeightmapSamplesVertically = numberOfTerrainsVertically * heightmapResolution - (numberOfTerrainsVertically - 1);
            
            if(terrainMismatchManager == null) {
                terrainMismatchManager = new TerrainMismatchManager();
            }
            terrainMismatchManager.Initialize(terrainInformations);
            terrainMismatchManager.terrainFormerInstance = this;
            if(terrainMismatchManager.IsMismatched) return false;

            splatPrototypes = firstTerrainData.splatPrototypes;

            allTerrainHeights = new float[totalHeightmapSamplesVertically, totalHeightmapSamplesHorizontally];
            UpdateAllHeightsFromSourceAssets();
            
            if(settings == null) {
                InitializeSettings();
            }

            if(settings == null) return false;

            settings.mainDirectory = mainDirectory;
            settings.AlwaysShowBrushSelectionChanged = AlwaysShowBrushSelectionValueChanged;
            settings.brushColour.ValueChanged = BrushColourChanged;
            
            brushCollection = new BrushCollection();

            CreateProjector();

            CreateGridPlane();
            
            /**
            * Get an instance of the built-in Unity Terrain Inspector so we can override the selectedTool property
            * when the user selects a different tool in Terrain Former. This makes it so the user can't accidentally
            * use two terain tools at once (eg. Unity Terrain's raise/lower, and Terrain Former's raise/lower)
            */
            unityTerrainInspectorType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.TerrainInspector");
            unityTerrainSelectedTool = unityTerrainInspectorType.GetProperty("selectedTool", BindingFlags.NonPublic | BindingFlags.Instance);
            
            UnityEngine.Object[] terrainInspectors = Resources.FindObjectsOfTypeAll(unityTerrainInspectorType);
            // Iterate through each Unity terrain inspector to find the Terrain Inspector(s) that belongs to this object
            foreach(UnityEngine.Object inspector in terrainInspectors) {
                Editor inspectorAsType = inspector as Editor;
                GameObject inspectorGameObject = ((Terrain)inspectorAsType.target).gameObject;

                if(inspectorGameObject == null) continue;

                if(inspectorGameObject == terrainFormer.gameObject) {
                    unityTerrainInspectors.Add(inspector);
                }
            }
            
            guiUtilityTextFieldInput = typeof(GUIUtility).GetProperty("textFieldInput", BindingFlags.NonPublic | BindingFlags.Static);

            inspectorWindowRepaintAllInspectors = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.InspectorWindow").GetMethod("RepaintAllInspectors", BindingFlags.Static | BindingFlags.NonPublic);

            terrainDataSetBasemapDirtyMethodInfo = typeof(TerrainData).GetMethod("SetBasemapDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            AssetWatcher.OnAssetsImported = OnAssetsImported;
            AssetWatcher.OnAssetsMoved = OnAssetsMoved;
            AssetWatcher.OnAssetsDeleted = OnAssetsDeleted;
            AssetWatcher.OnWillSaveAssetsAction = OnWillSaveAssets;
            
            return true;
        }
        
        internal static void InitializeSettings() {
            // Look for the main directory by finding the path of the Terrain Former script.
            GameObject temporaryGameObject = EditorUtility.CreateGameObjectWithHideFlags("TerrainFormerTemporaryObject", HideFlags.HideAndDontSave);
            TerrainFormer terrainFormerComponent = temporaryGameObject.AddComponent<TerrainFormer>();
            string terrainFormerPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(terrainFormerComponent));
            mainDirectory = Path.GetDirectoryName(terrainFormerPath) + "/";
            DestroyImmediate(terrainFormerComponent);
            DestroyImmediate(temporaryGameObject);
            texturesDirectory = mainDirectory + "Textures/";

            if(string.IsNullOrEmpty(mainDirectory) || Directory.Exists(mainDirectory) == false) {
                Debug.LogError("Terrain Former wasn't able to find its main directory.");
                return;
            }

            string absoluteSettingsPath = Utilities.GetAbsolutePathFromLocalPath(Path.Combine(mainDirectory, "Settings.tf"));
            settings = Settings.Create(absoluteSettingsPath);
        }

        private void OnDisable() {
            if(settings != null) settings.Save();

            brushCollection = null;

            // Destroy all gizmos
            if(brushProjectorGameObject != null) {
                DestroyImmediate(brushProjectorMaterial);
                DestroyImmediate(brushProjectorGameObject);
                DestroyImmediate(topPlaneGameObject);
                brushProjector = null;
            }

            if(gridPlane != null) {
                DestroyImmediate(gridPlaneMaterial);
                DestroyImmediate(gridPlane.gameObject);
                gridPlaneMaterial = null;
                gridPlane = null;
            }

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            AssetWatcher.OnAssetsImported -= OnAssetsImported;
            AssetWatcher.OnAssetsMoved -= OnAssetsMoved;
            AssetWatcher.OnAssetsDeleted -= OnAssetsDeleted;
            AssetWatcher.OnWillSaveAssetsAction -= OnWillSaveAssets;

            if(settings != null) {
                settings.AlwaysShowBrushSelectionChanged -= AlwaysShowBrushSelectionValueChanged;
                settings.brushColour.ValueChanged -= BrushColourChanged;
            }
            
            Instance = null;
            if(activeInspector == GetInstanceID()) activeInspector = 0;

            SceneView.onSceneGUIDelegate -= OnSceneGUICallback;
        }
        
        public override void OnInspectorGUI() {
            bool displayingProblem = false;
            
            // TODO: Display an actionable HelpBox if there is a terrain asset that is saved in scene and not seperately.
            // Stop if the initialization was unsuccessful
            if(terrainInformations == null || terrainInformations.Count == 0) {
                EditorGUILayout.HelpBox("There is no terrain attached to this object, nor are there any terrain objects as children to this object.", MessageType.Info);
                return;
            }
            else if(firstTerrainData == null) {
                EditorGUILayout.HelpBox("Missing terrain data asset. Reassign the terrain asset in the Unity Terrain component.", MessageType.Error);
                displayingProblem = true;
            }

            bool containsAtleastOneTerrainCollider = false;
            bool hasOneOrMoreTerrainCollidersDisabled = false;
            foreach(TerrainInformation terrainInformation in terrainInformations) {
                if(terrainInformation.collider == null) continue;
                containsAtleastOneTerrainCollider = true;

                if(terrainInformation.collider.enabled == false) hasOneOrMoreTerrainCollidersDisabled = true;

                break;
            }
            if(containsAtleastOneTerrainCollider == false) {
                if(terrainInformations.Count > 1) {
                    EditorGUILayout.HelpBox("There aren't any terrain colliders attached to any of the terrains in the terrain grid.", MessageType.Error);
                } else {
                    EditorGUILayout.HelpBox("This terrain object doesn't have a terrain collider attached to it.", MessageType.Error);
                }
                displayingProblem = true;
            }
            
            if(hasOneOrMoreTerrainCollidersDisabled) {
                EditorGUILayout.HelpBox("There is at least one terrain that has an inactive collider. Terrain editing functionality won't work on the affected terrain(s).", MessageType.Warning);
                displayingProblem = true;
            }

            if(target == null) {
                EditorGUILayout.HelpBox("There is no target object. Make sure Terrain Former is a component of a terrain object.", MessageType.Error);
                displayingProblem = true;
            }
            
            if(terrainMismatchManager != null) {
                terrainMismatchManager.Draw();
            }

            if(settings == null) {
                if(exceptionUponLoadingSettings == true) {
                    EditorGUILayout.HelpBox("The Settings.tf file couldn't load. Look at the errors in the Console for more details.", MessageType.Error);
                } else {
                    EditorGUILayout.HelpBox("The Settings.tf file couldn't load. There must be some invalid JSON in the file, possibly caused by a merge that happened in a source control system. You can safely ignore the Settings.tf file from source control.", MessageType.Error);
                }

                displayingProblem = true;
            }
            
            if(displayingProblem) return;
            
            if(Initialize() == false) return;
            
            if(largeBoldLabel == null) {
                largeBoldLabel = new GUIStyle(EditorStyles.largeLabel);
                largeBoldLabel.fontSize = 13;
                largeBoldLabel.fontStyle = FontStyle.Bold;
                largeBoldLabel.alignment = TextAnchor.MiddleCenter;
            }
            if(brushNameAlwaysShowBrushSelectionStyle == null) {
                brushNameAlwaysShowBrushSelectionStyle = new GUIStyle(GUI.skin.label);
                brushNameAlwaysShowBrushSelectionStyle.alignment = TextAnchor.MiddleRight;
            }
            if(gridListStyle == null) {
                gridListStyle = GUI.skin.GetStyle("GridList");
            }
            if(miniBoldLabelCentered == null) {
                miniBoldLabelCentered = new GUIStyle(EditorStyles.miniBoldLabel);
                miniBoldLabelCentered.alignment = TextAnchor.MiddleCenter;
                miniBoldLabelCentered.margin = new RectOffset();
                miniBoldLabelCentered.padding = new RectOffset();
                miniBoldLabelCentered.wordWrap = true;
            }
            if(miniButtonWithoutMargin == null) {
                miniButtonWithoutMargin = EditorStyles.miniButton;
                miniButtonWithoutMargin.margin = new RectOffset();
            }
            if(neighboursCellBoxStyle == null) {
                neighboursCellBoxStyle = new GUIStyle(GUI.skin.box);
                neighboursCellBoxStyle.padding = new RectOffset();
                neighboursCellBoxStyle.contentOffset = new Vector2();
                neighboursCellBoxStyle.alignment = TextAnchor.MiddleCenter;
                
                if(numberOfTerrainsHorizontally >= 10 || numberOfTerrainsVertically >= 10) {
                    neighboursCellBoxStyle.fontSize = 8;
                } else {
                    neighboursCellBoxStyle.fontSize = 10;
                }
            }
            
            EditorGUIUtility.labelWidth = CurrentTool == Tool.Settings ? 188f : 125f;

            CheckKeyboardShortcuts(Event.current);
            
            // Check if the user modified the heightmap resolution. If so, update the brush samples
            int heightmapResolution = firstTerrainData.heightmapResolution;
            if(lastHeightmapResolultion != -1 && lastHeightmapResolultion != heightmapResolution) {
                BrushSizeChanged();
                lastHeightmapResolultion = heightmapResolution;
            }
            
            /** 
            * Get the current Unity Terrain Inspector tool, and set the Terrain Former tool to none if the Unity Terrain
            * Inspector tool is not none.
            */
            if(unityTerrainInspectors != null && CurrentTool != Tool.None) {
                foreach(object inspector in unityTerrainInspectors) {
                    int unityTerrainTool = (int)unityTerrainSelectedTool.GetValue(inspector, null);
                    // If the tool is not "None" (-1), then the Terrain Former tool must be set to none
                    if(unityTerrainTool != -1) {
                        currentTool.Value = Tool.None;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(-8f); // HACK: This offset is required (for some reason) to make the toolbar horizontally centered
            Rect toolbarRect = EditorGUILayout.GetControlRect(false, 22f, GUILayout.MinWidth(240f), GUILayout.MaxWidth(290f));
            CurrentTool = (Tool)GUI.Toolbar(toolbarRect, (int)CurrentTool, toolsGUIContents);
            
#if (UNITY_2017_1_OR_NEWER == false)
            // Display a tooltip showing the current tool being hovered over
            Event currentEvent = Event.current;
            if(currentEvent.type == EventType.Repaint) {
                if(toolbarRect.Contains(currentEvent.mousePosition)) {
                    float mouseHorizontalDelta = currentEvent.mousePosition.x - toolbarRect.x;
                    float tabWidth = toolbarRect.width / toolsGUIContents.Length;
                    int toolIndex = Mathf.FloorToInt((mouseHorizontalDelta / toolbarRect.width) * toolsGUIContents.Length);
                    float centerOfTabHoveredOver = toolIndex * tabWidth + tabWidth * 0.5f + toolbarRect.x;

                    Vector2 tooltipBoxSize = GUI.skin.box.CalcSize(new GUIContent(toolsGUIContents[toolIndex].tooltip));

                    /**
                    * The GUI.box style in the dark skin has a transparent background and incorrect text colour. If the Unity Pro skin is 
                    * being used, we need to rebuild the GUISkin.box style based on the "OL box" style.
                    */
                    GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box);
                    if(EditorGUIUtility.isProSkin) {
                        tooltipStyle.normal.background = GUI.skin.GetStyle("OL box").normal.background;
                        tooltipStyle.normal.textColor = new Color(0.82f, 0.82f, 0.82f, 1f);
                    }

                    tooltipStyle.Draw(new Rect(centerOfTabHoveredOver - tooltipBoxSize.x * 0.5f, toolbarRect.y - 20f, tooltipBoxSize.x + 6f, tooltipBoxSize.y),
                        toolsGUIContents[toolIndex].tooltip, false, false, false, false);

                    isShowingToolTooltip = true;
                } else if(isShowingToolTooltip) {
                    isShowingToolTooltip = false;
                    Repaint();
                }
            }
#endif

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if(CurrentTool == Tool.None || activeInspector != GetInstanceID()) return;

            if(Event.current.type == EventType.MouseUp || Event.current.type == EventType.KeyUp) {
                UpdateDirtyBrushSamples();
            }

            GUILayout.Label(toolsGUIContents[(int)CurrentTool].tooltip, largeBoldLabel);

            switch(CurrentTool) {
                case Tool.Smooth:
                    settings.boxFilterSize = EditorGUILayout.IntSlider(boxFilterSizeContent, settings.boxFilterSize, 1, 5);

                    GUILayout.Label("Smooth All", EditorStyles.boldLabel);
                    
                    GUIUtilities.FillAndRightControl(
                        fillControl: (r) => {
                            settings.smoothingIterations = EditorGUI.IntSlider(r, smoothingIterationsContent, settings.smoothingIterations, 1, 10);
                        },
                        rightControl: (r) => {
                            r.yMin -= 2f;
                            r.yMax += 2f;
                            if(GUI.Button(r, smoothAllTerrainContent)) {
                                SmoothAll();
                            }
                        },
                        rightControlWidth: 100
                    );
                    break;
                case Tool.SetHeight:
                    GUIUtilities.FillAndRightControl(
                        fillControl: (r) => {
                            settings.setHeight = EditorGUI.Slider(r, "Set Height", settings.setHeight, 0f, terrainSize.y);
                        },
                        rightControl: (r) => {
                            r.yMax += 2;
                            r.yMin -= 2;
                            if(GUI.Button(r, "Apply to Terrain")) {
                                FlattenTerrain(settings.setHeight / terrainSize.y);
                            }
                        },
                        rightControlWidth: 125
                    );

                    break;
                case Tool.Flatten:
                    settings.flattenMode = (FlattenMode)EditorGUILayout.EnumPopup(flattenModeContent, settings.flattenMode);
                    break;
                case Tool.ShrinkWrap:
                    settings.shrinkWrapToolRaycastOffset = EditorGUILayout.FloatField(shrinkWrapRaycastOffsetContent, settings.shrinkWrapToolRaycastOffset);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button("Shrinkwrap All", GUILayout.Width(110f), GUILayout.Height(24f))) {
                        RegisterUndoForTerrainGrid("Shrinkwrapped Entire Terrain");
                        foreach(TerrainInformation terrainInformation in terrainInformations) {
                            terrainInformation.collider.enabled = false;
                        }

                        RaycastHit hitInfo;
                        float stepSize = terrainSize.x / 1024f;
                        foreach(TerrainInformation terrainInformation in terrainInformations) {
                            for(int y = 0; y < firstTerrainData.heightmapResolution; y++) {
                                for(int x = 0; x < firstTerrainData.heightmapResolution; x++) {
                                    Vector3 origin = new Vector3(terrainInformation.transform.position.x + x * stepSize, terrainInformation.transform.position.y + terrainSize.y,
                                        terrainInformation.transform.position.z + y * stepSize);
                                    if(Physics.Raycast(origin, Vector3.down, out hitInfo, terrainSize.y)) {
                                        allTerrainHeights[terrainInformation.heightmapYOffset + y, x + terrainInformation.heightmapXOffset] = (hitInfo.point.y + settings.shrinkWrapToolRaycastOffset - terrainInformation.transform.position.y) / terrainSize.y;
                                    }
                                }
                            }
                        }

                        UpdateAllHeightsInSourceAssets();
                        
                        foreach(TerrainInformation terrainInformation in terrainInformations) {
                            terrainInformation.collider.enabled = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    break;
                case Tool.PaintTexture:
                    EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

                    Texture2D[] splatIcons = new Texture2D[splatPrototypes.Length];
                    for(int i = 0; i < splatIcons.Length; ++i) {
                        splatIcons[i] = AssetPreview.GetAssetPreview(splatPrototypes[i].texture) ?? splatPrototypes[i].texture;
                    }

                    settings.selectedTextureIndex = GUIUtilities.TextureSelectionGrid(settings.selectedTextureIndex, splatIcons);

                    settings.targetOpacity = EditorGUILayout.Slider("Target Opacity", settings.targetOpacity, 0f, 1f);
                    
                    break;
                case Tool.Heightmap:
                    EditorGUILayout.LabelField("Modification", EditorStyles.boldLabel);
                    GUIUtilities.FillAndRightControl(
                        fillControl: (r) => {
                            settings.heightmapHeightOffset = EditorGUI.FloatField(r, "Offset Height", settings.heightmapHeightOffset);
                        },
                        rightControl: (r) => {
                            r.yMax += 2;
                            r.yMin -= 2;
                            if(GUI.Button(r, "Apply to Terrain")) {
                                HeightOffset(settings.heightmapHeightOffset);
                            }
                        },
                        rightControlWidth: 125
                    );

                    EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);
                    Rect heightmapSourceRect = EditorGUILayout.GetControlRect();
                    Rect heightmapSourceToolbarRect = EditorGUI.PrefixLabel(heightmapSourceRect, new GUIContent("Source"));
                    settings.heightmapSourceIsAlpha = GUI.Toolbar(heightmapSourceToolbarRect, settings.heightmapSourceIsAlpha ? 1 : 0, 
                        heightmapSources, EditorStyles.radioButton) == 1;
                    heightmapTexture = (Texture2D)EditorGUILayout.ObjectField("Heightmap Texture", heightmapTexture, typeof(Texture2D), false);

                    GUI.enabled = heightmapTexture != null;
                    Rect importHeightmapButtonRect = EditorGUILayout.GetControlRect(GUILayout.Width(145f), GUILayout.Height(24f));
                    importHeightmapButtonRect.x = Screen.width * 0.5f - 70f;
                    if(GUI.Button(importHeightmapButtonRect, "Import Heightmap")) {
                        ImportHeightmap();
                    }
                    GUI.enabled = true;

#if UNITY_2017_1_OR_NEWER
                    EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
                    if(GUILayout.Button("Export As EXR", GUILayout.Width(150f), GUILayout.Height(24f))) {
                        Texture2D heightmapTex = new Texture2D(totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically, TextureFormat.RGBAFloat, false);

                        string saveDestination = EditorUtility.SaveFilePanel("Terrain Former", string.Empty, "Heightmap", "exr");

                        if(string.IsNullOrEmpty(saveDestination) == false) {
                            ExportHeightmap(ref heightmapTex);
                            File.WriteAllBytes(saveDestination, heightmapTex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
                        }

                        DestroyImmediate(heightmapTex);
                    }
#endif
                    break;
                case Tool.Generate:
                    settings.generateRampCurve = EditorGUILayout.CurveField("Falloff", settings.generateRampCurve);
                    if(Event.current.commandName == "CurveChanged") { 
                        ClampAnimationCurve(settings.generateRampCurve);
                        if(Event.current.type != EventType.Used && Event.current.type != EventType.Layout) Event.current.Use();
                    }
                    
                    settings.generateHeight = EditorGUILayout.Slider("Height", settings.generateHeight, 0f, terrainSize.y);

                    EditorGUILayout.LabelField("Linear Ramp", EditorStyles.boldLabel);
                    settings.generateRampCurveInXAxis = GUIUtilities.ToolbarWithLabel(new GUIContent("Ramp Axis"), settings.generateRampCurveInXAxis ? 0 : 1,
                        new string[] { "X-axis", "Z-axis" }) == 0;
                    Rect createLinearRampRect = EditorGUILayout.GetControlRect(GUILayout.Height(22f));
                    if(GUI.Button(new Rect(createLinearRampRect.xMax - 145f, createLinearRampRect.y, 145f, 22f), "Create Linear Ramp")) {
                        CreateLinearRamp(settings.generateHeight);
                    }

                    EditorGUILayout.LabelField("Circular Ramp", EditorStyles.boldLabel);
                    Rect createCircularRampRect = EditorGUILayout.GetControlRect(GUILayout.Height(22f));
                    if(GUI.Button(new Rect(createCircularRampRect.xMax - 145f, createCircularRampRect.y, 145f, 22f), "Create Circular Ramp")) {
                        CreateCircularRamp(settings.generateHeight);
                    }
                    
                    break;
                case Tool.Settings:
                    Rect goToPreferencesButtonRect = EditorGUILayout.GetControlRect(false, 22f);
                    goToPreferencesButtonRect.xMin = goToPreferencesButtonRect.xMax - 200f;
                    if(GUI.Button(goToPreferencesButtonRect, "Terrain Former Preferences")) {
                        Type preferencesWindowType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.PreferencesWindow");
                        MethodInfo showPreferencesWindowMethodInfo = preferencesWindowType.GetMethod("ShowPreferencesWindow", BindingFlags.NonPublic | BindingFlags.Static);
                        FieldInfo selectedSectionIndexFieldInfo = preferencesWindowType.GetField("m_SelectedSectionIndex", BindingFlags.NonPublic | BindingFlags.Instance);

                        FieldInfo sectionsFieldInfo = preferencesWindowType.GetField("m_Sections", BindingFlags.NonPublic | BindingFlags.Instance);

                        Type sectionType = preferencesWindowType.GetNestedType("Section", BindingFlags.NonPublic);

                        showPreferencesWindowMethodInfo.Invoke(null, null);
                        EditorWindow preferencesWindow = EditorWindow.GetWindowWithRect(preferencesWindowType, new Rect(100f, 100f, 500f, 400f), true, "Unity Preferences");

                        // Call PreferencesWindow.OnGUI method force it to add the custom sections so we have access to all sections.
                        MethodInfo preferencesWindowOnGUIMethodInfo = preferencesWindowType.GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
                        preferencesWindowOnGUIMethodInfo.Invoke(preferencesWindow, null);

                        IList sections = (IList)sectionsFieldInfo.GetValue(preferencesWindow);
                        for(int i = 0; i < sections.Count; i++) {
                            GUIContent sectionsContent = (GUIContent)sectionType.GetField("content").GetValue(sections[i]);
                            string sectionText = sectionsContent.text;
                            if(sectionText == "Terrain Former") {
                                selectedSectionIndexFieldInfo.SetValue(preferencesWindow, i);
                                break;
                            }
                        }
                    }
                    
                    EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);

                    float newTerrainLateralSize = Mathf.Max(EditorGUILayout.DelayedFloatField("Terrain Width/Length", firstTerrainData.size.x), 0f);
                    float newTerrainHeight = Mathf.Max(EditorGUILayout.DelayedFloatField("Terrain Height", firstTerrainData.size.y), 0f);

                    bool terrainSizeChangedLaterally = newTerrainLateralSize != firstTerrainData.size.x;
                    if(terrainSizeChangedLaterally || newTerrainHeight != firstTerrainData.size.y) {
                        List<UnityEngine.Object> objectsThatWillBeModified = new List<UnityEngine.Object>();
                        
                        foreach(TerrainInformation ti in terrainInformations) {
                            objectsThatWillBeModified.Add(ti.terrainData);
                            if(terrainSizeChangedLaterally) objectsThatWillBeModified.Add(ti.transform);
                        }

                        // Calculate the center of the terrain grid and use that to decide where how to resposition the terrain grid cells.
                        Vector2 previousTerrainGridSize = new Vector2(numberOfTerrainsHorizontally * terrainSize.x, numberOfTerrainsVertically * terrainSize.z);
                        Vector3 centerOfTerrainGrid = new Vector3(firstTerrainTransform.position.x + previousTerrainGridSize.x * 0.5f, firstTerrainTransform.position.y,
                            firstTerrainTransform.position.z + previousTerrainGridSize.y * 0.5f);
                        Vector3 newTerrainGridSizeHalf = new Vector3(numberOfTerrainsHorizontally * newTerrainLateralSize * 0.5f, 0f, 
                            numberOfTerrainsVertically * newTerrainLateralSize * 0.5f);
                        
                        Undo.RegisterCompleteObjectUndo(objectsThatWillBeModified.ToArray(), terrainInformations.Count == 1 ? "Terrain Size Changed" : "Terrain Sizes Changed");

                        foreach(TerrainInformation ti in terrainInformations) {
                            // Reposition the terrain grid (if there is more than one terrain) because the terrain size has changed laterally
                            if(terrainSizeChangedLaterally) {
                                ti.transform.position = new Vector3(
                                    (centerOfTerrainGrid.x - newTerrainGridSizeHalf.x) + ti.gridXCoordinate * newTerrainLateralSize, 
                                    ti.transform.position.y,
                                    (centerOfTerrainGrid.z - newTerrainGridSizeHalf.z) + ti.gridYCoordinate * newTerrainLateralSize
                                );
                            }

                            ti.terrainData.size = new Vector3(newTerrainLateralSize, newTerrainHeight, newTerrainLateralSize);
                        }

                        terrainSize = new Vector3(newTerrainLateralSize, newTerrainHeight, newTerrainLateralSize);
                    }

                    /**
                    * The following code is highly repetitive, but it must be written in this fashion. Writing this code in a more generalized fashion
                    * requires Reflection, but unfortunately virtually all properties are attributed with "MethodImplOptions.InternalCall", which as far as I
                    * know are not possible to be invoked using Reflection. As such, these properties must be set the manual way for all of their behaviours 
                    * to be executed.
                    */

                    EditorGUI.BeginChangeCheck();

                    // Base Terrain
                    bool newDrawHeightmap = EditorGUILayout.BeginToggleGroup("Base Terrain", firstTerrain.drawHeightmap);
                    if(firstTerrain.drawHeightmap != newDrawHeightmap) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.drawHeightmap = newDrawHeightmap;
                    }

                    EditorGUI.indentLevel = 1;
                    float newHeightmapPixelError = EditorGUILayout.Slider("Pixel Error", firstTerrain.heightmapPixelError, 1f, 200f);
                    if(firstTerrain.heightmapPixelError != newHeightmapPixelError) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.heightmapPixelError = newHeightmapPixelError;
                    }
                    
                    bool newCastShadows = EditorGUILayout.Toggle("Cast Shadows", firstTerrain.castShadows);
                    if(firstTerrain.castShadows != newCastShadows) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.castShadows = newCastShadows;
                    }
                    
                    Terrain.MaterialType newMaterialType = (Terrain.MaterialType)EditorGUILayout.EnumPopup("Material Type", firstTerrain.materialType);
                    if(firstTerrain.materialType != newMaterialType) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.materialType = newMaterialType;
                    }

                    switch(newMaterialType) {
                        case Terrain.MaterialType.BuiltInLegacySpecular:
                            EditorGUI.indentLevel++;
                            Color newLegacySpecular = EditorGUILayout.ColorField("Specular Colour", firstTerrain.legacySpecular);
                            if(firstTerrain.legacySpecular != newLegacySpecular) {
                                foreach(TerrainInformation ti in terrainInformations) ti.terrain.legacySpecular = newLegacySpecular;
                            }

                            float newLegacyShininess = EditorGUILayout.Slider("Shininess", firstTerrain.legacyShininess, 0.03f, 1f);
                            if(firstTerrain.legacyShininess != newLegacyShininess) {
                                foreach(TerrainInformation ti in terrainInformations) ti.terrain.legacyShininess = newLegacyShininess;
                            }
                            EditorGUI.indentLevel--;
                            break;
                        case Terrain.MaterialType.Custom:
                            EditorGUI.indentLevel++;
                            Material newMaterialTemplate = (Material)EditorGUILayout.ObjectField("Custom Material", firstTerrain.materialTemplate, typeof(Material), false);
                            if(firstTerrain.materialTemplate != newMaterialTemplate) {
                                foreach(TerrainInformation ti in terrainInformations) ti.terrain.materialTemplate = newMaterialTemplate;
                            }

                            if(firstTerrain.materialTemplate != null && TerrainSettings.ShaderHasTangentChannel(firstTerrain.materialTemplate.shader))
                                EditorGUILayout.HelpBox("Materials with shaders that require tangent geometry shouldn't be used on terrains. Instead, use one of the shaders found under Nature/Terrain.", MessageType.Warning, true);
                            EditorGUI.indentLevel--;
                            break;
                    }

                    if(newMaterialType == Terrain.MaterialType.BuiltInStandard || newMaterialType == Terrain.MaterialType.Custom) {
                        ReflectionProbeUsage newReflectionProbeUsage = (ReflectionProbeUsage)EditorGUILayout.EnumPopup("Reflection Probes", firstTerrain.reflectionProbeUsage);

                        List<ReflectionProbeBlendInfo> tempClosestReflectionProbes = new List<ReflectionProbeBlendInfo>();
                        foreach(TerrainInformation ti in terrainInformations) {
                            ti.terrain.reflectionProbeUsage = newReflectionProbeUsage;
                        }
                        
                        if(firstTerrain.reflectionProbeUsage != ReflectionProbeUsage.Off) {
                            GUI.enabled = false;

                            foreach(TerrainInformation ti in terrainInformations) {
                                ti.terrain.GetClosestReflectionProbes(tempClosestReflectionProbes);
                                
                                for(int i = 0; i < tempClosestReflectionProbes.Count; i++) {
                                    Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(16f));
                                    
                                    float xOffset = controlRect.x + 32f;
                                    
                                    if(terrainInformations.Count > 1) {
                                        GUI.Label(new Rect(xOffset, controlRect.y, 105f, 16f), new GUIContent(ti.terrain.name, ti.terrain.name), EditorStyles.miniLabel);
                                        xOffset += 105f;
                                    } else {
                                        GUI.Label(new Rect(xOffset, controlRect.y, 16f, 16f), "#" + i, EditorStyles.miniLabel);
                                        xOffset += 16f;
                                    }
                                    
                                    float objectFieldWidth = controlRect.width - 50f - xOffset;
                                    EditorGUI.ObjectField(new Rect(xOffset, controlRect.y, objectFieldWidth, 16f), tempClosestReflectionProbes[i].probe, typeof(ReflectionProbe), true);
                                    xOffset += objectFieldWidth;
                                    GUI.Label(new Rect(xOffset, controlRect.y, 65f, 16f), "Weight " + tempClosestReflectionProbes[i].weight.ToString("f2"), EditorStyles.miniLabel);
                                }
                            }
                            GUI.enabled = true;
                        }
                    }

                    float newThickness = EditorGUILayout.FloatField("Thickness", firstTerrainData.thickness);
                    if(firstTerrainData.thickness != newThickness) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.thickness = newThickness;
                    }
                    EditorGUI.indentLevel = 0;

                    EditorGUILayout.EndToggleGroup();

                    // Tree and Detail Objects
                    bool newDrawTreesAndFoliage = EditorGUILayout.BeginToggleGroup("Tree and Detail Objects", firstTerrain.drawTreesAndFoliage);
                    if(firstTerrain.drawTreesAndFoliage != newDrawTreesAndFoliage) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.drawTreesAndFoliage = newDrawTreesAndFoliage;
                    }

                    EditorGUI.indentLevel = 1;
                    bool newBakeLightProbesForTrees = EditorGUILayout.Toggle("Bake Light Probes for Trees", firstTerrain.bakeLightProbesForTrees);
                    if(firstTerrain.bakeLightProbesForTrees != newBakeLightProbesForTrees) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.bakeLightProbesForTrees = newBakeLightProbesForTrees;
                    }

                    float newDetailObjectDistance = EditorGUILayout.Slider("Detail Distance", firstTerrain.detailObjectDistance, 0f, 250f);
                    if(firstTerrain.detailObjectDistance != newDetailObjectDistance) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.detailObjectDistance = newDetailObjectDistance;
                    }

                    bool newCollectDetailPatches = EditorGUILayout.Toggle(collectDetailPatchesContent, firstTerrain.collectDetailPatches);
                    if(firstTerrain.collectDetailPatches != newCollectDetailPatches) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.collectDetailPatches = newCollectDetailPatches;
                    }

                    float newDetailObjectDensity = EditorGUILayout.Slider("Detail Density", firstTerrain.detailObjectDensity, 0f, 1f);
                    if(firstTerrain.detailObjectDensity != newDetailObjectDensity) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.detailObjectDensity = newDetailObjectDensity;
                    }

                    float newTreeDistance = EditorGUILayout.Slider("Tree Distance", firstTerrain.treeDistance, 0f, 2000f);
                    if(firstTerrain.treeDistance != newTreeDistance) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.treeDistance = newTreeDistance;
                    }
                    
                    float newTreeBillboardDistance = EditorGUILayout.Slider("Billboard Start", firstTerrain.treeBillboardDistance, 5f, 2000f);
                    if(firstTerrain.treeBillboardDistance != newTreeBillboardDistance) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.treeBillboardDistance = newTreeBillboardDistance;
                    }

                    float newTreeCrossFadeLength = EditorGUILayout.Slider("Fade Length", firstTerrain.treeCrossFadeLength, 0f, 200f);
                    if(firstTerrain.treeCrossFadeLength != newTreeCrossFadeLength) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.treeCrossFadeLength = newTreeCrossFadeLength;
                    }

                    int newTreeMaximumFullLODCount = EditorGUILayout.IntSlider("Max. Mesh Trees", firstTerrain.treeMaximumFullLODCount, 0, 10000);
                    if(firstTerrain.treeMaximumFullLODCount != newTreeMaximumFullLODCount) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.treeMaximumFullLODCount = newTreeMaximumFullLODCount;
                    }

                    EditorGUI.indentLevel = 0;

                    EditorGUILayout.EndToggleGroup();
                    // If any tree/detail/base terrain settings have changed, redraw the scene view
                    if(EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

                    GUILayout.Label("Wind Settings for Grass", EditorStyles.boldLabel);

                    float newWavingGrassStrength = EditorGUILayout.Slider("Strength", firstTerrainData.wavingGrassStrength, 0f, 1f);
                    if(firstTerrainData.wavingGrassStrength != newWavingGrassStrength) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.wavingGrassStrength = newWavingGrassStrength;
                    }

                    float newWavingGrassSpeed = EditorGUILayout.Slider("Speed", firstTerrainData.wavingGrassSpeed, 0f, 1f);
                    if(firstTerrainData.wavingGrassSpeed != newWavingGrassSpeed) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.wavingGrassSpeed = newWavingGrassSpeed;
                    }

                    float newWavingGrassAmount = EditorGUILayout.Slider("Bending", firstTerrainData.wavingGrassAmount, 0f, 1f);
                    if(firstTerrainData.wavingGrassAmount != newWavingGrassAmount) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.wavingGrassAmount = newWavingGrassAmount;
                    }

                    Color newWavingGrassTint = EditorGUILayout.ColorField("Tint", firstTerrainData.wavingGrassTint);
                    if(firstTerrainData.wavingGrassTint != newWavingGrassTint) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.wavingGrassTint = newWavingGrassTint;
                    }
                    
                    GUILayout.Label("Resolution", EditorStyles.boldLabel);

                    int newHeightmapResolution = EditorGUILayout.IntPopup(TerrainSettings.heightmapResolutionContent, firstTerrainData.heightmapResolution, TerrainSettings.heightmapResolutionsContents,
                            TerrainSettings.heightmapResolutions);
                    if(firstTerrainData.heightmapResolution != newHeightmapResolution && 
                        EditorUtility.DisplayDialog("Terrain Former", "Changing the heightmap resolution will reset the heightmap.", "Change Anyway", "Cancel")) {
                        // TODO: Make this non-destructive
                        foreach(TerrainInformation ti in terrainInformations) {
                            ti.terrainData.heightmapResolution = newHeightmapResolution;
                            ti.terrainData.size = terrainSize;
                        }
                        heightmapResolution = newHeightmapResolution;
                        OnEnable();
                    }

                    int newAlphamapResolution = EditorGUILayout.IntPopup(TerrainSettings.alphamapResolutionContent, firstTerrainData.alphamapResolution, TerrainSettings.validTextureResolutionsContent,
                            TerrainSettings.validTextureResolutions);
                    if(firstTerrainData.alphamapResolution != newAlphamapResolution &&
                        EditorUtility.DisplayDialog("Terrain Former", "Changing the alphamap resolution will reset the alphamap.", "Change Anyway", "Cancel")) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.alphamapResolution = newAlphamapResolution;
                        alphamapResolution = newAlphamapResolution;
                        OnEnable();
                    }

                    int newBaseMapResolution = EditorGUILayout.IntPopup(TerrainSettings.basemapResolutionContent, firstTerrainData.baseMapResolution, TerrainSettings.validTextureResolutionsContent,
                            TerrainSettings.validTextureResolutions);
                    if(firstTerrainData.baseMapResolution != newBaseMapResolution) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrainData.baseMapResolution = newBaseMapResolution;
                    }


                    float newBasemapDistance = EditorGUILayout.Slider(TerrainSettings.basemapDistanceContent, firstTerrain.basemapDistance, 0f, 2000f);
                    if(firstTerrain.basemapDistance != newBasemapDistance) {
                        foreach(TerrainInformation ti in terrainInformations) ti.terrain.basemapDistance = newBasemapDistance;
                    }

                    // Detail Resolution
                    int newDetailResolution = Utilities.RoundToNearestAndClamp(EditorGUILayout.DelayedIntField(TerrainSettings.detailResolutionContent, firstTerrainData.detailResolution),
                        8, 0, 4048);
                    // Update all detail layers if the detail resolution has changed.
                    if(newDetailResolution != firstTerrainData.detailResolution &&
                        EditorUtility.DisplayDialog("Terrain Former", "Changing the detail map resolution will clear all details.", "Change Anyway", "Cancel")) {
                        List<int[,]> detailLayers = new List<int[,]>();
                        for(int i = 0; i < firstTerrainData.detailPrototypes.Length; i++) {
                            detailLayers.Add(firstTerrainData.GetDetailLayer(0, 0, firstTerrainData.detailWidth, firstTerrainData.detailHeight, i));
                        }
                        foreach(TerrainInformation terrainInformation in terrainInformations) {
                            terrainInformation.terrainData.SetDetailResolution(newDetailResolution, 8);
                            for(int i = 0; i < detailLayers.Count; i++) {
                                terrainInformation.terrainData.SetDetailLayer(0, 0, i, detailLayers[i]);
                            }
                        }
                    }

                    // Detail Resolution Per Patch
                    int currentDetailResolutionPerPatch = TerrainSettings.GetDetailResolutionPerPatch(firstTerrainData);
                    int newDetailResolutionPerPatch = Mathf.Clamp(EditorGUILayout.DelayedIntField(TerrainSettings.detailResolutionPerPatchContent, currentDetailResolutionPerPatch), 8, 128);
                    if(newDetailResolutionPerPatch != currentDetailResolutionPerPatch) {
                        foreach(TerrainInformation terrainInformation in terrainInformations) {
                            terrainInformation.terrainData.SetDetailResolution(firstTerrainData.detailResolution, newDetailResolutionPerPatch);
                        }
                    }

                    if(firstTerrain.materialType != Terrain.MaterialType.Custom) {
                        firstTerrain.materialTemplate = null;
                    }

                    if(terrainInformations.Count == 1) {
                        terrainInformations[0].terrainData = (TerrainData)EditorGUILayout.ObjectField("Terrain Data Asset", terrainInformations[0].terrainData, typeof(TerrainData), false);
                    } else {
                        EditorGUILayout.LabelField("Terrain Data Assets", EditorStyles.boldLabel);
                        foreach(TerrainInformation ti in terrainInformations) {
                            ti.terrainData = (TerrainData)EditorGUILayout.ObjectField(ti.transform.name, ti.terrainData, typeof(TerrainData), false);
                        }
                    }

                    // Draw the terrain informations as visual representations
                    if(terrainInformations.Count > 1) {
                        GUILayout.Space(5f);
                        neighboursFoldout = GUIUtilities.FullClickRegionFoldout("Neighbours", neighboursFoldout);
                        if(neighboursFoldout) {
                            Rect hoverRect = new Rect();
                            string hoverText = null;

                            const int neighboursCellSize = 30;
                            const int neighboursCellSizeMinusOne = neighboursCellSize - 1;

                            Rect neighboursGridRect = GUILayoutUtility.GetRect(Screen.width - 35f, numberOfTerrainsVertically * neighboursCellSize + 15f);
                            int neighboursGridRectWidth = neighboursCellSizeMinusOne * numberOfTerrainsHorizontally;
                            int neighboursGridRectHeight = neighboursCellSizeMinusOne * numberOfTerrainsVertically;
                            neighboursGridRect.yMin += 15f;
                            neighboursGridRect.xMin = Screen.width * 0.5f - neighboursGridRectWidth * 0.5f;
                            neighboursGridRect.width = neighboursGridRectWidth;

                            if(neighboursGridRect.Contains(Event.current.mousePosition)) Repaint();

                            GUIStyle boldLabelWithoutPadding = new GUIStyle(EditorStyles.boldLabel);
                            boldLabelWithoutPadding.padding = new RectOffset();
                            boldLabelWithoutPadding.alignment = TextAnchor.MiddleCenter;
                            // Axis Labels
                            GUI.Label(new Rect(Screen.width * 0.5f - 9f, neighboursGridRect.y - 15f, 20f, 10f), "Z", boldLabelWithoutPadding);
                            GUI.Label(new Rect(neighboursGridRect.xMax + 7f, neighboursGridRect.y + neighboursGridRectHeight * 0.5f - 6f, 10f, 10f), "X", boldLabelWithoutPadding);

                            foreach(TerrainInformation terrainInformation in terrainInformations) {
                                GUI.color = terrainInformation.terrain == firstTerrain && !isTerrainGridParentSelected ? new Color(0.4f, 0.4f, 0.75f) : Color.white;
                                Rect cellRect = new Rect(neighboursGridRect.x + terrainInformation.gridXCoordinate * neighboursCellSizeMinusOne, neighboursGridRect.y + 
                                    (numberOfTerrainsVertically - 1 - terrainInformation.gridYCoordinate) * neighboursCellSizeMinusOne, neighboursCellSize, neighboursCellSize);
                                
                                if(cellRect.Contains(Event.current.mousePosition)) {
                                    if(Event.current.type == EventType.MouseUp) {
                                        EditorGUIUtility.PingObject(terrainInformation.terrain.gameObject);
                                    } else {
                                        hoverText = terrainInformation.terrain.name;
                                        if(terrainInformation.terrain == firstTerrain && isTerrainGridParentSelected == false) hoverText += " (selected)";
                                        Vector2 calculatedSize = GUI.skin.box.CalcSize(new GUIContent(hoverText));
                                        hoverRect = new Rect(Mathf.Max(cellRect.x + 15f - calculatedSize.x * 0.5f, 0f), cellRect.y + calculatedSize.y + 5f, calculatedSize.x, calculatedSize.y);
                                    }
                                } 

                                GUI.Box(cellRect, (terrainInformation.gridXCoordinate + 1) + "x" + (terrainInformation.gridYCoordinate + 1), neighboursCellBoxStyle);
                            }

                            GUI.color = Color.white;

                            if(hoverText != null) {
                                GUI.Box(hoverRect, hoverText);
                            }
                        }
                    }

                    break;
            }

            float lastLabelWidth = EditorGUIUtility.labelWidth;
            
            if(CurrentTool >= firstNonMouseTool) return;

            GUILayout.Space(3f);

            /**
            * Brush Selection
            */
            if(settings.AlwaysShowBrushSelection || isSelectingBrush) {
                Rect brushesTitleRect = EditorGUILayout.GetControlRect();
                GUI.Label(brushesTitleRect, settings.AlwaysShowBrushSelection ? "Brushes" : "Select Brush", EditorStyles.boldLabel);

                if(settings.AlwaysShowBrushSelection) {
                    brushesTitleRect.xMin = brushesTitleRect.xMax - 300f;
                    GUI.Label(brushesTitleRect, CurrentBrush.name, brushNameAlwaysShowBrushSelectionStyle);
                }
                
                if(settings.brushSelectionDisplayType == BrushSelectionDisplayType.Tabbed) {
                    string newBrushTab = GUIUtilities.BrushTypeToolbar(settings.modeSettings[CurrentTool].selectedBrushTab, brushCollection);
                    if(newBrushTab != settings.modeSettings[CurrentTool].selectedBrushTab) {
                        settings.modeSettings[CurrentTool].selectedBrushTab = newBrushTab;
                        SelectedBrushTabChanged();
                    }
                }

                string newlySelectedBrush = GUIUtilities.BrushSelectionGrid(settings.modeSettings[CurrentTool].selectedBrushId);
                if(newlySelectedBrush != settings.modeSettings[CurrentTool].selectedBrushId) {
                    settings.modeSettings[CurrentTool].selectedBrushId = newlySelectedBrush;
                    SelectedBrushChanged();
                }
            }

            if(settings.AlwaysShowBrushSelection == false && isSelectingBrush == true) return;
            if(settings.AlwaysShowBrushSelection) {
                GUILayout.Space(6f);
            }
            if(CurrentTool != Tool.RaiseOrLower) {
                GUILayout.Label("Brush", EditorStyles.boldLabel);
            }

            // The width of the area used to show the button to select a brush. Only applicable when AlwaysShowBrushSelection is false.
            float brushSelectionWidth = Mathf.Clamp(settings.brushPreviewSize + 28f, 80f, 84f);

            GUILayout.BeginHorizontal(); // Brush Parameter Editor Horizontal Group
            
            // Draw Brush Paramater Editor
            if(settings.AlwaysShowBrushSelection) {
                EditorGUILayout.BeginVertical();
            } else {
                EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width - brushSelectionWidth - 15f));
            }

            bool isBrushProcedural = CurrentBrush is ImageBrush == false;
            
            float newBrushSize = EditorGUILayout.Slider("Size", settings.modeSettings[CurrentTool].brushSize, MinBrushSize, MaxBrushSize);
            if(newBrushSize != settings.modeSettings[CurrentTool].brushSize) {
                settings.modeSettings[CurrentTool].brushSize = newBrushSize;
                BrushSizeChanged();
            }

            float newBrushSpeed;
            if(CurrentTool == Tool.PaintTexture) {
                newBrushSpeed = EditorGUILayout.Slider("Opacity", settings.modeSettings[CurrentTool].brushSpeed, minBrushSpeed, 1f);
            } else if(CurrentTool == Tool.Smooth || CurrentTool == Tool.ShrinkWrap) {
                newBrushSpeed = EditorGUILayout.Slider("Speed", settings.modeSettings[CurrentTool].brushSpeed, minBrushSpeed, 1f);
            } else {
                newBrushSpeed = EditorGUILayout.Slider("Speed", settings.modeSettings[CurrentTool].brushSpeed, minBrushSpeed, maxBrushSpeed);
            }
            if(newBrushSpeed != settings.modeSettings[CurrentTool].brushSpeed) {
                settings.modeSettings[CurrentTool].brushSpeed = newBrushSpeed;
                BrushSpeedChanged();
            }
            
            if(isBrushProcedural) {
                settings.modeSettings[CurrentTool].brushFalloff = EditorGUILayout.CurveField("Falloff", settings.modeSettings[CurrentTool].brushFalloff);
                if(Event.current.commandName == "CurveChanged") {
                    BrushFalloffChanged();
                    if(Event.current.type != EventType.Used && Event.current.type != EventType.Layout) Event.current.Use();
                }
            } else {
                GUIUtilities.FillAndRightControl(
                    fillControl: (r) => {
                        Rect falloffToggleRect = new Rect(r);
                        falloffToggleRect.xMax = EditorGUIUtility.labelWidth;
                        bool newUseFalloffForCustomBrushes = EditorGUI.Toggle(falloffToggleRect, settings.modeSettings[CurrentTool].useFalloffForCustomBrushes);
                        if(newUseFalloffForCustomBrushes != settings.modeSettings[CurrentTool].useFalloffForCustomBrushes) {
                            settings.modeSettings[CurrentTool].useFalloffForCustomBrushes = newUseFalloffForCustomBrushes;
                            UpdatePreviewTexturesAndBrushSamples();
                        }

                        Rect falloffToggleLabelRect = new Rect(falloffToggleRect);
                        falloffToggleLabelRect.xMin += 15f;
                        EditorGUI.PrefixLabel(falloffToggleLabelRect, new GUIContent("Falloff"));

                        Rect falloffAnimationCurveRect = new Rect(r);
                        falloffAnimationCurveRect.xMin = EditorGUIUtility.labelWidth + 14f;
                        settings.modeSettings[CurrentTool].brushFalloff = EditorGUI.CurveField(falloffAnimationCurveRect, settings.modeSettings[CurrentTool].brushFalloff);
                        if(Event.current.commandName == "CurveChanged") {
                            BrushFalloffChanged();
                            if(Event.current.type != EventType.Used && Event.current.type != EventType.Layout) Event.current.Use();
                        }
                    },
                    rightControl: (r) => {
                        using(new GUIUtilities.GUIEnabledBlock(settings.modeSettings[CurrentTool].useFalloffForCustomBrushes)) {
                            Rect alphaFalloffLabelRect = new Rect(r);
                            alphaFalloffLabelRect.xMin += 14;
                            GUI.Label(alphaFalloffLabelRect, "Alpha");

                            Rect alphaFalloffRect = new Rect(r);
                            alphaFalloffRect.xMin--;
                            bool newUseAlphaFalloff = EditorGUI.Toggle(alphaFalloffRect, settings.modeSettings[CurrentTool].useAlphaFalloff);
                            if(newUseAlphaFalloff != settings.modeSettings[CurrentTool].useAlphaFalloff) {
                                settings.modeSettings[CurrentTool].useAlphaFalloff = newUseAlphaFalloff;
                                UpdatePreviewTexturesAndBrushSamples();
                            }
                        }
                    },
                    rightControlWidth: 54
                );
            }

            // We need to delay updating brush samples while changing falloff until changes have stopped for at least one frame
            if(falloffChangeQueued && (EditorApplication.timeSinceStartup - lastTimeBrushSamplesWereUpdated) > 0.05d) {
                falloffChangeQueued = false;
                UpdateDirtyBrushSamples();
            }

            if(isBrushProcedural == false && settings.modeSettings[CurrentTool].useFalloffForCustomBrushes == false) {
                GUI.enabled = false;
            }

            EditorGUI.indentLevel = 1;
            float newBrushRoundness = EditorGUILayout.Slider("Roundness", settings.modeSettings[CurrentTool].brushRoundness, 0f, 1f);
            if(newBrushRoundness != settings.modeSettings[CurrentTool].brushRoundness) {
                settings.modeSettings[CurrentTool].brushRoundness = newBrushRoundness;
                BrushRoundnessChanged();
            }
            EditorGUI.indentLevel = 0;

            if(isBrushProcedural == false && settings.modeSettings[CurrentTool].useFalloffForCustomBrushes == false) {
                GUI.enabled = true;
            }

            /**
            * Custom Brush Angle
            */
            float newBrushAngle = EditorGUILayout.Slider("Angle", settings.modeSettings[CurrentTool].brushAngle, -180f, 180f);
            if(newBrushAngle != settings.modeSettings[CurrentTool].brushAngle) {
                float delta = settings.modeSettings[CurrentTool].brushAngle - newBrushAngle;
                settings.modeSettings[CurrentTool].brushAngle = newBrushAngle;
                BrushAngleDeltaChanged(delta);
            }

            /**
            * Invert Brush (for custom brushes only)
            */
            if(settings.invertBrushTexturesGlobally) {
                GUI.enabled = false;
                EditorGUILayout.Toggle("Invert", true);
                GUI.enabled = true;
            } else {
                bool newInvertBrushTexture = EditorGUILayout.Toggle("Invert", settings.modeSettings[CurrentTool].invertBrushTexture);
                if(newInvertBrushTexture != settings.modeSettings[CurrentTool].invertBrushTexture) {
                    settings.modeSettings[CurrentTool].invertBrushTexture = newInvertBrushTexture;
                    InvertBrushTextureChanged();
                }
            }

            /**
            * Noise Brush Parameters
            */
            if(CurrentBrush is PerlinNoiseBrush) {
                EditorGUI.BeginChangeCheck();
                settings.perlinNoiseScale = EditorGUILayout.Slider("Noise Scale", settings.perlinNoiseScale, 5f, 750f);
                if(EditorGUI.EndChangeCheck()) {
                    samplesDirty |= SamplesDirty.ProjectorTexture;
                    UpdateAllNecessaryPreviewTextures();
                }
                
                bool perlinNoiseMinMaxChanged = GUIUtilities.MinMaxWithFloatFields("Noise Clipping", ref settings.perlinNoiseMin, ref settings.perlinNoiseMax, 0f, 1f, 3);
                if(perlinNoiseMinMaxChanged) {
                    samplesDirty |= SamplesDirty.ProjectorTexture;
                    UpdateAllNecessaryPreviewTextures();
                }
            }

            EditorGUILayout.EndVertical();

            if(settings.AlwaysShowBrushSelection == false) {
                GUILayout.Space(-4f);

                GUILayout.BeginVertical(GUILayout.Width(brushSelectionWidth), GUILayout.Height(95f));

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayoutOption[] brushNameLabelLayoutOptions = { GUILayout.Width(brushSelectionWidth - 17f), GUILayout.Height(24f) };
                GUILayout.Box(CurrentBrush.name, miniBoldLabelCentered, brushNameLabelLayoutOptions);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Draw Brush Preview
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if(GUILayout.Button(CurrentBrush.previewTexture, GUIStyle.none)) {
                    ToggleSelectingBrush();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Draw Select/Cancel Brush Selection Button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Select", miniButtonWithoutMargin, GUILayout.Width(60f), GUILayout.Height(18f))) {
                    ToggleSelectingBrush();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            
            GUILayout.EndHorizontal(); // Brush Parameter Editor Horizontal Group

            /**
            * Behaviour
            */
            EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);

            /**
            * TODO: Versions older than Unity 2017.1 don't handle out and ref keywords in blocks with parameters labels for some reason, so no
            * pretty parameter labels :(
            */
            if(GUIUtilities.TogglMinMaxAndFloatFields(
                "Random Spacing",                                       // label
                ref settings.modeSettings[CurrentTool].useBrushSpacing, // toggleValue
                ref settings.modeSettings[CurrentTool].minBrushSpacing, // minValue
                ref settings.modeSettings[CurrentTool].maxBrushSpacing, // maxValue
                minSpacingBounds,                                       // minValueBoundary
                maxSpacingBounds,                                       // maxValueBoundary
                5                                                       // significantDigits
            )) {
                // If the min/max values were changed, assume the user wants brush spacing to be enabled.
                settings.modeSettings[CurrentTool].useBrushSpacing = true;
            }
            
            float maxRandomOffset = Mathf.Min(firstTerrainData.heightmapWidth, firstTerrainData.heightmapHeight) * 0.5f;
            GUIUtilities.ToggleAndFillControl(
                new GUIContent("Random Offset"),                        // label
                ref settings.modeSettings[CurrentTool].useRandomOffset, // enableFillControl
                (r) => {                                                // fillControl
                    EditorGUI.BeginChangeCheck();
                    settings.modeSettings[CurrentTool].randomOffset = EditorGUI.Slider(r, settings.modeSettings[CurrentTool].randomOffset, minRandomOffset, maxRandomOffset);
                    if(EditorGUI.EndChangeCheck()) {
                        settings.modeSettings[CurrentTool].useRandomOffset = true;
                    }
                }
            );

            if(GUIUtilities.TogglMinMaxAndFloatFields(
                "Random Rotation",                                        // label
                ref settings.modeSettings[CurrentTool].useRandomRotation, // toggleValue
                ref settings.modeSettings[CurrentTool].minRandomRotation, // minValue
                ref settings.modeSettings[CurrentTool].maxRandomRotation, // maxValue
                minRandomRotationBounds,                                  // minValueBoundary
                maxRandomRotationBounds,                                  // maxValueBoundary
                5                                                         // significantDigits
            )) {
                settings.modeSettings[CurrentTool].useRandomRotation = true;
            }
             
            terrainMismatchManager.Draw();

            EditorGUIUtility.labelWidth = lastLabelWidth;
        }
        
        // Make sure the toolbar tooltip actually is hidden after the mouse has left the area.
        public override bool RequiresConstantRepaint() {
            return isShowingToolTooltip;
        }
        
        private void OnSceneGUICallback(SceneView sceneView) {
            // There are magical times where Terrain Former didn't receive the OnDisable message and continues to subscribe to OnSceneGUI
            if(terrainFormer == null) {
                OnDisable();
                return;
            }
            if(Initialize() == false) return;

            if(CurrentTool == Tool.None) {
                SetCursorEnabled(false);
            } else if((Event.current.control && mouseIsDown) == false) {
                UpdateProjector();
            }

            Event currentEvent = Event.current;
            // Get a unique ID for this editor so we can get events unique the editor's scope
            int controlId = GUIUtility.GetControlID(terrainEditorHash, FocusType.Passive);
            
            /**
            * Draw scene-view information
            */
            if(IsCurrentModeSculptive() && settings.showSceneViewInformation && 
                (settings.displaySceneViewCurrentHeight || settings.displaySceneViewCurrentTool || settings.displaySceneViewSculptOntoMode)) {
                /**
                * For some reason this must be set for the SceneViewPanel to be rendered correctly - this won't be an issue if it was simple called in a 
                * OnSceneGUI "message". However multiple OnSceneGUI calls don't come through if there are multiple inspectors tabs/windwos at once.
                */
                GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

                Handles.BeginGUI();

                if(sceneViewInformationAreaStyle == null) {
                    sceneViewInformationAreaStyle = new GUIStyle(GUI.skin.box);
                    sceneViewInformationAreaStyle.padding = new RectOffset(5, 0, 5, 0);
                }
                if(sceneViewInformationAreaStyle.normal.background == null || sceneViewInformationAreaStyle.normal.background.name == "OL box") {
                    sceneViewInformationAreaStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(settings.mainDirectory + "Textures/SceneInfoPanel.PSD");
                    sceneViewInformationAreaStyle.border = new RectOffset(12, 12, 12, 12);
                }

                int lines = settings.displaySceneViewCurrentHeight ? 1 : 0;
                lines += settings.displaySceneViewCurrentTool ? 1 : 0;
                lines += settings.displaySceneViewSculptOntoMode ? 1 : 0;

                GUILayout.BeginArea(new Rect(5f, 5f, 185f, 15f * lines + 14f), sceneViewInformationAreaStyle);

                const float parameterLabelOffset = 7f;
                const float valueParameterLeftOffset = 90f;
                float yOffset = 7f;

                if(settings.displaySceneViewCurrentTool) {
                    EditorGUI.LabelField(new Rect(parameterLabelOffset, yOffset, 135f, 18f), "Tool:");
                    GUI.Label(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), toolsGUIContents[(int)currentTool.Value].tooltip);
                    yOffset += 15f;
                }
                if(settings.displaySceneViewCurrentHeight) {
                    float height;
                    EditorGUI.LabelField(new Rect(parameterLabelOffset, yOffset, 135f, 18f), "Height:");
                    if(currentEvent.control && mouseIsDown) {
                        EditorGUI.LabelField(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), lastClickPosition.y.ToString("0.00"));
                    } else if(GetTerrainHeightAtMousePosition(out height)) {
                        EditorGUI.LabelField(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), height.ToString("0.00"));
                    } else {
                        EditorGUI.LabelField(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), "0.00");
                    }
                    yOffset += 15f;
                }
                if(settings.displaySceneViewSculptOntoMode) {
                    EditorGUI.LabelField(new Rect(parameterLabelOffset, yOffset, 135f, 18f), "Sculpt Onto:");
                    if(CurrentTool == Tool.SetHeight || CurrentTool == Tool.Flatten) {
                        EditorGUI.LabelField(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), "Plane (locked)");
                    } else {
                        EditorGUI.LabelField(new Rect(valueParameterLeftOffset, yOffset, 135f, 18f), raycastModes[settings.raycastOntoFlatPlane ? 0 : 1]);
                    }
                }
                GUILayout.EndArea();
                Handles.EndGUI();
            }

            if(GUIUtility.hotControl != 0 && GUIUtility.hotControl != controlId) return;
            
            CheckKeyboardShortcuts(currentEvent);
            
            if(IsCurrentModeSculptive() == false) return;

            /**
            * Frame Selected (Shortcut: F)
            */
            if(currentEvent.type == EventType.ExecuteCommand && currentEvent.commandName == "FrameSelected") {
                Vector3 mouseWorldspacePosition;
                if(GetMousePositionInWorldSpace(out mouseWorldspacePosition)) {
                    SceneView.currentDrawingSceneView.LookAt(pos: mouseWorldspacePosition, rot: SceneView.currentDrawingSceneView.rotation, 
                        newSize: GetCurrentToolSettings().brushSize * 1.4f);
                } else {
                    float largestTerrainAxis = Mathf.Max(numberOfTerrainsHorizontally * terrainSize.x, numberOfTerrainsVertically * terrainSize.z);
                    Vector3 centerOfTerrainGrid = firstTerrainTransform.position + new Vector3(numberOfTerrainsHorizontally * terrainSize.x * 0.5f, 0f, 
                        numberOfTerrainsVertically * terrainSize.z * 0.5f);
                    SceneView.currentDrawingSceneView.LookAt(centerOfTerrainGrid, SceneView.currentDrawingSceneView.rotation, largestTerrainAxis * 1.2f);
                }
                currentEvent.Use();
            }

            EventType editorEventType = currentEvent.GetTypeForControl(controlId);
            // Update mouse-related fields
            if(editorEventType == EventType.Repaint || currentEvent.isMouse) {
                if(mousePosition == Vector2.zero) {
                    lastMousePosition = currentEvent.mousePosition;
                } else {
                    lastMousePosition = mousePosition;
                }

                mousePosition = currentEvent.mousePosition;

                if(editorEventType == EventType.MouseDown) {
                    currentTotalMouseDelta = 0;
                } else {
                    currentTotalMouseDelta += mousePosition.y - lastMousePosition.y;
                }
            }

            // Only accept left clicks
            if(currentEvent.button != 0) return;

            switch(editorEventType) {
                // MouseDown will execute the same logic as MouseDrag
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    /*
                    * Break if any of the following rules are true:
                    * 1) The event happening for this window is a MouseDrag event and the hotControl isn't this window
                    * 2) Alt + Click have been executed
                    * 3) The HandleUtllity finds a control closer to this control
                    */
                    if(editorEventType == EventType.MouseDrag &&
                        GUIUtility.hotControl != controlId ||
                        (currentEvent.alt || currentEvent.button != 0) ||
                        HandleUtility.nearestControl != controlId) {
                        break;
                    }
                    if(currentEvent.type == EventType.MouseDown) {
                        /**
                        * To make sure the initial press down always sculpts the terrain while spacing is active, set 
                        * the mouseSpacingDistance to a high value to always activate it straight away
                        */
                        mouseSpacingDistance = float.MaxValue;
                        UpdateRandomSpacing();
                        GUIUtility.hotControl = controlId;
                        
                    }

                    // Update the lastClickPosition when the mouse has been pressed down
                    if(mouseIsDown == false) {
                        Vector3 hitPosition;
                        Vector2 uv;
                        if(Raycast(out hitPosition, out uv)) {
                            lastWorldspaceMousePosition = hitPosition;
                            lastClickPosition = hitPosition;
                            mouseIsDown = true;
                        }
                    }

                    currentEvent.Use();
                    break;
                case EventType.MouseUp:
                    // Reset the hotControl to nothing as long as it matches the TerrainEditor controlID
                    if(GUIUtility.hotControl != controlId) break;

                    GUIUtility.hotControl = 0;
                    
                    foreach(TerrainInformation terrainInformation in terrainInformations) {
                        // Render all aspects of terrain (heightmap, trees and details)
                        terrainInformation.terrain.editorRenderFlags = TerrainRenderFlags.all;
                        
                        if(CurrentTool == Tool.PaintTexture) {
                            terrainDataSetBasemapDirtyMethodInfo.Invoke(terrainInformation.terrainData, new object[] { true });
                        }
                        
                        if(settings.alwaysUpdateTerrainLODs) {
                            terrainInformation.terrain.ApplyDelayedHeightmapModification();
                        }
                    }

                    gridPlane.SetActive(false);

                    // Reset the flatten height tool's value after the mouse has been released
                    flattenHeight = -1f;

                    mouseIsDown = false;

                    if(currentCommand is ShrinkWrapCommand) {
                        ResetLayerOfTerrains();
                    }

                    currentCommand = null;
                    currentTotalMouseDelta = 0f;
                    lastClickPosition = Vector3.zero;
                    
                    currentEvent.Use();
                    break;
                case EventType.KeyUp:
                    // If a kew has been released, make sure any keyboard shortcuts have their changes applied via UpdateDirtyBrushSamples
                    UpdateDirtyBrushSamples();
                    break;
                case EventType.Repaint:
                    SetCursorEnabled(false);
                    break;
                case EventType.Layout:
                    if(CurrentTool == Tool.None) break;

                    // Sets the ID of the default control. If there is no other handle being hovered over, it will choose this value
                    HandleUtility.AddDefaultControl(controlId);
                    break;
            }
            
            // Apply the current terrain tool
            if(editorEventType == EventType.Repaint && mouseIsDown) {
                Vector3 mouseWorldspacePosition;
                if(GetMousePositionInWorldSpace(out mouseWorldspacePosition)) {
                    if(settings.modeSettings[CurrentTool].useBrushSpacing) {
                        mouseSpacingDistance += (new Vector2(lastWorldspaceMousePosition.x, lastWorldspaceMousePosition.z) -
                            new Vector2(mouseWorldspacePosition.x, mouseWorldspacePosition.z)).magnitude;
                    }

                    Vector3 finalMousePosition;
                    if(CurrentTool < firstNonMouseTool && currentEvent.control) {
                        finalMousePosition = lastClickPosition;
                    } else {
                        finalMousePosition = mouseWorldspacePosition;
                    }

                    // Apply the random offset to the mouse position (if necessary)
                    if(currentEvent.control == false && settings.modeSettings[CurrentTool].useRandomOffset) {
                        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * settings.modeSettings[CurrentTool].randomOffset;
                        finalMousePosition += new Vector3(randomOffset.x, 0f, randomOffset.y);
                    }
                    
                    /**
                    * Calculate the command coordinates for each terrain information which determines which area of a given terrain (if at all) 
                    * will have the current command applied to it
                    */
                    foreach(TerrainInformation terrainInformation in terrainInformations) {
                        terrainInformation.commandArea = CalculateCommandCoordinatesForTerrain(terrainInformation, finalMousePosition);
                    }
                    
                    commandArea = CalculateCommandCoordinatesForTerrainGrid(finalMousePosition);
                    
                    /**
                    * Update the grid position
                    */
                    if(settings.showSculptingGridPlane == true) {
                        if(gridPlane.activeSelf == false) {
                            gridPlane.SetActive(true);
                        }

                        Vector3 gridPosition;
                        // If the current tool is interactive, keep the grid at the lastGridPosition
                        if(currentEvent.control) {
                            gridPosition = new Vector3(lastClickPosition.x, lastClickPosition.y + 0.001f, lastClickPosition.z);
                        } else {
                            gridPosition = new Vector3(mouseWorldspacePosition.x, lastClickPosition.y + 0.001f, mouseWorldspacePosition.z);
                        }
                        float gridPlaneDistance = Mathf.Abs(lastClickPosition.y - SceneView.currentDrawingSceneView.camera.transform.position.y);
                        float gridPlaneSize = settings.modeSettings[CurrentTool].brushSize * 1.2f;
                        gridPlane.transform.position = gridPosition;
                        gridPlane.transform.localScale = Vector3.one * gridPlaneSize;

                        // Get the Logarithm of base 10 from the distance to get a power to mutliple the grid scale by
                        float power = Mathf.Round(Mathf.Log10(gridPlaneDistance) - 1);

                        // Make the grid appear as if it's being illuminated by the cursor but keeping the grids remain within unit size tiles
                        gridPlaneMaterial.mainTextureOffset = new Vector2(gridPosition.x, gridPosition.z) / Mathf.Pow(10f, power);

                        gridPlaneMaterial.mainTextureScale = new Vector2(gridPlaneSize, gridPlaneSize) / Mathf.Pow(10f, power);
                    }

                    // Update the unmodifidied heights that are used to make all commands order-independent (eg, interactive tools or smoothing)
                    if(currentCommand == null || Event.current.control == false) {
                        UpdateAllUnmodifiedHeights();
                    }

                    if(currentCommand == null) {
                        switch(CurrentTool) {
                            case Tool.RaiseOrLower:
                                currentCommand = new RaiseOrLowerCommand(GetBrushSamplesWithSpeed());
                                break;
                            case Tool.Smooth:
                                SmoothCommand smoothCommand = new SmoothCommand(GetBrushSamplesWithSpeed(), settings.boxFilterSize, totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically);
                                currentCommand = smoothCommand;
                                break;
                            case Tool.SetHeight:
                                SetHeightCommand setHeightCommand = new SetHeightCommand(GetBrushSamplesWithSpeed());
                                setHeightCommand.normalizedHeight = settings.setHeight / terrainSize.y;
                                currentCommand = setHeightCommand;
                                break;
                            case Tool.Flatten:
                                // Update the flatten height if it was reset before
                                if(flattenHeight == -1f) {
                                    flattenHeight = (mouseWorldspacePosition.y - firstTerrain.transform.position.y) / terrainSize.y;
                                }
                                FlattenCommand flattenCommand = new FlattenCommand(GetBrushSamplesWithSpeed());
                                flattenCommand.mode = settings.flattenMode;
                                flattenCommand.flattenHeight = flattenHeight;
                                currentCommand = flattenCommand;
                                break;
                            case Tool.ShrinkWrap:
                                ShrinkWrapCommand shrinkWrapCommand = new ShrinkWrapCommand(GetBrushSamplesWithSpeed(), totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically);
                                /**
                                * Cache the final multipliers so they aren't run potentially tens of thousands of times.
                                * These are of course confusing and require some time to understand to an outside developer, but it's required
                                * for the best performance. Also it's not really possible to make the math look that pretty/simple anyway.
                                */
                                shrinkWrapCommand.finalMultiplierX = (1f / (totalHeightmapSamplesHorizontally - 1)) * numberOfTerrainsHorizontally * terrainSize.x;
                                shrinkWrapCommand.finalMultiplierY = (1f / (totalHeightmapSamplesVertically - 1)) * numberOfTerrainsVertically * terrainSize.z;
                                shrinkWrapCommand.shrinkWrapRaycastOffset = settings.shrinkWrapToolRaycastOffset;
                                shrinkWrapCommand.terrainHeightCoefficient = 1f / terrainSize.y;
                                shrinkWrapCommand.firstTerrainPosition = firstTerrainTransform.position;
                                currentCommand = shrinkWrapCommand;
                                TemporarilyIgnoreRaycastsOnTerrains();
                                break;
                            case Tool.PaintTexture:
                                currentCommand = new TexturePaintCommand(GetBrushSamplesWithSpeed());
                                break;
                        }
                    } else {
                        /**
                        * Only allow the various Behaviours to be active when control isn't pressed to make these behaviours 
                        * not occur while using interactive tools
                        */
                        if(currentEvent.control == false) {
                            float spacing = settings.modeSettings[CurrentTool].brushSize * randomSpacing;

                            // If brush spacing is enabled, do not update the current command until the cursor has exceeded the required distance
                            if(settings.modeSettings[CurrentTool].useBrushSpacing && mouseSpacingDistance < spacing) {
                                lastWorldspaceMousePosition = mouseWorldspacePosition;
                                return;
                            } else {
                                UpdateRandomSpacing();
                                mouseSpacingDistance = 0f;
                            }

                            if(settings.modeSettings[CurrentTool].useRandomRotation && (CurrentBrush is FalloffBrush && settings.modeSettings[CurrentTool].brushRoundness == 1f) == false) {
                                RotateTemporaryBrushSamples();
                                currentCommand.brushSamples = temporarySamples;
                            }
                        }
                        
                        UpdateDirtyBrushSamples();
                        
                        /**
                        * Execute the current event
                        */
                        currentCommand.Execute(currentEvent, commandArea);
                    }
                    
                    brushProjectorGameObject.SetActive(true);
                    
                    float[,,] newAlphamaps;
                    float[,] newHeights;
                    // Update each terrainInfo's updated terrain region
                    foreach(TerrainInformation terrainInfo in terrainInformations) {
                        if(terrainInfo.commandArea == null || terrainInfo.hasChangedSinceLastSetHeights == false) continue;

                        terrainInfo.hasChangedSinceLastSetHeights = false;
                        
                        if(currentCommand is TexturePaintCommand) {
                            newAlphamaps = new float[terrainInfo.commandArea.heightAfterClipping, terrainInfo.commandArea.widthAfterClipping, firstTerrainData.alphamapLayers];
                            for(int l = 0; l < firstTerrainData.alphamapLayers; l++) {
                                for(int x = 0; x < terrainInfo.commandArea.widthAfterClipping; x++) {
                                    for(int y = 0; y < terrainInfo.commandArea.heightAfterClipping; y++) {
                                        newAlphamaps[y, x, l] = allTextureSamples[terrainInfo.toolCentricYOffset + y + terrainInfo.commandArea.bottomOffset, 
                                            terrainInfo.toolCentricXOffset + x + terrainInfo.commandArea.leftOffset, l];
                                    }
                                }
                            }

                            //terrainInfo.terrain.editorRenderFlags = TerrainRenderFlags.heightmap;

                            terrainInfo.terrainData.SetAlphamaps(terrainInfo.commandArea.leftOffset, terrainInfo.commandArea.bottomOffset, newAlphamaps);
                            terrainDataSetBasemapDirtyMethodInfo.Invoke(terrainInfo.terrainData, new object[] { false });
                        } else {
                            newHeights = new float[terrainInfo.commandArea.heightAfterClipping, terrainInfo.commandArea.widthAfterClipping];
                            for(int x = 0; x < terrainInfo.commandArea.widthAfterClipping; x++) {
                                for(int y = 0; y < terrainInfo.commandArea.heightAfterClipping; y++) {
                                    newHeights[y, x] = allTerrainHeights[terrainInfo.toolCentricYOffset + y + terrainInfo.commandArea.bottomOffset, terrainInfo.toolCentricXOffset + x + terrainInfo.commandArea.leftOffset];
                                }
                            }

                            terrainInfo.terrain.editorRenderFlags = TerrainRenderFlags.heightmap;

                            if(settings.alwaysUpdateTerrainLODs) {
                                terrainInfo.terrainData.SetHeights(terrainInfo.commandArea.leftOffset, terrainInfo.commandArea.bottomOffset, newHeights);
                            } else {
                                terrainInfo.terrainData.SetHeightsDelayLOD(terrainInfo.commandArea.leftOffset, terrainInfo.commandArea.bottomOffset, newHeights);
                            }
                        }
                    }
                }

                lastWorldspaceMousePosition = mouseWorldspacePosition;

                // While the mouse is down, always repaint
                SceneView.RepaintAll();
            }
        }

        private bool IsCurrentModeSculptive() {
            return CurrentTool != Tool.None && CurrentTool < firstNonMouseTool;
        }

        private static Vector2 preferencesItemScrollPosition;
        [PreferenceItem("Terrain Former")]
        private static void DrawPreferences() {
            if(settings == null) {
                InitializeSettings();
            }

            if(settings == null) {
                EditorGUILayout.HelpBox("There was a problem in initializing Terrain Former's settings.", MessageType.Warning);
                return;
            }

            EditorGUIUtility.labelWidth = 185f;

            preferencesItemScrollPosition = EditorGUILayout.BeginScrollView(preferencesItemScrollPosition);
            GUILayout.Label("General", EditorStyles.boldLabel);

            // Raycast Onto Plane
            settings.raycastOntoFlatPlane = GUIUtilities.ToolbarWithLabel(raycastModeLabelContent, settings.raycastOntoFlatPlane == true ? 0 : 1, raycastModes) == 0;
                        
            // Show Sculpting Grid Plane
            EditorGUI.BeginChangeCheck();
            settings.showSculptingGridPlane = EditorGUILayout.Toggle(showSculptingGridPlaneContent, settings.showSculptingGridPlane);
            if(EditorGUI.EndChangeCheck()) {
                SceneView.RepaintAll();
            }

            EditorGUIUtility.fieldWidth += 5f;
            settings.brushColour.Value = EditorGUILayout.ColorField("Brush Colour", settings.brushColour);
            EditorGUIUtility.fieldWidth -= 5f;
            
            settings.alwaysUpdateTerrainLODs = GUIUtilities.ToolbarWithLabel(alwaysUpdateTerrainLODsContent, settings.alwaysUpdateTerrainLODs ? 0 : 1, new string[] { "Always", "Mouse Up"}) == 0;

            bool newInvertBrushTexturesGlobally = EditorGUILayout.Toggle("Invert Brush Textures Globally", settings.invertBrushTexturesGlobally);
            if(newInvertBrushTexturesGlobally != settings.invertBrushTexturesGlobally) {
                settings.invertBrushTexturesGlobally = newInvertBrushTexturesGlobally;
                if(Instance != null) {
                    Instance.UpdateAllNecessaryPreviewTextures();
                    Instance.UpdateBrushProjectorTextureAndSamples();
                    Instance.Repaint();
                }
            }

            GUILayout.Label("User Interface", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            bool newAlwaysShowBrushSelection = EditorGUILayout.Toggle(alwaysShowBrushSelectionContent, settings.AlwaysShowBrushSelection);
            if(newAlwaysShowBrushSelection != settings.AlwaysShowBrushSelection) {
                settings.AlwaysShowBrushSelection = newAlwaysShowBrushSelection;
                if(Instance != null) Instance.UpdateAllNecessaryPreviewTextures();
            }

            settings.brushSelectionDisplayType = (BrushSelectionDisplayType)EditorGUILayout.Popup("Brush Selection Display Type",
                (int)settings.brushSelectionDisplayType, brushSelectionDisplayTypeLabels);

            Rect previewSizeRect = EditorGUILayout.GetControlRect();
            Rect previewSizePopupRect = EditorGUI.PrefixLabel(previewSizeRect, new GUIContent("Brush Preview Size"));
            previewSizePopupRect.xMax -= 2;
            int newBrushPreviewSize = EditorGUI.IntPopup(previewSizePopupRect, settings.brushPreviewSize, previewSizesContent, previewSizeValues);
            if(newBrushPreviewSize != settings.brushPreviewSize) {
                settings.brushPreviewSize = newBrushPreviewSize;
                if(Instance != null) Instance.UpdateAllNecessaryPreviewTextures();
            }
            if(EditorGUI.EndChangeCheck()) {
                if(Instance != null) Instance.Repaint();
            }

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            settings.showSceneViewInformation = EditorGUILayout.BeginToggleGroup("Show Scene View Information", settings.showSceneViewInformation);
            EditorGUI.indentLevel = 1;
            GUI.enabled = settings.showSceneViewInformation;
            settings.displaySceneViewCurrentTool = EditorGUILayout.Toggle("Display Current Tool", settings.displaySceneViewCurrentTool);
            settings.displaySceneViewCurrentHeight = EditorGUILayout.Toggle("Display Current Height", settings.displaySceneViewCurrentHeight);
            settings.displaySceneViewSculptOntoMode = EditorGUILayout.Toggle("Display Sculpt Onto", settings.displaySceneViewSculptOntoMode);
            EditorGUILayout.EndToggleGroup();
            EditorGUI.indentLevel = 0;
            GUI.enabled = true;
            if(EditorGUI.EndChangeCheck()) {
                SceneView.RepaintAll();
            }
            
            GUILayout.Label("Shortcuts", EditorStyles.boldLabel);
            foreach(Shortcut shortcut in Shortcut.Shortcuts.Values) {
                shortcut.DoShortcutField();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // If all the settings are at their default value, disable the "Restore Defaults"
            bool shortcutsNotDefault = false;
            foreach(Shortcut shortcut in Shortcut.Shortcuts.Values) {
                if(shortcut.Binding != shortcut.defaultBinding) {
                    shortcutsNotDefault = true;
                    break;
                }
            }

            if(settings.AreSettingsDefault() && shortcutsNotDefault == false) {
                GUI.enabled = false;
            }
            if(GUILayout.Button("Restore Defaults", GUILayout.Width(120f), GUILayout.Height(20))) {
                if(EditorUtility.DisplayDialog("Restore Defaults", "Are you sure you want to restore all settings to their defaults?", "Restore Defaults", "Cancel")) {
                    settings.RestoreDefaultSettings();

                    // Reset shortcuts to defaults
                    foreach(Shortcut shortcut in Shortcut.Shortcuts.Values) {
                        shortcut.waitingForInput = false;
                        shortcut.Binding = shortcut.defaultBinding;
                    }

                    if(Instance != null) Instance.Repaint();
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        
        private CommandArea CalculateCommandCoordinatesForTerrain(TerrainInformation terrainInformation, Vector3 mousePosition) {
            // Note the decrement of one at the end of both cursor calculations, this is because it's 0 based, not 1 based
            int cursorLeft = Mathf.RoundToInt(((mousePosition.x - terrainInformation.transform.position.x) / terrainSize.x) * (currentToolsResolution - 1));
            int cursorBottom = Mathf.RoundToInt(((mousePosition.z - terrainInformation.transform.position.z) / terrainSize.z) * (currentToolsResolution - 1f));
            
            // The bottom-left segments of where the brush samples will start.
            int leftOffset = Mathf.Max(cursorLeft - Mathf.RoundToInt(halfBrushSizeInSamples), 0);
            int bottomOffset = Mathf.Max(cursorBottom - Mathf.RoundToInt(halfBrushSizeInSamples), 0);

            // Check if there aren't any segments that will even be sculpted/painted
            if(leftOffset >= currentToolsResolution || bottomOffset >= currentToolsResolution || cursorLeft + halfBrushSizeInSamples < 0 || cursorBottom + halfBrushSizeInSamples < 0) {
                return null;
            }

            /** 
            * Create a paint patch used for offsetting the terrain samples.
            * Clipped left contains how many segments are being clipped to the left side of the terrain. The value is 0 if there 
            * are no segments being clipped. This same pattern applies to clippedBottom, clippedWidth, and clippedHeight respectively.
            */

            int clippedSamplesLeft = 0;
            if(cursorLeft - halfBrushSizeInSamples < 0) {
                clippedSamplesLeft = Mathf.CeilToInt(Mathf.Abs(cursorLeft - halfBrushSizeInSamples));
            }

            int clippedSamplesBottom = 0;
            if(cursorBottom - halfBrushSizeInSamples < 0) {
                clippedSamplesBottom = Mathf.CeilToInt(Mathf.Abs(cursorBottom - halfBrushSizeInSamples));
            }

            int widthAfterClipping = brushSizeInSamples - clippedSamplesLeft;
            if(leftOffset + brushSizeInSamples > currentToolsResolution) {
                widthAfterClipping = currentToolsResolution - leftOffset - clippedSamplesLeft;
            }

            int heightAfterClipping = brushSizeInSamples - clippedSamplesBottom;
            if(bottomOffset + brushSizeInSamples > currentToolsResolution) {
                heightAfterClipping = currentToolsResolution - bottomOffset - clippedSamplesBottom;
            }

            return new CommandArea(leftOffset, bottomOffset, clippedSamplesLeft, clippedSamplesBottom, widthAfterClipping, heightAfterClipping);
        }

        /**
        * Command Coordinates for Terrain Grid returns coordinates taking into account the entire terrain grid, and not taking into account per-terrain coordinates
        * which vary due to the fact the terrain grid has 1 redundant sample per axis.
        */
        private CommandArea CalculateCommandCoordinatesForTerrainGrid(Vector3 mousePosition) {
            float terrainGridHorizontalSize = numberOfTerrainsHorizontally * terrainSize.x;
            float terrainGridVerticalSize = numberOfTerrainsVertically * terrainSize.z;

            Vector2 mousePositionVersusBottomLeftPosition = new Vector2(mousePosition.x - firstTerrainTransform.position.x,
                mousePosition.z - firstTerrainTransform.position.z);
            
            // Note the decrement of one at the end of both cursor calculations, this is because it's 0 based, not 1 based
            int cursorLeft = Mathf.RoundToInt((mousePositionVersusBottomLeftPosition.x / terrainGridHorizontalSize) * (totalToolSamplesHorizontally - 1));
            int cursorBottom = Mathf.RoundToInt((mousePositionVersusBottomLeftPosition.y / terrainGridVerticalSize) * (totalToolSamplesVertically - 1));
            
            int leftOffset = Mathf.Max(cursorLeft - Mathf.RoundToInt(halfBrushSizeInSamples), 0);
            int bottomOffset = Mathf.Max(cursorBottom - Mathf.RoundToInt(halfBrushSizeInSamples), 0);

            // Check if there aren't any segments that will even be sculpted/painted
            if(leftOffset >= totalToolSamplesHorizontally || bottomOffset >= totalToolSamplesVertically || cursorLeft + halfBrushSizeInSamples < 0 || cursorBottom + halfBrushSizeInSamples < 0) {
                return null;
            }

            /** 
            * Create a paint patch used for offsetting the terrain samples.
            * Clipped left contains how many segments are being clipped to the left side of the terrain. The value is 0 if there 
            * are no segments being clipped. This same pattern applies to clippedBottom, clippedWidth, and clippedHeight respectively.
            */

            int clippedLeft = 0;
            if(cursorLeft - halfBrushSizeInSamples < 0) {
                clippedLeft = Mathf.CeilToInt(Mathf.Abs(cursorLeft - halfBrushSizeInSamples));
            }
            
            int clippedBottom = 0;
            if(cursorBottom - halfBrushSizeInSamples < 0) {
                clippedBottom = Mathf.CeilToInt(Mathf.Abs(cursorBottom - halfBrushSizeInSamples));
            }

            int widthAfterClipping = brushSizeInSamples - clippedLeft;
            if(leftOffset + brushSizeInSamples > totalToolSamplesHorizontally) {
                widthAfterClipping = totalToolSamplesHorizontally - leftOffset - clippedLeft;
            }

            int heightAfterClipping = brushSizeInSamples - clippedBottom;
            if(bottomOffset + brushSizeInSamples > totalToolSamplesVertically) {
                heightAfterClipping = totalToolSamplesVertically - bottomOffset - clippedBottom;
            }

            return new CommandArea(leftOffset, bottomOffset, clippedLeft, clippedBottom, widthAfterClipping, heightAfterClipping);
        }
        
        private void UpdateRandomSpacing() {
            randomSpacing = UnityEngine.Random.Range(settings.modeSettings[CurrentTool].minBrushSpacing, settings.modeSettings[CurrentTool].maxBrushSpacing);
        }

        private void RotateTemporaryBrushSamples() {
            cachedTerrainBrush = brushCollection.brushes[settings.modeSettings[CurrentTool].selectedBrushId];

            if(temporarySamples == null || temporarySamples.GetLength(0) != brushSizeInSamples) {
                temporarySamples = new float[brushSizeInSamples, brushSizeInSamples];
            }

            Vector2 midPoint = new Vector2(brushSizeInSamples * 0.5f, brushSizeInSamples * 0.5f);
            float angle = settings.modeSettings[CurrentTool].brushAngle + UnityEngine.Random.Range(settings.modeSettings[CurrentTool].minRandomRotation, settings.modeSettings[CurrentTool].maxRandomRotation);
            float sineOfAngle = Mathf.Sin(angle * Mathf.Deg2Rad);
            float cosineOfAngle = Mathf.Cos(angle * Mathf.Deg2Rad);
            Vector2 newPoint;

            for(int x = 0; x < brushSizeInSamples; x++) {
                for(int y = 0; y < brushSizeInSamples; y++) {
                    newPoint = Utilities.RotatePointAroundPoint(new Vector2(x, y), midPoint, angle, sineOfAngle, cosineOfAngle);
                    temporarySamples[x, y] = GetInteropolatedBrushSample(newPoint.x, newPoint.y) * settings.modeSettings[CurrentTool].brushSpeed;
                }
            }
        }

        private float GetInteropolatedBrushSample(float x, float y) {
            int flooredX = Mathf.FloorToInt(x);
            int flooredY = Mathf.FloorToInt(y);
            int flooredXPlus1 = flooredX + 1;
            int flooredYPlus1 = flooredY + 1;

            if(flooredX < 0 || flooredX >= brushSizeInSamples || flooredY < 0 || flooredY >= brushSizeInSamples) return 0f;

            float topLeftSample = cachedTerrainBrush.samples[flooredX, flooredY];
            float topRightSample = 0f;
            float bottomLeftSample = 0f;
            float bottomRightSample = 0f;

            if(flooredXPlus1 < brushSizeInSamples) {
                topRightSample = cachedTerrainBrush.samples[flooredXPlus1, flooredY];
            }

            if(flooredYPlus1 < brushSizeInSamples) {
                bottomLeftSample = cachedTerrainBrush.samples[flooredX, flooredYPlus1];

                if(flooredXPlus1 < brushSizeInSamples) {
                    bottomRightSample = cachedTerrainBrush.samples[flooredXPlus1, flooredYPlus1];
                }
            }

            return Mathf.Lerp(Mathf.Lerp(topLeftSample, topRightSample, x % 1f), Mathf.Lerp(bottomLeftSample, bottomRightSample, x % 1f), y % 1f);
        }

        private void UpdateDirtyBrushSamples() {
            if(samplesDirty == SamplesDirty.None || CurrentBrush == null) return;
            
            // Update only the brush samples, and don't even update the projector texture
            if((samplesDirty & SamplesDirty.BrushSamples) == SamplesDirty.BrushSamples) {
                CurrentBrush.UpdateSamplesWithSpeed(brushSizeInSamples);
            }
            if((samplesDirty & SamplesDirty.ProjectorTexture) == SamplesDirty.ProjectorTexture) {
                UpdateBrushProjectorTextureAndSamples();
            }
            if((samplesDirty & SamplesDirty.InspectorTexture) == SamplesDirty.InspectorTexture) {
                UpdateBrushInspectorTexture();
            }
            
            // Since the underlying burhshProjector pixels have been rotated, set the temporary rotation to zero.
            brushProjector.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            topPlaneGameObject.transform.eulerAngles = brushProjector.transform.eulerAngles;

            samplesDirty = SamplesDirty.None;
        }
        
        private void CheckKeyboardShortcuts(Event currentEvent) {
            if(GUIUtility.hotControl != 0) return;
            if(activeInspector != 0 && activeInspector != GetInstanceID()) return;
            if(currentEvent.type != EventType.KeyDown) return;

            // Only check for shortcuts when no terrain command is active
            if(currentCommand != null) return;

            /**
            * Check to make sure there is no textField focused. This will ensure that shortcut strokes will not override
            * typing in text fields. Through testing however, all textboxes seem to mark the event as Used. This is simply
            * here as a precaution.
            */
            if((bool)guiUtilityTextFieldInput.GetValue(null, null)) return;

            Shortcut.wasExecuted = false;

            // Z - Set tool to Raise/Lower
            if(Shortcut.Shortcuts["Select Raise/Lower Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.RaiseOrLower;
            }
            // X - Set tool to Smooth
            else if(Shortcut.Shortcuts["Select Smooth Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.Smooth;
            }
            // C - Set tool to Set Height
            else if(Shortcut.Shortcuts["Select Set Height Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.SetHeight;
            }
            // V - Set tool to Flatten
            else if(Shortcut.Shortcuts["Select Flatten Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.Flatten;
            }
            // B - Set tool to Paint Texture
            else if(Shortcut.Shortcuts["Select Paint Texture Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.PaintTexture;
            }
            // N - Set tool to Generate
            else if(Shortcut.Shortcuts["Select Shrinkwrap Tool"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.Generate;
            }
            // M - Set tool to Settings
            else if(Shortcut.Shortcuts["Select Settings Tab"].WasExecuted(currentEvent)) {
                CurrentTool = Tool.Settings;
            }

            // Tool centric shortcuts
            if(CurrentTool == Tool.None || CurrentTool >= firstNonMouseTool) {
                if(Shortcut.wasExecuted) {
                    Repaint();
                    currentEvent.Use();
                }
                return;
            }

            // Left Bracket - decrease brush size
            if(Shortcut.Shortcuts["Decrease Brush Size"].WasExecuted(currentEvent)) {
                settings.modeSettings[CurrentTool].brushSize = Mathf.Clamp(settings.modeSettings[CurrentTool].brushSize - GetBrushSizeIncrement(settings.modeSettings[CurrentTool].brushSize), MinBrushSize, MaxBrushSize);
                settings.modeSettings[CurrentTool].brushSize = Mathf.Round(settings.modeSettings[CurrentTool].brushSize / 0.1f) * 0.1f;
                BrushSizeChanged();
            }
            // Right Bracket - increase brush size
            else if(Shortcut.Shortcuts["Increase Brush Size"].WasExecuted(currentEvent)) {
                settings.modeSettings[CurrentTool].brushSize = Mathf.Clamp(settings.modeSettings[CurrentTool].brushSize + GetBrushSizeIncrement(settings.modeSettings[CurrentTool].brushSize), MinBrushSize, MaxBrushSize);
                settings.modeSettings[CurrentTool].brushSize = Mathf.Round(settings.modeSettings[CurrentTool].brushSize / 0.1f) * 0.1f;
                BrushSizeChanged();
            }
            // Minus - decrease brush speed
            else if(Shortcut.Shortcuts["Decrease Brush Speed"].WasExecuted(currentEvent)) {
                settings.modeSettings[CurrentTool].brushSpeed = Mathf.Clamp(Mathf.Round((settings.modeSettings[CurrentTool].brushSpeed - GetBrushSpeedIncrement(settings.modeSettings[CurrentTool].brushSpeed)) / 0.01f) * 0.01f, 
                    minBrushSpeed, maxBrushSpeed);
                BrushSpeedChanged();
            }
            // Equals - increase brush speed
            else if(Shortcut.Shortcuts["Increase Brush Speed"].WasExecuted(currentEvent)) {
                settings.modeSettings[CurrentTool].brushSpeed = Mathf.Clamp(Mathf.Round((settings.modeSettings[CurrentTool].brushSpeed + GetBrushSpeedIncrement(settings.modeSettings[CurrentTool].brushSpeed)) / 0.01f) * 0.01f, 
                    minBrushSpeed, maxBrushSpeed);
                BrushSpeedChanged();
            }
            // P - next brush
            else if(Shortcut.Shortcuts["Next Brush"].WasExecuted(currentEvent)) {
                IncrementSelectedBrush(1);
            }
            // O - previous brush
            else if(Shortcut.Shortcuts["Previous Brush"].WasExecuted(currentEvent)) {
                IncrementSelectedBrush(-1);
            }

            // Brush angle only applies to custom brushes
            if(CurrentBrush != null && CurrentBrush is ImageBrush) {
                // 0 - reset brush angle
                if(Shortcut.Shortcuts["Reset Brush Rotation"].WasExecuted(currentEvent)) {
                    float angleDeltaChange = settings.modeSettings[CurrentTool].brushAngle;
                    settings.modeSettings[CurrentTool].brushAngle = 0f;
                    if(angleDeltaChange != 0f) BrushAngleDeltaChanged(angleDeltaChange);
                }
                // ; - rotate brush anticlockwise
                else if(Shortcut.Shortcuts["Rotate Brush Anticlockwise"].WasExecuted(currentEvent)) {
                    float newBrushAngle = Mathf.Clamp(settings.modeSettings[CurrentTool].brushAngle + 2f, -180f, 180f);
                    if(newBrushAngle != settings.modeSettings[CurrentTool].brushAngle) {
                        float delta = settings.modeSettings[CurrentTool].brushAngle - newBrushAngle;
                        settings.modeSettings[CurrentTool].brushAngle = newBrushAngle;
                        BrushAngleDeltaChanged(delta);
                    }
                }
                // ' - rotate brush right
                else if(Shortcut.Shortcuts["Rotate Brush Clockwise"].WasExecuted(currentEvent)) {
                    float newBrushAngle = Mathf.Clamp(settings.modeSettings[CurrentTool].brushAngle - 2f, -180f, 180f);
                    if(newBrushAngle != settings.modeSettings[CurrentTool].brushAngle) {
                        float delta = settings.modeSettings[CurrentTool].brushAngle - newBrushAngle;
                        settings.modeSettings[CurrentTool].brushAngle = newBrushAngle;
                        BrushAngleDeltaChanged(delta);
                    }
                }
            }

            // I - Toggle projection mode
            if(Shortcut.Shortcuts["Toggle Sculpt Onto Mode"].WasExecuted(currentEvent)) {
                settings.raycastOntoFlatPlane = !settings.raycastOntoFlatPlane;
            }

            // Shift+G - Flatten Terrain Shortcut
            else if(Shortcut.Shortcuts["Flatten Terrain"].WasExecuted(currentEvent)) {
                FlattenTerrain(0f);
            }

            if(Shortcut.wasExecuted) {
                Repaint();
                currentEvent.Use();
            }
        }

        private float GetBrushSizeIncrement(float currentBrushSize) {
            float currentSizeCoefficient = currentBrushSize / terrainSize.x;

            if(currentSizeCoefficient > 0.5f) {
                return terrainSize.x * 0.025f;
            } else if(currentSizeCoefficient > 0.25f) {
                return terrainSize.x * 0.01f;
            } else if(currentSizeCoefficient > 0.1f) {
                return terrainSize.x * 0.005f;
            } else if(currentSizeCoefficient > 0.07f) {
                return terrainSize.x * 0.002f;
            } else {
                return terrainSize.x * 0.001f;
            }
        }

        private float GetBrushSpeedIncrement(float currentBrushSpeed) {
            if(currentBrushSpeed > 1f) {
                return 0.1f;
            } else if(currentBrushSpeed > 0.35f) {
                return 0.05f;
            } else {
                return 0.02f;
            }
        }
        
        private void IncrementSelectedBrush(int incrementFactor) {
            if(brushCollection.brushes.Count == 0) return;

            string currentSelectedBrushIndex = settings.modeSettings[CurrentTool].selectedBrushId;

            // Return if the increment/decrement will be out of bounds
            if((incrementFactor == -1 && currentSelectedBrushIndex == brushCollection.brushes.First().Value.id) ||
                (incrementFactor == 1 && currentSelectedBrushIndex == brushCollection.brushes.Last().Value.id)) return;
            
            // Continue to increment the current index until the current brush is found by Key (a unique string)
            int currentBrushIndex = 0;
            foreach(KeyValuePair<string, TerrainBrush> brush in brushCollection.brushes) {
                if(brush.Key != currentSelectedBrushIndex) {
                    currentBrushIndex++;
                    continue;
                }
                break;
            }

            settings.modeSettings[CurrentTool].selectedBrushId = brushCollection.brushes.ElementAt(currentBrushIndex + incrementFactor).Value.id;
            SelectedBrushChanged();
        }

        private void BrushFalloffChanged() {
            ClampAnimationCurve(BrushFalloff);

            samplesDirty |= SamplesDirty.ProjectorTexture;

            if(settings.AlwaysShowBrushSelection) {
                brushCollection.UpdatePreviewTextures();
            } else {
                UpdateBrushInspectorTexture();
            }
        }

        private void ToggleSelectingBrush() {
            isSelectingBrush = !isSelectingBrush;

            // Update the brush previews if the user is now selecting brushes
            if(isSelectingBrush) {
                brushCollection.UpdatePreviewTextures();
            }
        }

        private void CurrentToolChanged(Tool previousValue) {
            // Sometimes it's possible Terrain Former thinks the mouse is still pressed down as not every event is detected by Terrain Former
            mouseIsDown = false;

            bool unityTerrainInspectorWasActive = false;
            // If the built-in Unity tools were active, make them inactive by setting their tool to None (-1)
            foreach(object terrainInspector in unityTerrainInspectors) {
                if((int)unityTerrainSelectedTool.GetValue(terrainInspector, null) != -1) {
                    unityTerrainSelectedTool.SetValue(terrainInspector, -1, null);

                    unityTerrainInspectorWasActive = true;
                }
            }

            if(unityTerrainInspectorWasActive && CurrentTool != Tool.None) {
                // Update the heights of the terrain editor in case they were edited in the Unity terrain editor
                UpdateAllHeightsFromSourceAssets();
            }
            
            /**
            * All inspector windows must be updated to reflect across multiple inspectors that there is only one Terrain Former instance active at
            * once, and that also stops those Terrain Former instance(s) that are no longer active to not call OnInspectorGUI.
            */
            inspectorWindowRepaintAllInspectors.Invoke(null, null);

            activeInspector = GetInstanceID();
            Instance = this;

            if(settings == null) return;

            if(CurrentTool == Tool.None) {
                Debug.Log("SetActive");
                if(brushProjectorGameObject.activeSelf) brushProjectorGameObject.SetActive(false);
                return;
            }

            if(previousValue == Tool.None) Initialize(true);
            if(CurrentTool >= firstNonMouseTool) return;
            
            splatPrototypes = firstTerrainData.splatPrototypes;
            
            settings.modeSettings[CurrentTool] = settings.modeSettings[CurrentTool];
            
            switch(CurrentTool) {
                case Tool.PaintTexture:
                    currentToolsResolution = firstTerrainData.alphamapResolution;
                    totalToolSamplesHorizontally = currentToolsResolution * numberOfTerrainsHorizontally;
                    totalToolSamplesVertically = currentToolsResolution * numberOfTerrainsVertically;
                    break;
                default:
                    currentToolsResolution = heightmapResolution;
                    totalToolSamplesHorizontally = currentToolsResolution * numberOfTerrainsHorizontally - (numberOfTerrainsHorizontally - 1);
                    totalToolSamplesVertically = currentToolsResolution * numberOfTerrainsVertically - (numberOfTerrainsVertically - 1);
                    break;
            }
            
            foreach(TerrainInformation terrainInfo in terrainInformations) {
                terrainInfo.toolCentricXOffset = terrainInfo.gridXCoordinate * currentToolsResolution;
                terrainInfo.toolCentricYOffset = terrainInfo.gridYCoordinate * currentToolsResolution;
                if(CurrentTool != Tool.PaintTexture) {
                    terrainInfo.toolCentricXOffset -= terrainInfo.gridXCoordinate;
                    terrainInfo.toolCentricYOffset -= terrainInfo.gridYCoordinate;
                }
            }
            
            if(CurrentTool == Tool.PaintTexture) {
                UpdateAllAlphamapSamplesFromSourceAssets();
            } else {
                allTextureSamples = null;
            }
            
            brushProjector.orthographicSize = settings.modeSettings[CurrentTool].brushSize * 0.5f;
            topPlaneGameObject.transform.localScale = new Vector3(settings.modeSettings[CurrentTool].brushSize, settings.modeSettings[CurrentTool].brushSize, settings.modeSettings[CurrentTool].brushSize);
            BrushSizeInSamples = GetSegmentsFromUnits(settings.modeSettings[CurrentTool].brushSize);
            
            UpdateAllNecessaryPreviewTextures();
            
            if(settings.brushSelectionDisplayType == BrushSelectionDisplayType.Tabbed) {
                SelectedBrushTabChanged();
            } else {
                terrainBrushesOfCurrentType = brushCollection.brushes.Values.ToList();
            }
            
            UpdateBrushProjectorTextureAndSamples();
        }
        
        private void SelectedBrushChanged() {
            UpdateBrushTextures();
        }
        
        private void InvertBrushTextureChanged() {
            UpdateBrushTextures();

            if(settings.AlwaysShowBrushSelection) brushCollection.UpdatePreviewTextures();
        }

        private void BrushSpeedChanged() {
            samplesDirty |= SamplesDirty.BrushSamples;
        }

        private void BrushColourChanged() {
            brushProjector.material.color = settings.brushColour;
            topPlaneMaterial.color = settings.brushColour.Value * 0.9f;
        }

        private void BrushSizeChanged() {
            if(CurrentTool == Tool.None || CurrentTool >= firstNonMouseTool) return;

            BrushSizeInSamples = GetSegmentsFromUnits(settings.modeSettings[CurrentTool].brushSize);

            /**
            * HACK: Another spot where objects are seemingly randomly destroyed. The top plane and projector are (seemingly) destroyed between
            * switching from one terrain with Terrain Former to another.
            */
            if(topPlaneGameObject == null || brushProjector == null) {
                CreateProjector();
            }

            topPlaneGameObject.transform.localScale = new Vector3(settings.modeSettings[CurrentTool].brushSize, settings.modeSettings[CurrentTool].brushSize, settings.modeSettings[CurrentTool].brushSize);
            brushProjector.orthographicSize = settings.modeSettings[CurrentTool].brushSize * 0.5f;

            samplesDirty |= SamplesDirty.ProjectorTexture;
        }

        private void BrushRoundnessChanged() {
            samplesDirty |= SamplesDirty.ProjectorTexture;

            UpdateAllNecessaryPreviewTextures();
        }

        private void BrushAngleDeltaChanged(float delta) {
            UpdateAllNecessaryPreviewTextures();

            brushProjector.transform.eulerAngles = new Vector3(90f, brushProjector.transform.eulerAngles.y + delta, 0f);
            topPlaneGameObject.transform.eulerAngles = brushProjector.transform.eulerAngles;

            samplesDirty = SamplesDirty.BrushSamples | SamplesDirty.ProjectorTexture;
        }

        private void AlwaysShowBrushSelectionValueChanged() {
            /**
            * If the brush selection should always be shown, make sure isSelectingBrush is set to false because
            * when changing to AlwaysShowBrushSelection while the brush selection was active, it will return back to
            * selecting a brush.
            */
            if(settings.AlwaysShowBrushSelection == true) {
                isSelectingBrush = false;
            }
        }
        
        private void UpdatePreviewTexturesAndBrushSamples() {
            UpdateAllNecessaryPreviewTextures();
            UpdateBrushProjectorTextureAndSamples();
        }

        private void UpdateAllNecessaryPreviewTextures() {
            if(CurrentTool == Tool.None || CurrentTool >= firstNonMouseTool) return;

            if(settings.AlwaysShowBrushSelection || isSelectingBrush) {
                brushCollection.UpdatePreviewTextures();
            } else {
                UpdateBrushInspectorTexture();
            }
        }
        
        private void SelectedBrushTabChanged() {
            if(CurrentTool >= firstNonMouseTool) return;

            UpdateCurrentBrushesOfType();
        }

        private void UpdateCurrentBrushesOfType() {
            Type typeToDisplay;
            if(string.IsNullOrEmpty(settings.modeSettings[CurrentTool].selectedBrushTab)) {
                typeToDisplay = null;
            } else {
                typeToDisplay = terrainBrushTypes[settings.modeSettings[CurrentTool].selectedBrushTab];
            }

            if(typeToDisplay == null) {
                terrainBrushesOfCurrentType = brushCollection.brushes.Values.ToList();
                return;
            }

            terrainBrushesOfCurrentType.Clear();

            foreach(TerrainBrush terrainBrush in brushCollection.brushes.Values) {
                if(terrainBrush.GetType() != typeToDisplay) continue;

                terrainBrushesOfCurrentType.Add(terrainBrush);
            }
        }
        
        internal void ApplySplatPrototypes() {
            for(int i = 0; i < terrainInformations.Count; i++) {
                terrainInformations[i].terrainData.splatPrototypes = splatPrototypes;
            }
        }

        /**
        * Update the heights and alphamaps every time an Undo or Redo occurs - since we must rely on storing and managing the 
        * heights data manually for better editing performance.
        */
        private void UndoRedoPerformed() {
            if(target == null) return;

            UpdateAllHeightsFromSourceAssets();

            if(CurrentTool == Tool.PaintTexture) {
                splatPrototypes = firstTerrainData.splatPrototypes;
                UpdateAllAlphamapSamplesFromSourceAssets();
            }
        }

        private void RegisterUndoForTerrainGrid(string description, bool includeAlphamapTextures = false, List<UnityEngine.Object> secondaryObjectsToUndo = null) {
            List<UnityEngine.Object> objectsToRegister = new List<UnityEngine.Object>();
            if(secondaryObjectsToUndo != null) objectsToRegister.AddRange(secondaryObjectsToUndo);

            for(int i = 0; i < terrainInformations.Count; i++) {
                objectsToRegister.Add(terrainInformations[i].terrainData);
                                
                if(includeAlphamapTextures) {
                    objectsToRegister.AddRange(terrainInformations[i].terrainData.alphamapTextures);
                }
            }
            Undo.RegisterCompleteObjectUndo(objectsToRegister.ToArray(), description);
        }

        private void CreateGridPlane() {
            gridPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gridPlane.name = "GridPlane";
            gridPlane.transform.Rotate(90f, 0f, 0f);
            gridPlane.transform.localScale = Vector3.one * 20f;
            gridPlane.hideFlags = HideFlags.HideAndDontSave;
            gridPlane.SetActive(false);

            Shader gridShader = Shader.Find("Hidden/TerrainFormer/Grid");
            if(gridShader == null) {
                Debug.LogError("Terrain Former couldn't find its grid shader.");
                return;
            }
            
            gridPlaneMaterial = new Material(gridShader);
            gridPlaneMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(settings.mainDirectory + "Textures/Tile.psd");
            gridPlaneMaterial.mainTexture.wrapMode = TextureWrapMode.Repeat;
            gridPlaneMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            gridPlaneMaterial.hideFlags = HideFlags.HideAndDontSave;
            gridPlaneMaterial.mainTextureScale = new Vector2(8f, 8f); // Set texture scale to create 8x8 tiles
            gridPlane.GetComponent<Renderer>().sharedMaterial = gridPlaneMaterial;
        }

        private void CreateProjector() {
            /**
            * Create the brush projector
            */
            brushProjectorGameObject = new GameObject("TerrainFormerProjector");
            brushProjectorGameObject.hideFlags = HideFlags.HideAndDontSave;
            
            brushProjector = brushProjectorGameObject.AddComponent<Projector>();
            brushProjector.nearClipPlane = -1000f;
            brushProjector.farClipPlane = 1000f;
            brushProjector.orthographic = true;
            brushProjector.orthographicSize = 10f;
            brushProjector.transform.Rotate(90f, 0f, 0f);

            brushProjectorMaterial = new Material(Shader.Find("Hidden/TerrainFormer/Terrain Brush Preview"));
            brushProjectorMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            brushProjectorMaterial.hideFlags = HideFlags.HideAndDontSave;
            brushProjectorMaterial.color = settings.brushColour;
            brushProjector.material = brushProjectorMaterial;

            Texture2D outlineTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(settings.mainDirectory + "Textures/BrushOutline.png");
            outlineTexture.wrapMode = TextureWrapMode.Clamp;
            brushProjectorMaterial.SetTexture("_OutlineTex", outlineTexture);

            /**
            * Create the top plane
            */
            topPlaneGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            topPlaneGameObject.name = "Top Plane";
            topPlaneGameObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(topPlaneGameObject.GetComponent<MeshCollider>());
            topPlaneGameObject.transform.Rotate(90f, 0f, 0f);

            topPlaneMaterial = new Material(Shader.Find("Hidden/TerrainFormer/BrushPlaneTop"));
            topPlaneMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            topPlaneMaterial.hideFlags = HideFlags.HideAndDontSave;
            topPlaneMaterial.color = settings.brushColour.Value * 0.9f;
            topPlaneMaterial.SetTexture("_OutlineTex", outlineTexture);

            topPlaneGameObject.GetComponent<MeshRenderer>().sharedMaterial = topPlaneMaterial;

            SetCursorEnabled(false);
        }

        private void UpdateProjector() {
            if(brushProjector == null) return;

            if(CurrentTool == Tool.None || CurrentTool >= firstNonMouseTool) {
                SetCursorEnabled(false);
                return;
            }
            
            Vector3 position;
            if(GetMousePositionInWorldSpace(out position)) {
                // Always make sure the projector is positioned as high as necessary
                brushProjector.transform.position = new Vector3(position.x, firstTerrain.transform.position.y + firstTerrainData.size.y + 1f, position.z);
                brushProjector.farClipPlane = firstTerrainData.size.y + 2f;
                brushProjectorGameObject.SetActive(true);

                if(CurrentTool == Tool.Flatten) {
                    topPlaneGameObject.SetActive(position.y >= MinHeightDifferenceToShowTopPlane);
                    topPlaneGameObject.transform.position = new Vector3(position.x, position.y, position.z);
                } else if(CurrentTool == Tool.SetHeight) {
                    topPlaneGameObject.SetActive(settings.setHeight >= MinHeightDifferenceToShowTopPlane);
                    topPlaneGameObject.transform.position = new Vector3(position.x, firstTerrain.transform.position.y + settings.setHeight, position.z);
                } else {
                    topPlaneGameObject.SetActive(false);
                }
            } else {
                SetCursorEnabled(false);
            }
            
            HandleUtility.Repaint();
        }

        private void UpdateBrushTextures() {
            UpdateBrushInspectorTexture();
            UpdateBrushProjectorTextureAndSamples();
        }

        private void UpdateBrushProjectorTextureAndSamples() {
            lastTimeBrushSamplesWereUpdated = EditorApplication.timeSinceStartup;
            
            CurrentBrush.UpdateSamplesAndMainTexture(brushSizeInSamples);

            // HACK: Projector objects are destroyed (seemingly randomly), so recreate them if necessary
            if(brushProjectorGameObject == null || brushProjectorMaterial == null) {
                CreateProjector();
            }

            brushProjectorMaterial.mainTexture = brushProjectorTexture;
            topPlaneMaterial.mainTexture = brushProjectorTexture;

            if(currentCommand != null) {
                currentCommand.brushSamples = GetBrushSamplesWithSpeed();
            }
        }

        private void UpdateBrushInspectorTexture() {
            CurrentBrush.CreatePreviewTexture();
        }

        internal void DeleteSplatTexture(int indexToDelete) {
            RegisterUndoForTerrainGrid("Delete Splat Texture", true);
            
            int allTextureSamplesHorizontally = allTextureSamples.GetLength(0);
            int allTextureSamplesVertically = allTextureSamples.GetLength(1);
            int textureCount = allTextureSamples.GetLength(2);
            int newTextureCount = textureCount - 1;

            float[,,] oldTextureSamples = new float[allTextureSamplesVertically, allTextureSamplesHorizontally, textureCount];
            Array.Copy(allTextureSamples, oldTextureSamples, allTextureSamples.Length);
            
            // Duplicate the alphamaps array, except the part of the 3rd dimension whose index is the one to be deleted
            allTextureSamples = new float[allTextureSamplesVertically, allTextureSamplesHorizontally, newTextureCount];
            
            for(int x = 0; x < allTextureSamplesHorizontally; x++) {
                for(int y = 0; y < allTextureSamplesVertically; y++) {
                    for(int l = 0; l < indexToDelete; l++) {
                        allTextureSamples[y, x, l] = oldTextureSamples[y, x, l];
                    }
                    for(int l = indexToDelete + 1; l < textureCount; l++) {
                        allTextureSamples[y, x, l - 1] = oldTextureSamples[y, x, l];
                    }
                }
            }
            
            for(int x = 0; x < allTextureSamplesHorizontally; x++) {
                for(int y = 0; y < allTextureSamplesVertically; y++) {
                    float sum = 0f;

                    for(int l = 0; l < newTextureCount; l++) {
                        sum += allTextureSamples[y, x, l];
                    }

                    if(sum >= 0.01f) {
                        float sumCoefficient = 1f / sum;
                        for(int l = 0; l < newTextureCount; l++) {
                            allTextureSamples[y, x, l] *= sumCoefficient;
                        }
                    } else {
                        for(int l = 0; l < newTextureCount; l++) {
                            allTextureSamples[y, x, l] = l != 0 ? 0f : 1f;
                        }
                    }
                }
            }

            List<SplatPrototype> splatPrototypesList = new List<SplatPrototype>(splatPrototypes);
            splatPrototypesList.RemoveAt(indexToDelete);
            splatPrototypes = splatPrototypesList.ToArray();
            ApplySplatPrototypes();
            UpdateAllAlphamapSamplesInSourceAssets();
        }
        
#region GlobalTerrainModifications
        private void CreateLinearRamp(float maxHeight) {
            RegisterUndoForTerrainGrid("Created Ramp");
            
            float heightCoefficient = maxHeight / terrainSize.y;
            float height;
            /**
            * It might seem wasteful not not make these loops generic enough to avoid duplication, but the simplicity that brings as
            * well as the speed is far more important
            */
            if(settings.generateRampCurveInXAxis) {
                for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                    height = settings.generateRampCurve.Evaluate((float)x / totalHeightmapSamplesHorizontally) * heightCoefficient;
                    for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                        allTerrainHeights[y, x] = height;
                    }
                }
            } else {
                for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                    height = settings.generateRampCurve.Evaluate((float)y / totalHeightmapSamplesVertically) * heightCoefficient;
                    for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                        allTerrainHeights[y, x] = height;
                    }
                }
            }
            
            UpdateAllHeightsInSourceAssets();
        }
        
        private void CreateCircularRamp(float maxHeight) {
            RegisterUndoForTerrainGrid("Created Circular Ramp");
            
            float heightCoefficient = maxHeight / terrainSize.y;
            float halfTotalTerrainSize = Mathf.Min(totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically) * 0.5f;
            float distance;
            for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                    distance = CalculateDistance(x, y, halfTotalTerrainSize, halfTotalTerrainSize);
                    allTerrainHeights[y, x] = settings.generateRampCurve.Evaluate(1f - (distance / halfTotalTerrainSize)) * heightCoefficient;
                }
            }
            
            UpdateAllHeightsInSourceAssets();
        }

        private void HeightOffset(float heightmapHeightOffset) {
            RegisterUndoForTerrainGrid("Height Offset");

            for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                    allTerrainHeights[y, x] = Mathf.Clamp01(allTerrainHeights[y, x] + heightmapHeightOffset / terrainSize.y);
                }
            }

            // Create the silly array that we must send every terrain as we don't have any other choice
            float[,] newHeights = new float[heightmapResolution, heightmapResolution];
            for(int x = 0; x < heightmapResolution; x++) {
                for(int y = 0; y < heightmapResolution; y++) {
                    newHeights[y, x] = allTerrainHeights[y, x];
                }
            }

            foreach(TerrainInformation ti in terrainInformations) {
                ti.terrainData.SetHeights(0, 0, newHeights);
            }
        }

        private void ExportHeightmap(ref Texture2D tex) {
            for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                    float grey = allTerrainHeights[y, x] / terrainSize.y;
                    tex.SetPixel(y, x, new Color(grey, grey, grey));
                }
            }
            tex.Apply();
        }

        private void FlattenTerrain(float setHeight) {
            RegisterUndoForTerrainGrid("Flatten Terrain");

            for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                    allTerrainHeights[y, x] = setHeight;
                }
            }

            // Create the silly array that we must send every terrain as we don't have any other choice
            float[,] newHeights = new float[heightmapResolution, heightmapResolution];
            for(int x = 0; x < heightmapResolution; x++) {
                for(int y = 0; y < heightmapResolution; y++) {
                    newHeights[x, y] = setHeight;
                }
            }
            
            foreach(TerrainInformation ti in terrainInformations) {
                ti.terrainData.SetHeights(0, 0, newHeights);
            }
        }
        
        private void SmoothAll() {
            RegisterUndoForTerrainGrid("Smooth All");
            
            float[,] newHeights = new float[totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically];

            float heightSum;
            int neighbourCount, positionX, positionY;

            float totalOperations = settings.smoothingIterations * totalHeightmapSamplesHorizontally;
            float currentOperation = 0;
            
            for(int i = 0; i < settings.smoothingIterations; i++) {
                for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                    currentOperation++;

                    // Only update the progress bar every width segment, otherwise it will be called way too many times
                    if(EditorUtility.DisplayCancelableProgressBar("Smooth All", "Smoothing entire terrain…", currentOperation / totalOperations) == true) {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                        heightSum = 0f;
                        neighbourCount = 0;

                        for(int x2 = -settings.boxFilterSize; x2 <= settings.boxFilterSize; x2++) {
                            positionX = x + x2;
                            if(positionX < 0 || positionX >= totalHeightmapSamplesHorizontally) continue;
                            for(int y2 = -settings.boxFilterSize; y2 <= settings.boxFilterSize; y2++) {
                                positionY = y + y2;
                                if(positionY < 0 || positionY >= totalHeightmapSamplesVertically) continue;

                                heightSum += allTerrainHeights[positionY, positionX];
                                neighbourCount++;
                            }
                        }

                        newHeights[y, x] = heightSum / neighbourCount;
                    }
                }

                allTerrainHeights = newHeights;
            }
            
            EditorUtility.ClearProgressBar();
            UpdateAllHeightsInSourceAssets();
        }
        
        private void ImportHeightmap() {
            TextureImporter heightmapTextureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(heightmapTexture));
            if(heightmapTextureImporter.isReadable == false) {
                heightmapTextureImporter.isReadable = true;
                heightmapTextureImporter.SaveAndReimport();
            }

            float uPosition;
            float vPosition = 0f;
            Color bilinearSample;
            const float oneThird = 1f / 3f;
            for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
                for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
                    uPosition = (float)x / totalHeightmapSamplesHorizontally;
                    vPosition = (float)y / totalHeightmapSamplesVertically;
                    if(settings.heightmapSourceIsAlpha) {
                        allTerrainHeights[x, y] = heightmapTexture.GetPixelBilinear(uPosition, vPosition).a;
                    } else {
                        bilinearSample = heightmapTexture.GetPixelBilinear(uPosition, vPosition);
                        allTerrainHeights[x, y] = (bilinearSample.r + bilinearSample.g + bilinearSample.b) * oneThird;
                    }
                }

                if(EditorUtility.DisplayCancelableProgressBar("Terrain Former", "Applying heightmap to terrain", vPosition * 0.9f)) {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
            UpdateAllHeightsInSourceAssets();

            EditorUtility.ClearProgressBar();
        }
#endregion

        // If there have been changes to a given terrain in Terrain Former, don't reimport its heights on OnAssetsImported.
        private void OnWillSaveAssets(string[] assetPaths) {
            if(settings != null) settings.Save();

            foreach(TerrainInformation ti in terrainInformations) {
                foreach(string assetPath in assetPaths) {
                    if(ti.terrainAssetPath != assetPath || ti.hasChangedSinceLastSave) continue;

                    ti.ignoreOnAssetsImported = true;
                    ti.hasChangedSinceLastSave = false;
                }
            }
        }
        
        private void OnAssetsImported(string[] assetPaths) {
            // There's a possibility of no terrainInformations because of being no terrains on the object.
            if(terrainInformations == null) return;

            List<string> customBrushPaths = new List<string>();

            Type texture2DType = typeof(Texture2D);
            Type terrainDataType = typeof(TerrainData);

            foreach(string path in assetPaths) {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if(asset == null) continue;

                Type assetType = asset.GetType();
                if(assetType == texture2DType) {
                    /**
                    * If there are custom textures that have been update, keep a list of which onces have changed and update the brushCollection.
                    */
                    if(path.StartsWith(BrushCollection.localCustomBrushPath)) {
                        customBrushPaths.Add(path);
                    }
                } else if(assetType == terrainDataType) {
                    /**
                    * Check if the terrain has been modified externally. If this terrain's path matches this any terrain grid terrain,
                    * update the heights array.
                    */
                    foreach(TerrainInformation terrainInformation in terrainInformations) {
                        if(terrainInformation.ignoreOnAssetsImported) {
                            terrainInformation.ignoreOnAssetsImported = false;
                            continue;
                        }

                        if(terrainInformation.terrainData == null) continue;

                        float[,] temporaryHeights;
                        if(terrainInformation.terrainAssetPath == path && terrainInformation.terrainData.heightmapResolution == heightmapResolution) {
                            temporaryHeights = terrainInformation.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                            for(int x = 0; x < heightmapResolution; x++) {
                                for(int y = 0; y < heightmapResolution; y++) {
                                    allTerrainHeights[terrainInformation.heightmapYOffset + y, terrainInformation.heightmapXOffset + x] = temporaryHeights[y, x];
                                }
                            }
                        }
                    }
                }
            }

            if(customBrushPaths.Count > 0) {
                brushCollection.RefreshCustomBrushes(customBrushPaths.ToArray());
                brushCollection.UpdatePreviewTextures();
                UpdateCurrentBrushesOfType();
            }
        }
        
        // Check if the terrain asset has been moved.
        private void OnAssetsMoved(string[] sourcePaths, string[] destinationPaths) {
            for(int i = 0; i < sourcePaths.Length; i++) {
                foreach(TerrainInformation terrainInfo in terrainInformations) {
                    if(sourcePaths[i] == terrainInfo.terrainAssetPath) {
                        terrainInfo.terrainAssetPath = destinationPaths[i];
                    }
                }
            }
        }

        private void OnAssetsDeleted(string[] paths) {
            List<string> deletedCustomBrushPaths = new List<string>();

            foreach(string path in paths) {
                if(path.StartsWith(BrushCollection.localCustomBrushPath)) {
                    deletedCustomBrushPaths.Add(path);
                }
            }

            if(deletedCustomBrushPaths.Count > 0) {
                brushCollection.RemoveDeletedBrushes(deletedCustomBrushPaths.ToArray());
                brushCollection.UpdatePreviewTextures();
                UpdateCurrentBrushesOfType();
            }
        }

        private int[] previousTerrainLayers;
        internal void TemporarilyIgnoreRaycastsOnTerrains() {
            int numberOfTerrains = terrainInformations.Count;
            previousTerrainLayers = new int[numberOfTerrains];
            for(int t = 0; t < numberOfTerrains; t++) {
                previousTerrainLayers[t] = terrainInformations[t].terrain.gameObject.layer;
                // HACK: Ignore the terrain colliders by setting their layer to IgnoreRaycast temporarily.
                terrainInformations[t].terrain.gameObject.layer = Physics.IgnoreRaycastLayer;
            }
        }

        internal void ResetLayerOfTerrains() {
            // Reset all terrain layers to their previous values.
            for(int t = 0; t < terrainInformations.Count; t++) {
                terrainInformations[t].terrain.gameObject.layer = previousTerrainLayers[t];
            }
        }

#region Utlities
        // Clamp the falloff curve's values from time 0-1 and value 0-1
        private static void ClampAnimationCurve(AnimationCurve curve) {
            for(int i = 0; i < curve.keys.Length; i++) {
                Keyframe keyframe = curve.keys[i];
                curve.MoveKey(i, new Keyframe(Mathf.Clamp01(keyframe.time), Mathf.Clamp01(keyframe.value), keyframe.inTangent, keyframe.outTangent));
            }
        }

        /**
        * A modified version of the LinePlaneIntersection method from the 3D Math Functions script found on the Unify site 
        * Credit to Bit Barrel Media: http://wiki.unity3d.com/index.php?title=3d_Math_functions
        * This code has been modified to fit my needs and coding style.
        *---
        * Get the intersection between a line and a XZ facing plane. 
        * If the line and plane are not parallel, the function outputs true, otherwise false.
        */
        private bool LinePlaneIntersection(out Vector3 intersectingPoint) {
            Vector3 planePoint = new Vector3(0f, lastClickPosition.y, 0f);

            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);

            // Calculate the distance between the linePoint and the line-plane intersection point
            float dotNumerator = Vector3.Dot((planePoint - mouseRay.origin), Vector3.up);
            float dotDenominator = Vector3.Dot(mouseRay.direction, Vector3.up);

            // Check if the line and plane are not parallel
            if(dotDenominator != 0f) {
                float length = dotNumerator / dotDenominator;

                // Create a vector from the linePoint to the intersection point and set the vector length by normalizing and multiplying by the length
                Vector3 vector = mouseRay.direction * length;

                // Get the coordinates of the line-plane intersection point
                intersectingPoint = mouseRay.origin + vector;

                return true;
            } else {
                intersectingPoint = Vector3.zero;
                return false;
            }
        }
        
        // Checks if the cursor is hovering over the terrain
        private bool Raycast(out Vector3 pos, out Vector2 uv) {
            RaycastHit hitInfo;

            float closestSqrDistance = float.MaxValue;
            pos = Vector3.zero;
            uv = Vector2.zero;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            foreach(TerrainInformation terrainInformation in terrainInformations) {
                if(terrainInformation.collider.Raycast(mouseRay, out hitInfo, float.PositiveInfinity)) {
                    float sqrDistance = (mouseRay.origin - hitInfo.point).sqrMagnitude;
                    if(sqrDistance < closestSqrDistance) {
                        closestSqrDistance = sqrDistance;
                        pos = hitInfo.point;
                        uv = hitInfo.textureCoord;
                    }
                }
            }

            return closestSqrDistance != float.MaxValue;
        }

        private static float CalculateDistance(float x1, float y1, float x2, float y2) {
            float deltaX = x1 - x2;
            float deltaY = y1 - y2;

            float magnitude = deltaX * deltaX + deltaY * deltaY;

            return Mathf.Sqrt(magnitude);
        }

        internal void UpdateSetHeightAtMousePosition() {
            float height;
            if(GetTerrainHeightAtMousePosition(out height)) {
                settings.setHeight = height;
                Repaint();
            }
        }

        internal float[,] GetBrushSamplesWithSpeed() { 
            return brushCollection.brushes[settings.modeSettings[CurrentTool].selectedBrushId].samplesWithSpeed;
        }

        private bool GetTerrainHeightAtMousePosition(out float height) {
            RaycastHit hitInfo;
            foreach(TerrainInformation terrainInformation in terrainInformations) {
                if(terrainInformation.collider.Raycast(HandleUtility.GUIPointToWorldRay(mousePosition), out hitInfo, float.PositiveInfinity)) {
                    height = hitInfo.point.y - terrainInformation.transform.position.y;
                    return true;
                }
            }
            
            height = 0f;
            return false;
        }

        /**
        * Gets the mouse position in world space. This is a utlity method used to automatically get the position of 
        * the mouse depending on if it's being held down or not. Returns true if the terrain or plane was hit, 
        * returns false otherwise.
        */
        private bool GetMousePositionInWorldSpace(out Vector3 position) {
            // If the user is sampling height while in Set Height with Shift, only use a Raycast.
            if(mouseIsDown && (settings.raycastOntoFlatPlane || CurrentTool == Tool.SetHeight || CurrentTool == Tool.Flatten)) {
                if(LinePlaneIntersection(out position) == false) {
                    SetCursorEnabled(false);
                    return false;
                }
            } else {
                Vector2 uv;
                if(Raycast(out position, out uv) == false) {
                    SetCursorEnabled(false);
                    return false;
                }
            }

            return true;
        }

        private void SetCursorEnabled(bool enabled) {
            brushProjectorGameObject.SetActive(enabled);
            topPlaneGameObject.SetActive(enabled);
        }

        private int GetSegmentsFromUnits(float units) {
            float segmentDensity = currentToolsResolution / terrainSize.x;

            return Mathf.RoundToInt(units * segmentDensity);
        }
        
        internal void UpdateAllAlphamapSamplesFromSourceAssets() {
            allTextureSamples = new float[firstTerrainData.alphamapHeight * numberOfTerrainsVertically, firstTerrainData.alphamapWidth * numberOfTerrainsHorizontally, firstTerrainData.alphamapLayers];
            float[,,] currentAlphamapSamples;

            foreach(TerrainInformation terrainInfo in terrainInformations) {
                currentAlphamapSamples = terrainInfo.terrainData.GetAlphamaps(0, 0, firstTerrainData.alphamapWidth, firstTerrainData.alphamapHeight);

                for(int l = 0; l < firstTerrainData.alphamapLayers; l++) {
                    for(int x = 0; x < firstTerrainData.alphamapWidth; x++) {
                        for(int y = 0; y < firstTerrainData.alphamapHeight; y++) {
                            allTextureSamples[terrainInfo.alphamapsYOffset + y, terrainInfo.alphamapsXOffset + x, l] =
                                currentAlphamapSamples[y, x, l];
                        }
                    }
                }
            }
        }

        private void UpdateAllAlphamapSamplesInSourceAssets() {
            float[,,] newAlphamaps = new float[alphamapResolution, alphamapResolution, firstTerrainData.alphamapLayers];

            foreach(TerrainInformation terrainInfo in terrainInformations) {
                for(int l = 0; l < firstTerrainData.alphamapLayers; l++) {
                    for(int x = 0; x < alphamapResolution; x++) {
                        for(int y = 0; y < alphamapResolution; y++) {
                            newAlphamaps[x, y, l] = allTextureSamples[x + terrainInfo.alphamapsXOffset, y + terrainInfo.alphamapsYOffset, l];
                        }
                    }
                }
                terrainInfo.terrainData.SetAlphamaps(0, 0, newAlphamaps);
            }
        }

        private void UpdateAllHeightsFromSourceAssets() {
            float[,] temporaryHeights;
            foreach(TerrainInformation terrainInformation in terrainInformations) {
                temporaryHeights = terrainInformation.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                for(int x = 0; x < heightmapResolution; x++) {
                    for(int y = 0; y < heightmapResolution; y++) {
                        allTerrainHeights[terrainInformation.heightmapYOffset + y, terrainInformation.heightmapXOffset + x] = temporaryHeights[y, x];
                    }
                }
            }
        }

        private void UpdateAllHeightsInSourceAssets() {
            float[,] temporaryHeights = new float[heightmapResolution, heightmapResolution];
            foreach(TerrainInformation terrainInformation in terrainInformations) {
                for(int x = 0; x < heightmapResolution; x++) {
                    for(int y = 0; y < heightmapResolution; y++) {
                        temporaryHeights[y, x] = allTerrainHeights[y + terrainInformation.heightmapYOffset, x + terrainInformation.heightmapXOffset];
                    }
                }

                terrainInformation.terrainData.SetHeights(0, 0, temporaryHeights);
            }
        }

        private void UpdateAllUnmodifiedHeights() {
            allUnmodifiedTerrainHeights = (float[,])allTerrainHeights.Clone();

            //UnityEngine.Profiling.Profiler.BeginSample("UpdateAllUnmodifiedHeights");
            //if(allUnmodifiedTerrainHeights == null) {
            //    allUnmodifiedTerrainHeights = new float[totalHeightmapSamplesHorizontally, totalHeightmapSamplesVertically];
            //}

            //for(int x = 0; x < totalHeightmapSamplesHorizontally; x++) {
            //    for(int y = 0; y < totalHeightmapSamplesVertically; y++) {
            //        allUnmodifiedTerrainHeights[x, y] = allTerrainHeights[x, y];
            //    }
            //}
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        internal static ModeSettings GetCurrentToolSettings() {
            return settings.modeSettings[Instance.CurrentTool];
        }
#endregion
    }
}