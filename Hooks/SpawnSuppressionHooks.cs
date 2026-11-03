using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 1: Stage director card filtering, spawning suppression, and physical drop suppression
    /// for chests, terminals, fabricators, and chance shrines.
    /// </summary>
    public static class SpawnSuppressionHooks
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            On.RoR2.SceneDirector.GenerateInteractableCardSelection += OnGenerateInteractableCardSelection;
            SceneDirector.onGenerateInteractableCardSelection += OnEventGenerateInteractableCardSelection;
            On.RoR2.DirectorCore.TrySpawnObject += OnDirectorCoreTrySpawnObject;
            SceneDirector.onSpawnInteractableServer += OnSpawnInteractableServer;

            On.RoR2.ChestBehavior.ItemDrop += OnChestItemDrop;
            On.RoR2.OptionChestBehavior.ItemDrop += OnOptionChestItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup += OnShopTerminalDropPickup;
            On.RoR2.RouletteChestController.EjectPickupServer += OnRouletteChestEjectPickupServer;
            On.RoR2.RouletteChestController.Awake += OnRouletteChestAwake;
            On.RoR2.RouletteChestController.Start += OnRouletteChestStart;
            On.RoR2.ScavBackpackBehavior.ItemDrop += OnScavBackpackItemDrop;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += OnShrineChanceAddStack;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            On.RoR2.SceneDirector.GenerateInteractableCardSelection -= OnGenerateInteractableCardSelection;
            SceneDirector.onGenerateInteractableCardSelection -= OnEventGenerateInteractableCardSelection;
            On.RoR2.DirectorCore.TrySpawnObject -= OnDirectorCoreTrySpawnObject;
            SceneDirector.onSpawnInteractableServer -= OnSpawnInteractableServer;

            On.RoR2.ChestBehavior.ItemDrop -= OnChestItemDrop;
            On.RoR2.OptionChestBehavior.ItemDrop -= OnOptionChestItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup -= OnShopTerminalDropPickup;
            On.RoR2.RouletteChestController.EjectPickupServer -= OnRouletteChestEjectPickupServer;
            On.RoR2.RouletteChestController.Awake -= OnRouletteChestAwake;
            On.RoR2.RouletteChestController.Start -= OnRouletteChestStart;
            On.RoR2.ScavBackpackBehavior.ItemDrop -= OnScavBackpackItemDrop;
            On.RoR2.ShrineChanceBehavior.AddShrineStack -= OnShrineChanceAddStack;

            _initialized = false;
        }

        private static WeightedSelection<DirectorCard> OnGenerateInteractableCardSelection(
            On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig,
            SceneDirector self)
        {
            var selection = orig(self);
            if (selection != null)
            {
                for (int i = selection.Count - 1; i >= 0; i--)
                {
                    var choice = selection.GetChoice(i);
                    var card = choice.value;
                    if (card == null) continue;
                    string cardName = card.spawnCard != null ? card.spawnCard.name : "";
                    string prefabName = (card.spawnCard != null && card.spawnCard.prefab != null) ? card.spawnCard.prefab.name : "";

                    if (ShouldSuppressInteractable(cardName, prefabName, card.spawnCard != null ? card.spawnCard.prefab : null))
                    {
                        selection.RemoveChoice(i);
                    }
                }
            }
            return selection!;
        }

        private static void OnEventGenerateInteractableCardSelection(
            SceneDirector sceneDirector,
            DirectorCardCategorySelection dccs)
        {
            FilterCategorySelection(dccs);
        }

        private static void FilterCategorySelection(DirectorCardCategorySelection dccs)
        {
            if (dccs == null || dccs.categories == null) return;

            for (int i = 0; i < dccs.categories.Length; i++)
            {
                string catName = dccs.categories[i].name?.ToLowerInvariant() ?? "";

                if (ModConfig.DisablePrinters.Value &&
                    (catName.Contains("duplicator") || catName.Contains("printer")))
                {
                    dccs.categories[i].selectionWeight = 0f;
                }

                if (ModConfig.DisableFabricators.Value &&
                    (catName.Contains("fabricator") || catName.Contains("casino") || catName.Contains("adaptive") || catName.Contains("roulette")))
                {
                    dccs.categories[i].selectionWeight = 0f;
                }

                if (dccs.categories[i].cards != null)
                {
                    var validCards = new List<DirectorCard>();
                    for (int j = 0; j < dccs.categories[i].cards.Length; j++)
                    {
                        var card = dccs.categories[i].cards[j];
                        if (card == null) continue;
                        string cardName = card.spawnCard != null ? card.spawnCard.name : "";
                        string prefabName = (card.spawnCard != null && card.spawnCard.prefab != null) ? card.spawnCard.prefab.name : "";

                        if (!ShouldSuppressInteractable(cardName, prefabName, card.spawnCard != null ? card.spawnCard.prefab : null))
                        {
                            validCards.Add(card);
                        }
                    }
                    dccs.categories[i].cards = validCards.ToArray();
                    if (validCards.Count == 0)
                    {
                        dccs.categories[i].selectionWeight = 0f;
                    }
                }
            }
        }

        private static bool ShouldSuppressInteractable(string cardName, string prefabName, GameObject? prefab)
        {
            // Exemptions
            if (ModConfig.EnableVoidChests.Value && (InteractableClassifier.IsVoidChestName(cardName) || InteractableClassifier.IsVoidChestName(prefabName)))
                return false;

            if (InteractableClassifier.IsLunarInteractable(null, cardName) || InteractableClassifier.IsLunarInteractable(null, prefabName))
            {
                if (InteractableClassifier.IsLunarChestName(cardName) || InteractableClassifier.IsLunarChestName(prefabName))
                    return !ModConfig.EnableLunarChests.Value;
                return false;
            }

            if (InteractableClassifier.IsCauldronInteractable(null, cardName) || InteractableClassifier.IsCauldronInteractable(null, prefabName))
                return false;

            if (InteractableClassifier.IsBazaarScene())
                return false;

            if (QualityCompat.IsQualityLoaded && ModConfig.AllowQualityChestSpawns.Value && (InteractableClassifier.IsQualityChestName(cardName) || InteractableClassifier.IsQualityChestName(prefabName)))
                return false;

            // Equipment multishops
            if (InteractableClassifier.IsEquipmentMultishop(cardName) || InteractableClassifier.IsEquipmentMultishop(prefabName))
                return ModConfig.DisableEquipmentMultishops.Value;

            // Equipment chests / barrels
            if (InteractableClassifier.IsEquipmentChest(cardName) || InteractableClassifier.IsEquipmentChest(prefabName))
                return ModConfig.DisableEquipmentChests.Value;

            // Regular multishops (shops)
            if (InteractableClassifier.IsRegularMultishop(cardName) || InteractableClassifier.IsRegularMultishop(prefabName))
                return ModConfig.DisableMultishops.Value;

            if (IsSuppressedCardOrPrefab(cardName) || IsSuppressedCardOrPrefab(prefabName))
                return true;

            if (prefab != null)
            {
                if (ModConfig.DisableFabricators.Value && prefab.GetComponent<RouletteChestController>() != null)
                    return true;

                if (prefab.GetComponent<ShopTerminalBehavior>() != null)
                {
                    if (InteractableClassifier.IsEquipmentMultishop(cardName) || InteractableClassifier.IsEquipmentMultishop(prefabName) || InteractableClassifier.IsEquipmentMultishop(prefab.name))
                        return ModConfig.DisableEquipmentMultishops.Value;
                    return ModConfig.DisableMultishops.Value;
                }

                if (prefab.GetComponent<ChestBehavior>() != null)
                {
                    if (InteractableClassifier.IsEquipmentChest(cardName) || InteractableClassifier.IsEquipmentChest(prefabName) || InteractableClassifier.IsEquipmentChest(prefab.name))
                        return ModConfig.DisableEquipmentChests.Value;
                    if (ModConfig.DisableChestSpawns.Value)
                        return true;
                }
            }

            return false;
        }

        private static bool IsSuppressedCardOrPrefab(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();

            if (ModConfig.EnableVoidChests.Value && InteractableClassifier.IsVoidChestName(lower)) return false;
            if (InteractableClassifier.IsLunarInteractable(null, lower))
            {
                if (InteractableClassifier.IsLunarChestName(lower))
                    return !ModConfig.EnableLunarChests.Value;
                return false;
            }
            if (InteractableClassifier.IsCauldronInteractable(null, lower)) return false;

            if (InteractableClassifier.IsEquipmentMultishop(lower))
                return ModConfig.DisableEquipmentMultishops.Value;

            if (InteractableClassifier.IsEquipmentChest(lower))
                return ModConfig.DisableEquipmentChests.Value;

            if (InteractableClassifier.IsRegularMultishop(lower))
                return ModConfig.DisableMultishops.Value;

            if (InteractableClassifier.IsQualityChestName(lower))
            {
                if (QualityCompat.IsQualityLoaded && ModConfig.AllowQualityChestSpawns.Value) return false;
                return true;
            }

            if (ModConfig.DisableFabricators.Value)
            {
                if (lower.Contains("fabricator") || lower.Contains("casinochest") || lower.Contains("casino") || lower.Contains("adaptive") || lower.Contains("roulette"))
                    return true;
            }

            if (ModConfig.DisableChestSpawns.Value)
            {
                if (!InteractableClassifier.IsEquipmentChest(lower) && lower.Contains("chest"))
                    return true;
            }

            if (ModConfig.DisableMultishops.Value)
            {
                if (!InteractableClassifier.IsEquipmentMultishop(lower) && (lower.Contains("triple") || lower.Contains("multishop") || lower.Contains("terminal")))
                    return true;
            }

            if (ModConfig.DisablePrinters.Value)
            {
                if (lower.Contains("duplicator") || lower.Contains("printer")) return true;
            }

            if (ModConfig.DisableChanceShrines.Value)
            {
                if (lower.Contains("chance") || lower.Contains("gamble")) return true;
            }

            return false;
        }

        private static GameObject? OnDirectorCoreTrySpawnObject(
            On.RoR2.DirectorCore.orig_TrySpawnObject orig,
            DirectorCore self,
            DirectorSpawnRequest directorSpawnRequest)
        {
            if (directorSpawnRequest?.spawnCard != null)
            {
                string cardName = directorSpawnRequest.spawnCard.name ?? "";
                string prefabName = directorSpawnRequest.spawnCard.prefab != null ? directorSpawnRequest.spawnCard.prefab.name : "";

                if (ShouldSuppressInteractable(cardName, prefabName, directorSpawnRequest.spawnCard.prefab))
                {
                    return null;
                }
            }

            return orig(self, directorSpawnRequest);
        }

        private static void OnSpawnInteractableServer(SceneDirector sceneDirector, GameObject spawnedObject, DirectorCard card)
        {
            if (spawnedObject == null) return;

            string objName = spawnedObject.name ?? "";
            if (InteractableClassifier.IsLunarChestName(objName) && !ModConfig.EnableLunarChests.Value)
            {
                DestroySpawned(spawnedObject);
                return;
            }
            bool isVoid = ModConfig.EnableVoidChests.Value && InteractableClassifier.IsVoidChestName(objName);
            bool isLunar = InteractableClassifier.IsLunarInteractable(spawnedObject, objName);
            bool isCauldron = InteractableClassifier.IsCauldronInteractable(spawnedObject, objName);
            bool isBazaar = InteractableClassifier.IsBazaarScene();
            bool isQuality = QualityCompat.IsQualityLoaded && ModConfig.AllowQualityChestSpawns.Value && InteractableClassifier.IsQualityChestName(objName);
            if (isVoid || isLunar || isCauldron || isBazaar || isQuality) return;

            if (ModConfig.DisableFabricators.Value &&
                (spawnedObject.GetComponent<RouletteChestController>() != null ||
                 objName.IndexOf("fabricator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 objName.IndexOf("casinochest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 objName.IndexOf("casino", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                DestroySpawned(spawnedObject);
                return;
            }

            if (InteractableClassifier.IsEquipmentMultishop(objName))
            {
                if (ModConfig.DisableEquipmentMultishops.Value) DestroySpawned(spawnedObject);
                return;
            }

            if (InteractableClassifier.IsQualityChestName(objName))
            {
                if (!QualityCompat.IsQualityLoaded || !ModConfig.AllowQualityChestSpawns.Value)
                {
                    DestroySpawned(spawnedObject);
                }
                return;
            }

            if (InteractableClassifier.IsEquipmentChest(objName))
            {
                if (ModConfig.DisableEquipmentChests.Value) DestroySpawned(spawnedObject);
                return;
            }

            if (InteractableClassifier.IsRegularMultishop(objName))
            {
                if (ModConfig.DisableMultishops.Value) DestroySpawned(spawnedObject);
                return;
            }

            if (spawnedObject.GetComponent<ChestBehavior>() != null)
            {
                if (InteractableClassifier.IsEquipmentChest(objName))
                {
                    if (ModConfig.DisableEquipmentChests.Value) DestroySpawned(spawnedObject);
                }
                else if (ModConfig.DisableChestSpawns.Value)
                {
                    DestroySpawned(spawnedObject);
                }
                return;
            }

            if (spawnedObject.GetComponent<ShopTerminalBehavior>() != null)
            {
                if (InteractableClassifier.IsEquipmentMultishop(objName))
                {
                    if (ModConfig.DisableEquipmentMultishops.Value) DestroySpawned(spawnedObject);
                }
                else if (ModConfig.DisableMultishops.Value)
                {
                    DestroySpawned(spawnedObject);
                }
                return;
            }
        }

        private static void DestroySpawned(GameObject obj)
        {
            obj.SetActive(false);
            if (NetworkServer.active) NetworkServer.Destroy(obj);
            else UnityEngine.Object.Destroy(obj);
        }

        private static void OnChestItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
        {
            if (!NetworkServer.active)
            {
                orig(self);
                return;
            }

            if (self != null && self.gameObject != null)
            {
                if (InteractableClassifier.IsLunarInteractable(self.gameObject, self.gameObject.name) && !InteractableClassifier.IsLunarChestName(self.gameObject.name)) { orig(self); return; }
                if (InteractableClassifier.IsBazaarScene()) { orig(self); return; }
            }

            try
            {
                if (self != null) ChestRewardHooks.HandleChestOpened(self);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in HandleChestOpened: {ex}");
            }

            if (ModConfig.DisableChestDrops.Value || (self != null && self.gameObject != null && InteractableClassifier.IsLunarChestName(self.gameObject.name))) return; // Suppress physical drop
            orig(self);
        }

        private static void OnOptionChestItemDrop(On.RoR2.OptionChestBehavior.orig_ItemDrop orig, OptionChestBehavior self)
        {
            if (!NetworkServer.active)
            {
                orig(self);
                return;
            }

            if (self != null && self.gameObject != null)
            {
                if (InteractableClassifier.IsLunarInteractable(self.gameObject, self.gameObject.name) && !InteractableClassifier.IsLunarChestName(self.gameObject.name)) { orig(self); return; }
                if (InteractableClassifier.IsBazaarScene()) { orig(self); return; }
            }

            try
            {
                if (self != null) ChestRewardHooks.HandleOptionChestOpened(self);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in HandleOptionChestOpened: {ex}");
            }

            if (ModConfig.DisableChestDrops.Value || (self != null && self.gameObject != null && InteractableClassifier.IsLunarChestName(self.gameObject.name)))
            {
                if (self != null) ChestRewardHooks.ClearOptionChestGeneratedPickups(self);
                return;
            }
            orig(self);
        }

        public static bool IsSuppressingShopDroplet = false;

        private static void OnShopTerminalDropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
        {
            if (!NetworkServer.active)
            {
                orig(self);
                return;
            }

            if (self != null && self.gameObject != null)
            {
                // 1. In Bazaar Between Time, NEVER suppress anything (shop terminals, cauldrons, equipment)!
                if (InteractableClassifier.IsBazaarScene())
                {
                    orig(self);
                    return;
                }

                // 2. Lunar purchases (costs Lunar Coins, Lunar Shop, Lunar Bud) MUST drop their item!
                if (InteractableClassifier.IsLunarInteractable(self.gameObject, self.gameObject.name))
                {
                    orig(self);
                    return;
                }

                // 3. Cauldrons / Item Exchanges / 3D Printers / Scrappers MUST drop their item!
                if (InteractableClassifier.IsCauldronInteractable(self.gameObject, self.gameObject.name))
                {
                    orig(self);
                    return;
                }
            }

            // Multishop purchase: Immediately grant item to inventory without free roulette roll!
            CharacterMaster? buyer = (self != null && self.gameObject != null) ? ChestRewardHooks.FindOpenerMaster(self.gameObject) : null;
            PickupIndex pIndex = (self != null) ? self.CurrentPickupIndex() : PickupIndex.none;
            PickupDef? pickupDef = (pIndex != PickupIndex.none) ? PickupCatalog.GetPickupDef(pIndex) : null;

            bool directGranted = false;
            if (buyer != null && buyer.inventory != null && pickupDef != null)
            {
                if (pickupDef.itemIndex != ItemIndex.None)
                {
                    buyer.inventory.GiveItemPermanent(pickupDef.itemIndex, 1);
                    if (NetworkHelper.IsLocalMaster(buyer))
                    {
                        try
                        {
                            CharacterMasterNotificationQueue.PushPickupNotification(buyer, new UniquePickup { pickupIndex = pIndex });
                        }
                        catch { }
                    }
                    directGranted = true;
                }
                else if (pickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    RouletteManager.ExecuteGrantEquipmentServer(buyer, pickupDef.equipmentIndex);
                    directGranted = true;
                }

                if (directGranted)
                {
                    string buyerName = PlayerUtil.GetMasterDisplayName(buyer);
                    string itemName = pickupDef.nameToken != null ? Language.GetString(pickupDef.nameToken) : (pickupDef.internalName ?? "Item");
                    ServerEvents.Broadcast(Localization.Format(
                        "<color=#4ADE80>[ItemRoulette] {0} приобрёл в магазине: {1}!</color>",
                        "<color=#4ADE80>[ItemRoulette] {0} purchased from shop: {1}!</color>",
                        buyerName, itemName));

                    RouletteManager.GetServerState(buyer).UpdateStarsIfPending(buyer);
                }
            }

            if (directGranted)
            {
                IsSuppressingShopDroplet = true;
                try
                {
                    orig(self);
                }
                finally
                {
                    IsSuppressingShopDroplet = false;
                }
                return;
            }

            // Fallback: if buyer or item couldn't be resolved, let vanilla drop the item
            orig(self);
        }

        private static void OnRouletteChestEjectPickupServer(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController self, UniquePickup pickup)
        {
            if (ModConfig.DisableChestDrops.Value || ModConfig.DisableFabricators.Value)
            {
                // Suppress Fabricator / Adaptive chest drop
                return;
            }
            orig(self, pickup);
        }

        private static void OnRouletteChestAwake(On.RoR2.RouletteChestController.orig_Awake orig, RouletteChestController self)
        {
            orig(self);
            if (ModConfig.DisableFabricators.Value && self != null && self.gameObject != null)
            {
                self.gameObject.SetActive(false);
                if (NetworkServer.active)
                {
                    NetworkServer.Destroy(self.gameObject);
                }
            }
        }

        private static void OnRouletteChestStart(On.RoR2.RouletteChestController.orig_Start orig, RouletteChestController self)
        {
            orig(self);
            if (ModConfig.DisableFabricators.Value && self != null && self.gameObject != null)
            {
                self.gameObject.SetActive(false);
                if (NetworkServer.active)
                {
                    NetworkServer.Destroy(self.gameObject);
                }
            }
        }

        private static void OnScavBackpackItemDrop(On.RoR2.ScavBackpackBehavior.orig_ItemDrop orig, ScavBackpackBehavior self)
        {
            if (ModConfig.DisableChestDrops.Value)
            {
                // Suppress physical droplets from Scavenger backpack
                return;
            }
            orig(self);
        }

        private static void OnShrineChanceAddStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            if (ModConfig.DisableChanceShrines.Value)
            {
                // Still allow interaction audio/effect if spawned, but no drop
                return;
            }
            orig(self, activator);
        }
    }
}
