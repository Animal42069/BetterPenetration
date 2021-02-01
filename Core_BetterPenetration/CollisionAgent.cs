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
        public ChaControl m_collisionCharacter;
        public CollisionPoints m_collisionPoints;
        public CollisionOptions m_collisionOptions;
        private bool m_collisionPointsFound = false;

        public Transform m_bpKokanTarget;
        public Transform m_innerTarget;
        public Transform m_innerHeadTarget;
        public Transform m_kokanBone;
        public List<DynamicBone> m_kokanDynamicBones = new List<DynamicBone>();

        public bool adjustFAnimation = false;
 
        public CollisionAgent(ChaControl character, CollisionOptions options)
        {
            Initialize(character, options);
        }

        public void Initialize(ChaControl character, CollisionOptions options)
        {
            m_collisionOptions = options;

            List<CollisionPoint> frontHPoints = new List<CollisionPoint>();
            List<CollisionPoint> backHPoints = new List<CollisionPoint>();
            m_collisionPointsFound = false;

            if (character == null)
                return;

            m_collisionCharacter = character;

            m_kokanBone = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.KokanBone));
            if (m_kokanBone == null)
                return;

            for (int index = 0; index < options.frontCollisionInfo.Count; index++)
            {
                Transform frontHPoint = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(options.frontCollisionInfo[index].name));
                frontHPoints.Add(new CollisionPoint(frontHPoint, options.frontCollisionInfo[index]));
            }
            for (int index = 0; index < options.backCollisonInfo.Count; index++)
            {
                Transform backHPoint = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(options.backCollisonInfo[index].name));
                backHPoints.Add(new CollisionPoint(backHPoint, options.backCollisonInfo[index]));
            }
            Transform backOfHead = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.HeadLimit));

            m_bpKokanTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(LookTargets.BPKokanTarget));
            if (m_bpKokanTarget != null)
            {
                Console.WriteLine("BP Target Found " + m_bpKokanTarget.name);
                frontHPoints[0].transform = m_bpKokanTarget;
                frontHPoints[0].info.name = LookTargets.BPKokanTarget;
            }

            m_innerTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(LookTargets.InnerTarget));
            m_innerHeadTarget = m_collisionCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(LookTargets.InnerHeadTarget));

            if (frontHPoints.Count == options.frontCollisionInfo.Count && backHPoints.Count == options.backCollisonInfo.Count && backOfHead != null)
            {
                m_collisionPointsFound = true;
                m_collisionPoints = new CollisionPoints(frontHPoints, backHPoints, backOfHead);
            }

            Console.WriteLine("constrainPointsFound " + m_collisionPointsFound);

            m_kokanDynamicBones = new List<DynamicBone>();
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if (!dynamicBone.name.Contains(BoneNames.BPBone))
                    continue;

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

#if HS2 || AIS
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
            }
        }

        public void CheckForAdjustment(string animationFile)
        {
            adjustFAnimation = false;
            if (m_kokanBone != null && BoneNames.animationAdjustmentList.Contains(animationFile))
                adjustFAnimation = true;
        }

        public void AdjustMissionaryAnimation()
        {
            if (!m_collisionOptions.kokan_adjust || !adjustFAnimation || m_kokanBone == null)
                return;

            m_kokanBone.localPosition += new Vector3(0, m_collisionOptions.kokan_adjust_position_y, m_collisionOptions.kokan_adjust_position_z);
            m_kokanBone.localEulerAngles += new Vector3(m_collisionOptions.kokan_adjust_rotation_x, 0, 0);
        }

        public void UpdateCollisionOptions(CollisionOptions options)
        {
            m_collisionOptions = options;
        }

        public void ClearColliders()
        {
            foreach (DynamicBone dynamicBone in m_collisionCharacter.GetComponentsInChildren<DynamicBone>())
            {
                if (dynamicBone.name.Contains(BoneNames.BPBone))
                    dynamicBone.m_Colliders.Clear();
            }
        }
    }
}
