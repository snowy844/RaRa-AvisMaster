using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace JesseStiller.TerrainFormerExtension {
    internal abstract class TerrainCommand {
        protected abstract string GetName();
        
        internal float[,] brushSamples;

        private List<Object> objectsToRegisterForUndo = new List<Object>();

        internal TerrainCommand(float[,] brushSamples) {
            this.brushSamples = brushSamples;
        }
        
        internal void Execute(Event currentEvent, CommandArea commandArea) {
            if(commandArea == null) return;
            if(this is TexturePaintCommand && TerrainFormerEditor.splatPrototypes.Length == 0) return;
            
            objectsToRegisterForUndo.Clear();
            foreach(TerrainInformation terrainInformation in TerrainFormerEditor.Instance.terrainInformations) {
                if(terrainInformation.commandArea == null) continue;

                objectsToRegisterForUndo.Add(terrainInformation.terrainData);

                if(this is TexturePaintCommand) {
                    objectsToRegisterForUndo.AddRange(terrainInformation.terrainData.alphamapTextures);
                }
            }

            if(objectsToRegisterForUndo.Count == 0) return;
            
            Undo.RegisterCompleteObjectUndo(objectsToRegisterForUndo.ToArray(), GetName());
            
            foreach(TerrainInformation terrainInformation in TerrainFormerEditor.Instance.terrainInformations) {
                if(terrainInformation.commandArea == null) continue;

                terrainInformation.hasChangedSinceLastSetHeights = true;
                terrainInformation.hasChangedSinceLastSave = true;
            }
            int globalTerrainX, globalTerrainY;
            float brushSample;
            // OnControlClick
            if(currentEvent.control) {
                for(int y = 0; y < commandArea.heightAfterClipping; y++) {
                    for(int x = 0; x < commandArea.widthAfterClipping; x++) {
                        brushSample = brushSamples[x + commandArea.clippedLeft, y + commandArea.clippedBottom];
                        if(brushSample == 0f) continue;

                        globalTerrainX = x + commandArea.leftOffset;
                        globalTerrainY = y + commandArea.bottomOffset;
                        
                        OnControlClick(globalTerrainX, globalTerrainY, brushSample);
                    }
                }
            } 
            // OnShiftClick and OnShiftClickDown
            else if(currentEvent.shift) {
                OnShiftClickDown();
                for(int y = 0; y < commandArea.heightAfterClipping; y++) {
                    for(int x = 0; x < commandArea.widthAfterClipping; x++) {
                        brushSample = brushSamples[x + commandArea.clippedLeft, y + commandArea.clippedBottom];
                        if(brushSample == 0f) continue;

                        globalTerrainX = x + commandArea.leftOffset;
                        globalTerrainY = y + commandArea.bottomOffset;

                        OnShiftClick(globalTerrainX, globalTerrainY, brushSample);
                    }
                }
            }
            // OnClick
            else {
                for(int y = 0; y < commandArea.heightAfterClipping; y++) {
                    for(int x = 0; x < commandArea.widthAfterClipping; x++) {
                        brushSample = brushSamples[x + commandArea.clippedLeft, y + commandArea.clippedBottom];
                        if(brushSample == 0f) continue;

                        globalTerrainX = x + commandArea.leftOffset;
                        globalTerrainY = y + commandArea.bottomOffset;
                        
                        OnClick(globalTerrainX, globalTerrainY, brushSample);
                    }
                }
            }
        }

        protected abstract void OnClick(int globalX, int globalY, float brushSample);
        protected abstract void OnShiftClick(int globalX, int globalY, float brushSample);
        protected abstract void OnShiftClickDown();
        protected abstract void OnControlClick(int globalX, int globalY, float brushSample);
    }
}