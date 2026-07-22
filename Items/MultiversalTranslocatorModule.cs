using System;
using Perfectheart.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Perfectheart.Items
{
	public class MultiversalTranslocatorModule : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.value = 10000;
			Item.rare = ItemRarityID.Purple;
		}

        public override void AddRecipes()
		{
			var recipe = CreateRecipe()
				.AddIngredient(ItemID.DirtBlock, 10)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
    }
}