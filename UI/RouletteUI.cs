using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using ItemRoulette.Hooks;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace ItemRoulette.UI
{
    public class RouletteUI : MonoBehaviour
    {
        private static RouletteUI? _instance;
        public static RouletteUI? Instance => _instance;

        private enum BannerState
        {
            Hidden,
            Warning,
            Spinning,
            Finished,
            VoidDecision
        }

        public static uint GetLocalPlayerLunarCoins()
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null)
            {
                if (localUser.userProfile != null)
                {
                    return localUser.userProfile.coins;
                }
                if (localUser.currentNetworkUser != null)
                {
                    return localUser.currentNetworkUser.lunarCoins;
                }
            }
            return 0;
        }

        private BannerState _state = BannerState.Hidden;
        public bool IsSpinningOrBannerOpen => _state == BannerState.Spinning || _state == BannerState.Finished || _state == BannerState.VoidDecision;
        private List<RouletteItemEntry> _reelItems = new List<RouletteItemEntry>();
        private RouletteItemEntry? _wonItem;

        private const int ReelSize = 130;
        private const int WinningSlotIndex = 65;

        private const float CardWidth = 94f;
        private const float CardHeight = 94f;
        private const float CardSpacing = 8f;
        private const float SlotWidth = CardWidth + CardSpacing; // 102f

        private float _currentScrollX = 0f;
        private float _targetScrollX = 0f;
        private float _spinTimer = 0f;
        private int _lastTickedSlot = -1;

        // Auto-fade timer after winning or warning
        private float _visibilityTimer = 0f;
        private string _warningMessage = "";

        private int _paidCostForCurrentSpin = 0;
        private float _toastTimer = 0f;
        private string _toastMessage = "";
        private Color _toastColor = Color.white;

        // Void choice hold timer, queue, and current spin tier
        private float _voidHoldTimer = 0f;
        private bool _voidHoldTriggered = false;
        private bool _queuedVoidSpin = false;
        private RouletteTierType? _queuedMandatorySpinTier = null;
        private RouletteTierType? _spinTierType = null;
        private bool _isCurrentSpinFree = false;
        private int _voidRerollsUsed = 0;

        // Auto-Spin state & hold timer (Persistent mode)
        public bool IsAutoSpinActive { get; private set; } = false;
        private float _autoSpinHoldTimer = 0f;
        private bool _autoSpinHoldTriggered = false;
        private float _autoSpinCheckTimer = 0f;


        // Styles
        private GUIStyle? _topLabelStyle;
        private GUIStyle? _cardTitleStyle;
        private GUIStyle? _warningStyle;
        private GUIStyle? _questionMarkStyle;
        private GUIStyle? _toastStyle;
        private GUIStyle? _actionBtnStyle;

        private bool _spinRequestInFlight = false;
        private bool _voidDecisionInFlight = false;
        private float _voidDecisionFlightTimer = 0f;
        private bool _currentSpinRequiresVoidDecision = false;
        private int _voidItemBaselineCount = -1;
        private EquipmentIndex _voidItemBaselineEquipment = EquipmentIndex.None;

        private void Awake()
        {
            _instance = this;
            ServerSpinService.OnSpinResultReceived += HandleSpinResultReceived;
            ServerSpinService.OnVoidDecisionResultReceived += HandleVoidDecisionResultReceived;
            ResetUI();
        }

        private void OnDisable()
        {
            UiCommon.RestoreSinglePlayerPause();
        }

        private void OnDestroy()
        {
            UiCommon.RestoreSinglePlayerPause();
            ServerSpinService.OnSpinResultReceived -= HandleSpinResultReceived;
            ServerSpinService.OnVoidDecisionResultReceived -= HandleVoidDecisionResultReceived;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void ResetUI(bool preserveAutoSpin = false)
        {
            if ((_state == BannerState.Spinning || _state == BannerState.VoidDecision) && _wonItem != null)
            {
                HandleSceneExitTransition();
            }

            bool previousAuto = preserveAutoSpin && IsAutoSpinActive;
            _state = BannerState.Hidden;
            _spinTimer = 0f;
            _visibilityTimer = 0f;
            _currentScrollX = 0f;
            _targetScrollX = 0f;
            _wonItem = null;
            _reelItems.Clear();
            _lastTickedSlot = -1;
            _warningMessage = "";
            _toastTimer = 0f;
            _toastMessage = "";
            _voidHoldTimer = 0f;
            _voidHoldTriggered = false;
            _queuedVoidSpin = false;
            _queuedMandatorySpinTier = null;
            _voidRerollsUsed = 0;
            _currentSpinRequiresVoidDecision = false;
            _voidDecisionInFlight = false;
            _voidDecisionFlightTimer = 0f;
            _voidItemBaselineCount = -1;
            _voidItemBaselineEquipment = EquipmentIndex.None;
            _autoSpinHoldTimer = 0f;
            _autoSpinHoldTriggered = false;
            _autoSpinCheckTimer = 0f;
            _spinTierType = null;
            _isCurrentSpinFree = false;
            IsAutoSpinActive = previousAuto;
        }

        public void HandleSceneExitTransition()
        {
            if (_state == BannerState.VoidDecision && _wonItem != null)
            {
                bool isEquipment = _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                if (isEquipment)
                {
                    RouletteManager.Instance.AddFreeEquipmentRolls(1);
                }
                else
                {
                    // Player did not make a choice before stage transition:
                    // Save the pending void drop, refund 1 void roll, do not award item, do not deduct lunar coins!
                    RouletteManager.Instance.StoredPendingVoidDrop = _wonItem;
                    RouletteManager.Instance.AddPendingVoidRolls(1);
                }
                _state = BannerState.Hidden;
                _wonItem = null;
                _reelItems.Clear();
                return;
            }

            if (_state == BannerState.Spinning && _wonItem != null)
            {
                var master = GetLocalMaster();
                bool isVoidItem = _wonItem.IsVoidItem;
                bool alreadyOwns = isVoidItem && RouletteManager.HasItemInInventory(master, _wonItem.PickupDef?.itemIndex ?? ItemIndex.None);
                bool shouldPrompt = isVoidItem && !alreadyOwns && ModConfig.EnableVoidCorruptChoice.Value;

                if (shouldPrompt)
                {
                    RouletteManager.Instance.StoredPendingVoidDrop = _wonItem;
                    RouletteManager.Instance.AddPendingVoidRolls(1);
                    _state = BannerState.Hidden;
                    _wonItem = null;
                    _reelItems.Clear();
                    return;
                }

                FinishSpin();
            }
        }

        public void ForceFinishSpinImmediate()
        {
            HandleSceneExitTransition();
        }

        public void ToggleAutoSpin()
        {
            SetAutoSpin(!IsAutoSpinActive);
        }

        public void SetAutoSpin(bool active)
        {
            if (IsAutoSpinActive == active) return;
            IsAutoSpinActive = active;

            if (IsAutoSpinActive)
            {
                ShowToast(Localization.Format("[АВТО-РУЛЕТКА] Постоянный режим ВКЛЮЧЁН (Пауза: {0:F1}с)", "[AUTO-SPIN] Persistent mode ON (Delay: {0:F1}s)", ModConfig.AutoSpinDelay.Value), new Color(0.3f, 0.95f, 0.5f), 3.0f);
                PlaySound("Play_UI_menuClick");

                if (_state == BannerState.Hidden || _state == BannerState.Warning)
                {
                    TryTriggerNextAutoSpin();
                }
                else if (_state == BannerState.Finished)
                {
                    _visibilityTimer = Mathf.Min(_visibilityTimer, Mathf.Max(0.1f, ModConfig.AutoSpinDelay.Value));
                }
            }
            else
            {
                ShowToast(Localization.Get("[АВТО-РУЛЕТКА] Постоянный режим ВЫКЛЮЧЕН", "[AUTO-SPIN] Persistent mode OFF"), new Color(0.95f, 0.45f, 0.35f), 2.5f);
                PlaySound("Play_UI_menuClick");
            }
        }

        public void TryTriggerNextAutoSpin()
        {
            if (Run.instance == null) return;

            if (!ShouldDisplayUI())
            {
                return;
            }

            if (CasinoRouletteUI.IsOpen || CasinoRouletteUI.IsSpinning || CasinoRouletteUI.IsBannerActive)
            {
                return;
            }

            // If currently waiting for player's Void decision, NEVER auto-spin!
            if (_state == BannerState.VoidDecision)
            {
                return;
            }

            // 0. Prioritize mandatory spin (e.g. Anomalous Mutation) immediately
            if (_queuedMandatorySpinTier.HasValue && _state != BannerState.Spinning && !_spinRequestInFlight)
            {
                var tier = _queuedMandatorySpinTier.Value;
                _queuedMandatorySpinTier = null;
                TriggerSpinAction(explicitTier: tier);
                return;
            }

            // 1. Prioritize queued Void spin if present!
            if (_queuedVoidSpin && RouletteManager.Instance.PendingVoidRollsAvailable > 0)
            {
                if (_state != BannerState.Spinning)
                {
                    _queuedVoidSpin = false;
                    TriggerSpinAction(explicitTier: RouletteTierType.Void);
                }
                return;
            }

            if (!IsAutoSpinActive) return;

            var master = GetLocalMaster();
            if (master == null) return;

            // 2. Free or paid roll auto-spin
            bool isFree = RouletteManager.Instance.IsNextRollFree;
            bool canAfford = isFree || RouletteManager.Instance.CanAfford(master);

            if (!canAfford)
            {
                // Persistent mode: do NOT disable auto-spin! It remains active and waits for gold or free rolls.
                return;
            }

            if (_state != BannerState.Spinning)
            {
                TriggerSpinAction();
            }
        }

        private bool IsPauseActive()
        {
            if (PauseManager.isPaused) return true;

            // In multiplayer or when pause screen GameObject is actively visible in hierarchy
            if (PauseScreenController.instancesList != null)
            {
                for (int i = 0; i < PauseScreenController.instancesList.Count; i++)
                {
                    var p = PauseScreenController.instancesList[i];
                    if (p != null && p.gameObject != null && p.gameObject.activeInHierarchy)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldDisplayUI()
        {
            if (Run.instance == null) return false;

            // Hide completely in Pause Menu (ESC)
            if (IsPauseActive())
            {
                return false;
            }

            // Hide if game HUD is not active or disabled
            if (HUD.readOnlyInstanceList == null || HUD.readOnlyInstanceList.Count == 0) return false;
            var hud = HUD.readOnlyInstanceList[0];
            if (hud == null || !hud.gameObject.activeInHierarchy)
            {
                return false;
            }

            var canvas = hud.GetComponent<Canvas>() ?? hud.GetComponentInParent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                return false;
            }

            if (hud.mainContainer != null && !hud.mainContainer.activeInHierarchy)
            {
                return false;
            }

            if (hud.cinematicPanel != null && hud.cinematicPanel.activeInHierarchy)
            {
                return false;
            }

            if (hud.scoreboardPanel != null && hud.scoreboardPanel.activeInHierarchy)
            {
                return false;
            }

            // Hide if game over screen is up
            if (GameOverController.instance != null) return false;

            // Hide if player character master is missing
            var master = GetLocalMaster();
            if (master == null || master.inventory == null) return false;

            // In singleplayer, hide if player has no body
            // In multiplayer, allow access even if dead (drone mode, spectating, or waiting for revive)
            bool isMultiplayer = RoR2Application.isInMultiPlayer || (NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 1);
            if (!isMultiplayer && !master.hasBody)
            {
                return false;
            }

            // Strictly tie to in-game inventory display unless dead in multiplayer
            if (master.hasBody)
            {
                if (hud.itemInventoryDisplay == null || !hud.itemInventoryDisplay.gameObject.activeInHierarchy)
                {
                    return false;
                }
            }

            return true;
        }

        public static Rect GetInventoryFieldRect(HUD hud)
        {
            float targetW = Mathf.Clamp(Screen.width * 0.65f, 750f, 1300f);
            float targetH = 126f;
            float targetX = (Screen.width - targetW) / 2f;
            float targetY = 8f;

            if (hud != null && hud.itemInventoryDisplay != null)
            {
                Canvas? canvas = hud.GetComponent<Canvas>() ?? hud.GetComponentInParent<Canvas>();
                Camera? cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

                List<RectTransform> candidates = new List<RectTransform>();
                if (hud.itemInventoryDisplay.transform.parent is RectTransform parentRt)
                {
                    candidates.Add(parentRt);
                }
                if (hud.itemInventoryDisplay.transform is RectTransform invRt)
                {
                    candidates.Add(invRt);
                }
                if (hud.itemInventoryDisplay.hideInventoryImage != null && hud.itemInventoryDisplay.hideInventoryImage.transform is RectTransform hideRt)
                {
                    candidates.Add(hideRt);
                }

                float minScreenX = float.MaxValue;
                float maxScreenX = float.MinValue;
                float minScreenY = float.MaxValue;
                float maxScreenY = float.MinValue;
                Vector3[] corners = new Vector3[4];
                int found = 0;

                for (int c = 0; c < candidates.Count; c++)
                {
                    var rt = candidates[c];
                    if (rt == null) continue;
                    rt.GetWorldCorners(corners);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                        if (sp.x < minScreenX) minScreenX = sp.x;
                        if (sp.x > maxScreenX) maxScreenX = sp.x;
                        if (sp.y < minScreenY) minScreenY = sp.y;
                        if (sp.y > maxScreenY) maxScreenY = sp.y;
                    }
                    found++;
                }

                if (found > 0 && maxScreenX > minScreenX + 250f)
                {
                    float measuredW = maxScreenX - minScreenX;
                    float measuredH = maxScreenY - minScreenY;

                    // Expand horizontally to align with the visual bracket corner sprites [ and ]
                    // In RoR2, bracket corner sprites extend ~24px beyond the RectTransform edges
                    float padX = 24f;
                    targetX = Mathf.Max(0f, minScreenX - padX);
                    targetW = measuredW + (padX * 2f);

                    float guiY = Screen.height - maxScreenY;
                    float padY = 4f;
                    targetY = Mathf.Max(0f, guiY - padY);
                    targetH = Mathf.Max(126f, measuredH + (padY * 2f));
                }
            }

            return new Rect(targetX, targetY, targetW, targetH);
        }

        private string GetTruncatedText(string text, GUIStyle style, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (style.CalcSize(new GUIContent(text)).x <= maxWidth)
            {
                return text;
            }

            string ellipsis = "...";
            float ellipsisWidth = style.CalcSize(new GUIContent(ellipsis)).x;
            if (ellipsisWidth >= maxWidth) return ellipsis;

            for (int len = text.Length - 1; len > 0; len--)
            {
                string candidate = text.Substring(0, len).TrimEnd() + ellipsis;
                if (style.CalcSize(new GUIContent(candidate)).x <= maxWidth)
                {
                    return candidate;
                }
            }

            return ellipsis;
        }

        private void Update()
        {
            UiCommon.UpdateSinglePlayerPause();
            try
            {
                if (Run.instance == null)
                {
                    if (_state != BannerState.Hidden) ResetUI();
                    return;
                }

                RouletteManager.Instance.CheckAndHandleStageTransition();

                // Do not process or spin when paused / ESC menu
                if (!ShouldDisplayUI())
                {
                    if (_state == BannerState.Warning)
                    {
                        _state = BannerState.Hidden;
                    }
                    return;
                }

                // Toast countdown
                if (_toastTimer > 0f)
                {
                    _toastTimer -= Time.unscaledDeltaTime;
                }

                // Watchdog: If void decision has been in-flight for too long without server reply, unlock it
                if (_voidDecisionInFlight)
                {
                    _voidDecisionFlightTimer += Time.unscaledDeltaTime;
                    if (_voidDecisionFlightTimer > 3.5f)
                    {
                        _voidDecisionInFlight = false;
                        _voidDecisionFlightTimer = 0f;
                    }
                }
                else
                {
                    _voidDecisionFlightTimer = 0f;
                }

                // Inventory verification: if the won Void/Lunar item was already granted to inventory, automatically close the roulette!
                if ((_state == BannerState.VoidDecision || _state == BannerState.Finished) && _wonItem != null)
                {
                    var localMaster = GetLocalMaster();
                    if (localMaster != null && localMaster.inventory != null && _wonItem.PickupDef != null)
                    {
                        bool itemGranted = false;
                        if (_wonItem.PickupDef.itemIndex != ItemIndex.None && _voidItemBaselineCount >= 0)
                        {
                            int currentCount = localMaster.inventory.GetItemCountEffective(_wonItem.PickupDef.itemIndex);
                            if (currentCount > _voidItemBaselineCount)
                            {
                                itemGranted = true;
                            }
                        }
                        else if (_wonItem.PickupDef.equipmentIndex != EquipmentIndex.None)
                        {
                            if (PlayerUtil.GetStoredEquipmentIndex(localMaster.inventory) == _wonItem.PickupDef.equipmentIndex &&
                                _voidItemBaselineEquipment != _wonItem.PickupDef.equipmentIndex)
                            {
                                itemGranted = true;
                            }
                        }

                        if (itemGranted)
                        {
                            FinishVoidDecision();
                            return;
                        }
                    }
                }

                // If chat, console, or input field is open / typing, do not process hotkeys or hold-spins
                if (GUIHelper.IsTypingOrInputBlocked())
                {
                    _autoSpinHoldTimer = 0f;
                    _autoSpinHoldTriggered = false;
                    _voidHoldTimer = 0f;
                    _voidHoldTriggered = false;
                }
                else
                {
                    // If waiting for Void Corruption or Equipment decision: handle [B] (take), [R] (reroll), and [C] (decline/drop)
                    if (_state == BannerState.VoidDecision)
                    {
                        bool isEquipment = _wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                        if (Input.GetKeyDown(ModConfig.HotkeyKey))
                        {
                            AcceptVoidItem();
                            return;
                        }
                        if (!isEquipment && ModConfig.EnableVoidReroll.Value && Input.GetKeyDown(ModConfig.VoidRerollHotkeyKey))
                        {
                            RerollVoidItem();
                            return;
                        }
                        if (Input.GetKeyDown(ModConfig.VoidSpinHotkeyKey))
                        {
                            DeclineVoidItem();
                            return;
                        }
                        return; // Yield all other inputs while waiting for player's choice
                    }

                    // Persistent Auto-spin toggle: HOLD to toggle ON / OFF (default KeyCode.V)
                    KeyCode autoHotkey = ModConfig.AutoSpinHotkeyKey;
                    if (!CasinoRouletteUI.IsOpen && !CasinoRouletteUI.IsSpinning)
                    {
                        if (Input.GetKey(autoHotkey))
                        {
                            _autoSpinHoldTimer += Time.unscaledDeltaTime;
                            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);

                            if (_autoSpinHoldTimer >= holdThreshold && !_autoSpinHoldTriggered)
                            {
                                _autoSpinHoldTriggered = true;
                                ToggleAutoSpin();
                            }
                        }
                        else
                        {
                            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
                            if (!_autoSpinHoldTriggered && _autoSpinHoldTimer > 0.05f && _autoSpinHoldTimer < holdThreshold)
                            {
                                ShowToast(Localization.Format("[АВТО-РУЛЕТКА] Для переключения удерживайте [{0}] {1:F1}с", "[AUTO-SPIN] Hold [{0}] {1:F1}s to toggle", autoHotkey, holdThreshold), new Color(0.95f, 0.85f, 0.35f), 2.0f);
                                PlaySound("Play_UI_button_hover");
                            }
                            _autoSpinHoldTimer = 0f;
                            _autoSpinHoldTriggered = false;
                        }
                    }
                    else
                    {
                        _autoSpinHoldTimer = 0f;
                        _autoSpinHoldTriggered = false;
                    }

                    // Dedicated Void Spin hotkey: HOLD to spin or queue Void roll
                    KeyCode voidHotkey = ModConfig.VoidSpinHotkeyKey;
                    bool hasVoidChoice = RouletteManager.Instance.PendingVoidRollsAvailable > 0;

                    if (hasVoidChoice)
                    {
                        if (Input.GetKey(voidHotkey) && !CasinoRouletteUI.IsOpen && !CasinoRouletteUI.IsSpinning)
                        {
                            _voidHoldTimer += Time.unscaledDeltaTime;
                            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);

                            if (_voidHoldTimer >= holdThreshold && !_voidHoldTriggered)
                            {
                                _voidHoldTriggered = true;
                                PlaySound("Play_UI_button_hover");

                                if (_state == BannerState.Spinning || _state == BannerState.Finished)
                                {
                                    _queuedVoidSpin = true;
                                    ShowToast(Localization.Get("<color=#C084FC>★ [БЕЗДНА] Рулетка Бездны поставлена в очередь (следующий спин)! ★</color>", "<color=#C084FC>★ [VOID] Void spin queued for next spin! ★</color>"), ItemPoolHelper.ColorVoid, 2.5f);
                                }
                                else
                                {
                                    TriggerSpinAction(explicitTier: RouletteTierType.Void);
                                }
                            }
                        }
                        else
                        {
                            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
                            if (!_voidHoldTriggered && _voidHoldTimer > 0.05f && _voidHoldTimer < holdThreshold)
                            {
                                ShowToast(Localization.Format("[БЕЗДНА] Для крутки удерживайте [{0}] {1:F1}с", "[VOID] Hold [{0}] {1:F1}s to spin Void roll", voidHotkey, holdThreshold), ItemPoolHelper.ColorVoid, 1.5f);
                                PlaySound("Play_UI_menuClick");
                            }
                            _voidHoldTimer = 0f;
                            _voidHoldTriggered = false;
                        }
                    }
                    else
                    {
                        _voidHoldTimer = 0f;
                        _voidHoldTriggered = false;
                        if (Input.GetKeyDown(voidHotkey) && !CasinoRouletteUI.IsOpen && !CasinoRouletteUI.IsSpinning)
                        {
                            ShowToast(Localization.Get("[БЕЗДНА] Нет доступных круток Бездны (откройте Колыбель Бездны)", "[VOID] No Void spins available (open a Void Cradle)"), ItemPoolHelper.ColorVoid, 2.5f);
                        }
                    }

                    // Standard / Legendary Spin hotkey (Hotkey, default B)
                    KeyCode hotkey = ModConfig.HotkeyKey;
                    if (Input.GetKeyDown(hotkey) && !CasinoRouletteUI.IsOpen && !CasinoRouletteUI.IsSpinning)
                    {
                        TriggerSpinAction();
                    }
                }

                // Spinning animation physics
                if (_state == BannerState.Spinning)
                {
                    _spinTimer += Time.unscaledDeltaTime;
                    float duration = Mathf.Max(1.0f, ModConfig.SpinDuration.Value);
                    float progress = Mathf.Clamp01(_spinTimer / duration);

                    // Quintic ease-out deceleration
                    float ease = 1f - Mathf.Pow(1f - progress, 4.4f);
                    _currentScrollX = Mathf.Lerp(0f, _targetScrollX, ease);

                    // Ticking sound as cards cross center needle
                    var hud = HUD.readOnlyInstanceList.Count > 0 ? HUD.readOnlyInstanceList[0] : null;
                    Rect boxRect = GetInventoryFieldRect(hud!);
                    float vpWidth = boxRect.width - 16f;

                    float needleScreenX = _currentScrollX + (vpWidth / 2f);
                    int crossedSlot = Mathf.FloorToInt(needleScreenX / SlotWidth);
                    if (crossedSlot != _lastTickedSlot)
                    {
                        _lastTickedSlot = crossedSlot;
                        PlaySound("Play_UI_button_hover");
                    }

                    if (progress >= 1f)
                    {
                        if (_spinRequestInFlight)
                        {
                            _spinTimer = duration * 0.85f;
                        }
                        else
                        {
                            FinishSpin();
                        }
                    }
                }
                else if (_state == BannerState.Finished || _state == BannerState.Warning)
                {
                    // Count down visibility timer to auto-hide
                    _visibilityTimer -= Time.unscaledDeltaTime;
                    if (_visibilityTimer <= 0f)
                    {
                        _state = BannerState.Hidden;
                        if (_queuedMandatorySpinTier.HasValue)
                        {
                            var tier = _queuedMandatorySpinTier.Value;
                            _queuedMandatorySpinTier = null;
                            TriggerSpinAction(explicitTier: tier);
                        }
                        else
                        {
                            TryTriggerNextAutoSpin();
                        }
                    }
                }
                else if (_state == BannerState.Hidden)
                {
                    if (_queuedVoidSpin && RouletteManager.Instance.PendingVoidRollsAvailable > 0)
                    {
                        TryTriggerNextAutoSpin();
                    }
                    else if (IsAutoSpinActive)
                    {
                        // Periodic poll: trigger next auto spin as soon as gold or free rolls arrive
                        _autoSpinCheckTimer -= Time.unscaledDeltaTime;
                        if (_autoSpinCheckTimer <= 0f)
                        {
                            _autoSpinCheckTimer = 0.25f;
                            TryTriggerNextAutoSpin();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in RouletteUI.Update: {ex}");
            }
        }

        public void QueueOrTriggerMandatorySpin(RouletteTierType tier)
        {
            if (_state == BannerState.Spinning || _state == BannerState.VoidDecision || _state == BannerState.Finished || _spinRequestInFlight)
            {
                _queuedMandatorySpinTier = tier;
                return;
            }

            _queuedMandatorySpinTier = null;
            TriggerSpinAction(explicitTier: tier);
        }

        public CharacterMaster? GetLocalMaster() => PlayerUtil.GetLocalMaster();

        public void TriggerSpinAction(ChaosEventType forcedEvent = ChaosEventType.None, RouletteTierType? explicitTier = null)
        {
            if (_state == BannerState.Spinning || _state == BannerState.VoidDecision || _spinRequestInFlight) return;

            _voidRerollsUsed = 0;
            _currentSpinRequiresVoidDecision = false;
            _voidDecisionInFlight = false;

            var master = GetLocalMaster();
            if (master == null) return;

            bool isVoidSpin = (explicitTier == RouletteTierType.Void);
            bool isLunarVoid = (explicitTier == RouletteTierType.LunarVoid);
            byte tierKind = isLunarVoid ? (byte)2 : (isVoidSpin ? (byte)1 : (byte)0);

            if (isLunarVoid)
            {
                _paidCostForCurrentSpin = 0;
                _isCurrentSpinFree = true;
                _spinTierType = RouletteTierType.LunarVoid;
            }
            else if (isVoidSpin)
            {
                if (RouletteManager.Instance.PendingVoidRollsAvailable <= 0)
                {
                    _state = BannerState.Warning;
                    _warningMessage = Localization.Get("ITEMROULETTE_UI_NO_VOID_SPINS", "Нет доступных круток Бездны!", "No Void spins available!");
                    _visibilityTimer = 2.0f;
                    PlaySound("Play_UI_insufficient_funds");
                    return;
                }

                _paidCostForCurrentSpin = 0;
                _isCurrentSpinFree = true;
                _spinTierType = RouletteTierType.Void;
            }
            else
            {
                int cost = RouletteManager.Instance.GetCurrentCost();
                if (forcedEvent == ChaosEventType.None && !RouletteManager.Instance.CanAfford(master))
                {
                    _state = BannerState.Warning;
                    _warningMessage = Localization.FormatToken("ITEMROULETTE_UI_NOT_ENOUGH_GOLD", cost);
                    _visibilityTimer = 2.0f;
                    PlaySound("Play_UI_insufficient_funds");
                    return;
                }

                bool isFree = forcedEvent != ChaosEventType.None || RouletteManager.Instance.IsNextRollFree;
                _isCurrentSpinFree = isFree;
                _paidCostForCurrentSpin = isFree ? 0 : cost;
                _spinTierType = explicitTier ?? (forcedEvent != ChaosEventType.None ? null : (isFree ? RouletteManager.Instance.ActiveFreeRollTier : null));
            }

            _spinRequestInFlight = true;

            // Generate anticipation tape with dummy items
            _reelItems.Clear();
            var chances = ChaosEventManager.CalculateChances(master, RouletteManager.Instance.FiftyFiftyActive);
            bool isQualitySpin = false;
            QualityTierCompat activeQualityTier = QualityTierCompat.None;

            if (_spinTierType == RouletteTierType.BossReward && RouletteManager.Instance.PendingBossMinQualityTiers.Count > 0)
            {
                activeQualityTier = RouletteManager.Instance.PendingBossMinQualityTiers.Peek();
                isQualitySpin = true;
            }
            else if ((_spinTierType == null || _spinTierType == RouletteTierType.Common) && RouletteManager.Instance.FreeQualityRollsAvailable > 0)
            {
                isQualitySpin = true;
                if (RouletteManager.Instance.PendingGeneralMinQualityTiers.Count > 0)
                {
                    activeQualityTier = RouletteManager.Instance.PendingGeneralMinQualityTiers.Peek();
                }
            }
            else if (_spinTierType == RouletteTierType.QualityEquipment)
            {
                isQualitySpin = true;
            }

            for (int i = 0; i < ReelSize; i++)
            {
                RouletteItemEntry? entry = null;
                if (isLunarVoid)
                {
                    entry = CustomRoulettePool.CreateLunarVoidPool().Roll(master: master, chances: chances);
                }
                else
                {
                    entry = ItemPoolHelper.RollRandomEntry(chances: chances, forcedTier: _spinTierType, master: master, guaranteeQuality: isQualitySpin, minQualityTier: activeQualityTier);
                }
                if (entry != null) _reelItems.Add(entry);
            }

            if (forcedEvent != ChaosEventType.None && _reelItems.Count > WinningSlotIndex)
            {
                _reelItems[WinningSlotIndex].ChaosEvent = forcedEvent;
                var def = ChaosEventManager.GetDefinition(forcedEvent);
                _reelItems[WinningSlotIndex].ChaosPolarity = def != null ? def.Polarity : ChaosPolarity.None;
            }

            var hud = HUD.readOnlyInstanceList.Count > 0 ? HUD.readOnlyInstanceList[0] : null;
            Rect boxRect = GetInventoryFieldRect(hud!);
            float vpWidth = boxRect.width - 16f;

            float centerOffset = (WinningSlotIndex * SlotWidth) + (CardWidth / 2f);
            float jitter = UnityEngine.Random.Range(-CardWidth * 0.35f, CardWidth * 0.35f);
            _targetScrollX = centerOffset + jitter - (vpWidth / 2f);

            _currentScrollX = 0f;
            _spinTimer = 0f;
            _lastTickedSlot = -1;
            _state = BannerState.Spinning;

            PlaySound("Play_UI_menuClick");

            // Dispatch authoritative request to server
            ServerSpinService.SendSpinRequest(tierKind, forcedEvent);
        }

        private void HandleSpinResultReceived(SpinResultMessage msg)
        {
            _spinRequestInFlight = false;

            if (!msg.success)
            {
                _state = BannerState.Warning;
                if (msg.rejectReason == 1)
                {
                    _warningMessage = Localization.FormatToken("ITEMROULETTE_UI_NOT_ENOUGH_GOLD", msg.currentCost);
                }
                else if (msg.rejectReason == 2)
                {
                    _warningMessage = Localization.Get("ITEMROULETTE_UI_NO_VOID_SPINS", "Нет доступных круток Бездны!", "No Void spins available!");
                }
                else if (msg.rejectReason == 3)
                {
                    _warningMessage = Localization.Get("ITEMROULETTE_UI_CHOICE_PENDING", "Сделайте выбор предыдущего предмета!", "Make a choice for the previous item!");
                }
                else if (msg.rejectReason == 4)
                {
                    _warningMessage = Localization.Get("ITEMROULETTE_UI_EMPTY_POOL", "Пул предметов пуст!", "Item pool is empty!");
                }
                else
                {
                    _warningMessage = Localization.Get("ITEMROULETTE_UI_SPIN_ERROR", "Ошибка прокрутки!", "Spin error!");
                }
                _visibilityTimer = 2.0f;
                PlaySound("Play_UI_insufficient_funds");
                Debug.LogWarning($"[ItemRoulette] Spin failed on client. rejectReason={msg.rejectReason}, currentCost={msg.currentCost}");
                return;
            }

            // Reconstruct won item from authoritative server result
            // Do NOT overwrite _spinTierType with msg.tierType: msg.tierType is the won item's tier,
            // NOT the spin mode. Overwriting it spoils the roll in the header while still spinning and shows "(TIER - FREE)" on paid spins.
            _wonItem = ItemPoolHelper.CreateEntry(new PickupIndex(msg.pickupIndex), (RouletteTierType)msg.tierType);
            if (_wonItem != null)
            {
                _wonItem.ChaosEvent = (ChaosEventType)msg.chaosEvent;
                _wonItem.ChaosPolarity = (ChaosPolarity)msg.chaosPolarity;
                _wonItem.UpgradedByChanceDoll = msg.upgradedByChanceDoll;
                _wonItem.OriginalNameBeforeDollUpgrade = msg.oldItemName;
            }

            _paidCostForCurrentSpin = msg.paidCost;
            _isCurrentSpinFree = msg.paidCost == 0;
            _currentSpinRequiresVoidDecision = msg.requiresVoidDecision;

            if (_spinTierType == RouletteTierType.BossReward && RouletteManager.Instance.PendingBossMinQualityTiers.Count > 0)
            {
                RouletteManager.Instance.PendingBossMinQualityTiers.Dequeue();
            }
            else if ((_spinTierType == null || _spinTierType == RouletteTierType.Common) && RouletteManager.Instance.PendingGeneralMinQualityTiers.Count > 0)
            {
                RouletteManager.Instance.PendingGeneralMinQualityTiers.Dequeue();
            }

            // Place won item in slot 65
            if (WinningSlotIndex >= 0 && WinningSlotIndex < _reelItems.Count && _wonItem != null)
            {
                _reelItems[WinningSlotIndex] = _wonItem;
            }
        }

        private void FinishSpin()
        {
            _currentScrollX = _targetScrollX;

            var master = GetLocalMaster();
            if (master != null && _wonItem != null)
            {
                if (_currentSpinRequiresVoidDecision)
                {
                    _state = BannerState.VoidDecision;
                    _visibilityTimer = float.MaxValue;
                    _voidDecisionInFlight = false;
                    _voidDecisionFlightTimer = 0f;

                    if (master.inventory != null && _wonItem.PickupDef != null)
                    {
                        if (_wonItem.PickupDef.itemIndex != ItemIndex.None)
                        {
                            _voidItemBaselineCount = master.inventory.GetItemCountEffective(_wonItem.PickupDef.itemIndex);
                        }
                        else if (_wonItem.PickupDef.equipmentIndex != EquipmentIndex.None)
                        {
                            _voidItemBaselineEquipment = PlayerUtil.GetStoredEquipmentIndex(master.inventory);
                        }
                    }

                    // Cursor acquisition removed per user request: keyboard navigation remains active
                    PlaySound("Play_UI_shrine_gamble_win");
                    return;
                }

                _voidRerollsUsed = 0;
            _currentSpinRequiresVoidDecision = false;
            _voidDecisionInFlight = false;
                _state = BannerState.Finished;
                _visibilityTimer = IsAutoSpinActive ? Mathf.Max(0.1f, ModConfig.AutoSpinDelay.Value) : Mathf.Max(0.5f, ModConfig.PostSpinDisplayDuration.Value);

                if (_wonItem.ChaosEvent != ChaosEventType.None)
                {
                    var def = ChaosEventManager.GetDisplayDefinition(_wonItem.ChaosEvent);
                    if (def != null)
                    {
                        string polarityTag = def.Polarity == ChaosPolarity.Good ? Localization.Get("УДАЧА", "FORTUNE") : Localization.Get("БЕДСТВИЕ", "CALAMITY");
                        ShowToast($"[{polarityTag}] <b>{def.Title}</b>: {def.Description}", def.Color, 3.5f);
                    }
                }

                if (_wonItem.UpgradedByChanceDoll)
                {
                    string toast = Localization.Format("<color=#38E54D>★ [Кукла удачи] {0} ➔ <b>{1}</b> ({2})! ★</color>", "<color=#38E54D>★ [Chance Doll] {0} ➔ <b>{1}</b> ({2})! ★</color>", _wonItem.OriginalNameBeforeDollUpgrade ?? "", _wonItem.LocalizedName, _wonItem.TierLabel);
                    ShowToast(toast, new Color(0.2f, 1.0f, 0.4f), 4.0f);
                }

                PlaySound("Play_UI_shrine_gamble_win");

                // Request authoritative server grant now that the tape landed on the card!
                ServerSpinService.SendClaimAwardRequest();

                try
                {
                    var pIndex = _wonItem.PickupIndex;
                    if (pIndex != PickupIndex.none)
                    {
                        CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pIndex, isRental = false });
                    }
                }
                catch { }
            }
            else
            {
                _state = BannerState.Finished;
                _visibilityTimer = IsAutoSpinActive ? Mathf.Max(0.1f, ModConfig.AutoSpinDelay.Value) : Mathf.Max(0.5f, ModConfig.PostSpinDisplayDuration.Value);
            }
        }

        public void AcceptVoidItem()
        {
            if (_voidDecisionInFlight || _state != BannerState.VoidDecision || _wonItem == null) return;
            _voidDecisionInFlight = true;
            _voidDecisionFlightTimer = 0f;
            ServerSpinService.SendVoidDecision(0, _wonItem.PickupIndex.value, (byte)_wonItem.TierType);
        }

        public void RerollVoidItem()
        {
            if (_voidDecisionInFlight || _state != BannerState.VoidDecision || _wonItem == null) return;
            if (!ModConfig.EnableVoidReroll.Value) return;

            int maxRerolls = Mathf.Max(1, ModConfig.VoidRerollMaxPerItem.Value);
            if (_voidRerollsUsed >= maxRerolls)
            {
                bool isLunar = _wonItem != null && _wonItem.IsLunarItem;
                string tag = isLunar ? "[ЛУНА]" : "[БЕЗДНА]";
                string tagEn = isLunar ? "[LUNAR]" : "[VOID]";
                ShowToast(Localization.Format($"<color=#FF6B6B>{tag} Достигнут лимит переролов этого предмета ({{0}}/{{1}})!</color>", $"<color=#FF6B6B>{tagEn} Reroll limit reached for this item ({{0}}/{{1}})!</color>", _voidRerollsUsed, maxRerolls), new Color(1f, 0.4f, 0.4f), 3.5f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            uint coins = GetLocalPlayerLunarCoins();
            int cost = Mathf.Max(0, ModConfig.VoidRerollLunarCoinsCost.Value);

            if (coins < (uint)cost)
            {
                bool isLunar = _wonItem != null && _wonItem.IsLunarItem;
                string tag = isLunar ? "[ЛУНА]" : "[БЕЗДНА]";
                string tagEn = isLunar ? "[LUNAR]" : "[VOID]";
                ShowToast(Localization.Format($"<color=#FF6B6B>{tag} Недостаточно Лунных монет для перерола ({{0}}/{{1}} Лун. монет)!</color>", $"<color=#FF6B6B>{tagEn} Not enough Lunar Coins for reroll ({{0}}/{{1}} Lunar Coins)!</color>", coins, cost), new Color(1f, 0.4f, 0.4f), 3.5f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            _voidDecisionInFlight = true;
            _voidDecisionFlightTimer = 0f;
            ServerSpinService.SendVoidDecision(1, _wonItem.PickupIndex.value, (byte)_wonItem.TierType);
        }

        public void DeclineVoidItem()
        {
            if (_voidDecisionInFlight || _state != BannerState.VoidDecision || _wonItem == null) return;
            _voidDecisionInFlight = true;
            _voidDecisionFlightTimer = 0f;
            ServerSpinService.SendVoidDecision(2, _wonItem.PickupIndex.value, (byte)_wonItem.TierType);
        }

        private void HandleVoidDecisionResultReceived(VoidDecisionResultMessage msg)
        {
            _voidDecisionInFlight = false;
            _voidDecisionFlightTimer = 0f;

            if (!msg.success)
            {
                ShowToast(msg.message, new Color(1f, 0.4f, 0.4f), 3.5f);
                PlaySound("Play_UI_insufficient_funds");
                if (msg.message.Contains("Нет активного решения") || msg.message.Contains("No active Void decision"))
                {
                    FinishVoidDecision();
                }
                return;
            }

            if (msg.decision == 0) // Accept
            {
                bool isEquipment = _wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                bool isLunar = _wonItem != null && _wonItem.IsLunarItem;
                string wonName = _wonItem?.LocalizedName ?? "";

                FinishVoidDecision();

                if (isEquipment)
                {
                    ShowToast(Localization.Format("<color=#E58238>[СНАРЯЖЕНИЕ] Экипировано: {0}!</color>", "<color=#E58238>[EQUIPMENT] Equipped: {0}!</color>", wonName), ItemPoolHelper.ColorEquipment, 3.5f);
                }
                else if (isLunar)
                {
                    ShowToast(Localization.Format("<color=#38BDF8>[ЛУНА] Принята ересь: {0}!</color>", "<color=#38BDF8>[LUNAR] Embraced heresy: {0}!</color>", wonName), ItemPoolHelper.ColorLunar, 3.5f);
                }
                else
                {
                    ShowToast(Localization.Format("<color=#BF58FF>[БЕЗДНА] Принято заражение: {0}!</color>", "<color=#BF58FF>[VOID] Embraced corruption: {0}!</color>", wonName), ItemPoolHelper.ColorVoid, 3.5f);
                }
                PlaySound("Play_UI_shrine_gamble_win");
            }
            else if (msg.decision == 1) // Reroll
            {
                _voidRerollsUsed++;
                bool wasLunar = _wonItem != null && _wonItem.IsLunarItem;
                _wonItem = ItemPoolHelper.CreateEntry(new PickupIndex(msg.newPickupIndex), wasLunar ? RouletteTierType.Lunar : RouletteTierType.Void);
                if (WinningSlotIndex >= 0 && WinningSlotIndex < _reelItems.Count && _wonItem != null)
                {
                    _reelItems[WinningSlotIndex] = _wonItem;
                }

                var master = GetLocalMaster();
                if (master != null && master.inventory != null && _wonItem != null && _wonItem.PickupDef != null)
                {
                    if (_wonItem.PickupDef.itemIndex != ItemIndex.None)
                    {
                        _voidItemBaselineCount = master.inventory.GetItemCountEffective(_wonItem.PickupDef.itemIndex);
                    }
                    else if (_wonItem.PickupDef.equipmentIndex != EquipmentIndex.None)
                    {
                        _voidItemBaselineEquipment = PlayerUtil.GetStoredEquipmentIndex(master.inventory);
                    }
                }

                _currentScrollX = 0f;
                _spinTimer = 0f;
                _lastTickedSlot = -1;
                _state = BannerState.Spinning;

                PlaySound("Play_obj_lunarCoin_consume");
                if (wasLunar)
                {
                    ShowToast(Localization.Format("<color=#38BDF8>★ [ЛУНА] Перерол предмета! ★</color>", "<color=#38BDF8>★ [LUNAR] Item Reroll! ★</color>"), ItemPoolHelper.ColorLunar, 3.0f);
                }
                else
                {
                    ShowToast(Localization.Format("<color=#38E54D>★ [БЕЗДНА] Перерол предмета! ★</color>", "<color=#38E54D>★ [VOID] Item Reroll! ★</color>"), new Color(0.35f, 0.90f, 1f), 3.0f);
                }
            }
            else if (msg.decision == 2) // Decline
            {
                bool isEquipment = _wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                bool isLunar = _wonItem != null && _wonItem.IsLunarItem;
                FinishVoidDecision();
                if (isEquipment)
                {
                    ShowToast(Localization.Get("<color=#E58238>[СНАРЯЖЕНИЕ] Снаряжение сброшено на пол.</color>", "<color=#E58238>[EQUIPMENT] Dropped equipment on the floor.</color>"), ItemPoolHelper.ColorEquipment, 3.0f);
                }
                else if (isLunar)
                {
                    ShowToast(Localization.Get("<color=#9E9E9E>[ЛУНА] Вы отказались от Лунного предмета.</color>", "<color=#9E9E9E>[LUNAR] Declined Lunar item.</color>"), Color.gray, 3.0f);
                }
                else
                {
                    ShowToast(Localization.Get("<color=#9E9E9E>[БЕЗДНА] Вы отказались от предмета Бездны.</color>", "<color=#9E9E9E>[VOID] Declined Void item.</color>"), Color.gray, 3.0f);
                }
            }
        }

        private void FinishVoidDecision()
        {
            _voidRerollsUsed = 0;
            _currentSpinRequiresVoidDecision = false;
            _voidDecisionInFlight = false;
            _voidDecisionFlightTimer = 0f;
            _voidItemBaselineCount = -1;
            _voidItemBaselineEquipment = EquipmentIndex.None;
            _wonItem = null;
            _state = BannerState.Hidden;
            _visibilityTimer = 0f;

            if (_queuedMandatorySpinTier.HasValue)
            {
                var tier = _queuedMandatorySpinTier.Value;
                _queuedMandatorySpinTier = null;
                TriggerSpinAction(explicitTier: tier);
                return;
            }

            if (IsAutoSpinActive)
            {
                _autoSpinCheckTimer = Mathf.Max(0.2f, ModConfig.AutoSpinDelay.Value);
            }
        }

        public void ShowToast(string message, Color color, float duration = 3.5f)
        {
            _toastMessage = message;
            _toastColor = color;
            _toastTimer = duration;
        }

        private void PlaySound(string soundString)
        {
            try
            {
                if (RoR2Application.instance != null)
                {
                    Util.PlaySound(soundString, RoR2Application.instance.gameObject);
                }
            }
            catch { }
        }

        private void InitStyles()
        {
            if (_topLabelStyle != null) return;

            _topLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.95f) }
            };

            _cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
                richText = true,
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.35f, 0.35f) }
            };

            _questionMarkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white }
            };

            _actionBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        private static string GenerateTextProgressBar(float progress, int segments = 8)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(progress * segments), 0, segments);
            return "[" + new string('■', filled) + new string('□', segments - filled) + "]";
        }

        private void DrawToastNotification(Rect boxRect)
        {
            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
            if ((_autoSpinHoldTimer > 0f && _autoSpinHoldTimer < holdThreshold) || (_voidHoldTimer > 0f && _voidHoldTimer < holdThreshold))
            {
                return; // Yield position to the smooth filling HoldProgressBar
            }
            if (_toastTimer <= 0f || string.IsNullOrEmpty(_toastMessage)) return;

            float alpha = Mathf.Clamp01(_toastTimer / 0.5f);
            float toastWidth = Mathf.Min(boxRect.width, 560f);
            float toastHeight = 24f;
            float toastX = boxRect.x + (boxRect.width - toastWidth) / 2f;
            float toastY = (_state == BannerState.VoidDecision) 
                ? boxRect.yMax + 40f 
                : boxRect.yMax + 6f;
            Rect toastRect = new Rect(toastX, toastY, toastWidth, toastHeight);

            Color bg = new Color(0.06f, 0.08f, 0.12f, 0.92f * alpha);
            Color border = new Color(_toastColor.r, _toastColor.g, _toastColor.b, 0.95f * alpha);
            Color textCol = new Color(_toastColor.r, _toastColor.g, _toastColor.b, alpha);

            GUIHelper.DrawRect(toastRect, bg);
            GUIHelper.DrawBorder(toastRect, border, 1);

            _toastStyle!.normal.textColor = textCol;
            GUI.Label(toastRect, _toastMessage, _toastStyle);
        }

        private void DrawHoldProgressBar(Rect boxRect)
        {
            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
            bool isAutoHolding = _autoSpinHoldTimer > 0f && _autoSpinHoldTimer < holdThreshold;
            bool isVoidHolding = _voidHoldTimer > 0f && _voidHoldTimer < holdThreshold;

            if (!isAutoHolding && !isVoidHolding) return;

            float progress;
            string label;
            Color fillColor;
            Color borderColor;

            if (isAutoHolding)
            {
                progress = Mathf.Clamp01(_autoSpinHoldTimer / holdThreshold);
                string actionName = IsAutoSpinActive ? Localization.Get("Выключение", "Disabling") : Localization.Get("Включение", "Enabling");
                label = Localization.Format("{0} авто-рулетки [{1}]", "{0} auto-roulette [{1}]", actionName, ModConfig.AutoSpinHotkeyKey);
                fillColor = IsAutoSpinActive
                    ? new Color(0.95f, 0.38f, 0.32f, 0.92f)
                    : new Color(0.22f, 0.88f, 0.44f, 0.92f);
                borderColor = IsAutoSpinActive
                    ? new Color(0.98f, 0.50f, 0.45f, 0.95f)
                    : new Color(0.40f, 0.98f, 0.55f, 0.95f);
            }
            else
            {
                progress = Mathf.Clamp01(_voidHoldTimer / holdThreshold);
                string queueTag = (_state == BannerState.Spinning || _state == BannerState.Finished) 
                    ? Localization.Get(" в очередь", " to queue") 
                    : "";
                label = Localization.Format("Крутка Бездны{0} [{1}]", "Void Spin{0} [{1}]", queueTag, ModConfig.VoidSpinHotkeyKey);
                fillColor = new Color(0.68f, 0.38f, 0.95f, 0.92f);
                borderColor = new Color(0.80f, 0.55f, 1.0f, 0.95f);
            }

            float barWidth = Mathf.Min(340f, boxRect.width);
            float barHeight = 22f;
            float barX = boxRect.x + (boxRect.width - barWidth) / 2f;
            float barY = boxRect.yMax + 6f; // Exactly under the inventory field / items window!
            Rect barRect = new Rect(barX, barY, barWidth, barHeight);

            // 1. Dark sleek background
            GUIHelper.DrawRect(barRect, new Color(0.05f, 0.07f, 0.11f, 0.96f));

            // 2. Smooth Filling Bar
            float fillWidth = Mathf.Max(0f, (barWidth - 4f) * progress);
            Rect fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, fillWidth, barHeight - 4f);
            GUIHelper.DrawRect(fillRect, fillColor);

            // 3. Border
            GUIHelper.DrawBorder(barRect, borderColor, 1);

            // 4. Centered label with drop shadow for 100% crisp readability
            if (_toastStyle != null)
            {
                Rect shadowRect = new Rect(barRect.x + 1f, barRect.y + 1f, barRect.width, barRect.height);
                _toastStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(shadowRect, label, _toastStyle);

                _toastStyle.normal.textColor = Color.white;
                GUI.Label(barRect, label, _toastStyle);
            }
        }

        private void OnGUI()
        {
            try
            {
                // Only draw when in an active in-game run with active HUD and not paused (ESC)
                if (!ShouldDisplayUI()) return;

                // Yield to casino reel if casino is currently spinning or displaying result
                if (CasinoRouletteUI.IsBannerActive) return;

                InitStyles();

                using (GUIHelper.UseScale(GUIHelper.SafeUiScale, new Vector2(Screen.width / 2f, 0f)))
                {
                    var hud = HUD.readOnlyInstanceList[0];
                    Rect boxRect = GetInventoryFieldRect(hud);

                // 1. Idle state: Subtle hint pill positioned cleanly outside the items zone (above top border)
                if (_state == BannerState.Hidden)
                {
                    bool hasVoid = RouletteManager.Instance.PendingVoidRollsAvailable > 0;
                    bool hasHighTier = RouletteManager.Instance.FreeHighTierRollsAvailable > 0;
                    bool hasLegendary = RouletteManager.Instance.FreeLegendaryRollsAvailable > 0;
                    bool hasBoss = RouletteManager.Instance.FreeBossRollsAvailable > 0;
                    bool hasQualityEquipment = RouletteManager.Instance.FreeQualityEquipmentRollsAvailable > 0;
                    bool hasEquipment = RouletteManager.Instance.FreeEquipmentRollsAvailable > 0;
                    bool hasLunar = RouletteManager.Instance.FreeLunarRollsAvailable > 0;
                    bool hasVoidPotential = RouletteManager.Instance.FreeVoidPotentialRollsAvailable > 0;
                    bool hasUncommon = RouletteManager.Instance.FreeUncommonRollsAvailable > 0;
                    bool hasQuality = RouletteManager.Instance.FreeQualityRollsAvailable > 0;
                    bool hasSolitude = RouletteManager.Instance.SolitudeRollsAvailable > 0;
                    bool hasFree = RouletteManager.Instance.FreeRollsAvailable > 0;
                    bool isFree = RouletteManager.Instance.IsNextRollFree;
                    int voidRolls = RouletteManager.Instance.PendingVoidRollsAvailable;
                    int highTierRolls = RouletteManager.Instance.FreeHighTierRollsAvailable;
                    int legRolls = RouletteManager.Instance.FreeLegendaryRollsAvailable;
                    int bossRolls = RouletteManager.Instance.FreeBossRollsAvailable;
                    int qualityEquipmentRolls = RouletteManager.Instance.FreeQualityEquipmentRollsAvailable;
                    int equipmentRolls = RouletteManager.Instance.FreeEquipmentRollsAvailable;
                    int lunarRolls = RouletteManager.Instance.FreeLunarRollsAvailable;
                    int vpRolls = RouletteManager.Instance.FreeVoidPotentialRollsAvailable;
                    int uncommonRolls = RouletteManager.Instance.FreeUncommonRollsAvailable;
                    int qualityRolls = RouletteManager.Instance.FreeQualityRollsAvailable;
                    int solitudeRolls = RouletteManager.Instance.SolitudeRollsAvailable;
                    int freeRolls = RouletteManager.Instance.FreeRollsAvailable;
                    int nextCost = RouletteManager.Instance.GetCurrentCost();
                    var master = GetLocalMaster();
                    uint money = master != null ? master.money : 0;
                    bool affordable = isFree || money >= nextCost;

                    bool showChaosWidget = ModConfig.IsChaosSystemActive && ModConfig.ShowChanceWidgetInUI.Value;
                    var chances = ChaosEventManager.CalculateChances(master, RouletteManager.Instance.FiftyFiftyActive);

                    string chanceStr = "";
                    if (showChaosWidget)
                    {
                        chanceStr = $"   |   <color=#4ADE80>● {chances.GoodPercent:F0}%</color> / <color=#C0C0C0>{chances.NeutralPercent:F0}%</color> / <color=#F87171>● {chances.BadPercent:F0}%</color>";
                    }

                    float tierMult = master != null ? RouletteManager.Instance.GetPlayerTierMultiplier(master) : 1.0f;
                    float displayMult = tierMult + (hasSolitude ? RouletteManager.Instance.SolitudeRarityMultiplierBonus : 0f);
                    string luckTag = "";
                    if (Mathf.Abs(displayMult - 1.0f) > 0.01f)
                    {
                        luckTag = displayMult > 1.0f 
                            ? $"   |   <color=#4ADE80>{Localization.Get("Редкость", "Rarity")}: x{displayMult:F2}</color>" 
                            : $"   |   <color=#F87171>{Localization.Get("Редкость", "Rarity")}: x{displayMult:F2}</color>";
                    }

                    string casinoHint = ModConfig.IsCasinoRouletteActive ? $"   |   <color=#F1C40F>[{ModConfig.CasinoToggleHotkeyKey}] {Localization.Get("Рулетка", "Casino")}</color>" : "";
                    string journalHint = $"   |   <color=#38BDF8>[{ModConfig.NotificationJournalHotkeyKey}] {Localization.Get("Журнал", "Log")}</color>";

                    string autoHint;
                    if (_autoSpinHoldTimer > 0f)
                    {
                        float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
                        float pct = Mathf.Clamp01(_autoSpinHoldTimer / holdThreshold);
                        string action = IsAutoSpinActive ? Localization.Get("Выкл", "Off") : Localization.Get("Вкл", "On");
                        string bar = GenerateTextProgressBar(pct, 8);
                        autoHint = Localization.Format("   |   <color=#FACC15>[Удерж. {0}] {1} {2}</color>", "   |   <color=#FACC15>[Hold {0}] {1} {2}</color>", ModConfig.AutoSpinHotkeyKey, action, bar);
                    }
                    else
                    {
                        string autoStatusColor = IsAutoSpinActive ? "#4ADE80" : "#94A3B8";
                        string autoStatusText = IsAutoSpinActive ? Localization.Get("ВКЛ", "ON") : Localization.Get("ВЫКЛ", "OFF");
                        string waitingText = (IsAutoSpinActive && !affordable) ? Localization.Get(" (Ожидание $)", " (Waiting $)") : "";
                        autoHint = Localization.Format("   |   <color={0}>[Зажать {1}] Авто: {2}{3}</color>", "   |   <color={0}>[Hold {1}] Auto: {2}{3}</color>", autoStatusColor, ModConfig.AutoSpinHotkeyKey, autoStatusText, waitingText);
                    }

                    string voidHint = "";
                    if (_queuedVoidSpin && RouletteManager.Instance.PendingVoidRollsAvailable > 0)
                    {
                        voidHint = Localization.Get("   |   <color=#C084FC><b>[БЕЗДНА В ОЧЕРЕДИ]</b></color>", "   |   <color=#C084FC><b>[VOID QUEUED]</b></color>");
                    }
                    else if (hasVoid)
                    {
                        if (_voidHoldTimer > 0f)
                        {
                            float holdThreshold = Mathf.Max(0.4f, ModConfig.AutoSpinHoldDuration.Value);
                            float pct = Mathf.Clamp01(_voidHoldTimer / holdThreshold);
                            string bar = GenerateTextProgressBar(pct, 8);
                            voidHint = Localization.Format("   |   <color=#D8B4FE>[Удерж. {0}] Бездна {1}</color>", "   |   <color=#D8B4FE>[Hold {0}] Void {1}</color>", ModConfig.VoidSpinHotkeyKey, bar);
                        }
                        else
                        {
                            voidHint = Localization.Format("   |   <color=#C084FC>[Зажать {0}] Бездна: БЕСПЛАТНО ({1})</color>", "   |   <color=#C084FC>[Hold {0}] Void: FREE ({1})</color>", ModConfig.VoidSpinHotkeyKey, voidRolls);
                        }
                    }

                    string promptText;
                    Color borderColor;
                    Color pillBg;

                    if (hasHighTier)
                    {
                        pillBg = new Color(0.18f, 0.05f, 0.22f, 0.94f);
                        borderColor = ItemPoolHelper.ColorHighTierPylon; // Neon violet-gold
                        promptText = Localization.Format("[{0}] ЛУННЫЙ ПИЛОН (Красный/Бездна/Босс): БЕСПЛАТНО ({1})", "[{0}] LUNAR PYLON (Red/Void/Boss): FREE ({1})", ModConfig.HotkeyKey, highTierRolls);
                        _topLabelStyle!.normal.textColor = new Color(0.95f, 0.50f, 1f);
                    }
                    else if (hasLegendary)
                    {
                        pillBg = new Color(0.16f, 0.05f, 0.05f, 0.94f);
                        borderColor = ItemPoolHelper.ColorLegendary; // Red
                        promptText = Localization.Format("[{0}] ЛЕГЕНДАРНЫЙ КЕЙС: БЕСПЛАТНО ({1})", "[{0}] LEGENDARY CASE: FREE ({1})", ModConfig.HotkeyKey, legRolls);
                        _topLabelStyle!.normal.textColor = new Color(1f, 0.45f, 0.45f);
                    }
                    else if (hasBoss)
                    {
                        pillBg = new Color(0.18f, 0.15f, 0.04f, 0.94f);
                        borderColor = ItemPoolHelper.ColorBoss; // Yellow
                        promptText = Localization.Format("[{0}] БОСС-КЕЙС (Желтый): БЕСПЛАТНО ({1})", "[{0}] BOSS CASE (Yellow): FREE ({1})", ModConfig.HotkeyKey, bossRolls);
                        _topLabelStyle!.normal.textColor = new Color(1f, 0.85f, 0.20f);
                    }
                    else if (hasQualityEquipment)
                    {
                        pillBg = new Color(0.04f, 0.16f, 0.12f, 0.94f);
                        borderColor = new Color(0.20f, 0.90f, 0.55f, 0.95f); // Vibrant emerald-cyan
                        promptText = Localization.Format("[{0}] СНАРЯЖЕНИЕ КАЧЕСТВА: БЕСПЛАТНО ({1})", "[{0}] QUALITY EQUIPMENT: FREE ({1})", ModConfig.HotkeyKey, qualityEquipmentRolls);
                        _topLabelStyle!.normal.textColor = new Color(0.35f, 1f, 0.70f);
                    }
                    else if (hasEquipment)
                    {
                        pillBg = new Color(0.20f, 0.12f, 0.04f, 0.94f);
                        borderColor = ItemPoolHelper.ColorEquipment; // Amber/Orange
                        promptText = Localization.Format("[{0}] СНАРЯЖЕНИЕ: БЕСПЛАТНО ({1})", "[{0}] EQUIPMENT: FREE ({1})", ModConfig.HotkeyKey, equipmentRolls);
                        _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorEquipment;
                    }
                    else if (hasLunar)
                    {
                        pillBg = new Color(0.04f, 0.12f, 0.22f, 0.94f);
                        borderColor = ItemPoolHelper.ColorLunar; // Cyan/Blue
                        promptText = Localization.Format("[{0}] ЛУННЫЙ КЕЙС (Синий): БЕСПЛАТНО ({1})", "[{0}] LUNAR CASE (Blue): FREE ({1})", ModConfig.HotkeyKey, lunarRolls);
                        _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorLunar;
                    }
                    else if (hasVoidPotential)
                    {
                        pillBg = new Color(0.14f, 0.06f, 0.18f, 0.94f);
                        borderColor = ItemPoolHelper.ColorVoidPotential; // Purple
                        promptText = Localization.Format("[{0}] ПОТЕНЦИАЛ БЕЗДНЫ (80/15/5%): БЕСПЛАТНО ({1})", "[{0}] VOID POTENTIAL (80/15/5%): FREE ({1})", ModConfig.HotkeyKey, vpRolls);
                        _topLabelStyle!.normal.textColor = new Color(0.85f, 0.50f, 1f);
                    }
                    else if (hasUncommon)
                    {
                        pillBg = new Color(0.05f, 0.16f, 0.06f, 0.94f);
                        borderColor = ItemPoolHelper.ColorUncommon; // Green
                        promptText = Localization.Format("[{0}] НЕОБЫЧНЫЙ КЕЙС (Зеленый): БЕСПЛАТНО ({1})", "[{0}] UNCOMMON CASE (Green): FREE ({1})", ModConfig.HotkeyKey, uncommonRolls);
                        _topLabelStyle!.normal.textColor = new Color(0.50f, 1f, 0.50f);
                    }
                    else if (hasQuality)
                    {
                        pillBg = new Color(0.04f, 0.16f, 0.12f, 0.94f);
                        borderColor = new Color(0.20f, 0.90f, 0.55f, 0.95f); // Vibrant emerald-cyan
                        promptText = Localization.Format("[{0}] КРУТКА КАЧЕСТВА: БЕСПЛАТНО ({1})", "[{0}] QUALITY SPIN: FREE ({1})", ModConfig.HotkeyKey, qualityRolls);
                        _topLabelStyle!.normal.textColor = new Color(0.35f, 1f, 0.70f);
                    }
                    else if (hasSolitude)
                    {
                        pillBg = new Color(0.04f, 0.12f, 0.20f, 0.94f);
                        borderColor = new Color(0.30f, 0.85f, 1f, 0.95f); // Cyan lunar border
                        float bonus = RouletteManager.Instance.SolitudeRarityMultiplierBonus;
                        promptText = Localization.Format("[{0}] ОДИНОЧЕСТВО (+x{2:F2} Редкость): БЕСПЛАТНО ({1})", "[{0}] SOLITUDE (+x{2:F2} Rarity): FREE ({1})", ModConfig.HotkeyKey, solitudeRolls, bonus);
                        _topLabelStyle!.normal.textColor = new Color(0.40f, 0.90f, 1f);
                    }
                    else if (hasFree)
                    {
                        pillBg = new Color(0.08f, 0.10f, 0.14f, 0.88f);
                        borderColor = new Color(1f, 0.85f, 0.20f, 0.95f); // Golden border for free rolls
                        promptText = Localization.Format("[{0}] Крутить рулетку: БЕСПЛАТНО ({1})", "[{0}] Spin Roulette: FREE ({1})", ModConfig.HotkeyKey, freeRolls);
                        _topLabelStyle!.normal.textColor = new Color(1f, 0.90f, 0.30f);
                    }
                    else
                    {
                        pillBg = new Color(0.08f, 0.10f, 0.14f, 0.88f);
                        borderColor = affordable ? new Color(0.25f, 0.80f, 0.35f, 0.85f) : new Color(0.45f, 0.45f, 0.50f, 0.65f);
                        int paidRolls = RouletteManager.Instance.PaidRollsThisStage;
                        string stageRollInfo = paidRolls > 0 ? Localization.Format(" (Круток: {0})", " (Spins: {0})", paidRolls) : "";
                        promptText = Localization.Format("[{0}] Крутить рулетку: ${1}{2}", "[{0}] Spin Roulette: ${1}{2}", ModConfig.HotkeyKey, nextCost, stageRollInfo);
                        _topLabelStyle!.normal.textColor = affordable ? new Color(0.7f, 1f, 0.7f) : new Color(0.85f, 0.85f, 0.85f);
                    }

                    if (_autoSpinHoldTimer > 0f)
                    {
                        borderColor = new Color(0.98f, 0.80f, 0.15f, 0.95f);
                    }
                    else if (_voidHoldTimer > 0f)
                    {
                        borderColor = ItemPoolHelper.ColorVoid;
                    }
                    else if (IsAutoSpinActive)
                    {
                        borderColor = new Color(0.25f, 0.90f, 0.50f, 0.95f);
                    }

                    string fullPrompt = promptText + voidHint + chanceStr + luckTag + casinoHint + journalHint + autoHint;
                    float textWidth = _topLabelStyle!.CalcSize(new GUIContent(fullPrompt)).x + 24f;
                    float pillWidth = Mathf.Clamp(textWidth, 230f, 1250f);
                    float pillHeight = 22f;
                    float pillX = boxRect.x + (boxRect.width - pillWidth) / 2f;
                    float pillY = Mathf.Max(2f, boxRect.y - pillHeight - 4f); // Outside items zone, strictly ABOVE top border
                    Rect pillRect = new Rect(pillX, pillY, pillWidth, pillHeight);

                    GUIHelper.DrawRect(pillRect, pillBg);
                    GUIHelper.DrawBorder(pillRect, borderColor, 1);
                    GUI.Label(pillRect, fullPrompt, _topLabelStyle);

                    DrawToastNotification(boxRect);
                    DrawHoldProgressBar(boxRect);
                    return;
                }

                // 2. Warning state: Quick flash message outside the items zone (ABOVE top border)
                if (_state == BannerState.Warning)
                {
                    float warnWidth = 320f;
                    float warnHeight = 22f;
                    float warnX = boxRect.x + (boxRect.width - warnWidth) / 2f;
                    float warnY = Mathf.Max(2f, boxRect.y - warnHeight - 4f); // Outside items zone, strictly ABOVE top border
                    Rect warnRect = new Rect(warnX, warnY, warnWidth, warnHeight);

                    GUIHelper.DrawRect(warnRect, new Color(0.22f, 0.05f, 0.05f, 0.95f));
                    GUIHelper.DrawBorder(warnRect, new Color(0.95f, 0.25f, 0.25f, 1f), 1);

                    GUI.Label(warnRect, _warningMessage, _warningStyle);

                    DrawToastNotification(boxRect);
                    DrawHoldProgressBar(boxRect);
                    return;
                }

                // 3. Active State (Spinning / Finished): Matches the inventory field with large square cards
                GUIHelper.DrawRect(boxRect, new Color(0.07f, 0.08f, 0.11f, 0.96f));
                GUIHelper.DrawBorder(boxRect, new Color(0.26f, 0.29f, 0.38f, 0.92f), 1);

                // Top Header inside the field
                Rect headerRect = new Rect(boxRect.x, boxRect.y + 2f, boxRect.width, 16f);
                int curCost = RouletteManager.Instance.GetCurrentCost();
                int stageNum = Run.instance.stageClearCount + 1;
                int rolls = RouletteManager.Instance.TotalRolls;

                string headerText;
                if (_state == BannerState.Spinning)
                {
                    string spinMode = "";
                    if (_isCurrentSpinFree)
                    {
                        if (_spinTierType == RouletteTierType.Void)
                        {
                            spinMode = Localization.Get(" (БЕЗДНА - БЕСПЛАТНО)", " (VOID - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorVoid;
                        }
                        else if (_spinTierType == RouletteTierType.LunarVoid)
                        {
                            spinMode = Localization.Get(" (АНОМАЛИЯ - БЕЗДНА / ЛУНА)", " (ANOMALY - VOID / LUNAR)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorLunarVoid;
                        }
                        else if (_spinTierType == RouletteTierType.HighTierPylon)
                        {
                            spinMode = Localization.Get(" (ЛУННЫЙ ПИЛОН - БЕСПЛАТНО)", " (LUNAR PYLON - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorHighTierPylon;
                        }
                        else if (_spinTierType == RouletteTierType.Legendary)
                        {
                            spinMode = Localization.Get(" (ЛЕГЕНДАРНЫЙ - БЕСПЛАТНО)", " (LEGENDARY - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorLegendary;
                        }
                        else if (_spinTierType == RouletteTierType.Boss)
                        {
                            spinMode = Localization.Get(" (БОСС-КЕЙС - БЕСПЛАТНО)", " (BOSS CASE - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorBoss;
                        }
                        else if (_spinTierType == RouletteTierType.BossReward)
                        {
                            string qPart = "";
                            if (RouletteManager.Instance.PendingBossMinQualityTiers.Count > 0)
                            {
                                var qTier = RouletteManager.Instance.PendingBossMinQualityTiers.Peek();
                                qPart = $" [{QualityCompat.GetQualityName(qTier)}]";
                            }
                            spinMode = Localization.Format(" (НАГРАДА БОССА{0} - БЕСПЛАТНО)", " (BOSS REWARD{0} - FREE)", qPart);
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorBoss;
                        }
                        else if (_spinTierType == RouletteTierType.ZetAspects || _spinTierType == RouletteTierType.VanillaAspect)
                        {
                            spinMode = Localization.Get(" (АСПЕКТ - БЕСПЛАТНО)", " (ASPECT - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorZetAspects;
                        }
                        else if (_spinTierType == RouletteTierType.QualityEquipment)
                        {
                            spinMode = Localization.Get(" (СНАРЯЖЕНИЕ КАЧЕСТВА - БЕСПЛАТНО)", " (QUALITY EQUIPMENT - FREE)");
                            _topLabelStyle!.normal.textColor = new Color(0.35f, 1f, 0.70f);
                        }
                        else if (_spinTierType == RouletteTierType.Equipment)
                        {
                            spinMode = Localization.Get(" (СНАРЯЖЕНИЕ - БЕСПЛАТНО)", " (EQUIPMENT - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorEquipment;
                        }
                        else if (_spinTierType == RouletteTierType.Lunar)
                        {
                            spinMode = Localization.Get(" (ЛУННЫЙ КЕЙС - БЕСПЛАТНО)", " (LUNAR CASE - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorLunar;
                        }
                        else if (_spinTierType == RouletteTierType.VoidPotential)
                        {
                            spinMode = Localization.Get(" (ПОТЕНЦИАЛ БЕЗДНЫ 80/15/5% - БЕСПЛАТНО)", " (VOID POTENTIAL 80/15/5% - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorVoidPotential;
                        }
                        else if (_spinTierType == RouletteTierType.Uncommon)
                        {
                            spinMode = Localization.Get(" (НЕОБЫЧНЫЙ КЕЙС - БЕСПЛАТНО)", " (UNCOMMON CASE - FREE)");
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorUncommon;
                        }
                        else
                        {
                            spinMode = Localization.Get(" (БЕСПЛАТНО)", " (FREE)");
                            _topLabelStyle!.normal.textColor = new Color(1f, 0.90f, 0.30f);
                        }
                    }
                    else
                    {
                        spinMode = "";
                        _topLabelStyle!.normal.textColor = new Color(0.85f, 0.88f, 0.95f);
                    }
                    string autoTag = IsAutoSpinActive ? Localization.Get("<color=#4ADE80>[АВТО]</color> ", "<color=#4ADE80>[AUTO]</color> ") : "";
                    int paidRolls = RouletteManager.Instance.PaidRollsThisStage;
                    string queueTag = (_queuedVoidSpin && RouletteManager.Instance.PendingVoidRollsAvailable > 0) ? Localization.Get(" <color=#C084FC>[СЛЕД: БЕЗДНА]</color>", " <color=#C084FC>[NEXT: VOID]</color>") : "";
                    headerText = Localization.Format(
                        "{0}ПРОКРУТКА РУЛЕТКИ{1}...{2}   |   Этап {3}   |   Платных на этапе: {4}   |   Всего: {5}",
                        "{0}SPINNING ROULETTE{1}...{2}   |   Stage {3}   |   Paid this stage: {4}   |   Total: {5}",
                        autoTag, spinMode, queueTag, stageNum, paidRolls, rolls);
                }
                else if (_state == BannerState.VoidDecision)
                {
                    bool isEquipment = _wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                    bool isLunar = _wonItem != null && _wonItem.IsLunarItem;
                    string wonName = _wonItem != null ? _wonItem.LocalizedName : (isEquipment ? Localization.Get("Снаряжение", "Equipment") : (isLunar ? Localization.Get("Лунный предмет", "Lunar Item") : Localization.Get("Предмет Бездны", "Void Item")));

                    if (isEquipment)
                    {
                        headerText = Localization.Format(
                            "<color=#FB923C><b>[СНАРЯЖЕНИЕ: ВЫБОР]</b></color> ВЫПАЛО: <color=#FED7AA>{0}</color>   |   <color=#4ADE80>[{1}] ВЗЯТЬ (ЭКИПИРОВАТЬ)</color>   |   <color=#FB923C>[{2}] СКИНУТЬ НА ПОЛ</color>",
                            "<color=#FB923C><b>[EQUIPMENT: CHOICE]</b></color> WON: <color=#FED7AA>{0}</color>   |   <color=#4ADE80>[{1}] TAKE (EQUIP)</color>   |   <color=#FB923C>[{2}] DROP TO FLOOR</color>",
                            wonName, ModConfig.HotkeyKey, ModConfig.VoidSpinHotkeyKey);
                        _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorEquipment;
                    }
                    else
                    {
                        string costStr;
                        if (ModConfig.VoidDeclineCostType.Value == VoidDeclineCostMode.MaxHealthPenalty)
                        {
                            float hpCost = ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value;
                            costStr = Localization.Format("<color=#FF6B6B>(-{0:0}% Макс. ХП)</color>", "<color=#FF6B6B>(-{0:0}% Max HP)</color>", hpCost);
                        }
                        else
                        {
                            int lunarCost = Mathf.Max(0, ModConfig.VoidDeclineLunarCoinsCost.Value);
                            uint myCoins = GetLocalPlayerLunarCoins();
                            bool canAffordLunar = myCoins >= (uint)lunarCost;
                            costStr = canAffordLunar 
                                ? Localization.Format("<color=#8CE6FF>(-{0} Лун. монет)</color>", "<color=#8CE6FF>(-{0} Lunar Coins)</color>", lunarCost) 
                                : Localization.Format("<color=#FF6B6B>(Нужно {0} Лун. монет)</color>", "<color=#FF6B6B>(Requires {0} Lunar Coins)</color>", lunarCost);
                        }

                        string rerollHint = "";
                        if (ModConfig.EnableVoidReroll.Value)
                        {
                            int maxRerolls = Mathf.Max(1, ModConfig.VoidRerollMaxPerItem.Value);
                            int rerollCost = Mathf.Max(0, ModConfig.VoidRerollLunarCoinsCost.Value);
                            uint myCoins = GetLocalPlayerLunarCoins();
                            bool canReroll = _voidRerollsUsed < maxRerolls && myCoins >= (uint)rerollCost;
                            string rCol = canReroll ? "#38BDF8" : (_voidRerollsUsed >= maxRerolls ? "#9CA3AF" : "#FF6B6B");
                            rerollHint = Localization.Format(
                                "   |   <color={0}>[{1}] ПЕРЕКРУТИТЬ (-{2} Лун., {3}/{4})</color>",
                                "   |   <color={0}>[{1}] REROLL (-{2} Lunar, {3}/{4})</color>",
                                rCol, ModConfig.VoidRerollHotkeyKey, rerollCost, _voidRerollsUsed, maxRerolls);
                        }

                        if (isLunar)
                        {
                            headerText = Localization.Format(
                                "<color=#38BDF8><b>[ЛУНА: ВЫБОР ЕРЕСИ]</b></color> ВЫПАЛО: <color=#7DD3FC>{0}</color>   |   <color=#4ADE80>[{1}] ВЗЯТЬ</color>{2}   |   <color=#38BDF8>[{3}] ОТКАЗАТЬСЯ {4}</color>",
                                "<color=#38BDF8><b>[LUNAR: CHOICE OF HERESY]</b></color> WON: <color=#7DD3FC>{0}</color>   |   <color=#4ADE80>[{1}] TAKE</color>{2}   |   <color=#38BDF8>[{3}] DECLINE {4}</color>",
                                wonName, ModConfig.HotkeyKey, rerollHint, ModConfig.VoidSpinHotkeyKey, costStr);
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorLunar;
                        }
                        else
                        {
                            headerText = Localization.Format(
                                "<color=#C084FC><b>[БЕЗДНА: НОВОЕ ЗАРАЖЕНИЕ]</b></color> ВЫПАЛО: <color=#D8B4FE>{0}</color>   |   <color=#4ADE80>[{1}] ВЗЯТЬ</color>{2}   |   <color=#C084FC>[{3}] ОТКАЗАТЬСЯ {4}</color>",
                                "<color=#C084FC><b>[VOID: NEW INFESTATION]</b></color> WON: <color=#D8B4FE>{0}</color>   |   <color=#4ADE80>[{1}] TAKE</color>{2}   |   <color=#C084FC>[{3}] DECLINE {4}</color>",
                                wonName, ModConfig.HotkeyKey, rerollHint, ModConfig.VoidSpinHotkeyKey, costStr);
                            _topLabelStyle!.normal.textColor = ItemPoolHelper.ColorVoid;
                        }
                    }
                }
                else
                {
                    string wonName = _wonItem != null ? _wonItem.LocalizedName : Localization.Get("Предмет", "Item");
                    if (_wonItem != null && _wonItem.QualityTier != QualityTierCompat.None)
                    {
                        string qColor = ColorUtility.ToHtmlStringRGB(_wonItem.QualityColor);
                        wonName = $"{wonName} <color=#{qColor}>[{_wonItem.QualityLabel}]</color>";
                    }
                    string tierName = _wonItem != null ? _wonItem.TierLabel : "";
                    string nextCostStr = "";
                    if (RouletteManager.Instance.FreeHighTierRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("ЛУННЫЙ ПИЛОН ({0})", "LUNAR PYLON ({0})", RouletteManager.Instance.FreeHighTierRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeLegendaryRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("ЛЕГЕНДАРНЫЙ ({0})", "LEGENDARY ({0})", RouletteManager.Instance.FreeLegendaryRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeBossRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("НАГРАДА БОССА ({0})", "BOSS REWARD ({0})", RouletteManager.Instance.FreeBossRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeQualityEquipmentRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("СНАРЯЖЕНИЕ КАЧЕСТВА ({0})", "QUALITY EQUIPMENT ({0})", RouletteManager.Instance.FreeQualityEquipmentRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeEquipmentRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("СНАРЯЖЕНИЕ ({0})", "EQUIPMENT ({0})", RouletteManager.Instance.FreeEquipmentRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeLunarRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("ЛУННЫЙ КЕЙС ({0})", "LUNAR CASE ({0})", RouletteManager.Instance.FreeLunarRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeVoidPotentialRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("ПОТЕНЦИАЛ БЕЗДНЫ ({0})", "VOID POTENTIAL ({0})", RouletteManager.Instance.FreeVoidPotentialRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeUncommonRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("НЕОБЫЧНЫЙ ({0})", "UNCOMMON ({0})", RouletteManager.Instance.FreeUncommonRollsAvailable);
                    }
                    else if (RouletteManager.Instance.FreeQualityRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("КАЧЕСТВО ({0})", "QUALITY ({0})", RouletteManager.Instance.FreeQualityRollsAvailable);
                    }
                    else if (RouletteManager.Instance.SolitudeRollsAvailable > 0)
                    {
                        float bonus = RouletteManager.Instance.SolitudeRarityMultiplierBonus;
                        nextCostStr = Localization.Format("ОДИНОЧЕСТВО +x{1:F2} ({0})", "SOLITUDE +x{1:F2} ({0})", RouletteManager.Instance.SolitudeRollsAvailable, bonus);
                    }
                    else if (RouletteManager.Instance.FreeRollsAvailable > 0)
                    {
                        nextCostStr = Localization.Format("БЕСПЛАТНО ({0})", "FREE ({0})", RouletteManager.Instance.FreeRollsAvailable);
                    }
                    else
                    {
                        int paidRolls = RouletteManager.Instance.PaidRollsThisStage;
                        string stageRollInfo = paidRolls > 0 ? Localization.Format(" (Круток: {0})", " (Spins: {0})", paidRolls) : "";
                        nextCostStr = $"${curCost}{stageRollInfo}";
                    }

                    string voidHeaderHint = "";
                    if (_queuedVoidSpin && RouletteManager.Instance.PendingVoidRollsAvailable > 0)
                    {
                        voidHeaderHint = Localization.Get("   |   <color=#C084FC><b>[СЛЕДУЮЩИЙ: БЕЗДНА]</b></color>", "   |   <color=#C084FC><b>[NEXT: VOID]</b></color>");
                    }
                    else if (RouletteManager.Instance.PendingVoidRollsAvailable > 0)
                    {
                        voidHeaderHint = Localization.Format("   |   <color=#C084FC>[Зажать {0}] Бездна ({1})</color>", "   |   <color=#C084FC>[Hold {0}] Void ({1})</color>", ModConfig.VoidSpinHotkeyKey, RouletteManager.Instance.PendingVoidRollsAvailable);
                    }

                    string chaosTag = "";
                    if (_wonItem != null && _wonItem.ChaosEvent != ChaosEventType.None)
                    {
                        var def = ChaosEventManager.GetDefinition(_wonItem.ChaosEvent);
                        if (def != null)
                        {
                            string hex = def.Polarity == ChaosPolarity.Good ? "#4ADE80" : "#F87171";
                            chaosTag = $"  |  <color={hex}><b>[{def.Title}]</b></color>";
                        }
                    }
                    string autoTag = IsAutoSpinActive ? Localization.Get("<color=#4ADE80>[АВТО]</color> ", "<color=#4ADE80>[AUTO]</color> ") : "";
                    string nextSpinDelay = IsAutoSpinActive ? Localization.Format("   |   След. через {0:F1}с...", "   |   Next in {0:F1}s...", _visibilityTimer) : "";
                    string titlePrefix = (_spinTierType == RouletteTierType.Void)
                        ? Localization.Get("БЕЗДНА", "VOID")
                        : (_spinTierType == RouletteTierType.LunarVoid
                            ? Localization.Get("АНОМАЛИЯ", "ANOMALY")
                            : (_spinTierType == RouletteTierType.BossReward
                                ? Localization.Get("НАГРАДА БОССА", "BOSS REWARD")
                                : Localization.Get("РУЛЕТКА", "ROULETTE")));
                    string dollTag = (_wonItem != null && _wonItem.UpgradedByChanceDoll) ? Localization.Get(" <color=#38E54D>[КУКЛА УДАЧИ ★]</color>", " <color=#38E54D>[CHANCE DOLL ★]</color>") : "";
                    headerText = Localization.Format(
                        "{0}{1}: ВЫПАЛО {2} ({3}){4}{5}   |   След. [{6}]: {7}{8}{9}",
                        "{0}{1}: WON {2} ({3}){4}{5}   |   Next [{6}]: {7}{8}{9}",
                        autoTag, titlePrefix, wonName, tierName, dollTag, chaosTag, ModConfig.HotkeyKey, nextCostStr, voidHeaderHint, nextSpinDelay);
                    _topLabelStyle!.normal.textColor = _wonItem != null ? _wonItem.RarityColor : Color.yellow;
                }

                GUI.Label(headerRect, headerText, _topLabelStyle);

                // Viewport Reel between top header and bottom padding
                float vpY = boxRect.y + 20f;
                float vpH = Mathf.Max(CardHeight + 4f, boxRect.height - 26f);
                float vpX = boxRect.x + 8f;
                float vpW = boxRect.width - 16f;
                Rect viewportRect = new Rect(vpX, vpY, vpW, vpH);

                GUIHelper.DrawRect(viewportRect, new Color(0.03f, 0.04f, 0.06f, 0.95f));

                // Scissored Card Reel
                GUI.BeginGroup(viewportRect);
                float cardY = (vpH - CardHeight) / 2f;
                for (int i = 0; i < _reelItems.Count; i++)
                {
                    float cardX = (i * SlotWidth) - _currentScrollX;

                    // Culling cards outside visible viewport
                    if (cardX + CardWidth < -10f || cardX > vpW + 10f) continue;

                    Rect cardRect = new Rect(cardX, cardY, CardWidth, CardHeight);
                    var item = _reelItems[i];
                    bool isChaos = item.ChaosPolarity != ChaosPolarity.None;

                    // 1. Base card background
                    GUIHelper.DrawRect(cardRect, new Color(0.11f, 0.12f, 0.17f, 0.95f));

                    // 2. Draw Item Icon
                    float iconSize = 64f;
                    float iconX = cardRect.x + (CardWidth - iconSize) / 2f;
                    float iconY = cardRect.y + 4f;
                    Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

                    if (item.IconTexture != null)
                    {
                        if (item.IconSprite != null)
                        {
                            GUI.DrawTextureWithTexCoords(iconRect, item.IconTexture, item.SpriteUvRect);
                        }
                        else
                        {
                            GUI.DrawTexture(iconRect, item.IconTexture, ScaleMode.ScaleToFit);
                        }
                    }

                    // 3. Chaos Overlay: Corner triangle indicator matching Casino Roulette
                    if (isChaos)
                    {
                        Color topRightBg = item.ChaosPolarity == ChaosPolarity.Good
                            ? new Color(0.10f, 0.35f, 0.16f, 1.0f) // Solid Emerald
                            : new Color(0.40f, 0.08f, 0.12f, 1.0f); // Solid Crimson

                        Color dividerCol = item.ChaosPolarity == ChaosPolarity.Good
                            ? new Color(0.35f, 0.98f, 0.45f, 1.0f) // Crisp bright emerald divider
                            : new Color(0.98f, 0.30f, 0.35f, 1.0f); // Crisp bright crimson divider

                        Texture2D overlayTex = GUIHelper.CreateDiagonalOverlayTexture(94, 94, topRightBg, dividerCol, 2, 0.72f);
                        GUI.DrawTexture(cardRect, overlayTex);

                        // Tilted Question Mark '?' inside the top-right corner wedge
                        float qW = 22f;
                        float qH = 22f;
                        float qX = cardRect.xMax - qW - 3f;
                        float qY = cardRect.y + 4f;
                        Rect qMarkRect = new Rect(qX, qY, qW, qH);

                        Color qColor = item.ChaosPolarity == ChaosPolarity.Good
                            ? new Color(1f, 0.92f, 0.25f, 1f)
                            : new Color(1f, 0.35f, 0.38f, 1f);

                        Color outlineColor = item.ChaosPolarity == ChaosPolarity.Good
                            ? new Color(0.02f, 0.12f, 0.04f, 0.95f)
                            : new Color(0.14f, 0.02f, 0.04f, 0.95f);

                        Matrix4x4 prevMatrix = GUI.matrix;
                        Vector2 pivot = new Vector2(qMarkRect.center.x, qMarkRect.center.y);
                        GUIUtility.RotateAroundPivot(4f, pivot);

                        _questionMarkStyle!.fontSize = 18;
                        _questionMarkStyle.normal.textColor = outlineColor;
                        GUI.Label(new Rect(qMarkRect.x - 1f, qMarkRect.y, qW, qH), "?", _questionMarkStyle);
                        GUI.Label(new Rect(qMarkRect.x + 1f, qMarkRect.y, qW, qH), "?", _questionMarkStyle);
                        GUI.Label(new Rect(qMarkRect.x, qMarkRect.y - 1f, qW, qH), "?", _questionMarkStyle);
                        GUI.Label(new Rect(qMarkRect.x, qMarkRect.y + 1f, qW, qH), "?", _questionMarkStyle);

                        _questionMarkStyle.normal.textColor = qColor;
                        GUI.Label(qMarkRect, "?", _questionMarkStyle);

                        GUI.matrix = prevMatrix;
                    }

                    // 5. Bottom rarity accent bar (3px)
                    Rect bottomBar = new Rect(cardRect.x, cardRect.yMax - 3f, cardRect.width, 3f);
                    GUIHelper.DrawRect(bottomBar, item.RarityColor);

                    // Draw localized item name with ellipsis (...) if truncated
                    Rect nameRect = new Rect(cardRect.x + 2f, cardRect.y + 71f, CardWidth - 4f, 17f);
                    _cardTitleStyle!.normal.textColor = item.RarityColor;
                    string titlePrefix = (item.QualityTier != QualityTierCompat.None) ? $"<color=#{ColorUtility.ToHtmlStringRGB(item.QualityColor)}>✦</color> " : "";
                    string displayName = GetTruncatedText(titlePrefix + item.LocalizedName, _cardTitleStyle, CardWidth - 4f);
                    GUI.Label(nameRect, displayName, _cardTitleStyle);

                    // Card border: Golden highlight for winning slot when finished or Void/Lunar/Equipment highlight when VoidDecision
                    if ((_state == BannerState.Finished || _state == BannerState.VoidDecision) && i == WinningSlotIndex)
                    {
                        Color highlightCol;
                        if (_state == BannerState.VoidDecision)
                        {
                            if (_wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None)
                                highlightCol = ItemPoolHelper.ColorEquipment;
                            else if (_wonItem != null && _wonItem.IsLunarItem)
                                highlightCol = ItemPoolHelper.ColorLunar;
                            else
                                highlightCol = ItemPoolHelper.ColorVoid;
                        }
                        else
                        {
                            highlightCol = new Color(1f, 0.85f, 0.15f, 1f);
                        }
                        GUIHelper.DrawBorder(cardRect, highlightCol, 2);
                    }
                    else if (isChaos)
                    {
                        Color chaosBorder = item.ChaosPolarity == ChaosPolarity.Good
                            ? new Color(0.25f, 0.85f, 0.35f, 0.9f)
                            : new Color(0.95f, 0.25f, 0.30f, 0.9f);
                        GUIHelper.DrawBorder(cardRect, chaosBorder, 1);
                    }
                    else
                    {
                        GUIHelper.DrawBorder(cardRect, new Color(item.RarityColor.r, item.RarityColor.g, item.RarityColor.b, 0.45f), 1);
                    }
                }
                GUI.EndGroup();

                // Center Golden Needle Marker
                float needleX = vpX + (vpW / 2f);
                UiCommon.DrawCenterNeedle(needleX, vpY, vpH);

                // Action Buttons for Void / Equipment Decision
                if (_state == BannerState.VoidDecision)
                {
                    bool canClickDecision = !GUIHelper.IsTypingOrInputBlocked();
                    bool isEquipment = _wonItem != null && _wonItem.PickupDef != null && _wonItem.PickupDef.equipmentIndex != EquipmentIndex.None;
                    float btnHeight = 28f;
                    var btnStyle = _actionBtnStyle ?? _toastStyle!;
                    float btnY = boxRect.yMax + 6f;

                    if (isEquipment)
                    {
                        float btnWidth = 280f;
                        float gap = 20f;
                        float totalW = btnWidth * 2f + gap;
                        float startX = boxRect.x + (boxRect.width - totalW) / 2f;

                        Rect takeRect = new Rect(startX, btnY, btnWidth, btnHeight);
                        Rect dropRect = new Rect(startX + btnWidth + gap, btnY, btnWidth, btnHeight);

                        // 1. Take Button: [✓ ВЗЯТЬ (ЭКИПИРОВАТЬ) [B]]
                        Color takeBg = new Color(0.08f, 0.18f, 0.10f, 0.96f);
                        Color takeBorder = new Color(0.35f, 0.90f, 0.45f, 0.95f);
                        GUIHelper.DrawRect(takeRect, takeBg);
                        GUIHelper.DrawBorder(takeRect, takeBorder, 1);
                        btnStyle.normal.textColor = new Color(0.40f, 0.98f, 0.55f);
                        GUI.Label(takeRect, Localization.Format("✓ ВЗЯТЬ (ЭКИПИРОВАТЬ) [{0}]", "✓ TAKE (EQUIP) [{0}]", ModConfig.HotkeyKey), btnStyle);
                        if (canClickDecision && GUI.Button(takeRect, GUIContent.none, GUIStyle.none))
                        {
                            AcceptVoidItem();
                        }

                        // 2. Drop Button: [↓ СКИНУТЬ НА ПОЛ [C]]
                        Color dropBg = new Color(0.20f, 0.12f, 0.05f, 0.96f);
                        Color dropBorder = new Color(0.95f, 0.60f, 0.25f, 0.95f);
                        GUIHelper.DrawRect(dropRect, dropBg);
                        GUIHelper.DrawBorder(dropRect, dropBorder, 1);
                        btnStyle.normal.textColor = new Color(1f, 0.75f, 0.40f);
                        GUI.Label(dropRect, Localization.Format("↓ СКИНУТЬ НА ПОЛ [{0}]", "↓ DROP TO FLOOR [{0}]", ModConfig.VoidSpinHotkeyKey), btnStyle);
                        if (canClickDecision && GUI.Button(dropRect, GUIContent.none, GUIStyle.none))
                        {
                            DeclineVoidItem();
                        }
                    }
                    else
                    {
                        int lunarCost = Mathf.Max(0, ModConfig.VoidDeclineLunarCoinsCost.Value);
                        uint myCoins = GetLocalPlayerLunarCoins();
                        bool canAffordLunar = myCoins >= (uint)lunarCost;

                        bool enableReroll = ModConfig.EnableVoidReroll.Value;
                        int maxRerolls = Mathf.Max(1, ModConfig.VoidRerollMaxPerItem.Value);
                        int rerollCost = Mathf.Max(0, ModConfig.VoidRerollLunarCoinsCost.Value);
                        bool limitReached = _voidRerollsUsed >= maxRerolls;
                        bool canAffordReroll = myCoins >= (uint)rerollCost;

                        if (enableReroll)
                        {
                            float gap = 12f;
                            float btnWidth = Mathf.Clamp((boxRect.width - 24f - (gap * 2f)) / 3f, 200f, 280f);
                            float totalW = btnWidth * 3f + gap * 2f;
                            float startX = boxRect.x + (boxRect.width - totalW) / 2f;

                            Rect acceptRect = new Rect(startX, btnY, btnWidth, btnHeight);
                            Rect rerollRect = new Rect(startX + btnWidth + gap, btnY, btnWidth, btnHeight);
                            Rect declineRect = new Rect(startX + (btnWidth + gap) * 2f, btnY, btnWidth, btnHeight);

                            // 1. Accept Button: [✓ ВЗЯТЬ [B]]
                            Color acceptBg = new Color(0.08f, 0.18f, 0.10f, 0.96f);
                            Color acceptBorder = new Color(0.35f, 0.90f, 0.45f, 0.95f);
                            GUIHelper.DrawRect(acceptRect, acceptBg);
                            GUIHelper.DrawBorder(acceptRect, acceptBorder, 1);
                            btnStyle.normal.textColor = new Color(0.40f, 0.98f, 0.55f);
                            GUI.Label(acceptRect, Localization.Format("✓ ВЗЯТЬ [{0}]", "✓ TAKE [{0}]", ModConfig.HotkeyKey), btnStyle);
                            if (canClickDecision && GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
                            {
                                AcceptVoidItem();
                            }

                            // 2. Reroll Button: [↻ ПЕРЕКРУТИТЬ (-X Лун.) [0/3] [R]]
                            Color rerollBg;
                            Color rerollBorder;
                            string rerollLabel;

                            if (limitReached)
                            {
                                rerollBg = new Color(0.12f, 0.12f, 0.14f, 0.90f);
                                rerollBorder = new Color(0.40f, 0.40f, 0.40f, 0.60f);
                                btnStyle.normal.textColor = new Color(0.60f, 0.60f, 0.60f);
                                rerollLabel = Localization.Format("↻ ЛИМИТ ПЕРЕРОЛОВ ({0}/{1})", "↻ REROLL LIMIT ({0}/{1})", _voidRerollsUsed, maxRerolls);
                            }
                            else if (!canAffordReroll)
                            {
                                rerollBg = new Color(0.15f, 0.08f, 0.08f, 0.96f);
                                rerollBorder = new Color(0.85f, 0.35f, 0.35f, 0.70f);
                                btnStyle.normal.textColor = new Color(0.95f, 0.45f, 0.45f);
                                rerollLabel = Localization.Format("↻ ПЕРЕКРУТИТЬ (Нужно {0} Лун.) [{1}]", "↻ REROLL (Need {0} Lunar) [{1}]", rerollCost, ModConfig.VoidRerollHotkeyKey);
                            }
                            else
                            {
                                rerollBg = new Color(0.06f, 0.14f, 0.22f, 0.96f);
                                rerollBorder = new Color(0.25f, 0.75f, 1.0f, 0.95f);
                                btnStyle.normal.textColor = new Color(0.45f, 0.88f, 1.0f);
                                rerollLabel = Localization.Format("↻ ПЕРЕКРУТИТЬ (-{0} Лун.) [{1}/{2}] [{3}]", "↻ REROLL (-{0} Lunar) [{1}/{2}] [{3}]", rerollCost, _voidRerollsUsed, maxRerolls, ModConfig.VoidRerollHotkeyKey);
                            }

                            GUIHelper.DrawRect(rerollRect, rerollBg);
                            GUIHelper.DrawBorder(rerollRect, rerollBorder, 1);
                            GUI.Label(rerollRect, rerollLabel, btnStyle);
                            if (canClickDecision && GUI.Button(rerollRect, GUIContent.none, GUIStyle.none))
                            {
                                RerollVoidItem();
                            }

                            // 3. Decline Button
                            bool isHealthMode = ModConfig.VoidDeclineCostType.Value == VoidDeclineCostMode.MaxHealthPenalty;
                            Color declineBg;
                            Color declineBorder;
                            string declineLabel;

                            if (isHealthMode)
                            {
                                float hpCost = ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value;
                                declineBg = new Color(0.20f, 0.06f, 0.08f, 0.96f);
                                declineBorder = new Color(0.95f, 0.35f, 0.40f, 0.95f);
                                btnStyle.normal.textColor = new Color(1f, 0.55f, 0.55f);
                                declineLabel = Localization.Format("✕ ОТКАЗАТЬСЯ (-{0:0}% ХП) [{1}]", "✕ DECLINE (-{0:0}% HP) [{1}]", hpCost, ModConfig.VoidSpinHotkeyKey);
                            }
                            else
                            {
                                declineBg = canAffordLunar 
                                    ? new Color(0.18f, 0.08f, 0.22f, 0.96f) 
                                    : new Color(0.15f, 0.08f, 0.08f, 0.96f);
                                declineBorder = canAffordLunar 
                                    ? new Color(0.75f, 0.45f, 0.95f, 0.95f) 
                                    : new Color(0.85f, 0.35f, 0.35f, 0.70f);
                                btnStyle.normal.textColor = canAffordLunar ? new Color(0.85f, 0.60f, 1f) : new Color(0.95f, 0.45f, 0.45f);
                                declineLabel = canAffordLunar
                                    ? Localization.Format("✕ ОТКАЗАТЬСЯ (-{0} Лун.) [{1}]", "✕ DECLINE (-{0} Lunar) [{1}]", lunarCost, ModConfig.VoidSpinHotkeyKey)
                                    : Localization.Format("✕ ОТКАЗАТЬСЯ (Нужно {0} Лун.) [{1}]", "✕ DECLINE (Need {0} Lunar) [{1}]", lunarCost, ModConfig.VoidSpinHotkeyKey);
                            }

                            GUIHelper.DrawRect(declineRect, declineBg);
                            GUIHelper.DrawBorder(declineRect, declineBorder, 1);
                            GUI.Label(declineRect, declineLabel, btnStyle);
                            if (canClickDecision && GUI.Button(declineRect, GUIContent.none, GUIStyle.none))
                            {
                                DeclineVoidItem();
                            }
                        }
                        else
                        {
                            float btnWidth = 280f;
                            float gap = 20f;
                            float totalW = btnWidth * 2f + gap;
                            float startX = boxRect.x + (boxRect.width - totalW) / 2f;

                            Rect acceptRect = new Rect(startX, btnY, btnWidth, btnHeight);
                            Rect declineRect = new Rect(startX + btnWidth + gap, btnY, btnWidth, btnHeight);

                            // 1. Accept Button: [✓ ВЗЯТЬ ПРЕДМЕТ [B]]
                            Color acceptBg = new Color(0.08f, 0.18f, 0.10f, 0.96f);
                            Color acceptBorder = new Color(0.35f, 0.90f, 0.45f, 0.95f);
                            GUIHelper.DrawRect(acceptRect, acceptBg);
                            GUIHelper.DrawBorder(acceptRect, acceptBorder, 1);

                            btnStyle.normal.textColor = new Color(0.40f, 0.98f, 0.55f);
                            GUI.Label(acceptRect, Localization.Format("✓ ВЗЯТЬ ПРЕДМЕТ [{0}]", "✓ TAKE ITEM [{0}]", ModConfig.HotkeyKey), btnStyle);
                            if (canClickDecision && GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
                            {
                                AcceptVoidItem();
                            }

                            // 2. Decline Button
                            bool isHealthMode = ModConfig.VoidDeclineCostType.Value == VoidDeclineCostMode.MaxHealthPenalty;
                            Color declineBg;
                            Color declineBorder;
                            string declineLabel;

                            if (isHealthMode)
                            {
                                float hpCost = ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value;
                                declineBg = new Color(0.20f, 0.06f, 0.08f, 0.96f);
                                declineBorder = new Color(0.95f, 0.35f, 0.40f, 0.95f);
                                btnStyle.normal.textColor = new Color(1f, 0.55f, 0.55f);
                                declineLabel = Localization.Format("✕ ОТКАЗАТЬСЯ (-{0:0}% Макс. ХП) [{1}]", "✕ DECLINE (-{0:0}% Max HP) [{1}]", hpCost, ModConfig.VoidSpinHotkeyKey);
                            }
                            else
                            {
                                declineBg = canAffordLunar 
                                    ? new Color(0.18f, 0.08f, 0.22f, 0.96f) 
                                    : new Color(0.15f, 0.08f, 0.08f, 0.96f);
                                declineBorder = canAffordLunar 
                                    ? new Color(0.75f, 0.45f, 0.95f, 0.95f) 
                                    : new Color(0.85f, 0.35f, 0.35f, 0.70f);
                                btnStyle.normal.textColor = canAffordLunar ? new Color(0.85f, 0.60f, 1f) : new Color(0.95f, 0.45f, 0.45f);
                                declineLabel = canAffordLunar
                                    ? Localization.Format("✕ ОТКАЗАТЬСЯ (-{0} Лун. монет) [{1}]", "✕ DECLINE (-{0} Lunar Coins) [{1}]", lunarCost, ModConfig.VoidSpinHotkeyKey)
                                    : Localization.Format("✕ ОТКАЗАТЬСЯ (Нужно {0} Лун. монет) [{1}]", "✕ DECLINE (Need {0} Lunar Coins) [{1}]", lunarCost, ModConfig.VoidSpinHotkeyKey);
                            }

                            GUIHelper.DrawRect(declineRect, declineBg);
                            GUIHelper.DrawBorder(declineRect, declineBorder, 1);
                            GUI.Label(declineRect, declineLabel, btnStyle);
                            if (canClickDecision && GUI.Button(declineRect, GUIContent.none, GUIStyle.none))
                            {
                                DeclineVoidItem();
                            }
                        }
                    }
                }

                // Draw toast notification under the items window if active
                DrawToastNotification(boxRect);
                DrawHoldProgressBar(boxRect);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in RouletteUI.OnGUI: {ex}");
            }
        }
    }
}
