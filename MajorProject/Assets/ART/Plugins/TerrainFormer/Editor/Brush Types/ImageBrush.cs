using UnityEditor;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    internal class ImageBrush : TerrainBrush {
        private const string prettyTypeName = "Image";
        private const int typeSortOrder = 20;
        private static Texture2D typeIcon;
        public Texture2D sourceTexture;
        
        public ImageBrush(string name, string id, Texture2D sourceTexture) {
            this.name = name;
            this.id = id;
            this.sourceTexture = sourceTexture;
        }
        
        internal override Texture2D GetTypeIcon() {
            if(typeIcon == null) {
                typeIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainFormerEditor.texturesDirectory + "Icons/CustomBrushIcon.psd");
            }
            return typeIcon;
        }

        internal override float[,] GenerateTextureSamples(int size) {
            // When adding and deleting brushes at once, the add event is called first and as such it might try to update a destroyed texture
            if(sourceTexture == null) return null;

            float angle = TerrainFormerEditor.GetCurrentToolSettings().brushAngle;
            bool invertBrush = TerrainFormerEditor.GetCurrentToolSettings().invertBrushTexture || TerrainFormerEditor.settings.invertBrushTexturesGlobally;

            Vector2 currentPoint;
            float[,] samples;
            if(TerrainFormerEditor.GetCurrentToolSettings().useFalloffForCustomBrushes) {
                samples = GenerateFalloff(size);
            } else {
                samples = new float[size, size];
            }

            Vector2 newPoint;
            Vector2 midPoint = new Vector2(size * 0.5f, size * 0.5f);
            float sineOfAngle = Mathf.Sin(angle * Mathf.Deg2Rad);
            float cosineOfAngle = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sample;
            bool useFalloffForCustomBrushes = TerrainFormerEditor.GetCurrentToolSettings().useFalloffForCustomBrushes;
            bool useAlphaFalloff = TerrainFormerEditor.GetCurrentToolSettings().useAlphaFalloff;
            
            for(int x = 0; x < size; x++) {
                for(int y = 0; y < size; y++) {
                    currentPoint = new Vector2(x, y);
                    
                    if(angle == 0f) {
                        newPoint = currentPoint;
                    } else {
                        newPoint = Utilities.RotatePointAroundPoint(currentPoint, midPoint, angle, sineOfAngle, cosineOfAngle);
                    }

                    if(useFalloffForCustomBrushes && useAlphaFalloff) sample = samples[x, y];
                    else sample = 1f - samples[x, y];

                    if(invertBrush) {
                        samples[x, y] = sourceTexture.GetPixelBilinear(newPoint.x / size, newPoint.y / size).grayscale * sample;
                    } else {
                        samples[x, y] = (1f - sourceTexture.GetPixelBilinear(newPoint.x / size, newPoint.y / size).grayscale) * sample;
                    }
                }
            }
            
            return samples;
        }
    }
}