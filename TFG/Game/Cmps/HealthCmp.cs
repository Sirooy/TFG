using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Cmps
{
    public class HealthCmp
    {
        private float currentHealth;
        private float maxHealth;
        public Texture2D Texture;
        public Rectangle HealthBorderSourceRect;
        public Rectangle CurrentHealthSourceRect;

        public float CurrentHealth
        {
            get { return currentHealth; }
            set
            {
                currentHealth = value;
                currentHealth = Math.Clamp(currentHealth, 0.0f, maxHealth);
            }
        }

        public float MaxHealth
        {
            get { return maxHealth; }
            set
            {
                maxHealth     = value;
                currentHealth = MathF.Min(currentHealth, maxHealth);
            }
        }

        public HealthCmp(float maxHealth)
        {
            this.currentHealth = maxHealth;
            this.maxHealth     = maxHealth;
        }

        public HealthCmp(float currentHealth, float maxHealth)
        {
            this.currentHealth = currentHealth;
            this.maxHealth     = maxHealth;
        }

        public void AddHealth(float amount)
        {
            CurrentHealth = Math.Clamp(CurrentHealth + amount,
                0.0f, MaxHealth);
        }
    }
}
