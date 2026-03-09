using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Jovian.SaveSystem {
    /// <summary>
    /// Configuration for the save system. Stored as JSON in ProjectSettings/SaveSystemSettings.json.
    /// </summary>
    [Serializable]
    public sealed class SaveSystemSettings {
        private const string SettingsPath = "ProjectSettings/SaveSystemSettings.json";

        public SaveFormat saveFormat = SaveFormat.Json;
        public int maxAutoSavesPerSession = 3;
        public int currentSaveVersion = 1;
        public string obfuscationKey = "default-key";
        public string saveDirectoryName = "saves";

        public static SaveSystemSettings Load() {
            if(!File.Exists(SettingsPath)) {
                return new SaveSystemSettings();
            }

            try {
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<SaveSystemSettings>(json) ?? new SaveSystemSettings();
            } catch(Exception e) {
                Debug.LogWarning($"[SaveSystem] Failed to load settings: {e.Message}. Using defaults.");
                return new SaveSystemSettings();
            }
        }

        public void Save() {
            try {
                var directory = Path.GetDirectoryName(SettingsPath);
                if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            } catch(Exception e) {
                Debug.LogError($"[SaveSystem] Failed to save settings: {e.Message}");
            }
        }
    }
}
