using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Compat
{
    public static class ZetAspectsCompat
    {
        public const string ModGuid = "com.TPDespair.ZetAspects";

        private static bool _initialized = false;
        private static bool _isLoaded = false;
        private static Type? _catalogType;
        private static MethodInfo? _itemizeMethod;
        private static FieldInfo? _aspectItemIndexesField;
        private static FieldInfo? _equipToItemField;

        public static bool IsZetAspectsLoaded
        {
            get
            {
                if (!_initialized)
                {
                    Init();
                }
                return _isLoaded;
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
                    // Search in loaded assemblies for TPDespair.ZetAspects.Catalog
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name == "ZetAspects")
                        {
                            _catalogType = asm.GetType("TPDespair.ZetAspects.Catalog");
                            break;
                        }
                    }

                    if (_catalogType == null)
                    {
                        _catalogType = Type.GetType("TPDespair.ZetAspects.Catalog, ZetAspects");
                    }

                    if (_catalogType != null)
                    {
                        _itemizeMethod = _catalogType.GetMethod("ItemizeEliteEquipment", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        _aspectItemIndexesField = _catalogType.GetField("aspectItemIndexes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        _equipToItemField = _catalogType.GetField("equipToItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        Debug.Log("[ItemRoulette] ZetAspects integration successfully initialized.");
                    }
                    else
                    {
                        Debug.LogWarning("[ItemRoulette] ZetAspects plugin was detected, but Catalog type could not be loaded.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception initializing ZetAspects compat: {ex}");
                _isLoaded = false;
            }
        }

        public static ItemIndex GetAspectItemIndex(EquipmentIndex equipIndex)
        {
            if (!IsZetAspectsLoaded || equipIndex == EquipmentIndex.None) return ItemIndex.None;

            try
            {
                if (_itemizeMethod != null)
                {
                    var result = _itemizeMethod.Invoke(null, new object[] { equipIndex });
                    if (result is ItemIndex itemIdx && itemIdx != ItemIndex.None)
                    {
                        return itemIdx;
                    }
                }
                else if (_equipToItemField != null)
                {
                    var dict = _equipToItemField.GetValue(null) as Dictionary<EquipmentIndex, ItemIndex>;
                    if (dict != null && dict.TryGetValue(equipIndex, out ItemIndex itemIdx))
                    {
                        return itemIdx;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception calling ZetAspects.ItemizeEliteEquipment: {ex}");
            }

            return ItemIndex.None;
        }

        public static List<ItemIndex>? GetAspectItemIndexes()
        {
            if (!IsZetAspectsLoaded) return null;

            try
            {
                if (_aspectItemIndexesField != null)
                {
                    var list = _aspectItemIndexesField.GetValue(null) as List<ItemIndex>;
                    if (list != null && list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception getting ZetAspects aspectItemIndexes: {ex}");
            }

            return null;
        }

        public static List<PickupIndex> GetAspectPickupIndexes()
        {
            var result = new List<PickupIndex>();
            var items = GetAspectItemIndexes();
            if (items != null)
            {
                foreach (var itemIdx in items)
                {
                    if (itemIdx != ItemIndex.None)
                    {
                        var pIdx = PickupCatalog.FindPickupIndex(itemIdx);
                        if (pIdx != PickupIndex.none && !result.Contains(pIdx))
                        {
                            result.Add(pIdx);
                        }
                    }
                }
            }
            return result;
        }

        public static ItemTier GetAspectItemTier()
        {
            var items = GetAspectItemIndexes();
            if (items != null && items.Count > 0)
            {
                var def = ItemCatalog.GetItemDef(items[0]);
                if (def != null) return def.tier;
            }
            return ItemTier.Boss;
        }
    }
}
