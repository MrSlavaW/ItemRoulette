using System;
using System.Collections.Generic;
using System.Reflection;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using ItemRoulette.Hooks;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace ItemRoulette.UI
{
    public class CasinoRouletteUI : MonoBehaviour
    {
        private static CasinoRouletteUI? _instance;
        public static CasinoRouletteUI? Instance => _instance;
        public static bool IsOpen => _instance != null && _instance._isWindowOpen;
        public static bool IsSpinning => _instance != null && _instance._reelState == ReelState.Spinning;
        public static bool IsBannerActive => _instance != null && (_instance._reelState == ReelState.Spinning || _instance._reelState == ReelState.Finished);

        private bool _isWindowOpen = false;

        // Two-step Spin Confirmation
        private bool _confirmSpinPending = false;
        private float _confirmTimer = 0f;

        // Reel State
        private enum ReelState
        {
            Idle,
            Spinning,
            Finished
        }

        private ReelState _reelState = ReelState.Idle;
        private const int TapeLoops = 7;
        private const float CardWidth = 94f;
        private const float CardHeight = 94f;
        private const float CardSpacing = 8f;
        private const float SlotWidth = CardWidth + CardSpacing; // 102f

        private List<RoulettePocket> _tapePockets = new List<RoulettePocket>();
        private int _winningTapeIndex = 0;
        private float _currentScrollX = 0f;
        private float _targetScrollX = 0f;
        private float _spinTimer = 0f;
        private float _finishDisplayTimer = 0f;
        private int _lastTickedPocket = -1;
        private RoulettePocket _winningPocket;

        // Window Dragging & Position
        private Rect _windowRect = new Rect(100f, 100f, 960f, 320f);
        private bool _isWindowPosInitialized = false;
        private bool _isDraggingWindow = false;
        private Vector2 _dragMouseStart;
        private Vector2 _dragWindowStart;

        // Betting State: The Felt Bets & Tokens
        private CasinoFeltBets _feltBets = new CasinoFeltBets();
        private int _selectedChipDenomination = 25;

        // Life Bet Token
        private bool _isPlacingLifeBet = false;
        private bool _isLifeBetPlaced = false;
        private CasinoBetType _lifeBetType = CasinoBetType.None;
        private int _lifeBetTarget = 0;

        // Item Bet Token
        private bool _isPlacingItemBet = false;
        private bool _isItemBetActive = false;
        private int _itemBetTargetNumber = 0;
        private List<ItemIndex> _playerAvailableItems = new List<ItemIndex>();
        private int _selectedItemIdxIndex = 0;

        // Server-Authoritative Network State
        private string? _pendingSpinTxId = null;
        private bool _spinRequestInFlight = false;
        private CasinoSpinResultMessage? _lastSpinResult = null;

        // Toast notifications
        private float _toastTimer = 0f;
        private string _toastMessage = "";
        private Color _toastColor = Color.white;

        // Last Result Message
        private string _lastResultSummary = "";
        private string _lastResultHeadline = "";
        private Color _lastResultColor = Color.white;

        // Styles
        private GUIStyle? _winHeaderStyle;
        private GUIStyle? _labelBoldStyle;
        private GUIStyle? _labelSmallStyle;
        private GUIStyle? _pocketNumStyle;
        private GUIStyle? _pocketSubStyle;
        private GUIStyle? _spinBtnStyle;
        private GUIStyle? _spinConfirmBtnStyle;
        private GUIStyle? _resultStyle;
        private GUIStyle? _topLabelStyle;
        private GUIStyle? _questionMarkStyle;
        private GUIStyle? _toastStyle;
        private GUIStyle? _feltNumberStyle;
        private GUIStyle? _feltHeaderStyle;
        private GUIStyle? _feltBadgeStyle;
        private GUIStyle? _dragHandleStyle;
        private GUIStyle? _unifiedBtnStyle;


        public void ForceFinishSpinImmediate()
        {
            if (_reelState == ReelState.Spinning)
            {
                FinishSpin();
            }
        }

        public static bool IsPlayerInDroneOrDeadMode(CharacterMaster? master)
        {
            if (master == null) return true;
            if (master.lostBodyToDeath || !master.hasBody) return true;
            var body = master.GetBody();
            if (body == null) return true;
            if (body.healthComponent != null && !body.healthComponent.alive) return true;

            string bodyName = body.name ?? "";
            if (bodyName.IndexOf("drone", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (bodyName.IndexOf("spectat", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (bodyName.IndexOf("turret", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (bodyName.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            string token = body.baseNameToken ?? "";
            if (token.IndexOf("DRONE", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (token.IndexOf("TURRET", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private void Awake()
        {
            _instance = this;
            BuildInitialTape();
            CasinoServerService.OnCasinoSpinResultReceived += HandleCasinoSpinResultReceived;
            On.RoR2.PlayerCharacterMasterController.PollButtonInput += OnPollButtonInput;
            On.RoR2.PlayerCharacterMasterController.PollInputToButtonBuffers += OnPollInputToButtonBuffers;
            On.RoR2.PauseManager.TogglePauseScreen += OnTogglePauseScreen;
            On.RoR2.PauseManager.OpenMinimalPauseScreen += OnOpenMinimalPauseScreen;
            On.RoR2.UI.PauseScreenController.InstantiatePauseScreen += OnInstantiatePauseScreen;
        }

        private static readonly MethodInfo? _suppressAllButtonsMethod =
            typeof(PlayerCharacterMasterController).GetMethod("SuppressAllButtonsUntilReleased", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static void SuppressPlayerButtons(PlayerCharacterMasterController? pcmc)
        {
            if (pcmc == null) return;
            try
            {
                _suppressAllButtonsMethod?.Invoke(pcmc, null);
                var body = pcmc.master != null ? pcmc.master.GetBody() : null;
                if (body != null && body.inputBank != null)
                {
                    body.inputBank.skill1.PushState(false);
                    body.inputBank.skill2.PushState(false);
                    body.inputBank.skill3.PushState(false);
                    body.inputBank.skill4.PushState(false);
                    body.inputBank.interact.PushState(false);
                    body.inputBank.jump.PushState(false);
                    body.inputBank.activateEquipment.PushState(false);
                    body.inputBank.sprint.PushState(false);
                    body.inputBank.ping.PushState(false);
                }
            }
            catch { }
        }

        private static void OnPollInputToButtonBuffers(On.RoR2.PlayerCharacterMasterController.orig_PollInputToButtonBuffers orig, PlayerCharacterMasterController self)
        {
            if (self != null && self.networkUser != null && self.networkUser.isLocalPlayer)
            {
                if (CasinoRouletteUI.IsOpen || (AdminMenuUI.Instance != null && AdminMenuUI.Instance.IsOpen))
                {
                    SuppressPlayerButtons(self);
                    return;
                }
            }
            orig(self);
        }

        private static void OnTogglePauseScreen(On.RoR2.PauseManager.orig_TogglePauseScreen orig)
        {
            if (UiCommon.ShouldSuppressPauseScreen())
            {
                UiCommon.CloseOpenMenus();
                return;
            }
            orig();
        }

        private static void OnOpenMinimalPauseScreen(On.RoR2.PauseManager.orig_OpenMinimalPauseScreen orig)
        {
            if (UiCommon.ShouldSuppressPauseScreen())
            {
                UiCommon.CloseOpenMenus();
                return;
            }
            orig();
        }

        private static GameObject? OnInstantiatePauseScreen(On.RoR2.UI.PauseScreenController.orig_InstantiatePauseScreen orig, bool minimal)
        {
            if (UiCommon.ShouldSuppressPauseScreen())
            {
                UiCommon.CloseOpenMenus();
                return null;
            }
            return orig(minimal);
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            ReleaseCursor();
            CasinoServerService.OnCasinoSpinResultReceived -= HandleCasinoSpinResultReceived;
            On.RoR2.PlayerCharacterMasterController.PollButtonInput -= OnPollButtonInput;
            On.RoR2.PlayerCharacterMasterController.PollInputToButtonBuffers -= OnPollInputToButtonBuffers;
            On.RoR2.PauseManager.TogglePauseScreen -= OnTogglePauseScreen;
            On.RoR2.PauseManager.OpenMinimalPauseScreen -= OnOpenMinimalPauseScreen;
            On.RoR2.UI.PauseScreenController.InstantiatePauseScreen -= OnInstantiatePauseScreen;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private static void OnPollButtonInput(On.RoR2.PlayerCharacterMasterController.orig_PollButtonInput orig, PlayerCharacterMasterController self)
        {
            if (self != null && self.networkUser != null && self.networkUser.isLocalPlayer)
            {
                if (CasinoRouletteUI.IsOpen || (AdminMenuUI.Instance != null && AdminMenuUI.Instance.IsOpen))
                {
                    SuppressPlayerButtons(self);
                    return;
                }
            }
            orig(self);
        }

        private void CaptureCursor() => UiCommon.AcquireCursor(this);

        private void ReleaseCursor() => UiCommon.ReleaseCursor(this);

        public void ShowToast(string message, Color color, float duration = 3.5f)
        {
            _toastMessage = message;
            _toastColor = color;
            _toastTimer = duration;
        }

        private void BuildInitialTape()
        {
            _tapePockets.Clear();
            var master = GetLocalMaster();
            var chances = ChaosEventManager.CalculateChances(master, RouletteManager.Instance.FiftyFiftyActive);

            for (int loop = 0; loop < TapeLoops; loop++)
            {
                for (int i = 0; i < CasinoRouletteHelper.EuropeanWheelOrder.Length; i++)
                {
                    int num = CasinoRouletteHelper.EuropeanWheelOrder[i];
                    var color = CasinoRouletteHelper.GetColorForNumber(num);
                    var chaosEvt = ChaosEventManager.RollEvent(chances);
                    var def = ChaosEventManager.GetDefinition(chaosEvt);
                    var polarity = def != null ? def.Polarity : ChaosPolarity.None;
                    _tapePockets.Add(new RoulettePocket(num, color, polarity, chaosEvt));
                }
            }
            _winningTapeIndex = (3 * CasinoRouletteHelper.EuropeanWheelOrder.Length);
            _winningPocket = _tapePockets[_winningTapeIndex];
        }

        public void Open()
        {
            _isWindowOpen = true;
            _confirmSpinPending = false;
            CaptureCursor();
            RefreshPlayerItems();
            UpdateSelectedCellsStake();
        }

        public void Close()
        {
            _isWindowOpen = false;
            _confirmSpinPending = false;
            _isPlacingLifeBet = false;
            _isPlacingItemBet = false;
            _isDraggingWindow = false;
            ReleaseCursor();

            try
            {
                var master = GetLocalMaster();
                if (master != null && master.playerCharacterMasterController != null)
                {
                    SuppressPlayerButtons(master.playerCharacterMasterController);
                }
            }
            catch { }
        }

        private CharacterMaster? GetLocalMaster() => PlayerUtil.GetLocalMaster();

        private string GetPlayerName(CharacterMaster? master)
        {
            if (master == null) return Localization.Get("Игрок", "Player");
            var body = master.GetBody();
            if (body != null && !string.IsNullOrEmpty(body.GetUserName())) return body.GetUserName();
            if (master.playerCharacterMasterController != null && master.playerCharacterMasterController.networkUser != null)
            {
                return master.playerCharacterMasterController.networkUser.userName;
            }
            return Localization.Get("Игрок", "Player");
        }

        private void RefreshPlayerItems()
        {
            _playerAvailableItems.Clear();
            var master = GetLocalMaster();
            if (master == null || master.inventory == null) return;

            foreach (var idx in master.inventory.itemAcquisitionOrder)
            {
                var def = ItemCatalog.GetItemDef(idx);
                if (def != null && !def.hidden && def.tier != ItemTier.NoTier && master.inventory.GetItemCountEffective(idx) > 0)
                {
                    _playerAvailableItems.Add(idx);
                }
            }

            if (_selectedItemIdxIndex >= _playerAvailableItems.Count)
            {
                _selectedItemIdxIndex = Mathf.Max(0, _playerAvailableItems.Count - 1);
            }
        }

        private bool ShouldDisplayUI()
        {
            if (Run.instance == null) return false;
            if (PauseManager.isPaused) return false;
            if (GameOverController.instance != null) return false;

            if (HUD.readOnlyInstanceList == null || HUD.readOnlyInstanceList.Count == 0) return false;
            var hud = HUD.readOnlyInstanceList[0];
            if (hud == null || !hud.gameObject.activeInHierarchy) return false;

            var master = GetLocalMaster();
            if (master == null || master.inventory == null) return false;

            return true;
        }

        private void Update()
        {
            if (Run.instance == null)
            {
                if (_isWindowOpen) Close();
                return;
            }

            // Toast countdown
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
            }

            // Block hotkeys and window controls while typing into chat, console, or input field
            if (GUIHelper.IsTypingOrInputBlocked())
            {
                return;
            }

            // Instant Hotkey Toggle
            if (Input.GetKeyDown(ModConfig.CasinoToggleHotkeyKey))
            {
                if (_isWindowOpen)
                {
                    Close();
                    PlaySound("Play_UI_menuClick");
                }
                else
                {
                    Open();
                    PlaySound("Play_UI_menuClick");
                }
            }

            // Close on ESC
            if (_isWindowOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    UiCommon.CloseOpenMenus();
                    return;
                }

                // Handle spin confirmation timeout
                if (_confirmSpinPending)
                {
                    _confirmTimer -= Time.unscaledDeltaTime;
                    if (_confirmTimer <= 0f)
                    {
                        _confirmSpinPending = false;
                    }
                }
            }

            // Reel spinning animation
            if (_reelState == ReelState.Spinning)
            {
                _spinTimer += Time.unscaledDeltaTime;
                float duration = Mathf.Max(1.0f, ModConfig.SpinDuration.Value);
                float progress = Mathf.Clamp01(_spinTimer / duration);

                // Smooth quintic ease-out deceleration
                float ease = 1f - Mathf.Pow(1f - progress, 4.4f);
                _currentScrollX = Mathf.Lerp(0f, _targetScrollX, ease);

                // Audio tick when crossing needle in viewport
                var hud = HUD.readOnlyInstanceList.Count > 0 ? HUD.readOnlyInstanceList[0] : null;
                if (hud != null)
                {
                    Rect boxRect = RouletteUI.GetInventoryFieldRect(hud);
                    float vpWidth = boxRect.width - 16f;
                    float needleTapeX = _currentScrollX + (vpWidth / 2f);
                    int crossedSlot = Mathf.FloorToInt(needleTapeX / SlotWidth);
                    if (crossedSlot != _lastTickedPocket)
                    {
                        _lastTickedPocket = crossedSlot;
                        PlaySound("Play_UI_button_hover");
                    }
                }

                if (progress >= 1f)
                {
                    FinishSpin();
                }
            }
            else if (_reelState == ReelState.Finished)
            {
                _finishDisplayTimer -= Time.unscaledDeltaTime;
                if (_finishDisplayTimer <= 0f)
                {
                    _reelState = ReelState.Idle;
                }
            }
        }

        private void LateUpdate()
        {
            // Cursor visibility and lockState are handled cleanly by RoR2 via MPEventSystem.cursorOpenerCount.
        }

        private void RequestSpin()
        {
            if (_reelState == ReelState.Spinning || _spinRequestInFlight) return;

            var master = GetLocalMaster();
            if (master == null) return;

            int totalMoney = _feltBets.GetTotalMoneyBet();
            int requiredLunarCoins = _feltBets.GetRequiredLunarCoins();
            uint playerLunarCoins = RouletteUI.GetLocalPlayerLunarCoins();

            bool hasLifeBet = _isLifeBetPlaced && ModConfig.IsLifeBetActive;
            bool hasMoneyBet = totalMoney > 0;
            bool hasItemBet = _isItemBetActive && _playerAvailableItems.Count > 0;

            if (!hasLifeBet && !hasMoneyBet && !hasItemBet)
            {
                _lastResultSummary = Localization.Get("<color=#FFAA00>Сделайте ставку на столе (деньгами, жизнью или предметом)!</color>", "<color=#FFAA00>Place a bet on the table (gold, life, or item)!</color>");
                ShowToast(Localization.Get("Сделайте ставку на столе!", "Place a bet on the table!"), new Color(1f, 0.7f, 0.2f), 3.0f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            if (hasMoneyBet && totalMoney < 25)
            {
                _lastResultSummary = Localization.Get("<color=#FF5555>Минимальная ставка в казино: $25!</color>", "<color=#FF5555>Minimum casino bet is $25!</color>");
                ShowToast(Localization.Get("Минимальная ставка в казино: $25!", "Minimum casino bet is $25!"), new Color(1f, 0.4f, 0.4f), 3.0f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            if (hasMoneyBet && master.money < totalMoney)
            {
                _lastResultSummary = Localization.Format("<color=#FF5555>Недостаточно золота! У вас ${0}, требуется ${1}.</color>", "<color=#FF5555>Not enough gold! You have ${0}, need ${1}.</color>", master.money, totalMoney);
                ShowToast(Localization.Get("Недостаточно золота для всех ставок!", "Not enough gold for all bets!"), new Color(1f, 0.4f, 0.4f), 3.0f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            if (requiredLunarCoins > 0 && playerLunarCoins < (uint)requiredLunarCoins)
            {
                ShowToast(Localization.Format("Недостаточно Лунных монет для мульти-ставок! (Требуется: {0} ☾, у вас: {1} ☾)", "Not enough Lunar Coins for multi-bets! (Need: {0} ☾, have: {1} ☾)", requiredLunarCoins, playerLunarCoins), new Color(0.4f, 0.8f, 1f), 3.5f);
                PlaySound("Play_UI_insufficient_funds");
                return;
            }

            if (hasItemBet)
            {
                if (_selectedItemIdxIndex >= 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
                {
                    ItemIndex chosenItem = _playerAvailableItems[_selectedItemIdxIndex];
                    if (master.inventory != null && master.inventory.GetItemCountEffective(chosenItem) <= 0)
                    {
                        _lastResultSummary = Localization.Get("<color=#FF5555>Выбранного предмета больше нет в инвентаре!</color>", "<color=#FF5555>Selected item is no longer in inventory!</color>");
                        RefreshPlayerItems();
                        return;
                    }
                }
            }

            // Two-step confirmation logic for Life Bet
            if (hasLifeBet)
            {
                if (!_confirmSpinPending)
                {
                    _confirmSpinPending = true;
                    _confirmTimer = 5.0f;
                    PlaySound("Play_heretic_laugh");
                    ShowToast(Localization.Get("⚠ СТАВКА ЖИЗНИ! Нажмите [ SPIN ] повторно для подтверждения!", "⚠ LIFE BET! Click [ SPIN ] again to confirm!"), new Color(1f, 0.3f, 0.3f), 4.0f);
                    return;
                }
            }

            // Launch Spin!
            _confirmSpinPending = false;
            StartSpin();
        }

        private void StartSpin()
        {
            var master = GetLocalMaster();
            if (master == null || _spinRequestInFlight) return;

            if (_isLifeBetPlaced && IsPlayerInDroneOrDeadMode(master))
            {
                _isLifeBetPlaced = false;
                ShowToast(Localization.Get("[ОШИБКА] Ставка жизнью недоступна в режиме дрона / спектатора!", "[ERROR] Life bet is not available in drone / spectator mode!"), new Color(1f, 0.4f, 0.4f), 3.0f);
                return;
            }

            string txId = Guid.NewGuid().ToString("N");
            _pendingSpinTxId = txId;
            _spinRequestInFlight = true;

            int chosenItemIdx = -1;
            if (_isItemBetActive && _playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
            {
                chosenItemIdx = (int)_playerAvailableItems[_selectedItemIdxIndex];
            }

            var req = new CasinoSpinRequestMessage
            {
                txId = txId,
                isLifeBet = _isLifeBetPlaced && ModConfig.IsLifeBetActive,
                itemIndex = chosenItemIdx,
                itemTargetNumber = _itemBetTargetNumber
            };

            if (req.isLifeBet)
            {
                req.betType = (byte)_lifeBetType;
                req.targetNumber = _lifeBetTarget;
                req.moneyBetAmount = 0;
            }
            else
            {
                var activeBets = _feltBets.GetAllActiveBets();
                if (activeBets.Count > 0)
                {
                    req.betType = (byte)activeBets[0].Type;
                    req.targetNumber = activeBets[0].TargetNumber;
                    req.moneyBetAmount = activeBets[0].Amount;

                    if (activeBets.Count > 1)
                    {
                        int extraCount = activeBets.Count - 1;
                        req.extraBetTypes = new byte[extraCount];
                        req.extraTargetNumbers = new int[extraCount];
                        req.extraBetAmounts = new int[extraCount];

                        for (int i = 0; i < extraCount; i++)
                        {
                            req.extraBetTypes[i] = (byte)activeBets[i + 1].Type;
                            req.extraTargetNumbers[i] = activeBets[i + 1].TargetNumber;
                            req.extraBetAmounts[i] = activeBets[i + 1].Amount;
                        }
                    }
                }
            }

            PlaySound("Play_UI_menuClick");
            CasinoServerService.SendCasinoSpinRequest(req);
        }

        private void HandleCasinoSpinResultReceived(CasinoSpinResultMessage result)
        {
            if (result == null || result.txId != _pendingSpinTxId) return;

            _spinRequestInFlight = false;
            _pendingSpinTxId = null;

            if (!result.success)
            {
                switch (result.rejectReason)
                {
                    case 1:
                        ShowToast(Localization.Get("Недостаточно золота для ставки!", "Not enough gold for bet!"), new Color(1f, 0.4f, 0.4f), 3.5f);
                        break;
                    case 2:
                        ShowToast(Localization.Get("Выбранного предмета нет в инвентаре!", "Selected item is missing!"), new Color(1f, 0.4f, 0.4f), 3.5f);
                        break;
                    case 3:
                        ShowToast(Localization.Get("Ставка жизнью недоступна в режиме дрона / спектатора!", "Life bet not available in drone / spectator mode!"), new Color(1f, 0.4f, 0.4f), 3.5f);
                        break;
                    case 5:
                        ShowToast(Localization.Get("Недостаточно Лунных монет для мульти-ставок!", "Not enough Lunar Coins for multi-bets!"), new Color(0.4f, 0.8f, 1f), 3.5f);
                        break;
                    default:
                        ShowToast(Localization.Get("Казино отключено на сервере!", "Casino disabled on server!"), new Color(1f, 0.4f, 0.4f), 3.5f);
                        break;
                }
                PlaySound("Play_UI_insufficient_funds");
                _reelState = ReelState.Idle;
                return;
            }

            _lastSpinResult = result;

            // Build continuous tape with Chaos sectors
            var master = GetLocalMaster();
            var chances = ChaosEventManager.CalculateChances(master, false);
            int wheelLength = CasinoRouletteHelper.EuropeanWheelOrder.Length;
            _tapePockets.Clear();

            int rolledWheelIndex = 0;
            for (int i = 0; i < wheelLength; i++)
            {
                if (CasinoRouletteHelper.EuropeanWheelOrder[i] == result.winningNumber)
                {
                    rolledWheelIndex = i;
                    break;
                }
            }

            for (int loop = 0; loop < TapeLoops; loop++)
            {
                for (int i = 0; i < wheelLength; i++)
                {
                    int num = CasinoRouletteHelper.EuropeanWheelOrder[i];
                    var color = CasinoRouletteHelper.GetColorForNumber(num);
                    var chaosEvt = (loop == 4 && i == rolledWheelIndex) ? (ChaosEventType)result.chaosEvent : ChaosEventManager.RollEvent(chances);
                    var def = ChaosEventManager.GetDefinition(chaosEvt);
                    var polarity = def != null ? def.Polarity : ChaosPolarity.None;
                    _tapePockets.Add(new RoulettePocket(num, color, polarity, chaosEvt));
                }
            }

            _winningTapeIndex = (4 * wheelLength) + rolledWheelIndex;
            _winningPocket = _tapePockets[_winningTapeIndex];

            // Calculate exact target scroll offset to center winning card under the needle
            var hud = HUD.readOnlyInstanceList.Count > 0 ? HUD.readOnlyInstanceList[0] : null;
            float vpWidth = 800f;
            if (hud != null)
            {
                Rect boxRect = RouletteUI.GetInventoryFieldRect(hud);
                vpWidth = boxRect.width - 16f;
            }
            float needleX = vpWidth / 2f;
            float slotCenter = (_winningTapeIndex * SlotWidth) + (CardWidth / 2f);
            _targetScrollX = slotCenter - needleX;

            _currentScrollX = 0f;
            _spinTimer = 0f;
            _lastTickedPocket = -1;
            _reelState = ReelState.Spinning;
            _lastResultSummary = "";
            _lastResultHeadline = "";

            PlaySound("Play_UI_menuClick");
        }

        private void FinishSpin()
        {
            if (_reelState == ReelState.Finished) return;
            _reelState = ReelState.Finished;
            _currentScrollX = _targetScrollX;
            _finishDisplayTimer = Mathf.Max(3.0f, ModConfig.PostSpinDisplayDuration.Value + 1.5f);

            var master = GetLocalMaster();
            if (master == null || _lastSpinResult == null) return;

            // Claim server award so money/death/chaos/item modifications trigger precisely upon wheel stop
            if (!string.IsNullOrEmpty(_lastSpinResult.txId))
            {
                CasinoServerService.SendCasinoClaimAwardRequest(_lastSpinResult.txId);
            }

            var result = _lastSpinResult;
            int rolled = result.winningNumber;
            var color = (RouletteColor)result.winningColor;
            string colorName = CasinoRouletteHelper.GetColorName(color);

            List<string> resultLines = new List<string>();
            bool overallWin = false;

            // 1. Process Life Bet OR Money Bet
            if (result.isLifeBet)
            {
                string betDetails = CasinoRouletteHelper.GetBetTypeName(_lifeBetType, _lifeBetTarget);
                if (result.lifeBetSurvived)
                {
                    resultLines.Add(Localization.Format("<color=#2ECC71>★★★ СТАВКА ЖИЗНИ ВЫИГРАЛА: +${0} ({1})! Жизнь сохранена! ★★★</color>", "<color=#2ECC71>★★★ LIFE BET WON: +${0} ({1})! Life spared! ★★★</color>", result.lifeBetJackpot, betDetails));
                    overallWin = true;
                    ShowToast(Localization.Format("[ЖИЗНЬ СОХРАНЕНА] +${0}! ({1})", "[LIFE SPARED] +${0}! ({1})", result.lifeBetJackpot, betDetails), new Color(0.25f, 0.95f, 0.45f), 5.0f);
                }
                else
                {
                    resultLines.Add(Localization.Format("<color=#FF3333>☠ СТАВКА ЖИЗНИ ПРОИГРАНА: Вы погибли ({0})! ☠</color>", "<color=#FF3333>☠ LIFE BET LOST: You perished ({0})! ☠</color>", betDetails));
                    overallWin = false;
                    ShowToast(Localization.Format("[СМЕРТЬ] Ставка жизни проиграна на {0}!", "[DEATH] Life bet lost on {0}!", betDetails), new Color(1f, 0.25f, 0.25f), 5.0f);
                    Close();
                }
            }
            else
            {
                if (result.wonMoney)
                {
                    resultLines.Add(Localization.Format("<color=#4ADE80>★ Ставки на столе ВЫИГРАЛИ: +${0}!</color>", "<color=#4ADE80>★ Table bets WON: +${0}!</color>", result.totalPayout));
                    overallWin = true;
                }
                else if (_feltBets.GetTotalMoneyBet() > 0)
                {
                    resultLines.Add(Localization.Format("<color=#FF6666>☠ Ставки деньгами проиграли: -${0}.</color>", "<color=#FF6666>☠ Table bets lost: -${0}.</color>", _feltBets.GetTotalMoneyBet()));
                }
            }

            // 2. Process Item Bet on Chosen Number
            if (_isItemBetActive && _playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
            {
                ItemIndex itemIdx = _playerAvailableItems[_selectedItemIdxIndex];
                string itemName = ChaosEventManager.GetItemName(itemIdx);

                if (result.duplicatedItemIndex >= 0)
                {
                    try
                    {
                        var pIndex = PickupCatalog.FindPickupIndex((ItemIndex)result.duplicatedItemIndex);
                        if (pIndex != PickupIndex.none)
                        {
                            CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pIndex, isRental = false });
                        }
                    }
                    catch { }

                    resultLines.Add(Localization.Format("<color=#2ECC71>★★★ ДЖЕКПОТ НА ЧИСЛЕ {0}! Предмет <b>{1}</b> УДВОЕН (+1 копия)! ★★★</color>", "<color=#2ECC71>★★★ JACKPOT ON NUMBER {0}! Item <b>{1}</b> DOUBLED (+1 copy)! ★★★</color>", _itemBetTargetNumber, itemName));
                    overallWin = true;
                }
                else
                {
                    resultLines.Add(Localization.Format("<color=#FF4444>☠ Ставка предметом сгорела: потеряна 1 копия <b>{0}</b> (выпало {1}, ставка была на {2}).</color>", "<color=#FF4444>☠ Item bet lost: lost 1 copy of <b>{0}</b> (rolled {1}, bet on {2}).</color>", itemName, rolled, _itemBetTargetNumber));
                }
                RefreshPlayerItems();
            }

            // 3. Process Chaos Event on Winning Pocket
            if (ModConfig.IsChaosSystemActive && (ChaosEventType)result.chaosEvent != ChaosEventType.None)
            {
                var def = ChaosEventManager.GetDisplayDefinition((ChaosEventType)result.chaosEvent) ?? ChaosEventManager.GetDefinition((ChaosEventType)result.chaosEvent);
                if (def != null)
                {
                    string polarityHex = def.Polarity == ChaosPolarity.Good ? "#4ADE80" : "#F87171";
                    string polarityLabel = def.Polarity == ChaosPolarity.Good ? Localization.Get("УДАЧА", "FORTUNE") : Localization.Get("БЕДСТВИЕ", "CALAMITY");

                    resultLines.Add($"<color={polarityHex}><b>[{polarityLabel}]</b> {def.Title}: {def.Description}</color>");
                    ShowToast($"[{polarityLabel}: {def.Title}] {def.Description}", def.Color, 4.0f);

                    if (def.Polarity == ChaosPolarity.Good)
                    {
                        overallWin = true;
                    }
                }
            }

            _lastResultHeadline = Localization.Format("Выпало: [{0} {1}]", "Rolled: [{0} {1}]", rolled, colorName);
            _lastResultSummary = string.Join("\n", resultLines);
            _lastResultColor = overallWin ? new Color(0.35f, 0.95f, 0.50f) : new Color(0.95f, 0.40f, 0.40f);

            // Audio fanfare
            if (overallWin)
            {
                PlaySound("Play_UI_shrine_gamble_win");
            }
            else
            {
                PlaySound("Play_heretic_laugh");
            }
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
            if (_winHeaderStyle != null) return;

            _winHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };

            _labelBoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = Color.white }
            };

            _labelSmallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.75f, 0.78f, 0.85f) }
            };

            _pocketNumStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _pocketSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _spinBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _spinConfirmBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _topLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.95f) }
            };

            _questionMarkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
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

            _feltNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _feltHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _feltBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            _dragHandleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.40f, 0.85f, 0.95f) }
            };

            _unifiedBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
        }

        private void DrawToastNotification(Rect boxRect)
        {
            if (_toastTimer <= 0f || string.IsNullOrEmpty(_toastMessage)) return;

            float alpha = Mathf.Clamp01(_toastTimer / 0.5f);
            float toastWidth = Mathf.Min(boxRect.width, 560f);
            float toastHeight = 24f;
            float toastX = boxRect.x + (boxRect.width - toastWidth) / 2f;
            float toastY = boxRect.yMax + 6f;
            Rect toastRect = new Rect(toastX, toastY, toastWidth, toastHeight);

            Color bg = new Color(0.06f, 0.08f, 0.12f, 0.92f * alpha);
            Color border = new Color(_toastColor.r, _toastColor.g, _toastColor.b, 0.95f * alpha);
            Color textCol = new Color(_toastColor.r, _toastColor.g, _toastColor.b, alpha);

            GUIHelper.DrawRect(toastRect, bg);
            GUIHelper.DrawBorder(toastRect, border, 1);

            _toastStyle!.normal.textColor = textCol;
            GUI.Label(toastRect, _toastMessage, _toastStyle);
        }

        private void OnGUI()
        {
            try
            {
                if (!ShouldDisplayUI()) return;

                InitStyles();

                var hud = HUD.readOnlyInstanceList[0];

                // 1. Draw Top Reel Tape in the exact same spot and styling as Item Roulette
                if (_reelState == ReelState.Spinning || _reelState == ReelState.Finished)
                {
                    DrawTopReelBanner(hud);
                }

                // 2. Draw Draggable Betting Window
                if (_isWindowOpen)
                {
                    bool prevEnabled = GUI.enabled;
                    if (GUIHelper.IsTypingOrInputBlocked())
                    {
                        GUI.enabled = false;
                    }
                    try
                    {
                        DrawCasinoTableWindow();
                    }
                    finally
                    {
                        GUI.enabled = prevEnabled;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in CasinoRouletteUI.OnGUI: {ex}");
            }
        }

        // =========================================================================
        // TOP REEL BANNER (In HUD Inventory Box)
        // =========================================================================
        private void DrawTopReelBanner(HUD hud)
        {
            using (GUIHelper.UseScale(GUIHelper.SafeRouletteScale, new Vector2(Screen.width / 2f, 0f)))
            {
                Rect boxRect = RouletteUI.GetInventoryFieldRect(hud);

            // Field Background & Border (identical to Item Roulette)
            GUIHelper.DrawRect(boxRect, new Color(0.07f, 0.08f, 0.11f, 0.96f));
            GUIHelper.DrawBorder(boxRect, new Color(0.26f, 0.29f, 0.38f, 0.92f), 1);

            // Header Line
            Rect headerRect = new Rect(boxRect.x, boxRect.y + 2f, boxRect.width, 16f);
            string headerText;
            if (_reelState == ReelState.Spinning)
            {
                if (_isLifeBetPlaced && ModConfig.EnableLifeBet.Value)
                {
                    string betDetails = CasinoRouletteHelper.GetBetTypeName(_lifeBetType, _lifeBetTarget);
                    var (lifePrice, _, _) = CasinoRouletteHelper.CalculateLifeBetPrice(GetLocalMaster());
                    headerText = Localization.Format("РУЛЕТКА: ВРАЩЕНИЕ...   |   ☠ СТАВКА ЖИЗНИ: ${0} на [{1}] (СМЕРТЬ ПРИ ПОРАЖЕНИИ!)", "ROULETTE: SPINNING...   |   ☠ LIFE BET: ${0} on [{1}] (DEATH ON LOSS!)", lifePrice, betDetails);
                    _topLabelStyle!.normal.textColor = new Color(1f, 0.35f, 0.35f);
                }
                else
                {
                    int totalMoney = _feltBets.GetTotalMoneyBet();
                    string moneyStr = totalMoney > 0 ? $"${totalMoney}" : "";
                    string itemStr = _isItemBetActive && _playerAvailableItems.Count > 0
                        ? Localization.Format(" | Предмет: {0} (на #{1})", " | Item: {0} (on #{1})", ChaosEventManager.GetItemName(_playerAvailableItems[_selectedItemIdxIndex]), _itemBetTargetNumber)
                        : "";
                    headerText = Localization.Format("РУЛЕТКА: ВРАЩЕНИЕ...   |   Ставка: {0}{1}", "ROULETTE: SPINNING...   |   Bet: {0}{1}", moneyStr, itemStr);
                    _topLabelStyle!.normal.textColor = new Color(1f, 0.85f, 0.35f);
                }
            }
            else
            {
                int rolled = _winningPocket.Number;
                var color = _winningPocket.Color;
                string colName = CasinoRouletteHelper.GetColorName(color);
                string hexCol = color == RouletteColor.Green ? "#2ECC71" : (color == RouletteColor.Red ? "#E74C3C" : "#AAB7B8");
                string shortResult = _lastResultSummary.Replace("\n", "  |  ");
                headerText = Localization.Format("РУЛЕТКА: ВЫПАЛО <color={0}><b>[{1} {2}]</b></color>!   {3}", "ROULETTE: WON <color={0}><b>[{1} {2}]</b></color>!   {3}", hexCol, rolled, colName, shortResult);
                _topLabelStyle!.normal.textColor = _lastResultColor;
            }
            GUI.Label(headerRect, headerText, _topLabelStyle);

            // Viewport Reel
            float vpY = boxRect.y + 20f;
            float vpH = Mathf.Max(CardHeight + 4f, boxRect.height - 26f);
            float vpX = boxRect.x + 8f;
            float vpW = boxRect.width - 16f;
            Rect viewportRect = new Rect(vpX, vpY, vpW, vpH);

            GUIHelper.DrawRect(viewportRect, new Color(0.03f, 0.04f, 0.06f, 0.95f));

            // Scissored Tape
            GUI.BeginGroup(viewportRect);
            float cardY = (vpH - CardHeight) / 2f;
            for (int i = 0; i < _tapePockets.Count; i++)
            {
                float cardX = (i * SlotWidth) - _currentScrollX;
                if (cardX + CardWidth < -10f || cardX > vpW + 10f) continue;

                Rect cardRect = new Rect(cardX, cardY, CardWidth, CardHeight);
                var pocket = _tapePockets[i];
                bool isWinningSlot = (_reelState == ReelState.Finished && i == _winningTapeIndex);
                bool isChaos = pocket.ChaosPolarity != ChaosPolarity.None;

                Color bgCol;
                Color borderCol;
                string subtitle;
                Color subtitleCol;

                if (pocket.Number == 0)
                {
                    bgCol = new Color(0.04f, 0.22f, 0.10f, 0.95f); // Deep emerald
                    borderCol = new Color(0.18f, 0.85f, 0.44f, 0.95f);
                    subtitle = Localization.Get("ЗЕРО", "ZERO");
                    subtitleCol = new Color(0.20f, 0.95f, 0.50f);
                }
                else if (pocket.Color == RouletteColor.Red)
                {
                    bgCol = new Color(0.26f, 0.05f, 0.07f, 0.95f); // Deep crimson
                    borderCol = new Color(0.92f, 0.25f, 0.25f, 0.90f);
                    subtitle = Localization.Get("КРАСНОЕ", "RED");
                    subtitleCol = new Color(0.95f, 0.45f, 0.45f);
                }
                else
                {
                    bgCol = new Color(0.09f, 0.10f, 0.14f, 0.95f); // Sleek slate
                    borderCol = new Color(0.45f, 0.50f, 0.60f, 0.85f);
                    subtitle = Localization.Get("ЧЁРНОЕ", "BLACK");
                    subtitleCol = new Color(0.70f, 0.75f, 0.85f);
                }

                // 1. Background
                GUIHelper.DrawRect(cardRect, bgCol);

                // 2. Chaos Corner Wedge (rendered under the text so numbers/subtitles are never occluded)
                if (isChaos)
                {
                    Color topRightBg = pocket.ChaosPolarity == ChaosPolarity.Good
                        ? new Color(0.10f, 0.35f, 0.16f, 1.0f) // Solid Emerald
                        : new Color(0.40f, 0.08f, 0.12f, 1.0f); // Solid Crimson

                    Color dividerCol = pocket.ChaosPolarity == ChaosPolarity.Good
                        ? new Color(0.35f, 0.98f, 0.45f, 1.0f) // Crisp bright emerald divider
                        : new Color(0.98f, 0.30f, 0.35f, 1.0f); // Crisp bright crimson divider

                    Texture2D overlayTex = GUIHelper.CreateDiagonalOverlayTexture(94, 94, topRightBg, dividerCol, 2, 0.72f);
                    GUI.DrawTexture(cardRect, overlayTex);

                    // Tilted Question Mark '?' inside the top-right corner wedge
                    float qW = 22f;
                    float qH = 22f;
                    float qX = cardRect.x + 67f;
                    float qY = cardRect.y + 7f;
                    Rect qMarkRect = new Rect(qX, qY, qW, qH);

                    Color qColor = pocket.ChaosPolarity == ChaosPolarity.Good
                        ? new Color(1f, 0.92f, 0.25f, 1f)
                        : new Color(1f, 0.35f, 0.38f, 1f);

                    Color outlineColor = pocket.ChaosPolarity == ChaosPolarity.Good
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

                // 3. Number & Subtitle (Always centered across card width, perfectly aligned with the entire reel!)
                Rect numRect = new Rect(cardRect.x, cardRect.y + 14f, cardRect.width, 36f);
                _pocketNumStyle!.normal.textColor = Color.white;
                GUI.Label(numRect, pocket.Label, _pocketNumStyle);

                Rect subRect = new Rect(cardRect.x, cardRect.y + 56f, cardRect.width, 18f);
                _pocketSubStyle!.normal.textColor = subtitleCol;
                GUI.Label(subRect, subtitle, _pocketSubStyle);

                // 4. Accent Bar at bottom
                Rect accentBar = new Rect(cardRect.x, cardRect.yMax - 3f, cardRect.width, 3f);
                GUIHelper.DrawRect(accentBar, borderCol);

                // 5. If pocket is the item target and player bet an item, draw item icon badge in top-left!
                if (pocket.Number == _itemBetTargetNumber && _isItemBetActive && _playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
                {
                    ItemIndex betItem = _playerAvailableItems[_selectedItemIdxIndex];
                    var (sprite, tex, uv) = ItemPoolHelper.ResolveIcon(PickupCatalog.FindPickupIndex(betItem), null);
                    if (tex != null)
                    {
                        Rect badgeRect = new Rect(cardRect.x + 4f, cardRect.y + 4f, 22f, 22f);
                        GUIHelper.DrawRect(new Rect(badgeRect.x - 1f, badgeRect.y - 1f, badgeRect.width + 2f, badgeRect.height + 2f), new Color(0f, 0f, 0f, 0.85f));
                        GUI.DrawTextureWithTexCoords(badgeRect, tex, uv);
                    }
                }

                // 6. Highlight winning slot or chaos glow border
                if (isWinningSlot)
                {
                    GUIHelper.DrawBorder(cardRect, new Color(1f, 0.85f, 0.15f, 1f), 3);
                }
                else if (isChaos)
                {
                    Color chaosBorder = pocket.ChaosPolarity == ChaosPolarity.Good
                        ? new Color(0.25f, 0.85f, 0.35f, 0.9f)
                        : new Color(0.95f, 0.25f, 0.30f, 0.9f);
                    GUIHelper.DrawBorder(cardRect, chaosBorder, 1);
                }
                else
                {
                    GUIHelper.DrawBorder(cardRect, borderCol, 1);
                }
            }
            GUI.EndGroup();

            // Center Golden Needle Marker
            float needleX = vpX + (vpW / 2f);
            UiCommon.DrawCenterNeedle(needleX, vpY, vpH);

            // Draw Toast Notification under inventory box if active
            DrawToastNotification(boxRect);
            }
        }

        // =========================================================================
        // DRAGGABLE CASINO TABLE WINDOW
        // =========================================================================
        private void EnsureWindowPosition()
        {
            if (_isWindowPosInitialized) return;

            float scale = GUIHelper.SafeWindowsScale;
            float windowW = 948f;
            float windowH = 330f;
            float visualW = windowW * scale;
            float visualH = windowH * scale;
            float x = ModConfig.CasinoWindowPosX != null ? ModConfig.CasinoWindowPosX.Value : -1;
            float y = ModConfig.CasinoWindowPosY != null ? ModConfig.CasinoWindowPosY.Value : -1;

            if (x < 0 || y < 0)
            {
                x = Mathf.Max(10f, (Screen.width - visualW) / 2f);
                y = Mathf.Max(60f, Screen.height - visualH - 50f);
            }

            x = Mathf.Clamp(x, 0, Mathf.Max(0, Screen.width - visualW));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, Screen.height - visualH));

            _windowRect = new Rect(x, y, windowW, windowH);
            _isWindowPosInitialized = true;
        }

        private void HandleWindowDrag(Rect dragHandleRect)
        {
            var ev = Event.current;
            Vector2 currentMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (ev.type == EventType.MouseDown && ev.button == 0 && dragHandleRect.Contains(ev.mousePosition))
            {
                _isDraggingWindow = true;
                _dragMouseStart = currentMouse;
                _dragWindowStart = _windowRect.position;
                ev.Use();
            }
            else if (_isDraggingWindow && Input.GetMouseButton(0))
            {
                float scale = GUIHelper.SafeWindowsScale;
                float visualW = _windowRect.width * scale;
                float visualH = _windowRect.height * scale;
                Vector2 offset = currentMouse - _dragMouseStart;
                float newX = Mathf.Clamp(_dragWindowStart.x + offset.x, 0, Mathf.Max(0, Screen.width - visualW));
                float newY = Mathf.Clamp(_dragWindowStart.y + offset.y, 0, Mathf.Max(0, Screen.height - visualH));
                _windowRect.position = new Vector2(newX, newY);
                if (ev.type == EventType.MouseDrag)
                {
                    ev.Use();
                }
            }
            else if (_isDraggingWindow && (!Input.GetMouseButton(0) || ev.type == EventType.MouseUp))
            {
                _isDraggingWindow = false;
                if (ModConfig.CasinoWindowPosX != null) ModConfig.CasinoWindowPosX.Value = Mathf.RoundToInt(_windowRect.x);
                if (ModConfig.CasinoWindowPosY != null) ModConfig.CasinoWindowPosY.Value = Mathf.RoundToInt(_windowRect.y);
                if (ev.type == EventType.MouseUp)
                {
                    ev.Use();
                }
            }
        }

        private bool DrawUnifiedButton(
            Rect rect,
            string text,
            Color baseBg,
            Color baseBorder,
            Color textColor,
            int fontSize = 11,
            FontStyle fontStyle = FontStyle.Bold,
            bool isEnabled = true,
            bool pulse = false,
            Color? pulseBorder = null)
        {
            bool hover = isEnabled && rect.Contains(Event.current.mousePosition);
            Color bg = baseBg;
            Color border = baseBorder;

            if (pulse)
            {
                float p = 0.85f + Mathf.Sin(Time.unscaledTime * 8f) * 0.15f;
                bg = new Color(baseBg.r * p, baseBg.g * p, baseBg.b * p, baseBg.a);
                if (pulseBorder.HasValue)
                {
                    border = pulseBorder.Value;
                }
            }
            else if (hover)
            {
                bg = Color.Lerp(baseBg, Color.white, 0.14f);
                border = Color.Lerp(baseBorder, Color.white, 0.28f);
            }
            else if (!isEnabled)
            {
                bg = new Color(0.12f, 0.14f, 0.18f, 0.70f);
                border = new Color(0.24f, 0.28f, 0.35f, 0.50f);
                textColor = new Color(0.45f, 0.48f, 0.55f, 0.80f);
            }

            GUIHelper.DrawRect(rect, bg);
            GUIHelper.DrawBorder(rect, border, 1);

            if (_unifiedBtnStyle != null)
            {
                _unifiedBtnStyle.fontSize = fontSize;
                _unifiedBtnStyle.fontStyle = fontStyle;
                _unifiedBtnStyle.normal.textColor = textColor;
                GUI.Label(rect, text, _unifiedBtnStyle);
            }

            if (!isEnabled) return false;

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawCasinoTableWindow()
        {
            EnsureWindowPosition();

            float scale = GUIHelper.SafeWindowsScale;
            using (GUIHelper.UseScale(scale, _windowRect.position))
            {
                // Window Background & Gold Border
                GUIHelper.DrawRect(_windowRect, new Color(0.06f, 0.08f, 0.12f, 0.96f));
                GUIHelper.DrawBorder(_windowRect, new Color(0.85f, 0.70f, 0.25f, 0.95f), 1);

                // 1. Header Bar: Title on Left, Drag Button and Close Button on Right
                Rect titleRect = new Rect(_windowRect.x + 12f, _windowRect.y + 6f, _windowRect.width - 90f, 22f);
                GUI.Label(titleRect, Localization.Get("★ СТОЛ КАЗИНО • СТАВКИ ★", "★ CASINO TABLE • BETTING ★"), _winHeaderStyle);

                // Drag Button [ ✥ ] on the right
                Rect dragBtnRect = new Rect(_windowRect.xMax - 62f, _windowRect.y + 5f, 26f, 22f);
                HandleWindowDrag(dragBtnRect);

                bool isDragHover = dragBtnRect.Contains(Event.current.mousePosition);
                Color dragBg = _isDraggingWindow
                    ? new Color(0.20f, 0.45f, 0.70f, 0.95f)
                    : (isDragHover ? new Color(0.22f, 0.28f, 0.38f, 0.95f) : new Color(0.14f, 0.17f, 0.24f, 0.92f));
                Color dragBorder = _isDraggingWindow
                    ? new Color(0.40f, 0.85f, 0.95f, 1f)
                    : (isDragHover ? new Color(0.45f, 0.80f, 0.95f, 0.90f) : new Color(0.38f, 0.45f, 0.58f, 0.80f));
                Color dragIconCol = _isDraggingWindow
                    ? Color.white
                    : (isDragHover ? new Color(0.40f, 0.85f, 0.95f, 1f) : new Color(0.55f, 0.75f, 0.88f, 0.90f));

                DrawUnifiedButton(dragBtnRect, "✥", dragBg, dragBorder, dragIconCol, 14, FontStyle.Bold);

                // Close Button [ ✕ ] on the right
                Rect closeBtnRect = new Rect(_windowRect.xMax - 32f, _windowRect.y + 5f, 24f, 22f);
                bool isCloseHover = closeBtnRect.Contains(Event.current.mousePosition);
                Color closeBg = isCloseHover ? new Color(0.70f, 0.18f, 0.22f, 0.95f) : new Color(0.14f, 0.16f, 0.22f, 0.90f);
                Color closeBorder = isCloseHover ? new Color(1f, 0.45f, 0.50f, 1f) : new Color(0.42f, 0.35f, 0.40f, 0.75f);
                Color closeTextCol = isCloseHover ? Color.white : new Color(0.85f, 0.85f, 0.85f);

                if (DrawUnifiedButton(closeBtnRect, "✕", closeBg, closeBorder, closeTextCol, 12, FontStyle.Bold))
                {
                    Close();
                    PlaySound("Play_UI_menuClick");
                }

                // Divider Line
                GUIHelper.DrawRect(new Rect(_windowRect.x + 10f, _windowRect.y + 32f, _windowRect.width - 20f, 1f), new Color(0.25f, 0.28f, 0.38f, 0.8f));

                // 2. The Felt (Left Panel)
                float feltX = _windowRect.x + 12f;
                float feltY = _windowRect.y + 38f;
                DrawCasinoFelt(feltX, feltY);

                // 3. The Sidebar (Right Panel)
                float sideX = _windowRect.x + 682f;
                float sideY = _windowRect.y + 38f;
                float sideW = 254f;
                DrawCasinoSidebar(sideX, sideY, sideW);

                // Draw top cursor if mouse is over window
                GUIHelper.DrawTopCursor(_windowRect, scale, _windowRect.position);

                // Consume any unhandled mouse clicks inside window bounds to prevent IMGUI event bleeding
                var ev = Event.current;
                if (ev != null && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp) && _windowRect.Contains(ev.mousePosition))
                {
                    ev.Use();
                }
            }
        }

        // =========================================================================
        // CASINO FELT (Roulette Betting Grid)
        // =========================================================================
        private void DrawCasinoFelt(float startX, float startY)
        {
            float zeroW = 46f;
            float cellW = 45f;
            float cellH = 37f;
            float spacing = 2f;
            float zeroH = (cellH * 3f) + (spacing * 2f);

            // 1. Zero (0) - Emerald Green Wedge
            Rect zeroRect = new Rect(startX, startY, zeroW, zeroH);
            bool zeroHover = zeroRect.Contains(Event.current.mousePosition);
            Color zeroBg = zeroHover ? new Color(0.18f, 0.70f, 0.35f, 0.98f) : new Color(0.12f, 0.55f, 0.28f, 0.95f);
            Color zeroBorder = (_isItemBetActive && _itemBetTargetNumber == 0)
                ? new Color(0.3f, 1f, 0.5f, 1f)
                : (_feltBets.NumberBets[0] > 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.85f, 0.70f, 0.25f, 0.9f));

            GUIHelper.DrawRect(zeroRect, zeroBg);
            GUIHelper.DrawBorder(zeroRect, zeroBorder, 1);
            GUI.Label(zeroRect, "<color=#FFFFFF><b>0</b></color>", _pocketNumStyle ?? _feltNumberStyle);

            if (HandleFeltCellClick(zeroRect, out int zeroBtn))
            {
                OnFeltCellClicked(CasinoBetType.StraightNumber, 0, zeroBtn);
            }

            if (_feltBets.NumberBets[0] > 0)
            {
                DrawChipBadge(zeroRect, _feltBets.NumberBets[0]);
            }
            if (_isLifeBetPlaced && _lifeBetType == CasinoBetType.StraightNumber && _lifeBetTarget == 0)
            {
                DrawLifeBadge(zeroRect);
            }
            if (_isItemBetActive && _itemBetTargetNumber == 0 && _playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
            {
                DrawItemBadge(zeroRect, _playerAvailableItems[_selectedItemIdxIndex]);
            }

            // 2. Numbers Grid 1 to 36 (3 Rows x 12 Columns)
            float gridX = startX + zeroW + spacing;
            for (int col = 0; col < 12; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    int num = 0;
                    if (row == 0) num = (col + 1) * 3;
                    else if (row == 1) num = (col * 3) + 2;
                    else num = (col * 3) + 1;

                    float cX = gridX + (col * (cellW + spacing));
                    float cY = startY + (row * (cellH + spacing));
                    Rect cellRect = new Rect(cX, cY, cellW, cellH);

                    bool hover = cellRect.Contains(Event.current.mousePosition);
                    bool isRed = CasinoRouletteHelper.RedNumbers.Contains(num);
                    Color baseCol = isRed ? new Color(0.78f, 0.15f, 0.15f, 0.95f) : new Color(0.14f, 0.16f, 0.20f, 0.95f);
                    if (hover)
                    {
                        baseCol = isRed ? new Color(0.92f, 0.25f, 0.25f, 0.98f) : new Color(0.24f, 0.28f, 0.36f, 0.98f);
                    }

                    Color cellBorder = (_isItemBetActive && _itemBetTargetNumber == num)
                        ? new Color(0.3f, 1f, 0.5f, 1f)
                        : (_feltBets.NumberBets[num] > 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.40f, 0.45f, 0.55f, 0.8f));

                    GUIHelper.DrawRect(cellRect, baseCol);
                    GUIHelper.DrawBorder(cellRect, cellBorder, 1);
                    GUI.Label(cellRect, num.ToString(), _feltNumberStyle);

                    if (HandleFeltCellClick(cellRect, out int clickBtn))
                    {
                        OnFeltCellClicked(CasinoBetType.StraightNumber, num, clickBtn);
                    }

                    if (_feltBets.NumberBets[num] > 0)
                    {
                        DrawChipBadge(cellRect, _feltBets.NumberBets[num]);
                    }
                    if (_isLifeBetPlaced && _lifeBetType == CasinoBetType.StraightNumber && _lifeBetTarget == num)
                    {
                        DrawLifeBadge(cellRect);
                    }
                    if (_isItemBetActive && _itemBetTargetNumber == num && _playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
                    {
                        DrawItemBadge(cellRect, _playerAvailableItems[_selectedItemIdxIndex]);
                    }
                }
            }

            // 3. Column 2:1 Boxes (Right of Numbers)
            float totalGridW = (12f * (cellW + spacing)) - spacing;
            float colBoxX = gridX + totalGridW + spacing;
            float colBoxW = 42f;

            CasinoBetType[] colTypes = new CasinoBetType[] { CasinoBetType.Column3, CasinoBetType.Column2, CasinoBetType.Column1 };
            int[] colIndices = new int[] { 2, 1, 0 };

            for (int row = 0; row < 3; row++)
            {
                float cY = startY + (row * (cellH + spacing));
                Rect colRect = new Rect(colBoxX, cY, colBoxW, cellH);

                bool hover = colRect.Contains(Event.current.mousePosition);
                int colIdx = colIndices[row];
                CasinoBetType bType = colTypes[row];
                int betAmt = _feltBets.ColumnBets[colIdx];

                Color bg = hover ? new Color(0.24f, 0.30f, 0.42f, 0.95f) : new Color(0.16f, 0.19f, 0.26f, 0.92f);
                Color border = betAmt > 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.40f, 0.45f, 0.55f, 0.8f);

                GUIHelper.DrawRect(colRect, bg);
                GUIHelper.DrawBorder(colRect, border, 1);
                GUI.Label(colRect, "2:1", _feltHeaderStyle);

                if (HandleFeltCellClick(colRect, out int colBtn))
                {
                    OnFeltCellClicked(bType, 0, colBtn);
                }

                if (betAmt > 0)
                {
                    DrawChipBadge(colRect, betAmt);
                }
                if (_isLifeBetPlaced && _lifeBetType == bType)
                {
                    DrawLifeBadge(colRect);
                }
            }

            // 4. Dozens (1st 12, 2nd 12, 3rd 12)
            float dozY = startY + (3f * (cellH + spacing)) + 4f;
            float dozW = (totalGridW - (2f * spacing)) / 3f;
            float dozH = 26f;

            CasinoBetType[] dozTypes = new CasinoBetType[] { CasinoBetType.Dozen1_12, CasinoBetType.Dozen13_24, CasinoBetType.Dozen25_36 };
            string[] dozLabels = new string[] { Localization.Get("1st 12 (1-12)", "1st 12 (1-12)"), Localization.Get("2nd 12 (13-24)", "2nd 12 (13-24)"), Localization.Get("3rd 12 (25-36)", "3rd 12 (25-36)") };

            for (int i = 0; i < 3; i++)
            {
                float dX = gridX + (i * (dozW + spacing));
                Rect dozRect = new Rect(dX, dozY, dozW, dozH);

                bool hover = dozRect.Contains(Event.current.mousePosition);
                int betAmt = _feltBets.DozenBets[i];
                CasinoBetType bType = dozTypes[i];

                Color bg = hover ? new Color(0.22f, 0.28f, 0.38f, 0.95f) : new Color(0.14f, 0.17f, 0.24f, 0.92f);
                Color border = betAmt > 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.38f, 0.42f, 0.52f, 0.8f);

                GUIHelper.DrawRect(dozRect, bg);
                GUIHelper.DrawBorder(dozRect, border, 1);
                GUI.Label(dozRect, dozLabels[i], _feltHeaderStyle);

                if (HandleFeltCellClick(dozRect, out int dozBtn))
                {
                    OnFeltCellClicked(bType, 0, dozBtn);
                }

                if (betAmt > 0)
                {
                    DrawChipBadge(dozRect, betAmt);
                }
                if (_isLifeBetPlaced && _lifeBetType == bType)
                {
                    DrawLifeBadge(dozRect);
                }
            }

            // 5. Outside Bets (1-18, EVEN, RED, BLACK, ODD, 19-36)
            float outY = dozY + dozH + spacing;
            float outW = (totalGridW - (5f * spacing)) / 6f;
            float outH = 26f;

            CasinoBetType[] outTypes = new CasinoBetType[]
            {
                CasinoBetType.Low1_18,
                CasinoBetType.Even,
                CasinoBetType.Red,
                CasinoBetType.Black,
                CasinoBetType.Odd,
                CasinoBetType.High19_36
            };

            string[] outLabels = new string[]
            {
                "1 - 18",
                Localization.Get("ЧЁТ", "EVEN"),
                Localization.Get("КРАСНОЕ", "RED"),
                Localization.Get("ЧЁРНОЕ", "BLACK"),
                Localization.Get("НЕЧЁТ", "ODD"),
                "19 - 36"
            };

            for (int i = 0; i < 6; i++)
            {
                float oX = gridX + (i * (outW + spacing));
                Rect outRect = new Rect(oX, outY, outW, outH);

                bool hover = outRect.Contains(Event.current.mousePosition);
                CasinoBetType bType = outTypes[i];
                int betAmt = GetBetAmount(bType, 0);

                Color bg;
                if (bType == CasinoBetType.Red)
                {
                    bg = hover ? new Color(0.85f, 0.20f, 0.20f, 0.95f) : new Color(0.70f, 0.12f, 0.12f, 0.92f);
                }
                else if (bType == CasinoBetType.Black)
                {
                    bg = hover ? new Color(0.22f, 0.24f, 0.30f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.92f);
                }
                else
                {
                    bg = hover ? new Color(0.22f, 0.28f, 0.38f, 0.95f) : new Color(0.14f, 0.17f, 0.24f, 0.92f);
                }

                Color border = betAmt > 0 ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.38f, 0.42f, 0.52f, 0.8f);

                GUIHelper.DrawRect(outRect, bg);
                GUIHelper.DrawBorder(outRect, border, 1);
                GUI.Label(outRect, outLabels[i], _feltHeaderStyle);

                if (HandleFeltCellClick(outRect, out int outBtn))
                {
                    OnFeltCellClicked(bType, 0, outBtn);
                }

                if (betAmt > 0)
                {
                    DrawChipBadge(outRect, betAmt);
                }
                if (_isLifeBetPlaced && _lifeBetType == bType)
                {
                    DrawLifeBadge(outRect);
                }
            }

            // 6. Felt Status / Hints Bar
            float hintY = outY + outH + 6f;
            float totalFeltW = colBoxX + colBoxW - startX;
            Rect hintRect = new Rect(startX, hintY, totalFeltW, 20f);

            if (_isPlacingLifeBet)
            {
                GUI.Label(hintRect, Localization.Get("<color=#FF4444>☠ ВЫБЕРИТЕ СЕКТОР НА СТОЛЕ ДЛЯ СТАВКИ ЖИЗНИ ☠ (Кликните на число, цвет или дюжину)</color>", "<color=#FF4444>☠ SELECT TABLE SECTOR FOR LIFE BET ☠ (Click on number, color or dozen)</color>"), _feltHeaderStyle);
            }
            else if (_isPlacingItemBet)
            {
                GUI.Label(hintRect, Localization.Get("<color=#2ECC71>📦 ВЫБЕРИТЕ ЧИСЛО НА СТОЛЕ (0-36) ДЛЯ СТАВКИ ПРЕДМЕТА 📦</color>", "<color=#2ECC71>📦 SELECT A NUMBER (0-36) ON FELT FOR ITEM BET 📦</color>"), _feltHeaderStyle);
            }
            else
            {
                int numCount = _feltBets.GetNumberBetsCount();
                if (numCount == 0 && !_feltBets.HasAnyOutsideBet())
                {
                    GUI.Label(hintRect, Localization.Get("<color=#94A3B8>ЛКМ: выбрать число (1-е бесплатно) | ПКМ: снять | Секторы (цвет, ряды) только по 1</color>", "<color=#94A3B8>LMB: select number (1st free) | RMB: deselect | Sectors (color, rows) single only</color>"), _labelSmallStyle);
                }
                else if (_feltBets.HasAnyOutsideBet())
                {
                    GUI.Label(hintRect, Localization.Get("<color=#F59E0B>⚠ Секторные ставки не поддерживают мультивыбор (только одиночная ставка)</color>", "<color=#F59E0B>⚠ Sector bets do not support multi-betting (single bet only)</color>"), _labelSmallStyle);
                }
                else
                {
                    int nextCost = CasinoRouletteHelper.GetNextCellLunarCost(numCount);
                    int totalCost = _feltBets.GetRequiredLunarCoins();
                    GUI.Label(hintRect, Localization.Format("<color=#94A3B8>Выбрано чисел: <b>{0}</b> (цена: <color=#38BDF8>{1} ☾</color>) | След. число: <color=#F59E0B>+{2} ☾</color> | ПКМ: снять</color>", "<color=#94A3B8>Numbers selected: <b>{0}</b> (cost: <color=#38BDF8>{1} ☾</color>) | Next: <color=#F59E0B>+{2} ☾</color> | RMB: remove</color>", numCount, totalCost, nextCost), _labelSmallStyle);
                }
            }
        }

        // =========================================================================
        // SIDEBAR CONTROLS (Wallet, Chips, Tokens, Spin)
        // =========================================================================
        private void DrawCasinoSidebar(float sideX, float startY, float sideW)
        {
            var master = GetLocalMaster();
            uint playerMoney = master != null ? master.money : 0;
            uint playerLunarCoins = 0;
            if (master?.playerCharacterMasterController?.networkUser != null)
            {
                playerLunarCoins = master.playerCharacterMasterController.networkUser.lunarCoins;
            }

            float y = startY;

            // 1. Balance Box
            Rect walletBox = new Rect(sideX, y, sideW, 50f);
            GUIHelper.DrawRect(walletBox, new Color(0.10f, 0.12f, 0.18f, 0.85f));
            GUIHelper.DrawBorder(walletBox, new Color(0.30f, 0.35f, 0.45f, 0.70f), 1);

            GUI.Label(new Rect(sideX + 8f, y + 4f, sideW - 16f, 16f), Localization.Format("Баланс: <color=#2ECC71><b>${0}</b></color>", "Balance: <color=#2ECC71><b>${0}</b></color>", playerMoney), _labelBoldStyle);

            int reqCoins = _feltBets.GetRequiredLunarCoins();
            string coinCostStr = reqCoins > 0 ? $"<color=#F59E0B>(-{reqCoins} ☾)</color>" : "<color=#94A3B8>(0 ☾)</color>";
            GUI.Label(new Rect(sideX + 8f, y + 20f, sideW - 16f, 16f), Localization.Format("Монеты: <color=#38BDF8><b>{0} ☾</b></color> {1}", "Coins: <color=#38BDF8><b>{0} ☾</b></color> {1}", playerLunarCoins, coinCostStr), _labelSmallStyle);

            int totalMoneyBet = _feltBets.GetTotalMoneyBet();
            GUI.Label(new Rect(sideX + 8f, y + 34f, sideW - 16f, 16f), Localization.Format("Ставка на столе: <color=#F59E0B><b>${0}</b></color>", "Table Bet: <color=#F59E0B><b>${0}</b></color>", totalMoneyBet), _labelSmallStyle);
            y += 54f;

            // 2. Chip Denominations (2x2 Grid)
            GUI.Label(new Rect(sideX, y, sideW, 16f), Localization.Get("<b>НОМИНАЛ ФИШКИ:</b>", "<b>CHIP VALUE:</b>"), _labelBoldStyle);
            y += 18f;

            EnsureValidChipSelection(playerMoney);

            bool can25 = playerMoney >= 25;
            bool can100 = playerMoney >= 100;
            bool can500 = playerMoney >= 500;
            bool canMax = playerMoney > 500;

            float chipBtnW = (sideW - 6f) / 2f;
            DrawChipSelectorButton(new Rect(sideX, y, chipBtnW, 20f), "$25", 25, can25, Localization.Get("Требуется минимум $25", "Requires at least $25"));
            DrawChipSelectorButton(new Rect(sideX + chipBtnW + 6f, y, chipBtnW, 20f), "$100", 100, can100, Localization.Get("Требуется минимум $100", "Requires at least $100"));
            y += 23f;

            DrawChipSelectorButton(new Rect(sideX, y, chipBtnW, 20f), "$500", 500, can500, Localization.Get("Требуется минимум $500", "Requires at least $500"));
            DrawChipSelectorButton(new Rect(sideX + chipBtnW + 6f, y, chipBtnW, 20f), "MAX", -1, canMax, Localization.Get("MAX доступен при балансе > $500", "MAX requires balance > $500"));
            y += 26f;

            // 3. Special Action Tokens (Life Bet & Item Bet)
            if (ModConfig.IsLifeBetActive)
            {
                bool inDroneOrDead = IsPlayerInDroneOrDeadMode(master);
                if (inDroneOrDead && (_isLifeBetPlaced || _isPlacingLifeBet))
                {
                    _isLifeBetPlaced = false;
                    _isPlacingLifeBet = false;
                }

                Rect lifeTokenRect = new Rect(sideX, y, sideW, 24f);

                if (inDroneOrDead)
                {
                    DrawUnifiedButton(lifeTokenRect, Localization.Get("☠ ЖИЗНЬ (ДРОН / МЁРТВ)", "☠ LIFE (DRONE / DEAD)"),
                        new Color(0.14f, 0.14f, 0.16f, 0.75f), new Color(0.26f, 0.26f, 0.30f, 0.60f), new Color(0.50f, 0.50f, 0.55f), 11, FontStyle.Bold, isEnabled: false);
                }
                else if (_isLifeBetPlaced)
                {
                    string lifeSectorName = CasinoRouletteHelper.GetBetTypeName(_lifeBetType, _lifeBetTarget);
                    string text = Localization.Format("☠ ЖИЗНЬ НА [{0}] (Снять) ☠", "☠ LIFE ON [{0}] (Clear) ☠", lifeSectorName);
                    if (DrawUnifiedButton(lifeTokenRect, text,
                        new Color(0.58f, 0.14f, 0.18f, 0.95f), new Color(1f, 0.45f, 0.50f, 1f), new Color(1f, 0.95f, 0.95f), 11, FontStyle.Bold))
                    {
                        _isLifeBetPlaced = false;
                        _lifeBetType = CasinoBetType.None;
                        PlaySound("Play_UI_menuClick");
                    }
                }
                else if (_isPlacingLifeBet)
                {
                    string text = Localization.Get("☠ ВЫБЕРИТЕ СЕКТОР (Отмена) ☠", "☠ CLICK SECTOR (Cancel) ☠");
                    if (DrawUnifiedButton(lifeTokenRect, text,
                        new Color(0.70f, 0.15f, 0.18f, 0.95f), new Color(1f, 0.30f, 0.35f, 1f), Color.white, 11, FontStyle.Bold, isEnabled: true, pulse: true, pulseBorder: new Color(1f, 0.5f, 0.5f, 1f)))
                    {
                        _isPlacingLifeBet = false;
                        PlaySound("Play_UI_menuClick");
                    }
                }
                else
                {
                    string text = Localization.Get("☠ СТАВИТЬ ЖИЗНЬ НА СЕКТОР ☠", "☠ BET LIFE ON SECTOR ☠");
                    if (DrawUnifiedButton(lifeTokenRect, text,
                        new Color(0.24f, 0.12f, 0.15f, 0.92f), new Color(0.52f, 0.22f, 0.28f, 0.85f), new Color(0.95f, 0.75f, 0.78f), 11, FontStyle.Bold))
                    {
                        _isPlacingLifeBet = true;
                        _isPlacingItemBet = false;
                        PlaySound("Play_heretic_laugh");
                    }
                }
                y += 28f;
            }

            // Item Bet Section
            if (_playerAvailableItems.Count > 0)
            {
                float arrowW = 22f;
                float cardW = sideW - (arrowW * 2f) - 6f;

                Rect prevRect = new Rect(sideX, y, arrowW, 26f);
                if (DrawUnifiedButton(prevRect, "<", new Color(0.12f, 0.15f, 0.22f, 0.90f), new Color(0.32f, 0.38f, 0.48f, 0.75f), Color.white, 12, FontStyle.Bold))
                {
                    _selectedItemIdxIndex = (_selectedItemIdxIndex <= 0) ? _playerAvailableItems.Count - 1 : _selectedItemIdxIndex - 1;
                    _confirmSpinPending = false;
                    PlaySound("Play_UI_menuClick");
                }

                Rect itemBox = new Rect(sideX + arrowW + 3f, y, cardW, 26f);
                GUIHelper.DrawRect(itemBox, new Color(0.12f, 0.15f, 0.22f, 0.90f));
                GUIHelper.DrawBorder(itemBox, _isItemBetActive ? new Color(0.20f, 0.90f, 0.40f) : new Color(0.35f, 0.40f, 0.50f), 1);

                ItemIndex chosenItem = _playerAvailableItems[_selectedItemIdxIndex];
                string itemName = ChaosEventManager.GetItemName(chosenItem);
                var pIndex = PickupCatalog.FindPickupIndex(chosenItem);
                var (sprite, tex, uv) = ItemPoolHelper.ResolveIcon(pIndex, null);

                Rect iconR = new Rect(itemBox.x + 2f, itemBox.y + 2f, 22f, 22f);
                if (tex != null) GUI.DrawTextureWithTexCoords(iconR, tex, uv);
                else GUIHelper.DrawRect(iconR, Color.gray);

                Rect nameR = new Rect(iconR.xMax + 4f, itemBox.y + 4f, itemBox.width - 28f, 18f);
                GUI.Label(nameR, $"<b>{itemName}</b>", _labelSmallStyle);

                Rect nextRect = new Rect(sideX + arrowW + cardW + 6f, y, arrowW, 26f);
                if (DrawUnifiedButton(nextRect, ">", new Color(0.12f, 0.15f, 0.22f, 0.90f), new Color(0.32f, 0.38f, 0.48f, 0.75f), Color.white, 12, FontStyle.Bold))
                {
                    _selectedItemIdxIndex = (_selectedItemIdxIndex >= _playerAvailableItems.Count - 1) ? 0 : _selectedItemIdxIndex + 1;
                    _confirmSpinPending = false;
                    PlaySound("Play_UI_menuClick");
                }
                y += 29f;

                // Toggle / Assign Item Bet Button
                Rect itemBetBtnRect = new Rect(sideX, y, sideW, 22f);
                if (_isItemBetActive)
                {
                    string text = Localization.Format("★ 1x [{0}] на #{1} (Снять) ★", "★ 1x [{0}] on #{1} (Clear) ★", itemName, _itemBetTargetNumber);
                    if (DrawUnifiedButton(itemBetBtnRect, text,
                        new Color(0.12f, 0.42f, 0.22f, 0.95f), new Color(0.30f, 0.95f, 0.50f, 1f), Color.white, 11, FontStyle.Bold))
                    {
                        _isItemBetActive = false;
                        _confirmSpinPending = false;
                        PlaySound("Play_UI_menuClick");
                    }
                }
                else if (_isPlacingItemBet)
                {
                    string text = Localization.Get("📦 КЛИКНИТЕ ЧИСЛО (0-36) (Отмена)", "📦 CLICK NUMBER (0-36) (Cancel)");
                    if (DrawUnifiedButton(itemBetBtnRect, text,
                        new Color(0.15f, 0.55f, 0.28f, 0.95f), new Color(0.40f, 1f, 0.60f, 1f), Color.white, 11, FontStyle.Bold, isEnabled: true, pulse: true, pulseBorder: new Color(0.6f, 1f, 0.7f, 1f)))
                    {
                        _isPlacingItemBet = false;
                        PlaySound("Play_UI_menuClick");
                    }
                }
                else
                {
                    string text = Localization.Get("📦 ПОСТАВИТЬ ПРЕДМЕТ НА ЧИСЛО", "📦 BET ITEM ON A NUMBER");
                    if (DrawUnifiedButton(itemBetBtnRect, text,
                        new Color(0.12f, 0.22f, 0.20f, 0.92f), new Color(0.25f, 0.48f, 0.38f, 0.85f), new Color(0.75f, 0.95f, 0.82f), 11, FontStyle.Bold))
                    {
                        _isPlacingItemBet = true;
                        _isPlacingLifeBet = false;
                        PlaySound("Play_UI_menuClick");
                    }
                }
                y += 26f;
            }

            // 4. Primary Actions: [ SPIN ] and [ CLEAR BETS ]
            bool isSpinning = _reelState == ReelState.Spinning || _spinRequestInFlight;
            bool hasLifeBet = _isLifeBetPlaced && ModConfig.IsLifeBetActive && !IsPlayerInDroneOrDeadMode(master);
            bool hasMoneyBet = totalMoneyBet >= 25 && playerMoney >= (uint)totalMoneyBet;
            bool hasItemBet = _isItemBetActive && _playerAvailableItems.Count > 0;
            bool canSpin = !isSpinning && (hasLifeBet || hasMoneyBet || hasItemBet);

            Rect spinRect = new Rect(sideX, y, sideW, 34f);

            if (_spinRequestInFlight)
            {
                DrawUnifiedButton(spinRect, Localization.Get("ОЖИДАНИЕ СЕРВЕРА...", "WAITING FOR SERVER..."),
                    new Color(0.16f, 0.20f, 0.28f, 0.90f), new Color(0.35f, 0.42f, 0.55f, 0.75f), new Color(0.70f, 0.75f, 0.85f), 12, FontStyle.Bold, isEnabled: false);
            }
            else if (_reelState == ReelState.Spinning)
            {
                DrawUnifiedButton(spinRect, Localization.Get("РУЛЕТКА ВРАЩАЕТСЯ...", "ROULETTE SPINNING..."),
                    new Color(0.18f, 0.22f, 0.30f, 0.90f), new Color(0.38f, 0.45f, 0.58f, 0.75f), new Color(0.75f, 0.80f, 0.90f), 12, FontStyle.Bold, isEnabled: false);
            }
            else if (_confirmSpinPending)
            {
                int sec = Mathf.CeilToInt(_confirmTimer);
                if (hasLifeBet)
                {
                    string text = Localization.Format("☠ ПОДТВЕРДИТЬ ЖИЗНЬ? ({0}с) ☠", "☠ CONFIRM LIFE? ({0}s) ☠", sec);
                    if (DrawUnifiedButton(spinRect, text,
                        new Color(0.68f, 0.12f, 0.16f, 0.95f), new Color(1f, 0.35f, 0.40f, 1f), Color.white, 12, FontStyle.Bold, isEnabled: true, pulse: true, pulseBorder: new Color(1f, 0.5f, 0.5f, 1f)))
                    {
                        RequestSpin();
                    }
                }
                else
                {
                    string text = Localization.Format("⚠ ПОДТВЕРДИТЬ СТАВКУ? ({0}с) ⚠", "⚠ CONFIRM BET? ({0}s) ⚠", sec);
                    if (DrawUnifiedButton(spinRect, text,
                        new Color(0.65f, 0.42f, 0.10f, 0.95f), new Color(1f, 0.75f, 0.25f, 1f), Color.white, 12, FontStyle.Bold, isEnabled: true, pulse: true, pulseBorder: new Color(1f, 0.85f, 0.4f, 1f)))
                    {
                        RequestSpin();
                    }
                }
            }
            else
            {
                Color spinBg;
                Color spinBorder;
                if (!canSpin)
                {
                    spinBg = new Color(0.12f, 0.14f, 0.18f, 0.75f);
                    spinBorder = new Color(0.25f, 0.28f, 0.35f, 0.55f);
                }
                else if (hasLifeBet)
                {
                    spinBg = new Color(0.62f, 0.14f, 0.18f, 0.95f);
                    spinBorder = new Color(1f, 0.40f, 0.45f, 1f);
                }
                else
                {
                    spinBg = new Color(0.12f, 0.48f, 0.24f, 0.95f);
                    spinBorder = new Color(0.35f, 0.92f, 0.50f, 1f);
                }

                if (DrawUnifiedButton(spinRect, "[ SPIN ]", spinBg, spinBorder, canSpin ? Color.white : new Color(0.45f, 0.48f, 0.55f, 0.75f), 13, FontStyle.Bold, isEnabled: canSpin))
                {
                    RequestSpin();
                }
            }
            y += 38f;

            // Clear Bets Button
            Rect clearRect = new Rect(sideX, y, sideW, 20f);
            if (DrawUnifiedButton(clearRect, Localization.Get("СБРОС СТАВОК", "CLEAR BETS"),
                new Color(0.14f, 0.16f, 0.22f, 0.90f), new Color(0.32f, 0.36f, 0.46f, 0.75f), new Color(0.82f, 0.85f, 0.92f), 10, FontStyle.Bold))
            {
                ClearAllBets();
            }
            y += 24f;

            // Last Result Summary if present
            if (!string.IsNullOrEmpty(_lastResultSummary))
            {
                float resH = Mathf.Min(40f, _windowRect.yMax - y - 6f);
                if (resH > 16f)
                {
                    Rect resRect = new Rect(sideX, y, sideW, resH);
                    _resultStyle!.normal.textColor = _lastResultColor;
                    GUI.Label(resRect, _lastResultSummary, _resultStyle);
                }
            }
        }

        private void EnsureValidChipSelection(uint playerMoney)
        {
            if (_selectedChipDenomination == -1) // MAX
            {
                if (playerMoney <= 500)
                {
                    if (playerMoney >= 500) _selectedChipDenomination = 500;
                    else if (playerMoney >= 100) _selectedChipDenomination = 100;
                    else _selectedChipDenomination = 25;
                    UpdateSelectedCellsStake();
                }
            }
            else if (_selectedChipDenomination == 500)
            {
                if (playerMoney < 500)
                {
                    if (playerMoney >= 100) _selectedChipDenomination = 100;
                    else _selectedChipDenomination = 25;
                    UpdateSelectedCellsStake();
                }
            }
            else if (_selectedChipDenomination == 100)
            {
                if (playerMoney < 100)
                {
                    _selectedChipDenomination = 25;
                    UpdateSelectedCellsStake();
                }
            }
        }

        private void DrawChipSelectorButton(Rect rect, string label, int denomination, bool isAvailable, string? disabledReason = null)
        {
            bool isSelected = (_selectedChipDenomination == denomination);
            Color bg;
            Color border;
            Color textCol;

            if (!isAvailable)
            {
                bg = new Color(0.10f, 0.12f, 0.16f, 0.60f);
                border = new Color(0.22f, 0.25f, 0.32f, 0.45f);
                textCol = new Color(0.40f, 0.44f, 0.50f, 0.75f);
            }
            else if (isSelected)
            {
                bg = new Color(0.18f, 0.36f, 0.55f, 0.95f);
                border = new Color(0.40f, 0.85f, 0.95f, 1f);
                textCol = Color.white;
            }
            else
            {
                bg = new Color(0.12f, 0.15f, 0.22f, 0.90f);
                border = new Color(0.30f, 0.35f, 0.46f, 0.75f);
                textCol = new Color(0.85f, 0.88f, 0.95f);
            }

            string display = (isSelected && isAvailable) ? $"★ {label}" : label;

            if (DrawUnifiedButton(rect, display, bg, border, textCol, 11, FontStyle.Bold, isEnabled: true))
            {
                if (!isAvailable)
                {
                    PlaySound("Play_UI_insufficient_funds");
                    if (!string.IsNullOrEmpty(disabledReason))
                    {
                        ShowToast(disabledReason, new Color(1f, 0.45f, 0.45f), 2.5f);
                    }
                    return;
                }

                _selectedChipDenomination = denomination;
                PlaySound("Play_UI_menuClick");
                UpdateSelectedCellsStake();
            }
        }

        private bool HandleFeltCellClick(Rect cellRect, out int mouseButton)
        {
            mouseButton = -1;
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && cellRect.Contains(ev.mousePosition))
            {
                mouseButton = ev.button;
                ev.Use();
                return true;
            }
            return false;
        }

        private void OnFeltCellClicked(CasinoBetType type, int targetNumber, int mouseButton)
        {
            var master = GetLocalMaster();
            uint playerMoney = master != null ? master.money : 0;

            if (mouseButton == 0) // Left Click: Place chip / Place Life / Place Item
            {
                if (_isPlacingLifeBet)
                {
                    _lifeBetType = type;
                    _lifeBetTarget = targetNumber;
                    _isLifeBetPlaced = true;
                    _isPlacingLifeBet = false;
                    PlaySound("Play_heretic_laugh");
                    ShowToast(Localization.Format("Жетон Жизни установлен на: {0}!", "Life Bet placed on: {0}!", CasinoRouletteHelper.GetBetTypeName(type, targetNumber)), new Color(1f, 0.3f, 0.3f), 3.0f);
                    return;
                }

                if (_isPlacingItemBet)
                {
                    if (type == CasinoBetType.StraightNumber)
                    {
                        _itemBetTargetNumber = targetNumber;
                        _isItemBetActive = true;
                        _isPlacingItemBet = false;
                        PlaySound("Play_UI_coins_gain");
                        string itemName = (_playerAvailableItems.Count > 0 && _selectedItemIdxIndex < _playerAvailableItems.Count)
                            ? ChaosEventManager.GetItemName(_playerAvailableItems[_selectedItemIdxIndex])
                            : "Item";
                        ShowToast(Localization.Format("Предмет [{0}] поставлен на число {1}!", "Item [{0}] bet on number {1}!", itemName, targetNumber), new Color(0.3f, 0.9f, 0.4f), 3.0f);
                    }
                    else
                    {
                        ShowToast(Localization.Get("Ставка предметом возможна только на точное число (0-36)!", "Item bet is only allowed on a straight number (0-36)!"), new Color(1f, 0.5f, 0.3f), 3.0f);
                        PlaySound("Play_UI_insufficient_funds");
                    }
                    return;
                }

                // Minimum money check: cannot bet gold if money < $25
                if (playerMoney < 25)
                {
                    PlaySound("Play_UI_insufficient_funds");
                    ShowToast(Localization.Get("Минимальная ставка в казино: $25! У вас недостаточно золота.", "Minimum casino bet is $25! You do not have enough gold."), new Color(1f, 0.4f, 0.4f), 2.5f);
                    return;
                }

                // Standard Multi-Selection Bet
                int current = GetBetAmount(type, targetNumber);
                if (current > 0)
                {
                    // Already selected: Toggle off / deselect
                    SetBetAmount(type, targetNumber, 0);
                    PlaySound("Play_UI_menuClick");
                    return;
                }

                // Requirement 2: Multi-selection is strictly for numbers (0-36).
                // Outside bets (color, row, dozen, even/odd, low/high) cannot be multi-selected.
                bool isNumber = (type == CasinoBetType.StraightNumber);
                if (!isNumber)
                {
                    // Outside bet: only allowed if there are NO other bets anywhere on the table
                    if (_feltBets.GetActiveBetSectorsCount() > 0)
                    {
                        PlaySound("Play_UI_insufficient_funds");
                        ShowToast(Localization.Get("Секторные ставки (цвет, ряды, дюжины) могут быть только одиночными!", "Sector bets (color, rows, dozens) can only be placed as a single solitary bet!"), new Color(1f, 0.5f, 0.2f), 3.0f);
                        return;
                    }
                }
                else
                {
                    // Number bet: not allowed if an outside bet is already placed
                    if (_feltBets.HasAnyOutsideBet())
                    {
                        PlaySound("Play_UI_insufficient_funds");
                        ShowToast(Localization.Get("Мультивыбор доступен только для чисел! Снимите секторную ставку.", "Multi-selection is only available for numbers! Clear the sector bet first."), new Color(1f, 0.5f, 0.2f), 3.0f);
                        return;
                    }
                }

                int effectiveStake = GetEffectiveStake();
                if (effectiveStake < 25)
                {
                    PlaySound("Play_UI_insufficient_funds");
                    ShowToast(Localization.Get("Недостаточно золота для выбранной фишки (минимум $25)!", "Not enough gold for selected chip (minimum $25)!"), new Color(1f, 0.4f, 0.4f), 2.5f);
                    return;
                }

                // Requirement 1: Exponential Lunar Coin Progression for Numbers (+4, +8, +16...)
                if (isNumber)
                {
                    int currentNumberCount = _feltBets.GetNumberBetsCount();
                    if (currentNumberCount >= 1)
                    {
                        int nextTotalLunarCost = CasinoRouletteHelper.CalculateMultiBetLunarCost(currentNumberCount + 1);
                        int nextExtraCellCost = CasinoRouletteHelper.GetNextCellLunarCost(currentNumberCount);
                        uint playerLunarCoins = RouletteUI.GetLocalPlayerLunarCoins();

                        if (playerLunarCoins < (uint)nextTotalLunarCost)
                        {
                            PlaySound("Play_UI_insufficient_funds");
                            ShowToast(Localization.Format("Для выбора ещё одного числа требуется +{0} ☾ (всего: {1} ☾, у вас: {2} ☾)!", "Selecting an extra number requires +{0} ☾ (total: {1} ☾, you have: {2} ☾)!", nextExtraCellCost, nextTotalLunarCost, playerLunarCoins), new Color(0.4f, 0.8f, 1f), 3.5f);
                            return;
                        }
                    }
                }

                // Select this cell with the current effective stake
                SetBetAmount(type, targetNumber, effectiveStake);
                PlaySound("Play_UI_coins_gain");
            }
            else if (mouseButton == 1) // Right Click: Remove chip / token / Deselect
            {
                // Remove Life Bet if placed on this cell
                if (_isLifeBetPlaced && _lifeBetType == type && (type != CasinoBetType.StraightNumber || _lifeBetTarget == targetNumber))
                {
                    _isLifeBetPlaced = false;
                    _lifeBetType = CasinoBetType.None;
                    PlaySound("Play_UI_menuClick");
                    ShowToast(Localization.Get("Жетон Жизни снят со стола.", "Life Bet token removed from felt."), Color.white, 2.0f);
                    return;
                }

                // Remove Item Bet if placed on this cell
                if (_isItemBetActive && type == CasinoBetType.StraightNumber && _itemBetTargetNumber == targetNumber)
                {
                    _isItemBetActive = false;
                    PlaySound("Play_UI_menuClick");
                    ShowToast(Localization.Get("Ставка предметом снята со стола.", "Item bet removed from felt."), Color.white, 2.0f);
                    return;
                }

                // Deselect cell
                int current = GetBetAmount(type, targetNumber);
                if (current > 0)
                {
                    SetBetAmount(type, targetNumber, 0);
                    PlaySound("Play_UI_menuClick");
                }
            }
        }

        private int GetEffectiveStake()
        {
            var master = GetLocalMaster();
            uint playerMoney = master != null ? master.money : 0;
            if (playerMoney < 25) return 0;

            if (_selectedChipDenomination == -1) // MAX
            {
                return playerMoney > 500 ? (int)playerMoney : 0;
            }

            if (playerMoney >= _selectedChipDenomination)
            {
                return _selectedChipDenomination;
            }

            return 0;
        }

        private void UpdateSelectedCellsStake()
        {
            int stake = GetEffectiveStake();
            for (int i = 0; i <= 36; i++) if (_feltBets.NumberBets[i] > 0) _feltBets.NumberBets[i] = stake;
            for (int i = 0; i < 3; i++) if (_feltBets.ColumnBets[i] > 0) _feltBets.ColumnBets[i] = stake;
            for (int i = 0; i < 3; i++) if (_feltBets.DozenBets[i] > 0) _feltBets.DozenBets[i] = stake;
            if (_feltBets.BetLow1_18 > 0) _feltBets.BetLow1_18 = stake;
            if (_feltBets.BetHigh19_36 > 0) _feltBets.BetHigh19_36 = stake;
            if (_feltBets.BetEven > 0) _feltBets.BetEven = stake;
            if (_feltBets.BetOdd > 0) _feltBets.BetOdd = stake;
            if (_feltBets.BetRed > 0) _feltBets.BetRed = stake;
            if (_feltBets.BetBlack > 0) _feltBets.BetBlack = stake;
        }

        private int GetBetAmount(CasinoBetType type, int targetNumber)
        {
            switch (type)
            {
                case CasinoBetType.StraightNumber:
                    if (targetNumber >= 0 && targetNumber <= 36) return _feltBets.NumberBets[targetNumber];
                    return 0;
                case CasinoBetType.Column1: return _feltBets.ColumnBets[0];
                case CasinoBetType.Column2: return _feltBets.ColumnBets[1];
                case CasinoBetType.Column3: return _feltBets.ColumnBets[2];
                case CasinoBetType.Dozen1_12: return _feltBets.DozenBets[0];
                case CasinoBetType.Dozen13_24: return _feltBets.DozenBets[1];
                case CasinoBetType.Dozen25_36: return _feltBets.DozenBets[2];
                case CasinoBetType.Low1_18: return _feltBets.BetLow1_18;
                case CasinoBetType.High19_36: return _feltBets.BetHigh19_36;
                case CasinoBetType.Even: return _feltBets.BetEven;
                case CasinoBetType.Odd: return _feltBets.BetOdd;
                case CasinoBetType.Red: return _feltBets.BetRed;
                case CasinoBetType.Black: return _feltBets.BetBlack;
                default: return 0;
            }
        }

        private void SetBetAmount(CasinoBetType type, int targetNumber, int amount)
        {
            amount = Math.Max(0, amount);
            switch (type)
            {
                case CasinoBetType.StraightNumber:
                    if (targetNumber >= 0 && targetNumber <= 36) _feltBets.NumberBets[targetNumber] = amount;
                    break;
                case CasinoBetType.Column1: _feltBets.ColumnBets[0] = amount; break;
                case CasinoBetType.Column2: _feltBets.ColumnBets[1] = amount; break;
                case CasinoBetType.Column3: _feltBets.ColumnBets[2] = amount; break;
                case CasinoBetType.Dozen1_12: _feltBets.DozenBets[0] = amount; break;
                case CasinoBetType.Dozen13_24: _feltBets.DozenBets[1] = amount; break;
                case CasinoBetType.Dozen25_36: _feltBets.DozenBets[2] = amount; break;
                case CasinoBetType.Low1_18: _feltBets.BetLow1_18 = amount; break;
                case CasinoBetType.High19_36: _feltBets.BetHigh19_36 = amount; break;
                case CasinoBetType.Even: _feltBets.BetEven = amount; break;
                case CasinoBetType.Odd: _feltBets.BetOdd = amount; break;
                case CasinoBetType.Red: _feltBets.BetRed = amount; break;
                case CasinoBetType.Black: _feltBets.BetBlack = amount; break;
            }
            _confirmSpinPending = false;
        }

        private void DrawChipBadge(Rect cellRect, int amount)
        {
            float badgeW = 28f;
            float badgeH = 15f;
            Rect badgeRect = new Rect(cellRect.xMax - badgeW - 1f, cellRect.yMax - badgeH - 1f, badgeW, badgeH);

            GUIHelper.DrawRect(badgeRect, new Color(0.08f, 0.12f, 0.22f, 0.95f));
            GUIHelper.DrawBorder(badgeRect, new Color(1f, 0.85f, 0.25f, 0.95f), 1);

            string amtText = amount >= 1000 ? $"${amount / 1000f:F1}k" : $"${amount}";
            GUI.Label(badgeRect, amtText, _feltBadgeStyle);
        }

        private void DrawLifeBadge(Rect cellRect)
        {
            float badgeW = 24f;
            float badgeH = 15f;
            Rect lifeRect = new Rect(cellRect.x + 1f, cellRect.y + 1f, badgeW, badgeH);

            float pulse = 0.8f + Mathf.Sin(Time.unscaledTime * 8f) * 0.2f;
            GUIHelper.DrawRect(lifeRect, new Color(0.9f * pulse, 0.1f * pulse, 0.15f * pulse, 0.95f));
            GUIHelper.DrawBorder(lifeRect, Color.white, 1);
            GUI.Label(lifeRect, "☠", _feltBadgeStyle);
        }

        private void DrawItemBadge(Rect cellRect, ItemIndex item)
        {
            Rect itemBadgeRect = new Rect(cellRect.x + 2f, cellRect.yMax - 18f, 16f, 16f);
            var pIndex = PickupCatalog.FindPickupIndex(item);
            var (sprite, tex, uv) = ItemPoolHelper.ResolveIcon(pIndex, null);
            if (tex != null)
            {
                GUI.DrawTextureWithTexCoords(itemBadgeRect, tex, uv);
            }
            else
            {
                GUI.Label(itemBadgeRect, "📦", _feltBadgeStyle);
            }
        }

        private void ClearAllBets()
        {
            _feltBets.Clear();
            _isLifeBetPlaced = false;
            _lifeBetType = CasinoBetType.None;
            _lifeBetTarget = 0;
            _isItemBetActive = false;
            _confirmSpinPending = false;
            _isPlacingLifeBet = false;
            _isPlacingItemBet = false;
            PlaySound("Play_UI_menuClick");
        }
    }
}
