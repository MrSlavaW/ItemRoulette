using System;
using ItemRoulette.Hooks;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public class ItemRouletteNetworkMessage : MessageBase
    {
        public string payload = "";

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(payload);
        }

        public override void Deserialize(NetworkReader reader)
        {
            payload = reader.ReadString();
        }
    }

    public static class NetworkHelper
    {
        public const short ItemRouletteMsgType = 10077;

        public static void Init()
        {
            RoR2.Networking.NetworkManagerSystem.onStartServerGlobal += EnsureServerHandlerRegistered;
            Stage.onStageStartGlobal += _ => EnsureServerHandlerRegistered();
            Run.onRunStartGlobal += _ => EnsureServerHandlerRegistered();
        }

        public static void EnsureServerHandlerRegistered()
        {
            if (NetworkServer.active)
            {
                try
                {
                    NetworkServer.RegisterHandler(ItemRouletteMsgType, OnServerReceiveNetworkMessage);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ItemRoulette] Exception registering server network handler: {ex}");
                }
            }
        }

        private static void OnServerReceiveNetworkMessage(NetworkMessage netMsg)
        {
            try
            {
                var msg = netMsg.ReadMessage<ItemRouletteNetworkMessage>();
                if (msg != null && !string.IsNullOrEmpty(msg.payload))
                {
                    CharacterMaster? senderMaster = null;
                    if (netMsg.conn != null && netMsg.conn.playerControllers != null)
                    {
                        foreach (var pc in netMsg.conn.playerControllers)
                        {
                            if (pc != null && pc.gameObject != null)
                            {
                                var netUser = pc.gameObject.GetComponent<NetworkUser>();
                                if (netUser != null && netUser.master != null)
                                {
                                    senderMaster = netUser.master;
                                    break;
                                }
                            }
                        }
                    }

                    DropSuppressionHooks.ProcessNetworkTag(msg.payload, senderMaster);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception handling server network message: {ex}");
            }
        }

        public static void SendToServer(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            if (NetworkServer.active)
            {
                DropSuppressionHooks.ProcessNetworkTag(tag, LocalUserManager.GetFirstLocalUser()?.cachedMaster);
                return;
            }

            // HLAPI ClientScene transmission only. The old RoR2 console 'say' fallback was removed:
            // chat is no longer a transport and must not be able to carry commands.
            try
            {
                if (ClientScene.readyConnection != null && ClientScene.readyConnection.isConnected)
                {
                    var netMsg = new ItemRouletteNetworkMessage { payload = tag };
                    ClientScene.readyConnection.Send(ItemRouletteMsgType, netMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] HLAPI ClientScene send failed: {ex}");
            }
        }

        public static void SendChatMessage(string chatText)
        {
            if (string.IsNullOrEmpty(chatText)) return;

            if (NetworkServer.active)
            {
                ServerEvents.Broadcast(chatText);
            }
            else
            {
                SendToServer($"[ItemRoulette:ServerBroadcastChat:{chatText}]");
            }
        }

        public static bool IsLocalMaster(CharacterMaster? master)
        {
            if (master == null) return false;
            var localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null && localUser.cachedMaster == master) return true;
            if (master.playerCharacterMasterController != null &&
                master.playerCharacterMasterController.networkUser != null &&
                master.playerCharacterMasterController.networkUser.isLocalPlayer)
            {
                return true;
            }
            return false;
        }
    }
}
