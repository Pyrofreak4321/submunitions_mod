﻿using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Submunition
{
    public class DefToxicGasExtension : DefModExtension
    {
        public DamageDef toxicGasDamageDef;
        public float toxicGasDamageStrength = 1.00f;
        public float toxicGasDamageChance = 1.00f;
        public bool toxicGasDamageEnviro = false;
        public StatDef toxicGasDamageResist;

        public HediffDef toxicGasHediffToAdd;
        public float toxicGasHediffChance = 0.50f;
        public float toxicGasHediffStrength = 0.01f;
        public StatDef toxicGasHediffResist;
        public bool toxicGasHediffBodyScaling = false;

        public bool toxicGasWholeBody = false;

        public bool toxicGasAffectFlesh = true;
        public bool toxicGasAffectMech = false;

        public ThingDef toxicGasMote = null;
        public float toxicGasMoteRate = 0f;

        public float toxicGasSpreadChance = 0f;
        public float toxicGasSpreadRate = 0f;
        public float toxicGasSpreadBonusMin = 0f;
        public float toxicGasSpreadBonusMax = 0f;

        public int toxicGasSmokey = 0;
        public bool toxicGasHot = false;
        public bool toxicGasArea = false;
    }

    public class Gas_Toxic : Gas
    {
        private List<Pawn> touchingPawns = new List<Pawn>();
        private DefToxicGasExtension toxicGasExtension;

        private DamageDef toxicGasDamageDef;
        private HediffDef toxicGasHediffToAdd;

        private int tickCounter = (int)Rand.Range(1f, 100f);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (this.def.HasModExtension<DefToxicGasExtension>())
            {
                toxicGasExtension = this.def.GetModExtension<DefToxicGasExtension>();
            }
            else
            {
                toxicGasExtension = new DefToxicGasExtension();
            }

            toxicGasDamageDef = toxicGasExtension.toxicGasDamageDef;
            toxicGasHediffToAdd = toxicGasExtension.toxicGasHediffToAdd;
        }

        public override void Tick()
        {
            if (base.Spawned)
            {
                if (this.tickCounter % 10 == 0)
                {
                }
                if (this.tickCounter % 100 == 0)
                {
                    IntVec3 p = base.Position;
                    List<Thing> thingList = p.GetThingList(base.Map);
                    if (toxicGasExtension.toxicGasArea)
                    {
                        p.x++;
                        if (CellRect.WholeMap(base.Map).Contains(p))
                            thingList = thingList.Concat(p.GetThingList(base.Map)).ToList();
                        p.z++;
                        p.x--;
                        if (CellRect.WholeMap(base.Map).Contains(p))
                            thingList = thingList.Concat(p.GetThingList(base.Map)).ToList();
                        p.z--;
                        p.x--;
                        if (CellRect.WholeMap(base.Map).Contains(p))
                            thingList = thingList.Concat(p.GetThingList(base.Map)).ToList();
                        p.z--;
                        p.x++;
                        if (CellRect.WholeMap(base.Map).Contains(p))
                            thingList = thingList.Concat(p.GetThingList(base.Map)).ToList();
                    }

                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Pawn pawn = thingList[i] as Pawn;
                        if (pawn != null)
                        {
                           

                            if ((pawn.RaceProps.IsMechanoid && toxicGasExtension.toxicGasAffectMech) || (!pawn.RaceProps.IsMechanoid && toxicGasExtension.toxicGasAffectFlesh))
                            {
                                var rand = Rand.Value;
                                List<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                                BodyPartRecord part = parts.RandomElement();
                                Hediff hediff = null;
                                
                                if (this.toxicGasHediffToAdd != null && rand <= toxicGasExtension.toxicGasHediffChance)
                                {
                                    float num = toxicGasExtension.toxicGasHediffStrength;
                                    if (toxicGasExtension.toxicGasHediffResist != null)
                                    {
                                        num *= pawn.GetStatValue(toxicGasExtension.toxicGasHediffResist, true);
                                    }

                                    Log.Message("val - " + pawn.GetStatValue(toxicGasExtension.toxicGasHediffResist, true));
                                    
                                    if (toxicGasExtension.toxicGasHediffBodyScaling)
                                    {
                                        num = num / (pawn.BodySize * pawn.BodySize);
                                    }

                                    if (toxicGasExtension.toxicGasWholeBody)
                                    {
                                        hediff = pawn.health.hediffSet.GetFirstHediffOfDef(this.toxicGasHediffToAdd);
                                        part = null;
                                    }
                                    else
                                    {
                                        for (int h = 0; h < hediffs.Count; h++)
                                        {
                                            if (hediffs[h].def == this.toxicGasHediffToAdd && hediffs[h].Part.def == part.def)
                                            {
                                                hediff = hediffs[h];
                                            }
                                        }
                                    }
                                    if (num > 0f)
                                    {
                                        if (hediff != null)
                                        {
                                            hediff.Severity += num;
                                        }
                                        else
                                        {
                                            hediff = HediffMaker.MakeHediff(this.toxicGasHediffToAdd, pawn, part);
                                            hediff.Severity = num;
                                            pawn.health.AddHediff(hediff);
                                        }
                                    }
                                }

                                if (this.toxicGasDamageDef != null && rand <= toxicGasExtension.toxicGasDamageChance)
                                {
                                    float armorPenetration = 9999f;

                                    float dmg = toxicGasExtension.toxicGasDamageStrength;
                                    if (toxicGasExtension.toxicGasDamageResist != null)
                                    {
                                        dmg *= pawn.GetStatValue(toxicGasExtension.toxicGasDamageResist, true);
                                    }

                                    if (dmg > 0f)
                                    {
                                        DamageInfo dinfo = new DamageInfo(this.toxicGasDamageDef, dmg, armorPenetration, -1f, this, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                                        DamageWorker.DamageResult damageResult = pawn.TakeDamage(dinfo);

                                        BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire, null);
                                        Find.BattleLog.Add(battleLogEntry_DamageTaken);
                                        damageResult.AssociateWithLog(battleLogEntry_DamageTaken);

                                        if (toxicGasExtension.toxicGasHot)
                                        {
                                            pawn.TryAttachFire(Rand.Range(0.15f, 0.25f));
                                        }
                                    }
                                }
                            }
                        }
                        else if (toxicGasExtension.toxicGasDamageEnviro)
                        {
                            var rand = Rand.Value;
                            if (this.toxicGasDamageDef != null && rand <= toxicGasExtension.toxicGasDamageChance)
                            {
                                DamageInfo dinfo = new DamageInfo(this.toxicGasDamageDef, toxicGasExtension.toxicGasDamageStrength, 9999f, -1f, this, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                                DamageWorker.DamageResult damageResult = thingList[i].TakeDamage(dinfo);
                            }
                        }
                    }
                    if (toxicGasExtension.toxicGasHot)
                    {
                        FireUtility.TryStartFireIn(base.Position, base.Map, Rand.Range(0.25f, 1f));
                    }
                }

                if (toxicGasExtension.toxicGasMoteRate > 0 && this.tickCounter % toxicGasExtension.toxicGasMoteRate == 0)
                {
                    if (toxicGasExtension.toxicGasMote != null)
                    {
                        MoteThrown moteThrown;
                        Vector3 loc = base.Position.ToVector3Shifted();
                        moteThrown = (MoteThrown)ThingMaker.MakeThing(toxicGasExtension.toxicGasMote, null);
                        moteThrown.Scale = Rand.Range(1f, 2f);
                        moteThrown.rotationRate = Rand.Range(45f, 90f) * Rand.Sign;
                        moteThrown.exactPosition = loc;
                        moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        moteThrown.SetVelocity(0f, 0f);
                        GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                }
                if (toxicGasExtension.toxicGasSpreadRate > 0 && this.tickCounter % toxicGasExtension.toxicGasSpreadRate == 0)
                {
                    if (Rand.Value <= toxicGasExtension.toxicGasSpreadChance)
                    {
                        IntVec3 p = base.Position;
                        p.x += (int)Rand.Range(-2f, 2f);
                        p.z += (int)Rand.Range(-2f, 2f);

                        if (p.Walkable(base.Map) && p.GetGas(base.Map) == null)
                        {
                            Thing thing = ThingMaker.MakeThing(this.def, null);
                            thing.stackCount = 1;
                            GenSpawn.Spawn(thing, p, base.Map, WipeMode.Vanish);
                            Gas g = thing as Gas;
                            g.destroyTick = this.destroyTick + (int)(Rand.Range(toxicGasExtension.toxicGasSpreadBonusMin,toxicGasExtension.toxicGasSpreadBonusMax));
                        }
                    }
                }

                if (toxicGasExtension.toxicGasSmokey > 0)
                {
                    if (this.tickCounter % (8 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        Vector3 loc = base.Position.ToVector3Shifted();
                        loc -= new Vector3(0.5f, 0f, 0.5f);
                        loc += new Vector3(Rand.Value, 0f, Rand.Value);

                        FleckMaker.ThrowSmoke(loc, base.Map, Rand.Range(0.25f, 1.5f));

                        //MoteThrown moteThrown;
                        //moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
                        //moteThrown.Scale = Rand.Range(1.5f, 2.5f);
                        //moteThrown.rotationRate = Rand.Range(-30f, 30f);
                        //moteThrown.exactPosition = loc;
                        //moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        //moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        //moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
                        //GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                    if (this.tickCounter % (10 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        Vector3 loc = base.Position.ToVector3Shifted();
                        loc -= new Vector3(0.5f, 0f, 0.5f);
                        loc += new Vector3(Rand.Value, 0f, Rand.Value);

                        FleckMaker.ThrowFireGlow(loc, base.Map, Rand.Range(0.25f, 1.5f));

                        //MoteThrown moteThrown;
                        //moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_FireGlow, null);
                        //moteThrown.Scale = Rand.Range(1.5f, 2.5f);
                        //moteThrown.rotationRate = Rand.Range(-3f, 3f);
                        //moteThrown.exactPosition = loc;
                        //moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        //moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        //moteThrown.SetVelocity((float)Rand.Range(0, 360), 0.12f);
                        //GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                    if (this.tickCounter % (6 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        Vector3 loc = base.Position.ToVector3Shifted();
                        loc -= new Vector3(0.5f, 0f, 0.5f);
                        loc += new Vector3(Rand.Value, 0f, Rand.Value);

                        FleckMaker.ThrowMicroSparks(loc, base.Map);

                        //MoteThrown moteThrown;
                        //moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_MicroSparks, null);
                        //moteThrown.Scale = Rand.Range(0.8f, 1.2f);
                        //moteThrown.rotationRate = Rand.Range(-12f, 12f);
                        //moteThrown.exactPosition = loc;
                        //moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        //moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        //moteThrown.SetVelocity((float)Rand.Range(35, 45), 1.2f);
                        //GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                }
                this.tickCounter++;
            }

            base.Tick();
        }

    }
}