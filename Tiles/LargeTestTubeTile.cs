using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Perfectheart.Items;
using Perfectheart.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Perfectheart.Tiles {
    public class LargeTestTubeTile : ModTile {
        protected int MultiversalTranslocatorModuleType;
        protected Dictionary<(int, int), int> TestTubeSpawnState = new();
        protected Dictionary<(int, int), long> FrameTimers = new();

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Height = 9;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16, 16, 16, 16, 16];
            TileObjectData.newTile.Origin = new Point16(2, 8);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(255, 192, 203), Language.GetText("Mods.PerfectheartMod.Map.LargeTestTube"));

            MultiversalTranslocatorModuleType = ModContent.ItemType<MultiversalTranslocatorModule>();
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            yield return new Item(ModContent.ItemType<LargeTestTube>());
            var tile = Main.tile[i, j];
            if (tile.TileFrameX >= 90)
                yield return new Item(ModContent.ItemType<MultiversalTranslocatorModule>());
        }

        public override void MouseOver(int i, int j)
        {
            var tile = Main.tile[i, j];

            if (tile.TileFrameX >= 90 && tile.TileFrameX <= 178) {
                Main.LocalPlayer.cursorItemIconEnabled = true;
                Main.LocalPlayer.cursorItemIconID = -1;
                Main.LocalPlayer.cursorItemIconText = Language.GetTextValue("Mods.PerfectheartMod.Dialogue.ActivateLargeTestTube");
                return;
            }

            if (Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem].type != MultiversalTranslocatorModuleType) return;
            
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
                
            var style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            Main.LocalPlayer.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(MultiversalTranslocatorModuleType, style);
        }

        public static void SetAllTileFrameX(int i, int j, int frameX, int frameY) {
            var tile = Main.tile[i, j];
            var topX = i - tile.TileFrameX % 90 / 18;
            var topY = j - tile.TileFrameY % 160 / 18;

            for (var x = topX; x < topX + 5; x++)
            {
                for (var y = topY; y < topY + 9; y++)
                {
                    Main.tile[x, y].TileFrameX = (short)(frameX + ((x - topX) * 18));
                    Main.tile[x, y].TileFrameY = (short)(frameY + ((y - topY) * 18));
                }
            }
        }

        public long GetLastFrameTimer(int x, int y)
        {
            if (FrameTimers.TryGetValue((x, y), out var timer)) return timer;

            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            FrameTimers[(x, y)] = currentTime;
            return currentTime;
        }

        public void SetLastFrameTimer(int x, int y)
        {
            FrameTimers[(x, y)] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public int GetTestTubeSpawnState(int topX, int topY)
        {
            if (TestTubeSpawnState.TryGetValue((topX, topY), out var state)) return state;

            TestTubeSpawnState[(topX, topY)] = 0;
            return 0;
        }

        public void SetTestTubeSpawnState(int topX, int topY, int state)
        {
            TestTubeSpawnState[(topX, topY)] = state;
        }

        public override bool RightClick(int i, int j)
        {
            var tile = Main.tile[i, j];
            if (tile.TileFrameX >= 90 && tile.TileFrameX <= 178)
            {
                var topX = i - tile.TileFrameX % 90 / 18;
                var topY = j - tile.TileFrameY % 160 / 18;

                for (var x = topX; x < topX + 5; x++)
                {
                    for (var y = topY; y < topY + 9; y++)
                    {
                        SetLastFrameTimer(x, y);
                    }
                }
                SetAllTileFrameX(i, j, 180, 0);
                return true;
            }

            if (Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem].type !=
                MultiversalTranslocatorModuleType) return false;
            
            Main.LocalPlayer.ConsumeItem(MultiversalTranslocatorModuleType);
            SetAllTileFrameX(i, j, 90, 0);
            SoundEngine.PlaySound(SoundID.Unlock);
            return true;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            if (Main.gamePaused) return;

            var tile = Main.tile[i, j];
            var topX = i - tile.TileFrameX % 90 / 18;
            var topY = j - tile.TileFrameY % 160 / 18;
            var frameMs = DateTimeOffset.Now.ToUnixTimeMilliseconds() - GetLastFrameTimer(i, j);
            if (tile.TileFrameX >= 90 && tile.TileFrameX <= 178)
            {
                SetTestTubeSpawnState(topX, topY, 0);
                if (frameMs > 1000 / 2)
                {   
                    if (tile.TileFrameY >= 162)
                    {
                        tile.TileFrameY -= 162;
                    }
                    else
                    {
                        tile.TileFrameY += 162;
                    }
                    SetLastFrameTimer(i, j);
                }
            }
            else if (tile.TileFrameX >= 180 && tile.TileFrameX <= 268)
            {
                var state = GetTestTubeSpawnState(topX, topY);
                if (i == topX && j == topY) {
                    if (frameMs > 1000) {
                        SetTestTubeSpawnState(topX, topY, state + 1);
                        SetLastFrameTimer(i, j);
                    }
                    if (state == 3) {
                        SoundEngine.PlaySound(SoundID.Tink);
                    }
                }
                if (state == 3) {
                    tile.TileFrameX = (short)(270 + (i - topX) * 18);
                    tile.TileFrameY = (short)(0 + (j - topY) * 18);
                    SetLastFrameTimer(i, j);
                    return;
                }
                if (state % 2 == 0)
                {
                    tile.TileFrameY = (short)(162 + (j - topY) * 18);
                }
                else
                {
                    tile.TileFrameY = (short)(0 + (j - topY) * 18);
                }
            }
            else if (tile.TileFrameX >= 270 && tile.TileFrameX <= 358)
            {
                if (frameMs <= 1000 * 2) return;
                
                if (i == topX && j == topY)
                {
                    if (tile.TileFrameY >= 324 && tile.TileFrameY <= 484)
                    {
                        var pt = new Point16(topX + 3, topY + 4).ToWorldCoordinates();
                        SoundEngine.PlaySound(SoundID.Item121);
                        SoundEngine.PlaySound(SoundID.Shatter);
                        var leader = Projectile.NewProjectileDirect(WorldGen.GetItemSource_FromTileBreak(i, j), pt + new Vector2(10, 0), Vector2.Zero, ModContent.ProjectileType<SpawnBeam>(), 0, 0f);
                        if (leader.ModProjectile is SpawnBeam spawnBeam)
                        {
                            spawnBeam.IsLeader = true;
                        }
                        else
                        {
                            Mod.Logger.ErrorFormat("Created projectile for spawn beam, but ModProjectile was not of type SpawnBeam: {0}", leader.type);
                        }
                        
                        for (var k = 1; k < 15; k++) {
                            Projectile.NewProjectileDirect(WorldGen.GetItemSource_FromTileBreak(i, j), pt + new Vector2(10, k * -100), Vector2.Zero, ModContent.ProjectileType<SpawnBeam>(), 0, 0f);
                        }
                    }
                    else if (tile.TileFrameY <= 322)
                    {
                        SoundEngine.PlaySound(SoundID.Tink);
                    }
                }
                if (tile.TileFrameY < 486)
                {   
                    tile.TileFrameY += 162;
                }
                SetLastFrameTimer(i, j);
            }
        }
    }
}