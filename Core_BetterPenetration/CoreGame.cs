#if !STUDIO
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
#if HS2 || AI
        internal static List<ItemColliderInfo> itemColliderInfo;
        internal static List<DynamicBoneCollider> m_itemColliders = new List<DynamicBoneCollider>();
#endif

        public static void InitializeAgents(List<ChaControl> danCharacterList, List<ChaControl> collisionCharacterList, List<DanOptions> danOptions, List<CollisionOptions> collisionOptions)
        {
            InitializeDanAgents(danCharacterList, danOptions);
            InitializeCollisionAgents(collisionCharacterList, collisionOptions);
#if HS2 || AI
            InitializeItemColliderInfo();
#endif
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

        public static void LookAtDanUpdate(Transform lookAtTransform, string currentMotion, bool topStick, bool changingAnimation, int maleNum, int femaleNum, bool twoDans, bool isInScene)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!changingAnimation)
                collisionAgents[femaleNum].AdjustMissionaryAnimation();

            if (danHasNewTarget[maleNum] && !changingAnimation)
                LookAtDanSetup(lookAtTransform, currentMotion, topStick, maleNum, femaleNum, twoDans, isInScene);

            danAgents[maleNum].SetDanTarget(collisionAgents[femaleNum], twoDans);
        }

        public static void LookAtDanSetup(Transform lookAtTransform, string currentMotion, bool topStick, int maleNum, int femaleNum, bool twoDans, bool isInScene)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!twoDans && danAgents.Count > 1 && danAgents[1] != null)
            {
                danAgents[1].RemoveDanColliders(collisionAgents[femaleNum]);
#if HS2 || AI
                danAgents[1].RemoveMidsectionColliders(collisionAgents[femaleNum].m_collisionCharacter);
#endif
            }

            if (maleNum == 1 && !twoDans)
                return;

            CollisionAgent firstAgent = collisionAgents[femaleNum];
            CollisionAgent secondAgent = null;

            var secondFemaleNum = femaleNum == 0 ? 1 : 0;
            if (collisionAgents.Count > secondFemaleNum && collisionAgents[secondFemaleNum].m_collisionCharacter.visibleAll && collisionAgents[secondFemaleNum].m_collisionCharacter.objTop != null)
                secondAgent = collisionAgents[secondFemaleNum];

            danAgents[maleNum].SetupNewDanTarget(lookAtTransform, currentMotion, topStick, isInScene, firstAgent, secondAgent);
            danHasNewTarget[maleNum] = false;
        }

        public static void ClearKokanBones()
        {
            if (collisionAgents == null || collisionAgents.Count == 0)
                return;

            foreach (var agent in collisionAgents)
            {
                if (agent == null)
                    continue;

                agent.ClearKokanDynamicBones();
            }
        }

        public static void LookAtDanRelease(int maleNum, int femaleNum, bool twoDans)
        {
            if (maleNum >= danAgents.Count || femaleNum >= collisionAgents.Count)
                return;

            if (!twoDans && danAgents.Count > 1 && danAgents[1] != null)
            {
                danAgents[1].RemoveDanColliders(collisionAgents[femaleNum]);
#if HS2 || AI
                danAgents[1].RemoveMidsectionColliders(collisionAgents[femaleNum].m_collisionCharacter);
#endif
            }

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

        public static void UpdateDanOptions(int maleNum, float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth, bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft, bool limitCorrection, float maxCorrection)
        {
            if (maleNum >= danAgents.Count || danAgents[maleNum] == null)
                return;

            danAgents[maleNum].UpdateDanOptions(danLengthSquish, danGirthSquish, squishThreshold, squishOralGirth, simplifyPenetration, simplifyOral, rotateTamaWithShaft, limitCorrection, maxCorrection);
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
                if (dynamicBone == null ||
                    dynamicBone.m_Root == null ||
                    dynamicBone.name == null ||
                    character != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                if (dynamicBone.name.Contains(BoneNames.BPBone) || dynamicBone.name.Contains(BoneNames.BellyBone))
                    dynamicBone.SetWeight(weight);
            }
        }

        internal static void SetupFingerColliders(string animation)
        {
            DanAgent danAgent = danAgents[0];
            CollisionAgent firstAgent = collisionAgents[0];
            CollisionAgent secondAgent = null;
            if (collisionAgents.Count > 1 && collisionAgents[1].m_collisionCharacter.visibleAll && collisionAgents[1].m_collisionCharacter.objTop != null)
                secondAgent = collisionAgents[1];

            ClearFingerColliders(danAgent, firstAgent, secondAgent);
            AddFingerColliders(animation, danAgent, firstAgent, secondAgent);
        }

        internal static void ClearFingerColliders()
        {
            DanAgent danAgent = danAgents[0];
            CollisionAgent firstAgent = collisionAgents[0];
            CollisionAgent secondAgent = null;
            if (collisionAgents.Count > 1)
                secondAgent = collisionAgents[1];

            ClearFingerColliders(danAgent, firstAgent, secondAgent);
        }

        internal static void ClearFingerColliders(DanAgent danAgent, CollisionAgent firstAgent, CollisionAgent secondAgent = null)
        {
            if (firstAgent == null)
                return;

            firstAgent.RemoveFingerColliders(firstAgent);

            if (danAgent != null)
                danAgent.RemoveFingerColliders(firstAgent);

            if (secondAgent == null)
                return;

            firstAgent.RemoveFingerColliders(secondAgent);
            secondAgent.RemoveFingerColliders(firstAgent);

            if (danAgent != null)
                danAgent.RemoveFingerColliders(secondAgent);
        }

        internal static void AddFingerColliders(string animation, DanAgent danAgent, CollisionAgent firstAgent, CollisionAgent secondAgent = null)
        {
            if (animation == null || danAgent == null || firstAgent == null)
                return;

            if (BoneNames.maleFingerAnimationNames.Contains(animation))
            {
                danAgent.AddFingerColliders(firstAgent);

                if (secondAgent != null)
                    danAgent.AddFingerColliders(secondAgent);

                return;
            }

            if (BoneNames.femaleSelfFingerAnimationNames.Contains(animation))
            {
                firstAgent.AddFingerColliders(firstAgent);
                return;
            }

            if (secondAgent == null)
                return;

            if (BoneNames.firstFemaleFingerAnimationNames.Contains(animation))
            {
                firstAgent.AddFingerColliders(secondAgent);
                return;
            }

            if (BoneNames.secondFemaleFingerAnimationNames.Contains(animation))
            {
                secondAgent.AddFingerColliders(firstAgent);
                return;
            }
        }

#if HS2 || AI

        internal static void SetupItemColliders(string animation)
        {
            ClearItemColliders();
            AddItemColliders(animation);
        }

        internal static void AddItemColliders(string animation)
        {
            if (danAgents == null || danAgents[0] == null || collisionAgents == null || collisionAgents[0] == null)
                return;

            m_itemColliders = new List<DynamicBoneCollider>();
            m_itemColliders.AddRange(GetCharacterItemColliders(danAgents[0].m_danCharacter, animation));
            m_itemColliders.AddRange(GetCharacterItemColliders(collisionAgents[0].m_collisionCharacter, animation));

            collisionAgents[0].AddColliders(m_itemColliders);
        }

        internal static List<DynamicBoneCollider> GetCharacterItemColliders(ChaControl character, string animation)
        {
            var itemList = new List<DynamicBoneCollider>();

            if (character == null)
                return itemList;

            foreach (var boneInfo in CoreGame.itemColliderInfo)
            {
                if (!boneInfo.animationNames.Contains(animation))
                    continue;

                foreach (var boneName in boneInfo.itemBones)
                {
                    var bones = character.GetComponentsInChildren<Transform>().Where(bone => bone.name.Equals(boneName));
                    if (bones == null || bones.Count() == 0)
                        break;

                    foreach (var bone in bones)
                    {
                        float radiusScale = Tools.ComputeRadiusScale(bone, boneInfo.direction);
                        float heightScale = Tools.ComputeHeightScale(bone, boneInfo.direction);

                        var collider = Tools.InitializeCollider(bone, boneInfo.colliderRadius * radiusScale, boneInfo.colliderHeight * heightScale, Vector3.zero, boneInfo.direction);
                        itemList.Add(collider);
                    }
                }
            }

            return itemList;
        }

        internal static void ClearItemColliders()
        {
            if (collisionAgents == null || collisionAgents[0] == null)
                return;

            collisionAgents[0].RemoveColliders(m_itemColliders);
        }

        internal static void InitializeItemColliderInfo()
        {
            itemColliderInfo = new List<ItemColliderInfo>
            {
#if HS2
                new ItemColliderInfo(BoneNames.vibeAnimationNames, BoneNames.vibeBones, DynamicBoneColliderBase.Direction.Y, 0.15f, 0.56f),
                new ItemColliderInfo(BoneNames.vibe2AnimationNames, BoneNames.vibe2Bones, DynamicBoneColliderBase.Direction.Y, 0.15f, 0.56f),
                new ItemColliderInfo(BoneNames.dildoAnimationNames, BoneNames.dildoBones, DynamicBoneColliderBase.Direction.Y, 0.172f, 0.576f),
                new ItemColliderInfo(BoneNames.tentacleAnimationNames, BoneNames.tentacleBones, DynamicBoneColliderBase.Direction.X, 0.164f, 0.656f),
#else
                new ItemColliderInfo(BoneNames.vibeAnimationNames, BoneNames.vibeBones, DynamicBoneColliderBase.Direction.Y, 0.1f, 0.5f),
                new ItemColliderInfo(BoneNames.vibe2AnimationNames, BoneNames.vibe2Bones, DynamicBoneColliderBase.Direction.Y, 0.1f, 0.5f),
                new ItemColliderInfo(BoneNames.dildoAnimationNames, BoneNames.dildoBones, DynamicBoneColliderBase.Direction.Y, 0.115f, 0.5f),
                new ItemColliderInfo(BoneNames.tentacleAnimationNames, BoneNames.tentacleBones, DynamicBoneColliderBase.Direction.X, 0.115f, 0.625f),
#endif
            };
        }
#endif
    }
}
#endif