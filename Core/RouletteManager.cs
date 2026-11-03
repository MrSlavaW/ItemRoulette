using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Hooks;
using ItemRoulette.UI;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    public class RouletteManager
    {
        private static RouletteManager? _instance;
        public static RouletteManager Instance => _instance ??= new RouletteManager();

        public int FreeRollsAvailable { get => RunStateStore.Instance.FreeRollsAvailable; internal set => RunStateStore.Instance.FreeRollsAvailable = value; }
        public int FreeUncommonRollsAvailable { get => RunStateStore.Instance.FreeUncommonRollsAvailable; internal set => RunStateStore.Instance.FreeUncommonRollsAvailable = value; }
        public int FreeBossRollsAvailable { get => RunStateStore.Instance.FreeBossRollsAvailable; internal set => RunStateStore.Instance.FreeBossRollsAvailable = value; }
        public int FreeLegendaryRollsAvailable { get => RunStateStore.Instance.FreeLegendaryRollsAvailable; internal set => RunStateStore.Instance.FreeLegendaryRollsAvailable = value; }
        public int FreeVoidPotentialRollsAvailable { get => RunStateStore.Instance.FreeVoidPotentialRollsAvailable; internal set => RunStateStore.Instance.FreeVoidPotentialRollsAvailable = value; }
        public int FreeHighTierRollsAvailable { get => RunStateStore.Instance.FreeHighTierRollsAvailable; internal set => RunStateStore.Instance.FreeHighTierRollsAvailable = value; }
        public int FreeQualityRollsAvailable { get => RunStateStore.Instance.FreeQualityRollsAvailable; internal set => RunStateStore.Instance.FreeQualityRollsAvailable = value; }
        public int FreeEquipmentRollsAvailable { get => RunStateStore.Instance.FreeEquipmentRollsAvailable; internal set => RunStateStore.Instance.FreeEquipmentRollsAvailable = value; }
        public int FreeQualityEquipmentRollsAvailable { get => RunStateStore.Instance.FreeQualityEquipmentRollsAvailable; internal set => RunStateStore.Instance.FreeQualityEquipmentRollsAvailable = value; }
        public int FreeLunarRollsAvailable { get => RunStateStore.Instance.FreeLunarRollsAvailable; internal set => RunStateStore.Instance.FreeLunarRollsAvailable = value; }
        public int PendingVoidRollsAvailable { get => RunStateStore.Instance.PendingVoidRollsAvailable; internal set => RunStateStore.Instance.PendingVoidRollsAvailable = value; }
        public int SolitudeRollsAvailable { get => RunStateStore.Instance.SolitudeRollsAvailable; internal set => RunStateStore.Instance.SolitudeRollsAvailable = value; }
        public float SolitudeRarityMultiplierBonus { get => RunStateStore.Instance.SolitudeRarityMultiplierBonus; internal set => RunStateStore.Instance.SolitudeRarityMultiplierBonus = value; }
        public RouletteItemEntry? StoredPendingVoidDrop { get => RunStateStore.Instance.StoredPendingVoidDrop; set => RunStateStore.Instance.StoredPendingVoidDrop = value; }
        public int PaidRollsThisStage { get => RunStateStore.Instance.PaidRollsThisStage; internal set => RunStateStore.Instance.PaidRollsThisStage = value; }
        public int TotalRolls { get => RunStateStore.Instance.TotalRolls; internal set => RunStateStore.Instance.TotalRolls = value; }
        public bool LastSpinWasFree { get => RunStateStore.Instance.LastSpinWasFree; internal set => RunStateStore.Instance.LastSpinWasFree = value; }
        public int MountainShrinesUsedThisStage { get => RunStateStore.Instance.MountainShrinesUsedThisStage; internal set => RunStateStore.Instance.MountainShrinesUsedThisStage = value; }
        public Queue<QualityTierCompat> PendingBossMinQualityTiers { get; } = new Queue<QualityTierCompat>();
        public Queue<QualityTierCompat> PendingGeneralMinQualityTiers { get; } = new Queue<QualityTierCompat>();

        public static bool HasItemInInventory(CharacterMaster? master, ItemIndex itemIndex)
        {
            if (master == null || master.inventory == null || itemIndex == ItemIndex.None) return false;
            return master.inventory.GetItemCountEffective(itemIndex) > 0;
        }

        public static ItemIndex GetSolitudeItemIndex() => ItemCatalog.FindItemIndex("OnLevelUpFreeUnlock");

        public static int GetSolitudeCount(Inventory? inv)
        {
            if (inv == null) return 0;
            var idx = GetSolitudeItemIndex();
            return idx != ItemIndex.None ? inv.GetItemCountEffective(idx) : 0;
        }

        public bool IsNextRollFree => FreeHighTierRollsAvailable > 0 ||
                                      FreeLegendaryRollsAvailable > 0 ||
                                      FreeBossRollsAvailable > 0 ||
                                      FreeQualityEquipmentRollsAvailable > 0 ||
                                      FreeEquipmentRollsAvailable > 0 ||
                                      FreeLunarRollsAvailable > 0 ||
                                      FreeVoidPotentialRollsAvailable > 0 ||
                                      FreeUncommonRollsAvailable > 0 ||
                                      FreeQualityRollsAvailable > 0 ||
                                      SolitudeRollsAvailable > 0 ||
                                      FreeRollsAvailable > 0;
        public bool IsVoidRollAvailable => PendingVoidRollsAvailable > 0;

        public RouletteTierType? ActiveFreeRollTier
        {
            get
            {
                if (FreeHighTierRollsAvailable > 0) return RouletteTierType.HighTierPylon;
                if (FreeLegendaryRollsAvailable > 0) return RouletteTierType.Legendary;
                if (FreeBossRollsAvailable > 0) return RouletteTierType.BossReward;
                if (FreeQualityEquipmentRollsAvailable > 0) return RouletteTierType.QualityEquipment;
                if (FreeEquipmentRollsAvailable > 0) return RouletteTierType.Equipment;
                if (FreeLunarRollsAvailable > 0) return RouletteTierType.Lunar;
                if (FreeVoidPotentialRollsAvailable > 0) return RouletteTierType.VoidPotential;
                if (FreeUncommonRollsAvailable > 0) return RouletteTierType.Uncommon;
                return null;
            }
        }

        public void AddFreeRolls(int count)
        {
            if (count > 0) FreeRollsAvailable += count;
        }

        public void AddFreeUncommonRolls(int count)
        {
            if (count > 0) FreeUncommonRollsAvailable += count;
        }

        public void AddFreeBossRolls(int count)
        {
            if (count > 0) FreeBossRollsAvailable += count;
        }

        public void AddFreeLegendaryRolls(int count)
        {
            if (count > 0) FreeLegendaryRollsAvailable += count;
        }

        public void AddFreeVoidPotentialRolls(int count)
        {
            if (count > 0) FreeVoidPotentialRollsAvailable += count;
        }

        public void AddFreeHighTierRolls(int count)
        {
            if (count > 0) FreeHighTierRollsAvailable += count;
        }

        public void AddFreeQualityRolls(int count)
        {
            if (count > 0) FreeQualityRollsAvailable += count;
        }

        public void AddFreeEquipmentRolls(int count)
        {
            if (count > 0) FreeEquipmentRollsAvailable += count;
        }

        public void AddFreeQualityEquipmentRolls(int count)
        {
            if (count > 0) FreeQualityEquipmentRollsAvailable += count;
        }

        public void AddFreeLunarRolls(int count)
        {
            if (count > 0) FreeLunarRollsAvailable += count;
        }

        public void AddSolitudeFreeRolls(int count, float rarityBonus)
        {
            if (count > 0)
            {
                SolitudeRollsAvailable += count;
                SolitudeRarityMultiplierBonus = Mathf.Max(SolitudeRarityMultiplierBonus, rarityBonus);
            }
        }

        public static void AddSolitudeFreeRollsServer(CharacterMaster master, int count, float rarityBonus)
        {
            if (master == null || count <= 0) return;
            var state = GetServerState(master);
            state.SolitudeRollsAvailable += count;
            state.SolitudeRarityMultiplierBonus = Mathf.Max(state.SolitudeRarityMultiplierBonus, rarityBonus);
            if (NetworkHelper.IsLocalMaster(master))
            {
                Instance.SolitudeRollsAvailable = state.SolitudeRollsAvailable;
                Instance.SolitudeRarityMultiplierBonus = state.SolitudeRarityMultiplierBonus;
            }
        }

        public bool ConsumeFreeQualityRoll()
        {
            if (FreeQualityRollsAvailable > 0)
            {
                FreeQualityRollsAvailable--;
                return true;
            }
            return false;
        }

        public void AddPendingVoidRolls(int count)
        {
            if (count > 0) PendingVoidRollsAvailable += count;
        }

        public void CancelPendingVoidChoice()
        {
            if (PendingVoidRollsAvailable > 0)
            {
                PendingVoidRollsAvailable--;
            }
        }

        private bool _starsAppliedThisStage { get => RunStateStore.Instance.StarsAppliedThisStage; set => RunStateStore.Instance.StarsAppliedThisStage = value; }
        private bool _teleporterChargedThisStage { get => RunStateStore.Instance.TeleporterChargedThisStage; set => RunStateStore.Instance.TeleporterChargedThisStage = value; }
        private int _lastResetStageInstanceId { get => RunStateStore.Instance.LastResetStageInstanceId; set => RunStateStore.Instance.LastResetStageInstanceId = value; }
        private int _lastResetStageClearCount { get => RunStateStore.Instance.LastResetStageClearCount; set => RunStateStore.Instance.LastResetStageClearCount = value; }

        public int PendingFreeRollsNextStage { get => RunStateStore.Instance.PendingFreeRollsNextStage; set => RunStateStore.Instance.PendingFreeRollsNextStage = value; }
        public float ChaosCostPenaltyMultiplier { get => RunStateStore.Instance.ChaosCostPenaltyMultiplier; set => RunStateStore.Instance.ChaosCostPenaltyMultiplier = value; }
        public bool FiftyFiftyActive { get => RunStateStore.Instance.FiftyFiftyActive; set => RunStateStore.Instance.FiftyFiftyActive = value; }
        public HashSet<ArtifactIndex> StageTemporaryArtifacts => RunStateStore.Instance.StageTemporaryArtifacts;

        public void ClearStageTemporaryArtifacts() => RunStateStore.Instance.ClearStageTemporaryArtifacts();

        public bool IsLowGravityActive => RunStateStore.Instance.IsLowGravityActive;
        public float OriginalGravityValue { get => RunStateStore.Instance.OriginalGravityValue; set => RunStateStore.Instance.OriginalGravityValue = value; }

        public void ApplyStageLowGravity(float newGravity) => RunStateStore.Instance.ApplyStageLowGravity(newGravity);
        public void RestoreStageGravity() => RunStateStore.Instance.RestoreStageGravity();

        public float GetPlayerTierMultiplier(CharacterMaster? master) => RunStateStore.Instance.GetPlayerTierMultiplier(master);
        public void SetPlayerTierMultiplier(uint masterNetId, float multiplier) => RunStateStore.Instance.SetPlayerTierMultiplier(masterNetId, multiplier);
        public void ClearPlayerTierMultipliers() => RunStateStore.Instance.ClearPlayerTierMultipliers();

        public void GrantSpectralItems(CharacterMaster master, ItemIndex itemIndex, int count) => RunStateStore.Instance.GrantSpectralItems(master, itemIndex, count);
        public void ClearSpectralItems() => RunStateStore.Instance.ClearSpectralItems();

        public int GetVoidCurseStacks(CharacterMaster master) => RunStateStore.Instance.GetVoidCurseStacks(master);
        public void AddVoidCurseStack(CharacterMaster master) => RunStateStore.Instance.AddVoidCurseStack(master);
        public void SetVoidCurseStacksClient(uint masterNetIdValue, int stacks) => RunStateStore.Instance.SetVoidCurseStacksClient(masterNetIdValue, stacks);
        public void ClearVoidCurseStacks() => RunStateStore.Instance.ClearVoidCurseStacks();

        public void GrantTemporaryItems(CharacterMaster master, ItemIndex itemIndex, int count, float durationSeconds)
        {
            if (!UnityEngine.Networking.NetworkServer.active || master == null || master.inventory == null) return;

            master.inventory.GiveItemTemp(itemIndex, (float)count);

            try
            {
                var pIndex = PickupCatalog.FindPickupIndex(itemIndex);
                if (pIndex != PickupIndex.none)
                {
                    var unique = new UniquePickup
                    {
                        pickupIndex = pIndex,
                        decayValue = (float)count
                    };
                    bool isLocal = NetworkHelper.IsLocalMaster(master);
                    if (isLocal)
                    {
                        CharacterMasterNotificationQueue.PushPickupNotification(master, unique);
                    }
                }
            }
            catch { }
        }

        public void CheckAndHandleStageTransition()
        {
            if (Run.instance == null)
            {
                _lastResetStageInstanceId = 0;
                _lastResetStageClearCount = -1;
                return;
            }

            int curClearCount = Run.instance.stageClearCount;
            int curStageId = Stage.instance != null ? Stage.instance.GetInstanceID() : 0;

            bool stageChanged = false;
            if (_lastResetStageClearCount == -1)
            {
                stageChanged = true;
            }
            else if (curClearCount != _lastResetStageClearCount)
            {
                stageChanged = true;
            }
            else if (curStageId != 0 && _lastResetStageInstanceId != 0 && curStageId != _lastResetStageInstanceId)
            {
                stageChanged = true;
            }

            if (stageChanged)
            {
                Debug.Log($"[ItemRoulette] Stage transition detected (clearCount: {_lastResetStageClearCount} -> {curClearCount}, stageId: {_lastResetStageInstanceId} -> {curStageId}). Resetting for new stage.");
                DropSuppressionHooks.ResetStageDropState();
                ResetForNewStage();
                RouletteUI.Instance?.ResetUI(preserveAutoSpin: true);
            }
        }

        public void ResetForNewStage()
        {
            int curClearCount = Run.instance != null ? Run.instance.stageClearCount : -1;
            int curStageId = Stage.instance != null ? Stage.instance.GetInstanceID() : 0;

            if (curClearCount != -1 && curClearCount == _lastResetStageClearCount &&
                curStageId != 0 && curStageId == _lastResetStageInstanceId)
            {
                return;
            }

            _lastResetStageClearCount = curClearCount;
            if (curStageId != 0)
            {
                _lastResetStageInstanceId = curStageId;
            }

            RunStateStore.Instance.ResetForNewStage(ModConfig.FreeRollsPerStageBase.Value, ModConfig.ResetCostOnNewStage.Value);
            PendingBossMinQualityTiers.Clear();
            PendingGeneralMinQualityTiers.Clear();

            if (UnityEngine.Networking.NetworkServer.active)
            {
                foreach (var kvp in _serverStates)
                {
                    if (kvp.Value.PendingAward != null)
                    {
                        var master = PlayerUtil.FindMaster(kvp.Key, "", null);
                        if (master != null)
                        {
                            ServerSpinService.GrantPendingAwardServer(master, kvp.Value);
                        }
                    }
                    kvp.Value.ResetForNewStage(ModConfig.FreeRollsPerStageBase.Value, ModConfig.ResetCostOnNewStage.Value);
                }

                if (PlayerCharacterMasterController.instances != null)
                {
                    foreach (var pcmc in PlayerCharacterMasterController.instances)
                    {
                        if (pcmc != null && pcmc.master != null && pcmc.master.inventory != null)
                        {
                            var state = GetServerState(pcmc.master);
                            state.UpdateStarsIfPending(pcmc.master);
                        }
                    }
                }
            }
        }

        public void ResetForNewRun()
        {
            RunStateStore.Instance.ResetForNewRun();
            PendingBossMinQualityTiers.Clear();
            PendingGeneralMinQualityTiers.Clear();
            if (UnityEngine.Networking.NetworkServer.active)
            {
                ClearServerStates();
            }
        }

        public void UpdateStarsIfPending(CharacterMaster? master)
        {
            if (master == null) return;
            if (UnityEngine.Networking.NetworkServer.active)
            {
                GetServerState(master).UpdateStarsIfPending(master);
            }
        }

        public static int GetSaleStarCount(Inventory? inventory)
        {
            if (inventory == null) return 0;
            int count = 0;

            // 1. LowerPricedChests (official internal name for Sale Star in Seekers of the Storm / DLC2)
            ItemIndex lowerPricedIdx = ItemCatalog.FindItemIndex("LowerPricedChests");
            if (lowerPricedIdx != ItemIndex.None)
            {
                count += inventory.GetItemCountEffective(lowerPricedIdx);
            }

            // Consumed variant (if player opened a chest earlier in the stage)
            ItemIndex lowerPricedConsumedIdx = ItemCatalog.FindItemIndex("LowerPricedChestsConsumed");
            if (lowerPricedConsumedIdx != ItemIndex.None)
            {
                count += inventory.GetItemCountEffective(lowerPricedConsumedIdx);
            }

            // Quality mod integration: count active and consumed Quality Sale Stars (Uncommon, Rare, Epic, Legendary)
            if (ItemRoulette.Compat.QualityCompat.IsQualityLoaded)
            {
                for (var tier = ItemRoulette.Compat.QualityTierCompat.Uncommon; tier <= ItemRoulette.Compat.QualityTierCompat.Legendary; tier++)
                {
                    if (lowerPricedIdx != ItemIndex.None)
                    {
                        ItemIndex qActive = ItemRoulette.Compat.QualityCompat.GetItemIndexOfQuality(lowerPricedIdx, tier);
                        if (qActive != ItemIndex.None && qActive != lowerPricedIdx)
                        {
                            count += inventory.GetItemCountEffective(qActive);
                        }
                    }
                    if (lowerPricedConsumedIdx != ItemIndex.None)
                    {
                        ItemIndex qConsumed = ItemRoulette.Compat.QualityCompat.GetItemIndexOfQuality(lowerPricedConsumedIdx, tier);
                        if (qConsumed != ItemIndex.None && qConsumed != lowerPricedConsumedIdx)
                        {
                            count += inventory.GetItemCountEffective(qConsumed);
                        }
                    }
                }
            }

            // 2. Direct name "SaleStar" if any mod/DLC uses this alias
            ItemIndex saleStarIdx = ItemCatalog.FindItemIndex("SaleStar");
            if (saleStarIdx != ItemIndex.None && saleStarIdx != lowerPricedIdx && saleStarIdx != lowerPricedConsumedIdx)
            {
                count += inventory.GetItemCountEffective(saleStarIdx);
            }

            // 3. Fallback: scan catalog items for token or name containing LowerPricedChests or SaleStar
            if (count == 0 && ItemCatalog.itemCount > 0)
            {
                for (ItemIndex i = 0; (int)i < ItemCatalog.itemCount; i++)
                {
                    var def = ItemCatalog.GetItemDef(i);
                    if (def != null)
                    {
                        string name = def.name ?? "";
                        if (name.IndexOf("LowerPricedChests", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("SaleStar", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count += inventory.GetItemCountEffective(i);
                        }
                    }
                }
            }

            return count;
        }

        public static int GetChanceDollCount(Inventory? inventory)
        {
            if (inventory == null) return 0;
            int count = 0;

            // 1. ExtraShrineItem (official internal name for Chance Doll / Кукла удачи in Seekers of the Storm / DLC2)
            ItemIndex extraShrineIdx = ItemCatalog.FindItemIndex("ExtraShrineItem");
            if (extraShrineIdx != ItemIndex.None)
            {
                count += inventory.GetItemCountEffective(extraShrineIdx);
            }

            // 2. Direct name "ChanceDoll" if any mod/DLC uses this alias
            ItemIndex chanceDollIdx = ItemCatalog.FindItemIndex("ChanceDoll");
            if (chanceDollIdx != ItemIndex.None && chanceDollIdx != extraShrineIdx)
            {
                count += inventory.GetItemCountEffective(chanceDollIdx);
            }

            // 3. Fallback: scan catalog items for token or name containing ExtraShrineItem or ChanceDoll
            if (count == 0 && ItemCatalog.itemCount > 0)
            {
                for (ItemIndex i = 0; (int)i < ItemCatalog.itemCount; i++)
                {
                    var def = ItemCatalog.GetItemDef(i);
                    if (def != null)
                    {
                        string name = def.name ?? "";
                        if (name.IndexOf("ExtraShrineItem", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("ChanceDoll", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count += inventory.GetItemCountEffective(i);
                        }
                    }
                }
            }

            return count;
        }

        public bool TryUpgradeWithChanceDoll(
            CharacterMaster master,
            ref RouletteItemEntry entry,
            out string oldItemName,
            out float chancePercent,
            out int dollCount)
        {
            oldItemName = "";
            chancePercent = 0f;
            dollCount = 0;

            if (!ModConfig.EnableChanceDollSupport.Value || master == null || master.inventory == null || entry == null || entry.UpgradedByChanceDoll)
            {
                return false;
            }

            // Strictly White (Common) -> Green (Uncommon) and Green (Uncommon) -> Red (Legendary)
            RouletteTierType targetTier;
            if (entry.TierType == RouletteTierType.Common)
            {
                targetTier = RouletteTierType.Uncommon;
            }
            else if (entry.TierType == RouletteTierType.Uncommon)
            {
                targetTier = RouletteTierType.Legendary;
            }
            else
            {
                return false;
            }

            dollCount = GetChanceDollCount(master.inventory);
            if (dollCount <= 0)
            {
                return false;
            }

            float baseChance = ModConfig.ChanceDollBaseChancePercent.Value;
            float stackChance = ModConfig.ChanceDollStackChancePercent.Value;
            chancePercent = Mathf.Clamp(baseChance + (dollCount - 1) * stackChance, 0f, 100f);

            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll >= chancePercent)
            {
                return false;
            }

            var pool = ItemPoolHelper.GetListForTier(targetTier);
            if (pool == null || pool.Count == 0)
            {
                return false;
            }

            oldItemName = !string.IsNullOrEmpty(entry.LocalizedName)
                ? entry.LocalizedName
                : (entry.PickupDef != null ? Language.GetString(entry.PickupDef.nameToken) : Localization.Get("Предмет", "Item"));

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            PickupIndex newPickupIndex = pool[randomIndex];

            var upgradedEntry = ItemPoolHelper.CreateEntry(newPickupIndex, targetTier);
            upgradedEntry.ChaosEvent = entry.ChaosEvent;
            upgradedEntry.ChaosPolarity = entry.ChaosPolarity;
            upgradedEntry.UpgradedByChanceDoll = true;
            upgradedEntry.OriginalNameBeforeDollUpgrade = oldItemName;

            entry = upgradedEntry;
            return true;
        }

        public void OnMountainShrineActivated()
        {
            MountainShrinesUsedThisStage++;
            try
            {
                int bonusPerShrine = ModConfig.FreeRollsPerMountainShrine.Value;
                if (ModConfig.MountainShrineGrantsBossRolls.Value)
                {
                    NetworkHelper.SendChatMessage(Localization.Format(
                        "<color=#E5C962>[ItemRoulette] Вызов Горы принят! Полная зарядка портала принесёт +{0} бесплатную крутку Награды Босса (85% Зелёный / 5% Красный / 10% Босс)!</color>",
                        "<color=#E5C962>[ItemRoulette] Mountain Shrine activated! Fully charging the teleporter will award +{0} free Boss Reward spin(s) (85% Green / 5% Red / 10% Boss)!</color>",
                        bonusPerShrine));
                }
                else
                {
                    NetworkHelper.SendChatMessage(Localization.Format(
                        "<color=#E5C962>[ItemRoulette] Вызов Горы принят! Полная зарядка портала принесёт +{0} бесплатную прокрутку рулетки!</color>",
                        "<color=#E5C962>[ItemRoulette] Mountain Shrine activated! Fully charging the teleporter will award +{0} free roulette spin(s)!</color>",
                        bonusPerShrine));
                }
            }
            catch { }
        }

        public void AddTeleporterChargedFreeRoll(int mountainShrineCount = 0)
        {
            if (_teleporterChargedThisStage) return; // Only once per stage for charged teleporter
            _teleporterChargedThisStage = true;

            int effectiveShrines = Mathf.Max(MountainShrinesUsedThisStage, mountainShrineCount);
            int baseBonus = ModConfig.FreeRollsOnTeleporterCharged.Value;
            int shrineBonus = effectiveShrines * ModConfig.FreeRollsPerMountainShrine.Value;
            int totalBonus = baseBonus + shrineBonus;

            if (totalBonus > 0)
            {
                FreeRollsAvailable += totalBonus;
                try
                {
                    string shrineDetail = shrineBonus > 0 
                        ? Localization.Format(" (Портал: +{0}, Тотем Горы x{1}: +{2})", " (Teleporter: +{0}, Mountain Shrine x{1}: +{2})", baseBonus, effectiveShrines, shrineBonus) 
                        : "";
                    NetworkHelper.SendChatMessage(Localization.Format(
                        "<color=#FFE853>[ItemRoulette] Портал полностью заряжен! Получено бесплатных прокруток: +{0}!{1}</color>",
                        "<color=#FFE853>[ItemRoulette] Teleporter fully charged! Free roulette spins awarded: +{0}!{1}</color>",
                        totalBonus, shrineDetail));
                }
                catch { }
            }
        }

        public void AddBossDefeatedFreeRoll(int mountainShrineCount = 0)
        {
            AddTeleporterChargedFreeRoll(mountainShrineCount);
        }

        private static readonly Dictionary<uint, PlayerRouletteState> _serverStates = new Dictionary<uint, PlayerRouletteState>();

        public static PlayerRouletteState GetServerState(CharacterMaster master)
        {
            if (master == null) return new PlayerRouletteState();
            uint key = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();
            if (!_serverStates.TryGetValue(key, out var state))
            {
                state = new PlayerRouletteState();
                _serverStates[key] = state;
            }
            return state;
        }

        public static void ClearServerStates()
        {
            _serverStates.Clear();
        }

        public static void AddFreeRollsToAllServer(int count)
        {
            if (PlayerCharacterMasterController.instances != null)
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null)
                    {
                        var state = GetServerState(pcmc.master);
                        state.FreeRollsAvailable += count;
                    }
                }
            }
        }

        public static void AddTargetedFreeRollServer(CharacterMaster master, ClientEventKind kind, int count, float value = 0f)
        {
            var state = GetServerState(master);
            switch (kind)
            {
                case ClientEventKind.FreeRoll:
                    state.FreeRollsAvailable += count;
                    if (value >= 0)
                    {
                        for (int i = 0; i < count; i++) state.PendingGeneralMinQualityTiers.Enqueue((ItemRoulette.Compat.QualityTierCompat)(int)value);
                    }
                    break;
                case ClientEventKind.FreeLegendary:
                    state.FreeLegendaryRollsAvailable += count;
                    if (value >= 0)
                    {
                        for (int i = 0; i < count; i++) state.PendingGeneralMinQualityTiers.Enqueue((ItemRoulette.Compat.QualityTierCompat)(int)value);
                    }
                    break;
                case ClientEventKind.FreeQuality:
                    state.FreeQualityRollsAvailable += count;
                    if (value >= 0)
                    {
                        for (int i = 0; i < count; i++) state.PendingGeneralMinQualityTiers.Enqueue((ItemRoulette.Compat.QualityTierCompat)(int)value);
                    }
                    break;
                case ClientEventKind.FreeHighTier: state.FreeHighTierRollsAvailable += count; break;
                case ClientEventKind.FreeUncommon: state.FreeUncommonRollsAvailable += count; break;
                case ClientEventKind.FreeBoss:
                    state.FreeBossRollsAvailable += count;
                    if (value >= 0)
                    {
                        for (int i = 0; i < count; i++) state.PendingBossMinQualityTiers.Enqueue((ItemRoulette.Compat.QualityTierCompat)(int)value);
                    }
                    break;
                case ClientEventKind.FreeEquipment: state.FreeEquipmentRollsAvailable += count; break;
                case ClientEventKind.FreeQualityEquipment: state.FreeQualityEquipmentRollsAvailable += count; break;
                case ClientEventKind.FreeLunar: state.FreeLunarRollsAvailable += count; break;
                case ClientEventKind.SolitudeFreeRoll:
                    state.SolitudeRollsAvailable += count;
                    state.SolitudeRarityMultiplierBonus = Mathf.Max(state.SolitudeRarityMultiplierBonus, value);
                    break;
                case ClientEventKind.AurelioniteMax:
                    state.FreeLegendaryRollsAvailable += 1;
                    state.FreeVoidPotentialRollsAvailable += 2;
                    break;
                case ClientEventKind.VoidChoice: state.PendingVoidRollsAvailable += count; break;
                case ClientEventKind.FreeVoidPotential: state.FreeVoidPotentialRollsAvailable += count; break;
            }
        }

        public static int CalculateCost(int paidRollsThisStage, float chaosPenaltyMultiplier)
        {
            return RoulettePricing.CalculateCost(paidRollsThisStage, chaosPenaltyMultiplier);
        }

        public int GetCurrentCost()
        {
            CheckAndHandleStageTransition();
            if (IsNextRollFree) return 0;
            return RoulettePricing.CalculateCost(PaidRollsThisStage, ChaosCostPenaltyMultiplier);
        }

        public bool CanAfford(CharacterMaster master)
        {
            if (IsNextRollFree) return true;
            return RoulettePricing.CanAfford(master, GetCurrentCost());
        }

        public bool DeductVoidRoll()
        {
            if (PendingVoidRollsAvailable > 0)
            {
                PendingVoidRollsAvailable--;
                TotalRolls++;
                LastSpinWasFree = true;
                return true;
            }
            return false;
        }

        public static void ExecuteGrantEquipmentServer(CharacterMaster master, EquipmentIndex newEquipment)
        {
            if (master == null || master.inventory == null || newEquipment == EquipmentIndex.None) return;

            var body = master.GetBody();
            EquipmentIndex oldEquipment = PlayerUtil.GetStoredEquipmentIndex(master.inventory);

            if (oldEquipment != EquipmentIndex.None)
            {
                PickupIndex oldPickupIndex = PickupCatalog.FindPickupIndex(oldEquipment);
                if (oldPickupIndex != PickupIndex.none)
                {
                    Vector3 dropPos = (body != null)
                        ? (body.corePosition + Vector3.up * 1.5f)
                        : (master.deathFootPosition != Vector3.zero ? master.deathFootPosition + Vector3.up * 1.5f : master.transform.position + Vector3.up * 1.5f);
                    Vector3 dropVel = (body != null) ? (Vector3.up * 5f + body.transform.forward * 3f) : (Vector3.up * 5f);

                    PickupDropletController.CreatePickupDroplet(
                        new GenericPickupController.CreatePickupInfo
                        {
                            pickup = new UniquePickup { pickupIndex = oldPickupIndex }
                        },
                        dropPos,
                        dropVel
                    );
                }
            }

            EquipmentLocation loc = master.inventory.activeEquipmentLocation;
            master.inventory.SetEquipmentIndex(newEquipment, in loc);
        }

        public static void ExecuteDropEquipmentToGroundServer(CharacterMaster master, PickupIndex pickupIndex)
        {
            if (master == null || pickupIndex == PickupIndex.none) return;

            var body = master.GetBody();
            Vector3 dropPos = (body != null)
                ? (body.corePosition + Vector3.up * 1.5f)
                : (master.deathFootPosition != Vector3.zero ? master.deathFootPosition + Vector3.up * 1.5f : master.transform.position + Vector3.up * 1.5f);
            Vector3 dropVel = (body != null) ? (Vector3.up * 5f + body.transform.forward * 3f) : (Vector3.up * 5f);

            PickupDropletController.CreatePickupDroplet(
                new GenericPickupController.CreatePickupInfo
                {
                    pickup = new UniquePickup { pickupIndex = pickupIndex }
                },
                dropPos,
                dropVel
            );
        }
    }
}
