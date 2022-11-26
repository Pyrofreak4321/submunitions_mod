using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace Submunition
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = new Harmony("com.ifp.advancedmunitions");
            harmony.PatchAll();
        }
    }


    [HarmonyPatch(typeof(Verb_LaunchProjectile), nameof(Verb_LaunchProjectile.HighlightFieldRadiusAroundTarget))]
    class radiusFix
    {
        static void Postfix( Verb_LaunchProjectile __instance, ref float __result)
        {
            if (__instance.Projectile.HasModExtension<DefAdvancedProjectileExtension>())
            {
                DefAdvancedProjectileExtension ext = __instance.Projectile.GetModExtension<DefAdvancedProjectileExtension>();
                __result = Math.Max(Math.Max(ext.fragSpreadRadius, ext.spawnSpreadRadius), ext.explosionSpreadRadius);
            } 
        }
    }

    /*
    [HarmonyPatch(typeof(Projectile), "StartingTicksToImpact")]
    class fieldgunSpeedFix
    {
        static void Postfix(Projectile __instance, ref float __result)
        {
            if (__instance.Launcher.def.HasModExtension<DefAdvancedTurret>())
            {
                DefAdvancedTurret ext = __instance.Launcher.def.GetModExtension<DefAdvancedTurret>();
                __result = __result / ext.speedBoost;
            }
        }
    }
    */

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch), new Type[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef) })]
    public static class fireCluster
    {
        public static bool Prefix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (__instance.def.HasModExtension<DefClusterExtension>())
            {
                DefClusterExtension ext = __instance.def.GetModExtension<DefClusterExtension>();
                float dist = (origin.ToIntVec3() - usedTarget.Cell).LengthHorizontal+1;
                
                IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(usedTarget.Cell, ext.pelletSpread, true);
                int max = GenRadial.NumCellsInRadius((ext.pelletSpread/ext.atRange)*dist);

                for (int i = 0; i < ext.pelletCount; i++)
                {
                    int num2 = Rand.Range(0, max);
                    IntVec3 randomCell = usedTarget.Cell + GenRadial.RadialPattern[num2];

                    Projectile pellet = (Projectile)GenSpawn.Spawn(ext.pelletDef, origin.ToIntVec3(), __instance.Map, WipeMode.Vanish);

                    ShootLine s = new ShootLine(origin.ToIntVec3(), randomCell);
                    IEnumerable<IntVec3> cells = s.Points();
                    float mod = (1f+(ext.pelletSpread/10f)) / Math.Max(cells.Count(),1f);
                    bool flag = true;
                    LocalTargetInfo target = randomCell;

                    for (int c = 0; c < cells.Count() && flag; c++)
                    {
                        if (!cells.ElementAt(c).ToVector3().Equals(origin))
                        {
                            List<Thing> things = cells.ElementAt(c).GetThingList(__instance.Map);
                            for (int t = 0; t < things.Count && flag; t++)
                            {
                                Pawn pawn = things[t] as Pawn;

                                float chance = 0;
                                if(usedTarget.HasThing && usedTarget.Thing.Equals(things[t]))
                                {
                                    chance = 100f;
                                }
                                else if (pawn != null)
                                {
                                    dist = (cells.ElementAt(c) - randomCell).LengthHorizontal;
                                    chance = pawn.BodySize * (float)Math.Pow(mod * c, 4f) * ((pawn.GetPosture() != PawnPosture.Standing && dist > ext.pelletSpread) ? .2f : 1f);
                                }
                                else
                                {
                                    chance = things[t].BaseBlockChance() * (float)Math.Pow(mod * c, 4f);
                                }


                                if (Rand.Chance(chance))
                                {
                                    target = things[t];
                                    flag = false;
                                }
                            }
                        }
                    }

                    pellet.Launch(launcher, origin, target, intendedTarget, pellet.HitFlags, false, equipment, null);
                }

                __instance.Destroy();

                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(GenDraw), nameof(GenDraw.DrawRadiusRing), new Type[] { typeof(IntVec3), typeof(float)})]
    public static class fixLargeRadius
    {
        public static bool Prefix(IntVec3 center, float radius)
        {
            if (radius > GenRadial.MaxRadialPatternRadius)
                return false;
            return true;
        }
    }




    public class DefClusterExtension : DefModExtension
    {
        public int pelletCount = 6;
        public float pelletSpread = 1.9f;
        public int atRange = 10;
        public ThingDef pelletDef;
    }

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

            public bool submunitionBypassPawn = false;

            public float submunitionPopSize = 0;

            public DamageDef submunitionDamageDef;
            public int submunitionDamageAmount = -1;
            public SoundDef submunitionSoundExplode;
            public float submunitionSpawnThingChance = 0f;
            public ThingDef submunitionSpawnThingDef;

            public bool submunitionFragment = false;
            public ThingDef submunitionFragmentThingDef;

            public bool submunitionEmit = false; 
            public bool blockedByRoof = false;
            public float expanding = 0;
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
        private int submunitionDuration = 1;

        private float submunitionExplosionRadius;

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
            if (this.Spawned)
            {
                base.Tick();

                if (this.ticksToImpact < this.preDetonation && this.preDetonation > 0)
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
                else if (this.ticksToSubDetonation > 0)
                {
                    this.ticksToSubDetonation--;
                    if (this.ticksToSubDetonation <= 0)
                    {
                        this.ticksOfRain = Math.Min(this.ticksToImpact, this.submunitionDuration);
                    }
                }
                else if (this.ticksOfRain > 0)
                {
                    this.ticksOfRain--;
                    if (this.ticksOfRain % this.submunitionInterval == 0)
                    {
                        this.Explode();
                    }
                }
            }
        }

        protected override void Impact(Thing hitThing)
        {
            this.Detonate();
            if (this.destroyOnImpact)
            {
                this.ticksToSubDetonation = -1;
                this.ticksOfRain = -1;
                this.ticksToDetonation = -1;
                this.landed = true;
                this.DeSpawn(DestroyMode.Vanish);
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
                this.ticksToSubDetonation = this.submunitionDelay;
                this.ticksOfRain = this.submunitionDuration;

                if (this.ticksToDetonation == 0)
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

            //IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.ExactPosition.ToIntVec3(), explosionRadius, true);



            if (this.initalBlast)
            {
                if (this.extension.submunitionPopSize > 0)
                {
                    popSize = this.extension.submunitionPopSize;
                }

                if (this.extension.submunitionPopSize >= 0)
                    GenExplosion.DoExplosion(position, map2, popSize, damageDef,
                        launcher, damageAmount, armorPenetration, soundExplode, equipmentDef,
                        def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance,
                        postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors,
                        preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance,
                        this.def.projectile.preExplosionSpawnThingCount,
                        this.def.projectile.explosionChanceToStartFire, this.def.projectile.explosionDamageFalloff);

                this.initalBlast = false;
                
                submunitionExplosionRadius = this.extension.submunitionExplosionRadius;
            }

            if (this.ticksToSubDetonation <= 0 && this.ticksToSubDetonation > -1)
            {
                int submunitionCount;
                float submunitionMultiplier;
                DamageDef submunitionDamageDef;
                SoundDef submunitionSoundExplode;
                float submunitionSpawnThingChance;
                ThingDef submunitionSpawnThingDef;
                int submunitionDamageAmount;

                submunitionCount = this.extension.submunitionCount;
                submunitionMultiplier = this.extension.submunitionMultiplier;
                submunitionSoundExplode = this.extension.submunitionSoundExplode;
                submunitionDamageDef = this.extension.submunitionDamageDef;
                submunitionSpawnThingChance = this.extension.submunitionSpawnThingChance;
                submunitionSpawnThingDef = this.extension.submunitionSpawnThingDef;
                submunitionDamageAmount = this.extension.submunitionDamageAmount;
                submunitionExplosionRadius += this.extension.expanding;

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
                    submunitionCount = 1+(int)Math.Floor(submunitionMultiplier * explosionRadius);
                }

                if (this.extension.submunitionFragment && this.extension.submunitionFragmentThingDef != null)
                {
                    IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.destination.ToIntVec3(), explosionRadius, true);

                    for (int i = 0; i < (submunitionCount); i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement();

                        Projectile frag = (Projectile)GenSpawn.Spawn(this.extension.submunitionFragmentThingDef, base.Position, base.Map, WipeMode.Vanish);

                        ShootLine s = new ShootLine(base.Position, randomCell);
                        IEnumerable<IntVec3> cells = s.Points();
                        bool flag = true;
                        LocalTargetInfo target = randomCell;

                        if(frag.HitFlags != ProjectileHitFlags.None)
                        {
                            for (int c = 0; c < cells.Count() && flag; c++)
                            {
                                List<Thing> things = cells.ElementAt(c).GetThingList(base.Map);
                                for (int t = 0; t < things.Count && flag; t++)
                                {
                                    if (things[t] as Pawn != null)
                                    {
                                        target = things[t];
                                        flag = false;
                                    }
                                    else if (Rand.Chance(things[t].BaseBlockChance()))
                                    {
                                        target = things[t];
                                        flag = false;
                                    }
                                }
                            }
                        }

                        frag.Launch(this, target, this.intendedTarget, frag.HitFlags, false, EquipmentDef.GetConcreteExample());
                    }
                }
                else
                {
                    IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.Position, explosionRadius, true);
                    if (this.extension.submunitionEmit && this.extension.submunitionSpawnThingDef != null)
                    {
                        cellRect = GenRadial.RadialCellsAround(base.Position, explosionRadius+submunitionExplosionRadius, true);
                    }


                    for (int i = 0; i < (submunitionCount); i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement();

                        bool blocked = false;
                        if (this.def.projectile.flyOverhead)
                        {
                            RoofDef roofDef = base.Map.roofGrid.RoofAt(randomCell);
                            if (roofDef != null)
                            {
                                if (this.extension.blockedByRoof)
                                {
                                    blocked = true;
                                }
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
                            if(this.extension.submunitionEmit && this.extension.submunitionSpawnThingDef != null)
                            {
                                GenSpawn.Spawn(this.extension.submunitionSpawnThingDef, randomCell, map, WipeMode.Vanish);
                            }
                            else
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
            }

            if ((this.ticksToDetonation <= 0 && this.ticksOfRain <= 0 && this.ticksToSubDetonation <= 0))
            {
                this.landed = true;
                this.DeSpawn(DestroyMode.Vanish);
            }
        }
    }

    public class Projectile_SubmunitionPrime : Projectile_Submunition
    {

    }

    //////////////////////////////////////////////////////////

    public class DefAdvancedProjectileExtension : DefModExtension
    {
        public bool fragment = false;
        public int fragCount = 0;
        public float fragMultiplier = 0.5f;
        public float fragSpreadRadius = 1f;
        public ThingDef fragThingDef;

        public bool spawner = false;
        public int spawnCount = 0;
        public float spawnMultiplier = 0.5f;
        public float spawnSpreadRadius = 1f;
        public ThingDef spawnThingDef;

        public bool explosive = false;
        public int explosionCount = 0;
        public float explosionMultiplier = 0.5f;
        public float explosionRadius = 3f;
        public float explosionSpreadRadius = 1f;
        public DamageDef explosionDamageDef;
        public int explosionDamageAmount = -1;
        public SoundDef SoundExplode;
        public float preExplosionSpawnThingChance = 0f;
        public int preExplosionSpawnThingCount = 0;
        public ThingDef preExplosionSpawnThingDef;
        public float postExplosionSpawnThingChance = 0f;
        public int postExplosionSpawnThingCount = 0;
        public ThingDef postExplosionSpawnThingDef;
        public EffecterDef explosionEffect;


        public int secondaryDelay = 0;
        public int interval = 0;
        public int duration = 0;
        public int preImpactDetonation = 0;
        public float expanding = 0;

        public bool destroyOnImpact = true;
        public bool blockedByRoof = false;
    }

    public class Projectile_Advanced : Projectile
    {
        private DefAdvancedProjectileExtension extension;
        private int ticksToPrimaryDet = 0;
        private int ticksToSecondaryDet = 0;
        private int ticksOfSecondary = 0;
        private float radiusExpansion = 0;


        private bool detonated = false;
        private bool initalBlast = true;
        private bool wiped = false;

        private CellRect wholemap;
        private RoofGrid roofGrid;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksToPrimaryDet, "ticksToPrimaryDet", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToSecondaryDet, "ticksToSecondaryDet", 0, false);
            Scribe_Values.Look<int>(ref this.ticksOfSecondary, "ticksOfSecondary", 0, false);
            Scribe_Values.Look<float>(ref this.radiusExpansion, "radiusExpansion", 0, false);
            Scribe_Values.Look<bool>(ref this.detonated, "detonated", false, false);
            Scribe_Values.Look<bool>(ref this.initalBlast, "initalBlast", true, false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.def.HasModExtension<DefAdvancedProjectileExtension>())
            {
                extension = this.def.GetModExtension<DefAdvancedProjectileExtension>();
            }
            else
            {
                extension = new DefAdvancedProjectileExtension();
            }
        }

        public override void Tick()
        {
            if (this.Spawned)
            {
                base.Tick();

                if (this.ticksToImpact < this.extension.preImpactDetonation && this.extension.preImpactDetonation > 0)
                {
                    this.Detonate();
                }

                if (this.ticksToPrimaryDet > 0)
                {
                    this.ticksToPrimaryDet--;
                    if (this.ticksToPrimaryDet <= 0)
                    {
                        this.Explode();
                    }
                }
                else if (this.ticksToSecondaryDet > 0)
                {
                    this.ticksToSecondaryDet--;
                    if (this.ticksToSecondaryDet <= 0)
                    {
                        this.ticksOfSecondary = this.extension.duration;
                    }
                }
                else if (this.ticksOfSecondary > 0)
                {
                    this.radiusExpansion += this.extension.expanding;
                    this.ticksOfSecondary--;
                    if (this.ticksOfSecondary % this.extension.interval == 0)
                    {
                        this.Explode();
                    }
                }
            }
        }

        protected override void Impact(Thing hitThing)
        {
            this.landed = true;
            this.Detonate();
            if (!this.wiped && this.extension.destroyOnImpact)
            {
                this.ticksToSecondaryDet = -1;
                this.ticksOfSecondary = -1;
                this.ticksToPrimaryDet = -1;
                this.DeSpawn(DestroyMode.Vanish);
            }
        }

        protected virtual void Detonate()
        {
            if (!this.detonated)
            {
                this.detonated = true;

                this.wholemap = CellRect.WholeMap(base.Map);
                this.roofGrid = new RoofGrid(base.Map);
                this.ticksToPrimaryDet = this.def.projectile.explosionDelay;
                this.ticksToSecondaryDet = this.extension.secondaryDelay;
                this.ticksOfSecondary = this.extension.duration;

                if (this.ticksToPrimaryDet == 0)
                {
                    this.Explode();
                }
            }
        }

        protected virtual void Explode()
        {
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef);

            Map map = base.Map;
            IntVec3 position = base.ExactPosition.ToIntVec3();
            Thing launcher = this.launcher;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            Thing thing = this.intendedTarget.Thing;

            if (this.initalBlast)
            { 
                if (this.def.projectile.explosionEffect != null)
                {
                    Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                    effecter.Trigger(new TargetInfo(base.ExactPosition.ToIntVec3(), map, false), new TargetInfo(base.ExactPosition.ToIntVec3(), map, false));
                    effecter.Cleanup();
                }

                if (this.def.projectile.explosionRadius > 0)
                    GenExplosion.DoExplosion(position, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef,
                        launcher, base.DamageAmount, base.ArmorPenetration, this.def.projectile.soundExplode, equipmentDef,
                        def, thing, this.def.projectile.postExplosionSpawnThingDef, this.def.projectile.postExplosionSpawnChance,
                        this.def.projectile.postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors,
                        this.def.projectile.preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance,
                        this.def.projectile.preExplosionSpawnThingCount,
                        this.def.projectile.explosionChanceToStartFire, this.def.projectile.explosionDamageFalloff);

                this.initalBlast = false;
            }

            if (this.ticksToSecondaryDet <= 0 && this.ticksToSecondaryDet > -1)
            {
                if (this.extension.fragment && this.extension.fragThingDef != null)
                {
                    IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.destination.ToIntVec3(), this.extension.fragSpreadRadius+this.radiusExpansion, true);
                    int count = this.extension.fragCount;
                    if (count == 0)
                    {
                        count = (int)(this.extension.fragMultiplier * (Math.PI * Math.Pow(this.extension.fragSpreadRadius + this.radiusExpansion, 2)));
                    }
                    for (int i = 0; i < count; i++)
                    {
                        IEnumerable<IntVec3> cells;
                        IntVec3 randomCell = cellRect.RandomElement();
                        if (randomCell.InBounds(base.Map))
                        {
                            Projectile frag = (Projectile)GenSpawn.Spawn(this.extension.fragThingDef, base.Position, base.Map, WipeMode.Vanish);

                            cells = new ShootLine(base.Position, randomCell).Points();

                            bool flag = true;
                            LocalTargetInfo target = new LocalTargetInfo(randomCell);

                            if (frag.HitFlags != ProjectileHitFlags.None)
                            {
                                for (int c = 0; c < cells.Count() && flag; c++)
                                {
                                    List<Thing> things = cells.ElementAt(c).GetThingList(base.Map);
                                    for (int t = 0; t < things.Count && flag; t++)
                                    {
                                        float dist = (cells.ElementAt(c) - base.Position).LengthHorizontal;
                                        Pawn pawn = things[t] as Pawn;

                                        float chance = 0;

                                        if (pawn != null)
                                        {
                                            chance = pawn.BodySize * ((pawn.GetPosture() != PawnPosture.Standing && dist > 2) ? .2f : 1f);
                                        }
                                        else
                                        {
                                            chance = things[t].BaseBlockChance();
                                        }


                                        if (Rand.Chance(chance * 1.25f))
                                        {
                                            target = things[t];
                                            flag = false;
                                        }
                                    }
                                }
                            }

                            frag.Launch(this, target, this.intendedTarget, frag.HitFlags, false, equipmentDef.GetConcreteExample());
                        }
                    }
                }

                if (this.extension.spawner && this.extension.spawnThingDef != null)
                {
                    IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.Position, this.extension.spawnSpreadRadius + this.radiusExpansion, true);
                    int count = this.extension.spawnCount;
                    if (count == 0)
                    {
                        count = (int)(this.extension.spawnMultiplier * (Math.PI * Math.Pow(this.extension.spawnSpreadRadius + this.radiusExpansion, 2)));
                    }
                    for (int i = 0; i < count; i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement();
                        if (randomCell.InBounds(base.Map))
                        {

                            bool blocked = false;
                            if (this.def.projectile.flyOverhead && !this.landed)
                            {
                                RoofDef roofDef = base.Map.roofGrid.RoofAt(randomCell);
                                if (roofDef != null)
                                {
                                    if (this.extension.blockedByRoof)
                                    {
                                        blocked = true;
                                    }
                                    if (roofDef.isThickRoof)
                                    {
                                        blocked = true;
                                    }
                                    if (!blocked && (randomCell.GetEdifice(base.Map) == null || randomCell.GetEdifice(base.Map).def.Fillage != FillCategory.Full))
                                    {
                                        RoofCollapserImmediate.DropRoofInCells(randomCell, base.Map, null);
                                    }
                                }
                            }
                            else
                            {
                                if ((randomCell - base.Position).LengthHorizontal > 0)
                                    randomCell = GenSight.PointsOnLineOfSight(base.Position, randomCell).Last();
                            }

                            if (this.wholemap.Contains(randomCell) && !blocked)
                            {
                                GenSpawn.Spawn(this.extension.spawnThingDef, randomCell, map, WipeMode.Vanish);
                            }
                        }
                    }
                }

                if (this.extension.explosive && this.extension.explosionDamageDef != null)
                {
                    IEnumerable<IntVec3> cellRect = GenRadial.RadialCellsAround(base.Position, this.extension.explosionSpreadRadius + this.radiusExpansion, true);
                    int count = this.extension.explosionCount;
                    if (count == 0)
                    {
                        count = (int)(this.extension.explosionMultiplier * (Math.PI * Math.Pow(this.extension.explosionSpreadRadius + this.radiusExpansion, 2)));
                    }
                    for (int i = 0; i < count; i++)
                    {
                        IntVec3 randomCell = cellRect.RandomElement();
                        if (randomCell.InBounds(base.Map))
                        {

                            bool blocked = false;
                            if (this.def.projectile.flyOverhead && !this.landed)
                            {
                                RoofDef roofDef = base.Map.roofGrid.RoofAt(randomCell);
                                if (roofDef != null)
                                {
                                    if (this.extension.blockedByRoof)
                                    {
                                        blocked = true;
                                    }
                                    if (roofDef.isThickRoof)
                                    {
                                        blocked = true;
                                    }
                                    if (!blocked && (randomCell.GetEdifice(base.Map) == null || randomCell.GetEdifice(base.Map).def.Fillage != FillCategory.Full))
                                    {
                                        RoofCollapserImmediate.DropRoofInCells(randomCell, base.Map, null);
                                    }
                                }
                            }
                            else
                            {
                                if ((randomCell - base.Position).LengthHorizontal > 0)
                                    randomCell = GenSight.PointsOnLineOfSight(base.Position, randomCell).Last();
                            }

                            if (this.wholemap.Contains(randomCell) && !blocked)
                            {
                                if (this.extension.explosionEffect != null)
                                {
                                    Effecter effecter = this.extension.explosionEffect.Spawn();
                                    effecter.Trigger(new TargetInfo(base.ExactPosition.ToIntVec3(), map, false), new TargetInfo(base.ExactPosition.ToIntVec3(), map, false));
                                    effecter.Cleanup();
                                }

                                GenExplosion.DoExplosion(randomCell, map, this.extension.explosionRadius, this.extension.explosionDamageDef, launcher, this.extension.explosionDamageAmount,
                                        this.extension.explosionDamageDef.defaultArmorPenetration, this.extension.explosionDamageDef.soundExplosion, equipmentDef, def, thing, this.extension.postExplosionSpawnThingDef,
                                        this.extension.postExplosionSpawnThingChance, this.extension.postExplosionSpawnThingCount,
                                        this.def.projectile.applyDamageToExplosionCellsNeighbors,
                                        this.extension.preExplosionSpawnThingDef, this.extension.preExplosionSpawnThingChance,
                                        this.extension.preExplosionSpawnThingCount,
                                        this.def.projectile.explosionChanceToStartFire,
                                        this.def.projectile.explosionDamageFalloff);
                            }
                        }
                    }
                }
            }

            if ((this.ticksToPrimaryDet <= 0 && this.ticksToSecondaryDet <= 0 && this.ticksOfSecondary <= 0))
            {
                this.wiped = true;
                this.DeSpawn(DestroyMode.Vanish);
            }
        }
    }
}
