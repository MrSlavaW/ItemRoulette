using System.Collections.Generic;
using ItemRoulette.Compat;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Hooks
{
    public enum OptionDropRewardTier
    {
        None,
        Legendary,
        Uncommon,
        Boss,
        VoidChoice,
        HighTierPylon,
        VoidPotential,
        Common,
        AurelioniteFragmentLow,
        AurelioniteFragmentMid,
        AurelioniteFragmentMax
    }

    /// <summary>
    /// Snapshot of Sale Star inventory at the exact moment of interaction [E],
    /// before vanilla or rework mods consume stars early in PurchaseInteraction.OnInteractionBegin.
    /// </summary>
    public class SaleStarSnapshot
    {
        public CharacterMaster Master { get; set; } = null!;
        public List<(QualityTierCompat tier, int count)> StarEntries { get; set; } = new List<(QualityTierCompat, int)>();
        public int TotalStars { get; set; }
        public float Timestamp { get; set; }
    }

    /// <summary>
    /// Holds shared transient stage state and debouncing caches across drop suppression and reward modules.
    /// </summary>
    public static class DropStateManager
    {
        public static readonly Dictionary<GameObject, Interactor> LastInteractors = new Dictionary<GameObject, Interactor>();
        public static readonly HashSet<int> ProcessedChestInstanceIds = new HashSet<int>();
        public static readonly Dictionary<GameObject, SaleStarSnapshot> ChestSaleStarSnapshots = new Dictionary<GameObject, SaleStarSnapshot>();

        public static float LastVoidRewardTime { get; set; } = -100f;
        public static float LastWorldBurstTime { get; set; } = -100f;
        public static OptionDropRewardTier LastWorldBurstTier { get; set; } = OptionDropRewardTier.None;
        public static bool TeleporterChargedThisStage { get; set; } = false;
        public static bool BossSaleStarAwardedThisStage { get; set; } = false;

        public static void ResetForNewStage()
        {
            LastInteractors.Clear();
            ProcessedChestInstanceIds.Clear();
            ChestSaleStarSnapshots.Clear();
            LastVoidRewardTime = -100f;
            LastWorldBurstTime = -100f;
            LastWorldBurstTier = OptionDropRewardTier.None;
            TeleporterChargedThisStage = false;
            BossSaleStarAwardedThisStage = false;
        }

        /// <summary>
        /// Retrieves and removes a Sale Star snapshot recorded when the player interacted with the chest.
        /// Matches by exact object, root GameObject, or hierarchy.
        /// </summary>
        public static bool TryGetSaleStarSnapshot(GameObject obj, out SaleStarSnapshot? snapshot)
        {
            snapshot = null;
            if (obj == null) return false;

            float now = Time.time;

            if (ChestSaleStarSnapshots.TryGetValue(obj, out snapshot) && snapshot != null)
            {
                ChestSaleStarSnapshots.Remove(obj);
                if (now - snapshot.Timestamp <= 30f && snapshot.TotalStars > 0) return true;
            }

            if (obj.transform.root != null && ChestSaleStarSnapshots.TryGetValue(obj.transform.root.gameObject, out snapshot) && snapshot != null)
            {
                ChestSaleStarSnapshots.Remove(obj.transform.root.gameObject);
                if (now - snapshot.Timestamp <= 30f && snapshot.TotalStars > 0) return true;
            }

            GameObject? matchKey = null;
            foreach (var kvp in ChestSaleStarSnapshots)
            {
                if (kvp.Key != null && (kvp.Key.transform.IsChildOf(obj.transform) || obj.transform.IsChildOf(kvp.Key.transform)))
                {
                    matchKey = kvp.Key;
                    snapshot = kvp.Value;
                    break;
                }
            }

            if (matchKey != null)
            {
                ChestSaleStarSnapshots.Remove(matchKey);
                if (snapshot != null && now - snapshot.Timestamp <= 30f && snapshot.TotalStars > 0) return true;
            }

            snapshot = null;
            return false;
        }
    }
}
