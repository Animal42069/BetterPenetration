namespace Core_BetterPenetration
{
    class DanOptions
    {
        public float danVerticalCenter;
        public float danRadius;
        public float danHeadLength;
        public float danLengthSquish;
        public float danGirthSquish;
        public float squishThreshold;
        public float fingerRadius;
        public float fingerLength;
        public bool useFingerColliders;
        public bool simplifyPenetration;

        public DanOptions(float danVerticalCenter, float danRadius, float danHeadLength, 
            float danLengthSquish, float danGirthSquish, float squishThreshold,
            float fingerRadius, float fingerLength, bool useFingerColliders, bool simplifyPenetration)
        {
            this.danVerticalCenter = danVerticalCenter;
            this.danRadius = danRadius;
            this.danHeadLength = danHeadLength;
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.fingerRadius = fingerRadius;
            this.fingerLength = fingerLength;
            this.useFingerColliders = useFingerColliders;
            this.simplifyPenetration = simplifyPenetration;
        }
    }
}
