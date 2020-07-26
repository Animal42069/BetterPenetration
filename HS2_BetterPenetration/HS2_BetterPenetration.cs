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
        public const string VERSION = "2.0.2.0";
        private const string head_target = "k_f_head_00";
        private const string chest_target = "k_f_spine03_00";
        private const string kokan_target = "k_f_kokan_00";
        private const string ana_target = "k_f_ana_00";
        private const string dan_base = "cm_J_dan101_00";
        private const string dan_head = "cm_J_dan109_00";
        private const string dan_sack = "cm_J_dan_f_top";

        private static Harmony harmony;

        private static ConfigEntry<float>[] _dan_length = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_girth = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_sack_size = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_softness = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_collider_headlength = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_collider_radius = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_move_limit = new ConfigEntry<float>[2];
        private static ConfigEntry<float>[] _dan_angle_limit = new ConfigEntry<float>[2];
        
        private static ConfigEntry<float>[] _allow_telescope_percent = new ConfigEntry<float>[2];
        private static ConfigEntry<bool>[] _force_telescope = new ConfigEntry<bool>[2];

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
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
        private static float[] lastDanAngle = new float[2] { 0, 0 };
        private static float[] lastDanLength = new float[2] { 0, 0 };
        private static Vector3[] lastDanForwardVector = new Vector3[2] { new Vector3(0, 0, 0), new Vector3(0, 0, 0) };
        private static Quaternion[] lastDanRotation = new Quaternion[2] { new Quaternion(0, 0, 0, 0), new Quaternion(0, 0, 0, 0) };
        private static Vector3[] lastDan109Position = new Vector3[2] { new Vector3(0, 0, 0), new Vector3(0, 0, 0) };
        private static float[] lastDan101TargetDistance = new float[2] { 0, 0 };

        private static bool[] bHPointsFound = new bool[2] { false, false };
        private static int[] targetF = new int[2] { 0, 0 };
        private static ConstrainPoints[] constrainPoints;
        private static H_Lookat_dan lookat_Dan;

        private static readonly float[] frontOffsets = { -0.35f, 0.1f,/* -0.2f,*/ 0f, -0.65f };
        private static readonly float[] backOffsets = { -0.35f , 0.1f, /*0.1f,*/ 0.05f, 0.05f };
        private static readonly string[] frontHPointsList = { kokan_target, "cf_J_sk_00_02", /*"k_f_kosi03_03",*/ "N_Waist_f", "k_f_spine03_03" };
        private static readonly string[] backHPointsList = { ana_target, "cf_J_sk_04_02",/* "cf_J_sk_04_01",*/ "N_Waist_b", "N_Back" };
        private static readonly bool[] frontHPointsInward = { false, false,/* false,*/ false, false };
        private static readonly bool[] backHPointsInward = { false, false, /*false,*/ true, true };

        private static float lastAdjustTime = 0;

        private void Awake()
        {
            for (int index = 0; index < _dan_length.Length; index++)
            {
                _dan_collider_headlength[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Collider: Length of Head", 0.2f, "Distance from the center of the head bone to the tip, used for collision purposes.");
                _dan_collider_radius[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Collider: Radius of Shaft", 0.25f, "Radius of the shaft collider.");
                _dan_length[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Length", 1.8f, "Set the length of the penis.  Apparent Length is about 0.2 larget than this, depending on uncensor.  2.0 is about 8 inches or 20 cm.");
                _dan_girth[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Girth", 1.0f, "Set the scale of the circumference of the penis.");
                _dan_softness[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Softness", 0.1f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
                _allow_telescope_percent[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Telescope Threshold", 0.4f, "Allow the penis to begin telescoping after it has penetrated a certain amount. 0 = never telescope, 0.5 = allow telescoping after the halfway point, 1 = always allow telescoping.");
                _force_telescope[index] = Config.Bind<bool>("Male " + (index + 1) + " Options", "Penis: Telescope Always", false, "Force the penis to always telescope at the threshold point, instead of only doing it when it prevents clipping.");
                _dan_move_limit[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Telescope Limiter", 3.6f, "Sets a limit for how fast the penis can change length, preventing the penis from suddenly changing length when it hits a new collision point.  Maximum amount the penis can grow/shrink per second, at an insertion rate of 1 per second.  Scales to insertion rate.");
                _dan_angle_limit[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Penis: Angle Limiter", 60.0f, "Maximum angle the penis rotate per second.");
                _dan_sack_size[index] = Config.Bind<float>("Male " + (index + 1) + " Options", "Sack: Size", 1.0f, "Set the scale (size) of the sack");
            }

            _clipping_depth = Config.Bind<float>("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int index = 0; index < frontOffsets.Length; index++)
                _front_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Front Collision " + index, frontOffsets[index], "Individual offset on colision point, to improve clipping"));
            for (int index = 0; index < backOffsets.Length; index++)
                _back_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Back Collision " + index, backOffsets[index], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Vertical", -0.025f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Depth", -0.05f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Depth", 0.05f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Vertical", 0.025f, "Vertical offset of the mouth target");

            for (int index = 0; index < _dan_length.Length; index++)
            {
                _dan_length[index].SettingChanged += delegate
                {
                    if (inHScene && danCollider[index] != null)
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
           }

            referenceLookAtTarget[maleNum] = danPoints[maleNum].danEnd;
            lastDan109Position[maleNum] = danPoints[maleNum].danEnd.position;
            lastDanRotation[maleNum] = danPoints[maleNum].danEnd.rotation;
            lastDanForwardVector[maleNum] = danPoints[maleNum].danEnd.position - danPoints[maleNum].danStart.position;
            lastDanLength[maleNum] = _dan_length[maleNum].Value;
            lastDanAngle[maleNum] = 0;

            bDanPenetration[maleNum] = false;
            if (__instance.transLookAtNull != null && __instance.transLookAtNull.name != chest_target && __instance.strPlayMotion.Contains("Idle") == false && __instance.strPlayMotion.Contains("OUT") == false)
            {
                bDanPenetration[maleNum] = true;
                referenceLookAtTarget[maleNum] = __instance.transLookAtNull;
                lastDan101TargetDistance[maleNum] = Vector3.Distance(referenceLookAtTarget[maleNum].position, danPoints[maleNum].danStart.position);
            }

            SetDanTarget(__instance, maleNum);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void OffsetPenisTarget(H_Lookat_dan __instance)
        {
            if (!inHScene)
                return;

            float adjustTime = Time.time;
            float timeSinceLastAdjust = adjustTime - lastAdjustTime ;
            lastAdjustTime = adjustTime;

            for (int male = 0; male < bDansFound.Length; male++)
            {
                if (timeSinceLastAdjust < 0.0001)
                {
                    danPoints[male].danStart.rotation = lastDanRotation[male];
                    danPoints[male].danEnd.SetPositionAndRotation(lastDan109Position[male], lastDanRotation[male]);
                }
                else
                {
                    Console.WriteLine("Time since last adjust " + timeSinceLastAdjust);
                    if (bDansFound[male])
                    {
                        danPoints[male].danStart.localScale = new Vector3(_dan_girth[male].Value, _dan_girth[male].Value, 1);
                        danPoints[male].danTop.localScale = new Vector3(_dan_sack_size[male].Value, _dan_sack_size[male].Value, _dan_sack_size[male].Value);

                        if (bHPointsFound[targetF[male]])
                        {
                            float maxDanSizeAdjust = _dan_move_limit[male].Value * timeSinceLastAdjust;
                            float maxDanAngleAdjust = _dan_angle_limit[male].Value * timeSinceLastAdjust;

                            Console.WriteLine("maxDanSizeAdjust " + maxDanSizeAdjust + " , maxDanAngleAdjust " + maxDanAngleAdjust);

                            SetDanTarget(__instance, male, maxDanSizeAdjust, maxDanAngleAdjust);
                        }
                    }

                    if (!b2MAnimation)
                        return;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            male_list = __instance.GetMales().Where(male => male != null).ToArray();
            fem_list = __instance.GetFemales().Where(female => female != null).ToArray();

            danPoints = new DanPoints[male_list.Length];
            referenceLookAtTarget = new Transform[male_list.Length];
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

                    lastDan109Position[maleIndex] = danPoints[maleIndex].danEnd.position;
                    lastDanRotation[maleIndex] = danPoints[maleIndex].danEnd.rotation;
                    lastDanForwardVector[maleIndex] = danPoints[maleIndex].danEnd.position - danPoints[maleIndex].danStart.position;
                    lastDanLength[maleIndex] = _dan_length[maleIndex].Value;
                    lastDanAngle[maleIndex] = 0;
                }

                referenceLookAtTarget[maleIndex] = dan101;
                lastDan101TargetDistance[maleIndex] = Vector3.Distance(referenceLookAtTarget[maleIndex].position, danPoints[maleIndex].danStart.position);
                Console.WriteLine("bDansFound " + bDansFound[maleIndex]);
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

                hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_head")).FirstOrDefault();

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
            inHScene = true;
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

        private static void SetDanTarget(H_Lookat_dan __instance, int maleIndex, float maxLengthAdjust = 10000, float maxAngleAdjust = 10000)
        {
            if (!bDansFound[maleIndex] || referenceLookAtTarget == null || referenceLookAtTarget.Length <= maleIndex)
                return;

            if (referenceLookAtTarget[maleIndex] == null)
                referenceLookAtTarget[maleIndex] = danPoints[maleIndex].danEnd;

            Vector3 dan101_pos = danPoints[maleIndex].danStart.position;
            Vector3 lookTarget = referenceLookAtTarget[maleIndex].position;

            if (referenceLookAtTarget[maleIndex].name == kokan_target)
                lookTarget = lookTarget + (referenceLookAtTarget[maleIndex].forward * _kokanForwardOffset.Value) + (referenceLookAtTarget[maleIndex].up * _kokanUpOffset.Value);
            if (referenceLookAtTarget[maleIndex].name == head_target)
                lookTarget = lookTarget + (referenceLookAtTarget[maleIndex].forward * _headForwardOffset.Value) + (referenceLookAtTarget[maleIndex].up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            float tDan101ToTarget = _dan_length[maleIndex].Value / distDan101ToTarget;
            Vector3 dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);

            if (bDanPenetration[maleIndex])
            {
                // scale to insertions per second, one full insertion is the movement of the dan in and out.
                maxLengthAdjust *= ((distDan101ToTarget - lastDan101TargetDistance[maleIndex]) * 2 * _dan_length[maleIndex].Value) * Time.deltaTime;
                Console.WriteLine("maxLengthAdjust scaled to " + maxLengthAdjust);

                if (referenceLookAtTarget[maleIndex].name == kokan_target || referenceLookAtTarget[maleIndex].name == ana_target)
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

                    for (int index = 0; index < constrainPoints[maleIndex].frontConstrainPoints.Count; index++)
                        Console.WriteLine("frontHitPoints" + index + ": " + frontHitPoints[index].x + " , " + frontHitPoints[index].y + " , " + frontHitPoints[index].z);

                    for (int index = 0; index < constrainPoints[maleIndex].backConstrainPoints.Count; index++)
                        Console.WriteLine("backHitPoints" + index + ": " + backHitPoints[index].x + " , " + backHitPoints[index].y + " , " + backHitPoints[index].z);

                    float danLength = _dan_length[maleIndex].Value;
                    Plane kokanPlane = new Plane(danPoints[maleIndex].danStart.forward, lookTarget);

                    if (_dan_length[maleIndex].Value > distDan101ToTarget)
                        danLength = _dan_length[maleIndex].Value - (_dan_length[maleIndex].Value - distDan101ToTarget) * _dan_softness[maleIndex].Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = _dan_length[maleIndex].Value * (1 - _dan_softness[maleIndex].Value);

                    float minDanLength = distDan101ToTarget + (danLength * (1 - _allow_telescope_percent[maleIndex].Value));
                    bool bConstrainPastNearSide = true;

                    Console.WriteLine("LastDanLength " + lastDanLength[maleIndex]);

                    if (minDanLength < lastDanLength[maleIndex] - maxLengthAdjust)
                        minDanLength = lastDanLength[maleIndex] - maxLengthAdjust;

                    if (danLength > lastDanLength[maleIndex] + maxLengthAdjust)
                        danLength = lastDanLength[maleIndex] + maxLengthAdjust;

                    if (minDanLength > danLength)
                        minDanLength = danLength;

                    if (_force_telescope[maleIndex].Value)
                        danLength = minDanLength;

                    tDan101ToTarget = danLength / distDan101ToTarget;
                    dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);


                    Console.WriteLine("dan101_pos " + dan101_pos.x + " , " + dan101_pos.y + " , " + dan101_pos.z);
                    Console.WriteLine("lookTarget " + lookTarget.x + " , " + lookTarget.y + " , " + lookTarget.z);
                    Console.WriteLine("dan109_pos " + dan109_pos.x + " , " + dan109_pos.y + " , " + dan109_pos.z);
                    Console.WriteLine("danLength " + danLength);
                    Console.WriteLine("minDanLength " + minDanLength);

                    bool bConstrainPastFarSide = false;
                    Vector3 adjustedDanPos = dan109_pos;

                        for (int index = 1; index < constrainPoints[targetF[maleIndex]].frontConstrainPoints.Count; index++)
                        {
                            Vector3 firstVectorRight = constrainPoints[targetF[maleIndex]].frontConstrainPoints[index - 1].right;
                            Vector3 firstVectorUp = constrainPoints[targetF[maleIndex]].frontConstrainPoints[index - 1].forward;
                            Vector3 secondVectorRight = constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].right;
                            Vector3 secondVectorUp = constrainPoints[targetF[maleIndex]].frontConstrainPoints[index].forward;

                            if (frontHPointsInward[index - 1])
                            {
                                firstVectorRight = -firstVectorRight;
                                firstVectorUp = -firstVectorUp;
                            }

                            if (frontHPointsInward[index])
                            {
                                secondVectorRight = -secondVectorRight;
                                secondVectorUp = -secondVectorUp;
                            }

                            TwistedPlane hPlane = new TwistedPlane(frontHitPoints[index - 1], firstVectorRight, firstVectorUp, frontHitPoints[index], secondVectorRight, secondVectorUp);

                            if (index == constrainPoints[targetF[maleIndex]].frontConstrainPoints.Count - 1)
                                bConstrainPastFarSide = true;

                            Console.WriteLine("hPlane.firstOrigin" + index + ": " + hPlane.firstOrigin.x + " , " + hPlane.firstOrigin.y + " , " + hPlane.firstOrigin.z);
                            Console.WriteLine("hPlane.firstVector" + index + ": " + hPlane.firstVector.x + " , " + hPlane.firstVector.y + " , " + hPlane.firstVector.z);
                            Console.WriteLine("hPlane.firstUpVector" + index + ": " + hPlane.firstUpVector.x + " , " + hPlane.firstUpVector.y + " , " + hPlane.firstUpVector.z);
                            Console.WriteLine("hPlane.secondOrigin" + index + ": " + hPlane.secondOrigin.x + " , " + hPlane.secondOrigin.y + " , " + hPlane.secondOrigin.z);
                            Console.WriteLine("hPlane.secondVector" + index + ": " + hPlane.secondVector.x + " , " + hPlane.secondVector.y + " , " + hPlane.secondVector.z);
                            Console.WriteLine("hPlane.secondUpVector" + index + ": " + hPlane.secondUpVector.x + " , " + hPlane.secondUpVector.y + " , " + hPlane.secondUpVector.z);
                            Console.WriteLine("bConstrainPastNearSideF" + index + ": " + bConstrainPastNearSide);
                            Console.WriteLine("bConstrainPastFarSide" + index + ": " + bConstrainPastFarSide);
                            adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bool bHitPointFound);

                            Console.WriteLine("adjustedDanPosF" + index + ": " + adjustedDanPos.x + " , " + adjustedDanPos.y + " , " + adjustedDanPos.z);
                            Console.WriteLine("newDistanceF" + index + ": " + danLength);


                            if (bHitPointFound)
                                break;
                        }

                    bConstrainPastFarSide = false;
                    bConstrainPastNearSide = true;

                        for (int index = 1; index < constrainPoints[targetF[maleIndex]].backConstrainPoints.Count; index++)
                        {
                            Vector3 firstVectorRight = constrainPoints[targetF[maleIndex]].backConstrainPoints[index - 1].right;
                            Vector3 firstVectorUp = constrainPoints[targetF[maleIndex]].backConstrainPoints[index - 1].forward;
                            Vector3 secondVectorRight = constrainPoints[targetF[maleIndex]].backConstrainPoints[index].right;
                            Vector3 secondVectorUp = constrainPoints[targetF[maleIndex]].backConstrainPoints[index].forward;

                            if (!backHPointsInward[index - 1])
                            {
                                firstVectorRight = -firstVectorRight;
                                firstVectorUp = -firstVectorUp;
                            }

                            if (!backHPointsInward[index])
                            {
                                secondVectorRight = -secondVectorRight;
                                secondVectorUp = -secondVectorUp;
                            }

                            TwistedPlane hPlane = new TwistedPlane(backHitPoints[index - 1], firstVectorRight, firstVectorUp, backHitPoints[index], secondVectorRight, secondVectorUp);

                            if (index == constrainPoints[targetF[maleIndex]].backConstrainPoints.Count - 1)
                                bConstrainPastFarSide = true;

                            Console.WriteLine("hPlane.firstOrigin" + index + ": " + hPlane.firstOrigin.x + " , " + hPlane.firstOrigin.y + " , " + hPlane.firstOrigin.z);
                            Console.WriteLine("hPlane.firstVector" + index + ": " + hPlane.firstVector.x + " , " + hPlane.firstVector.y + " , " + hPlane.firstVector.z);
                            Console.WriteLine("hPlane.firstUpVector" + index + ": " + hPlane.firstUpVector.x + " , " + hPlane.firstUpVector.y + " , " + hPlane.firstUpVector.z);
                            Console.WriteLine("hPlane.secondOrigin" + index + ": " + hPlane.secondOrigin.x + " , " + hPlane.secondOrigin.y + " , " + hPlane.secondOrigin.z);
                            Console.WriteLine("hPlane.secondVector" + index + ": " + hPlane.secondVector.x + " , " + hPlane.secondVector.y + " , " + hPlane.secondVector.z);
                            Console.WriteLine("hPlane.secondUpVector" + index + ": " + hPlane.secondUpVector.x + " , " + hPlane.secondUpVector.y + " , " + hPlane.secondUpVector.z);
                            Console.WriteLine("bConstrainPastNearSideF" + index + ": " + bConstrainPastNearSide);
                            Console.WriteLine("bConstrainPastFarSide" + index + ": " + bConstrainPastFarSide);
                            adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bool bHitPointFound);

                            Console.WriteLine("adjustedDanPosB" + index + ": " + adjustedDanPos.x + " , " + adjustedDanPos.y + " , " + adjustedDanPos.z);
                            Console.WriteLine("newDistanceB" + index + ": " + danLength);

                            if (bHitPointFound)
                                break;
                        }

                        dan109_pos = adjustedDanPos;

                    Console.WriteLine("dan109_pos "+ dan109_pos.x + " , " + dan109_pos.y + " , " + dan109_pos.z);
                }
                else if (referenceLookAtTarget[maleIndex].name == head_target)
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

            Vector3 danForwardVector = dan109_pos - dan101_pos;
            Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Normalize(Vector3.Cross(danForwardVector, danPoints[maleIndex].danTop.right)));

            danPoints[maleIndex].danStart.rotation = danQuaternion;
            danPoints[maleIndex].danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);

            float danAngle = Vector3.Angle(danPoints[maleIndex].danEnd.position - danPoints[maleIndex].danStart.position, danPoints[maleIndex].danTop.up);

            if (danAngle > lastDanAngle[maleIndex] + 7.5 || danAngle < lastDanAngle[maleIndex] - 7.5)
            {
                Console.WriteLine("Large Angle Shift Detected! " + lastDanAngle + " to " + danAngle);
            }

            lastDanAngle[maleIndex] = danAngle;
            lastDanForwardVector[maleIndex] = danForwardVector;
            lastDanLength[maleIndex] = Vector3.Distance(dan101_pos, dan109_pos);
            lastDanRotation[maleIndex] = danQuaternion;
            lastDan109Position[maleIndex] = dan109_pos;
            lastDan101TargetDistance[maleIndex] = distDan101ToTarget;
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