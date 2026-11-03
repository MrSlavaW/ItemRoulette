using System;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Shared player lookup and naming helpers.
    /// </summary>
    public static class PlayerUtil
    {
        /// <summary>
        /// Returns the actual stored equipment index including modded quality tiers (e.g. from Goorakh-Quality).
        /// Quality mod hooks inventory.currentEquipmentIndex to return the base vanilla index; reading the slot directly preserves the actual quality index.
        /// </summary>
        public static EquipmentIndex GetStoredEquipmentIndex(Inventory? inventory)
        {
            if (inventory == null) return EquipmentIndex.None;
            var location = inventory.activeEquipmentLocation;
            return inventory.GetEquipment(in location).equipmentIndex;
        }

        public static CharacterMaster? GetLocalMaster()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            if (user != null && user.cachedMaster != null) return user.cachedMaster;
            if (PlayerCharacterMasterController.instances != null)
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.networkUser != null && pcmc.networkUser.isLocalPlayer && pcmc.master != null)
                    {
                        return pcmc.master;
                    }
                }
                if (PlayerCharacterMasterController.instances.Count == 1)
                {
                    return PlayerCharacterMasterController.instances[0]?.master;
                }
            }
            return null;
        }

        public static string GetMasterIdentifier(CharacterMaster? master)
        {
            if (master == null) return "Unknown";
            var pcmc = master.playerCharacterMasterController ?? master.GetComponent<PlayerCharacterMasterController>();
            if (pcmc != null && pcmc.networkUser != null && !string.IsNullOrEmpty(pcmc.networkUser.userName))
            {
                return pcmc.networkUser.userName;
            }
            return master.netId.ToString();
        }

        public static string GetMasterDisplayName(CharacterMaster? master)
        {
            if (master == null) return Localization.Get("Игрок", "Player");
            var pcmc = master.playerCharacterMasterController ?? master.GetComponent<PlayerCharacterMasterController>();
            if (pcmc != null && pcmc.networkUser != null && !string.IsNullOrEmpty(pcmc.networkUser.userName))
            {
                return pcmc.networkUser.userName;
            }
            return Localization.Get("Игрок", "Player");
        }

        public static CharacterMaster? FindMaster(uint masterNetId, string safeName, CharacterMaster? senderHint = null)
        {
            if (senderHint != null && senderHint.inventory != null)
            {
                return senderHint;
            }

            if (masterNetId > 0)
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null && pcmc.master.netId.Value == masterNetId)
                    {
                        return pcmc.master;
                    }
                }
                if (NetworkServer.active)
                {
                    var obj = NetworkServer.FindLocalObject(new NetworkInstanceId(masterNetId));
                    if (obj != null)
                    {
                        var m = obj.GetComponent<CharacterMaster>();
                        if (m != null) return m;
                    }
                }
            }

            string unescaped = !string.IsNullOrEmpty(safeName) ? Uri.UnescapeDataString(safeName) : "";
            if (!string.IsNullOrEmpty(unescaped) || !string.IsNullOrEmpty(safeName))
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null && pcmc.networkUser != null)
                    {
                        if (string.Equals(pcmc.networkUser.userName, safeName, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(unescaped) && string.Equals(pcmc.networkUser.userName, unescaped, StringComparison.OrdinalIgnoreCase)))
                        {
                            return pcmc.master;
                        }
                    }
                }
            }

            return null;
        }

        public static CharacterMaster? FindMaster(string idStr, CharacterMaster? senderHint = null)
        {
            if (senderHint != null && senderHint.inventory != null)
            {
                return senderHint;
            }
            if (string.IsNullOrEmpty(idStr)) return null;

            string netIdStr = idStr;
            string nameStr = idStr;
            if (idStr.Contains("_"))
            {
                var split = idStr.Split('_');
                netIdStr = split[0];
                nameStr = split.Length > 1 ? split[1] : idStr;
            }

            if (uint.TryParse(netIdStr, out uint netIdVal))
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc != null && pcmc.master != null && pcmc.master.netId.Value == netIdVal)
                    {
                        return pcmc.master;
                    }
                }
                if (NetworkServer.active)
                {
                    var obj = NetworkServer.FindLocalObject(new NetworkInstanceId(netIdVal));
                    if (obj != null)
                    {
                        var m = obj.GetComponent<CharacterMaster>();
                        if (m != null) return m;
                    }
                }
            }

            string unescapedName = Uri.UnescapeDataString(nameStr);
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc != null && pcmc.master != null)
                {
                    if (pcmc.networkUser != null &&
                        (string.Equals(pcmc.networkUser.userName, nameStr, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(pcmc.networkUser.userName, unescapedName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return pcmc.master;
                    }
                    if (string.Equals(pcmc.master.netId.ToString(), netIdStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return pcmc.master;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Idempotent capture/release of the mouse cursor via MPEventSystem.cursorOpenerCount.
    /// One instance per window; Capture/Release can be called repeatedly.
    /// </summary>
    public sealed class CursorCapture
    {
        private bool _captured;

        public void Capture()
        {
            if (_captured) return;
            if (MPEventSystemManager.kbmEventSystem != null)
            {
                MPEventSystemManager.kbmEventSystem.cursorOpenerCount++;
            }
            _captured = true;
        }

        public void Release()
        {
            if (!_captured) return;
            if (MPEventSystemManager.kbmEventSystem != null)
            {
                MPEventSystemManager.kbmEventSystem.cursorOpenerCount--;
            }
            _captured = false;
        }
    }
}
