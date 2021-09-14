using System;
using UnityEngine;
using Verse;


namespace Submunition
{
    public class SM_Graphic_Random : Graphic_Random
    {
		private const float PositionVariance = 0.25f;
		private const float SizeVariance = 0.25f;
		private const float SizeVarianceMin = 1f - SizeVariance;
		private const float SizeVarianceMax = 1f + SizeVariance;

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			Rand.PushState();
			Rand.Seed = thing.thingIDNumber.GetHashCode();
			float angle = (float)Rand.Range(0, 360);
			Vector3 s = new Vector3(Rand.Range(SizeVarianceMin, SizeVarianceMax) * this.drawSize.x, 0f, Rand.Range(SizeVarianceMin, SizeVarianceMax) * this.drawSize.y);
			Vector3 pos = loc + new Vector3(Rand.Range(-PositionVariance, PositionVariance), 0f, Rand.Range(-PositionVariance, PositionVariance));
			Rand.PopState();

			Graphic graphic;
			if (thing != null)
			{
				graphic = this.SubGraphicFor(thing);
			}
			else
			{
				graphic = this.subGraphics[0];
			}

			
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.AngleAxis(angle, Vector3.up), s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
		}
	}

	public class SM_Graphic_Flicker : Graphic_Random
	{
		private const float PositionVariance = 0.25f;
		private const float SizeVariance = 0.15f;
		private const float SizeVarianceMin = 1f - SizeVariance;
		private const float SizeVarianceMax = 1f + SizeVariance;

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			Rand.PushState();
			Rand.Seed = thing.thingIDNumber.GetHashCode();
			Vector3 s = new Vector3(Rand.Range(SizeVarianceMin, SizeVarianceMax) * this.drawSize.x, 0f, Rand.Range(SizeVarianceMin, SizeVarianceMax) * this.drawSize.y);
			Vector3 pos = loc + new Vector3(Rand.Range(-PositionVariance, PositionVariance), 0f, Rand.Range(-PositionVariance, PositionVariance));
			Rand.PopState();

			if (thingDef == null)
			{
				Log.ErrorOnce("Fire DrawWorker with null thingDef: " + loc, 3427324);
				return;
			}
			if (this.subGraphics == null)
			{
				Log.ErrorOnce("Graphic_Flicker has no subgraphics " + thingDef, 358773632);
				return;
			}
			int num = Find.TickManager.TicksGame;
			if (thing != null)
			{
				num += Mathf.Abs(thing.thingIDNumber ^ 8453458);
			}
			int num2 = num / 15;
			int num3 = Mathf.Abs(num2 ^ ((thing != null) ? thing.thingIDNumber : 0) * 391) % this.subGraphics.Length;
			
			if (num3 < 0 || num3 >= this.subGraphics.Length)
			{
				Log.ErrorOnce("Fire drawing out of range: " + num3, 7453435);
				num3 = 0;
			}
			
			Graphic graphic = this.subGraphics[num3];

			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.identity, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
		}
	}
}
