using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public enum NotificationCategory : byte
    {
        All = 0,
        Casino = 1,
        Chaos = 2,
        Reward = 3,
        General = 4,
        Curse = 5,
        Roulette = 6
    }

    public class NotificationEntry
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public string Message { get; set; } = "";
        public string PlainText { get; set; } = "";
        public Color AccentColor { get; set; } = Color.white;
        public NotificationCategory Category { get; set; } = NotificationCategory.General;
        public int StageNumber { get; set; } = 1;
        public string StageFormatted { get; set; } = Localization.Get("Этап 1", "Stage 1");
        public float RunTimeSeconds { get; set; } = 0f;
        public string TimeFormatted { get; set; } = "00:00";
        public string Initiator { get; set; } = "";
        public DateTime RealTime { get; set; } = DateTime.Now;

        public NotificationEntry() { }

        public NotificationEntry(string message, Color accentColor, NotificationCategory category, int stage, float runTime, string? initiator = null)
        {
            Message = Regex.Replace(message, @"<sprite\b[^>]*>", string.Empty).Trim();
            PlainText = Regex.Replace(Message, "<.*?>", string.Empty).Trim();
            AccentColor = accentColor;
            Category = category;
            StageNumber = Mathf.Max(1, stage);
            StageFormatted = Localization.FormatToken("ITEMROULETTE_STAGE_FORMAT", StageNumber);
            RunTimeSeconds = Mathf.Max(0f, runTime);
            Initiator = NotificationManager.DetectInitiator(message, initiator);

            int minutes = Mathf.FloorToInt(RunTimeSeconds / 60f);
            int seconds = Mathf.FloorToInt(RunTimeSeconds % 60f);
            TimeFormatted = $"{minutes:D2}:{seconds:D2}";
            RealTime = DateTime.Now;
        }
    }

    public class ActiveToast
    {
        public NotificationEntry Entry { get; set; }
        public float TimeRemaining { get; set; }
        public float TotalDuration { get; set; }
        public float SlideOffset { get; set; }

        public ActiveToast(NotificationEntry entry, float duration = 5.0f)
        {
            Entry = entry;
            TotalDuration = duration;
            TimeRemaining = duration;
            SlideOffset = -40f; // Slide in from left
        }

        public float Alpha
        {
            get
            {
                if (TimeRemaining <= 0f) return 0f;
                float elapsed = TotalDuration - TimeRemaining;
                if (elapsed < 0.25f) return Mathf.Clamp01(elapsed / 0.25f);
                if (TimeRemaining < 0.50f) return Mathf.Clamp01(TimeRemaining / 0.50f);
                return 1f;
            }
        }
    }

    public class NotificationNetMessage : MessageBase
    {
        public string text = "";
        public float r = 1f;
        public float g = 1f;
        public float b = 1f;
        public byte category = 0;
        public int stage = 1;
        public float runTime = 0f;
        public string initiator = "";

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(text);
            writer.Write(r);
            writer.Write(g);
            writer.Write(b);
            writer.Write(category);
            writer.WritePackedUInt32((uint)stage);
            writer.Write(runTime);
            writer.Write(initiator ?? "");
        }

        public override void Deserialize(NetworkReader reader)
        {
            text = reader.ReadString();
            r = reader.ReadSingle();
            g = reader.ReadSingle();
            b = reader.ReadSingle();
            category = reader.ReadByte();
            stage = (int)reader.ReadPackedUInt32();
            runTime = reader.ReadSingle();
            initiator = reader.ReadString();
        }
    }

    /// <summary>
    /// Central manager for in-game notifications and run-long archive.
    /// Replaces public chat spam with smooth bottom-left achievement-style popups
    /// and a persistent scrollable event journal (Hotkey J).
    /// </summary>
    public static class NotificationManager
    {
        public const short NotificationMsgType = 10087;

        private static readonly List<NotificationEntry> _archive = new List<NotificationEntry>();
        private static readonly List<ActiveToast> _activeToasts = new List<ActiveToast>();
        private static readonly object _lock = new object();

        public static event Action<NotificationEntry>? OnNotificationAdded;
        public static event Action? OnArchiveChanged;

        private static bool _clientHandlerRegistered = false;

        public static void Init()
        {
            Run.onRunStartGlobal += _ => ClearArchive();
            Run.onRunDestroyGlobal += _ => ClearArchive();
            RoR2.Networking.NetworkManagerSystem.onStartClientGlobal += _ => EnsureClientHandlerRegistered();
            Run.onRunStartGlobal += _ => EnsureClientHandlerRegistered();
            Stage.onStageStartGlobal += _ => EnsureClientHandlerRegistered();
        }

        public static void EnsureClientHandlerRegistered()
        {
            if (_clientHandlerRegistered) return;
            try
            {
                var client = NetworkManager.singleton != null ? NetworkManager.singleton.client : null;
                if (client != null)
                {
                    client.RegisterHandler(NotificationMsgType, OnClientReceiveNotification);
                    _clientHandlerRegistered = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to register client NotificationMsg handler: {ex}");
            }
        }

        private static void OnClientReceiveNotification(NetworkMessage netMsg)
        {
            // If this machine is the host/server, BroadcastNotification already added it locally!
            if (NetworkServer.active) return;

            try
            {
                var msg = netMsg.ReadMessage<NotificationNetMessage>();
                if (msg != null && !string.IsNullOrWhiteSpace(msg.text))
                {
                    Color col = new Color(msg.r, msg.g, msg.b, 1f);
                    var cat = (NotificationCategory)msg.category;
                    var entry = new NotificationEntry(msg.text, col, cat, msg.stage, msg.runTime, msg.initiator);
                    AddNotificationLocal(entry);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling NotificationNetMessage: {ex}");
            }
        }

        /// <summary>
        /// Broadcasts notification to all clients (in MP) and adds to local archive & toast queue.
        /// </summary>
        public static void BroadcastNotification(string message, Color? customColor = null, NotificationCategory? customCategory = null, string? initiator = null)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            int stage = Run.instance != null ? Run.instance.stageClearCount + 1 : 1;
            float runTime = Run.instance != null ? Run.instance.GetRunStopwatch() : 0f;

            var (autoCat, autoColor) = DetectCategoryAndColor(message);
            NotificationCategory category = customCategory ?? autoCat;
            Color accentColor = customColor ?? autoColor;
            string finalInitiator = DetectInitiator(message, initiator);

            var entry = new NotificationEntry(message, accentColor, category, stage, runTime, finalInitiator);

            // In Multiplayer Server: broadcast to all clients
            if (NetworkServer.active)
            {
                try
                {
                    var netMsg = new NotificationNetMessage
                    {
                        text = entry.Message,
                        r = accentColor.r,
                        g = accentColor.g,
                        b = accentColor.b,
                        category = (byte)category,
                        stage = stage,
                        runTime = runTime,
                        initiator = finalInitiator
                    };
                    NetworkServer.SendToAll(NotificationMsgType, netMsg);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception broadcasting NotificationNetMessage: {ex}");
                }
            }

            // Always add locally on the executing machine (Host or SP)
            AddNotificationLocal(entry);
        }

        /// <summary>
        /// Adds a notification locally to the archive and toast queue.
        /// </summary>
        public static void AddNotification(string message, Color? customColor = null, NotificationCategory? customCategory = null, string? initiator = null)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            int stage = Run.instance != null ? Run.instance.stageClearCount + 1 : 1;
            float runTime = Run.instance != null ? Run.instance.GetRunStopwatch() : 0f;

            var (autoCat, autoColor) = DetectCategoryAndColor(message);
            NotificationCategory category = customCategory ?? autoCat;
            Color accentColor = customColor ?? autoColor;
            string finalInitiator = DetectInitiator(message, initiator);

            var entry = new NotificationEntry(message, accentColor, category, stage, runTime, finalInitiator);
            AddNotificationLocal(entry);
        }

        private static void AddNotificationLocal(NotificationEntry entry)
        {
            lock (_lock)
            {
                // De-duplicate: ignore if the exact same message was added within the last 1.0 second
                if (_archive.Count > 0)
                {
                    var last = _archive[_archive.Count - 1];
                    if (string.Equals(last.Message, entry.Message, StringComparison.Ordinal) &&
                        Mathf.Abs(last.RunTimeSeconds - entry.RunTimeSeconds) < 1.0f)
                    {
                        return; // Duplicate ignored
                    }
                }

                _archive.Add(entry);

                // Add active toast
                var toast = new ActiveToast(entry, 5.0f);
                _activeToasts.Add(toast);

                // If more than 4 toasts are stacked, accelerate oldest
                if (_activeToasts.Count > 4)
                {
                    int toAccelerate = _activeToasts.Count - 4;
                    for (int i = 0; i < toAccelerate; i++)
                    {
                        _activeToasts[i].TimeRemaining = Mathf.Min(_activeToasts[i].TimeRemaining, 0.4f);
                    }
                }
            }

            try
            {
                OnNotificationAdded?.Invoke(entry);
                OnArchiveChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in notification event handler: {ex}");
            }
        }

        public static void ClearArchive()
        {
            lock (_lock)
            {
                _archive.Clear();
                _activeToasts.Clear();
            }
            try
            {
                OnArchiveChanged?.Invoke();
            }
            catch { }
        }

        public static List<NotificationEntry> GetArchiveSnapshot()
        {
            lock (_lock)
            {
                return new List<NotificationEntry>(_archive);
            }
        }

        public static List<ActiveToast> GetActiveToastsSnapshot()
        {
            lock (_lock)
            {
                return new List<ActiveToast>(_activeToasts);
            }
        }

        public static void UpdateActiveToasts(float deltaTime)
        {
            lock (_lock)
            {
                for (int i = _activeToasts.Count - 1; i >= 0; i--)
                {
                    var toast = _activeToasts[i];
                    toast.TimeRemaining -= deltaTime;
                    toast.SlideOffset = Mathf.Lerp(toast.SlideOffset, 0f, deltaTime * 14f);

                    if (toast.TimeRemaining <= 0f)
                    {
                        _activeToasts.RemoveAt(i);
                    }
                }
            }
        }

        public static (NotificationCategory category, Color accentColor) DetectCategoryAndColor(string message)
        {
            if (string.IsNullOrEmpty(message)) return (NotificationCategory.General, Color.white);

            string upper = message.ToUpperInvariant();

            // 1. Chaos events (Good & Bad / Calamity / Fortune) - must precede Casino so [Рулетка | УДАЧА] is Chaos!
            if (upper.Contains("ХАОС") || upper.Contains("CHAOS") || 
                upper.Contains("УДАЧ") || upper.Contains("FORTUNE") || 
                upper.Contains("БЕДСТВИ") || upper.Contains("CALAMITY") || 
                upper.Contains("АНОМАЛИ") || upper.Contains("ANOMALY"))
            {
                Color col = new Color(0.30f, 0.88f, 0.50f); // Emerald
                if (upper.Contains("БЕДСТВИ") || upper.Contains("CALAMITY") || upper.Contains("ОТРИЦАТ") || 
                    upper.Contains("BAD") || upper.Contains("УНИЧТОЖ") || upper.Contains("КОРРОЗИ") || upper.Contains("CORROSION"))
                {
                    col = new Color(0.95f, 0.35f, 0.35f); // Red
                }
                return (NotificationCategory.Chaos, col);
            }

            // 2. Casino events (Only actual Casino Roulette table & bets, not generic roulette!)
            if (upper.Contains("КАЗИНО") || upper.Contains("CASINO") || 
                upper.Contains("СТАВК") || upper.Contains("BET") || 
                upper.Contains("ДЖЕКПОТ") || upper.Contains("JACKPOT") || 
                upper.Contains("ЗЕРО") || upper.Contains("ZERO"))
            {
                Color col = new Color(1f, 0.82f, 0.20f); // Gold
                if (upper.Contains("ЖИЗН") || upper.Contains("LIFE") || upper.Contains("СМЕРТ") || upper.Contains("DEAD"))
                {
                    col = new Color(1f, 0.35f, 0.40f); // Crimson
                }
                return (NotificationCategory.Casino, col);
            }

            // 3. Regular Roulette spins / items won / Chance Doll
            if (upper.Contains("[РУЛЕТКА]") || upper.Contains("[ROULETTE]") || 
                upper.Contains("КУКЛА УДАЧИ") || upper.Contains("CHANCE DOLL"))
            {
                Color col = new Color(0.35f, 0.85f, 1f); // Azure / Cyan
                var matchCol = Regex.Match(message, "<color=(#[0-9A-Fa-f]{6})>");
                if (matchCol.Success && ColorUtility.TryParseHtmlString(matchCol.Groups[1].Value, out Color parsedCol))
                {
                    col = parsedCol;
                }
                return (NotificationCategory.Roulette, col);
            }

            // 4. Reward events
            if (upper.Contains("НАГРАД") || upper.Contains("REWARD") || 
                upper.Contains("БОСС") || upper.Contains("BOSS") ||
                upper.Contains("ПИЛОН") || upper.Contains("PYLON") || 
                upper.Contains("БЕЗДН") || upper.Contains("VOID") ||
                upper.Contains("СНАРЯЖЕН") || upper.Contains("EQUIPMENT") || 
                upper.Contains("СУНДУК") || upper.Contains("CHEST") ||
                upper.Contains("ЛУНН") || upper.Contains("LUNAR") || 
                upper.Contains("КРУТК") || upper.Contains("SPIN"))
            {
                Color col = new Color(1f, 0.85f, 0.25f);
                if (upper.Contains("БЕЗДН") || upper.Contains("VOID")) col = new Color(0.75f, 0.45f, 1f);
                else if (upper.Contains("БОСС") || upper.Contains("BOSS")) col = new Color(1f, 0.88f, 0.25f);
                else if (upper.Contains("ЛУНН") || upper.Contains("LUNAR")) col = new Color(0.35f, 0.85f, 1f);
                return (NotificationCategory.Reward, col);
            }

            // 4. Fallback: check HTML color in message
            var match = Regex.Match(message, "<color=(#[0-9A-Fa-f]{6})>");
            if (match.Success && ColorUtility.TryParseHtmlString(match.Groups[1].Value, out Color parsedColor))
            {
                return (NotificationCategory.General, parsedColor);
            }

            return (NotificationCategory.General, new Color(0.85f, 0.88f, 0.95f));
        }

        public static string DetectInitiator(string message, string? explicitInitiator = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitInitiator))
            {
                return explicitInitiator.Trim();
            }

            if (string.IsNullOrEmpty(message))
            {
                return Localization.Get("ITEMROULETTE_INITIATOR_SERVER");
            }

            // 1. Check all connected player master controllers
            try
            {
                if (PlayerCharacterMasterController.instances != null)
                {
                    foreach (var pcmc in PlayerCharacterMasterController.instances)
                    {
                        if (pcmc == null) continue;
                        string? name = pcmc.networkUser != null ? pcmc.networkUser.userName : null;
                        if (string.IsNullOrWhiteSpace(name) && pcmc.master != null)
                        {
                            name = PlayerUtil.GetMasterDisplayName(pcmc.master);
                        }

                        if (!string.IsNullOrWhiteSpace(name) && name.Length >= 2)
                        {
                            if (message.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return name;
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Check local user name if single player
            try
            {
                var localMaster = PlayerUtil.GetLocalMaster();
                if (localMaster != null)
                {
                    string localName = PlayerUtil.GetMasterDisplayName(localMaster);
                    if (!string.IsNullOrWhiteSpace(localName) && message.IndexOf(localName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return localName;
                    }
                }
            }
            catch { }

            return Localization.Get("ITEMROULETTE_INITIATOR_SERVER");
        }
    }
}
