using System;
using TinyJSON;

namespace JesseStiller.TerrainFormerExtension {
    internal class BaseSettings {
        private const bool showSculptingGridPlaneDefault = false;
        [Include]
        internal bool showSculptingGridPlane = showSculptingGridPlaneDefault;

        private const bool raycastOntoFlatPlaneDefault = true;
        [Include]
        internal bool raycastOntoFlatPlane = raycastOntoFlatPlaneDefault;

        private const bool showSceneViewInformationDefault = true;
        [Include]
        internal bool showSceneViewInformation = showSceneViewInformationDefault;

        private const bool displaySceneViewSculptOntoModeDefault = true;
        [Include]
        internal bool displaySceneViewSculptOntoMode = displaySceneViewSculptOntoModeDefault;

        private const bool displaySceneViewCurrentToolDefault = true;
        [Include]
        internal bool displaySceneViewCurrentTool = displaySceneViewCurrentToolDefault;

        private const bool displaySceneViewCurrentHeightDefault = true;
        [Include]
        internal bool displaySceneViewCurrentHeight = displaySceneViewCurrentHeightDefault;

        private const int brushPreviewSizeDefault = 48;
        [Include]
        internal int brushPreviewSize = brushPreviewSizeDefault;

        private const int texurePreviewSizeDefault = 64;
        [Include]
        internal int texurePreviewSize = texurePreviewSizeDefault;

        private const bool alwaysShowBrushSelectionDefault = false;
        [Include]
        internal bool alwaysShowBrushSelection = alwaysShowBrushSelectionDefault;
        internal bool AlwaysShowBrushSelection {
            get {
                return alwaysShowBrushSelection;
            }
            set {
                if(value == alwaysShowBrushSelection) return;

                alwaysShowBrushSelection = value;
                if(AlwaysShowBrushSelectionChanged != null) AlwaysShowBrushSelectionChanged();
            }
        }
        internal Action AlwaysShowBrushSelectionChanged;

        private const bool alwaysUpdateTerrainLODsDefault = true;
        [Include]
        internal bool alwaysUpdateTerrainLODs = alwaysUpdateTerrainLODsDefault;

        private const bool invertBrushTexturesGloballyDefault = false;
        [Include]
        internal bool invertBrushTexturesGlobally = invertBrushTexturesGloballyDefault;

        private const BrushSelectionDisplayType brushSelectionDisplayTypeDefault = BrushSelectionDisplayType.Tabbed;
        [Include]
        internal BrushSelectionDisplayType brushSelectionDisplayType = brushSelectionDisplayTypeDefault;

        internal virtual bool AreSettingsDefault() {
            return
                showSculptingGridPlane         == showSculptingGridPlaneDefault &&
                raycastOntoFlatPlane           == raycastOntoFlatPlaneDefault &&
                showSceneViewInformation       == showSceneViewInformationDefault &&
                displaySceneViewSculptOntoMode == displaySceneViewSculptOntoModeDefault &&
                displaySceneViewCurrentTool    == displaySceneViewCurrentToolDefault &&
                displaySceneViewCurrentHeight  == displaySceneViewCurrentHeightDefault &&
                brushPreviewSize               == brushPreviewSizeDefault &&
                texurePreviewSize              == texurePreviewSizeDefault &&
                alwaysShowBrushSelection       == alwaysShowBrushSelectionDefault &&
                alwaysUpdateTerrainLODs        == alwaysUpdateTerrainLODsDefault &&
                invertBrushTexturesGlobally    == invertBrushTexturesGloballyDefault &&
                brushSelectionDisplayType      == brushSelectionDisplayTypeDefault;
        }

        internal virtual void RestoreDefaultSettings() {
            showSculptingGridPlane         = showSculptingGridPlaneDefault;
            raycastOntoFlatPlane           = raycastOntoFlatPlaneDefault;
            showSceneViewInformation       = showSceneViewInformationDefault;
            displaySceneViewSculptOntoMode = displaySceneViewSculptOntoModeDefault;
            displaySceneViewCurrentTool    = displaySceneViewCurrentToolDefault;
            displaySceneViewCurrentHeight  = displaySceneViewCurrentHeightDefault;
            brushPreviewSize               = brushPreviewSizeDefault;
            texurePreviewSize              = texurePreviewSizeDefault;
            alwaysShowBrushSelection       = alwaysShowBrushSelectionDefault;
            alwaysUpdateTerrainLODs        = alwaysUpdateTerrainLODsDefault;
            invertBrushTexturesGlobally    = invertBrushTexturesGloballyDefault;
            brushSelectionDisplayType      = brushSelectionDisplayTypeDefault;
        }
    }
}