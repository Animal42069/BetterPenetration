using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if HS2 || AI || HS2_STUDIO || AI_STUDIO
using AIProject;
using AIChara;
#endif

namespace Core_BetterPenetration
{
    class DanAgent
    {
        internal ChaControl m_danCharacter;
        private DanPoints m_danPoints;
        private DanOptions m_danOptions;
		private List<DynamicBoneCollider> m_danColliders;
        private List<DynamicBone> m_tamaBones;
        private bool m_danPointsFound = false;
        private bool m_bpDanPointsFound = false;
        private bool m_bpTamaFound = false;
        private int tamaSelfColliders;

#if !HS2_STUDIO && !AI_STUDIO
        private Transform m_referenceTarget;
        private DynamicBoneCollider m_indexCollider;
        private DynamicBoneCollider m_middleCollider;
        private DynamicBoneCollider m_ringCollider;       
        private bool m_danPenetration = false;
#endif

#if HS2 || AI || HS2_STUDIO || AI_STUDIO
        private const float DefaultDanLength = 1.9f;
#elif KK
        private const float DefaultDanLength = 0.19f;
#endif

        private float m_baseDanLength = DefaultDanLength;
        private float m_baseSectionHalfLength = DefaultDanLength / 2;

        public DanAgent(ChaControl character, DanOptions options)
        {
            Initialize(character, options);
        }

        private void Initialize(ChaControl character, DanOptions options)
        {
            ClearDanAgent();

            if (character == null)
                return;

            m_danOptions = options;
            m_danCharacter = character;

            InitializeDan();
            InitializeTama();

#if !HS2_STUDIO && !AI_STUDIO
            UpdateFingerColliders(m_danOptions.fingerRadius, m_danOptions.fingerLength);
#endif

            Debug.Log($"BP Dan Found { m_bpDanPointsFound}; BP Tama Found {m_bpTamaFound}");
        }

        private void InitializeDan()
        {
            List<Transform> danTransforms = new List<Transform>();
            foreach (var boneName in BoneNames.DanBones)
            {
                Transform danBone = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(boneName));
                if (danBone != null)
                    danTransforms.Add(danBone);
            }

            Transform tamaTop = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.TamaTop));

            if (tamaTop == null || danTransforms.Count < 2)
                return;

            if (danTransforms.Count == BoneNames.DanBones.Count)
                m_bpDanPointsFound = true;

            m_danPoints = new DanPoints(danTransforms, tamaTop);
            m_danPointsFound = true;
            m_baseDanLength = DefaultDanLength * m_danPoints.GetDanLossyScale();
            m_baseSectionHalfLength = m_baseDanLength / (2 * (m_danPoints.danPoints.Count - 1));

            for (int danPoint = 1; danPoint < m_danPoints.danPoints.Count; danPoint++)
            {
                m_danColliders.Add(InitializeCollider(m_danPoints.danPoints[danPoint - 1].transform, m_danOptions.danRadius * m_danPoints.danPoints[danPoint].defaultLossyScale.x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                    new Vector3(0, m_danOptions.danVerticalCenter, m_baseSectionHalfLength), DynamicBoneCollider.Direction.Z));
            }
        }

        private void InitializeTama()
        {
            m_tamaBones = new List<DynamicBone>();

            foreach(var tamaBoneName in BoneNames.TamaBones)
            {
                var tamaBone = m_danCharacter.GetComponentsInChildren<DynamicBone>().FirstOrDefault(x => x.name != null && x.name.Contains(tamaBoneName));

                if (tamaBone == null)
                    return;

                m_tamaBones.Add(tamaBone);
            }

            m_bpTamaFound = true;

            foreach (var tamaBone in m_tamaBones)
            {
                if (m_bpDanPointsFound && m_danColliders.Count >= 2)
                    tamaBone.m_Colliders.Add(m_danColliders[1]);
                else if (m_danColliders.Count >= 1)
                    tamaBone.m_Colliders.Add(m_danColliders[0]);
            }

            AddTamaColliders(m_danCharacter);
            tamaSelfColliders = m_tamaBones[0].m_Colliders.Count();
        }

        internal void ResetTamaParticles()
        {
            foreach (var tamaBone in m_tamaBones)
            {
                if (tamaBone == null)
                    continue;

                tamaBone.ResetParticlesPosition();
            }
        }

        private DynamicBoneCollider InitializeCollider(Transform parent, float radius, float length, Vector3 centerOffset,
            DynamicBoneCollider.Direction direction = DynamicBoneCollider.Direction.X,
            DynamicBoneCollider.Bound bound = DynamicBoneCollider.Bound.Outside)
        {
            if (parent == null)
                return null;

            DynamicBoneCollider collider = parent.GetComponent<DynamicBoneCollider>();

            if (collider == null)
                collider = parent.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

            collider.m_Direction = direction;
            collider.m_Center = centerOffset;
            collider.m_Bound = bound;
            collider.m_Radius = radius;
            collider.m_Height = length;

            return collider;
        }

        internal void UpdateDanCollider(float danRadius, float danHeadLength, float danVerticalCenter)
        {
            if (!m_danPointsFound || m_danColliders.Count < 1 || m_danColliders.Count >= m_danPoints.danPoints.Count)
                return;

            m_danOptions.danRadius = danRadius;
            m_danOptions.danHeadLength = danHeadLength;
            m_danOptions.danVerticalCenter = danVerticalCenter;

            for (int danCollider = 0; danCollider < m_danColliders.Count; danCollider++)
            {
                m_danColliders[danCollider] = InitializeCollider(m_danPoints.danPoints[danCollider].transform, m_danOptions.danRadius * m_danPoints.danPoints[danCollider + 1].defaultLossyScale.x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                    new Vector3(0, m_danOptions.danVerticalCenter, m_baseSectionHalfLength), DynamicBoneCollider.Direction.Z);
            }
        }

        internal void UpdateDanCollider(DanOptions danOptions)
        {
            if (!m_danPointsFound || m_danColliders.Count < 1 || m_danColliders.Count >= m_danPoints.danPoints.Count)
                return;

            m_danOptions.danRadius = danOptions.danRadius;
            m_danOptions.danHeadLength = danOptions.danHeadLength;
            m_danOptions.danVerticalCenter = danOptions.danVerticalCenter;

            for (int danCollider = 0; danCollider < m_danColliders.Count; danCollider++)
            {
                m_danColliders[danCollider] = InitializeCollider(m_danPoints.danPoints[danCollider].transform, m_danOptions.danRadius * m_danPoints.danPoints[danCollider + 1].defaultLossyScale.x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                    new Vector3(0, m_danOptions.danVerticalCenter, m_baseSectionHalfLength), DynamicBoneCollider.Direction.Z);
            }
        }

        internal void UpdateDanOptions(DanOptions danOptions)
        {
            m_danOptions = danOptions;
        }

        internal void UpdateDanOptions(float danLengthSquish, float danGirthSquish, float squishThreshold)
        {
            m_danOptions.danLengthSquish = danLengthSquish;
            m_danOptions.danGirthSquish = danGirthSquish;
            m_danOptions.squishThreshold = squishThreshold;
        }

        internal void SetDanTarget(Vector3 enterTarget, Vector3 endTarget)
        {
            if (!m_danPointsFound)
                return;

            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();

            float danDistanceToTarget = Vector3.Distance(danStartPosition, enterTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            m_danPoints.SquishDanGirth(girthScaleFactor);
            adjustedDanLength = GetMaxDanLength(adjustedDanLength, enterTarget, endTarget, danDistanceToTarget);
            ScaleDanColliders(adjustedDanLength);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(enterTarget, endTarget, adjustedDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints);
        }

        private float GetSquishedDanLength(float danDistanceToTarget)
        {
            float danLength = m_baseDanLength;
            float innerSquishDistance = m_baseDanLength - m_baseDanLength * m_danOptions.squishThreshold - danDistanceToTarget;

            if (innerSquishDistance > 0)
                danLength = m_baseDanLength - (innerSquishDistance * m_danOptions.danLengthSquish);

            return danLength;
        }

        private float GetSquishedDanGirth(float danDistanceToTarget)
        {
            float girthScaleFactor = 1;
            float innerSquishDistance = m_baseDanLength - m_baseDanLength * m_danOptions.squishThreshold - danDistanceToTarget;

            if (innerSquishDistance > 0)
                girthScaleFactor = 1 + innerSquishDistance / m_baseDanLength * m_danOptions.danGirthSquish;

            return girthScaleFactor;
        }

        private float GetMaxDanLength(float danLength, Vector3 lookTarget, Vector3 limitPoint, float danDistanceToTarget)
        {
            float maxDanLength = danLength;
            float targetDistanceToLimit = Vector3.Distance(lookTarget, limitPoint);

            if (maxDanLength > danDistanceToTarget + targetDistanceToLimit)
                maxDanLength = danDistanceToTarget + targetDistanceToLimit;

            return maxDanLength;
        }

        private List<Vector3> AdjustDanPointsToTargets(Vector3 danLookTarget, Vector3 danEndTarget, float danLength, float danDistanceToTarget)
        {
            List<Vector3> adjustedDanPoints = new List<Vector3>();
            foreach (var point in m_danPoints.danPoints)
                adjustedDanPoints.Add(point.transform.position);

            Vector3 outsideVector = Vector3.Normalize(danLookTarget - adjustedDanPoints[0]);
            Vector3 insideVector = Vector3.Normalize(danEndTarget - danLookTarget);

            bool singleVector = false;
            if (!m_bpDanPointsFound)
            {
                insideVector = outsideVector = Vector3.Normalize(danEndTarget - adjustedDanPoints[0]);
                singleVector = true;
            }
            else if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f) || danLength <= danDistanceToTarget)
            {
                insideVector = outsideVector;
                singleVector = true;
            }

            float danSegmentLength = danLength / (adjustedDanPoints.Count - 1);
            for (int point = 1; point < adjustedDanPoints.Count; point++)
            {
                float outsideDistance = danDistanceToTarget - danSegmentLength * (point - 1);

                if (singleVector || outsideDistance < 0.001)
                {
                    adjustedDanPoints[point] = adjustedDanPoints[point - 1] + insideVector * danSegmentLength;
                }
                else if (outsideDistance < (danSegmentLength - 0.001))
                {      
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    adjustedDanPoints[point] = danLookTarget + insideVector * (float)distanceAlongInside;
                }
                else
                {
                    adjustedDanPoints[point] = adjustedDanPoints[point - 1] + outsideVector * danSegmentLength;
                }
            }

            return adjustedDanPoints;
        }

        private void ScaleDanColliders(float danLength)
        {
            if (m_danColliders == null)
                return;

            float danSegmentLength = danLength / m_danColliders.Count;
            foreach (var collider in m_danColliders)
                collider.m_Center = new Vector3(collider.m_Center.x, collider.m_Center.y, danSegmentLength / 2);
        }

        internal void AddDanColliders(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null && dynamicBone.name.Contains(BoneNames.BPBone) && !dynamicBone.m_Colliders.Contains(danCollider))
                        dynamicBone.m_Colliders.Add(danCollider);
                }
            }
        }

        internal void RemoveDanColliders(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null && dynamicBone.name.Contains(BoneNames.BPBone))
                        dynamicBone.m_Colliders.Remove(danCollider);
                }
            }
        }

        internal void AddTamaColliders(ChaControl target, bool midSectionOnly = true)
        {
            if (!m_bpTamaFound)
                return;

            foreach (DynamicBoneCollider dynamicBoneCollider in target.GetComponentsInChildren<DynamicBoneCollider>())
            {
                if (dynamicBoneCollider == null || 
                    (midSectionOnly && !BoneNames.MidSectionColliders.Contains(dynamicBoneCollider.name)) || 
                    !BoneNames.BodyColliders.Contains(dynamicBoneCollider.name))
                    continue;

                foreach (var tamaBone in m_tamaBones)
                {
                    if (tamaBone == null)
                        continue;

                    tamaBone.m_Colliders.Add(dynamicBoneCollider);
                }
            }
        }

        internal void RemoveTamaColliders()
        {
            if (!m_bpTamaFound)
                return;

            foreach (var tamaBone in m_tamaBones)
            {
                if (tamaBone == null)
                    continue;

                if (tamaBone.m_Colliders.Count > tamaSelfColliders)
                    tamaBone.m_Colliders.RemoveRange(tamaSelfColliders, tamaBone.m_Colliders.Count - tamaSelfColliders);
            }
        }

#if !HS2_STUDIO && !AI_STUDIO
        internal void UpdateDanOptions(float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth, bool useFingerColliders, bool simplifyPenetration, bool simplifyOral)
        {
            m_danOptions.danLengthSquish = danLengthSquish;
            m_danOptions.danGirthSquish = danGirthSquish;
            m_danOptions.squishThreshold = squishThreshold;
            m_danOptions.squishOralGirth = squishOralGirth;
            m_danOptions.useFingerColliders = useFingerColliders;
            m_danOptions.simplifyPenetration = simplifyPenetration;
            m_danOptions.simplifyOral = simplifyOral;
        }

        internal void UpdateFingerColliders(float fingerRadius, float fingerLength)
        {
            if (!m_danPointsFound)
                return;

            Transform index = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.IndexFinger));
            Transform middle = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.MiddleFinger));
            Transform ring = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.RingFinger));

            m_indexCollider = InitializeCollider(index, fingerRadius, fingerLength, Vector3.zero);
            m_middleCollider = InitializeCollider(middle, fingerRadius, fingerLength, Vector3.zero);
            m_ringCollider = InitializeCollider(ring, fingerRadius, fingerLength, Vector3.zero);

            m_danOptions.fingerRadius = fingerRadius;
            m_danOptions.fingerLength = fingerLength;
        }

        internal void SetDanTarget(CollisionAgent targetAgent)
        {
            if (!m_danPointsFound || !targetAgent.m_collisionPointsFound)
                return;

            if (m_referenceTarget == null)
            {
                m_danPoints.AimDanTop();
                return;
            }

            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_referenceTarget.position;

            if (m_referenceTarget.name == LookTargets.KokanTarget)
                danTarget += (m_referenceTarget.forward * targetAgent.m_collisionOptions.kokanForwardOffset) + (m_referenceTarget.up * targetAgent.m_collisionOptions.kokanUpOffset);
            else if (m_referenceTarget.name == LookTargets.HeadTarget)
                danTarget += (m_referenceTarget.forward * targetAgent.m_collisionOptions.headForwardOffset) + (m_referenceTarget.up * targetAgent.m_collisionOptions.headUpOffset);

            if (targetAgent.m_collisionOptions.kokan_adjust && targetAgent.adjustFAnimation && (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.BPKokanTarget))
                danTarget += m_referenceTarget.forward * targetAgent.m_collisionOptions.kokan_adjust_position_z;

            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;

            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            if (m_referenceTarget.name != LookTargets.HeadTarget || m_danOptions.squishOralGirth)
                m_danPoints.SquishDanGirth(girthScaleFactor);

            if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                adjustedDanLength = GetMaxDanLength(adjustedDanLength, danTarget, targetAgent.m_innerTarget.position, danDistanceToTarget);
            else if (m_referenceTarget.name == LookTargets.HeadTarget)             
                adjustedDanLength = GetMaxDanLength(adjustedDanLength, danTarget, targetAgent.m_innerHeadTarget.position, danDistanceToTarget);

            danEndTarget = ConstrainDan(danStartPosition, danTargetVector, danEndTarget, adjustedDanLength, danDistanceToTarget, targetAgent);
            ScaleDanColliders(adjustedDanLength);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints);
        }

        private Vector3 ConstrainDan(Vector3 danStart, Vector3 danTargetVector, Vector3 danEndTarget, float danLength, float danDistanceToTarget, CollisionAgent targetAgent)
        {
            if (m_danPenetration)
            {
                if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                {
                    if (m_danOptions.simplifyPenetration && m_bpDanPointsFound)
                        danEndTarget = targetAgent.m_innerTarget.position;
                    else
                        danEndTarget = ConstrainDanToBody(targetAgent, danStart, danTargetVector, danLength, m_referenceTarget.name == LookTargets.AnaTarget);
                }
                else if (m_referenceTarget.name == LookTargets.HeadTarget)
                {
                    if (m_danOptions.simplifyOral && m_bpDanPointsFound)
                        danEndTarget = targetAgent.m_innerHeadTarget.position;
                    else
                        danEndTarget = ConstrainDanToHead(danStart, danTargetVector, danLength);
                }
            }
            else if ((m_referenceTarget.name == LookTargets.KokanTarget) && (m_baseDanLength > danDistanceToTarget))
            {
                danEndTarget = ConstrainDanToPull(danStart, danTargetVector, danLength);
            }

            return danEndTarget;
        }

        private Vector3 ConstrainDanToBody(CollisionAgent target, Vector3 dan101_pos, Vector3 danVector, float squishedDanLength, bool anaTarget)
        {
            Vector3 adjustedDan109 = dan101_pos + danVector * squishedDanLength;
            bool constrainPointFound = false;

            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, anaTarget);
            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, anaTarget);
            return adjustedDan109;
        }

        private Vector3 ConstrainDanToCollisionPoints(Vector3 danStart, Vector3 danEnd, float targetLength, ref bool constrainPointFound, List<CollisionPoint> collisionPoints, float clippingDepth, bool frontSide, bool anaTarget)
        {
            Vector3 adjustedDanEnd = danEnd;
            if (constrainPointFound)
                return adjustedDanEnd;

            var constainPastNearSide = true;
            var constainPastFarSide = false;

            List<Vector3> collisionPointPositions = new List<Vector3>();

            var firstPoint = true;

            foreach (var collisionPoint in collisionPoints)
            {
                var offset = collisionPoint.info.offset;
                if (firstPoint && (anaTarget == frontSide))
                    offset = 0;

                if (frontSide == collisionPoint.info.inward)
                    collisionPointPositions.Add(collisionPoint.transform.position + (clippingDepth + offset) * collisionPoint.transform.forward);
                else
                    collisionPointPositions.Add(collisionPoint.transform.position - (clippingDepth + offset) * collisionPoint.transform.forward);

                firstPoint = false;
            }
  
            for (int index = 1; index < collisionPoints.Count; index++)
            {
                if (constrainPointFound)
                    break;

                CollisionPoint nearPoint = collisionPoints[index - 1];
                CollisionPoint farPoint = collisionPoints[index];

                Vector3 nearVectorRight = nearPoint.transform.right;
                Vector3 farVectorRight = farPoint.transform.right;

                if (frontSide == nearPoint.info.inward)
                    nearVectorRight = -nearVectorRight;

                if (frontSide == farPoint.info.inward)
                    farVectorRight = -farVectorRight;

                TwistedPlane hPlane = new TwistedPlane(collisionPointPositions[index - 1], nearVectorRight, collisionPointPositions[index], farVectorRight);

                if (index == collisionPoints.Count - 1)
                    constainPastFarSide = true;

                adjustedDanEnd = hPlane.ConstrainLineToTwistedPlane(danStart, danEnd, targetLength, ref constainPastNearSide, constainPastFarSide, out constrainPointFound);
            }

            return adjustedDanEnd;
        }

        private Vector3 ConstrainDanToHead(Vector3 dan101_pos, Vector3 danVector, float squishedDanLength)
        {
            return (dan101_pos + danVector * squishedDanLength);
        }

        private Vector3 ConstrainDanToPull(Vector3 dan101_pos, Vector3 danVector, float squishedDanLength)
        {
            return (dan101_pos + danVector * squishedDanLength);
        }

        private void ResetDanAdjustment()
        {
            m_danPoints.ResetDanPoints();
            ScaleDanColliders(m_baseDanLength);
        }

        internal void ClearDanTarget(CollisionAgent firstTarget, CollisionAgent secondTarget = null)
        {
            ClearTarget();
            ResetDanAdjustment();
            RemoveTamaColliders();
            RemoveColliders(firstTarget);
            if (secondTarget != null)
                RemoveColliders(secondTarget);
        }

        internal void SetupNewDanTarget(Transform lookAtTransform, string currentMotion, bool topStick, CollisionAgent firstTarget, CollisionAgent secondTarget = null)
        {
            ClearDanTarget(firstTarget, secondTarget);

            if (!m_danPointsFound)
                return;

            if (secondTarget != null)
            {
                if (lookAtTransform == null)
                {
                    AddDanColliders(firstTarget);
                    AddDanColliders(secondTarget);
                }
                else if (lookAtTransform.name == LookTargets.KokanTarget)
                {
                    AddDanColliders(firstTarget);
                    if (m_danOptions.useFingerColliders)
                        AddFingerColliders(secondTarget);
                }
            }
            else
            {
                if (lookAtTransform == null || lookAtTransform.name == LookTargets.KokanTarget)
                    AddDanColliders(firstTarget);

                if (m_danOptions.useFingerColliders && lookAtTransform == null)
                    AddFingerColliders(firstTarget);
            }

            AddTamaColliders(firstTarget.m_collisionCharacter, false);
            if (secondTarget != null)
                AddTamaColliders(secondTarget.m_collisionCharacter, false);

            if (currentMotion == string.Empty)
                return;

            m_referenceTarget = lookAtTransform;
#if KK
            m_danPenetration = (topStick && m_referenceTarget != null) || currentMotion.Contains("IN");
#else
            m_danPenetration = topStick && m_referenceTarget != null;
#endif

            if (m_danPenetration && firstTarget.m_bpKokanTarget != null &&
               (m_referenceTarget == null || m_referenceTarget.name == LookTargets.KokanTarget) && 
               !currentMotion.Contains("Idle") && !currentMotion.Contains("Pull") && !currentMotion.Contains("OUT"))
                m_referenceTarget = firstTarget.m_bpKokanTarget;
        }

        private void AddDanColliders(CollisionAgent target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
                {
                    if (danCollider != null && !dynamicBone.m_Colliders.Contains(danCollider))
                        dynamicBone.m_Colliders.Add(danCollider);
                }
            }
        }

        private void RemoveDanColliders(CollisionAgent target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
                {
                    if (danCollider != null)
                        dynamicBone.m_Colliders.Remove(danCollider);
                }
            }
        }
	
        private void AddFingerColliders(CollisionAgent target)
        {
            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                if (m_indexCollider != null && !dynamicBone.m_Colliders.Contains(m_indexCollider))
                    dynamicBone.m_Colliders.Add(m_indexCollider);

                if (m_middleCollider != null && !dynamicBone.m_Colliders.Contains(m_middleCollider))
                    dynamicBone.m_Colliders.Add(m_middleCollider);

                if (m_ringCollider != null && !dynamicBone.m_Colliders.Contains(m_ringCollider))
                    dynamicBone.m_Colliders.Add(m_ringCollider);
            }
        }

        private void RemoveFingerColliders(CollisionAgent target)
        {
            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                if (m_indexCollider != null)
                    dynamicBone.m_Colliders.Remove(m_indexCollider);

                if (m_middleCollider != null)
                    dynamicBone.m_Colliders.Remove(m_middleCollider);

                if (m_ringCollider != null)
                    dynamicBone.m_Colliders.Remove(m_ringCollider);
            }
        }

        private void ClearTarget()
        {
            m_referenceTarget = null;
            m_danPenetration = false;
        }

        internal void RemoveColliders(CollisionAgent target)
        {
            RemoveDanColliders(target);
            RemoveFingerColliders(target);
        }
#endif

            internal void ClearDanAgent()
        {
            m_danPointsFound = false;
            m_bpDanPointsFound = false;
            m_bpTamaFound = false;

            if (m_danPoints != null)
                m_danPoints.ResetDanPoints();
            m_danPoints = null;

            if (m_danColliders != null)
                foreach (var danCollider in m_danColliders)
                    UnityEngine.Object.Destroy(danCollider);

            m_tamaBones = new List<DynamicBone>();
            m_danColliders = new List<DynamicBoneCollider>();

#if !HS2_STUDIO && !AI_STUDIO

            ClearTarget();

            UnityEngine.Object.Destroy(m_indexCollider);
            UnityEngine.Object.Destroy(m_middleCollider);
            UnityEngine.Object.Destroy(m_ringCollider);
#endif
        }
    }
}