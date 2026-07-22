using System;
using Perfectheart.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Perfectheart.Items
{
	public class Heart : ModItem
	{
		public override void SetDefaults()
		{
			Item.damage = 50;
			Item.DamageType = DamageClass.Melee;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 6;
			Item.value = 10000;
			Item.rare = ItemRarityID.Green;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
		}

        public override void AddRecipes()
		{
			var recipe = CreateRecipe()
				.AddIngredient(ItemID.DirtBlock, 10)
				.AddTile(TileID.WorkBenches)
				.Register();
		}

        public override bool? UseItem(Player player)
        {
			NPC.SpawnBoss((int)player.position.X + 250, (int)player.position.Y - 40, ModContent.NPCType<PerfectheartBoss>(), Array.FindIndex(Main.player, x => x == player));
            return true;
        }
    }
}