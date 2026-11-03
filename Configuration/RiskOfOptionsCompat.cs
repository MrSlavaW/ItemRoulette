using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace ItemRoulette.Configuration
{
    public static class RiskOfOptionsCompat
    {
        public const string PluginGuid = "com.rune580.riskofoptions";

        public static bool IsLoaded => Chainloader.PluginInfos.ContainsKey(PluginGuid);

        private static readonly HashSet<string> _registeredCategories = new HashSet<string>();

        public static void Init()
        {
            if (!IsLoaded) return;
            RegisterOptions();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RegisterOptions()
        {
            try
            {
                ModSettingsManager.SetModDescriptionToken("ITEMROULETTE_MOD_DESC");

                // Load mod icon for RiskOfOptions menu
                try
                {
                    string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                    string iconPath = Path.Combine(pluginDir, "icon.png");
                    if (File.Exists(iconPath))
                    {
                        byte[] data = File.ReadAllBytes(iconPath);
                        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                        if (ImageConversion.LoadImage(tex, data))
                        {
                            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            ModSettingsManager.SetModIcon(sprite);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Failed to load RiskOfOptions icon: {ex}");
                }

                // ==========================================
                // 00a. Client - General & UI
                // ==========================================
                AddOption(new ChoiceOption(ModConfig.LanguageSetting), "ITEMROULETTE_CONFIG_LANGUAGE_NAME", "ITEMROULETTE_CONFIG_LANGUAGE_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new CheckBoxOption(ModConfig.RequireArtifact), "ITEMROULETTE_CONFIG_REQUIREARTIFACT_NAME", "ITEMROULETTE_CONFIG_REQUIREARTIFACT_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new CheckBoxOption(ModConfig.PauseGameWhileOpen), "ITEMROULETTE_CONFIG_PAUSEGAMEWHILEOPEN_NAME", "ITEMROULETTE_CONFIG_PAUSEGAMEWHILEOPEN_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new SliderOption(ModConfig.SpinDuration, new SliderConfig { min = 1.0f, max = 10.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_SPINDURATION_NAME", "ITEMROULETTE_CONFIG_SPINDURATION_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new SliderOption(ModConfig.PostSpinDisplayDuration, new SliderConfig { min = 0.5f, max = 5.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_POSTSPINDISPLAYDURATION_NAME", "ITEMROULETTE_CONFIG_POSTSPINDISPLAYDURATION_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new SliderOption(ModConfig.AutoSpinHoldDuration, new SliderConfig { min = 0.2f, max = 3.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_AUTOSPINHOLDDURATION_NAME", "ITEMROULETTE_CONFIG_AUTOSPINHOLDDURATION_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new SliderOption(ModConfig.AutoSpinDelay, new SliderConfig { min = 0.2f, max = 3.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_AUTOSPINDELAY_NAME", "ITEMROULETTE_CONFIG_AUTOSPINDELAY_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new CheckBoxOption(ModConfig.ShowChanceWidgetInUI), "ITEMROULETTE_CONFIG_SHOWCHANCEWIDGETINUI_NAME", "ITEMROULETTE_CONFIG_SHOWCHANCEWIDGETINUI_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new CheckBoxOption(ModConfig.EnableItemDescriptionAdditions), "ITEMROULETTE_CONFIG_ENABLEITEMDESCRIPTIONADDITIONS_NAME", "ITEMROULETTE_CONFIG_ENABLEITEMDESCRIPTIONADDITIONS_DESC", "ITEMROULETTE_CAT_00A");
                AddOption(new SliderOption(ModConfig.UiScale, new SliderConfig { min = 0.1f, max = 5.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_UISCALE_NAME", "ITEMROULETTE_CONFIG_UISCALE_DESC", "ITEMROULETTE_CAT_00A");

                // ==========================================
                // 00b. Client - Controls & Hotkeys
                // ==========================================
                AddOption(new KeyBindOption(ModConfig.Hotkey), "ITEMROULETTE_CONFIG_HOTKEY_NAME", "ITEMROULETTE_CONFIG_HOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.VoidSpinHotkey), "ITEMROULETTE_CONFIG_VOIDSPINHOTKEY_NAME", "ITEMROULETTE_CONFIG_VOIDSPINHOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.AutoSpinHotkey), "ITEMROULETTE_CONFIG_AUTOSPINHOTKEY_NAME", "ITEMROULETTE_CONFIG_AUTOSPINHOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.VoidRerollHotkey), "ITEMROULETTE_CONFIG_VOIDREROLLHOTKEY_NAME", "ITEMROULETTE_CONFIG_VOIDREROLLHOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.AdminMenuHotkey), "ITEMROULETTE_CONFIG_ADMINMENUHOTKEY_NAME", "ITEMROULETTE_CONFIG_ADMINMENUHOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.CasinoToggleHotkey), "ITEMROULETTE_CONFIG_CASINOTOGGLEHOTKEY_NAME", "ITEMROULETTE_CONFIG_CASINOTOGGLEHOTKEY_DESC", "ITEMROULETTE_CAT_00B");
                AddOption(new KeyBindOption(ModConfig.NotificationJournalHotkey), "ITEMROULETTE_CONFIG_NOTIFICATIONJOURNALHOTKEY_NAME", "ITEMROULETTE_CONFIG_NOTIFICATIONJOURNALHOTKEY_DESC", "ITEMROULETTE_CAT_00B");

                // ==========================================
                // 01. Drop Suppression
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.DisableChestSpawns), "ITEMROULETTE_CONFIG_DISABLECHESTSPAWNS_NAME", "ITEMROULETTE_CONFIG_DISABLECHESTSPAWNS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableChestDrops), "ITEMROULETTE_CONFIG_DISABLECHESTDROPS_NAME", "ITEMROULETTE_CONFIG_DISABLECHESTDROPS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableBossDrops), "ITEMROULETTE_CONFIG_DISABLEBOSSDROPS_NAME", "ITEMROULETTE_CONFIG_DISABLEBOSSDROPS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableChanceShrines), "ITEMROULETTE_CONFIG_DISABLECHANCESHRINES_NAME", "ITEMROULETTE_CONFIG_DISABLECHANCESHRINES_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisablePrinters), "ITEMROULETTE_CONFIG_DISABLEPRINTERS_NAME", "ITEMROULETTE_CONFIG_DISABLEPRINTERS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableFabricators), "ITEMROULETTE_CONFIG_DISABLEFABRICATORS_NAME", "ITEMROULETTE_CONFIG_DISABLEFABRICATORS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableMultishops), "ITEMROULETTE_CONFIG_DISABLEMULTISHOPS_NAME", "ITEMROULETTE_CONFIG_DISABLEMULTISHOPS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableEquipmentChests), "ITEMROULETTE_CONFIG_DISABLEEQUIPMENTCHESTS_NAME", "ITEMROULETTE_CONFIG_DISABLEEQUIPMENTCHESTS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.DisableEquipmentMultishops), "ITEMROULETTE_CONFIG_DISABLEEQUIPMENTMULTISHOPS_NAME", "ITEMROULETTE_CONFIG_DISABLEEQUIPMENTMULTISHOPS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.EnableVoidChests), "ITEMROULETTE_CONFIG_ENABLEVOIDCHESTS_NAME", "ITEMROULETTE_CONFIG_ENABLEVOIDCHESTS_DESC", "ITEMROULETTE_CAT_01");
                AddOption(new CheckBoxOption(ModConfig.EnableLunarChests), "ITEMROULETTE_CONFIG_ENABLELUNARCHESTS_NAME", "ITEMROULETTE_CONFIG_ENABLELUNARCHESTS_DESC", "ITEMROULETTE_CAT_01");

                // ==========================================
                // 02a. Economy - Cost Scaling
                // ==========================================
                AddOption(new IntSliderOption(ModConfig.BaseCost, new IntSliderConfig { min = 5, max = 200 }), "ITEMROULETTE_CONFIG_BASECOST_NAME", "ITEMROULETTE_CONFIG_BASECOST_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new ChoiceOption(ModConfig.CostScalingMode), "ITEMROULETTE_CONFIG_COSTSCALINGMODE_NAME", "ITEMROULETTE_CONFIG_COSTSCALINGMODE_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new SliderOption(ModConfig.RollCostIncreasePercent, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_ROLLCOSTINCREASEPERCENT_NAME", "ITEMROULETTE_CONFIG_ROLLCOSTINCREASEPERCENT_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new CheckBoxOption(ModConfig.ScaleWithTime), "ITEMROULETTE_CONFIG_SCALEWITHTIME_NAME", "ITEMROULETTE_CONFIG_SCALEWITHTIME_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new SliderOption(ModConfig.TimeCostScalingMultiplier, new SliderConfig { min = 0.0f, max = 3.0f, FormatString = "{0:0.0}x" }), "ITEMROULETTE_CONFIG_TIMECOSTSCALINGMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_TIMECOSTSCALINGMULTIPLIER_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new CheckBoxOption(ModConfig.ScaleWithStage), "ITEMROULETTE_CONFIG_SCALEWITHSTAGE_NAME", "ITEMROULETTE_CONFIG_SCALEWITHSTAGE_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new SliderOption(ModConfig.StageCostIncreasePercent, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_STAGECOSTINCREASEPERCENT_NAME", "ITEMROULETTE_CONFIG_STAGECOSTINCREASEPERCENT_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new SliderOption(ModConfig.ExponentialCostMultiplier, new SliderConfig { min = 1.0f, max = 2.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_EXPONENTIALCOSTMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_EXPONENTIALCOSTMULTIPLIER_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new IntSliderOption(ModConfig.LinearCostIncrement, new IntSliderConfig { min = 1, max = 50 }), "ITEMROULETTE_CONFIG_LINEARCOSTINCREMENT_NAME", "ITEMROULETTE_CONFIG_LINEARCOSTINCREMENT_DESC", "ITEMROULETTE_CAT_02A");
                AddOption(new CheckBoxOption(ModConfig.ResetCostOnNewStage), "ITEMROULETTE_CONFIG_RESETCOSTONNEWSTAGE_NAME", "ITEMROULETTE_CONFIG_RESETCOSTONNEWSTAGE_DESC", "ITEMROULETTE_CAT_02A");

                // ==========================================
                // 02b. Economy - Free Spins
                // ==========================================
                AddOption(new IntSliderOption(ModConfig.FreeRollsPerStageBase, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_FREEROLLSPERSTAGEBASE_NAME", "ITEMROULETTE_CONFIG_FREEROLLSPERSTAGEBASE_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new IntSliderOption(ModConfig.FreeRollsOnTeleporterCharged, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_FREEROLLSONTELEPORTERCHARGED_NAME", "ITEMROULETTE_CONFIG_FREEROLLSONTELEPORTERCHARGED_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new IntSliderOption(ModConfig.FreeRollsPerSaleStar, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_FREEROLLSPERSALESTAR_NAME", "ITEMROULETTE_CONFIG_FREEROLLSPERSALESTAR_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new CheckBoxOption(ModConfig.EnableSaleStarRework), "ITEMROULETTE_CONFIG_ENABLESALESTARREWORK_NAME", "ITEMROULETTE_CONFIG_ENABLESALESTARREWORK_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new CheckBoxOption(ModConfig.SaleStarDuplicateChestSpins), "ITEMROULETTE_CONFIG_SALESTARDUPLICATECHESTSPINS_NAME", "ITEMROULETTE_CONFIG_SALESTARDUPLICATECHESTSPINS_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new IntSliderOption(ModConfig.SaleStarBossRewardSpinsBase, new IntSliderConfig { min = 1, max = 10 }), "ITEMROULETTE_CONFIG_SALESTARBOSSREWARDSPINSBASE_NAME", "ITEMROULETTE_CONFIG_SALESTARBOSSREWARDSPINSBASE_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new IntSliderOption(ModConfig.SaleStarBossRewardSpinsPerStack, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_SALESTARBOSSREWARDSPINSPERSTACK_NAME", "ITEMROULETTE_CONFIG_SALESTARBOSSREWARDSPINSPERSTACK_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new ChoiceOption(ModConfig.SaleStarBossTriggerTimingSetting), "ITEMROULETTE_CONFIG_SALESTARBOSSTRIGGERTIMING_NAME", "ITEMROULETTE_CONFIG_SALESTARBOSSTRIGGERTIMING_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new CheckBoxOption(ModConfig.SaleStarQualityBossGrantsQualitySpins), "ITEMROULETTE_CONFIG_SALESTARQUALITYBOSS_NAME", "ITEMROULETTE_CONFIG_SALESTARQUALITYBOSS_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new CheckBoxOption(ModConfig.SaleStarQualityGuaranteesMinimumTier), "ITEMROULETTE_CONFIG_SALESTARQUALITYMINTIER_NAME", "ITEMROULETTE_CONFIG_SALESTARQUALITYMINTIER_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new IntSliderOption(ModConfig.FreeRollsPerMountainShrine, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_FREEROLLSPERMOUNTAINSHRINE_NAME", "ITEMROULETTE_CONFIG_FREEROLLSPERMOUNTAINSHRINE_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new CheckBoxOption(ModConfig.MountainShrineGrantsBossRolls), "ITEMROULETTE_CONFIG_MOUNTAINSHRINEBOSSROLLS_NAME", "ITEMROULETTE_CONFIG_MOUNTAINSHRINEBOSSROLLS_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new SliderOption(ModConfig.BossRewardWeightUncommon, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTUNCOMMON_NAME", "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTUNCOMMON_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new SliderOption(ModConfig.BossRewardWeightLegendary, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTLEGENDARY_NAME", "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTLEGENDARY_DESC", "ITEMROULETTE_CAT_02B");
                AddOption(new SliderOption(ModConfig.BossRewardWeightBoss, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTBOSS_NAME", "ITEMROULETTE_CONFIG_BOSSREWARDWEIGHTBOSS_DESC", "ITEMROULETTE_CAT_02B");

                // ==========================================
                // 02c. Economy - Item Modifiers
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.EnableChanceDollSupport), "ITEMROULETTE_CONFIG_ENABLECHANCEDOLLSUPPORT_NAME", "ITEMROULETTE_CONFIG_ENABLECHANCEDOLLSUPPORT_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new SliderOption(ModConfig.ChanceDollBaseChancePercent, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_CHANCEDOLLBASECHANCEPERCENT_NAME", "ITEMROULETTE_CONFIG_CHANCEDOLLBASECHANCEPERCENT_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new SliderOption(ModConfig.ChanceDollStackChancePercent, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_CHANCEDOLLSTACKCHANCEPERCENT_NAME", "ITEMROULETTE_CONFIG_CHANCEDOLLSTACKCHANCEPERCENT_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new CheckBoxOption(ModConfig.EnableLongstandingSolitudeMechanic), "ITEMROULETTE_CONFIG_ENABLESOLITUDEMECHANIC_NAME", "ITEMROULETTE_CONFIG_ENABLESOLITUDEMECHANIC_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new IntSliderOption(ModConfig.LongstandingSolitudeFreeRollsBase, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_SOLITUDEBASE_NAME", "ITEMROULETTE_CONFIG_SOLITUDEBASE_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new IntSliderOption(ModConfig.LongstandingSolitudeFreeRollsPerStack, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_SOLITUDEPERSTACK_NAME", "ITEMROULETTE_CONFIG_SOLITUDEPERSTACK_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new SliderOption(ModConfig.LongstandingSolitudeRarityMultiplierBase, new SliderConfig { min = 0f, max = 2.0f, FormatString = "+x{0:0.00}" }), "ITEMROULETTE_CONFIG_SOLITUDEMULTBASE_NAME", "ITEMROULETTE_CONFIG_SOLITUDEMULTBASE_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new SliderOption(ModConfig.LongstandingSolitudeRarityMultiplierPerStack, new SliderConfig { min = 0f, max = 1.0f, FormatString = "+x{0:0.00}" }), "ITEMROULETTE_CONFIG_SOLITUDEMULTPERSTACK_NAME", "ITEMROULETTE_CONFIG_SOLITUDEMULTPERSTACK_DESC", "ITEMROULETTE_CAT_02C");
                AddOption(new CheckBoxOption(ModConfig.LongstandingSolitudeRewardAllPlayers), "ITEMROULETTE_CONFIG_SOLITUDEREWARDALL_NAME", "ITEMROULETTE_CONFIG_SOLITUDEREWARDALL_DESC", "ITEMROULETTE_CAT_02C");

                // ==========================================
                // 02d. Rewards - Team Share
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.GoldChestRewardAllPlayers), "ITEMROULETTE_CONFIG_GOLDCHESTREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_GOLDCHESTREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_02D");
                AddOption(new CheckBoxOption(ModConfig.PreplacedChestRewardAllPlayers), "ITEMROULETTE_CONFIG_PREPLACEDCHESTREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_PREPLACEDCHESTREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_02D");
                AddOption(new CheckBoxOption(ModConfig.SecretBossRewardAllPlayers), "ITEMROULETTE_CONFIG_SECRETBOSSREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_SECRETBOSSREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_02D");
                AddOption(new CheckBoxOption(ModConfig.EquipmentChestRewardAllPlayers), "ITEMROULETTE_CONFIG_EQUIPMENTCHESTREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_EQUIPMENTCHESTREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_02D");
                AddOption(new CheckBoxOption(ModConfig.LunarChestRewardAllPlayers), "ITEMROULETTE_CONFIG_LUNARCHESTREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_LUNARCHESTREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_02D");

                // ==========================================
                // 02e. Rewards - Confirmations & Declines
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.EnableVoidCorruptChoice), "ITEMROULETTE_CONFIG_ENABLEVOIDCORRUPTCHOICE_NAME", "ITEMROULETTE_CONFIG_ENABLEVOIDCORRUPTCHOICE_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new ChoiceOption(ModConfig.VoidDeclineCostType), "ITEMROULETTE_CONFIG_VOIDDECLINECOSTMODE_NAME", "ITEMROULETTE_CONFIG_VOIDDECLINECOSTMODE_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new IntSliderOption(ModConfig.VoidDeclineLunarCoinsCost, new IntSliderConfig { min = 0, max = 50 }), "ITEMROULETTE_CONFIG_VOIDDECLINELUNARCOINSCOST_NAME", "ITEMROULETTE_CONFIG_VOIDDECLINELUNARCOINSCOST_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new SliderOption(ModConfig.VoidDeclineMaxHealthPenaltyPercent, new SliderConfig { min = 5f, max = 90f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_VOIDDECLINEMAXHEALTHPENALTYPERCENT_NAME", "ITEMROULETTE_CONFIG_VOIDDECLINEMAXHEALTHPENALTYPERCENT_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new SliderOption(ModConfig.VoidDeclineMaxHealthPenaltyCap, new SliderConfig { min = 30f, max = 95f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_VOIDDECLINEMAXHEALTHPENALTYCAP_NAME", "ITEMROULETTE_CONFIG_VOIDDECLINEMAXHEALTHPENALTYCAP_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new CheckBoxOption(ModConfig.EnableVoidReroll), "ITEMROULETTE_CONFIG_ENABLEVOIDREROLL_NAME", "ITEMROULETTE_CONFIG_ENABLEVOIDREROLL_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new IntSliderOption(ModConfig.VoidRerollLunarCoinsCost, new IntSliderConfig { min = 0, max = 20 }), "ITEMROULETTE_CONFIG_VOIDREROLLLUNARCOINSCOST_NAME", "ITEMROULETTE_CONFIG_VOIDREROLLLUNARCOINSCOST_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new IntSliderOption(ModConfig.VoidRerollMaxPerItem, new IntSliderConfig { min = 1, max = 10 }), "ITEMROULETTE_CONFIG_VOIDREROLLMAXPERITEM_NAME", "ITEMROULETTE_CONFIG_VOIDREROLLMAXPERITEM_DESC", "ITEMROULETTE_CAT_02E");
                AddOption(new CheckBoxOption(ModConfig.EnableEquipmentDropChoice), "ITEMROULETTE_CONFIG_ENABLEEQUIPMENTDROPCHOICE_NAME", "ITEMROULETTE_CONFIG_ENABLEEQUIPMENTDROPCHOICE_DESC", "ITEMROULETTE_CAT_02E");

                // ==========================================
                // 03a. Item Pools - Tier Weights
                // ==========================================
                AddOption(new SliderOption(ModConfig.WeightCommon, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTCOMMON_NAME", "ITEMROULETTE_CONFIG_WEIGHTCOMMON_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new SliderOption(ModConfig.WeightUncommon, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTUNCOMMON_NAME", "ITEMROULETTE_CONFIG_WEIGHTUNCOMMON_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new SliderOption(ModConfig.WeightLegendary, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTLEGENDARY_NAME", "ITEMROULETTE_CONFIG_WEIGHTLEGENDARY_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new SliderOption(ModConfig.WeightBoss, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTBOSS_NAME", "ITEMROULETTE_CONFIG_WEIGHTBOSS_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new CheckBoxOption(ModConfig.IncludeLunar), "ITEMROULETTE_CONFIG_INCLUDELUNAR_NAME", "ITEMROULETTE_CONFIG_INCLUDELUNAR_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new SliderOption(ModConfig.WeightLunar, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTLUNAR_NAME", "ITEMROULETTE_CONFIG_WEIGHTLUNAR_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new CheckBoxOption(ModConfig.IncludeVoid), "ITEMROULETTE_CONFIG_INCLUDEVOID_NAME", "ITEMROULETTE_CONFIG_INCLUDEVOID_DESC", "ITEMROULETTE_CAT_03A");
                AddOption(new SliderOption(ModConfig.WeightVoid, new SliderConfig { min = 0f, max = 50f, FormatString = "{0:0}" }), "ITEMROULETTE_CONFIG_WEIGHTVOID_NAME", "ITEMROULETTE_CONFIG_WEIGHTVOID_DESC", "ITEMROULETTE_CAT_03A");

                // ==========================================
                // 03b. Item Pools - Rules & Blacklists
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.IncludeLunarEquipment), "ITEMROULETTE_CONFIG_INCLUDELUNAREQUIPMENT_NAME", "ITEMROULETTE_CONFIG_INCLUDELUNAREQUIPMENT_DESC", "ITEMROULETTE_CAT_03B");
                AddOption(new StringInputFieldOption(ModConfig.LunarCustomBlacklist), "ITEMROULETTE_CONFIG_LUNARCUSTOMBLACKLIST_NAME", "ITEMROULETTE_CONFIG_LUNARCUSTOMBLACKLIST_DESC", "ITEMROULETTE_CAT_03B");

                // ==========================================
                // 03c. Item Pools - Lunar Items
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.AllowShapedGlass), "ITEMROULETTE_CONFIG_ALLOWSHAPEDGLASS_NAME", "ITEMROULETTE_CONFIG_ALLOWSHAPEDGLASS_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowBrittleCrown), "ITEMROULETTE_CONFIG_ALLOWBRITTLECROWN_NAME", "ITEMROULETTE_CONFIG_ALLOWBRITTLECROWN_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowTranscendence), "ITEMROULETTE_CONFIG_ALLOWTRANSCENDENCE_NAME", "ITEMROULETTE_CONFIG_ALLOWTRANSCENDENCE_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowCorpsebloom), "ITEMROULETTE_CONFIG_ALLOWCORPSEBLOOM_NAME", "ITEMROULETTE_CONFIG_ALLOWCORPSEBLOOM_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowGestureOfTheDrowned), "ITEMROULETTE_CONFIG_ALLOWGESTUREOFTHEDROWNED_NAME", "ITEMROULETTE_CONFIG_ALLOWGESTUREOFTHEDROWNED_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowBeadsOfFealty), "ITEMROULETTE_CONFIG_ALLOWBEADSOFFEALTY_NAME", "ITEMROULETTE_CONFIG_ALLOWBEADSOFFEALTY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowFocusedConvergence), "ITEMROULETTE_CONFIG_ALLOWFOCUSEDCONVERGENCE_NAME", "ITEMROULETTE_CONFIG_ALLOWFOCUSEDCONVERGENCE_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowDefiantGouge), "ITEMROULETTE_CONFIG_ALLOWDEFIANTGOUGE_NAME", "ITEMROULETTE_CONFIG_ALLOWDEFIANTGOUGE_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowMercurialRachis), "ITEMROULETTE_CONFIG_ALLOWMERCURIALRACHIS_NAME", "ITEMROULETTE_CONFIG_ALLOWMERCURIALRACHIS_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowPurity), "ITEMROULETTE_CONFIG_ALLOWPURITY_NAME", "ITEMROULETTE_CONFIG_ALLOWPURITY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowVisionsOfHeresy), "ITEMROULETTE_CONFIG_ALLOWVISIONSOFHERESY_NAME", "ITEMROULETTE_CONFIG_ALLOWVISIONSOFHERESY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowHooksOfHeresy), "ITEMROULETTE_CONFIG_ALLOWHOOKSOFHERESY_NAME", "ITEMROULETTE_CONFIG_ALLOWHOOKSOFHERESY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowStridesOfHeresy), "ITEMROULETTE_CONFIG_ALLOWSTRIDESOFHERESY_NAME", "ITEMROULETTE_CONFIG_ALLOWSTRIDESOFHERESY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowEssenceOfHeresy), "ITEMROULETTE_CONFIG_ALLOWESSENCEOFHERESY_NAME", "ITEMROULETTE_CONFIG_ALLOWESSENCEOFHERESY_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowEulogyZero), "ITEMROULETTE_CONFIG_ALLOWEULOGYZERO_NAME", "ITEMROULETTE_CONFIG_ALLOWEULOGYZERO_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowEgocentrism), "ITEMROULETTE_CONFIG_ALLOWEGOCENTRISM_NAME", "ITEMROULETTE_CONFIG_ALLOWEGOCENTRISM_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowStoneFluxPauldron), "ITEMROULETTE_CONFIG_ALLOWSTONEFLUXPAULDRON_NAME", "ITEMROULETTE_CONFIG_ALLOWSTONEFLUXPAULDRON_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowLightFluxPauldron), "ITEMROULETTE_CONFIG_ALLOWLIGHTFLUXPAULDRON_NAME", "ITEMROULETTE_CONFIG_ALLOWLIGHTFLUXPAULDRON_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowLongstandingSolitude), "ITEMROULETTE_CONFIG_ALLOWLONGSTANDINGSOLITUDE_NAME", "ITEMROULETTE_CONFIG_ALLOWLONGSTANDINGSOLITUDE_DESC", "ITEMROULETTE_CAT_03C");
                AddOption(new CheckBoxOption(ModConfig.AllowNeutroniumWeight), "ITEMROULETTE_CONFIG_ALLOWNEUTRONIUMWEIGHT_NAME", "ITEMROULETTE_CONFIG_ALLOWNEUTRONIUMWEIGHT_DESC", "ITEMROULETTE_CAT_03C");

                // ==========================================
                // 03d. Item Pools - Lunar Equipment
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.AllowSpinelTonic), "ITEMROULETTE_CONFIG_ALLOWSPINELTONIC_NAME", "ITEMROULETTE_CONFIG_ALLOWSPINELTONIC_DESC", "ITEMROULETTE_CAT_03D");
                AddOption(new CheckBoxOption(ModConfig.AllowGlowingMeteorite), "ITEMROULETTE_CONFIG_ALLOWGLOWINGMETEORITE_NAME", "ITEMROULETTE_CONFIG_ALLOWGLOWINGMETEORITE_DESC", "ITEMROULETTE_CAT_03D");
                AddOption(new CheckBoxOption(ModConfig.AllowHelfireTincture), "ITEMROULETTE_CONFIG_ALLOWHELFIRETINCTURE_NAME", "ITEMROULETTE_CONFIG_ALLOWHELFIRETINCTURE_DESC", "ITEMROULETTE_CAT_03D");
                AddOption(new CheckBoxOption(ModConfig.AllowEffigyOfGrief), "ITEMROULETTE_CONFIG_ALLOWEFFIGYOFGRIEF_NAME", "ITEMROULETTE_CONFIG_ALLOWEFFIGYOFGRIEF_DESC", "ITEMROULETTE_CAT_03D");
                AddOption(new CheckBoxOption(ModConfig.AllowTotemOfSacrifice), "ITEMROULETTE_CONFIG_ALLOWTOTEMOFSACRIFICE_NAME", "ITEMROULETTE_CONFIG_ALLOWTOTEMOFSACRIFICE_DESC", "ITEMROULETTE_CAT_03D");

                // ==========================================
                // 03e. Item Pools - Elite Aspects
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.IncludeVanillaAspectsInRoulette), "ITEMROULETTE_CONFIG_INCLUDEVANILLAASPECTSINROULETTE_NAME", "ITEMROULETTE_CONFIG_INCLUDEVANILLAASPECTSINROULETTE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new ChoiceOption(ModConfig.VanillaAspectsPoolModeSetting), "ITEMROULETTE_CONFIG_VANILLAASPECTSPOOLMODE_NAME", "ITEMROULETTE_CONFIG_VANILLAASPECTSPOOLMODE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new SliderOption(ModConfig.WeightVanillaAspects, new SliderConfig { min = 0.0f, max = 10.0f, FormatString = "{0:0.0}" }), "ITEMROULETTE_CONFIG_WEIGHTVANILLAASPECTS_NAME", "ITEMROULETTE_CONFIG_WEIGHTVANILLAASPECTS_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.IncludeAspectsInEquipmentPool), "ITEMROULETTE_CONFIG_INCLUDEASPECTSINEQUIPMENTPOOL_NAME", "ITEMROULETTE_CONFIG_INCLUDEASPECTSINEQUIPMENTPOOL_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new SliderOption(ModConfig.AspectDropRateInEquipmentPool, new SliderConfig { min = 0.0f, max = 50.0f, FormatString = "{0:0.0}%" }), "ITEMROULETTE_CONFIG_ASPECTDROPRATEINEQUIPMENTPOOL_NAME", "ITEMROULETTE_CONFIG_ASPECTDROPRATEINEQUIPMENTPOOL_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectFire), "ITEMROULETTE_CONFIG_ALLOWASPECTFIRE_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTFIRE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectLightning), "ITEMROULETTE_CONFIG_ALLOWASPECTLIGHTNING_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTLIGHTNING_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectIce), "ITEMROULETTE_CONFIG_ALLOWASPECTICE_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTICE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectMalachite), "ITEMROULETTE_CONFIG_ALLOWASPECTMALACHITE_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTMALACHITE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectCelestine), "ITEMROULETTE_CONFIG_ALLOWASPECTCELESTINE_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTCELESTINE_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectMending), "ITEMROULETTE_CONFIG_ALLOWASPECTMENDING_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTMENDING_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectPerfected), "ITEMROULETTE_CONFIG_ALLOWASPECTPERFECTED_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTPERFECTED_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new CheckBoxOption(ModConfig.AllowAspectVoid), "ITEMROULETTE_CONFIG_ALLOWASPECTVOID_NAME", "ITEMROULETTE_CONFIG_ALLOWASPECTVOID_DESC", "ITEMROULETTE_CAT_03E");
                AddOption(new StringInputFieldOption(ModConfig.VanillaAspectsCustomBlacklist), "ITEMROULETTE_CONFIG_VANILLAASPECTSCUSTOMBLACKLIST_NAME", "ITEMROULETTE_CONFIG_VANILLAASPECTSCUSTOMBLACKLIST_DESC", "ITEMROULETTE_CAT_03E");

                // ==========================================
                // 04. Mod Compatibility
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.EnableZetAspectsIntegration), "ITEMROULETTE_CONFIG_ENABLEZETASPECTSINTEGRATION_NAME", "ITEMROULETTE_CONFIG_ENABLEZETASPECTSINTEGRATION_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.IncludeZetAspectsInPool), "ITEMROULETTE_CONFIG_INCLUDEZETASPECTSINPOOL_NAME", "ITEMROULETTE_CONFIG_INCLUDEZETASPECTSINPOOL_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new ChoiceOption(ModConfig.ZetAspectsPoolModeSetting), "ITEMROULETTE_CONFIG_ZETASPECTSPOOLMODE_NAME", "ITEMROULETTE_CONFIG_ZETASPECTSPOOLMODE_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new SliderOption(ModConfig.WeightZetAspects, new SliderConfig { min = 0.0f, max = 10.0f, FormatString = "{0:0.0}" }), "ITEMROULETTE_CONFIG_WEIGHTZETASPECTS_NAME", "ITEMROULETTE_CONFIG_WEIGHTZETASPECTS_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new StringInputFieldOption(ModConfig.ZetAspectsCustomBlacklist), "ITEMROULETTE_CONFIG_ZETASPECTSCUSTOMBLACKLIST_NAME", "ITEMROULETTE_CONFIG_ZETASPECTSCUSTOMBLACKLIST_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.ZetAspectsAllowVoidAspect), "ITEMROULETTE_CONFIG_ZETASPECTSALLOWVOIDASPECT_NAME", "ITEMROULETTE_CONFIG_ZETASPECTSALLOWVOIDASPECT_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.ZetAspectsAllowLunarAspect), "ITEMROULETTE_CONFIG_ZETASPECTSALLOWLUNARASPECT_NAME", "ITEMROULETTE_CONFIG_ZETASPECTSALLOWLUNARASPECT_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.ZetAspectsDuplicateExistingOnChaos), "ITEMROULETTE_CONFIG_ZETASPECTSDUPLICATEEXISTINGONCHAOS_NAME", "ITEMROULETTE_CONFIG_ZETASPECTSDUPLICATEEXISTINGONCHAOS_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.EnableQualityIntegration), "ITEMROULETTE_CONFIG_ENABLEQUALITYINTEGRATION_NAME", "ITEMROULETTE_CONFIG_ENABLEQUALITYINTEGRATION_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new SliderOption(ModConfig.QualityRollChanceMultiplier, new SliderConfig { min = 0.0f, max = 5.0f, FormatString = "{0:0.0}x" }), "ITEMROULETTE_CONFIG_QUALITYROLLCHANCEMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_QUALITYROLLCHANCEMULTIPLIER_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.AllowQualityChestSpawns), "ITEMROULETTE_CONFIG_ALLOWQUALITYCHESTSPAWNS_NAME", "ITEMROULETTE_CONFIG_ALLOWQUALITYCHESTSPAWNS_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.QualityChestRewardAllPlayers), "ITEMROULETTE_CONFIG_QUALITYCHESTREWARDALLPLAYERS_NAME", "ITEMROULETTE_CONFIG_QUALITYCHESTREWARDALLPLAYERS_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new CheckBoxOption(ModConfig.EnableChaosQualityInfluence), "ITEMROULETTE_CONFIG_ENABLECHAOSQUALITYINFLUENCE_NAME", "ITEMROULETTE_CONFIG_ENABLECHAOSQUALITYINFLUENCE_DESC", "ITEMROULETTE_CAT_04");
                AddOption(new SliderOption(ModConfig.ChaosQualityInfluenceFactor, new SliderConfig { min = 0.0f, max = 0.5f, FormatString = "{0:0.00}%" }), "ITEMROULETTE_CONFIG_CHAOSQUALITYINFLUENCEFACTOR_NAME", "ITEMROULETTE_CONFIG_CHAOSQUALITYINFLUENCEFACTOR_DESC", "ITEMROULETTE_CAT_04");

                // ==========================================
                // 05a. Chaos System - Settings & Odds
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.EnableChaosSystem), "ITEMROULETTE_CONFIG_ENABLECHAOSSYSTEM_NAME", "ITEMROULETTE_CONFIG_ENABLECHAOSSYSTEM_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.BaseGoodEventChance, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_BASEGOODEVENTCHANCE_NAME", "ITEMROULETTE_CONFIG_BASEGOODEVENTCHANCE_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.BaseBadEventChance, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_BASEBADEVENTCHANCE_NAME", "ITEMROULETTE_CONFIG_BASEBADEVENTCHANCE_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.LunarVoidChancePerItem, new SliderConfig { min = 0f, max = 25f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_LUNARVOIDCHANCEPERITEM_NAME", "ITEMROULETTE_CONFIG_LUNARVOIDCHANCEPERITEM_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.MaxLunarVoidBonusCap, new SliderConfig { min = 0f, max = 100f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_MAXLUNARVOIDBONUSCAP_NAME", "ITEMROULETTE_CONFIG_MAXLUNARVOIDBONUSCAP_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.CloverBonusPerItem, new SliderConfig { min = 0f, max = 25f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_CLOVERBONUSPERITEM_NAME", "ITEMROULETTE_CONFIG_CLOVERBONUSPERITEM_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.PurityPenaltyPerItem, new SliderConfig { min = 0f, max = 25f, FormatString = "{0:0}%" }), "ITEMROULETTE_CONFIG_PURITYPENALTYPERITEM_NAME", "ITEMROULETTE_CONFIG_PURITYPENALTYPERITEM_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.LowGravityValue, new SliderConfig { min = -30.0f, max = 0.0f, FormatString = "{0:0.0}" }), "ITEMROULETTE_CONFIG_LOWGRAVITYVALUE_NAME", "ITEMROULETTE_CONFIG_LOWGRAVITYVALUE_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.TierOddsBoostMultiplier, new SliderConfig { min = 1.0f, max = 3.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_TIERODDSBOOSTMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_TIERODDSBOOSTMULTIPLIER_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new SliderOption(ModConfig.TierOddsPenaltyMultiplier, new SliderConfig { min = 0.1f, max = 1.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_TIERODDSPENALTYMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_TIERODDSPENALTYMULTIPLIER_DESC", "ITEMROULETTE_CAT_05A");
                AddOption(new CheckBoxOption(ModConfig.EnableAdminPanel), "ITEMROULETTE_CONFIG_ENABLEADMINPANEL_NAME", "ITEMROULETTE_CONFIG_ENABLEADMINPANEL_DESC", "ITEMROULETTE_CAT_05A");

                // ==========================================
                // 05b. Chaos Events - Positive
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.AllowEventDoubleItem), "ITEMROULETTE_CONFIG_ALLOWEVENTDOUBLEITEM_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTDOUBLEITEM_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventTemporaryDuplicates), "ITEMROULETTE_CONFIG_ALLOWEVENTTEMPORARYDUPLICATES_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTTEMPORARYDUPLICATES_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventSpectralDuplicates), "ITEMROULETTE_CONFIG_ALLOWEVENTSPECTRALDUPLICATES_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTSPECTRALDUPLICATES_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventQualityCashback), "ITEMROULETTE_CONFIG_ALLOWEVENTQUALITYCASHBACK_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTQUALITYCASHBACK_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventDoubleGold), "ITEMROULETTE_CONFIG_ALLOWEVENTDOUBLEGOLD_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTDOUBLEGOLD_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventNextStageFreeRolls), "ITEMROULETTE_CONFIG_ALLOWEVENTNEXTSTAGEFREEROLLS_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTNEXTSTAGEFREEROLLS_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventKillEnemies30), "ITEMROULETTE_CONFIG_ALLOWEVENTKILLENEMIES30_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTKILLENEMIES30_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventAllyMirror), "ITEMROULETTE_CONFIG_ALLOWEVENTALLYMIRROR_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTALLYMIRROR_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventHeal50Percent), "ITEMROULETTE_CONFIG_ALLOWEVENTHEAL50PERCENT_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTHEAL50PERCENT_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventFriendlyClone), "ITEMROULETTE_CONFIG_ALLOWEVENTFRIENDLYCLONE_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTFRIENDLYCLONE_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventElementalAspectDrop), "ITEMROULETTE_CONFIG_ALLOWEVENTELEMENTALASPECTDROP_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTELEMENTALASPECTDROP_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventTierOddsBoost), "ITEMROULETTE_CONFIG_ALLOWEVENTTIERODDSBOOST_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTTIERODDSBOOST_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventAlliedDroneSquad), "ITEMROULETTE_CONFIG_ALLOWEVENTALLIEDDRONESQUAD_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTALLIEDDRONESQUAD_DESC", "ITEMROULETTE_CAT_05B");
                AddOption(new CheckBoxOption(ModConfig.AllowEventForgeAnomaly), "ITEMROULETTE_CONFIG_ALLOWEVENTFORGEANOMALY_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTFORGEANOMALY_DESC", "ITEMROULETTE_CAT_05B");

                // ==========================================
                // 05c. Chaos Events - Negative
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.AllowEventLoseGold30), "ITEMROULETTE_CONFIG_ALLOWEVENTLOSEGOLD30_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTLOSEGOLD30_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventCostIncrease10), "ITEMROULETTE_CONFIG_ALLOWEVENTCOSTINCREASE10_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTCOSTINCREASE10_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventRandomLunarOrVoid), "ITEMROULETTE_CONFIG_ALLOWEVENTRANDOMLUNARORVOID_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTRANDOMLUNARORVOID_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventTierDownSacrifice), "ITEMROULETTE_CONFIG_ALLOWEVENTTIERDOWNSACRIFICE_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTTIERDOWNSACRIFICE_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventNextSpinFiftyFifty), "ITEMROULETTE_CONFIG_ALLOWEVENTNEXTSPINFIFTYFIFTY_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTNEXTSPINFIFTYFIFTY_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventHallowedCurse), "ITEMROULETTE_CONFIG_ALLOWEVENTHALLOWEDCURSE_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTHALLOWEDCURSE_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventDamageHp30), "ITEMROULETTE_CONFIG_ALLOWEVENTDAMAGEHP30_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTDAMAGEHP30_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventConvertHpToBarrier), "ITEMROULETTE_CONFIG_ALLOWEVENTCONVERTHPTOBARRIER_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTCONVERTHPTOBARRIER_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventEnemyItemMimic), "ITEMROULETTE_CONFIG_ALLOWEVENTENEMYITEMMIMIC_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTENEMYITEMMIMIC_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventEvilClone), "ITEMROULETTE_CONFIG_ALLOWEVENTEVILCLONE_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTEVILCLONE_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventRandomStageArtifact), "ITEMROULETTE_CONFIG_ALLOWEVENTRANDOMSTAGEARTIFACT_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTRANDOMSTAGEARTIFACT_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventCombatShrineX2), "ITEMROULETTE_CONFIG_ALLOWEVENTCOMBATSHRINEX2_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTCOMBATSHRINEX2_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventLowGravityMoonX2), "ITEMROULETTE_CONFIG_ALLOWEVENTLOWGRAVITYMOONX2_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTLOWGRAVITYMOONX2_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventTierOddsPenalty), "ITEMROULETTE_CONFIG_ALLOWEVENTTIERODDSPENALTY_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTTIERODDSPENALTY_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventHostileDroneSquad), "ITEMROULETTE_CONFIG_ALLOWEVENTHOSTILEDRONESQUAD_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTHOSTILEDRONESQUAD_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new SliderOption(ModConfig.HostileDroneDamageMultiplier, new SliderConfig { min = 0.01f, max = 2.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_HOSTILEDRONEDAMAGEMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEDAMAGEMULTIPLIER_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new SliderOption(ModConfig.HostileDroneHealthMultiplier, new SliderConfig { min = 0.01f, max = 2.0f, FormatString = "{0:0.00}x" }), "ITEMROULETTE_CONFIG_HOSTILEDRONEHEALTHMULTIPLIER_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEHEALTHMULTIPLIER_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new SliderOption(ModConfig.HostileDroneSpawnDistance, new SliderConfig { min = 5.0f, max = 50.0f, FormatString = "{0:0.0}m" }), "ITEMROULETTE_CONFIG_HOSTILEDRONESPAWNDISTANCE_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONESPAWNDISTANCE_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new SliderOption(ModConfig.HostileDroneStunDuration, new SliderConfig { min = 0.0f, max = 30.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_HOSTILEDRONESTUNDURATION_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONESTUNDURATION_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new IntSliderOption(ModConfig.HostileDroneGunnerCount, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_HOSTILEDRONEGUNNERCOUNT_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEGUNNERCOUNT_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new IntSliderOption(ModConfig.HostileDroneFlameCount, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_HOSTILEDRONEFLAMECOUNT_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEFLAMECOUNT_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new IntSliderOption(ModConfig.HostileDroneMissileCount, new IntSliderConfig { min = 0, max = 10 }), "ITEMROULETTE_CONFIG_HOSTILEDRONEMISSILECOUNT_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEMISSILECOUNT_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new IntSliderOption(ModConfig.HostileDroneMegaCount, new IntSliderConfig { min = 0, max = 5 }), "ITEMROULETTE_CONFIG_HOSTILEDRONEMEGACOUNT_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONEMEGACOUNT_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.EnableHostileDroneLifetime), "ITEMROULETTE_CONFIG_ENABLEHOSTILEDRONELIFETIME_NAME", "ITEMROULETTE_CONFIG_ENABLEHOSTILEDRONELIFETIME_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new SliderOption(ModConfig.HostileDroneLifetimeSeconds, new SliderConfig { min = 5.0f, max = 300.0f, FormatString = "{0:0.0}s" }), "ITEMROULETTE_CONFIG_HOSTILEDRONELIFETIMESECONDS_NAME", "ITEMROULETTE_CONFIG_HOSTILEDRONELIFETIMESECONDS_DESC", "ITEMROULETTE_CAT_05C");
                AddOption(new CheckBoxOption(ModConfig.AllowEventVoidCorrosion), "ITEMROULETTE_CONFIG_ALLOWEVENTVOIDCORROSION_NAME", "ITEMROULETTE_CONFIG_ALLOWEVENTVOIDCORROSION_DESC", "ITEMROULETTE_CAT_05C");

                // ==========================================
                // 05d. Chaos System - Artifact Pool
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactSpite), "ITEMROULETTE_CONFIG_ALLOWARTIFACTSPITE_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTSPITE_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactWarfare), "ITEMROULETTE_CONFIG_ALLOWARTIFACTWARFARE_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTWARFARE_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactHonor), "ITEMROULETTE_CONFIG_ALLOWARTIFACTHONOR_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTHONOR_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactChaos), "ITEMROULETTE_CONFIG_ALLOWARTIFACTCHAOS_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTCHAOS_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactDissonance), "ITEMROULETTE_CONFIG_ALLOWARTIFACTDISSONANCE_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTDISSONANCE_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactKin), "ITEMROULETTE_CONFIG_ALLOWARTIFACTKIN_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTKIN_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactSwarms), "ITEMROULETTE_CONFIG_ALLOWARTIFACTSWARMS_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTSWARMS_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactFrailty), "ITEMROULETTE_CONFIG_ALLOWARTIFACTFRAILTY_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTFRAILTY_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactSoul), "ITEMROULETTE_CONFIG_ALLOWARTIFACTSOUL_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTSOUL_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactGlass), "ITEMROULETTE_CONFIG_ALLOWARTIFACTGLASS_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTGLASS_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactEvolution), "ITEMROULETTE_CONFIG_ALLOWARTIFACTEVOLUTION_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTEVOLUTION_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactVengeance), "ITEMROULETTE_CONFIG_ALLOWARTIFACTVENGEANCE_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTVENGEANCE_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactMetamorphosis), "ITEMROULETTE_CONFIG_ALLOWARTIFACTMETAMORPHOSIS_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTMETAMORPHOSIS_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactEnigma), "ITEMROULETTE_CONFIG_ALLOWARTIFACTENIGMA_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTENIGMA_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactSacrifice), "ITEMROULETTE_CONFIG_ALLOWARTIFACTSACRIFICE_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTSACRIFICE_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactCommand), "ITEMROULETTE_CONFIG_ALLOWARTIFACTCOMMAND_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTCOMMAND_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactDeath), "ITEMROULETTE_CONFIG_ALLOWARTIFACTDEATH_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTDEATH_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactDevotion), "ITEMROULETTE_CONFIG_ALLOWARTIFACTDEVOTION_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTDEVOTION_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactDelusion), "ITEMROULETTE_CONFIG_ALLOWARTIFACTDELUSION_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTDELUSION_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowArtifactRebirth), "ITEMROULETTE_CONFIG_ALLOWARTIFACTREBIRTH_NAME", "ITEMROULETTE_CONFIG_ALLOWARTIFACTREBIRTH_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new CheckBoxOption(ModConfig.AllowModdedArtifacts), "ITEMROULETTE_CONFIG_ALLOWMODDEDARTIFACTS_NAME", "ITEMROULETTE_CONFIG_ALLOWMODDEDARTIFACTS_DESC", "ITEMROULETTE_CAT_05D");
                AddOption(new StringInputFieldOption(ModConfig.ArtifactCustomBlacklist), "ITEMROULETTE_CONFIG_ARTIFACTCUSTOMBLACKLIST_NAME", "ITEMROULETTE_CONFIG_ARTIFACTCUSTOMBLACKLIST_DESC", "ITEMROULETTE_CAT_05D");

                // ==========================================
                // 06. Casino Roulette
                // ==========================================
                AddOption(new CheckBoxOption(ModConfig.EnableCasinoRoulette), "ITEMROULETTE_CONFIG_ENABLECASINOROULETTE_NAME", "ITEMROULETTE_CONFIG_ENABLECASINOROULETTE_DESC", "ITEMROULETTE_CAT_06");
                AddOption(new CheckBoxOption(ModConfig.EnableLifeBet), "ITEMROULETTE_CONFIG_ENABLELIFEBET_NAME", "ITEMROULETTE_CONFIG_ENABLELIFEBET_DESC", "ITEMROULETTE_CAT_06");
                AddOption(new CheckBoxOption(ModConfig.LifeBetRespectsExtraLives), "ITEMROULETTE_CONFIG_LIFEBETRESPECTSEXTRALIVES_NAME", "ITEMROULETTE_CONFIG_LIFEBETRESPECTSEXTRALIVES_DESC", "ITEMROULETTE_CAT_06");
                AddOption(new IntSliderOption(ModConfig.LifeBetLevelStageBaseCost, new IntSliderConfig { min = 5, max = 100 }), "ITEMROULETTE_CONFIG_LIFEBETLEVELSTAGEBASECOST_NAME", "ITEMROULETTE_CONFIG_LIFEBETLEVELSTAGEBASECOST_DESC", "ITEMROULETTE_CAT_06");

            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to register RiskOfOptions: {ex}");
            }
        }

        private static void AddOption(BaseOption option, string nameToken, string descToken, string catToken)
        {
            ModSettingsManager.AddOption(option, ItemRoulettePlugin.PluginGUID, ItemRoulettePlugin.PluginName, nameToken, descToken);
            if (!string.IsNullOrEmpty(catToken) && _registeredCategories.Add(option.Category))
            {
                ModSettingsManager.SetCategoryNameToken(ItemRoulettePlugin.PluginGUID, option, catToken);
            }
        }
    }
}
