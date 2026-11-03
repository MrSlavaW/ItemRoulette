using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Compat
{
    public enum QualityTierCompat
    {
        None = -1,
        Uncommon = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    public static class QualityCompat
    {
        public const string ModGuid = "com.Gorakh.ItemQualities";

        private static bool _initialized = false;
        private static bool _isLoaded = false;

        private static Type? _qualityCatalogType;
        private static Type? _qualityTierType;
        private static Type? _itemQualityGroupType;
        private static Type? _dropTableQualityHandlerType;
        private static Type? _configsGeneralType;

        // QualityCatalog methods
        private static MethodInfo? _getQualityTierItemMethod;
        private static MethodInfo? _getQualityTierPickupMethod;
        private static MethodInfo? _getItemIndexOfQualityMethod;
        private static MethodInfo? _getEquipmentIndexOfQualityMethod;
        private static MethodInfo? _getPickupIndexOfQualityMethod;
        private static MethodInfo? _getItemQualityGroupMethod;
        private static MethodInfo? _getQualityTierDefMethod;

        // ItemQualityGroup members
        private static FieldInfo? _baseItemIndexField;
        private static MethodInfo? _groupGetItemIndexMethod;

        // Configs.General members
        private static PropertyInfo? _globalQualityChanceProp;

        // ObjectPurchaseContext & ItemQualityCounts members (for chest Sale Star interaction)
        private static Type? _objectPurchaseContextType;
        private static PropertyInfo? _opcResultsProp;
        private static FieldInfo? _usedSaleStarCountsField;
        private static Type? _itemQualityCountsType;
        private static MethodInfo? _qualityCountsGetTotalQualityMethod;
        private static MethodInfo? _qualityCountsGetHighestQualityMethod;

        public static bool IsQualityLoaded
        {
            get
            {
                if (!_initialized)
                {
                    Init();
                }
                return _isLoaded && ModConfig.EnableQualityIntegration.Value;
            }
        }

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _isLoaded = Chainloader.PluginInfos.ContainsKey(ModGuid);
                if (_isLoaded)
                {
                    Assembly? qualityAssembly = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name == "ItemQualities")
                        {
                            qualityAssembly = asm;
                            break;
                        }
                    }

                    if (qualityAssembly != null)
                    {
                        _qualityCatalogType = qualityAssembly.GetType("ItemQualities.QualityCatalog");
                        _qualityTierType = qualityAssembly.GetType("ItemQualities.QualityTier");
                        _itemQualityGroupType = qualityAssembly.GetType("ItemQualities.ItemQualityGroup");
                        _dropTableQualityHandlerType = qualityAssembly.GetType("ItemQualities.DropTableQualityHandler");
                        _configsGeneralType = qualityAssembly.GetType("ItemQualities.Configs+General");

                        if (_qualityCatalogType != null)
                        {
                            _getQualityTierItemMethod = _qualityCatalogType.GetMethod("GetQualityTier", new[] { typeof(ItemIndex) });
                            _getQualityTierPickupMethod = _qualityCatalogType.GetMethod("GetQualityTier", new[] { typeof(PickupIndex) });
                            _getItemIndexOfQualityMethod = _qualityCatalogType.GetMethod("GetItemIndexOfQuality", new[] { typeof(ItemIndex), _qualityTierType ?? typeof(int) });
                            _getEquipmentIndexOfQualityMethod = _qualityCatalogType.GetMethod("GetEquipmentIndexOfQuality", new[] { typeof(EquipmentIndex), _qualityTierType ?? typeof(int) });
                            _getPickupIndexOfQualityMethod = _qualityCatalogType.GetMethod("GetPickupIndexOfQuality", new[] { typeof(PickupIndex), _qualityTierType ?? typeof(int) });
                            _getItemQualityGroupMethod = _qualityCatalogType.GetMethod("GetItemQualityGroup", new[] { typeof(ItemIndex) });
                            _getQualityTierDefMethod = _qualityCatalogType.GetMethod("GetQualityTierDef", new[] { _qualityTierType ?? typeof(int) });
                        }

                        if (_itemQualityGroupType != null)
                        {
                            _baseItemIndexField = _itemQualityGroupType.GetField("BaseItemIndex", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            _groupGetItemIndexMethod = _itemQualityGroupType.GetMethod("GetItemIndex", new[] { _qualityTierType ?? typeof(int) });
                        }

                        if (_configsGeneralType != null)
                        {
                            var field = _configsGeneralType.GetField("GlobalQualityChance", BindingFlags.Public | BindingFlags.Static);
                            if (field != null)
                            {
                                var configEntry = field.GetValue(null);
                                if (configEntry != null)
                                {
                                    _globalQualityChanceProp = configEntry.GetType().GetProperty("Value");
                                }
                            }
                        }

                        _objectPurchaseContextType = qualityAssembly.GetType("ItemQualities.ObjectPurchaseContext");
                        _itemQualityCountsType = qualityAssembly.GetType("ItemQualities.ItemQualityCounts");
                        if (_objectPurchaseContextType != null)
                        {
                            _opcResultsProp = _objectPurchaseContextType.GetProperty("Results", BindingFlags.Public | BindingFlags.Instance);
                            var purchaseResultsType = qualityAssembly.GetType("ItemQualities.ObjectPurchaseContext+PurchaseResults");
                            if (purchaseResultsType != null)
                            {
                                _usedSaleStarCountsField = purchaseResultsType.GetField("UsedSaleStarCounts", BindingFlags.Public | BindingFlags.Instance);
                            }
                        }
                        if (_itemQualityCountsType != null)
                        {
                            _qualityCountsGetTotalQualityMethod = _itemQualityCountsType.GetMethod("get_TotalQualityCount", BindingFlags.Public | BindingFlags.Instance);
                            _qualityCountsGetHighestQualityMethod = _itemQualityCountsType.GetMethod("get_HighestQuality", BindingFlags.Public | BindingFlags.Instance);
                        }

                        Debug.Log("[ItemRoulette] Successfully hooked into Goorakh-Quality (ItemQualities.dll).");
                    }
                    else
                    {
                        Debug.LogWarning("[ItemRoulette] ItemQualities plugin GUID detected, but assembly 'ItemQualities' not found in AppDomain.");
                        _isLoaded = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to initialize QualityCompat: {ex}");
                _isLoaded = false;
            }
        }

        public static QualityTierCompat GetQualityTier(ItemIndex itemIndex)
        {
            if (!IsQualityLoaded || _getQualityTierItemMethod == null || itemIndex == ItemIndex.None)
                return QualityTierCompat.None;

            try
            {
                object? res = _getQualityTierItemMethod.Invoke(null, new object[] { itemIndex });
                if (res != null)
                {
                    return (QualityTierCompat)(int)res;
                }
            }
            catch { }

            return QualityTierCompat.None;
        }

        public static QualityTierCompat GetQualityTier(PickupIndex pickupIndex)
        {
            if (!IsQualityLoaded || _getQualityTierPickupMethod == null || pickupIndex == PickupIndex.none)
                return QualityTierCompat.None;

            try
            {
                object? res = _getQualityTierPickupMethod.Invoke(null, new object[] { pickupIndex });
                if (res != null)
                {
                    return (QualityTierCompat)(int)res;
                }
            }
            catch { }

            return QualityTierCompat.None;
        }

        public static ItemIndex GetItemIndexOfQuality(ItemIndex baseItem, QualityTierCompat tier)
        {
            if (!IsQualityLoaded || _getItemIndexOfQualityMethod == null || baseItem == ItemIndex.None)
                return baseItem;

            try
            {
                ItemIndex actualBase = GetBaseItemIndex(baseItem);
                object? res = _getItemIndexOfQualityMethod.Invoke(null, new object[] { actualBase != ItemIndex.None ? actualBase : baseItem, (int)tier });
                if (res is ItemIndex idx && idx != ItemIndex.None)
                {
                    return idx;
                }
            }
            catch { }

            return baseItem;
        }

        public static EquipmentIndex GetEquipmentIndexOfQuality(EquipmentIndex baseEquip, QualityTierCompat tier)
        {
            if (!IsQualityLoaded || _getEquipmentIndexOfQualityMethod == null || baseEquip == EquipmentIndex.None)
                return baseEquip;

            try
            {
                object? res = _getEquipmentIndexOfQualityMethod.Invoke(null, new object[] { baseEquip, (int)tier });
                if (res is EquipmentIndex idx && idx != EquipmentIndex.None)
                {
                    return idx;
                }
            }
            catch { }

            return baseEquip;
        }

        public static PickupIndex GetPickupIndexOfQuality(PickupIndex basePickup, QualityTierCompat tier)
        {
            if (!IsQualityLoaded || basePickup == PickupIndex.none)
                return basePickup;

            try
            {
                PickupDef pDef = PickupCatalog.GetPickupDef(basePickup);
                if (pDef == null) return basePickup;

                if (pDef.equipmentIndex != EquipmentIndex.None)
                {
                    // Equipment: bypass ItemQualities's GetPickupIndexOfQuality which mistakenly checks itemIndex (-1) and returns group 0 (Rusty Key)!
                    EquipmentIndex upgradedEquip = GetEquipmentIndexOfQuality(pDef.equipmentIndex, tier);
                    if (upgradedEquip != EquipmentIndex.None && upgradedEquip != pDef.equipmentIndex)
                    {
                        var pIndex = PickupCatalog.FindPickupIndex(upgradedEquip);
                        if (pIndex != PickupIndex.none) return pIndex;
                    }
                    return basePickup;
                }

                if (pDef.itemIndex != ItemIndex.None)
                {
                    ItemIndex upgradedItem = GetItemIndexOfQuality(pDef.itemIndex, tier);
                    if (upgradedItem != ItemIndex.None && upgradedItem != pDef.itemIndex)
                    {
                        var pIndex = PickupCatalog.FindPickupIndex(upgradedItem);
                        if (pIndex != PickupIndex.none) return pIndex;
                    }
                    return basePickup;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception calling GetPickupIndexOfQuality: {ex}");
            }

            return basePickup;
        }

        public static ItemIndex GetBaseItemIndex(ItemIndex itemIndex)
        {
            if (!IsQualityLoaded || _getItemQualityGroupMethod == null || itemIndex == ItemIndex.None)
                return itemIndex;

            try
            {
                object? group = _getItemQualityGroupMethod.Invoke(null, new object[] { itemIndex });
                if (group != null && _baseItemIndexField != null)
                {
                    object? baseIdx = _baseItemIndexField.GetValue(group);
                    if (baseIdx is ItemIndex idx && idx != ItemIndex.None)
                    {
                        return idx;
                    }
                }
            }
            catch { }

            return itemIndex;
        }

        public static Color GetQualityColor(QualityTierCompat tier)
        {
            switch (tier)
            {
                case QualityTierCompat.Uncommon:
                    return new Color(0.20f, 0.85f, 0.65f, 1f); // Teal / Emerald
                case QualityTierCompat.Rare:
                    return new Color(0.30f, 0.60f, 1.0f, 1f);  // Cyan / Blue
                case QualityTierCompat.Epic:
                    return new Color(0.75f, 0.35f, 0.95f, 1f); // Purple / Violet
                case QualityTierCompat.Legendary:
                    return new Color(1.0f, 0.70f, 0.15f, 1f);  // Gold / Amber
                default:
                    return Color.white;
            }
        }

        public static string GetQualityName(QualityTierCompat tier)
        {
            switch (tier)
            {
                case QualityTierCompat.Uncommon: return ItemRoulette.Core.Localization.Get("Необычное", "Uncommon");
                case QualityTierCompat.Rare: return ItemRoulette.Core.Localization.Get("Редкое", "Rare");
                case QualityTierCompat.Epic: return ItemRoulette.Core.Localization.Get("Эпическое", "Epic");
                case QualityTierCompat.Legendary: return ItemRoulette.Core.Localization.Get("Легендарное", "Legendary");
                default: return "";
            }
        }

        public static float GetBaseGlobalQualityChance()
        {
            if (!IsQualityLoaded || _globalQualityChanceProp == null) return 10f;
            try
            {
                object? val = _globalQualityChanceProp.GetValue(null);
                if (val is float f) return f;
            }
            catch { }
            return 10f;
        }

        public static PickupIndex RollQuality(PickupIndex basePickup, CharacterMaster? master, float extraBonusChance, bool guaranteeQuality = false, QualityTierCompat minTier = QualityTierCompat.None)
        {
            if (!IsQualityLoaded || basePickup == PickupIndex.none)
                return basePickup;

            try
            {
                float baseChance = GetBaseGlobalQualityChance() * ModConfig.QualityRollChanceMultiplier.Value;
                float finalChance = baseChance + extraBonusChance;
                finalChance = Mathf.Clamp(finalChance, 0f, 100f);

                int luck = 0;
                if (master != null && master.inventory != null)
                {
                    luck = master.inventory.GetItemCountEffective(RoR2Content.Items.Clover.itemIndex)
                         - master.inventory.GetItemCountEffective(RoR2Content.Items.LunarBadLuck.itemIndex);
                }

                bool rollSuccess = guaranteeQuality || (minTier > QualityTierCompat.None);
                if (!rollSuccess)
                {
                    // Luck roll calculation
                    for (int i = 0; i <= Mathf.Max(0, luck); i++)
                    {
                        if (UnityEngine.Random.value * 100f <= finalChance)
                        {
                            rollSuccess = true;
                            break;
                        }
                    }
                }

                if (rollSuccess)
                {
                    QualityTierCompat targetTier;
                    if (minTier > QualityTierCompat.None)
                    {
                        targetTier = minTier;
                    }
                    else
                    {
                        // Roll tier: Uncommon (60%), Rare (28%), Epic (10%), Legendary (2%)
                        float tierRoll = UnityEngine.Random.value * 100f;
                        if (tierRoll > 98f) targetTier = QualityTierCompat.Legendary;
                        else if (tierRoll > 88f) targetTier = QualityTierCompat.Epic;
                        else if (tierRoll > 60f) targetTier = QualityTierCompat.Rare;
                        else targetTier = QualityTierCompat.Uncommon;
                    }

                    PickupIndex upgraded = GetPickupIndexOfQuality(basePickup, targetTier);
                    if (upgraded != PickupIndex.none && upgraded != basePickup)
                    {
                        return upgraded;
                    }

                    // Fallback to other quality tiers so it NEVER stays plain if rollSuccess was guaranteed!
                    if (guaranteeQuality || minTier > QualityTierCompat.None)
                    {
                        foreach (var fallbackTier in new[] { QualityTierCompat.Uncommon, QualityTierCompat.Rare, QualityTierCompat.Epic, QualityTierCompat.Legendary })
                        {
                            var fb = GetPickupIndexOfQuality(basePickup, fallbackTier);
                            if (fb != PickupIndex.none && fb != basePickup)
                            {
                                return fb;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception rolling quality: {ex}");
            }

            return basePickup;
        }

        public static QualityTierCompat RollQualityTierWithMinimum(QualityTierCompat minTier)
        {
            if (minTier >= QualityTierCompat.Legendary) return QualityTierCompat.Legendary;

            float roll = UnityEngine.Random.value * 100f;
            switch (minTier)
            {
                case QualityTierCompat.Epic:
                    return (roll > 85f) ? QualityTierCompat.Legendary : QualityTierCompat.Epic;

                case QualityTierCompat.Rare:
                    if (roll > 94f) return QualityTierCompat.Legendary;
                    if (roll > 70f) return QualityTierCompat.Epic;
                    return QualityTierCompat.Rare;

                case QualityTierCompat.Uncommon:
                default:
                    if (roll > 98f) return QualityTierCompat.Legendary;
                    if (roll > 88f) return QualityTierCompat.Epic;
                    if (roll > 60f) return QualityTierCompat.Rare;
                    return QualityTierCompat.Uncommon;
            }
        }

        public static bool UpgradeItemQualityInInventory(CharacterMaster? master, out string upgradedName, out QualityTierCompat newTier)
        {
            upgradedName = "";
            newTier = QualityTierCompat.None;

            if (!IsQualityLoaded || master == null || master.inventory == null)
                return false;

            try
            {
                var candidates = new List<(ItemIndex currentIdx, ItemIndex nextIdx, QualityTierCompat targetTier, ItemIndex baseIdx)>();

                foreach (var idx in master.inventory.itemAcquisitionOrder)
                {
                    if (master.inventory.GetItemCountEffective(idx) <= 0) continue;

                    var def = ItemCatalog.GetItemDef(idx);
                    if (def == null || def.hidden || def.tier == ItemTier.NoTier) continue;

                    QualityTierCompat currentTier = GetQualityTier(idx);
                    if ((int)currentTier < (int)QualityTierCompat.Legendary)
                    {
                        QualityTierCompat nextTierCandidate = (QualityTierCompat)((int)currentTier + 1);
                        ItemIndex baseIdx = (currentTier == QualityTierCompat.None) ? idx : GetBaseItemIndex(idx);
                        ItemIndex nextIdx = GetItemIndexOfQuality(baseIdx, nextTierCandidate);

                        if (nextIdx != ItemIndex.None && nextIdx != idx)
                        {
                            candidates.Add((idx, nextIdx, nextTierCandidate, baseIdx));
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    master.inventory.RemoveItemPermanent(chosen.currentIdx, 1);
                    master.inventory.GiveItemPermanent(chosen.nextIdx, 1);

                    var pIdx = PickupCatalog.FindPickupIndex(chosen.nextIdx);
                    if (pIdx != PickupIndex.none)
                    {
                        CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pIdx, isRental = false });
                    }

                    newTier = chosen.targetTier;
                    var itemDef = ItemCatalog.GetItemDef(chosen.nextIdx);
                    string rawName = itemDef != null ? Language.GetString(itemDef.nameToken) : chosen.nextIdx.ToString();
                    upgradedName = System.Text.RegularExpressions.Regex.Replace(rawName, @"<sprite\b[^>]*>", string.Empty).Trim();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception upgrading quality in inventory: {ex}");
            }

            return false;
        }

        public static bool DowngradeItemQualityInInventory(CharacterMaster? master, out string downgradedName, out QualityTierCompat oldTier, out QualityTierCompat newTier)
        {
            downgradedName = "";
            oldTier = QualityTierCompat.None;
            newTier = QualityTierCompat.None;

            if (!IsQualityLoaded || master == null || master.inventory == null)
                return false;

            try
            {
                var candidates = new List<(ItemIndex currentIdx, ItemIndex targetIdx, QualityTierCompat fromTier, QualityTierCompat toTier)>();

                foreach (var idx in master.inventory.itemAcquisitionOrder)
                {
                    if (master.inventory.GetItemCountEffective(idx) <= 0) continue;
                    var def = ItemCatalog.GetItemDef(idx);
                    if (def == null || def.hidden || def.tier == ItemTier.NoTier) continue;

                    QualityTierCompat currentTier = GetQualityTier(idx);
                    if (currentTier > QualityTierCompat.None)
                    {
                        QualityTierCompat lowerTier = (QualityTierCompat)((int)currentTier - 1);
                        ItemIndex baseIdx = GetBaseItemIndex(idx);
                        ItemIndex targetIdx = (lowerTier == QualityTierCompat.None) ? baseIdx : GetItemIndexOfQuality(baseIdx, lowerTier);

                        if (targetIdx != ItemIndex.None && targetIdx != idx)
                        {
                            candidates.Add((idx, targetIdx, currentTier, lowerTier));
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    master.inventory.RemoveItemPermanent(chosen.currentIdx, 1);
                    master.inventory.GiveItemPermanent(chosen.targetIdx, 1);

                    oldTier = chosen.fromTier;
                    newTier = chosen.toTier;
                    var itemDef = ItemCatalog.GetItemDef(chosen.currentIdx);
                    string rawName = itemDef != null ? Language.GetString(itemDef.nameToken) : chosen.currentIdx.ToString();
                    downgradedName = System.Text.RegularExpressions.Regex.Replace(rawName, @"<sprite\b[^>]*>", string.Empty).Trim();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception downgrading quality in inventory: {ex}");
            }

            return false;
        }

        #region Sale Star Helpers

        /// <summary>
        /// Returns all active Sale Stars in the inventory (both base vanilla and Quality variants).
        /// </summary>
        public static List<(ItemIndex itemIndex, int count, QualityTierCompat tier)> GetActiveSaleStarsInInventory(Inventory? inventory)
        {
            var result = new List<(ItemIndex, int, QualityTierCompat)>();
            if (inventory == null) return result;

            ItemIndex baseActive = ItemCatalog.FindItemIndex("LowerPricedChests");
            if (baseActive != ItemIndex.None)
            {
                int baseCount = inventory.GetItemCountEffective(baseActive);
                if (baseCount > 0)
                {
                    result.Add((baseActive, baseCount, QualityTierCompat.None));
                }

                if (IsQualityLoaded)
                {
                    for (QualityTierCompat tier = QualityTierCompat.Uncommon; tier <= QualityTierCompat.Legendary; tier++)
                    {
                        ItemIndex qIdx = GetItemIndexOfQuality(baseActive, tier);
                        if (qIdx != ItemIndex.None && qIdx != baseActive)
                        {
                            int qCount = inventory.GetItemCountEffective(qIdx);
                            if (qCount > 0)
                            {
                                result.Add((qIdx, qCount, tier));
                            }
                        }
                    }
                }
            }

            // Fallback for custom alias "SaleStar" if any mod registers it without LowerPricedChests
            ItemIndex aliasIdx = ItemCatalog.FindItemIndex("SaleStar");
            if (aliasIdx != ItemIndex.None && aliasIdx != baseActive)
            {
                int aliasCount = inventory.GetItemCountEffective(aliasIdx);
                if (aliasCount > 0)
                {
                    result.Add((aliasIdx, aliasCount, QualityTierCompat.None));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the corresponding consumed Sale Star index for an active Sale Star (matching quality tier).
        /// </summary>
        public static ItemIndex GetConsumedSaleStarIndex(ItemIndex activeSaleStarIndex)
        {
            ItemIndex baseConsumed = ItemCatalog.FindItemIndex("LowerPricedChestsConsumed");
            if (baseConsumed == ItemIndex.None) return ItemIndex.None;
            if (!IsQualityLoaded || activeSaleStarIndex == ItemIndex.None) return baseConsumed;

            QualityTierCompat tier = GetQualityTier(activeSaleStarIndex);
            if (tier == QualityTierCompat.None) return baseConsumed;

            ItemIndex qConsumed = GetItemIndexOfQuality(baseConsumed, tier);
            return (qConsumed != ItemIndex.None) ? qConsumed : baseConsumed;
        }

        /// <summary>
        /// Inspects a chest GameObject for Quality Sale Stars consumed on purchase (ItemQualities integration).
        /// </summary>
        public static bool GetChestUsedQualitySaleStars(GameObject? chestObj, out int qualityCount, out QualityTierCompat highestTier)
        {
            qualityCount = 0;
            highestTier = QualityTierCompat.None;
            if (!IsQualityLoaded || chestObj == null || _objectPurchaseContextType == null || _opcResultsProp == null || _usedSaleStarCountsField == null)
                return false;

            try
            {
                var opc = chestObj.GetComponent(_objectPurchaseContextType);
                if (opc != null)
                {
                    var results = _opcResultsProp.GetValue(opc);
                    if (results != null)
                    {
                        var counts = _usedSaleStarCountsField.GetValue(results);
                        if (counts != null && _qualityCountsGetTotalQualityMethod != null)
                        {
                            int totalQ = (int)_qualityCountsGetTotalQualityMethod.Invoke(counts, null);
                            if (totalQ > 0)
                            {
                                qualityCount = totalQ;
                                if (_qualityCountsGetHighestQualityMethod != null)
                                {
                                    object? hq = _qualityCountsGetHighestQualityMethod.Invoke(counts, null);
                                    if (hq != null)
                                    {
                                        highestTier = (QualityTierCompat)(int)hq;
                                    }
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in GetChestUsedQualitySaleStars: {ex}");
            }

            return false;
        }

        #endregion
    }
}
