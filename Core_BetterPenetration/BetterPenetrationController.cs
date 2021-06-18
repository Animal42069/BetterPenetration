#if AI_STUDIO || HS2_STUDIO || KK_STUDIO
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
using ExtensibleSaveFormat;
using System.Linq;
using BepInEx;
using System.Reflection;
using System;

#if AI_STUDIO || HS2_STUDIO
using AIChara;
#endif

namespace Core_BetterPenetration
{
    public class BetterPenetrationController : CharaCustomFunctionController
    {
        private DanAgent danAgent;
        private DanOptions danOptions;
        public ChaControl collisionAgent;
        public Transform danEntryTarget;
        public Transform danEndTarget;
        public bool danTargetsValid = false;
        private bool cardReloaded = false;
        internal object[] danEntryConstraint;
        internal object[] danEndConstraint;

        public const float DefaultLengthSquish = 0.6f;
        public const float DefaultGirthSquish = 0.2f;
        public const float DefaultSquishThreshold = 0.2f;
        public const float DefaultColliderLengthScale = 1f;
        public const float DefaultColliderRadiusScale = 1f;

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PluginData data = new PluginData { version = 1 };
            data.data.Add("Enabled", enabled);
            data.data.Add("LengthSquish", danOptions.danLengthSquish);
            data.data.Add("GirthSquish", danOptions.danGirthSquish);
            data.data.Add("SquishThreshold", danOptions.squishThreshold);
            data.data.Add("ColliderRadiusScale", danOptions.danRadiusScale);
            data.data.Add("ColliderLengthScale", danOptions.danLengthScale);

            SetExtendedData(data);
        }
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            PluginData data = GetExtendedData();

            float lengthSquish = DefaultLengthSquish;
            float girthSquish = DefaultGirthSquish;
            float squishThreshold = DefaultSquishThreshold;
            float colliderRadiusScale = DefaultColliderRadiusScale;
            float colliderLengthScale = DefaultColliderLengthScale;

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

            }

            danOptions = new DanOptions(colliderRadiusScale, colliderLengthScale, lengthSquish, girthSquish, squishThreshold);
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

        protected void LateUpdate()
        {
            if (!danTargetsValid || danEntryTarget == null || danEndTarget == null)
                return;

            danAgent.SetDanTarget(danEntryTarget.position, danEndTarget.position);
        }

        public void ClearDanAgent()
        {
            danTargetsValid = false;

            if (danAgent == null)
                return;

            danAgent.ClearDanAgent();
            danAgent = null;
        }

        public void InitializeDanAgent()
        {
            ClearDanAgent();

            danEntryTarget = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals("k_f_dan_entry"));
            danEndTarget = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Equals("k_f_dan_end"));

            if (danEntryTarget == null || danEndTarget == null)
                return;

            if (danOptions == null)
                danOptions = new DanOptions(DefaultColliderRadiusScale, DefaultColliderLengthScale, DefaultLengthSquish, DefaultGirthSquish, DefaultSquishThreshold);

            danAgent = new DanAgent(ChaControl, danOptions);
            danTargetsValid = true;
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
                danEntryConstraint = constraintParams;
            else
                danEndConstraint = constraintParams;
        }

        public void AddDanConstraints(BaseUnityPlugin plugin)
        {
            if (danEntryTarget == null || danEndTarget == null || collisionAgent == null)
                return;

            if (danEntryConstraint != null && danEntryConstraint.GetValue(1) != null)
            {
                var parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEntryConstraint.GetValue(1) as string).FirstOrDefault();

                if (parentTransform != null)
                {
                    danEntryConstraint.SetValue(parentTransform, 1);
                    danEntryConstraint.SetValue(danEntryTarget, 2);
                    plugin.GetType().GetMethod("AddConstraint", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(plugin, danEntryConstraint);
                }
            }

            if (danEndConstraint != null && danEndConstraint.GetValue(1) != null)
            {
                var parentTransform = collisionAgent.GetComponentsInChildren<Transform>().Where(x => x.name == danEndConstraint.GetValue(1) as string).FirstOrDefault();

                if (parentTransform != null)
                {
                    danEndConstraint.SetValue(parentTransform, 1);
                    danEndConstraint.SetValue(danEndTarget, 2);
                    plugin.GetType().GetMethod("AddConstraint", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(plugin, danEndConstraint);
                }
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
    }
}
#endif