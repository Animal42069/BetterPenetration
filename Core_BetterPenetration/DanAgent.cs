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
        private ChaControl m_danCharacter;
        private DanPoints m_danPoints;
        private DanOptions m_danOptions;
        private Transform m_referenceTarget;
        private List<DynamicBoneCollider> m_danColliders;
        private DynamicBoneCollider m_indexCollider;
        private DynamicBoneCollider m_middleCollider;
        private DynamicBoneCollider m_ringCollider;
        private bool m_danPointsFound = false;
        private bool m_bpDanPointsFound = false;
        private bool m_danPenetration = false;
#if HS2 || AI
        private float m_baseDanLength = 1.8f;
        private float m_baseSectionHalfLength = 0.95f;
#elif KK
        private float m_baseDanLength = 0.18f;
        private float m_baseSectionHalfLength = 0.095f;
#endif

        public DanAgent(ChaControl character, DanOptions options)
        {
      //      Console.WriteLine("Initialize DanAgent");

            Initialize(character, options);
        }

        public void Initialize(ChaControl character, DanOptions options)
        {
            m_danPointsFound = false;
            m_bpDanPointsFound = false;
            m_danPenetration = false;
            m_danOptions = options;
            m_danColliders = new List<DynamicBoneCollider>();

            if (character == null)
                return;

            m_danCharacter = character;

            Transform dan101 = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanBase));
            Transform dan109 = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanHead));
            Transform danTop = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanTop));

            if (dan101 != null && dan109 != null && danTop != null)
            {
                Transform[] danMid = new Transform[3];
                danMid[0] = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanMid0));
                danMid[1] = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanMid1));
                danMid[2] = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.DanMid2));

                if (danMid[0] != null && danMid[1] != null && danMid[2] != null)
                {
                    m_danPoints = new DanPoints(dan101, dan109, danTop, danMid);
                    m_bpDanPointsFound = true;
                }
                else
                {
                    m_danPoints = new DanPoints(dan101, dan109, danTop);
                }

                m_danPointsFound = true;

                m_baseDanLength = Vector3.Distance(dan101.position, dan109.position);
                if (MathHelpers.ApproximatelyZero(m_baseDanLength))
                    m_baseDanLength = 1.8f;

                m_baseSectionHalfLength = m_danPoints.danEnd.localPosition.z / (2 * (m_danPoints.danPoints.Count - 1));
                if (MathHelpers.ApproximatelyZero(m_baseSectionHalfLength))
                    m_baseSectionHalfLength = 1.9f / (2 * (m_danPoints.danPoints.Count - 1));

                for (int danPoint = 1; danPoint < m_danPoints.danPoints.Count; danPoint++)
                {
                    m_danColliders.Add(InitializeCollider(m_danPoints.danPoints[danPoint - 1], m_danOptions.danRadius * m_danPoints.danLossyScale[danPoint].x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                        DynamicBoneCollider.Direction.Z, m_danOptions.danVerticalCenter, m_baseSectionHalfLength));
                }


       /*         if (!m_bpDanPointsFound)
                {
                    m_baseSectionHalfLength = m_danPoints.danEnd.localPosition.z / 2;
                    if (MathHelpers.ApproximatelyZero(m_baseSectionHalfLength))
                        m_baseSectionHalfLength = 0.95f;

                    m_danColliders.Add(InitializeCollider(m_danPoints.danStart, m_danOptions.danRadius * m_danPoints.danLossyScale[0].x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                        DynamicBoneCollider.Direction.Z, m_danOptions.danVerticalCenter, m_baseSectionHalfLength));
                }
                else
                {
                    m_baseSectionHalfLength = m_danPoints.danEnd.localPosition.z / (2 * (m_danPoints.danPoints.Count - 1));
                    if (MathHelpers.ApproximatelyZero(m_baseSectionHalfLength))
                        m_baseSectionHalfLength = 0.2375f;

                    for (int danPoint = 1; danPoint < m_danPoints.danPoints.Count; danPoint++)
                    {
                        m_danColliders.Add(InitializeCollider(m_danPoints.danPoints[danPoint - 1], m_danOptions.danRadius * m_danPoints.danLossyScale[danPoint].x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                            DynamicBoneCollider.Direction.Z, m_danOptions.danVerticalCenter, m_baseSectionHalfLength));
                    }
                }*/
            }

            UpdateFingerColliders(m_danOptions.fingerRadius, m_danOptions.fingerLength);

            Console.WriteLine("Dan Found " + m_danPointsFound);
            Console.WriteLine("BP Dan Found " + m_bpDanPointsFound);
        }

        private DynamicBoneCollider InitializeCollider(Transform parent, float radius, float length,
            DynamicBoneCollider.Direction direction = DynamicBoneCollider.Direction.X, float verticalCenter = 0, float horizontalCenter = 0)
        {
            if (parent == null)
                return null;

            DynamicBoneCollider collider = parent.GetComponent<DynamicBoneCollider>();

            if (collider == null)
                collider = parent.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

            collider.m_Direction = direction;
            collider.m_Center = new Vector3(0, verticalCenter, horizontalCenter);
            collider.m_Bound = DynamicBoneCollider.Bound.Outside;
            collider.m_Radius = radius;
            collider.m_Height = length;

     /*       Console.WriteLine($"InitializeCollider {parent.name}");
            Console.WriteLine($"m_Radius {collider.m_Radius}");
            Console.WriteLine($"m_Height {collider.m_Height}");
            Console.WriteLine($"m_Center {collider.m_Center.z}");
     */
            return collider;
        }

        public void UpdateDanCollider(float danRadius, float danHeadLength, float danVerticalCenter)
        {
            if (!m_danPointsFound || m_danColliders.Count < 1 || m_danColliders.Count >= m_danPoints.danPoints.Count)
                return;

            for (int danCollider = 0; danCollider < m_danColliders.Count; danCollider++)
            {
                m_danColliders[danCollider] = InitializeCollider(m_danPoints.danPoints[danCollider], m_danOptions.danRadius * m_danPoints.danLossyScale[danCollider + 1].x, ((m_baseSectionHalfLength + m_danOptions.danHeadLength) * 2),
                    DynamicBoneCollider.Direction.Z, m_danOptions.danVerticalCenter, m_baseSectionHalfLength);
            }

            m_danOptions.danRadius = danRadius;
            m_danOptions.danHeadLength = danHeadLength;
            m_danOptions.danVerticalCenter = danVerticalCenter;
        }

        public void UpdateFingerColliders(float fingerRadius, float fingerLength)
        {
            if (!m_danPointsFound)
                return;

            Transform index = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.IndexFinger));
            Transform middle = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.MiddleFinger));
            Transform ring = m_danCharacter.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains(BoneNames.RingFinger));

            m_indexCollider = InitializeCollider(index, fingerRadius, fingerLength);
            m_middleCollider = InitializeCollider(middle, fingerRadius, fingerLength);
            m_ringCollider = InitializeCollider(ring, fingerRadius, fingerLength);

            m_danOptions.fingerRadius = fingerRadius;
            m_danOptions.fingerLength = fingerLength;
        }

        public void UpdateDanOptions(float danLengthSquish, float danGirthSquish, float squishThreshold, bool useFingerColliders)
        {
            m_danOptions.danLengthSquish = danLengthSquish;
            m_danOptions.danGirthSquish = danGirthSquish;
            m_danOptions.squishThreshold = squishThreshold;
            m_danOptions.useFingerColliders = useFingerColliders;
        }

        public void SetDanTarget(CollisionAgent targetAgent)
        {
            if (m_referenceTarget == null || !m_danPointsFound)
                return;

            Vector3 dan101_pos = m_danPoints.danStart.position;
            Vector3 lookTarget = m_referenceTarget.position;

            if (m_referenceTarget.name == LookTargets.KokanTarget)
                lookTarget += (m_referenceTarget.forward * targetAgent.m_collisionOptions.kokanForwardOffset) + (m_referenceTarget.up * targetAgent.m_collisionOptions.kokanUpOffset);
            else if (m_referenceTarget.name == LookTargets.HeadTarget)
                lookTarget += (m_referenceTarget.forward * targetAgent.m_collisionOptions.headForwardOffset) + (m_referenceTarget.up * targetAgent.m_collisionOptions.headUpOffset);

            if (targetAgent.m_collisionOptions.kokan_adjust && targetAgent.adjustFAnimation && m_referenceTarget.name == LookTargets.KokanTarget)
                lookTarget += m_referenceTarget.forward * targetAgent.m_collisionOptions.kokan_adjust_position_z;

            float danDistanceToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (danDistanceToTarget == 0)
                return;

            Vector3 danVector = Vector3.Normalize(lookTarget - dan101_pos);
            Vector3 dan109_pos = dan101_pos + danVector * m_baseDanLength;
            float danLength = m_baseDanLength;
            //     if (!m_bpDanPointsFound)
            //      {
            if (m_danPenetration)
            {
                if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                {
                    dan109_pos = ConstrainDanToBody(targetAgent, dan101_pos, lookTarget, danVector, danDistanceToTarget, out danLength);
                }
                else if (m_referenceTarget.name == LookTargets.HeadTarget)
                {
                    dan109_pos = ConstrainDanToHead(targetAgent, dan101_pos, lookTarget, danVector, danDistanceToTarget, out danLength);
                }
            }
            else if ((m_referenceTarget.name == LookTargets.KokanTarget) && (m_baseDanLength > danDistanceToTarget))
            {
                dan109_pos = ConstrainDanToPull(dan101_pos, danVector, danDistanceToTarget, out danLength);
            }


            if (!m_bpDanPointsFound)
            {
                Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);
                Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, m_danPoints.danTop.right));

                m_danPoints.danStart.rotation = danQuaternion;
                m_danPoints.danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);
            }
            else
            {
                Vector3 outsideVector = danVector;
                Vector3 insideVector = Vector3.Normalize(dan109_pos - lookTarget);
                List<Vector3> danPoints = new List<Vector3>();
           //     danPoints.Add(dan101_pos);
                foreach (var point in m_danPoints.danPoints)
                    danPoints.Add(point.position);
           //     danPoints.Add(dan109_pos);
                float danSegmentLength = danLength / (danPoints.Count - 1);

                //        Console.WriteLine()

        //        float lengthScaleFactor = danLength / m_baseDanLength;
                float girthScaleFactor = 1 + (m_baseDanLength / danLength - 1) * m_danOptions.danGirthSquish;

    
                m_danPoints.danPoints[0].localScale = new Vector3(m_danPoints.danScale[0].x * girthScaleFactor, m_danPoints.danScale[0].y * girthScaleFactor, m_danPoints.danScale[0].z/* * lengthScaleFactor*/);
                m_danPoints.danPoints[1].localScale = new Vector3(m_danPoints.danScale[1].x * girthScaleFactor, m_danPoints.danScale[1].y * girthScaleFactor, m_danPoints.danScale[1].z);
                m_danPoints.danPoints[2].localScale = new Vector3(m_danPoints.danScale[2].x * (1 - (1 - 1 / girthScaleFactor) / 2), m_danPoints.danScale[2].y * (1 - (1 - 1 / girthScaleFactor) / 2), m_danPoints.danScale[2].z);
                m_danPoints.danPoints[3].localScale = new Vector3(m_danPoints.danScale[3].x * (1 / girthScaleFactor), m_danPoints.danScale[3].y * (1 / girthScaleFactor), m_danPoints.danScale[3].z);
                m_danPoints.danPoints[4].localScale = new Vector3(m_danPoints.danScale[4].x * (1 / girthScaleFactor), m_danPoints.danScale[4].y * (1 / girthScaleFactor), m_danPoints.danScale[4].z);

                m_danColliders[0].m_Center = new Vector3(m_danColliders[0].m_Center.x, m_danColliders[0].m_Center.y, danSegmentLength / 2);
                m_danColliders[1].m_Center = new Vector3(m_danColliders[1].m_Center.x, m_danColliders[1].m_Center.y, danSegmentLength / 2);
                m_danColliders[2].m_Center = new Vector3(m_danColliders[2].m_Center.x, m_danColliders[2].m_Center.y, danSegmentLength / 2);
                m_danColliders[3].m_Center = new Vector3(m_danColliders[3].m_Center.x, m_danColliders[3].m_Center.y, danSegmentLength / 2);

                //  m_danPoints.danMid[0].localScale = new Vector3(girthScaleFactor, girthScaleFactor, m_danPoints.danMid[0].localScale.z);

       /*         Console.WriteLine($"m_danPoints.danStart.localScale {m_danPoints.danStart.localScale.z:F3}");
                Console.WriteLine($"girthScaleFactor {girthScaleFactor:F3}");
                Console.WriteLine($"1 - (1 - 1 / girthScaleFactor) / 2 {(1 - (1 - 1 / girthScaleFactor) / 2):F3}");
                Console.WriteLine($"1 / girthScaleFactor {(1 / girthScaleFactor):F3}");
                Console.WriteLine($"dan101_pos {dan101_pos.x:F3}, {dan101_pos.y:F3}, {dan101_pos.z:F3}");
                Console.WriteLine($"dan109_pos {dan109_pos.x:F3}, {dan109_pos.y:F3}, {dan109_pos.z:F3}");
                Console.WriteLine($"lookTarget {lookTarget.x:F3}, {lookTarget.y:F3}, {lookTarget.z:F3}");
                Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
                Console.WriteLine($"danLength {danLength}");
                Console.WriteLine($"danSegmentLength {danSegmentLength}");
                Console.WriteLine($"outsideVector {outsideVector.x:F3}, {outsideVector.y:F3}, {outsideVector.z:F3}");
                Console.WriteLine($"insideVector {insideVector.x:F3}, {insideVector.y:F3}, {insideVector.z:F3}");
       */
                if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f) || danLength <= danDistanceToTarget)
                {
                    Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);

                    for (int point = 1; point < danPoints.Count; point++)
                        danPoints[point] = danPoints[point - 1] + danForwardVector * danSegmentLength;
                }
                else
                {
                    for (int point = 1; point < danPoints.Count; point++)
                    {
                        if (danSegmentLength * (point - 1) >= danDistanceToTarget)
                        {
                            danPoints[point] = danPoints[point - 1] + insideVector * danSegmentLength;
                        }
                        else if (danSegmentLength * point >= danDistanceToTarget)
                        {
                            float outsideDistance = danDistanceToTarget - danSegmentLength * (point - 1);
                            double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                            MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                            //       Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                            danPoints[point] = lookTarget + insideVector * (float)distanceAlongInside;
                        }
                        else
                        {
                            danPoints[point] = danPoints[point - 1] + outsideVector * danSegmentLength;
                        }

       /*                 if (danDistanceToTarget >= danSegmentLength * point)
                        {
                            danPoints[point] = danPoints[point - 1] + outsideVector * danSegmentLength;
                        }
                        else if (danDistanceToTarget <= danSegmentLength * point)
                        {
                            danPoints[point] = danPoints[point - 1] + insideVector * danSegmentLength;
                        }
                        else
                        {
                            float outsideDistance = danDistanceToTarget - danSegmentLength * (point - 1);
                            double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                            MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                            //       Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                            danPoints[point] = lookTarget + insideVector * (float)distanceAlongInside;
                        }*/
                    }
                }

                for (int point = 0; point < danPoints.Count - 1; point++)
                {
                    Vector3 forwardVector = Vector3.Normalize(danPoints[point + 1] - danPoints[point]);
                    Quaternion danQuaternion = Quaternion.LookRotation(forwardVector, Vector3.Cross(forwardVector, m_danPoints.danTop.right));
                    m_danPoints.danPoints[point].SetPositionAndRotation(danPoints[point], danQuaternion);

               /*     if (point == 0)
                        m_danPoints.danStart.rotation = danQuaternion;
                    else
                        m_danPoints.danMid[point - 1].SetPositionAndRotation(danPoints[point], danQuaternion);
               */
                    if (point == danPoints.Count - 2)
                        m_danPoints.danPoints[point + 1].SetPositionAndRotation(danPoints[point + 1], danQuaternion);
                }

       //         m_danPoints.danTop.localEulerAngles = m_danPoints.danStart.localEulerAngles;

      /*          Console.WriteLine($"dan101_pos {m_danPoints.danPoints[0].position.x:F3}, {m_danPoints.danPoints[0].position.y:F3}, {m_danPoints.danPoints[0].position.z:F3}");
                Console.WriteLine($"dan103_pos {m_danPoints.danPoints[1].position.x:F3}, {m_danPoints.danPoints[1].position.y:F3}, {m_danPoints.danPoints[1].position.z:F3}");
                Console.WriteLine($"dan105_pos {m_danPoints.danPoints[2].position.x:F3}, {m_danPoints.danPoints[2].position.y:F3}, {m_danPoints.danPoints[2].position.z:F3}");
                Console.WriteLine($"dan107_pos {m_danPoints.danPoints[3].position.x:F3}, {m_danPoints.danPoints[3].position.y:F3}, {m_danPoints.danPoints[3].position.z:F3}");
                Console.WriteLine($"dan109_pos {m_danPoints.danPoints[4].position.x:F3}, {m_danPoints.danPoints[4].position.y:F3}, {m_danPoints.danPoints[4].position.z:F3}");
      */

                /*         Console.WriteLine($"dan101_pos {dan101_pos.x:F3}, {dan101_pos.y:F3}, {dan101_pos.z:F3}");
                         Console.WriteLine($"dan109_pos {dan109_pos.x:F3}, {dan109_pos.y:F3}, {dan109_pos.z:F3}");
                         Console.WriteLine($"lookTarget {lookTarget.x:F3}, {lookTarget.y:F3}, {lookTarget.z:F3}");
                         Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
                         Console.WriteLine($"danLength {danLength}");
                         Console.WriteLine($"danSegmentLength {danSegmentLength}");
                         Console.WriteLine($"outsideVector {outsideVector.x:F3}, {outsideVector.y:F3}, {outsideVector.z:F3}");
                         Console.WriteLine($"insideVector {insideVector.x:F3}, {insideVector.y:F3}, {insideVector.z:F3}");
                */
                /*       if (danDistanceToTarget >= danSegmentLength * 4)
                       {
                           dan103_pos = dan101_pos + outsideVector * danSegmentLength;
                           dan105_pos = dan103_pos + outsideVector * danSegmentLength;
                           dan107_pos = dan105_pos + outsideVector * danSegmentLength;
                           dan109_pos = dan107_pos + outsideVector * danSegmentLength;
                       }
                       else if (danDistanceToTarget >= danSegmentLength * 3)
                       {
                           dan103_pos = dan101_pos + outsideVector * danSegmentLength;
                           dan105_pos = dan103_pos + outsideVector * danSegmentLength;
                           dan107_pos = dan105_pos + outsideVector * danSegmentLength;

                           if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f))
                           {
                               dan109_pos = dan107_pos + insideVector * danSegmentLength;
                           }
                           else
                           {
                               float outsideDistance = danDistanceToTarget - danSegmentLength * 3;
                               double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                               MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                               Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                               dan109_pos = lookTarget + insideVector * (float)distanceAlongInside;
                           }
                       }
                       else if (danDistanceToTarget >= danSegmentLength * 2)
                       {
                           dan103_pos = dan101_pos + outsideVector * danSegmentLength;
                           dan105_pos = dan103_pos + outsideVector * danSegmentLength;

                           if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f))
                           {
                               dan107_pos = dan105_pos + insideVector * danSegmentLength;
                           }
                           else
                           {
                               float outsideDistance = danDistanceToTarget - danSegmentLength * 2;
                               double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                               MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                               Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                               dan107_pos = lookTarget + insideVector * (float)distanceAlongInside;
                           }
                           //    m_danPoints.danMid[2].position =
                           dan109_pos = dan107_pos + insideVector * danSegmentLength;
                       }
                       else if (danDistanceToTarget >= danSegmentLength)
                       {
                           dan103_pos = dan101_pos + outsideVector * danSegmentLength;

                           if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f))
                           {
                               dan105_pos = dan103_pos + insideVector * danSegmentLength;
                           }
                           else
                           {
                               float outsideDistance = danDistanceToTarget - danSegmentLength;
                               double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                               MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                               Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                               dan105_pos = lookTarget + insideVector * (float)distanceAlongInside;
                           }
                           //   m_danPoints.danMid[1].position =
                           dan107_pos = dan105_pos + insideVector * danSegmentLength;
                           dan109_pos = dan107_pos + insideVector * danSegmentLength;
                       }
                       else if (!MathHelpers.ApproximatelyZero(danDistanceToTarget))
                       {
                           if (MathHelpers.VectorsEqual(outsideVector, insideVector, 0.001f))
                           {
                               dan103_pos = dan101_pos + insideVector * danSegmentLength;
                           }
                           else
                           {
                               float outsideDistance = danDistanceToTarget;
                               double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                               MathHelpers.SolveSSATriangle(danSegmentLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                               Console.WriteLine($"outsideDistance {outsideDistance}, angleOutsideToInside {MathHelpers.RadToDeg(angleOutsideToInside)}, distanceAlongInside {distanceAlongInside}");
                               dan103_pos = lookTarget + insideVector * (float)distanceAlongInside;
                           }
                           //    m_danPoints.danMid[0].position =
                           dan105_pos = dan103_pos + insideVector * danSegmentLength;
                           dan107_pos = dan105_pos + insideVector * danSegmentLength;
                           dan109_pos = dan107_pos + insideVector * danSegmentLength;
                       }
                       else
                       {
                           dan103_pos = dan101_pos + insideVector * danSegmentLength;
                           dan105_pos = dan103_pos + insideVector * danSegmentLength;
                           dan107_pos = dan105_pos + insideVector * danSegmentLength;
                           dan109_pos = dan107_pos + insideVector * danSegmentLength;
                       }

                       Console.WriteLine($"dan103_pos {dan103_pos.x:F3}, {dan103_pos.y:F3}, {dan103_pos.z:F3}");
                       Console.WriteLine($"dan105_pos {dan105_pos.x:F3}, {dan105_pos.y:F3}, {dan105_pos.z:F3}");
                       Console.WriteLine($"dan107_pos {dan107_pos.x:F3}, {dan107_pos.y:F3}, {dan107_pos.z:F3}");
                       Console.WriteLine($"dan109_pos {dan109_pos.x:F3}, {dan109_pos.y:F3}, {dan109_pos.z:F3}");
                */

                /*          Vector3 dan101ForwardVector = Vector3.Normalize(dan103_pos - dan101_pos);
                          Quaternion dan101Quaternion = Quaternion.LookRotation(dan101ForwardVector, Vector3.Cross(dan101ForwardVector, m_danPoints.danTop.right));
                          m_danPoints.danStart.rotation = dan101Quaternion;

                          Vector3 dan103ForwardVector = Vector3.Normalize(dan105_pos - dan103_pos);
                          Quaternion dan103Quaternion = Quaternion.LookRotation(dan103ForwardVector, Vector3.Cross(dan103ForwardVector, m_danPoints.danTop.right));
                          m_danPoints.danMid[0].SetPositionAndRotation(dan103_pos, dan103Quaternion);

                          Vector3 dan105ForwardVector = Vector3.Normalize(dan107_pos - dan105_pos);
                          Quaternion dan105Quaternion = Quaternion.LookRotation(dan105ForwardVector, Vector3.Cross(dan105ForwardVector, m_danPoints.danTop.right));
                          m_danPoints.danMid[1].SetPositionAndRotation(dan105_pos, dan105Quaternion);

                          Vector3 dan107ForwardVector = Vector3.Normalize(dan109_pos - dan107_pos);
                          Quaternion dan107Quaternion = Quaternion.LookRotation(dan107ForwardVector, Vector3.Cross(dan107ForwardVector, m_danPoints.danTop.right));
                          m_danPoints.danMid[2].SetPositionAndRotation(dan107_pos, dan107Quaternion);
                          m_danPoints.danEnd.SetPositionAndRotation(dan109_pos, dan107Quaternion);*/
            }
        }

        private Vector3 ConstrainDanToBody(CollisionAgent target, Vector3 dan101_pos, Vector3 lookTarget, Vector3 danVector, float danDistanceToTarget, out float danLength)
        {
     //       Console.WriteLine("ConstrainDanToBody");

            danLength = m_baseDanLength;
     //       Plane kokanPlane = new Plane(m_danPoints.danStart.forward, lookTarget);

            if (m_baseDanLength - danDistanceToTarget > m_baseDanLength * m_danOptions.squishThreshold)
                danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget - m_baseDanLength * m_danOptions.squishThreshold) * m_danOptions.danLengthSquish;
            //    danLength = danDistanceToTarget + (m_baseDanLength - danDistanceToTarget) * m_danOptions.danLengthSquish;
            //       danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;

            //         if (kokanPlane.GetSide(dan101_pos))
            //             danLength = m_baseDanLength * m_danOptions.squishThreshold + m_baseDanLength * (1 - m_danOptions.squishThreshold) * m_danOptions.danLengthSquish;

            //      float minDanLength = danDistanceToTarget + (danLength * (1 - m_danOptions.telescopeThreshold));

            //       if (minDanLength > danLength)
            //          minDanLength = danLength;

            //      if (m_danOptions.forceTelescope)
            //           danLength = minDanLength;

       /*     Console.WriteLine($"m_baseDanLength {m_baseDanLength}");
            Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
            Console.WriteLine($"danLength {danLength}");
       */

            Vector3 adjustedDan109 = dan101_pos + danVector * danLength;
            bool constrainPointFound = false;

            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, danLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true);
            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, danLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false);
            return adjustedDan109;
        }

        private Vector3 ConstrainDanToCollisionPoints(Vector3 danStart, Vector3 danEnd, float targetLength, ref bool constrainPointFound, List<CollisionPoint> collisionPoints, float clippingDepth, bool frontSide)
        {
            Vector3 adjustedDanEnd = danEnd;
            if (constrainPointFound)
                return adjustedDanEnd;

            bool constainPastNearSide = true;
            bool constainPastFarSide = false;

            List<Vector3> collisionPointPositions = new List<Vector3>();

            foreach (var collisionPoint in collisionPoints)
            {
                if (frontSide == collisionPoint.info.inward)
                    collisionPointPositions.Add(collisionPoint.transform.position + (clippingDepth + collisionPoint.info.offset) * collisionPoint.transform.forward);
                else
                    collisionPointPositions.Add(collisionPoint.transform.position - (clippingDepth + collisionPoint.info.offset) * collisionPoint.transform.forward);

     /*          if (true)
                {
                    if (!frontSide)
                        Console.WriteLine($"FrontHitPoint{collisionPointPositions.Count}: {collisionPointPositions[collisionPointPositions.Count - 1].x:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].y:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].z:F3}");
                    else
                        Console.WriteLine($"BackHitPoint{collisionPointPositions.Count}: {collisionPointPositions[collisionPointPositions.Count - 1].x:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].y:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].z:F3}");
                }*/
            }

            //      int currentCollisionPoint = 1;

            //      for (int danIndex = 1; danIndex < adjustedDanPoints.Count; danIndex++)
            //     {
            //          constainPastFarSide = false;

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
                //      if (!frontSide)
                //         Console.WriteLine($"ConstrainDanToCollisionPoints danIndex {danIndex} collision index {index} constrainPointFound {constrainPointFound} adjustmentMade {adjustmentMade}");
                //     Console.WriteLine($"forwardVector {forwardVector}");

                //     if (constrainPointFound)
                //      {
                //    if (adjustmentMade)
                //    {
                //       for (int nextDanIndex = danIndex + 1; nextDanIndex < adjustedDanPoints.Count; nextDanIndex++)
                //            adjustedDanPoints[nextDanIndex] = adjustedDanPoints[danIndex] + forwardVector * targetLength * (nextDanIndex - danIndex);
                //    }

                //          currentCollisionPoint = index;
                //         break;
                //     }
                //    }
            }

            return adjustedDanEnd;
        }

        private Vector3 ConstrainDanToHead(CollisionAgent target, Vector3 dan101_pos, Vector3 lookTarget, Vector3 danVector, float danDistanceToTarget, out float danLength)
        {
            //    float maxDistance;

         //   Console.WriteLine("ConstrainDanToHead");

            Vector3 headCollisionPoint = target.m_collisionPoints.headCollisionPoint.position;

            float danDistanceToLimit = Vector3.Distance(dan101_pos, headCollisionPoint);
            float targetDistanceToLimit = Vector3.Distance(lookTarget, headCollisionPoint);

            danLength = m_baseDanLength;

            if (danDistanceToLimit < targetDistanceToLimit)
                danDistanceToTarget = 0;
         //   {
             //   danLength = m_baseDanLength * m_danOptions.squishThreshold + m_baseDanLength * (1 - m_danOptions.squishThreshold) * m_danOptions.danLengthSquish;
          //      danLength = m_baseDanLength * (1 - (1 - m_danOptions.squishThreshold) * m_danOptions.danLengthSquish);
                //         danLength = m_baseDanLength * (1 - m_danOptions.danSoftness);
                //        maxDistance = danDistanceToLimit;
         //   }
        //    else 
            if (m_baseDanLength - danDistanceToTarget > m_baseDanLength * m_danOptions.squishThreshold)
            {
                //            danLength = danDistanceToTarget + (m_baseDanLength - danDistanceToTarget) * m_danOptions.danLengthSquish;
                danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget - m_baseDanLength * m_danOptions.squishThreshold) * m_danOptions.danLengthSquish;

           /*     if (m_baseDanLength > danDistanceToTarget)
                    danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;
                else
                    danLength = m_baseDanLength;
                maxDistance = danDistanceToTarget + targetDistanceToLimit;*/
            }

     /*       Console.WriteLine($"m_baseDanLength {m_baseDanLength}");
            Console.WriteLine($"danDistanceToLimit {danDistanceToLimit}");
            Console.WriteLine($"targetDistanceToLimit {targetDistanceToLimit}");
            Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
            Console.WriteLine($"danLength {danLength}");
     */
            //     if (danLength > maxDistance)
            //         danLength = maxDistance;

            return (dan101_pos + danVector * danLength);
        }

        private Vector3 ConstrainDanToPull(Vector3 dan101_pos, Vector3 danVector, float danDistanceToTarget, out float danLength)
        {
       //     Console.WriteLine("ConstrainDanToPull");

            danLength = m_baseDanLength;
            if (m_baseDanLength - danDistanceToTarget > m_baseDanLength * m_danOptions.squishThreshold)
                danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget - m_baseDanLength * m_danOptions.squishThreshold) * m_danOptions.danLengthSquish;
            //      danLength = danDistanceToTarget + (m_baseDanLength - danDistanceToTarget) * m_danOptions.danLengthSquish;

       //     Console.WriteLine($"m_baseDanLength {m_baseDanLength}");
      //      Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
       //     Console.WriteLine($"danLength {danLength}");

            /*     danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;
                 float minDanLength = danDistanceToTarget + (danLength * (1 - m_danOptions.telescopeThreshold));

                 if (minDanLength > danLength)
                     minDanLength = danLength;

                 if (m_danOptions.forceTelescope)
                     danLength = minDanLength;
            */
            return (dan101_pos + danVector * danLength);
        }

        public void SetupNewDanTarget(Transform lookAtTransform, string currentMotion, bool topStick, CollisionAgent firstTarget, CollisionAgent secondTarget = null)
        {
            ClearTarget();

            if (!m_danPointsFound)
                return;

            if (secondTarget != null)
            {
                if (lookAtTransform == null)
                {
                    AddDanColliders(firstTarget);
                    AddDanColliders(secondTarget);
                    RemoveFingerColliders(firstTarget);
                    RemoveFingerColliders(secondTarget);
                }
                else if (lookAtTransform.name == LookTargets.KokanTarget)
                {
                    AddDanColliders(firstTarget);
                    RemoveDanColliders(secondTarget);
                    RemoveFingerColliders(firstTarget);

                    if (m_danOptions.useFingerColliders)
                        AddFingerColliders(secondTarget);
                    else
                        RemoveFingerColliders(secondTarget);
                }
                else
                {
                    RemoveDanColliders(firstTarget);
                    RemoveDanColliders(secondTarget);
                    RemoveFingerColliders(firstTarget);
                    RemoveFingerColliders(secondTarget);
                }
            }
            else
            {
                if (lookAtTransform == null || lookAtTransform.name == LookTargets.KokanTarget)
                    AddDanColliders(firstTarget);
                else
                    RemoveDanColliders(firstTarget);

                if (m_danOptions.useFingerColliders && lookAtTransform == null)
                    AddFingerColliders(firstTarget);
                else
                    RemoveFingerColliders(firstTarget);
            }

            if (lookAtTransform == null)
                return;

            m_referenceTarget = lookAtTransform;
            if (topStick && m_referenceTarget.name == LookTargets.KokanTarget && !currentMotion.Contains("Idle") && !currentMotion.Contains("Pull") && !currentMotion.Contains("OUT") && firstTarget.m_bpKokanTarget != null)
                m_referenceTarget = firstTarget.m_bpKokanTarget;

            m_danPenetration = topStick || currentMotion.Contains("IN");
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

        public void ClearTarget()
        {
            m_referenceTarget = null;
            m_danPenetration = false;
        }

        public void RemoveColliders(CollisionAgent target)
        {
            RemoveDanColliders(target);
            RemoveFingerColliders(target);
        }

        public void DestroyColliders()
        {
            m_danPointsFound = false;
            m_bpDanPointsFound = false;

            ClearTarget();

            foreach (var danCollider in m_danColliders)
                UnityEngine.Object.Destroy(danCollider);
            UnityEngine.Object.Destroy(m_indexCollider);
            UnityEngine.Object.Destroy(m_middleCollider);
            UnityEngine.Object.Destroy(m_ringCollider);
        }
    }
}
