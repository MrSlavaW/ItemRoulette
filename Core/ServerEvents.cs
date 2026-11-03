using System;
using System.Collections.Generic;
using System.Globalization;
using ItemRoulette.Configuration;
using ItemRoulette.Hooks;
using ItemRoulette.UI;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public enum ClientEventKind : byte
    {
        None = 0,
        FreeRoll,
        FreeLegendary,
        FreeQuality,
        FreeHighTier,
        FreeUncommon,
        FreeBoss,
        AurelioniteMax,
        LunarCoins,
        VoidChoice,
        FreeVoidPotential,
        SyncConfig,
        SyncVoidCurse,
        SetStageGravity,
        SetPlayerTierLuck,
        FreeEquipment,
        FreeQualityEquipment,
        FreeLunar,
        MandatoryLunarVoidSpin,
        SolitudeFreeRoll
    }

    /// <summary>
    /// Typed server -> client message (HLAPI). Clients can never send it to each other:
    /// it is only ever dispatched by the server and only handled on the client side.
    /// </summary>
    public class ClientEventMessage : MessageBase
    {
        public byte kind;
        public int count;
        public float value;
        public uint subjectNetId;
        public byte flags;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(kind);
            writer.WritePackedUInt32((uint)count);
            writer.Write(value);
            writer.WritePackedUInt32(subjectNetId);
            writer.Write(flags);
        }

        public override void Deserialize(NetworkReader reader)
        {
            kind = reader.ReadByte();
            count = (int)reader.ReadPackedUInt32();
            value = reader.ReadSingle();
            subjectNetId = reader.ReadPackedUInt32();
            flags = reader.ReadByte();
        }
    }

    /// <summary>
    /// Server -> client event channel. Replaces the old "[ItemRoulette:...]" chat tags for everything
    /// the server tells clients. Chat is used for human-readable text only.
    /// </summary>
    public static class ServerEvents
    {
        public const short ClientEventMsgType = 10078;

        public static void SendToAll(ClientEventMessage msg)
        {
            if (NetworkServer.active && msg != null)
            {
                NetworkServer.SendToAll(ClientEventMsgType, msg);
            }
        }

        public static void SendToTarget(CharacterMaster master, ClientEventMessage msg)
        {
            if (master == null || msg == null || !NetworkServer.active) return;
            var conn = master.playerCharacterMasterController?.networkUser?.connectionToClient;
            if (conn != null)
            {
                conn.Send(ClientEventMsgType, msg);
            }
            else if (NetworkClient.active && NetworkHelper.IsLocalMaster(master))
            {
                ApplyClientEvent(msg);
            }
        }

        private const string TagPrefix = "[ItemRoulette:";

        // Actions that the server executes locally (they used to be bounced through chat).
        private static readonly HashSet<string> ServerLocalActions = new HashSet<string>
        {
            "EnableStageArtifact", "TriggerCombatShrineX2", "GrantElementalAspect", "GrantAspectItem",
            "SpawnAlliedDrones", "SpawnHostileDrones"
        };

        // ---------------------------------------------------------------- client side

        public static void Init()
        {
            NotificationManager.Init();
            RoR2.Networking.NetworkManagerSystem.onStartClientGlobal += _ => EnsureClientHandlerRegistered();
            Run.onRunStartGlobal += _ => EnsureClientHandlerRegistered();
            Stage.onStageStartGlobal += _ => EnsureClientHandlerRegistered();
        }

        public static void EnsureClientHandlerRegistered()
        {
            try
            {
                NotificationManager.EnsureClientHandlerRegistered();
                foreach (var client in NetworkClient.allClients)
                {
                    if (client != null)
                    {
                        client.RegisterHandler(ClientEventMsgType, OnClientReceive);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception registering client event handler: {ex}");
            }
        }

        private static void OnClientReceive(NetworkMessage netMsg)
        {
            try
            {
                var msg = netMsg.ReadMessage<ClientEventMessage>();
                if (msg != null) ApplyClientEvent(msg);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling client event: {ex}");
            }
        }

        private static void Toast(string text, Color color, float duration)
        {
            RouletteUI.Instance?.ShowToast(text, color, duration);
        }

        private static void ApplyClientEvent(ClientEventMessage m)
        {
            var mgr = RouletteManager.Instance;
            switch ((ClientEventKind)m.kind)
            {
                case ClientEventKind.FreeRoll:
                    mgr.AddFreeRolls(m.count);
                    if (m.value >= 0)
                    {
                        var tier = (ItemRoulette.Compat.QualityTierCompat)(int)m.value;
                        for (int i = 0; i < m.count; i++) mgr.PendingGeneralMinQualityTiers.Enqueue(tier);
                    }
                    Toast(Localization.Format("[РУЛЕТКА] +{0} бесплатная крутка!", "[ROULETTE] +{0} free spin!", m.count), new Color(1f, 0.85f, 0.20f), 3.0f);
                    break;
                case ClientEventKind.FreeLegendary:
                    mgr.AddFreeLegendaryRolls(m.count);
                    if (m.value >= 0)
                    {
                        var tier = (ItemRoulette.Compat.QualityTierCompat)(int)m.value;
                        for (int i = 0; i < m.count; i++) mgr.PendingGeneralMinQualityTiers.Enqueue(tier);
                    }
                    Toast(Localization.Format("[ЛЕГЕНДАРНЫЙ КЕЙС] Получено {0} бесплатных красных круток!", "[LEGENDARY CASE] Received {0} free Red spins!", m.count), ItemPoolHelper.ColorLegendary, 4.0f);
                    break;
                case ClientEventKind.FreeQuality:
                    mgr.AddFreeQualityRolls(m.count);
                    if (m.value >= 0)
                    {
                        var tier = (ItemRoulette.Compat.QualityTierCompat)(int)m.value;
                        for (int i = 0; i < m.count; i++) mgr.PendingGeneralMinQualityTiers.Enqueue(tier);
                        string qName = ItemRoulette.Compat.QualityCompat.GetQualityName(tier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(ItemRoulette.Compat.QualityCompat.GetQualityColor(tier));
                        Toast(Localization.Format(
                            $"[СУНДУК КАЧЕСТВА] Получено {{0}} круток качества (<color=#{qColorHex}>{qName}</color>)!",
                            $"[QUALITY CHEST] Received {{0}} Quality spins (<color=#{qColorHex}>{tier}</color>)!",
                            m.count), new Color(0.22f, 0.94f, 0.49f), 4.5f);
                    }
                    else
                    {
                        Toast(Localization.Format("[СУНДУК КАЧЕСТВА] Получено {0} круток качества!", "[QUALITY CHEST] Received {0} Quality spins!", m.count), new Color(0.22f, 0.94f, 0.49f), 4.5f);
                    }
                    break;
                case ClientEventKind.FreeHighTier:
                    mgr.AddFreeHighTierRolls(m.count);
                    Toast(Localization.Format("[ЛУННЫЙ ПИЛОН] Получено {0} элитных круток (Красный/Бездна/Босс)!", "[LUNAR PYLON] Received {0} elite spins (Red/Void/Boss)!", m.count), ItemPoolHelper.ColorHighTierPylon, 5.0f);
                    break;
                case ClientEventKind.FreeUncommon:
                    mgr.AddFreeUncommonRolls(m.count);
                    Toast(Localization.Format("[НЕОБЫЧНЫЙ КЕЙС] Получено {0} зеленых круток!", "[UNCOMMON CASE] Received {0} Green spins!", m.count), ItemPoolHelper.ColorUncommon, 4.0f);
                    break;
                case ClientEventKind.FreeBoss:
                    mgr.AddFreeBossRolls(m.count);
                    if (m.value >= 0)
                    {
                        var tier = (ItemRoulette.Compat.QualityTierCompat)(int)m.value;
                        for (int i = 0; i < m.count; i++) mgr.PendingBossMinQualityTiers.Enqueue(tier);
                        string qName = ItemRoulette.Compat.QualityCompat.GetQualityName(tier);
                        string qColorHex = ColorUtility.ToHtmlStringRGB(ItemRoulette.Compat.QualityCompat.GetQualityColor(tier));
                        Toast(Localization.Format(
                            $"[НАГРАДА БОССА] +{{0}} крутка (качество: <color=#{qColorHex}>{qName}</color>)!",
                            $"[BOSS REWARD] +{{0}} spin (quality: <color=#{qColorHex}>{tier}</color>)!",
                            m.count), ItemPoolHelper.ColorBoss, 4.5f);
                    }
                    else
                    {
                        Toast(Localization.Format("[НАГРАДА БОССА] Получено {0} круток (85% Зеленый / 5% Красный / 10% Босс)!", "[BOSS REWARD] Received {0} spins (85% Green / 5% Red / 10% Boss)!", m.count), ItemPoolHelper.ColorBoss, 4.0f);
                    }
                    break;
                case ClientEventKind.FreeEquipment:
                    mgr.AddFreeEquipmentRolls(m.count);
                    Toast(Localization.Format("[СНАРЯЖЕНИЕ] Получено {0} круток снаряжения!", "[EQUIPMENT] Received {0} equipment spins!", m.count), ItemPoolHelper.ColorEquipment, 4.0f);
                    break;
                case ClientEventKind.FreeQualityEquipment:
                    mgr.AddFreeQualityEquipmentRolls(m.count);
                    Toast(Localization.Format("[СНАРЯЖЕНИЕ КАЧЕСТВА] Получено {0} круток снаряжения качества!", "[QUALITY EQUIPMENT] Received {0} quality equipment spins!", m.count), new Color(0.22f, 0.94f, 0.49f), 4.5f);
                    break;
                case ClientEventKind.FreeLunar:
                    mgr.AddFreeLunarRolls(m.count);
                    Toast(Localization.Format("[ЛУННАЯ КАПСУЛА] Получено {0} Лунных круток!", "[LUNAR POD] Received {0} Lunar spins!", m.count), ItemPoolHelper.ColorLunar, 4.0f);
                    break;
                case ClientEventKind.AurelioniteMax:
                    mgr.AddFreeLegendaryRolls(1);
                    mgr.AddFreeVoidPotentialRolls(2);
                    Toast(Localization.Get("[ОСКОЛОК АУРЕЛИОНИТА] Получено 3 элитные крутки: 1 Красная + 2 Потенциала!", "[AURELIONITE FRAGMENT] Received 3 elite spins: 1 Red + 2 Potential!"), new Color(1f, 0.84f, 0f), 5.0f);
                    break;
                case ClientEventKind.LunarCoins:
                    var localUser = LocalUserManager.GetFirstLocalUser();
                    if (localUser != null && localUser.currentNetworkUser != null)
                    {
                        localUser.currentNetworkUser.AwardLunarCoins((uint)m.count);
                        Toast(Localization.Format("[ЛУННЫЙ МЕШОК] +{0} Лунных монет!", "[LUNAR POUCH] +{0} Lunar Coins!", m.count), new Color(0.3f, 0.9f, 1f), 4.0f);
                    }
                    break;
                case ClientEventKind.VoidChoice:
                    mgr.AddPendingVoidRolls(m.count);
                    Toast(Localization.Format("[БЕЗДНА] Начислена крутка Бездны ({0})! Зажмите [{1}] для прокрутки.", "[VOID] Awarded Void spin ({0})! Hold [{1}] to spin.", m.count, ModConfig.VoidSpinHotkeyKey), ItemPoolHelper.ColorVoid, 4.5f);
                    break;
                case ClientEventKind.FreeVoidPotential:
                    mgr.AddFreeVoidPotentialRolls(m.count);
                    Toast(Localization.Format("[ПОТЕНЦИАЛ БЕЗДНЫ] Получено {0} бесплатных круток (80/15/5%)!", "[VOID POTENTIAL] Received {0} free spins (80/15/5%)!", m.count), ItemPoolHelper.ColorVoidPotential, 4.5f);
                    RouletteUI.Instance?.TryTriggerNextAutoSpin();
                    break;
                case ClientEventKind.SyncConfig:
                    bool casino = (m.flags & 1) != 0;
                    bool admin = (m.flags & 2) != 0;
                    ModConfig.SyncedEnableCasinoRoulette = casino;
                    ModConfig.SyncedEnableAdminPanel = admin;
                    ModConfig.SyncedEnableChaosSystem = (m.flags & 4) != 0;
                    ModConfig.SyncedEnableLifeBet = (m.flags & 8) != 0;
                    ModConfig.SyncedEnableVoidChests = (m.flags & 16) != 0;
                    if (!casino) CasinoRouletteUI.Instance?.Close();
                    if (!admin) AdminMenuUI.Instance?.SetOpen(false);
                    break;
                case ClientEventKind.SyncVoidCurse:
                    if (m.count == 0 && m.subjectNetId == 0)
                    {
                        mgr.ClearVoidCurseStacks();
                    }
                    else
                    {
                        mgr.SetVoidCurseStacksClient(m.subjectNetId, m.count);
                        PlayerUtil.FindMaster(m.subjectNetId.ToString(), null)?.GetBody()?.RecalculateStats();
                    }
                    break;
                case ClientEventKind.SetStageGravity:
                    mgr.ApplyStageLowGravity(m.value);
                    break;
                case ClientEventKind.SetPlayerTierLuck:
                    mgr.SetPlayerTierMultiplier(m.subjectNetId, m.value);
                    break;
                case ClientEventKind.MandatoryLunarVoidSpin:
                    RouletteUI.Instance?.QueueOrTriggerMandatorySpin(RouletteTierType.LunarVoid);
                    break;
                case ClientEventKind.SolitudeFreeRoll:
                    mgr.AddSolitudeFreeRolls(m.count, m.value);
                    Toast(Localization.Format("[ОДИНОЧЕСТВО] +{0} бесплатная крутка (редкость +x{1:F2})!", "[SOLITUDE] +{0} free spin (rarity +x{1:F2})!", m.count, m.value), new Color(0.35f, 0.85f, 1f), 4.0f);
                    break;
            }
        }

        // ---------------------------------------------------------------- server side

        /// <summary>
        /// Server-side broadcast. Any "[ItemRoulette:...]" tags in the text are converted to typed HLAPI messages
        /// (or executed locally for server-only actions) and removed; the remaining text goes to chat as plain text.
        /// </summary>
        public static void Broadcast(string text, string? initiator = null)
        {
            if (string.IsNullOrEmpty(text) || !NetworkServer.active) return;

            string remaining = text;
            while (true)
            {
                int start = remaining.IndexOf(TagPrefix, StringComparison.Ordinal);
                if (start < 0) break;
                int end = remaining.IndexOf(']', start);
                if (end < 0) break;

                string tagBody = remaining.Substring(start + 1, end - start - 1);
                try
                {
                    DispatchTag(tagBody);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception dispatching tag '{tagBody}': {ex}");
                }
                remaining = remaining.Remove(start, end - start + 1);
            }

            string visible = System.Text.RegularExpressions.Regex.Replace(remaining, "<.*?>", string.Empty).Trim();
            if (visible.Length == 0) return;

            try
            {
                NotificationManager.BroadcastNotification(remaining.Trim(), initiator: initiator);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception broadcasting notification: {ex}");
            }
        }

        /// <summary>
        /// Server-side plain broadcast. Removes ALL "[ItemRoulette:...]" tags without executing them.
        /// Used for text that originates from clients.
        /// </summary>
        public static void BroadcastPlain(string text, string? initiator = null)
        {
            if (string.IsNullOrEmpty(text) || !NetworkServer.active) return;
            string clean = StripTags(text);
            if (string.IsNullOrWhiteSpace(clean)) return;
            try
            {
                NotificationManager.BroadcastNotification(clean, initiator: initiator);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception broadcasting plain notification: {ex}");
            }
        }

        public static string StripTags(string message)
        {
            while (true)
            {
                int startIdx = message.IndexOf(TagPrefix, StringComparison.Ordinal);
                if (startIdx < 0) break;
                int endIdx = message.IndexOf(']', startIdx);
                if (endIdx < 0) break;
                message = message.Remove(startIdx, endIdx - startIdx + 1).Trim();
            }
            return message;
        }

        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        }

        private static void DispatchTag(string tagBody)
        {
            // tagBody: "ItemRoulette:Action:arg1:arg2..."
            string[] parts = tagBody.Split(':');
            if (parts.Length < 3) return;
            string action = parts[1];

            if (ServerLocalActions.Contains(action))
            {
                ChaosEventManager.ProcessLocalAction(action, parts);
                return;
            }

            var msg = new ClientEventMessage();
            string target = "All";

            switch (action)
            {
                case "FreeRollAll":
                    msg.kind = (byte)ClientEventKind.FreeRoll; msg.count = ParseInt(parts[2], 1);
                    break;
                case "FreeSolitudeRollAll":
                    msg.kind = (byte)ClientEventKind.SolitudeFreeRoll;
                    msg.count = ParseInt(parts[2], 1);
                    msg.value = parts.Length > 3 && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float rAll) ? rAll : 0.2f;
                    break;
                case "LunarCoinsAll":
                    msg.kind = (byte)ClientEventKind.LunarCoins; msg.count = ParseInt(parts[2], 10);
                    break;
                case "FreeRoll": msg.kind = (byte)ClientEventKind.FreeRoll; goto targeted;
                case "FreeLegendaryRoll": msg.kind = (byte)ClientEventKind.FreeLegendary; goto targeted;
                case "FreeQualityRoll":
                    msg.kind = (byte)ClientEventKind.FreeQuality;
                    target = parts[2];
                    msg.count = parts.Length > 3 ? ParseInt(parts[3], 1) : 1;
                    msg.value = parts.Length > 4 && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qTier) ? (float)qTier : -1f;
                    break;
                case "FreeHighTierRoll": msg.kind = (byte)ClientEventKind.FreeHighTier; goto targeted;
                case "FreeUncommonRoll": msg.kind = (byte)ClientEventKind.FreeUncommon; goto targeted;
                case "FreeBossRoll":
                    msg.kind = (byte)ClientEventKind.FreeBoss;
                    target = parts[2];
                    msg.count = parts.Length > 3 ? ParseInt(parts[3], 1) : 1;
                    msg.value = parts.Length > 4 && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bTier) ? (float)bTier : -1f;
                    break;
                case "FreeEquipmentRoll": msg.kind = (byte)ClientEventKind.FreeEquipment; goto targeted;
                case "FreeQualityEquipmentRoll": msg.kind = (byte)ClientEventKind.FreeQualityEquipment; goto targeted;
                case "FreeLunarRoll": msg.kind = (byte)ClientEventKind.FreeLunar; goto targeted;
                case "FreeSolitudeRoll":
                    msg.kind = (byte)ClientEventKind.SolitudeFreeRoll;
                    target = parts[2];
                    msg.count = parts.Length > 3 ? ParseInt(parts[3], 1) : 1;
                    msg.value = parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float rSol) ? rSol : 0.2f;
                    break;
                case "FreeAurelioniteMax": msg.kind = (byte)ClientEventKind.AurelioniteMax; goto targeted;
                case "VoidChoice": msg.kind = (byte)ClientEventKind.VoidChoice; goto targeted;
                case "FreeVoidPotentialRoll": msg.kind = (byte)ClientEventKind.FreeVoidPotential; goto targeted;
                targeted:
                    target = parts[2];
                    msg.count = parts.Length > 3 ? ParseInt(parts[3], 1) : 1;
                    msg.value = parts.Length > 4 && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tVal) ? (float)tVal : -1f;
                    break;
                case "SyncConfig":
                    if (parts.Length < 7) return;
                    msg.kind = (byte)ClientEventKind.SyncConfig;
                    for (int i = 0; i < 5; i++)
                    {
                        if (bool.TryParse(parts[2 + i], out bool b) && b) msg.flags |= (byte)(1 << i);
                    }
                    break;
                case "SyncVoidCurse":
                    if (parts.Length < 4) return;
                    msg.kind = (byte)ClientEventKind.SyncVoidCurse;
                    uint.TryParse(parts[2], out msg.subjectNetId);
                    msg.count = ParseInt(parts[3], 0);
                    break;
                case "SetStageGravity":
                    msg.kind = (byte)ClientEventKind.SetStageGravity;
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out msg.value);
                    break;
                case "SetPlayerTierLuck":
                    if (parts.Length < 4) return;
                    msg.kind = (byte)ClientEventKind.SetPlayerTierLuck;
                    uint.TryParse(parts[2], out msg.subjectNetId);
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out msg.value);
                    break;
                default:
                    Debug.LogWarning($"[ItemRoulette] Unknown server event tag '{action}'");
                    return;
            }

            if (msg.count < 0) msg.count = 0;

            if (string.Equals(target, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (PlayerCharacterMasterController.instances != null)
                {
                    foreach (var pcmc in PlayerCharacterMasterController.instances)
                    {
                        if (pcmc != null && pcmc.master != null)
                        {
                            RouletteManager.AddTargetedFreeRollServer(pcmc.master, (ClientEventKind)msg.kind, msg.count, msg.value);
                        }
                    }
                }
                NetworkServer.SendToAll(ClientEventMsgType, msg);
                return;
            }

            var master = PlayerUtil.FindMaster(target, null);
            if (master != null)
            {
                RouletteManager.AddTargetedFreeRollServer(master, (ClientEventKind)msg.kind, msg.count, msg.value);
            }

            var conn = master?.playerCharacterMasterController?.networkUser?.connectionToClient;
            if (conn != null)
            {
                conn.Send(ClientEventMsgType, msg);
            }
            else if (master != null && NetworkClient.active)
            {
                ApplyClientEvent(msg);
            }
            else
            {
                Debug.LogWarning($"[ItemRoulette] Could not resolve target '{target}' for event {action}");
            }
        }
    }
}
