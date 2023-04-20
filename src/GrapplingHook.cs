using MelonLoader;
using UnityEngine;
using HarmonyLib;
using SLZ.Bonelab;
using SLZ.Rig;
using SLZ.Props;
using SLZ.VRMK;
using SLZ.Marrow.Warehouse;
using System.Collections.Generic;
using System.Linq;
using System;
using SLZ;
using SLZ.Interaction;
using System.Collections;

namespace AttackOnBonelab
{
    public class GrapplingHook : Component
    {
        private const float MAX_SHOOT_DIST = 50f;
        private const float HOOK_RADIUS = 0.1f;
        private const float SHOOT_SPEED_MS = 80f;
        private const float MAX_SHOOT_TIME = 2f;
        private const float RETRACT_SPEED_MS = 120f;

        // TODO: Get layer names
        // 0
        // 10
        // 12
        // 13
        // 15
        private const int LAYER_MASK = 46081;

        private static GameObject _reticlePrefab;
        private static GameObject reticlePrefab
        {
            get
            {
                if (!_reticlePrefab)
                {
                    _reticlePrefab = Geometry.CreatePrefabCube(
                        "aob_reticle",
                        Color.magenta,
                        -HOOK_RADIUS,
                        HOOK_RADIUS,
                        -HOOK_RADIUS,
                        HOOK_RADIUS,
                        -HOOK_RADIUS,
                        HOOK_RADIUS,
                        Resources
                            .FindObjectsOfTypeAll<Shader>()
                            .First(shader => shader.name == "SLZ/Highlighter")
                    );
                }
                return _reticlePrefab;
            }
        }

        private static GameObject _linePrefab;
        private static GameObject linePrefab
        {
            get
            {
                if (!_linePrefab)
                {
                    _linePrefab = Geometry.CreatePrefabLine(
                        "aob_line",
                        Color.black,
                        0.02f
                    );
                }
                return _linePrefab;
            }
        }

        private static AudioClip audioShoot;

        private GameObject Reticle;
        private LineRenderer Line;
        public Hand Hand;

        private AudioSource audioSource;
        private GrapplingLine line;
        private Target? target;
        private float? shootStartTime;
        private Vector3 prevHookPos;
        private Vector3 prevPlayerPos;
        private ConfigurableJoint joint;
        private bool isRetracting = false;

        private void Awake()
        {
            if (!Reticle)
                Reticle = GameObject.Instantiate(reticlePrefab);
            if (!Line)
            {
                Line = GameObject
                    .Instantiate(linePrefab)
                    .GetComponent<LineRenderer>();
                Line.transform.SetParent(transform);
            }

            audioSource = base.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.minDistance = 0.5f;
            audioSource.spatialBlend = 1f;
        }

        private void LateUpdate()
        {
            var validAimHit = UpdateAim();
            if (validAimHit.HasValue && ShootKeyDown())
                Shoot(validAimHit.Value);
            if (target.HasValue && ShootKeyUp())
                Retract(GetTargetPosition());
        }

        private void FixedUpdate()
        {
            UpdateLine();
        }

        private RaycastHit? UpdateAim()
        {
            RaycastHit hit;
            if (
                Physics.SphereCast(
                    transform.position,
                    HOOK_RADIUS,
                    transform.forward,
                    out hit,
                    MAX_SHOOT_DIST,
                    LAYER_MASK,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                Reticle.transform.position = hit.point;
                Reticle.transform.rotation = Quaternion.LookRotation(
                    Hand.manager.physicsRig.m_head.forward
                );
                Reticle.active = true;
                return hit;
            }

            Reticle.active = false;
            return null;
        }

        private bool ShootKeyDown() =>
            // TODO: Does the "Down" method work?
            Hand.Controller.GetPrimaryInteractionButtonDown();

        private bool ShootKeyUp() =>
            // TODO: Does the "Up" method work?
            Hand.Controller.GetPrimaryInteractionButtonUp();

        private void Shoot(RaycastHit aimHit)
        {
            Log.Debug("Grapple shoot");
            target = new Target()
            {
                Hit = aimHit,
                ConnectedAnchor =
                    aimHit.rigidbody.transform.InverseTransformPoint(
                        aimHit.point
                    ),
            };
            this.audioSource.PlayOneShot(audioShoot);
            Hand.Controller.haptor.Haptic_Tap();
            Line.gameObject.active = true;
            prevHookPos = prevPlayerPos = transform.position;
            shootStartTime = Time.time;
            UpdateLine();
        }
    }
}
