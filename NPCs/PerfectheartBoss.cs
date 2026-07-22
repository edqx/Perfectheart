using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Perfectheart.Enums;
using Perfectheart.Projectiles;
using rail;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Perfectheart.NPCs
{
    public class PerfectheartBoss : ModNPC
    {
        private static int _tilePixelHeight = 16;

        private static int _pixelWidth = 80;
        private static int _pixelHeight = 80;

        private static int _defense = 50;
        
        public int NumFightAttempts;
        public float AscendVelocity;
        
        private static int _floatDownInitTileOffset = 7;
        private static int _floatDownWaitTicks = 120;
        private static int _floatDownDurationTicks = 180;

        public Vector2 FloatDownStartPosition;
        public uint FloatDownStartTick;

        public Vector2 SitPosition;
        public FightStage Stage = FightStage.Nil;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Entity.type] = 8;
        }

        public override void SetDefaults()
        {
            Entity.aiStyle = -1;
            Entity.lifeMax = 100000;
            Entity.damage = 0;
            Entity.defense = _defense;
            Entity.knockBackResist = 0f;
            Entity.width = _pixelWidth;
            Entity.height = _pixelHeight;
            Entity.noGravity = true;
            Entity.noTileCollide = true;
            Entity.dontTakeDamage = true;
            Entity.friendly = true;
            Entity.HitSound = SoundID.NPCHit5;
            Entity.despawnEncouraged = false;

            NPCID.Sets.NoTownNPCHappiness[Entity.type] = true;

            Music = -1;
        }

        public override bool NeedSaving()
        {
            return true;
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Set("SitPositionX", SitPosition.X);
            tag.Set("SitPositionY", SitPosition.Y);
        }

        public override void LoadData(TagCompound tag)
        {
            var x = tag.Get<float>("SitPositionX");
            var y = tag.Get<float>("SitPositionY");
            
            SitPosition =  new Vector2(x, y);
            Entity.position = SitPosition;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)Stage);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Stage = (FightStage)reader.ReadByte();
        }

        public override void OnSpawn(IEntitySource source)
        {
            SitPosition.X = Entity.position.X;
            SitPosition.Y = Entity.position.Y + (_floatDownInitTileOffset * _tilePixelHeight);

            FloatDownStartPosition = Entity.position;
            
            FloatDownStartTick = Main.GameUpdateCount;
            Stage = FightStage.GracefullyFloatingDown;
        }

        public override bool CanChat()
        {
            return NumFightAttempts <= 3;
        }

        public override string GetChat()
        {
            NumFightAttempts = 1;
            return GetChatForFightAttempt();
        }

        public string GetChatForFightAttempt()
        {
            return NumFightAttempts switch
            {
                1 => Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt1"),
                2 => Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt2"),
                3 => Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt3"),
                _ => ""
            };
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "Fight";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                OnFightButtonClicked();
            }
        }

        private void OnFightButtonClicked()
        {
            if (FightAttempt())
            {
                Main.CloseNPCChatOrSign();
                StartFight();
            }
            else
            {
                Main.npcChatText = GetChatForFightAttempt();
            }
        }

        /**
         * Returns true if fight can begin.
         */
        private bool FightAttempt()
        {
            if (NumFightAttempts < 3)
            {
                NumFightAttempts++;
                return false;
            }

            return true;
        }

        private void StartFight()
        {
            switch (Main.netMode)
            {
                case NetmodeID.Server:
                    ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PerfectheartMod.Dialogue.FightBegin"), Color.Pink);
                    break;
                case NetmodeID.SinglePlayer:
                    Main.NewText(Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightBegin"), Color.Pink);
                    break;
            }
            TransitionToFlyUpStage();
        }

        public bool IsFightActive()
        {
            return Stage >= FightStage.FlyUp;
        }

        public override void AI()
        {
            if (Entity.target < 0 || Entity.target == 255 || Main.player[Entity.target].dead || !Main.player[Entity.target].active)
            {
                Entity.TargetClosest();
            }

            if (Stage == FightStage.GracefullyFloatingDown || Stage == FightStage.WaitingForFight)
            {
                LookInDirectionOfNearestPlayer();
            }

            Entity.spriteDirection = Entity.direction;

            switch (Stage)
            {
                case FightStage.Nil:
                    break;
                case FightStage.GracefullyFloatingDown:
                    AiFloatingDown();
                    break;
                case FightStage.WaitingForFight:
                    break;
                case FightStage.FlyUp:
                    AiFlyUp();
                    break;
                case FightStage.Hover:
                    AiHover();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetEntityMidpoint()
        {
            return Entity.position.X + 22f;
        }

        private Player FindNearestPlayer()
        {
            var entityMidPoint = GetEntityMidpoint();

            Player nearestPlayer = null;
            var nearestPlayerDistX = 9999999f;
            foreach (var player in Main.player)
            {
                if (player == null || player.dead || !player.active) continue;
                
                var distX = MathF.Abs(player.position.X - entityMidPoint);
                if (distX >= nearestPlayerDistX) continue;
                
                nearestPlayerDistX = distX;
                nearestPlayer = player;
            }
            return nearestPlayer;
        }

        private void LookInDirectionOfPosition(float x)
        {
            var entityMidPoint = GetEntityMidpoint();
            Entity.direction = x > entityMidPoint ? 1 : -1;
        }

        private void LookInDirectionOfPlayer(Player player)
        {
            LookInDirectionOfPosition(player.position.X);
        }

        private void LookInDirectionOfNearestPlayer()
        {
            var nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null)
            {
                LookInDirectionOfPlayer(nearestPlayer);
            }
        }

        private void AiFloatingDown()
        {
            if (Main.GameUpdateCount - FloatDownStartTick < _floatDownWaitTicks) return;

            var diff = Main.GameUpdateCount - FloatDownStartTick - _floatDownWaitTicks;
            if (diff > _floatDownDurationTicks)
            {
                TransitionToWaitingForFightStage();
                return;
            }
            
            var t = (float)diff / (float)_floatDownDurationTicks;
            Entity.position = Vector2.Lerp(FloatDownStartPosition, SitPosition, t);
        }

        private void TransitionToWaitingForFightStage()
        {
            Stage = FightStage.WaitingForFight;
        }

        private void TransitionToFlyUpStage()
        {
            Filters.Scene.Activate("PerfectheartSpawnFlash");
            Stage = FightStage.FlyUp;
            Entity.boss = true;
        }

        private void AiFlyUp()
        {
            Entity.despawnEncouraged = false;
            
            AscendVelocity += 0.4f;
            if (AscendVelocity > 8f) AscendVelocity = 8f;
            
            Entity.position -= new Vector2(0f, AscendVelocity);

            float t = (SitPosition.Y - Entity.position.Y) / SitPosition.Y;
            Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(t);
            
            // reached top of map
            if (Entity.position.Y < 0) TransitionToHoverStage();
        }

        private void TransitionToHoverStage()
        {
            Filters.Scene.Deactivate("PerfectheartSpawnFlash");
            Entity.friendly = false;
            Stage = FightStage.Hover;
        }

        private void AiHover()
        {
            Entity.position = Main.player[Entity.target].position + new Vector2(250f, -50f);
        }

        public override void OnKill()
        {
            Stage = FightStage.Nil;
        }
    }
}
