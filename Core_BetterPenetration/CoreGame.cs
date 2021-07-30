#if !HS2_STUDIO && !AI_STUDIO && !KK_STUDIO
using System.Collections.Generic;
using UnityEngine;
using System;

#if HS2 || AI
using AIChara;
#endif

namespace Core_BetterPenetration
{
    class CoreGame
    {
        internal static List<DanAgent> danAgents;
        internal static List<CollisionAgent> collisionAgents;
        internal static List<bool> danHasNewTarget;

        public static void InitializeAgents(List<ChaControl> danCharacterList, List<ChaControl> collisionCharacterList, List<DanOptions> danOptions, List<CollisionOptions> collisionOptions)
        {
            InitializeDanAgents(danCharacterList, danOptions);
            InitializeCollisionAgents(collisionCharacterList, collisionOptions);
        }

        public static void InitializeDanAgents(List<ChaControl> danCharacterList, List<DanOptions> danOptions)
        {
            danAgents = new List<DanAgent>();
            danHasNewTarget = new List<bool>();

            int characterNum = 0;
            foreach (var character in danCharacterList)
            {
                if (character == null)
                    continue;

                danAgents.Add(new DanAgent(character, danOptions[characterNum]));
                danHasNewTarget.Add(false);
                characterNum++;
            }
        }

        public static void ClearDanAgents()
        {
            if (danAgents == null)
                return;

            foreach (var agent in danAgents)
                agent.ClearDanAgent();
        }

        internal static void ClearCollisionAgents()
        {
            if (collisionAgents == null)
                return;

            foreach (var collisionAgent in collisionAgents)
                collisionAgent.ClearColliders();
        }

        public static void InitializeCollisionAgents(List<ChaControl> collisionCharacterList, List<CollisionOptions> collisionOptions)
        {
            collisionAgents = new List<CollisionAgent>();

            int characterNum = 0;
            foreach (var character in collisionCharacterList)
            {
                if (character == null)
                    continue;

                collisionAgents.Add(new CollisionAgent(character, collisionOptions[characterNum++]));
            }
        }

        public static void LookAtDanUpdate(Transform lookAtTransform, string currentMotion, bool topStick, bool changingAnimation, int maleNum, int femaleNum)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!changingAnimation)
                collisionAgents[femaleNum].AdjustMissionaryAnimation();

            if (danHasNewTarget[maleNum] && !changingAnimation)
            {
                if (collisionAgents.Count > 1 && collisionAgents[1].m_collisionCharacter.visibleAll && collisionAgents[1].m_collisionCharacter.objTop != null)
                {
                    var secondTarget = 1 - femaleNum;
                    if (secondTarget < 0)
                        secondTarget = 0;

                    danAgents[maleNum].SetupNewDanTarget(lookAtTransform, currentMotion, topStick, collisionAgents[femaleNum], collisionAgents[secondTarget]);
                }
                else
                {
                    danAgents[maleNum].SetupNewDanTarget(lookAtTransform, currentMotion, topStick, collisionAgents[femaleNum]);
                }

                danHasNewTarget[maleNum] = false;
            }

            danAgents[maleNum].SetDanTarget(collisionAgents[femaleNum]);
        }

        public static void LookAtDanSetup(Transform lookAtTransform, string currentMotion, bool topStick, int maleNum, int femaleNum, bool twoDans)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!twoDans && danAgents.Count > 1 && danAgents[1] != null)
                danAgents[1].RemoveColliders(collisionAgents[femaleNum]);

            if (maleNum == 1 && !twoDans)
                return;

            if (collisionAgents.Count > 1 && collisionAgents[1].m_collisionCharacter.visibleAll && collisionAgents[1].m_collisionCharacter.objTop != null)
            {
                var secondTarget = 1 - femaleNum;
                if (secondTarget < 0)
                    secondTarget = 0;

                danAgents[maleNum].SetupNewDanTarget(lookAtTransform, currentMotion, topStick, collisionAgents[femaleNum], collisionAgents[secondTarget]);
            }
            else
            {
                danAgents[maleNum].SetupNewDanTarget(lookAtTransform, currentMotion, topStick, collisionAgents[femaleNum]);
            }
        }

        public static void LookAtDanRelease(int maleNum, int femaleNum, bool twoDans)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!twoDans && danAgents.Count > 1 && danAgents[1] != null)
                danAgents[1].RemoveColliders(collisionAgents[femaleNum]);

            if (maleNum == 1 && !twoDans)
                return;

            if (collisionAgents.Count > 1 && collisionAgents[1].m_collisionCharacter.visibleAll && collisionAgents[1].m_collisionCharacter.objTop != null)
            {
                var secondTarget = 1 - femaleNum;
                if (secondTarget < 0)
                    secondTarget = 0;

                danAgents[maleNum].ClearDanTarget(collisionAgents[femaleNum], collisionAgents[secondTarget]);
            }
            else
            {
                danAgents[maleNum].ClearDanTarget(collisionAgents[femaleNum]);
            }
        }

        public static void OnChangeAnimation(string newAnimationFile)
        {
            foreach (var socketAgent in collisionAgents)
                socketAgent.adjustFAnimation = false;

            SetDansHaveNewTarget(true);

            if (collisionAgents == null || collisionAgents[0] == null)
                return;

            collisionAgents[0].CheckForAdjustment(newAnimationFile);
        }

        public static void ResetParticles()
        {
            foreach (var agent in danAgents)
                agent.ResetTamaParticles();

            foreach (var agent in collisionAgents)
                agent.ResetKokanParticles();
        }

        public static void EnableParticles(bool enable)
        {
            foreach (var agent in collisionAgents)
                agent.EnableKokanParticles(enable);
        }

        public static void SetDansHaveNewTarget(bool set)
        {
            for (int index = 0; index < danHasNewTarget.Count; index++)
                danHasNewTarget[index] = set;
        }

        public static void UpdateDanCollider(int maleNum, float danRadiusScale, float danLengthScale)
        {
            if (maleNum >= danAgents.Count || danAgents[maleNum] == null)
                return;

            danAgents[maleNum].UpdateDanColliders(danRadiusScale, danLengthScale);
        }

        public static void UpdateFingerColliders(int maleNum, float fingerRadius, float fingerLength)
        {
            if (maleNum >= danAgents.Count || danAgents[maleNum] == null)
                return;

            danAgents[maleNum].UpdateFingerColliders(fingerRadius, fingerLength);
        }

        public static void UpdateDanOptions(int maleNum, float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth, bool useFingerColliders, bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft)
        {
            if (maleNum >= danAgents.Count || danAgents[maleNum] == null)
                return;

            danAgents[maleNum].UpdateDanOptions(danLengthSquish, danGirthSquish, squishThreshold, squishOralGirth, useFingerColliders, simplifyPenetration, simplifyOral, rotateTamaWithShaft);
        }

        public static void UpdateCollisionOptions(int femaleNum, CollisionOptions options)
        {
            if (femaleNum >= collisionAgents.Count || collisionAgents[femaleNum] == null)
                return;

            collisionAgents[femaleNum].UpdateCollisionOptions(options);
        }

        public static void OnEndScene()
        {
            ClearDanAgents();
            ClearCollisionAgents();
            danAgents = null;
            collisionAgents = null;
            danHasNewTarget = null;
        }

        public static void RemoveCollidersFromCoordinate(ChaControl character)
        {
            var dynamicBones = character.GetComponentsInChildren<DynamicBone>(true);

            if (dynamicBones == null)
                return;

            foreach (var dynamicBone in dynamicBones)
            {
                if (dynamicBone == null || 
                    dynamicBone.m_Colliders == null || 
                    (dynamicBone.name != null && (dynamicBone.name.Contains("Vagina") || dynamicBone.name.Contains("cm_J_dan"))))
                    continue;

                for (int collider = 0; collider < dynamicBone.m_Colliders.Count; collider++)
                {
                    if (dynamicBone.m_Colliders[collider] != null && 
                        dynamicBone.m_Colliders[collider].name != null && 
                        (dynamicBone.m_Colliders[collider].name.Contains("Vagina") || dynamicBone.m_Colliders[collider].name.Contains("cm_J_dan")))
                        dynamicBone.m_Colliders.RemoveAt(collider);
                }
            }
        }

        internal static void SetAgentsBPBoneWeights(float weight)
        {
            SetDanAgentsBPBoneWeights(weight);
            SetCollisionAgentsBPBoneWeights(weight);
        }

        internal static void SetDanAgentsBPBoneWeights(float weight)
        {
            if (danAgents == null)
                return;

            foreach (var agent in danAgents)
            {
                if (agent?.m_danCharacter == null)
                    continue;

                SetBPBoneWeights(agent.m_danCharacter, weight);
            }
        }

        internal static void SetCollisionAgentsBPBoneWeights(float weight)
        {
            if (collisionAgents == null)
                return;

            foreach (var agent in collisionAgents)
            {
                if (agent?.m_collisionCharacter == null)
                    continue;

                SetBPBoneWeights(agent.m_collisionCharacter, weight);
            }
        }

        internal static void SetBPBoneWeights(ChaControl character, float weight)
        {
            var dynamicBones = character.GetComponentsInChildren<DynamicBone>(true);

            if (dynamicBones == null)
                return;

            foreach (var dynamicBone in dynamicBones)
            {
                if (dynamicBone == null || dynamicBone.m_Root == null || dynamicBone.name == null || !dynamicBone.name.Contains(BoneNames.BPBone))
                        continue;

                dynamicBone.SetWeight(weight);
            }
        }
    }
}
#endif