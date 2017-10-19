using UnityEditor;

namespace JesseStiller.TerrainFormerExtension { 
    internal class SavedTool {
        internal readonly string preferencesKey;
        internal readonly Tool defaultValue;

        private Tool value;
        internal Tool Value {
            get {
                return value;
            }
            set {
                if(this.value == value) return;
                this.value = value;
                EditorPrefs.SetInt(preferencesKey, (int)value);
            }
        }

        public SavedTool(string preferencesKey, Tool defaultValue) {
            this.preferencesKey = preferencesKey;
            this.defaultValue = defaultValue;
            value = (Tool)EditorPrefs.GetInt(preferencesKey, (int)defaultValue);
        }
    }
}