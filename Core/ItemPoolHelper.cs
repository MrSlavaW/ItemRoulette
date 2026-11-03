using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    public enum RouletteTierType
    {
        Common,
        Uncommon,
        Legendary,
        Boss,
        Lunar,
        Void,
        VoidPotential,
        HighTierPylon,
        ZetAspects,
        Equipment,
        VanillaAspect,
        QualityEquipment,
        LunarVoid,
        BossReward
    }

    public class RouletteItemEntry
    {
        public PickupIndex PickupIndex { get; set; }
        public PickupDef PickupDef { get; set; } = null!;
        public string LocalizedName { get; set; } = "";
        public Color RarityColor { get; set; }
        public Sprite? IconSprite { get; set; }
        public Texture? IconTexture { get; set; }
        public Rect SpriteUvRect { get; set; }
        public RouletteTierType TierType { get; set; }
        public string TierLabel { get; set; } = "";
        public ChaosEventType ChaosEvent { get; set; } = ChaosEventType.None;
        public ChaosPolarity ChaosPolarity { get; set; } = ChaosPolarity.None;
        public bool UpgradedByChanceDoll { get; set; } = false;
        public string? OriginalNameBeforeDollUpgrade { get; set; }
        public QualityTierCompat QualityTier { get; set; } = QualityTierCompat.None;
        public Color QualityColor { get; set; } = Color.clear;
        public string QualityLabel { get; set; } = "";

        public bool IsVoidItem
        {
            get
            {
                if (TierType == RouletteTierType.Void) return true;
                if (PickupDef != null)
                {
                    return PickupDef.itemTier == ItemTier.VoidTier1 ||
                           PickupDef.itemTier == ItemTier.VoidTier2 ||
                           PickupDef.itemTier == ItemTier.VoidTier3 ||
                           PickupDef.itemTier == ItemTier.VoidBoss;
                }
                return false;
            }
        }

        public bool IsLunarItem
        {
            get
            {
                if (TierType == RouletteTierType.Lunar) return true;
                if (PickupDef != null)
                {
                    return PickupDef.itemTier == ItemTier.Lunar ||
                           (PickupDef.equipmentIndex != EquipmentIndex.None && EquipmentCatalog.GetEquipmentDef(PickupDef.equipmentIndex)?.isLunar == true);
                }
                return false;
            }
        }
    }

    public static class ItemPoolHelper
    {
        public static readonly Color ColorCommon = new Color(0.85f, 0.85f, 0.85f, 1f);
        public static readonly Color ColorUncommon = new Color(0.35f, 0.88f, 0.35f, 1f);
        public static readonly Color ColorLegendary = new Color(0.92f, 0.28f, 0.28f, 1f);
        public static readonly Color ColorBoss = new Color(0.95f, 0.80f, 0.15f, 1f);
        public static readonly Color ColorLunar = new Color(0.20f, 0.65f, 0.95f, 1f);
        public static readonly Color ColorVoid = new Color(0.85f, 0.30f, 0.90f, 1f);
        public static readonly Color ColorVoidPotential = new Color(0.75f, 0.35f, 1.0f, 1f);
        public static readonly Color ColorHighTierPylon = new Color(0.92f, 0.40f, 1.0f, 1f);
        public static readonly Color ColorZetAspects = new Color(1.0f, 0.55f, 0.05f, 1f);
        public static readonly Color ColorEquipment = new Color(0.95f, 0.55f, 0.15f, 1f);
        public static readonly Color ColorLunarVoid = new Color(0.70f, 0.45f, 1.0f, 1f);

        private static readonly Dictionary<PickupIndex, (Sprite? sprite, Texture? tex, Rect uv)> _iconCache =
            new Dictionary<PickupIndex, (Sprite?, Texture?, Rect)>();

        public static List<PickupIndex>? GetListForTier(RouletteTierType tier, bool forceLunarEquipment = false)
        {
            if (Run.instance == null) return null;

            switch (tier)
            {
                case RouletteTierType.Common:
                    return Run.instance.availableTier1DropList;
                case RouletteTierType.Uncommon:
                    return Run.instance.availableTier2DropList;
                case RouletteTierType.Legendary:
                    var redList = Run.instance.availableTier3DropList;
                    if (ModConfig.IncludeZetAspectsInPool.Value && ZetAspectsCompat.IsZetAspectsLoaded)
                    {
                        bool shouldInject = ModConfig.ZetAspectsPoolModeSetting.Value == ZetAspectsPoolMode.LegendaryTier ||
                                            (ModConfig.ZetAspectsPoolModeSetting.Value == ZetAspectsPoolMode.AutoByModTier && ZetAspectsCompat.GetAspectItemTier() == ItemTier.Tier3);
                        if (shouldInject)
                        {
                            var aspectPickups = GetFilteredZetAspectsPool();
                            if (aspectPickups != null && aspectPickups.Count > 0)
                            {
                                var combined = new List<PickupIndex>(redList ?? new List<PickupIndex>());
                                foreach (var pIdx in aspectPickups)
                                {
                                    if (!combined.Contains(pIdx)) combined.Add(pIdx);
                                }
                                return combined;
                            }
                        }
                    }
                    return redList;
                case RouletteTierType.Boss:
                    var bossList = Run.instance.availableBossDropList;
                    if (ModConfig.IncludeZetAspectsInPool.Value && ZetAspectsCompat.IsZetAspectsLoaded)
                    {
                        bool shouldInject = ModConfig.ZetAspectsPoolModeSetting.Value == ZetAspectsPoolMode.BossTier ||
                                            (ModConfig.ZetAspectsPoolModeSetting.Value == ZetAspectsPoolMode.AutoByModTier && ZetAspectsCompat.GetAspectItemTier() == ItemTier.Boss);
                        if (shouldInject)
                        {
                            var aspectPickups = GetFilteredZetAspectsPool();
                            if (aspectPickups != null && aspectPickups.Count > 0)
                            {
                                var combined = new List<PickupIndex>(bossList ?? new List<PickupIndex>());
                                foreach (var pIdx in aspectPickups)
                                {
                                    if (!combined.Contains(pIdx)) combined.Add(pIdx);
                                }
                                return combined;
                            }
                        }
                    }
                    else if (!ZetAspectsCompat.IsZetAspectsLoaded && ModConfig.IncludeVanillaAspectsInRoulette.Value)
                    {
                        if (ModConfig.VanillaAspectsPoolModeSetting.Value == VanillaAspectsPoolMode.BossTier)
                        {
                            var aspectPickups = GetFilteredVanillaAspectsPool();
                            if (aspectPickups != null && aspectPickups.Count > 0)
                            {
                                var combined = new List<PickupIndex>(bossList ?? new List<PickupIndex>());
                                foreach (var pIdx in aspectPickups)
                                {
                                    if (!combined.Contains(pIdx)) combined.Add(pIdx);
                                }
                                return combined;
                            }
                        }
                    }
                    return bossList;
                case RouletteTierType.Lunar:
                    return GetFilteredLunarPool(forceLunarEquipment);
                case RouletteTierType.Void:
                    var list = new List<PickupIndex>();
                    if (Run.instance.availableVoidTier1DropList != null) list.AddRange(Run.instance.availableVoidTier1DropList);
                    if (Run.instance.availableVoidTier2DropList != null) list.AddRange(Run.instance.availableVoidTier2DropList);
                    if (Run.instance.availableVoidTier3DropList != null) list.AddRange(Run.instance.availableVoidTier3DropList);
                    if (Run.instance.availableVoidBossDropList != null) list.AddRange(Run.instance.availableVoidBossDropList);
                    return list.Count > 0 ? list : null;
                case RouletteTierType.VoidPotential:
                    return Run.instance.availableTier2DropList;
                case RouletteTierType.LunarVoid:
                    var lvList = new List<PickupIndex>();
                    var vList = GetListForTier(RouletteTierType.Void);
                    if (vList != null) lvList.AddRange(vList);
                    var lList = GetFilteredPassiveLunarPool();
                    if (lList != null) lvList.AddRange(lList);
                    return lvList.Count > 0 ? lvList : null;
                case RouletteTierType.ZetAspects:
                    return GetFilteredZetAspectsPool();
                case RouletteTierType.Equipment:
                case RouletteTierType.QualityEquipment:
                    return GetFilteredEquipmentPool();
                case RouletteTierType.VanillaAspect:
                    return GetFilteredVanillaAspectsPool();
                default:
                    return Run.instance.availableTier1DropList;
            }
        }

        public static List<PickupIndex>? GetFilteredVanillaAspectsPool()
        {
            if (Run.instance == null) return null;

            var result = new List<PickupIndex>();
            var seen = new HashSet<EquipmentIndex>();

            void TryAdd(EquipmentIndex eqIndex, bool allowed)
            {
                if (allowed && eqIndex != EquipmentIndex.None && seen.Add(eqIndex))
                {
                    var pIndex = PickupCatalog.FindPickupIndex(eqIndex);
                    if (pIndex != PickupIndex.none)
                    {
                        result.Add(pIndex);
                    }
                }
            }

            // 1. Base RoR2 Elite Equipment
            TryAdd(RoR2Content.Equipment.AffixRed.equipmentIndex, ModConfig.AllowAspectFire.Value);
            TryAdd(RoR2Content.Equipment.AffixBlue.equipmentIndex, ModConfig.AllowAspectLightning.Value);
            TryAdd(RoR2Content.Equipment.AffixWhite.equipmentIndex, ModConfig.AllowAspectIce.Value);
            TryAdd(RoR2Content.Equipment.AffixPoison.equipmentIndex, ModConfig.AllowAspectMalachite.Value);
            TryAdd(RoR2Content.Equipment.AffixHaunted.equipmentIndex, ModConfig.AllowAspectCelestine.Value);
            TryAdd(RoR2Content.Equipment.AffixLunar.equipmentIndex, ModConfig.AllowAspectPerfected.Value);

            // 2. DLC Equipment
            try
            {
                if (DLC1Content.Equipment.EliteEarthEquipment != null)
                {
                    TryAdd(DLC1Content.Equipment.EliteEarthEquipment.equipmentIndex, ModConfig.AllowAspectMending.Value);
                }
                if (DLC1Content.Equipment.EliteVoidEquipment != null)
                {
                    TryAdd(DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex, ModConfig.AllowAspectVoid.Value);
                }
            }
            catch { }

            // 3. Modded Elite Equipment from EliteCatalog
            try
            {
                if (EliteCatalog.eliteList != null)
                {
                    foreach (var eliteIndex in EliteCatalog.eliteList)
                    {
                        var eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                        if (eliteDef != null && eliteDef.eliteEquipmentDef != null)
                        {
                            var eqIndex = eliteDef.eliteEquipmentDef.equipmentIndex;
                            if (eqIndex != EquipmentIndex.None && !seen.Contains(eqIndex))
                            {
                                TryAdd(eqIndex, true);
                            }
                        }
                    }
                }
            }
            catch { }

            // 4. Blacklist filter
            string blacklist = ModConfig.VanillaAspectsCustomBlacklist.Value;
            if (!string.IsNullOrWhiteSpace(blacklist) && result.Count > 0)
            {
                string[] blacklistEntries = blacklist.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = result.Count - 1; i >= 0; i--)
                {
                    var pDef = PickupCatalog.GetPickupDef(result[i]);
                    if (pDef == null) continue;
                    var eqDef = EquipmentCatalog.GetEquipmentDef(pDef.equipmentIndex);
                    string eqName = eqDef?.name ?? "";
                    string token = pDef.nameToken ?? "";
                    string locName = Language.GetString(token);

                    bool isBlacklisted = false;
                    foreach (var raw in blacklistEntries)
                    {
                        string b = raw.Trim();
                        if (string.IsNullOrEmpty(b)) continue;
                        if (eqName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            token.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            locName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isBlacklisted = true;
                            break;
                        }
                    }
                    if (isBlacklisted)
                    {
                        result.RemoveAt(i);
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        public static List<PickupIndex> GetBaseEquipmentPool()
        {
            var eqList = Run.instance?.availableEquipmentDropList;
            if (eqList != null && eqList.Count > 0)
            {
                return eqList;
            }

            var fallback = new List<PickupIndex>();
            try
            {
                if (EquipmentCatalog.equipmentCount > 0)
                {
                    foreach (var eqIndex in EquipmentCatalog.allEquipment)
                    {
                        var eqDef = EquipmentCatalog.GetEquipmentDef(eqIndex);
                        if (eqDef != null && eqDef.canDrop && !eqDef.isLunar)
                        {
                            var pIdx = PickupCatalog.FindPickupIndex(eqIndex);
                            if (pIdx != PickupIndex.none)
                            {
                                fallback.Add(pIdx);
                            }
                        }
                    }
                }
            }
            catch { }

            return fallback;
        }

        public static List<PickupIndex>? GetFilteredEquipmentPool()
        {
            if (Run.instance == null) return null;

            // Check if aspect replaces equipment roll
            if (ModConfig.IncludeAspectsInEquipmentPool.Value)
            {
                float aspectRate = Mathf.Clamp(ModConfig.AspectDropRateInEquipmentPool.Value, 0f, 100f);
                if (aspectRate > 0f && UnityEngine.Random.Range(0f, 100f) < aspectRate)
                {
                    List<PickupIndex>? aspects = null;
                    if (ZetAspectsCompat.IsZetAspectsLoaded)
                    {
                        aspects = GetFilteredZetAspectsPool();
                    }
                    if (aspects == null || aspects.Count == 0)
                    {
                        aspects = GetFilteredVanillaAspectsPool();
                    }

                    if (aspects != null && aspects.Count > 0)
                    {
                        return aspects;
                    }
                }
            }

            var basePool = GetBaseEquipmentPool();
            return basePool.Count > 0 ? basePool : null;
        }

        public static List<PickupIndex>? GetFilteredZetAspectsPool()
        {
            if (!ZetAspectsCompat.IsZetAspectsLoaded) return null;
            var allAspects = ZetAspectsCompat.GetAspectPickupIndexes();
            if (allAspects == null || allAspects.Count == 0) return null;

            var result = new List<PickupIndex>();
            string blacklist = ModConfig.ZetAspectsCustomBlacklist.Value;
            string[]? blacklistEntries = !string.IsNullOrWhiteSpace(blacklist)
                ? blacklist.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                : null;

            foreach (var pIdx in allAspects)
            {
                var pDef = PickupCatalog.GetPickupDef(pIdx);
                if (pDef == null) continue;

                string itemName = "";
                string token = pDef.nameToken ?? "";
                string localizedName = Language.GetString(token);

                if (pDef.itemIndex != ItemIndex.None)
                {
                    var itemDef = ItemCatalog.GetItemDef(pDef.itemIndex);
                    if (itemDef != null)
                    {
                        itemName = itemDef.name ?? "";

                        if (!ModConfig.ZetAspectsAllowVoidAspect.Value &&
                            (itemName.IndexOf("void", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             token.IndexOf("void", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            continue;
                        }

                        if (!ModConfig.ZetAspectsAllowLunarAspect.Value &&
                            (itemName.IndexOf("lunar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             itemName.IndexOf("perfect", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             token.IndexOf("lunar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             token.IndexOf("perfect", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            continue;
                        }
                    }
                }

                if (blacklistEntries != null && blacklistEntries.Length > 0)
                {
                    bool isBlacklisted = false;
                    foreach (var raw in blacklistEntries)
                    {
                        string b = raw.Trim();
                        if (string.IsNullOrEmpty(b)) continue;
                        if (itemName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            token.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            localizedName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isBlacklisted = true;
                            break;
                        }
                    }
                    if (isBlacklisted) continue;
                }

                result.Add(pIdx);
            }

            return result.Count > 0 ? result : null;
        }

        public static List<PickupIndex>? GetFilteredLunarPool(bool forceIncludeEquipment = false)
        {
            if (Run.instance == null) return null;

            var baseList = Run.instance.availableLunarItemDropList;
            var result = new List<PickupIndex>();

            if (baseList != null)
            {
                foreach (var pIdx in baseList)
                {
                    if (IsLunarPickupAllowed(pIdx))
                    {
                        result.Add(pIdx);
                    }
                }
            }

            if ((forceIncludeEquipment || ModConfig.IncludeLunarEquipment.Value) && Run.instance.availableLunarEquipmentDropList != null)
            {
                foreach (var pIdx in Run.instance.availableLunarEquipmentDropList)
                {
                    if (IsLunarPickupAllowed(pIdx) && !result.Contains(pIdx))
                    {
                        result.Add(pIdx);
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        public static List<PickupIndex>? GetFilteredPassiveLunarPool()
        {
            if (Run.instance == null) return null;

            var baseList = Run.instance.availableLunarItemDropList;
            if (baseList == null) return null;

            var result = new List<PickupIndex>();
            foreach (var pIdx in baseList)
            {
                var pDef = PickupCatalog.GetPickupDef(pIdx);
                if (pDef != null && pDef.equipmentIndex == EquipmentIndex.None && IsLunarPickupAllowed(pIdx))
                {
                    result.Add(pIdx);
                }
            }

            return result.Count > 0 ? result : null;
        }

        public static bool IsLunarPickupAllowed(PickupIndex pickupIndex)
        {
            var pDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pDef == null) return false;

            string itemName = "";
            string token = "";
            string localizedName = "";

            if (pDef.itemIndex != ItemIndex.None)
            {
                var itemDef = ItemCatalog.GetItemDef(pDef.itemIndex);
                if (itemDef != null)
                {
                    itemName = itemDef.name ?? "";
                    token = itemDef.nameToken ?? "";
                    localizedName = Language.GetString(token);

                    // 20 Individual Lunar item toggles:
                    if (!ModConfig.AllowShapedGlass.Value && (itemName == "LunarDagger" || itemName.IndexOf("ShapedGlass", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARDAGGER", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowBrittleCrown.Value && (itemName == "GoldOnHit" || itemName.IndexOf("BrittleCrown", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("GOLDONHIT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowTranscendence.Value && (itemName == "ShieldOnly" || itemName.IndexOf("Transcendence", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("SHIELDONLY", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowCorpsebloom.Value && (itemName == "RepeatHeal" || itemName.IndexOf("Corpsebloom", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("REPEATHEAL", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowGestureOfTheDrowned.Value && (itemName == "AutoCastEquipment" || itemName.IndexOf("Gesture", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("AUTOCASTEQUIPMENT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowBeadsOfFealty.Value && (itemName == "LunarTrinket" || itemName.IndexOf("Beads", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARTRINKET", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowFocusedConvergence.Value && (itemName == "FocusConvergence" || itemName.IndexOf("FocusedConvergence", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("FOCUSEDCONVERGENCE", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("FOCUSCONVERGENCE", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowDefiantGouge.Value && (itemName == "MonstersOnShrineUse" || itemName == "MonksEarring" || itemName.IndexOf("DefiantGouge", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("MONSTERSONSHRINEUSE", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowMercurialRachis.Value && (itemName == "RandomDamageZone" || itemName.IndexOf("MercurialRachis", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("RANDOMDAMAGEZONE", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowPurity.Value && (itemName == "LunarBadLuck" || itemName.IndexOf("Purity", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARBADLUCK", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowVisionsOfHeresy.Value && (itemName == "LunarPrimaryReplacement" || itemName.IndexOf("Vision", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARPRIMARYREPLACEMENT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowHooksOfHeresy.Value && (itemName == "LunarSecondaryReplacement" || itemName.IndexOf("Hook", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARSECONDARYREPLACEMENT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowStridesOfHeresy.Value && (itemName == "LunarUtilityReplacement" || itemName.IndexOf("Stride", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARUTILITYREPLACEMENT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowEssenceOfHeresy.Value && (itemName == "LunarSpecialReplacement" || itemName.IndexOf("Essence", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARSPECIALREPLACEMENT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowEulogyZero.Value && (itemName == "RandomlyLunar" || itemName.IndexOf("Eulogy", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("RANDOMLYLUNAR", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowEgocentrism.Value && (itemName == "LunarSun" || itemName.IndexOf("Egocentrism", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("LUNARSUN", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowStoneFluxPauldron.Value && (itemName == "HalfSpeedDoubleHealth" || itemName.IndexOf("StoneFlux", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("HALFSPEEDDOUBLEHEALTH", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowLightFluxPauldron.Value && (itemName == "HalfAttackSpeedHalfCooldowns" || itemName.IndexOf("LightFlux", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("HALFATTACKSPEEDHALFCOOLDOWNS", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowLongstandingSolitude.Value && (itemName == "OnLevelUpFreeUnlock" || itemName.IndexOf("Solitude", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("ONLEVELUPFREEUNLOCK", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowNeutroniumWeight.Value && (itemName == "TransferDebuffOnHit" || itemName.IndexOf("Neutronium", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("TRANSFERDEBUFFONHIT", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;
                }
            }
            else if (pDef.equipmentIndex != EquipmentIndex.None)
            {
                if (!ModConfig.IncludeLunarEquipment.Value)
                    return false;

                var eqDef = EquipmentCatalog.GetEquipmentDef(pDef.equipmentIndex);
                if (eqDef != null)
                {
                    itemName = eqDef.name ?? "";
                    token = eqDef.nameToken ?? "";
                    localizedName = Language.GetString(token);

                    // 5 Lunar Equipment toggles:
                    if (!ModConfig.AllowSpinelTonic.Value && (itemName == "LunarPotion" || itemName == "Tonic" || itemName.IndexOf("Tonic", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("TONIC", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowGlowingMeteorite.Value && (itemName == "Meteor" || itemName.IndexOf("Meteor", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("METEOR", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowHelfireTincture.Value && (itemName == "BurnNearby" || itemName.IndexOf("Helfire", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("BURNNEARBY", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowEffigyOfGrief.Value && (itemName == "CrippleWard" || itemName.IndexOf("Effigy", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("CRIPPLEWARD", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;

                    if (!ModConfig.AllowTotemOfSacrifice.Value && (itemName == "SpawnAltar" || itemName.IndexOf("Totem", StringComparison.OrdinalIgnoreCase) >= 0 || token.IndexOf("SPAWNALTAR", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;
                }
            }

            // Custom blacklist check
            string customBlacklist = ModConfig.LunarCustomBlacklist.Value;
            if (!string.IsNullOrWhiteSpace(customBlacklist))
            {
                var entries = customBlacklist.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawEntry in entries)
                {
                    string entry = rawEntry.Trim();
                    if (string.IsNullOrEmpty(entry)) continue;

                    if (itemName.IndexOf(entry, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        token.IndexOf(entry, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        localizedName.IndexOf(entry, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a dynamic, weighted item pool from a dictionary of tiers and their relative weights.
        /// </summary>
        public static CustomRoulettePool CreatePool(IDictionary<RouletteTierType, float> weights)
        {
            return new CustomRoulettePool(weights);
        }

        /// <summary>
        /// Creates a dynamic, weighted item pool from an array of tier-weight tuples.
        /// </summary>
        public static CustomRoulettePool CreatePool(params (RouletteTierType tier, float weight)[] weights)
        {
            return new CustomRoulettePool(weights);
        }

        /// <summary>
        /// Rolls an item directly from a custom set of tier weights mid-game without retaining a pool instance.
        /// </summary>
        public static RouletteItemEntry? RollCustom(
            IDictionary<RouletteTierType, float> weights,
            global::Xoroshiro128Plus? rng = null,
            CharacterMaster? master = null,
            ChaosChances? chances = null,
            bool applyPlayerTierMultiplier = false,
            bool applyChanceDoll = false)
        {
            return new CustomRoulettePool(weights).Roll(rng, master, chances, applyPlayerTierMultiplier, applyChanceDoll);
        }

        /// <summary>
        /// Rolls an item directly from custom tier-weight tuples mid-game.
        /// </summary>
        public static RouletteItemEntry? RollCustom(
            (RouletteTierType tier, float weight)[] weights,
            global::Xoroshiro128Plus? rng = null,
            CharacterMaster? master = null,
            ChaosChances? chances = null,
            bool applyPlayerTierMultiplier = false,
            bool applyChanceDoll = false)
        {
            return new CustomRoulettePool(weights).Roll(rng, master, chances, applyPlayerTierMultiplier, applyChanceDoll);
        }

        public static RouletteItemEntry? RollRandomEntry(
            global::Xoroshiro128Plus? rng = null, 
            ChaosChances? chances = null, 
            RouletteTierType? forcedTier = null,
            CharacterMaster? master = null,
            bool guaranteeQuality = false,
            float solitudeRarityBonus = 0f,
            QualityTierCompat minQualityTier = QualityTierCompat.None)
        {
            if (Run.instance == null) return null;

            if (forcedTier.HasValue)
            {
                if (forcedTier.Value == RouletteTierType.BossReward)
                {
                    return CustomRoulettePool.CreateBossRewardPool().Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
                }

                if (forcedTier.Value == RouletteTierType.HighTierPylon)
                {
                    return CustomRoulettePool.CreateHighTierPylonPool().Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
                }

                if (forcedTier.Value == RouletteTierType.VoidPotential)
                {
                    return CustomRoulettePool.CreateVoidPotentialPool().Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
                }

                if (forcedTier.Value == RouletteTierType.QualityEquipment)
                {
                    return new CustomRoulettePool((RouletteTierType.QualityEquipment, 100f)).Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: true, minQualityTier: minQualityTier);
                }

                if (forcedTier.Value == RouletteTierType.Lunar)
                {
                    return CustomRoulettePool.CreateLunarChestPool().Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
                }

                if (forcedTier.Value == RouletteTierType.LunarVoid)
                {
                    return CustomRoulettePool.CreateLunarVoidPool().Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
                }

                return new CustomRoulettePool((forcedTier.Value, 100f)).Roll(rng, master, chances, applyPlayerTierMultiplier: false, guaranteeQuality: guaranteeQuality, minQualityTier: minQualityTier);
            }

            return CustomRoulettePool.CreateDefaultPaidPool().Roll(rng, master, chances, applyPlayerTierMultiplier: true, guaranteeQuality: guaranteeQuality, solitudeRarityBonus: solitudeRarityBonus, minQualityTier: minQualityTier);
        }

        public static RouletteItemEntry CreateEntry(PickupIndex pickupIndex, RouletteTierType tierType)
        {
            PickupDef pDef = PickupCatalog.GetPickupDef(pickupIndex);
            var (sprite, tex, uv) = ResolveIcon(pickupIndex, pDef);

            string name = pDef != null ? Language.GetString(pDef.nameToken) : Localization.Get("Предмет", "Item");
            if (pDef != null && (string.IsNullOrEmpty(name) || name == pDef.nameToken || name.StartsWith("ITEM_", StringComparison.OrdinalIgnoreCase)))
            {
                if (pDef.itemIndex != ItemIndex.None)
                {
                    name = ChaosEventManager.GetItemName(pDef.itemIndex);
                }
            }
            if (!string.IsNullOrEmpty(name))
            {
                name = Regex.Replace(name, @"<sprite\b[^>]*>", "").Trim();
            }
            Color color;
            string tierLabel;

            switch (tierType)
            {
                case RouletteTierType.Common:
                    color = ColorCommon;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_COMMON", "Обычный", "Common");
                    break;
                case RouletteTierType.Uncommon:
                    color = ColorUncommon;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_UNCOMMON", "Необычный", "Uncommon");
                    break;
                case RouletteTierType.Legendary:
                    color = ColorLegendary;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_LEGENDARY", "Легендарный", "Legendary");
                    break;
                case RouletteTierType.Boss:
                    color = ColorBoss;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_BOSS", "Босс", "Boss");
                    break;
                case RouletteTierType.Lunar:
                    color = ColorLunar;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_LUNAR", "Лунный", "Lunar");
                    break;
                case RouletteTierType.Void:
                    color = ColorVoid;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_VOID", "Бездна", "Void");
                    break;
                case RouletteTierType.VoidPotential:
                    color = ColorVoidPotential;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_VOID_POTENTIAL", "Потенциал Бездны", "Void Potential");
                    break;
                case RouletteTierType.HighTierPylon:
                    color = ColorHighTierPylon;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_HIGH_TIER_PYLON", "Лунный Пилон", "Lunar Pylon");
                    break;
                case RouletteTierType.ZetAspects:
                    color = ColorZetAspects;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_ASPECT", "Аспект", "Aspect");
                    break;
                case RouletteTierType.Equipment:
                    color = ColorEquipment;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_EQUIPMENT", "Снаряжение", "Equipment");
                    break;
                case RouletteTierType.QualityEquipment:
                    color = ColorEquipment;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_QUALITY_EQUIPMENT", "Снаряжение Качества", "Quality Equipment");
                    break;
                case RouletteTierType.VanillaAspect:
                    color = ColorZetAspects;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_ASPECT", "Аспект", "Aspect");
                    break;
                case RouletteTierType.LunarVoid:
                    color = ColorLunarVoid;
                    tierLabel = Localization.Get("ITEMROULETTE_TIER_LUNAR_VOID", "Бездна / Луна", "Void / Lunar");
                    break;
                default:
                    color = pDef != null ? pDef.baseColor : ColorCommon;
                    tierLabel = Localization.Get("ITEMROULETTE_WORD_ITEM", "Предмет", "Item");
                    break;
            }

            var entry = new RouletteItemEntry
            {
                PickupIndex = pickupIndex,
                PickupDef = pDef!,
                LocalizedName = name,
                RarityColor = color,
                IconSprite = sprite,
                IconTexture = tex,
                SpriteUvRect = uv,
                TierType = tierType,
                TierLabel = tierLabel
            };

            if (QualityCompat.IsQualityLoaded)
            {
                var qTier = QualityCompat.GetQualityTier(pickupIndex);
                if (qTier != QualityTierCompat.None)
                {
                    entry.QualityTier = qTier;
                    entry.QualityColor = QualityCompat.GetQualityColor(qTier);
                    entry.QualityLabel = QualityCompat.GetQualityName(qTier);
                }
            }

            return entry;
        }

        public static (Sprite? sprite, Texture? tex, Rect uv) ResolveIcon(PickupIndex pIndex, PickupDef? pDef = null)
        {
            if (pDef == null) pDef = PickupCatalog.GetPickupDef(pIndex);
            if (pDef == null) return (null, null, new Rect(0, 0, 1, 1));

            if (_iconCache.TryGetValue(pIndex, out var cached))
            {
                return cached;
            }

            Sprite? sprite = null;

            // 1. Direct sprite on PickupDef
            if (pDef.iconSprite != null)
            {
                sprite = pDef.iconSprite;
            }

            // 2. ItemDef sprite or Addressables Reference
            if (sprite == null && pDef.itemIndex != ItemIndex.None)
            {
                var itemDef = ItemCatalog.GetItemDef(pDef.itemIndex);
                if (itemDef != null)
                {
                    if (itemDef.pickupIconSprite != null)
                    {
                        sprite = itemDef.pickupIconSprite;
                    }
                    else if (itemDef.pickupIconSpriteReference != null)
                    {
                        if (itemDef.pickupIconSpriteReference.Asset != null)
                        {
                            sprite = itemDef.pickupIconSpriteReference.Asset as Sprite;
                        }
                        else
                        {
                            try
                            {
                                var handle = itemDef.pickupIconSpriteReference.LoadAssetAsync<Sprite>();
                                sprite = handle.WaitForCompletion();
                            }
                            catch { }
                        }
                    }
                }
            }

            // 3. PickupDef Addressables Reference
            if (sprite == null && pDef.iconSpriteReference != null)
            {
                if (pDef.iconSpriteReference.Asset != null)
                {
                    sprite = pDef.iconSpriteReference.Asset as Sprite;
                }
                else
                {
                    try
                    {
                        var handle = pDef.iconSpriteReference.LoadAssetAsync<Sprite>();
                        sprite = handle.WaitForCompletion();
                    }
                    catch { }
                }
            }

            // 4. EquipmentDef sprite or Addressables Reference
            if (sprite == null && pDef.equipmentIndex != EquipmentIndex.None)
            {
                var equipDef = EquipmentCatalog.GetEquipmentDef(pDef.equipmentIndex);
                if (equipDef != null)
                {
                    if (equipDef.pickupIconSprite != null)
                    {
                        sprite = equipDef.pickupIconSprite;
                    }
                    else if (equipDef.pickupIconSpriteReference != null)
                    {
                        if (equipDef.pickupIconSpriteReference.Asset != null)
                        {
                            sprite = equipDef.pickupIconSpriteReference.Asset as Sprite;
                        }
                        else
                        {
                            try
                            {
                                var handle = equipDef.pickupIconSpriteReference.LoadAssetAsync<Sprite>();
                                sprite = handle.WaitForCompletion();
                            }
                            catch { }
                        }
                    }
                }
            }

            // Format Texture & UV
            Texture? finalTex = null;
            Rect uv = new Rect(0, 0, 1, 1);

            if (sprite != null && sprite.texture != null)
            {
                finalTex = sprite.texture;
                Rect tr = sprite.rect;
                float tw = finalTex.width;
                float th = finalTex.height;
                if (tw > 0 && th > 0)
                {
                    uv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
                }
            }
            else if (pDef.iconTexture != null)
            {
                finalTex = pDef.iconTexture;
            }

            var result = (sprite, finalTex, uv);
            _iconCache[pIndex] = result;
            return result;
        }

        private static readonly MethodInfo? _handleDuplicator = typeof(ItemDef)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name.StartsWith("<AttemptGrant>g__HandleDuplicator|", StringComparison.Ordinal));

        /// <summary>
        /// Replicates vanilla Substandard Duplicator (DLC3) behavior for items awarded via ItemRoulette.
        /// Invokes vanilla/modded HandleDuplicator logic to preserve visual effects and modded Quality tier scaling.
        /// </summary>
        public static void ApplySubstandardDuplicator(CharacterMaster? master, ItemIndex itemIndex)
        {
            if (master == null || master.inventory == null || itemIndex == ItemIndex.None) return;
            var inventory = master.inventory;

            try
            {
                if (DLC3Content.Items.Duplicator == null || inventory.GetItemCountEffective(DLC3Content.Items.Duplicator.itemIndex) <= 0)
                {
                    return;
                }

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef == null) return;

                var toGrant = ItemIndex.None;
                if (itemDef.ContainsTag(ItemTag.CanBeTemporary))
                {
                    toGrant = itemIndex;
                }
                else if (itemDef.DoesNotContainTag(ItemTag.ObjectiveRelated) &&
                         itemDef.DoesNotContainTag(ItemTag.Scrap) &&
                         itemDef.DoesNotContainTag(ItemTag.WorldUnique))
                {
                    toGrant = Util.RollTemporaryItemFromItemIndex(itemIndex);
                }

                if (toGrant == ItemIndex.None) return;

                var body = master.GetBody();
                if (body != null && _handleDuplicator != null)
                {
                    _handleDuplicator.Invoke(null, new object[] { toGrant, body, 1f });
                    return;
                }

                if (body != null && CharacterBody.CommonAssets.duplicatorPrefab != null)
                {
                    EffectManager.SpawnEffect(CharacterBody.CommonAssets.duplicatorPrefab,
                        new EffectData { origin = body.corePosition, start = body.corePosition, scale = body.radius }, true);
                }
                inventory.GiveItemTemp(toGrant, 1f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] ApplySubstandardDuplicator failed for item {itemIndex}: {ex}");
            }
        }
    }
}
