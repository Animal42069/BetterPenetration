﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if HS2 || AI
using AIChara;
#endif

namespace Core_BetterPenetration
{
    class CollisionAgent
    {
        internal ChaControl m_collisionCharacter;
        internal CollisionPoints m_collisionPoints;
        internal CollisionOptions m_collisionOptions;
        internal bool m_collisionPointsFound = false;

        internal Transform m_bpKokanTarget;
        internal Transform m_bpAnaTarget;
        internal Transform m_innerTarget;
        internal Transform m_innerHeadTarget;
        internal Transform m_kokanBone;
        internal Transform m_siriBoneL;
        internal Transform m_siriBoneR;
        internal Transform m_oralPullBone;
        internal List<DynamicBone> m_kokanDynamicBones = new List<DynamicBone>();
        internal DynamicBone m_anaDynamicBones;
        internal DynamicBone m_bellyDynamicBone;
        internal List<DynamicBoneCollider> m_fingerColliders = new List<DynamicBoneCollider>();
        internal List<Transform> m_kokanPullBones = new List<Transform>();
        internal List<Transform> m_anaPullBones = new List<Transform>();
        internal Transform m_innerKokan;

        internal float currentKokanPull = 0f;
        internal float currentOralPull = 0f;
        internal float currentAnaPull = 0f;
        internal Vector3 currentKokanDanDirection = Vector3.up;
        internal Vector3 currentOralDanDirection = Vector3.Normalize(Vector3.up + Vector3.back);
        internal Vector3 currentAnaDanDirection = Vector3.up;
        internal bool adjustFAnimation = false;
        internal bool anaPullException = false;
        internal float m_bellyColliderRadius;

        public CollisionAgent(ChaControl character, CollisionOptions options)
        {
            Initialize(character, options);
        }

        internal void Initialize(ChaControl character, CollisionOptions options)
        {
            m_collisionOptions = options;
            currentKokanPull = 0f;
            currentOralPull = 0f;
            currentAnaPull = 0f;
            currentKokanDanDirection = Vector3.up;
            currentOralDanDirection = Vector3.Normalize(Vector3.up + Vector3.back);
            currentAnaDanDirection = Vector3.up;

            List<CollisionPoint> frontCollisionPoints = new List<CollisionPoint>();
            List<CollisionPoint> backCollisionPoints = new List<CollisionPoint>();
            m_collisionPointsFound = false;

            if (character == null)
                return;

            m_collisionCharacter = character;

            m_kokanBone = Tools.GetTransformOfChaControl(m_collisionCharacter, BoneNames.KokanBone);
            if (m_kokanBone == null)
                return;

            m_bpKokanTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.BPKokanTarget);
            m_bpAnaTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.BPAnaTarget);
            m_innerTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.InnerTarget);
            m_innerHeadTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.InnerHeadTarget);
            m_siriBoneL = Tools.GetTransformOfChaControl(m_collisionCharacter, BoneNames.ButtBoneL);
            m_siriBoneR = Tools.GetTransformOfChaControl(m_collisionCharacter, BoneNames.ButtBoneR);
            m_innerKokan = Tools.GetTransformOfChaControl(m_collisionCharacter, "cf_J_Vagina_Inner"); 

#if !STUDIO
            for (int index = 0; index < options.frontCollisionInfo.Count; index++)
            {
                Transform frontCollisionPoint = Tools.GetTransformOfChaControl(m_collisionCharacter, options.frontCollisionInfo[index].name);
                frontCollisionPoints.Add(new CollisionPoint(frontCollisionPoint, options.frontCollisionInfo[index]));
            }
            for (int index = 0; index < options.backCollisonInfo.Count; index++)
            {
                Transform backCollisionPoint = Tools.GetTransformOfChaControl(m_collisionCharacter, options.backCollisonInfo[index].name);
                backCollisionPoints.Add(new CollisionPoint(backCollisionPoint, options.backCollisonInfo[index]));
            }

            if (frontCollisionPoints.Count == options.frontCollisionInfo.Count && 
                backCollisionPoints.Count == options.backCollisonInfo.Count &&
                m_innerTarget != null && m_innerHeadTarget != null)
            {
                m_collisionPointsFound = true;
                m_collisionPoints = new CollisionPoints(frontCollisionPoints, backCollisionPoints);
            }
#else
            m_collisionPoints = null;
#endif
            UnityEngine.Debug.Log($"constrainPointsFound {m_collisionPointsFound}");

            m_kokanDynamicBones = new List<DynamicBone>();
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if (dynamicBone == null || 
                    dynamicBone.m_Root == null || 
                    dynamicBone.name == null || 
                    m_collisionCharacter != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                if (dynamicBone.name.Contains(BoneNames.BPBone))
                {
                    dynamicBone.m_Colliders.Clear();
                    m_kokanDynamicBones.Add(dynamicBone);
                }
                else if (dynamicBone.name.Contains(BoneNames.BellyBone))
                {
                    dynamicBone.m_Colliders.Clear();
                    m_bellyDynamicBone = dynamicBone;
                    m_bellyColliderRadius = dynamicBone.m_Radius;
                    UpdateBellyBones(options.bellyBulgeScale);
                }
                else if (dynamicBone.name.Contains(BoneNames.AnaTarget))
                {
                    dynamicBone.m_Colliders.Clear();
                    m_anaDynamicBones = dynamicBone;
                }
            }

            m_kokanPullBones = new List<Transform>();
            foreach (var boneName in BoneNames.KokanPullBones)
            {
                var kokanTransform = Tools.GetTransformOfChaControl(m_collisionCharacter, boneName);
                if (kokanTransform == null)
                    continue;

                m_kokanPullBones.Add(kokanTransform);
            }

            m_oralPullBone = Tools.GetTransformOfChaControl(m_collisionCharacter, BoneNames.MouthPullBone);

            foreach (var boneName in BoneNames.AnaPullBones)
            {
                var anaTransform = Tools.GetTransformOfChaControl(m_collisionCharacter, boneName);
                if (anaTransform == null)
                    continue;

                m_anaPullBones.Add(anaTransform);
            }

#if !STUDIO
#if AI || HS2
            InitializeFingerColliders(0.055f, 0.18f);
#else
            InitializeFingerColliders(0.0055f, 0.018f);
#endif
#endif
        }

        internal void CheckForAdjustment(string animationFile)
        {
            adjustFAnimation = false;
            anaPullException = false;
            if (m_kokanBone != null && BoneNames.animationAdjustmentList.Contains(animationFile))
                adjustFAnimation = true;
            if (m_bpAnaTarget != null && BoneNames.anaPullExceptionList.Contains(animationFile))
                anaPullException = true;
        }

        internal void AdjustMissionaryAnimation()
        {
            if (!m_collisionOptions.kokan_adjust || !adjustFAnimation || m_kokanBone == null)
                return;

            m_kokanBone.localPosition += new Vector3(0, m_collisionOptions.kokan_adjust_position_y, m_collisionOptions.kokan_adjust_position_z);
            m_kokanBone.localEulerAngles += new Vector3(m_collisionOptions.kokan_adjust_rotation_x, 0, 0);
        }

        internal void AdjustAnalAnimation()
        {
            if (!m_collisionOptions.ana_adjust || m_siriBoneL == null || m_siriBoneR == null)
                return;

            m_siriBoneR.localPosition += m_collisionOptions.ana_adjust_position;
            m_siriBoneR.localEulerAngles += m_collisionOptions.ana_adjust_rotation;

            m_siriBoneL.localPosition += new Vector3(-m_collisionOptions.ana_adjust_position.x, m_collisionOptions.ana_adjust_position.y, m_collisionOptions.ana_adjust_position.z);
            m_siriBoneL.localEulerAngles += new Vector3(m_collisionOptions.ana_adjust_rotation.x, -m_collisionOptions.ana_adjust_rotation.y, -m_collisionOptions.ana_adjust_rotation.z);
        }

        internal void UpdateCollisionOptions(CollisionOptions options)
        {
            m_collisionOptions = options;

            if (m_collisionPoints == null)
                return;

            m_collisionPoints.UpdateCollisionOptions(options);
            UpdateBellyBones(options.bellyBulgeScale);
        }

        internal void UpdateBellyBones(float radiusScale)
        {
            if (m_bellyDynamicBone == null)
                return;

            m_bellyDynamicBone.m_Radius = m_bellyColliderRadius * radiusScale;
#if HS2 || AIS
            m_bellyDynamicBone.UpdateParameters();
#endif
        }

        internal void ClearColliders()
        {
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if ((dynamicBone.name.Contains(BoneNames.BPBone) || dynamicBone.name.Contains(BoneNames.BellyBone)) && 
                    m_collisionCharacter == dynamicBone.GetComponentInParent<ChaControl>())
                    dynamicBone.m_Colliders.Clear();
            }
        }

        internal void ResetParticles()
        {
            ResetKokanParticles();
            ResetAnaParticles();
            ResetBellyParticles();
        }

        internal void EnableParticles(bool enable)
        {
            EnableKokanParticles(enable);
            EnableAnaParticles(enable);
            EnableBellyParticles(enable);
        }

        internal void ResetKokanParticles()
        {
            foreach (var kokanBone in m_kokanDynamicBones)
            {
                if (kokanBone == null)
                    continue;

                kokanBone.ResetParticlesPosition();
            }
        }

        internal void EnableKokanParticles(bool enable)
        {
            foreach (var kokanBone in m_kokanDynamicBones)
            {
                if (kokanBone == null)
                    continue;

                kokanBone.enabled = enable;
            }
        }

        internal void ResetAnaParticles()
        {
            if (m_anaDynamicBones == null)
                return;

            m_anaDynamicBones.ResetParticlesPosition();
        }

        internal void EnableAnaParticles(bool enable)
        {
            if (m_anaDynamicBones == null)
                return;

            m_anaDynamicBones.enabled = enable;
        }

        internal void ResetBellyParticles()
        {
            if (m_bellyDynamicBone == null)
                return;

            m_bellyDynamicBone.ResetParticlesPosition();
        }

        internal void EnableBellyParticles(bool enable)
        {
            if (m_bellyDynamicBone == null)
                return;

            m_bellyDynamicBone.enabled = enable;
        }

        internal void PullKokanBones(float pullAmount, Vector3 outerDirection, Vector3 insideDirection)
        {
            if (!m_collisionOptions.enableKokanPush || m_kokanPullBones == null)
            {
                ReturnKokanBones();
                return;
            }

            Vector3 averageDirection = Vector3.Normalize(outerDirection + insideDirection);

            Vector3 outer = insideDirection;
            Vector3 inner = insideDirection;

            if (m_collisionOptions.outer == CollisionOptions.TargetType.Average)
                outer = averageDirection;
            else if (m_collisionOptions.outer == CollisionOptions.TargetType.Outer)
                outer = outerDirection;

            if (m_collisionOptions.inner == CollisionOptions.TargetType.Average)
                inner = averageDirection;
            else if (m_collisionOptions.inner == CollisionOptions.TargetType.Outer)
                inner = outerDirection;

            if (m_innerKokan != null)
            {
                Vector3 innerPosition = m_bpKokanTarget.position + inner;
                Vector3 upVector = Vector3.Normalize(innerPosition - m_bpKokanTarget.position);
                Quaternion innerQuaternion = Quaternion.LookRotation(m_innerKokan.transform.forward, upVector);
                m_innerKokan.transform.rotation = innerQuaternion;
            }

            currentKokanDanDirection = outer;
            currentKokanPull += pullAmount * m_collisionOptions.kokanPullRate * Time.deltaTime;
            if (currentKokanPull > m_collisionOptions.maxKokanPush)
                currentKokanPull = m_collisionOptions.maxKokanPush;
            if (currentKokanPull < -m_collisionOptions.maxKokanPull)
                currentKokanPull = -m_collisionOptions.maxKokanPull;

            for (var kokanBone = 0; kokanBone < m_kokanPullBones.Count; kokanBone++)
            {
                m_kokanPullBones[kokanBone].localPosition = BoneNames.KokanPullWeights[kokanBone] * currentKokanPull * m_kokanPullBones[kokanBone].InverseTransformDirection(currentKokanDanDirection);
            }
       }

        internal void ReturnKokanBones()
        {
            if (m_innerKokan != null)
                m_innerKokan.localRotation = Quaternion.identity;

            if (MathHelpers.ApproximatelyZero(currentKokanPull) || m_kokanPullBones == null)
                return;

            var returnRate = m_collisionOptions.kokanReturnRate * Time.deltaTime;

            if (currentKokanPull > returnRate)
                currentKokanPull -= returnRate;
            else if (currentKokanPull < -returnRate)
                currentKokanPull += returnRate;
            else
                currentKokanPull = 0;

            for (var kokanBone = 0; kokanBone < m_kokanPullBones.Count; kokanBone++)
                m_kokanPullBones[kokanBone].localPosition = BoneNames.KokanPullWeights[kokanBone] * currentKokanPull * Vector3.up;
        }

        internal void PullOralBone(float pullAmount, Vector3 danDirection)
        {
            if (!m_collisionOptions.enableOralPush || m_oralPullBone == null)
            {
                ReturnOralBones();
                return;
            }

            currentOralPull += pullAmount * m_collisionOptions.oralPullRate * Time.deltaTime;
            if (currentOralPull > m_collisionOptions.maxOralPush)
                currentOralPull = m_collisionOptions.maxOralPush;
            if (currentOralPull < -m_collisionOptions.maxOralPull)
                currentOralPull = -m_collisionOptions.maxOralPull;

            currentOralDanDirection = danDirection;
            m_oralPullBone.localPosition = currentOralPull * m_oralPullBone.InverseTransformDirection(currentOralDanDirection);
        }

        internal void ReturnOralBones()
        {
            if (MathHelpers.ApproximatelyZero(currentOralPull) || m_oralPullBone == null)
                return;

            var returnRate = m_collisionOptions.oralReturnRate * Time.deltaTime;

            if (currentOralPull > returnRate)
                currentOralPull -= returnRate;
            else if (currentOralPull < -returnRate)
                currentOralPull += returnRate;
            else
                currentOralPull = 0;

            m_oralPullBone.localPosition = currentOralPull * m_oralPullBone.InverseTransformDirection(currentOralDanDirection);
        }

        internal void PullAnaBone(float pullAmount, Vector3 outerDirection, Vector3 insideDirection)
        {
            if (!m_collisionOptions.enableAnaPush || m_anaPullBones == null)
            {
                ReturnAnaBones();
                return;
            }

            Vector3 averageDirection = Vector3.Normalize(outerDirection + insideDirection);

            Vector3 outer = insideDirection;
            Vector3 inner = insideDirection;

            if (m_collisionOptions.outer == CollisionOptions.TargetType.Average)
                outer = averageDirection;
            else if (m_collisionOptions.outer == CollisionOptions.TargetType.Outer)
                outer = outerDirection;

            if (m_collisionOptions.inner == CollisionOptions.TargetType.Average)
                inner = averageDirection;
            else if (m_collisionOptions.inner == CollisionOptions.TargetType.Outer)
                inner = outerDirection;

            if (m_bpAnaTarget != null)
            {
                Vector3 innerPosition = m_bpAnaTarget.position + inner;
                Vector3 upVector = Vector3.Normalize(innerPosition - m_bpAnaTarget.position);
                Quaternion innerQuaternion = Quaternion.LookRotation(m_bpAnaTarget.transform.forward, upVector);
                m_bpAnaTarget.transform.rotation = innerQuaternion;
            }

            currentAnaDanDirection = outer;
            currentAnaPull += pullAmount * m_collisionOptions.anaPullRate * Time.deltaTime;
            if (currentAnaPull > m_collisionOptions.maxAnaPush)
                currentAnaPull = m_collisionOptions.maxAnaPush;
            if (currentAnaPull < -m_collisionOptions.maxAnaPull)
                currentAnaPull = -m_collisionOptions.maxAnaPull;

            for (var anaBone = 0; anaBone < m_anaPullBones.Count; anaBone++)
            {
                m_anaPullBones[anaBone].localPosition = BoneNames.AnaPullWeights[anaBone] * currentAnaPull * m_anaPullBones[anaBone].InverseTransformDirection(currentAnaDanDirection);
            }
        }

        internal void ReturnAnaBones()
        {
            if (m_bpAnaTarget != null)
                m_bpAnaTarget.localRotation = Quaternion.identity;

            if (MathHelpers.ApproximatelyZero(currentAnaPull) || m_anaPullBones == null)
                return;

            var returnRate = m_collisionOptions.anaReturnRate * Time.deltaTime;

            if (currentAnaPull > returnRate)
                currentAnaPull -= returnRate;
            else if (currentAnaPull < -returnRate)
                currentAnaPull += returnRate;
            else
                currentAnaPull = 0;

            for (var anaBone = 0; anaBone < m_anaPullBones.Count; anaBone++)
            {
                m_anaPullBones[anaBone].localPosition = BoneNames.AnaPullWeights[anaBone] * currentAnaPull * Vector3.up;
            }
        }

        internal void InitializeFingerColliders(float fingerRadius, float fingerLength)
        {
            m_fingerColliders = new List<DynamicBoneCollider>();
            foreach (var bone in BoneNames.FingerColliders)
            {
                var fingerTransform = Tools.GetTransformOfChaControl(m_collisionCharacter, bone);
                if (fingerTransform == null)
                    continue;

                var fingerCollider = Tools.InitializeCollider(fingerTransform, fingerRadius * (fingerTransform.lossyScale.y + fingerTransform.lossyScale.z) / 2, fingerLength * fingerTransform.lossyScale.x, Vector3.zero);

                m_fingerColliders.Add(fingerCollider);
            }
        }

        internal void AddFingerColliders(CollisionAgent target)
        {
            if (m_fingerColliders == null || m_fingerColliders.Count == 0)
                return;

            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                foreach (var collider in m_fingerColliders)
                {
                    if (collider != null && !dynamicBone.m_Colliders.Contains(collider))
                        dynamicBone.m_Colliders.Add(collider);
                }
            }
        }

        internal void RemoveFingerColliders(CollisionAgent target)
        {
            if (m_fingerColliders == null || m_fingerColliders.Count == 0)
                return;

            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                foreach (var collider in m_fingerColliders)
                {
                    if (collider != null)
                        dynamicBone.m_Colliders.Remove(collider);
                }
            }
        }

        internal void AddCollidersToKokan(List<DynamicBoneCollider> colliders)
        {
            if (colliders == null || colliders.Count == 0)
                return;

            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                foreach (DynamicBone dynamicBone in m_kokanDynamicBones)
                {
                    if (!dynamicBone.m_Colliders.Contains(collider))
                        dynamicBone.m_Colliders.Add(collider);
                }

                if (m_bellyDynamicBone == null)
                    continue;

                m_bellyDynamicBone.m_Colliders.Add(collider);
            }
        }

        internal void RemoveCollidersFromKokan(List<DynamicBoneCollider> colliders)
        {
            if (colliders == null || colliders.Count == 0)
                return;

            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                foreach (DynamicBone dynamicBone in m_kokanDynamicBones)
                        dynamicBone.m_Colliders.Remove(collider);

                if (!m_collisionOptions.enableBellyBulge || m_bellyDynamicBone == null)
                    continue;

                m_bellyDynamicBone.m_Colliders.Remove(collider);
            }
        }

        internal void AddCollidersToAna(List<DynamicBoneCollider> colliders)
        {
            if (colliders == null || colliders.Count == 0 || m_anaDynamicBones == null)
                return;

            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                if (!m_anaDynamicBones.m_Colliders.Contains(collider))
                    m_anaDynamicBones.m_Colliders.Add(collider);
            }
        }

        internal void RemoveCollidersFromAna(List<DynamicBoneCollider> colliders)
        {
            if (colliders == null || colliders.Count == 0 || m_anaDynamicBones == null)
                return;

            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                m_anaDynamicBones.m_Colliders.Remove(collider);
            }
        }

        internal void ClearKokanDynamicBones()
        {
            if (m_kokanDynamicBones == null || m_kokanDynamicBones.Count == 0)
                return;

            foreach (var kokanBone in m_kokanDynamicBones)
            {
                if (kokanBone == null)
                    continue;

                kokanBone.m_Colliders.Clear();
            }

            if (m_bellyDynamicBone == null)
                return;

            m_bellyDynamicBone.m_Colliders.Clear();
        }

        internal void ClearAnaDynamicBones()
        {
            if (m_anaDynamicBones == null)
                return;

            m_anaDynamicBones.m_Colliders.Clear();
        }
    }
}
//#endif