using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;


namespace Submunition
{

    public class DamageWorker_Inc : DamageWorker_Flame
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            victim.TryAttachFire(Rand.Range(0.5f, 1f));

            return base.Apply(dinfo, victim);
        }

        public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
        {
            FireUtility.TryStartFireIn(c, explosion.Map, Rand.Range(0.5f, 1f));
            base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
        }
    }

    public class DamageWorker_Inc_noShake : DamageWorker_Inc
    {
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            if (this.def.explosionHeatEnergyPerCell > 1.401298E-45f)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, this.def.explosionHeatEnergyPerCell * (float)cellsToAffect.Count);
            }
            MoteMaker.MakeStaticMote(explosion.Position, explosion.Map, ThingDefOf.Mote_ExplosionFlash, explosion.radius * 6f);

            this.ExplosionVisualEffectCenter(explosion);
        }
    }

    public class DamageWorker_noShake : DamageWorker
    {
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            if (this.def.explosionHeatEnergyPerCell > 1.401298E-45f)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, this.def.explosionHeatEnergyPerCell * (float)cellsToAffect.Count);
            }
            MoteMaker.MakeStaticMote(explosion.Position, explosion.Map, ThingDefOf.Mote_ExplosionFlash, explosion.radius * 6f);

            this.ExplosionVisualEffectCenter(explosion);
        }
    }

    public class DamageWorker_AddInjury_noShake : DamageWorker_AddInjury
    {
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            if (this.def.explosionHeatEnergyPerCell > 1.401298E-45f)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, this.def.explosionHeatEnergyPerCell * (float)cellsToAffect.Count);
            }
            MoteMaker.MakeStaticMote(explosion.Position, explosion.Map, ThingDefOf.Mote_ExplosionFlash, explosion.radius * 6f);

            this.ExplosionVisualEffectCenter(explosion);
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

                if (pawn.equipment != null)
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


                if (pawn.RaceProps.IsMechanoid)
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
                if (!pawn.RaceProps.IsMechanoid)
                {
                    List<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    BodyPartRecord part = parts.RandomElement();

                    DamageInfo damage = new DamageInfo(DamageDefOf.Burn, dinfo.Amount, 9999f, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                    DamageWorker.DamageResult damageResult = pawn.TakeDamage(damage);
                    //if (pawn.Dead)
                    //{
                    //    Map map = Find.CurrentMap;
                    //    PawnKindDef varKind = PawnKindDef.Named("Thrumbo");
                    //    PawnGenerationRequest request = new PawnGenerationRequest(varKind, null, PawnGenerationContext.NonPlayer, map.Tile);
                    //    Pawn varPawn = PawnGenerator.GeneratePawn(request);
                    //    GenSpawn.Spawn(varPawn, pawn.Position, map, Rot4.Random, WipeMode.Vanish, false);

                    //    varPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, null, false);
                    //}                    
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

        public bool advHediffChainAffectFlesh = true;
        public bool advHediffChainAffectMech = false;

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



    public class AdvHediff_worker
    {
        DefAdvHediffExtension advHediffExtension;

        public void PostAdd(HediffWithComps main, DamageInfo? dinfo)
        {
            if (main.def.HasModExtension<DefAdvHediffExtension>())
            {
                advHediffExtension = main.def.GetModExtension<DefAdvHediffExtension>();
            }
            else
            {
                advHediffExtension = new DefAdvHediffExtension();
            }

            if (advHediffExtension.advHediffChainHediffToAdd != null && Rand.Value <= advHediffExtension.advHediffChainHediffChance &&
                ((main.pawn.RaceProps.IsMechanoid && advHediffExtension.advHediffChainAffectMech) || (!main.pawn.RaceProps.IsMechanoid && advHediffExtension.advHediffChainAffectFlesh)))
            {
                List<BodyPartRecord> parts = main.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();
                BodyPartRecord part = parts.RandomElement();
                List<Hediff> hediffs = main.pawn.health.hediffSet.hediffs;
                Hediff hediff = null;

                float num = advHediffExtension.advHediffChainHediffStrength;

                if (num == -1)
                {
                    num = main.Severity * advHediffExtension.advHediffChainHediffScale;
                }

                if (advHediffExtension.advHediffChainHediffResist != null)
                {
                    num *= main.pawn.GetStatValue(advHediffExtension.advHediffChainHediffResist, true);
                }

                if (advHediffExtension.advHediffHediffBodyScaling)
                {
                    num = num / (main.pawn.BodySize * main.pawn.BodySize);
                }

                if (!advHediffExtension.advHediffChainHediffRandomPart)
                {
                    if (parts.Contains(main.Part))
                    {
                        part = main.Part;
                    }
                    else
                    {
                        part = null;
                    }
                }

                if (advHediffExtension.advHediffChainHediffWholeBody)
                {
                    hediff = main.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffChainHediffToAdd);
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
                        hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffChainHediffToAdd, main.pawn, part);
                        hediff.Severity = num;
                        main.pawn.health.AddHediff(hediff);
                    }
                }
            }
        }

        public void Tick(HediffWithComps main)
        {
            BodyPartRecord part = null;

            if (!main.IsTended() || !advHediffExtension.advHediffTendPreventEffects)
            {
                if (advHediffExtension.advHediffDamageDef != null)
                {
                    if (main.ageTicks % advHediffExtension.advHediffDamageInterval == 0)
                    {
                        List<BodyPartRecord> parts = main.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();

                        part = parts.RandomElement();

                        if (!advHediffExtension.advHediffDamageRandomPart)
                        {
                            if (parts.Contains(main.Part))
                            {
                                part = main.Part;
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
                            dmg = main.Severity * advHediffExtension.advHediffDamageScale;
                        }

                        if (advHediffExtension.advHediffDamageResist != null)
                        {
                            dmg *= main.pawn.GetStatValue(advHediffExtension.advHediffDamageResist, true);
                        }

                        if (dmg > 0f)
                        {
                            DamageInfo dinfo = new DamageInfo(advHediffExtension.advHediffDamageDef, dmg, armorPenetration, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                            DamageWorker.DamageResult damageResult = main.pawn.TakeDamage(dinfo);

                            BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(main.pawn, RulePackDefOf.DamageEvent_Fire, null);
                            Find.BattleLog.Add(battleLogEntry_DamageTaken);
                            damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                        }
                    }
                }
                if (advHediffExtension.advHediffHediffToAdd != null)
                {
                    if (main.ageTicks % advHediffExtension.advHediffHediffInterval == 0)
                    {
                        List<BodyPartRecord> parts = main.pawn.health.hediffSet.GetNotMissingParts().Where(x => x.coverageAbs > 0f).ToList();

                        if (part == null)
                        {
                            part = parts.RandomElement();
                        }
                        List<Hediff> hediffs = main.pawn.health.hediffSet.hediffs;
                        Hediff hediff = null;

                        float num = advHediffExtension.advHediffHediffStrength;

                        if (num == -1)
                        {
                            num = main.Severity * advHediffExtension.advHediffHediffScale;
                        }

                        if (advHediffExtension.advHediffHediffResist != null)
                        {
                            num *= main.pawn.GetStatValue(advHediffExtension.advHediffHediffResist, true);
                        }

                        if (!advHediffExtension.advHediffHediffRandomPart)
                        {
                            if (parts.Contains(main.Part))
                            {
                                part = main.Part;
                            }
                            else
                            {
                                part = null;
                            }
                        }

                        if (advHediffExtension.advHediffHediffWholeBody)
                        {
                            hediff = main.pawn.health.hediffSet.GetFirstHediffOfDef(advHediffExtension.advHediffHediffToAdd);
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
                                hediff = HediffMaker.MakeHediff(advHediffExtension.advHediffHediffToAdd, main.pawn, part);
                                hediff.Severity = num;
                                main.pawn.health.AddHediff(hediff);
                            }
                        }
                    }

                }
            }

            if (advHediffExtension.advHediffSeverityInterval > 0)
            {
                if (main.ageTicks % advHediffExtension.advHediffSeverityInterval == 0)
                {
                    main.Severity += advHediffExtension.advHediffSeverityOffset;
                }
            }
        }

        public void Tended_NewTemp(HediffWithComps main, float quality, float maxQuality, int batchPosition = 0)
        {
            if (advHediffExtension.advHediffTendReducesSeverity > 0)
            {
                float dificulty = advHediffExtension.advHediffTendDificulty;
                float num = quality * (1f - advHediffExtension.advHediffTendDificulty);

                if (num <= 0) num = 0.01f;

                if (num >= Rand.Value)
                {
                    if (batchPosition == 0 && main.pawn.Spawned)
                    {
                        MoteMaker.ThrowText(main.pawn.DrawPos, main.pawn.Map, "TextMote_TreatSuccess".Translate(num.ToStringPercent()), 6.5f);
                    }
                    main.Severity -= advHediffExtension.advHediffTendReducesSeverity;
                }
                else if (batchPosition == 0 && main.pawn.Spawned)
                {
                    MoteMaker.ThrowText(main.pawn.DrawPos, main.pawn.Map, "TextMote_TreatFailed".Translate(num.ToStringPercent()), 6.5f);
                }
            }
            else if (advHediffExtension.advHediffTendReducesSeverity < 0)
            {
                main.Severity -= (quality*Math.Abs(advHediffExtension.advHediffTendReducesSeverity));
            }
        }
    }

    public class AdvHediff : HediffWithComps
    {
        AdvHediff_worker worker;

        public override void PostAdd(DamageInfo? dinfo)
        {
            worker = new AdvHediff_worker();
            base.PostAdd(dinfo);
            worker.PostAdd(this, dinfo);
        }

        public override void Tick()
        {
            worker.Tick(this);
            base.Tick();
        }

        public override void Tended_NewTemp(float quality, float maxQuality, int batchPosition = 0)
        {
            worker.Tended_NewTemp(this, quality, maxQuality, batchPosition);
            base.Tended_NewTemp(quality, maxQuality, batchPosition);
        }

    }

    public class AdvHediff_Injury : Hediff_Injury
    {
        AdvHediff_worker worker;

        public override void PostAdd(DamageInfo? dinfo)
        {
            worker = new AdvHediff_worker();
            base.PostAdd(dinfo);
            worker.PostAdd(this, dinfo);
        }

        public override void Tick()
        {
            worker.Tick(this);
            base.Tick();
        }

        public override void Tended_NewTemp(float quality, float maxQuality, int batchPosition = 0)
        {
            worker.Tended_NewTemp(this, quality, maxQuality, batchPosition);
            base.Tended_NewTemp(quality, maxQuality, batchPosition);
        }

    }
}
