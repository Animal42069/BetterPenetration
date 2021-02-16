using System.Collections.Generic;

namespace Core_BetterPenetration
{
#if HS2 || AI
    static class LookTargets
    {
        internal const string HeadTarget = "k_f_head_00";
        internal const string KokanTarget = "k_f_kokan_00";
        internal const string BPKokanTarget = "cf_J_Vagina_root";
        internal const string AnaTarget = "k_f_ana_00";
        internal const string InnerTarget = "cf_J_Kosi01";
        internal const string InnerHeadTarget = "cf_J_Head";
    }

    static class BoneNames
    {
        internal const string KokanBone = "cf_J_Kokan";
        internal const string DanBase = "cm_J_dan101_00";
        internal const string DanMid0 = "cm_J_dan103_00";
        internal const string DanMid1 = "cm_J_dan105_00";
        internal const string DanMid2 = "cm_J_dan107_00";
        internal const string DanHead = "cm_J_dan109_00";
        internal const string DanTop = "cm_J_dan_f_top";
        internal const string IndexFinger = "cf_J_Hand_Index03_R";
        internal const string MiddleFinger = "cf_J_Hand_Middle03_R";
        internal const string RingFinger = "cf_J_Hand_Ring03_R";
        internal const string BPBone = "cf_J_Vagina";

        internal static readonly List<string> DanBones = new List<string> { DanBase, DanMid0, DanMid1, DanMid2, DanHead };
        internal static readonly List<string> frontCollisionList = new List<string> { LookTargets.KokanTarget, "cf_J_sk_00_02", "N_Waist_f", "k_f_spine03_03" };
        internal static readonly List<string> backCollisionList = new List<string> { LookTargets.AnaTarget, "N_Waist_b", "N_Back" };
        internal static readonly List<string> animationAdjustmentList = new List<string> { "ais_f_00", "ais_f_01", "ais_f_12", "ais_f_19", "ais_f_20" };

        internal static readonly Dictionary<string, UncensorDynamicBone> uncensorBoneList = new Dictionary<string, UncensorDynamicBone> {
                { "cf_J_Vagina_Pivot_B", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_B", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_F", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_F", 0.19f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_Inner_F", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_Inner_F", 0.34f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_L.005", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_L.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_R.005", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_R.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Sides", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.X) } };
    }
#endif

#if AI_STUDIO || HS2_STUDIO
    static class BoneNames
    {
        internal static readonly string BPBone = "cf_J_Vagina";
        internal static readonly string BPKokanTarget = "cf_J_Vagina_root";
        internal static readonly string BPDanEntryTarget = "k_f_dan_entry";
        internal static readonly string DanBase = "cm_J_dan101_00";
        internal static readonly string DanMid0 = "cm_J_dan103_00";
        internal static readonly string DanMid1 = "cm_J_dan105_00";
        internal static readonly string DanMid2 = "cm_J_dan107_00";
        internal static readonly string DanHead = "cm_J_dan109_00";
        internal static readonly string DanTop = "cm_J_dan_f_top";

        internal static readonly List<string> DanBones = new List<string>{ DanBase, DanMid0, DanMid1, DanMid2, DanHead };
    }
#endif

#if KK
    static class LookTargets
    {
        internal const string HeadTarget = "k_f_head_00";
        internal const string KokanTarget = "k_f_kokan_00";
        internal const string BPKokanTarget = "cf_J_Vagina_root";
        internal const string AnaTarget = "k_f_ana_00";
        internal const string InnerTarget = "cf_J_Kosi01";
        internal const string InnerHeadTarget = "cf_J_Head";
    }

    static class BoneNames
    {
        internal const string KokanBone = "cf_j_kokan";
        internal const string DanBase = "cm_J_dan101_00";
        internal const string DanMid0 = "cm_J_dan103_00";
        internal const string DanMid1 = "cm_J_dan105_00";
        internal const string DanMid2 = "cm_J_dan107_00";
        internal const string DanHead = "cm_J_dan109_00";
        internal const string DanTop = "cm_J_dan_f_top";
        internal const string IndexFinger = "cf_j_index03_R";
        internal const string MiddleFinger = "cf_j_middle03_R";
        internal const string RingFinger = "cf_j_ring03_R";
        internal const string BPBone = "cf_J_Vagina";

        internal static readonly List<string> DanBones = new List<string> { DanBase, DanMid0, DanMid1, DanMid2, DanHead };
        internal static readonly List<string> frontCollisionList = new List<string> { LookTargets.KokanTarget, "a_n_waist_f", "a_n_bust_f" };
        internal static readonly List<string> backCollisionList = new List<string> { LookTargets.AnaTarget, "a_n_waist_b", "a_n_back" };
        internal static readonly List<string> animationAdjustmentList = new List<string> { "ais_f_00", "ais_f_01", "ais_f_12", "ais_f_19", "ais_f_20" };

        internal static readonly Dictionary<string, UncensorDynamicBone> uncensorBoneList = new Dictionary<string, UncensorDynamicBone> {
                { "cf_J_Vagina_Pivot_B", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_B", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_F", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_F", 0.19f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_Inner_F", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.Z, "cf_J_Vagina_Collider_Inner_F", 0.34f, 0.0011f) } ,
                { "cf_J_Vagina_Pivot_L.005", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_L.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Pivot_R.005", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.XZ, "cf_J_Vagina_Collider_R.005", 0.39f, 0.0021f) } ,
                { "cf_J_Vagina_Sides", new UncensorDynamicBone(UncensorDynamicBone.DynamicBoneDirection.X) } };
    }

#endif
}
