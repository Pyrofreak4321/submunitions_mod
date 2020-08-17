using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Submunition
{
    public class Building_TrapExplosive_Unstable : Building_Trap
    {
        private List<Pawn> touchingPawns = new List<Pawn>();

        protected override float SpringChance(Pawn p)
        {
            return 999f;
        }

        public override ushort PathFindCostFor(Pawn p)
        {
            return 0;
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            return 0;
        }
        public override bool IsDangerousFor(Pawn p)
        {
            return false;
        }

        public override void Tick()
        {
            if (base.Spawned)
            {
                List<Thing> thingList = base.Position.GetThingList(base.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Pawn pawn = thingList[i] as Pawn;
                    if (pawn != null && !this.touchingPawns.Contains(pawn))
                    {
                        this.touchingPawns.Add(pawn);
                        base.Spring(pawn);
                    }
                }
                for (int j = 0; j < this.touchingPawns.Count; j++)
                {
                    Pawn pawn2 = this.touchingPawns[j];
                    if (!pawn2.Spawned || pawn2.Position != base.Position)
                    {
                        this.touchingPawns.Remove(pawn2);
                    }
                }
            }
            base.Tick();
        }

        protected override void SpringSub(Pawn p)
        {
            base.GetComp<CompExplosive>().StartWick(null);
        }
    }

    public class Building_TrapExplosive_Proximity : Building_Trap
    {
        private List<Pawn> touchingPawns = new List<Pawn>();

        protected override void SpringSub(Pawn p)
        {
            base.GetComp<CompExplosive>().StartWick(null);
        }

        private void CheckSpring(Pawn p)
        {
            if (Rand.Chance(this.SpringChance(p)))
            {
                Map map = base.Map;
                this.Spring(p);
                if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
                {
                    Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(p.LabelShort, p), "LetterFriendlyTrapSprung".Translate(p.LabelShort, p), LetterDefOf.NegativeEvent, new TargetInfo(base.Position, map, false), null, null);
                }
            }
        }

        public override void Tick()
        {
            if (base.Spawned)
            {
                List<IntVec3> cellRect = (List<IntVec3>)GenRadial.RadialCellsAround(base.Position, 3, true);
                List<Thing> thingList = new List<Thing>();

                for (int c = 0; c < cellRect.Count; c++)
                {
                    thingList = thingList.Concat(cellRect[c].GetThingList(base.Map)).ToList();
                }

                for (int i = 0; i < thingList.Count; i++)
                {
                    Pawn pawn = thingList[i] as Pawn;
                    if (pawn != null && !this.touchingPawns.Contains(pawn))
                    {
                        this.touchingPawns.Add(pawn);
                        this.CheckSpring(pawn);
                    }
                }
                for (int j = 0; j < this.touchingPawns.Count; j++)
                {
                    Pawn pawn2 = this.touchingPawns[j];
                    if (!pawn2.Spawned || pawn2.Position != base.Position)
                    {
                        this.touchingPawns.Remove(pawn2);
                    }
                }
            }
            base.Tick();
        }
    }

    public class DefAdvancedTurret : DefModExtension
    {
        public int speedBoost = 10;
    }

}