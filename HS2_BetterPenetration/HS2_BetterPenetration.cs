using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using AIChara;

namespace HS2_BetterPenetration
{
    [BepInPlugin("animal42069.HS2betterpenetration", "HS2 Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.1")]
    [BepInProcess("HoneySelect2")]
    [BepInProcess("HoneySelect2VR")]
    public class HS2_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.4.6.0";
        private static Harmony harmony;
        private static HScene hScene;
        private static bool patched = false;

        private static readonly ConfigEntry<float>[] _dan_softness = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_headlength = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_radius = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _dan_collider_verticalcenter = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _finger_collider_length = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _finger_collider_radius = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<float>[] _allow_telescope_percent = new ConfigEntry<float>[2];
        private static readonly ConfigEntry<bool>[] _force_telescope = new ConfigEntry<bool>[2];

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static ConfigEntry<bool> _kokan_adjust;
        private static ConfigEntry<bool> _use_finger_colliders;
        private static ConfigEntry<bool> _use_bounding_colliders;
        private static ConfigEntry<float> _kokan_adjust_position_y;
        private static ConfigEntry<float> _kokan_adjust_position_z;
        private static ConfigEntry<float> _kokan_adjust_rotation_x;
        private static readonly List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static readonly List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;
        private static bool loadingCharacter = false;
        private static bool b2MAnimation;
        private static bool adjustFAnimation;
        private static Transform kokanBoneAdjustTarget;

        public static ChaControl[] fem_list;
        public static ChaControl[] male_list;
        public static List<DynamicBone>[] kokanBones = new List<DynamicBone>[2];
        public static DynamicBoneCollider[] danCollider = new DynamicBoneCollider[2];
        public static DynamicBoneCollider[] indexCollider = new DynamicBoneCollider[2];
        public static DynamicBoneCollider[] middleCollider = new DynamicBoneCollider[2];
        public static DynamicBoneCollider[] ringCollider = new DynamicBoneCollider[2];

        private static readonly bool[] bDansFound = new bool[2] { false, false };
        private static DanPoints[] danPoints;
        private static readonly float[] baseDanLength = new float[2] { 1.8f, 1.8f };
        private static readonly bool[] bDanPenetration = new bool[2] { false, false };
        private static readonly bool[] bDanPull = new bool[2] { false, false };
        private static Transform[] referenceLookAtTarget;
        private static readonly Transform[] bpKokanTarget = new Transform[2];
        private static readonly bool[] changingAnimations = new bool[2] { false, false };
        private static readonly bool[] bHPointsFound = new bool[2] { false, false };
        private static readonly int[] targetF = new int[2] { 0, 0 };
        private static ConstrainPoints[] constrainPoints;
        private static readonly Vector3[] lastDanPostion = new Vector3[2];

        private const string head_target = "k_f_head_00";
        private const string chest_target_00 = "k_f_spine03_00";
        private const string chest_target_01 = "k_f_spine03_01";
        private const string kokan_target = "k_f_kokan_00";
        private const string bp_kokan_target = "cf_J_Vagina_root";
        private const string ana_target = "k_f_ana_00";
        private const string dan_base = "cm_J_dan101_00";
        private const string dan_head = "cm_J_dan109_00";
        private const string dan_sack = "cm_J_dan_f_top";
        private const string index_finger = "cf_J_Hand_Index03_R";
        private const string middle_finger = "cf_J_Hand_Middle03_R";
        private const string ring_finger = "cf_J_Hand_Ring03_R";

        private const string headHPoint = "cf_J_Head";
        private static readonly List<string> frontHPointsList = new List<string> { kokan_target, "cf_J_sk_00_02", "N_Waist_f", "k_f_spine03_03" };
        private static readonly List<string> backHPointsList = new List<string> { ana_target, "cf_J_sk_04_02", "N_Waist_b", "N_Back" };
        private static readonly List<float> frontOffsets = new List<float> { -0.35f, 0.25f, 0f, -0.65f };
        private static readonly List<float> backOffsets = new List<float> { -0.05f, 0.25f, 0.05f, 0.05f };
        private static readonly List<bool> frontHPointsInward = new List<bool> { false, false, false, false };
        private static readonly List<bool> backHPointsInward = new List<bool> { false, false, true, true };

        private static readonly List<string> colliderList = new List<string> { "cf_J_Vagina_Collider_B", "cf_J_Vagina_Collider_F", "cf_J_Vagina_Collider_Inner_F", "cf_J_Vagina_Collider_L.005", "cf_J_Vagina_Collider_R.005" };
        private static readonly List<string> dynamicBonesList = new List<string> { "cf_J_Vagina_Pivot_B", "cf_J_Vagina_Pivot_F", "cf_J_Vagina_Pivot_Inner_F", "cf_J_Vagina_Pivot_L.005", "cf_J_Vagina_Pivot_R.005" };
        private static readonly List<float> colliderHeightList = new List<float> { 0.39f, 0.19f, 0.34f, 0.39f, 0.39f };
        private static readonly List<float> colliderRadiusList = new List<float> { 0.0021f, 0.0011f, 0.0011f, 0.0021f, 0.0021f };

        private static readonly List<string> animationAdjustmentList = new List<string> { "ais_f_00", "ais_f_01", "ais_f_12", "ais_f_19", "ais_f_20" };

        private void Awake()
        {
            for (int maleNum = 0; maleNum < _dan_collider_headlength.Length; maleNum++)
            {
                _finger_collider_length[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Length", 0.6f, "Lenght of the finger colliders.");
                _finger_collider_radius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Radius", 0.2f, "Radius of the finger colliders.");
                _dan_collider_headlength[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Length of Head", 0.35f, "Distance from the center of the head bone to the tip, used for collision purposes.");
                _dan_collider_radius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Radius of Shaft", 0.32f, "Radius of the shaft collider.");
                _dan_collider_verticalcenter[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Vertical Center", -0.03f, "Vertical Center of the shaft collider");
                _dan_softness[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Softness", 0.15f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
                _allow_telescope_percent[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Limiter: Telescope Threshold", 0.6f, "Allow the penis to begin telescoping after it has penetrated a certain amount. 0 = never telescope, 0.5 = allow telescoping after the halfway point, 1 = always allow telescoping.");
                _force_telescope[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Limiter: Telescope Always", true, "Force the penis to always telescope at the threshold point, instead of only doing it when it prevents clipping.");

                _dan_collider_radius[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < danCollider.Length; index++)
                    {
                        if (inHScene && danCollider[index] != null && bDansFound[index])
                        {
                            danCollider[index].m_Radius = _dan_collider_radius[index].Value;
                        }
                    }
                };

                _dan_collider_headlength[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < danCollider.Length; index++)
                    {
                        if (inHScene && danCollider[index] != null && bDansFound[index])
                        {
                            danCollider[index].m_Height = danPoints[index].danEnd.localPosition.z + (_dan_collider_headlength[index].Value  * 2);
                        }
                    }
                };

                _finger_collider_radius[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < indexCollider.Length; index++)
                    {
                        if (inHScene && indexCollider[index] != null && middleCollider[index] != null && ringCollider[index] != null)
                        {
                            indexCollider[index].m_Radius = _finger_collider_radius[index].Value;
                            middleCollider[index].m_Radius = _finger_collider_radius[index].Value;
                            ringCollider[index].m_Radius = _finger_collider_radius[index].Value;
                        }
                    }
                };

                _finger_collider_length[maleNum].SettingChanged += delegate
                {
                    for (int index = 0; index < indexCollider.Length; index++)
                    {
                        if (inHScene && indexCollider[index] != null && middleCollider[index] != null && ringCollider[index] != null)
                        {
                            indexCollider[index].m_Height = _finger_collider_length[index].Value;
                            middleCollider[index].m_Height = _finger_collider_length[index].Value;
                            ringCollider[index].m_Height = _finger_collider_length[index].Value;
                        }
                    }
                };


            }

            _clipping_depth = Config.Bind("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int femaleNum = 0; femaleNum < frontOffsets.Count; femaleNum++)
                _front_collision_point_offset.Add(Config.Bind("Female Options", "Clipping Offset: Front Collision " + femaleNum, frontOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            for (int femaleNum = 0; femaleNum < backOffsets.Count; femaleNum++)
                _back_collision_point_offset.Add(Config.Bind("Female Options", "Clipping Offset: Back Collision " + femaleNum, backOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind("Female Options", "Target Offset: Vagina Vertical", 0.0f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind("Female Options", "Target Offset: Mouth Vertical", 0.03f, "Vertical offset of the mouth target");
            _use_finger_colliders = Config.Bind("Female Options", "Colliders: Use Finger Colliders", true, "Use finger colliders");
            _use_bounding_colliders = Config.Bind("Female Options", "Colliders: Use Bounding Colliders", true, "Use internal bounding colliders to help animate correctly");
            _kokan_adjust = Config.Bind("Female Options", "Joint Adjustment: Missionary Correction", false, "NOTE: There is an Illusion bug that causes the vagina to appear sunken in certain missionary positions.  It is best to use Advanced Bonemod and adjust your female character's cf_J_Kokan Offset Y to 0.001.  If you don't do that, enabling this option will attempt to fix the problem by guessing where the bone should be");
            _kokan_adjust_position_y = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Y", -0.075f, "Amount to adjust the Vagina bone position Y for certain Missionary positions to correct its appearance");
            _kokan_adjust_position_z = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Z", 0.0625f, "Amount to adjust the Vagina bone position Z for certain Missionary positions to correct its appearance");
            _kokan_adjust_rotation_x = Config.Bind("Female Options", "Joint Adjustment: Missionary Rotation X", 10.0f, "Amount to adjust the Vagina bone rotation X for certain Missionary positions to correct its appearance");

            harmony = new Harmony("HS2_BetterPenetration");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        public static void BeforeCharacterReload()
        { 
            if (!inHScene)
                return;

            loadingCharacter = true;

            for (int maleNum = 0; maleNum < male_list.Length; maleNum++)
                changingAnimations[maleNum] = true;
        }

        public static void AfterCharacterReload()
        {
            if (!inHScene)
                return;

            AddPColliders(false);

            loadingCharacter = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void HScene_PostSetStartVoice(HScene __instance)
        {
            hScene = __instance;
            AddPColliders(true);
            inHScene = true;
        }

        public static void AddPColliders(bool setDanLength)
        {
            if (hScene == null)
                return;

            male_list = hScene.GetMales().Where(male => male != null).ToArray();
            fem_list = hScene.GetFemales().Where(female => female != null).ToArray();

            danPoints = new DanPoints[male_list.Length];
            referenceLookAtTarget = new Transform[male_list.Length];
            constrainPoints = new ConstrainPoints[fem_list.Length];
            kokanBones = new List<DynamicBone>[fem_list.Length];

            int maleNum = 0;
            foreach (var male in male_list.Where(male => male != null))
            {
                Transform dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_base)).FirstOrDefault();
                Transform dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_head)).FirstOrDefault();
                Transform danTop = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_sack)).FirstOrDefault();
                Transform index = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(index_finger)).FirstOrDefault();
                Transform middle = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(middle_finger)).FirstOrDefault();
                Transform ring = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(ring_finger)).FirstOrDefault();

                bDansFound[maleNum] = false;
                bDanPenetration[maleNum] = false;
                bDanPull[maleNum] = false;
                targetF[maleNum] = 0;

                if (dan101 != null && dan109 != null && danTop != null)
                {
                    if (setDanLength)
                    {
                        baseDanLength[maleNum] = Vector3.Distance(dan101.position, dan109.position);
                        if (Geometry.ApproximatelyZero(baseDanLength[maleNum]))
                            baseDanLength[maleNum] = 1.8f;
                    }

                    danPoints[maleNum] = new DanPoints(dan101, dan109, danTop);
                    lastDanPostion[maleNum] = dan109.position;
                    bDansFound[maleNum] = true;

                    danCollider[maleNum] = dan101.GetComponent<DynamicBoneCollider>();

                    if (danCollider[maleNum] == null)
                        danCollider[maleNum] = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                    
                    danCollider[maleNum].m_Direction = DynamicBoneColliderBase.Direction.Z;
                    danCollider[maleNum].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    danCollider[maleNum].m_Center = new Vector3(0, _dan_collider_verticalcenter[maleNum].Value, danPoints[maleNum].danEnd.localPosition.z / 2);
                    danCollider[maleNum].m_Radius = _dan_collider_radius[maleNum].Value;
                    danCollider[maleNum].m_Height = danPoints[maleNum].danEnd.localPosition.z + (_dan_collider_headlength[maleNum].Value * 2);
				
                }

                if (index != null && middle != null && ring != null)
                {
                    indexCollider[maleNum] = index.GetComponent<DynamicBoneCollider>();
                    middleCollider[maleNum] = middle.GetComponent<DynamicBoneCollider>();
                    ringCollider[maleNum] = ring.GetComponent<DynamicBoneCollider>();

                    if (indexCollider[maleNum] == null)
                        indexCollider[maleNum] = index.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

                    if (middleCollider[maleNum] == null)
                        middleCollider[maleNum] = middle.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

                    if (ringCollider[maleNum] == null)
                        ringCollider[maleNum] = ring.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

                    indexCollider[maleNum].m_Direction = DynamicBoneColliderBase.Direction.X;
                    indexCollider[maleNum].m_Center = new Vector3(0, 0, 0);
                    indexCollider[maleNum].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    indexCollider[maleNum].m_Radius = _finger_collider_radius[maleNum].Value;
                    indexCollider[maleNum].m_Height = _finger_collider_length[maleNum].Value;

                    middleCollider[maleNum].m_Direction = DynamicBoneColliderBase.Direction.X;
                    middleCollider[maleNum].m_Center = new Vector3(0, 0, 0);
                    middleCollider[maleNum].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    middleCollider[maleNum].m_Radius = _finger_collider_radius[maleNum].Value;
                    middleCollider[maleNum].m_Height = _finger_collider_length[maleNum].Value;

                    ringCollider[maleNum].m_Direction = DynamicBoneColliderBase.Direction.X;
                    ringCollider[maleNum].m_Center = new Vector3(0, 0, 0);
                    ringCollider[maleNum].m_Bound = DynamicBoneColliderBase.Bound.Outside;
                    ringCollider[maleNum].m_Radius = _finger_collider_radius[maleNum].Value;
                    ringCollider[maleNum].m_Height = _finger_collider_length[maleNum].Value;
                }

                Console.WriteLine("bDansFound " + bDansFound[maleNum]);
                maleNum++;
            }

            int femaleNum = 0;
            foreach (var female in fem_list.Where(female => female != null))
            {
                List<Transform> frontHPoints = new List<Transform>();
                List<Transform> backHPoints = new List<Transform>();
                Transform hPointBackOfHead;
                bHPointsFound[femaleNum] = false;

                for (int index = 0; index < frontHPointsList.Count; index++)
                    frontHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(frontHPointsList[index])).FirstOrDefault());

                for (int index = 0; index < backHPointsList.Count; index++)
                    backHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(backHPointsList[index])).FirstOrDefault());

                hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(headHPoint)).FirstOrDefault();

                bpKokanTarget[femaleNum] = female.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(bp_kokan_target)).FirstOrDefault();
                if (bpKokanTarget[femaleNum] != null)
                {
                    Console.WriteLine("BP Target Found " + bpKokanTarget[femaleNum].name);
                    frontHPoints[0] = bpKokanTarget[femaleNum];
                }

                if (frontHPoints.Count == frontHPointsList.Count && backHPoints.Count == backHPointsList.Count && hPointBackOfHead != null)
                {
                    bHPointsFound[femaleNum] = true;
                    constrainPoints[femaleNum] = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                }

                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                    db.m_Colliders.Clear();

                Console.WriteLine("bHPointsFound " + bHPointsFound[femaleNum]);

                Transform kokanBone = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Kokan")).FirstOrDefault();
                List<DynamicBone> dbList = new List<DynamicBone>();
                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                {
                    if (db == null)
                        continue;

                    if (db.m_Root != null)
                    {
                        int colliderIndex = dynamicBonesList.IndexOf(db.m_Root.name);
                        if (colliderIndex >= 0)
                        {
                            DynamicBoneCollider dbc = female.GetComponentsInChildren<DynamicBoneCollider>().Where(x => x.name.Contains(colliderList[colliderIndex])).FirstOrDefault();
                            if (dbc == null)
                            {
                                Transform colliderTransform = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(colliderList[colliderIndex])).FirstOrDefault();

                                if (colliderTransform != null)
                                {
                                    Console.WriteLine("collider " + colliderTransform.name);

                                    dbc = colliderTransform.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                                    dbc.m_Bound = DynamicBoneColliderBase.Bound.Inside;
                                    dbc.m_Direction = DynamicBoneColliderBase.Direction.Y;

                                    if (kokanBone == null)
                                    {
                                        dbc.m_Height = colliderHeightList[colliderIndex];
                                        dbc.m_Radius = colliderRadiusList[colliderIndex];
                                    }
                                    else
                                    {
                                        if (colliderIndex < 3)
                                        {
                                            dbc.m_Height = colliderHeightList[colliderIndex] * kokanBone.lossyScale.z;
                                            dbc.m_Radius = colliderRadiusList[colliderIndex] * kokanBone.lossyScale.z;
                                            db.m_Radius *= kokanBone.lossyScale.z;
                                        }
                                        else
                                        {

                                            dbc.m_Height = colliderHeightList[colliderIndex] * (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
                                            dbc.m_Radius = colliderRadiusList[colliderIndex] * (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
                                            db.m_Radius *= (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
                                        }
                                    }
                                }
                            }

                            if (_use_bounding_colliders.Value && dbc != null)
                                db.m_Colliders.Add(dbc);
                        }
                        else if (kokanBone != null)
                        {
                            db.m_Radius *= kokanBone.lossyScale.x;
                        }

                        db.UpdateParameters();
                        dbList.Add(db);
                    }
                }

                kokanBones[femaleNum] = dbList;
                femaleNum++;
            }

            kokanBoneAdjustTarget = fem_list[0].GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Kokan")).FirstOrDefault();

            Console.WriteLine("AddColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_PreChangeAnimation(HScene.AnimationListInfo _info)
        {
            adjustFAnimation = false;

            if (!inHScene)
                return;

            for (int maleNum = 0; maleNum < male_list.Length; maleNum++)
            	changingAnimations[maleNum] = true;

            if (_info.fileFemale == null || fem_list == null || fem_list[0] == null)
                return;

            for (int i = 0; i < animationAdjustmentList.Count; i++)
            {
                if (animationAdjustmentList[i] == _info.fileFemale)
                {
                    if (kokanBoneAdjustTarget != null)
                        adjustFAnimation = true;
              
                    return;
                }
            }
        }
		
        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance, System.Text.StringBuilder ___assetName, ChaControl ___male)
        {
            if (loadingCharacter || !inHScene || __instance == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 99)
                maleNum = 1;

            if (!bDansFound[maleNum])
                return;

            b2MAnimation = false;
            if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
                b2MAnimation = true;

            if (!b2MAnimation && male_list[1] != null)
            {
                RemoveDanColliders(1, 0);
                RemoveFingerColliders(1, 0);
            }

            if (maleNum == 1 && b2MAnimation == false)
                return;

            targetF[maleNum] = __instance.numFemale;
            if (targetF[maleNum] >= constrainPoints.Length)
                targetF[maleNum] = 0;

            if (!bHPointsFound[targetF[maleNum]])
                return;

            SetupNewDanTarget(__instance, maleNum);
			lastDanPostion[maleNum] = danPoints[maleNum].danEnd.position;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance, ChaControl ___male)
        {
            if (___male == null || loadingCharacter || !inHScene)
                return;

            int maleNum = 0;
            if (___male.chaID != 99)
            {
                if (!b2MAnimation)
                    return;
                maleNum = 1;
            }

            if (!bDansFound[maleNum] || !bHPointsFound[targetF[maleNum]])
                return;

            if (_kokan_adjust.Value && adjustFAnimation && !hScene.NowChangeAnim)
                AdjustFemaleAnimation();

			if (changingAnimations[maleNum] && !hScene.NowChangeAnim)
		        SetupNewDanTarget(__instance, maleNum);

            if (bDanPenetration[maleNum] || bDanPull[maleNum])
                SetDanTarget(maleNum);

            lastDanPostion[maleNum] = danPoints[maleNum].danEnd.position;
        }

        private static void SetupNewDanTarget(H_Lookat_dan lookAtDan, int maleNum)
        {
            referenceLookAtTarget[maleNum] = null;
            bDanPenetration[maleNum] = false;
            bDanPull[maleNum] = false;
            changingAnimations[maleNum] = false;
            lastDanPostion[maleNum] = danPoints[maleNum].danEnd.position;

            if (lookAtDan != null && lookAtDan.strPlayMotion != null)
            {
                if (lookAtDan.strPlayMotion.Contains("Pull"))
                {
                    bDanPull[maleNum] = true;
                }
                else if (lookAtDan.bTopStick && lookAtDan.transLookAtNull != null && (lookAtDan.transLookAtNull.name == kokan_target || lookAtDan.transLookAtNull.name == ana_target || lookAtDan.transLookAtNull.name == head_target))
                {
                    bDanPenetration[maleNum] = true;
                    referenceLookAtTarget[maleNum] = lookAtDan.transLookAtNull;

                    if (referenceLookAtTarget[maleNum].name == kokan_target && bpKokanTarget[targetF[maleNum]] != null)
                        referenceLookAtTarget[maleNum] = bpKokanTarget[targetF[maleNum]];
                }
            }

            if (referenceLookAtTarget[maleNum] == null || referenceLookAtTarget[maleNum].name != ana_target && referenceLookAtTarget[maleNum].name != head_target && referenceLookAtTarget[maleNum].name != chest_target_00 && referenceLookAtTarget[maleNum].name != chest_target_01)
            {
                AddDanColliders(maleNum, targetF[maleNum]);

                if (_use_finger_colliders.Value && (referenceLookAtTarget[maleNum] == null || referenceLookAtTarget[maleNum].name != kokan_target))
                    AddFingerColliders(maleNum, targetF[maleNum]);
                else
                    RemoveFingerColliders(maleNum, targetF[maleNum]);
            }
            else
            {
                RemoveDanColliders(maleNum, targetF[maleNum]);
                RemoveFingerColliders(maleNum, targetF[maleNum]);
            }
        }

        private static void AddDanColliders(int maleNum, int femaleNum)
        {
            foreach (DynamicBone db in kokanBones[femaleNum])
            {
                if (danCollider[maleNum] != null && !db.m_Colliders.Contains(danCollider[maleNum]))
                    db.m_Colliders.Add(danCollider[maleNum]);
            }
        }

        private static void RemoveDanColliders(int maleNum, int femaleNum)
        {
            foreach (DynamicBone db in kokanBones[femaleNum])
            {
                if (danCollider[maleNum] != null && db.m_Colliders.Contains(danCollider[maleNum]))
                    db.m_Colliders.Remove(danCollider[maleNum]);
            }
        }

        private static void AddFingerColliders(int maleNum, int femaleNum)
        {
            foreach (DynamicBone db in kokanBones[femaleNum])
            {
                if (indexCollider[maleNum] != null && !db.m_Colliders.Contains(indexCollider[maleNum]))
                    db.m_Colliders.Add(indexCollider[maleNum]);

                if (middleCollider[maleNum] != null && !db.m_Colliders.Contains(middleCollider[maleNum]))
                    db.m_Colliders.Add(middleCollider[maleNum]);

                if (ringCollider[maleNum] != null && !db.m_Colliders.Contains(ringCollider[maleNum]))
                    db.m_Colliders.Add(ringCollider[maleNum]);
            }
        }

        private static void RemoveFingerColliders(int maleNum, int femaleNum)
        {
            foreach (DynamicBone db in kokanBones[femaleNum])
            {
                if (indexCollider[maleNum] != null && db.m_Colliders.Contains(indexCollider[maleNum]))
                    db.m_Colliders.Remove(indexCollider[maleNum]);

                if (middleCollider[maleNum] != null && db.m_Colliders.Contains(middleCollider[maleNum]))
                    db.m_Colliders.Remove(middleCollider[maleNum]);

                if (ringCollider[maleNum] != null && db.m_Colliders.Contains(ringCollider[maleNum]))
                    db.m_Colliders.Remove(ringCollider[maleNum]);
            }
        }

        private static void SetDanTarget(int maleNum)
        {
            if (referenceLookAtTarget[maleNum] == null)
                return;

            Vector3 dan101_pos = danPoints[maleNum].danStart.position;
            Vector3 lookTarget = referenceLookAtTarget[maleNum].position;

            if (referenceLookAtTarget[maleNum].name == kokan_target || referenceLookAtTarget[maleNum].name == bp_kokan_target)
            {
                lookTarget += (referenceLookAtTarget[maleNum].forward * _kokanForwardOffset.Value) + (referenceLookAtTarget[maleNum].up * _kokanUpOffset.Value);

                if (_kokan_adjust.Value && adjustFAnimation)
                {
                    lookTarget += referenceLookAtTarget[maleNum].forward * _kokan_adjust_position_z.Value;
                }
            }
            if (referenceLookAtTarget[maleNum].name == head_target)
                lookTarget += (referenceLookAtTarget[maleNum].forward * _headForwardOffset.Value) + (referenceLookAtTarget[maleNum].up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            Vector3 danVector = Vector3.Normalize(lookTarget - dan101_pos);
            Vector3 dan109_pos = dan101_pos + danVector * baseDanLength[maleNum];

            if (bDanPenetration[maleNum])
            {
                if (referenceLookAtTarget[maleNum].name == kokan_target || referenceLookAtTarget[maleNum].name == ana_target || referenceLookAtTarget[maleNum].name == bp_kokan_target)
                {
                    List<Vector3> frontHitPoints = new List<Vector3>();
                    List<Vector3> backHitPoints = new List<Vector3>();

                    for (int index = 0; index < constrainPoints[targetF[maleNum]].frontConstrainPoints.Count; index++)
                    {
                        if (frontHPointsInward[index])
                            frontHitPoints.Add(constrainPoints[targetF[maleNum]].frontConstrainPoints[index].position + (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].frontConstrainPoints[index].forward);
                        else
                            frontHitPoints.Add(constrainPoints[targetF[maleNum]].frontConstrainPoints[index].position - (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].frontConstrainPoints[index].forward);
                    }
                    for (int index = 0; index < constrainPoints[targetF[maleNum]].backConstrainPoints.Count; index++)
                    {
                        if (backHPointsInward[index])
                            backHitPoints.Add(constrainPoints[targetF[maleNum]].backConstrainPoints[index].position - (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].backConstrainPoints[index].forward);
                        else
                            backHitPoints.Add(constrainPoints[targetF[maleNum]].backConstrainPoints[index].position + (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints[targetF[maleNum]].backConstrainPoints[index].forward);
                    }

                    float danLength = baseDanLength[maleNum];
                    Plane kokanPlane = new Plane(danPoints[maleNum].danStart.forward, lookTarget);

                    if (baseDanLength[maleNum] > distDan101ToTarget)
                        danLength = baseDanLength[maleNum] - (baseDanLength[maleNum] - distDan101ToTarget) * _dan_softness[maleNum].Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = baseDanLength[maleNum] * (1 - _dan_softness[maleNum].Value);

                    float minDanLength = distDan101ToTarget + (danLength * (1 - _allow_telescope_percent[maleNum].Value));

                    if (minDanLength > danLength)
                        minDanLength = danLength;

                    if (_force_telescope[maleNum].Value)
                        danLength = minDanLength;

                    dan109_pos = dan101_pos + danVector * danLength;

                    bool bHitPointFound = false;
                    bool bConstrainPastNearSide = true;
                    bool bConstrainPastFarSide = false;
                    Vector3 adjustedDanPos = dan109_pos;

                    for (int index = 1; index < constrainPoints[targetF[maleNum]].frontConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints[targetF[maleNum]].frontConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints[targetF[maleNum]].frontConstrainPoints[index].right;

                        if (frontHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (frontHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(frontHitPoints[index - 1], firstVectorRight, frontHitPoints[index], secondVectorRight);

                        if (index == constrainPoints[targetF[maleNum]].frontConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                    }

                    bConstrainPastFarSide = false;
                    bConstrainPastNearSide = true;
                    for (int index = 1; index < constrainPoints[targetF[maleNum]].backConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints[targetF[maleNum]].backConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints[targetF[maleNum]].backConstrainPoints[index].right;

                        if (!backHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (!backHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(backHitPoints[index - 1], firstVectorRight, backHitPoints[index], secondVectorRight);

                        if (index == constrainPoints[targetF[maleNum]].backConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                   }
                   dan109_pos = adjustedDanPos;
                }
                else if (referenceLookAtTarget[maleNum].name == head_target)
                {
                    float danLength;
                    float max_dist;

                    if (Vector3.Distance(dan101_pos, constrainPoints[targetF[maleNum]].headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints[targetF[maleNum]].headConstrainPoint.position))
                    {
                        danLength = baseDanLength[maleNum] * (1 - _dan_softness[maleNum].Value);
                        max_dist = Vector3.Distance(dan101_pos, constrainPoints[targetF[maleNum]].headConstrainPoint.position);
                    }
                    else
                    {
                        if (baseDanLength[maleNum] > distDan101ToTarget)
                            danLength = baseDanLength[maleNum] - (baseDanLength[maleNum] - distDan101ToTarget) * _dan_softness[maleNum].Value;
                        else
                            danLength = baseDanLength[maleNum];
                        max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints[targetF[maleNum]].headConstrainPoint.position);
                    }

                    if (danLength > max_dist)
                        danLength = max_dist;

                    dan109_pos = dan101_pos + danVector * danLength;
                }
            }

            Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);
            Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, danPoints[maleNum].danTop.right));

            danPoints[maleNum].danStart.rotation = danQuaternion;
            danPoints[maleNum].danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);
        }

        //-- IK Solver Patch --//
        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "LateUpdate")]
        public static void HScene_PreLateUpdate()
        {
            if (hScene == null)
                return;

            EarlyAimDan(0);
            if (b2MAnimation)
                EarlyAimDan(1);
        }

        private static void EarlyAimDan(int maleNum)
        {
            if (referenceLookAtTarget[maleNum] != null)
            {
                Vector3 danForwardVector = Vector3.Normalize(lastDanPostion[maleNum] - danPoints[maleNum].danStart.position);
                Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, danPoints[maleNum].danTop.right));

                danPoints[maleNum].danStart.rotation = danQuaternion;
            }
        }

        private static void AdjustFemaleAnimation()
        {
            kokanBoneAdjustTarget.localPosition += new Vector3(0, _kokan_adjust_position_y.Value, _kokan_adjust_position_z.Value);
            kokanBoneAdjustTarget.localEulerAngles += new Vector3(_kokan_adjust_rotation_x.Value, 0, 0);
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            if (lsm != LoadSceneMode.Single)
                return;
            
            if (scene.name == "HScene")
            {
                harmony.PatchAll(typeof(HS2_BetterPenetration));

                Console.WriteLine("HS2_BetterPenetration: Searching for Uncensor Selector");
                Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo pluginInfo);
                if (pluginInfo != null && pluginInfo.Instance != null)
                {
                    Type nestedType = pluginInfo.Instance.GetType().GetNestedType("UncensorSelectorController", AccessTools.all);
                    if (nestedType != null)
                    {
                        Console.WriteLine("HS2_BetterPenetration: UncensorSelector found, trying to patch");
                        MethodInfo methodInfo = AccessTools.Method(nestedType, "ReloadCharacterBody", null, null);
                        if (methodInfo != null)
                        {
                            harmony.Patch(methodInfo, new HarmonyMethod(typeof(HS2_BetterPenetration), "BeforeCharacterReload"), new HarmonyMethod(typeof(HS2_BetterPenetration), "AfterCharacterReload"), null, null);
                            Console.WriteLine("HS2_BetterPenetration: UncensorSelector patched correctly");
                        }
                    }
                }

                patched = true;
            }
            else if (patched)
            {
                harmony.UnpatchAll(nameof(HS2_BetterPenetration));
                patched = false;
            }
        }

        private static void SceneManager_sceneUnloaded(Scene scene)
        {
            if (scene.name != "HScene")
                return;
                
            Console.WriteLine("SceneManager_sceneUnloaded");

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
                Destroy(danCollider[i]);

            for (int i = 0; i < indexCollider.Length; i++)
                Destroy(indexCollider[i]);

            for (int i = 0; i < middleCollider.Length; i++)
                Destroy(middleCollider[i]);

            for (int i = 0; i < ringCollider.Length; i++)
                Destroy(ringCollider[i]);

            Console.WriteLine("Clearing females list");
            Array.Clear(fem_list, 0, fem_list.Length);
            Console.WriteLine("Clearing males list");
            Array.Clear(male_list, 0, male_list.Length);

            if (patched)
            {
                harmony.UnpatchAll(nameof(HS2_BetterPenetration));
                patched = false;
            }
        }
    }
}