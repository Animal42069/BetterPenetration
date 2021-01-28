namespace Core_BetterPenetration
{
    class DanOptions
    {
        public float danVerticalCenter;
        public float danRadius;
        public float danHeadLength;
        public float danSoftness;
        public float telescopeThreshold;
        public bool forceTelescope;
        public float fingerRadius;
        public float fingerLength;
        public bool useFingerColliders;

        public DanOptions(float danVerticalCenter, float danRadius, float danHeadLength, float danSoftness,
            float telescopeThreshold, bool forceTelescope,
            float fingerRadius, float fingerLength, bool useFingerColliders)
        {
            this.danVerticalCenter = danVerticalCenter;
            this.danRadius = danRadius;
            this.danHeadLength = danHeadLength;
            this.danSoftness = danSoftness;
            this.telescopeThreshold = telescopeThreshold;
            this.forceTelescope = forceTelescope;
            this.fingerRadius = fingerRadius;
            this.fingerLength = fingerLength;
            this.useFingerColliders = useFingerColliders;
        }
    }
}
