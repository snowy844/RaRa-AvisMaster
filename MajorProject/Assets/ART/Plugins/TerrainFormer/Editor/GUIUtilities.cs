using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JesseStiller.TerrainFormerExtension {
    internal static class GUIUtilities {
        private static Texture2D selectedSelectionGridItemTexture;
        private static GUIStyle helpBoxWithoutTheBox;
        private static GUIStyle labelCenteredVertically;
        private static readonly MethodInfo getHelpIconMethodInfo;

        private const int textureSelectionGridPadding = 4;
        private const int textureSelectionGridPaddingHalf = textureSelectionGridPadding / 2;

        private static readonly int terrainFormerRadioButtonHash = "TerrainFormerRadioButtonHash".GetHashCode();
        private static Texture2D lineTexture;

        static GUIUtilities() {
            getHelpIconMethodInfo = typeof(EditorGUIUtility).GetMethod("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic);
        }

        internal class GUIEnabledBlock : IDisposable {
            private bool enabled;

            public GUIEnabledBlock(bool enabled) {
                this.enabled = enabled;
                if(enabled) return;

                GUI.enabled = false;
            }

            public void Dispose() {
                if(enabled) return;

                GUI.enabled = true;
            }
        }

        internal class ConditionalColouredBlock : IDisposable {
            private bool enabled;
            private Color previousColour;

            public ConditionalColouredBlock(Color colour, bool enabled) {
                this.enabled = enabled;
                if(enabled == false) return;

                previousColour = GUI.color;

                GUI.color = colour;
            }

            public void Dispose() {
                if(enabled == false) return;

                GUI.color = previousColour;
            }
        }

        internal static void FillAndRightControl(Action<Rect> fillControl, Action<Rect> rightControl = null, GUIContent labelContent = null, int rightControlWidth = 0) {
            Rect baseRect = EditorGUILayout.GetControlRect();

            if(labelContent != null) {
                GUI.Label(baseRect, labelContent);
            }

            Rect fillRect = new Rect(baseRect);
            if(labelContent != null) {
                fillRect.xMin += EditorGUIUtility.labelWidth;
            }
            fillRect.xMax -= rightControlWidth;

            if(fillControl == null) {
                Debug.LogError("A \"Fill Control\" wasn't passed");
                return;
            }
            fillControl(fillRect);

            if(rightControl != null) {
                Rect rightControlRect = new Rect(baseRect);
                rightControlRect.xMin = fillRect.xMax + 4f;
                rightControl(rightControlRect);
            }
        }

        internal static void ToggleAndFillControl(GUIContent label, ref bool enableFillControl, Action<Rect> fillControl) {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect toggleRect = new Rect(controlRect);
            toggleRect.xMax = EditorGUIUtility.labelWidth;
            toggleRect.yMin -= 1f;
            enableFillControl = EditorGUI.ToggleLeft(toggleRect, label, enableFillControl);

            if(enableFillControl == false) {
                GUI.enabled = false;
            }
            Rect fillRect = new Rect(controlRect);
            fillRect.xMin = EditorGUIUtility.labelWidth + 14f;
            fillControl(fillRect);

            if(enableFillControl == false) {
                GUI.enabled = true;
            }
        }

        /// <summary>
        /// An EditorGUI control with a label, a toggle, min/max slider and min/max float fields.
        /// </summary>
        /// <returns>Returns a bool indicating if the controls' min/max values have changed or not.</returns>
        internal static bool TogglMinMaxAndFloatFields(string label, ref bool toggleValue, ref float minValue, ref float maxValue, float minValueBoundary, 
            float maxValueBoundary, int significantDigits) {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect toggleRect = new Rect(controlRect);
            toggleRect.xMax = EditorGUIUtility.labelWidth + 14;
            toggleRect.yMin -= 1f;
            toggleValue = EditorGUI.ToggleLeft(toggleRect, label, toggleValue);

            EditorGUI.BeginChangeCheck();
            Rect fillRect = new Rect(controlRect);
            fillRect.xMin = EditorGUIUtility.labelWidth + 14;
            
            Rect leftRect = new Rect(fillRect);
            leftRect.xMax = leftRect.xMin + 50f;
            minValue = Utilities.FloorToSignificantDigits(Mathf.Clamp(EditorGUI.FloatField(leftRect, minValue), minValueBoundary, maxValueBoundary), significantDigits);

            Rect middleRect = new Rect(fillRect);
            middleRect.xMin = leftRect.xMin + 55;
            middleRect.xMax = fillRect.xMax - 55f;
            EditorGUI.MinMaxSlider(middleRect, ref minValue, ref maxValue, minValueBoundary, maxValueBoundary);

            Rect rightRect = new Rect(fillRect);
            rightRect.xMin = rightRect.xMax - 50;
            maxValue = Utilities.FloorToSignificantDigits(Mathf.Clamp(EditorGUI.FloatField(rightRect, maxValue), minValueBoundary, maxValueBoundary), significantDigits);
            return EditorGUI.EndChangeCheck();
        }

        /// <summary>
        /// An EditorGUI control with a label, a min/max slider and min/max float fields.
        /// </summary>
        /// <returns>Returns a bool indicating if the controls' min/max values have changed or not.</returns>
        internal static bool MinMaxWithFloatFields(string label, ref float minValue, ref float maxValue, float minValueBoundary, float maxValueBoundary, int significantDigits) {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect labelRect = new Rect(controlRect);
            labelRect.xMax = EditorGUIUtility.labelWidth + 14;
            labelRect.yMin -= 1f;
            EditorGUI.LabelField(labelRect, label);

            EditorGUI.BeginChangeCheck();
            Rect fillRect = new Rect(controlRect);
            fillRect.xMin = EditorGUIUtility.labelWidth + 14;

            Rect leftRect = new Rect(fillRect);
            leftRect.xMax = leftRect.xMin + 50f;
            minValue = Utilities.FloorToSignificantDigits(Mathf.Clamp(EditorGUI.FloatField(leftRect, minValue), minValueBoundary, maxValueBoundary), significantDigits);

            Rect middleRect = new Rect(fillRect);
            middleRect.xMin = leftRect.xMin + 55;
            middleRect.xMax = fillRect.xMax - 55f;
            EditorGUI.MinMaxSlider(middleRect, ref minValue, ref maxValue, minValueBoundary, maxValueBoundary);

            Rect rightRect = new Rect(fillRect);
            rightRect.xMin = rightRect.xMax - 50;
            maxValue = Utilities.FloorToSignificantDigits(Mathf.Clamp(EditorGUI.FloatField(rightRect, maxValue), minValueBoundary, maxValueBoundary), significantDigits);
            return EditorGUI.EndChangeCheck();
        }

        internal static void ActionableHelpBox(string message, MessageType messageType, Action DrawActions) {
            if(helpBoxWithoutTheBox == null) {
                helpBoxWithoutTheBox = new GUIStyle(EditorStyles.helpBox);
                helpBoxWithoutTheBox.normal.background = null;
                helpBoxWithoutTheBox.padding = new RectOffset();
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(GUIContent.none, new GUIContent(message, (Texture2D)getHelpIconMethodInfo.Invoke(null, new object[] { messageType })), helpBoxWithoutTheBox, null);

            GUILayout.Space(-10f);

            if(DrawActions != null) DrawActions();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        internal static bool FullClickRegionFoldout(string header, bool folded) {
            Rect clickRect = GUILayoutUtility.GetRect(new GUIContent(header), EditorStyles.foldout);
            float defaultLeftMargin = clickRect.xMin;
            clickRect.xMin = 0f;

            Rect labelRect = new Rect(clickRect);
            labelRect.xMin = defaultLeftMargin;

            GUI.Box(labelRect, header, EditorStyles.label);

            Rect toggleRect = new Rect(2f, clickRect.y, EditorGUIUtility.labelWidth - EditorGUI.indentLevel, clickRect.height);
            if(Event.current.type == EventType.Repaint) {
                EditorStyles.foldout.Draw(toggleRect, false, false, folded, false);
            }

            Event currentEvent = Event.current;
            if(currentEvent.type == EventType.MouseDown && clickRect.Contains(currentEvent.mousePosition)) {
                folded = !folded;
                currentEvent.Use();
            }
            return folded;
        }

        internal static string BrushSelectionGrid(string previouslySelected) {
            int brushesToDisplay = TerrainFormerEditor.Instance.terrainBrushesOfCurrentType.Count;
            int brushesPerRow = Mathf.FloorToInt((Screen.width - 20f) / TerrainFormerEditor.settings.brushPreviewSize);
            int rows = Math.Max(Mathf.CeilToInt((float)brushesToDisplay / brushesPerRow), 1);
            int brushPreviewSize = TerrainFormerEditor.settings.brushPreviewSize;
            int padding = 4;
            int halfPadding = padding / 2;
            int brushPreviewSizeWithPadding = brushPreviewSize + padding;

            CreateSelectionTextureIfNecessary();

            Rect controlRect = GUILayoutUtility.GetRect(Screen.width, rows * brushPreviewSizeWithPadding);
            
            Event currentEvent = Event.current;

            GUI.BeginGroup(controlRect, GUI.skin.box);
            int currentColumn = 0;
            int currentRow = 0;
            
            if(currentEvent.type == EventType.MouseUp) {
                int selectedColumn = Mathf.FloorToInt(currentEvent.mousePosition.x / brushPreviewSizeWithPadding);
                int selectedRow = Mathf.FloorToInt(currentEvent.mousePosition.y / brushPreviewSizeWithPadding);
                int selectedItem = selectedRow * brushesPerRow + selectedColumn;

                TerrainFormerEditor.Instance.isSelectingBrush = false;
                currentEvent.Use();

                if(selectedItem <= TerrainFormerEditor.Instance.terrainBrushesOfCurrentType.Count) {
                    return TerrainFormerEditor.Instance.terrainBrushesOfCurrentType[selectedItem].id;
                } else {
                    return previouslySelected;
                }
            }

            int numberOfBrushesInSelectedType = 0;
            foreach(TerrainBrush terrainBrush in TerrainFormerEditor.Instance.terrainBrushesOfCurrentType) {
                Rect selectionRect = new Rect(currentColumn * brushPreviewSizeWithPadding, currentRow * brushPreviewSizeWithPadding, brushPreviewSizeWithPadding, brushPreviewSizeWithPadding);
                Rect imageRect = new Rect(selectionRect.x + halfPadding, selectionRect.y + halfPadding, brushPreviewSize, brushPreviewSize);
                
                if(previouslySelected == terrainBrush.id) {
                    GUI.DrawTexture(selectionRect, selectedSelectionGridItemTexture);
                }

                GUI.DrawTexture(imageRect, terrainBrush.previewTexture);

                if(TerrainFormerEditor.settings.brushSelectionDisplayType == BrushSelectionDisplayType.ImageWithTypeIcon) {
                    GUI.DrawTexture(new Rect(imageRect.x + brushPreviewSizeWithPadding - 18f, imageRect.y - halfPadding, 16f, 16f), terrainBrush.GetTypeIcon());
                }
                if(currentColumn++ == brushesPerRow - 1) {
                    currentColumn = 0;
                    currentRow++;
                }
                numberOfBrushesInSelectedType++;
            }

            if(numberOfBrushesInSelectedType == 0) {
                GUI.Label(new Rect(10f, brushPreviewSize * 0.5f - 7f, 400f, 19f), "There are no brushes in this group.");
            }
            
            GUI.EndGroup();
            
            return previouslySelected;
        }

        // Returns the selected brush tab
        internal static string BrushTypeToolbar(string selectedBrushTab, BrushCollection brushCollection) {
            Rect controlRect = GUILayoutUtility.GetRect(Screen.width - 10f, 18f);
            float tabWidth = controlRect.width / TerrainFormerEditor.terrainBrushTypes.Keys.Count;
            GUIStyle tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            GUIStyle selectedTabButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            selectedTabButtonStyle.normal.background = EditorStyles.toolbarButton.onNormal.background;

            GUI.BeginGroup(controlRect);
            int i = 0;
            foreach(string typeName in TerrainFormerEditor.terrainBrushTypes.Keys) {
                Rect typeRect = new Rect(i * tabWidth, 0f, tabWidth, 25f);
                
                if(Event.current.type == EventType.MouseUp && typeRect.Contains(Event.current.mousePosition)) {
                    Event.current.Use();
                    return typeName;
                }
                
                GUI.Box(typeRect, typeName, selectedBrushTab == typeName ? selectedTabButtonStyle : tabButtonStyle);
                i++;
            }
            GUI.EndGroup();

            return selectedBrushTab;
        }
        
        internal static int TextureSelectionGrid(int previouslySelected, Texture2D[] icons) {
            if(labelCenteredVertically == null) {
                labelCenteredVertically = new GUIStyle(GUI.skin.label);
                labelCenteredVertically.alignment = TextAnchor.MiddleLeft;
            }

            CreateSelectionTextureIfNecessary();

            int brushesPerRow = Mathf.FloorToInt((Screen.width - 20f) / TerrainFormerEditor.settings.brushPreviewSize);
            int rows = Mathf.CeilToInt((float)icons.Length / brushesPerRow);
            int brushPreviewSize = TerrainFormerEditor.settings.brushPreviewSize;
            int brushPreviewSizeWithPadding = brushPreviewSize + textureSelectionGridPadding;

            Rect selectionGridRect = GUILayoutUtility.GetRect(Screen.width, Mathf.Max(rows * brushPreviewSizeWithPadding, 30f));
            
            Event currentEvent = Event.current;

            GUIStyle addAndRemovePanel = new GUIStyle();
            addAndRemovePanel.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainFormerEditor.settings.mainDirectory + "Textures/SelectionGridAddAndRemovePanel.psd");
            addAndRemovePanel.border = new RectOffset(2, 2, 2, 0);

            GUIStyle preButton = "RL FooterButton";
            GUIContent iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add texture…");
            GUIContent iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus", "Delete selected texture");

            Rect addAndRemoveFooterRect = new Rect(selectionGridRect);
            addAndRemoveFooterRect.yMin -= 15f;
            addAndRemoveFooterRect.xMin = addAndRemoveFooterRect.xMax - 56f;
            GUI.Box(addAndRemoveFooterRect, GUIContent.none, addAndRemovePanel);

            Rect addButtonRect = new Rect(addAndRemoveFooterRect);
            addButtonRect.width = 28f;
            addButtonRect.height = 16f;
            if(GUI.Button(addButtonRect, iconToolbarPlus, preButton)) {
                PaintTextureEditorWindow.CreateAndShowForAdditions();
            }

            Rect minusButtonRect = new Rect(addButtonRect);
            minusButtonRect.xMin += 28f;
            minusButtonRect.xMax += 28f;
            using(new GUIEnabledBlock(TerrainFormerEditor.splatPrototypes.Length > 0)) {
                if(GUI.Button(minusButtonRect, iconToolbarMinus, preButton)) {
                    TerrainFormerEditor.Instance.DeleteSplatTexture(previouslySelected);
                }
            }

            GUI.BeginGroup(selectionGridRect, GUI.skin.box);
            int currentColumn = 0;
            int currentRow = 0;

            if(icons.Length == 0) {
                GUI.Label(new Rect(5f, 0f, selectionGridRect.width, selectionGridRect.height), "No textures have been defined.", labelCenteredVertically);
                GUI.EndGroup();
                return 0;
            }

            if(currentEvent.type == EventType.MouseUp || (currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)) {
                int selectedColumn = Mathf.FloorToInt(currentEvent.mousePosition.x / brushPreviewSizeWithPadding);
                int selectedRow = Mathf.FloorToInt(currentEvent.mousePosition.y / brushPreviewSizeWithPadding);
                int selectedItem = selectedRow * brushesPerRow + selectedColumn;
                
                if(currentEvent.clickCount == 2 && selectedItem >= 0 && selectedItem < TerrainFormerEditor.splatPrototypes.Length) {
                    PaintTextureEditorWindow.CreateAndShow(selectedItem);
                }

                currentEvent.Use();
                if(selectedItem <= icons.Length) {
                    return Mathf.Clamp(selectedItem, 0, icons.Length - 1);
                } else {
                    return Mathf.Clamp(previouslySelected, 0, icons.Length - 1);
                }
            }
            
            for(int i = 0; i < icons.Length; i++) { 
                Rect selectionRect = new Rect(currentColumn * brushPreviewSizeWithPadding, currentRow * brushPreviewSizeWithPadding, brushPreviewSizeWithPadding, brushPreviewSizeWithPadding);
                Rect imageRect = new Rect(selectionRect.x + textureSelectionGridPaddingHalf, selectionRect.y + textureSelectionGridPaddingHalf, brushPreviewSize, brushPreviewSize);

                if(i == previouslySelected) {
                    GUI.DrawTexture(selectionRect, selectedSelectionGridItemTexture);
                }

                GUI.DrawTexture(imageRect, icons[i]);
                 
                if(currentColumn++ == brushesPerRow - 1) {
                    currentColumn = 0;
                    currentRow++;
                }
            }
            
            GUI.EndGroup();
            
            return Mathf.Clamp(previouslySelected, 0, icons.Length - 1);
        }
        
        private static void CreateSelectionTextureIfNecessary() {
            if(selectedSelectionGridItemTexture != null) return;
            selectedSelectionGridItemTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            selectedSelectionGridItemTexture.hideFlags = HideFlags.HideAndDontSave;
            selectedSelectionGridItemTexture.SetPixel(0, 0, new Color(0.493f, 0.74f, 1f));
            selectedSelectionGridItemTexture.Apply();
        }

        internal static bool RadioButtonWithControl(bool value, Action<Rect> control) {
            Rect rect = EditorGUILayout.GetControlRect();

            Rect radioButtonRect = new Rect(rect.x + 2, rect.y + 2, 14f, 15f);

            Event current = Event.current;

            int controlId = GUIUtility.GetControlID(terrainFormerRadioButtonHash, FocusType.Passive, rect);
            
            switch(Event.current.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
                    if(rect.Contains(current.mousePosition) == false) break;
                    current.Use();
                    GUI.changed = true;
                    GUIUtility.hotControl = controlId;
                    break;
                case EventType.MouseUp:
                    if(GUIUtility.hotControl != controlId || rect.Contains(current.mousePosition) == false) break;
                    value = true;
                    current.Use();
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                    break;
                case EventType.MouseDrag:
                    if(GUIUtility.hotControl != controlId) break;
                    current.Use();
                    break;
            }

            if(current.type == EventType.Repaint) {
                Texture2D radioButtonTexture;
                if(value) {
                    if(GUIUtility.hotControl == controlId) {
                        radioButtonTexture = EditorStyles.radioButton.onActive.background;
                    } else {
                        radioButtonTexture = EditorStyles.radioButton.onNormal.background;
                    }
                } else {
                    if(GUIUtility.hotControl == controlId) {
                        radioButtonTexture = EditorStyles.radioButton.active.background;
                    } else {
                        radioButtonTexture = EditorStyles.radioButton.normal.background;
                    }
                }

                GUI.DrawTexture(radioButtonRect, radioButtonTexture, ScaleMode.ScaleAndCrop);
            }

            // Draw the control
            Rect controlRect= new Rect(rect);
            controlRect.xMin = 18;
            control(controlRect);

            return value;
        }

        internal static void DrawLine(Rect rect1, Rect rect2, float width, Color color) {
            DrawLine(new Vector2(rect1.x, rect1.y), new Vector2(rect2.x, rect2.y), width, color);
        }

        internal static void DrawLine(Vector2 pointA, Vector2 pointB, float width, Color color) {
            if(lineTexture == null) {
                lineTexture = new Texture2D(1, 1);
            }
            
            lineTexture.SetPixel(1, 1, color);
            lineTexture.Apply();

            Matrix4x4 lastMatrix = GUI.matrix;
            
            float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * 180f / Mathf.PI;

            GUIUtility.RotateAroundPivot(angle, pointA);
            float length = Vector2.Distance(pointA, pointB);
            // Using GUI.DrawTexture results is a darker than desired result.
            EditorGUI.DrawPreviewTexture(new Rect(pointA.x, pointA.y, length, width), lineTexture);
            GUI.matrix = lastMatrix;
            GUI.color = Color.white;
        }

        internal static int ToolbarWithLabel(GUIContent labelContent, int selectedIndex, string[] toolbarOptions) {
            Rect labelRect = EditorGUILayout.GetControlRect();
            Rect toolbarRect = EditorGUI.PrefixLabel(labelRect, labelContent);
            
            return GUI.Toolbar(toolbarRect, selectedIndex, toolbarOptions, EditorStyles.radioButton);
        }
    }
}