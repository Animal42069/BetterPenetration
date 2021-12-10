namespace Core_BetterPenetration
{
    class DanOptions
    {
        public float danLengthSquish;
        public float danGirthSquish;
        public float squishThreshold;
        public float danRadiusScale;
        public float danLengthScale;

        public bool simplifyVaginal;
        public bool simplifyOral;
        public bool simplifyAnal;
        public bool squishOralGirth;
        public bool rotateTamaWithShaft;

        public bool limitCorrection;
        public float maxCorrection;

        public DanOptions(float danRadiusScale, float danLengthScale,
            float danLengthSquish, float danGirthSquish, float squishThreshold, bool squishOralGirth,
            bool simplifyVaginal, bool simplifyOral, bool simplifyAnal, bool rotateTamaWithShaft, 
            bool limitCorrection, float maxCorrection)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.simplifyVaginal = simplifyVaginal;
            this.simplifyOral = simplifyOral;
            this.simplifyAnal = simplifyAnal;
            this.squishOralGirth = squishOralGirth;
            this.rotateTamaWithShaft = rotateTamaWithShaft;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
            this.limitCorrection = limitCorrection;
            this.maxCorrection = maxCorrection;
        }

        public DanOptions(float danRadiusScale, float danLengthScale,
            float danLengthSquish, float danGirthSquish, float squishThreshold,
            bool limitCorrection, float maxCorrection)
        {
            this.danLengthSquish = danLengthSquish;
            this.danGirthSquish = danGirthSquish;
            this.squishThreshold = squishThreshold;
            this.danRadiusScale = danRadiusScale;
            this.danLengthScale = danLengthScale;
            simplifyVaginal = true;
            simplifyOral = true;
            simplifyAnal = true;
            squishOralGirth = true;
            rotateTamaWithShaft = true;
            this.limitCorrection = limitCorrection;
            this.maxCorrection = maxCorrection;
        }
    }
}