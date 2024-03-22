using System;

namespace Cmps
{
    public class HealthCmp
    {
        public float CurrentHealth;
        public float MaxHealth;

        public HealthCmp(float maxHealth)
        {
            CurrentHealth = maxHealth;
            MaxHealth     = maxHealth;
        }

        public HealthCmp(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth     = maxHealth;
        }

        public void AddHealth(float amount)
        {
            CurrentHealth = Math.Clamp(CurrentHealth + amount,
                0.0f, MaxHealth);
        }
    }
}
