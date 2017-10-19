using UnityEditor;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    internal class TerrainInformation {
        internal Transform transform;
        internal Terrain terrain;
        internal TerrainData terrainData;
        internal TerrainCollider collider;
        internal string terrainAssetPath;
        internal CommandArea commandArea;
        internal int gridXCoordinate, gridYCoordinate; // Co-ordinates of the terrain in respect to a terrain grid
        internal int heightmapXOffset, heightmapYOffset;
        internal int alphamapsXOffset, alphamapsYOffset;
        internal int toolCentricXOffset, toolCentricYOffset; // The samples' offsets based on the current tool selected
        internal bool hasChangedSinceLastSetHeights = false;
        internal bool hasChangedSinceLastSave = false;
        internal bool ignoreOnAssetsImported = false;
        
        internal TerrainInformation(Terrain terrain) {
            this.terrain = terrain;
            transform = terrain.transform;
            collider = transform.GetComponent<TerrainCollider>();
            terrainData = terrain.terrainData;

            terrainAssetPath = AssetDatabase.GetAssetPath(terrainData);
        }
    }
}
