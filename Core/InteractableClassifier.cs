using System;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Helper methods for classifying game interactables, containers, and scenes.
    /// </summary>
    public static class InteractableClassifier
    {
        public static bool IsVoidCradle(GameObject? obj, string? name = null)
        {
            if (obj != null)
            {
                // 1. Mod component check (e.g. RiskOfBrainrot-FruityCradles)
                foreach (var comp in obj.GetComponents<Component>())
                {
                    if (comp != null && comp.GetType().Name.IndexOf("FruityCradle", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                var pi = obj.GetComponent<PurchaseInteraction>() ?? obj.GetComponentInParent<PurchaseInteraction>();
                if (pi != null)
                {
                    // 2. Token-based detection (independent of cost percentage or mechanic changes)
                    string dToken = (pi.displayNameToken ?? "").ToUpperInvariant();
                    string cToken = (pi.contextToken ?? "").ToUpperInvariant();
                    if (dToken.Contains("VOID_CHEST") || dToken.Contains("CRADLE") ||
                        cToken.Contains("VOID_CHEST") || cToken.Contains("CRADLE"))
                    {
                        return true;
                    }

                    // 3. Health or Soul cost check (matches ANY % of HP: 50%, 20%, Soul cost, etc.)
                    if (pi.costType == CostTypeIndex.PercentHealth)
                    {
                        return true;
                    }

                    string costTypeName = pi.costType.ToString().ToLowerInvariant();
                    if (costTypeName.Contains("soul") || costTypeName.Contains("health"))
                    {
                        return true;
                    }
                }
            }

            string n = (name ?? obj?.name ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(n)) return false;

            if (n.Contains("cradle") || n.Contains("fruitycradle") || n.Contains("fruity"))
            {
                return true;
            }

            // In RoR2 vanilla, Void Cradle prefab is "VoidChest" or "VoidChest(Clone)"
            if (n.Contains("voidchest") && !n.Contains("potential") && !n.Contains("option") && !n.Contains("triple"))
            {
                return true;
            }

            return false;
        }

        public static bool IsVoidPotential(GameObject? obj, string? name = null)
        {
            if (IsVoidCradle(obj, name))
            {
                return false;
            }

            string n = (name ?? obj?.name ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(n)) return false;

            if (n.Contains("potential") || n.Contains("option") || n.Contains("triple"))
            {
                return true;
            }

            if (obj != null && (obj.GetComponent<OptionChestBehavior>() != null || obj.GetComponentInParent<OptionChestBehavior>() != null))
            {
                return true;
            }

            return false;
        }

        public static bool IsVoidChestName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("voidchest") ||
                   lower.Contains("voidcradle") ||
                   lower.Contains("voidtriple") ||
                   lower.Contains("void") ||
                   lower.Contains("cradle") ||
                   lower.Contains("optionchest") ||
                   lower.Contains("fruitycradle") ||
                   lower.Contains("potential");
        }

        public static bool IsEquipmentMultishop(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("tripleshopequipment") ||
                   (lower.Contains("equipment") && (lower.Contains("multishop") || lower.Contains("triple") || lower.Contains("terminal")));
        }

        public static bool IsQualityEquipmentChest(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("qualityequipment") ||
                   (lower.Contains("quality") && (lower.Contains("equipmentbarrel") || lower.Contains("equipbarrel") || lower.Contains("equipment")));
        }

        public static bool IsEquipmentChest(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            if (IsEquipmentMultishop(lower)) return false;
            if (IsQualityEquipmentChest(lower)) return false;
            return lower.Contains("equipmentbarrel") ||
                   lower.Contains("equipbarrel") ||
                   lower.Contains("equipment");
        }

        public static bool IsRegularMultishop(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            if (IsEquipmentMultishop(lower)) return false;
            if (IsVoidChestName(lower)) return false;
            if (IsLunarInteractable(null, lower)) return false;
            return lower.Contains("multishop") ||
                   lower.Contains("tripleshop") ||
                   lower.Contains("triple") ||
                   lower.Contains("shoplevel1") ||
                   lower.Contains("shoplevel2");
        }

        public static bool IsEquipmentChestName(string? name)
        {
            return IsEquipmentChest(name) || IsEquipmentMultishop(name) || IsQualityEquipmentChest(name);
        }

        public static bool IsQualityChestName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("qualitychest") || IsQualityEquipmentChest(lower);
        }

        public static bool IsBazaarScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.name.IndexOf("bazaar", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                var def = SceneCatalog.GetSceneDefForCurrentScene();
                if (def != null && def.baseSceneName.IndexOf("bazaar", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            catch { }
            return false;
        }

        public static bool IsLunarChestName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("lunarchest") || lower.Contains("lunarpod");
        }

        public static bool IsLunarInteractable(GameObject? obj, string? name = null)
        {
            if (obj != null)
            {
                var pi = obj.GetComponent<PurchaseInteraction>() ?? obj.GetComponentInParent<PurchaseInteraction>();
                if (pi != null)
                {
                    if (pi.costType == CostTypeIndex.LunarCoin) return true;
                    string dToken = (pi.displayNameToken ?? "").ToUpperInvariant();
                    string cToken = (pi.contextToken ?? "").ToUpperInvariant();
                    if (dToken.Contains("LUNAR") || cToken.Contains("LUNAR")) return true;
                }
            }
            string n = (name ?? obj?.name ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(n)) return false;
            return n.Contains("lunarchest") || n.Contains("lunarpod") || n.Contains("lunarshop") || n.Contains("bazaar");
        }

        public static bool IsCauldronInteractable(GameObject? obj, string? name = null)
        {
            if (obj != null)
            {
                var pi = obj.GetComponent<PurchaseInteraction>() ?? obj.GetComponentInParent<PurchaseInteraction>();
                if (pi != null)
                {
                    if (pi.costType == CostTypeIndex.WhiteItem ||
                        pi.costType == CostTypeIndex.GreenItem ||
                        pi.costType == CostTypeIndex.RedItem ||
                        pi.costType == CostTypeIndex.BossItem)
                    {
                        return true;
                    }
                    string costName = pi.costType.ToString().ToLowerInvariant();
                    if (costName.Contains("item") || costName.Contains("scrap")) return true;
                }
            }
            string n = (name ?? obj?.name ?? "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(n)) return false;
            return n.Contains("cauldron") || n.Contains("soup") || n.Contains("scrapper") || n.Contains("cleanse") || n.Contains("duplicator") || n.Contains("printer");
        }
    }
}
