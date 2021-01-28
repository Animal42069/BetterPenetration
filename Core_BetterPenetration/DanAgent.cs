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
        private DynamicBoneCollider m_danCollider;
        private DynamicBoneCollider m_indexCollider;
        private DynamicBoneCollider m_middleCollider;
        private DynamicBoneCollider m_ringCollider;
        private bool m_danPointsFound = false;
        private bool m_bpDanPointsFound = false;
        private bool m_danPenetration = false;
#if HS2 || AI
        private float m_baseDanLength = 1.8f;
        private float m_baseDanCenter = 0.95f;
#elif KK
        private float m_baseDanLength = 0.18f;
        private float m_baseDanCenter = 0.095f;
#endif

        public DanAgent(ChaControl character, DanOptions options)
        {
            Console.WriteLine("Initialize DanAgent");

            Initialize(character, options);
        }

        public void Initialize(ChaControl character, DanOptions options)
        {
            m_danPointsFound = false;
            m_bpDanPointsFound = false;
            m_danPenetration = false;
            m_danOptions = options;

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
                m_baseDanCenter = m_danPoints.danEnd.localPosition.z / 2;
                if (MathHelpers.ApproximatelyZero(m_baseDanCenter))
                    m_baseDanCenter = 0.95f;       

                m_danCollider = InitializeCollider(dan101, m_danOptions.danRadius, ((m_baseDanCenter + m_danOptions.danHeadLength) * 2), 
                    DynamicBoneCollider.Direction.Z, m_danOptions.danVerticalCenter, m_baseDanCenter);
            }

            UpdateFingerColliders(m_danOptions.fingerRadius, m_danOptions.fingerLength);

            Console.WriteLine("bDansFound " + m_danPointsFound);
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

            return collider;
        }

        public void UpdateDanCollider(float danRadius, float danHeadLength, float danVerticalCenter)
        {
            if (!m_danPointsFound)
                return;

            m_danCollider = InitializeCollider(m_danPoints.danStart, danRadius, ((m_baseDanCenter + danHeadLength) * 2),
                DynamicBoneCollider.Direction.Z, danVerticalCenter, m_baseDanCenter);

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

        public void UpdateDanOptions(float danSoftness, float telescopeThreshold, bool forceTelescope, bool useFingerColliders)
        {
            m_danOptions.danSoftness = danSoftness;
            m_danOptions.telescopeThreshold = telescopeThreshold;
            m_danOptions.forceTelescope = forceTelescope;
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
                Console.WriteLine($"dan101_pos {dan101_pos.x:F3}, {dan101_pos.y:F3}, {dan101_pos.z:F3}");
                Console.WriteLine($"dan109_pos {dan109_pos.x:F3}, {dan109_pos.y:F3}, {dan109_pos.z:F3}");
                Console.WriteLine($"lookTarget {lookTarget.x:F3}, {lookTarget.y:F3}, {lookTarget.z:F3}");
                Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
                Console.WriteLine($"danLength {danLength}");

                Vector3 outsideVector = danVector;
                Vector3 insideVector = Vector3.Normalize(dan109_pos - lookTarget);
                float danSegmentLength = danLength / 4;

                Console.WriteLine($"outsideVector {outsideVector.x:F3}, {outsideVector.y:F3}, {outsideVector.z:F3}");
                Console.WriteLine($"insideVector {insideVector.x:F3}, {insideVector.y:F3}, {insideVector.z:F3}");

                if (danDistanceToTarget >= danSegmentLength * 4)
                {
                    m_danPoints.danMid[0].position = m_danPoints.danStart.position + outsideVector * danSegmentLength;
                    m_danPoints.danMid[1].position = m_danPoints.danMid[0].position + outsideVector * danSegmentLength;
                    m_danPoints.danMid[2].position = m_danPoints.danMid[1].position + outsideVector * danSegmentLength;
                    m_danPoints.danEnd.position = m_danPoints.danMid[2].position + outsideVector * danSegmentLength;
                }
                else if (danDistanceToTarget >= danSegmentLength * 3)
                {
                    m_danPoints.danMid[0].position = m_danPoints.danStart.position + outsideVector * danSegmentLength;
                    m_danPoints.danMid[1].position = m_danPoints.danMid[0].position + outsideVector * danSegmentLength;
                    m_danPoints.danMid[2].position = m_danPoints.danMid[1].position + outsideVector * danSegmentLength;

                    float outsideDistance = danDistanceToTarget - danSegmentLength * 3;
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(danLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    m_danPoints.danEnd.position = lookTarget + insideVector * (float)distanceAlongInside;
                }
                else if (danDistanceToTarget >= danSegmentLength * 2)
                {
                    m_danPoints.danMid[0].position = m_danPoints.danStart.position + outsideVector * danSegmentLength;
                    m_danPoints.danMid[1].position = m_danPoints.danMid[0].position + outsideVector * danSegmentLength;

                    float outsideDistance = danDistanceToTarget - danSegmentLength * 2;
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(danLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    m_danPoints.danMid[2].position = lookTarget + insideVector * (float)distanceAlongInside;

                    //    m_danPoints.danMid[2].position =
                    m_danPoints.danEnd.position = m_danPoints.danMid[2].position + insideVector * danSegmentLength;
                }
                else if (danDistanceToTarget >= danSegmentLength)
                {
                    m_danPoints.danMid[0].position = m_danPoints.danStart.position + outsideVector * danSegmentLength;

                    float outsideDistance = danDistanceToTarget - danSegmentLength;
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(danLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    m_danPoints.danMid[1].position = lookTarget + insideVector * (float)distanceAlongInside;

                    //   m_danPoints.danMid[1].position =
                    m_danPoints.danMid[2].position = m_danPoints.danMid[1].position + insideVector * danSegmentLength;
                    m_danPoints.danEnd.position = m_danPoints.danMid[2].position + insideVector * danSegmentLength;
                }
                else if (!MathHelpers.ApproximatelyZero(danDistanceToTarget))
                {
                    float outsideDistance = danDistanceToTarget;
                    double angleOutsideToInside = (double)MathHelpers.DegToRad(Vector3.Angle(outsideVector, -insideVector));
                    MathHelpers.SolveSSATriangle(danLength, outsideDistance, angleOutsideToInside, out double distanceAlongInside, out _, out _);
                    m_danPoints.danMid[0].position = lookTarget + insideVector * (float)distanceAlongInside;

                    //    m_danPoints.danMid[0].position =
                    m_danPoints.danMid[1].position = m_danPoints.danMid[0].position + insideVector * danSegmentLength;
                    m_danPoints.danMid[2].position = m_danPoints.danMid[1].position + insideVector * danSegmentLength;
                    m_danPoints.danEnd.position = m_danPoints.danMid[2].position + insideVector * danSegmentLength;
                }
                else
                {
                    m_danPoints.danMid[0].position = m_danPoints.danStart.position + insideVector * danSegmentLength;
                    m_danPoints.danMid[1].position = m_danPoints.danMid[0].position + insideVector * danSegmentLength;
                    m_danPoints.danMid[2].position = m_danPoints.danMid[1].position + insideVector * danSegmentLength;
                    m_danPoints.danEnd.position = m_danPoints.danMid[2].position + insideVector * danSegmentLength;
                }

                Console.WriteLine($"m_danPoints.danMid[0].position {m_danPoints.danMid[0].position.x:F3}, {m_danPoints.danMid[0].position.y:F3}, {m_danPoints.danMid[0].position.z:F3}");
                Console.WriteLine($"m_danPoints.danMid[1].position {m_danPoints.danMid[1].position.x:F3}, {m_danPoints.danMid[1].position.y:F3}, {m_danPoints.danMid[1].position.z:F3}");
                Console.WriteLine($"m_danPoints.danMid[2].position {m_danPoints.danMid[2].position.x:F3}, {m_danPoints.danMid[2].position.y:F3}, {m_danPoints.danMid[2].position.z:F3}");
                Console.WriteLine($"m_danPoints.danEnd.position {m_danPoints.danEnd.position.x:F3}, {m_danPoints.danEnd.position.y:F3}, {m_danPoints.danEnd.position.z:F3}");


                Vector3 dan101ForwardVector = Vector3.Normalize(m_danPoints.danMid[0].position - m_danPoints.danStart.position);
                Quaternion dan101Quaternion = Quaternion.LookRotation(dan101ForwardVector, Vector3.Cross(dan101ForwardVector, m_danPoints.danTop.right));
                m_danPoints.danStart.rotation = dan101Quaternion;

                Vector3 dan103ForwardVector = Vector3.Normalize(m_danPoints.danMid[1].position - m_danPoints.danMid[0].position);
                Quaternion dan103Quaternion = Quaternion.LookRotation(dan103ForwardVector, Vector3.Cross(dan103ForwardVector, m_danPoints.danTop.right));
                m_danPoints.danMid[0].rotation = dan103Quaternion;

                Vector3 dan105ForwardVector = Vector3.Normalize(m_danPoints.danMid[2].position - m_danPoints.danMid[1].position);
                Quaternion dan105Quaternion = Quaternion.LookRotation(dan105ForwardVector, Vector3.Cross(dan105ForwardVector, m_danPoints.danTop.right));
                m_danPoints.danMid[1].rotation = dan105Quaternion;

                Vector3 dan107ForwardVector = Vector3.Normalize(m_danPoints.danMid[3].position - m_danPoints.danMid[2].position);
                Quaternion dan107Quaternion = Quaternion.LookRotation(dan107ForwardVector, Vector3.Cross(dan107ForwardVector, m_danPoints.danTop.right));
                m_danPoints.danMid[2].rotation = dan107Quaternion;

                Vector3 dan109ForwardVector = Vector3.Normalize(m_danPoints.danEnd.position - m_danPoints.danMid[3].position);
                Quaternion dan109Quaternion = Quaternion.LookRotation(dan109ForwardVector, Vector3.Cross(dan109ForwardVector, m_danPoints.danTop.right));
                m_danPoints.danEnd.rotation = dan109Quaternion;


            }

        //    }
       /*     else
            {
                if (m_danPenetration)
                {
                    if (m_referenceTarget.name == LookTargets.KokanTarget || m_referenceTarget.name == LookTargets.AnaTarget || m_referenceTarget.name == LookTargets.BPKokanTarget)
                    {
                        ConstrainBPDanToBody(targetAgent, lookTarget, danDistanceToTarget);
                    }
                    else if (m_referenceTarget.name == LookTargets.HeadTarget)
                    {
                        ConstrainDanToHead(targetAgent, dan101_pos, lookTarget, danVector, danDistanceToTarget);
                    }
                }
                else if ((m_referenceTarget.name == LookTargets.KokanTarget) && (m_baseDanLength > danDistanceToTarget))
                {
                    ConstrainDanToPull(dan101_pos, danVector, danDistanceToTarget);
                }
            }*/
        }

        private Vector3 ConstrainDanToBody(CollisionAgent target, Vector3 dan101_pos, Vector3 lookTarget, Vector3 danVector, float danDistanceToTarget, out float danLength)
        {
            Console.WriteLine("ConstrainDanToBody");

            danLength = m_baseDanLength;
            Plane kokanPlane = new Plane(m_danPoints.danStart.forward, lookTarget);

            if (m_baseDanLength > danDistanceToTarget)
                danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;

            if (kokanPlane.GetSide(dan101_pos))
                danLength = m_baseDanLength * (1 - m_danOptions.danSoftness);

            float minDanLength = danDistanceToTarget + (danLength * (1 - m_danOptions.telescopeThreshold));

            if (minDanLength > danLength)
                minDanLength = danLength;

            if (m_danOptions.forceTelescope)
                danLength = minDanLength;

            Vector3 adjustedDan109 = dan101_pos + danVector * danLength;
            bool constrainPointFound = false;

            //   List<Vector3> adjustedDanPoints = new List<Vector3>();
            //   adjustedDanPoints.Add(dan101_pos);
            //   adjustedDanPoints.Add(adjustedDan109);

            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, ref danLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true);
            adjustedDan109 = ConstrainDanToCollisionPoints(dan101_pos, adjustedDan109, ref danLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false);
            return adjustedDan109;
        }

 /*       private void ConstrainBPDanToBody(CollisionAgent target, Vector3 lookTarget, Vector3 danVector, float danDistanceToTarget)
        {
            Console.WriteLine("ConstrainBPDanToBody");

            float targetDanLength = m_baseDanLength;
            Plane kokanPlane = new Plane(m_danPoints.danStart.forward, lookTarget);

            if (m_baseDanLength > danDistanceToTarget)
                targetDanLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;

            if (kokanPlane.GetSide(m_danPoints.danStart.position))
                targetDanLength = m_baseDanLength * (1 - m_danOptions.danSoftness);

            float minDanLength = danDistanceToTarget + (targetDanLength * (1 - m_danOptions.telescopeThreshold));

            if (minDanLength > targetDanLength)
                minDanLength = targetDanLength;

            if (m_danOptions.forceTelescope)
                targetDanLength = minDanLength;

      //      targetDanLength = targetDanLength / 4;
       //     minDanLength = minDanLength / 4;
            Vector3 adjustedDan101 = m_danPoints.danStart.position;

            Vector3 adjustedDan109 = adjustedDan101 + danVector * targetDanLength;
            bool constrainPointFound = false;
            adjustedDan109 = ConstrainDanToCollisionPoints(adjustedDan101, adjustedDan109, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true);
            adjustedDan109 = ConstrainDanToCollisionPoints(adjustedDan101, adjustedDan109, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false);



            List<Vector3> adjustedDanPoints = new List<Vector3>();
            Vector3 forwardVector = Vector3.Normalize(lookTarget - adjustedDan101);
            adjustedDanPoints.Add(adjustedDan101);
            adjustedDanPoints.Add(adjustedDan101 + forwardVector * targetDanLength);
            adjustedDanPoints.Add(adjustedDan101 + forwardVector * targetDanLength * 2);
            adjustedDanPoints.Add(adjustedDan101 + forwardVector * targetDanLength * 3);
            adjustedDanPoints.Add(adjustedDan101 + forwardVector * targetDanLength * 4);

            int i = 1;
            foreach(var point in adjustedDanPoints)
                Console.WriteLine($"dan10{i++} {point.x:F3} , {point.y:F3} , {point.z:F3}");


 //           bool constrainPointFound = false;
   //         ConstrainDanToCollisionPoints(ref adjustedDanPoints, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, true);
   //         ConstrainDanToCollisionPoints(ref adjustedDanPoints, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, true);

            /*
                        Vector3 dan103Vector = Vector3.Normalize(lookTarget - adjustedDan101);
                        Vector3 adjustedDan103 = adjustedDan101 + dan103Vector * targetDanLength;
                        bool constrainPointFound = false;
                        Vector3 forwardVector = dan103Vector;
                        if (danDistanceToTarget < targetDanLength)
                        {
                            adjustedDan103 = ConstrainDanToCollisionPoints(adjustedDan101, adjustedDan103, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, ref forwardVector);
                            adjustedDan103 = ConstrainDanToCollisionPoints(adjustedDan101, adjustedDan103, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, ref forwardVector);
                        }

                        Vector3 dan105Vector = Vector3.Normalize(adjustedDan103 - adjustedDan101);
                        if (constrainPointFound)
                            dan105Vector = forwardVector;
                        Vector3 adjustedDan105 = adjustedDan103 + dan105Vector * targetDanLength;
                        if (danDistanceToTarget < 2 * targetDanLength)
                        {
                            constrainPointFound = false;
                            adjustedDan105 = ConstrainDanToCollisionPoints(adjustedDan103, adjustedDan105, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, ref forwardVector, true);
                            adjustedDan105 = ConstrainDanToCollisionPoints(adjustedDan103, adjustedDan105, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, ref forwardVector, true);
                        }

                        Vector3 dan107Vector = Vector3.Normalize(adjustedDan105 - adjustedDan103);
                        if (constrainPointFound)
                            dan107Vector = forwardVector;
                        Vector3 adjustedDan107 = adjustedDan105 + dan107Vector * targetDanLength;

                        if (danDistanceToTarget < 3 * targetDanLength)
                        {
                            constrainPointFound = false;
                            adjustedDan107 = ConstrainDanToCollisionPoints(adjustedDan105, adjustedDan107, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, ref forwardVector);
                            adjustedDan107 = ConstrainDanToCollisionPoints(adjustedDan105, adjustedDan107, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, ref forwardVector);
                        }

                        Vector3 dan109Vector = Vector3.Normalize(adjustedDan107 - adjustedDan105);
                        if (constrainPointFound)
                            dan109Vector = forwardVector;
                        Vector3 adjustedDan109 = adjustedDan107 + dan109Vector * targetDanLength;
                        if (danDistanceToTarget < 4 * targetDanLength)
                        {
                            constrainPointFound = false;
                            adjustedDan109 = ConstrainDanToCollisionPoints(adjustedDan107, adjustedDan109, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.frontCollisionPoints, target.m_collisionOptions.clippingDepth, true, ref forwardVector);
                            adjustedDan109 = ConstrainDanToCollisionPoints(adjustedDan107, adjustedDan109, ref targetDanLength, minDanLength, ref constrainPointFound, target.m_collisionPoints.backCollisionPoints, target.m_collisionOptions.clippingDepth, false, ref forwardVector);
                        }*/
            /*        Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
                    Console.WriteLine($"targetDanLength {targetDanLength}");
                    Console.WriteLine($"lookTarget {lookTarget.x:F2} , {lookTarget.y:F2} , {lookTarget.z:F2}");
                    Console.WriteLine($"adjustedDan101 {adjustedDan101.x:F2} , {adjustedDan101.y:F2} , {adjustedDan101.z:F2}");
                    Console.WriteLine($"adjustedDan103 {adjustedDan103.x:F2} , {adjustedDan103.y:F2} , {adjustedDan103.z:F2}");
                    Console.WriteLine($"adjustedDan105 {adjustedDan105.x:F2} , {adjustedDan105.y:F2} , {adjustedDan105.z:F2}");
                    Console.WriteLine($"adjustedDan107 {adjustedDan107.x:F2} , {adjustedDan107.y:F2} , {adjustedDan107.z:F2}");
                    Console.WriteLine($"adjustedDan109 {adjustedDan109.x:F2} , {adjustedDan109.y:F2} , {adjustedDan109.z:F2}");

                    Console.WriteLine($"dan103Vector {dan103Vector.x:F2} , {dan103Vector.y:F2} , {dan103Vector.z:F2}");
                    Console.WriteLine($"dan105Vector {dan105Vector.x:F2} , {dan105Vector.y:F2} , {dan105Vector.z:F2}");
                    Console.WriteLine($"dan107Vector {dan107Vector.x:F2} , {dan107Vector.y:F2} , {dan107Vector.z:F2}");
                    Console.WriteLine($"dan109Vector {dan109Vector.x:F2} , {dan109Vector.y:F2} , {dan109Vector.z:F2}");
            

            Console.WriteLine($"danDistanceToTarget {danDistanceToTarget}");
            Console.WriteLine($"targetDanLength {targetDanLength}");
            Console.WriteLine($"lookTarget {lookTarget.x:F3} , {lookTarget.y:F3} , {lookTarget.z:F3}");

            i = 1;
            foreach (var point in adjustedDanPoints)
                Console.WriteLine($"dan10{i++} {point.x:F3} , {point.y:F3} , {point.z:F3}");

            Vector3 dan101ForwardVector = Vector3.Normalize(adjustedDanPoints[1] - adjustedDanPoints[0]);
            Quaternion dan101Quaternion = Quaternion.LookRotation(dan101ForwardVector, Vector3.Cross(dan101ForwardVector, m_danPoints.danTop.right));
            m_danPoints.danStart.rotation = dan101Quaternion;

            Vector3 dan103ForwardVector = Vector3.Normalize(adjustedDanPoints[2] - adjustedDanPoints[1]);
            Quaternion dan103Quaternion = Quaternion.LookRotation(dan103ForwardVector, Vector3.Cross(dan103ForwardVector, m_danPoints.danTop.right));
            m_danPoints.danMid[0].SetPositionAndRotation(adjustedDanPoints[1], dan103Quaternion);

            Vector3 dan105ForwardVector = Vector3.Normalize(adjustedDanPoints[3] - adjustedDanPoints[2]);
            Quaternion dan105Quaternion = Quaternion.LookRotation(dan105ForwardVector, Vector3.Cross(dan105ForwardVector, m_danPoints.danTop.right));
            m_danPoints.danMid[1].SetPositionAndRotation(adjustedDanPoints[2], dan105Quaternion);

            Vector3 dan107ForwardVector = Vector3.Normalize(adjustedDanPoints[4] - adjustedDanPoints[3]);
            Quaternion dan107Quaternion = Quaternion.LookRotation(dan107ForwardVector, Vector3.Cross(dan107ForwardVector, m_danPoints.danTop.right));
            m_danPoints.danMid[2].SetPositionAndRotation(adjustedDanPoints[3], dan107Quaternion);

            m_danPoints.danEnd.SetPositionAndRotation(adjustedDanPoints[4], dan107Quaternion);
        }*/

        private Vector3 ConstrainDanToCollisionPoints(Vector3 danStart, Vector3 danEnd, ref float targetLength, float minLength, ref bool constrainPointFound, List<CollisionPoint> collisionPoints, float clippingDepth, bool frontSide)
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

           /*     if (print)
                {
                    if (!frontSide)
             //           Console.WriteLine($"FrontHitPoint{collisionPointPositions.Count}: {collisionPointPositions[collisionPointPositions.Count - 1].x:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].y:F3}, {collisionPointPositions[collisionPointPositions.Count - 1].z:F3}");
             //       else
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

                    adjustedDanEnd = hPlane.ConstrainLineToTwistedPlane(danStart, danEnd, ref targetLength, minLength, ref constainPastNearSide, constainPastFarSide, out constrainPointFound);
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
            float maxDistance;

            Vector3 headCollisionPoint = target.m_collisionPoints.headCollisionPoint.position;

            float danDistanceToLimit = Vector3.Distance(dan101_pos, headCollisionPoint);
            float targetDistanceToLimit = Vector3.Distance(lookTarget, headCollisionPoint);

            if (danDistanceToLimit < targetDistanceToLimit)
            {
                danLength = m_baseDanLength * (1 - m_danOptions.danSoftness);
                maxDistance = danDistanceToLimit;
            }
            else
            {
                if (m_baseDanLength > danDistanceToTarget)
                    danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;
                else
                    danLength = m_baseDanLength;
                maxDistance = danDistanceToTarget + targetDistanceToLimit;
            }

            if (danLength > maxDistance)
                danLength = maxDistance;

            return (dan101_pos + danVector * danLength);
        }

        private Vector3 ConstrainDanToPull(Vector3 dan101_pos, Vector3 danVector, float danDistanceToTarget, out float danLength)
        {
            danLength = m_baseDanLength - (m_baseDanLength - danDistanceToTarget) * m_danOptions.danSoftness;
            float minDanLength = danDistanceToTarget + (danLength * (1 - m_danOptions.telescopeThreshold));

            if (minDanLength > danLength)
                minDanLength = danLength;

            if (m_danOptions.forceTelescope)
                danLength = minDanLength;

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
            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                if (m_danCollider != null && !dynamicBone.m_Colliders.Contains(m_danCollider))
                    dynamicBone.m_Colliders.Add(m_danCollider);
            }
        }

        private void RemoveDanColliders(CollisionAgent target)
        {
            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                if (m_danCollider != null)
                    dynamicBone.m_Colliders.Remove(m_danCollider);
            }
        }

        private void AddFingerColliders(CollisionAgent target)
        {
            foreach (DynamicBone dynamicBone in target.m_kokanDynamicBones)
            {
                if (m_indexCollider != null && !dynamicBone.m_Colliders.Contains(m_indexCollider))
                    dynamicBone.m_Colliders.Add(m_indexCollider);

                if (m_middleCollider!= null && !dynamicBone.m_Colliders.Contains(m_middleCollider))
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

            UnityEngine.Object.Destroy(m_danCollider);
            UnityEngine.Object.Destroy(m_indexCollider);
            UnityEngine.Object.Destroy(m_middleCollider);
            UnityEngine.Object.Destroy(m_ringCollider);
        }
    }
}
