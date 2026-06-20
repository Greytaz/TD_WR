using System.IO;
using UnityEngine;

namespace TowerDefense.Utils
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "td_save.json");

        [System.Serializable]
        public class SaveData
        {
            public int bestWaveReached;
        }

        public static void SaveBestWave(int wave)
        {
            int currentBest = LoadBestWave();
            if (wave <= currentBest) return;

            SaveData data = new SaveData { bestWaveReached = wave };
            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(SavePath, json);
                Debug.Log($"Saved best wave: {wave} to {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save data: {e.Message}");
            }
        }

        public static int LoadBestWave()
        {
            if (!File.Exists(SavePath))
            {
                return 0;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data.bestWaveReached;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load data: {e.Message}");
                return 0;
            }
        }

        public static void ClearSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
    }
}
