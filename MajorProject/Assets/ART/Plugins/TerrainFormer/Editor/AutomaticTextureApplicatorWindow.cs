using UnityEditor;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    public class AutomaticTextureApplicatorWindow : EditorWindow {
        private Texture2D underwaterTexture, grassTexture, cliffTexture;
        private float seaLevel = 10f;
        private float cliffThresholdAngle = 20f;

        internal static void Initialize() {
            AutomaticTextureApplicatorWindow window = GetWindow<AutomaticTextureApplicatorWindow>(true, "Automatic Texture Applicator");
            window.maxSize = new Vector2(450f, 400f);
            window.minSize = window.maxSize;
        }

        private void OnGUI() {
            if(TerrainFormerEditor.Instance == null) {
                EditorGUILayout.HelpBox("Terrain Former is not currently active.", MessageType.Warning);
                return;
            }
            if(TerrainFormerEditor.Instance.terrainInformations.Count == 0) {
                EditorGUILayout.HelpBox("The currently running instance of Terrain Former has no valid terrains.", MessageType.Warning);
                return;
            }

            underwaterTexture = (Texture2D)EditorGUILayout.ObjectField("Underwater Texture", underwaterTexture, typeof(Texture2D), false);
            grassTexture = (Texture2D)EditorGUILayout.ObjectField("Grass Texture", grassTexture, typeof(Texture2D), false);
            cliffTexture = (Texture2D)EditorGUILayout.ObjectField("Cliff Texture", cliffTexture, typeof(Texture2D), false);
            seaLevel = EditorGUILayout.Slider("Sea Level", seaLevel, 0f, TerrainFormerEditor.Instance.terrainInformations[0].terrainData.size.y);
            cliffThresholdAngle = EditorGUILayout.Slider("Cliff Angle Threshold", cliffThresholdAngle, 0f, 90f);

            if(GUILayout.Button("Apply")) {
                Apply();
            }
        }

        private void Apply() {
            float[,,] currentAlphamaps;
            
            foreach(TerrainInformation ti in TerrainFormerEditor.Instance.terrainInformations) {
                currentAlphamaps = ti.terrainData.GetAlphamaps(0, 0, ti.terrainData.alphamapWidth, ti.terrainData.alphamapHeight);

                for(int y = 0; y < ti.terrainData.alphamapHeight; y++) {
                    for(int x = 0; x < ti.terrainData.alphamapWidth; x++) {
                        float height = ti.terrainData.GetInterpolatedHeight((float)y / ti.terrainData.alphamapHeight, (float)x / ti.terrainData.alphamapWidth);
                        if(height <= seaLevel) {
                            float blend = Mathf.Abs(height - seaLevel) * 2f;

                            currentAlphamaps[x, y, 0] = 0f;
                            currentAlphamaps[x, y, 1] = Mathf.Min(blend, 1f);
                            currentAlphamaps[x, y, 2] = 1f - Mathf.Min(blend, 1f);
                        } else {
                            float cutoffCoefficient = cliffThresholdAngle / 90f;
                            float steepness = ti.terrainData.GetSteepness((float)y / ti.terrainData.alphamapHeight, (float)x / ti.terrainData.alphamapWidth) / 90f;
                            //float yDirection = ti.terrainData.GetInterpolatedNormal((float)y / ti.terrainData.alphamapHeight, (float)x / ti.terrainData.alphamapWidth).y;
                            float blend;
                            
                            if(steepness < cutoffCoefficient - 0.1f) {
                                blend = 0f;
                            } else if(steepness < cutoffCoefficient + 0.1f) {
                                blend = 1f;
                            } else {
                                blend = 1f;
                            }

                            //yDirection = Mathf.Abs(cutoffCoefficient - yDirection);

                            currentAlphamaps[x, y, 1] = Mathf.Min(blend, 1f);
                            currentAlphamaps[x, y, 0] = 1f - Mathf.Min(blend, 1f);
                            currentAlphamaps[x, y, 2] = 0f;
                        }
                        
                    }
                }

                ti.terrainData.SetAlphamaps(0, 0, currentAlphamaps);
            }
        }
    }
}