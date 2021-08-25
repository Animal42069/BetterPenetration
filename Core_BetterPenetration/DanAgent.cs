using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if HS2 || AI
using AIChara;
#endif

namespace Core_BetterPenetration
{
    class DanAgent
    {
        internal ChaControl m_danCharacter;
        internal DanPoints m_danPoints;
        internal DanOptions m_danOptions;
		internal List<DynamicBoneCollider> m_danColliders;
        internal List<float> m_danColliderRadius;
        internal List<float> m_danColliderLength;
        internal List<DynamicBone> m_tamaBones;
        internal bool m_danPointsFound = false;
        internal bool m_bpDanPointsFound = false;
        internal bool m_bpColliderBonesFound = false;
        internal bool m_bpTamaFound = false;
        internal int tamaSelfColliders;

#if !STUDIO
        internal Transform m_referenceTarget;
        internal DynamicBoneCollider m_indexCollider;
        internal DynamicBoneCollider m_middleCollider;
        internal DynamicBoneCollider m_ringCollider;       
        internal bool m_danPenetration = false;
#endif

#if HS2 || AI
        internal const float DefaultDanLength = 1.898f;
        internal const float DefaultColliderVertical = -0.03f;
        internal const float DefaultColliderLength = 0.15f;
        internal const float DefaultColliderRadius = 0.18f;
#elif KK
        internal const float DefaultDanLength = 0.1898f;
        internal const float DefaultColliderVertical = 0.0f;
        internal const float DefaultColliderLength = 0.008f;
        internal const float DefaultColliderRadius = 0.024f;
#endif

        internal float m_baseDanLength = DefaultDanLength;
        internal float lastDanDistance;

        public DanAgent(ChaControl character, DanOptions options)
        {
            Initialize(character, options);
        }

        internal void Initialize(ChaControl character, DanOptions options)
        {
            ClearDanAgent();

            if (character == null)
                return;

            m_danOptions = options;
            m_danCharacter = character;

            InitializeDan();
            InitializeTama();

#if !STUDIO
            UpdateFingerColliders(m_danOptions.fingerRadius, m_danOptions.fingerLength);
#endif
            UnityEngine.Debug.Log($"BP Dan Found { m_bpDanPointsFound}; BP Tama Found {m_bpTamaFound}");
        }

        internal void InitializeDan()
        {
            List<Transform> danTransforms = new List<Transform>();
            foreach (var boneName in BoneNames.DanBones)
            {
                Transform danBone = Tools.GetTransformOfChaControl(m_danCharacter, boneName);
                if (danBone != null)
                    danTransforms.Add(danBone);
            }

            Transform tamaTop = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.TamaTop);

            if (tamaTop == null || danTransforms.Count < 2)
                return;

            if (danTransforms.Count > 2)
                m_bpDanPointsFound = true;

            if (danTransforms.Count == 9)
                m_bpColliderBonesFound = true;

            Transform danEnd = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.BPDanEnd);

            m_danPoints = new DanPoints(danTransforms, tamaTop, danEnd);
            m_danPointsFound = true;
            m_baseDanLength = Vector3.Distance(danTransforms[0].position, danTransforms[1].position) * (danTransforms.Count - 1);
            lastDanDistance = m_baseDanLength;

            if (m_bpColliderBonesFound)
            {
                m_danColliders = Tools.GetCollidersOfChaControl(m_danCharacter, BoneNames.BPDanBone);

                m_danColliderRadius = new List<float>();
                m_danColliderLength = new List<float>();

                foreach (var collider in m_danColliders)
                {
                    m_danColliderRadius.Add(collider.m_Radius);
                    m_danColliderLength.Add(collider.m_Height);
                }

                UpdateDanColliders(m_danOptions.danRadiusScale, m_danOptions.danLengthScale);
            }
            else
            {
                float baseSectionHalfLength = m_baseDanLength / (2 * (m_danPoints.danPoints.Count - 1));

                for (int danPoint = 1; danPoint < m_danPoints.danPoints.Count; danPoint++)
                {
                    m_danColliders.Add(InitializeCollider(m_danPoints.danPoints[danPoint - 1].transform, DefaultColliderRadius * m_danPoints.danPoints[danPoint].defaultLossyScale.x, ((baseSectionHalfLength + DefaultColliderLength) * 2),
                        new Vector3(0, DefaultColliderVertical, baseSectionHalfLength), DynamicBoneCollider.Direction.Z));
                }
            }
        }

        internal void InitializeTama()
        {
            ClearTama();

            foreach (var tamaBoneName in BoneNames.TamaBones)
            {
                var tamaBone = Tools.GetDynamicBoneOfChaControl(m_danCharacter, tamaBoneName);

                if (tamaBone == null)
                    return;

                m_tamaBones.Add(tamaBone);
            }

            m_bpTamaFound = true;

            foreach (var tamaBone in m_tamaBones)
            {
                if (m_bpDanPointsFound && m_danColliders.Count >= 2)
                {
                    tamaBone.m_Colliders.Add(m_danColliders[0]);
                    tamaBone.m_Colliders.Add(m_danColliders[1]);
                }
                else if (m_danColliders.Count >= 1)
                {
                    tamaBone.m_Colliders.Add(m_danColliders[0]);
                }
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

        internal DynamicBoneCollider InitializeCollider(Transform parent, float radius, float length, Vector3 centerOffset,
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

        internal void UpdateDanColliders(float radiusScale, float lengthScale)
        {
            if (!m_danPointsFound || m_danColliders.Count < 1 || m_danColliders.Count >= m_danPoints.danPoints.Count)
                return;

            m_danOptions.danRadiusScale = radiusScale;
            m_danOptions.danLengthScale = lengthScale;

            for (var collider = 0; collider < m_danColliders.Count; collider++)
            {
                m_danColliders[collider].m_Radius = m_danColliderRadius[collider] * m_danOptions.danRadiusScale;
                m_danColliders[collider].m_Height = m_danColliderLength[collider] * m_danOptions.danLengthScale;
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

#if STUDIO
        internal void SetDanTarget(Vector3 enterTarget, Vector3 endTarget, CollisionAgent targetAgent, bool isKokan, bool isOral)
        {
            if (!m_danPointsFound)
                return;

            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTargetVector = Vector3.Normalize(enterTarget - danStartPosition);

            float danDistanceToTarget = Vector3.Distance(danStartPosition, enterTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            m_danPoints.SquishDanGirth(girthScaleFactor);
            adjustedDanLength = GetMaxDanLength(adjustedDanLength, enterTarget, endTarget, danDistanceToTarget);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(enterTarget, endTarget, adjustedDanLength, danDistanceToTarget);

            m_danPoints.AimDanPoints(adjustedDanPoints, true);
            AdjustPullBones(targetAgent, danTargetVector, danDistanceToTarget, isKokan, isOral, true);

            m_danPoints.GetDanLossyScale();
        }
#endif
    internal float GetSquishedDanLength(float danDistanceToTarget)
        {
            float danLength = m_baseDanLength;
            float innerSquishDistance = m_baseDanLength - m_baseDanLength * m_danOptions.squishThreshold - danDistanceToTarget;

            if (innerSquishDistance > 0)
                danLength = m_baseDanLength - (innerSquishDistance * m_danOptions.danLengthSquish);

            return danLength;
        }

        internal float GetSquishedDanGirth(float danDistanceToTarget)
        {
            float girthScaleFactor = 1;
            float innerSquishDistance = m_baseDanLength - m_baseDanLength * m_danOptions.squishThreshold - danDistanceToTarget;

            if (innerSquishDistance > 0)
                girthScaleFactor = 1 + innerSquishDistance / m_baseDanLength * m_danOptions.danGirthSquish;

            return girthScaleFactor;
        }

        internal float GetMaxDanLength(float danLength, Vector3 lookTarget, Vector3 limitPoint, float danDistanceToTarget)
        {
            float maxDanLength = danLength;
            float targetDistanceToLimit = Vector3.Distance(lookTarget, limitPoint);

            if (maxDanLength > danDistanceToTarget + targetDistanceToLimit)
                maxDanLength = danDistanceToTarget + targetDistanceToLimit;

            return maxDanLength;
        }

        internal List<Vector3> AdjustDanPointsToTargets(Vector3 danLookTarget, Vector3 danEndTarget, float danLength, float danDistanceToTarget)
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

        internal void AddDanColliders(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null && 
                        dynamicBone.name.Contains(BoneNames.BPBone) && 
                        !dynamicBone.m_Colliders.Contains(danCollider) && 
                        target == dynamicBone.GetComponentInParent<ChaControl>())
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
                    if (danCollider != null && 
                        dynamicBone.name.Contains(BoneNames.BPBone) &&
                        target == dynamicBone.GetComponentInParent<ChaControl>())
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
                    !BoneNames.BodyColliders.Contains(dynamicBoneCollider.name) ||
                    target != dynamicBoneCollider.GetComponentInParent<ChaControl>())
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

        internal void AdjustPullBones(CollisionAgent targetAgent, Vector3 danDirection, float danDistanceToTarget, bool isVaginal, bool isOral, bool twoDans)
        {
            return;

            float distanceChange = lastDanDistance - danDistanceToTarget;

            if (isVaginal)
                targetAgent.PullKokanBones(distanceChange, danDirection);
            else if (!twoDans)
                targetAgent.ReturnKokanBones();

            if (isOral)
                targetAgent.PullOralBone(distanceChange, danDirection);
            else if (!twoDans)
                targetAgent.ReturnOralBones();

            if (isVaginal || isOral)
                lastDanDistance = danDistanceToTarget;
            else
                lastDanDistance = m_baseDanLength;
        }

#if !STUDIO
        internal void UpdateDanOptions(float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth, bool useFingerColliders, bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft)
        {
            m_danOptions.danLengthSquish = danLengthSquish;
            m_danOptions.danGirthSquish = danGirthSquish;
            m_danOptions.squishThreshold = squishThreshold;
            m_danOptions.squishOralGirth = squishOralGirth;
            m_danOptions.useFingerColliders = useFingerColliders;
            m_danOptions.simplifyPenetration = simplifyPenetration;
            m_danOptions.simplifyOral = simplifyOral;
            m_danOptions.rotateTamaWithShaft = rotateTamaWithShaft;
        }

        internal void UpdateFingerColliders(float fingerRadius, float fingerLength)
        {
            if (!m_danPointsFound)
                return;

            Transform index = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.IndexFinger);
            Transform middle = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.MiddleFinger);
            Transform ring = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.RingFinger);

            m_indexCollider = InitializeCollider(index, fingerRadius, fingerLength, Vector3.zero);
            m_middleCollider = InitializeCollider(middle, fingerRadius, fingerLength, Vector3.zero);
            m_ringCollider = InitializeCollider(ring, fingerRadius, fingerLength, Vector3.zero);

            m_danOptions.fingerRadius = fingerRadius;
            m_danOptions.fingerLength = fingerLength;
        }

        internal void SetDanTarget(CollisionAgent targetAgent, bool twoDans)
        {
            if (!m_danPointsFound || !targetAgent.m_collisionPointsFound)
                return;

            if (m_referenceTarget == null)
                AdjustDanToTargetNull(targetAgent, twoDans);
            else if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                AdjustDanToTargetKokan(targetAgent, twoDans);
            else if (m_referenceTarget.name == LookTargets.AnaTarget)
                AdjustDanToTargetAna(targetAgent, twoDans);
            else if (m_referenceTarget.name == LookTargets.HeadTarget)
                AdjustDanToTargetHead(targetAgent, twoDans);
            else
                AdjustDanToTargetNull(targetAgent, twoDans);
        }

        internal void AdjustDanToTargetNull(CollisionAgent targetAgent, bool twoDans)
        {
            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_danPoints.GetDanEndPosition();
            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;
            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, m_baseDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft);

            AdjustPullBones(targetAgent, danTargetVector, danDistanceToTarget, false, false, twoDans);
        }

        internal void AdjustDanToTargetKokan(CollisionAgent targetAgent, bool twoDans)
        {
            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_referenceTarget.position;

            danTarget += targetAgent.m_collisionOptions.kokanOffset * m_referenceTarget.up;

            if (targetAgent.m_collisionOptions.kokan_adjust && targetAgent.adjustFAnimation)
                danTarget += m_referenceTarget.forward * targetAgent.m_collisionOptions.kokan_adjust_position_z;

            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;

            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            m_danPoints.SquishDanGirth(girthScaleFactor);

            Vector3 innerLimit = targetAgent.m_innerTarget.position + targetAgent.m_collisionOptions.innerKokanOffset * m_referenceTarget.up;

            danEndTarget = ConstrainDan(danStartPosition, danTargetVector, danEndTarget, innerLimit, adjustedDanLength, danDistanceToTarget, targetAgent);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft);

            AdjustPullBones(targetAgent, danTargetVector, danDistanceToTarget, true, false, twoDans);
        }

        internal void AdjustDanToTargetHead(CollisionAgent targetAgent, bool twoDans)
        {
            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_referenceTarget.position;

            danTarget += targetAgent.m_collisionOptions.mouthOffset * m_referenceTarget.up;

            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;

            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            if (m_danOptions.squishOralGirth)
                m_danPoints.SquishDanGirth(girthScaleFactor);

            Vector3 innerLimit = targetAgent.m_innerHeadTarget.position + targetAgent.m_collisionOptions.innerMouthOffset * m_referenceTarget.up;

            adjustedDanLength = GetMaxDanLength(adjustedDanLength, danTarget, innerLimit, danDistanceToTarget);
            danEndTarget = ConstrainDan(danStartPosition, danTargetVector, danEndTarget, innerLimit, adjustedDanLength, danDistanceToTarget, targetAgent);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft);

            AdjustPullBones(targetAgent, danTargetVector, danDistanceToTarget, false, true, twoDans);
        }

        internal void AdjustDanToTargetAna(CollisionAgent targetAgent, bool twoDans)
        {
            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_referenceTarget.position;
            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;

            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);
            float adjustedDanLength = GetSquishedDanLength(danDistanceToTarget);
            float girthScaleFactor = GetSquishedDanGirth(danDistanceToTarget);

            if (m_danOptions.squishOralGirth)
                m_danPoints.SquishDanGirth(girthScaleFactor);

            Vector3 innerLimit = targetAgent.m_innerTarget.position + targetAgent.m_collisionOptions.innerKokanOffset * m_referenceTarget.up;

            danEndTarget = ConstrainDan(danStartPosition, danTargetVector, danEndTarget, innerLimit, adjustedDanLength, danDistanceToTarget, targetAgent);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft);

            AdjustPullBones(targetAgent, danTargetVector, danDistanceToTarget, false, false, twoDans);
        }

        internal Vector3 ConstrainDan(Vector3 danStart, Vector3 danTargetVector, Vector3 danEndTarget, Vector3 innerLimit, float danLength, float danDistanceToTarget, CollisionAgent targetAgent)
        {
            if (m_danPenetration)
            {
                if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                {
                    if (m_danOptions.simplifyPenetration && m_bpDanPointsFound)
                        danEndTarget = innerLimit;
                    else
                        danEndTarget = ConstrainDanToBody(targetAgent, danStart, danTargetVector, danLength, m_referenceTarget.name == LookTargets.AnaTarget);
                }
                else if (m_referenceTarget.name == LookTargets.HeadTarget)
                {
                    if (m_danOptions.simplifyOral && m_bpDanPointsFound)
                        danEndTarget = innerLimit;
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

        internal Vector3 ConstrainDanToBody(CollisionAgent target, Vector3 dan101_pos, Vector3 danVector, float squishedDanLength, bool anaTarget)
        {
            Vector3 adjustedDan109 = dan101_pos + danVector * squishedDanLength;
            bool constrainPointFound = false;

            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, anaTarget);
            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, anaTarget);
            return adjustedDan109;
        }

        internal Vector3 ConstrainDanToCollisionPoints(Vector3 danStart, Vector3 danEnd, float targetLength, ref bool constrainPointFound, List<CollisionPoint> collisionPoints, float clippingDepth, bool frontSide, bool anaTarget)
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

        internal Vector3 ConstrainDanToHead(Vector3 dan101_pos, Vector3 danVector, float squishedDanLength)
        {
            return (dan101_pos + danVector * squishedDanLength);
        }

        internal Vector3 ConstrainDanToPull(Vector3 dan101_pos, Vector3 danVector, float squishedDanLength)
        {
            return (dan101_pos + danVector * squishedDanLength);
        }

        internal void ResetDanAdjustment()
        {
            m_danPoints.ResetDanPoints();
    //        ScaleDanColliders(m_baseDanLength);
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
               (currentMotion.Contains("Insert") || (!currentMotion.Contains("Idle") && !currentMotion.Contains("Pull") && !currentMotion.Contains("OUT"))))
                m_referenceTarget = firstTarget.m_bpKokanTarget;
        }

        internal void AddDanColliders(CollisionAgent target)
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

        internal void RemoveDanColliders(CollisionAgent target)
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
	
        internal void AddFingerColliders(CollisionAgent target)
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

        internal void RemoveFingerColliders(CollisionAgent target)
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

        internal void ClearTarget()
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

        internal void ClearTama()
        {
            m_bpTamaFound = false;
            m_tamaBones = new List<DynamicBone>();
        }

        internal void ClearDanAgent()
        {
            m_danPointsFound = false;
            m_bpDanPointsFound = false;
            m_bpColliderBonesFound = false;

            if (m_danPoints != null)
                m_danPoints.ResetDanPoints();
            m_danPoints = null;

            m_danColliders = new List<DynamicBoneCollider>();

            ClearTama();

#if !STUDIO

            ClearTarget();

            UnityEngine.Object.Destroy(m_indexCollider);
            UnityEngine.Object.Destroy(m_middleCollider);
            UnityEngine.Object.Destroy(m_ringCollider);
#endif
        }
    }
}