using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace Perfectheart
{
	public class Perfectheart : Mod
	{
        public override void Load()
        {
			base.Load();
			
        	var filterRef = new Ref<Effect>(Assets.Request<Effect>("Effects/SpawnFlash", AssetRequestMode.ImmediateLoad).Value);
			Filters.Scene["PerfectheartSpawnFlash"] = new Filter(new ScreenShaderData(filterRef, "FilterSpawnFlash"), EffectPriority.VeryHigh);
        }
	}
}