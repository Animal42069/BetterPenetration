#if STUDIO
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

#if AI || HS2
using AIChara;
#endif

namespace Core_BetterPenetration
{
    public class BetterPenetrationController : CharaCustomFunctionController
    {
        internal DanAgent danAgent;
        internal DanOptions danOptions;
        internal CollisionAgent collisionAgent;
        internal CollisionOptions collisionOptions;
        internal ControllerOptions controllerOptions;
        public string danEntryParentName;
        public string danEndParentName;
        public Transform danEntryChild;
        public Transform danEndChild;
        public bool danTargetsValid = false;
        internal bool isOral = false;
        internal bool isAna = false;
        internal bool isKokan = false;
        internal bool cardReloaded = false;
        internal object[] danEntryConstraint;
        internal object[] danEndConstraint;

        public const float DefaultLengthSquish = 0.6f;
        public const float DefaultGirthSquish = 0.2f;
        public const float DefaultSquishThreshold = 0.2f;
        public const float DefaultColliderLengthScale = 1f;
        public const float DefaultColliderRadiusScale = 1f;

#if HS2 || AI
        public const float DefaultMaxKokanPush = 0.075f;
        public const float DefaultMaxKokanPull = 0.15f;
        public const float DefaultMaxAnaPush = 0.0f;
        public const float DefaultMaxAnaPull = 0.2f;
        public const float DefaultMaxOralPush = 0.02f;
        public const float DefaultMaxOralPull = 0.1f;
        public const float DefaultPullRate = 36.0f;
        public const float DefaultOralPullRate = 18.0f;
        public const float DefaultReturnRate = 0.3f;
#else
        public const float DefaultMaxKokanPush = 0.0075f;
        public const float DefaultMaxKokanPull = 0.015f;
        public const float DefaultMaxAnaPush = 0.00f;
        public const float DefaultMaxAnaPull = 0.02f;
        public const float DefaultMaxOralPush = 0.002f;
        public const float DefaultMaxOralPull = 0.01f;
        public const float DefaultPullRate = 3.60f;
        public const float DefaultOralPullRate = 1.80f;
        public const float DefaultReturnRate = 0.03f;
#endif

        internal const ControllerOptions.AutoTarget DefaultDanAutoTarget = ControllerOptions.AutoTarget.Off;

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PluginData data = new PluginData { version = 1 };
            data.data.Add("Enabled", enabled);
            data.data.Add("LengthSquish", danOptions.danLengthSquish);
            data.data.Add("GirthSquish", danOptions.danGirthSquish);
            data.data.Add("SquishThreshold", danOptions.squishThreshold);
            data.data.Add("ColliderRadiusScale", danOptions.danRadiusScale);
            data.data.Add("ColliderLengthScale", danOptions.danLengthScale);
            data.data.Add("DanAutoTarget", controllerOptions.danAutoTarget);
            data.data.Add("MaxPush", collisionOptions.maxOralPush);
            data.data.Add("MaxPull", collisionOptions.maxOralPull);
            data.data.Add("PullRate", collisionOptions.oralPullRate);
            data.data.Add("ReturnRate", collisionOptions.oralReturnRate);
            data.data.Add("EnableBellyBulge", collisionOptions.enableBellyBulge);
            data.data.Add("BellyBulgeScale", collisionOptions.bellyBulgeScale);

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
            ControllerOptions.AutoTarget autoTarget = DefaultDanAutoTarget;
            float maxPush = DefaultMaxOralPush;
            float maxPull = DefaultMaxOralPull; 
            float pullRate = DefaultOralPullRate;
            float returnRate = DefaultReturnRate;
            bool enableBellyBulge = false;
            float bellyBulgeScale = 1.0f;

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
                    autoTarget = (ControllerOptions.AutoTarget)DanAutoTarget;

                if (data.data.TryGetValue("MaxPush", out var MaxPush))
                    maxPush = (float)MaxPush;

                if (data.data.TryGetValue("MaxPull", out var MaxPull))
                    maxPull = (float)MaxPull;

                if (data.data.TryGetValue("PullRate", out var PullRate))
                    pullRate = (float)PullRate;

                if (data.data.TryGetValue("ReturnRate", out var ReturnRate))
                    returnRate = (float)ReturnRate;

                if (data.data.TryGetValue("EnableBellyBulge", out var EnableBellyBulge))
                    enableBellyBulge = (bool)EnableBellyBulge;

                if (data.data.TryGetValue("BellyBulgeScale", out var BellyBulgeScale))
                    bellyBulgeScale = (float)BellyBulgeScale;
            }

            danOptions = new DanOptions(colliderRadiusScale, colliderLengthScale, lengthSquish, girthSquish, squishThreshold, false, 10.0f);
            collisionOptions = new CollisionOptions(maxPush, maxPull, pullRate, returnRate, enableBellyBulge, bellyBulgeScale);
            controllerOptions = new ControllerOptions(autoTarget);
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
            if (controllerOptions.danAutoTarget == ControllerOptions.AutoTarget.Off || !danTargetsValid)
                return;

            string targetTransform = BoneNames.BPKokanTarget;
            if (controllerOptions.danAutoTarget == ControllerOptions.AutoTarget.Oral)
                targetTransform = BoneNames.HeadTarget;
            else if (controllerOptions.danAutoTarget == ControllerOptions.AutoTarget.Anal)
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
                collisionAgent == this.collisionAgent?.m_collisionCharacter)
            {
                return;
            }

            RemoveDanConstraints(plugin);
            RemoveCollisionAgent(isKokan, isAna);

            Transform danEntryParent = autoTarget;
            danEntryParentName = autoTarget.name;
            isKokan = danEntryParentName.Contains("Vagina");
            isAna = danEntryParentName.Contains("Ana");
            isOral = danEntryParentName.Contains("Mouth");

            SetCollisionAgent(collisionAgent, isKokan, isAna, isOral);

#if AI || HS2
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
                $"{danAgent.m_danCharacter.fileParam.fullname} Penis First Target" };

            Transform danEndParent;
            if (danEntryParentName == BoneNames.HeadTarget)
                danEndParent = Tools.GetTransformOfChaControl(collisionAgent, BoneNames.InnerHeadTarget);
            else
                danEndParent = Tools.GetTransformOfChaControl(collisionAgent, BoneNames.InnerTarget);

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
                $"{danAgent.m_danCharacter.fileParam.fullname} Penis Second Target" };

            AddDanConstraints(plugin, danEntryParent, danEndParent);
        }

        protected void LateUpdate()
        {
            if (!danTargetsValid || danEntryChild == null || danEndChild == null)
                return;

            danAgent.SetDanTarget(danEntryChild.position, danEndChild.position, collisionAgent, isKokan, isOral, isAna);
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

            danEntryChild = Tools.GetTransformOfChaControl(ChaControl, BoneNames.BPDanEntryTarget);
            danEndChild = Tools.GetTransformOfChaControl(ChaControl, BoneNames.BPDanEndTarget);

            if (danEntryChild == null || danEndChild == null)
                return;

            if (controllerOptions == null)
                controllerOptions = new ControllerOptions(DefaultDanAutoTarget);

            if (danOptions == null)
                danOptions = new DanOptions(DefaultColliderRadiusScale, DefaultColliderLengthScale, DefaultLengthSquish, DefaultGirthSquish, DefaultSquishThreshold, false, 2.0f);

            if (collisionOptions == null)
                collisionOptions = new CollisionOptions(DefaultMaxOralPush, DefaultMaxOralPull, DefaultOralPullRate, DefaultReturnRate, false, 1.0f);

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

            if (collisionAgent == null || collisionAgent.m_collisionCharacter == null)
                return;

            danAgent.AddTamaColliders(collisionAgent.m_collisionCharacter);
        }

        public void SetCollisionAgent(ChaControl target, bool kokanTarget, bool anaTarget, bool oralTarget)
        {
            if (danAgent == null || controllerOptions == null || !danTargetsValid)
                return;

            if (collisionAgent != null && collisionAgent.m_collisionCharacter != null)
            {
                danAgent.RemoveDanCollidersFromTarget(collisionAgent.m_collisionCharacter, isKokan, isAna);
                danAgent.RemoveTamaColliders();
            }

            collisionAgent = new CollisionAgent(target, collisionOptions);

            isKokan = kokanTarget;
            isAna = anaTarget;
            isOral = oralTarget;

            if (isKokan)
                danAgent.AddDanCollidersToTargetKokan(collisionAgent.m_collisionCharacter, collisionOptions.enableBellyBulge);

            if (isAna)
                danAgent.AddDanCollidersToTargetAna(collisionAgent.m_collisionCharacter);

            danAgent.AddTamaColliders(collisionAgent.m_collisionCharacter, false);
        }
        
        public void SetBellyColliders(bool enable)
        {
            if (!isKokan || collisionAgent == null || collisionAgent.m_collisionCharacter == null)
                return;

            if (enable)
                danAgent.AddDanCollidersToTargeBelly(collisionAgent.m_collisionCharacter);
            else
                danAgent.RemoveDanCollidersFromTargetBelly(collisionAgent.m_collisionCharacter);
        }

        public void RemoveCollisionAgent(bool wasKokan, bool wasAna)
        {
            if (danAgent == null || controllerOptions == null || !danTargetsValid || collisionAgent == null || collisionAgent.m_collisionCharacter == null)
                return;

            danAgent.RemoveDanCollidersFromTarget(collisionAgent.m_collisionCharacter, wasKokan, wasAna);
            danAgent.RemoveTamaColliders();

            collisionAgent = null;
        }

        public void SaveConstraintParams(bool isEntry, object[] constraintParams)
        {
            if (isEntry)
            {
                danEntryConstraint = constraintParams;
                danEntryParentName = constraintParams.GetValue(1) as string;

                isKokan = danEntryParentName.Contains("Vagina");
                isAna = danEntryParentName.Contains("Ana");
                isOral = danEntryParentName.Contains("Mouth");
            }
            else
            {
                danEndConstraint = constraintParams;
                danEndParentName = constraintParams.GetValue(1) as string;
            }
        }

        public void AddDanConstraints(BaseUnityPlugin plugin, Transform danEntryParent = null, Transform danEndParent = null)
        {
            if (!danTargetsValid || collisionAgent == null || collisionAgent.m_collisionCharacter == null)
                return;

            if (danEntryConstraint != null)
            {
                var parentTransform = danEntryParent;
                if (parentTransform == null && !danEntryParentName.IsNullOrEmpty())
                    parentTransform = Tools.GetTransformOfChaControl(collisionAgent.m_collisionCharacter, danEntryParentName);
                if (parentTransform == null && danEntryConstraint.GetValue(1) != null)
                    parentTransform = Tools.GetTransformOfChaControl(collisionAgent.m_collisionCharacter, danEntryConstraint.GetValue(1) as string);

                if (parentTransform != null)
                {
                    danEntryConstraint.SetValue(parentTransform, 1);
                    danEntryConstraint.SetValue(danEntryChild, 2);
                    Traverse.Create(plugin)
                            .Method("AddConstraint", new Type[] { typeof(bool), typeof(Transform), typeof(Transform), typeof(bool), typeof(Vector3), typeof(bool), typeof(Quaternion), typeof(bool), typeof(Vector3), typeof(string) })
                            .GetValue(danEntryConstraint);
                }
            }

            if (danEndConstraint != null)
            {
                var parentTransform = danEndParent;
                if (parentTransform == null && !danEndParentName.IsNullOrEmpty())
                    parentTransform = Tools.GetTransformOfChaControl(collisionAgent.m_collisionCharacter, danEndParentName);
                if (parentTransform == null && danEndConstraint.GetValue(1) != null)
                    parentTransform = Tools.GetTransformOfChaControl(collisionAgent.m_collisionCharacter, danEndConstraint.GetValue(1) as string);

                if (parentTransform != null)
                {
                    danEndConstraint.SetValue(parentTransform, 1);
                    danEndConstraint.SetValue(danEndChild, 2);
                    Traverse.Create(plugin)
                            .Method("AddConstraint", new Type[] { typeof(bool), typeof(Transform), typeof(Transform), typeof(bool), typeof(Vector3), typeof(bool), typeof(Quaternion), typeof(bool), typeof(Vector3), typeof(string) })
                            .GetValue(danEndConstraint);
                }
            }
        }

        public void RemoveDanConstraints(BaseUnityPlugin plugin)
        {
            if (danEntryChild == null || danEndChild == null || collisionAgent == null)
                return;

            danEntryParentName = null;
            danEndParentName = null;
            isKokan = false;
            isAna = false;
            isOral = false;

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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
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
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return (int)DefaultDanAutoTarget;

                return (int)controllerOptions.danAutoTarget;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                controllerOptions.danAutoTarget = (ControllerOptions.AutoTarget)value;
            }
        }

        public float MaxPull
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return DefaultMaxOralPull;

                return collisionOptions.maxOralPull;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                collisionOptions.maxOralPull = value;
            }
        }

        public float MaxPush
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return DefaultMaxOralPush;

                return collisionOptions.maxOralPush;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                collisionOptions.maxKokanPush = value;
                collisionOptions.maxAnaPush = value;
                collisionOptions.maxOralPush = value;
            }
        }

        public float PullRate
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return DefaultPullRate;

                return collisionOptions.oralPullRate;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                collisionOptions.kokanPullRate = value;
				collisionOptions.anaPullRate = value;
                collisionOptions.oralPullRate = value;
            }
        }

        public float ReturnRate
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return DefaultReturnRate;

                return collisionOptions.oralReturnRate;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                collisionOptions.kokanReturnRate = value;
				collisionOptions.anaReturnRate = value;
                collisionOptions.oralReturnRate = value;
            }
        }

        public bool EnableBellyBulge
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return false;

                return collisionOptions.enableBellyBulge;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                collisionOptions.enableBellyBulge = value;
                SetBellyColliders(collisionOptions.enableBellyBulge);
            }
        }

        public float BellyBulgeScale
        {
            get
            {
                if (danAgent == null || controllerOptions == null || !danTargetsValid)
                    return 1.0f;

                return collisionOptions.bellyBulgeScale;
            }
            set
            {
                if (danAgent == null || !danTargetsValid || collisionAgent == null)
                    return;

                collisionOptions.bellyBulgeScale = value;
                collisionAgent.UpdateBellyBones(collisionOptions.bellyBulgeScale);
            }
        }

    }
}
#endif