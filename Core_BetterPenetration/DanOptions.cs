namespace Core_BetterPenetration
{
    class DanOptions
    {
        public float danLengthSquish;
        public float danGirthSquish;
        public float squishThreshold;
        public float danRadiusScale;
        public float danLengthScale;

        public bool simplifyPenetration;
        public bool simplifyOral;
        public bool squishOralGirth;
        public bool rotateTamaWithShaft;

        public DanOptions(float danRadiusScale, float danLengthScale,
            float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth,
            bool simplifyPenetration, bool simplifyOral, bool rotateTamaWithShaft)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.simplifyPenetration = simplifyPenetration;
            this.simplifyOral = simplifyOral;
            this.squishOralGirth = squishOralGirth;
            this.rotateTamaWithShaft = rotateTamaWithShaft;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
        }

        public DanOptions(float danRadiusScale, float danLengthScale,
            float danLengthSquish, float danGirthSquish, float squishThreshold)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
            simplifyPenetration = true;
            simplifyOral = true;
            squishOralGirth = true;
            rotateTamaWithShaft = true;
        }
    }
}