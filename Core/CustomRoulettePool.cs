using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Represents a dynamic, weighted item pool that can be configured on the fly mid-game.
    /// Provides methods to inspect available items, calculate normalized drop probabilities,
    /// and roll items with full support for RoR2 RNG, player tier multipliers, Quality mod, and Chaos events.
    /// </summary>
    public class CustomRoulettePool
    {
        private readonly Dictionary<RouletteTierType, float> _tierWeights = new Dictionary<RouletteTierType, float>();

        public CustomRoulettePool()
        {
        }

        public CustomRoulettePool(IDictionary<RouletteTierType, float> weights)
        {
            if (weights != null)
            {
                foreach (var kvp in weights)
                {
                    SetTier(kvp.Key, kvp.Value);
                }
            }
        }

        public CustomRoulettePool(params (RouletteTierType tier, float weight)[] weights)
        {
            if (weights != null)
            {
                foreach (var (tier, weight) in weights)
                {
                    SetTier(tier, weight);
                }
            }
        }

        /// <summary>
        /// Sets or updates the weight for a specific tier.
        /// </summary>
        public CustomRoulettePool SetTier(RouletteTierType tier, float weight)
        {
            if (weight <= 0f)
            {
                _tierWeights.Remove(tier);
            }
            else
            {
                _tierWeights[tier] = weight;
            }
            return this;
        }

        /// <summary>
        /// Adds weight to a specific tier (or sets it if not present).
        /// </summary>
        public CustomRoulettePool AddTier(RouletteTierType tier, float weight)
        {
            if (weight <= 0f) return this;

            if (_tierWeights.TryGetValue(tier, out float current))
            {
                _tierWeights[tier] = current + weight;
            }
            else
            {
                _tierWeights[tier] = weight;
            }
            return this;
        }

        /// <summary>
        /// Removes a tier from the pool.
        /// </summary>
        public CustomRoulettePool RemoveTier(RouletteTierType tier)
        {
            _tierWeights.Remove(tier);
            return this;
        }

        /// <summary>
        /// Clears all tier weights.
        /// </summary>
        public CustomRoulettePool Clear()
        {
            _tierWeights.Clear();
            return this;
        }

        /// <summary>
        /// Gets a read-only view of configured raw weights.
        /// </summary>
        public IReadOnlyDictionary<RouletteTierType, float> RawWeights => _tierWeights;
        public bool ForceIncludeLunarEquipment { get; set; } = false;
        public bool DisallowEquipment { get; set; } = false;

        public List<PickupIndex>? ResolveListForTier(RouletteTierType tier)
        {
            if (DisallowEquipment && tier == RouletteTierType.Lunar)
            {
                return ItemPoolHelper.GetFilteredPassiveLunarPool();
            }
            return ItemPoolHelper.GetListForTier(tier, ForceIncludeLunarEquipment);
        }

        /// <summary>
        /// Resolves the actual item list for a tier in the current run.
        /// Handles composite pseudo-tiers like VoidPotential and HighTierPylon if requested.
        /// </summary>
        public static List<PickupIndex>? GetListForTier(RouletteTierType tier)
        {
            return ItemPoolHelper.GetListForTier(tier);
        }

        /// <summary>
        /// Returns all distinct PickupIndexes available across all configured tiers in the active run.
        /// Useful for inspecting the pool or displaying preview icons.
        /// </summary>
        public List<PickupIndex> GetAllAvailablePickups()
        {
            var result = new List<PickupIndex>();
            var seen = new HashSet<PickupIndex>();

            if (Run.instance == null) return result;

            foreach (var kvp in _tierWeights)
            {
                if (kvp.Value <= 0f) continue;

                var list = ResolveListForTier(kvp.Key);
                if (list == null || list.Count == 0) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var pIndex = list[i];
                    if (seen.Add(pIndex))
                    {
                        result.Add(pIndex);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a map of tier to available PickupIndexes for every tier in this pool.
        /// </summary>
        public Dictionary<RouletteTierType, List<PickupIndex>> GetPickupsByTier()
        {
            var result = new Dictionary<RouletteTierType, List<PickupIndex>>();
            if (Run.instance == null) return result;

            foreach (var kvp in _tierWeights)
            {
                if (kvp.Value <= 0f) continue;

                var list = ResolveListForTier(kvp.Key);
                if (list != null && list.Count > 0)
                {
                    result[kvp.Key] = new List<PickupIndex>(list);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates normalized probabilities (0.0 to 100.0%) for each tier,
        /// automatically discarding tiers that have 0 available items in the current run,
        /// and optionally applying player tier multiplier (Chaos blessings/curses).
        /// </summary>
        public Dictionary<RouletteTierType, float> GetNormalizedChances(CharacterMaster? master = null, bool applyPlayerTierMultiplier = false, float solitudeRarityBonus = 0f)
        {
            var result = new Dictionary<RouletteTierType, float>();
            if (Run.instance == null || _tierWeights.Count == 0) return result;

            float tierMult = (applyPlayerTierMultiplier && master != null)
                ? RouletteManager.Instance.GetPlayerTierMultiplier(master)
                : 1.0f;

            if (solitudeRarityBonus > 0f)
            {
                tierMult += solitudeRarityBonus;
            }

            float totalWeight = 0f;
            var activeWeights = new Dictionary<RouletteTierType, float>();

            foreach (var kvp in _tierWeights)
            {
                if (kvp.Value <= 0f) continue;

                var list = ResolveListForTier(kvp.Key);
                if (list == null || list.Count == 0) continue;

                float effectiveWeight = kvp.Value;
                if (applyPlayerTierMultiplier && (kvp.Key == RouletteTierType.Uncommon || kvp.Key == RouletteTierType.Legendary || kvp.Key == RouletteTierType.Boss || kvp.Key == RouletteTierType.ZetAspects || kvp.Key == RouletteTierType.VanillaAspect))
                {
                    effectiveWeight *= tierMult;
                }

                if (effectiveWeight > 0f)
                {
                    activeWeights[kvp.Key] = effectiveWeight;
                    totalWeight += effectiveWeight;
                }
            }

            if (totalWeight <= 0f) return result;

            foreach (var kvp in activeWeights)
            {
                result[kvp.Key] = (kvp.Value / totalWeight) * 100f;
            }

            return result;
        }

        /// <summary>
        /// Rolls a random item from this pool.
        /// Supports RoR2 RNG, player luck/tier multipliers, Quality mod upgrades, Chance Doll upgrades, and Chaos events.
        /// </summary>
        public RouletteItemEntry? Roll(
            global::Xoroshiro128Plus? rng = null,
            CharacterMaster? master = null,
            ChaosChances? chances = null,
            bool applyPlayerTierMultiplier = false,
            bool applyChanceDoll = false,
            bool guaranteeQuality = false,
            float solitudeRarityBonus = 0f,
            QualityTierCompat minQualityTier = QualityTierCompat.None)
        {
            if (Run.instance == null || _tierWeights.Count == 0) return null;

            float tierMult = (applyPlayerTierMultiplier && master != null)
                ? RouletteManager.Instance.GetPlayerTierMultiplier(master)
                : 1.0f;

            if (solitudeRarityBonus > 0f)
            {
                tierMult += solitudeRarityBonus;
            }

            // 1. Filter out empty pools and build effective weights
            var activeTiers = new List<KeyValuePair<RouletteTierType, float>>();
            var resolvedPools = new Dictionary<RouletteTierType, List<PickupIndex>>();
            float totalWeight = 0f;

            foreach (var kvp in _tierWeights)
            {
                if (kvp.Value <= 0f) continue;

                var list = ResolveListForTier(kvp.Key);
                if (list == null || list.Count == 0) continue;

                resolvedPools[kvp.Key] = list;

                float weight = kvp.Value;
                if (applyPlayerTierMultiplier && (kvp.Key == RouletteTierType.Uncommon || kvp.Key == RouletteTierType.Legendary || kvp.Key == RouletteTierType.Boss || kvp.Key == RouletteTierType.ZetAspects || kvp.Key == RouletteTierType.VanillaAspect))
                {
                    weight *= tierMult;
                }

                if (weight > 0f)
                {
                    activeTiers.Add(new KeyValuePair<RouletteTierType, float>(kvp.Key, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight <= 0f || activeTiers.Count == 0)
            {
                // Never fall back to Common for Equipment or QualityEquipment tiers!
                bool isEquipmentRoll = _tierWeights.ContainsKey(RouletteTierType.Equipment) || _tierWeights.ContainsKey(RouletteTierType.QualityEquipment);
                if (isEquipmentRoll)
                {
                    var eqPool = ItemPoolHelper.GetBaseEquipmentPool();
                    if (eqPool != null && eqPool.Count > 0)
                    {
                        resolvedPools[RouletteTierType.Equipment] = eqPool;
                        activeTiers.Add(new KeyValuePair<RouletteTierType, float>(RouletteTierType.Equipment, 100f));
                        totalWeight = 100f;
                    }
                }
                else
                {
                    // Fallback to Common tier if available
                    var commonPool = ResolveListForTier(RouletteTierType.Common);
                    if (commonPool != null && commonPool.Count > 0)
                    {
                        resolvedPools[RouletteTierType.Common] = commonPool;
                        activeTiers.Add(new KeyValuePair<RouletteTierType, float>(RouletteTierType.Common, 100f));
                        totalWeight = 100f;
                    }
                }

                if (totalWeight <= 0f || activeTiers.Count == 0) return null;
            }

            // 2. Roll a tier based on cumulative weights
            float rollVal = rng != null ? rng.RangeFloat(0f, totalWeight) : UnityEngine.Random.Range(0f, totalWeight);
            RouletteTierType chosenTier = activeTiers[0].Key;
            float current = 0f;

            for (int i = 0; i < activeTiers.Count; i++)
            {
                current += activeTiers[i].Value;
                if (current >= rollVal)
                {
                    chosenTier = activeTiers[i].Key;
                    break;
                }
            }

            // 3. Pick random PickupIndex from the chosen tier
            if (!resolvedPools.TryGetValue(chosenTier, out var pool) || pool == null || pool.Count == 0)
            {
                chosenTier = activeTiers[0].Key;
                if (!resolvedPools.TryGetValue(chosenTier, out pool) || pool == null || pool.Count == 0)
                {
                    return null;
                }
            }
            int index = rng != null ? rng.RangeInt(0, pool.Count) : UnityEngine.Random.Range(0, pool.Count);
            PickupIndex pIndex = pool[index];

            // 4. Quality Mod Integration
            if (QualityCompat.IsQualityLoaded)
            {
                float extraChaosQualityChance = 0f;
                if (ModConfig.EnableChaosQualityInfluence.Value && master != null)
                {
                    ChaosChances currentChances = chances ?? ChaosEventManager.CalculateChances(master);
                    float totalChaos = currentChances.GoodPercent + currentChances.BadPercent;
                    extraChaosQualityChance = totalChaos * ModConfig.ChaosQualityInfluenceFactor.Value;
                }

                bool shouldGuarantee = guaranteeQuality || (minQualityTier > QualityTierCompat.None) || (chosenTier == RouletteTierType.QualityEquipment) || (RouletteManager.Instance.FreeQualityRollsAvailable > 0);
                PickupIndex qualityPickup = QualityCompat.RollQuality(pIndex, master, extraChaosQualityChance, shouldGuarantee, minQualityTier);
                if (qualityPickup != PickupIndex.none && qualityPickup != pIndex)
                {
                    bool isEquipmentTier = (chosenTier == RouletteTierType.Equipment || chosenTier == RouletteTierType.QualityEquipment);
                    if (isEquipmentTier)
                    {
                        var qDef = PickupCatalog.GetPickupDef(qualityPickup);
                        if (qDef != null && qDef.equipmentIndex != EquipmentIndex.None)
                        {
                            pIndex = qualityPickup;
                        }
                    }
                    else
                    {
                        pIndex = qualityPickup;
                    }
                }
            }

            // 5. Create entry
            var entry = ItemPoolHelper.CreateEntry(pIndex, chosenTier);

            // 6. Optional Chance Doll upgrade
            if (applyChanceDoll && master != null)
            {
                RouletteManager.Instance.TryUpgradeWithChanceDoll(master, ref entry, out _, out _, out _);
            }

            // 7. Chaos Event roll
            if (chances.HasValue)
            {
                entry.ChaosEvent = ChaosEventManager.RollEvent(chances.Value, rng);
                var def = ChaosEventManager.GetDefinition(entry.ChaosEvent);
                entry.ChaosPolarity = def != null ? def.Polarity : ChaosPolarity.None;
            }

            return entry;
        }

        // ==========================================
        // Static Presets & Factory Methods
        // ==========================================

        /// <summary>
        /// Creates a pool configured with the standard paid roulette weights from ModConfig.
        /// </summary>
        public static CustomRoulettePool CreateDefaultPaidPool()
        {
            var pool = new CustomRoulettePool();

            float wCommon = Mathf.Max(0f, ModConfig.WeightCommon.Value);
            float wUncommon = Mathf.Max(0f, ModConfig.WeightUncommon.Value);
            float wLegendary = Mathf.Max(0f, ModConfig.WeightLegendary.Value);
            float wBoss = Mathf.Max(0f, ModConfig.WeightBoss.Value);
            float wLunar = ModConfig.IncludeLunar.Value ? Mathf.Max(0f, ModConfig.WeightLunar.Value) : 0f;
            float wVoid = ModConfig.IncludeVoid.Value ? Mathf.Max(0f, ModConfig.WeightVoid.Value) : 0f;

            if (wCommon > 0f) pool.SetTier(RouletteTierType.Common, wCommon);
            if (wUncommon > 0f) pool.SetTier(RouletteTierType.Uncommon, wUncommon);
            if (wLegendary > 0f) pool.SetTier(RouletteTierType.Legendary, wLegendary);
            if (wBoss > 0f) pool.SetTier(RouletteTierType.Boss, wBoss);
            if (wLunar > 0f) pool.SetTier(RouletteTierType.Lunar, wLunar);
            if (wVoid > 0f) pool.SetTier(RouletteTierType.Void, wVoid);

            if (ModConfig.IncludeZetAspectsInPool.Value && ZetAspectsCompat.IsZetAspectsLoaded)
            {
                if (ModConfig.ZetAspectsPoolModeSetting.Value == ZetAspectsPoolMode.DedicatedTier)
                {
                    float wAspects = Mathf.Max(0f, ModConfig.WeightZetAspects.Value);
                    if (wAspects > 0f) pool.SetTier(RouletteTierType.ZetAspects, wAspects);
                }
            }
            else if (!ZetAspectsCompat.IsZetAspectsLoaded && ModConfig.IncludeVanillaAspectsInRoulette.Value)
            {
                if (ModConfig.VanillaAspectsPoolModeSetting.Value == VanillaAspectsPoolMode.DedicatedTier)
                {
                    float wAspects = Mathf.Max(0f, ModConfig.WeightVanillaAspects.Value);
                    if (wAspects > 0f) pool.SetTier(RouletteTierType.VanillaAspect, wAspects);
                }
            }

            return pool;
        }

        /// <summary>
        /// Creates a Void Potential (Тройник Бездны) pool: 80% Green (Uncommon), 15% Red (Legendary), 5% Yellow (Boss).
        /// </summary>
        public static CustomRoulettePool CreateVoidPotentialPool()
        {
            return new CustomRoulettePool(
                (RouletteTierType.Uncommon, 80f),
                (RouletteTierType.Legendary, 15f),
                (RouletteTierType.Boss, 5f)
            );
        }

        /// <summary>
        /// Creates a Boss Reward pool: 85% Green (Uncommon), 5% Red (Legendary), 10% Yellow (Boss).
        /// </summary>
        public static CustomRoulettePool CreateBossRewardPool()
        {
            float wUncommon = ModConfig.BossRewardWeightUncommon != null ? Mathf.Max(0f, ModConfig.BossRewardWeightUncommon.Value) : 85f;
            float wLegendary = ModConfig.BossRewardWeightLegendary != null ? Mathf.Max(0f, ModConfig.BossRewardWeightLegendary.Value) : 5f;
            float wBoss = ModConfig.BossRewardWeightBoss != null ? Mathf.Max(0f, ModConfig.BossRewardWeightBoss.Value) : 10f;
            return new CustomRoulettePool(
                (RouletteTierType.Uncommon, wUncommon),
                (RouletteTierType.Legendary, wLegendary),
                (RouletteTierType.Boss, wBoss)
            );
        }

        /// <summary>
        /// Creates a High-Tier Pylon / Elite drop pool: 45% Red (Legendary), 45% Void (Бездна), 10% Yellow (Boss).
        /// </summary>
        public static CustomRoulettePool CreateHighTierPylonPool()
        {
            return new CustomRoulettePool(
                (RouletteTierType.Legendary, 45f),
                (RouletteTierType.Void, 45f),
                (RouletteTierType.Boss, 10f)
            );
        }

        /// <summary>
        /// <summary>
        /// Creates a Lunar Pod / Chest pool with both passive Lunar items and active Lunar equipments.
        /// </summary>
        public static CustomRoulettePool CreateLunarChestPool()
        {
            var pool = new CustomRoulettePool((RouletteTierType.Lunar, 100f));
            pool.ForceIncludeLunarEquipment = true;
            return pool;
        }

        /// <summary>
        /// Creates a pool for Anomalous Mutation with passive Void items and passive Lunar items (no equipment).
        /// </summary>
        public static CustomRoulettePool CreateLunarVoidPool()
        {
            var pool = new CustomRoulettePool(
                (RouletteTierType.Void, 50f),
                (RouletteTierType.Lunar, 50f)
            );
            pool.DisallowEquipment = true;
            pool.ForceIncludeLunarEquipment = false;
            return pool;
        }

        /// <summary>
        /// Factory helper to build a custom pool from a dictionary.
        /// </summary>
        public static CustomRoulettePool FromWeights(IDictionary<RouletteTierType, float> weights)
        {
            return new CustomRoulettePool(weights);
        }

        /// <summary>
        /// Factory helper to build a custom pool from value tuples.
        /// </summary>
        public static CustomRoulettePool FromWeights(params (RouletteTierType tier, float weight)[] weights)
        {
            return new CustomRoulettePool(weights);
        }
    }
}
