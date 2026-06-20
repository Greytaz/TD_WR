using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Enemies
{
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyBase : MonoBehaviour
    {
        public EnemyData enemyData;

        private EnemyMovement movement;
        private EnemyHealth health;

        private void Awake()
        {
            movement = GetComponent<EnemyMovement>();
            health = GetComponent<EnemyHealth>();
        }

        public void Spawn(List<Vector3> pathWaypoints, float healthMultiplier, float speedMultiplier)
        {
            movement.enabled = true;
            health.enabled = true;

            movement.Initialize(enemyData, pathWaypoints, speedMultiplier);
            health.Initialize(healthMultiplier);
        }
    }
}
