namespace JesseStiller.TerrainFormerExtension {
    internal class CommandArea {
        /**
        * Clipped left/bottom refers to how many units have been clipped on a given side, and clipped width/height are the spans taking into account clipping from 
        * the brush hanging off edge(s) of the terrain.
        */
        public readonly int leftOffset, bottomOffset, clippedLeft, clippedBottom, widthAfterClipping, heightAfterClipping;

        public CommandArea(int leftOffset, int bottomOffset, int clippedLeft, int clippedBottom, int widthAfterClipping, int heightAfterClipping) {
            this.leftOffset = leftOffset;
            this.bottomOffset = bottomOffset;
            this.clippedLeft = clippedLeft;
            this.clippedBottom = clippedBottom;
            this.widthAfterClipping = widthAfterClipping;
            this.heightAfterClipping = heightAfterClipping;
        }

        public override string ToString() {
            return string.Format("Clipped (left: {0}, bottom: {1}, width: {2}, height: {3}), offset (left: {4}, bottom: {5})", clippedLeft, clippedBottom,
                widthAfterClipping, heightAfterClipping, leftOffset, bottomOffset);
        }
    }
}