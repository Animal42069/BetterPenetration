#if AI_STUDIO || HS2_STUDIO
using KKAPI;
using KKAPI.Chara;
using AIChara;
using UnityEngine;
using ExtensibleSaveFormat;
using System.Linq;

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

        public const float DefaultLengthSquish = 0.6f;
        public const float DefaultGirthSquish = 0.25f;
        public const float DefaultSquishThreshold = 0.2f;
        public const float DefaultColliderVertical = -0.03f;
        public const float DefaultColliderLength = 0.35f;
        public const float DefaultColliderRadius = 0.32f;

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PluginData data = new PluginData { version = 1 };
            data.data.Add("Enabled", enabled);
            data.data.Add("LengthSquish", danOptions.danLengthSquish);
            data.data.Add("GirthSquish", danOptions.danGirthSquish);
            data.data.Add("SquishThreshold", danOptions.squishThreshold);
            data.data.Add("ColliderRadius", danOptions.danRadius);
            data.data.Add("ColliderLength", danOptions.danHeadLength);
            data.data.Add("ColliderVertical", danOptions.danVerticalCenter);

            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            PluginData data = GetExtendedData();

            float lengthSquish = DefaultLengthSquish;
            float girthSquish = DefaultGirthSquish;
            float squishThreshold = DefaultSquishThreshold;
            float colliderRadius = DefaultColliderRadius;
            float colliderLength = DefaultColliderLength;
            float colliderVertical = DefaultColliderVertical;

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

                if (data.data.TryGetValue("ColliderRadius", out var ColliderRadius))
                    colliderRadius = (float)ColliderRadius;

                if (data.data.TryGetValue("ColliderLength", out var ColliderLength))
                    colliderLength = (float)ColliderLength;

                if (data.data.TryGetValue("ColliderVertical", out var ColliderVertical))
                    colliderVertical = (float)ColliderVertical;
            }

            danOptions = new DanOptions(colliderVertical, colliderRadius, colliderLength, lengthSquish, girthSquish, squishThreshold);
            InitializeDanAgent();
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

        protected void LateUpdate()
        {
            if (!danTargetsValid)
                return;

            danAgent.SetDanTarget(danEntryTarget.position, danEndTarget.position);
        }

        public void ClearDanAgent()
        {
            danTargetsValid = false;

            if (danAgent != null)
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
                danOptions = new DanOptions(DefaultColliderVertical, DefaultColliderRadius, DefaultColliderLength, DefaultLengthSquish, DefaultGirthSquish, DefaultSquishThreshold);

            danAgent = new DanAgent(ChaControl, danOptions);
            danTargetsValid = true;
        }

        public void SetCollisionAgent(ChaControl target)
        {
            if (danAgent == null || danOptions == null || !danTargetsValid)
                return;

            if (collisionAgent != null)
                danAgent.RemoveDanColliders(collisionAgent);

            collisionAgent = target;
            danAgent.AddDanColliders(collisionAgent);
        }

        public void RemoveCollisionAgent()
        {
            if (danAgent == null || danOptions == null || !danTargetsValid)
                return;

            if (collisionAgent != null)
                danAgent.RemoveDanColliders(collisionAgent);

            collisionAgent = null;
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

        public float DanColliderRadius
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultColliderRadius;

                return danOptions.danRadius;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danRadius = value;
                danAgent.UpdateDanCollider(danOptions);
            }
        }

        public float DanColliderLength
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultColliderLength;

                return danOptions.danHeadLength;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danHeadLength = value;
                danAgent.UpdateDanCollider(danOptions);
            }
        }

        public float DanColliderVertical
        {
            get
            {
                if (danAgent == null || danOptions == null || !danTargetsValid)
                    return DefaultColliderVertical;

                return danOptions.danVerticalCenter;
            }
            set
            {
                if (danAgent == null || !danTargetsValid)
                    return;

                danOptions.danVerticalCenter = value;
                danAgent.UpdateDanCollider(danOptions);
            }
        }
    }
}
#endif