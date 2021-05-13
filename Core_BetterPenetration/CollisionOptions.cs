using System.Collections.Generic;
using UnityEngine;

namespace Core_BetterPenetration
{
    class CollisionOptions
    {
        internal Vector3 kokanOffset = Vector3.zero;
        internal Vector3 innerKokanOffset = Vector3.zero;
        internal Vector3 mouthOffset = Vector3.zero;
        internal Vector3 innerMouthOffset = Vector3.zero;

        internal bool kokan_adjust = false;
        internal float kokan_adjust_position_z = 0;
        internal float kokan_adjust_position_y = 0;
        internal float kokan_adjust_rotation_x = 0;
        internal float clippingDepth = 0;

        internal List<CollisionPointInfo> frontCollisionInfo;
        internal List<CollisionPointInfo> backCollisonInfo;

        public CollisionOptions(Vector3 kokanOffset, Vector3 innerKokanOffset, Vector3 mouthOffset, Vector3 innerMouthOffset, bool kokan_adjust,
        float kokan_adjust_position_z, float kokan_adjust_position_y, float kokan_adjust_rotation_x, float clippingDepth, List<CollisionPointInfo> frontInfo, List<CollisionPointInfo> backInfo)
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

            frontCollisionInfo = frontInfo;
            backCollisonInfo = backInfo;
        }
    }
}
