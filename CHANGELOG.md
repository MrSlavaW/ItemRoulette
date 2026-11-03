# Changelog

## [1.5.5] - 2026-11-03
### Vanilla-Authentic Artifact Icon Overhaul:
- **Risk of Rain 2 Visual Parity**:
  - Replaced the legacy flat casino-styled procedural artifact icon with an authentic celestial obelisk roulette disc matching the exact aesthetic language of vanilla artifacts.
  - Implemented the iconic dual-palette visual style: deep midnight-indigo base (`#100734`) paired with luminous lunar lavender etchings (`#EBD7F2`) and 100% transparent alpha background.
- **Dedicated Selected & Deselected Sprite States**:
  - `smallIconSelectedSprite`: Radiant lavender glow across outer notched rim, radial pocket dividers, and the celestial crystal indicator chevron when the artifact is enabled.
  - `smallIconDeselectedSprite`: Subdued dusk-mauve tones (`#554866`) with soft alpha transparency for dormant state on the artifact selection screen.
- **Embedded & Custom-Loadable Asset Pipeline**:
  - High-resolution textures are compiled directly into `ItemRoulette.dll` as embedded resources (`Assets/Artifact/`), ensuring zero external file dependency and instant load times.
  - Added support for player/modder customization via loose files in `BepInEx/plugins/HorinisTeam-ItemRoulette/Assets/Artifact/` with a graceful procedural fallback.

## [1.5.4] - 2026-11-03
### Granular UI Scaling Category & Independent Element Sizing:
- **Dedicated UI Scaling Category (`00c. Client - UI Scale`)**:
  - Moved UI scale settings into their own dedicated configuration category in `ModConfig` and RiskOfOptions.
  - Added full English and Russian category localization (`ITEMROULETTE_CAT_00C`).
- **Granular Multiplicative Scaling Sliders**:
  - **Master UI Scale (`UiScale`)**: Global multiplier affecting all mod UI elements proportionally (0.1x to 5.0x, default 1.0x).
  - **Roulette UI Scale (`UiScaleRoulette`)**: Independent scale for the top item reel, hint pill, hold bar, and Void decision modal (0.1x to 5.0x, default 1.0x).
  - **Windows UI Scale (`UiScaleWindows`)**: Independent scale for all draggable/modal windows — Casino betting table, Run Event Log / Journal, and Chaos Admin Menu (0.1x to 5.0x, default 1.0x).
  - **Notifications UI Scale (`UiScaleToasts`)**: Independent scale for toast notification cards in the bottom-left corner of the screen (0.1x to 5.0x, default 1.0x).
- **Proportional Compounding**:
  - Each element's effective scale compounds multiplicatively (`Master * Element`) while remaining clamped within safe boundaries (`0.1f` to `5.0f`), preserving anchor alignments and crisp rendering across all display setups.

## [1.5.3] - 2026-11-03
### UI Elements Scaling & High-Resolution Display Adaptation:
- **Global UI Scale Setting (`UiScale`)**:
  - Added configuration setting `UiScale` (0.1x to 5.0x, default 1.0x) under category `00a. Client - General & UI`.
  - Registered RiskOfOptions slider (`0.10x` to `5.00x` with step `0.05x`) for live in-game adjustment.
  - Implemented full English and Russian localization tokens (`ITEMROULETTE_CONFIG_UISCALE_NAME`, `ITEMROULETTE_CONFIG_UISCALE_DESC`).
- **Zero-Allocation IMGUI Matrix Scaling (`GUIHelper.GuiScaleScope`)**:
  - Implemented engine-level matrix scaling using `GUIUtility.ScaleAroundPivot` in a zero-allocation `IDisposable` scope (`using (GUIHelper.UseScale(scale, pivot))`).
  - Scales all mod UI elements seamlessly across high-resolution displays (1440p, 4K), ultrawides, and compact screens (Steam Deck).
  - Automatically transforms IMGUI mouse event coordinates (`Event.current.mousePosition`), guaranteeing 100% pixel-perfect click and hover registration for all buttons and interactive controls.
- **Smart Anchor Alignment**:
  - Top reel banner, hint pill, hold bar, and item decision modal (`RouletteUI` & `CasinoRouletteUI`) scale outward from top-center `(Screen.width / 2, 0)`, maintaining perfect symmetrical alignment above player inventory.
  - Toast notification card stack (`NotificationToastUI`) scales from bottom-left screen anchor `(24, Screen.height - 180)` above the character portrait.
  - Draggable windows (Casino betting table, notification journal, chaos admin menu) scale from top-left window coordinates with dynamic screen bounds clamping (`_windowRect.size * scale`).
- **Native Cursor Isolation**:
  - Isolated software mouse cursors (`GUIHelper.DrawTopCursor`) to render at native 1:1 screen resolution (`Matrix4x4.identity`), preventing cursors from becoming oversized at high UI scale or microscopic at low UI scale.

## [1.5.2] - 2026-11-03
### Native Permanent Curse Penalty & Admin Menu Sync Fix:
- **Native Max Health Penalty for Void/Lunar Item Declines**:
  - Replaced custom IL hooks on `CharacterBody.RecalculateStats` with the native engine buff `RoR2Content.Buffs.PermanentCurse` (the Eclipse 8 / Void Collapse stacking curse buff).
  - Dynamically calculates the required curse stacks based on decline penalty percentage ($\text{targetCurse} = \frac{1}{1 - \text{fraction}}$).
  - Natively synchronizes across all clients via UNet syncvars, cleanly reduces Max HP, and renders the native diagonal striped curse overlay on the right of the client health bar.
  - Automatically persists across intra-stage body respawns (Dio's Best Friend, Larva) via `CharacterBody.onBodyStartGlobal` and cleans up on stage transitions.
- **Admin Menu Roll Duplication Fix**:
  - Fixed an issue in `AdminMenuUI` where clicking `+5 Free`, `+1 Red`, or `+1 Void` dispatched the action to the server while simultaneously incrementing local client counters, causing the HUD to display double the actual available spins (e.g., showing 12 when 6 were granted).
- **Website URL**:
  - Updated mod manifest website URL to official GitHub repository: `https://github.com/MrSlavaW/ItemRoulette`.

## [1.5.1] - 2026-11-03
### Upstream Localization Hotfix & Networked Subtitle Tokens (Videowiz92 Upstream Parity):
- **Chaos Events Description Overwrite Fix**:
  - Made `ChaosEventDefinition.Description` a read-only property to prevent assigning single-language strings to dual-language fields.
  - Migrated all dynamic event result descriptions (`DoubleItem`, `TemporaryDuplicates`, `SpectralDuplicates`, `QualityCashback`, `DoubleGold`, `AllyMirror`, `ForgeAnomaly`, `LoseGold30`, `SacrificeItem`, `HallowedCurse`, `VoidCorrosion`) to tokenized lookups via `SetDescriptionFromToken(token, args...)` and `SetDescription(ru, en)`.
  - Fixed rare fallback strings in `ExecuteElementalAspect`, `ExecuteAlliedDrones`, and aspect resolution.
- **Client-Authoritative Minion Subtitles (`CharacterBody.subtitleNameToken`)**:
  - Replaced hardcoded Russian string concatenation on the host with networked structural tokens (`ITEMROULETTE_SUBTITLE_COMPANION:{owner}` and `ITEMROULETTE_SUBTITLE_DRONE:{owner}`).
  - Hooked `CharacterBody.GetSubtitle` on clients to dynamically format companion and drone subtitles according to each player's local language.
  - Added dedicated registered tokens `ITEMROULETTE_SUBTITLE_HOSTILE_DRONE` and `ITEMROULETTE_SUBTITLE_HOSTILE_DRONE_REBOOTING`.
- **Special Thanks**:
  - Invaluable assistance, report, and testing from **Videowiz92**.

## [1.5.0] - 2026-10-31
### Artifact of the Roulette, Quality Mod Equipment Fix, Substandard Duplicator & Polish:
- **Artifact of the Roulette ("Artifact Edition")**:
  - Introduced the **Artifact of the Roulette** (`ITEMROULETTE_ARTIFACT_NAME`). ItemRoulette mechanics are now gated exclusively behind this artifact by default: enable it in the lobby or run rulebook to activate roulette drop suppression and UI.
  - Runs played without the artifact active now operate 100% as standard vanilla Risk of Rain 2.
  - Added procedural high-resolution 128×128 circular artifact icons with zero external asset dependencies.
  - Added `RequireArtifact` config option (`00a. Client - General & UI`) to toggle between artifact-gated play and classic always-on behavior.
- **Quality Equipment Preservation (Goorakh-Quality Bug Fix)**:
  - Replaced all calls reading `inventory.currentEquipmentIndex` (which is intercepted by Goorakh-Quality to return base vanilla indices) with `PlayerUtil.GetStoredEquipmentIndex`, directly inspecting the inventory's equipment slot structure.
  - Swapping equipment via roulette or granting elemental aspects now preserves the modded quality tier and drops the previously held equipment with its true quality intact.
  - Fixed an issue where the Void decision banner failed to close after taking quality equipment.
- **Substandard Duplicator (DLC3) Integration**:
  - Added support for the Substandard Duplicator (`ItemPoolHelper.ApplySubstandardDuplicator`) during permanent item grants, properly triggering duplicate visual effects and tiered duplicate multipliers from the Quality mod.
- **Configurable Life Bet & Extra Life Resurrections**:
  - Added `LifeBetRespectsExtraLives` configuration setting (`06. Casino Roulette`).
  - When enabled (`true`), losing a Life Bet triggers standard lethal damage (`Suicide`), allowing extra-life revival items (Dio's Best Friend, Pluripotent Larva, Seed of Life) to resurrect the player.
  - When disabled (`false`, default), death remains absolute via `TrueKill()`.
- **Working Single-Player Pause (`PauseGameWhileOpen`)**:
  - Implemented single-player game pause while the roulette reel (spinning, result display, Void decision), European Casino, or Admin menu is open.
  - Time scale is safely preserved and restored without interfering with the RoR2 pause menu (ESC) or multiplayer sessions.
- **Comprehensive English Localization & Text Cleanup**:
  - Fixed leaked Russian broadcast strings in `ChestRewardHooks` (Lunar Pods, Scavenger Packs, Void Cradles, Void Potentials).
  - Fixed `ChaosEvents` dynamic description assignment so Russian text no longer overwrites English translations.
  - Fixed `ItemDescriptionHooks` Sale Star timing string (`timingStrEn`) leaking Russian in English tooltips.
  - Fixed item name fallback strings in `ServerSpinService` and `NotificationManager`.

## [1.4.4] - 2026-10-25
### Void Decision Auto-Close & Inventory Grant Verification:
- **Void & Lunar Decision Auto-Close on Item Grant**:
  - Implemented continuous inventory verification in `RouletteUI.Update`: tracks baseline counts of won Void/Lunar items and equipment. The moment the player's inventory receives the item (via server grant or network sync), the roulette banner immediately closes (`FinishVoidDecision`), preventing tape hangs on screen.
  - Fixed an issue where accepting a Void item (`[B]`) transitioned into `BannerState.Finished` with a prolonged delay instead of instantly hiding the banner, causing the tape to linger after the item was already equipped/in inventory.
  - Added an in-flight watchdog timer (3.5s): prevents hotkey input lockups if a decision network packet is delayed or dropped, allowing players to safely re-press decision keys without softlocking.

## [1.4.3] - 2026-10-24
### Sale Star Interaction Timing, Overhaul & Quality Mod Compatibility:
- **Sale Star Interaction Snapshot System (`InteractionBegin`)**:
  - Fixed an issue where opening a chest with a single Sale Star (`LowerPricedChests`) or while running overhaul mods (such as `RiskOfBrainrot-2R4R` / `RiskierRain`) awarded 0 bonus roulette spins.
  - Implemented an **Interaction Snapshot** mechanism: captures active Sale Stars and Quality tiers at the exact moment of interaction (`PurchaseInteraction.OnInteractionBegin` / `Interactor.PerformInteraction`), before the vanilla game or overhaul mods consume stars early upon payment.
  - Guaranteed duplicate chest spin calculation (`starMultiplier = 1 + totalStars`) and Quality tier inheritance (`highestSpentStarTier`) during chest opening (`ItemDrop`), completely preventing premature star loss across all mod configurations.
  - Added self-healing star consumption: prevents double-consumption if other mods or vanilla already consumed stars, while automatically consuming any remaining untransformed active star stacks.

## [1.4.2] - 2026-10-24
### Void & Lunar Decision Synchronization & Softlock Fix:
- **Void & Lunar Item Confirmation / Decline Fix**:
  - Fixed an issue where players were unable to take (`[B]`) or decline (`[C]`) Void or Lunar heresy items after spinning or rerolling ("Нет активного решения Бездны." / "No active Void decision.").
  - Synchronized item pickup and tier metadata within client-to-server decision packets (`VoidDecisionRequestMessage`), allowing the server to validate and self-heal any desynchronized decision states.
  - Guaranteed that item entries are never nulled out during Void/Lunar rerolls (`[R]`), maintaining persistent state until the player makes a final choice.
  - Added request in-flight safeguards to prevent rapid double-click race conditions and automatic UI unlock on stale server responses.

## [1.4.1] - 2026-10-24
### Notification System, Event Journal, Formatting & Quality Fixes:
- **Notification Journal & Event Archiving (`[J]`)**:
  - Added full run event archiving (`NotificationJournalUI`) tracking all item wins, casino spins, and chaos occurrences until stage end.
  - Added dedicated 5th category tab `🎰 РУЛЕТКА` (`Roulette`) logging won items, Void decisions, and Chance Doll upgrades.
  - Added initiator display (`[00:00 | Этап X | Игрок]`) across toast cards and journal entries with multiplayer networking sync.
  - Cleaned up obsolete chat messages in favor of modern toast and journal notifications.
- **Localization & String Formatting Fixes**:
  - Fixed an overload ambiguity in `Localization.Format` / added `Localization.FormatToken` ensuring all placeholders (`{0}`..`{4}`) and item rarity colors are replaced correctly.
  - Localized all notification categories, journal headers, and action buttons in Russian and English.
- **Casino Roulette Access**:
  - Removed temporary lock stub; the authentic European Casino Roulette table opens immediately via hotkey (`C`) or the admin panel.
- **Chaos Events & Quality Mod Polish**:
  - Fixed hidden system item (`MonsoonPlayerHelper`) selection during Chaos events (Spectral Resonance, Temporary Duplicates, Double Item). All events now strictly target real visible items.
  - Filtered out raw TextMeshPro `<sprite name="...">` tags injected by the Quality mod from toast cards and reforging logs.
  - Removed duplicate notification broadcasts on item reforging (Forge Anomaly) and corruption (Void Corrosion).
  - Reforged quality tier names now display in their respective tier accent color.

## [1.4.0] - 2026-10-23
### Server-Authoritative Architecture, Boss Pool Overhaul, Sale Star Quality, Drone Rebalance & Localization:
- **Server-Authoritative Multiplayer Architecture**:
  - Replaced legacy chat-based networking with high-performance, typed HLAPI network messages (`SpinRequestMessage`, `SpinResultMessage`, `ClientEventMessage`, `VoidDecisionMessage`).
  - Server maintains full authority over gold costs, free roll inventories, RNG rolls, and item granting, eliminating client/server desync and duplicate reward glitches in multiplayer sessions.
  - Reel tape animation and winning card displays are synchronized purely from server outcome data.
- **Dedicated Teleporter Boss Reward Pool**:
  - Charging the Teleporter and activating Mountain Shrines now exclusively award spins from the dedicated **Boss Reward** pool (85% Uncommon, 5% Legendary, 10% Boss item). White/Common items are excluded from teleporter completion rewards and the anticipation reel.
  - Mountain Shrines award +1 additional boss-tier spin per shrine upon teleporter event completion.
- **Sale Star (`LowerPricedChest`) & Quality Compatibility Overhaul**:
  - Fixed an issue where only +1 spin was awarded regardless of the number of Sale Stars. Now properly consumes all active stars in inventory and grants +1 duplicate spin per star.
  - Added full Quality mod tier inheritance: duplicate spins from Quality Sale Stars guarantee items of at least that quality tier.
  - All 70 dummy cards on the reel tape and the winning item properly synchronize quality frames and colors during quality spins.
- **Hostile Drones Chaos Event Rebalance & Customization**:
  - Fixed point-blank instant death: hostile drones deal 10% base damage and have 10% base health by default (`HostileDroneDamageMultiplier = 0.10`, `HostileDroneHealthMultiplier = 0.10`).
  - Increased spawn distance to 15 meters (`HostileDroneSpawnDistance = 15m`).
  - Added a 10-second reboot delay / stun before attacking (`HostileDroneStunDuration = 10s`) with hardware weapon lockout, allowing players to react, seek cover, or eliminate the drones safely before they boot up.
  - Fully configurable squad composition: customize counts for Gunner (default: 4), Flame (default: 3), Missile (default: 2), and Mega Drone TC-280 (default: 1), with 0 excluding that type from the event pool.
  - Added optional self-destruct lifespan timer (`EnableHostileDroneLifetime` and `HostileDroneLifetimeSeconds`).
- **Complete Localization & Configuration Menu Fix**:
  - Resolved a JSON structure issue where 14 config options (including item description tooltips and Longstanding Solitude settings) were omitted from game loading.
  - 100% tokenized localization with 690 keys fully translated in both Russian (`ru`) and English (`en`).
- **Total Casino Roulette & Felt Overhaul (Authentic European Betting Table)**:
  - **Interactive European Felt (Сукно)**: Sector `0` (Zero), 3x12 number grid (1–36) with accurate Red/Black colors, Columns (`2:1`), Dozens (`1st 12`, `2nd 12`, `3rd 12`), and Outside Bets (`1-18`, `EVEN`, `RED`, `BLACK`, `ODD`, `19-36`).
  - **Dynamic Chip Betting & Exploit Prevention**: Minimum casino bet is `$25`. Chips require exact face value (`$25`, `$100`, `$500`). `MAX` button is strictly available only when player money exceeds the maximum fixed chip (`playerMoney > 500`), completely preventing $1 chaos exploit farming.
  - **Exponential Lunar Coins Multi-Bet Economy**: 1st chosen number is free (0 ☾). Each subsequent number chosen doubles its additional Lunar Coin cost (+4, +8, +16, +32... formula: $4 \times (2^{N-1} - 1)$).
  - **Outside Bets Single-Only Restriction**: Outside bets (Color, Columns 2:1, Dozens, Parity, Halves) are restricted to single solitary bets to prevent exploit hedging (e.g. betting all 3 columns or both colors). Multi-bet is strictly reserved for straight numbers.
  - **Draggable Window**: Window position can be moved smoothly across the screen using the corner drag handle icon (✥), clamped to screen boundaries and persisted across game restarts (`CasinoWindowPosX`, `CasinoWindowPosY`).
  - **Independent HUD Spin Reel**: The authentic roulette tape remains rendered in the HUD top inventory box, keeping the betting table clean.
  - **Life Bet Token (`[ ☠ LIFE BET ]`)**: Place a life bet on any sector (Color/Even x2, Dozen/Column x3, Straight Number x36 jackpot) with 2-step confirmation and permanent death on loss.
  - **Item Bet on Any Chosen Number (0–36)**: Bet an inventory item on any specific number. Hitting that number duplicates the item and upgrades its quality tier!
  - **Server-Authoritative Casino Protocol**: Multi-bet evaluation, lunar coins validation, and payouts are fully authoritative on the host.
  - **Player Input Isolation in UI**: Fixed character attacking or firing primary skills (LMB/RMB) while clicking anywhere inside the Casino or Admin UI menus by intercepting `PlayerCharacterMasterController.PollButtonInput` and consuming unhandled window events.
  - **Seamless ESC Menu Closing without Opening Game Pause**: Pressing Escape while any mod menu is open now cleanly closes the mod window without triggering the game's pause menu.
- **Dedicated In-Game Notification System & Run Event Journal (`[J]`)**:
  - **Complete Chat Decoupling**: 100% of automated mod announcements (Casino spins, Chaos events, Boss rewards, Void choices, etc.) are removed from RoR2's public chat window, leaving the chat completely clean for player communication.
  - **Bottom-Left Achievement-Style Popups**: High-performance, stacked on-screen notification toasts in the bottom-left corner with color-coded category stripes, time badges, smooth slide-in, and auto-decay.
  - **Persistent Run Event Journal**: All gameplay events throughout the entire run are archived until the run ends. Pressing **[J]** opens an interactive modal journal with category tabs (`ALL`, `CASINO`, `CHAOS`, `REWARDS`), newest-first reverse chronological order, and smooth scrolling.
  - **Full Multiplayer Synchronization**: Host and all connected clients receive notifications through dedicated HLAPI network messages (`NotificationNetMessage`).

## [1.3.16] - 2026-10-18
### Independent Cost Scaling by Time & Stages:
- **Separated Time and Stage Scaling (`03. Cost Scaling`)**:
  - Split previously coupled difficulty scaling into two completely independent configuration options:
    - `ScaleWithTime` (default: `true`): Toggles dynamic price scaling with run time and difficulty coefficient.
    - `TimeCostScalingMultiplier` (default: `1.0x`): Fine-tunes the rate of time-based cost growth (0.0x = disabled, 0.5x = half speed, 1.0x = standard RoR2 chest rate).
    - `ScaleWithStage` (default: `true`): Toggles cost increase per completed stage (+X% per stage).
    - `StageCostIncreasePercent` (default: `10.0%`): Configurable percentage added to base price for each completed stage.
  - Players can now configure any combination: price growing only between stages without growing during a stage, price growing only by the game timer, or keeping a static base price throughout the entire run.
- **Risk of Options Integration**:
  - Added individual sliders and checkboxes for both Time and Stage scaling controls.

## [1.3.15] - 2026-10-18
### Configurable Chaos Events Pool:
- **Individual Chaos Event Toggles**:
  - Added dedicated configuration toggles for each of the 30 Chaos Events (14 Positive and 16 Negative outcomes).
  - Toggles are organized under two new clear categories:
    - `05c. Chaos Events - Positive`: 14 positive boon events (Item Duplication, Temporary Arsenal, Spectral Resonance, Rarity Cashback, Gold Jackpot, Fortune Advance, Orbital Sweep, Ally Mirror, Healing Grace, Phantom Ally, Elite Essence, Rarity Blessing, Drone Squadron, and Forge Anomaly).
    - `05d. Chaos Events - Negative`: 16 hazard events (Bandit Ambush, Inflation, Void Temptation, Natural Sacrifice, All or Nothing, Void Leech, Blood Tax, Fragile Aegis, Monster Mimic, Evil Doppelganger, Stage Anomaly, Shrine Wrath, Moon Gravity, Commoner's Curse, Drone Uprising, and Void Corrosion).
- **In-Game Risk of Options Integration**:
  - All 30 individual event toggles are exposed in the Risk of Options menu with real-time checkbox configuration.
- **Dynamic Odds Balancing**:
  - If a player disables all events in a polarity category (e.g., all negative events disabled), the calculated chance for that polarity automatically drops to 0%, completely preventing wasted/dead spins.
- **Admin / Debug Menu Enhancements `[F7]`**:
  - Added an active event counter in section headers (e.g. `14/14 ПОЛОЖИТЕЛЬНЫХ СОБЫТИЙ`).
  - Added a visual dimmed indicator `[ВЫКЛ]` / `[OFF]` for events disabled in configuration.

## [1.3.14] - 2026-10-16
### Localization & Internationalization (EN / RU):
- **Full English (EN) Language Support**:
  - Comprehensive English localization across all aspects of the mod alongside native Russian.
  - Automatic language detection: detects client language (`RoR2.Language.currentLanguageName`). Automatically defaults to Russian if the game is set to Russian (`"ru"`), and English for any other language (`"en"`, `"zh-CN"`, `"fr"`, `"de"`, `"es"`, etc.).
- **Configurable Language Setting (`00. Language`)**:
  - `LanguageSetting` (default: `Auto`): Choose between `Auto`, `English`, or `Russian` directly via config or the Risk of Options menu.
- **100% Bilingual Interface & HUD**:
  - **HUD Reel**: Spin prompts, hold progress bars, auto-spin badges, free roll counter pills, tier upgrade toasts, and status alerts.
  - **Void Corruptions**: Bilingual decision banners, action buttons (`Take [B]`, `Reroll [R]`, `Decline [C]`), cost displays, and max HP penalty warnings.
  - **All 29 Chaos Events**: Fully localized titles, descriptions, drone squad options, and chat announcements for all 14 Positive and 15 Negative events.
  - **Casino Roulette**: Board bets, pocket labels, chip/currency selectors (Gold / Life), validation warnings, win/loss toasts, chat broadcasts, and Life Bet permadeath confirmation modals.
  - **Admin / Debug Menu `[F7]`**: Control buttons, positive/negative event columns, and instant simulation alerts.
  - **Quality Mod Integration**: Localized quality tier frames, prompts, and custom events (*Forge Anomaly* & *Void Corrosion*).

## [1.3.13] - 2026-10-16
### Quality Mod Compatibility (Goorakh-Quality):
- **Full Soft-Dependency Integration**:
  - Added pure reflection bridge in `QualityCompat.cs` for `com.Gorakh.ItemQualities` (`ItemQualities.dll`). Zero hard dependencies, completely isolated.
  - Automatically identifies and supports all Quality tiers: Uncommon, Rare, Epic, and Legendary.
- **Configurable Quality Chest Generation & Rewards (`04d. Quality Integration`)**:
  - `AllowQualityChestSpawns` (default: `true`): Toggle generation of `QualityChest1` and `QualityChest2` interactables on stages.
  - `QualityChestRewardAllPlayers` (default: `false`): Configure reward distribution when opening Quality chests (Opener Only vs Team-wide). Opening rewards free quality rolls with guaranteed quality upgrades.
  - `QualityRollChanceMultiplier` (default: `1.0x`): Multiplier for base quality chance during roulette rolls.
- **Chaos Quality Influence Mechanic**:
  - `EnableChaosQualityInfluence` (default: `true`): Connects player's total accumulated Chaos percentage (`Good% + Bad%`) to item quality upgrades.
  - `ChaosQualityInfluenceFactor` (default: `0.05%` per 1% Chaos): Increases quality upgrade odds with high-risk builds (+5% quality chance at 100% Chaos).
- **Exclusive Chaos Events**:
  - **Forge Anomaly (Good)**: Automatically reforges a random inventory item or won item, elevating its quality tier by +1. Only rolled when Quality mod is loaded.
  - **Void Corrosion (Bad)**: Corrupts an item in the player's inventory, degrading its quality tier by 1. Only rolled when Quality mod is loaded.
  - **Ally Mirror Enhancement**: Faithfully duplicates ally items with their exact Quality tier preserved.
- **HUD & UI Quality Presentation**:
  - Quality items on the HUD reel are adorned with glowing rarity accents, quality frames, and the `✦` symbol.
  - Status prompt indicates available `[B] КРУТКА КАЧЕСТВА: БЕСПЛАТНО (X)` rolls with dedicated emerald-cyan styling.

## [1.3.12] - 2026-10-16
### Elite Aspect Pool Filtering:
- **Incomplete Aspect Validation**:
  - Added strict validation in `ElementalAspectDrop` event to ensure only fully functional elite aspects with active passive buff definitions (`passiveBuffDef != null`) are included in the pool.
  - Automatically filters out incomplete/dummy internal DLC equipment without passive buffs.

## [1.3.11] - 2026-10-16
### Dependency & Clean Installation Fix:
- **HookGenPatcher Dependency Added**:
  - Added `RiskofThunder-HookGenPatcher-1.2.5` to Thunderstore dependencies in `manifest.json`.
  - Fixes `TypeLoadException: Could not load file or assembly 'MMHOOK_RoR2'` when installing the mod on clean/fresh mod profiles.

## [1.3.8] - 2026-10-15
### Void Item Reroll Mechanic (Lunar Coins):
- **Void Decision Reroll System**:
  - Added ability to reroll an unowned Void item directly from the decision banner using Lunar Coins.
  - New hotkey `VoidRerollHotkey` (default: `KeyCode.R`, configurable in `01. Controls` and RiskOfOptions).
- **Configurable Reroll Settings (`02. Drop Suppression`)**:
  - `EnableVoidReroll` (default: `true`): Enable or disable Void item rerolling.
  - `VoidRerollLunarCoinsCost` (default: `2`, range 0-20): Cost in Lunar Coins per reroll attempt.
  - `VoidRerollMaxPerItem` (default: `3`, range 1-10): Maximum reroll attempts allowed per dropped Void item.
- **Interactive 3-Button Decision Banner**:
  - Dynamically displays 3 responsive action buttons in `VoidDecision` state:
    - `[✓ ВЗЯТЬ [B]]` (Green)
    - `[↻ ПЕРЕКРУТИТЬ (-2 Лун.) [0/3] [R]]` (Cyan/Celestial Lunar)
    - `[✕ ОТКАЗАТЬСЯ (-4 Лун. / -30% ХП) [C]]` (Violet / Crimson)
  - Clear state indicators for reaching maximum rerolls or insufficient coins.
- **Multiplayer Synchronization**:
  - Server-authoritative deduction of Lunar Coins with broadcast chat notifications.

## [1.3.7] - 2026-10-15
### Artifact Pool Configuration & Artifact of Kin Removal:
- **Artifact of Kin (`SingleMonsterType`) Disabled by Default**:
  - Removed from the base active debuff pool (`AllowArtifactKin = false`). This eliminates the infinite teleporter boss mob spawn spam during teleporter charging.
- **Dedicated Artifact Pool Configuration (`05b. Artifact Pool`)**:
  - Added new configuration category in config file and RiskOfOptions menu.
  - Individual toggles for all debuff artifacts: Spite, Warfare, Honor, Chaos (coop), Dissonance, Kin, Swarms, Frailty, Soul.
  - Optional toggles for all other game artifacts: Devotion, Delusion, Rebirth, Glass, Evolution, Vengeance, Metamorphosis, Enigma, Sacrifice, Command, Death.
- **Modded Artifact Support**:
  - `AllowModdedArtifacts` (default: `true`): dynamic discovery and inclusion of any third-party artifacts from `ArtifactCatalog`.
  - `ArtifactCustomBlacklist`: customizable string blacklist for unwanted artifacts.

## [1.3.6] - 2026-10-15
### Switchable Void Decline Cost (Lunar Coins vs Max HP):
- **Configurable Decline Cost Mode (`VoidDeclineCostMode`)**:
  - `LunarCoins`: Classic decline costing Lunar Coins (default: 4 coins).
  - `MaxHealthPenalty`: Decline at the cost of temporary maximum health reduction (default: 30% per decline).
- **Cumulative Stacking & Safety Cap**:
  - Declining multiple times on the same stage accumulates penalty: 1st decline = -30%, 2nd = -60%, 3rd = -90%.
  - Added configurable safety cap `VoidDeclineMaxHealthPenaltyCap` (default: 90%) preventing instant suicide.
- **RoR2 Native Curse Bar Visualization (`cursePenalty`)**:
  - Deducted maximum health is visually blocked on the health bar with native diagonal curse stripes.
- **Stage Reset**:
  - Health penalty stacks are cleared upon teleporter stage transition or starting a new run.
- **Adaptive UI & Multiplayer Sync**:
  - Action buttons and header labels dynamically update to show cost in coins or health penalty.
  - Server-authoritative penalty processing with multiplayer chat announcements and stack synchronization.

## [1.3.5] - 2026-10-15
### Lunar Item Pool Configuration & ZetAspects Mod Support:
- **Comprehensive Lunar Item Pool Settings**:
  - Added `04b. Lunar Pool` configuration category with individual toggles for all 20 passive Lunar items and 5 Lunar equipments across base game and DLCs (SotV, SotS).
  - Added custom Lunar blacklist setting (`LunarCustomBlacklist`).
- **ZetAspects (`com.TPDespair.ZetAspects`) Integration**:
  - Mod compatibility layer with soft-dependency reflection.
  - In Chaos event «Elemental Aspect Drop», aspects are awarded as permanent passive items instead of overriding active equipment.
  - Added setting to include ZetAspects elite items into the roulette pool.

## [1.3.4] - 2026-10-14
### Void Roll Isolation & Interaction Fixes:
- **Void Rolls Excluded from Auto-Spin**:
  - Auto-spin mode (`[V]`) will not consume Void rolls; player retains full manual control via `[C]`.
- **UI & Button Fixes**:
  - Fixed button text clipping and currency token issues.
- **Multiplayer Cradle Fix**:
  - Resolved local player match check so opening a Void Cradle awards a roll exclusively to the activator.
- **Lunar Shop & Cauldron Compatibility**:
  - Ensured physical purchases and conversions in Bazaar Between Time and Cauldrons function seamlessly without interference.

## [1.3.0] - 2026-10-13
### Auto-Spin, Chance Doll & Chaos Overhaul:
- Continuous Auto-Spin mode with hold progress bar (`[Hold V]`).
- Chance Doll integration: 5% chance per stack to upgrade rolled item tier (White -> Green -> Red).
- New Chaos events: Support Drones, Hostile Enemy Drones, High-Tier Pylon, Aurelionite quality scaling.
- In-game config via RiskOfOptions.

## [1.0.0] - 2026-10-10
### Initial Release:
- Top HUD CS2-style case opening roulette (`[B]`).
- European Casino roulette wheel with gold bets (`[K]`).
- Life Bet mechanic with permadeath risk.
- 28 dynamic Chaos events.
