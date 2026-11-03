using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Authoritative per-player roulette state (tracked on the server).
    /// </summary>
    public class PlayerRouletteState
    {
        public int FreeRollsAvailable;
        public int FreeUncommonRollsAvailable;
        public int FreeBossRollsAvailable;
        public int FreeLegendaryRollsAvailable;
        public int FreeVoidPotentialRollsAvailable;
        public int FreeHighTierRollsAvailable;
        public int FreeQualityRollsAvailable;
        public int FreeEquipmentRollsAvailable;
        public int FreeQualityEquipmentRollsAvailable;
        public int FreeLunarRollsAvailable;
        public int PendingVoidRollsAvailable;
        public int SolitudeRollsAvailable;
        public float SolitudeRarityMultiplierBonus;
        public Queue<QualityTierCompat> PendingBossMinQualityTiers = new Queue<QualityTierCompat>();
        public Queue<QualityTierCompat> PendingGeneralMinQualityTiers = new Queue<QualityTierCompat>();
        public int PaidRollsThisStage;
        public int TotalRolls;
        public int PendingFreeRollsNextStage;
        public float ChaosCostPenaltyMultiplier;
        public bool FiftyFiftyActive;
        public bool StarsAppliedThisStage;
        public bool LastSpinWasFree;

        // Void decision state
        public bool InVoidDecision;
        public RouletteItemEntry? PendingVoidEntry;
        public int VoidRerollsUsed;
        public int PaidCostForVoidSpin;
        public bool CurrentDecisionIsLunarVoid;

        // Pending regular spin award waiting for reel animation to finish
        public RouletteItemEntry? PendingAward;
        public int PendingAwardPaidCost;
        public bool PendingAwardUpgradedByDoll;
        public string? PendingAwardOldItemName;
        public int PendingAwardDollCount;
        public float PendingAwardDollChance;

        public bool IsNextRollFree => FreeHighTierRollsAvailable > 0 ||
                                      FreeLegendaryRollsAvailable > 0 ||
                                      FreeBossRollsAvailable > 0 ||
                                      FreeQualityEquipmentRollsAvailable > 0 ||
                                      FreeEquipmentRollsAvailable > 0 ||
                                      FreeLunarRollsAvailable > 0 ||
                                      FreeVoidPotentialRollsAvailable > 0 ||
                                      FreeUncommonRollsAvailable > 0 ||
                                      FreeQualityRollsAvailable > 0 ||
                                      SolitudeRollsAvailable > 0 ||
                                      FreeRollsAvailable > 0;

        public RouletteTierType? GetActiveFreeRollTier()
        {
            if (FreeHighTierRollsAvailable > 0) return RouletteTierType.HighTierPylon;
            if (FreeLegendaryRollsAvailable > 0) return RouletteTierType.Legendary;
            if (FreeBossRollsAvailable > 0) return RouletteTierType.BossReward;
            if (FreeQualityEquipmentRollsAvailable > 0) return RouletteTierType.QualityEquipment;
            if (FreeEquipmentRollsAvailable > 0) return RouletteTierType.Equipment;
            if (FreeLunarRollsAvailable > 0) return RouletteTierType.Lunar;
            if (FreeVoidPotentialRollsAvailable > 0) return RouletteTierType.VoidPotential;
            if (FreeUncommonRollsAvailable > 0) return RouletteTierType.Uncommon;
            return null;
        }

        public void DeductFirstFreeRoll()
        {
            if (FreeHighTierRollsAvailable > 0) FreeHighTierRollsAvailable--;
            else if (FreeLegendaryRollsAvailable > 0) FreeLegendaryRollsAvailable--;
            else if (FreeBossRollsAvailable > 0) FreeBossRollsAvailable--;
            else if (FreeQualityEquipmentRollsAvailable > 0) FreeQualityEquipmentRollsAvailable--;
            else if (FreeEquipmentRollsAvailable > 0) FreeEquipmentRollsAvailable--;
            else if (FreeLunarRollsAvailable > 0) FreeLunarRollsAvailable--;
            else if (FreeVoidPotentialRollsAvailable > 0) FreeVoidPotentialRollsAvailable--;
            else if (FreeUncommonRollsAvailable > 0) FreeUncommonRollsAvailable--;
            else if (SolitudeRollsAvailable > 0)
            {
                SolitudeRollsAvailable--;
                if (SolitudeRollsAvailable <= 0) SolitudeRarityMultiplierBonus = 0f;
            }
            else if (FreeQualityRollsAvailable > 0) FreeQualityRollsAvailable--;
            else if (FreeRollsAvailable > 0) FreeRollsAvailable--;
        }

        public bool ConsumeFreeQualityRoll()
        {
            if (FreeQualityRollsAvailable > 0)
            {
                FreeQualityRollsAvailable--;
                return true;
            }
            return false;
        }

        public void UpdateStarsIfPending(CharacterMaster master)
        {
            if (ModConfig.EnableSaleStarRework.Value || StarsAppliedThisStage || master == null || master.inventory == null) return;

            int stars = RouletteManager.GetSaleStarCount(master.inventory);
            if (stars <= 0) return;

            StarsAppliedThisStage = true;
            int bonus = stars * ModConfig.FreeRollsPerSaleStar.Value;
            if (bonus <= 0) return;

            try
            {
                string playerId = Uri.EscapeDataString(PlayerUtil.GetMasterIdentifier(master));
                string playerName = PlayerUtil.GetMasterDisplayName(master);
                ServerEvents.Broadcast(Localization.Format(
                    "<color=#8CE6FF>[ItemRoulette:FreeRoll:{0}:{1}] {2} — Звезда скидок ({3} шт.): получено +{1} бесплатных прокруток рулетки на этапе!</color>",
                    "<color=#8CE6FF>[ItemRoulette:FreeRoll:{0}:{1}] {2} — Sale Star (x{3}): granted +{1} free roulette spins for this stage!</color>",
                    playerId, bonus, playerName, stars));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in UpdateStarsIfPending: {ex}");
            }
        }

        public void ResetForNewStage(int stageBaseFreeRolls, bool resetCost)
        {
            if (resetCost) PaidRollsThisStage = 0;
            FreeRollsAvailable += stageBaseFreeRolls + PendingFreeRollsNextStage;
            PendingFreeRollsNextStage = 0;
            PendingBossMinQualityTiers.Clear();
            PendingGeneralMinQualityTiers.Clear();
            ChaosCostPenaltyMultiplier = 0f;
            FiftyFiftyActive = false;
            CurrentDecisionIsLunarVoid = false;
            StarsAppliedThisStage = false;
            LastSpinWasFree = false;
            if (InVoidDecision && PendingVoidEntry != null)
            {
                bool isEquipment = PendingVoidEntry.PickupDef != null && PendingVoidEntry.PickupDef.equipmentIndex != EquipmentIndex.None;
                if (isEquipment)
                {
                    FreeEquipmentRollsAvailable++;
                }
                else if (PendingVoidEntry.IsLunarItem)
                {
                    FreeLunarRollsAvailable++;
                }
                else
                {
                    PendingVoidRollsAvailable++;
                }
            }
            InVoidDecision = false;
            PendingVoidEntry = null;
            VoidRerollsUsed = 0;
            PendingAward = null;
        }

        public void ResetForNewRun()
        {
            PaidRollsThisStage = 0;
            TotalRolls = 0;
            FreeRollsAvailable = 0;
            FreeUncommonRollsAvailable = 0;
            FreeBossRollsAvailable = 0;
            FreeLegendaryRollsAvailable = 0;
            FreeVoidPotentialRollsAvailable = 0;
            FreeHighTierRollsAvailable = 0;
            FreeQualityRollsAvailable = 0;
            FreeEquipmentRollsAvailable = 0;
            FreeQualityEquipmentRollsAvailable = 0;
            FreeLunarRollsAvailable = 0;
            PendingVoidRollsAvailable = 0;
            SolitudeRollsAvailable = 0;
            SolitudeRarityMultiplierBonus = 0f;
            PendingFreeRollsNextStage = 0;
            PendingBossMinQualityTiers.Clear();
            PendingGeneralMinQualityTiers.Clear();
            ChaosCostPenaltyMultiplier = 0f;
            FiftyFiftyActive = false;
            CurrentDecisionIsLunarVoid = false;
            StarsAppliedThisStage = false;
            LastSpinWasFree = false;
            InVoidDecision = false;
            PendingVoidEntry = null;
            VoidRerollsUsed = 0;
            PendingAward = null;
        }
    }
}
