using System;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Coordinator for all drop suppression, item intercept, and reward modules.
    /// Manages sub-hook registration:
    /// - <see cref="SpawnSuppressionHooks"/>: Director card filtering and physical drop suppression
    /// - <see cref="ChestRewardHooks"/>: Chest opening interaction detection and roll rewards
    /// - <see cref="TeleporterRewardHooks"/>: Teleporter, mountain shrine, and boss rewards
    /// - <see cref="DropletInterceptHooks"/>: Option droplet interception and burst debouncing
    /// - <see cref="CursePenaltyHooks"/>: Health reduction curse hooks for Void and Lunar items
    /// </summary>
    public static class DropSuppressionHooks
    {
        private static bool _initialized = false;
        private static NetworkUser.NetworkUserGenericDelegate? _networkUserDiscoveredHandler;

        public static void Init()
        {
            if (_initialized) return;

            // 1. Initialize sub-modules
            SpawnSuppressionHooks.Init();
            ChestRewardHooks.Init();
            TeleporterRewardHooks.Init();
            DropletInterceptHooks.Init();
            CursePenaltyHooks.Init();
            ItemDescriptionHooks.Init();

            // 2. Discover network users for host config sync
            _networkUserDiscoveredHandler = user =>
            {
                if (NetworkServer.active)
                {
                    BroadcastHostConfigSync();
                }
            };
            NetworkUser.onNetworkUserDiscovered += _networkUserDiscoveredHandler;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            // 1. Cleanup sub-modules
            SpawnSuppressionHooks.Cleanup();
            ChestRewardHooks.Cleanup();
            TeleporterRewardHooks.Cleanup();
            DropletInterceptHooks.Cleanup();
            CursePenaltyHooks.Cleanup();
            ItemDescriptionHooks.Cleanup();

            if (_networkUserDiscoveredHandler != null)
            {
                NetworkUser.onNetworkUserDiscovered -= _networkUserDiscoveredHandler;
                _networkUserDiscoveredHandler = null;
            }

            _initialized = false;
        }

        public static void ResetStageDropState()
        {
            DropStateManager.ResetForNewStage();
        }

        public static void BroadcastHostConfigSync()
        {
            if (!NetworkServer.active) return;
            try
            {
                bool casino = ModConfig.EnableCasinoRoulette.Value;
                bool admin = ModConfig.EnableAdminPanel.Value;
                bool chaos = ModConfig.EnableChaosSystem.Value;
                bool lifeBet = ModConfig.EnableLifeBet.Value;
                bool voidChests = ModConfig.EnableVoidChests.Value;

                byte flags = 0;
                if (casino) flags |= 1;
                if (admin) flags |= 2;
                if (chaos) flags |= 4;
                if (lifeBet) flags |= 8;
                if (voidChests) flags |= 16;

                ServerEvents.SendToAll(new ClientEventMessage
                {
                    kind = (byte)ClientEventKind.SyncConfig,
                    flags = flags
                });

                string syncTag = $"[ItemRoulette:SyncConfig:{casino}:{admin}:{chaos}:{lifeBet}:{voidChests}]";
                NetworkHelper.SendChatMessage(syncTag);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in BroadcastHostConfigSync: {ex}");
            }
        }

        // =====================================================================
        // Backward-Compatibility Forwarders
        // =====================================================================

        public static void ApplyVoidHealthPenaltyServer(CharacterMaster master, string itemName)
        {
            CursePenaltyHooks.ApplyCursePenaltyServer(master, itemName, isLunar: false);
        }

        public static CharacterMaster? FindMaster(uint masterNetId, string safeName, CharacterMaster? senderHint = null)
        {
            return PlayerUtil.FindMaster(masterNetId, safeName, senderHint);
        }

        public static CharacterMaster? FindMaster(string idStr, CharacterMaster? senderHint = null)
        {
            return PlayerUtil.FindMaster(idStr, senderHint);
        }

        public static string GetMasterIdentifier(CharacterMaster? master)
        {
            return PlayerUtil.GetMasterIdentifier(master);
        }

        public static string GetMasterDisplayName(CharacterMaster? master)
        {
            return PlayerUtil.GetMasterDisplayName(master);
        }

        public static void ProcessNetworkTag(string tag, CharacterMaster? sender = null)
        {
            if (string.IsNullOrEmpty(tag)) return;
            string clean = tag.Trim('[', ']');
            if (clean.StartsWith("ItemRoulette:"))
            {
                string[] parts = clean.Split(':');
                if (parts.Length >= 2)
                {
                    string action = parts[1];
                    if (action == "ServerBroadcastChat" && parts.Length >= 3)
                    {
                        string chatText = string.Join(":", new ArraySegment<string>(parts, 2, parts.Length - 2));
                        ServerEvents.BroadcastPlain(chatText);
                        return;
                    }
                    if (action == "ServerMoneyDelta" && parts.Length >= 6)
                    {
                        if (uint.TryParse(parts[3], out uint netIdVal) && int.TryParse(parts[5], out int delta))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            if (target != null)
                            {
                                if (delta > 0)
                                    target.GiveMoney((uint)delta, true);
                                else
                                    target.money = (uint)Math.Max(0, (long)target.money + delta);
                            }
                        }
                        return;
                    }
                    if (action == "ServerSuicide" && parts.Length >= 5)
                    {
                        if (uint.TryParse(parts[3], out uint netIdVal))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            target?.GetBody()?.healthComponent?.Suicide();
                        }
                        return;
                    }
                    if (action == "ServerItemAward" && parts.Length >= 7)
                    {
                        if (uint.TryParse(parts[3], out uint netIdVal) && int.TryParse(parts[5], out int itemIdxVal))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            if (target != null && target.inventory != null)
                            {
                                target.inventory.GiveItemPermanent((ItemIndex)itemIdxVal, 1);
                            }
                        }
                        return;
                    }
                    if (action == "ServerItemRemove" && parts.Length >= 7)
                    {
                        if (uint.TryParse(parts[3], out uint netIdVal) && int.TryParse(parts[5], out int itemIdxVal))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            if (target != null && target.inventory != null)
                            {
                                target.inventory.RemoveItemPermanent((ItemIndex)itemIdxVal, 1);
                            }
                        }
                        return;
                    }
                    if (action == "ExecuteCasinoChaos" && parts.Length >= 6)
                    {
                        if (uint.TryParse(parts[3], out uint netIdVal) && int.TryParse(parts[5], out int evTypeVal))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            int cost = parts.Length >= 7 && int.TryParse(parts[6], out int c) ? c : 100;
                            if (target != null && Enum.IsDefined(typeof(ChaosEventType), (ChaosEventType)evTypeVal))
                            {
                                var sampleItem = ItemPoolHelper.RollRandomEntry(master: target);
                                if (sampleItem == null)
                                {
                                    sampleItem = ItemPoolHelper.CreateEntry(PickupCatalog.FindPickupIndex(RoR2Content.Items.Syringe.itemIndex), RouletteTierType.Common);
                                }
                                ChaosEventManager.ExecuteEvent(target, (ChaosEventType)evTypeVal, sampleItem, cost);
                            }
                        }
                        return;
                    }
                    if (action == "AdminAction" && parts.Length >= 6)
                    {
                        if (!ModConfig.IsAdminPanelActive) return;

                        if (uint.TryParse(parts[3], out uint netIdVal))
                        {
                            var target = PlayerUtil.FindMaster(netIdVal, parts[4], sender);
                            if (target != null)
                            {
                                string actionKind = parts[5];
                                int param = parts.Length >= 7 && int.TryParse(parts[6], out int p) ? p : 0;
                                ExecuteAdminActionServer(target, actionKind, param);
                            }
                        }
                        return;
                    }

                    ChaosEventManager.ProcessLocalAction(action, parts);
                }
            }
        }

        public static void ExecuteAdminActionServer(CharacterMaster target, string actionKind, int param)
        {
            if (!NetworkServer.active || !ModConfig.IsAdminPanelActive || target == null) return;

            string targetId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(target));
            var state = RouletteManager.GetServerState(target);

            switch (actionKind)
            {
                case "AddMoney":
                    target.GiveMoney((uint)Math.Max(1, param), true);
                    break;
                case "AddFreeRolls":
                    ServerEvents.Broadcast($"[ItemRoulette:FreeRoll:{targetId}:{param}]");
                    break;
                case "AddFreeLegendary":
                    ServerEvents.Broadcast($"[ItemRoulette:FreeLegendaryRoll:{targetId}:{param}]");
                    break;
                case "AddPendingVoid":
                    ServerEvents.Broadcast($"[ItemRoulette:VoidChoice:{targetId}:{param}]");
                    break;
                case "SetFiftyFifty":
                    state.FiftyFiftyActive = (param != 0);
                    break;
                case "ResetCost":
                    state.ChaosCostPenaltyMultiplier = 0f;
                    break;
                case "ResetLuck":
                    uint netId = target.netId.Value != 0 ? target.netId.Value : (uint)target.GetInstanceID();
                    RouletteManager.Instance.SetPlayerTierMultiplier(netId, 1.0f);
                    ServerEvents.Broadcast($"[ItemRoulette:SetPlayerTierLuck:{netId}:1.0]");
                    break;
                case "GrantSpectral":
                    if (target.inventory != null)
                    {
                        var roll = ItemPoolHelper.RollRandomEntry(master: target);
                        if (roll?.PickupDef != null)
                        {
                            RouletteManager.Instance.GrantSpectralItems(target, roll.PickupDef.itemIndex, param > 0 ? param : 5);
                        }
                    }
                    break;
                case "GrantTemp":
                    if (target.inventory != null)
                    {
                        var roll = ItemPoolHelper.RollRandomEntry(master: target);
                        if (roll?.PickupDef != null)
                        {
                            RouletteManager.Instance.GrantTemporaryItems(target, roll.PickupDef.itemIndex, param > 0 ? param : 10, 60f);
                        }
                    }
                    break;
                case "TestChaos":
                    if (Enum.IsDefined(typeof(ChaosEventType), (ChaosEventType)param))
                    {
                        var evType = (ChaosEventType)param;
                        var sampleItem = ItemPoolHelper.RollRandomEntry(master: target);
                        if (sampleItem == null)
                        {
                            sampleItem = ItemPoolHelper.CreateEntry(PickupCatalog.FindPickupIndex(RoR2Content.Items.Syringe.itemIndex), RouletteTierType.Common);
                        }
                        sampleItem.ChaosEvent = evType;
                        var def = ChaosEventManager.GetDefinition(evType);
                        sampleItem.ChaosPolarity = def != null ? def.Polarity : ChaosPolarity.None;
                        int paidCost = RouletteManager.Instance.GetCurrentCost();
                        ChaosEventManager.ExecuteEvent(target, evType, sampleItem, paidCost);
                    }
                    break;
            }
        }
    }
}
