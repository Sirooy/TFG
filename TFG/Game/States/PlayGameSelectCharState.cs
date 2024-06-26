﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using UI;
using Core;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;

namespace States
{
    public class PlayGameSelectCharState : GameState
    {
        private static PlayerSkill[] WarriorSkills = new PlayerSkill[]
        {
            new RotatingDashPlayerSkill(1),
            new RotatingDashPlayerSkill(2),
            new RotatingDashPlayerSkill(3),
            new RotatingDashPlayerSkill(4),
            new SwordStabPSkill(1),
            new SwordStabPSkill(2),
            new SwordStabPSkill(3),
            new SwordSpinPSkill(1),
            new SwordSpinPSkill(2),
            new SwordSpinPSkill(3)
        };

        private static PlayerSkill[] MageSkills = new PlayerSkill[]
        {
            new TeleportPSkill(1),
            new TeleportPSkill(2),
            new TeleportPSkill(3),
            new TeleportPSkill(4),
            new FireballPSkill(1),
            new FireballPSkill(2),
            new FireballPSkill(3),
            new WaterballPSkill(1),
            new WaterballPSkill(2),
            new WaterballPSkill(3),
            new LightningballPSkill(1),
            new LightningballPSkill(2),
            new LightningballPSkill(3)
        };

        private static PlayerSkill[] RangerSkills = new PlayerSkill[]
        {
            new PathFollowPSkill(1),
            new PathFollowPSkill(2),
            new PathFollowPSkill(3),
            new PathFollowPSkill(4),
            new ArrowPSkill(1),
            new ArrowPSkill(2),
            new ArrowPSkill(3),
            new PullingArrowPSkill(1),
            new PullingArrowPSkill(2),
            new PullingArrowPSkill(3),
            new HealingArrowPSkill(1),
            new HealingArrowPSkill(2)
        };

        private static PlayerSkill[] CommonSkills = new PlayerSkill[]
        {
            new DragDashPlayerSkill(1),
            new DragDashPlayerSkill(2),
            new DragDashPlayerSkill(3),
            new DragDashPlayerSkill(4),
            new DirectDamagePSkill(1),
            new DirectDamagePSkill(2),
            new DirectDamagePSkill(3),
            new DirectHealthPSkill(1),
            new DirectHealthPSkill(2),
            new DirectHealthPSkill(3),
        };

        private static PlayerSkill[][] Skills = new PlayerSkill[][]
        {
            WarriorSkills,
            MageSkills,
            RangerSkills,
            CommonSkills
        };

        private static Color[] DiceColors = new Color[]
        {
            new Color(1.0f, 0.3f, 0.3f),
            new Color(0.3f, 0.3f, 1.0f),
            new Color(0.3f, 1.0f, 0.3f),
            new Color(1.0f, 1.0f, 1.0f),
        };

        public class CharData
        {
            public PlayerType Type;
            public Rectangle IconSourceRect;

            public CharData(PlayerType type, Rectangle iconSourceRect)
            {
                Type           = type;
                IconSourceRect = iconSourceRect;
            }
        };

        private GameMain game;
        private PlayGameState parentState;
        private SpriteBatch spriteBatch;
        private UIContext ui;
        private int[] selectedChars;
        private CharData[] charData;
        private Song music;

        public PlayGameSelectCharState(GameMain game, PlayGameState parentState)
        {
            this.game          = game;
            this.parentState   = parentState;
            this.spriteBatch   = game.SpriteBatch;
            this.selectedChars = new int[3] { -1, -1, -1 };
            this.charData      = new CharData[3]
            {
                new CharData(PlayerType.Warrior, new Rectangle(32, 64, 32, 32)),
                new CharData(PlayerType.Mage,    new Rectangle(64, 64, 32, 32)),
                new CharData(PlayerType.Ranger,  new Rectangle(96, 64, 32, 32))
            };
            this.music = game.Content.Load<Song>(
                GameContent.MusicPath("SelectCharMusic"));

            CreateUI();
        }

        private void CreateUI()
        {
            Texture2D backTexture = game.Content.Load<Texture2D>(
                GameContent.BackgroundPath("CommonBackground"));

            ui = new UIContext(game.Screen);

            //BACKGROUND IMAGE
            Constraints backgroundConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(1.0f),
                new AspectConstraint(1.0f));
            UIImage background = new UIImage(ui, backgroundConstraints,
                backTexture, new Rectangle(0, 0, backTexture.Width, backTexture.Height));
            ui.AddElement(background);

            //MAIN LAYOUT
            Constraints mainLayoutConstraints = new Constraints(
                new CenterConstraint(),
                new CenterConstraint(),
                new PercentConstraint(0.9f),
                new PercentConstraint(0.9f));
            UILayout mainLayout = new UILayout(ui, mainLayoutConstraints,
                LayoutType.Vertical, LayoutAlign.Start, 
                new PercentConstraint(0.05f));
            ui.AddElement(mainLayout);

            CreateUITitle(mainLayout);
            CreateUIChars(mainLayout);
            CreateUISelectedChars(mainLayout);
            CreateUIStartButton(mainLayout);
        }

        private void CreateUITitle(UILayout mainLayout)
        {
            SpriteFont uiFont = game.Content.Load<SpriteFont>(
                GameContent.FontPath("MainFont"));
            Texture2D titleTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("CharSelectTitle"));

            Constraints titleConstraints = new Constraints(
               new CenterConstraint(),
               new PixelConstraint(0.0f),
               new AspectConstraint(1.0f),
               new PercentConstraint(0.20f));
            //UIString titleString = new UIString(ui, titleConstraints,
            //    uiFont, "Select Character", Color.Black);
            UIImage titleImg = new UIImage(ui, titleConstraints,
                titleTexture, new Rectangle(0, 0, titleTexture.Width,
                titleTexture.Height));
            mainLayout.AddElement(titleImg);
        }

        private void CreateUIChars(UILayout mainLayout)
        {
            Texture2D uiTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("UI"));

            Constraints charsLayoutConstraints = new Constraints(
                new CenterConstraint(),
                new PixelConstraint(0.0f),
                new PercentConstraint(0.8f),
                new PercentConstraint(0.35f));
            UILayout charsLayout = new UILayout(ui, charsLayoutConstraints,
                LayoutType.Horizontal, LayoutAlign.Center, 
                new PercentConstraint(0.05f));
            mainLayout.AddElement(charsLayout);

            Constraints charImgConstraints = new Constraints(
                new PixelConstraint(0.0f),
                new CenterConstraint(),
                new PercentConstraint(0.2f),
                new AspectConstraint(1.0f));
            for (int i = 0; i < (int)PlayerType.NumTypes; ++i)
            {
                UIImage charImg = new UIImage(ui, charImgConstraints,
                    uiTexture, new Rectangle(32 + 32 * i, 32, 32, 32));

                UIButtonEventHandler charImgEventHandler = new UIButtonEventHandler();
                int index = i;
                charImgEventHandler.OnPress += (UIElement element) =>
                {
                    SelectCharacter(index);
                    if(GetEmptySelectedCharIndex() == -1)
                    {
                        UIElement startButton = ui.GetElement<UIElement>
                            ("StartButton");
                        startButton.IsVisible = true;
                    }
                };
                charImg.EventHandler = charImgEventHandler;
                charsLayout.AddElement(charImg);
            }
        }

        private void SelectCharacter(int index)
        {
            int slotIndex = GetEmptySelectedCharIndex();
            if (slotIndex == -1) return;

            CharData data            = charData[index];
            selectedChars[slotIndex] = (int)data.Type;

            UILayout selectedCharsLayout = ui.GetElement<UILayout>("SelectedChars");
            UIImage imageSlot = selectedCharsLayout.GetElement<UIImage>(slotIndex);
            imageSlot.Source  = data.IconSourceRect;
        }

        private int GetEmptySelectedCharIndex()
        {
            for(int i = 0;i < selectedChars.Length; ++i)
            {
                if (selectedChars[i] == -1)
                    return i;
            }

            return -1;
        }

        private void CreateUISelectedChars(UILayout mainLayout)
        {
            Texture2D uiTexture = game.Content.Load<Texture2D>(
                GameContent.TexturePath("UI"));

            Constraints selectedCharsLayoutConstraints = new Constraints(
                new CenterConstraint(),
                new PixelConstraint(0.0f),
                new PercentConstraint(0.5f),
                new PercentConstraint(0.15f));
            UILayout selectedCharsLayout = new UILayout(ui,
                selectedCharsLayoutConstraints, LayoutType.Horizontal,
                LayoutAlign.Center, new PercentConstraint(0.03f));
            mainLayout.AddElement(selectedCharsLayout, "SelectedChars");

            Constraints selectedCharImgConstraints = new Constraints(
                new PixelConstraint(0.0f),
                new CenterConstraint(),
                new AspectConstraint(1.0f),
                new PercentConstraint(1.0f));

            for(int i = 0;i < 3; ++i)
            {
                UIImage selectedCharImg = new UIImage(ui, selectedCharImgConstraints,
                    uiTexture, new Rectangle(0, 64, 32, 32));

                UIButtonEventHandler buttonEventHandler = new UIButtonEventHandler();
                int index = i;
                buttonEventHandler.OnPress += (UIElement element) =>
                {
                    if (selectedChars[index] != -1)
                    {
                        selectedChars[index]   = -1;
                        selectedCharImg.Source = new Rectangle(0, 64, 32, 32);

                        UIElement startButton = ui.GetElement<UIElement>
                            ("StartButton");
                        startButton.IsVisible = false;
                    }
                };
                selectedCharImg.EventHandler = buttonEventHandler;

                selectedCharsLayout.AddElement(selectedCharImg);
            }
        }

        private void CreateUIStartButton(UILayout mainLayout)
        {
            Constraints startButtonConstraints = new Constraints(
                new CenterConstraint(),
                new PixelConstraint(0.0f),
                new AspectConstraint(1.0f),
                new PercentConstraint(0.1f));
            UIImage startButton = UIUtil.CreateCommonButton(ui, startButtonConstraints,
                "Start", game.Content);
            mainLayout.AddElement(startButton, "StartButton");

            UIButtonEventHandler startButtonEventHandler = (UIButtonEventHandler)
                startButton.EventHandler;
            startButtonEventHandler.OnPress += (UIElement element) =>
            {
                CreatePlayerCharacters();
                parentState.GameStates.PopAllActiveStates();
                parentState.GameStates.PushState<PlayGameDungeonState>();
            };
        }

        public override StateResult Update(GameTime gameTime)
        {
            ui.Update();

            return StateResult.StopExecuting;
        }

        public override StateResult Draw(GameTime gameTime)
        {
            game.GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin(samplerState: SamplerState.PointWrap,
                blendState: BlendState.NonPremultiplied);
            ui.Draw(spriteBatch);
            spriteBatch.End();

            return StateResult.KeepExecuting;
        }

        public override void OnEnter()
        {
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(music);

            parentState.EntityManager.Clear();
            parentState.PlayerData.Dices.Clear();

            for (int i = 0; i < selectedChars.Length; ++i)
                selectedChars[i] = -1;
            ResetUI();

            DebugDraw.Camera = null;
            DebugLog.Info("OnEnter state: {0}", nameof(PlayGameLoseState));
        }

        private void CreatePlayerCharacters()
        {
            EntityFactory factory = parentState.EntityFactory;

            foreach(int charType in selectedChars)
            {
                PlayerType type = (PlayerType)charType;
                factory.CreatePlayer(type, Vector2.Zero);
            }
        }

        private void ResetUI()
        {
            //Set start button to invisible
            UIImage startButton   = ui.GetElement<UIImage>("StartButton");
            startButton.IsVisible = false;

            //Reset selected characters
            UILayout selectedCharsLayout = ui.GetElement<UILayout>("SelectedChars");
            for(int i = 0;i < selectedCharsLayout.ChildrenCount; ++i)
            {
                UIImage img = selectedCharsLayout.GetElement<UIImage>(i);
                img.Source  = new Rectangle(0, 64, 32, 32);
            }
        }

        public override void OnExit()
        {
            CreateAllDices();
            MediaPlayer.Stop();
            DebugLog.Info("OnExit state: {0}", nameof(PlayGameLoseState));
        }

        private void CreateAllDices()
        {
            List<int> faceCounts = new List<int>() { 6,4,5,6 };

            foreach(int i in selectedChars)
            {
                int faceCountIndex = Random.Shared.Next(faceCounts.Count);
                int faceCount      = faceCounts[faceCountIndex];
                CharData data      = charData[i];

                CreateDice(data.Type, faceCount);
                faceCounts.RemoveAt(faceCountIndex);
            }

            //Create common dice
            Dice commonDice = CreateDice((PlayerType)3, faceCounts[0] - 2);
            commonDice.Faces.Add(CommonSkills[Random.Shared.Next(4)]);
            commonDice.Faces.Add(CommonSkills[Random.Shared.Next(4)]);
        }

        private Dice CreateDice(PlayerType type, int faceCount)
        {
            List<PlayerSkill> faces = new List<PlayerSkill>();
            for(int i = 0;i < faceCount; ++i)
            {
                PlayerSkill[] typeSkills = Skills[(int)type];

                faces.Add(typeSkills[Random.Shared.Next(typeSkills.Length)]);
            }

            Dice dice = new Dice(faces, DiceColors[(int)type]);
            parentState.PlayerData.Dices.Add(dice);

            return dice;
        }
    }
}
