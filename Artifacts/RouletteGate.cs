using System;
using ItemRoulette.Configuration;
using ItemRoulette.Core.Modules;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Artifacts
{
    /// <summary>
    /// Coordinates runtime activation/deactivation of ItemRoulette mechanics based on the Artifact of the Roulette.
    /// Handles symmetrical activation and cleanup of drop suppression and UI modules.
    /// </summary>
    public static class RouletteGate
    {
        private static bool _active = true;
        private static int _unknownTicks = 0;
        private const int UnknownTicksBeforeOff = 12; // ~3 seconds of missing lobby/run before deactivating

        public static bool IsActive => _active;

        public static void Init()
        {
            Run.onRunStartGlobal += OnRunStart;
            Run.onRunDestroyGlobal += OnRunDestroy;
            PreGameController.onPreGameControllerSetRuleBookGlobal += OnRuleBookChanged;
            RoR2Application.onUpdate += OnUpdate;

            // If RequireArtifact is enabled, start with mechanics deactivated until the lobby/run chooses the artifact
            if (ModConfig.RequireArtifact != null && ModConfig.RequireArtifact.Value)
            {
                SetActive(false);
            }
            else
            {
                Tick();
            }
        }

        private static void OnRunStart(Run run)
        {
            Tick();
        }

        private static void OnRunDestroy(Run run)
        {
            Tick();
        }

        private static void OnRuleBookChanged(PreGameController controller, RuleBook ruleBook)
        {
            Tick();
        }

        private static void OnUpdate()
        {
            // Lightweight polling in lobby to pick up artifact toggle instantly
            if (PreGameController.instance != null)
            {
                Tick();
            }
        }

        public static bool? Desired
        {
            get
            {
                if (ModConfig.RequireArtifact != null && !ModConfig.RequireArtifact.Value)
                {
                    return true;
                }

                var def = RouletteArtifact.Def;
                if (def == null)
                {
                    return false;
                }

                RuleBook? ruleBook = null;
                if (Run.instance != null)
                {
                    ruleBook = Run.instance.ruleBook;
                }
                else if (PreGameController.instance != null)
                {
                    ruleBook = PreGameController.instance.readOnlyRuleBook;
                }

                if (ruleBook == null)
                {
                    return null;
                }

                var targetArtifactIndex = def.artifactIndex;
                for (int i = 0, count = RuleCatalog.ruleCount; i < count; i++)
                {
                    if (ruleBook.GetRuleChoice(i)?.artifactIndex == targetArtifactIndex)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static void Tick()
        {
            var desired = Desired;
            if (desired == null)
            {
                if (_active && ++_unknownTicks >= UnknownTicksBeforeOff)
                {
                    _unknownTicks = 0;
                    SetActive(false);
                }
                return;
            }

            _unknownTicks = 0;
            if (desired.Value != _active)
            {
                SetActive(desired.Value);
            }
        }

        public static void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;

            var dropModule = ModuleManager.GetModule<DropSuppressionModule>();
            var uiModule = ModuleManager.GetModule<RouletteUIModule>();

            try
            {
                if (active)
                {
                    Debug.Log("[ItemRoulette] Artifact of the Roulette enabled: activating ItemRoulette mechanics.");
                    dropModule?.Initialize();
                    uiModule?.Initialize();
                }
                else
                {
                    Debug.Log("[ItemRoulette] Artifact of the Roulette not enabled: ItemRoulette mechanics OFF (playing vanilla).");
                    uiModule?.Cleanup();
                    dropModule?.Cleanup();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemRoulette] Error toggling RouletteGate active state ({active}): {ex}");
            }
        }
    }
}
