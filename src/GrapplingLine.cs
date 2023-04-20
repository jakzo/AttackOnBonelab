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
    public class GrapplingLine : Component
    {
        private const float HOOK_RADIUS = 0.1f;
        private const float SHOOT_SPEED_MS = 80f;
        private const float MAX_SHOOT_TIME = 2f;
        private const float RETRACT_SPEED_MS = 120f;

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

        private LineRenderer Line;
        public Hand Hand;

        private AudioSource audioSource;
        private Target? target;
        private float? shootStartTime;
        private Vector3 prevHookPos;
        private Vector3 prevPlayerPos;
        private ConfigurableJoint joint;
        private bool isRetracting = false;

        private void Awake()
        {
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

        private void FixedUpdate()
        {
            if (!target.HasValue)
            {
                Line.gameObject.active = false;
                return;
            }

            Line.SetPosition(0, transform.position);

            var targetPos = GetTargetPosition();
            if (shootStartTime.HasValue)
            {
                var prevHookToTargetSegment = targetPos - prevHookPos;
                var playerDeltaPos = transform.position - prevPlayerPos;
                var playerDeltaPosTowardsTarget = new Vector3(
                    Mathf.Clamp01(playerDeltaPos.x / prevHookToTargetSegment.x)
                        * prevHookToTargetSegment.x,
                    Mathf.Clamp01(playerDeltaPos.y / prevHookToTargetSegment.y)
                        * prevHookToTargetSegment.y,
                    Mathf.Clamp01(playerDeltaPos.z / prevHookToTargetSegment.z)
                        * prevHookToTargetSegment.z
                );
                var hookPlusPlayerPos =
                    prevHookPos + playerDeltaPosTowardsTarget;
                var hookPlusPlayerToTargetSegment =
                    targetPos - hookPlusPlayerPos;
                var hookPlusPlayerToTargetDist =
                    hookPlusPlayerToTargetSegment.magnitude;
                var hookDeltaDist = Time.fixedDeltaTime * SHOOT_SPEED_MS;
                if (hookDeltaDist < hookPlusPlayerToTargetDist)
                {
                    prevHookPos =
                        hookPlusPlayerPos
                        + hookPlusPlayerToTargetSegment
                            * (hookDeltaDist / hookPlusPlayerToTargetDist);
                    prevPlayerPos = transform.position;
                    Line.SetPosition(1, prevHookPos);

                    var elapsed = Time.time - shootStartTime;
                    if (elapsed > MAX_SHOOT_TIME)
                        Retract(prevHookPos);
                    return;
                }
                shootStartTime = null;
                CreateJoint();
            }
            Line.SetPosition(1, targetPos);
        }

        struct Target
        {
            public RaycastHit Hit;
            public Vector3 ConnectedAnchor;
        }

        private void CreateJoint()
        {
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = target.Value.Hit.rigidbody;
            joint.anchor = transform.InverseTransformPoint(transform.position);
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = target.Value.ConnectedAnchor;
            joint.xMotion =
                joint.yMotion =
                joint.zMotion =
                    ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit()
            {
                limit = target.Value.Hit.distance + 0.075f,
                bounciness = 1f,
            };
            joint.linearLimitSpring = new SoftJointLimitSpring()
            {
                spring = 504857.16f,
                damper = 50485.715f,
            };
            joint.xDrive =
                joint.yDrive =
                joint.zDrive =
                    new JointDrive()
                    {
                        positionSpring = 0f,
                        positionDamper = 0f,
                        maximumForce = 0f,
                    };
            joint.breakForce = float.PositiveInfinity;
            joint.enableCollision = true;
        }

        private Vector3 GetTargetPosition() =>
            target.HasValue
                ? target.Value.Hit.rigidbody.transform.TransformPoint(
                    target.Value.ConnectedAnchor
                )
                : transform.position;

        private void Retract(Vector3 hookStartPos)
        {
            if (isRetracting)
                return;

            MelonCoroutines.Start(RetractCoroutine(hookStartPos));
        }

        private IEnumerator RetractCoroutine(Vector3 hookStartPos)
        {
            isRetracting = true;
            float startTime = Time.time;
            target = null;

            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }

            while (Time.time - startTime < RETRACT_SPEED_MS)
            {
                float elapsedTime = Time.time - startTime;
                Vector3 newPosition = Vector3.Lerp(
                    hookStartPos,
                    transform.position,
                    elapsedTime / RETRACT_SPEED_MS
                );
                Line.SetPosition(1, newPosition);

                yield return null;
            }

            Line.gameObject.active = false;
            isRetracting = false;
        }
    }
}
