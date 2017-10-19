using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    internal class ShrinkWrapCommand : TerrainCommand {
        private static readonly int ignoreRaycastLayerMask = ~(1 << Physics.IgnoreRaycastLayer);
        private static readonly Vector3 downDirection = Vector3.down;
        
        private const string name = "Shrink Wrap";
        protected override string GetName() {
            return name;
        }

        private int totalTerrainWidth, totalTerrainHeight;
        internal Vector3 firstTerrainPosition;
        internal float terrainHeightCoefficient;
        internal float finalMultiplierX, finalMultiplierY;
        internal float shrinkWrapRaycastOffset;
        private RaycastHit hitInfo;

        internal ShrinkWrapCommand(float[,] brushSamples, int totalTerrainWidth, int totalTerrainHeight) : base(brushSamples) {
            this.totalTerrainWidth = totalTerrainWidth;
            this.totalTerrainHeight = totalTerrainHeight;
        }
        
        protected override void OnClick(int x, int y, float brushSample) {
            const int blurRadius = 2;

            float heightSum = 0f;
            int neighbourCount = 0;
            int positionX, positionY;

            /**
            * TODO: Versions older than Unity 2017.1 don't handle out and ref keywords in blocks with parameters labels for some reason, so no
            * pretty parameter labels :(
            */
            if(Physics.Raycast(
                new Vector3(
                    x: firstTerrainPosition.x + x * finalMultiplierX,
                    y: firstTerrainPosition.y + TerrainFormerEditor.Instance.terrainSize.y,
                    z: firstTerrainPosition.z + y * finalMultiplierY), // origin
                downDirection,                                         // direction
                out hitInfo,                                           // hitInfo
                TerrainFormerEditor.Instance.terrainSize.y,            // maxDistance
                ignoreRaycastLayerMask,                                // layerMask
                QueryTriggerInteraction.Ignore)                        // queryTriggerInteraction
                ) {
                TerrainFormerEditor.allTerrainHeights[y, x] = Mathf.Lerp(TerrainFormerEditor.allTerrainHeights[y, x], 
                    (hitInfo.point.y - shrinkWrapRaycastOffset) * terrainHeightCoefficient, brushSample * 4f);
                return;
            }
            
            // Blur/smooth the final result
            for(int x2 = -blurRadius; x2 <= blurRadius; x2++) {
                positionX = x + x2;
                if(positionX < 0 || positionX >= totalTerrainWidth) continue;

                for(int y2 = -blurRadius; y2 <= blurRadius; y2++) {
                    positionY = y + y2;
                    if(positionY < 0 || positionY >= totalTerrainHeight) continue;

                    heightSum += TerrainFormerEditor.allUnmodifiedTerrainHeights[positionY, positionX];
                    neighbourCount++;
                }
            }

            TerrainFormerEditor.allTerrainHeights[y, x] = Mathf.Lerp(TerrainFormerEditor.allUnmodifiedTerrainHeights[y, x], heightSum / neighbourCount, brushSample);
        }

        protected override void OnControlClick(int x, int y, float brushSample) { }

        protected override void OnShiftClick(int globalX, int globalY, float brushSample) { }

        protected override void OnShiftClickDown() { }
    }
}