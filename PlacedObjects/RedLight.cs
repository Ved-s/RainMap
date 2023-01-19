using Microsoft.Xna.Framework;
using System;
using static RainMap.PlacedObjects.ILightObject;

namespace RainMap.PlacedObjects
{
	public class RedLight : LightFixture
	{
		public int FlickerWait;
		public int Flicker;
		public float Sin;
		public float SwitchOn;
		public bool GravityDependent;
		public bool Powered;

		private LightData LightSource => Lights[0];

		private float NoElectricity
		{
			get
			{
				return 0f;
				//if (this.room == null)
				//{
				//    return 0f;
				//}
				//return 1f - this.room.ElectricPower;
			}
		}

		public override void Initialize()
		{
			Sin = 0.5f;
			FlickerWait = 350;
			//FlatLightSource = new LightSource(placedObject.pos, false, new Color(1f, 0.05f, 0.05f), this);
			//FlatLightSource.flat = true;

			// TODO: room effects
			GravityDependent = false;//(placedInRoom.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) > 0f && (float)lightData.randomSeed > 0f);
			Powered = (NoElectricity > 0.5f || !GravityDependent);
			SwitchOn = RandomSeed / 100f;

			Lights = new[]
			{
								new LightData(Color.Red, GameAssets.LightMask0)
						};
		}

		public override void Update()
		{
			if (GravityDependent)
			{
				if (!Powered)
				{
					LightSource.Enabled = false;
					//this.flatLightSource.setAlpha = new float?(0f);
					if (NoElectricity <= MathHelper.Lerp(0.65f, 0.95f, SwitchOn) || 0.5f >= 1f / MathHelper.Lerp(20f, 80f, SwitchOn))
					{
						return;
					}
					Powered = true;
					Flicker = 7;
					//this.room.PlaySound(SoundID.Red_Light_On, this.placedObject.pos, 1f, 1f);
				}
				else if (NoElectricity < 0.6f && 0.5f < 0.05f)
				{
					Powered = false;
				}
			}
			float num = (!GravityDependent) ? 1f : NoElectricity;
			//FlickerWait--;
			//Sin += 1f / MathHelper.Lerp(60f, 80f, (float)0.5f);
			LightSource.Radius = MathHelper.Lerp(290f, 310f, 0.5f + MathF.Sin(Sin * 3.1415927f * 2f) * 0.5f) * 0.16f;
			LightSource.Radius = LightSource.Radius * 100;

			if (FlickerWait < 1)
			{
				FlickerWait = 350;
				Flicker = 7;
			}

			if (Flicker > 0)
			{
				Flicker--;
				if (0.5f < 0.33333334f)
				{
					float num2 = MathF.Pow((float)0.5f, 0.5f);
					LightSource.Color.SetAlpha(num2 * num);
					//this.flatLightSource.setAlpha = new float?(num2 * 0.25f * num);
					//this.flatLightSource.setRad = new float?(num2 * 30f);
				}
			}
			else
			{
				LightSource.Color.SetAlpha(MathHelper.Lerp(0.9f, 1f, 0.5f + MathF.Sin(Sin * 3.1415927f * 2f) * 0.5f * (float)0.5f) * num);
				//this.flatLightSource.setAlpha = new float?(0.25f * num);
				//this.flatLightSource.setRad = new float?(Mathf.Lerp(28f, 32f, 0.5f));
			}
			LightSource.RoomPos = Position;
			//this.flatLightSource.setPos = new Vector2?(this.placedObject.pos);
		}
	}
}
