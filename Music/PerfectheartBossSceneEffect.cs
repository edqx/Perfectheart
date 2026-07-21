using System;
using PerfectheadMod.System;
using Perfectheart.Enums;
using Terraria;
using Terraria.ModLoader;

namespace Perfectheart {

    public class PerfectheartBossSceneEffect : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Music/TeeheeTime");
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player)
        {
            return PerfectheartBossSystem.BossStage != FightStage.Nil && PerfectheartBossSystem.BossStage != FightStage.WaitingForFight;
        }
    }
}