using System.Collections.Generic;

namespace Core_BetterPenetration
{
#if HS2 || AI
    static class LookTargets
    {
        public const string HeadTarget = "k_f_head_00";
        public const string KokanTarget = "k_f_kokan_00";
        public const string BPKokanTarget = "cf_J_Vagina_root";
        public const string AnaTarget = "k_f_ana_00";
    }

    static class BoneNames
    {
        public const string headLimit = "cf_J_Head";
        public const string KokanBone = "cf_J_Kokan";
        public const string DanBase = "cm_J_dan101_00";
        public const string DanMid0 = "cm_J_dan103_00";
        public const string DanMid1 = "cm_J_dan105_00";
        public const string DanMid2 = "cm_J_dan107_00";
        public const string DanHead = "cm_J_dan109_00";
        public const string DanTop = "cm_J_dan_f_top";
        public const string IndexFinger = "cf_J_Hand_Index03_R";
        public const string MiddleFinger = "cf_J_Hand_Middle03_R";
        public const string RingFinger = "cf_J_Hand_Ring03_R";
        public const string BPBone = "cf_J_Vagina";

        public static readonly List<string> frontCollisionList = new List<string> { LookTargets.KokanTarget, "cf_J_sk_00_02", "N_Waist_f", "k_f_spine03_03" };
        public static readonly List<string> backCollisionList = new List<string> { LookTargets.AnaTarget, "cf_J_sk_04_02", "N_Waist_b", "N_Back" };

        public static readonly List<string> animationAdjustmentList = new List<string> { "ais_f_00", "ais_f_01", "ais_f_12", "ais_f_19", "ais_f_20" };

        public static readonly Dictionary<string, UncensorDynamicBone> uncensorBoneList = new Dictionary<string, UncensorDynamicBone> {
                { "cf_J_Vagina_Pivot_B", new UncensorDynamicBone("cf_J_Vagina_Pivot_B", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_B", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_F", new UncensorDynamicBone("cf_J_Vagina_Pivot_F", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_F", 0.19f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_Inner_F", new UncensorDynamicBone("cf_J_Vagina_Pivot_Inner_F", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_Inner_F", 0.34f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_L.005", new UncensorDynamicBone("cf_J_Vagina_Pivot_L.005", UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_L.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_R.005", new UncensorDynamicBone("cf_J_Vagina_Pivot_R.005", UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_R.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Sides", new UncensorDynamicBone("cf_J_Vagina_Sides", UncensorDynamicBone.DynamicBoneDirection.X) } };
    }
#endif

#if KK
    static class LookTargets
    {
        public const string HeadTarget = "k_f_head_00";
        public const string KokanTarget = "k_f_kokan_00";
        public const string BPKokanTarget = "cf_J_Vagina_root";
        public const string AnaTarget = "k_f_ana_00";
    }

    static class BoneNames
    {
        public const string headLimit = "cf_j_head";
        public const string KokanBone = "cf_j_kokan";
        public const string DanBase = "cm_J_dan101_00";
        public const string DanMid0 = "cm_J_dan103_00";
        public const string DanMid1 = "cm_J_dan105_00";
        public const string DanMid2 = "cm_J_dan107_00";
        public const string DanHead = "cm_J_dan109_00";
        public const string DanTop = "cm_J_dan_f_top";
        public const string IndexFinger = "cf_j_index03_R";
        public const string MiddleFinger = "cf_j_middle03_R";
        public const string RingFinger = "cf_j_ring03_R";
        public const string BPBone = "cf_J_Vagina";

        public static readonly List<string> frontCollisionList = new List<string> { LookTargets.KokanTarget, "a_n_waist_f", "a_n_bust_f" };
        public static readonly List<string> backCollisionList = new List<string> { LookTargets.AnaTarget, "a_n_waist_b", "a_n_back" };
        public static readonly List<string> animationAdjustmentList = new List<string> { "ais_f_00", "ais_f_01", "ais_f_12", "ais_f_19", "ais_f_20" };

        public static readonly Dictionary<string, UncensorDynamicBone> uncensorBoneList = new Dictionary<string, UncensorDynamicBone> {
                { "cf_J_Vagina_Pivot_B", new UncensorDynamicBone("cf_J_Vagina_Pivot_B", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_B", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_F", new UncensorDynamicBone("cf_J_Vagina_Pivot_F", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_F", 0.19f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_Inner_F", new UncensorDynamicBone("cf_J_Vagina_Pivot_Inner_F", UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_Inner_F", 0.34f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_L.005", new UncensorDynamicBone("cf_J_Vagina_Pivot_L.005", UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_L.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_R.005", new UncensorDynamicBone("cf_J_Vagina_Pivot_R.005", UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_R.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Sides", new UncensorDynamicBone("cf_J_Vagina_Sides", UncensorDynamicBone.DynamicBoneDirection.X) } };
    }

#endif
}
