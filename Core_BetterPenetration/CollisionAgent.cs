using System;
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
        internal Transform m_innerTarget;
        internal Transform m_innerHeadTarget;
        internal Transform m_kokanBone;
        internal Transform m_oralPullBone;
        internal List<DynamicBone> m_kokanDynamicBones = new List<DynamicBone>();
        internal List<Transform> m_kokanPullBones = new List<Transform>();

        internal float currentKokanPull = 0f;
        internal float currentOralPull = 0f;
        internal Vector3 currentKokanDanDirection = Vector3.up;
        internal Vector3 currentOralDanDirection = Vector3.Normalize(Vector3.up + Vector3.back);
        internal bool adjustFAnimation = false;
 
        public CollisionAgent(ChaControl character, CollisionOptions options)
        {
            Initialize(character, options);
        }

        internal void Initialize(ChaControl character, CollisionOptions options)
        {
            m_collisionOptions = options;
            currentKokanPull = 0f;
            currentOralPull = 0f;
            currentKokanDanDirection = Vector3.up;
            currentOralDanDirection = Vector3.Normalize(Vector3.up + Vector3.back);

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
            m_innerTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.InnerTarget);
            m_innerHeadTarget = Tools.GetTransformOfChaControl(m_collisionCharacter, LookTargets.InnerHeadTarget);
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
            
            if (m_bpKokanTarget != null)
            {
                frontCollisionPoints[0].transform = m_bpKokanTarget;
                frontCollisionPoints[0].info.name = LookTargets.BPKokanTarget;
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
                    !dynamicBone.name.Contains(BoneNames.BPBone) || 
                    m_collisionCharacter != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                dynamicBone.m_Colliders.Clear();
                m_kokanDynamicBones.Add(dynamicBone);
            }

            m_kokanPullBones = new List<Transform>();
            foreach (var boneName in BoneNames.KokanPullBones)
            {
                var kokanTransform = Tools.GetTransformOfChaControl(m_collisionCharacter, boneName);
                if (kokanTransform == null)
                    continue;

                m_kokanPullBones.Add(kokanTransform);
            }

            m_oralPullBone = Tools.GetTransformOfChaControl(m_collisionCharacter, BoneNames.mouthPullBone);
        }

        internal void CheckForAdjustment(string animationFile)
        {
            adjustFAnimation = false;
            if (m_kokanBone != null && BoneNames.animationAdjustmentList.Contains(animationFile))
                adjustFAnimation = true;
        }

        internal void AdjustMissionaryAnimation()
        {
            if (!m_collisionOptions.kokan_adjust || !adjustFAnimation || m_kokanBone == null)
                return;

            m_kokanBone.localPosition += new Vector3(0, m_collisionOptions.kokan_adjust_position_y, m_collisionOptions.kokan_adjust_position_z);
            m_kokanBone.localEulerAngles += new Vector3(m_collisionOptions.kokan_adjust_rotation_x, 0, 0);
        }

        internal void UpdateCollisionOptions(CollisionOptions options)
        {
            m_collisionOptions = options;

            if (m_collisionPoints == null)
                return;

            m_collisionPoints.UpdateCollisionOptions(options);
        }

        internal void ClearColliders()
        {
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if (dynamicBone.name.Contains(BoneNames.BPBone) && m_collisionCharacter == dynamicBone.GetComponentInParent<ChaControl>())
                    dynamicBone.m_Colliders.Clear();
            }
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

        internal void PullKokanBones(float pullAmount, Vector3 danDirection)
        {
            if (!m_collisionOptions.enableKokanPush || m_kokanPullBones == null)
                return;

            currentKokanDanDirection = danDirection;
            currentKokanPull += pullAmount * m_collisionOptions.kokanPullRate * Time.deltaTime;
            if (currentKokanPull > m_collisionOptions.maxKokanPush)
                currentKokanPull = m_collisionOptions.maxKokanPush;
            if (currentKokanPull < -m_collisionOptions.maxKokanPull)
                currentKokanPull = -m_collisionOptions.maxKokanPull;

            if (m_collisionOptions.useDanAngle)
            {
                for (var kokanBone = 0; kokanBone < m_kokanPullBones.Count; kokanBone++)
                {
                    if (kokanBone < 2)
                        m_kokanPullBones[kokanBone].localPosition = 0.4f * currentKokanPull * m_kokanPullBones[kokanBone].InverseTransformDirection(currentKokanDanDirection);
                    else if (kokanBone < 4)
                        m_kokanPullBones[kokanBone].localPosition = 0.7f * currentKokanPull * m_kokanPullBones[kokanBone].InverseTransformDirection(currentKokanDanDirection);
                    else
                        m_kokanPullBones[kokanBone].localPosition = currentKokanPull * m_kokanPullBones[kokanBone].InverseTransformDirection(currentKokanDanDirection);
                }
            }
            else
            {
                for (var kokanBone = 0; kokanBone < m_kokanPullBones.Count; kokanBone++)
                {
                    if (kokanBone < 2)
                        m_kokanPullBones[kokanBone].localPosition = 0.4f * currentKokanPull * Vector3.up;
                    else if (kokanBone < 4)
                        m_kokanPullBones[kokanBone].localPosition = 0.7f * currentKokanPull * Vector3.up;
                    else
                        m_kokanPullBones[kokanBone].localPosition = currentKokanPull * Vector3.up;
                }
            }
        }

        internal void ReturnKokanBones()
        {
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
            {
                if (kokanBone < 2)
                    m_kokanPullBones[kokanBone].localPosition = 0.6f * currentKokanPull * Vector3.up;
                else if (kokanBone < 4)
                    m_kokanPullBones[kokanBone].localPosition = 0.8f * currentKokanPull * Vector3.up;
                else
                    m_kokanPullBones[kokanBone].localPosition = currentKokanPull * Vector3.up;
            }
        }

        internal void PullOralBone(float pullAmount, Vector3 danDirection)
        {
            if (!m_collisionOptions.enableOralPush || m_oralPullBone == null)
                return;

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
    }
}
//#endif