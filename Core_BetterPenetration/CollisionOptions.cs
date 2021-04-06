using System.Collections.Generic;

namespace Core_BetterPenetration
{
    class CollisionOptions
    {
        internal bool useBoundingColliders = false;

        internal float kokanForwardOffset = 0;
        internal float kokanUpOffset = 0;
        internal float headForwardOffset = 0;
        internal float headUpOffset = 0;

        internal bool kokan_adjust = false;
        internal float kokan_adjust_position_z = 0;
        internal float kokan_adjust_position_y = 0;
        internal float kokan_adjust_rotation_x = 0;
        internal float clippingDepth = 0;

        internal List<CollisionPointInfo> frontCollisionInfo;
        internal List<CollisionPointInfo> backCollisonInfo;

        public CollisionOptions(bool useBoundingColliders, float kokanForwardOffset, float kokanUpOffset, float headForwardOffset, float headUpOffset, bool kokan_adjust,
        float kokan_adjust_position_z, float kokan_adjust_position_y, float kokan_adjust_rotation_x, float clippingDepth, List<CollisionPointInfo> frontInfo, List<CollisionPointInfo> backInfo)
        {
            this.useBoundingColliders = useBoundingColliders;

            this.kokanForwardOffset = kokanForwardOffset;
            this.kokanUpOffset = kokanUpOffset;
            this.headForwardOffset = headForwardOffset;
            this.headUpOffset = headUpOffset;

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
