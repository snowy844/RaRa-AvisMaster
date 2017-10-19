using TinyJSON;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    [System.Serializable]
    internal class ModeSettings {
        [Include]
        internal bool useFalloffForCustomBrushes = false;
        [Include]
        internal bool useAlphaFalloff = true;
        [Include]
        internal string selectedBrushTab = "All";
        [Include]
        internal string selectedBrushId;
        [Include]
        internal float brushSize = 35f;
        [Include]
        internal float brushSpeed = 1f;
        [Include]
        internal float brushRoundness = 1f;
        [Include]
        internal float brushAngle = 0f;

        [Exclude]
        internal AnimationCurve brushFalloff = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 1f));
        [Include]
        internal FauxKeyframe[] brushFalloffFauxFrames;

        // Random Spacing
        [Include]
        internal bool useBrushSpacing = false;
        [Include]
        internal float minBrushSpacing = 1f;
        [Include]
        internal float maxBrushSpacing = 50f;

        // Random Rotation
        [Include]
        internal bool useRandomRotation = false;
        [Include]
        internal float minRandomRotation = -180f;
        [Include]
        internal float maxRandomRotation = 180f;

        // Random Offset
        [Include]
        internal bool useRandomOffset = false;
        [Include]
        internal float randomOffset = 30f;

        [Include]
        internal bool invertBrushTexture = false;
    }
}
