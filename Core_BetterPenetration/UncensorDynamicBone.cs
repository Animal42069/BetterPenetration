namespace Core_BetterPenetration
{
    class UncensorDynamicBone
    {
        internal enum DynamicBoneDirection
        {
            X,
            Z,
            XZ
        };

        internal DynamicBoneDirection direction;
        internal string selfColliderName;
        internal float selfColliderHeight;
        internal float selfColliderRadius;

        public UncensorDynamicBone(DynamicBoneDirection direction)
        {
            this.direction = direction;
            selfColliderName = null;
            selfColliderHeight = 0;
            selfColliderRadius = 0;
        }

        public UncensorDynamicBone(DynamicBoneDirection direction, string collderName, float colliderHeight, float colliderRadius)
        {
            this.direction = direction;
            selfColliderName = collderName;
            selfColliderHeight = colliderHeight;
            selfColliderRadius = colliderRadius;
        }
    }
}
