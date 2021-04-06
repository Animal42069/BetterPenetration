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
        private bool m_collisionPointsFound = false;

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
                Console.WriteLine("BP Target Found " + m_bpKokanTarget.name);
                frontCollisionPoints[0].transform = m_bpKokanTarget;
                frontCollisionPoints[0].info.name = LookTargets.BPKokanTarget;
            }

            m_innerTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(LookTargets.InnerTarget));
            m_innerHeadTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(LookTargets.InnerHeadTarget));

            if (frontCollisionPoints.Count == options.frontCollisionInfo.Count && backCollisionPoints.Count == options.backCollisonInfo.Count)
            {
                m_collisionPointsFound = true;
                m_collisionPoints = new CollisionPoints(frontCollisionPoints, backCollisionPoints);
            }

            Console.WriteLine("constrainPointsFound " + m_collisionPointsFound);

            m_kokanDynamicBones = new List<DynamicBone>();
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {

                if (!dynamicBone.name.Contains(BoneNames.BPBone))
                    continue;
#if KK
                m_kokanDynamicBones.Add(dynamicBone);
#else
                dynamicBone.m_Colliders.Clear();

                if (dynamicBone == null || dynamicBone.m_Root == null)
                    continue;

                if (!BoneNames.uncensorBoneList.TryGetValue(dynamicBone.m_Root.name, out UncensorDynamicBone dynamicBoneValues))
                    continue;

                if (dynamicBoneValues.direction == UncensorDynamicBone.DynamicBoneDirection.Z)
                    dynamicBone.m_Radius *= m_kokanBone.lossyScale.z;
                else if (dynamicBoneValues.direction == UncensorDynamicBone.DynamicBoneDirection.Z)
                    dynamicBone.m_Radius *= m_kokanBone.lossyScale.x;
                else
                    dynamicBone.m_Radius *= (m_kokanBone.lossyScale.x + m_kokanBone.lossyScale.z) / 2;

#if HS2 || AIS || HS2_STUDIO || AI_STUDIO
                dynamicBone.UpdateParameters();
#endif
                m_kokanDynamicBones.Add(dynamicBone);

                if (!m_collisionOptions.useBoundingColliders || dynamicBoneValues.selfColliderName == null)
                    continue;

                DynamicBoneCollider selfCollider = m_collisionCharacter.GetComponentsInChildren<DynamicBoneCollider>().FirstOrDefault(x => x.name != null && x.name.Contains(dynamicBoneValues.selfColliderName));
                if (selfCollider == null)
                {
                    Transform colliderTransform = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(dynamicBoneValues.selfColliderName));
                    if (colliderTransform == null)
                        continue;

                    selfCollider = colliderTransform.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                    selfCollider.m_Bound = DynamicBoneCollider.Bound.Inside;
                    selfCollider.m_Direction = DynamicBoneCollider.Direction.Y;

                    if (dynamicBoneValues.direction == UncensorDynamicBone.DynamicBoneDirection.Z)
                    {
                        selfCollider.m_Height = dynamicBoneValues.selfColliderHeight * m_kokanBone.lossyScale.z;
                        selfCollider.m_Radius = dynamicBoneValues.selfColliderRadius * m_kokanBone.lossyScale.z;
                    }
                    else if (dynamicBoneValues.direction == UncensorDynamicBone.DynamicBoneDirection.X)
                    {
                        selfCollider.m_Height = dynamicBoneValues.selfColliderHeight * m_kokanBone.lossyScale.x;
                        selfCollider.m_Radius = dynamicBoneValues.selfColliderRadius * m_kokanBone.lossyScale.x;
                    }
                    else
                    {
                        selfCollider.m_Height = dynamicBoneValues.selfColliderHeight * (m_kokanBone.lossyScale.x + m_kokanBone.lossyScale.z) / 2;
                        selfCollider.m_Radius = dynamicBoneValues.selfColliderRadius * (m_kokanBone.lossyScale.x + m_kokanBone.lossyScale.z) / 2;
                    }
                }

                dynamicBone.m_Colliders.Add(selfCollider);
#endif
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
    }
}
#endif