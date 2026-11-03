using BepInEx.Configuration;
using UnityEngine;
using ItemRoulette.Core;
using ItemRoulette.Hooks;

namespace ItemRoulette.Configuration
{
    public enum CostFormula
    {
        LinearPercent,
        Exponential,
        Linear
    }

    public enum VoidDeclineCostMode
    {
        LunarCoins,
        MaxHealthPenalty
    }

    public enum ZetAspectsPoolMode
    {
        DedicatedTier,
        BossTier,
        LegendaryTier,
        AutoByModTier
    }

    public enum VanillaAspectsPoolMode
    {
        EquipmentPool,
        DedicatedTier,
        BossTier,
        Disabled
    }

    public enum SaleStarBossTriggerTiming
    {
        OnTeleporterCharged,
        OnAllBossesDefeated
    }

    public static class ModConfig
    {
        #region Properties
        // 00a. Client - General & UI
        public static ConfigEntry<ItemRoulette.Core.ModLanguage> LanguageSetting { get; private set; } = null!;
        public static ConfigEntry<bool> RequireArtifact { get; private set; } = null!;
        public static ConfigEntry<bool> PauseGameWhileOpen { get; private set; } = null!;
        public static ConfigEntry<float> SpinDuration { get; private set; } = null!;
        public static ConfigEntry<float> PostSpinDisplayDuration { get; private set; } = null!;
        public static ConfigEntry<float> AutoSpinHoldDuration { get; private set; } = null!;
        public static ConfigEntry<float> AutoSpinDelay { get; private set; } = null!;
        public static ConfigEntry<bool> ShowChanceWidgetInUI { get; private set; } = null!;
        public static ConfigEntry<bool> EnableItemDescriptionAdditions { get; private set; } = null!;

        // 00b. Client - Controls & Hotkeys
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> VoidSpinHotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> AutoSpinHotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> VoidRerollHotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> AdminMenuHotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CasinoToggleHotkey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NotificationJournalHotkey { get; private set; } = null!;

        // 00c. Client - UI Scale
        public static ConfigEntry<float> UiScale { get; private set; } = null!;
        public static ConfigEntry<float> UiScaleRoulette { get; private set; } = null!;
        public static ConfigEntry<float> UiScaleWindows { get; private set; } = null!;
        public static ConfigEntry<float> UiScaleToasts { get; private set; } = null!;

        // 01. Drop Suppression
        public static ConfigEntry<bool> DisableChestSpawns { get; private set; } = null!;
        public static ConfigEntry<bool> DisableChestDrops { get; private set; } = null!;
        public static ConfigEntry<bool> DisableBossDrops { get; private set; } = null!;
        public static ConfigEntry<bool> DisableChanceShrines { get; private set; } = null!;
        public static ConfigEntry<bool> DisablePrinters { get; private set; } = null!;
        public static ConfigEntry<bool> DisableFabricators { get; private set; } = null!;
        public static ConfigEntry<bool> DisableMultishops { get; private set; } = null!;
        public static ConfigEntry<bool> DisableEquipmentChests { get; private set; } = null!;
        public static ConfigEntry<bool> DisableEquipmentMultishops { get; private set; } = null!;
        public static ConfigEntry<bool> EnableVoidChests { get; private set; } = null!;
        public static ConfigEntry<bool> EnableLunarChests { get; private set; } = null!;

        // 02a. Economy - Cost Scaling
        public static ConfigEntry<int> BaseCost { get; private set; } = null!;
        public static ConfigEntry<CostFormula> CostScalingMode { get; private set; } = null!;
        public static ConfigEntry<float> RollCostIncreasePercent { get; private set; } = null!;
        public static ConfigEntry<bool> ScaleWithTime { get; private set; } = null!;
        public static ConfigEntry<float> TimeCostScalingMultiplier { get; private set; } = null!;
        public static ConfigEntry<bool> ScaleWithStage { get; private set; } = null!;
        public static ConfigEntry<float> StageCostIncreasePercent { get; private set; } = null!;
        public static ConfigEntry<float> ExponentialCostMultiplier { get; private set; } = null!;
        public static ConfigEntry<int> LinearCostIncrement { get; private set; } = null!;
        public static ConfigEntry<bool> ResetCostOnNewStage { get; private set; } = null!;

        // 02b. Economy - Free Spins
        public static ConfigEntry<int> FreeRollsPerStageBase { get; private set; } = null!;
        public static ConfigEntry<int> FreeRollsOnTeleporterCharged { get; private set; } = null!;
        public static ConfigEntry<int> FreeRollsPerSaleStar { get; private set; } = null!;
        public static ConfigEntry<int> FreeRollsPerMountainShrine { get; private set; } = null!;
        public static ConfigEntry<bool> EnableSaleStarRework { get; private set; } = null!;
        public static ConfigEntry<bool> SaleStarDuplicateChestSpins { get; private set; } = null!;
        public static ConfigEntry<int> SaleStarBossRewardSpinsBase { get; private set; } = null!;
        public static ConfigEntry<int> SaleStarBossRewardSpinsPerStack { get; private set; } = null!;
        public static ConfigEntry<SaleStarBossTriggerTiming> SaleStarBossTriggerTimingSetting { get; private set; } = null!;
        public static ConfigEntry<bool> SaleStarQualityBossGrantsQualitySpins { get; private set; } = null!;
        public static ConfigEntry<bool> MountainShrineGrantsBossRolls { get; private set; } = null!;
        public static ConfigEntry<bool> SaleStarQualityGuaranteesMinimumTier { get; private set; } = null!;

        // 02b-1. Boss Reward Pool Weights
        public static ConfigEntry<float> BossRewardWeightUncommon { get; private set; } = null!;
        public static ConfigEntry<float> BossRewardWeightLegendary { get; private set; } = null!;
        public static ConfigEntry<float> BossRewardWeightBoss { get; private set; } = null!;

        // 02c. Economy - Item Modifiers
        public static ConfigEntry<bool> EnableChanceDollSupport { get; private set; } = null!;
        public static ConfigEntry<float> ChanceDollBaseChancePercent { get; private set; } = null!;
        public static ConfigEntry<float> ChanceDollStackChancePercent { get; private set; } = null!;
        public static ConfigEntry<bool> EnableLongstandingSolitudeMechanic { get; private set; } = null!;
        public static ConfigEntry<int> LongstandingSolitudeFreeRollsBase { get; private set; } = null!;
        public static ConfigEntry<int> LongstandingSolitudeFreeRollsPerStack { get; private set; } = null!;
        public static ConfigEntry<float> LongstandingSolitudeRarityMultiplierBase { get; private set; } = null!;
        public static ConfigEntry<float> LongstandingSolitudeRarityMultiplierPerStack { get; private set; } = null!;
        public static ConfigEntry<bool> LongstandingSolitudeRewardAllPlayers { get; private set; } = null!;

        // 02d. Rewards - Team Share
        public static ConfigEntry<bool> GoldChestRewardAllPlayers { get; private set; } = null!;
        public static ConfigEntry<bool> PreplacedChestRewardAllPlayers { get; private set; } = null!;
        public static ConfigEntry<bool> SecretBossRewardAllPlayers { get; private set; } = null!;
        public static ConfigEntry<bool> EquipmentChestRewardAllPlayers { get; private set; } = null!;
        public static ConfigEntry<bool> LunarChestRewardAllPlayers { get; private set; } = null!;

        // 02e. Rewards - Confirmations & Declines
        public static ConfigEntry<bool> EnableVoidCorruptChoice { get; private set; } = null!;
        public static ConfigEntry<VoidDeclineCostMode> VoidDeclineCostType { get; private set; } = null!;
        public static ConfigEntry<int> VoidDeclineLunarCoinsCost { get; private set; } = null!;
        public static ConfigEntry<float> VoidDeclineMaxHealthPenaltyPercent { get; private set; } = null!;
        public static ConfigEntry<float> VoidDeclineMaxHealthPenaltyCap { get; private set; } = null!;
        public static ConfigEntry<bool> EnableVoidReroll { get; private set; } = null!;
        public static ConfigEntry<int> VoidRerollLunarCoinsCost { get; private set; } = null!;
        public static ConfigEntry<int> VoidRerollMaxPerItem { get; private set; } = null!;
        public static ConfigEntry<bool> EnableEquipmentDropChoice { get; private set; } = null!;

        // 03a. Item Pools - Tier Weights
        public static ConfigEntry<float> WeightCommon { get; private set; } = null!;
        public static ConfigEntry<float> WeightUncommon { get; private set; } = null!;
        public static ConfigEntry<float> WeightLegendary { get; private set; } = null!;
        public static ConfigEntry<float> WeightBoss { get; private set; } = null!;
        public static ConfigEntry<bool> IncludeLunar { get; private set; } = null!;
        public static ConfigEntry<float> WeightLunar { get; private set; } = null!;
        public static ConfigEntry<bool> IncludeVoid { get; private set; } = null!;
        public static ConfigEntry<float> WeightVoid { get; private set; } = null!;

        // 03b. Item Pools - Rules & Blacklists
        public static ConfigEntry<bool> IncludeLunarEquipment { get; private set; } = null!;
        public static ConfigEntry<string> LunarCustomBlacklist { get; private set; } = null!;

        // 03c. Item Pools - Lunar Items
        public static ConfigEntry<bool> AllowShapedGlass { get; private set; } = null!;
        public static ConfigEntry<bool> AllowBrittleCrown { get; private set; } = null!;
        public static ConfigEntry<bool> AllowTranscendence { get; private set; } = null!;
        public static ConfigEntry<bool> AllowCorpsebloom { get; private set; } = null!;
        public static ConfigEntry<bool> AllowGestureOfTheDrowned { get; private set; } = null!;
        public static ConfigEntry<bool> AllowBeadsOfFealty { get; private set; } = null!;
        public static ConfigEntry<bool> AllowFocusedConvergence { get; private set; } = null!;
        public static ConfigEntry<bool> AllowDefiantGouge { get; private set; } = null!;
        public static ConfigEntry<bool> AllowMercurialRachis { get; private set; } = null!;
        public static ConfigEntry<bool> AllowPurity { get; private set; } = null!;
        public static ConfigEntry<bool> AllowVisionsOfHeresy { get; private set; } = null!;
        public static ConfigEntry<bool> AllowHooksOfHeresy { get; private set; } = null!;
        public static ConfigEntry<bool> AllowStridesOfHeresy { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEssenceOfHeresy { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEulogyZero { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEgocentrism { get; private set; } = null!;
        public static ConfigEntry<bool> AllowStoneFluxPauldron { get; private set; } = null!;
        public static ConfigEntry<bool> AllowLightFluxPauldron { get; private set; } = null!;
        public static ConfigEntry<bool> AllowLongstandingSolitude { get; private set; } = null!;
        public static ConfigEntry<bool> AllowNeutroniumWeight { get; private set; } = null!;

        // 03d. Item Pools - Lunar Equipment
        public static ConfigEntry<bool> AllowSpinelTonic { get; private set; } = null!;
        public static ConfigEntry<bool> AllowGlowingMeteorite { get; private set; } = null!;
        public static ConfigEntry<bool> AllowHelfireTincture { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEffigyOfGrief { get; private set; } = null!;
        public static ConfigEntry<bool> AllowTotemOfSacrifice { get; private set; } = null!;

        // 03e. Item Pools - Elite Aspects
        public static ConfigEntry<bool> IncludeVanillaAspectsInRoulette { get; private set; } = null!;
        public static ConfigEntry<VanillaAspectsPoolMode> VanillaAspectsPoolModeSetting { get; private set; } = null!;
        public static ConfigEntry<float> WeightVanillaAspects { get; private set; } = null!;
        public static ConfigEntry<bool> IncludeAspectsInEquipmentPool { get; private set; } = null!;
        public static ConfigEntry<float> AspectDropRateInEquipmentPool { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectFire { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectLightning { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectIce { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectMalachite { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectCelestine { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectMending { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectPerfected { get; private set; } = null!;
        public static ConfigEntry<bool> AllowAspectVoid { get; private set; } = null!;
        public static ConfigEntry<string> VanillaAspectsCustomBlacklist { get; private set; } = null!;

        // 04. Mod Compatibility
        public static ConfigEntry<bool> EnableZetAspectsIntegration { get; private set; } = null!;
        public static ConfigEntry<bool> IncludeZetAspectsInPool { get; private set; } = null!;
        public static ConfigEntry<ZetAspectsPoolMode> ZetAspectsPoolModeSetting { get; private set; } = null!;
        public static ConfigEntry<float> WeightZetAspects { get; private set; } = null!;
        public static ConfigEntry<string> ZetAspectsCustomBlacklist { get; private set; } = null!;
        public static ConfigEntry<bool> ZetAspectsAllowVoidAspect { get; private set; } = null!;
        public static ConfigEntry<bool> ZetAspectsAllowLunarAspect { get; private set; } = null!;
        public static ConfigEntry<bool> ZetAspectsDuplicateExistingOnChaos { get; private set; } = null!;
        public static ConfigEntry<bool> EnableQualityIntegration { get; private set; } = null!;
        public static ConfigEntry<float> QualityRollChanceMultiplier { get; private set; } = null!;
        public static ConfigEntry<bool> AllowQualityChestSpawns { get; private set; } = null!;
        public static ConfigEntry<bool> QualityChestRewardAllPlayers { get; private set; } = null!;
        public static ConfigEntry<bool> EnableChaosQualityInfluence { get; private set; } = null!;
        public static ConfigEntry<float> ChaosQualityInfluenceFactor { get; private set; } = null!;

        // 05a. Chaos System - Settings & Odds
        public static ConfigEntry<bool> EnableChaosSystem { get; private set; } = null!;
        public static ConfigEntry<float> BaseGoodEventChance { get; private set; } = null!;
        public static ConfigEntry<float> BaseBadEventChance { get; private set; } = null!;
        public static ConfigEntry<float> LunarVoidChancePerItem { get; private set; } = null!;
        public static ConfigEntry<float> MaxLunarVoidBonusCap { get; private set; } = null!;
        public static ConfigEntry<float> CloverBonusPerItem { get; private set; } = null!;
        public static ConfigEntry<float> PurityPenaltyPerItem { get; private set; } = null!;
        public static ConfigEntry<float> LowGravityValue { get; private set; } = null!;
        public static ConfigEntry<float> TierOddsBoostMultiplier { get; private set; } = null!;
        public static ConfigEntry<float> TierOddsPenaltyMultiplier { get; private set; } = null!;
        public static ConfigEntry<bool> EnableAdminPanel { get; private set; } = null!;

        // 05b. Chaos Events - Positive
        public static ConfigEntry<bool> AllowEventDoubleItem { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventTemporaryDuplicates { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventSpectralDuplicates { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventQualityCashback { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventDoubleGold { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventNextStageFreeRolls { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventKillEnemies30 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventAllyMirror { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventHeal50Percent { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventFriendlyClone { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventElementalAspectDrop { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventTierOddsBoost { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventAlliedDroneSquad { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventForgeAnomaly { get; private set; } = null!;

        // 05c. Chaos Events - Negative
        public static ConfigEntry<bool> AllowEventLoseGold30 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventCostIncrease10 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventRandomLunarOrVoid { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventTierDownSacrifice { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventNextSpinFiftyFifty { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventHallowedCurse { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventDamageHp30 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventConvertHpToBarrier { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventEnemyItemMimic { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventEvilClone { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventRandomStageArtifact { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventCombatShrineX2 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventLowGravityMoonX2 { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventTierOddsPenalty { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventHostileDroneSquad { get; private set; } = null!;
        public static ConfigEntry<float> HostileDroneDamageMultiplier { get; private set; } = null!;
        public static ConfigEntry<float> HostileDroneHealthMultiplier { get; private set; } = null!;
        public static ConfigEntry<float> HostileDroneSpawnDistance { get; private set; } = null!;
        public static ConfigEntry<float> HostileDroneStunDuration { get; private set; } = null!;
        public static ConfigEntry<int> HostileDroneGunnerCount { get; private set; } = null!;
        public static ConfigEntry<int> HostileDroneFlameCount { get; private set; } = null!;
        public static ConfigEntry<int> HostileDroneMissileCount { get; private set; } = null!;
        public static ConfigEntry<int> HostileDroneMegaCount { get; private set; } = null!;
        public static ConfigEntry<bool> EnableHostileDroneLifetime { get; private set; } = null!;
        public static ConfigEntry<float> HostileDroneLifetimeSeconds { get; private set; } = null!;
        public static ConfigEntry<bool> AllowEventVoidCorrosion { get; private set; } = null!;

        // 05d. Chaos System - Artifact Pool
        public static ConfigEntry<bool> AllowArtifactSpite { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactWarfare { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactHonor { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactChaos { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactDissonance { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactKin { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactSwarms { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactFrailty { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactSoul { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactGlass { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactEvolution { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactVengeance { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactMetamorphosis { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactEnigma { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactSacrifice { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactCommand { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactDeath { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactDevotion { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactDelusion { get; private set; } = null!;
        public static ConfigEntry<bool> AllowArtifactRebirth { get; private set; } = null!;
        public static ConfigEntry<bool> AllowModdedArtifacts { get; private set; } = null!;
        public static ConfigEntry<string> ArtifactCustomBlacklist { get; private set; } = null!;

        // 06. Casino Roulette
        public static ConfigEntry<bool> EnableCasinoRoulette { get; private set; } = null!;
        public static ConfigEntry<bool> EnableLifeBet { get; private set; } = null!;
        public static ConfigEntry<bool> LifeBetRespectsExtraLives { get; private set; } = null!;
        public static ConfigEntry<int> LifeBetLevelStageBaseCost { get; private set; } = null!;
        public static ConfigEntry<float> CasinoWindowPosX { get; private set; } = null!;
        public static ConfigEntry<float> CasinoWindowPosY { get; private set; } = null!;

        // Quick Access KeyCode Getters
        public static KeyCode HotkeyKey => Hotkey.Value.MainKey;
        public static KeyCode VoidSpinHotkeyKey => VoidSpinHotkey.Value.MainKey;
        public static KeyCode AutoSpinHotkeyKey => AutoSpinHotkey.Value.MainKey;
        public static KeyCode VoidRerollHotkeyKey => VoidRerollHotkey.Value.MainKey != KeyCode.None ? VoidRerollHotkey.Value.MainKey : KeyCode.R;
        public static KeyCode AdminMenuHotkeyKey => AdminMenuHotkey.Value.MainKey;
        public static KeyCode CasinoToggleHotkeyKey => CasinoToggleHotkey.Value.MainKey;
        public static KeyCode NotificationJournalHotkeyKey => NotificationJournalHotkey.Value.MainKey != KeyCode.None ? NotificationJournalHotkey.Value.MainKey : KeyCode.J;

        // Backward Compatibility Forwarders
        public static ConfigEntry<bool> ScaleWithDifficulty => ScaleWithTime;
        public static ConfigEntry<int> FreeRollsOnBossKill => FreeRollsOnTeleporterCharged;

        // Multi-player Host Config Sync State
        public static bool? SyncedEnableCasinoRoulette { get; set; }
        public static bool? SyncedEnableAdminPanel { get; set; }
        public static bool? SyncedEnableChaosSystem { get; set; }
        public static bool? SyncedEnableLifeBet { get; set; }
        public static bool? SyncedEnableVoidChests { get; set; }

        // Casino Roulette
        public static bool IsCasinoRouletteActive => SyncedEnableCasinoRoulette ?? (EnableCasinoRoulette != null && EnableCasinoRoulette.Value);
        public static bool IsAdminPanelActive => SyncedEnableAdminPanel ?? EnableAdminPanel.Value;
        public static bool IsChaosSystemActive => SyncedEnableChaosSystem ?? EnableChaosSystem.Value;
        public static bool IsLifeBetActive => SyncedEnableLifeBet ?? EnableLifeBet.Value;
        public static bool IsVoidChestsActive => SyncedEnableVoidChests ?? EnableVoidChests.Value;

        public static void ResetSyncedConfig()
        {
            SyncedEnableCasinoRoulette = null;
            SyncedEnableAdminPanel = null;
            SyncedEnableChaosSystem = null;
            SyncedEnableLifeBet = null;
            SyncedEnableVoidChests = null;
        }

        public static bool IsChaosEventAllowed(ChaosEventType eventType)
        {
            switch (eventType)
            {
                case ChaosEventType.DoubleItem: return AllowEventDoubleItem.Value;
                case ChaosEventType.TemporaryDuplicates: return AllowEventTemporaryDuplicates.Value;
                case ChaosEventType.SpectralDuplicates: return AllowEventSpectralDuplicates.Value;
                case ChaosEventType.QualityCashback: return AllowEventQualityCashback.Value;
                case ChaosEventType.DoubleGold: return AllowEventDoubleGold.Value;
                case ChaosEventType.NextStageFreeRolls: return AllowEventNextStageFreeRolls.Value;
                case ChaosEventType.KillEnemies30: return AllowEventKillEnemies30.Value;
                case ChaosEventType.AllyMirror: return AllowEventAllyMirror.Value;
                case ChaosEventType.Heal50Percent: return AllowEventHeal50Percent.Value;
                case ChaosEventType.FriendlyClone: return AllowEventFriendlyClone.Value;
                case ChaosEventType.ElementalAspectDrop: return AllowEventElementalAspectDrop.Value;
                case ChaosEventType.TierOddsBoost: return AllowEventTierOddsBoost.Value;
                case ChaosEventType.AlliedDroneSquad: return AllowEventAlliedDroneSquad.Value;
                case ChaosEventType.ForgeAnomaly: return Compat.QualityCompat.IsQualityLoaded && AllowEventForgeAnomaly.Value;
                case ChaosEventType.LoseGold30: return AllowEventLoseGold30.Value;
                case ChaosEventType.CostIncrease10: return AllowEventCostIncrease10.Value;
                case ChaosEventType.RandomLunarOrVoid: return AllowEventRandomLunarOrVoid.Value;
                case ChaosEventType.TierDownSacrifice: return AllowEventTierDownSacrifice.Value;
                case ChaosEventType.NextSpinFiftyFifty: return AllowEventNextSpinFiftyFifty.Value;
                case ChaosEventType.HallowedCurse: return AllowEventHallowedCurse.Value;
                case ChaosEventType.DamageHp30: return AllowEventDamageHp30.Value;
                case ChaosEventType.ConvertHpToBarrier: return AllowEventConvertHpToBarrier.Value;
                case ChaosEventType.EnemyItemMimic: return AllowEventEnemyItemMimic.Value;
                case ChaosEventType.EvilClone: return AllowEventEvilClone.Value;
                case ChaosEventType.RandomStageArtifact: return AllowEventRandomStageArtifact.Value;
                case ChaosEventType.CombatShrineX2: return AllowEventCombatShrineX2.Value;
                case ChaosEventType.LowGravityMoonX2: return AllowEventLowGravityMoonX2.Value;
                case ChaosEventType.TierOddsPenalty: return AllowEventTierOddsPenalty.Value;
                case ChaosEventType.HostileDroneSquad: return AllowEventHostileDroneSquad.Value;
                case ChaosEventType.VoidCorrosion: return Compat.QualityCompat.IsQualityLoaded && AllowEventVoidCorrosion.Value;
                default: return true;
            }
        }
        #endregion

        public static void Init(ConfigFile config)
        {
            // ==========================================
            // 00a. Client - General & UI
            // ==========================================
            LanguageSetting = config.Bind(
                "00a. Client - General & UI",
                "Language",
                ItemRoulette.Core.ModLanguage.Auto,
                "Язык для интерфейса рулетки, уведомлений и событий (Auto / English / Russian). Auto определяет язык игры."
            );

            RequireArtifact = config.Bind(
                "00a. Client - General & UI",
                "RequireArtifact",
                true,
                "Включать механики рулетки только при выбранном 'Артефакте Рулетки' (Artifact of the Roulette) в лобби/правилах забега. При false рулетка активна всегда."
            );
            RequireArtifact.SettingChanged += (s, e) => ItemRoulette.Artifacts.RouletteGate.Tick();

            PauseGameWhileOpen = config.Bind(
                "00a. Client - General & UI",
                "PauseGameWhileOpen",
                false,
                "Приостанавливать ли одиночную игру при открытом окне рулетки (в мультиплеере всегда real-time)."
            );

            SpinDuration = config.Bind(
                "00a. Client - General & UI",
                "SpinDuration",
                5.2f,
                "Длительность анимации прокрутки рулетки в секундах."
            );

            PostSpinDisplayDuration = config.Bind(
                "00a. Client - General & UI",
                "PostSpinDisplayDuration",
                1.5f,
                "Время отображения окна рулетки после завершения вращения перед автоматическим скрытием в секундах."
            );

            AutoSpinHoldDuration = config.Bind(
                "00a. Client - General & UI",
                "AutoSpinHoldDuration",
                1.0f,
                "Время удержания клавиши (по умолчанию V) в секундах для включения или выключения режима авто-рулетки."
            );

            AutoSpinDelay = config.Bind(
                "00a. Client - General & UI",
                "AutoSpinDelay",
                0.8f,
                "Пауза в секундах между завершением предыдущей крутки и запуском следующей в режиме авто-рулетки."
            );

            ShowChanceWidgetInUI = config.Bind(
                "00a. Client - General & UI",
                "ShowChanceWidgetInUI",
                true,
                "Отображать ли цветной виджет актуальных шансов Хаоса (Удача / Бедствие) в интерфейсе рулетки."
            );

            EnableItemDescriptionAdditions = config.Bind(
                "00a. Client - General & UI",
                "EnableItemDescriptionAdditions",
                true,
                "Добавлять ли к описаниям ванильных предметов подсказки об их новых механиках в ItemRoulette (Затяжное одиночество, Кукла шанса, Звезда скидок, Клевер, Чистота)."
            );

            // ==========================================
            // 00b. Client - Controls & Hotkeys
            // ==========================================
            Hotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "Hotkey",
                new KeyboardShortcut(KeyCode.B),
                "Горячая клавиша для открытия и прокрутки стандартной рулетки предметов (по умолчанию B)."
            );

            VoidSpinHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "VoidSpinHotkey",
                new KeyboardShortcut(KeyCode.C),
                "Горячая клавиша для рулетки предметов Бездны. Одиночный клик — крутить, удержание 1.0с — отказаться."
            );

            AutoSpinHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "AutoSpinHotkey",
                new KeyboardShortcut(KeyCode.V),
                "Горячая клавиша для переключения автоматической прокрутки (зажать для включения/выключения)."
            );

            VoidRerollHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "VoidRerollHotkey",
                new KeyboardShortcut(KeyCode.R),
                "Горячая клавиша для переролла предмета Бездны в окне выбора (по умолчанию R)."
            );

            AdminMenuHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "AdminMenuHotkey",
                new KeyboardShortcut(KeyCode.F7),
                "Горячая клавиша для вызова тестового админ-меню эффектов Хаоса (по умолчанию F7)."
            );

            CasinoToggleHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "CasinoToggleHotkey",
                new KeyboardShortcut(KeyCode.N),
                "Горячая клавиша для открытия и закрытия меню Казино-рулетки (по умолчанию N)."
            );

            NotificationJournalHotkey = config.Bind(
                "00b. Client - Controls & Hotkeys",
                "NotificationJournalHotkey",
                new KeyboardShortcut(KeyCode.J),
                "Горячая клавиша для открытия и закрытия Журнала событий забега (по умолчанию J)."
            );

            // ==========================================
            // 00c. Client - UI Scale
            // ==========================================
            UiScale = config.Bind(
                "00c. Client - UI Scale",
                "UiScale",
                1.0f,
                "Общий множитель масштабирования всех элементов интерфейса мода (0.1 - 5.0, по умолчанию 1.0). Влияет на весь UI целиком."
            );

            UiScaleRoulette = config.Bind(
                "00c. Client - UI Scale",
                "UiScaleRoulette",
                1.0f,
                "Масштаб рулетки предметов и ленты казино (0.1 - 5.0, по умолчанию 1.0). Управляет размером верхнего баннера, полоски подсказки, индикатора зажатия и окон выбора."
            );

            UiScaleWindows = config.Bind(
                "00c. Client - UI Scale",
                "UiScaleWindows",
                1.0f,
                "Масштаб окон мода (0.1 - 5.0, по умолчанию 1.0). Управляет размером стола ставок казино, журнала событий забега и админ-меню."
            );

            UiScaleToasts = config.Bind(
                "00c. Client - UI Scale",
                "UiScaleToasts",
                1.0f,
                "Масштаб всплывающих уведомлений и тостов (0.1 - 5.0, по умолчанию 1.0). Управляет размером карточек событий в левом нижнем углу."
            );

            // ==========================================
            // 01. Drop Suppression
            // ==========================================
            DisableChestSpawns = config.Bind(
                "01. Drop Suppression",
                "DisableChestSpawns",
                true,
                "Отключить появление сундуков, принтеров и мультишопов директором карты (бочки, дроны и алтари остаются)."
            );

            DisableChestDrops = config.Bind(
                "01. Drop Suppression",
                "DisableChestDrops",
                true,
                "Блокировать выпадение предметов из сундуков и терминалов, если они каким-то образом открылись."
            );

            DisableBossDrops = config.Bind(
                "01. Drop Suppression",
                "DisableBossDrops",
                true,
                "Блокировать выпадение предметов с босса стадии (телепорта)."
            );

            DisableChanceShrines = config.Bind(
                "01. Drop Suppression",
                "DisableChanceShrines",
                true,
                "Отключить появление Алтарей Случая (Shrine of Chance) на локациях."
            );

            DisablePrinters = config.Bind(
                "01. Drop Suppression",
                "DisablePrinters",
                true,
                "Отключить появление 3D-принтеров на стадиях."
            );

            DisableFabricators = config.Bind(
                "01. Drop Suppression",
                "DisableFabricators",
                true,
                "Отключить появление Фабрикаторов и Адаптивных сундуков на карте."
            );

            DisableMultishops = config.Bind(
                "01. Drop Suppression",
                "DisableMultishops",
                true,
                "Отключить появление тройных терминалов выбора пассивных предметов на локациях."
            );

            DisableEquipmentChests = config.Bind(
                "01. Drop Suppression",
                "DisableEquipmentChests",
                false,
                "Отключить появление бочек и сундуков снаряжения на локациях."
            );

            DisableEquipmentMultishops = config.Bind(
                "01. Drop Suppression",
                "DisableEquipmentMultishops",
                false,
                "Отключить появление тройных терминалов активного снаряжения на локациях."
            );

            EnableVoidChests = config.Bind(
                "01. Drop Suppression",
                "EnableVoidChests",
                true,
                "Разрешить появление сундуков Бездны (Void Cradle / Void Chest / Void Triple) на карте."
            );

            EnableLunarChests = config.Bind(
                "01. Drop Suppression",
                "EnableLunarChests",
                true,
                "Разрешить появление Лунных Капсул (Lunar Pods) на карте."
            );

            // ==========================================
            // 02a. Economy - Cost Scaling
            // ==========================================
            BaseCost = config.Bind(
                "02a. Economy - Cost Scaling",
                "BaseCost",
                25,
                "Базовая стоимость прокрутки (эквивалент стоимости маленького сундука на 1 этапе)."
            );

            CostScalingMode = config.Bind(
                "02a. Economy - Cost Scaling",
                "CostScalingMode",
                CostFormula.LinearPercent,
                "Формула увеличения цены за открытия на этапе: LinearPercent (процентная), Exponential (экспонента) или Linear (фиксированная)."
            );

            RollCostIncreasePercent = config.Bind(
                "02a. Economy - Cost Scaling",
                "RollCostIncreasePercent",
                20.0f,
                "Процент прибавки к базовой цене за каждую платную крутку на текущем этапе (20.0 = +20% от базы)."
            );

            ScaleWithTime = config.Bind(
                "02a. Economy - Cost Scaling",
                "ScaleWithTime",
                true,
                "Масштабировать ли базовую стоимость крутки от времени забега и динамического таймера сложности."
            );

            TimeCostScalingMultiplier = config.Bind(
                "02a. Economy - Cost Scaling",
                "TimeCostScalingMultiplier",
                1.0f,
                "Множитель масштабирования цены от времени (1.0 = стандартный рост, 0.5 = в два раза медленнее)."
            );

            ScaleWithStage = config.Bind(
                "02a. Economy - Cost Scaling",
                "ScaleWithStage",
                true,
                "Масштабировать ли базовую стоимость от количества завершенных этапов (+X% за каждый этап)."
            );

            StageCostIncreasePercent = config.Bind(
                "02a. Economy - Cost Scaling",
                "StageCostIncreasePercent",
                10.0f,
                "Процент прибавки к базовой цене за каждый пройденный этап (10.0 = +10% за каждый этап)."
            );

            ExponentialCostMultiplier = config.Bind(
                "02a. Economy - Cost Scaling",
                "ExponentialCostMultiplier",
                1.20f,
                "Множитель цены за каждое открытие на текущем этапе при Exponential режиме (1.20 = +20% за прокрутку)."
            );

            LinearCostIncrement = config.Bind(
                "02a. Economy - Cost Scaling",
                "LinearCostIncrement",
                15,
                "Фиксированная прибавка к базовой цене за каждое открытие при Linear режиме."
            );

            ResetCostOnNewStage = config.Bind(
                "02a. Economy - Cost Scaling",
                "ResetCostOnNewStage",
                true,
                "Сбрасывать ли счетчик открытий и наценку при переходе на следующий этап."
            );

            // ==========================================
            // 02b. Economy - Free Spins
            // ==========================================
            FreeRollsPerStageBase = config.Bind(
                "02b. Economy - Free Spins",
                "FreeRollsPerStageBase",
                0,
                "Базовое количество бесплатных прокруток в начале каждого этапа (по умолчанию 0)."
            );

            FreeRollsOnTeleporterCharged = config.Bind(
                "02b. Economy - Free Spins",
                "FreeRollsOnTeleporterCharged",
                1,
                "Количество бесплатных прокруток, начисляемых за 100% зарядку телепорта этапа."
            );

            FreeRollsPerSaleStar = config.Bind(
                "02b. Economy - Free Spins",
                "FreeRollsPerSaleStar",
                1,
                "Количество бесплатных прокруток в начале каждого этапа за каждый предмет 'Звезда скидок' (Sale Star). (Используется только если отключен EnableSaleStarRework)"
            );

            EnableSaleStarRework = config.Bind(
                "02b. Economy - Free Spins",
                "EnableSaleStarRework",
                true,
                "Включить переработанную механику Выгодной звезды (взаимоисключающий выбор: сундук или босс этапа)."
            );

            SaleStarDuplicateChestSpins = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarDuplicateChestSpins",
                true,
                "Дублировать получаемые крутки рулетки при срабатывании Выгодной звезды на сундук."
            );

            SaleStarBossRewardSpinsBase = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarBossRewardSpinsBase",
                2,
                "Количество бесплатных круток босс-пула за награду с босса при 1 активной Выгодной звезде."
            );

            SaleStarBossRewardSpinsPerStack = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarBossRewardSpinsPerStack",
                1,
                "Дополнительных круток босс-пула за каждый последующий стак Выгодной звезды при победе над боссом."
            );

            SaleStarBossTriggerTimingSetting = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarBossTriggerTiming",
                SaleStarBossTriggerTiming.OnTeleporterCharged,
                "Когда срабатывает Выгодная звезда на боссе: OnTeleporterCharged (при 100% зарядке портала) или OnAllBossesDefeated (сразу при убийстве всех боссов)."
            );

            SaleStarQualityBossGrantsQualitySpins = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarQualityBossGrantsQualitySpins",
                true,
                "Если активна интеграция с модом на качество: даровать гарантированные крутки качества (Free Quality Rolls) при срабатывании качественных Выгодных звёзд на боссе."
            );

            FreeRollsPerMountainShrine = config.Bind(
                "02b. Economy - Free Spins",
                "FreeRollsPerMountainShrine",
                1,
                "Количество дополнительных бесплатных круток за каждый активированный Тотем Горы при победе над боссом."
            );

            MountainShrineGrantsBossRolls = config.Bind(
                "02b. Economy - Free Spins",
                "MountainShrineGrantsBossRolls",
                true,
                "Крутки за Тотем Горы выдаются из пула Награды Босса (85% Зелёный / 5% Красный / 10% Босс) вместо обычных круток рулетки."
            );

            SaleStarQualityGuaranteesMinimumTier = config.Bind(
                "02b. Economy - Free Spins",
                "SaleStarQualityGuaranteesMinimumTier",
                true,
                "Крутки от качественных Выгодных звёзд гарантируют выпадение предмета с качеством не ниже качества самой звезды."
            );

            BossRewardWeightUncommon = config.Bind(
                "02b. Economy - Free Spins",
                "BossRewardWeightUncommon",
                85f,
                "Вес необычных (зелёных) предметов в пуле Награды Босса (по умолчанию 85%)."
            );

            BossRewardWeightLegendary = config.Bind(
                "02b. Economy - Free Spins",
                "BossRewardWeightLegendary",
                5f,
                "Вес легендарных (красных) предметов в пуле Награды Босса (по умолчанию 5%)."
            );

            BossRewardWeightBoss = config.Bind(
                "02b. Economy - Free Spins",
                "BossRewardWeightBoss",
                10f,
                "Вес предметов Босса (жёлтых) в пуле Награды Босса (по умолчанию 10%)."
            );

            // ==========================================
            // 02c. Economy - Item Modifiers
            // ==========================================
            EnableChanceDollSupport = config.Bind(
                "02c. Economy - Item Modifiers",
                "EnableChanceDollSupport",
                true,
                "Включить поддержку предмета 'Кукла удачи' (Chance Doll / ExtraShrineItem): шанс улучшить выпавший предмет на 1 тир (белое->зелёное->красное)."
            );

            ChanceDollBaseChancePercent = config.Bind(
                "02c. Economy - Item Modifiers",
                "ChanceDollBaseChancePercent",
                5.0f,
                "Базовый шанс (%) улучшения качества предмета при наличии 1-й 'Куклы удачи' (по умолчанию 5%)."
            );

            ChanceDollStackChancePercent = config.Bind(
                "02c. Economy - Item Modifiers",
                "ChanceDollStackChancePercent",
                5.0f,
                "Дополнительный шанс (%) улучшения качества предмета за каждый последующий стак 'Куклы удачи'."
            );

            EnableLongstandingSolitudeMechanic = config.Bind(
                "02c. Economy - Item Modifiers",
                "EnableLongstandingSolitudeMechanic",
                true,
                "Включить особую механику предмета 'Затяжное одиночество' (Longstanding Solitude): бесплатные крутки с повышенным множителем редкости при повышении уровня."
            );

            LongstandingSolitudeFreeRollsBase = config.Bind(
                "02c. Economy - Item Modifiers",
                "LongstandingSolitudeFreeRollsBase",
                1,
                "Базовое количество бесплатных круток за 1-й стак 'Затяжного одиночества' при повышении уровня персонажа."
            );

            LongstandingSolitudeFreeRollsPerStack = config.Bind(
                "02c. Economy - Item Modifiers",
                "LongstandingSolitudeFreeRollsPerStack",
                1,
                "Количество дополнительных бесплатных круток за каждый следующий стак 'Затяжного одиночества' при повышении уровня."
            );

            LongstandingSolitudeRarityMultiplierBase = config.Bind(
                "02c. Economy - Item Modifiers",
                "LongstandingSolitudeRarityMultiplierBase",
                0.20f,
                "Базовый бонус к множителю редкости (+x0.20) для круток, полученных от 'Затяжного одиночества'."
            );

            LongstandingSolitudeRarityMultiplierPerStack = config.Bind(
                "02c. Economy - Item Modifiers",
                "LongstandingSolitudeRarityMultiplierPerStack",
                0.10f,
                "Дополнительный бонус к множителю редкости (+x0.10) за каждый последующий стак 'Затяжного одиночества'."
            );

            LongstandingSolitudeRewardAllPlayers = config.Bind(
                "02c. Economy - Item Modifiers",
                "LongstandingSolitudeRewardAllPlayers",
                false,
                "Выдавать ли бесплатные крутки 'Затяжного одиночества' всей команде (true) или только владельцу предмета (false)."
            );

            // ==========================================
            // 02d. Rewards - Team Share
            // ==========================================
            GoldChestRewardAllPlayers = config.Bind(
                "02d. Rewards - Team Share",
                "GoldChestRewardAllPlayers",
                true,
                "Выдавать 1 бесплатную крутку легендарных предметов всей команде при открытии Золотого сундука."
            );

            PreplacedChestRewardAllPlayers = config.Bind(
                "02d. Rewards - Team Share",
                "PreplacedChestRewardAllPlayers",
                true,
                "Выдавать 1 бесплатную стандартную крутку всей команде при открытии найденных сундуков."
            );

            SecretBossRewardAllPlayers = config.Bind(
                "02d. Rewards - Team Share",
                "SecretBossRewardAllPlayers",
                true,
                "Выдавать 1 бесплатную красную крутку всей команде при победе над секретным боссом (AWU)."
            );

            EquipmentChestRewardAllPlayers = config.Bind(
                "02d. Rewards - Team Share",
                "EquipmentChestRewardAllPlayers",
                false,
                "Выдавать 1 бесплатную крутку снаряжения всей команде при открытии сундука/бочки снаряжения."
            );

            LunarChestRewardAllPlayers = config.Bind(
                "02d. Rewards - Team Share",
                "LunarChestRewardAllPlayers",
                false,
                "Выдавать 1 бесплатную крутку лунных предметов всей команде при открытии Лунной Капсулы."
            );

            // ==========================================
            // 02e. Rewards - Confirmations & Declines
            // ==========================================
            EnableVoidCorruptChoice = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "EnableVoidCorruptChoice",
                true,
                "Запрашивать ли подтверждение при выпадении предмета Бездны или Луны: Взять или Отказаться."
            );

            VoidDeclineCostType = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidDeclineCostMode",
                VoidDeclineCostMode.MaxHealthPenalty,
                "Вариант цены за отказ: LunarCoins (Лунные монеты) или MaxHealthPenalty (потеря максимального здоровья до конца этапа)."
            );

            VoidDeclineLunarCoinsCost = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidDeclineLunarCoinsCost",
                4,
                "Стоимость очищения / отказа от предмета в Лунных монетах при режиме LunarCoins."
            );

            VoidDeclineMaxHealthPenaltyPercent = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidDeclineMaxHealthPenaltyPercent",
                30.0f,
                "Процент потери максимального здоровья за один отказ до конца этапа при режиме MaxHealthPenalty."
            );

            VoidDeclineMaxHealthPenaltyCap = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidDeclineMaxHealthPenaltyCap",
                90.0f,
                "Максимальный суммарный процент потери максимального здоровья (лимит накопления штрафа)."
            );

            EnableVoidReroll = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "EnableVoidReroll",
                true,
                "Включить ли возможность переролла выпавшего предмета Бездны в окне выбора за Лунные монеты."
            );

            VoidRerollLunarCoinsCost = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidRerollLunarCoinsCost",
                2,
                "Стоимость одного переролла предмета Бездны в Лунных монетах."
            );

            VoidRerollMaxPerItem = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "VoidRerollMaxPerItem",
                3,
                "Максимальное количество перероллов для одной выпавшей вещи Бездны."
            );

            EnableEquipmentDropChoice = config.Bind(
                "02e. Rewards - Confirmations & Declines",
                "EnableEquipmentDropChoice",
                true,
                "Запрашивать ли выбор при выпадении активного снаряжения: Взять (экипировать) или Скинуть на пол."
            );

            // ==========================================
            // 03a. Item Pools - Tier Weights
            // ==========================================
            WeightCommon = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightCommon",
                70.0f,
                "Вес выпадения обычных (белых / Tier 1) предметов."
            );

            WeightUncommon = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightUncommon",
                25.0f,
                "Вес выпадения необычных (зеленых / Tier 2) предметов."
            );

            WeightLegendary = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightLegendary",
                3.5f,
                "Вес выпадения легендарных (красных / Tier 3) предметов."
            );

            WeightBoss = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightBoss",
                1.5f,
                "Вес выпадения предметов босса (желтых)."
            );

            IncludeLunar = config.Bind(
                "03a. Item Pools - Tier Weights",
                "IncludeLunar",
                false,
                "Включить ли лунные предметы в пул стандартной рулетки."
            );

            WeightLunar = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightLunar",
                1.0f,
                "Вес выпадения лунных предметов (синих), если включены."
            );

            IncludeVoid = config.Bind(
                "03a. Item Pools - Tier Weights",
                "IncludeVoid",
                false,
                "Включить ли предметы Бездны (Void) в пул стандартной рулетки."
            );

            WeightVoid = config.Bind(
                "03a. Item Pools - Tier Weights",
                "WeightVoid",
                1.0f,
                "Вес выпадения предметов Бездны (фиолетовых), если включены."
            );

            // ==========================================
            // 03b. Item Pools - Rules & Blacklists
            // ==========================================
            IncludeLunarEquipment = config.Bind(
                "03b. Item Pools - Rules & Blacklists",
                "IncludeLunarEquipment",
                false,
                "Включать ли лунное снаряжение (активки) в пул лунных предметов рулетки."
            );

            LunarCustomBlacklist = config.Bind(
                "03b. Item Pools - Rules & Blacklists",
                "LunarCustomBlacklist",
                "",
                "Пользовательский черный список предметов (внутренние названия, токены или имена через запятую)."
            );

            // ==========================================
            // 03c. Item Pools - Lunar Items
            // ==========================================
            AllowShapedGlass = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowShapedGlass",
                true,
                "Разрешить 'Хрупкое стекло' (Shaped Glass) в пуле лунных предметов рулетки."
            );

            AllowBrittleCrown = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowBrittleCrown",
                true,
                "Разрешить 'Хрупкий венец' (Brittle Crown) в пуле лунных предметов рулетки."
            );

            AllowTranscendence = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowTranscendence",
                true,
                "Разрешить 'Трансцендентность' (Transcendence) в пуле лунных предметов рулетки."
            );

            AllowCorpsebloom = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowCorpsebloom",
                true,
                "Разрешить 'Трупный цветок' (Corpsebloom) в пуле лунных предметов рулетки."
            );

            AllowGestureOfTheDrowned = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowGestureOfTheDrowned",
                true,
                "Разрешить 'Жест утопленника' (Gesture of the Drowned) в пуле лунных предметов рулетки."
            );

            AllowBeadsOfFealty = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowBeadsOfFealty",
                true,
                "Разрешить 'Бусины верности' (Beads of Fealty) в пуле лунных предметов рулетки."
            );

            AllowFocusedConvergence = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowFocusedConvergence",
                true,
                "Разрешить 'Фокусированная конвергенция' (Focused Convergence) в пуле лунных предметов рулетки."
            );

            AllowDefiantGouge = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowDefiantGouge",
                true,
                "Разрешить 'Дерзкое долото' (Defiant Gouge) в пуле лунных предметов рулетки."
            );

            AllowMercurialRachis = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowMercurialRachis",
                true,
                "Разрешить 'Ртутный позвоночник' (Mercurial Rachis) в пуле лунных предметов рулетки."
            );

            AllowPurity = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowPurity",
                true,
                "Разрешить 'Чистота' (Purity) в пуле лунных предметов рулетки."
            );

            AllowVisionsOfHeresy = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowVisionsOfHeresy",
                true,
                "Разрешить 'Видения ереси' (Visions of Heresy) в пуле лунных предметов рулетки."
            );

            AllowHooksOfHeresy = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowHooksOfHeresy",
                true,
                "Разрешить 'Крючья ереси' (Hooks of Heresy) в пуле лунных предметов рулетки."
            );

            AllowStridesOfHeresy = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowStridesOfHeresy",
                true,
                "Разрешить 'Поступь ереси' (Strides of Heresy) в пуле лунных предметов рулетки."
            );

            AllowEssenceOfHeresy = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowEssenceOfHeresy",
                true,
                "Разрешить 'Эссенция ереси' (Essence of Heresy) в пуле лунных предметов рулетки."
            );

            AllowEulogyZero = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowEulogyZero",
                true,
                "Разрешить 'Надгробная речь' (Eulogy Zero) в пуле лунных предметов рулетки."
            );

            AllowEgocentrism = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowEgocentrism",
                true,
                "Разрешить 'Эгоцентризм' (Egocentrism) в пуле лунных предметов рулетки."
            );

            AllowStoneFluxPauldron = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowStoneFluxPauldron",
                true,
                "Разрешить 'Наплечник каменного потока' (Stone Flux Pauldron) в пуле лунных предметов рулетки."
            );

            AllowLightFluxPauldron = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowLightFluxPauldron",
                true,
                "Разрешить 'Наплечник легкого потока' (Light Flux Pauldron) в пуле лунных предметов рулетки."
            );

            AllowLongstandingSolitude = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowLongstandingSolitude",
                true,
                "Разрешить 'Затяжное одиночество' (Longstanding Solitude) в пуле лунных предметов рулетки."
            );

            AllowNeutroniumWeight = config.Bind(
                "03c. Item Pools - Lunar Items",
                "AllowNeutroniumWeight",
                true,
                "Разрешить 'Груз из нейтрония' (Neutronium Weight) в пуле лунных предметов рулетки."
            );

            // ==========================================
            // 03d. Item Pools - Lunar Equipment
            // ==========================================
            AllowSpinelTonic = config.Bind(
                "03d. Item Pools - Lunar Equipment",
                "AllowSpinelTonic",
                true,
                "Разрешить снаряжение 'Спинелевая настойка' (Spinel Tonic) в лунном пуле рулетки."
            );

            AllowGlowingMeteorite = config.Bind(
                "03d. Item Pools - Lunar Equipment",
                "AllowGlowingMeteorite",
                true,
                "Разрешить снаряжение 'Светящийся метеорит' (Glowing Meteorite) в лунном пуле рулетки."
            );

            AllowHelfireTincture = config.Bind(
                "03d. Item Pools - Lunar Equipment",
                "AllowHelfireTincture",
                true,
                "Разрешить снаряжение 'Адская настойка' (Helfire Tincture) в лунном пуле рулетки."
            );

            AllowEffigyOfGrief = config.Bind(
                "03d. Item Pools - Lunar Equipment",
                "AllowEffigyOfGrief",
                true,
                "Разрешить снаряжение 'Изваяние скорби' (Effigy of Grief) в лунном пуле рулетки."
            );

            AllowTotemOfSacrifice = config.Bind(
                "03d. Item Pools - Lunar Equipment",
                "AllowTotemOfSacrifice",
                true,
                "Разрешить снаряжение 'Тотем жертвы' (Totem of Sacrifice) в лунном пуле рулетки."
            );

            // ==========================================
            // 03e. Item Pools - Elite Aspects
            // ==========================================
            IncludeVanillaAspectsInRoulette = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "IncludeVanillaAspectsInRoulette",
                true,
                "Включить ли стандартные аспекты элитных монстров в рулетку (когда ZetAspects не активен)."
            );

            VanillaAspectsPoolModeSetting = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "VanillaAspectsPoolMode",
                VanillaAspectsPoolMode.EquipmentPool,
                "Каким образом стандартные аспекты попадают в рулетку: DedicatedTier (отдельный вес и тир), BossTier (вместе с босс-предметами), EquipmentPool (только в крутках снаряжения), или Disabled."
            );

            WeightVanillaAspects = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "WeightVanillaAspects",
                0.5f,
                "Относительный вес аспектов в платной рулетке при режиме DedicatedTier (по умолчанию 0.5)."
            );

            IncludeAspectsInEquipmentPool = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "IncludeAspectsInEquipmentPool",
                true,
                "Разрешить ли выпадение аспектов при прокрутке сундуков снаряжения."
            );

            AspectDropRateInEquipmentPool = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AspectDropRateInEquipmentPool",
                5.0f,
                "Вероятность (%) получить элитный аспект вместо обычного снаряжения при крутке снаряжения (по умолчанию 5%)."
            );

            AllowAspectFire = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectFire",
                true,
                "Разрешить выпадение аспекта 'Огненный аспект (Ифрит)' (Fire Aspect (Ifrit) / AffixRed) в рулетке."
            );

            AllowAspectLightning = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectLightning",
                true,
                "Разрешить выпадение аспекта 'Электрический аспект (Перегрузка)' (Lightning Aspect (Overloading) / AffixBlue) в рулетке."
            );

            AllowAspectIce = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectIce",
                true,
                "Разрешить выпадение аспекта 'Ледяной аспект (Холод)' (Ice Aspect (Glacial) / AffixWhite) в рулетке."
            );

            AllowAspectMalachite = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectMalachite",
                true,
                "Разрешить выпадение аспекта 'Малахитовый аспект (Яд)' (Malachite Aspect (Poison) / AffixPoison) в рулетке."
            );

            AllowAspectCelestine = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectCelestine",
                true,
                "Разрешить выпадение аспекта 'Небесный аспект (Призрак)' (Celestine Aspect (Ghost) / AffixHaunted) в рулетке."
            );

            AllowAspectMending = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectMending",
                true,
                "Разрешить выпадение аспекта 'Аспект исцеления (Земля)' (Mending Aspect (Earth) / EliteEarthEquipment) в рулетке."
            );

            AllowAspectPerfected = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectPerfected",
                true,
                "Разрешить выпадение аспекта 'Совершенный аспект (Лунный)' (Perfected Aspect (Lunar) / AffixLunar) в рулетке."
            );

            AllowAspectVoid = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "AllowAspectVoid",
                true,
                "Разрешить выпадение аспекта 'Аспект Бездны' (Void Aspect / EliteVoidEquipment) в рулетке."
            );

            VanillaAspectsCustomBlacklist = config.Bind(
                "03e. Item Pools - Elite Aspects",
                "VanillaAspectsCustomBlacklist",
                "",
                "Список названий или токенов аспектов через запятую, исключаемых из пула."
            );

            // ==========================================
            // 04. Mod Compatibility
            // ==========================================
            EnableZetAspectsIntegration = config.Bind(
                "04. Mod Compatibility",
                "EnableZetAspectsIntegration",
                true,
                "Включить интеграцию с модом ZetAspects: выдавать аспекты как пассивные предметы вместо замены экипировки."
            );

            IncludeZetAspectsInPool = config.Bind(
                "04. Mod Compatibility",
                "IncludeZetAspectsInPool",
                true,
                "Включать ли аспекты ZetAspects в пул рулетки."
            );

            ZetAspectsPoolModeSetting = config.Bind(
                "04. Mod Compatibility",
                "ZetAspectsPoolMode",
                ZetAspectsPoolMode.DedicatedTier,
                "Каким образом аспекты добавляются в рулетку: DedicatedTier (отдельный вес и тир), BossTier (в список босс-предметов), LegendaryTier (в список легендарных) или AutoByModTier."
            );

            WeightZetAspects = config.Bind(
                "04. Mod Compatibility",
                "WeightZetAspects",
                0.5f,
                "Относительный вес аспектов в платной рулетке при режиме DedicatedTier (по умолчанию 0.5)."
            );

            ZetAspectsCustomBlacklist = config.Bind(
                "04. Mod Compatibility",
                "ZetAspectsCustomBlacklist",
                "",
                "Список названий аспектов через запятую, которые будут исключены из пула рулетки."
            );

            ZetAspectsAllowVoidAspect = config.Bind(
                "04. Mod Compatibility",
                "ZetAspectsAllowVoidAspect",
                true,
                "Разрешить ли выпадение аспекта Бездны (Void) в пуле аспектов рулетки."
            );

            ZetAspectsAllowLunarAspect = config.Bind(
                "04. Mod Compatibility",
                "ZetAspectsAllowLunarAspect",
                true,
                "Разрешить ли выпадение Совершенного/Лунного аспекта (Perfected) в пуле аспектов рулетки."
            );

            ZetAspectsDuplicateExistingOnChaos = config.Bind(
                "04. Mod Compatibility",
                "ZetAspectsDuplicateExistingOnChaos",
                true,
                "Баланс ZetAspects: при выпадении аспекта из события хаоса (Elemental Aspect), если у игрока уже есть хотя бы 1 аспект, вместо нового случайного аспекта выдается случайный дубликат уже имеющегося аспекта."
            );

            EnableQualityIntegration = config.Bind(
                "04. Mod Compatibility",
                "EnableQualityIntegration",
                true,
                "Включить ли интеграцию с модом Quality (Goorakh-Quality): улучшение предметов до уровней качества."
            );

            QualityRollChanceMultiplier = config.Bind(
                "04. Mod Compatibility",
                "QualityRollChanceMultiplier",
                1.0f,
                "Множитель базового шанса улучшения качества предмета при прокрутке рулетки."
            );

            AllowQualityChestSpawns = config.Bind(
                "04. Mod Compatibility",
                "AllowQualityChestSpawns",
                true,
                "Разрешить ли появление Сундуков Качества (QualityChest1 и QualityChest2) на этапах."
            );

            QualityChestRewardAllPlayers = config.Bind(
                "04. Mod Compatibility",
                "QualityChestRewardAllPlayers",
                false,
                "Кому начисляется бесплатная крутка при открытии Сундука Качества: true = всей команде, false = только открывшему."
            );

            EnableChaosQualityInfluence = config.Bind(
                "04. Mod Compatibility",
                "EnableChaosQualityInfluence",
                true,
                "Влияет ли общий накопленный процент Хаоса игрока на шанс улучшения качества предметов."
            );

            ChaosQualityInfluenceFactor = config.Bind(
                "04. Mod Compatibility",
                "ChaosQualityInfluenceFactor",
                0.05f,
                "Прирост шанса качества за каждый 1% общего Хаоса (по умолчанию 0.05% за 1% Хаоса)."
            );

            // ==========================================
            // 05a. Chaos System - Settings & Odds
            // ==========================================
            EnableChaosSystem = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "EnableChaosSystem",
                true,
                "Включить ли систему Хаоса с двойными эффектами (предмет + событие)."
            );

            BaseGoodEventChance = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "BaseGoodEventChance",
                10.0f,
                "Базовый шанс появления Положительного события в % (по умолчанию 10%)."
            );

            BaseBadEventChance = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "BaseBadEventChance",
                10.0f,
                "Базовый шанс появления Негативного события в % (по умолчанию 10%)."
            );

            LunarVoidChancePerItem = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "LunarVoidChancePerItem",
                5.0f,
                "Прирост к шансам (Good и Bad) за каждый Синий/Фиолетовый предмет в инвентаре (в %, по умолчанию 5%)."
            );

            MaxLunarVoidBonusCap = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "MaxLunarVoidBonusCap",
                35.0f,
                "Максимальный суммарный бонус от Синих и Фиолетовых предметов (в %, по умолчанию 35%)."
            );

            CloverBonusPerItem = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "CloverBonusPerItem",
                5.0f,
                "Прирост к шансу Положительных событий за каждый Клевер (57 Leaf Clover) в инвентаре."
            );

            PurityPenaltyPerItem = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "PurityPenaltyPerItem",
                5.0f,
                "Прирост к шансу Негативных событий за каждую Чистоту (Purity) в инвентаре."
            );

            LowGravityValue = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "LowGravityValue",
                -7.5f,
                "Значение вертикальной гравитации при дебафе 'Лунная гравитация x2' (по умолчанию -7.5, нормальная -30)."
            );

            TierOddsBoostMultiplier = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "TierOddsBoostMultiplier",
                1.85f,
                "Множитель шансов зеленых, красных и босс-вещей при бафе 'Благословение редкости' (по умолчанию 1.85x)."
            );

            TierOddsPenaltyMultiplier = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "TierOddsPenaltyMultiplier",
                0.40f,
                "Множитель шансов зеленых, красных и босс-вещей при дебафе 'Проклятие ширпотреба' (по умолчанию 0.40x)."
            );

            EnableAdminPanel = config.Bind(
                "05a. Chaos System - Settings & Odds",
                "EnableAdminPanel",
                false,
                "Включить ли тестовую админ-панель эффектов Хаоса (F7)."
            );

            // ==========================================
            // 05b. Chaos Events - Positive
            // ==========================================
            AllowEventDoubleItem = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventDoubleItem",
                true,
                "Разрешить событие 'Удвоение предмета' (Double Item): Выдает вторую копию выигранного предмета."
            );

            AllowEventTemporaryDuplicates = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventTemporaryDuplicates",
                true,
                "Разрешить событие 'Временный арсенал' (Temporary Arsenal): Дает +10 временных копий предмета на 60 секунд."
            );

            AllowEventSpectralDuplicates = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventSpectralDuplicates",
                true,
                "Разрешить событие 'Спектральный резонанс' (Spectral Resonance): Дает +5 спектральных копий предмета до конца этапа."
            );

            AllowEventQualityCashback = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventQualityCashback",
                true,
                "Разрешить событие 'Кэшбэк по качеству' (Rarity Cashback): Возвращает стоимость крутки с бонусом за редкость."
            );

            AllowEventDoubleGold = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventDoubleGold",
                true,
                "Разрешить событие 'Золотой джекпот' (Gold Jackpot): Удваивает текущий баланс золота."
            );

            AllowEventNextStageFreeRolls = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventNextStageFreeRolls",
                true,
                "Разрешить событие 'Аванс фортуны' (Fortune Advance): Дает +3 бесплатные крутки на следующем этапе."
            );

            AllowEventKillEnemies30 = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventKillEnemies30",
                true,
                "Разрешить событие 'Орбитальная зачистка' (Orbital Sweep): Мгновенно уничтожает 30% монстров на карте."
            );

            AllowEventAllyMirror = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventAllyMirror",
                true,
                "Разрешить событие 'Братская щедрость' (Ally Mirror): Копирует предмет того же тира у союзника."
            );

            AllowEventHeal50Percent = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventHeal50Percent",
                true,
                "Разрешить событие 'Благодать исцеления' (Healing Grace): Восстанавливает 50% максимального здоровья."
            );

            AllowEventFriendlyClone = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventFriendlyClone",
                true,
                "Разрешить событие 'Фантомный соратник' (Phantom Ally): Призывает союзного клона-доппельгангера."
            );

            AllowEventElementalAspectDrop = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventElementalAspectDrop",
                true,
                "Разрешить событие 'Эссенция элиты' (Elite Essence): Дает случайный элитный аспект."
            );

            AllowEventTierOddsBoost = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventTierOddsBoost",
                true,
                "Разрешить событие 'Благословение редкости' (Rarity Blessing): Временно повышает шансы высоких тиров предметов."
            );

            AllowEventAlliedDroneSquad = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventAlliedDroneSquad",
                true,
                "Разрешить событие 'Звено поддержки' (Drone Squadron): Призывает звено боевых дронов-союзников."
            );

            AllowEventForgeAnomaly = config.Bind(
                "05b. Chaos Events - Positive",
                "AllowEventForgeAnomaly",
                true,
                "Разрешить событие 'Кузнечная аномалия' (Forge Anomaly): Повышает тир качества предмета на +1 (требуется мод Quality)."
            );

            // ==========================================
            // 05c. Chaos Events - Negative
            // ==========================================
            AllowEventLoseGold30 = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventLoseGold30",
                true,
                "Разрешить событие 'Ограбление' (Bandit Ambush): Отнимает 30% текущего золота."
            );

            AllowEventCostIncrease10 = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventCostIncrease10",
                true,
                "Разрешить событие 'Инфляция' (Inflation): Увеличивает стоимость круток на +10% до конца этапа."
            );

            AllowEventRandomLunarOrVoid = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventRandomLunarOrVoid",
                true,
                "Разрешить событие 'Аномальная мутация' (Anomalous Mutation): Запускает обязательную фиолетово-синюю крутку пассивных предметов Бездны и Луны."
            );

            AllowEventTierDownSacrifice = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventTierDownSacrifice",
                true,
                "Разрешить событие 'Жертва естества' (Natural Sacrifice): Заменяет случайный предмет более низким тиром."
            );

            AllowEventNextSpinFiftyFifty = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventNextSpinFiftyFifty",
                true,
                "Разрешить событие 'Пан или пропал' (All or Nothing): Устанавливает шансы следующей крутки на 50% Удача / 50% Бедствие."
            );

            AllowEventHallowedCurse = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventHallowedCurse",
                true,
                "Разрешить событие 'Проклятие пустоты' (Void Leech): Накладывает временное проклятие здоровья (Curse stack)."
            );

            AllowEventDamageHp30 = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventDamageHp30",
                true,
                "Разрешить событие 'Кровавая подать' (Blood Tax): Отнимает 30% текущего здоровья."
            );

            AllowEventConvertHpToBarrier = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventConvertHpToBarrier",
                true,
                "Разрешить событие 'Хрупкий покров' (Fragile Aegis): Временно превращает часть здоровья в барьер."
            );

            AllowEventEnemyItemMimic = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventEnemyItemMimic",
                true,
                "Разрешить событие 'Зеркало монстров' (Monster Mimic): Дарует монстрам на этапе копию предмета игрока."
            );

            AllowEventEvilClone = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventEvilClone",
                true,
                "Разрешить событие 'Злой двойник' (Evil Doppelganger): Призывает враждебного клона с предметами игрока."
            );

            AllowEventRandomStageArtifact = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventRandomStageArtifact",
                true,
                "Разрешить событие 'Аномалия этапа' (Stage Anomaly): Временно активирует случайный артефакт испытания на этапе."
            );

            AllowEventCombatShrineX2 = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventCombatShrineX2",
                true,
                "Разрешить событие 'Гнев алтарей' (Shrine Wrath): Провоцирует нападение элитных монстров (эффект алтаря битвы x2)."
            );

            AllowEventLowGravityMoonX2 = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventLowGravityMoonX2",
                true,
                "Разрешить событие 'Лунная гравитация' (Moon Gravity): Снижает гравитацию до конца этапа."
            );

            AllowEventTierOddsPenalty = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventTierOddsPenalty",
                true,
                "Разрешить событие 'Проклятие ширпотреба' (Commoner's Curse): Временно снижает шансы высоких тиров предметов."
            );

            AllowEventHostileDroneSquad = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventHostileDroneSquad",
                true,
                "Разрешить событие 'Предательство дронов' (Drone Uprising): Призывает отряд враждебных дронов."
            );

            HostileDroneDamageMultiplier = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneDamageMultiplier",
                0.10f,
                "Множитель наносимого урона враждебных дронов (по умолчанию 0.10 = 10%)."
            );

            HostileDroneHealthMultiplier = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneHealthMultiplier",
                0.10f,
                "Множитель максимального здоровья враждебных дронов (по умолчанию 0.10 = 10%)."
            );

            HostileDroneSpawnDistance = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneSpawnDistance",
                15.0f,
                "Дистанция спавна враждебных дронов от игрока в метрах (по умолчанию 15м)."
            );

            HostileDroneStunDuration = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneStunDuration",
                10.0f,
                "Длительность оглушения / перезагрузки дронов перед боем в секундах (по умолчанию 10с)."
            );

            HostileDroneGunnerCount = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneGunnerCount",
                4,
                "Количество дронов-стрелков в отряде (0 для исключения, по умолчанию 4)."
            );

            HostileDroneFlameCount = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneFlameCount",
                3,
                "Количество огнемётных дронов в отряде (0 для исключения, по умолчанию 3)."
            );

            HostileDroneMissileCount = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneMissileCount",
                2,
                "Количество ракетных дронов в отряде (0 для исключения, по умолчанию 2)."
            );

            HostileDroneMegaCount = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneMegaCount",
                1,
                "Количество Мега-дронов TC-280 в отряде (0 для исключения, по умолчанию 1)."
            );

            EnableHostileDroneLifetime = config.Bind(
                "05c. Chaos Events - Negative",
                "EnableHostileDroneLifetime",
                false,
                "Включить таймер жизни враждебных дронов (по истечении времени они самоуничтожаются)."
            );

            HostileDroneLifetimeSeconds = config.Bind(
                "05c. Chaos Events - Negative",
                "HostileDroneLifetimeSeconds",
                30.0f,
                "Время жизни враждебных дронов в секундах до самоуничтожения (по умолчанию 30с)."
            );

            AllowEventVoidCorrosion = config.Bind(
                "05c. Chaos Events - Negative",
                "AllowEventVoidCorrosion",
                true,
                "Разрешить событие 'Коррозия пустоты' (Void Corrosion): Снижает тир качества предмета на 1 (требуется мод Quality)."
            );

            // ==========================================
            // 05d. Chaos System - Artifact Pool
            // ==========================================
            AllowArtifactSpite = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactSpite",
                true,
                "Разрешить артефакт 'Ненависть' (Spite): Враги после смерти роняют бомбы."
            );

            AllowArtifactWarfare = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactWarfare",
                true,
                "Разрешить артефакт 'Война' (Warfare): Увеличивает скорость монстров (если установлен мод)."
            );

            AllowArtifactHonor = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactHonor",
                true,
                "Разрешить артефакт 'Честь' (Honor): Все монстры появляются только элитными."
            );

            AllowArtifactChaos = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactChaos",
                true,
                "Разрешить артефакт 'Хаос' (Chaos): Огонь по своим (активируется только в мультиплеере)."
            );

            AllowArtifactDissonance = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactDissonance",
                true,
                "Разрешить артефакт 'Разлад' (Dissonance): Монстры появляются вне своих обычных локаций."
            );

            AllowArtifactKin = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactKin",
                false,
                "Разрешить артефакт 'Единство' (Kin): На этапе только один вид монстров (отключен по умолчанию)."
            );

            AllowArtifactSwarms = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactSwarms",
                true,
                "Разрешить артефакт 'Рой' (Swarms): Удваивает количество монстров, уменьшает ХП в 2 раза."
            );

            AllowArtifactFrailty = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactFrailty",
                true,
                "Разрешить артефакт 'Хрупкость' (Frailty): Урон от падения смертелен."
            );

            AllowArtifactSoul = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactSoul",
                true,
                "Разрешить артефакт 'Душа' (Soul): После гибели врагов появляются огоньки."
            );

            AllowArtifactGlass = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactGlass",
                false,
                "Разрешить артефакт 'Стекло' (Glass): 500% урона, но здоровье игроков снижено до 10%."
            );

            AllowArtifactEvolution = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactEvolution",
                false,
                "Разрешить артефакт 'Эволюция' (Evolution): Монстры получают предметы между этапами."
            );

            AllowArtifactVengeance = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactVengeance",
                false,
                "Разрешить артефакт 'Возмездие' (Vengeance): Теневой клон игрока каждые 10 минут."
            );

            AllowArtifactMetamorphosis = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactMetamorphosis",
                false,
                "Разрешить артефакт 'Метаморфоза' (Metamorphosis): Случайный персонаж на каждом этапе."
            );

            AllowArtifactEnigma = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactEnigma",
                false,
                "Разрешить артефакт 'Загадка' (Enigma): Случайная экипировка при каждой активации."
            );

            AllowArtifactSacrifice = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactSacrifice",
                false,
                "Разрешить артефакт 'Жертва' (Sacrifice): Сундуки не появляются, предметы падают с монстров."
            );

            AllowArtifactCommand = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactCommand",
                false,
                "Разрешить артефакт 'Командование' (Command): Выбор предмета из меню при выпадении."
            );

            AllowArtifactDeath = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactDeath",
                false,
                "Разрешить артефакт 'Смерть' (Death): Смерть одного игрока убивает всю команду."
            );

            AllowArtifactDevotion = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactDevotion",
                false,
                "Разрешить артефакт 'Преданность' (Devotion): Яйца лемуриан вместо дронов."
            );

            AllowArtifactDelusion = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactDelusion",
                false,
                "Разрешить артефакт 'Иллюзия' (Delusion): Сброс сундуков на телепорте."
            );

            AllowArtifactRebirth = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowArtifactRebirth",
                false,
                "Разрешить артефакт 'Возрождение' (Rebirth): Сохранение предмета на следующий забег."
            );

            AllowModdedArtifacts = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "AllowModdedArtifacts",
                true,
                "Разрешить автоматическое включение в пул любых сторонних артефактов из модов."
            );

            ArtifactCustomBlacklist = config.Bind(
                "05d. Chaos System - Artifact Pool",
                "ArtifactCustomBlacklist",
                "",
                "Пользовательский чёрный список артефактов (внутренние названия или токены через запятую)."
            );

            // ==========================================
            // 06. Casino Roulette
            // ==========================================
            EnableCasinoRoulette = config.Bind(
                "06. Casino Roulette",
                "EnableCasinoRoulette",
                true,
                "Включить или отключить режим Европейской Казино-рулетки (ставки деньгами, предметами и жизнью)."
            );

            EnableLifeBet = config.Bind(
                "06. Casino Roulette",
                "EnableLifeBet",
                true,
                "Включить ли функцию 'Ставка жизни' в Казино-рулетке (поражение = смерть персонажа)."
            );

            LifeBetRespectsExtraLives = config.Bind(
                "06. Casino Roulette",
                "LifeBetRespectsExtraLives",
                false,
                "Если true, проигрыш ставки жизни активирует предметы возрождения (Dio's Best Friend, Pluripotent Larva, Seed of Life). Если false (по умолчанию) — наступает перманентная истинная смерть (TrueKill)."
            );

            LifeBetLevelStageBaseCost = config.Bind(
                "06. Casino Roulette",
                "LifeBetLevelStageBaseCost",
                25,
                "Базовая стоимость для формулы (Уровень игрока * Этап)."
            );

            CasinoWindowPosX = config.Bind(
                "06. Casino Roulette",
                "CasinoWindowPosX",
                -1f,
                "Позиция X окна казино на экране (-1 для центрирования по умолчанию)."
            );

            CasinoWindowPosY = config.Bind(
                "06. Casino Roulette",
                "CasinoWindowPosY",
                -1f,
                "Позиция Y окна казино на экране (-1 для центрирования по умолчанию)."
            );

            // Host Config Sync Event Listeners
            EnableCasinoRoulette.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            EnableAdminPanel.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            EnableChaosSystem.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            EnableLifeBet.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            EnableVoidChests.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            EnableQualityIntegration.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            AllowQualityChestSpawns.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            DisableMultishops.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            DisableEquipmentChests.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
            DisableEquipmentMultishops.SettingChanged += (s, e) => DropSuppressionHooks.BroadcastHostConfigSync();
        }
    }
}
