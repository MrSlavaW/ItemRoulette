using System;
using HarmonyLib;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Hooks
{
    /// <summary>
    /// Module 5: Curse penalty hooks for declining Void and Lunar items.
    /// Manages max health reduction stacks and stats recalculation.
    /// </summary>
    public static class CursePenaltyHooks
    {
        private static readonly AccessTools.FieldRef<CharacterBody, float> _maxHealthRef =
            AccessTools.FieldRefAccess<CharacterBody, float>("<maxHealth>k__BackingField");
        private static readonly AccessTools.FieldRef<CharacterBody, float> _cursePenaltyRef =
            AccessTools.FieldRefAccess<CharacterBody, float>("<cursePenalty>k__BackingField");

        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            _initialized = true;
        }

        public static void Cleanup()
        {
            if (!_initialized) return;
            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            _initialized = false;
        }

        public static void ApplyCursePenaltyServer(CharacterMaster master, string itemName, bool isLunar = false)
        {
            if (master == null) return;

            RouletteManager.Instance.AddVoidCurseStack(master);
            master.GetBody()?.RecalculateStats();
            int stacks = RouletteManager.Instance.GetVoidCurseStacks(master);
            float penaltyPercent = Mathf.Clamp(
                stacks * ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value,
                0f,
                ModConfig.VoidDeclineMaxHealthPenaltyCap.Value);

            // Broadcast typed HLAPI event to all clients to synchronize curse stacks
            ServerEvents.SendToAll(new ClientEventMessage
            {
                kind = (byte)ClientEventKind.SyncVoidCurse,
                subjectNetId = master.netId.Value,
                count = stacks
            });

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

        private static void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self == null || self.master == null) return;

            int stacks = RouletteManager.Instance.GetVoidCurseStacks(self.master);
            if (stacks > 0)
            {
                float penaltyPerStack = ModConfig.VoidDeclineMaxHealthPenaltyPercent.Value;
                float cap = ModConfig.VoidDeclineMaxHealthPenaltyCap.Value;
                float totalPercent = Mathf.Clamp(stacks * penaltyPerStack, 0f, cap);
                float fraction = totalPercent / 100f;

                if (fraction > 0f && fraction < 1f)
                {
                    float healthMultiplier = 1f - fraction;
                    float newMaxHealth = self.maxHealth * healthMultiplier;
                    if (newMaxHealth < 1f) newMaxHealth = 1f;

                    _maxHealthRef(self) = newMaxHealth;
                    _cursePenaltyRef(self) += (fraction / healthMultiplier);

                    if (UnityEngine.Networking.NetworkServer.active && self.healthComponent != null && self.healthComponent.health > self.maxHealth)
                    {
                        self.healthComponent.health = self.maxHealth;
                    }
                }
            }
        }
    }
}
