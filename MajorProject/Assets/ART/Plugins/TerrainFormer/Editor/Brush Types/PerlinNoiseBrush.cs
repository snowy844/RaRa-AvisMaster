using UnityEditor;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    internal class PerlinNoiseBrush : TerrainBrush {
        private const string prettyTypeName = "Perlin Noise";
        private const int typeSortOrder = 10;
        private static Texture2D typeIcon;
        
        public PerlinNoiseBrush(string name, string id) {
            this.name = name;
            this.id = id;
        }

        internal override Texture2D GetTypeIcon() {
            if(typeIcon == null) {
                typeIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainFormerEditor.texturesDirectory + "Icons/LinearProceduralBrushIcon.psd");
            }
            return typeIcon;
        }
        
        // TODO: Add Support for multiple layers
        internal override float[,] GenerateTextureSamples(int pixelsPerAxis) {
            float[,] samples = GenerateFalloff(pixelsPerAxis);
            
            float spanCoefficient = (1f / pixelsPerAxis) * TerrainFormerEditor.settings.perlinNoiseScale;
            float minMaxDifferenceCoefficient = 1f / (TerrainFormerEditor.settings.perlinNoiseMax - TerrainFormerEditor.settings.perlinNoiseMin);
            for(int x = 0; x < pixelsPerAxis; x++) {
                for(int y = 0; y < pixelsPerAxis; y++) {
                    samples[x, y] = Mathf.Clamp01(samples[x, y] * (Mathf.PerlinNoise(x * spanCoefficient, y * spanCoefficient) - TerrainFormerEditor.settings.perlinNoiseMin) * minMaxDifferenceCoefficient);
                }
            }

            if(TerrainFormerEditor.GetCurrentToolSettings().invertBrushTexture || TerrainFormerEditor.settings.invertBrushTexturesGlobally) {
                for(int x = 0; x < pixelsPerAxis; x++) {
                    for(int y = 0; y < pixelsPerAxis; y++) {
                        samples[x, y] = 1f - samples[x, y];
                    }
                }
            }
            return samples;
        }
    }
}