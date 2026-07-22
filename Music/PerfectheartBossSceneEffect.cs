using Perfectheart.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Perfectheart.Music {

    public class PerfectheartBossSceneEffect : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Music/TeeheeTime");
        public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;

        public override bool IsSceneEffectActive(Player player)
        {
            foreach (var npc in Main.npc)
            {
                if (!npc.active) continue;
                if (npc.type != ModContent.NPCType<PerfectheartBoss>()) continue;

                if (npc.ModNPC is PerfectheartBoss modNpc && modNpc.IsFightActive())
                {
                    return true;
                }
            }

            return false;
        }
    }
}