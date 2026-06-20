using System;
using UnityEngine;

namespace TowerDefense.Effects
{
    public enum StatusEffectType
    {
        Slow,
        Burn,
        Stun
    }

    [System.Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float value; // e.g. slow speed modifier (0.5), or tick damage for burn
        public float timer;
        public float tickTimer;

        public ActiveStatusEffect(StatusEffectType type, float duration, float value)
        {
            this.type = type;
            this.duration = duration;
            this.value = value;
            this.timer = duration;
            this.tickTimer = 0f;
        }
    }
}
