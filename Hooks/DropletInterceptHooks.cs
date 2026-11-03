using System;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 4: Intercepts world droplets (Void Fields cells, Lunar Pylons, Halcyon Shrines, Void Potential orbs),
    /// debounces multiplayer bursts, and awards roulette spins.
    /// </summary>
    public static class DropletInterceptHooks
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += OnCreatePickupDroplet_CreatePickupInfo;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= OnCreatePickupDroplet_CreatePickupInfo;

            _initialized = false;
        }

        public static OptionDropRewardTier AnalyzeOptionDrop(GenericPickupController.CreatePickupInfo pickupInfo)
        {
            int whiteCount = 0;
            int greenCount = 0;
            int redCount = 0;
            int bossCount = 0;
            int voidCount = 0;
            int lunarCount = 0;
            int totalCount = 0;

            if (pickupInfo.pickerOptions != null && pickupInfo.pickerOptions.Length > 0)
            {
                foreach (var opt in pickupInfo.pickerOptions)
                {
                    var pDef = PickupCatalog.GetPickupDef(opt.pickupIndex);
                    if (pDef != null)
                    {
                        if (pDef.itemIndex != ItemIndex.None)
                        {
                            var itemDef = ItemCatalog.GetItemDef(pDef.itemIndex);
                            if (itemDef != null)
                            {
                                totalCount++;
                                switch (itemDef.tier)
                                {
                                    case ItemTier.Tier1: whiteCount++; break;
                                    case ItemTier.Tier2: greenCount++; break;
                                    case ItemTier.Tier3: redCount++; break;
                                    case ItemTier.Boss: bossCount++; break;
                                    case ItemTier.Lunar: lunarCount++; break;
                                    case ItemTier.VoidTier1:
                                    case ItemTier.VoidTier2:
                                    case ItemTier.VoidTier3:
                                    case ItemTier.VoidBoss:
                                        voidCount++; break;
                                }
                            }
                        }
                        else if (pDef.equipmentIndex != EquipmentIndex.None)
                        {
                            var eqDef = EquipmentCatalog.GetEquipmentDef(pDef.equipmentIndex);
                            if (eqDef != null && eqDef.isLunar) lunarCount++;
                        }
                    }
                }
            }

            // 0. Aurelionite Fragment / Halcyon Shrine Quality Scaling (SotS)
            if (pickupInfo.prefabOverride != null)
            {
                string pName = (pickupInfo.prefabOverride.name ?? "").ToLowerInvariant();
                if (pName.Contains("fragment") || pName.Contains("halcyon") || pName.Contains("aurelion"))
                {
                    int optCount = pickupInfo.pickerOptions != null ? pickupInfo.pickerOptions.Length : 3;
                    if (optCount <= 3) return OptionDropRewardTier.AurelioniteFragmentLow;
                    else if (optCount == 4) return OptionDropRewardTier.AurelioniteFragmentMid;
                    else return OptionDropRewardTier.AurelioniteFragmentMax;
                }
            }

            // 1. Guaranteed Red (Void Fields Cell 9, Legendary Option Chest)
            if (redCount > 0 && whiteCount == 0 && greenCount == 0 && voidCount == 0 && bossCount == 0)
                return OptionDropRewardTier.Legendary;

            // 2. High-Tier Pylon / Elite mix: Red + Void + Boss (Lunar Pylon artifact)
            if ((redCount > 0 || voidCount > 0 || bossCount > 0) && whiteCount == 0 && greenCount == 0)
            {
                if ((redCount > 0 && voidCount > 0) || (redCount > 0 && bossCount > 0) || (voidCount > 0 && bossCount > 0))
                    return OptionDropRewardTier.HighTierPylon;
            }

            // 3. Guaranteed Green (Void Fields Cells 5-8)
            if (greenCount > 0 && whiteCount == 0 && redCount == 0 && voidCount == 0 && bossCount == 0)
                return OptionDropRewardTier.Uncommon;

            // 4. Guaranteed Boss (Yellow)
            if (bossCount > 0 && whiteCount == 0 && greenCount == 0 && redCount == 0 && voidCount == 0)
                return OptionDropRewardTier.Boss;

            // 5. Guaranteed Void
            if (voidCount > 0 && whiteCount == 0 && greenCount == 0 && redCount == 0 && bossCount == 0)
                return OptionDropRewardTier.VoidChoice;

            // 6. Void Potential (Standard: Green + Red/Boss/Void)
            if (greenCount > 0 && (redCount > 0 || bossCount > 0 || voidCount > 0))
                return OptionDropRewardTier.VoidPotential;

            // 7. Guaranteed White (Void Fields Cells 1-4)
            if (whiteCount > 0 && greenCount == 0 && redCount == 0 && bossCount == 0 && voidCount == 0)
                return OptionDropRewardTier.Common;

            // 8. Fallback by prefab override name
            if (pickupInfo.prefabOverride != null)
            {
                string pName = (pickupInfo.prefabOverride.name ?? "").ToLowerInvariant();
                if (pName.Contains("void") || pName.Contains("potential") || pName.Contains("triple"))
                    return OptionDropRewardTier.VoidPotential;
            }

            return OptionDropRewardTier.Common;
        }

        private static void OnCreatePickupDroplet_CreatePickupInfo(
            On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig,
            GenericPickupController.CreatePickupInfo pickupInfo,
            Vector3 position,
            Vector3 velocity)
        {
            if (!UnityEngine.Networking.NetworkServer.active)
            {
                orig(pickupInfo, position, velocity);
                return;
            }

            if (SpawnSuppressionHooks.IsSuppressingShopDroplet)
            {
                // Item was already granted directly to buyer's inventory! Suppress physical droplet.
                return;
            }

            if (InteractableClassifier.IsBazaarScene())
            {
                orig(pickupInfo, position, velocity);
                return;
            }

            if (ModConfig.DisableChestDrops.Value)
            {
                bool isOptionDrop = false;
                if (pickupInfo.prefabOverride != null)
                {
                    string pName = pickupInfo.prefabOverride.name ?? "";
                    if (pName.IndexOf("OptionPickup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("VoidPotential", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("VoidTriple", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("Command", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("Fragment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("Halcyon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pName.IndexOf("Aurelion", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isOptionDrop = true;
                    }
                }
                else if (pickupInfo.pickerOptions != null && pickupInfo.pickerOptions.Length > 0)
                {
                    isOptionDrop = true;
                }

                if (isOptionDrop)
                {
                    // Debounce against duplicate awarding: if droplet was spawned by a chest, opener was already rewarded
                    if (Time.time - DropStateManager.LastVoidRewardTime <= 1.0f)
                    {
                        return;
                    }

                    OptionDropRewardTier rewardTier = AnalyzeOptionDrop(pickupInfo);

                    // Multiplayer burst debounce: event completion spawns N spheres simultaneously.
                    // Award once per burst to the team, ignoring subsequent droplets of the same burst!
                    if (Time.time - DropStateManager.LastWorldBurstTime > 1.2f || DropStateManager.LastWorldBurstTier != rewardTier)
                    {
                        DropStateManager.LastWorldBurstTime = Time.time;
                        DropStateManager.LastWorldBurstTier = rewardTier;

                        switch (rewardTier)
                        {
                            case OptionDropRewardTier.HighTierPylon:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#E056FD>[ItemRoulette:FreeHighTierRoll:All:1] Активирован Лунный Пилон / Элитная сфера! Всем игрокам начислена 1 бесплатная ЭЛИТНАЯ крутка (Красный / Бездна / Босс)!</color>",
                                    "<color=#E056FD>[ItemRoulette:FreeHighTierRoll:All:1] Lunar Pylon / Elite sphere activated! All players awarded 1 free ELITE spin (Red / Void / Boss)!</color>"));
                                break;
                            case OptionDropRewardTier.Legendary:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:1] Завершена ячейка (Гарантированная редкость)! Всем игрокам начислена 1 бесплатная КРАСНАЯ (Легендарная) крутка!</color>",
                                    "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:1] Cell completed (Guaranteed Rarity)! All players awarded 1 free RED (Legendary) spin!</color>"));
                                break;
                            case OptionDropRewardTier.Uncommon:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#55FF55>[ItemRoulette:FreeUncommonRoll:All:1] Завершена ячейка (Гарантированная редкость)! Всем игрокам начислена 1 бесплатная ЗЕЛЕНАЯ (Необычная) крутка!</color>",
                                    "<color=#55FF55>[ItemRoulette:FreeUncommonRoll:All:1] Cell completed (Guaranteed Rarity)! All players awarded 1 free GREEN (Uncommon) spin!</color>"));
                                break;
                            case OptionDropRewardTier.Boss:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#FFD700>[ItemRoulette:FreeBossRoll:All:1] Награда босса получена! Всем игрокам начислена 1 бесплатная ЖЕЛТАЯ (Босс) крутка!</color>",
                                    "<color=#FFD700>[ItemRoulette:FreeBossRoll:All:1] Boss reward claimed! All players awarded 1 free YELLOW (Boss) spin!</color>"));
                                break;
                            case OptionDropRewardTier.VoidChoice:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#D070FF>[ItemRoulette:VoidChoice:All:1] Сфера Бездны поглощена! Всем игрокам предоставлен выбор бесплатной крутки Бездны.</color>",
                                    "<color=#D070FF>[ItemRoulette:VoidChoice:All:1] Void Sphere absorbed! All players granted a free Void spin choice.</color>"));
                                break;
                            case OptionDropRewardTier.VoidPotential:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:All:1] Потенциал Бездны поглощен! Всем игрокам начислена 1 бесплатная крутка (80% зеленый / 15% красный / 5% босс)!</color>",
                                    "<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:All:1] Void Potential absorbed! All players awarded 1 free spin (80% Green / 15% Red / 5% Boss)!</color>"));
                                break;
                            case OptionDropRewardTier.AurelioniteFragmentLow:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#E8C15A>[ItemRoulette:FreeUncommonRoll:All:1] Святилище Алциона завершено (Базовое качество)! Всем игрокам начислена 1 бесплатная ЗЕЛЕНАЯ крутка!</color>",
                                    "<color=#E8C15A>[ItemRoulette:FreeUncommonRoll:All:1] Shrine of the Aurelionite completed (Base tier)! All players awarded 1 free GREEN spin!</color>"));
                                break;
                            case OptionDropRewardTier.AurelioniteFragmentMid:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#E8C15A>[ItemRoulette:FreeVoidPotentialRoll:All:2] Святилище Алциона завершено (Среднее качество)! Всем игрокам начислено по 2 бесплатные крутки Потенциала (80% зеленый / 15% красный / 5% босс)!</color>",
                                    "<color=#E8C15A>[ItemRoulette:FreeVoidPotentialRoll:All:2] Shrine of the Aurelionite completed (Mid tier)! All players awarded 2 free Potential spins (80% Green / 15% Red / 5% Boss)!</color>"));
                                break;
                            case OptionDropRewardTier.AurelioniteFragmentMax:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#FFD700>[ItemRoulette:FreeAurelioniteMax:All:3] Святилище Алциона завершено (МАКСИМАЛЬНОЕ качество)! Всем игрокам начислено по 3 элитные крутки (1 Красная + 2 Потенциала)!</color>",
                                    "<color=#FFD700>[ItemRoulette:FreeAurelioniteMax:All:3] Shrine of the Aurelionite completed (MAX tier)! All players awarded 3 elite spins (1 Red + 2 Potential)!</color>"));
                                break;
                            case OptionDropRewardTier.Common:
                            default:
                                ServerEvents.Broadcast(Localization.Get(
                                    "<color=#FFE853>[ItemRoulette:FreeRollAll:1] Награда получена! Всем игрокам начислена 1 бесплатная крутка рулетки!</color>",
                                    "<color=#FFE853>[ItemRoulette:FreeRollAll:1] Reward claimed! All players awarded 1 free roulette spin!</color>"));
                                break;
                        }
                    }
                    return; // Physical sphere does not spawn in world
                }
            }

            orig(pickupInfo, position, velocity);
        }
    }
}
