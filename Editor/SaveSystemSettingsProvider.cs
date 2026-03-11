using System.Collections.Generic;
using UnityEditor;

namespace Jovian.SaveSystem.Editor {
    public sealed class SaveSystemSettingsProvider : SettingsProvider {
        private SaveSystemSettings settings;
        private SerializedObject serializedSettings;

        private SaveSystemSettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement) {
            settings = SaveSystemSettings.Load();
        }

        public override void OnGUI(string searchContext) {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Save System Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            settings.saveFormat = (SaveFormat)EditorGUILayout.EnumPopup("Save Format", settings.saveFormat);
            settings.maxAutoSavesPerSession = EditorGUILayout.IntSlider("Max Auto Saves Per Session", settings.maxAutoSavesPerSession, 1, 10);
            settings.currentSaveVersion = EditorGUILayout.IntField("Current Save Version", settings.currentSaveVersion);
            settings.saveDirectoryName = EditorGUILayout.TextField("Save Directory Name", settings.saveDirectoryName);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Binary Obfuscation", EditorStyles.boldLabel);
            settings.obfuscationKey = EditorGUILayout.TextField("Obfuscation Key", settings.obfuscationKey);

            if(settings.saveFormat == SaveFormat.Json) {
                EditorGUILayout.HelpBox("JSON format is human-readable and suitable for development. Switch to Binary for release builds.", MessageType.Info);
            }

            if(EditorGUI.EndChangeCheck()) {
                settings.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider() {
            SaveSystemSettingsProvider provider = new SaveSystemSettingsProvider("Project/Jovian/Save System", SettingsScope.Project) {
                keywords = new HashSet<string>(new[] { "save", "persistence", "serialization", "binary", "json" })
            };
            return provider;
        }
    }
}
