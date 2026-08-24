using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

namespace Watermelon
{
    public class SavePresets
    {
        private const string PRESETS_FOLDER_NAME = "SavePresets";
        private const string SAVE_FILE_NAME = "save";
        public static bool saveDataMofied = false;
        public const string DEFAULT_DIRECTORY = "Custom";
        public const string META_SUFFIX = ".meta";

        private static void LoadSaveFromPath(string presetPath)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Save Presets]: Preset can't be activated in playmode!");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogError("[Save Presets]: Preset can't be activated during compiling!");
                return;
            }

            if (!File.Exists(presetPath))
            {
                Debug.LogError(string.Format("[Save Presets]: Preset  at path {0} doesn�t  exist!", presetPath));
                return;
            }

            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentSceneName.Equals("Init") || (currentSceneName.Equals("Level Editor")))
            {
                //EditorSceneManager.OpenScene(Path.Combine(CoreEditor.FOLDER_SCENES, "Game.unity"));
            }

            // Replace current save file with the preset
            File.Copy(presetPath, GetSavePath(), true);

            // Restore any named save files bundled alongside this preset (e.g. per-world saves)
            string extrasDirectory = GetPresetExtrasDirectory(presetPath);

            if (Directory.Exists(extrasDirectory))
            {
                string[] extraFilePaths = Directory.GetFiles(extrasDirectory, "*.json");

                for (int i = 0; i < extraFilePaths.Length; i++)
                {
                    string extraFileName = Path.GetFileNameWithoutExtension(extraFilePaths[i]);
                    string extraTargetPath = Path.Combine(Application.persistentDataPath, extraFileName + ".save");

                    File.Copy(extraFilePaths[i], extraTargetPath, true);
                }
            }

            // Start game
            EditorApplication.isPlaying = true;
#endif
        }

        private static void CreateSavePreset(string saveName, string tabName = DEFAULT_DIRECTORY, string[] extraFiles = null)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                SaveController.Save(true, false);

            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[Save Presets]: Preset name can't be empty!");
                return;
            }

            if (!Directory.Exists(GetDirectoryPath())) //Creating SavePresets folder
            {
                Directory.CreateDirectory(GetDirectoryPath());
            }

            if (!Directory.Exists(GetDirectoryPath(tabName))) //Creating custom folder
            {
                Directory.CreateDirectory(GetDirectoryPath(tabName));
            }

            string savePath = GetSavePath();

            string presetPath = GetPresetPath(saveName, tabName);

            if (EditorApplication.isPlaying)
            {
                SaveFile saveFileCopy = SaveController.GetSaveFileCopy();
                File.WriteAllText(presetPath, JsonUtility.ToJson(saveFileCopy));

                if (extraFiles != null && extraFiles.Length > 0)
                {
                    string extrasDirectory = GetPresetExtrasDirectory(presetPath);
                    Directory.CreateDirectory(extrasDirectory);

                    for (int i = 0; i < extraFiles.Length; i++)
                    {
                        SaveFile extraFileCopy = SaveController.GetFile(extraFiles[i]);

                        if (extraFileCopy == null)
                            continue;

                        extraFileCopy.Flush(updateLastSaved: false);
                        File.WriteAllText(Path.Combine(extrasDirectory, extraFiles[i] + ".json"), JsonUtility.ToJson(extraFileCopy));
                    }
                }
            }
            else
            {
                if (!File.Exists(savePath))
                {
                    Debug.LogError("[Save Presets]: Save file doesn�t exist!");

                    return;
                }

                File.Copy(savePath, presetPath, true);

                if (extraFiles != null && extraFiles.Length > 0)
                {
                    string extrasDirectory = GetPresetExtrasDirectory(presetPath);
                    Directory.CreateDirectory(extrasDirectory);

                    for (int i = 0; i < extraFiles.Length; i++)
                    {
                        string extraSourcePath = Path.Combine(Application.persistentDataPath, extraFiles[i] + ".save");

                        if (File.Exists(extraSourcePath))
                        {
                            File.Copy(extraSourcePath, Path.Combine(extrasDirectory, extraFiles[i] + ".json"), true);
                        }
                    }
                }
            }

            File.SetCreationTime(presetPath, DateTime.Now);

            saveDataMofied = true;
#endif
        }

        private static string GetPresetExtrasDirectory(string presetPath)
        {
            return Path.ChangeExtension(presetPath, null);
        }



        public static void LoadSave(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);
            LoadSaveFromPath(presetPath);
        }

        public static void CreateSave(string saveName, string tabName = DEFAULT_DIRECTORY, string id = "", string[] extraFiles = null)
        {
#if UNITY_EDITOR
            if (id.Length == 0)
            {
                id = saveName;
            }

            CreateSavePreset(saveName, tabName, extraFiles);
            SetId(saveName, tabName, id);
#endif
        }

        public static void SetId(string name, string tabName, string id)
        {
            string presetPath = GetPresetPath(name, tabName) + META_SUFFIX;
            File.WriteAllText(presetPath, id);
        }

        private static string GetPresetPathById(string id)
        {
            string directoryPath = SavePresets.GetDirectoryPath();
            string[] directoryEntries = Directory.GetDirectories(directoryPath);
            string[] fileEntries;

            for (int i = 0; i < directoryEntries.Length; i++)
            {
                fileEntries = Directory.GetFiles(directoryEntries[i]);

                for (int j = 0; j < fileEntries.Length; j++)
                {
                    if (fileEntries[j].EndsWith(SavePresets.META_SUFFIX))
                    {
                        if (File.ReadAllText(fileEntries[j]).Equals(id))
                        {
                            return fileEntries[j].Replace(SavePresets.META_SUFFIX,string.Empty);
                        }
                    }
                }
            }

            return string.Empty;
        }

        public static void LoadSaveById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if (presetPath.Length == 0)
            {
                Debug.LogError(string.Format("[Save Presets]: Preset with id {0} doesn�t  exist!", id));
                return;
            }

            LoadSaveFromPath(presetPath);
        }

        public static void RemoveSave(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);

            string extrasDirectory = GetPresetExtrasDirectory(presetPath);

            if (Directory.Exists(extrasDirectory))
            {
                Directory.Delete(extrasDirectory, true);
            }

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            presetPath += META_SUFFIX;

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            saveDataMofied = true;
        }

        public static bool IsSaveExist(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);
            return File.Exists(presetPath);
        }


        public static bool IsSaveExistById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if(presetPath.Length == 0) // id isn`t found
            {
                return false;
            }

            if (File.Exists(presetPath))
            {
                return true;
            }
            else // remove meta file
            {
                File.Delete(presetPath + META_SUFFIX);
                return false;
            }
        }



        public static void RemoveSaveById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if (presetPath.Length == 0) // id isn`t found
            {
                return;
            }

            string extrasDirectory = GetPresetExtrasDirectory(presetPath);

            if (Directory.Exists(extrasDirectory))
            {
                Directory.Delete(extrasDirectory, true);
            }

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            presetPath += META_SUFFIX;

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }
            saveDataMofied = true;
        }

        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME + ".save");
        }

        public static string GetPresetPath(string saveName, string tabName)
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME, tabName, saveName + ".json");
        }

        public static string GetDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME);
        }

        public static string GetDirectoryPath(string tabName)
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME, tabName);
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
