﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Graphics;

namespace Core
{
    public enum MinigameState
    {
        Executing,
        ShowingResult,
        Finished
    }

    public static class MinigameUtil
    {
        public static void DirectChargeLerp(float dt, float fillSpeed, 
            Color minColor, Color maxColor, ref float currentValue, 
            ref Color currentColor, ref MinigameState state)
        {
            currentValue += dt * fillSpeed;
            if (currentValue > 1.0f)
                currentValue = 0.0f;

            if (MouseInput.IsLeftButtonPressed() ||
                KeyboardInput.IsKeyPressed(Keys.Space))
            {
                currentValue = MathF.Round(currentValue, 1);
                state = MinigameState.ShowingResult;
            }
            currentColor = Color.Lerp(minColor, maxColor, currentValue);
        }

        public static void ZigZagChargeLerp(float dt, float fillSpeed,
            Color minColor, Color maxColor, ref float currentValue,
            ref Color currentColor, ref MinigameState state)
        {

        }
    }

    public static class RotatingArrowMinigame
    {
        private static MinigameState state;
        private static float rps;   //Revolutions per second
        private static float angle;
        private static float showResultTime;
        private static Vector2 direction;

        public static float Angle { get { return angle; } }
        public static Vector2 Direction { get{ return direction; } }

        public static void Init(float rps, float showResultTime)
        {
            RotatingArrowMinigame.rps            = rps;
            RotatingArrowMinigame.showResultTime = showResultTime;

            angle     = (float) Random.Shared.NextDouble() * MathUtil.PI2;
            state     = MinigameState.Executing;
            CalculateDirection();
        }

        public static MinigameState Update(float dt)
        {
            if(state == MinigameState.Executing)
            {
                angle += dt * rps * MathUtil.PI2;
                angle = MathHelper.WrapAngle(angle);
                CalculateDirection();

                if (MouseInput.IsLeftButtonPressed() || 
                    KeyboardInput.IsKeyPressed(Keys.Space))
                {
                    state = MinigameState.ShowingResult;
                }
            }
            else if (state == MinigameState.ShowingResult)
            {
                showResultTime -= dt;
                if (showResultTime <= 0.0f)
                    state = MinigameState.Finished;
            }

            return state;
        }
        private static void CalculateDirection()
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            direction = new Vector2(cos, sin);
        }

        public static void Draw(SpriteBatch spriteBatch, 
            Vector2 position, float thickness, 
            float length, Color color)
        {
            spriteBatch.DrawArrow(position, direction, length, thickness, color);
        }
    }

    public static class ZigZagArrowMinigame
    {
        private static MinigameState state;
        private static float maxAngle;
        private static float wavesPerSecond;
        private static float currentAngleTime;
        private static float showResultTime;
        private static Vector2 direction;

        public static Vector2 Direction { get { return direction; } }

        public static void Init(float maxAngle, float wavesPerSecond, 
            float showResultTime)
        {
            ZigZagArrowMinigame.maxAngle       = maxAngle;
            ZigZagArrowMinigame.wavesPerSecond = wavesPerSecond;
            ZigZagArrowMinigame.showResultTime = showResultTime;
            state            = MinigameState.Executing;
            currentAngleTime = 0.0f;
        }

        public static MinigameState Update(float dt, Entity target, Camera2D camera)
        {
            if(state == MinigameState.Executing)
            {
                Vector2 baseDirection = target.Position -
                MouseInput.GetPosition(camera);
                float length = baseDirection.Length();
                if (length != 0.0f)
                    baseDirection /= length;
                else
                    baseDirection = Vector2.UnitX;

                currentAngleTime += dt * MathUtil.PI2 * wavesPerSecond; //1 sec -> 360 degrees
                if (currentAngleTime >= MathUtil.PI2)
                    currentAngleTime -= MathUtil.PI2;
                float angle = MathF.Cos(currentAngleTime) * maxAngle;

                direction = baseDirection.Rotate(angle);

                if (MouseInput.IsLeftButtonPressed() ||
                    KeyboardInput.IsKeyPressed(Keys.Space))
                {
                    state = MinigameState.ShowingResult;
                }
            }
            else if(state == MinigameState.ShowingResult)
            {
                showResultTime -= dt;
                if (showResultTime <= 0.0f)
                    state = MinigameState.Finished;
            }

            return state;
        }

        public static void Draw(SpriteBatch spriteBatch,
            Vector2 position, float thickness,
            float length, Color color)
        {
            spriteBatch.DrawArrow(position, direction,
                length, thickness, color);
        }
    }

    
    public static class ZigZagChargeArrowMinigame
    {
        private static MinigameState state;
        private static int levels;
        private static int currentLevel;
        private static Color minColor;
        private static Color maxColor;
        private static float maxAngle;
        private static float showResultTime;
        private static float wavesPerSecond;
        private static float minDragDistance;
        private static float maxDragDistance;
        private static float currentAngleTime;
        private static Vector2 direction;

        public static int CurrentLevel
        {
            get { return currentLevel; }
        }

        public static float CurrentLevelPower
        {
            get { return (float) currentLevel / levels; }
        }

        public static Vector2 Direction { get { return direction; } }

        public static void Init(int levels, float maxAngle, float wavesPerSecond, 
            float showResultTime, float minDragDistance, float maxDragDistance, 
            Color minColor, Color maxColor)
        {
            ZigZagChargeArrowMinigame.levels          = levels;
            ZigZagChargeArrowMinigame.maxAngle        = maxAngle;
            ZigZagChargeArrowMinigame.showResultTime  = showResultTime;
            ZigZagChargeArrowMinigame.wavesPerSecond  = wavesPerSecond;
            ZigZagChargeArrowMinigame.minDragDistance = minDragDistance;
            ZigZagChargeArrowMinigame.maxDragDistance = maxDragDistance;
            ZigZagChargeArrowMinigame.currentLevel    = 0;
            ZigZagChargeArrowMinigame.minColor        = minColor;
            ZigZagChargeArrowMinigame.maxColor        = maxColor;
            state            = MinigameState.Executing;
            currentAngleTime = 0.0f;
        }

        public static MinigameState Update(float dt, Entity target, Camera2D camera)
        {
            if(state == MinigameState.Executing)
            {
                Vector2 baseDirection = target.Position -
                MouseInput.GetPosition(camera);
                float length = baseDirection.Length();
                if (length != 0.0f)
                    baseDirection /= length;
                else
                    baseDirection = Vector2.UnitX;

                float dist = Math.Clamp(length, minDragDistance, maxDragDistance);
                float t = (dist - minDragDistance) / (maxDragDistance - minDragDistance);
                currentLevel = (int)MathF.Round(levels * t);
                float powerT = ((float)currentLevel / levels);

                currentAngleTime += dt * MathUtil.PI2 * wavesPerSecond; //1 sec -> 360 degrees
                if (currentAngleTime >= MathUtil.PI2)
                    currentAngleTime -= MathUtil.PI2;
                float angle = MathF.Cos(currentAngleTime) * maxAngle * powerT;

                direction = baseDirection.Rotate(angle);

                if (MouseInput.IsLeftButtonPressed() ||
                    KeyboardInput.IsKeyPressed(Keys.Space))
                {
                    state = MinigameState.ShowingResult;
                }
            }
            else if (state == MinigameState.ShowingResult)
            {
                showResultTime -= dt;
                if (showResultTime <= 0.0f)
                    state = MinigameState.Finished;
            }

            return state;
        }

        public static void Draw(SpriteBatch spriteBatch,
            Vector2 position, float arrowThickness,
            float arrowLength, float barLength, float barHeight, 
            Color arrowColor, Color barColor)
        {
            spriteBatch.DrawArrow(position, direction, 
                arrowLength, arrowThickness, arrowColor);

            float t            = (float)currentLevel / levels;
            Color currentColor = Color.Lerp(minColor, maxColor, t);
            spriteBatch.DrawRectangle(position - new Vector2(barLength * 0.5f, barHeight* 0.5f),
                new Vector2(barLength * t, barHeight), currentColor);
            spriteBatch.DrawBar(position, barLength, barHeight, barColor);
        }
    }

    public static class ChargeBarMinigame
    {
        private static MinigameState state;
        private static float fillSpeed; //Time that it takes for the bar to fill
        private static float currentValue;
        private static float showResultTime;
        private static float minValue;
        private static float maxValue;
        private static Color minColor;
        private static Color maxColor;
        private static Color currentColor;

        public static float Value 
        { 
            get 
            {
                return minValue + currentValue * (maxValue - minValue);
            } 
        }

        public static Color CurrentColor
        {
            get
            {
                return currentColor;
            }
        }

        public static void Init(float fillSpeed, float showResultTime, 
            Color minColor, Color maxColor, 
            float minValue = 0.0f, float maxValue = 1.0f)
        {
            ChargeBarMinigame.fillSpeed      = 1.0f / fillSpeed;
            ChargeBarMinigame.showResultTime = showResultTime;
            ChargeBarMinigame.minColor       = minColor;
            ChargeBarMinigame.maxColor       = maxColor;
            ChargeBarMinigame.minValue       = minValue;
            ChargeBarMinigame.maxValue       = maxValue;

            currentValue = 0.0f;
            state        = MinigameState.Executing;
        }

        public static MinigameState Update(float dt)
        {
            if (state == MinigameState.Executing)
            {
                MinigameUtil.DirectChargeLerp(dt, fillSpeed,
                    minColor, maxColor, ref currentValue, 
                    ref currentColor, ref state);
            }
            else if(state == MinigameState.ShowingResult)
            {
                showResultTime -= dt;
                if(showResultTime <= 0.0f)
                    state = MinigameState.Finished;
            }

            return state;
        }

        public static void Draw(SpriteBatch spriteBatch,
            Vector2 center, float length,
            float height, Color color)
        {
            spriteBatch.DrawRectangle(center - new Vector2(length * 0.5f, height * 0.5f),
                new Vector2(length * currentValue, height), currentColor);
            spriteBatch.DrawBar(center, length, height, color);
        }
    }

    public static class ChargeCircleMinigame
    {
        private static MinigameState state;
        private static float growSpeed; //Time that it takes for the circle to fully grow
        private static float currentValue;
        private static float showResultTime;
        private static float minRadius;
        private static float maxRadius;
        private static Color minColor;
        private static Color maxColor;
        private static Color currentColor;

        public static float Radius
        {
            get
            {
                return minRadius + currentValue * (maxRadius - minRadius);
            }
        }

        public static void Init(float growSpeed, float showResultTime,
            Color minColor, Color maxColor,
            float minRadius = 1.0f, float maxRadius = 5.0f)
        {
            ChargeCircleMinigame.growSpeed      = 1.0f / growSpeed;
            ChargeCircleMinigame.showResultTime = showResultTime;
            ChargeCircleMinigame.minColor       = minColor;
            ChargeCircleMinigame.maxColor       = maxColor;
            ChargeCircleMinigame.minRadius      = minRadius;
            ChargeCircleMinigame.maxRadius      = maxRadius;

            currentValue = 0.0f;
            state = MinigameState.Executing;
        }

        public static MinigameState Update(float dt)
        {
            if (state == MinigameState.Executing)
            {
                MinigameUtil.DirectChargeLerp(dt, growSpeed,
                    minColor, maxColor, ref currentValue,
                    ref currentColor, ref state);
            }
            else if (state == MinigameState.ShowingResult)
            {
                showResultTime -= dt;
                if (showResultTime <= 0.0f)
                    state = MinigameState.Finished;
            }

            return state;
        }

        public static void Draw(SpriteBatch spriteBatch,
            Vector2 center, float thickness, 
            Color limitsColor, int points)
        {
            spriteBatch.DrawCircle(center, minRadius, thickness,
                limitsColor, points);
            spriteBatch.DrawCircle(center, maxRadius, thickness,
                limitsColor, points);
            spriteBatch.DrawCircle(center, Radius, thickness,
                currentColor, points);
        }
    }
}
