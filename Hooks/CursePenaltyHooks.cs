using System;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 5: Curse penalty hooks for declining Void and Lunar items.
    /// Uses vanilla RoR2Content.Buffs.PermanentCurse which automatically handles
    /// maxHealth reduction, curse bar visuals on the HUD, and network sync across all clients.
    /// </summary>
    public static class CursePenaltyHooks
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            CharacterBody.onBodyStartGlobal += OnBodyStartGlobal;
            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            CharacterBody.onBodyStartGlobal -= OnBodyStartGlobal;
            _initialized = false;
        }

        /// <summary>
        /// Calculates the number of PermanentCurse buff stacks needed to achieve the total penalty percentage.
        /// Vanilla formula: cursePenalty += buffs * 0.01; maxHealth = baseMaxHealth / cursePenalty.
        /// Target: maxHealth = baseMaxHealth * (1 - fraction) => cursePenalty = 1 / (1 - fraction).
        /// Buffs needed = (cursePenalty - 1) / 0.01 = (fraction / (1 - fraction)) * 100.
        /// </summary>
        public static int CalculateCurseBuffs(int stacks)
        {
            if (stacks <= 0) return 0;
            float penaltyPerStack = ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value;
            float cap = ModConfig.VoidDeclineMaxHealthPenaltyCap.Value;
            float totalPercent = Mathf.Clamp(stacks * penaltyPerStack, 0f, cap);
            float fraction = Mathf.Clamp(totalPercent, 0f, 95f) / 100f;
            return Mathf.RoundToInt((fraction / (1f - fraction)) * 100f);
        }

        private static void OnBodyStartGlobal(CharacterBody body)
        {
            if (!NetworkServer.active || body == null || body.master == null) return;

            int stacks = RouletteManager.Instance.GetVoidCurseStacks(body.master);
            if (stacks > 0 && RoR2Content.Buffs.PermanentCurse != null)
            {
                int desiredBuffs = CalculateCurseBuffs(stacks);
                body.SetBuffCount(RoR2Content.Buffs.PermanentCurse.buffIndex, desiredBuffs);
                Debug.Log($"[ItemRoulette] Re-applied {desiredBuffs} PermanentCurse buffs on body spawn for {PlayerUtil.GetMasterDisplayName(body.master)} (stacks={stacks})");
            }
        }

        public static void ApplyCursePenaltyServer(CharacterMaster master, string itemName, bool isLunar = false)
        {
            if (master == null || !NetworkServer.active) return;

            RouletteManager.Instance.AddVoidCurseStack(master);

            int stacks = RouletteManager.Instance.GetVoidCurseStacks(master);
            float penaltyPercent = Mathf.Clamp(
                stacks * ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value,
                0f,
                ModConfig.VoidDeclineMaxHealthPenaltyCap.Value);

            int desiredBuffs = CalculateCurseBuffs(stacks);

            var body = master.GetBody();
            if (body != null && RoR2Content.Buffs.PermanentCurse != null)
            {
                body.SetBuffCount(RoR2Content.Buffs.PermanentCurse.buffIndex, desiredBuffs);
            }

            Debug.Log($"[ItemRoulette] ApplyCursePenaltyServer: player={PlayerUtil.GetMasterDisplayName(master)}, stacks={stacks}, penalty={penaltyPercent:0}%, desiredBuffs={desiredBuffs}, newMaxHealth={(body != null ? body.maxHealth : -1f)}");

            string playerName = PlayerUtil.GetMasterDisplayName(master);
            string colorHex = isLunar ? "#55A5FF" : "#FF5555";

            ServerEvents.Broadcast(Localization.Format(
                $"<color={colorHex}>[ItemRoulette] {{0}} жертвует плотью ради очищения ({{1}}): -{{2:0}}% макс. ХП до конца этапа!</color>",
                $"<color={colorHex}>[ItemRoulette] {{0}} sacrifices flesh for cleansing ({{1}}): -{{2:0}}% max HP until stage end!</color>",
                playerName, itemName, penaltyPercent));
        }

        public static void ApplyVoidHealthPenaltyServer(CharacterMaster master, string itemName)
        {
            ApplyCursePenaltyServer(master, itemName, isLunar: false);
        }
    }
}
