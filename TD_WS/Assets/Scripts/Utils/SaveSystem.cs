using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Utils
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "td_save.json");
        private static string RunSavePath => Path.Combine(Application.persistentDataPath, "td_run_save.json");

        [System.Serializable]
        public class SaveData
        {
            public int bestWaveReached;
            public int currentRunWave;
        }

        [System.Serializable]
        public class TowerSaveEntry
        {
            public int x;
            public int z;
            public string towerType;
            public int baseTier;
            public int bodyTier;
            public int weaponTier;
        }

        [System.Serializable]
        public class ActiveRunSaveData
        {
            public int gold;
            public int hp;
            public int wave;
            public List<string> activePerks = new List<string>();
            public List<TowerSaveEntry> towers = new List<TowerSaveEntry>();
        }

        private static SaveData LoadData()
        {
            if (!File.Exists(SavePath))
            {
                return new SaveData { bestWaveReached = 0, currentRunWave = 0 };
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) data = new SaveData();
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load data: {e.Message}");
                return new SaveData();
            }
        }

        private static void SaveDataDirect(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save data: {e.Message}");
            }
        }

        public static void SaveBestWave(int wave)
        {
            SaveData data = LoadData();
            if (wave > data.bestWaveReached)
            {
                data.bestWaveReached = wave;
                SaveDataDirect(data);
                Debug.Log($"Saved best wave: {wave} to {SavePath}");
            }
        }

        public static int LoadBestWave()
        {
            return LoadData().bestWaveReached;
        }

        public static void SaveCurrentRunWave(int wave)
        {
            SaveData data = LoadData();
            data.currentRunWave = wave;
            SaveDataDirect(data);
            Debug.Log($"Saved current run wave: {wave} to {SavePath}");
        }

        public static int LoadCurrentRunWave()
        {
            return LoadData().currentRunWave;
        }

        public static void SaveActiveRun(ActiveRunSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(RunSavePath, json);
                Debug.Log($"Saved active run state to {RunSavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save active run: {e.Message}");
            }
        }

        public static ActiveRunSaveData LoadActiveRun()
        {
            if (!File.Exists(RunSavePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(RunSavePath);
                return JsonUtility.FromJson<ActiveRunSaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load active run: {e.Message}");
                return null;
            }
        }

        public static bool HasActiveRun()
        {
            return File.Exists(RunSavePath);
        }

        public static void ClearActiveRun()
        {
            if (File.Exists(RunSavePath))
            {
                try
                {
                    File.Delete(RunSavePath);
                    Debug.Log("Cleared active run save file.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to clear active run save file: {e.Message}");
                }
            }
        }

        public static void ClearSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
            ClearActiveRun();
        }
    }
}
