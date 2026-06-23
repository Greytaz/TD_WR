using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Data
{
    /// <summary>
    /// SO_TowerSkin — ScriptableObject (файл-чертеж данных в Unity), который хранит 
    /// визуальный облик (скин) башни для каждого уровня (Tier) улучшения.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_NewSkin", menuName = "Tower Defense/Skins/Tower Skin")]
    public class SO_TowerSkin : ScriptableObject
    {
        [Header("General (Общие настройки)")]
        public string skinID;               // Уникальный текстовый ID скина
        public string skinName;             // Красивое название скина для UI
        public Sprite skinIcon;             // Иконка для отображения в магазине/сундуках
        public bool isDefault = false;      // Является ли этот скин стандартным

        [Header("Model Replacements (Замена 3D-моделей по уровням)")]
        // Префабы (3D-модели в сборе), которые содержат компонент TowerVisuals
        public GameObject tier1VisualPrefab; 
        public GameObject tier2VisualPrefab;
        public GameObject tier3VisualPrefab;

        /// <summary>
        /// Возвращает нужную модель в зависимости от уровня башни в бою.
        /// </summary>
        public GameObject GetVisualPrefab(int tier)
        {
            switch (tier)
            {
                case 1: return tier1VisualPrefab;
                case 2: return tier2VisualPrefab;
                case 3: return tier3VisualPrefab;
                default: return tier1VisualPrefab;
            }
        }
    }

    /// <summary>
    /// SO_TowerPerk — ScriptableObject (чертеж), описывающий перк (пассивное улучшение), 
    /// которое можно выбить из сундуков и экипировать на башню.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_NewPerk", menuName = "Tower Defense/Perks/Tower Perk")]
    public class SO_TowerPerk : ScriptableObject
    {
        [Header("General (Общие настройки)")]
        public string perkID;               // Уникальный ID перка
        public string perkName;             // Название перка
        [TextArea]
        public string description;          // Описание, что делает перк
        public Sprite icon;                 // Картинка перка

        [Header("Stat Modifiers (Модификаторы характеристик в % или единицах)")]
        public float damageMultiplier = 1.0f;     // Множитель урона (например, 1.15 означает +15% урона)
        public float fireRateMultiplier = 1.0f;   // Множитель скорости атаки
        public float rangeMultiplier = 1.0f;      // Множитель дальности стрельбы
        public bool slowOnHit = false;            // Будет ли пуля замедлять врагов-жуков
        public float bonusDamageToBugs = 0f;      // Дополнительный урон по жукам (SCI-FI!)
    }

    /// <summary>
    /// Class-контейнер, сохраняющий текущее постоянное состояние конкретной башни игрока.
    /// Это то, что сохраняется в PlayerPrefs (память игры на диске) между запусками.
    /// </summary>
    [System.Serializable]
    public class TowerMetaState
    {
        public TowerType towerType;          // Тип башни
        public bool isUnlocked = false;       // Разблокирована ли она вообще для боя
        public int currentLevel = 1;         // Постоянный уровень башни (увеличивается за фрагменты)
        public int currentFragments = 0;     // Сколько фрагментов (карточек) накоплено

        public string activeSkinID;          // ID текущего выбранного скина
        public List<string> unlockedSkinIDs = new List<string>(); // Список открытых скинов

        public List<string> equippedPerkIDs = new List<string>(); // Надетые перки (максимум 2-3)
    }

    /// <summary>
    /// TowerMetaManager — Singleton (главный управляющий класс, который существует в игре в единственном экземпляре).
    /// Отвечает за мета-прогресс: сундуки, фрагменты, перки, скины и сохранения.
    /// </summary>
    public class TowerMetaManager : MonoBehaviour
    {
        public static TowerMetaManager Instance { get; private set; }

        [Header("All Assets Configurations (Все чертежи в игре)")]
        public List<TowerData> allTowersData;           // Список всех башен
        public List<SO_TowerSkin> allSkins;             // База всех скинов
        public List<SO_TowerPerk> allPerks;             // База всех перков

        // Текущее состояние прогресса всех башен игрока
        private Dictionary<TowerType, TowerMetaState> m_TowerStates = new Dictionary<TowerType, TowerMetaState>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Не уничтожать менеджер при переходе между сценами
                LoadProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Возвращает мета-состояние башни (уровень, фрагменты, скины) по её типу.
        /// </summary>
        public TowerMetaState GetTowerState(TowerType type)
        {
            if (!m_TowerStates.ContainsKey(type))
            {
                // Если состояния еще нет, создаем стандартное
                TowerMetaState newState = new TowerMetaState { towerType = type };
                
                // Делаем базовые башни разблокированными по умолчанию
                if (type == TowerType.Archer) 
                {
                    newState.isUnlocked = true;
                }
                
                newState.activeSkinID = "Default";
                newState.unlockedSkinIDs.Add("Default");
                m_TowerStates[type] = newState;
            }
            return m_TowerStates[type];
        }

        /// <summary>
        /// Возвращает префаб модели башни с учетом её текущего Tier в бою и её Meta-скина (скина игрока).
        /// </summary>
        public GameObject GetVisualPrefabForBattle(TowerType type, int tier)
        {
            TowerMetaState state = GetTowerState(type);
            SO_TowerSkin activeSkin = allSkins.Find(s => s.skinID == state.activeSkinID);
            
            if (activeSkin == null)
            {
                // Если скин не найден, ищем стандартный скин
                activeSkin = allSkins.Find(s => s.isDefault && s.skinID.Contains(type.ToString()));
            }

            if (activeSkin != null)
            {
                return activeSkin.GetVisualPrefab(tier);
            }

            return null;
        }

        /// <summary>
        /// Добавить фрагменты башни (выпали из сундука или босса).
        /// </summary>
        public void AddFragments(TowerType type, int amount)
        {
            TowerMetaState state = GetTowerState(type);
            state.currentFragments += amount;
            Debug.Log($"[META] Получено {amount} фрагментов для {type}. Всего: {state.currentFragments}");
            SaveProgress();
        }

        /// <summary>
        /// Попытка поднять постоянный уровень башни (Level Up) за накопленные фрагменты.
        /// </summary>
        public bool TryLevelUpTower(TowerType type)
        {
            TowerMetaState state = GetTowerState(type);
            int required = GetRequiredFragmentsForLevel(state.currentLevel);

            if (state.currentFragments >= required)
            {
                state.currentFragments -= required;
                state.currentLevel++;
                Debug.Log($"[META] Башня {type} успешно улучшена до постоянного уровня {state.currentLevel}!");
                SaveProgress();
                return true;
            }

            Debug.Log($"[META] Недостаточно фрагментов для улучшения {type}! Нужно: {required}, есть: {state.currentFragments}");
            return false;
        }

        /// <summary>
        /// Возвращает количество фрагментов, необходимых для перехода на следующий уровень.
        /// </summary>
        public int GetRequiredFragmentsForLevel(int currentLevel)
        {
            // Формула: Уровень 1 -> 2 требует 10 карт, 2 -> 3 требует 20 карт, 3 -> 4 требует 40 карт и т.д.
            return currentLevel * 10;
        }

        /// <summary>
        /// Разблокировать скин (например, выпал из легендарного сундука).
        /// </summary>
        public void UnlockSkin(string skinID)
        {
            SO_TowerSkin skin = allSkins.Find(s => s.skinID == skinID);
            if (skin == null) return;

            // Определяем к какому типу башни относится скин по ID
            foreach (var tower in allTowersData)
            {
                if (skinID.Contains(tower.towerType.ToString()))
                {
                    TowerMetaState state = GetTowerState(tower.towerType);
                    if (!state.unlockedSkinIDs.Contains(skinID))
                    {
                        state.unlockedSkinIDs.Add(skinID);
                        Debug.Log($"[META] Открыт новый скин: {skin.skinName} для башни {tower.towerType}!");
                        SaveProgress();
                    }
                    break;
                }
            }
        }

        #region Save / Load Progress (Сохранения)
        public void SaveProgress()
        {
            foreach (var kvp in m_TowerStates)
            {
                string json = JsonUtility.ToJson(kvp.Value);
                PlayerPrefs.SetString("TowerMeta_" + kvp.Key, json);
            }
            PlayerPrefs.Save();
            Debug.Log("[META] Прогресс башен сохранен!");
        }

        public void LoadProgress()
        {
            m_TowerStates.Clear();
            // Загружаем прогресс для каждого известного типа башни
            System.Array types = System.Enum.GetValues(typeof(TowerType));
            foreach (TowerType type in types)
            {
                string key = "TowerMeta_" + type;
                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key);
                    TowerMetaState state = JsonUtility.FromJson<TowerMetaState>(json);
                    m_TowerStates[type] = state;
                }
                else
                {
                    // Инициализируем дефолтное состояние
                    GetTowerState(type);
                }
            }
            Debug.Log("[META] Прогресс башен успешно загружен!");
        }
        #endregion
    }
}
