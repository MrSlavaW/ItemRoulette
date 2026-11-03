using System;
using ItemRoulette.Configuration;
using ItemRoulette.Hooks;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public class SpinRequestMessage : MessageBase
    {
        public byte tierKind; // 0 = Auto, 1 = ExplicitVoid
        public string txId = "";
        public short forcedChaosEvent; // 0 = None, or (short)ChaosEventType

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(tierKind);
            writer.Write(txId);
            writer.Write(forcedChaosEvent);
        }

        public override void Deserialize(NetworkReader reader)
        {
            tierKind = reader.ReadByte();
            txId = reader.ReadString();
            forcedChaosEvent = reader.ReadInt16();
        }
    }

    public class SpinResultMessage : MessageBase
    {
        public string txId = "";
        public bool success;
        public byte rejectReason; // 0 = None, 1 = InsufficientFunds, 2 = NoVoidRolls, 3 = InDecision, 4 = RollFailed
        public int pickupIndex;
        public byte tierType;
        public int itemIndex;
        public int equipIndex;
        public byte qualityTier;
        public byte chaosEvent;
        public byte chaosPolarity;
        public int paidCost;
        public bool requiresVoidDecision;
        public bool upgradedByChanceDoll;
        public string oldItemName = "";

        // Synchronized state for client HUD
        public int freeRolls;
        public int freeUncommon;
        public int freeBoss;
        public int freeLegendary;
        public int freeVoidPotential;
        public int freeHighTier;
        public int freeQuality;
        public int freeEquipment;
        public int freeQualityEquipment;
        public int freeLunar;
        public int pendingVoidRolls;
        public int solitudeRolls;
        public float solitudeRarityBonus;
        public int paidRollsThisStage;
        public int totalRolls;
        public uint currentCost;
        public float chaosCostPenaltyMultiplier;
        public bool fiftyFiftyActive;
        public bool lastSpinWasFree;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(txId);
            writer.Write(success);
            writer.Write(rejectReason);
            writer.WritePackedUInt32((uint)pickupIndex);
            writer.Write(tierType);
            writer.WritePackedUInt32((uint)itemIndex);
            writer.WritePackedUInt32((uint)equipIndex);
            writer.Write(qualityTier);
            writer.Write(chaosEvent);
            writer.Write(chaosPolarity);
            writer.WritePackedUInt32((uint)paidCost);
            writer.Write(requiresVoidDecision);
            writer.Write(upgradedByChanceDoll);
            writer.Write(oldItemName);

            writer.WritePackedUInt32((uint)freeRolls);
            writer.WritePackedUInt32((uint)freeUncommon);
            writer.WritePackedUInt32((uint)freeBoss);
            writer.WritePackedUInt32((uint)freeLegendary);
            writer.WritePackedUInt32((uint)freeVoidPotential);
            writer.WritePackedUInt32((uint)freeHighTier);
            writer.WritePackedUInt32((uint)freeQuality);
            writer.WritePackedUInt32((uint)freeEquipment);
            writer.WritePackedUInt32((uint)freeQualityEquipment);
            writer.WritePackedUInt32((uint)freeLunar);
            writer.WritePackedUInt32((uint)pendingVoidRolls);
            writer.WritePackedUInt32((uint)solitudeRolls);
            writer.Write(solitudeRarityBonus);
            writer.WritePackedUInt32((uint)paidRollsThisStage);
            writer.WritePackedUInt32((uint)totalRolls);
            writer.WritePackedUInt32(currentCost);
            writer.Write(chaosCostPenaltyMultiplier);
            writer.Write(fiftyFiftyActive);
            writer.Write(lastSpinWasFree);
        }

        public override void Deserialize(NetworkReader reader)
        {
            txId = reader.ReadString();
            success = reader.ReadBoolean();
            rejectReason = reader.ReadByte();
            pickupIndex = (int)reader.ReadPackedUInt32();
            tierType = reader.ReadByte();
            itemIndex = (int)reader.ReadPackedUInt32();
            equipIndex = (int)reader.ReadPackedUInt32();
            qualityTier = reader.ReadByte();
            chaosEvent = reader.ReadByte();
            chaosPolarity = reader.ReadByte();
            paidCost = (int)reader.ReadPackedUInt32();
            requiresVoidDecision = reader.ReadBoolean();
            upgradedByChanceDoll = reader.ReadBoolean();
            oldItemName = reader.ReadString();

            freeRolls = (int)reader.ReadPackedUInt32();
            freeUncommon = (int)reader.ReadPackedUInt32();
            freeBoss = (int)reader.ReadPackedUInt32();
            freeLegendary = (int)reader.ReadPackedUInt32();
            freeVoidPotential = (int)reader.ReadPackedUInt32();
            freeHighTier = (int)reader.ReadPackedUInt32();
            freeQuality = (int)reader.ReadPackedUInt32();
            freeEquipment = (int)reader.ReadPackedUInt32();
            freeQualityEquipment = (int)reader.ReadPackedUInt32();
            freeLunar = (int)reader.ReadPackedUInt32();
            pendingVoidRolls = (int)reader.ReadPackedUInt32();
            solitudeRolls = (int)reader.ReadPackedUInt32();
            solitudeRarityBonus = reader.ReadSingle();
            paidRollsThisStage = (int)reader.ReadPackedUInt32();
            totalRolls = (int)reader.ReadPackedUInt32();
            currentCost = reader.ReadPackedUInt32();
            chaosCostPenaltyMultiplier = reader.ReadSingle();
            fiftyFiftyActive = reader.ReadBoolean();
            lastSpinWasFree = reader.ReadBoolean();
        }
    }

    public class VoidDecisionRequestMessage : MessageBase
    {
        public byte decision; // 0 = Accept, 1 = Reroll, 2 = Decline
        public string txId = "";
        public int pickupIndex;
        public byte tierType;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(decision);
            writer.Write(txId);
            writer.WritePackedUInt32((uint)pickupIndex);
            writer.Write(tierType);
        }

        public override void Deserialize(NetworkReader reader)
        {
            decision = reader.ReadByte();
            txId = reader.ReadString();
            pickupIndex = (int)reader.ReadPackedUInt32();
            tierType = reader.ReadByte();
        }
    }

    public class VoidDecisionResultMessage : MessageBase
    {
        public string txId = "";
        public bool success;
        public byte decision;
        public int newPickupIndex;
        public byte newQualityTier;
        public string message = "";
        public int pendingVoidRolls;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(txId);
            writer.Write(success);
            writer.Write(decision);
            writer.WritePackedUInt32((uint)newPickupIndex);
            writer.Write(newQualityTier);
            writer.Write(message);
            writer.WritePackedUInt32((uint)pendingVoidRolls);
        }

        public override void Deserialize(NetworkReader reader)
        {
            txId = reader.ReadString();
            success = reader.ReadBoolean();
            decision = reader.ReadByte();
            newPickupIndex = (int)reader.ReadPackedUInt32();
            newQualityTier = reader.ReadByte();
            message = reader.ReadString();
            pendingVoidRolls = (int)reader.ReadPackedUInt32();
        }
    }

    public class ClaimAwardRequestMessage : MessageBase
    {
        public string txId = "";

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(txId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            txId = reader.ReadString();
        }
    }

    /// <summary>
    /// Server-authoritative spin service. Validates costs, rolls items, awards inventory,
    /// and controls Void item decisions without trusting client outcomes.
    /// </summary>
    public static class ServerSpinService
    {
        public const short SpinRequestMsgType = 10079;
        public const short SpinResultMsgType = 10080;
        public const short VoidDecisionRequestMsgType = 10081;
        public const short VoidDecisionResultMsgType = 10082;
        public const short ClaimAwardMsgType = 10083;

        public static event Action<SpinResultMessage>? OnSpinResultReceived;
        public static event Action<VoidDecisionResultMessage>? OnVoidDecisionResultReceived;

        public static void Init()
        {
            RoR2.Networking.NetworkManagerSystem.onStartServerGlobal += EnsureServerHandlers;
            RoR2.Networking.NetworkManagerSystem.onStartClientGlobal += _ => EnsureClientHandlers();
            Stage.onStageStartGlobal += _ =>
            {
                EnsureServerHandlers();
                EnsureClientHandlers();
            };
            Run.onRunStartGlobal += _ =>
            {
                EnsureServerHandlers();
                EnsureClientHandlers();
            };
        }

        public static void EnsureServerHandlers()
        {
            if (NetworkServer.active)
            {
                try
                {
                    NetworkServer.RegisterHandler(SpinRequestMsgType, OnServerReceiveSpinRequest);
                    NetworkServer.RegisterHandler(VoidDecisionRequestMsgType, OnServerReceiveVoidDecisionRequest);
                    NetworkServer.RegisterHandler(ClaimAwardMsgType, OnServerReceiveClaimAward);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception registering ServerSpinService handlers: {ex}");
                }
            }
        }

        public static void EnsureClientHandlers()
        {
            try
            {
                foreach (var client in NetworkClient.allClients)
                {
                    if (client != null)
                    {
                        client.RegisterHandler(SpinResultMsgType, OnClientReceiveSpinResult);
                        client.RegisterHandler(VoidDecisionResultMsgType, OnClientReceiveVoidDecisionResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception registering client Spin handlers: {ex}");
            }
        }

        public static CharacterMaster? GetMasterFromConnection(NetworkConnection conn)
        {
            if (conn == null || conn.playerControllers == null) return null;
            foreach (var pc in conn.playerControllers)
            {
                if (pc != null && pc.gameObject != null)
                {
                    var netUser = pc.gameObject.GetComponent<NetworkUser>();
                    if (netUser != null && netUser.master != null)
                    {
                        return netUser.master;
                    }
                }
            }
            return null;
        }

        public static NetworkUser? GetNetworkUserForMaster(CharacterMaster master)
        {
            if (master == null) return null;
            if (master.playerCharacterMasterController != null && master.playerCharacterMasterController.networkUser != null)
            {
                return master.playerCharacterMasterController.networkUser;
            }
            foreach (var nu in NetworkUser.readOnlyInstancesList)
            {
                if (nu != null && nu.master == master) return nu;
            }
            return null;
        }

        // ------------------------------------------------------------- Client Side Send

        public static void SendSpinRequest(bool explicitVoid, ChaosEventType forcedEvent = ChaosEventType.None)
        {
            SendSpinRequest(explicitVoid ? (byte)1 : (byte)0, forcedEvent);
        }

        public static void SendSpinRequest(byte tierKind, ChaosEventType forcedEvent = ChaosEventType.None)
        {
            string txId = Guid.NewGuid().ToString("N");
            var req = new SpinRequestMessage
            {
                tierKind = tierKind,
                txId = txId,
                forcedChaosEvent = (short)forcedEvent
            };

            if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
            {
                ClientScene.readyConnection.Send(SpinRequestMsgType, req);
            }
            else if (NetworkServer.active)
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    ProcessSpinServerDirect(localMaster, req);
                }
            }
        }

        public static void SendVoidDecision(byte decision, int pickupIndex = 0, byte tierType = 0)
        {
            string txId = Guid.NewGuid().ToString("N");
            var req = new VoidDecisionRequestMessage
            {
                decision = decision,
                txId = txId,
                pickupIndex = pickupIndex,
                tierType = tierType
            };

            if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
            {
                ClientScene.readyConnection.Send(VoidDecisionRequestMsgType, req);
            }
            else if (NetworkServer.active)
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    ProcessVoidDecisionServerDirect(localMaster, req);
                }
            }
        }

        public static void SendClaimAwardRequest()
        {
            string txId = Guid.NewGuid().ToString("N");
            var req = new ClaimAwardRequestMessage { txId = txId };

            if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
            {
                ClientScene.readyConnection.Send(ClaimAwardMsgType, req);
            }
            else if (NetworkServer.active)
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    var state = RouletteManager.GetServerState(localMaster);
                    GrantPendingAwardServer(localMaster, state);
                }
            }
        }

        // ------------------------------------------------------------- Server Message Handlers

        private static void OnServerReceiveClaimAward(NetworkMessage netMsg)
        {
            if (!NetworkServer.active) return;
            try
            {
                var senderMaster = GetMasterFromConnection(netMsg.conn);
                if (senderMaster == null) return;
                var state = RouletteManager.GetServerState(senderMaster);
                GrantPendingAwardServer(senderMaster, state);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling ClaimAwardRequest: {ex}");
            }
        }

        private static void OnServerReceiveSpinRequest(NetworkMessage netMsg)
        {
            if (!NetworkServer.active) return;
            try
            {
                var msg = netMsg.ReadMessage<SpinRequestMessage>();
                if (msg == null) return;

                var senderMaster = GetMasterFromConnection(netMsg.conn);
                if (senderMaster == null)
                {
                    Debug.LogWarning("[ItemRoulette] SpinRequest received but could not resolve sender master.");
                    return;
                }

                var result = ExecuteSpinOnServer(senderMaster, msg);
                netMsg.conn.Send(SpinResultMsgType, result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling SpinRequest: {ex}");
            }
        }

        private static void OnServerReceiveVoidDecisionRequest(NetworkMessage netMsg)
        {
            if (!NetworkServer.active) return;
            try
            {
                var msg = netMsg.ReadMessage<VoidDecisionRequestMessage>();
                if (msg == null) return;

                var senderMaster = GetMasterFromConnection(netMsg.conn);
                if (senderMaster == null) return;

                var result = ExecuteVoidDecisionOnServer(senderMaster, msg);
                netMsg.conn.Send(VoidDecisionResultMsgType, result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling VoidDecisionRequest: {ex}");
            }
        }

        private static void ProcessSpinServerDirect(CharacterMaster localMaster, SpinRequestMessage req)
        {
            var result = ExecuteSpinOnServer(localMaster, req);
            OnClientReceiveSpinResultDirect(result);
        }

        private static void ProcessVoidDecisionServerDirect(CharacterMaster localMaster, VoidDecisionRequestMessage req)
        {
            var result = ExecuteVoidDecisionOnServer(localMaster, req);
            OnClientReceiveVoidDecisionResultDirect(result);
        }

        // ------------------------------------------------------------- Authoritative Server Execution

        private static void PopulateSyncState(SpinResultMessage result, PlayerRouletteState state)
        {
            result.freeRolls = state.FreeRollsAvailable;
            result.freeUncommon = state.FreeUncommonRollsAvailable;
            result.freeBoss = state.FreeBossRollsAvailable;
            result.freeLegendary = state.FreeLegendaryRollsAvailable;
            result.freeVoidPotential = state.FreeVoidPotentialRollsAvailable;
            result.freeHighTier = state.FreeHighTierRollsAvailable;
            result.freeQuality = state.FreeQualityRollsAvailable;
            result.freeEquipment = state.FreeEquipmentRollsAvailable;
            result.freeQualityEquipment = state.FreeQualityEquipmentRollsAvailable;
            result.freeLunar = state.FreeLunarRollsAvailable;
            result.pendingVoidRolls = state.PendingVoidRollsAvailable;
            result.solitudeRolls = state.SolitudeRollsAvailable;
            result.solitudeRarityBonus = state.SolitudeRarityMultiplierBonus;
            result.paidRollsThisStage = state.PaidRollsThisStage;
            result.totalRolls = state.TotalRolls;
            result.chaosCostPenaltyMultiplier = state.ChaosCostPenaltyMultiplier;
            result.fiftyFiftyActive = state.FiftyFiftyActive;
            result.lastSpinWasFree = state.LastSpinWasFree;
            result.currentCost = (uint)RouletteManager.CalculateCost(state.PaidRollsThisStage, state.ChaosCostPenaltyMultiplier);
        }

        private static SpinResultMessage ExecuteSpinOnServer(CharacterMaster master, SpinRequestMessage msg)
        {
            var state = RouletteManager.GetServerState(master);
            state.UpdateStarsIfPending(master);

            var result = new SpinResultMessage { txId = msg.txId, totalRolls = state.TotalRolls };
            PopulateSyncState(result, state);

            if (state.InVoidDecision)
            {
                if (state.PendingVoidEntry == null)
                {
                    // Stale / orphaned decision state: recover cleanly
                    state.InVoidDecision = false;
                }
                else
                {
                    Debug.LogWarning($"[ItemRoulette] SpinRequest rejected: Player '{PlayerUtil.GetMasterIdentifier(master)}' is currently in a pending decision for '{state.PendingVoidEntry.LocalizedName}'.");
                    result.success = false;
                    result.rejectReason = 3; // InDecision
                    return result;
                }
            }

            ChaosEventType forcedChaos = ChaosEventType.None;
            if (msg.forcedChaosEvent > 0 && Enum.IsDefined(typeof(ChaosEventType), (ChaosEventType)msg.forcedChaosEvent))
            {
                if (ModConfig.IsAdminPanelActive)
                {
                    forcedChaos = (ChaosEventType)msg.forcedChaosEvent;
                }
            }
            bool isForcedChaos = forcedChaos != ChaosEventType.None;

            RouletteTierType? resolvedTier = null;
            int paidCost = 0;
            bool guaranteeQuality = false;
            bool isExplicitVoid = (msg.tierKind == 1);
            bool isLunarVoid = (msg.tierKind == 2);
            bool isFree = false;

            if (isLunarVoid)
            {
                isFree = true;
                resolvedTier = RouletteTierType.LunarVoid;
                paidCost = 0;
            }
            else if (isExplicitVoid)
            {
                if (state.PendingVoidRollsAvailable <= 0)
                {
                    Debug.LogWarning($"[ItemRoulette] SpinRequest rejected: Player '{PlayerUtil.GetMasterIdentifier(master)}' has no Void rolls available.");
                    result.success = false;
                    result.rejectReason = 2; // NoVoidRolls
                    return result;
                }
                isFree = true;
                resolvedTier = RouletteTierType.Void;
                paidCost = 0;
            }
            else if (isForcedChaos)
            {
                isFree = true;
                paidCost = 0;
                resolvedTier = null;
            }
            else
            {
                RouletteTierType? freeTier = state.GetActiveFreeRollTier();
                bool hasSolitudeRoll = (freeTier == null && state.SolitudeRollsAvailable > 0);
                isFree = freeTier != null || hasSolitudeRoll || state.FreeRollsAvailable > 0 || state.FreeQualityRollsAvailable > 0;
                int cost = isFree ? 0 : RouletteManager.CalculateCost(state.PaidRollsThisStage, state.ChaosCostPenaltyMultiplier);

                if (!isFree)
                {
                    if (master.money < (uint)cost)
                    {
                        Debug.LogWarning($"[ItemRoulette] SpinRequest rejected: Player '{PlayerUtil.GetMasterIdentifier(master)}' has insufficient funds (has ${master.money}, needs ${cost}).");
                        result.success = false;
                        result.rejectReason = 1; // InsufficientFunds
                        result.currentCost = (uint)cost;
                        return result;
                    }
                    paidCost = cost;
                    resolvedTier = null;
                }
                else
                {
                    paidCost = 0;
                    resolvedTier = freeTier;
                    if (freeTier == RouletteTierType.QualityEquipment)
                    {
                        guaranteeQuality = true;
                    }
                    else if (freeTier == null && state.FreeQualityRollsAvailable > 0)
                    {
                        guaranteeQuality = true;
                    }
                }
            }

            ItemRoulette.Compat.QualityTierCompat minQualityTier = ItemRoulette.Compat.QualityTierCompat.None;
            if (resolvedTier == RouletteTierType.BossReward && state.PendingBossMinQualityTiers.Count > 0)
            {
                minQualityTier = state.PendingBossMinQualityTiers.Peek();
                guaranteeQuality = true;
            }
            else if (state.PendingGeneralMinQualityTiers.Count > 0)
            {
                minQualityTier = state.PendingGeneralMinQualityTiers.Peek();
                guaranteeQuality = true;
            }

            // Roll Item (BEFORE deducting currency or free rolls!)
            var chances = ChaosEventManager.CalculateChances(master, state.FiftyFiftyActive);

            float solitudeBonus = (resolvedTier == null && state.SolitudeRollsAvailable > 0) ? state.SolitudeRarityMultiplierBonus : 0f;
            var entry = ItemPoolHelper.RollRandomEntry(chances: chances, forcedTier: resolvedTier, master: master, guaranteeQuality: guaranteeQuality, solitudeRarityBonus: solitudeBonus, minQualityTier: minQualityTier);
            if (entry == null || entry.PickupDef == null)
            {
                Debug.LogWarning($"[ItemRoulette] Spin roll failed on server: ItemPoolHelper.RollRandomEntry returned null for player '{PlayerUtil.GetMasterIdentifier(master)}', resolvedTier={resolvedTier}, isFree={isFree}. No currency or free rolls were deducted.");
                result.success = false;
                result.rejectReason = 4; // RollFailed
                return result;
            }

            // ROLL SUCCEEDED: Commit state and deduct currency / free rolls!
            if (resolvedTier == RouletteTierType.BossReward && state.PendingBossMinQualityTiers.Count > 0)
            {
                state.PendingBossMinQualityTiers.Dequeue();
            }
            else if (state.PendingGeneralMinQualityTiers.Count > 0)
            {
                state.PendingGeneralMinQualityTiers.Dequeue();
            }
            if (state.FiftyFiftyActive)
            {
                state.FiftyFiftyActive = false;
                if (NetworkHelper.IsLocalMaster(master)) RouletteManager.Instance.FiftyFiftyActive = false;
            }

            state.TotalRolls++;
            state.LastSpinWasFree = isFree;

            if (isLunarVoid)
            {
                // Mandatory Anomalous Mutation spin: free, do not consume saved counters
            }
            else if (isExplicitVoid)
            {
                state.PendingVoidRollsAvailable--;
            }
            else if (!isFree)
            {
                master.money -= (uint)paidCost;
                state.PaidRollsThisStage++;
            }
            else if (resolvedTier == null && state.SolitudeRollsAvailable > 0)
            {
                state.SolitudeRollsAvailable--;
                if (state.SolitudeRollsAvailable <= 0)
                {
                    state.SolitudeRarityMultiplierBonus = 0f;
                }
            }
            else if (!isForcedChaos)
            {
                state.DeductFirstFreeRoll();
            }

            if (isForcedChaos)
            {
                entry.ChaosEvent = forcedChaos;
                var def = ChaosEventManager.GetDefinition(forcedChaos);
                entry.ChaosPolarity = def != null ? def.Polarity : ChaosPolarity.None;
            }
            bool upgradedByDoll = RouletteManager.Instance.TryUpgradeWithChanceDoll(master, ref entry, out string oldItemName, out float dollChance, out int dollCount);

            bool isVoid = entry.IsVoidItem;
            bool isLunar = entry.IsLunarItem;
            bool isEquipment = entry.PickupDef != null && entry.PickupDef.equipmentIndex != EquipmentIndex.None;
            bool alreadyOwned = isVoid && RouletteManager.HasItemInInventory(master, entry.PickupDef?.itemIndex ?? ItemIndex.None);
            bool requiresChoice = isLunarVoid ||
                                  (isEquipment && ModConfig.EnableEquipmentDropChoice.Value) ||
                                  (!isEquipment && isVoid && !alreadyOwned && ModConfig.EnableVoidCorruptChoice.Value) ||
                                  (!isEquipment && isLunar && (ModConfig.EnableVoidCorruptChoice.Value || resolvedTier == RouletteTierType.Lunar));

            state.CurrentDecisionIsLunarVoid = isLunarVoid;

            if (requiresChoice)
            {
                state.InVoidDecision = true;
                state.PendingVoidEntry = entry;
                state.PaidCostForVoidSpin = paidCost;
                state.VoidRerollsUsed = 0;
                state.PendingAward = null;
            }
            else
            {
                // Defer granting until client finishes spinning reel
                state.InVoidDecision = false;
                state.PendingAward = entry;
                state.PendingAwardPaidCost = paidCost;
                state.PendingAwardUpgradedByDoll = upgradedByDoll;
                state.PendingAwardOldItemName = oldItemName;
                state.PendingAwardDollCount = dollCount;
                state.PendingAwardDollChance = dollChance;
            }

            result.success = true;
            result.rejectReason = 0;
            result.pickupIndex = entry.PickupIndex.value;
            result.tierType = (byte)entry.TierType;
            result.itemIndex = entry.PickupDef != null ? (int)entry.PickupDef.itemIndex : -1;
            result.equipIndex = entry.PickupDef != null ? (int)entry.PickupDef.equipmentIndex : -1;
            result.qualityTier = (byte)entry.QualityTier;
            result.chaosEvent = (byte)entry.ChaosEvent;
            result.chaosPolarity = (byte)entry.ChaosPolarity;
            result.paidCost = paidCost;
            result.requiresVoidDecision = requiresChoice;
            result.upgradedByChanceDoll = upgradedByDoll;
            result.oldItemName = oldItemName ?? "";

            PopulateSyncState(result, state);

            return result;
        }

        private static VoidDecisionResultMessage ExecuteVoidDecisionOnServer(CharacterMaster master, VoidDecisionRequestMessage msg)
        {
            var state = RouletteManager.GetServerState(master);
            var result = new VoidDecisionResultMessage { txId = msg.txId, decision = msg.decision };

            if (!state.InVoidDecision || state.PendingVoidEntry == null)
            {
                if (msg.pickupIndex > 0)
                {
                    var pickup = new PickupIndex(msg.pickupIndex);
                    if (pickup.isValid)
                    {
                        var recoveredEntry = ItemPoolHelper.CreateEntry(pickup, (RouletteTierType)msg.tierType);
                        if (recoveredEntry != null && recoveredEntry.PickupDef != null)
                        {
                            Debug.LogWarning($"[ItemRoulette] Recovered Void/Lunar decision state for '{PlayerUtil.GetMasterDisplayName(master)}' ({recoveredEntry.LocalizedName}).");
                            state.InVoidDecision = true;
                            state.PendingVoidEntry = recoveredEntry;
                            if (recoveredEntry.TierType == RouletteTierType.LunarVoid)
                            {
                                state.CurrentDecisionIsLunarVoid = true;
                            }
                        }
                    }
                }
            }

            if (!state.InVoidDecision || state.PendingVoidEntry == null)
            {
                result.success = false;
                result.message = Localization.Get("Нет активного решения Бездны.", "No active Void decision.");
                return result;
            }

            var netUser = GetNetworkUserForMaster(master);

            if (msg.decision == 0) // Accept / Take
            {
                var entry = state.PendingVoidEntry;
                if (entry != null && entry.PickupDef != null)
                {
                    string playerName = PlayerUtil.GetMasterDisplayName(master);
                    string itemName = !string.IsNullOrEmpty(entry.LocalizedName) ? entry.LocalizedName : Localization.Get("Предмет Бездны", "Void Item");
                    string tierStr = !string.IsNullOrEmpty(entry.TierLabel) ? $" ({entry.TierLabel})" : "";
                    string qualitySuffix = entry.QualityTier > ItemRoulette.Compat.QualityTierCompat.None && !string.IsNullOrEmpty(entry.QualityLabel) ? $" [{entry.QualityLabel}]" : "";
                    Color entryColor = entry.RarityColor != default && entry.RarityColor != Color.clear ? entry.RarityColor : ItemPoolHelper.ColorVoid;
                    string colorHex = ColorUtility.ToHtmlStringRGB(entryColor);

                    string wonText = Localization.FormatToken(
                        "ITEMROULETTE_ROULETTE_VOID_CHOSE",
                        colorHex, playerName, itemName, tierStr, qualitySuffix);

                    NotificationManager.BroadcastNotification(wonText, entryColor, NotificationCategory.Roulette, playerName);

                    if (entry.PickupDef.itemIndex != ItemIndex.None)
                    {
                        master.inventory?.GiveItemPermanent(entry.PickupDef.itemIndex, 1);
                        ItemPoolHelper.ApplySubstandardDuplicator(master, entry.PickupDef.itemIndex);
                        if (NetworkHelper.IsLocalMaster(master))
                        {
                            try
                            {
                                CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = entry.PickupIndex, isRental = false });
                            }
                            catch { }
                        }
                    }
                    else if (entry.PickupDef.equipmentIndex != EquipmentIndex.None)
                    {
                        RouletteManager.ExecuteGrantEquipmentServer(master, entry.PickupDef.equipmentIndex);
                        if (NetworkHelper.IsLocalMaster(master))
                        {
                            try
                            {
                                CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = entry.PickupIndex, isRental = false });
                            }
                            catch { }
                        }
                    }

                    if (entry.ChaosEvent != ChaosEventType.None)
                    {
                        ChaosEventManager.ExecuteEvent(master, entry.ChaosEvent, entry, state.PaidCostForVoidSpin);
                    }
                }

                state.InVoidDecision = false;
                state.PendingVoidEntry = null;
                state.CurrentDecisionIsLunarVoid = false;
                state.UpdateStarsIfPending(master);
                result.success = true;
                result.pendingVoidRolls = state.PendingVoidRollsAvailable;
            }
            else if (msg.decision == 1) // Reroll
            {
                var entry = state.PendingVoidEntry;
                bool isEquipment = entry != null && entry.PickupDef != null && entry.PickupDef.equipmentIndex != EquipmentIndex.None;
                if (isEquipment)
                {
                    result.success = false;
                    result.message = Localization.Get("Переролл недоступен для снаряжения.", "Reroll is not available for equipment.");
                    return result;
                }

                int maxRerolls = ModConfig.VoidRerollMaxPerItem.Value;
                if (state.VoidRerollsUsed >= maxRerolls)
                {
                    result.success = false;
                    result.message = Localization.Get("Лимит рероллов исчерпан!", "Reroll limit reached!");
                    return result;
                }

                int cost = ModConfig.VoidRerollLunarCoinsCost.Value;
                if (netUser == null || netUser.lunarCoins < (uint)cost)
                {
                    result.success = false;
                    result.message = Localization.Get("Недостаточно Лунных монет!", "Not enough Lunar Coins!");
                    return result;
                }

                netUser.DeductLunarCoins((uint)cost);
                state.VoidRerollsUsed++;

                RouletteItemEntry? newEntry;
                if (state.CurrentDecisionIsLunarVoid)
                {
                    newEntry = CustomRoulettePool.CreateLunarVoidPool().Roll(master: master);
                }
                else
                {
                    bool wasLunar = (state.PendingVoidEntry?.IsLunarItem ?? false) || ((RouletteTierType)msg.tierType == RouletteTierType.Lunar);
                    newEntry = ItemPoolHelper.RollRandomEntry(forcedTier: wasLunar ? RouletteTierType.Lunar : RouletteTierType.Void, master: master);
                }

                if (newEntry == null || newEntry.PickupDef == null)
                {
                    bool wasLunar = (state.PendingVoidEntry?.IsLunarItem ?? false) || ((RouletteTierType)msg.tierType == RouletteTierType.Lunar);
                    newEntry = entry ?? (wasLunar 
                        ? CustomRoulettePool.CreateLunarChestPool().Roll(master: master) 
                        : CustomRoulettePool.CreateHighTierPylonPool().Roll(master: master));
                }

                state.PendingVoidEntry = newEntry;
                state.InVoidDecision = true;

                result.success = true;
                result.newPickupIndex = newEntry != null ? newEntry.PickupIndex.value : (entry != null ? entry.PickupIndex.value : 0);
                result.newQualityTier = newEntry != null ? (byte)newEntry.QualityTier : (byte)0;
                result.pendingVoidRolls = state.PendingVoidRollsAvailable;
            }
            else if (msg.decision == 2) // Decline / Drop on floor
            {
                var entry = state.PendingVoidEntry;
                bool isEquipment = entry != null && entry.PickupDef != null && entry.PickupDef.equipmentIndex != EquipmentIndex.None;

                if (entry != null)
                {
                    string playerName = PlayerUtil.GetMasterDisplayName(master);
                    string itemName = !string.IsNullOrEmpty(entry.LocalizedName) ? entry.LocalizedName : Localization.Get("Предмет", "Item");
                    string dropText = Localization.FormatToken("ITEMROULETTE_ROULETTE_VOID_DECLINED", playerName, itemName);
                    NotificationManager.BroadcastNotification(dropText, Color.gray, NotificationCategory.Roulette, playerName);
                }

                if (isEquipment && entry != null)
                {
                    RouletteManager.ExecuteDropEquipmentToGroundServer(master, entry.PickupIndex);
                }
                else
                {
                    if (ModConfig.VoidDeclineCostType.Value == VoidDeclineCostMode.LunarCoins)
                    {
                        int cost = ModConfig.VoidDeclineLunarCoinsCost.Value;
                        if (netUser == null || netUser.lunarCoins < (uint)cost)
                        {
                            result.success = false;
                            result.message = Localization.Get("Недостаточно Лунных монет!", "Not enough Lunar Coins!");
                            return result;
                        }
                        netUser.DeductLunarCoins((uint)cost);
                    }
                    else
                    {
                        bool wasLunar = (state.PendingVoidEntry?.IsLunarItem ?? false) || ((RouletteTierType)msg.tierType == RouletteTierType.Lunar);
                        CursePenaltyHooks.ApplyCursePenaltyServer(master, state.PendingVoidEntry?.LocalizedName ?? "", wasLunar);
                    }
                }

                state.InVoidDecision = false;
                state.PendingVoidEntry = null;
                state.CurrentDecisionIsLunarVoid = false;
                state.UpdateStarsIfPending(master);
                result.success = true;
                result.pendingVoidRolls = state.PendingVoidRollsAvailable;
            }

            return result;
        }

        public static void GrantPendingAwardServer(CharacterMaster master, PlayerRouletteState state)
        {
            if (master == null || state == null || state.PendingAward == null || state.InVoidDecision) return;

            var entry = state.PendingAward;
            int paidCost = state.PendingAwardPaidCost;
            bool upgradedByDoll = state.PendingAwardUpgradedByDoll;
            string? oldItemName = state.PendingAwardOldItemName;
            int dollCount = state.PendingAwardDollCount;
            float dollChance = state.PendingAwardDollChance;

            state.PendingAward = null;

            string playerNameWon = PlayerUtil.GetMasterDisplayName(master);
            string itemWonName = !string.IsNullOrEmpty(entry.LocalizedName) ? entry.LocalizedName : Localization.Get("Предмет", "Item");
            string wonTierStr = !string.IsNullOrEmpty(entry.TierLabel) ? $" ({entry.TierLabel})" : "";
            string wonQualitySuffix = entry.QualityTier > ItemRoulette.Compat.QualityTierCompat.None && !string.IsNullOrEmpty(entry.QualityLabel) ? $" [{entry.QualityLabel}]" : "";
            Color entryWonColor = entry.RarityColor != default && entry.RarityColor != Color.clear ? entry.RarityColor : Color.white;
            string colorWonHex = ColorUtility.ToHtmlStringRGB(entryWonColor);

            string wonRegularText = Localization.FormatToken(
                "ITEMROULETTE_ROULETTE_WON",
                colorWonHex, playerNameWon, itemWonName, wonTierStr, wonQualitySuffix);

            NotificationManager.BroadcastNotification(wonRegularText, entryWonColor, NotificationCategory.Roulette, playerNameWon);

            if (upgradedByDoll)
            {
                ServerEvents.Broadcast(Localization.FormatToken(
                    "ITEMROULETTE_ROULETTE_CHANCE_DOLL",
                    playerNameWon, oldItemName ?? "", entry.LocalizedName, entry.TierLabel, dollChance), playerNameWon);
            }

            if (entry.PickupDef != null)
            {
                if (entry.PickupDef.itemIndex != ItemIndex.None)
                {
                    master.inventory?.GiveItemPermanent(entry.PickupDef.itemIndex, 1);
                    ItemPoolHelper.ApplySubstandardDuplicator(master, entry.PickupDef.itemIndex);
                    if (NetworkHelper.IsLocalMaster(master))
                    {
                        try
                        {
                            var pIndex = entry.PickupIndex;
                            if (pIndex != PickupIndex.none)
                            {
                                CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pIndex, isRental = false });
                            }
                        }
                        catch { }
                    }
                }
                else if (entry.PickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    RouletteManager.ExecuteGrantEquipmentServer(master, entry.PickupDef.equipmentIndex);
                    if (NetworkHelper.IsLocalMaster(master))
                    {
                        try
                        {
                            var pIndex = entry.PickupIndex;
                            if (pIndex != PickupIndex.none)
                            {
                                CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pIndex, isRental = false });
                            }
                        }
                        catch { }
                    }
                }
            }

            if (entry.ChaosEvent != ChaosEventType.None)
            {
                ChaosEventManager.ExecuteEvent(master, entry.ChaosEvent, entry, paidCost);
            }

            state.UpdateStarsIfPending(master);
        }

        // ------------------------------------------------------------- Client Receivers

        private static void OnClientReceiveSpinResult(NetworkMessage netMsg)
        {
            try
            {
                var msg = netMsg.ReadMessage<SpinResultMessage>();
                if (msg != null)
                {
                    OnClientReceiveSpinResultDirect(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling SpinResult on client: {ex}");
            }
        }

        private static void OnClientReceiveVoidDecisionResult(NetworkMessage netMsg)
        {
            try
            {
                var msg = netMsg.ReadMessage<VoidDecisionResultMessage>();
                if (msg != null)
                {
                    OnClientReceiveVoidDecisionResultDirect(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling VoidDecisionResult on client: {ex}");
            }
        }

        private static void OnClientReceiveSpinResultDirect(SpinResultMessage msg)
        {
            // Sync local RouletteManager state immediately
            var mgr = RouletteManager.Instance;
            mgr.FreeRollsAvailable = msg.freeRolls;
            mgr.FreeUncommonRollsAvailable = msg.freeUncommon;
            mgr.FreeBossRollsAvailable = msg.freeBoss;
            mgr.FreeLegendaryRollsAvailable = msg.freeLegendary;
            mgr.FreeVoidPotentialRollsAvailable = msg.freeVoidPotential;
            mgr.FreeHighTierRollsAvailable = msg.freeHighTier;
            mgr.FreeQualityRollsAvailable = msg.freeQuality;
            mgr.FreeEquipmentRollsAvailable = msg.freeEquipment;
            mgr.FreeQualityEquipmentRollsAvailable = msg.freeQualityEquipment;
            mgr.FreeLunarRollsAvailable = msg.freeLunar;
            mgr.PendingVoidRollsAvailable = msg.pendingVoidRolls;
            mgr.SolitudeRollsAvailable = msg.solitudeRolls;
            mgr.SolitudeRarityMultiplierBonus = msg.solitudeRarityBonus;
            mgr.PaidRollsThisStage = msg.paidRollsThisStage;
            mgr.TotalRolls = msg.totalRolls;
            mgr.ChaosCostPenaltyMultiplier = msg.chaosCostPenaltyMultiplier;
            mgr.FiftyFiftyActive = msg.fiftyFiftyActive;
            mgr.LastSpinWasFree = msg.lastSpinWasFree;

            OnSpinResultReceived?.Invoke(msg);
        }

        private static void OnClientReceiveVoidDecisionResultDirect(VoidDecisionResultMessage msg)
        {
            var mgr = RouletteManager.Instance;
            mgr.PendingVoidRollsAvailable = msg.pendingVoidRolls;

            OnVoidDecisionResultReceived?.Invoke(msg);
        }
    }
}
