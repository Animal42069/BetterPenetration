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
        public float fingerRadius;
        public float fingerLength;
        public bool useFingerColliders;
        public bool simplifyPenetration;
        public bool simplifyOral;
        public bool squishOralGirth;
        public bool rotateTamaWithShaft;

        public DanOptions(float danRadiusScale, float danLengthScale,
            float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth,
            float fingerRadius, float fingerLength, bool useFingerColliders,
            bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft)
        {
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
        internal enum AutoTarget
        {
            Off,
            Vaginal,
            Anal,
            Oral
        }

        internal AutoTarget danAutoTarget;

        public DanOptions(float danRadiusScale, float danLengthScale, float danLengthSquish, float danGirthSquish, float squishThreshold, AutoTarget danAutoTarget)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
            this.danAutoTarget = danAutoTarget;
        }
#endif
    }
}
