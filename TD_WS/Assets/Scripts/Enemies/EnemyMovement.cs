using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Utils;

namespace TowerDefense.Enemies
{
    public class EnemyMovement : MonoBehaviour
    {
        private EnemyData enemyData;
        private List<Vector3> waypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private float currentSpeed;
        private float slowModifier = 1f;
        private bool isStunned = false;
        private Vector3 positionOffset;

        public void Initialize(EnemyData data, List<Vector3> pathWaypoints, float waveSpeedMultiplier)
        {
            enemyData = data;
            waypoints = pathWaypoints;
            currentWaypointIndex = 0;
            slowModifier = 1f;
            isStunned = false;
            currentSpeed = enemyData.speed * waveSpeedMultiplier;

            // Generate small random offset for crowd appearance
            positionOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));

            if (waypoints != null && waypoints.Count > 0)
            {
                transform.position = waypoints[0] + positionOffset;
            }
        }

        private void Update()
        {
            if (isStunned || waypoints == null || waypoints.Count == 0) return;

            MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            if (currentWaypointIndex >= waypoints.Count) return;

            Vector3 targetPosition = waypoints[currentWaypointIndex] + positionOffset;
            
            // Retain the enemy's own Y height, or align to grid Y
            targetPosition.y = transform.position.y;

            float step = currentSpeed * slowModifier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // Rotate towards target waypoint
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    ReachBase();
                }
            }
        }

        public void SetSlowModifier(float modifier)
        {
            slowModifier = modifier;
        }

        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
        }

        private void ReachBase()
        {
            // Reached player base! Trigger base damage event
            EventBus.TriggerEnemyReachedBase();

            // Recycle enemy to pool
            ObjectPool.Instance.ReturnToPool(gameObject, enemyData.enemyName);
        }
    }
}
