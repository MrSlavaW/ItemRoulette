using System;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using ItemRoulette.UI;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 3: Teleporter, Mountain Shrine, Boss drop suppression and mini-boss rewards.
    /// </summary>
    public static class TeleporterRewardHooks
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterChargedGlobal;
            On.RoR2.BossGroup.DropRewards += OnBossGroupDropRewards;
            On.RoR2.ShrineBossBehavior.AddShrineStack += OnShrineBossAddShrineStack;
            GlobalEventManager.onCharacterDeathServer += OnCharacterDeathServer;
            GlobalEventManager.onCharacterLevelUp += OnCharacterLevelUpServer;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterChargedGlobal;
            On.RoR2.BossGroup.DropRewards -= OnBossGroupDropRewards;
            On.RoR2.ShrineBossBehavior.AddShrineStack -= OnShrineBossAddShrineStack;
            GlobalEventManager.onCharacterDeathServer -= OnCharacterDeathServer;
            GlobalEventManager.onCharacterLevelUp -= OnCharacterLevelUpServer;

            _initialized = false;
        }

        private static void OnTeleporterChargedGlobal(TeleporterInteraction tp)
        {
            try
            {
                if (DropStateManager.TeleporterChargedThisStage) return;
                DropStateManager.TeleporterChargedThisStage = true;

                if (NetworkServer.active && ModConfig.EnableSaleStarRework.Value && !DropStateManager.BossSaleStarAwardedThisStage)
                {
                    ProcessBossSaleStarRewardsServer(tp != null ? tp.transform.position : Vector3.zero);
                }

                int tpBonus = (tp != null) ? tp.shrineBonusStacks : 0;
                int mountainCount = Mathf.Max(RouletteManager.Instance.MountainShrinesUsedThisStage, tpBonus);

                int baseBonus = ModConfig.FreeRollsOnTeleporterCharged.Value;
                int shrineBonus = mountainCount * ModConfig.FreeRollsPerMountainShrine.Value;

                if (NetworkServer.active)
                {
                    if (ModConfig.MountainShrineGrantsBossRolls.Value)
                    {
                        int totalBossBonus = baseBonus + shrineBonus;
                        if (totalBossBonus > 0)
                        {
                            string detail = (shrineBonus > 0)
                                ? Localization.Format(
                                    " (Портал: +{0}, Тотем Горы x{1}: +{2})",
                                    " (Teleporter: +{0}, Mountain Shrine x{1}: +{2})",
                                    baseBonus, mountainCount, shrineBonus)
                                : "";

                            ServerEvents.Broadcast(Localization.Format(
                                "<color=#FFCE31>[ItemRoulette:FreeBossRoll:All:{0}] Портал заряжен и босс повержен! Всем игрокам начислено +{0} круток Награды Босса (85% Зелёный / 5% Красный / 10% Босс)!{1}</color>",
                                "<color=#FFCE31>[ItemRoulette:FreeBossRoll:All:{0}] Teleporter charged and boss defeated! All players awarded +{0} Boss Reward spins (85% Green / 5% Red / 10% Boss)!{1}</color>",
                                totalBossBonus, detail));
                        }
                    }
                    else
                    {
                        int totalBonus = baseBonus + shrineBonus;
                        if (totalBonus > 0)
                        {
                            string shrineDetail = (shrineBonus > 0)
                                ? Localization.Format(
                                    " (Портал: +{0}, Тотем Горы x{1}: +{2})",
                                    " (Teleporter: +{0}, Mountain Shrine x{1}: +{2})",
                                    baseBonus, mountainCount, shrineBonus)
                                : "";

                            ServerEvents.Broadcast(Localization.Format(
                                "<color=#FFE853>[ItemRoulette:FreeRollAll:{0}] Портал полностью заряжен! Всем игрокам начислено +{1} бесплатных круток рулетки!{2}</color>",
                                "<color=#FFE853>[ItemRoulette:FreeRollAll:{0}] Teleporter fully charged! All players awarded +{1} free roulette spins!{2}</color>",
                                totalBonus, totalBonus, shrineDetail));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in OnTeleporterChargedGlobal: {ex}");
            }
        }

        private static void OnBossGroupDropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            if (NetworkServer.active && ModConfig.EnableSaleStarRework.Value &&
                ModConfig.SaleStarBossTriggerTimingSetting.Value == SaleStarBossTriggerTiming.OnAllBossesDefeated)
            {
                try
                {
                    Vector3 dropPos = (self != null && self.dropPosition != null) ? self.dropPosition.position : Vector3.zero;
                    ProcessBossSaleStarRewardsServer(dropPos);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception in OnBossGroupDropRewards SaleStar: {ex}");
                }
            }

            if (ModConfig.DisableBossDrops.Value)
            {
                // Suppress teleporter boss item rewards drop
                return;
            }
            orig(self);
        }

        private static void OnShrineBossAddShrineStack(On.RoR2.ShrineBossBehavior.orig_AddShrineStack orig, ShrineBossBehavior self, Interactor activator)
        {
            orig(self, activator);
            if (!NetworkServer.active) return;
            try
            {
                RouletteManager.Instance.OnMountainShrineActivated();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in OnMountainShrineActivated: {ex}");
            }
        }

        private static void OnCharacterDeathServer(DamageReport report)
        {
            if (!NetworkServer.active) return;
            try
            {
                if (report?.victimBody != null)
                {
                    string bodyName = report.victimBody.name ?? "";
                    string lower = bodyName.ToLowerInvariant();

                    bool isAwu = lower.Contains("superroboball") ||
                                 report.victimBody.bodyIndex == BodyCatalog.FindBodyIndex("SuperRoboBallBossBody");

                    if (isAwu)
                    {
                        if (ModConfig.SecretBossRewardAllPlayers.Value)
                        {
                            ServerEvents.Broadcast(Localization.Get(
                                "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:1] Секретный босс 4 этапа повержен (Alloy Worship Unit)! Всем игрокам начислена 1 бесплатная крутка ЛЕГЕНДАРНЫХ (красных) предметов!</color>",
                                "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:All:1] Stage 4 secret boss defeated (Alloy Worship Unit)! All players awarded 1 free LEGENDARY (Red) spin!</color>"));
                        }
                        else
                        {
                            var killer = report.attackerMaster;
                            if (killer != null)
                            {
                                var minionOwnership = killer.GetComponent<MinionOwnership>();
                                if (minionOwnership != null && minionOwnership.ownerMaster != null)
                                {
                                    killer = minionOwnership.ownerMaster;
                                }
                            }
                            string killerName = PlayerUtil.GetMasterDisplayName(killer);
                            string killerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(killer));
                            ServerEvents.Broadcast(Localization.Format(
                                "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{0}:1] Секретный босс 4 этапа повержен (Alloy Worship Unit)! {1} начислена 1 бесплатная крутка ЛЕГЕНДАРНЫХ (красных) предметов!</color>",
                                "<color=#FF5555>[ItemRoulette:FreeLegendaryRoll:{0}:1] Stage 4 secret boss defeated (Alloy Worship Unit)! {1} awarded 1 free LEGENDARY (Red) spin!</color>",
                                killerId, killerName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in OnCharacterDeathServer: {ex}");
            }
        }

        private static void OnCharacterLevelUpServer(CharacterBody body)
        {
            if (!NetworkServer.active || !ModConfig.EnableLongstandingSolitudeMechanic.Value) return;
            try
            {
                if (body == null || body.master == null || body.master.inventory == null) return;
                if (body.master.playerCharacterMasterController == null) return; // Only player characters

                int count = RouletteManager.GetSolitudeCount(body.master.inventory);
                if (count <= 0) return;

                int baseSpins = ModConfig.LongstandingSolitudeFreeRollsBase.Value;
                int stackSpins = (count - 1) * ModConfig.LongstandingSolitudeFreeRollsPerStack.Value;
                int totalSpins = baseSpins + stackSpins;
                if (totalSpins <= 0) return;

                float baseRarity = ModConfig.LongstandingSolitudeRarityMultiplierBase.Value;
                float stackRarity = (count - 1) * ModConfig.LongstandingSolitudeRarityMultiplierPerStack.Value;
                float totalRarity = baseRarity + stackRarity;

                string playerName = PlayerUtil.GetMasterDisplayName(body.master);

                if (ModConfig.LongstandingSolitudeRewardAllPlayers.Value)
                {
                    ServerEvents.Broadcast(Localization.Format(
                        "<color=#4ADEDE>[ItemRoulette:FreeSolitudeRollAll:{0}:{1:F2}] {2} повысил уровень с 'Затяжным одиночеством' ({3} шт.)! Всем игрокам начислено +{0} бесплатных круток (множитель редкости +x{1:F2})!</color>",
                        "<color=#4ADEDE>[ItemRoulette:FreeSolitudeRollAll:{0}:{1:F2}] {2} leveled up with Longstanding Solitude (x{3})! All players awarded +{0} free spins (rarity multiplier +x{1:F2})!</color>",
                        totalSpins, totalRarity, playerName, count));
                }
                else
                {
                    string playerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(body.master));
                    ServerEvents.Broadcast(Localization.Format(
                        "<color=#4ADEDE>[ItemRoulette:FreeSolitudeRoll:{0}:{1}:{2:F2}] {3} повысил уровень с 'Затяжным одиночеством' ({4} шт.)! Начислено +{1} бесплатных круток (множитель редкости +x{2:F2})!</color>",
                        "<color=#4ADEDE>[ItemRoulette:FreeSolitudeRoll:{0}:{1}:{2:F2}] {3} leveled up with Longstanding Solitude (x{4})! Awarded +{1} free spins (rarity multiplier +x{2:F2})!</color>",
                        playerId, totalSpins, totalRarity, playerName, count));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in OnCharacterLevelUpServer: {ex}");
            }
        }

        public static void ProcessBossSaleStarRewardsServer(Vector3 soundPosition)
        {
            if (!NetworkServer.active || !ModConfig.EnableSaleStarRework.Value) return;
            if (DropStateManager.BossSaleStarAwardedThisStage) return;
            if (PlayerCharacterMasterController.instances == null) return;

            ItemIndex defaultConsumedIdx = ItemCatalog.FindItemIndex("LowerPricedChestsConsumed");
            bool anyAwarded = false;

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc == null || pcmc.master == null || pcmc.master.inventory == null) continue;

                var activeEntries = QualityCompat.GetActiveSaleStarsInInventory(pcmc.master.inventory);
                if (activeEntries.Count == 0) continue;

                int totalActiveStars = 0;
                int qualityStarsCount = 0;
                QualityTierCompat highestTier = QualityTierCompat.None;

                // 1. Transform each active star into its exact corresponding consumed variant (preserving quality tier)
                foreach (var entry in activeEntries)
                {
                    totalActiveStars += entry.count;
                    if (entry.tier > QualityTierCompat.None)
                    {
                        qualityStarsCount += entry.count;
                        if (entry.tier > highestTier)
                        {
                            highestTier = entry.tier;
                        }
                    }

                    ItemIndex consumedIdx = QualityCompat.GetConsumedSaleStarIndex(entry.itemIndex);
                    if (consumedIdx == ItemIndex.None) consumedIdx = defaultConsumedIdx;

                    pcmc.master.inventory.RemoveItemPermanent(entry.itemIndex, entry.count);
                    if (consumedIdx != ItemIndex.None)
                    {
                        pcmc.master.inventory.GiveItemPermanent(consumedIdx, entry.count);
                    }
                }

                if (totalActiveStars <= 0) continue;

                // 2. Play Sale Star proc sound
                var bodyObj = pcmc.master.GetBodyObject();
                if (bodyObj != null)
                {
                    Util.PlaySound("Play_item_proc_lowerPricedChest", bodyObj);
                }

                string playerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(pcmc.master));
                string playerName = PlayerUtil.GetMasterDisplayName(pcmc.master);

                // 3. Award +1 Boss Reward spin per star stack, with minimum quality matching each star's tier
                foreach (var entry in activeEntries)
                {
                    if (entry.tier > QualityTierCompat.None && ModConfig.SaleStarQualityGuaranteesMinimumTier.Value)
                    {
                        string qualityNameRu = QualityCompat.GetQualityName(entry.tier);
                        string colorHex = ColorUtility.ToHtmlStringRGB(QualityCompat.GetQualityColor(entry.tier));

                        ServerEvents.Broadcast(Localization.Format(
                            $"<color=#{colorHex}>[ItemRoulette:FreeBossRoll:{playerId}:{entry.count}:{(int)entry.tier}] {{0}} — {qualityNameRu} Выгодная звезда ({{1}} шт.) сработала на награду босса! Начислено +{{1}} круток Награды Босса (мин. качество: {qualityNameRu})!</color>",
                            $"<color=#{colorHex}>[ItemRoulette:FreeBossRoll:{playerId}:{entry.count}:{(int)entry.tier}] {{0}} — {entry.tier} Sale Star (x{{1}}) triggered on boss reward! Awarded +{{1}} Boss Reward spin(s) (min. quality: {entry.tier})!</color>",
                            playerName, entry.count));
                    }
                    else
                    {
                        ServerEvents.Broadcast(Localization.Format(
                            "<color=#FFCE31>[ItemRoulette:FreeBossRoll:{0}:{1}] {2} — Выгодная звезда ({1} шт.) сработала на награду босса! Начислено +{1} круток Награды Босса (85% Зелёный / 5% Красный / 10% Босс)!</color>",
                            "<color=#FFCE31>[ItemRoulette:FreeBossRoll:{0}:{1}] {2} — Sale Star (x{1}) triggered on boss reward! Awarded +{1} Boss Reward spin(s) (85% Green / 5% Red / 10% Boss)!</color>",
                            playerId, entry.count, playerName));
                    }
                }

                anyAwarded = true;
            }

            if (anyAwarded)
            {
                DropStateManager.BossSaleStarAwardedThisStage = true;
            }
        }
    }
}
