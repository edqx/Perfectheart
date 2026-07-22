using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Perfectheart.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace Perfectheart.Projectiles
{
	public class SpawnBeam : ModProjectile
	{
		public bool IsLeader
		{
			get => (int)Projectile.ai[0] > 0;
			set => Projectile.ai[0] = value ? 1 : 0;
		}

		private int _numFrames = 13;
		private int _pixelWidth = 53;
		private int _pixelHeight = 71;

		public int Progress = 0;
		
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = _numFrames;
		}

		public override void SetDefaults() {
			Projectile.width = _pixelWidth;
			Projectile.height = _pixelHeight;

			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Default;
			Projectile.ignoreWater = true;
            Projectile.light = 1f;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;

			Projectile.alpha = 0;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Progress = 0;
		}

		public override void AI() {
			// last frame of animation
			if (Projectile.frame >= Main.projFrames[Projectile.type] - 1)
			{
				Filters.Scene.Activate("PerfectheartSpawnFlash");
				
				Projectile.Opacity = 0;

				if (IsLeader)
				{
					switch (Progress)
					{
						case >= 100:
							Filters.Scene.Deactivate("PerfectheartSpawnFlash");
							Projectile.Kill();
							break;
						case > 80:
						{
							Progress += 1;
							var x = (float)(100 - Progress) / (100f - 80f);
							Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(x);
							break;
						}
						case >= 50:
						{
							if (Progress == 50)
							{
								NPC.SpawnBoss((int)Projectile.position.X, (int)Projectile.position.Y, ModContent.NPCType<PerfectheartBoss>(), Main.myPlayer);
							}
							Progress += 1;
							Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(1f);
							break;
						}
						default:
							Progress += 10;
							Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress((float)Progress / 50f);
							break;
					}
				}
				else
				{
					Projectile.Kill();
				}

				return;
			}
			
			if (++Projectile.frameCounter >= 5) {
				Projectile.frameCounter = 0;
				if (++Projectile.frame >= Main.projFrames[Projectile.type])
				{
					Projectile.frame = 0;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			var spriteEffects = SpriteEffects.None;
			if (Projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			var texture = (Texture2D)ModContent.Request<Texture2D>(Texture);

			var frameHeight = texture.Height / Main.projFrames[Projectile.type];
			var startY = frameHeight * Projectile.frame;

			var sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

			var origin = sourceRectangle.Size() / 2f;

			var offsetX = 20f;
			origin.X = Projectile.spriteDirection == 1 ? sourceRectangle.Width - offsetX : offsetX;

			var drawColor = Projectile.GetAlpha(Color.White);
			Main.EntitySpriteDraw(texture,
				Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
				sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

			return false;
		}
	}
}