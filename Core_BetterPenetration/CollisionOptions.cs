using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionOptions
    {
        internal float kokanOffset = 0.0f;
        internal float innerKokanOffset = 0.0f;
        internal float mouthOffset = 0.0f;
        internal float innerMouthOffset = 0.0f;

        internal bool kokan_adjust = false;
        internal float kokan_adjust_position_z = 0;
        internal float kokan_adjust_position_y = 0;
        internal float kokan_adjust_rotation_x = 0;
        internal float clippingDepth = 0;
        internal bool ana_adjust = false;
        internal Vector3 ana_adjust_position = Vector3.zero;
        internal Vector3 ana_adjust_rotation = Vector3.zero;
#if HS2 || AI
        internal bool enableKokanPush = true;
#else
        internal bool enableKokanPush = false;
#endif
        internal float maxKokanPush = 0.08f;
        internal float maxKokanPull = 0.04f;
        internal float kokanPullRate = 18.0f;
        internal float kokanReturnRate = 0.3f;
        internal bool enableOralPush = true;
        internal float maxOralPush = 0.02f;
        internal float maxOralPull = 0.10f;
        internal float oralPullRate = 18.0f;
        internal float oralReturnRate = 0.3f;
#if HS2 || AI
        internal bool enableAnaPush = true;
#else
        internal bool enableAnaPush = false;
#endif
        internal float maxAnaPush = 0.08f;
        internal float maxAnaPull = 0.04f;
        internal float anaPullRate = 18.0f;
        internal float anaReturnRate = 0.3f;

        internal TargetType outer = TargetType.Average;
        internal TargetType inner = TargetType.Average;

        internal List<CollisionPointInfo> frontCollisionInfo;
        internal List<CollisionPointInfo> backCollisonInfo;

        internal bool enableBellyBulge = true;
        internal float bellyBulgeScale = 1.0f;

        public enum TargetType
        {
            Outer = 0,
            Average = 1,
            Inside = 2
        }

        public CollisionOptions(float kokanOffset, float innerKokanOffset, float mouthOffset, float innerMouthOffset, 
            bool kokan_adjust, float kokan_adjust_position_z, float kokan_adjust_position_y, float kokan_adjust_rotation_x,
            bool ana_adjust, Vector3 ana_adjust_position, Vector3 ana_adjust_rotation,
            float clippingDepth, List<CollisionPointInfo> frontInfo, List<CollisionPointInfo> backInfo,
            bool enableKokanPush, float maxKokanPush, float maxKokanPull, float kokanPullRate, float kokanReturnRate,
            bool enableOralPush, float maxOralPush, float maxOralPull, float oralPullRate, float oralReturnRate,
            bool enableAnaPush, float maxAnaPush, float maxAnaPull, float anaPullRate, float anaReturnRate, TargetType outer = TargetType.Average, TargetType inner = TargetType.Inside,
            bool enableBellyBulge = true, float bellyBulgeScale = 1.0f)
        {
            this.kokanOffset = kokanOffset;
            this.innerKokanOffset = innerKokanOffset;
            this.mouthOffset = mouthOffset;
            this.innerMouthOffset = innerMouthOffset;

            this.kokan_adjust = kokan_adjust;
            this.kokan_adjust_position_z = kokan_adjust_position_z;
            this.kokan_adjust_position_y = kokan_adjust_position_y;
            this.kokan_adjust_rotation_x = kokan_adjust_rotation_x;
            this.clippingDepth = clippingDepth;

            this.ana_adjust = ana_adjust;
            this.ana_adjust_position = ana_adjust_position;
            this.ana_adjust_rotation = ana_adjust_rotation;

#if HS2 || AI
            this.enableKokanPush = enableKokanPush;
#else
            this.enableKokanPush = false;
#endif
            this.maxKokanPush = maxKokanPush;
            this.maxKokanPull = maxKokanPull;
            this.kokanPullRate = kokanPullRate;
            this.kokanReturnRate = kokanReturnRate;

            this.enableOralPush = enableOralPush;
            this.maxOralPush = maxOralPush;
            this.maxOralPull = maxOralPull;
            this.oralPullRate = oralPullRate;
            this.oralReturnRate = oralReturnRate;

            this.enableAnaPush = enableAnaPush;
            this.maxAnaPush = maxAnaPush;
            this.maxAnaPull = maxAnaPull;
            this.anaPullRate = anaPullRate;
            this.anaReturnRate = anaReturnRate;

            frontCollisionInfo = frontInfo;
            backCollisonInfo = backInfo;

            this.outer = outer;
            this.inner = inner;

            this.enableBellyBulge = enableBellyBulge;
            this.bellyBulgeScale = bellyBulgeScale;
        }

#if STUDIO
        public CollisionOptions(float maxPush, float maxPull, float pullRate, float returnRate, bool enableBellyBulge = true, float bellyBulgeScale = 1.0f)
        {
            kokanOffset = 0.0f;
            innerKokanOffset = 0.0f;
            mouthOffset = 0.0f;
            innerMouthOffset = 0.0f;

            kokan_adjust = false;
            kokan_adjust_position_z = 0;
            kokan_adjust_position_y = 0;
            kokan_adjust_rotation_x = 0;
            clippingDepth = 0;
            ana_adjust = false;
            ana_adjust_position = Vector3.zero;
            ana_adjust_rotation = Vector3.zero;

#if HS2 || AI
            enableKokanPush = true;
            enableOralPush = true;
            enableAnaPush = true;
#else
            enableKokanPush = false;
            enableOralPush = true;
            enableAnaPush = false;
#endif
            maxKokanPush = maxPush;
            maxKokanPull = maxPull;
            kokanPullRate = pullRate;
            kokanReturnRate = returnRate;

            maxOralPush = maxPush;
            maxOralPull = maxPull;
            oralPullRate = pullRate;
            oralReturnRate = returnRate;

            maxAnaPush = maxPush;
            maxAnaPull = maxPull;
            anaPullRate = pullRate;
            anaReturnRate = returnRate;

            frontCollisionInfo = null;
            backCollisonInfo = null;

            this.enableBellyBulge = enableBellyBulge;
            this.bellyBulgeScale = bellyBulgeScale;
        }
#endif
    }
}
