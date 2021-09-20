#if AI || HS2
using System.Collections.Generic;

namespace Core_BetterPenetration
{
    class ItemColliderInfo
    {
        internal List<string> animationNames;
        internal List<string> itemBones;
        internal DynamicBoneColliderBase.Direction direction;
        internal float colliderRadius;
        internal float colliderHeight;

        public ItemColliderInfo(List<string> animationNames, List<string> itemBones, DynamicBoneColliderBase.Direction direction, float colliderRadius, float colliderHeight)
        {
            this.animationNames = animationNames;
            this.itemBones = itemBones;
            this.direction = direction;
            this.colliderRadius = colliderRadius;
            this.colliderHeight = colliderHeight;
        }
    }
}
#endif