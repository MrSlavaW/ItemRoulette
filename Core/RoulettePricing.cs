using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Pure calculation logic for roulette spin costs, difficulty scaling, and affordability checks.
    /// </summary>
    public static class RoulettePricing
    {
        public static int CalculateCost(int paidRollsThisStage, float chaosPenaltyMultiplier)
        {
            if (Run.instance == null) return ModConfig.BaseCost.Value;

            float calculatedCost = ModConfig.BaseCost.Value;

            // 1. Time Scaling: dynamic growth based on game timer and difficulty coefficient
            if (ModConfig.ScaleWithTime.Value)
            {
                float vanillaScaled = Run.instance.GetDifficultyScaledCost(ModConfig.BaseCost.Value);
                float mult = Mathf.Max(0f, ModConfig.TimeCostScalingMultiplier.Value);
                if (Mathf.Abs(mult - 1.0f) > 0.001f)
                {
                    calculatedCost = ModConfig.BaseCost.Value + (vanillaScaled - ModConfig.BaseCost.Value) * mult;
                }
                else
                {
                    calculatedCost = vanillaScaled;
                }
            }

            // 2. Stage Scaling: independent growth per completed stage
            if (ModConfig.ScaleWithStage.Value && Run.instance.stageClearCount > 0)
            {
                float stagePercent = Mathf.Max(0f, ModConfig.StageCostIncreasePercent.Value);
                float stageMultiplier = 1.0f + (stagePercent / 100f) * Run.instance.stageClearCount;
                calculatedCost *= stageMultiplier;
            }

            int baseCost = Mathf.RoundToInt(calculatedCost);

            int finalCost;
            if (ModConfig.CostScalingMode.Value == CostFormula.LinearPercent)
            {
                // Formula: BaseCost + (20% of BaseCost * PaidRollsThisStage)
                float rollPercent = Mathf.Max(0f, ModConfig.RollCostIncreasePercent.Value);
                float rollMultiplier = 1.0f + (rollPercent / 100f) * paidRollsThisStage;
                finalCost = Mathf.RoundToInt(baseCost * rollMultiplier);
            }
            else if (ModConfig.CostScalingMode.Value == CostFormula.Exponential)
            {
                float multiplier = Mathf.Max(1.0f, ModConfig.ExponentialCostMultiplier.Value);
                finalCost = Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, paidRollsThisStage));
            }
            else
            {
                int increment = ModConfig.LinearCostIncrement.Value;
                if (ModConfig.ScaleWithTime.Value)
                {
                    increment = Run.instance.GetDifficultyScaledCost(increment);
                }
                finalCost = baseCost + (increment * paidRollsThisStage);
            }

            if (chaosPenaltyMultiplier > 0f)
            {
                finalCost = Mathf.RoundToInt(finalCost * (1f + chaosPenaltyMultiplier));
            }

            return Mathf.Max(1, finalCost);
        }

        public static bool CanAfford(CharacterMaster? master, int cost)
        {
            if (cost <= 0) return true;
            if (master == null) return false;
            return master.money >= (uint)cost;
        }
    }
}
