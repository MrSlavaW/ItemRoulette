using System;
using System.Collections.Generic;
using System.Reflection;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using ItemRoulette.UI;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 2: Interaction detection, chest open identification, and awarding free rolls.
    /// </summary>
    public static class ChestRewardHooks
    {
        private static readonly FieldInfo? _displayTierField =
            typeof(OptionChestBehavior).GetField("displayTier", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo? _pickupPrefabField =
            typeof(OptionChestBehavior).GetField("pickupPrefab", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo? _generatedPickupsField =
            typeof(OptionChestBehavior).GetField("generatedPickups", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            On.RoR2.Interactor.PerformInteraction += OnInteractorPerformInteraction;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += OnPurchaseInteractionBegin;
            On.RoR2.ScavBackpackBehavior.Open += OnScavBackpackOpen;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            On.RoR2.Interactor.PerformInteraction -= OnInteractorPerformInteraction;
            On.RoR2.PurchaseInteraction.OnInteractionBegin -= OnPurchaseInteractionBegin;
            On.RoR2.ScavBackpackBehavior.Open -= OnScavBackpackOpen;

            _initialized = false;
        }

        private static void OnInteractorPerformInteraction(On.RoR2.Interactor.orig_PerformInteraction orig, Interactor self, GameObject interactableObject)
        {
            if (interactableObject != null && self != null)
            {
                DropStateManager.LastInteractors[interactableObject] = self;
                if (UnityEngine.Networking.NetworkServer.active && ModConfig.EnableSaleStarRework.Value && ModConfig.SaleStarDuplicateChestSpins.Value)
                {
                    CaptureSaleStarSnapshot(interactableObject, self);
                }
            }
            orig(self, interactableObject);
        }

        private static void OnPurchaseInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            if (UnityEngine.Networking.NetworkServer.active && self != null && activator != null && ModConfig.EnableSaleStarRework.Value && ModConfig.SaleStarDuplicateChestSpins.Value)
            {
                CaptureSaleStarSnapshot(self.gameObject, activator);
            }
            orig(self, activator);
        }

        private static void CaptureSaleStarSnapshot(GameObject? obj, Interactor? activator)
        {
            if (obj == null || activator == null) return;
            if (!UnityEngine.Networking.NetworkServer.active) return;
            if (!ModConfig.EnableSaleStarRework.Value || !ModConfig.SaleStarDuplicateChestSpins.Value) return;

            var pi = obj.GetComponent<PurchaseInteraction>() ?? obj.GetComponentInParent<PurchaseInteraction>();
            if (pi == null || !pi.saleStarCompatible) return;
            if (!pi.CanBeAffordedByInteractor(activator)) return;

            var body = activator.GetComponent<CharacterBody>();
            if (body == null || body.master == null || body.master.inventory == null) return;

            var activeEntries = QualityCompat.GetActiveSaleStarsInInventory(body.master.inventory);
            if (activeEntries == null || activeEntries.Count == 0) return;

            int totalStars = 0;
            var starEntries = new List<(QualityTierCompat tier, int count)>();
            foreach (var entry in activeEntries)
            {
                if (entry.count <= 0) continue;
                totalStars += entry.count;
                starEntries.Add((entry.tier, entry.count));
            }

            if (totalStars <= 0) return;

            var snapshot = new SaleStarSnapshot
            {
                Master = body.master,
                TotalStars = totalStars,
                StarEntries = starEntries,
                Timestamp = Time.time
            };

            DropStateManager.ChestSaleStarSnapshots[obj] = snapshot;
            if (obj.transform.root != null && obj.transform.root.gameObject != obj)
            {
                DropStateManager.ChestSaleStarSnapshots[obj.transform.root.gameObject] = snapshot;
            }
        }

        private static bool ConsumeOpenerSaleStarsServer(
            GameObject? chestObj,
            ref CharacterMaster? master,
            out int totalStars,
            out List<(QualityTierCompat tier, int count)> starEntries)
        {
            totalStars = 0;
            starEntries = new List<(QualityTierCompat, int)>();

            if (!UnityEngine.Networking.NetworkServer.active) return false;
            if (!ModConfig.EnableSaleStarRework.Value || !ModConfig.SaleStarDuplicateChestSpins.Value) return false;

            // 1. Check if we captured an interaction snapshot before game/mods consumed stars on [E]
            SaleStarSnapshot? snapshot = null;
            if (chestObj != null)
            {
                DropStateManager.TryGetSaleStarSnapshot(chestObj, out snapshot);
            }

            if (snapshot != null)
            {
                if (master == null) master = snapshot.Master;
                totalStars = snapshot.TotalStars;
                starEntries = snapshot.StarEntries;

                // Ensure any untransformed remaining active stars in inventory are consumed (e.g. vanilla transforms only 1)
                if (master != null && master.inventory != null)
                {
                    ConsumeRemainingActiveStarsServer(master.inventory);
                    var bodyObj = master.GetBodyObject();
                    if (bodyObj != null)
                    {
                        Util.PlaySound("Play_item_proc_lowerPricedChest", bodyObj);
                    }
                }

                return totalStars > 0;
            }

            // 2. Direct inventory inspection fallback (if no snapshot was taken or chest opened without interaction)
            if (master != null && master.inventory != null)
            {
                var activeEntries = QualityCompat.GetActiveSaleStarsInInventory(master.inventory);
                if (activeEntries != null && activeEntries.Count > 0)
                {
                    ItemIndex defaultConsumedIdx = ItemCatalog.FindItemIndex("LowerPricedChestsConsumed");

                    foreach (var entry in activeEntries)
                    {
                        if (entry.count <= 0) continue;
                        totalStars += entry.count;
                        starEntries.Add((entry.tier, entry.count));

                        ItemIndex consumedIdx = QualityCompat.GetConsumedSaleStarIndex(entry.itemIndex);
                        if (consumedIdx == ItemIndex.None) consumedIdx = defaultConsumedIdx;

                        master.inventory.RemoveItemPermanent(entry.itemIndex, entry.count);
                        if (consumedIdx != ItemIndex.None)
                        {
                            master.inventory.GiveItemPermanent(consumedIdx, entry.count);
                        }
                    }

                    if (totalStars > 0)
                    {
                        var bodyObj = master.GetBodyObject();
                        if (bodyObj != null)
                        {
                            Util.PlaySound("Play_item_proc_lowerPricedChest", bodyObj);
                        }
                        return true;
                    }
                }
            }

            // 3. Fallback for Quality mod if QualityCompat registered consumed stars on the chest
            if (chestObj != null && QualityCompat.GetChestUsedQualitySaleStars(chestObj, out int qCount, out var qHighestTier))
            {
                totalStars = qCount;
                starEntries.Add((qHighestTier, qCount));
                return true;
            }

            return false;
        }

        private static void ConsumeRemainingActiveStarsServer(Inventory inventory)
        {
            var activeEntries = QualityCompat.GetActiveSaleStarsInInventory(inventory);
            if (activeEntries == null || activeEntries.Count == 0) return;

            ItemIndex defaultConsumedIdx = ItemCatalog.FindItemIndex("LowerPricedChestsConsumed");
            foreach (var entry in activeEntries)
            {
                if (entry.count <= 0) continue;
                ItemIndex consumedIdx = QualityCompat.GetConsumedSaleStarIndex(entry.itemIndex);
                if (consumedIdx == ItemIndex.None) consumedIdx = defaultConsumedIdx;

                inventory.RemoveItemPermanent(entry.itemIndex, entry.count);
                if (consumedIdx != ItemIndex.None)
                {
                    inventory.GiveItemPermanent(consumedIdx, entry.count);
                }
            }
        }

        public static void HandleChestOpened(ChestBehavior self)
        {
            if (!UnityEngine.Networking.NetworkServer.active) return;
            if (self == null || self.gameObject == null) return;

            int instanceId = self.gameObject.GetInstanceID();
            if (DropStateManager.ProcessedChestInstanceIds.Contains(instanceId)) return;
            DropStateManager.ProcessedChestInstanceIds.Add(instanceId);

            string objName = self.gameObject.name ?? "";
            string lower = objName.ToLowerInvariant();

            CharacterMaster? openerMaster = FindOpenerMaster(self.gameObject);
            DropStateManager.LastInteractors.Remove(self.gameObject);

            // Intercept Lunar Pod / Lunar Chest BEFORE generic IsLunarInteractable check!
            if (InteractableClassifier.IsLunarChestName(lower))
            {
                string openerNameLunar = PlayerUtil.GetMasterDisplayName(openerMaster);
                string openerIdLunar = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(openerMaster));
                bool rewardAll = ModConfig.LunarChestRewardAllPlayers.Value;
                if (rewardAll)
                    ServerEvents.Broadcast(Localization.Get(
                        $"<color=#38BDF8>[ItemRoulette:FreeLunarRoll:All:1] {openerNameLunar} открыл Лунную Капсулу! Всем игрокам начислена 1 бесплатная крутка Лунных предметов!</color>",
                        $"<color=#38BDF8>[ItemRoulette:FreeLunarRoll:All:1] {openerNameLunar} opened a Lunar Pod! Granted 1 free Lunar spin to all players!</color>"), openerNameLunar);
                else
                    ServerEvents.Broadcast(Localization.Get(
                        $"<color=#38BDF8>[ItemRoulette:FreeLunarRoll:{openerIdLunar}:1] {openerNameLunar} открыл Лунную Капсулу! Начислена 1 бесплатная крутка Лунных предметов.</color>",
                        $"<color=#38BDF8>[ItemRoulette:FreeLunarRoll:{openerIdLunar}:1] {openerNameLunar} opened a Lunar Pod! Granted 1 free Lunar spin.</color>"), openerNameLunar);
                return;
            }

            if (InteractableClassifier.IsLunarInteractable(self.gameObject, lower) || InteractableClassifier.IsBazaarScene()) return;

            ConsumeOpenerSaleStarsServer(self.gameObject, ref openerMaster, out int totalStars, out var starEntries);

            string openerName = PlayerUtil.GetMasterDisplayName(openerMaster);
            string openerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(openerMaster));

            int starMultiplier = 1 + totalStars;
            bool starProc = totalStars > 0;
            string starNote = starProc ? $" (Выгодная звезда: x{starMultiplier})!" : "!";
            string starNoteEn = starProc ? $" (Sale Star: x{starMultiplier})!" : "!";

            QualityTierCompat highestSpentStarTier = QualityTierCompat.None;
            int qualityStarsSpent = 0;
            int normalStarsSpent = 0;

            foreach (var (sTier, sCount) in starEntries)
            {
                if (sTier > QualityTierCompat.None)
                {
                    qualityStarsSpent += sCount;
                    if (sTier > highestSpentStarTier) highestSpentStarTier = sTier;
                }
                else
                {
                    normalStarsSpent += sCount;
                }
            }

            if (highestSpentStarTier > QualityTierCompat.None)
            {
                string qNameRu = QualityCompat.GetQualityName(highestSpentStarTier);
                string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(highestSpentStarTier));
                starNote = $" (<color=#{qColorHex}>{qNameRu} Выгодная звезда: x{starMultiplier}</color>)!";
                starNoteEn = $" (<color=#{qColorHex}>{highestSpentStarTier} Sale Star: x{starMultiplier}</color>)!";
            }

            // 0a. Quality Equipment Barrel / Chest
            if (QualityCompat.IsQualityLoaded && InteractableClassifier.IsQualityEquipmentChest(lower))
            {
                int count = 1 * starMultiplier;
                bool rewardAll = ModConfig.EquipmentChestRewardAllPlayers.Value || ModConfig.QualityChestRewardAllPlayers.Value;
                if (rewardAll)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityEquipmentRoll:All:{count}] {{0}} открыл Бочку снаряжения качества!{starNote} Всем игрокам начислено {count} бесплатных круток снаряжения с гарантированным качеством!</color>",
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityEquipmentRoll:All:{count}] {{0}} opened Quality Equipment Barrel!{starNoteEn} All players awarded {count} guaranteed Quality equipment spins!</color>",
                        openerName));
                }
                else
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityEquipmentRoll:{openerId}:{count}] {{0}} открыл Бочку снаряжения качества!{starNote} Начислено {count} бесплатных круток снаряжения с гарантированным качеством.</color>",
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityEquipmentRoll:{openerId}:{count}] {{0}} opened Quality Equipment Barrel!{starNoteEn} Awarded {count} guaranteed Quality equipment spins.</color>",
                        openerName));
                }
                return;
            }

            // 0b. Equipment Chest / Barrel
            if (InteractableClassifier.IsEquipmentChest(lower))
            {
                int count = 1 + normalStarsSpent;
                if (ModConfig.EquipmentChestRewardAllPlayers.Value)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF9520>[ItemRoulette:FreeEquipmentRoll:All:{count}] {{0}} открыл Сундук снаряжения!{starNote} Всем игрокам начислено {count} бесплатных круток снаряжения!</color>",
                        $"<color=#FF9520>[ItemRoulette:FreeEquipmentRoll:All:{count}] {{0}} opened Equipment Chest!{starNoteEn} All players awarded {count} free equipment spins!</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityEquipmentRoll:All:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount} круток снаряжения с качеством {qName}!</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityEquipmentRoll:All:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount} equipment spin(s) with quality {qTier}!</color>",
                            openerName));
                    }
                }
                else
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF9520>[ItemRoulette:FreeEquipmentRoll:{openerId}:{count}] {{0}} открыл Сундук снаряжения!{starNote} Начислено {count} бесплатных круток снаряжения.</color>",
                        $"<color=#FF9520>[ItemRoulette:FreeEquipmentRoll:{openerId}:{count}] {{0}} opened Equipment Chest!{starNoteEn} Awarded {count} free equipment spins.</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityEquipmentRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount} круток снаряжения с качеством {qName}.</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityEquipmentRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount} equipment spin(s) with quality {qTier}.</color>",
                            openerName));
                    }
                }
                return;
            }

            // 0. Scavenger Backpack
            bool isScavBackpack = lower.Contains("scavbackpack") ||
                                  lower.Contains("scavsack") ||
                                  lower.Contains("scavenger") ||
                                  self.GetComponent<ScavBackpackBehavior>() != null;
            if (isScavBackpack)
            {
                bool isLunar = lower.Contains("lunar") || self.lunarChance >= 1.0f;
                int playerCount = (Run.instance != null && Run.instance.participatingPlayerCount > 0) ? Run.instance.participatingPlayerCount : (PlayerCharacterMasterController.instances?.Count ?? 1);

                if (isLunar)
                {
                    ServerEvents.Broadcast(Localization.Get(
                        $"<color=#4BE0FF>[ItemRoulette:LunarCoinsAll:10] {openerName} открыл Лунный Мешок Падальщика! Всем игрокам начислено по 10 Лунных монет!</color>",
                        $"<color=#4BE0FF>[ItemRoulette:LunarCoinsAll:10] {openerName} opened a Lunar Scavenger's Pack! All players awarded 10 Lunar Coins!</color>"), openerName);
                }
                else
                {
                    int totalItems = 10;
                    var scavBehavior = self.GetComponent<ScavBackpackBehavior>();
                    if (scavBehavior != null && scavBehavior.totalItems > 0) totalItems = scavBehavior.totalItems;
                    int countPerPlayer = Mathf.CeilToInt((float)totalItems / Mathf.Max(1, playerCount));

                    ServerEvents.Broadcast(Localization.Get(
                        $"<color=#FFE853>[ItemRoulette:FreeRollAll:{countPerPlayer}] {openerName} открыл Мешок Мусорщика! Всем игрокам начислено по {countPerPlayer} бесплатных круток ({totalItems} на команду с округлением)!</color>",
                        $"<color=#FFE853>[ItemRoulette:FreeRollAll:{countPerPlayer}] {openerName} opened a Scavenger's Pack! All players awarded {countPerPlayer} free spins ({totalItems} for team rounded up)!</color>"), openerName);
                }
                return;
            }

            // 1. Void Cradle -> Void Choice
            if (InteractableClassifier.IsVoidCradle(self.gameObject, lower))
            {
                DropStateManager.LastVoidRewardTime = Time.time;
                ServerEvents.Broadcast(Localization.Get(
                    $"<color=#D070FF>[ItemRoulette:VoidChoice:{openerId}:1] {openerName} открыл Колыбель Бездны! Предоставлен выбор бесплатной крутки Бездны.</color>",
                    $"<color=#D070FF>[ItemRoulette:VoidChoice:{openerId}:1] {openerName} opened a Void Cradle! Void choice granted.</color>"), openerName);
                return;
            }

            // 2. Void Potential
            if (InteractableClassifier.IsVoidPotential(self.gameObject, lower) || InteractableClassifier.IsVoidChestName(lower))
            {
                DropStateManager.LastVoidRewardTime = Time.time;
                ServerEvents.Broadcast(Localization.Get(
                    $"<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:{openerId}:1] {openerName} открыл Потенциал Бездны! Начислена 1 бесплатная крутка (80% зеленый / 15% красный / 5% босс)!</color>",
                    $"<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:{openerId}:1] {openerName} opened Void Potential! Awarded 1 free spin (80% green / 15% red / 5% boss)!</color>"), openerName);
                return;
            }

            // 2b. Quality Chest
            if (QualityCompat.IsQualityLoaded && InteractableClassifier.IsQualityChestName(lower))
            {
                bool isLarge = lower.Contains("qualitychest2") || self.tier2Chance >= 0.8f;
                int baseCount = isLarge ? 2 : 1;
                int rollsCount = (1 + normalStarsSpent) * baseCount;
                string chestTitle = isLarge ? Localization.Get("Большой Сундук Качества", "Large Quality Chest") : Localization.Get("Сундук Качества", "Quality Chest");

                if (ModConfig.QualityChestRewardAllPlayers.Value)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityRoll:All:{rollsCount}] {{0}} открыл {chestTitle}!{starNote} Всем игрокам начислено {rollsCount} бесплатных круток с гарантированным качеством!</color>",
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityRoll:All:{rollsCount}] {{0}} opened {chestTitle}!{starNoteEn} All players awarded {rollsCount} guaranteed Quality spins!</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount * baseCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount * baseCount} круток с качеством {qName}!</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount * baseCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount * baseCount} spin(s) with quality {qTier}!</color>",
                            openerName));
                    }
                }
                else
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityRoll:{openerId}:{rollsCount}] {{0}} открыл {chestTitle}!{starNote} Начислено {rollsCount} бесплатных круток с гарантированным качеством.</color>",
                        $"<color=#38EF7D>[ItemRoulette:FreeQualityRoll:{openerId}:{rollsCount}] {{0}} opened {chestTitle}!{starNoteEn} Awarded {rollsCount} guaranteed Quality spins.</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount * baseCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount * baseCount} круток с качеством {qName}.</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount * baseCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount * baseCount} spin(s) with quality {qTier}.</color>",
                            openerName));
                    }
                }
                return;
            }

            // 2c. Gold / Legendary Chest
            bool isGold = lower.Contains("goldchest") || lower.Contains("legendary") || self.tier3Chance >= 1.0f;
            if (isGold)
            {
                int count = 1 + normalStarsSpent;
                if (ModConfig.GoldChestRewardAllPlayers.Value)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:{count}] {{0}} открыл Золотой Сундук!{starNote} Всем игрокам начислено {count} бесплатных круток ЛЕГЕНДАРНЫХ (красных) предметов!</color>",
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:{count}] {{0}} opened Legendary Chest!{starNoteEn} All players awarded {count} free LEGENDARY (Red) spins!</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:All:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount} легендарных круток с качеством {qName}!</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:All:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount} Legendary spin(s) with quality {qTier}!</color>",
                            openerName));
                    }
                }
                else
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{openerId}:{count}] {{0}} открыл Золотой Сундук!{starNote} Начислено {count} бесплатных круток ЛЕГЕНДАРНЫХ предметов.</color>",
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{openerId}:{count}] {{0}} opened Legendary Chest!{starNoteEn} Awarded {count} free LEGENDARY spins.</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount} легендарных круток с качеством {qName}.</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount} Legendary spin(s) with quality {qTier}.</color>",
                            openerName));
                    }
                }
                return;
            }

            // 3. Pre-placed / Standard Chest
            int standardCount = 1 + normalStarsSpent;
            if (ModConfig.PreplacedChestRewardAllPlayers.Value)
            {
                ServerEvents.Broadcast(Localization.Format(
                    $"<color=#FFE853>[ItemRoulette:FreeRollAll:{standardCount}] {{0}} открыл сундук!{starNote} Всем игрокам начислено {standardCount} бесплатных круток рулетки!</color>",
                    $"<color=#FFE853>[ItemRoulette:FreeRollAll:{standardCount}] {{0}} opened chest!{starNoteEn} All players awarded {standardCount} free roulette spins!</color>",
                    openerName));

                foreach (var (qTier, qCount) in starEntries)
                {
                    if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                    string qName = QualityCompat.GetQualityName(qTier);
                    string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount} круток с качеством {qName}!</color>",
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount} spin(s) with quality {qTier}!</color>",
                        openerName));
                }
            }
            else
            {
                ServerEvents.Broadcast(Localization.Format(
                    $"<color=#FFE853>[ItemRoulette:FreeRoll:{openerId}:{standardCount}] {{0}} открыл сундук!{starNote} Начислено {standardCount} бесплатных круток рулетки.</color>",
                    $"<color=#FFE853>[ItemRoulette:FreeRoll:{openerId}:{standardCount}] {{0}} opened chest!{starNoteEn} Awarded {standardCount} free roulette spins.</color>",
                    openerName));

                foreach (var (qTier, qCount) in starEntries)
                {
                    if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                    string qName = QualityCompat.GetQualityName(qTier);
                    string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount} круток с качеством {qName}.</color>",
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount} spin(s) with quality {qTier}.</color>",
                        openerName));
                }
            }
        }

        public static ItemTier GetOptionChestDisplayTier(OptionChestBehavior chest)
        {
            if (chest == null || _displayTierField == null) return ItemTier.AssignedAtRuntime;
            try { return (ItemTier)(_displayTierField.GetValue(chest) ?? ItemTier.AssignedAtRuntime); }
            catch { return ItemTier.AssignedAtRuntime; }
        }

        public static GameObject? GetOptionChestPickupPrefab(OptionChestBehavior chest)
        {
            if (chest == null || _pickupPrefabField == null) return null;
            try { return _pickupPrefabField.GetValue(chest) as GameObject; }
            catch { return null; }
        }

        public static void ClearOptionChestGeneratedPickups(OptionChestBehavior chest)
        {
            if (chest == null || _generatedPickupsField == null) return;
            try { _generatedPickupsField.SetValue(chest, null); }
            catch { }
        }

        public static void HandleOptionChestOpened(OptionChestBehavior self)
        {
            if (!UnityEngine.Networking.NetworkServer.active) return;
            if (self == null || self.gameObject == null) return;

            int instanceId = self.gameObject.GetInstanceID();
            if (DropStateManager.ProcessedChestInstanceIds.Contains(instanceId)) return;
            DropStateManager.ProcessedChestInstanceIds.Add(instanceId);

            string objName = self.gameObject.name ?? "";
            string lower = objName.ToLowerInvariant();



            if (InteractableClassifier.IsLunarInteractable(self.gameObject, lower) || InteractableClassifier.IsBazaarScene()) return;

            CharacterMaster? openerMaster = FindOpenerMaster(self.gameObject);
            DropStateManager.LastInteractors.Remove(self.gameObject);

            ItemTier tier = GetOptionChestDisplayTier(self);
            bool isVoidTier = tier == ItemTier.VoidTier1 || tier == ItemTier.VoidTier2 || tier == ItemTier.VoidTier3 || tier == ItemTier.VoidBoss;

            GameObject? pPrefab = GetOptionChestPickupPrefab(self);
            bool isVoidPrefab = pPrefab != null && pPrefab.name.IndexOf("Void", StringComparison.OrdinalIgnoreCase) >= 0;

            bool isVoid = InteractableClassifier.IsVoidChestName(lower) || isVoidTier || isVoidPrefab;

            if (InteractableClassifier.IsVoidCradle(self.gameObject, lower))
            {
                string openerNameVoid = PlayerUtil.GetMasterDisplayName(openerMaster);
                string openerIdVoid = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(openerMaster));
                DropStateManager.LastVoidRewardTime = Time.time;
                ServerEvents.Broadcast(Localization.Get(
                    $"<color=#D070FF>[ItemRoulette:VoidChoice:{openerIdVoid}:1] {openerNameVoid} открыл Колыбель Бездны! Предоставлен выбор бесплатной крутки Бездны.</color>",
                    $"<color=#D070FF>[ItemRoulette:VoidChoice:{openerIdVoid}:1] {openerNameVoid} opened a Void Cradle! Void choice granted.</color>"), openerNameVoid);
                return;
            }

            if (isVoid || InteractableClassifier.IsVoidPotential(self.gameObject, lower))
            {
                string openerNameVoidPot = PlayerUtil.GetMasterDisplayName(openerMaster);
                string openerIdVoidPot = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(openerMaster));
                DropStateManager.LastVoidRewardTime = Time.time;
                ServerEvents.Broadcast(Localization.Get(
                    $"<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:{openerIdVoidPot}:1] {openerNameVoidPot} открыл Потенциал Бездны! Начислена 1 бесплатная крутка (80% зеленый / 15% красный / 5% босс)!</color>",
                    $"<color=#BF58FF>[ItemRoulette:FreeVoidPotentialRoll:{openerIdVoidPot}:1] {openerNameVoidPot} opened Void Potential! Awarded 1 free spin (80% green / 15% red / 5% boss)!</color>"), openerNameVoidPot);
                return;
            }

            ConsumeOpenerSaleStarsServer(self.gameObject, ref openerMaster, out int totalStars, out var starEntries);

            string openerName = PlayerUtil.GetMasterDisplayName(openerMaster);
            string openerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(openerMaster));

            int starMultiplier = 1 + totalStars;
            bool starProc = totalStars > 0;
            string starNote = starProc ? $" (Выгодная звезда: x{starMultiplier})!" : "!";
            string starNoteEn = starProc ? $" (Sale Star: x{starMultiplier})!" : "!";

            QualityTierCompat highestSpentStarTier = QualityTierCompat.None;
            int qualityStarsSpent = 0;
            int normalStarsSpent = 0;

            foreach (var (sTier, sCount) in starEntries)
            {
                if (sTier > QualityTierCompat.None)
                {
                    qualityStarsSpent += sCount;
                    if (sTier > highestSpentStarTier) highestSpentStarTier = sTier;
                }
                else
                {
                    normalStarsSpent += sCount;
                }
            }

            if (highestSpentStarTier > QualityTierCompat.None)
            {
                string qNameRu = QualityCompat.GetQualityName(highestSpentStarTier);
                string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(highestSpentStarTier));
                starNote = $" (<color=#{qColorHex}>{qNameRu} Выгодная звезда: x{starMultiplier}</color>)!";
                starNoteEn = $" (<color=#{qColorHex}>{highestSpentStarTier} Sale Star: x{starMultiplier}</color>)!";
            }

            bool isGold = lower.Contains("goldchest") || lower.Contains("legendary") || tier == ItemTier.Tier3;
            if (isGold)
            {
                int count = 1 + normalStarsSpent;
                if (ModConfig.GoldChestRewardAllPlayers.Value)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:{count}] {{0}} открыл Золотой Сундук!{starNote} Всем игрокам начислено {count} бесплатных круток ЛЕГЕНДАРНЫХ (красных) предметов!</color>",
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:{count}] {{0}} opened Legendary Chest!{starNoteEn} All players awarded {count} free LEGENDARY (Red) spins!</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:All:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount} легендарных круток с качеством {qName}!</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:All:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount} Legendary spin(s) with quality {qTier}!</color>",
                            openerName));
                    }
                }
                else
                {
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{openerId}:{count}] {{0}} открыл Золотой Сундук!{starNote} Начислено {count} бесплатных круток ЛЕГЕНДАРНЫХ предметов.</color>",
                        $"<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{openerId}:{count}] {{0}} opened Legendary Chest!{starNoteEn} Awarded {count} free LEGENDARY spins.</color>",
                        openerName));

                    foreach (var (qTier, qCount) in starEntries)
                    {
                        if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                        string qName = QualityCompat.GetQualityName(qTier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount} легендарных круток с качеством {qName}.</color>",
                            $"<color=#{qColorHex}>[ItemRoulette:FreeLegendaryRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount} Legendary spin(s) with quality {qTier}.</color>",
                            openerName));
                    }
                }
                return;
            }

            int standardCount = 1 + normalStarsSpent;
            if (ModConfig.PreplacedChestRewardAllPlayers.Value)
            {
                ServerEvents.Broadcast(Localization.Format(
                    $"<color=#FFE853>[ItemRoulette:FreeRollAll:{standardCount}] {{0}} открыл сундук!{starNote} Всем игрокам начислено {standardCount} бесплатных круток рулетки!</color>",
                    $"<color=#FFE853>[ItemRoulette:FreeRollAll:{standardCount}] {{0}} opened chest!{starNoteEn} All players awarded {standardCount} free roulette spins!</color>",
                    openerName));

                foreach (var (qTier, qCount) in starEntries)
                {
                    if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                    string qName = QualityCompat.GetQualityName(qTier);
                    string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Всем игрокам начислено +{qCount} круток с качеством {qName}!</color>",
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:All:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! All players awarded +{qCount} spin(s) with quality {qTier}!</color>",
                        openerName));
                }
            }
            else
            {
                ServerEvents.Broadcast(Localization.Format(
                    $"<color=#FFE853>[ItemRoulette:FreeRoll:{openerId}:{standardCount}] {{0}} открыл сундук!{starNote} Начислено {standardCount} бесплатных круток рулетки.</color>",
                    $"<color=#FFE853>[ItemRoulette:FreeRoll:{openerId}:{standardCount}] {{0}} opened chest!{starNoteEn} Awarded {standardCount} free roulette spins.</color>",
                    openerName));

                foreach (var (qTier, qCount) in starEntries)
                {
                    if (qTier <= QualityTierCompat.None || qCount <= 0) continue;
                    string qName = QualityCompat.GetQualityName(qTier);
                    string qColorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(qTier));
                    ServerEvents.Broadcast(Localization.Format(
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} активировал {qName} Выгодную звезду! Начислено +{qCount} круток с качеством {qName}.</color>",
                        $"<color=#{qColorHex}>[ItemRoulette:FreeQualityRoll:{openerId}:{qCount}:{(int)qTier}] {{0}} activated {qTier} Sale Star! Awarded +{qCount} spin(s) with quality {qTier}.</color>",
                        openerName));
                }
            }
        }

        public static CharacterMaster? FindOpenerMaster(GameObject obj)
        {
            if (obj == null) return null;

            Interactor? activator = null;
            if (!DropStateManager.LastInteractors.TryGetValue(obj, out activator))
            {
                if (obj.transform.root != null && DropStateManager.LastInteractors.TryGetValue(obj.transform.root.gameObject, out var rootActivator))
                {
                    activator = rootActivator;
                }
                else
                {
                    foreach (var kvp in DropStateManager.LastInteractors)
                    {
                        if (kvp.Key != null && (kvp.Key.transform.IsChildOf(obj.transform) || obj.transform.IsChildOf(kvp.Key.transform)))
                        {
                            activator = kvp.Value;
                            break;
                        }
                    }
                }
            }

            CharacterMaster? openerMaster = activator?.GetComponent<CharacterBody>()?.master;
            if (openerMaster == null)
            {
                float bestDist = 25f;
                foreach (var body in CharacterBody.readOnlyInstancesList)
                {
                    if (body != null && body.isPlayerControlled && body.master != null)
                    {
                        float d = Vector3.Distance(body.corePosition, obj.transform.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            openerMaster = body.master;
                        }
                    }
                }
            }

            return openerMaster;
        }

        public static void HandleShopTerminalOpened(ShopTerminalBehavior self)
        {
            // Multishops do not award free spins; item is directly granted to inventory upon purchase
        }

        private static void OnScavBackpackOpen(On.RoR2.ScavBackpackBehavior.orig_Open orig, ScavBackpackBehavior self)
        {
            if (UnityEngine.Networking.NetworkServer.active && self != null && self.gameObject != null)
            {
                var chest = self.GetComponent<ChestBehavior>();
                if (chest != null)
                {
                    HandleChestOpened(chest);
                }
            }
            orig(self);
        }
    }
}
