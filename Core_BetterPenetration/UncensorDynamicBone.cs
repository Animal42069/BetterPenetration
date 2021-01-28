namespace Core_BetterPenetration
{
    class UncensorDynamicBone
    {
        public enum DynamicBoneDirection
        {
            X,
            Z,
            XZ
        };

        public string name;
        public DynamicBoneDirection direction;
        public string selfColliderName;
        public float selfColliderHeight;
        public float selfColliderRadius;

        public UncensorDynamicBone(string boneName, DynamicBoneDirection direction)
        {
            name = boneName;
            this.direction = direction;
            selfColliderName = null;
            selfColliderHeight = 0;
            selfColliderRadius = 0;
        }

        public UncensorDynamicBone(string boneName, DynamicBoneDirection direction, string collderName, float colliderHeight, float colliderRadius)
        {
            name = boneName;
            this.direction = direction;
            selfColliderName = collderName;
            selfColliderHeight = colliderHeight;
            selfColliderRadius = colliderRadius;
        }
    }
}
