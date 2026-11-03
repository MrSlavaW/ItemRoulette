using System;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    /// <summary>
    /// Server-authoritative controller managing the reboot/stun delay, combat activation,
    /// and optional lifespan self-destruction for hostile drones spawned by Chaos Events.
    /// </summary>
    public class HostileDroneController : MonoBehaviour
    {
        private CharacterMaster? _master;
        private CharacterBody? _body;
        private CharacterMaster? _targetPlayerMaster;

        private float _stunRemaining;
        private bool _isStunned;

        private bool _hasLifetime;
        private float _lifetimeRemaining;

        public void Initialize(CharacterMaster droneMaster, CharacterMaster targetPlayer, float stunDuration, bool hasLifetime, float lifetimeDuration)
        {
            _master = droneMaster;
            _targetPlayerMaster = targetPlayer;
            _stunRemaining = Mathf.Max(0f, stunDuration);
            _isStunned = _stunRemaining > 0f;
            _hasLifetime = hasLifetime;
            _lifetimeRemaining = Mathf.Max(1f, lifetimeDuration);

            // Disable AI immediately if stun is active
            if (_isStunned && _master != null)
            {
                var ais = _master.GetComponents<BaseAI>();
                foreach (var ai in ais)
                {
                    if (ai != null)
                    {
                        ai.enabled = false;
                        if (ai.currentEnemy != null)
                        {
                            ai.currentEnemy.gameObject = null;
                            ai.currentEnemy.bestHurtBox = null;
                        }
                    }
                }
            }

            ApplyBodyModifications();
        }

        private void ApplyBodyModifications()
        {
            if (_master == null) return;
            _body = _master.GetBody();
            if (_body == null) return;

            if (_isStunned && _stunRemaining > 0f)
            {
                // Force mechanical drone to accept stun state
                var setStateOnHurt = _body.GetComponent<SetStateOnHurt>();
                if (setStateOnHurt != null)
                {
                    setStateOnHurt.canBeStunned = true;
                    setStateOnHurt.SetStun(_stunRemaining);
                }

                SetSkillsEnabled(false);
                SuppressInput();

                if (RoR2Content.Buffs.Nullified != null)
                {
                    _body.AddTimedBuff(RoR2Content.Buffs.Nullified, _stunRemaining);
                }

                _body.subtitleNameToken = "ITEMROULETTE_SUBTITLE_HOSTILE_DRONE_REBOOTING";
            }
            else
            {
                _body.subtitleNameToken = "ITEMROULETTE_SUBTITLE_HOSTILE_DRONE";
            }
        }

        private void SetSkillsEnabled(bool enabled)
        {
            if (_body == null || _body.skillLocator == null) return;
            var loc = _body.skillLocator;
            if (loc.primary != null) loc.primary.enabled = enabled;
            if (loc.secondary != null) loc.secondary.enabled = enabled;
            if (loc.utility != null) loc.utility.enabled = enabled;
            if (loc.special != null) loc.special.enabled = enabled;
        }

        private void SuppressInput()
        {
            if (_body == null || _body.inputBank == null) return;
            var input = _body.inputBank;
            input.skill1.PushState(false);
            input.skill2.PushState(false);
            input.skill3.PushState(false);
            input.skill4.PushState(false);
            input.moveVector = Vector3.zero;
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (_master == null)
            {
                Destroy(this);
                return;
            }

            // Resolve body if spawned late
            if (_body == null)
            {
                _body = _master.GetBody();
                if (_body != null)
                {
                    ApplyBodyModifications();
                }
            }

            // Handle Stun / Reboot phase
            if (_isStunned)
            {
                // Continuously enforce AI suppression, skill disable, and input clearing
                if (_master != null)
                {
                    var ais = _master.GetComponents<BaseAI>();
                    foreach (var ai in ais)
                    {
                        if (ai != null)
                        {
                            ai.enabled = false;
                            if (ai.currentEnemy != null)
                            {
                                ai.currentEnemy.gameObject = null;
                                ai.currentEnemy.bestHurtBox = null;
                            }
                        }
                    }
                }

                SetSkillsEnabled(false);
                SuppressInput();

                _stunRemaining -= Time.deltaTime;
                if (_stunRemaining <= 0f)
                {
                    _isStunned = false;
                    ActivateCombatAI();
                }
                return;
            }

            // Handle Lifespan countdown
            if (_hasLifetime)
            {
                _lifetimeRemaining -= Time.deltaTime;
                if (_lifetimeRemaining <= 0f)
                {
                    SelfDestruct();
                }
            }
        }

        private void ActivateCombatAI()
        {
            SetSkillsEnabled(true);

            if (_master != null)
            {
                var ais = _master.GetComponents<BaseAI>();
                var targetBody = _targetPlayerMaster?.GetBody();

                foreach (var ai in ais)
                {
                    if (ai != null)
                    {
                        if (ai.leader != null)
                        {
                            ai.leader.gameObject = null;
                        }
                        ai.enabled = true;
                        ai.prioritizePlayers = true;
                        ai.enemyAttention = 60f;
                        if (targetBody != null && ai.currentEnemy != null)
                        {
                            ai.currentEnemy.gameObject = targetBody.gameObject;
                            ai.currentEnemy.bestHurtBox = targetBody.mainHurtBox;
                        }
                    }
                }
            }

            if (_body != null)
            {
                _body.subtitleNameToken = "ITEMROULETTE_SUBTITLE_HOSTILE_DRONE";
                Util.PlaySound("Play_drone_alert", _body.gameObject);
            }
        }

        private void SelfDestruct()
        {
            try
            {
                if (_body != null && _body.healthComponent != null && _body.healthComponent.alive)
                {
                    _body.healthComponent.Suicide(null, null, DamageType.Generic);
                }
                else if (_master != null && _master.hasBody)
                {
                    _master.TrueKill();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception during hostile drone self-destruction: {ex}");
            }
            finally
            {
                Destroy(this);
            }
        }
    }
}
