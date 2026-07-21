using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Perfectheart.NPCs;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace Perfectheart.Projectiles
{
	public class SpawnBeam : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 13;
		}

		public override void SetDefaults() {
			Projectile.width = 53;
			Projectile.height = 71;

			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Default;
			Projectile.ignoreWater = true;
            Projectile.light = 1f;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;

			Projectile.alpha = 0;
		}

		public override void AI() {
            if (Projectile.frame >= 12) {
                if (Projectile.ai[0] == 1f) {
                    Filters.Scene.Activate("PerfectheartSpawnFlash");
                    Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(1f);
                    Projectile.ai[0] = 2f;
				    Projectile.alpha = 255;
                } else if (Projectile.ai[0] == 2f) {
                    if (Projectile.ai[1] > 80) {
                        Projectile.ai[1] = Projectile.ai[1] + 1;
                        Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(1f - ((1f - Projectile.ai[1] / 100f) / ((100f - 80f) / 50f))); // idfk
                    } else if (Projectile.ai[1] > 50) {
                        Projectile.ai[1] = Projectile.ai[1] + 1;
                        Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(0.5f);
                    } else {
                        Projectile.ai[1] = Projectile.ai[1] + 10;
                        Filters.Scene["PerfectheartSpawnFlash"].GetShader().UseProgress(Projectile.ai[1] / 100f);
                    }
                    if (Projectile.ai[1] >= 100) {
                        Projectile.Kill();
                    } else if (Projectile.ai[1] == 50) {
                        NPC.SpawnBoss((int)Projectile.position.X, (int)Projectile.position.Y, ModContent.NPCType<PerfectheartBoss>(), Main.myPlayer);
                    }
                } else {
                    Projectile.Kill();
                }
                return;
            }

			if (++Projectile.frameCounter >= 5) {
				Projectile.frameCounter = 0;
				if (++Projectile.frame >= Main.projFrames[Projectile.type]) Projectile.frame = 0;
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