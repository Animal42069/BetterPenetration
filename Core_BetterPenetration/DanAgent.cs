using System;
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
        internal List<DynamicBoneCollider> m_virtualDanColliders;
#if HS2 || AI
        internal List<DynamicBoneCollider> m_midsectionColliders;
#endif
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
        internal List<DynamicBoneCollider> m_fingerColliders = new List<DynamicBoneCollider>();      
        internal bool m_danPenetration = false;
#endif

#if HS2 || AI
        internal const float DefaultDanLength = 1.898f;
        internal const float DefaultColliderVertical = -0.03f;
        internal const float DefaultColliderLength = 0.15f;
        internal const float DefaultColliderRadius = 0.18f;
#elif KK || KKS
        internal const float DefaultDanLength = 0.1898f;
        internal const float DefaultColliderVertical = 0.0f;
        internal const float DefaultColliderLength = 0.008f;
        internal const float DefaultColliderRadius = 0.024f;
#endif

        internal float m_baseDanLength = DefaultDanLength;
        internal float lastDanDistance;
        internal Vector3 lastDanEndVector = Vector3.zero;
        internal Vector3 lastDanEnd = Vector3.zero;

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
#if AI || HS2
            InitializeFingerColliders(0.055f, 0.18f);
            InitializeMidsectionColliders();
#else
            InitializeFingerColliders(0.0055f, 0.018f);
#endif
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

            List<Transform> virtualDanTransforms = new List<Transform>();
            foreach (var boneName in BoneNames.VirtualDanBones)
            {
                Transform virtualDanBone = Tools.GetTransformOfChaControl(m_danCharacter, boneName);
                if (virtualDanBone != null)
                    virtualDanTransforms.Add(virtualDanBone);
            }

            Transform tamaTop = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.TamaTop);

            if (tamaTop == null || danTransforms.Count < 2)
                return;

            if (danTransforms.Count > 2)
                m_bpDanPointsFound = true;

            if (danTransforms.Count == 9)
                m_bpColliderBonesFound = true;

            Transform danEnd = Tools.GetTransformOfChaControl(m_danCharacter, BoneNames.BPDanEnd);

            m_danPoints = new DanPoints(danTransforms, tamaTop, danEnd, virtualDanTransforms);
            m_danPointsFound = true;
            m_baseDanLength = Vector3.Distance(danTransforms[0].position, danTransforms[1].position) * (danTransforms.Count - 1);
            lastDanDistance = m_baseDanLength;
            lastDanEndVector = Vector3.zero;

            if (m_bpColliderBonesFound)
            {
                m_danColliders = Tools.GetCollidersOfChaControl(m_danCharacter, BoneNames.BPDanBone);
                m_virtualDanColliders = Tools.GetCollidersOfChaControl(m_danCharacter, BoneNames.virtualBPDanBone);

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
                    m_danColliders.Add(Tools.InitializeCollider(m_danPoints.danPoints[danPoint - 1].transform, DefaultColliderRadius * m_danPoints.danPoints[danPoint].defaultLossyScale.x, ((baseSectionHalfLength + DefaultColliderLength) * 2),
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

        internal void ResetParticles()
        {
            foreach (var tamaBone in m_tamaBones)
            {
                if (tamaBone == null)
                    continue;

                tamaBone.ResetParticlesPosition();
            }
        }

        internal void UpdateDanColliders(float radiusScale, float lengthScale)
        {
            if (!m_danPointsFound || m_danPoints == null || m_danColliders == null || m_danOptions  == null || m_danColliderRadius == null || m_danColliderLength == null || m_danColliders.Count < 1 || m_danColliders.Count >= m_danPoints.danPoints.Count)
                return;

            m_danOptions.danRadiusScale = radiusScale;
            m_danOptions.danLengthScale = lengthScale;

            for (var collider = 0; collider < m_danColliders.Count; collider++)
            {
                m_danColliders[collider].m_Radius = m_danColliderRadius[collider] * m_danOptions.danRadiusScale;
                m_danColliders[collider].m_Height = m_danColliderLength[collider] * m_danOptions.danLengthScale;
            }

            if (m_virtualDanColliders == null || m_virtualDanColliders.Count < 1 || m_virtualDanColliders.Count > m_danColliders.Count)
                return;

            for (var collider = 0; collider < m_virtualDanColliders.Count; collider++)
            {
                m_virtualDanColliders[collider].m_Radius = m_danColliders[collider].m_Radius;
                m_virtualDanColliders[collider].m_Height = m_danColliders[collider].m_Height;
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
        internal void SetDanTarget(Vector3 enterTarget, Vector3 endTarget, CollisionAgent targetAgent, bool isKokan, bool isOral, bool isAnal)
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

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(enterTarget, endTarget, adjustedDanLength, danDistanceToTarget, out List<Vector3> virtualDanPoints, isKokan);

            m_danPoints.AimDanPoints(adjustedDanPoints, true, virtualDanPoints);

            Vector3 insideVector = Vector3.Normalize(endTarget - enterTarget);

            AdjustPullBones(targetAgent, danTargetVector, insideVector, danDistanceToTarget, isKokan, isOral, isAnal, true);

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

        internal List<Vector3> AdjustDanPointsToTargets(Vector3 danLookTarget, Vector3 danEndTarget, float danLength, float danDistanceToTarget, out List<Vector3> virtualDanPoints, bool isKokan = false)
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
            float outsideDistance;
            for (int point = 1; point < adjustedDanPoints.Count; point++)
            {
                outsideDistance = danDistanceToTarget - danSegmentLength * (point - 1);

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

            virtualDanPoints = new List<Vector3>();
            if (!isKokan || m_danPoints.virtualDanPoints == null || m_danPoints.virtualDanPoints.Count == 0)
                return adjustedDanPoints;

            foreach (var point in m_danPoints.virtualDanPoints)
                virtualDanPoints.Add(point.position);

            float fullDanSegmentLength = m_baseDanLength / (adjustedDanPoints.Count - 1);
            outsideDistance = danDistanceToTarget - fullDanSegmentLength;
            if (singleVector || outsideDistance < 0.001)
            {
                virtualDanPoints[0] = adjustedDanPoints[0] + insideVector * fullDanSegmentLength;
            }
            else if (outsideDistance < (fullDanSegmentLength - 0.001))
            {
                double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                MathHelpers.SolveSSATriangle(fullDanSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                virtualDanPoints[0] = danLookTarget + insideVector * (float)distanceAlongInside;
            }
            else
            {
                virtualDanPoints[0] = adjustedDanPoints[0] + outsideVector * fullDanSegmentLength;
            }

            for (int point = 1; point < virtualDanPoints.Count; point++)
            {
                outsideDistance = danDistanceToTarget - fullDanSegmentLength * (2 * point - 1);

                if (singleVector || outsideDistance < 0.001)
                {
                    virtualDanPoints[point] = virtualDanPoints[point - 1] + insideVector * 2 * fullDanSegmentLength;
                }
                else if (outsideDistance < (2 * fullDanSegmentLength - 0.001))
                {
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(2 * fullDanSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    virtualDanPoints[point] = danLookTarget + insideVector * (float)distanceAlongInside;
                }
                else
                {
                    virtualDanPoints[point] = virtualDanPoints[point - 1] + outsideVector * 2 * fullDanSegmentLength;
                }
            }

            return adjustedDanPoints;
        }

        internal void AddDanCollidersToTargetKokan(ChaControl target, bool enableBellyBulge)
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

            if (!enableBellyBulge)
                return;

            AddDanCollidersToTargeBelly(target);
        }

        internal void AddDanCollidersToTargeBelly(ChaControl target)
        {
            foreach (var virutalDanCollider in m_virtualDanColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (virutalDanCollider != null &&
                        dynamicBone.name.Contains(BoneNames.BellyBone) &&
                        !dynamicBone.m_Colliders.Contains(virutalDanCollider) &&
                        target == dynamicBone.GetComponentInParent<ChaControl>())
                        dynamicBone.m_Colliders.Add(virutalDanCollider);
                }
            }
        }

        internal void AddDanCollidersToTargetAna(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null &&
                        (dynamicBone.name.Contains(BoneNames.AnaTarget)) &&
                        !dynamicBone.m_Colliders.Contains(danCollider) &&
                        target == dynamicBone.GetComponentInParent<ChaControl>())
                        dynamicBone.m_Colliders.Add(danCollider);
                }
            }
        }

        internal void AddDanCollidersToDB2(ChaControl target)
        {
            if (m_danColliders == null || m_danColliders.Count == 0)
                return;

            foreach (DynamicBone_Ver02 dynamicBone in target.GetComponentsInChildren<DynamicBone_Ver02>(true))
            {
                if (dynamicBone == null || dynamicBone.Colliders == null ||
                    target != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                foreach (var collider in m_danColliders)
                {
                    if (collider == null || dynamicBone.Colliders.Contains(collider))
                        continue;

                    dynamicBone.Colliders.Add(collider);
                }
            }
        }

        internal void RemoveDanCollidersFromDB2(ChaControl target)
        {
            if (m_danColliders == null || m_danColliders.Count == 0)
                return;

            foreach (DynamicBone_Ver02 dynamicBone in target.GetComponentsInChildren<DynamicBone_Ver02>(true))
            {
                if (dynamicBone == null || dynamicBone.Colliders == null || dynamicBone.Colliders.Count == 0)
                    continue;

                foreach (var collider in m_danColliders)
                {
                    if (collider == null || !dynamicBone.Colliders.Contains(collider))
                        continue;

                    dynamicBone.Colliders.Remove(collider);
                }
            }
        }

        internal void RemoveDanCollidersFromTarget(ChaControl target, bool wasKokan, bool wasAna)
        {
            if (wasKokan)
            {
                RemoveDanCollidersFromTargetKokan(target);
                RemoveDanCollidersFromTargetBelly(target);
            }

            if (wasAna)
            {
                RemoveDanCollidersFromTargetAna(target);
            }
        }

        internal void RemoveDanCollidersFromTargetKokan(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null &&
                        dynamicBone.name.Contains(BoneNames.BPBone) &&
                        target == dynamicBone.GetComponentInParent<ChaControl>())
                    {
                        dynamicBone.m_Colliders.Remove(danCollider);
                    }
                }
            }
        }

        internal void RemoveDanCollidersFromTargetBelly(ChaControl target)
        {
            foreach (var virtualDanCollider in m_virtualDanColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (virtualDanCollider != null &&
                        dynamicBone.name.Contains(BoneNames.BellyBone) &&
                        target == dynamicBone.GetComponentInParent<ChaControl>())
                        dynamicBone.m_Colliders.Remove(virtualDanCollider);
                }
            }
        }

        internal void RemoveDanCollidersFromTargetAna(ChaControl target)
        {
            foreach (var danCollider in m_danColliders)
            {
                foreach (DynamicBone dynamicBone in target.GetComponentsInChildren<DynamicBone>())
                {
                    if (danCollider != null &&
                        dynamicBone.name.Contains(BoneNames.AnaTarget) &&
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
#if HS2 || AI
        internal void InitializeMidsectionColliders()
        {
            m_midsectionColliders = Tools.GetCollidersOfChaControl(m_danCharacter, BoneNames.MidSectionColliders);

            if (m_midsectionColliders == null || m_midsectionColliders.Count == 0)
                return;
        }

        internal void AddMidsectionColliders(ChaControl target)
        {
            if (m_midsectionColliders == null || m_midsectionColliders.Count == 0)
                return;

            foreach (DynamicBone_Ver02 dynamicBone in target.GetComponentsInChildren<DynamicBone_Ver02>(true))
            {
                if (dynamicBone == null || dynamicBone.Colliders == null ||
                    target != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                foreach (var collider in m_midsectionColliders)
                {
                    if (collider == null)
                        continue;

                    dynamicBone.Colliders.Add(collider);
                }
            }
        }

        internal void RemoveMidsectionColliders(ChaControl target)
        {
            if (m_midsectionColliders == null || m_midsectionColliders.Count == 0)
                return;

            foreach (DynamicBone_Ver02 dynamicBone in target.GetComponentsInChildren<DynamicBone_Ver02>(true))
            {
                if (dynamicBone == null || dynamicBone.Colliders == null || dynamicBone.Colliders.Count == 0)
                    continue;

                foreach (var collider in m_midsectionColliders)
                {
                    if (collider == null || !dynamicBone.Colliders.Contains(collider))
                        continue;

                    dynamicBone.Colliders.Remove(collider);
                }
            }
        }
#endif
        internal void AdjustPullBones(CollisionAgent targetAgent, Vector3 danDirection, Vector3 insideDirection, float danDistanceToTarget, bool isVaginal, bool isOral, bool isAnal, bool twoDans)
        {
            if (danDistanceToTarget >= m_baseDanLength)
            {
                isVaginal = false;
                isOral = false;
            }

            float distanceChange = lastDanDistance - danDistanceToTarget;

            if (isVaginal)
                targetAgent.PullKokanBones(distanceChange, danDirection, insideDirection);
            else if (!twoDans)
                targetAgent.ReturnKokanBones();

            if (isOral)
                targetAgent.PullOralBone(distanceChange, danDirection);
            else if (!twoDans)
                targetAgent.ReturnOralBones();

            if (isAnal)
                targetAgent.PullAnaBone(distanceChange, danDirection, insideDirection);
            else if (!twoDans)
                targetAgent.ReturnAnaBones();

            if (isVaginal || isOral || isAnal)
                lastDanDistance = danDistanceToTarget;
            else
                lastDanDistance = m_baseDanLength;
        }

#if !STUDIO
        internal void UpdateDanOptions(float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth, 
            bool simplifyVaginal, bool simplifyOral, bool rotateTamaWithShaft, bool limitCorrection, float maxCorrection)
        {
            m_danOptions.danLengthSquish = danLengthSquish;
            m_danOptions.danGirthSquish = danGirthSquish;
            m_danOptions.squishThreshold = squishThreshold;
            m_danOptions.squishOralGirth = squishOralGirth;
            m_danOptions.simplifyVaginal = simplifyVaginal;
            m_danOptions.simplifyOral = simplifyOral;
            m_danOptions.rotateTamaWithShaft = rotateTamaWithShaft;
            m_danOptions.limitCorrection = limitCorrection;
            m_danOptions.maxCorrection = maxCorrection;
        }

        internal void InitializeFingerColliders(float fingerRadius, float fingerLength)
        {
            m_fingerColliders = new List<DynamicBoneCollider>();
            foreach (var bone in BoneNames.FingerColliders)
            {
                var fingerTransform = Tools.GetTransformOfChaControl(m_danCharacter, bone);
                if (fingerTransform == null)
                    continue;

                var fingerCollider = Tools.InitializeCollider(fingerTransform, fingerRadius * (fingerTransform.lossyScale.y + fingerTransform.lossyScale.z) / 2, fingerLength * fingerTransform.lossyScale.x, Vector3.zero);

                m_fingerColliders.Add(fingerCollider);
            }
        }

        internal void SetDanTarget(CollisionAgent targetAgent, bool twoDans)
        {
            if (!m_danPointsFound || !targetAgent.m_collisionPointsFound)
                return;

            if (m_referenceTarget == null)
                AdjustDanToTargetNull(targetAgent);
            else if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                AdjustDanToTargetKokan(targetAgent, twoDans);
            else if (m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPAnaTarget)
                AdjustDanToTargetAna(targetAgent, twoDans);
            else if (m_referenceTarget.name == LookTargets.HeadTarget)
                AdjustDanToTargetHead(targetAgent, twoDans);
            else
                AdjustDanToTargetNull(targetAgent);
        }

        internal void AdjustDanToTargetNull(CollisionAgent targetAgent)
        {
            Vector3 danStartPosition = m_danPoints.GetDanStartPosition();
            Vector3 danTarget = m_danPoints.GetDanEndPosition();
            Vector3 danTargetVector = Vector3.Normalize(danTarget - danStartPosition);
            Vector3 danEndTarget = danStartPosition + danTargetVector * m_baseDanLength;
            float danDistanceToTarget = Vector3.Distance(danStartPosition, danTarget);

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, m_baseDanLength, danDistanceToTarget, out List<Vector3> _, false);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft, null);

            Vector3 insideVector = Vector3.Normalize(danEndTarget - danTarget);

            AdjustPullBones(targetAgent, danTargetVector, insideVector, danDistanceToTarget, false, false, false, false);
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
            
            if (m_danOptions.limitCorrection && lastDanEndVector != Vector3.zero)
            {
                Vector3 offsetPostion = MathHelpers.CastToSegment(lastDanEnd, danEndTarget, lastDanEndVector);

                float maxCorrection = m_danOptions.maxCorrection * Time.deltaTime;
                if (Vector3.Distance(offsetPostion, lastDanEnd) > maxCorrection)
                    danEndTarget = lastDanEnd + Vector3.Normalize(offsetPostion - lastDanEnd) * maxCorrection;
            }

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget, out List<Vector3> virtualDanPoints, true);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft, virtualDanPoints);

            if (m_danPoints.danEnd != null)
            {
                lastDanEnd = m_danPoints.danEnd.position;
                lastDanEndVector = m_danPoints.danEnd.forward;
            }

            Vector3 insideVector = Vector3.Normalize(danEndTarget - danTarget);

            AdjustPullBones(targetAgent, danTargetVector, insideVector, danDistanceToTarget, true, false, false, twoDans);
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

            if (m_danOptions.limitCorrection && lastDanEndVector != Vector3.zero)
            {
                Vector3 offsetPostion = MathHelpers.CastToSegment(lastDanEnd, danEndTarget, lastDanEndVector);

                float maxCorrection = m_danOptions.maxCorrection * Time.deltaTime;
                if (Vector3.Distance(offsetPostion, lastDanEnd) > maxCorrection)
                    danEndTarget = lastDanEnd + Vector3.Normalize(offsetPostion - lastDanEnd) * maxCorrection;
            }

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget, out List<Vector3> _, false);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft, null);

            Vector3 insideVector = Vector3.Normalize(danEndTarget - danTarget);

            AdjustPullBones(targetAgent, danTargetVector, insideVector, danDistanceToTarget, false, true, false, twoDans);
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

            if (m_danOptions.limitCorrection && lastDanEndVector != Vector3.zero)
            {
                Vector3 offsetPostion = MathHelpers.CastToSegment(lastDanEnd, danEndTarget, lastDanEndVector);

                float maxCorrection = m_danOptions.maxCorrection * Time.deltaTime;
                if (Vector3.Distance(offsetPostion, lastDanEnd) > maxCorrection)
                    danEndTarget = lastDanEnd + Vector3.Normalize(offsetPostion - lastDanEnd) * maxCorrection;
            }

            List<Vector3> adjustedDanPoints = AdjustDanPointsToTargets(danTarget, danEndTarget, adjustedDanLength, danDistanceToTarget, out List<Vector3> _, false);
            m_danPoints.AimDanPoints(adjustedDanPoints, m_danOptions.rotateTamaWithShaft, null);

            Vector3 insideVector = Vector3.Normalize(danEndTarget - danTarget);

            AdjustPullBones(targetAgent, danTargetVector, insideVector, danDistanceToTarget, false, false, true, twoDans);
        }

        internal Vector3 ConstrainDan(Vector3 danStart, Vector3 danTargetVector, Vector3 danEndTarget, Vector3 innerLimit, float danLength, float danDistanceToTarget, CollisionAgent targetAgent)
        {
            if (m_danPenetration)
            {
                if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                {
                    if (m_danOptions.simplifyVaginal && m_bpDanPointsFound)
                        danEndTarget = innerLimit;
                    else
                        danEndTarget = ConstrainDanToBody(targetAgent, danStart, danTargetVector, danLength);
                }
                else if (m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPAnaTarget)
                {
                    if (m_danOptions.simplifyAnal && m_bpDanPointsFound)
                        danEndTarget = innerLimit;
                    else
                        danEndTarget = ConstrainDanToBody(targetAgent, danStart, danTargetVector, danLength);
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

        internal Vector3 ConstrainDanToBody(CollisionAgent target, Vector3 dan101_pos, Vector3 danVector, float squishedDanLength)
        {
            Vector3 adjustedDan109 = dan101_pos + danVector * squishedDanLength;
            bool constrainPointFound = false;

            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, target.m_collisionOptions.enableBellyBulge);
            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, squishedDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, false);
            return adjustedDan109;
        }

        internal Vector3 ConstrainDanToCollisionPoints(Vector3 danStart, Vector3 danEnd, float targetLength, ref bool constrainPointFound, List<CollisionPoint> collisionPoints, float clippingDepth, bool frontSide, bool bellyBulge)
        {
            Vector3 adjustedDanEnd = danEnd;
            if (constrainPointFound)
                return adjustedDanEnd;

            var constainPastNearSide = true;
            var constainPastFarSide = false;

            List<Vector3> collisionPointPositions = new List<Vector3>();

            foreach (var collisionPoint in collisionPoints)
            {
                if (collisionPoint == null || collisionPoint.transform == null)
                    continue;

                var offset = collisionPoint.info.offset;

                if (!frontSide || !bellyBulge)
                    offset += clippingDepth;

                if (frontSide == collisionPoint.info.inward)
                    collisionPointPositions.Add(collisionPoint.transform.position + offset * collisionPoint.transform.forward);
                else
                    collisionPointPositions.Add(collisionPoint.transform.position - offset * collisionPoint.transform.forward);
            }
  
            for (int index = 1; index < collisionPoints.Count; index++)
            {
                if (constrainPointFound)
                    break;

                CollisionPoint nearPoint = collisionPoints[index - 1];
                CollisionPoint farPoint = collisionPoints[index];

                if (nearPoint.transform == null || farPoint.transform == null)
                    continue;

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
            RemoveDanColliders(firstTarget);
#if HS2 || AI
            RemoveMidsectionColliders(firstTarget.m_collisionCharacter);
            RemoveDanCollidersFromDB2(firstTarget.m_collisionCharacter);
#endif

            if (secondTarget == null)
                return;

            RemoveDanColliders(secondTarget);
#if HS2 || AI
            RemoveMidsectionColliders(secondTarget.m_collisionCharacter);
            RemoveDanCollidersFromDB2(secondTarget.m_collisionCharacter);
#endif
        }

        internal void SetupNewDanTarget(Transform lookAtTransform, string currentMotion, bool topStick, bool isInScene, CollisionAgent firstTarget, CollisionAgent secondTarget = null, bool twoDans = false)
        {
            ClearDanTarget(firstTarget, secondTarget);

            if (!isInScene || !m_danPointsFound)
                return;

            if (lookAtTransform == null || lookAtTransform.name == LookTargets.KokanTarget)
                AddDanCollidersToKokan(firstTarget, twoDans);
            else if (lookAtTransform.name == LookTargets.AnaTarget)
                AddDanCollidersToAna(firstTarget, twoDans);

            AddTamaColliders(firstTarget.m_collisionCharacter, false);
#if HS2 || AI
            AddMidsectionColliders(firstTarget.m_collisionCharacter);
            AddDanCollidersToDB2(firstTarget.m_collisionCharacter);
#endif

            if (secondTarget != null)
            {
                if (lookAtTransform == null || lookAtTransform.name == LookTargets.AnaTarget)
                    AddDanCollidersToKokan(secondTarget, twoDans);
                else if (lookAtTransform.name == LookTargets.KokanTarget)
                    AddDanCollidersToAna(secondTarget, twoDans);
                AddTamaColliders(secondTarget.m_collisionCharacter, false);
#if HS2 || AI
                AddMidsectionColliders(secondTarget.m_collisionCharacter);
                AddDanCollidersToDB2(secondTarget.m_collisionCharacter);
#endif
            }

            if (currentMotion == string.Empty)
                return;

            m_referenceTarget = lookAtTransform;
#if KK || KKS
            m_danPenetration = (topStick && m_referenceTarget != null) || currentMotion.Contains("IN");
#else
            m_danPenetration = topStick && m_referenceTarget != null;
#endif

            if (m_danPenetration && firstTarget.m_bpKokanTarget != null &&
               (m_referenceTarget == null || m_referenceTarget.name == LookTargets.KokanTarget) &&
               (currentMotion.Contains("Insert") || (!currentMotion.Contains("Idle") && !currentMotion.Contains("Pull") && !currentMotion.Contains("OUT"))))
                m_referenceTarget = firstTarget.m_bpKokanTarget;
            else if (m_danPenetration && firstTarget.m_bpAnaTarget != null &&
               (m_referenceTarget == null || m_referenceTarget.name == LookTargets.AnaTarget) &&
               (currentMotion.Contains("Insert") || (!currentMotion.Contains("Idle") && !currentMotion.Contains("Pull") && !currentMotion.Contains("OUT"))))
                m_referenceTarget = firstTarget.m_bpAnaTarget;
        }

        internal void AddDanCollidersToKokan(CollisionAgent target, bool twoDans)
        {
            foreach (var danCollider in m_danColliders)
            {
                if (danCollider == null)
                    continue;

                foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
                {
                    if (!dynamicBone.m_Colliders.Contains(danCollider))
                        dynamicBone.m_Colliders.Add(danCollider);
                }

                if (!twoDans && target.m_anaDynamicBones != null && !target.m_anaDynamicBones.m_Colliders.Contains(danCollider))
                    target.m_anaDynamicBones.m_Colliders.Add(danCollider);
            }

            foreach (var virtualDanCollider in m_virtualDanColliders)
            {
                if (virtualDanCollider == null)
                    continue;

                if (target.m_bellyDynamicBone != null && !target.m_bellyDynamicBone.m_Colliders.Contains(virtualDanCollider))
                    target.m_bellyDynamicBone.m_Colliders.Add(virtualDanCollider);
            }
        }

        internal void AddDanCollidersToAna(CollisionAgent target, bool twoDans)
        {
            if (target.m_anaDynamicBones == null)
                return;

            foreach (var danCollider in m_danColliders)
            {
                if (danCollider == null)
                    continue;

                if (!target.m_anaDynamicBones.m_Colliders.Contains(danCollider))
                    target.m_anaDynamicBones.m_Colliders.Add(danCollider);

                if (!twoDans)
                {
                    foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
                    {
                        if (!dynamicBone.m_Colliders.Contains(danCollider))
                            dynamicBone.m_Colliders.Add(danCollider);
                    }
                }
            }
        }

        internal void RemoveDanColliders(CollisionAgent target)
        {
            foreach (var danCollider in m_danColliders)
            {
                if (danCollider == null)
                    continue;

                foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
                {
                    if (danCollider != null)
                        dynamicBone.m_Colliders.Remove(danCollider);
                }

                if (target.m_anaDynamicBones != null)
                    target.m_anaDynamicBones.m_Colliders.Remove(danCollider);

                if (target.m_bellyDynamicBone != null)
                    target.m_bellyDynamicBone.m_Colliders.Remove(danCollider);
            }

            foreach (var virtualDanCollider in m_virtualDanColliders)
            {
                if (virtualDanCollider == null)
                    continue;

                if (target.m_bellyDynamicBone != null)
                    target.m_bellyDynamicBone.m_Colliders.Remove(virtualDanCollider);
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

        internal void RemoveFingerColliders(CollisionAgent target, CollisionAgent secondTarget)
        {
            RemoveFingerColliders(target);

            if (secondTarget == null)
                return;

            RemoveFingerColliders(secondTarget);
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

        internal void ClearTarget()
        {
            m_referenceTarget = null;
            m_danPenetration = false;
            lastDanEndVector = Vector3.zero;
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
            m_virtualDanColliders = new List<DynamicBoneCollider>();

            ClearTama();

#if !STUDIO

            ClearTarget();

            if (m_fingerColliders == null || m_fingerColliders.Count == 0)
                return;

            foreach (var collider in m_fingerColliders)
            {
                if (collider != null)
                    UnityEngine.Object.Destroy(collider);
            }
#endif

        }

        internal void ToggleMaleColliders()
        {
            foreach (var danCollider in m_danColliders)
            {
                if (danCollider == null)
                    continue;

                danCollider.enabled = !danCollider.enabled;
            }

            foreach (var virtualDanCollider in m_virtualDanColliders)
            {
                if (virtualDanCollider == null)
                    continue;

                virtualDanCollider.enabled = !virtualDanCollider.enabled;
            }
        }
    }
}