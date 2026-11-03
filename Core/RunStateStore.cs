using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Central state store for current run and stage variables:
    /// free roll inventories, temporary chaos modifiers, spectral items, gravity and curse stacks.
    /// </summary>
    public class RunStateStore
    {
        private static RunStateStore? _instance;
        public static RunStateStore Instance => _instance ??= new RunStateStore();

        // -----------------------------------------------------------------
        // Roll Inventories & Counters
        // -----------------------------------------------------------------
        public int FreeRollsAvailable { get; set; } = 0;
        public int FreeUncommonRollsAvailable { get; set; } = 0;
        public int FreeBossRollsAvailable { get; set; } = 0;
        public int FreeLegendaryRollsAvailable { get; set; } = 0;
        public int FreeVoidPotentialRollsAvailable { get; set; } = 0;
        public int FreeHighTierRollsAvailable { get; set; } = 0;
        public int FreeQualityRollsAvailable { get; set; } = 0;
        public int FreeEquipmentRollsAvailable { get; set; } = 0;
        public int FreeQualityEquipmentRollsAvailable { get; set; } = 0;
        public int FreeLunarRollsAvailable { get; set; } = 0;
        public int PendingVoidRollsAvailable { get; set; } = 0;
        public int SolitudeRollsAvailable { get; set; } = 0;
        public float SolitudeRarityMultiplierBonus { get; set; } = 0f;
        public int PendingFreeRollsNextStage { get; set; } = 0;

        public int PaidRollsThisStage { get; set; } = 0;
        public int TotalRolls { get; set; } = 0;

        public bool LastSpinWasFree { get; set; } = false;
        public RouletteItemEntry? StoredPendingVoidDrop { get; set; } = null;

        // -----------------------------------------------------------------
        // Stage Modifiers & Temporary Effects
        // -----------------------------------------------------------------
        public float ChaosCostPenaltyMultiplier { get; set; } = 0f;
        public bool FiftyFiftyActive { get; set; } = false;

        public readonly HashSet<ArtifactIndex> StageTemporaryArtifacts = new HashSet<ArtifactIndex>();

        public bool IsLowGravityActive { get; private set; } = false;
        public float OriginalGravityValue { get; set; } = -30f;

        private readonly Dictionary<uint, float> _playerTierMultipliers = new Dictionary<uint, float>();
        private readonly List<(CharacterMaster master, ItemIndex itemIndex, int count)> _spectralItems = new List<(CharacterMaster, ItemIndex, int)>();
        private readonly Dictionary<NetworkInstanceId, int> _playerVoidCurseStacks = new Dictionary<NetworkInstanceId, int>();

        // -----------------------------------------------------------------
        // Stage Activation Counters
        // -----------------------------------------------------------------
        public int MountainShrinesUsedThisStage { get; set; } = 0;
        public bool StarsAppliedThisStage { get; set; } = false;
        public bool TeleporterChargedThisStage { get; set; } = false;

        public int LastResetStageInstanceId { get; set; } = 0;
        public int LastResetStageClearCount { get; set; } = -1;

        // -----------------------------------------------------------------
        // Modifiers Management
        // -----------------------------------------------------------------
        public void ClearStageTemporaryArtifacts()
        {
            if (StageTemporaryArtifacts.Count == 0) return;

            try
            {
                if (RunArtifactManager.instance != null)
                {
                    foreach (var artIndex in StageTemporaryArtifacts)
                    {
                        var artDef = ArtifactCatalog.GetArtifactDef(artIndex);
                        if (artDef != null && NetworkServer.active)
                        {
                            RunArtifactManager.instance.SetArtifactEnabledServer(artDef, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception clearing stage temporary artifacts: {ex}");
            }
            finally
            {
                StageTemporaryArtifacts.Clear();
            }
        }

        public void ApplyStageLowGravity(float newGravity)
        {
            if (!IsLowGravityActive)
            {
                OriginalGravityValue = Physics.gravity.y;
                IsLowGravityActive = true;
            }
            Physics.gravity = new Vector3(0f, newGravity, 0f);
        }

        public void RestoreStageGravity()
        {
            if (IsLowGravityActive)
            {
                IsLowGravityActive = false;
                float target = OriginalGravityValue != 0f ? OriginalGravityValue : (Run.instance != null ? Run.baseGravity : -30f);
                Physics.gravity = new Vector3(0f, target, 0f);
            }
        }

        public float GetPlayerTierMultiplier(CharacterMaster? master)
        {
            if (master == null) return 1.0f;
            uint key = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();
            return _playerTierMultipliers.TryGetValue(key, out float mult) ? mult : 1.0f;
        }

        public void SetPlayerTierMultiplier(uint masterNetId, float multiplier)
        {
            _playerTierMultipliers[masterNetId] = Mathf.Max(0.05f, multiplier);
        }

        public void ClearPlayerTierMultipliers()
        {
            _playerTierMultipliers.Clear();
        }

        public void GrantSpectralItems(CharacterMaster master, ItemIndex itemIndex, int count)
        {
            if (!NetworkServer.active || master == null || master.inventory == null) return;

            master.inventory.GiveItemRented(itemIndex, count);
            _spectralItems.Add((master, itemIndex, count));

            try
            {
                var pIndex = PickupCatalog.FindPickupIndex(itemIndex);
                if (pIndex != PickupIndex.none)
                {
                    var unique = new UniquePickup
                    {
                        pickupIndex = pIndex,
                        isRental = true
                    };
                    if (NetworkHelper.IsLocalMaster(master))
                    {
                        CharacterMasterNotificationQueue.PushPickupNotification(master, unique);
                    }
                }
            }
            catch { }
        }

        public void ClearSpectralItems()
        {
            if (!NetworkServer.active)
            {
                _spectralItems.Clear();
                return;
            }

            foreach (var entry in _spectralItems)
            {
                if (entry.master != null && entry.master.inventory != null)
                {
                    if (entry.master.inventory.GetItemCountRented(entry.itemIndex) > 0)
                    {
                        entry.master.inventory.RemoveItemRented(entry.itemIndex, entry.count);
                    }
                }
            }
            _spectralItems.Clear();
        }

        public int GetVoidCurseStacks(CharacterMaster master)
        {
            if (master == null) return 0;
            return _playerVoidCurseStacks.TryGetValue(master.netId, out int count) ? count : 0;
        }

        public void AddVoidCurseStack(CharacterMaster master)
        {
            if (master == null) return;
            int current = GetVoidCurseStacks(master);
            _playerVoidCurseStacks[master.netId] = current + 1;

            var body = master.GetBody();
            if (body != null)
            {
                body.RecalculateStats();
            }
        }

        public void SetVoidCurseStacksClient(uint masterNetIdValue, int stacks)
        {
            var netId = new NetworkInstanceId(masterNetIdValue);
            _playerVoidCurseStacks[netId] = stacks;

            if (PlayerCharacterMasterController.instances != null)
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null && pcmc.master.netId.Value == masterNetIdValue)
                    {
                        var body = pcmc.master.GetBody();
                        if (body != null)
                        {
                            body.RecalculateStats();
                        }
                        break;
                    }
                }
            }
        }

        public void ClearVoidCurseStacks()
        {
            _playerVoidCurseStacks.Clear();
            if (UnityEngine.Networking.NetworkServer.active)
            {
                ServerEvents.SendToAll(new ClientEventMessage
                {
                    kind = (byte)ClientEventKind.SyncVoidCurse,
                    count = 0,
                    subjectNetId = 0
                });
            }
            if (PlayerCharacterMasterController.instances != null)
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null)
                    {
                        var body = pcmc.master.GetBody();
                        if (body != null)
                        {
                            body.RecalculateStats();
                        }
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Resets
        // -----------------------------------------------------------------
        public void ResetForNewStage(int freeRollsPerStageBase = 0, bool resetCost = true)
        {
            ClearStageTemporaryArtifacts();
            RestoreStageGravity();
            ClearSpectralItems();
            ClearPlayerTierMultipliers();
            ClearVoidCurseStacks();

            if (resetCost) PaidRollsThisStage = 0;
            FreeRollsAvailable += freeRollsPerStageBase + PendingFreeRollsNextStage;
            PendingFreeRollsNextStage = 0;

            ChaosCostPenaltyMultiplier = 0f;
            FiftyFiftyActive = false;
            MountainShrinesUsedThisStage = 0;
            StarsAppliedThisStage = false;
            TeleporterChargedThisStage = false;
            LastSpinWasFree = false;
        }

        public void ResetForNewRun()
        {
            ResetForNewStage();

            FreeRollsAvailable = 0;
            FreeUncommonRollsAvailable = 0;
            FreeBossRollsAvailable = 0;
            FreeLegendaryRollsAvailable = 0;
            FreeVoidPotentialRollsAvailable = 0;
            FreeHighTierRollsAvailable = 0;
            FreeQualityRollsAvailable = 0;
            FreeEquipmentRollsAvailable = 0;
            FreeQualityEquipmentRollsAvailable = 0;
            FreeLunarRollsAvailable = 0;
            PendingVoidRollsAvailable = 0;
            SolitudeRollsAvailable = 0;
            SolitudeRarityMultiplierBonus = 0f;
            PendingFreeRollsNextStage = 0;

            TotalRolls = 0;
            LastSpinWasFree = false;
            StoredPendingVoidDrop = null;
            LastResetStageInstanceId = 0;
            LastResetStageClearCount = -1;
        }
    }
}
