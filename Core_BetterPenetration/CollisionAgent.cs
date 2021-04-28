#if !HS2_STUDIO && !AI_STUDIO
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
        private Transform m_kokanBone;
        internal List<DynamicBone> m_kokanDynamicBones = new List<DynamicBone>();

        internal bool adjustFAnimation = false;
 
        public CollisionAgent(ChaControl character, CollisionOptions options)
        {
            Initialize(character, options);
        }

        private void Initialize(ChaControl character, CollisionOptions options)
        {
            m_collisionOptions = options;

            List<CollisionPoint> frontCollisionPoints = new List<CollisionPoint>();
            List<CollisionPoint> backCollisionPoints = new List<CollisionPoint>();
            m_collisionPointsFound = false;

            if (character == null)
                return;

            m_collisionCharacter = character;

            m_kokanBone = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.KokanBone));
            if (m_kokanBone == null)
                return;

            for (int index = 0; index < options.frontCollisionInfo.Count; index++)
            {
                Transform frontCollisionPoint = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(options.frontCollisionInfo[index].name));
                frontCollisionPoints.Add(new CollisionPoint(frontCollisionPoint, options.frontCollisionInfo[index]));
            }
            for (int index = 0; index < options.backCollisonInfo.Count; index++)
            {
                Transform backCollisionPoint = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(options.backCollisonInfo[index].name));
                backCollisionPoints.Add(new CollisionPoint(backCollisionPoint, options.backCollisonInfo[index]));
            }

            m_bpKokanTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(LookTargets.BPKokanTarget));
            if (m_bpKokanTarget != null)
            {
                frontCollisionPoints[0].transform = m_bpKokanTarget;
                frontCollisionPoints[0].info.name = LookTargets.BPKokanTarget;
            }

            m_innerTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(LookTargets.InnerTarget));
            m_innerHeadTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(LookTargets.InnerHeadTarget));

            if (frontCollisionPoints.Count == options.frontCollisionInfo.Count && 
                backCollisionPoints.Count == options.backCollisonInfo.Count &&
                m_innerTarget != null && m_innerHeadTarget != null)
            {
                m_collisionPointsFound = true;
                m_collisionPoints = new CollisionPoints(frontCollisionPoints, backCollisionPoints);
            }

            Debug.Log($"constrainPointsFound {m_collisionPointsFound}");

            m_kokanDynamicBones = new List<DynamicBone>();
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if (dynamicBone == null || dynamicBone.m_Root == null || dynamicBone.name == null || !dynamicBone.name.Contains(BoneNames.BPBone))
                    continue;

                dynamicBone.m_Colliders.Clear();
                m_kokanDynamicBones.Add(dynamicBone);
            }
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
                if (dynamicBone.name.Contains(BoneNames.BPBone))
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
    }
}
#endif