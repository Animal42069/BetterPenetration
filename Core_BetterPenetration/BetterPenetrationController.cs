#if AI_STUDIO || HS2_STUDIO || KK_STUDIO
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
using ExtensibleSaveFormat;
using System.Linq;
using BepInEx;
using System.Reflection;
using HarmonyLib;
using System.Collections;
using System;

#if AI_STUDIO || HS2_STUDIO
using AIChara;
#endif

namespace Core_BetterPenetration
{
    public class BetterPenetrationController : CharaCustomFunctionController
    {
        internal DanAgent danAgent;
        internal DanOptions danOptions;
        public ChaControl collisionAgent;
        public string danEntryParentName;
        public string danEndParentName;
        public Transform danEntryChild;
        public Transform danEndChild;
        public bool danTargetsValid = false;
        internal bool cardReloaded = false;
        internal object[] danEntryConstraint;
        internal object[] danEndConstraint;

        public const float DefaultLengthSquish = 0.6f;
        public const float DefaultGirthSquish = 0.2f;
        public const float DefaultSquishThreshold = 0.2f;
        public const float DefaultColliderLengthScale = 1f;
        public const float DefaultColliderRadiusScale = 1f;
        internal const DanOptions.AutoTarget DefaultDanAutoTarget = DanOptions.AutoTarget.Off;

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PluginData data = new PluginData { version = 1 };
            data.data.Add("Enabled", enabled);
            data.data.Add("LengthSquish", danOptions.danLengthSquish);
            data.data.Add("GirthSquish", danOptions.danGirthSquish);
            data.data.Add("SquishThreshold", danOptions.squishThreshold);
            data.data.Add("ColliderRadiusScale", danOptions.danRadiusScale);
            data.data.Add("ColliderLengthScale", danOptions.danLengthScale);
            data.data.Add("DanAutoTarget", danOptions.danAutoTarget);

            SetExtendedData(data);
        }
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            danTargetsValid = false;

            PluginData data = GetExtendedData();

            float lengthSquish = DefaultLengthSquish;
            float girthSquish = DefaultGirthSquish;
            float squishThreshold = DefaultSquishThreshold;
            float colliderRadiusScale = DefaultColliderRadiusScale;
            float colliderLengthScale = DefaultColliderLengthScale;
            DanOptions.AutoTarget autoTarget = DefaultDanAutoTarget;

            if (data != null)
            {
                if (data.data.TryGetValue("Enabled", out var Enabled))
                    enabled = (bool)Enabled;

                if (data.data.TryGetValue("LengthSquish", out var LengthSquish))
                    lengthSquish = (float)LengthSquish;

                if (data.data.TryGetValue("GirthSquish", out var GirthSquish))
                    girthSquish = (float)GirthSquish;

                if (data.data.TryGetValue("SquishThreshold", out var SquishThreshold))
                    squishThreshold = (float)SquishThreshold;

                if (data.data.TryGetValue("ColliderRadiusScale", out var ColliderRadiusScale))
                    colliderRadiusScale = (float)ColliderRadiusScale;

                if (data.data.TryGetValue("ColliderLengthScale", out var ColliderLengthScale))
                    colliderLengthScale = (float)ColliderLengthScale;

                if (data.data.TryGetValue("DanAutoTarget", out var DanAutoTarget))
                    autoTarget = (DanOptions.AutoTarget)DanAutoTarget;

            }

            danOptions = new DanOptions(colliderRadiusScale, colliderLengthScale, lengthSquish, girthSquish, squishThreshold, autoTarget);
            cardReloaded = true;

            base.OnReload(currentGameMode, maintainState);
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDestroy()
        {
            ClearDanAgent();
            base.OnDestroy();
        }

        protected override void Update()
        {
            if (cardReloaded)
            {
                InitializeDanAgent();
                cardReloaded = false;
            }

            base.Update();
        }

        internal void CheckAutoTarget(BaseUnityPlugin plugin)
        {
            if (danOptions.danAutoTarget == DanOptions.AutoTarget.Off || !danTargetsValid)
                return;

            string targetTransform = BoneNames.BPKokanTarget;
            if (danOptions.danAutoTarget == DanOptions.AutoTarget.Oral)
                targetTransform = BoneNames.HeadTarget;
            else if (danOptions.danAutoTarget == DanOptions.AutoTarget.Anal)
                targetTransform = BoneNames.AnaTarget;

            var potentialTargets = FindObjectsOfType<Transform>().Where(x => x.name.Equals(targetTransform));
            if (potentialTargets == null)
                return;

            Transform autoTarget = null;
            float currentTargetDistance = danAgent.m_baseDanLength * 2.0f;

            foreach (var potentialTarget in potentialTargets)
            {
                if (potentialTarget.GetComponentInParent<ChaControl>() == danAgent.m_danCharacter)
                    continue;

                var potentialTargetDistance = Vector3.Distance(danAgent.m_danPoints.GetDanEndPosition(), potentialTarget.position);

                if (potentialTargetDistance >= currentTargetDistance)
                    continue;

                autoTarget = potentialTarget;
                currentTargetDistance = potentialTargetDistance;
            }

            if (autoTarget == null)
                return;

            var collisionAgent = autoTarget.GetComponentInParent<ChaControl>();

            if (danEntryConstraint != null && danEntryConstraint.GetValue(1) != null && 
                danEntryConstraint.GetValue(1).ToString() == autoTarget.name &&
                collisionAgent == this.collisionAgent)
            {
                return;
            }

            RemoveDanConstraints(plugin);
            RemoveCollisionAgent();

            Transform danEntryParent = autoTarget;
            danEntryParentName = autoTarget.name;
            SetCollisionAgent(collisionAgent, danEntryParentName == BoneNames.BPKokanTarget);

#if AI_STUDIO || HS2_STUDIO
            Vector3 headOffset = new Vector3(0f, -0.05f, 0.02f);
#else
            Vector3 headOffset = new Vector3(0f, -0.01f, 0f);
#endif

            danEntryConstraint = new object[] { 
                true, 
                danEntryParent, 
                danEntryChild, 
                true, 
                (danEntryParentName == BoneNames.HeadTarget)? headOffset: Vector3.zero, 
                false,
                Quaternion.identity,
                false, 
                Vector3.zero, 
                $"{danAgent.m_danCharacter.fileParam.fullname}'s Penis First Target" };

            Transform danEndParent;
            if (danEntryParentName == BoneNames.HeadTarget)
                danEndParent= collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(BoneNames.InnerHeadTarget)).FirstOrDefault();
            else
                danEndParent = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(BoneNames.InnerTarget)).FirstOrDefault();

            danEndParentName = danEndParent.name;

            danEndConstraint = new object[] { 
                true,
                danEndParent, 
                danEndChild, 
                true, 
                Vector3.zero, 
                false,
                Quaternion.identity, 
                false, 
                Vector3.zero, 
                $"{danAgent.m_danCharacter.fileParam.fullname}'s Penis Second Target" };

            AddDanConstraints(plugin, danEntryParent, danEndParent);
        }

        protected void LateUpdate()
        {
            if (!danTargetsValid || danEntryChild == null || danEndChild == null)
                return;

            danAgent.SetDanTarget(danEntryChild.position, danEndChild.position);
        }

        public void ClearDanAgent()
        {
            danTargetsValid = false;
            danEntryChild = null;
            danEndChild = null;

            if (danAgent == null)
                return;

            danAgent.ClearDanAgent();
            danAgent = null;
        }

        public void InitializeDanAgent()
        {
            ClearDanAgent();

            danEntryChild = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(BoneNames.BPDanEntryTarget));
            danEndChild = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals(BoneNames.BPDanEndTarget));

            if (danEntryChild == null || danEndChild == null)
                return;

            if (danOptions == null)
                danOptions = new DanOptions(DefaultColliderRadiusScale, DefaultColliderLengthScale, DefaultLengthSquish, DefaultGirthSquish, DefaultSquishThreshold, DefaultDanAutoTarget);

            danAgent = new DanAgent(ChaControl, danOptions);
            danTargetsValid = true;

            InitializeTama();
        }

        public void ClearTama()
        {
            if (danAgent == null)
                return;

            danAgent.ClearTama();
       }

        public void InitializeTama()
        {
            if (danAgent == null)
                return;

            danAgent.InitializeTama();

            if (collisionAgent == null)
                return;

            danAgent.AddTamaColliders(collisionAgent);
        }

        public void SetCollisionAgent(ChaControl target, bool kokanTarget)
        {
            if (danAgent == null || danOptions == null || !danTargetsValid)
                return;

            if (collisionAgent != null)
            {
                danAgent.RemoveDanColliders(collisionAgent);
                danAgent.RemoveTamaColliders();
            }

            collisionAgent = target;

            if (kokanTarget)
                danAgent.AddDanColliders(collisionAgent);

            danAgent.AddTamaColliders(collisionAgent, false);
        }
        
        public void RemoveCollisionAgent()
        {
            if (danAgent == null || danOptions == null || !danTargetsValid || collisionAgent == null)
                return;

            danAgent.RemoveDanColliders(collisionAgent);
            danAgent.RemoveTamaColliders();

            collisionAgent = null;
        }

        public void SaveConstraintParams(bool isEntry, object[] constraintParams)
        {
            if (isEntry)
            {
                danEntryConstraint = constraintParams;
                danEntryParentName = constraintParams.GetValue(1) as string;
            }
            else
            {
                danEndConstraint = constraintParams;
                danEndParentName = constraintParams.GetValue(1) as string;
            }
        }

        public void AddDanConstraints(BaseUnityPlugin plugin, Transform danEntryParent = null, Transform danEndParent = null)
        {
            if (!danTargetsValid || collisionAgent == null)
                return;

            if (danEntryConstraint != null)
            {
                var parentTransform = danEntryParent;
                if (parentTransform == null && !danEntryParentName.IsNullOrEmpty())
                    parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEntryParentName).FirstOrDefault();
                if (parentTransform == null && danEntryConstraint.GetValue(1) != null)
                    parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEntryConstraint.GetValue(1) as string).FirstOrDefault();

                if (parentTransform != null)
                {
                    danEntryConstraint.SetValue(parentTransform, 1);
                    danEntryConstraint.SetValue(danEntryChild, 2);
                    plugin.GetType().GetMethod("AddConstraint", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(plugin, danEntryConstraint);
                }
            }

            if (danEndConstraint != null)
            {
                var parentTransform = danEndParent;
                if (parentTransform == null && !danEndParentName.IsNullOrEmpty())
                    parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEndParentName).FirstOrDefault();
                if (parentTransform == null && danEndConstraint.GetValue(1) != null)
                    parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEndConstraint.GetValue(1) as string).FirstOrDefault();

                if (parentTransform != null)
                {
                    danEndConstraint.SetValue(parentTransform, 1);
                    danEndConstraint.SetValue(danEndChild, 2);
                    plugin.GetType().GetMethod("AddConstraint", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(plugin, danEndConstraint);
                }
            }
        }

        public void RemoveDanConstraints(BaseUnityPlugin plugin)
        {
            if (danEntryChild == null || danEndChild == null || collisionAgent == null)
                return;

            danEntryParentName = null;
            danEndParentName = null;

            var pluginTraverse = Traverse.Create(plugin);
            if (pluginTraverse == null)
                return;

            IList constraintsList = pluginTraverse.Field<IList>("_constraints").Value;
            if (constraintsList == null)
                return;

            for (int constraintIndex = constraintsList.Count -1; constraintIndex >= 0; constraintIndex--)
            {
                var constraint = constraintsList[constraintIndex];
                if (constraint == null)
                    continue;

                Traverse constraintTraverse = Traverse.Create(constraint);
                if (constraintTraverse == null)
                    continue;

                Transform childTransform = constraintTraverse.Field<Transform>("childTransform").Value;
                if (childTransform == null || childTransform.GetComponentInParent<ChaControl>() != danAgent.m_danCharacter || (childTransform.name != BoneNames.BPDanEntryTarget && childTransform.name != BoneNames.BPDanEndTarget))
                    continue;

                plugin.GetType().GetMethod("RemoveConstraintAt", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(plugin, new object[] { constraintIndex });
            }
        }

        public bool Enabled
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return false;

                return enabled;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                enabled = value;
            }
        }

        public float DanLengthSquish
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultLengthSquish;

                return danOptions.danLengthSquish;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danLengthSquish = value;
                danAgent.UpdateDanOptions(danOptions);
            }
        }

        public float DanGirthSquish
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultGirthSquish;

                return danOptions.danGirthSquish;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danGirthSquish = value;
                danAgent.UpdateDanOptions(danOptions);
            }
        }

        public float DanSquishThreshold
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultSquishThreshold;

                return danOptions.squishThreshold;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.squishThreshold = value;
                danAgent.UpdateDanOptions(danOptions);
            }
        }

        public float DanColliderRadiusScale
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultColliderRadiusScale;

                return danOptions.danRadiusScale;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danRadiusScale = value;
                danAgent.UpdateDanColliders(danOptions.danRadiusScale, danOptions.danLengthScale);
            }
        }

        public float DanColliderLengthScale
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultColliderLengthScale;

                return danOptions.danLengthScale;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danLengthScale = value;
                danAgent.UpdateDanColliders(danOptions.danRadiusScale, danOptions.danLengthScale);
            }
        }

        public int DanAutoTarget
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return (int)DefaultDanAutoTarget;

                return (int)danOptions.danAutoTarget;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danAutoTarget = (DanOptions.AutoTarget)value;
            }
        }
    }
}
#endif