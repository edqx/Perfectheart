using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using PerfectheadMod.System;
using Perfectheart.Enums;
using Perfectheart.Projectiles;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Perfectheart.NPCs
{
    public class PerfectheartBoss : ModNPC
    {
        public int NumFightAttempts = 0;
        public float AscendVelocity = 0f;

        public float GracefullyFloatDownStartY = 0f;
        public uint GracefullyFloatDownFrame;

        public float AngelicWrathMidpointX = 0f;
        public bool IsAngelicWrathActive = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Entity.type] = 8;
        }

        public override void SetDefaults()
        {
            Entity.aiStyle = -1;
            Entity.lifeMax = 100000;
            Entity.damage = 0;
            Entity.defense = 50;
            Entity.knockBackResist = 0f;
            Entity.width = 75;
            Entity.height = 80;
            Entity.noGravity = true;
            Entity.noTileCollide = true;
            Entity.dontTakeDamage = true;
            Entity.friendly = true;
            Entity.HitSound = SoundID.NPCHit5;
            Entity.despawnEncouraged = false;

            NPCID.Sets.NoTownNPCHappiness[Entity.type] = true;

            Music = -1;
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
                FightAttempt();
                Main.npcChatText = GetChatForFightAttempt();
            }
        }

        private bool FightAttempt()
        {
            if (NumFightAttempts < 3)
            {
                NumFightAttempts++;
                return false;
            }
            else
            {
                return true;
            }
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
            PerfectheartBossSystem.BossStage = FightStage.FightStarting;
            Entity.boss = true;
        }

        public override void OnKill()
        {
            PerfectheartBossSystem.BossStage = FightStage.Nil;
            foreach (var proj in Main.projectile)
            {
                if (proj.type == ModContent.ProjectileType<AngelicWrath>())
                {
                    proj.Kill();
                }
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            GracefullyFloatDownStartY = Entity.position.Y;
            GracefullyFloatDownFrame = Main.GameUpdateCount;
            PerfectheartBossSystem.BossStage = FightStage.GracefullyFloatingDown;
        }

        private bool CallAngelicWrath()
        {
            if (IsAngelicWrathActive) return false;

            IsAngelicWrathActive = true;
            float k = 0;
            AngelicWrathMidpointX = Entity.position.X;
            while (k < Main.maxTilesY)
            {
                Projectile.NewProjectileDirect(
                    NPC.GetSource_FromThis(),
                    new Vector2(AngelicWrathMidpointX, 0f) + new Vector2(-100, k).ToWorldCoordinates(),
                    Vector2.Zero,
                    ModContent.ProjectileType<AngelicWrath>(),
                    0,
                    0f,
                    -1,
                    k
                );
                Projectile.NewProjectileDirect(
                    NPC.GetSource_FromThis(),
                    new Vector2(AngelicWrathMidpointX, 0f) + new Vector2(100, k).ToWorldCoordinates(),
                    Vector2.Zero,
                    ModContent.ProjectileType<AngelicWrath>(),
                    0,
                    0f,
                    -1,
                    k
                );
                k += 6f;
            }
            return true;
        }

        public override void AI()
        {
            if (Entity.target < 0 || Entity.target == 255 || Main.player[Entity.target].dead || !Main.player[Entity.target].active)
            {
                Entity.TargetClosest();
            }

            if (IsAngelicWrathActive)
            {
                AngelicWrathAnnihilate();
            }

            if (PerfectheartBossSystem.BossStage == FightStage.GracefullyFloatingDown || PerfectheartBossSystem.BossStage == FightStage.WaitingForFight)
            {
                LookInDirectionOfNearestPlayer();
            }

            Entity.spriteDirection = Entity.direction;

            switch (PerfectheartBossSystem.BossStage)
            {
                case FightStage.Nil:
                    break;
                case FightStage.GracefullyFloatingDown:
                    AiFloatingDown();
                    break;
                case FightStage.FightStarting:
                    AiStartingFight();
                    break;
                case FightStage.Hover:
                    AiHover();
                    break;
                case FightStage.WaitingForFight:
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

        private void AngelicWrathAnnihilate()
        {
            var min = AngelicWrathMidpointX - 100 * 16;
            var max = AngelicWrathMidpointX + 100 * 16;

            foreach (var player in Main.player)
            {
                if (player == null || !player.active || player.dead) continue;
                
                Mod.Logger.DebugFormat("Position: {0} {1}", player.position.X, AngelicWrathMidpointX);
                if (player.position.X <= min || player.position.X >= max)
                {
                    player.KillMe(PlayerDeathReason.ByNPC(Entity.whoAmI), 999999999, 0, false);
                }
            }
        }

        private void AiFloatingDown()
        {
            if (Main.GameUpdateCount - GracefullyFloatDownFrame < 120) return;

            long diff = Main.GameUpdateCount - GracefullyFloatDownFrame - 120;
            if (diff > 180)
            {
                PerfectheartBossSystem.BossStage = FightStage.WaitingForFight;
                return;
            }
            Entity.position.Y = GracefullyFloatDownStartY + diff / (float)180 * 7 * 16;
        }

        private void AiStartingFight()
        {
            Entity.position = Entity.position - new Vector2(0f, AscendVelocity);
            Entity.despawnEncouraged = false;
            AscendVelocity += 0.4f;
            if (AscendVelocity > 32f)
            {
                AscendVelocity = 32f;
            }
            // reached top of map
            if (Entity.position.Y >= 0) return;
            
            Entity.friendly = false;
            PerfectheartBossSystem.BossStage = FightStage.Hover;
            CallAngelicWrath();
        }

        private void AiHover()
        {
            Entity.position = Main.player[Entity.target].position + new Vector2(250f, -50f);
        }
    }
}
