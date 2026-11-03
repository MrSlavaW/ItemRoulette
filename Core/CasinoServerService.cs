using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public class CasinoSpinRequestMessage : MessageBase
    {
        public string txId = "";
        public bool isLifeBet;
        public int moneyBetAmount;
        public byte betType; // (byte)CasinoBetType
        public int targetNumber; // 0-36
        public int itemIndex; // (int)ItemIndex, or -1 if none
        public int itemTargetNumber; // 0-36

        // Multi-bet extension: extra bets placed on the felt
        public byte[] extraBetTypes = Array.Empty<byte>();
        public int[] extraTargetNumbers = Array.Empty<int>();
        public int[] extraBetAmounts = Array.Empty<int>();

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(txId);
            writer.Write(isLifeBet);
            writer.WritePackedUInt32((uint)Math.Max(0, moneyBetAmount));
            writer.Write(betType);
            writer.WritePackedUInt32((uint)Math.Max(0, targetNumber));
            writer.Write(itemIndex);
            writer.WritePackedUInt32((uint)Math.Max(0, itemTargetNumber));

            int extraCount = extraBetTypes != null ? extraBetTypes.Length : 0;
            writer.WritePackedUInt32((uint)extraCount);
            for (int i = 0; i < extraCount; i++)
            {
                writer.Write(extraBetTypes![i]);
                writer.WritePackedUInt32((uint)Math.Max(0, extraTargetNumbers![i]));
                writer.WritePackedUInt32((uint)Math.Max(0, extraBetAmounts![i]));
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            txId = reader.ReadString();
            isLifeBet = reader.ReadBoolean();
            moneyBetAmount = (int)reader.ReadPackedUInt32();
            betType = reader.ReadByte();
            targetNumber = (int)reader.ReadPackedUInt32();
            itemIndex = reader.ReadInt32();
            itemTargetNumber = (int)reader.ReadPackedUInt32();

            int extraCount = (int)reader.ReadPackedUInt32();
            if (extraCount > 0)
            {
                extraBetTypes = new byte[extraCount];
                extraTargetNumbers = new int[extraCount];
                extraBetAmounts = new int[extraCount];
                for (int i = 0; i < extraCount; i++)
                {
                    extraBetTypes[i] = reader.ReadByte();
                    extraTargetNumbers[i] = (int)reader.ReadPackedUInt32();
                    extraBetAmounts[i] = (int)reader.ReadPackedUInt32();
                }
            }
            else
            {
                extraBetTypes = Array.Empty<byte>();
                extraTargetNumbers = Array.Empty<int>();
                extraBetAmounts = Array.Empty<int>();
            }
        }
    }

    public class CasinoSpinResultMessage : MessageBase
    {
        public string txId = "";
        public bool success;
        public byte rejectReason; // 0 = None, 1 = InsufficientFunds, 2 = MissingItem, 3 = DeadOrSpectator, 4 = FeatureDisabled
        public int winningNumber; // 0-36
        public byte winningColor; // 0 = Green, 1 = Red, 2 = Black
        public byte chaosEvent;   // (byte)ChaosEventType
        public byte chaosPolarity; // (byte)ChaosPolarity
        public uint totalPayout;  // gold won from bets
        public bool wonMoney;
        public bool isLifeBet;
        public bool lifeBetSurvived;
        public uint lifeBetJackpot;
        public int duplicatedItemIndex; // ItemIndex of item duplicated on Zero, or -1
        public byte duplicatedItemQuality; // Quality tier if Quality mod is active
        public uint updatedPlayerMoney; // Synced gold after transaction

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(txId);
            writer.Write(success);
            writer.Write(rejectReason);
            writer.WritePackedUInt32((uint)Math.Max(0, winningNumber));
            writer.Write(winningColor);
            writer.Write(chaosEvent);
            writer.Write(chaosPolarity);
            writer.WritePackedUInt32(totalPayout);
            writer.Write(wonMoney);
            writer.Write(isLifeBet);
            writer.Write(lifeBetSurvived);
            writer.WritePackedUInt32(lifeBetJackpot);
            writer.Write(duplicatedItemIndex);
            writer.Write(duplicatedItemQuality);
            writer.WritePackedUInt32(updatedPlayerMoney);
        }

        public override void Deserialize(NetworkReader reader)
        {
            txId = reader.ReadString();
            success = reader.ReadBoolean();
            rejectReason = reader.ReadByte();
            winningNumber = (int)reader.ReadPackedUInt32();
            winningColor = reader.ReadByte();
            chaosEvent = reader.ReadByte();
            chaosPolarity = reader.ReadByte();
            totalPayout = reader.ReadPackedUInt32();
            wonMoney = reader.ReadBoolean();
            isLifeBet = reader.ReadBoolean();
            lifeBetSurvived = reader.ReadBoolean();
            lifeBetJackpot = reader.ReadPackedUInt32();
            duplicatedItemIndex = reader.ReadInt32();
            duplicatedItemQuality = reader.ReadByte();
            updatedPlayerMoney = reader.ReadPackedUInt32();
        }
    }

    public class CasinoClaimAwardRequestMessage : MessageBase
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

    public class PendingCasinoAward
    {
        public string txId = "";
        public int winningNum;
        public RouletteColor winningCol;
        public uint totalPayout;
        public ItemIndex chosenItem = ItemIndex.None;
        public int itemTargetNumber;
        public bool duplicateItem;
        public bool removeItem;
        public byte duplicatedItemQuality;
        public bool isLifeBet;
        public bool lifeSurvived;
        public uint lifeJackpot;
        public CasinoBetType betType;
        public int targetNumber;
        public ChaosEventType chaosEvt = ChaosEventType.None;
        public int totalBet;
        public float timestamp;
    }

    /// <summary>
    /// Server-authoritative service for Casino Roulette transactions, RNG rolls, payouts, and life bets.
    /// Strictly operates with server authority (NetworkServer.active) to prevent multiplayer desyncs and client cheating.
    /// </summary>
    public static class CasinoServerService
    {
        public const short CasinoSpinRequestMsgType = 10084;
        public const short CasinoSpinResultMsgType = 10085;
        public const short CasinoClaimAwardMsgType = 10086;

        private static readonly Dictionary<CharacterMaster, PendingCasinoAward> _pendingCasinoAwards = new();

        public static event Action<CasinoSpinResultMessage>? OnCasinoSpinResultReceived;

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
                CasinoRouletteHelper.ClearSpinHistory();
            };
        }

        public static void EnsureServerHandlers()
        {
            if (NetworkServer.active)
            {
                try
                {
                    NetworkServer.RegisterHandler(CasinoSpinRequestMsgType, OnServerReceiveCasinoSpinRequest);
                    NetworkServer.RegisterHandler(CasinoClaimAwardMsgType, OnServerReceiveCasinoClaimAward);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception registering CasinoServerService server handler: {ex}");
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
                        client.RegisterHandler(CasinoSpinResultMsgType, OnClientReceiveCasinoSpinResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception registering CasinoServerService client handler: {ex}");
            }
        }

        public static void SendCasinoSpinRequest(CasinoSpinRequestMessage req)
        {
            if (req == null) return;

            if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
            {
                ClientScene.readyConnection.Send(CasinoSpinRequestMsgType, req);
            }
            else if (NetworkServer.active)
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    var result = ExecuteCasinoSpinOnServer(localMaster, req);
                    OnClientReceiveCasinoSpinResultDirect(result);
                }
            }
        }

        public static void SendCasinoClaimAwardRequest(string txId)
        {
            var req = new CasinoClaimAwardRequestMessage { txId = txId ?? "" };
            if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
            {
                ClientScene.readyConnection.Send(CasinoClaimAwardMsgType, req);
            }
            else if (NetworkServer.active)
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    GrantPendingCasinoAwardServer(localMaster, txId ?? "");
                }
            }
        }

        private static void OnServerReceiveCasinoClaimAward(NetworkMessage netMsg)
        {
            if (!NetworkServer.active) return;
            try
            {
                var req = netMsg.ReadMessage<CasinoClaimAwardRequestMessage>();
                if (req == null) return;

                var master = ServerSpinService.GetMasterFromConnection(netMsg.conn);
                if (master != null)
                {
                    GrantPendingCasinoAwardServer(master, req.txId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling CasinoClaimAward: {ex}");
            }
        }

        private static void OnServerReceiveCasinoSpinRequest(NetworkMessage netMsg)
        {
            if (!NetworkServer.active) return;
            try
            {
                var msg = netMsg.ReadMessage<CasinoSpinRequestMessage>();
                if (msg == null) return;

                var senderMaster = ServerSpinService.GetMasterFromConnection(netMsg.conn);
                if (senderMaster == null)
                {
                    Debug.LogWarning("[ItemRoulette] CasinoSpinRequest received but sender master could not be resolved.");
                    return;
                }

                var result = ExecuteCasinoSpinOnServer(senderMaster, msg);
                netMsg.conn.Send(CasinoSpinResultMsgType, result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling CasinoSpinRequest: {ex}");
            }
        }

        private static void OnClientReceiveCasinoSpinResult(NetworkMessage netMsg)
        {
            try
            {
                var result = netMsg.ReadMessage<CasinoSpinResultMessage>();
                if (result != null)
                {
                    OnClientReceiveCasinoSpinResultDirect(result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling CasinoSpinResult: {ex}");
            }
        }

        public static void OnClientReceiveCasinoSpinResultDirect(CasinoSpinResultMessage result)
        {
            if (result == null) return;

            // Update local player money if available
            var master = PlayerUtil.GetLocalMaster();
            if (master != null && result.success)
            {
                master.money = result.updatedPlayerMoney;
            }

            // Record into client history buffer
            if (result.success)
            {
                var pocket = new RoulettePocket(
                    result.winningNumber,
                    (RouletteColor)result.winningColor,
                    (ChaosPolarity)result.chaosPolarity,
                    (ChaosEventType)result.chaosEvent
                );
                CasinoRouletteHelper.RecordSpinResult(pocket);
            }

            try
            {
                OnCasinoSpinResultReceived?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in OnCasinoSpinResultReceived event handler: {ex}");
            }
        }

        public static CasinoSpinResultMessage ExecuteCasinoSpinOnServer(CharacterMaster master, CasinoSpinRequestMessage req)
        {
            var result = new CasinoSpinResultMessage
            {
                txId = req.txId,
                isLifeBet = req.isLifeBet,
                duplicatedItemIndex = -1,
                duplicatedItemQuality = 0
            };

            if (master == null)
            {
                result.success = false;
                result.rejectReason = 4;
                return result;
            }

            // If there was an uncollected pending award for this master, resolve it first
            if (_pendingCasinoAwards.TryGetValue(master, out var uncollectedAward))
            {
                _pendingCasinoAwards.Remove(master);
                ExecutePendingCasinoAward(master, uncollectedAward);
            }

            // 1. Validation: Life Bet dead/spectator mode check
            if (req.isLifeBet)
            {
                if (!ModConfig.IsLifeBetActive)
                {
                    result.success = false;
                    result.rejectReason = 4;
                    result.updatedPlayerMoney = master.money;
                    return result;
                }

                var body = master.GetBody();
                bool isAlive = master.hasBody && body != null && body.healthComponent != null && body.healthComponent.alive;
                if (!isAlive || (body != null && body.isPlayerControlled == false))
                {
                    result.success = false;
                    result.rejectReason = 3; // Dead or spectator mode
                    result.updatedPlayerMoney = master.money;
                    return result;
                }
            }

            // 2. Validation: Gold balance check, Minimum bet, Multi-bet restrictions & Lunar coins check
            int totalBet = !req.isLifeBet ? Math.Max(0, req.moneyBetAmount) : 0;
            int totalBetFields = (!req.isLifeBet && req.moneyBetAmount > 0 && (CasinoBetType)req.betType != CasinoBetType.None) ? 1 : 0;

            // Minimum bet check: Cannot bet less than minimum chip ($25)
            if (totalBet > 0 && totalBet < 25)
            {
                result.success = false;
                result.rejectReason = 1; // Insufficient funds (minimum bet is $25)
                result.updatedPlayerMoney = master.money;
                return result;
            }

            if (req.extraBetAmounts != null && req.extraBetAmounts.Length > 0)
            {
                // Requirement 2: Multi-betting is strictly restricted to straight numbers (0-36).
                // Outside bets (color, row, dozen, even/odd, low/high) CANNOT be multi-selected.
                if ((CasinoBetType)req.betType != CasinoBetType.StraightNumber)
                {
                    result.success = false;
                    result.rejectReason = 4; // Invalid bet configuration
                    result.updatedPlayerMoney = master.money;
                    return result;
                }

                for (int i = 0; i < req.extraBetAmounts.Length; i++)
                {
                    int extraAmt = Math.Max(0, req.extraBetAmounts[i]);
                    var eType = (CasinoBetType)req.extraBetTypes[i];
                    if (eType != CasinoBetType.StraightNumber)
                    {
                        result.success = false;
                        result.rejectReason = 4; // Invalid bet configuration (only numbers allowed in multi-bet)
                        result.updatedPlayerMoney = master.money;
                        return result;
                    }

                    if (extraAmt > 0)
                    {
                        totalBetFields++;
                    }
                }
            }

            if (totalBet > 0 && master.money < totalBet)
            {
                result.success = false;
                result.rejectReason = 1; // Insufficient funds
                result.updatedPlayerMoney = master.money;
                return result;
            }

            // Requirement 1: Multi-bet lunar coins check (+4, +8, +16...)
            int requiredLunarCoins = CasinoRouletteHelper.CalculateMultiBetLunarCost(totalBetFields);
            var netUser = master.playerCharacterMasterController != null ? master.playerCharacterMasterController.networkUser : null;
            uint playerLunarCoins = netUser != null ? netUser.lunarCoins : 0;
            if (requiredLunarCoins > 0 && playerLunarCoins < (uint)requiredLunarCoins)
            {
                result.success = false;
                result.rejectReason = 5; // Insufficient Lunar Coins
                result.updatedPlayerMoney = master.money;
                return result;
            }

            // 3. Validation: Item bet check
            ItemIndex chosenItem = req.itemIndex >= 0 ? (ItemIndex)req.itemIndex : ItemIndex.None;
            if (chosenItem != ItemIndex.None)
            {
                if (master.inventory == null || master.inventory.GetItemCountEffective(chosenItem) <= 0)
                {
                    result.success = false;
                    result.rejectReason = 2; // Missing item in inventory
                    result.updatedPlayerMoney = master.money;
                    return result;
                }
            }

            // 4. Atomic Currency Deduction
            if (totalBet > 0)
            {
                master.money = (uint)Math.Max(0, (long)master.money - totalBet);
            }
            if (requiredLunarCoins > 0 && netUser != null)
            {
                netUser.DeductLunarCoins((uint)requiredLunarCoins);
            }

            // 5. Server-Authoritative RNG: Pick winning pocket strictly from European wheel order
            int wheelLength = CasinoRouletteHelper.EuropeanWheelOrder.Length;
            int rolledWheelIndex = Run.instance != null
                ? Run.instance.stageRng.RangeInt(0, wheelLength)
                : UnityEngine.Random.Range(0, wheelLength);

            int winningNum = CasinoRouletteHelper.EuropeanWheelOrder[rolledWheelIndex];
            RouletteColor winningCol = CasinoRouletteHelper.GetColorForNumber(winningNum);

            // Roll Chaos Event on the winning sector (if Chaos enabled)
            ChaosEventType chaosEvt = ChaosEventType.None;
            ChaosPolarity chaosPol = ChaosPolarity.None;
            try
            {
                var chances = ChaosEventManager.CalculateChances(master, false);
                chaosEvt = ChaosEventManager.RollEvent(chances);
                var def = ChaosEventManager.GetDefinition(chaosEvt);
                chaosPol = def != null ? def.Polarity : ChaosPolarity.None;
            }
            catch { }

            // 6. Payout Calculation: Money Bets
            uint totalPayout = 0;
            bool wonAnyMoney = false;

            if (!req.isLifeBet)
            {
                // Main bet
                var mainType = (CasinoBetType)req.betType;
                if (mainType != CasinoBetType.None && req.moneyBetAmount > 0)
                {
                    if (CasinoRouletteHelper.DoesBetWin(mainType, req.targetNumber, winningNum, out int mult))
                    {
                        totalPayout += (uint)(req.moneyBetAmount * mult);
                        wonAnyMoney = true;
                    }
                }

                // Extra bets (Multi-betting)
                if (req.extraBetTypes != null && req.extraBetTypes.Length > 0)
                {
                    for (int i = 0; i < req.extraBetTypes.Length; i++)
                    {
                        var eType = (CasinoBetType)req.extraBetTypes[i];
                        int eTarget = req.extraTargetNumbers != null && i < req.extraTargetNumbers.Length ? req.extraTargetNumbers[i] : 0;
                        int eAmount = req.extraBetAmounts != null && i < req.extraBetAmounts.Length ? req.extraBetAmounts[i] : 0;

                        if (eType != CasinoBetType.None && eAmount > 0)
                        {
                            if (CasinoRouletteHelper.DoesBetWin(eType, eTarget, winningNum, out int eMult))
                            {
                                totalPayout += (uint)(eAmount * eMult);
                                wonAnyMoney = true;
                            }
                        }
                    }
                }
            }

            // 7. Payout Calculation: Item Bet on Number (0-36)
            bool duplicateItem = false;
            bool removeItem = false;
            byte originalQualityTier = 0;
            int itemTarget = Math.Clamp(req.itemTargetNumber, 0, 36);
            if (chosenItem != ItemIndex.None && master.inventory != null)
            {
                if (winningNum == itemTarget)
                {
                    duplicateItem = true;
                    if (QualityCompat.IsQualityLoaded)
                    {
                        var qTier = QualityCompat.GetQualityTier(chosenItem);
                        if (qTier != QualityTierCompat.None)
                        {
                            originalQualityTier = (byte)qTier;
                        }
                    }
                    result.duplicatedItemIndex = (int)chosenItem;
                    result.duplicatedItemQuality = originalQualityTier;
                }
                else
                {
                    removeItem = true;
                }
            }

            // 8. Payout Calculation: Life Bet
            bool lifeSurvived = false;
            uint lifeJackpot = 0;

            if (req.isLifeBet)
            {
                var mainType = (CasinoBetType)req.betType;
                lifeSurvived = CasinoRouletteHelper.DoesBetWin(mainType, req.targetNumber, winningNum, out int lifeMult);

                var (lifePrice, _, _) = CasinoRouletteHelper.CalculateLifeBetPrice(master);
                if (lifeSurvived)
                {
                    int finalMult = lifeMult > 0 ? lifeMult : 2;
                    lifeJackpot = (uint)(lifePrice * finalMult);
                }
            }

            // 9. Store Pending Award (Deferred until wheel stops spinning on client)
            var pendingAward = new PendingCasinoAward
            {
                txId = req.txId,
                winningNum = winningNum,
                winningCol = winningCol,
                totalPayout = totalPayout,
                chosenItem = chosenItem,
                itemTargetNumber = itemTarget,
                duplicateItem = duplicateItem,
                removeItem = removeItem,
                duplicatedItemQuality = originalQualityTier,
                isLifeBet = req.isLifeBet,
                lifeSurvived = lifeSurvived,
                lifeJackpot = lifeJackpot,
                betType = (CasinoBetType)req.betType,
                targetNumber = req.targetNumber,
                chaosEvt = chaosEvt,
                totalBet = totalBet,
                timestamp = Time.time
            };
            _pendingCasinoAwards[master] = pendingAward;

            // 10. Record into server history buffer
            var winningPocket = new RoulettePocket(winningNum, winningCol, chaosPol, chaosEvt);
            CasinoRouletteHelper.RecordSpinResult(winningPocket);

            // 11. Populate Result Message
            result.success = true;
            result.rejectReason = 0;
            result.winningNumber = winningNum;
            result.winningColor = (byte)winningCol;
            result.chaosEvent = (byte)chaosEvt;
            result.chaosPolarity = (byte)chaosPol;
            result.totalPayout = totalPayout;
            result.wonMoney = wonAnyMoney;
            result.isLifeBet = req.isLifeBet;
            result.lifeBetSurvived = lifeSurvived;
            result.lifeBetJackpot = lifeJackpot;
            result.updatedPlayerMoney = master.money;

            return result;
        }

        public static void GrantPendingCasinoAwardServer(CharacterMaster master, string txId)
        {
            if (master == null) return;
            if (_pendingCasinoAwards.TryGetValue(master, out var pending))
            {
                if (string.IsNullOrEmpty(txId) || string.IsNullOrEmpty(pending.txId) || pending.txId == txId)
                {
                    _pendingCasinoAwards.Remove(master);
                    ExecutePendingCasinoAward(master, pending);
                }
            }
        }

        public static void ExecutePendingCasinoAward(CharacterMaster master, PendingCasinoAward award)
        {
            if (master == null || award == null) return;

            // 1. Payout gold
            if (!award.isLifeBet && award.totalPayout > 0)
            {
                master.GiveMoney(award.totalPayout, true);
            }

            // 2. Item Bet on Chosen Number (0-36)
            if (award.chosenItem != ItemIndex.None && master.inventory != null)
            {
                var itemDef = ItemCatalog.GetItemDef(award.chosenItem);
                string itemName = itemDef != null ? Language.GetString(itemDef.nameToken) : award.chosenItem.ToString();
                string playerName = PlayerUtil.GetMasterDisplayName(master);

                if (award.duplicateItem)
                {
                    master.inventory.GiveItem(award.chosenItem, 1);
                    ServerEvents.Broadcast(Localization.FormatToken("ITEMROULETTE_CASINO_ITEM_DUPLICATED", award.itemTargetNumber, playerName, itemName), playerName);
                }
                else if (award.removeItem)
                {
                    master.inventory.RemoveItemPermanent(award.chosenItem, 1);
                    ServerEvents.Broadcast(Localization.FormatToken("ITEMROULETTE_CASINO_ITEM_LOST", award.winningNum, award.itemTargetNumber, playerName, itemName), playerName);
                }
            }

            // 3. Life Bet resolution
            if (award.isLifeBet)
            {
                string betTypeName = CasinoRouletteHelper.GetBetTypeName(award.betType, award.targetNumber);
                string playerName = PlayerUtil.GetMasterDisplayName(master);

                if (award.lifeSurvived)
                {
                    if (award.lifeJackpot > 0)
                    {
                        master.GiveMoney(award.lifeJackpot, true);
                    }

                    ServerEvents.Broadcast(Localization.FormatToken("ITEMROULETTE_CASINO_LIFE_WON", playerName, betTypeName, award.lifeJackpot), playerName);
                }
                else
                {
                    ServerEvents.Broadcast(Localization.FormatToken("ITEMROULETTE_CASINO_LIFE_LOST", playerName, betTypeName, award.winningNum), playerName);

                    // Server kill
                    try
                    {
                        var body = master.GetBody();
                        if (ModConfig.LifeBetRespectsExtraLives != null && ModConfig.LifeBetRespectsExtraLives.Value)
                        {
                            // Suicide allows extra life resurrection (Dio's Best Friend, Pluripotent Larva, Seed of Life) to trigger naturally
                            if (body != null && body.healthComponent != null && body.healthComponent.alive)
                            {
                                body.healthComponent.Suicide(null, null, DamageType.BypassArmor | DamageType.BypassBlock);
                            }
                            else
                            {
                                master.TrueKill();
                            }
                        }
                        else
                        {
                            if (body != null && body.healthComponent != null)
                            {
                                var damageInfo = new DamageInfo
                                {
                                    damage = body.healthComponent.fullCombinedHealth * 9999f,
                                    attacker = null,
                                    inflictor = null,
                                    damageType = DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection,
                                    damageColorIndex = DamageColorIndex.Void,
                                    crit = true
                                };
                                body.healthComponent.TakeDamage(damageInfo);
                            }
                            master.TrueKill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ItemRoulette] Exception executing life bet death: {ex}");
                    }
                }
            }

            // 4. Chaos Event on Server
            if (award.chaosEvt != ChaosEventType.None)
            {
                try
                {
                    RouletteItemEntry? eventItemEntry = null;
                    if (award.chosenItem != ItemIndex.None)
                    {
                        eventItemEntry = ItemPoolHelper.CreateEntry(PickupCatalog.FindPickupIndex(award.chosenItem), RouletteTierType.Uncommon);
                    }
                    else if (master.inventory != null)
                    {
                        var validItems = new List<ItemIndex>();
                        foreach (var idx in master.inventory.itemAcquisitionOrder)
                        {
                            var def = ItemCatalog.GetItemDef(idx);
                            if (def != null && !def.hidden && def.tier != ItemTier.NoTier && master.inventory.GetItemCountEffective(idx) > 0)
                            {
                                validItems.Add(idx);
                            }
                        }
                        if (validItems.Count > 0)
                        {
                            var rndItem = validItems[UnityEngine.Random.Range(0, validItems.Count)];
                            eventItemEntry = ItemPoolHelper.CreateEntry(PickupCatalog.FindPickupIndex(rndItem), RouletteTierType.Uncommon);
                        }
                    }

                    if (eventItemEntry == null || eventItemEntry.PickupDef == null || eventItemEntry.PickupDef.itemIndex == ItemIndex.None)
                    {
                        eventItemEntry = ItemPoolHelper.RollRandomEntry(master: master) ?? ItemPoolHelper.CreateEntry(PickupCatalog.FindPickupIndex(RoR2Content.Items.Syringe.itemIndex), RouletteTierType.Common);
                    }

                    ChaosEventManager.ExecuteEvent(master, award.chaosEvt, eventItemEntry, award.totalBet);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception executing chaos event from casino: {ex}");
                }
            }
        }

        public static void FlushAllPendingAwardsServer()
        {
            if (_pendingCasinoAwards.Count == 0) return;
            var list = new List<KeyValuePair<CharacterMaster, PendingCasinoAward>>(_pendingCasinoAwards);
            _pendingCasinoAwards.Clear();
            foreach (var kvp in list)
            {
                if (kvp.Key != null)
                {
                    ExecutePendingCasinoAward(kvp.Key, kvp.Value);
                }
            }
        }

        public static void ClearPendingAwardsServer()
        {
            _pendingCasinoAwards.Clear();
        }
    }
}
