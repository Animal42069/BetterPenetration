namespace Core_BetterPenetration
{
    class DanOptions
    {
        public float danLengthSquish;
        public float danGirthSquish;
        public float squishThreshold;
        public float danRadiusScale;
        public float danLengthScale;
#if !AI_STUDIO && !HS2_STUDIO && !KK_STUDIO
        public float danVerticalCenter;
        public float danRadius;
        public float danHeadLength;
        public float fingerRadius;
        public float fingerLength;
        public bool useFingerColliders;
        public bool simplifyPenetration;
        public bool simplifyOral;
        public bool squishOralGirth;
        public bool rotateTamaWithShaft;

        public DanOptions(float danVerticalCenter, float danRadius, float danHeadLength,
            float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth,
            float fingerRadius, float fingerLength, bool useFingerColliders,
            bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft, float danRadiusScale = 1, float danLengthScale = 1)
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
            this.simplifyOral = simplifyOral;
            this.squishOralGirth = squishOralGirth;
            this.rotateTamaWithShaft = rotateTamaWithShaft;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
        }
#else
        public DanOptions(float danRadiusScale, float danLengthScale, float danLengthSquish, float danGirthSquish, float squishThreshold)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
        }
#endif
    }
}
