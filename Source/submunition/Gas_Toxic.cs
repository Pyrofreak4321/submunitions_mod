using System;
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
                    if (this.tickCounter % (7 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        MoteThrown moteThrown;
                        Vector3 loc = base.Position.ToVector3Shifted();
                        moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
                        moteThrown.Scale = Rand.Range(1.5f, 2.5f);
                        moteThrown.rotationRate = Rand.Range(-30f, 30f);
                        moteThrown.exactPosition = loc;
                        moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
                        GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                    if (this.tickCounter % (1 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        MoteThrown moteThrown;
                        Vector3 loc = base.Position.ToVector3Shifted();
                        moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_FireGlow, null);
                        moteThrown.Scale = Rand.Range(1.5f, 2.5f);
                        moteThrown.rotationRate = Rand.Range(-3f, 3f);
                        moteThrown.exactPosition = loc;
                        moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        moteThrown.SetVelocity((float)Rand.Range(0, 360), 0.12f);
                        GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                    if (this.tickCounter % (4 * toxicGasExtension.toxicGasSmokey) == 0)
                    {
                        MoteThrown moteThrown;
                        Vector3 loc = base.Position.ToVector3Shifted();
                        moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_MicroSparks, null);
                        moteThrown.Scale = Rand.Range(0.8f, 1.2f);
                        moteThrown.rotationRate = Rand.Range(-12f, 12f);
                        moteThrown.exactPosition = loc;
                        moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
                        moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
                        moteThrown.SetVelocity((float)Rand.Range(35, 45), 1.2f);
                        GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), base.Map, WipeMode.Vanish);
                    }
                }
                this.tickCounter++;
            }

            base.Tick();
        }

    }

    public class DamageWorker_Inc : DamageWorker_Flame
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn pawn = victim as Pawn;
            if (pawn != null)
            {
                victim.TryAttachFire(Rand.Range(0.5f, 1f));
            }

            return base.Apply(dinfo, victim);
        }

        public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
        {
            FireUtility.TryStartFireIn(c, explosion.Map, Rand.Range(0.5f, 1f));
            base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
        }
    }

    public class DamageWorker_Disolve_Riot : DamageWorker
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn pawn = victim as Pawn;
            if (pawn != null)
            {
                if (pawn.apparel != null)
                {
                    for (int i = 0; i < pawn.apparel.WornApparelCount; i++)
                    {
                        Thing ap = pawn.apparel.WornApparel[i];
                        if (ap != null)
                        {
                            ap.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dinfo.Amount, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
                        }

                    }
                }

                if(pawn.equipment != null)
                {
                    List<ThingWithComps> gear = pawn.equipment.AllEquipmentListForReading;
                    if (gear != null)
                    {
                        for (int i = 0; i < gear.Count; i++)
                        {
                            ThingWithComps ap = gear[i];
                            if (ap != null)
                            {
                                ap.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dinfo.Amount, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
                            }
                        }
                    }
                }

                if (pawn.inventory != null)
                {
                    List<Thing> gear = pawn.inventory.innerContainer.ToList();
                    if (gear != null)
                    {
                        for (int i = 0; i < gear.Count; i++)
                        {
                            Thing ap = gear[i];
                            if (ap != null)
                            {
                                ap.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dinfo.Amount, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
                            }
                        }
                    }
                }


                if (pawn.RaceProps.FleshType.defName == FleshTypeDefOf.Mechanoid.defName)
                {
                   return base.Apply(dinfo, pawn);
                }
            }

            return new DamageWorker.DamageResult();
        }
    }

    public class DamageWorker_Disolve_All : DamageWorker
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn pawn = victim as Pawn;
            if (pawn != null)
            {
                if (pawn.RaceProps.FleshType.defName != FleshTypeDefOf.Mechanoid.defName)
                {
                    List<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    BodyPartRecord part = parts.RandomElement();

                    DamageInfo damage = new DamageInfo(DamageDefOf.Burn, dinfo.Amount, 9999f, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                    DamageWorker.DamageResult damageResult = pawn.TakeDamage(damage);
                }
            }
            else if (victim != null)
            {
                DamageInfo damage = new DamageInfo(dinfo.Def, dinfo.Amount * 5, dinfo.ArmorPenetrationInt, dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.IntendedTarget);
                return base.Apply(damage, victim);
            }

            return new DamageWorker.DamageResult();
        }
    }

    public class DefAdvHediffExtension : DefModExtension
    {
        public DamageDef advHediffDamageDef;
        public float advHediffDamageStrength = 1.00f;
        public float advHediffDamageScale = 1.00f;
        public int advHediffDamageInterval = 60;
        public bool advHediffDamageRandomPart = false;
        public StatDef advHediffDamageResist;

        public HediffDef advHediffHediffToAdd;
        public int advHediffHediffInterval = 60;
        public float advHediffHediffStrength = .1f;
        public float advHediffHediffScale = 1.00f;
        public bool advHediffHediffRandomPart = false;
        public bool advHediffHediffWholeBody = false;
        public bool advHediffHediffBodyScaling = false;
        public StatDef advHediffHediffResist;


        public int advHediffSeverityInterval = -1;
        public float advHediffSeverityOffset = 0f;
        

        public HediffDef advHediffChainHediffToAdd;
        public float advHediffChainHediffStrength = .1f;
        public float advHediffChainHediffScale = 1f;
        public float advHediffChainHediffChance = 1f;
        public bool advHediffChainHediffRandomPart = false;
        public bool advHediffChainHediffWholeBody = false;
        public bool advHediffChainHediffMerge = true;
        public StatDef advHediffChainHediffResist;

        public bool advHediffTendPreventEffects = false;
        public float advHediffTendReducesSeverity = 0f;
        public float advHediffTendDificulty = 0f;
    }

    public class AdvHediff : HediffWithComps
    {
        private DefAdvHediffExtension advHediffExtension;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.def.HasModExtension<DefAdvHediffExtension>())
            {
                advHediffExtension = this.def.GetModExtension<DefAdvHediffExtension>();
            }
            else
            {
                advHediffExtension = new DefAdvHediffExtension();
            }

            if (advHediffExtension.advHediffChainHediffToAdd != null && Rand.Value <= advHediffExtension.advHediffChainHediffChance)
            {
                List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                BodyPartRecord part = parts.RandomElement();
                List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
                Hediff hediff = null;

                float num = advHediffExtension.advHediffChainHediffStrength;

                if (num == -1)
                {
                    num = this.Severity * advHediffExtension.advHediffChainHediffScale;
                }

                if (advHediffExtension.advHediffChainHediffResist != null)
                {
                    num *= this.pawn.GetStatValue(advHediffExtension.advHediffChainHediffResist, true);
                }

                if (advHediffExtension.advHediffHediffBodyScaling)
                {
                    num =  num / (this.pawn.BodySize * this.pawn.BodySize);
                }

                if (!advHediffExtension.advHediffChainHediffRandomPart)
                {
                    if (parts.Contains(this.Part))
                    {
                        part = this.Part;
                    }
                    else
                    {
                        part = null;
                    }
                }

                if (advHediffExtension.advHediffChainHediffWholeBody)
                {
                    hediff = this.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffChainHediffToAdd);
                    part = null;
                }
                else
                {
                    for (int h = 0; h < hediffs.Count; h++)
                    {
                        if (hediffs[h].def == advHediffExtension.advHediffChainHediffToAdd && hediffs[h].Part.def == part.def)
                        {
                            hediff = hediffs[h];
                        }
                    }
                }

                if (num > 0f)
                {
                    if (hediff != null && advHediffExtension.advHediffChainHediffMerge)
                    {
                        hediff.Severity += num;
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffChainHediffToAdd, this.pawn, part);
                        hediff.Severity = num;
                        this.pawn.health.AddHediff(hediff);
                    }
                }

            }

        }

        public override void Tick()
        {
            BodyPartRecord part = null;

            if (!this.IsTended() || !advHediffExtension.advHediffTendPreventEffects)
            {
                if (advHediffExtension.advHediffDamageDef != null)
                {
                    if (this.ageTicks % advHediffExtension.advHediffDamageInterval == 0)
                    {
                        List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                        
                        part = parts.RandomElement();

                        if (!advHediffExtension.advHediffDamageRandomPart)
                        {
                            if (parts.Contains(this.Part))
                            {
                                part = this.Part;
                            }
                            else
                            {
                                part = null;
                            }
                        }

                        float armorPenetration = 9999f;

                        float dmg = advHediffExtension.advHediffDamageStrength;

                        if (dmg == -1)
                        {
                            dmg = this.Severity * advHediffExtension.advHediffDamageScale;
                        }

                        if (advHediffExtension.advHediffDamageResist != null)
                        {
                            dmg *= this.pawn.GetStatValue(advHediffExtension.advHediffDamageResist, true);
                        }

                        if (dmg > 0f)
                        {
                            DamageInfo dinfo = new DamageInfo(advHediffExtension.advHediffDamageDef, dmg, armorPenetration, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                            DamageWorker.DamageResult damageResult = this.pawn.TakeDamage(dinfo);

                            BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(this.pawn, RulePackDefOf.DamageEvent_Fire, null);
                            Find.BattleLog.Add(battleLogEntry_DamageTaken);
                            damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                        }
                    }
                }
                if (advHediffExtension.advHediffHediffToAdd != null)
                {
                    if (this.ageTicks % advHediffExtension.advHediffHediffInterval == 0)
                    {
                        List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();

                        if (part == null)
                        {
                            part = parts.RandomElement();
                        }
                        List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
                        Hediff hediff = null;

                        float num = advHediffExtension.advHediffHediffStrength;

                        if (num == -1)
                        {
                            num = this.Severity * advHediffExtension.advHediffHediffScale;
                        }

                        if (advHediffExtension.advHediffHediffResist != null)
                        {
                            num *= this.pawn.GetStatValue(advHediffExtension.advHediffHediffResist, true);
                        }

                        if (!advHediffExtension.advHediffHediffRandomPart)
                        {
                            if (parts.Contains(this.Part))
                            {
                                part = this.Part;
                            }
                            else
                            {
                                part = null;
                            }
                        }

                        if (advHediffExtension.advHediffHediffWholeBody)
                        {
                            hediff = this.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffHediffToAdd);
                            part = null;
                        }
                        else
                        {
                            for (int h = 0; h < hediffs.Count; h++)
                            {
                                if (hediffs[h].def == advHediffExtension.advHediffHediffToAdd && hediffs[h].Part.def == part.def)
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
                                hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffHediffToAdd, this.pawn, part);
                                hediff.Severity = num;
                                this.pawn.health.AddHediff(hediff);
                            }
                        }
                    }

                }
            }

            if (advHediffExtension.advHediffSeverityInterval > 0)
            {
                if (this.ageTicks % advHediffExtension.advHediffSeverityInterval == 0)
                {
                    this.Severity += advHediffExtension.advHediffSeverityOffset;
                }
            }

            base.Tick();
        }

        public override void Tended(float quality, int batchPosition = 0)
        {
            if (advHediffExtension.advHediffTendReducesSeverity > 0)
            {
                float dificulty = advHediffExtension.advHediffTendDificulty;
                float num = quality * (1f - advHediffExtension.advHediffTendDificulty);

                if (num <= 0) num = 0.01f;

                if (num >= Rand.Value)
                {
                    if (batchPosition == 0 && this.pawn.Spawned)
                    {
                        MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "TextMote_TreatSuccess".Translate(num.ToStringPercent()), 6.5f);
                    }
                    this.Severity -= advHediffExtension.advHediffTendReducesSeverity;
                }
                else if (batchPosition == 0 && this.pawn.Spawned)
                {
                    MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "TextMote_TreatFailed".Translate(num.ToStringPercent()), 6.5f);
                }
            }
            else if (advHediffExtension.advHediffTendReducesSeverity < 0)
            {
                this.Severity -= quality;
            }

            base.Tended(quality, batchPosition);
        }

    }

    public class AdvHediff_Injury : Hediff_Injury
    {
        private DefAdvHediffExtension advHediffExtension;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.def.HasModExtension<DefAdvHediffExtension>())
            {
                advHediffExtension = this.def.GetModExtension<DefAdvHediffExtension>();
            }
            else
            {
                advHediffExtension = new DefAdvHediffExtension();
            }

            if (advHediffExtension.advHediffChainHediffToAdd != null && Rand.Value <= advHediffExtension.advHediffChainHediffChance)
            {
                List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                BodyPartRecord part = parts.RandomElement();
                List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
                Hediff hediff = null;

                float num = advHediffExtension.advHediffChainHediffStrength;

                if (num == -1)
                {
                    num = this.Severity * advHediffExtension.advHediffChainHediffScale;
                }

                if (advHediffExtension.advHediffChainHediffResist != null)
                {
                    num *= this.pawn.GetStatValue(advHediffExtension.advHediffChainHediffResist, true);
                }

                if (advHediffExtension.advHediffHediffBodyScaling)
                {
                    num = num / (this.pawn.BodySize * this.pawn.BodySize);
                }

                if (!advHediffExtension.advHediffChainHediffRandomPart)
                {
                    if (parts.Contains(this.Part))
                    {
                        part = this.Part;
                    }
                    else
                    {
                        part = null;
                    }
                }

                if (advHediffExtension.advHediffChainHediffWholeBody)
                {
                    hediff = this.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffChainHediffToAdd);
                    part = null;
                }
                else
                {
                    for (int h = 0; h < hediffs.Count; h++)
                    {
                        if (hediffs[h].def == advHediffExtension.advHediffChainHediffToAdd && hediffs[h].Part.def == part.def)
                        {
                            hediff = hediffs[h];
                        }
                    }
                }

                if (num > 0f)
                {
                    if (hediff != null && advHediffExtension.advHediffChainHediffMerge)
                    {
                        hediff.Severity += num;
                    }
                    else
                    {
                        hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffChainHediffToAdd, this.pawn, part);
                        hediff.Severity = num;
                        this.pawn.health.AddHediff(hediff);
                    }
                }

            }

        }

        public override void Tick()
        {
            BodyPartRecord part = null;

            if (!this.IsTended() || !advHediffExtension.advHediffTendPreventEffects)
            {
                if (advHediffExtension.advHediffDamageDef != null)
                {
                    if (this.ageTicks % advHediffExtension.advHediffDamageInterval == 0)
                    {
                        List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                        part = parts.RandomElement();
                       
                        if (!advHediffExtension.advHediffDamageRandomPart)
                        {
                            if (parts.Contains(this.Part))
                            {
                                part = this.Part;
                            }
                            else
                            {
                                part = null;
                            }
                        }

                        float armorPenetration = 9999f;

                        float dmg = advHediffExtension.advHediffDamageStrength;

                        if (dmg == -1)
                        {
                            dmg = this.Severity * advHediffExtension.advHediffDamageScale;
                        }

                        if (advHediffExtension.advHediffDamageResist != null)
                        {
                            dmg *= this.pawn.GetStatValue(advHediffExtension.advHediffDamageResist, true);
                        }

                        if (dmg > 0f)
                        {
                            DamageInfo dinfo = new DamageInfo(advHediffExtension.advHediffDamageDef, dmg, armorPenetration, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                            DamageWorker.DamageResult damageResult = this.pawn.TakeDamage(dinfo);

                            BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(this.pawn, RulePackDefOf.DamageEvent_Fire, null);
                            Find.BattleLog.Add(battleLogEntry_DamageTaken);
                            damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                        }
                    }
                }
                if (advHediffExtension.advHediffHediffToAdd != null)
                {
                    if (this.ageTicks % advHediffExtension.advHediffHediffInterval == 0)
                    {
                        List<BodyPartRecord> parts = this.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();

                        if (part == null)
                        {
                            part = parts.RandomElement();
                        }
                        List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
                        Hediff hediff = null;

                        float num = advHediffExtension.advHediffHediffStrength;

                        if (num == -1)
                        {
                            num = this.Severity * advHediffExtension.advHediffHediffScale;
                        }

                        if (advHediffExtension.advHediffHediffResist != null)
                        {
                            num *= this.pawn.GetStatValue(advHediffExtension.advHediffHediffResist, true);
                        }

                        if (!advHediffExtension.advHediffHediffRandomPart)
                        {
                            if (parts.Contains(this.Part))
                            {
                                part = this.Part;
                            }
                            else
                            {
                                part = null;
                            }
                        }

                        if (advHediffExtension.advHediffHediffWholeBody)
                        {
                            hediff = this.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffHediffToAdd);
                            part = null;
                        }
                        else
                        {
                            for (int h = 0; h < hediffs.Count; h++)
                            {
                                if (hediffs[h].def == advHediffExtension.advHediffHediffToAdd && hediffs[h].Part.def == part.def)
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
                                hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffHediffToAdd, this.pawn, part);
                                hediff.Severity = num;
                                this.pawn.health.AddHediff(hediff);
                            }
                        }
                    }

                }
            }

            if(advHediffExtension.advHediffSeverityInterval > 0 ) 
            {
                if(this.ageTicks % advHediffExtension.advHediffSeverityInterval == 0)
                {
                    this.Severity += advHediffExtension.advHediffSeverityOffset;
                }
            }

            base.Tick();
        }

        public override void Tended(float quality, int batchPosition = 0)
        {
            if (advHediffExtension.advHediffTendReducesSeverity > 0)
            {
                float dificulty = advHediffExtension.advHediffTendDificulty;
                float num = quality * (1f - advHediffExtension.advHediffTendDificulty);

                if (num <= 0) num = 0.01f;

                if (num >= Rand.Value)
                {
                    if (batchPosition == 0 && this.pawn.Spawned)
                    {
                        MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "TextMote_TreatSuccess".Translate(num.ToStringPercent()), 6.5f);
                    }
                    this.Severity -= advHediffExtension.advHediffTendReducesSeverity;
                }
                else if (batchPosition == 0 && this.pawn.Spawned)
                {
                    MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "TextMote_TreatFailed".Translate(num.ToStringPercent()), 6.5f);
                }
            }
            else if (advHediffExtension.advHediffTendReducesSeverity < 0)
            {
                this.Severity -= quality;
            }

            base.Tended(quality, batchPosition);
        }

    }
}