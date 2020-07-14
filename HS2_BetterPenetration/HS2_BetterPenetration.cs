using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace HS2_BetterPenetration
{

    [BepInPlugin("animal42069.HS2betterpenetration", "HS2 Better Penetration", VERSION)]
    [BepInProcess("HoneySelect2")]
    public class HS2_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.0.0.0";

        private static Harmony harmony;

   //     private static ConfigEntry<float> _dan109Length;
        private static ConfigEntry<float>[] _dan_length = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_girth = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_sack_size = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_softness = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_collider_headlength = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_collider_radius = new ConfigEntry<float>[2];

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static ConfigEntry<bool> _use_telescope_method;
        private static List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;
        private static bool b2MAnimation;

        public static AIChara.ChaControl[] fem_list;
        public static AIChara.ChaControl[] male_list;
        public static List<DynamicBone>[] kokanBones = new List<DynamicBone>[2];
        public static DynamicBoneCollider[] danCollider = new DynamicBoneCollider[2];

        private static bool[] bDansFound = new bool[2] { false, false };
        private static DanPoints[] danPoints;
        private static bool[] bDanPenetration = new bool[2] { false, false };
        private static Transform[] referenceLookAtTarget;

        private static bool[] bHPointsFound = new bool[2] { false, false };
        private static int[] targetF = new int[2] { 0, 0 };
        private static ConstrainPoints[] constrainPoints;
        private static H_Lookat_dan lookat_Dan;

        private static readonly float[] frontOffsets = { -0.08f, -0.15f, -0.08f, -0.65f };
        private static readonly float[] backOffsets = { 0.1f, 0.05f, 0.0f, 0.0f };
        private static readonly string[] frontHPointsList = { "cf_J_sk_00_02", "k_f_kosi03_03", "N_Waist_f", "k_f_spine03_03" };
        private static readonly string[] backHPointsList = { "cf_J_sk_04_02", "cf_J_sk_04_01", "N_Waist_b", "N_Back" };
        private static readonly bool[] frontHPointsInward = { false, false, false, false };
        private static readonly bool[] backHPointsInward = { false, false, true, true };

   //     private static readonly string[] swappedListF = { "ai3p_01", "h2_mf2_03", "h2_mf2_04", "h2_mf2_05" };

        private void Awake()
        {
            for (int index = 0; index < _dan_length.Length; index++)
            {
                _dan_collider_headlength[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Collider: Length of Head", 0.2f, "Distance from the center of the head bone to the tip, used for collision purposes.");
                _dan_collider_radius[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Collider: Radius of Shaft", 0.25f, "Radius of the shaft collider.");
                _dan_length[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Length", 1.8f, "Set the length of the penis.  Apparent Length is about 0.2 larget than this, depending on uncensor.  2.0 is about 8 inches or 20 cm.");
                _dan_girth[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Girth", 1.0f, "Set the scale of the circumference of the penis.");
                _dan_softness[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Softness", 0.1f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
                _dan_sack_size[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Sack: Size", 1.0f, "Set the scale (size) of the sack");
            }

            _clipping_depth = Config.Bind<float>("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int index = 0; index < frontOffsets.Length; index++)
                _front_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Front Collision " + index, frontOffsets[index], "Individual offset on colision point, to improve clipping"));
            for (int index = 0; index < backOffsets.Length; index++)
                _back_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Back Collision " + index, backOffsets[index], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Vertical", -0.035f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Vertical", 0.025f, "Vertical offset of the mouth target");
            _use_telescope_method = Config.Bind<bool>("Female Options", "Use Telescope Method", false, "Use an alternate method of telescoping the penis to prevent clipping instead of repositioning it.");

            for (int index = 0; index < _dan_length.Length; index++)
            {
                _dan_length[index].SettingChanged += delegate
                {
                    if (inHScene)
                    {
                        danCollider[index].m_Center = new Vector3(0, 0, _dan_length[index].Value / 2);
                        danCollider[index].m_Height = _dan_length[index].Value + (_dan_collider_headlength[index].Value * 2);
                    }
                };

                _dan_girth[index].SettingChanged += delegate
                {
                    if (inHScene && bDansFound[index])
                    {
                        danPoints[index].danStart.localScale = new Vector3(_dan_girth[index].Value, _dan_girth[index].Value, 1);
                    }
                };

                _dan_sack_size[index].SettingChanged += delegate
                {
                    if (inHScene && danPoints[index].danTop != null)
                    {
                        danPoints[index].danTop.localScale = new Vector3(_dan_sack_size[index].Value, _dan_sack_size[index].Value, _dan_sack_size[index].Value);
                    }
                };

                _dan_collider_radius[index].SettingChanged += delegate
                {
                    if (inHScene)
                    {
                        danCollider[index].m_Radius = _dan_collider_radius[index].Value;
                    }
                };

                _dan_collider_headlength[index].SettingChanged += delegate
                {
                    if (inHScene)
                    {
                        danCollider[index].m_Height = _dan_length[index].Value + (_dan_collider_headlength[index].Value * 2);
                    }
                };
            }
            harmony = new Harmony("HS2_BetterPenetration");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_ChangeAnimation(HScene __instance, HScene.AnimationListInfo _info)
        {
            if (!inHScene)
                return;

            for (int index = 0; index < male_list.Length; index++)
            {
                bDanPenetration[index] = false;
                referenceLookAtTarget[index] = null;
            }

            b2MAnimation = false;
            if (lookat_Dan != null)
            {
                lookat_Dan.transLookAtNull = null;
                lookat_Dan.dan_Info.SetTargetTransform(null);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void HScene_ChangeMotion(H_Lookat_dan __instance, System.Text.StringBuilder ___assetName)
        {  
            if (!inHScene || __instance == null)
                return;

            int maleNum = 0;
            if (lookat_Dan == null)
                lookat_Dan = __instance;

            b2MAnimation = false;
            if (___assetName != null && ___assetName.Length != 0)
            {
                if (___assetName.ToString().Contains("m2f"))
                {
                    b2MAnimation = true;

                    string substring = ___assetName.ToString().Substring(___assetName.ToString().Length - 3, 3);
                    if (substring == "_02")
                        maleNum = 1;
                }

                if (!bDansFound[maleNum])
                    return;

                targetF[maleNum] = __instance.numFemale;
                if (targetF[maleNum] >= constrainPoints.Length)
                    targetF[maleNum] = 0;
   /*             if (constrainPoints.Length > 1)
                {
                    foreach (string position in swappedListF)
                    {
                        if (position == ___assetName.ToString())
                        {
                            Console.WriteLine("Swapping Girls ");
                            targetF[maleNum] = 1;
                            break;
                        }
                    }
                }*/
            }

            referenceLookAtTarget[maleNum] = danPoints[maleNum].danEnd;
            bDanPenetration[maleNum] = false;
            if (__instance.transLookAtNull != null && __instance.transLookAtNull.name != "k_f_spine03_00" && __instance.strPlayMotion.Contains("Idle") == false && __instance.strPlayMotion.Contains("OUT") == false)
            {
                bDanPenetration[maleNum] = true;
                referenceLookAtTarget[maleNum] = __instance.transLookAtNull;
            }

            SetDanTarget(__instance, maleNum);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void OffsetPenisTarget(H_Lookat_dan __instance)
        {
            if (!inHScene)
                return;

            for (int male = 0; male < bDansFound.Length; male++)
            {
                if (bDansFound[male])
                {
                    danPoints[male].danStart.localScale = new Vector3(_dan_girth[male].Value, _dan_girth[male].Value, 1);
                    danPoints[male].danTop.localScale = new Vector3(_dan_sack_size[male].Value, _dan_sack_size[male].Value, _dan_sack_size[male].Value);

                    if (bHPointsFound[male])
                        SetDanTarget(__instance, male);
                }

                if (!b2MAnimation)
                    return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            inHScene = true;
            male_list = __instance.GetMales().Where(male => male != null).ToArray();
            fem_list = __instance.GetFemales().Where(female => female != null).ToArray();

            bDansFound = new bool[male_list.Length];
            bDanPenetration = new bool[male_list.Length];
            danPoints = new DanPoints[male_list.Length];
            targetF = new int[male_list.Length];
            referenceLookAtTarget = new Transform[male_list.Length];
            danCollider = new DynamicBoneCollider[male_list.Length];
            bHPointsFound = new bool[fem_list.Length];
            constrainPoints = new ConstrainPoints[fem_list.Length];
            kokanBones = new List<DynamicBone>[fem_list.Length];

            int maleIndex = 0;
            foreach (var male in male_list.Where(male => male != null))
            {
                Transform dan101;
                Transform dan109;
                Transform danTop;

                dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan101_00")).FirstOrDefault();
                dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan109_00")).FirstOrDefault();
                danTop = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan_f_top")).FirstOrDefault();

                bDansFound[maleIndex] = false;
                bDanPenetration[maleIndex] = false;
                targetF[maleIndex] = 0;
                if (dan101 != null && dan109 != null && danTop != null)
                {
                    danPoints[maleIndex] = new DanPoints(dan101, dan109, danTop);

                    bDansFound[maleIndex] = true;
                    dan101.localScale = new Vector3(_dan_girth[maleIndex].Value, _dan_girth[maleIndex].Value, 1);

                    danCollider[maleIndex] = dan101.GetComponent<DynamicBoneCollider>();

                    if (danCollider[maleIndex] == null)
                        danCollider[maleIndex] = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

                    danCollider[maleIndex].m_Direction = DynamicBoneColliderBase.Direction.Z;
                    danCollider[maleIndex].m_Center = new Vector3(0, 0, _dan_length[maleIndex].Value / 2);
                    danCollider[maleIndex].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    danCollider[maleIndex].m_Radius = _dan_collider_radius[maleIndex].Value;
                    danCollider[maleIndex].m_Height = _dan_length[maleIndex].Value + (_dan_collider_headlength[maleIndex].Value * 2);
                    danPoints[maleIndex].danTop.localScale = new Vector3(_dan_sack_size[maleIndex].Value, _dan_sack_size[maleIndex].Value, _dan_sack_size[maleIndex].Value);
                }

                referenceLookAtTarget[maleIndex] = dan101;
                Console.WriteLine("bDansFound " + bDansFound);
                maleIndex++;
            }

            int femaleIndex = 0;
            foreach (var female in fem_list.Where(female => female != null))
            {
                List<Transform> frontHPoints = new List<Transform>();
                List<Transform> backHPoints = new List<Transform>();
                Transform hPointBackOfHead;
                bHPointsFound[femaleIndex] = false;

                for (int index = 0; index < frontHPointsList.Length; index++)
                    frontHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(frontHPointsList[index])).FirstOrDefault());

                for (int index = 0; index < backHPointsList.Length; index++)
                    backHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(backHPointsList[index])).FirstOrDefault());

                hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_head_03")).FirstOrDefault();

                if (frontHPoints.Count == frontHPointsList.Length && backHPoints.Count == backHPointsList.Length && hPointBackOfHead != null)
                {
                    bHPointsFound[femaleIndex] = true;
                    constrainPoints[femaleIndex] = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                }

                Console.WriteLine("bHPointsFound " + bHPointsFound);

                List<DynamicBone> dbList = new List<DynamicBone>();

                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                {
                    

                    if (db != null)
                    {
                        Console.WriteLine(db.m_Root.name + " found, adding collilders");

                        dbList.Add(db);

                        for (int i = 0; i < danCollider.Length; i++)
                        {
                            if (db.m_Colliders.Contains(danCollider[i]))
                            {
                                Console.WriteLine("Instance of " + danCollider[i].name + " already exists in list for DB " + db.name);
                            }
                            else
                            {
                                db.m_Colliders.Add(danCollider[i]);
                                Console.WriteLine(danCollider[i].name + " added to " + female.name + " for bone " + db.name);
                            }
                        }
                    }
                }

                kokanBones[femaleIndex] = dbList;
                femaleIndex++;
            }
            Console.WriteLine("AddColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            Console.WriteLine("HScene::EndProc");

            inHScene = false;
            for (int index = 0; index < male_list.Length; index++)
            {
                bDansFound[index] = false;
                bDanPenetration[index] = false;
                targetF[index] = 0;
            }

            for (int index = 0; index < fem_list.Length; index++)
            {
                bHPointsFound[index] = false;
            }

            if (!inHScene)
            {

                for (int i = 0; i < kokanBones.Length; i++)
                {
                    if (kokanBones[i].Any())
                    {
                        foreach (DynamicBone kokanBone in kokanBones[i])
                        {
                            if (kokanBone != null)
                            {
                                Console.WriteLine("Clearing colliders from " + kokanBone.m_Root.name);
                                kokanBone.m_Colliders.Clear();
                            }
                        }
                    }
                }

                for (int i = 0; i < danCollider.Length; i++)
                    Destroy(danCollider[1]);

                Console.WriteLine("Clearing females list");
                Array.Clear(fem_list, 0, fem_list.Length);
                Console.WriteLine("Clearing males list");
                Array.Clear(male_list, 0, male_list.Length);
            }
        }

        private static void SetDanTarget(H_Lookat_dan __instance, int maleIndex)
        {
            if (!bDansFound[maleIndex] || !bHPointsFound[maleIndex] || referenceLookAtTarget == null || referenceLookAtTarget.Length <= maleIndex)
                return;

            if (referenceLookAtTarget[maleIndex] == null)
                referenceLookAtTarget[maleIndex] = danPoints[maleIndex].danEnd;

            Vector3 dan101_pos = danPoints[maleIndex].danStart.position;
            Vector3 lookTarget = referenceLookAtTarget[maleIndex].position;

            if (referenceLookAtTarget[maleIndex].name == "k_f_kokan_00")
                lookTarget = lookTarget + (referenceLookAtTarget[maleIndex].forward * _kokanForwardOffset.Value) + (referenceLookAtTarget[maleIndex].up * _kokanUpOffset.Value);
            if (referenceLookAtTarget[maleIndex].name == "k_f_head_00")
                lookTarget = lookTarget + (referenceLookAtTarget[maleIndex].forward * _headForwardOffset.Value) + (referenceLookAtTarget[maleIndex].up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            float tDan101ToTarget = _dan_length[maleIndex].Value / distDan101ToTarget;
            Vector3 dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);

            if (bDanPenetration[maleIndex])
            {
                if (referenceLookAtTarget[maleIndex].name == "k_f_kokan_00" || referenceLookAtTarget[maleIndex].name == "k_f_ana_00")
                {
                    List<Vector3> frontHitPoints = new List<Vector3>();
                    List<Vector3> backHitPoints = new List<Vector3>();

                    for (int index = 0; index < constrainPoints[targetF[maleIndex]].frontConstrainPoints.Count; index++)
                    {
                        if (frontHPointsInward[index])
                            frontHitPoints.Add(constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].position + (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].forward);
                        else
                            frontHitPoints.Add(constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].position - (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].forward);
                    }
                    for (int index = 0; index < constrainPoints[targetF[maleIndex]].backConstrainPoints.Count; index++)
                    {
                        if (backHPointsInward[index])
                            backHitPoints.Add(constrainPoints[targetF[maleIndex]].backConstrainPoints[index].position - (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleIndex]].backConstrainPoints[index].forward);
                        else
                            backHitPoints.Add(constrainPoints[targetF[maleIndex]].backConstrainPoints[index].position + (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleIndex]].backConstrainPoints[index].forward);
                    }

                    float danLength = _dan_length[maleIndex].Value;
                    Plane kokanPlane = new Plane(danPoints[maleIndex].danStart.forward, lookTarget);

                    if (_dan_length[maleIndex].Value > distDan101ToTarget)
                        danLength = _dan_length[maleIndex].Value - (_dan_length[maleIndex].Value - distDan101ToTarget) * _dan_softness[maleIndex].Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = _dan_length[maleIndex].Value * (1 - _dan_softness[maleIndex].Value);

                    tDan101ToTarget = danLength / distDan101ToTarget;
                    dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);

                    Vector3 adjustedDanPos = dan109_pos;

                    bool bConstrainPastNearSide = false;
                    for (int index = 1; index < constrainPoints[targetF[maleIndex]].frontConstrainPoints.Count; index++)
                    {
                        Plane hPlane = new Plane(Vector3.Normalize(Vector3.Cross(constrainPoints[targetF[maleIndex]].frontConstrainPoints[index - 1].right, frontHitPoints[index] - frontHitPoints[index - 1])), Vector3.Lerp(frontHitPoints[index - 1], frontHitPoints[index], 0.5f));
                        if (frontHPointsInward[index - 1])
                            hPlane.Flip();

                        if (index == 1)
                            bConstrainPastNearSide = hPlane.GetSide(adjustedDanPos);

                        Vector3 constrainedDanPos = Geometry.ConstrainLineToHitPlane(dan101_pos, adjustedDanPos, danLength, frontHitPoints[index-1], frontHitPoints[index], hPlane, ref bConstrainPastNearSide, out float constainedAngle, out float hitDistance);

                        if (_use_telescope_method.Value == true)
                        {
                            bConstrainPastNearSide = false;

                            if (hitDistance > 0 && hitDistance < danLength)
                            {
                                danLength = hitDistance;
                                tDan101ToTarget = danLength / distDan101ToTarget;
                                adjustedDanPos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);
                            }
                        }
                        else
                        {
                            if (constainedAngle > 0)
                                adjustedDanPos = constrainedDanPos;
                        }
                    }

                    for (int index = 1; index < constrainPoints[targetF[maleIndex]].backConstrainPoints.Count; index++)
                    {
                        Plane hPlane = new Plane(Vector3.Normalize(Vector3.Cross(-constrainPoints[targetF[maleIndex]].backConstrainPoints[index - 1].right, backHitPoints[index] - backHitPoints[index - 1])), Vector3.Lerp(backHitPoints[index - 1], backHitPoints[index], 0.5f));
                        if (backHPointsInward[index - 1])
                            hPlane.Flip();

                        if (index == 1)
                            bConstrainPastNearSide = hPlane.GetSide(adjustedDanPos);

                        Vector3 constrainedDanPos = Geometry.ConstrainLineToHitPlane(dan101_pos, adjustedDanPos, danLength, backHitPoints[index - 1], backHitPoints[index], hPlane, ref bConstrainPastNearSide, out float constainedAngle, out float hitDistance);

                        if (_use_telescope_method.Value == true)
                        {
                            bConstrainPastNearSide = false;

                            if (hitDistance > 0 && hitDistance < danLength)
                            {
                                danLength = hitDistance;
                                tDan101ToTarget = danLength / distDan101ToTarget;
                                adjustedDanPos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);
                            }
                        }
                        else
                        {
                            if (constainedAngle > 0)
                                adjustedDanPos = constrainedDanPos;
                        }
                    }

                    dan109_pos = adjustedDanPos;
                }
                else if (referenceLookAtTarget[maleIndex].name == "k_f_head_00")
                {
                    float danLength = _dan_length[maleIndex].Value;

                    if (Vector3.Distance(dan101_pos, constrainPoints[targetF[maleIndex]].headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints[targetF[maleIndex]].headConstrainPoint.position))
                    {
                        danLength = _dan_length[maleIndex].Value * (1 - _dan_softness[maleIndex].Value);
                        tDan101ToTarget = danLength / distDan101ToTarget;

                        float max_dist = Vector3.Distance(dan101_pos, constrainPoints[targetF[maleIndex]].headConstrainPoint.position);

                        if (danLength > max_dist)
                            tDan101ToTarget = max_dist / distDan101ToTarget;

                        dan109_pos = Vector3.LerpUnclamped(lookTarget, dan101_pos, tDan101ToTarget);
                    }
                    else
                    {
                        if (_dan_length[maleIndex].Value > distDan101ToTarget)
                            danLength = _dan_length[maleIndex].Value - (_dan_length[maleIndex].Value - distDan101ToTarget) * _dan_softness[maleIndex].Value;
                        tDan101ToTarget = danLength / distDan101ToTarget;

                        float max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints[targetF[maleIndex]].headConstrainPoint.position);

                        if (danLength > max_dist)
                            tDan101ToTarget = max_dist / distDan101ToTarget;

                        dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);
                    }
                }
            }

            Vector3 danForwardVector = dan109_pos - danPoints[maleIndex].danStart.position;
            Vector3 danRightVector = danPoints[maleIndex].danTop.right;
            Vector3 danUpVector = Vector3.Normalize(Vector3.Cross(danForwardVector, danRightVector));

            danPoints[maleIndex].danStart.rotation = Quaternion.LookRotation(danForwardVector, danUpVector);
            danPoints[maleIndex].danEnd.SetPositionAndRotation(dan109_pos, Quaternion.LookRotation(danForwardVector, danUpVector));
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            if (lsm != LoadSceneMode.Single)
                return;

            if (scene.name == "HScene")
                harmony.PatchAll(typeof(HS2_BetterPenetration));
            else
                harmony.UnpatchAll(nameof(HS2_BetterPenetration));
        }
    }
}