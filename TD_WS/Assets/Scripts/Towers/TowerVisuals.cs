using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// TowerVisuals — компонент, который твой друг-аниматор вешает на 3D-модель (префаб) башни.
    /// Он позволяет аниматору вручную связать ключевые точки модели в Инспекторе Unity:
    /// - Что должно крутиться в сторону врагов (голова башни).
    /// - Откуда должны вылетать пули/лазеры (точка стрельбы).
    /// - Где лежит аниматор для запуска анимаций атаки и прокачки.
    /// </summary>
    public class TowerVisuals : MonoBehaviour
    {
        [Header("Animator & Animations (Анимации)")]
        [Tooltip("Компонент Animator, который воспроизводит Idle, Attack, Upgrade анимации.")]
        public Animator animator;

        [Header("Critical Nodes (Ключевые точки для кода)")]
        [Tooltip("Часть модели, которая будет поворачиваться лицом к пробегающим врагам-жукам.")]
        public Transform rotatableHead;

        [Tooltip("Пустой объект на конце ствола пушки, откуда физически спавнятся пули.")]
        public Transform firePoint;

        [Header("Upgrade Effects (Эффекты при появлении/апгрейде)")]
        [Tooltip("Частицы, которые проигрываются один раз при спавне или прокачке именно этой модели.")]
        public ParticleSystem spawnVFX;

        private void Start()
        {
            // Если аниматор не настроен в инспекторе вручную, попробуем найти его сами
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Проиграем эффект появления, если он настроен у аниматора
            if (spawnVFX != null)
            {
                spawnVFX.Play();
            }
        }

        /// <summary>
        /// Вызывается из скрипта TowerBase, чтобы запустить анимацию выстрела.
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (animator != null)
            {
                // Запускаем триггер атаки (должен быть настроен аниматором в Unity Animator)
                animator.SetTrigger("Attack");
            }
        }

        /// <summary>
        /// Вызывается из скрипта TowerBase, когда башню улучшают.
        /// </summary>
        public void PlayUpgradeAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("Upgrade");
            }
        }
    }
}
