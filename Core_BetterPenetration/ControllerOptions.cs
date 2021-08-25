#if STUDIO
namespace Core_BetterPenetration
{
    class ControllerOptions
    {
        internal enum AutoTarget
        {
            Off,
            Vaginal,
            Anal,
            Oral
        }

        internal AutoTarget danAutoTarget;

        public ControllerOptions(AutoTarget danAutoTarget)
        {
            this.danAutoTarget = danAutoTarget;
        }
    }
}
#endif
