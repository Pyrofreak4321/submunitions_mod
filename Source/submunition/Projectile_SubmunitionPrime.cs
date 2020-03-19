using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Submunition
{
    public class DefSubmunitionExtension : DefModExtension
    {
        public int submunitionCount = 0;
        public float submunitionMultiplier = 0.5f;
        public float submunitionExplosionRadius = 3f;

        public int submunitionDelay = 0;
        public int submunitionDuration = 0;
        public int submunitionInterval = 0;
        public int submunitionPreDetonation = 0;
        public bool submunitionDestroyOnImpact = false;

        public float submunitionPopSize = 0;

        public DamageDef submunitionDamageDef;// = DamageDefOf.Bomb;
        public int submunitionDamageAmount = -1;
        public SoundDef submunitionSoundExplode;// = DamageDefOf.Bomb.soundExplosion;
        public float submunitionSpawnThingChance = 0f;
        public ThingDef submunitionSpawnThingDef;

        public bool submunitionFragment = false;
        public ThingDef submunitionFragmentThingDef;
    }

    public class Projectile_Submunition : Projectile
    {
        private DefSubmunitionExtension extension;
        private int ticksToDetonation = 0;
        private int ticksToSubDetonation = 0;
        private int ticksOfRain = 0;
        private int preDetonation = 0;
        private bool destroyOnImpact = false;
        private bool detonated = false;

        private int submunitionDelay = 0;
        private int submunitionInterval = 1;
        private int submunitionDuration = 0;

        private bool initalBlast = true;
        private CellRect wholemap;
        private RoofGrid roofGrid;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.def.HasModExtension<DefSubmunitionExtension>())
            {
                extension = this.def.GetModExtension<DefSubmunitionExtension>();
            }
            else
            {
                extension = new DefSubmunitionExtension();
            }
            this.preDetonation = this.extension.submunitionPreDetonation;
            this.destroyOnImpact = this.extension.submunitionDestroyOnImpact;
        }

        public override void Tick()
        {
            base.Tick();

            if(this.ticksToImpact < this.preDetonation && this.preDetonation > 0)
            {
                this.Detonate();
            }


            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
            if (this.ticksToSubDetonation > 0)
            {
                this.ticksToSubDetonation--;
                if (this.ticksToSubDetonation <= 0)
                {
                    this.ticksOfRain = this.submunitionDuration;
                }
            }
            if (this.ticksOfRain > 0)
            {
                this.ticksOfRain--;
                if (this.ticksOfRain % this.submunitionInterval == 0)
                {
                    this.Explode();
                }
            }
        }

        protected override void Impact(Thing hitThing)
        {
            this.Detonate();
            if (this.destroyOnImpact)
            {
                this.landed = true;
                this.Destroy(DestroyMode.Vanish);
            }
        }

        protected virtual void Detonate()
        {
            if (!this.detonated)
            {
                this.detonated = true;

                this.submunitionInterval = this.extension.submunitionInterval;
                this.submunitionDelay = this.extension.submunitionDelay;
                this.submunitionDuration = this.extension.submunitionDuration;

                this.wholemap = CellRect.WholeMap(base.Map);
                this.roofGrid = new RoofGrid(base.Map);
                this.ticksToDetonation = this.def.projectile.explosionDelay;
                this.ticksOfRain = 0;

                if (this.def.projectile.explosionDelay == 0)
                {
                    this.Explode();
                }
            }
        }

        protected virtual void Explode()
        {
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher.Faction);

            Map map = base.Map;
            if (this.def.projectile.explosionEffect != null)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(base.ExactPosition.ToIntVec3(), map, false), new TargetInfo(base.ExactPosition.ToIntVec3(), map, false));
                effecter.Cleanup();
            }
            IntVec3 position = base.ExactPosition.ToIntVec3();
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            DamageDef damageDef = this.def.projectile.damageDef;
            Thing launcher = this.launcher;
            int damageAmount = base.DamageAmount;
            float armorPenetration = base.ArmorPenetration;
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            Thing thing = this.intendedTarget.Thing;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            float popSize = 2f;

            IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.ExactPosition.ToIntVec3(), explosionRadius, true);

            if (this.initalBlast)
            {
                if(this.extension.submunitionPopSize > 0)
                {
                    popSize = this.extension.submunitionPopSize;
                }
                this.ticksToSubDetonation = this.submunitionDelay;

                GenExplosion.DoExplosion(position, map2, popSize, damageDef,
                    launcher, damageAmount, armorPenetration, soundExplode, equipmentDef,
                    def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance,
                    postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors,
                    preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance,
                    this.def.projectile.preExplosionSpawnThingCount,
                    this.def.projectile.explosionChanceToStartFire, this.def.projectile.explosionDamageFalloff);

                this.initalBlast = false;
            }

            if(this.ticksToSubDetonation <= 0)
            {
                int submunitionCount;
                float submunitionMultiplier;
                float submunitionExplosionRadius;
                DamageDef submunitionDamageDef;
                SoundDef submunitionSoundExplode;
                float submunitionSpawnThingChance;
                ThingDef submunitionSpawnThingDef;
                int submunitionDamageAmount;

                submunitionCount = this.extension.submunitionCount;
                submunitionMultiplier = this.extension.submunitionMultiplier;
                submunitionExplosionRadius = this.extension.submunitionExplosionRadius;
                submunitionSoundExplode = this.extension.submunitionSoundExplode;
                submunitionDamageDef = this.extension.submunitionDamageDef;
                submunitionSpawnThingChance = this.extension.submunitionSpawnThingChance;
                submunitionSpawnThingDef = this.extension.submunitionSpawnThingDef;
                submunitionDamageAmount = this.extension.submunitionDamageAmount;

                if (submunitionDamageDef == null)
                {
                    submunitionDamageDef = DefDatabase<DamageDef>.GetNamed("Bomb");
                }
                if (submunitionSoundExplode == null)
                {
                    submunitionSoundExplode = submunitionDamageDef.soundExplosion;
                }

                if (submunitionDamageAmount == -1)
                {
                    submunitionDamageAmount = damageAmount;
                }

                if (submunitionCount == 0)
                {
                    submunitionCount = (int)Math.Floor(submunitionMultiplier * explosionRadius);
                }

                if (this.extension.submunitionFragment && this.extension.submunitionFragmentThingDef != null)
                {
                    cellRect = GenRadial.RadialCellsAround(base.destination.ToIntVec3(), explosionRadius, true);

                    for (int i = 0; i < (submunitionCount); i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement();
                        if (this.wholemap.Contains(randomCell))
                        {
                            Projectile frag = (Projectile)GenSpawn.Spawn(this.extension.submunitionFragmentThingDef, base.Position, base.Map, WipeMode.Vanish);
                            frag.Launch(this.launcher, randomCell, this.intendedTarget, this.HitFlags, null);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < (submunitionCount); i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement(); //RandomCell;

                        bool blocked = false;
                        if (this.def.projectile.flyOverhead)
                        {
                            RoofDef roofDef = base.Map.roofGrid.RoofAt(randomCell);
                            if (roofDef != null)
                            {
                                if (roofDef.isThickRoof)
                                {
                                    blocked = true;
                                }
                                if (randomCell.GetEdifice(base.Map) == null || randomCell.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                                {
                                    RoofCollapserImmediate.DropRoofInCells(randomCell, base.Map, null);
                                }
                            }
                        }

                        if (this.wholemap.Contains(randomCell) && !blocked)
                        {
                            GenExplosion.DoExplosion(randomCell, map, submunitionExplosionRadius, submunitionDamageDef, launcher, submunitionDamageAmount,
                                armorPenetration, submunitionSoundExplode, equipmentDef, def, thing, submunitionSpawnThingDef,
                                submunitionSpawnThingChance, postExplosionSpawnThingCount,
                                this.def.projectile.applyDamageToExplosionCellsNeighbors,
                                preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance,
                                this.def.projectile.preExplosionSpawnThingCount,
                                this.def.projectile.explosionChanceToStartFire,
                                this.def.projectile.explosionDamageFalloff);
                        }
                    }
                }
            }
           

            if ((this.ticksToDetonation <= 0 && this.ticksOfRain <= 0 && this.ticksToSubDetonation <= 0))
            {
                this.landed = true;
                this.Destroy(DestroyMode.Vanish);
            } 

        }
    }

    public class Projectile_SubmunitionPrime : Projectile_Submunition
    {

    }

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

        protected override void SpringSub(Pawn p) {
            base.GetComp<CompExplosive>().StartWick(null);
        }
    }
}
