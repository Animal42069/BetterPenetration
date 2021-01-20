using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Manager;
using System.Reflection;
using AIChara;

namespace AI_BetterPenetration
{
    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.1")]
    [BepInProcess("AI-Syoujyo")]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.5.2.0";
        private static Harmony harmony;
        private static HScene hScene;
        private static bool patched = false;

        private static ConfigEntry<float> _dan_softness;
        private static ConfigEntry<float> _dan_collider_headlength;
        private static ConfigEntry<float> _dan_collider_radius;
        private static ConfigEntry<float> _dan_collider_verticalcenter;
        private static ConfigEntry<float> _finger_collider_length;
        private static ConfigEntry<float> _finger_collider_radius;
        private static ConfigEntry<float> _allow_telescope_percent;
        private static ConfigEntry<bool> _force_telescope;

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static ConfigEntry<bool> _kokan_adjust;
        private static ConfigEntry<bool> _use_finger_colliders;
 //       private static ConfigEntry<bool> _use_bounding_colliders;
        private static ConfigEntry<float> _kokan_adjust_position_y;
        private static ConfigEntry<float> _kokan_adjust_position_z;
        private static ConfigEntry<float> _kokan_adjust_rotation_x;
        private static readonly List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static readonly List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;
        private static bool loadingCharacter = false;
        private static bool adjustFAnimation;
        private static Transform kokanBoneAdjustTarget;

        public static ChaControl[] fem_list;
        public static ChaControl[] male_list;
        public static List<DynamicBone> kokanBones = new List<DynamicBone>();
        public static DynamicBoneCollider danCollider = new DynamicBoneCollider();
        public static DynamicBoneCollider indexCollider = new DynamicBoneCollider();
        public static DynamicBoneCollider middleCollider = new DynamicBoneCollider();
        public static DynamicBoneCollider ringCollider = new DynamicBoneCollider();

        private static bool bDansFound = false;
        private static DanPoints danPoints;
        private static float baseDanLength = 1.8f;
        private static bool bDanPenetration = false;
        private static Transform referenceLookAtTarget;
        private static Transform bpKokanTarget;
        private static bool changingAnimations = false;
        private static bool bHPointsFound = false;
        private static ConstrainPoints constrainPoints;
        private static Vector3 lastDanPostion;

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
            _finger_collider_length = Config.Bind("Male Options", "Finger Collider: Length", 0.6f, "Lenght of the finger colliders.");
            _finger_collider_radius = Config.Bind("Male Options", "Finger Collider: Radius", 0.2f, "Radius of the finger colliders.");
            _dan_collider_headlength = Config.Bind("Male Options", "Penis Collider: Length of Head", 0.35f, "Distance from the center of the head bone to the tip, used for collision purposes.");
            _dan_collider_radius = Config.Bind("Male Options", "Penis Collider: Radius of Shaft", 0.32f, "Radius of the shaft collider.");
            _dan_collider_verticalcenter = Config.Bind("Male Options", "Penis Collider: Vertical Center", -0.03f, "Vertical Center of the shaft collider");
            _dan_softness = Config.Bind("Male Options", "Penis: Softness", 0.15f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
            _allow_telescope_percent = Config.Bind("Male Options", "Limiter: Telescope Threshold", 0.6f, "Allow the penis to begin telescoping after it has penetrated a certain amount. 0 = never telescope, 0.5 = allow telescoping after the halfway point, 1 = always allow telescoping.");
            _force_telescope = Config.Bind("Male Options", "Limiter: Telescope Always", true, "Force the penis to always telescope at the threshold point, instead of only doing it when it prevents clipping.");

            _dan_collider_radius.SettingChanged += delegate
            {
                if (inHScene && danCollider != null && bDansFound)
                {
                    danCollider.m_Radius = _dan_collider_radius.Value;
                }
            };

            _dan_collider_headlength.SettingChanged += delegate
            {
                if (inHScene && danCollider != null && bDansFound)
                {
                	danCollider.m_Height = baseDanLength + (_dan_collider_headlength.Value  * 2);
                }
            };

            _finger_collider_radius.SettingChanged += delegate
            {
                    if (inHScene && indexCollider != null && middleCollider != null && ringCollider != null)
                    {
                        indexCollider.m_Radius = _finger_collider_radius.Value;
                        middleCollider.m_Radius = _finger_collider_radius.Value;
                        ringCollider.m_Radius = _finger_collider_radius.Value;
                    }
            };

            _finger_collider_length.SettingChanged += delegate
            {
                    if (inHScene && indexCollider != null && middleCollider != null && ringCollider != null)
                    {
                        indexCollider.m_Height = _finger_collider_length.Value;
                        middleCollider.m_Height = _finger_collider_length.Value;
                        ringCollider.m_Height = _finger_collider_length.Value;
                    }
            };



            _clipping_depth = Config.Bind("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int femaleNum = 0; femaleNum < frontOffsets.Count; femaleNum++)
                _front_collision_point_offset.Add(Config.Bind("Female Options", "Clipping Offset: Front Collision " + femaleNum, frontOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            for (int femaleNum = 0; femaleNum < backOffsets.Count; femaleNum++)
                _back_collision_point_offset.Add(Config.Bind("Female Options", "Clipping Offset: Back Collision " + femaleNum, backOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind("Female Options", "Target Offset: Vagina Vertical", -0.15f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind("Female Options", "Target Offset: Mouth Vertical", 0.03f, "Vertical offset of the mouth target");
            _use_finger_colliders = Config.Bind("Female Options", "Colliders: Use Finger Colliders", true, "Use finger colliders");
//            _use_bounding_colliders = Config.Bind("Female Options", "Colliders: Use Bounding Colliders", false, "Use internal bounding colliders to help animate correctly");
            _kokan_adjust = Config.Bind("Female Options", "Joint Adjustment: Missionary Correction", false, "NOTE: There is an Illusion bug that causes the vagina to appear sunken in certain missionary positions.  It is best to use Advanced Bonemod and adjust your female character's cf_J_Kokan Offset Y to 0.001.  If you don't do that, enabling this option will attempt to fix the problem by guessing where the bone should be");
            _kokan_adjust_position_y = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Y", -0.075f, "Amount to adjust the Vagina bone position Y for certain Missionary positions to correct its appearance");
            _kokan_adjust_position_z = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Z", 0.0625f, "Amount to adjust the Vagina bone position Z for certain Missionary positions to correct its appearance");
            _kokan_adjust_rotation_x = Config.Bind("Female Options", "Joint Adjustment: Missionary Rotation X", 10.0f, "Amount to adjust the Vagina bone rotation X for certain Missionary positions to correct its appearance");

            harmony = new Harmony("AI_BetterPenetration");
        }

        public static void BeforeCharacterReload()
        { 
            if (!inHScene)
                return;

            loadingCharacter = true;

            for (int maleNum = 0; maleNum < male_list.Length; maleNum++)
                changingAnimations = true;
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

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        public static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            RemovePCollidersFromCoordinate(__instance);
        }

        private static void RemovePCollidersFromCoordinate(ChaControl character)
        {
            var dynamicBones = character.GetComponentsInChildren<DynamicBone>(true);

            if (dynamicBones == null)
                return;

            foreach (var dynamicBone in dynamicBones)
            {
                if (dynamicBone == null || dynamicBone.m_Colliders == null || (dynamicBone.name != null && dynamicBone.name.Contains("Vagina")))
                    continue;

                for (int collider = 0; collider < dynamicBone.m_Colliders.Count; collider++)
                {
                    if (dynamicBone.m_Colliders[collider] != null && dynamicBone.m_Colliders[collider].name != null && dynamicBone.m_Colliders[collider].name.Contains("Vagina"))
                        dynamicBone.m_Colliders[collider] = null;
                }
            }
        }

        public static void AddPColliders(bool setDanLength)
        {
            if (hScene == null)
                return;

            male_list = hScene.GetMales().Where(male => male != null).ToArray();
            fem_list = hScene.GetFemales().Where(female => female != null).ToArray();

            danCollider = new DynamicBoneCollider();
			referenceLookAtTarget = null;
            constrainPoints = new ConstrainPoints();
            kokanBones = new List<DynamicBone>();

            bDansFound = false;
            foreach (var male in male_list.Where(male => male != null))
            {
                if (!bDansFound)
                {
	                Transform dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_base)).FirstOrDefault();
	                Transform dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_head)).FirstOrDefault();
	                Transform danTop = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(dan_sack)).FirstOrDefault();
	                Transform index = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(index_finger)).FirstOrDefault();
	                Transform middle = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(middle_finger)).FirstOrDefault();
	                Transform ring = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(ring_finger)).FirstOrDefault();

                    bDanPenetration = false;
                    if (dan101 != null && dan109 != null && danTop != null)
	                {
		                    if (setDanLength)
		                    {
		                    	baseDanLength = Vector3.Distance(dan101.position, dan109.position);
		                        if (Geometry.ApproximatelyZero(baseDanLength))
		                            baseDanLength = 1.8f;
		                    }

	                    	danPoints = new DanPoints(dan101, dan109, danTop);
	                    	lastDanPostion = new Vector3(0,0,0);
	                        bDansFound = true;

	                        danCollider = dan101.GetComponent<DynamicBoneCollider>();

	                        if (danCollider == null)
	                            danCollider = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                    
		                    danCollider.m_Direction = DynamicBoneColliderBase.Direction.Z;
		                    danCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
		                    danCollider.m_Center = new Vector3(0, _dan_collider_verticalcenter.Value, baseDanLength / 2);
		                    danCollider.m_Radius = _dan_collider_radius.Value;
		                    danCollider.m_Height = baseDanLength + (_dan_collider_headlength.Value * 2);
				
	                }

	                if (index != null && middle != null && ring != null)
	                {
	                    indexCollider = index.GetComponent<DynamicBoneCollider>();
	                    middleCollider = middle.GetComponent<DynamicBoneCollider>();
	                    ringCollider = ring.GetComponent<DynamicBoneCollider>();

	                    if (indexCollider == null)
	                        indexCollider = index.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

	                    if (middleCollider == null)
	                        middleCollider = middle.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

	                    if (ringCollider == null)
	                        ringCollider = ring.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

	                    indexCollider.m_Direction = DynamicBoneColliderBase.Direction.X;
	                    indexCollider.m_Center = new Vector3(0, 0, 0);
	                    indexCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
	                    indexCollider.m_Radius = _finger_collider_radius.Value;
	                    indexCollider.m_Height = _finger_collider_length.Value;

	                    middleCollider.m_Direction = DynamicBoneColliderBase.Direction.X;
	                    middleCollider.m_Center = new Vector3(0, 0, 0);
	                    middleCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
	                    middleCollider.m_Radius = _finger_collider_radius.Value;
	                    middleCollider.m_Height = _finger_collider_length.Value;

	                    ringCollider.m_Direction = DynamicBoneColliderBase.Direction.X;
	                    ringCollider.m_Center = new Vector3(0, 0, 0);
	                    ringCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
	                    ringCollider.m_Radius = _finger_collider_radius.Value;
	                    ringCollider.m_Height = _finger_collider_length.Value;
	                }

                    Console.WriteLine("bDansFound " + bDansFound);
                }
            }

            bHPointsFound = false;
            foreach (var female in fem_list.Where(female => female != null))
            {
                if (!bHPointsFound)
                {
                    List<Transform> frontHPoints = new List<Transform>();
                    List<Transform> backHPoints = new List<Transform>();
                    Transform hPointBackOfHead;

                    for (int index = 0; index < frontHPointsList.Count; index++)
                        frontHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(frontHPointsList[index])).FirstOrDefault());

                    for (int index = 0; index < backHPointsList.Count; index++)
                        backHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(backHPointsList[index])).FirstOrDefault());

                    hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(headHPoint)).FirstOrDefault();

                    bpKokanTarget = female.GetComponentsInChildren<Transform>(true).Where(x => x.name.Equals(bp_kokan_target)).FirstOrDefault();
                    if (bpKokanTarget != null)
                    {
                        if (bpKokanTarget.gameObject.activeSelf == false)
                            bpKokanTarget.gameObject.SetActive(true);
                        
                        Console.WriteLine("BP Target Found " + bpKokanTarget.name);
                        frontHPoints[0] = bpKokanTarget;
                    }

                    if (frontHPoints.Count == frontHPointsList.Count && backHPoints.Count == backHPointsList.Count && hPointBackOfHead != null)
                    {
                        bHPointsFound = true;
                        constrainPoints = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                    }

	                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>(true).Where(x => x.name != null && x.name.Contains("cf_J_Vagina")))
	                    db.m_Colliders.Clear();

	                Console.WriteLine("bHPointsFound " + bHPointsFound);

	                Transform kokanBone = female.GetComponentsInChildren<Transform>().Where(x => x.name != null && x.name.Contains("cf_J_Kokan")).FirstOrDefault();
	                List<DynamicBone> dbList = new List<DynamicBone>();
	                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>(true).Where(x => x.name != null && x.name.Contains("cf_J_Vagina")))
	                {
	                    if (db == null)
	                        continue;

                        if (db.enabled == false)
                            db.enabled = true;

	                    if (db.m_Root != null)
	                    {
	                        int colliderIndex = dynamicBonesList.IndexOf(db.m_Root.name);
	                        if (colliderIndex >= 0)
	                        {
	                        //    DynamicBoneCollider dbc = female.GetComponentsInChildren<DynamicBoneCollider>().Where(x => x.name.Contains(colliderList[colliderIndex])).FirstOrDefault();
	                         //   if (dbc == null)
	                         //   {
	                                Transform colliderTransform = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(colliderList[colliderIndex])).FirstOrDefault();

	                                if (colliderTransform != null)
	                                {
	                                    Console.WriteLine("collider " + colliderTransform.name);

	                               //     dbc = colliderTransform.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
	                               //     dbc.m_Bound = DynamicBoneColliderBase.Bound.Inside;
	                               //     dbc.m_Direction = DynamicBoneColliderBase.Direction.Y;

	                                    if (kokanBone == null)
	                                    {
	                              //          dbc.m_Height = colliderHeightList[colliderIndex];
	                              //          dbc.m_Radius = colliderRadiusList[colliderIndex];
	                                    }
	                                    else
	                                    {
	                                        if (colliderIndex < 3)
	                                        {
	                            //                dbc.m_Height = colliderHeightList[colliderIndex] * kokanBone.lossyScale.z;
	                              //              dbc.m_Radius = colliderRadiusList[colliderIndex] * kokanBone.lossyScale.z;
	                                            db.m_Radius *= kokanBone.lossyScale.z;
	                                        }
	                                        else
	                                        {

	                            //                dbc.m_Height = colliderHeightList[colliderIndex] * (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
	                            //                dbc.m_Radius = colliderRadiusList[colliderIndex] * (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
	                                            db.m_Radius *= (kokanBone.lossyScale.x + kokanBone.lossyScale.z) / 2;
	                                        }
	                                    }
	                                }
	                       //     }

	                     //       if (_use_bounding_colliders.Value && dbc != null)
	                   //             db.m_Colliders.Add(dbc);
	                        }
	                        else if (kokanBone != null)
	                        {
	                            db.m_Radius *= kokanBone.lossyScale.x;
	                        }

	                        db.UpdateParameters();
	                        dbList.Add(db);
	                    }
	                }

	                kokanBones = dbList;
	            }

            	kokanBoneAdjustTarget = fem_list[0].GetComponentsInChildren<Transform>().Where(x => x.name != null && x.name.Contains("cf_J_Kokan")).FirstOrDefault();
			}
            Console.WriteLine("AddColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_PreChangeAnimation(HScene.AnimationListInfo _info)
        {
            adjustFAnimation = false;

            if (!inHScene)
                return;

            changingAnimations = true;

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
        private static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance)
        {
            if (loadingCharacter || !inHScene || __instance == null || !bDansFound || !bHPointsFound)
                return;

            SetupNewDanTarget(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance)
        {
            if (loadingCharacter || !inHScene || !bDansFound || !bHPointsFound)
                return;

            if (_kokan_adjust.Value && adjustFAnimation && !hScene.NowChangeAnim)
                AdjustFemaleAnimation();

            if (changingAnimations && !hScene.NowChangeAnim)
                SetupNewDanTarget(__instance);

            SetDanTarget();

            lastDanPostion = danPoints.danEnd.position;
        }

        private static void SetupNewDanTarget(H_Lookat_dan lookAtDan)
        {
            referenceLookAtTarget = null;
            bDanPenetration = false;
            changingAnimations = false;
            lastDanPostion = new Vector3(0, 0, 0);

            if (lookAtDan == null || lookAtDan.strPlayMotion == null)
                return;

            if (lookAtDan.transLookAtNull == null || lookAtDan.transLookAtNull.name == kokan_target)
                AddDanColliders();
            else
                RemoveDanColliders();

            if (_use_finger_colliders.Value && lookAtDan.transLookAtNull == null)
                AddFingerColliders();
            else
                RemoveFingerColliders();

            if (lookAtDan.transLookAtNull == null)
                return;

            referenceLookAtTarget = lookAtDan.transLookAtNull;
            if (lookAtDan.bTopStick && referenceLookAtTarget.name == kokan_target && !lookAtDan.strPlayMotion.Contains("Idle") && !lookAtDan.strPlayMotion.Contains("Pull") && !lookAtDan.strPlayMotion.Contains("OUT") && bpKokanTarget != null)
                referenceLookAtTarget = bpKokanTarget;

            bDanPenetration = lookAtDan.bTopStick;
        }

        private static void AddDanColliders()
        {
            foreach (DynamicBone db in kokanBones)
            {
                if (danCollider != null && !db.m_Colliders.Contains(danCollider))
                    db.m_Colliders.Add(danCollider);
            }
        }

        private static void RemoveDanColliders()
        {
            foreach (DynamicBone db in kokanBones)
            {
                if (danCollider != null)
                    db.m_Colliders.Remove(danCollider);
            }
        }

        private static void AddFingerColliders()
        {
            foreach (DynamicBone db in kokanBones)
            {
                if (indexCollider != null && !db.m_Colliders.Contains(indexCollider))
                    db.m_Colliders.Add(indexCollider);

                if (middleCollider != null && !db.m_Colliders.Contains(middleCollider))
                    db.m_Colliders.Add(middleCollider);

                if (ringCollider != null && !db.m_Colliders.Contains(ringCollider))
                    db.m_Colliders.Add(ringCollider);
            }
        }

        private static void RemoveFingerColliders()
        {
            foreach (DynamicBone db in kokanBones)
            {
                if (indexCollider != null)
                    db.m_Colliders.Remove(indexCollider);

                if (middleCollider != null)
                    db.m_Colliders.Remove(middleCollider);

                if (ringCollider != null)
                    db.m_Colliders.Remove(ringCollider);
            }
        }

		private static void SetDanTarget()
        {
            if (referenceLookAtTarget == null)
                return;

            Vector3 dan101_pos = danPoints.danStart.position;
            Vector3 lookTarget = referenceLookAtTarget.position;

            if (referenceLookAtTarget.name == kokan_target)
                lookTarget += (referenceLookAtTarget.forward * _kokanForwardOffset.Value) + (referenceLookAtTarget.up * _kokanUpOffset.Value);
            else if (referenceLookAtTarget.name == head_target)
                lookTarget += (referenceLookAtTarget.forward * _headForwardOffset.Value) + (referenceLookAtTarget.up * _headUpOffset.Value);

            if (_kokan_adjust.Value && adjustFAnimation && (referenceLookAtTarget.name == kokan_target || referenceLookAtTarget.name == kokan_target))
                lookTarget += referenceLookAtTarget.forward * _kokan_adjust_position_z.Value;

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            Vector3 danVector = Vector3.Normalize(lookTarget - dan101_pos);
            Vector3 dan109_pos = dan101_pos + danVector * baseDanLength;

            if (bDanPenetration)
            {
                if (referenceLookAtTarget.name == kokan_target || referenceLookAtTarget.name == ana_target || referenceLookAtTarget.name == bp_kokan_target)
                {
                    List<Vector3> frontHitPoints = new List<Vector3>();
                    List<Vector3> backHitPoints = new List<Vector3>();

                    for (int index = 0; index < constrainPoints.frontConstrainPoints.Count; index++)
                    {
                        if (frontHPointsInward[index])
                            frontHitPoints.Add(constrainPoints.frontConstrainPoints[index].position + (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints.frontConstrainPoints[index].forward);
                        else
                            frontHitPoints.Add(constrainPoints.frontConstrainPoints[index].position - (_clipping_depth.Value + _front_collision_point_offset[index].Value) * constrainPoints.frontConstrainPoints[index].forward);
                    }
                    for (int index = 0; index < constrainPoints.backConstrainPoints.Count; index++)
                    {
                        if (backHPointsInward[index])
                            backHitPoints.Add(constrainPoints.backConstrainPoints[index].position - (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints.backConstrainPoints[index].forward);
                        else
                            backHitPoints.Add(constrainPoints.backConstrainPoints[index].position + (_clipping_depth.Value + _back_collision_point_offset[index].Value) * constrainPoints.backConstrainPoints[index].forward);
                    }

                    float danLength = baseDanLength;
                    Plane kokanPlane = new Plane(danPoints.danStart.forward, lookTarget);

                    if (baseDanLength > distDan101ToTarget)
                        danLength = baseDanLength - (baseDanLength - distDan101ToTarget) * _dan_softness.Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = baseDanLength * (1 - _dan_softness.Value);

                    float minDanLength = distDan101ToTarget + (danLength * (1 - _allow_telescope_percent.Value));

                    if (minDanLength > danLength)
                        minDanLength = danLength;

                    if (_force_telescope.Value)
                        danLength = minDanLength;

                    dan109_pos = dan101_pos + danVector * danLength;

                    bool bHitPointFound = false;
                    bool bConstrainPastNearSide = true;
                    bool bConstrainPastFarSide = false;
                    Vector3 adjustedDanPos = dan109_pos;

                    for (int index = 1; index < constrainPoints.frontConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints.frontConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints.frontConstrainPoints[index].right;

                        if (frontHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (frontHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(frontHitPoints[index - 1], firstVectorRight, frontHitPoints[index], secondVectorRight);

                        if (index == constrainPoints.frontConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                    }

                    bConstrainPastFarSide = false;
                    bConstrainPastNearSide = true;
                    for (int index = 1; index < constrainPoints.backConstrainPoints.Count; index++)
                    {
                        if (bHitPointFound)
                            break;

                        Vector3 firstVectorRight = constrainPoints.backConstrainPoints[index - 1].right;
                        Vector3 secondVectorRight = constrainPoints.backConstrainPoints[index].right;

                        if (!backHPointsInward[index - 1])
                            firstVectorRight = -firstVectorRight;

                        if (!backHPointsInward[index])
                            secondVectorRight = -secondVectorRight;

                        TwistedPlane hPlane = new TwistedPlane(backHitPoints[index - 1], firstVectorRight, backHitPoints[index], secondVectorRight);

                        if (index == constrainPoints.backConstrainPoints.Count - 1)
                            bConstrainPastFarSide = true;

                        adjustedDanPos = hPlane.ConstrainLineToTwistedPlane(dan101_pos, adjustedDanPos, ref danLength, minDanLength, ref bConstrainPastNearSide, bConstrainPastFarSide, out bHitPointFound);
                   }
                   dan109_pos = adjustedDanPos;
                }
                else if (referenceLookAtTarget.name == head_target)
                {
                    float danLength;
                    float max_dist;

                    if (Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position))
                    {
                        danLength = baseDanLength * (1 - _dan_softness.Value);
                        max_dist = Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position);
                    }
                    else
                    {
                        if (baseDanLength > distDan101ToTarget)
                            danLength = baseDanLength - (baseDanLength - distDan101ToTarget) * _dan_softness.Value;
                        else
                            danLength = baseDanLength;
                        max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position);
                    }

                    if (danLength > max_dist)
                        danLength = max_dist;

                    dan109_pos = dan101_pos + danVector * danLength;
                }
            }
            else if ((referenceLookAtTarget.name == kokan_target) && (baseDanLength > distDan101ToTarget))
            {
                float danLength = baseDanLength - (baseDanLength - distDan101ToTarget) * _dan_softness.Value;
                float minDanLength = distDan101ToTarget + (danLength * (1 - _allow_telescope_percent.Value));

                if (minDanLength > danLength)
                    minDanLength = danLength;

                if (_force_telescope.Value)
                    danLength = minDanLength;

                dan109_pos = dan101_pos + danVector * danLength;
            }

            Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);
            Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, danPoints.danTop.right));

            danPoints.danStart.rotation = danQuaternion;
            danPoints.danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            Console.WriteLine("HScene::EndProc");

            EndHScene();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProcADV")]
        public static void HScene_EndProcADV_Patch()
        {
            Console.WriteLine("HScene::EndProcADV");

            EndHScene();
        }

        private static void AdjustFemaleAnimation()
        {
            kokanBoneAdjustTarget.localPosition += new Vector3(0, _kokan_adjust_position_y.Value, _kokan_adjust_position_z.Value);
            kokanBoneAdjustTarget.localEulerAngles += new Vector3(_kokan_adjust_rotation_x.Value, 0, 0);
        }

        private static void EndHScene()
        {
            inHScene = false;
            bDansFound = false;
            bDanPenetration = false;
            bHPointsFound = false;

            foreach (var lookat in hScene.ctrlLookAts)
            {
                if (lookat == null)
                    continue;

                lookat.transLookAtNull = null;
            }

            if (kokanBones.Any())
            {
                foreach (DynamicBone kokanBone in kokanBones)
                {
                    if (kokanBone != null)
                    {
                        Console.WriteLine("Clearing colliders from " + kokanBone.m_Root.name);
                        kokanBone.m_Colliders.Clear();
                    }
                }
            }

            Destroy(danCollider);
            Destroy(indexCollider);
            Destroy(middleCollider);
            Destroy(ringCollider);
            Console.WriteLine("Clearing females list");
            Array.Clear(fem_list, 0, fem_list.Length);
            Console.WriteLine("Clearing males list");
            Array.Clear(male_list, 0, male_list.Length);
        }

		private void Update()
        {
            var isHScene = HSceneManager.isHScene;
            
            if (isHScene && !patched)
                HScene_sceneLoaded(true);
            else if (!isHScene && patched)
                HScene_sceneLoaded(false);

            if (hScene == null)
                return;
		}

        private static void HScene_sceneLoaded(bool loaded)
        {
            patched = loaded;

            if (loaded)
            {
                harmony.PatchAll(typeof(AI_BetterPenetration));

                Console.WriteLine("AI_BetterPenetration: Searching for Uncensor Selector");
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
                            harmony.Patch(methodInfo, new HarmonyMethod(typeof(AI_BetterPenetration), "BeforeCharacterReload"), new HarmonyMethod(typeof(AI_BetterPenetration), "AfterCharacterReload"), null, null);
                            Console.WriteLine("HS2_BetterPenetration: UncensorSelector patched correctly");
                        }
                    }
                }
            }
            else
            {
                harmony.UnpatchAll(nameof(AI_BetterPenetration));
            }
        }
    }
}