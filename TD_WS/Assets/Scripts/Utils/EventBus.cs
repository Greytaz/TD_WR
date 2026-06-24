using System;

namespace TowerDefense.Utils
{
    public static class EventBus
    {
        // Economy & Base events
        public static Action<int> OnGoldChanged;
        public static Action<int> OnBaseHPChanged;
        public static Action<int> OnLivesChanged; // Optional, or same as Base HP

        // Wave events
        public static Action<int> OnWaveStarted;
        public static Action<int> OnWaveCompleted;
        public static Action<int, int> OnEnemySpawned; // current, max
        
        // Enemy events
        public static Action<int> OnEnemyKilled; // gold reward
        public static Action<Data.EnemyData> OnEnemyKilledData; // enemy data
        public static Action OnEnemyReachedBase;
        public static Action<Data.EnemyData> OnEnemyReachedBaseData;

        // Player Progress events
        public static Action OnPlayerProgressChanged;

        // Tower events
        public static Action<Towers.TowerBase> OnTowerSelected;
        public static Action OnTowerDeselected;
        public static Action OnTowerPlaced;
        public static Action<Towers.TowerBase, float> OnTowerDamageDealt;

        // Game State events
        public static Action<int> OnGameOver; // waves survived
        public static Action OnGameRestarted;

        // Trigger helpers
        public static void TriggerGoldChanged(int amount) => OnGoldChanged?.Invoke(amount);
        public static void TriggerBaseHPChanged(int hp) => OnBaseHPChanged?.Invoke(hp);
        public static void TriggerWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
        public static void TriggerWaveCompleted(int wave) => OnWaveCompleted?.Invoke(wave);
        public static void TriggerEnemyKilled(int gold) => OnEnemyKilled?.Invoke(gold);
        public static void TriggerEnemyKilledData(Data.EnemyData data) => OnEnemyKilledData?.Invoke(data);
        public static void TriggerEnemyReachedBase() => OnEnemyReachedBase?.Invoke();
        public static void TriggerEnemyReachedBaseData(Data.EnemyData data) => OnEnemyReachedBaseData?.Invoke(data);
        public static void TriggerPlayerProgressChanged() => OnPlayerProgressChanged?.Invoke();
        public static void TriggerTowerSelected(Towers.TowerBase tower) => OnTowerSelected?.Invoke(tower);
        public static void TriggerTowerDeselected() => OnTowerDeselected?.Invoke();
        public static void TriggerTowerPlaced() => OnTowerPlaced?.Invoke();
        public static void TriggerTowerDamageDealt(Towers.TowerBase tower, float damage) => OnTowerDamageDealt?.Invoke(tower, damage);
        public static void TriggerGameOver(int waves) => OnGameOver?.Invoke(waves);
        public static void TriggerGameRestarted() => OnGameRestarted?.Invoke();
    }
}
