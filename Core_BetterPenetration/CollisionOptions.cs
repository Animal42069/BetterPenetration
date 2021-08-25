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
        internal bool enableKokanPush = true;
        internal bool useDanAngle = false;
        internal float maxKokanPush = 0.08f;
        internal float maxKokanPull = 0.04f;
        internal float kokanPullRate = 18.0f;
        internal float kokanReturnRate = 0.3f;
        internal bool enableOralPush = true;
        internal float maxOralPush = 0.02f;
        internal float maxOralPull = 0.10f;
        internal float oralPullRate = 18.0f;
        internal float oralReturnRate = 0.3f;

        internal List<CollisionPointInfo> frontCollisionInfo;
        internal List<CollisionPointInfo> backCollisonInfo;

        public CollisionOptions(float kokanOffset, float innerKokanOffset, float mouthOffset, float innerMouthOffset, bool kokan_adjust,
        float kokan_adjust_position_z, float kokan_adjust_position_y, float kokan_adjust_rotation_x, float clippingDepth, List<CollisionPointInfo> frontInfo, List<CollisionPointInfo> backInfo,
        bool enableKokanPush, bool useDanAngle, float maxKokanPush, float maxKokanPull, float kokanPullRate, float kokanReturnRate,
        bool enableOralPush, float maxOralPush, float maxOralPull, float oralPullRate, float oralReturnRate)
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

            this.enableKokanPush = enableKokanPush;
            this.useDanAngle = useDanAngle;
            this.maxKokanPush = maxKokanPush;
            this.maxKokanPull = maxKokanPull;
            this.kokanPullRate = kokanPullRate;
            this.kokanReturnRate = kokanReturnRate;

            this.enableOralPush = enableOralPush;
            this.maxOralPush = maxOralPush;
            this.maxOralPull = maxOralPull;
            this.oralPullRate = oralPullRate;
            this.oralReturnRate = oralReturnRate;

            frontCollisionInfo = frontInfo;
            backCollisonInfo = backInfo;
        }

        public CollisionOptions(bool enablePush, float maxPush, float maxPull, float pullRate, float returnRate)
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

            enableKokanPush = enablePush;
            useDanAngle = true;
            maxKokanPush = maxPush;
            maxKokanPull = maxPull;
            kokanPullRate = pullRate;
            kokanReturnRate = returnRate;

            enableOralPush = enablePush;
            maxOralPush = maxPush;
            maxOralPull = maxPull;
            oralPullRate = pullRate;
            oralReturnRate = returnRate;

            frontCollisionInfo = null;
            backCollisonInfo = null;
        }
    }
}
