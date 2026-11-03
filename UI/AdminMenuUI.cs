using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.UI
{
    public class AdminMenuUI : MonoBehaviour
    {
        private static AdminMenuUI? _instance;
        public static AdminMenuUI? Instance => _instance;

        private bool _isOpen = false;
        public bool IsOpen => _isOpen;

        private Rect _windowRect = new Rect(50f, 40f, 1040f, 640f);
        private Vector2 _scrollPos = Vector2.zero;

        private GUIStyle? _winHeaderStyle;
        private GUIStyle? _sectionHeaderStyle;
        private GUIStyle? _itemRowStyle;
        private GUIStyle? _buttonStyleGood;
        private GUIStyle? _buttonStyleBad;
        private GUIStyle? _buttonStyleUtil;
        private GUIStyle? _statsStyle;

        private void Awake()
        {
            _instance = this;
        }


        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            ReleaseCursor();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void CaptureCursor() => UiCommon.AcquireCursor(this);

        private void ReleaseCursor() => UiCommon.ReleaseCursor(this);

        public void SetOpen(bool open)
        {
            if (_isOpen == open) return;
            _isOpen = open;
            if (_isOpen)
            {
                CaptureCursor();
            }
            else
            {
                ReleaseCursor();
                try
                {
                    var master = GetLocalMaster();
                    if (master != null && master.playerCharacterMasterController != null)
                    {
                        CasinoRouletteUI.SuppressPlayerButtons(master.playerCharacterMasterController);
                    }
                }
                catch { }
            }
        }

        private void Update()
        {
            if (Run.instance == null)
            {
                if (_isOpen) SetOpen(false);
                return;
            }

            // Block hotkeys and window controls while typing into chat, console, or input field
            if (GUIHelper.IsTypingOrInputBlocked())
            {
                return;
            }

            // Toggle admin menu on hotkey (F7 by default)
            if (Input.GetKeyDown(ModConfig.AdminMenuHotkeyKey))
            {
                if (!ModConfig.IsAdminPanelActive)
                {
                    RouletteUI.Instance?.ShowToast(Localization.Get("<color=#FF5555>[АДМИН] Панель отключена в настройках хоста.</color>", "<color=#FF5555>[ADMIN] Panel is disabled in host settings.</color>"), new Color(1f, 0.4f, 0.4f), 3.0f);
                    if (_isOpen) SetOpen(false);
                    return;
                }
                SetOpen(!_isOpen);
            }

            if (!ModConfig.IsAdminPanelActive && _isOpen)
            {
                SetOpen(false);
                return;
            }

            // Close on ESC if open
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                UiCommon.CloseOpenMenus();
            }
        }

        private void LateUpdate()
        {
            // Cursor lockState and visibility are handled cleanly via MPEventSystem.cursorOpenerCount
        }

        private void InitStyles()
        {
            if (_winHeaderStyle != null) return;

            _winHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.88f, 0.35f) }
            };

            _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = Color.white }
            };

            _itemRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            _buttonStyleGood = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 1f, 0.4f) }
            };

            _buttonStyleBad = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.35f, 0.35f) }
            };

            _buttonStyleUtil = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 1f) }
            };

            _statsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.8f, 0.85f, 0.95f) }
            };
        }

        private CharacterMaster? GetLocalMaster() => PlayerUtil.GetLocalMaster();

        private void OnGUI()
        {
            if (!_isOpen || Run.instance == null) return;

            bool prevEnabled = GUI.enabled;
            if (GUIHelper.IsTypingOrInputBlocked())
            {
                GUI.enabled = false;
            }

            try
            {
                InitStyles();

            // Center window if out of bounds or smaller than modern layout
            if (_windowRect.width > Screen.width || _windowRect.height > Screen.height || _windowRect.width < 960f)
            {
                _windowRect = new Rect(30f, 30f, Mathf.Min(1040f, Screen.width - 60f), Mathf.Min(640f, Screen.height - 60f));
            }

            GUI.color = Color.white;
            GUIHelper.DrawRect(_windowRect, new Color(0.06f, 0.08f, 0.12f, 0.98f));
            GUIHelper.DrawBorder(_windowRect, new Color(0.35f, 0.45f, 0.65f, 0.95f), 2);

            // 1. Header
            Rect headerRect = new Rect(_windowRect.x + 10f, _windowRect.y + 6f, _windowRect.width - 90f, 24f);
            GUI.Label(headerRect, Localization.Format("[ItemRoulette] ТЕСТОВОЕ АДМИН-МЕНЮ ХАОСА [{0}]", "[ItemRoulette] CHAOS ADMIN TEST MENU [{0}]", ModConfig.AdminMenuHotkey.Value), _winHeaderStyle);

            Rect closeBtnRect = new Rect(_windowRect.xMax - 75f, _windowRect.y + 6f, 65f, 22f);
            if (GUI.Button(closeBtnRect, Localization.Get("✕ Закрыть", "✕ Close"), _buttonStyleUtil))
            {
                SetOpen(false);
                return;
            }

            // 2. Toolbar Quick Actions
            var master = GetLocalMaster();
            Rect toolbarRect = new Rect(_windowRect.x + 12f, _windowRect.y + 34f, _windowRect.width - 24f, 28f);
            GUIHelper.DrawRect(toolbarRect, new Color(0.10f, 0.13f, 0.18f, 0.95f));
            GUIHelper.DrawBorder(toolbarRect, new Color(0.20f, 0.25f, 0.35f, 0.85f), 1);

            float tX = toolbarRect.x + 4f;
            float tY = toolbarRect.y + 3f;

            if (GUI.Button(new Rect(tX, tY, 82f, 22f), Localization.Get("+1,000 Золота", "+1,000 Gold"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "AddMoney", 1000);
                }
            }
            tX += 86f;

            if (GUI.Button(new Rect(tX, tY, 78f, 22f), Localization.Get("★ Казино", "★ Casino"), _buttonStyleGood))
            {
                _isOpen = false;
                CasinoRouletteUI.Instance?.Open();
            }
            tX += 82f;

            bool isAuto = RouletteUI.Instance != null && RouletteUI.Instance.IsAutoSpinActive;
            string autoBtnText = isAuto ? Localization.Get("Авто: ВКЛ", "Auto: ON") : Localization.Get("Авто: ВЫКЛ", "Auto: OFF");
            if (GUI.Button(new Rect(tX, tY, 74f, 22f), autoBtnText, isAuto ? _buttonStyleGood : _buttonStyleUtil))
            {
                if (RouletteUI.Instance != null)
                {
                    RouletteUI.Instance.ToggleAutoSpin();
                }
            }
            tX += 78f;

            if (GUI.Button(new Rect(tX, tY, 72f, 22f), Localization.Get("+5 Беспл.", "+5 Free"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "AddFreeRolls", 5);
                    RouletteManager.Instance.AddFreeRolls(5);
                    RouletteUI.Instance?.ShowToast(Localization.Get("+5 Бесплатных круток!", "+5 Free spins!"), new Color(0.4f, 0.95f, 0.5f), 2.5f);
                }
            }
            tX += 76f;

            if (GUI.Button(new Rect(tX, tY, 68f, 22f), Localization.Get("+1 Красн.", "+1 Red"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "AddFreeLegendary", 1);
                    RouletteManager.Instance.AddFreeLegendaryRolls(1);
                    RouletteUI.Instance?.ShowToast(Localization.Get("+1 Легендарная крутка!", "+1 Legendary spin!"), ItemPoolHelper.ColorLegendary, 2.5f);
                }
            }
            tX += 72f;

            if (GUI.Button(new Rect(tX, tY, 72f, 22f), Localization.Get("+1 Бездна", "+1 Void"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "AddPendingVoid", 1);
                    RouletteManager.Instance.AddPendingVoidRolls(1);
                    RouletteUI.Instance?.ShowToast(Localization.Get("+1 Крутка Бездны!", "+1 Void spin!"), ItemPoolHelper.ColorVoid, 2.5f);
                }
            }
            tX += 76f;

            if (GUI.Button(new Rect(tX, tY, 74f, 22f), Localization.Get("50/50: ВКЛ", "50/50: ON"), _buttonStyleBad))
            {
                if (master != null)
                {
                    SendAdminAction(master, "SetFiftyFifty", 1);
                    RouletteManager.Instance.FiftyFiftyActive = true;
                    RouletteUI.Instance?.ShowToast(Localization.Get("Режим 50/50 активен!", "50/50 mode active!"), new Color(1f, 0.5f, 0.2f), 2.5f);
                }
            }
            tX += 78f;

            if (GUI.Button(new Rect(tX, tY, 78f, 22f), Localization.Get("Сброс цены", "Reset Cost"), _buttonStyleUtil))
            {
                if (master != null)
                {
                    SendAdminAction(master, "ResetCost", 0);
                    RouletteManager.Instance.ChaosCostPenaltyMultiplier = 0f;
                    RouletteUI.Instance?.ShowToast(Localization.Get("Штраф цены рулетки сброшен!", "Cost penalty reset!"), new Color(0.4f, 0.95f, 0.5f), 2.5f);
                }
            }
            tX += 82f;

            if (GUI.Button(new Rect(tX, tY, 82f, 22f), Localization.Get("Сброс редк.", "Reset Luck"), _buttonStyleUtil))
            {
                if (master != null)
                {
                    SendAdminAction(master, "ResetLuck", 0);
                    RouletteManager.Instance.ClearPlayerTierMultipliers();
                    RouletteUI.Instance?.ShowToast(Localization.Get("Модификатор редкости сброшен (x1.00)!", "Rarity odds modifier reset (x1.00)!"), new Color(0.4f, 0.95f, 0.5f), 2.5f);
                }
            }
            tX += 86f;

            if (GUI.Button(new Rect(tX, tY, 80f, 22f), Localization.Get("+Спектр x5", "+Spectral x5"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "GrantSpectral", 5);
                }
            }
            tX += 84f;

            if (GUI.Button(new Rect(tX, tY, 76f, 22f), Localization.Get("+Врем x10", "+Temp x10"), _buttonStyleGood))
            {
                if (master != null)
                {
                    SendAdminAction(master, "GrantTemp", 10);
                }
            }
            tX += 80f;

            if (GUI.Button(new Rect(tX, tY, 88f, 22f), Localization.Get("☠ Цена жизни", "☠ Life Price"), _buttonStyleBad))
            {
                var (total, lvlPart, itemPart) = CasinoRouletteHelper.CalculateLifeBetPrice(master);
                if (CasinoRouletteUI.Instance != null)
                {
                    CasinoRouletteUI.Instance.ShowToast(Localization.Format("[Цена Жизни] Итого: ${0} (Ур*Этап: ${1} | Вещи: ${2})", "[Life Price] Total: ${0} (Lvl*Stage: ${1} | Items: ${2})", total, lvlPart, itemPart), new Color(1f, 0.4f, 0.4f), 5f);
                }
            }

            // 3. Columns Content: Good Events (Left) and Bad Events (Right)
            float colWidth = (_windowRect.width - 32f) / 2f;
            float colHeight = _windowRect.height - 135f;

            Rect leftColRect = new Rect(_windowRect.x + 12f, _windowRect.y + 68f, colWidth, colHeight);
            Rect rightColRect = new Rect(_windowRect.x + 16f + colWidth, _windowRect.y + 68f, colWidth, colHeight);

            DrawEventsColumn(leftColRect, ChaosPolarity.Good, master);
            DrawEventsColumn(rightColRect, ChaosPolarity.Bad, master);

            // 4. Footer Status Bar
            Rect footerRect = new Rect(_windowRect.x + 12f, _windowRect.yMax - 28f, _windowRect.width - 24f, 24f);
            GUIHelper.DrawRect(footerRect, new Color(0.09f, 0.11f, 0.16f, 0.95f));
            GUIHelper.DrawBorder(footerRect, new Color(0.25f, 0.30f, 0.40f, 0.85f), 1);

            var chances = ChaosEventManager.CalculateChances(master, RouletteManager.Instance.FiftyFiftyActive);
            int curCost = RouletteManager.Instance.GetCurrentCost();
            int freeRolls = RouletteManager.Instance.FreeRollsAvailable;
            uint money = master != null ? master.money : 0;

            float tierMult = master != null ? RouletteManager.Instance.GetPlayerTierMultiplier(master) : 1.0f;
            string tierLuckStr = "";
            if (Mathf.Abs(tierMult - 1.0f) > 0.01f)
            {
                tierLuckStr = tierMult > 1.0f 
                    ? Localization.Format("  |  <b>{0}:</b> <color=#4ADE80>x{1:F2}</color>", "  |  <b>{0}:</b> <color=#4ADE80>x{1:F2}</color>", Localization.Get("Редкость", "Rarity"), tierMult)
                    : Localization.Format("  |  <b>{0}:</b> <color=#F87171>x{1:F2}</color>", "  |  <b>{0}:</b> <color=#F87171>x{1:F2}</color>", Localization.Get("Редкость", "Rarity"), tierMult);
            }

            int dollCount = RouletteManager.GetChanceDollCount(master?.inventory);
            string dollStatus = "";
            if (dollCount > 0)
            {
                float dollChance = Mathf.Clamp(ModConfig.ChanceDollBaseChancePercent.Value + (dollCount - 1) * ModConfig.ChanceDollStackChancePercent.Value, 0f, 100f);
                dollStatus = Localization.Format("  |  <b>{0}:</b> <color=#38E54D>{1} шт. ({2:F0}%)</color>", "  |  <b>{0}:</b> <color=#38E54D>{1}x ({2:F0}%)</color>", Localization.Get("Кукла", "Doll"), dollCount, dollChance);
            }

            int solitudeCount = RouletteManager.GetSolitudeCount(master?.inventory);
            int solitudeRolls = RouletteManager.Instance.SolitudeRollsAvailable;
            string solitudeStatus = "";
            if (solitudeCount > 0 || solitudeRolls > 0)
            {
                float solitudeBonus = RouletteManager.Instance.SolitudeRarityMultiplierBonus;
                solitudeStatus = Localization.Format("  |  <b>{0}:</b> <color=#4ADEDE>{1} шт. ({2} кр., +x{3:F2})</color>", "  |  <b>{0}:</b> <color=#4ADEDE>{1}x ({2} spins, +x{3:F2})</color>", Localization.Get("Одиночество", "Solitude"), solitudeCount, solitudeRolls, solitudeBonus);
            }

            int voidRolls = RouletteManager.Instance.PendingVoidRollsAvailable;
            string voidStatus = voidRolls > 0 ? Localization.Format("<b>{0} [{1}]:</b> <color=#C084FC>{2}</color>  |  ", "<b>{0} [{1}]:</b> <color=#C084FC>{2}</color>  |  ", Localization.Get("Бездна", "Void"), ModConfig.VoidSpinHotkey.Value, voidRolls) : "";
            string statsStr = Localization.Format(
                "<b>{0}:</b> ${1}  |  <b>{2} [{3}]:</b> ${4}  |  <b>{5}:</b> {6}  |  {7}<b>{8}:</b> <color=#4ADE80>● {9} {10:F0}%</color> / <color=#C0C0C0>{11} {12:F0}%</color> / <color=#F87171>● {13} {14:F0}%</color>{15}{16}{17}",
                "<b>{0}:</b> ${1}  |  <b>{2} [{3}]:</b> ${4}  |  <b>{5}:</b> {6}  |  {7}<b>{8}:</b> <color=#4ADE80>● {9} {10:F0}%</color> / <color=#C0C0C0>{11} {12:F0}%</color> / <color=#F87171>● {13} {14:F0}%</color>{15}{16}{17}",
                Localization.Get("Золото", "Gold"), money,
                Localization.Get("След. крутка", "Next Spin"), ModConfig.Hotkey.Value, curCost,
                Localization.Get("Бесплатных", "Free"), freeRolls,
                voidStatus,
                Localization.Get("Шансы", "Odds"),
                Localization.Get("Удача", "Fortune"), chances.GoodPercent,
                Localization.Get("Нейтр.", "Neutral"), chances.NeutralPercent,
                Localization.Get("Бедствие", "Calamity"), chances.BadPercent,
                tierLuckStr, dollStatus, solitudeStatus);

            GUI.Label(new Rect(footerRect.x + 8f, footerRect.y + 3f, footerRect.width - 16f, 20f), statsStr, _statsStyle);

            // Draw cursor on top only when pointer is inside window bounds
            GUIHelper.DrawTopCursor(_windowRect);
        }
        finally
        {
            GUI.enabled = prevEnabled;
        }
    }

        private void DrawEventsColumn(Rect rect, ChaosPolarity polarity, CharacterMaster? master)
        {
            GUIHelper.DrawRect(rect, new Color(0.08f, 0.10f, 0.14f, 0.92f));
            Color bCol = polarity == ChaosPolarity.Good
                ? new Color(0.25f, 0.80f, 0.40f, 0.85f)
                : new Color(0.90f, 0.30f, 0.35f, 0.85f);
            GUIHelper.DrawBorder(rect, bCol, 1);

            List<ChaosEventType> eventsList;
            if (polarity == ChaosPolarity.Good)
            {
                eventsList = new List<ChaosEventType>
                {
                    ChaosEventType.DoubleItem,
                    ChaosEventType.TemporaryDuplicates,
                    ChaosEventType.SpectralDuplicates,
                    ChaosEventType.QualityCashback,
                    ChaosEventType.DoubleGold,
                    ChaosEventType.NextStageFreeRolls,
                    ChaosEventType.KillEnemies30,
                    ChaosEventType.AllyMirror,
                    ChaosEventType.Heal50Percent,
                    ChaosEventType.FriendlyClone,
                    ChaosEventType.ElementalAspectDrop,
                    ChaosEventType.TierOddsBoost,
                    ChaosEventType.AlliedDroneSquad
                };
                if (QualityCompat.IsQualityLoaded)
                {
                    eventsList.Add(ChaosEventType.ForgeAnomaly);
                }
            }
            else
            {
                eventsList = new List<ChaosEventType>
                {
                    ChaosEventType.LoseGold30,
                    ChaosEventType.CostIncrease10,
                    ChaosEventType.RandomLunarOrVoid,
                    ChaosEventType.TierDownSacrifice,
                    ChaosEventType.NextSpinFiftyFifty,
                    ChaosEventType.HallowedCurse,
                    ChaosEventType.DamageHp30,
                    ChaosEventType.ConvertHpToBarrier,
                    ChaosEventType.EnemyItemMimic,
                    ChaosEventType.EvilClone,
                    ChaosEventType.RandomStageArtifact,
                    ChaosEventType.CombatShrineX2,
                    ChaosEventType.LowGravityMoonX2,
                    ChaosEventType.TierOddsPenalty,
                    ChaosEventType.HostileDroneSquad
                };
                if (QualityCompat.IsQualityLoaded)
                {
                    eventsList.Add(ChaosEventType.VoidCorrosion);
                }
            }
            ChaosEventType[] events = eventsList.ToArray();
            int enabledCount = 0;
            for (int e = 0; e < events.Length; e++)
            {
                if (ChaosEventManager.IsEventAllowed(events[e])) enabledCount++;
            }

            string title = polarity == ChaosPolarity.Good
                ? Localization.Format("<color=#4ADE80><b>🟢 {0}/{1} ПОЛОЖИТЕЛЬНЫХ СОБЫТИЙ (УДАЧА)</b></color>", "<color=#4ADE80><b>🟢 {0}/{1} POSITIVE EVENTS (FORTUNE)</b></color>", enabledCount, events.Length)
                : Localization.Format("<color=#F87171><b>🔴 {0}/{1} НЕГАТИВНЫХ СОБЫТИЙ (БЕДСТВИЕ)</b></color>", "<color=#F87171><b>🔴 {0}/{1} NEGATIVE EVENTS (CALAMITY)</b></color>", enabledCount, events.Length);

            GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 20f), title, _sectionHeaderStyle);

            float rowY = rect.y + 26f;
            float rowHeight = (rect.height - 30f) / events.Length;

            for (int i = 0; i < events.Length; i++)
            {
                var evType = events[i];
                var def = ChaosEventManager.GetDefinition(evType);
                if (def == null) continue;

                Rect rowRect = new Rect(rect.x + 4f, rowY + (i * rowHeight), rect.width - 8f, rowHeight - 2f);
                GUIHelper.DrawRect(rowRect, (i % 2 == 0) ? new Color(0.10f, 0.12f, 0.17f, 0.85f) : new Color(0.12f, 0.14f, 0.20f, 0.85f));

                // Title label
                string num = $"{i + 1}. ";
                bool isAllowed = ChaosEventManager.IsEventAllowed(evType);
                string statusTag = isAllowed ? "" : (Localization.IsRussian ? " <color=#6B7280>[ВЫКЛ]</color>" : " <color=#6B7280>[OFF]</color>");
                string labelText = $"{num}<b>{def.Title}</b>{statusTag}";
                GUI.Label(new Rect(rowRect.x + 6f, rowRect.y + 4f, rowRect.width - 150f, rowHeight - 6f), labelText, _itemRowStyle);

                // Button [Тест] - Instantly apply
                Rect testBtn = new Rect(rowRect.xMax - 144f, rowRect.y + 3f, 68f, rowHeight - 6f);
                var btnStyle = polarity == ChaosPolarity.Good ? _buttonStyleGood : _buttonStyleBad;

                if (GUI.Button(testBtn, Localization.Get("Тест", "Test"), btnStyle))
                {
                    ExecuteInstantTest(master, evType);
                }

                // Button [Спин] - Trigger roulette spin with forced event
                Rect spinBtn = new Rect(rowRect.xMax - 72f, rowRect.y + 3f, 68f, rowHeight - 6f);
                if (GUI.Button(spinBtn, Localization.Get("Спин", "Spin"), _buttonStyleUtil))
                {
                    SetOpen(false);
                    if (RouletteUI.Instance != null)
                    {
                        RouletteUI.Instance.TriggerSpinAction(evType);
                    }
                }
            }
        }

        private void SendAdminAction(CharacterMaster master, string actionKind, int param = 0)
        {
            if (master == null) return;

            if (UnityEngine.Networking.NetworkServer.active)
            {
                ItemRoulette.Hooks.DropSuppressionHooks.ExecuteAdminActionServer(master, actionKind, param);
            }
            else
            {
                string txId = Guid.NewGuid().ToString("N");
                string safeName = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(master));
                NetworkHelper.SendToServer($"[ItemRoulette:AdminAction:{txId}:{master.netId.Value}:{safeName}:{actionKind}:{param}]");
            }
        }

        private void ExecuteInstantTest(CharacterMaster? master, ChaosEventType evType)
        {
            if (master == null) return;

            SendAdminAction(master, "TestChaos", (int)evType);

            var def = ChaosEventManager.GetDisplayDefinition(evType) ?? ChaosEventManager.GetDefinition(evType);
            if (RouletteUI.Instance != null && def != null)
            {
                string polarityTag = def.Polarity == ChaosPolarity.Good ? Localization.Get("УДАЧА", "FORTUNE") : Localization.Get("БЕДСТВИЕ", "CALAMITY");
                RouletteUI.Instance.ShowToast($"[{polarityTag}] <b>{def.Title}</b>: {def.Description}", def.Color, 3.5f);
            }
        }
    }
}
