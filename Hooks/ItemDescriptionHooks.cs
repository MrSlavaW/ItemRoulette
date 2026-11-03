using System;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module: Appends non-destructive contextual explanations of ItemRoulette mechanics
    /// to vanilla/DLC item descriptions in tooltips, inspect panels, and logbook.
    /// </summary>
    public static class ItemDescriptionHooks
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            On.RoR2.Language.GetLocalizedStringByToken += OnGetLocalizedStringByToken;

            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            On.RoR2.Language.GetLocalizedStringByToken -= OnGetLocalizedStringByToken;

            _initialized = false;
        }

        private static string OnGetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, RoR2.Language self, string token)
        {
            string originalText = orig(self, token);
            if (!ModConfig.EnableItemDescriptionAdditions.Value || string.IsNullOrEmpty(originalText))
            {
                return originalText;
            }

            if (originalText.Contains("[ItemRoulette]"))
            {
                return originalText;
            }

            if (TryGetModDescription(token, out string extraDesc))
            {
                return originalText + "\n\n" + extraDesc;
            }

            return originalText;
        }

        private static bool TryGetModDescription(string token, out string extraDesc)
        {
            extraDesc = string.Empty;
            if (string.IsNullOrEmpty(token)) return false;

            switch (token)
            {
                case "ITEM_ONLEVELUPFREEUNLOCK_DESC":
                    if (!ModConfig.EnableLongstandingSolitudeMechanic.Value) return false;
                    int baseRolls = ModConfig.LongstandingSolitudeFreeRollsBase.Value;
                    int perStackRolls = ModConfig.LongstandingSolitudeFreeRollsPerStack.Value;
                    float baseMult = ModConfig.LongstandingSolitudeRarityMultiplierBase.Value;
                    float perStackMult = ModConfig.LongstandingSolitudeRarityMultiplierPerStack.Value;
                    extraDesc = Localization.Format(
                        "<style=cSub><color=#4ADEDE>[ItemRoulette]</color> При повышении уровня даёт <color=#FFFFFF>{0}</color> бесплатную крутку рулетки (<color=#FFFFFF>+{1}</color> за стак) с бонусом <color=#4ADEDE>+x{2:F2}</color> (<color=#4ADEDE>+{3:F2}</color> за стак) к множителю редкости.</style>",
                        "<style=cSub><color=#4ADEDE>[ItemRoulette]</color> On level up, grants <color=#FFFFFF>{0}</color> free roulette spin (<color=#FFFFFF>+{1}</color> per stack) with a <color=#4ADEDE>+x{2:F2}</color> (<color=#4ADEDE>+{3:F2}</color> per stack) rarity multiplier bonus.</style>",
                        baseRolls, perStackRolls, baseMult, perStackMult);
                    return true;

                case "ITEM_EXTRASHRINEITEM_DESC":
                    if (!ModConfig.EnableChanceDollSupport.Value) return false;
                    float baseChance = ModConfig.ChanceDollBaseChancePercent.Value;
                    float stackChance = ModConfig.ChanceDollStackChancePercent.Value;
                    extraDesc = Localization.Format(
                        "<style=cSub><color=#38E54D>[ItemRoulette]</color> Даёт <color=#38E54D>{0:F0}%</color> шанс (<color=#38E54D>+{1:F0}%</color> за стак) улучшить качество выпавшего в рулетке предмета на +1 тир (Обычный → Необычный → Легендарный).</style>",
                        "<style=cSub><color=#38E54D>[ItemRoulette]</color> Grants a <color=#38E54D>{0:F0}%</color> chance (<color=#38E54D>+{1:F0}%</color> per stack) to upgrade the won item's tier in the roulette (Common → Uncommon → Legendary).</style>",
                        baseChance, stackChance);
                    return true;

                case "ITEM_LOWERPRICEDCHESTS_DESC":
                    if (ModConfig.EnableSaleStarRework.Value)
                    {
                        string timingStrRu = ModConfig.SaleStarBossTriggerTimingSetting.Value == SaleStarBossTriggerTiming.OnAllBossesDefeated 
                            ? "при убийстве боссов" 
                            : "при зарядке портала";
                        string timingStrEn = ModConfig.SaleStarBossTriggerTimingSetting.Value == SaleStarBossTriggerTiming.OnAllBossesDefeated 
                            ? "on all bosses defeated" 
                            : "on teleporter charged";
                        string qualityNote = (ItemRoulette.Compat.QualityCompat.IsQualityLoaded && ModConfig.SaleStarQualityGuaranteesMinimumTier.Value)
                            ? Localization.Get(" Качественные звёзды гарантируют минимальное качество крутки не ниже качества самой звезды.", " Quality Stars guarantee minimum spin quality equal to the star's tier.")
                            : "";
                        string timingStr = Localization.IsRussian ? timingStrRu : timingStrEn;
                        extraDesc = Localization.Format(
                            "<style=cSub><color=#8CE6FF>[ItemRoulette]</color> Звезда может быть потрачена либо на сундук (<color=#FFFFFF>дублируя</color> получаемые крутки), либо на награду с босса этапа {0} (даруя <color=#FFFFFF>+1</color> за стак бесплатную крутку <color=#FFCE31>Награды Босса</color>: 85% Зелёный / 5% Красный / 10% Босс).{1} Срабатывание на одном источнике блокирует срабатывание на другом до следующего этапа.</style>",
                            "<style=cSub><color=#8CE6FF>[ItemRoulette]</color> Can be consumed either on a chest (<color=#FFFFFF>duplicating</color> awarded spins) or on stage boss reward {0} (granting <color=#FFFFFF>+1</color> per stack free <color=#FFCE31>Boss Reward</color> spin: 85% Green / 5% Red / 10% Boss).{1} Consuming on one source blocks it for the other until next stage.</style>",
                            timingStr, qualityNote);
                        return true;
                    }
                    else
                    {
                        int freePerStar = ModConfig.FreeRollsPerSaleStar.Value;
                        if (freePerStar <= 0) return false;
                        extraDesc = Localization.Format(
                            "<style=cSub><color=#8CE6FF>[ItemRoulette]</color> В начале каждого этапа дарует <color=#FFFFFF>+{0}</color> бесплатную крутку рулетки за каждый стак предмета.</style>",
                            "<style=cSub><color=#8CE6FF>[ItemRoulette]</color> At the start of each stage, grants <color=#FFFFFF>+{0}</color> free roulette spin per item stack.</style>",
                            freePerStar);
                        return true;
                    }

                case "ITEM_CLOVER_DESC":
                    if (!ModConfig.IsChaosSystemActive) return false;
                    float cloverBonus = ModConfig.CloverBonusPerItem.Value;
                    if (cloverBonus <= 0f) return false;
                    extraDesc = Localization.Format(
                        "<style=cSub><color=#4ADE80>[ItemRoulette]</color> Прокрутки рулетки более благосклонны. Увеличивает шанс положительных событий Хаоса (Удача) на <color=#4ADE80>+{0:F0}%</color> за каждый стак предмета.</style>",
                        "<style=cSub><color=#4ADE80>[ItemRoulette]</color> Roulette spins are luckier. Increases the chance of positive Chaos events (Fortune) by <color=#4ADE80>+{0:F0}%</color> per item stack.</style>",
                        cloverBonus);
                    return true;

                case "ITEM_LUNARBADLUCK_DESC":
                    if (!ModConfig.IsChaosSystemActive) return false;
                    float purityPenalty = ModConfig.PurityPenaltyPerItem.Value;
                    if (purityPenalty <= 0f) return false;
                    extraDesc = Localization.Format(
                        "<style=cSub><color=#F87171>[ItemRoulette]</color> Прокрутки рулетки менее благосклонны. Увеличивает шанс отрицательных событий Хаоса (Бедствие) на <color=#F87171>+{0:F0}%</color> за каждый стак предмета.</style>",
                        "<style=cSub><color=#F87171>[ItemRoulette]</color> Roulette spins are unluckier. Increases the chance of negative Chaos events (Calamity) by <color=#F87171>+{0:F0}%</color> per item stack.</style>",
                        purityPenalty);
                    return true;

                default:
                    return false;
            }
        }
    }
}
