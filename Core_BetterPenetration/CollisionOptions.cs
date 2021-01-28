using System.Collections.Generic;

namespace Core_BetterPenetration
{
    class CollisionOptions
    {
        public bool useBoundingColliders = false;

        public float kokanForwardOffset = 0;
        public float kokanUpOffset = 0;
        public float headForwardOffset = 0;
        public float headUpOffset = 0;

        public bool kokan_adjust = false;
        public float kokan_adjust_position_z = 0;
        public float kokan_adjust_position_y = 0;
        public float kokan_adjust_rotation_x = 0;
        public float clippingDepth = 0;

        public List<CollidonPointInfo> frontCollisionInfo;
        public List<CollidonPointInfo> backCollisonInfo;

        public CollisionOptions(bool useBoundingColliders, float kokanForwardOffset, float kokanUpOffset, float headForwardOffset, float headUpOffset, bool kokan_adjust,
        float kokan_adjust_position_z, float kokan_adjust_position_y, float kokan_adjust_rotation_x, float clippingDepth, List<CollidonPointInfo> frontInfo, List<CollidonPointInfo> backInfo)
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
