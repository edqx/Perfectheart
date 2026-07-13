using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using PerfectheadMod.System;
using PerfectheartMod.Enums;
using PerfectheartMod.Projectiles;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PerfectheartMod.NPCs
{
    public class PerfectheartBoss : ModNPC
    {
        public int NumFightAttempts = 0;
        public float ascendVelocity = 0f;

        public float gracefullyFloatDownStartY = 0f;
        public uint gracefullyFloatDownFrame;

        public float angelicWrathMidpointX = 0f;
        public bool isAngelicWrathActive = false;

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
            switch (NumFightAttempts)
            {
                case 1:
                    return Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt1");
                case 2:
                    return Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt2");
                case 3:
                    return Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightAttempt3");
            }
            return "";
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

        void OnFightButtonClicked()
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

        bool FightAttempt()
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

        void StartFight()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PerfectheartMod.Dialogue.FightBegin"), Microsoft.Xna.Framework.Color.Pink);
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(Language.GetTextValue("Mods.PerfectheartMod.Dialogue.FightBegin"), Microsoft.Xna.Framework.Color.Pink);
            }
            PerfectheartBossSystem.BossStage = FightStage.FightStarting;
            Entity.boss = true;
        }

        public override void OnKill()
        {
            PerfectheartBossSystem.BossStage = FightStage.Nil;
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.type == ModContent.ProjectileType<AngelicWrath>())
                {
                    proj.Kill();
                }
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            gracefullyFloatDownStartY = Entity.position.Y;
            gracefullyFloatDownFrame = Main.GameUpdateCount;
            PerfectheartBossSystem.BossStage = FightStage.GracefullyFloatingDown;
        }

        bool CallAngelicWrath()
        {
            if (isAngelicWrathActive) return false;

            isAngelicWrathActive = true;
            float k = 0;
            angelicWrathMidpointX = Entity.position.X;
            while (k < Main.maxTilesY)
            {
                Projectile.NewProjectileDirect(
                    NPC.GetSource_FromThis(),
                    new Vector2(angelicWrathMidpointX, 0f) + new Vector2(-100, k).ToWorldCoordinates(),
                    Vector2.Zero,
                    ModContent.ProjectileType<AngelicWrath>(),
                    0,
                    0f,
                    -1,
                    k
                );
                Projectile.NewProjectileDirect(
                    NPC.GetSource_FromThis(),
                    new Vector2(angelicWrathMidpointX, 0f) + new Vector2(100, k).ToWorldCoordinates(),
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

            if (isAngelicWrathActive)
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
                    AIFloatingDown();
                    break;
                case FightStage.FightStarting:
                    AIStartingFight();
                    break;
                case FightStage.Hover:
                    AIHover();
                    break;
            }
        }

        float GetEntityMidpoint()
        {
            return Entity.position.X + (90 / 4);
        }

        Player FindNearestPlayer()
        {
            float entityMidPoint = GetEntityMidpoint();

            Player nearestPlayer = null;
            float nearestPlayerDistX = 9999999f;
            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player != null && !player.dead && player.active)
                {
                    float distX = MathF.Abs(player.position.X - entityMidPoint);
                    if (distX < nearestPlayerDistX)
                    {
                        nearestPlayerDistX = distX;
                        nearestPlayer = player;
                    }
                }
            }
            return nearestPlayer;
        }

        void LookInDirectionOfPosition(float x)
        {
            float entityMidPoint = GetEntityMidpoint();
            Entity.direction = x > entityMidPoint ? 1 : -1;
        }

        void LookInDirectionOfPlayer(Player player)
        {
            LookInDirectionOfPosition(player.position.X);
        }

        void LookInDirectionOfNearestPlayer()
        {
            Player nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null)
            {
                LookInDirectionOfPlayer(nearestPlayer);
            }
        }

        void AngelicWrathAnnihilate()
        {
            float min = angelicWrathMidpointX - 100 * 16;
            float max = angelicWrathMidpointX + 100 * 16;

            for (int i = 0; i < Main.player.Length; i++)
            {
                Player player = Main.player[i];
                if (player != null && player.active && !player.dead)
                {
                    Mod.Logger.DebugFormat("Position: {0} {1}", player.position.X, angelicWrathMidpointX);
                    if (player.position.X <= min || player.position.X >= max)
                    {
                        player.KillMe(PlayerDeathReason.ByNPC(Entity.whoAmI), 999999999, 0, false);
                    }
                }
            }
        }

        void AIFloatingDown()
        {
            if (Main.GameUpdateCount - gracefullyFloatDownFrame < 120) return;

            long diff = Main.GameUpdateCount - gracefullyFloatDownFrame - 120;
            if (diff > 180)
            {
                PerfectheartBossSystem.BossStage = FightStage.WaitingForFight;
                return;
            }
            Entity.position.Y = gracefullyFloatDownStartY + diff / (float)180 * 7 * 16;
        }

        void AIStartingFight()
        {
            Entity.position = Entity.position - new Vector2(0f, ascendVelocity);
            Entity.despawnEncouraged = false;
            ascendVelocity += 0.4f;
            if (ascendVelocity > 32f)
            {
                ascendVelocity = 32f;
            }
            // reached top of map
            if (Entity.position.Y < 0)
            {
                Entity.friendly = false;
                PerfectheartBossSystem.BossStage = FightStage.Hover;
                CallAngelicWrath();
            }
            return;
        }

        void AIHover()
        {
            Entity.position = Main.player[Entity.target].position + new Vector2(250f, -50f);
        }
    }
}
