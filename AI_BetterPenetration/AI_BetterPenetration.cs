using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Manager;

namespace AI_BetterPenetration
{
    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    [BepInProcess("AI-Syoujyo")]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.2.1.0";
        private static Harmony harmony;
		private static HScene hScene;
        private static bool patched;
		
        private static ConfigEntry<float> _dan_length;
        private static ConfigEntry<float> _dan_girth;
        private static ConfigEntry<float> _dan_sack_size;
        private static ConfigEntry<float> _dan_softness;
        private static ConfigEntry<float> _dan_collider_headlength;
        private static ConfigEntry<float> _dan_collider_radius;
        private static ConfigEntry<float> _dan_collider_verticalcenter;
        private static ConfigEntry<float> _allow_telescope_percent;
        private static ConfigEntry<bool> _force_telescope;

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static readonly List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static readonly List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;

        public static AIChara.ChaControl[] fem_list;
        public static AIChara.ChaControl[] male_list;
        public static List<DynamicBone> kokanBones = new List<DynamicBone>();
        public static DynamicBoneCollider danCollider = new DynamicBoneCollider();

        private static bool bDansFound = false;
        private static DanPoints danPoints;
        private static bool bDanPenetration = false;
        private static Transform referenceLookAtTarget;
        private static Transform bpKokanTarget;
        private static float lastDanLength;
        private static Vector3 lastDanVector;
        private static Quaternion lastDanRotation;
        private static Vector3 lastDan109Position;
        private static float lastDan101TargetDistance;
        private static float lastAdjustTime;
        private static bool changingAnimations = false;
        private static bool bHPointsFound = false;
        private static ConstrainPoints constrainPoints;

        private const string head_target = "k_f_head_00";
        private const string chest_target = "k_f_spine03_00";
        private const string kokan_target = "k_f_kokan_00";
        private const string bp_kokan_target = "cf_J_Vagina_root";
        private const string ana_target = "k_f_ana_00";
        private const string dan_base = "cm_J_dan101_00";
        private const string dan_head = "cm_J_dan109_00";
        private const string dan_sack = "cm_J_dan_f_top";

        private const string headHPoint = "cf_J_Head";
        private static readonly string[] frontHPointsList = { kokan_target, "cf_J_sk_00_02", "N_Waist_f", "k_f_spine03_03" };
        private static readonly string[] backHPointsList = { ana_target, "cf_J_sk_04_02", "N_Waist_b", "N_Back" };
        private static readonly float[] frontOffsets = { -0.35f, 0.25f, 0f, -0.65f };
        private static readonly float[] backOffsets = { -0.05f, 0.25f, 0.05f, 0.05f };
        private static readonly bool[] frontHPointsInward = { false, false, false, false };
        private static readonly bool[] backHPointsInward = { false, false, true, true };

        private static readonly string[] colliderList = { "cf_J_Vagina_Collider_B", "cf_J_Vagina_Collider_F", "cf_J_Vagina_Collider_Inner_F", "cf_J_Vagina_Collider_L.005", "cf_J_Vagina_Collider_R.005" };
        private static readonly string[] dynamicBonesList = { "cf_J_Vagina_Pivot_B", "cf_J_Vagina_Pivot_F", "cf_J_Vagina_Pivot_Inner_F", "cf_J_Vagina_Pivot_L.005", "cf_J_Vagina_Pivot_R.005" };
        private static readonly float[] colliderHeightList = { 0.39f, 0.19f, 0.34f, 0.39f, 0.39f };
        private static readonly float[] colliderRadiusList = { 0.0021f, 0.0011f, 0.0011f, 0.0021f, 0.0021f };

        private void Awake()
        {
            _dan_collider_headlength = Config.Bind<float>("Male Options", "Collider: Length of Head", 0.4f, "Distance from the center of the head bone to the tip, used for collision purposes.");
            _dan_collider_radius = Config.Bind<float>("Male Options", "Collider: Radius of Shaft", 0.32f, "Radius of the shaft collider.");
            _dan_collider_verticalcenter = Config.Bind<float>("Male Options", "Collider: Vertical Center", -0.03f, "Vertical Center of the shaft collider");
            _dan_length = Config.Bind<float>("Male Options", "Penis: Length", 1.75f, "Set the length of the penis.  Apparent Length is about 0.2 larget than this, depending on uncensor.  2.0 is about 8 inches or 20 cm.");
            _dan_girth = Config.Bind<float>("Male Options", "Penis: Girth", 1.0f, "Set the scale of the circumference of the penis.");
            _dan_sack_size = Config.Bind<float>("Male Options", "Penis: Sack Size", 1.0f, "Set the scale (size) of the sack");
            _dan_softness = Config.Bind<float>("Male Options", "Penis: Softness", 0.15f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");
            _allow_telescope_percent = Config.Bind<float>("Male Options", "Limiter: Telescope Threshold", 0.6f, "Allow the penis to begin telescoping after it has penetrated a certain amount. 0 = never telescope, 0.5 = allow telescoping after the halfway point, 1 = always allow telescoping.");
            _force_telescope = Config.Bind<bool>("Male Options", "Limiter: Telescope Always", true, "Force the penis to always telescope at the threshold point, instead of only doing it when it prevents clipping.");

            _dan_girth.SettingChanged += delegate
            {
                if (inHScene && bDansFound)
                {
                    danPoints.danStart.localScale = new Vector3(_dan_girth.Value, _dan_girth.Value, 1);
                }
            };

            _dan_sack_size.SettingChanged += delegate
            {
                if (inHScene && bDansFound)
                {
                    danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);
                }
            };

            _dan_collider_radius.SettingChanged += delegate
            {
                if (inHScene && danCollider != null)
                {
                    danCollider.m_Radius = _dan_collider_radius.Value;
                }
            };

            _dan_collider_headlength.SettingChanged += delegate
            {
                if (inHScene && danCollider != null)
                {
                    danCollider.m_Height = _dan_length.Value + (_dan_collider_headlength.Value * 2);
                }
            };


            _clipping_depth = Config.Bind<float>("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            for (int femaleNum = 0; femaleNum < frontOffsets.Length; femaleNum++)
                _front_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Front Collision " + femaleNum, frontOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            for (int femaleNum = 0; femaleNum < backOffsets.Length; femaleNum++)
                _back_collision_point_offset.Add(Config.Bind<float>("Female Options", "Clipping Offset: Back Collision " + femaleNum, backOffsets[femaleNum], "Individual offset on colision point, to improve clipping"));
            _kokanForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Vertical", 0.0f, "Vertical offset of the vagina target");
            _kokanUpOffset = Config.Bind<float>("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target");
            _headForwardOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target");
            _headUpOffset = Config.Bind<float>("Female Options", "Target Offset: Mouth Vertical", -0.05f, "Vertical offset of the mouth target");
            harmony = new Harmony("AI_BetterPenetration");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            hScene = __instance;
            male_list = __instance.GetMales().Where(male => male != null).ToArray();
            fem_list = __instance.GetFemales().Where(female => female != null).ToArray();

            danCollider = new DynamicBoneCollider();
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

                    bDanPenetration = false;
                    if (dan101 != null && dan109 != null && danTop != null)
                    {
                        danPoints = new DanPoints(dan101, dan109, danTop);

                        bDansFound = true;
                        dan101.localScale = new Vector3(_dan_girth.Value, _dan_girth.Value, 1);

                        danCollider = dan101.GetComponent<DynamicBoneCollider>();

                        if (danCollider == null)
                            danCollider = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                            
                        danCollider.m_Direction = DynamicBoneColliderBase.Direction.Z;
                        danCollider.m_Center = new Vector3(0, _dan_collider_verticalcenter.Value, _dan_length.Value / 2);
                        danCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
                        danCollider.m_Radius = _dan_collider_radius.Value;
                        danCollider.m_Height = _dan_length.Value + (_dan_collider_headlength.Value * 2);
						
                        danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);
	                    lastDan109Position = danPoints.danEnd.position;
	                    lastDanRotation = danPoints.danEnd.rotation;
	                    lastDanVector = danPoints.danEnd.position - danPoints.danStart.position;
	                    lastDanLength = _dan_length.Value;
	                    lastAdjustTime = Time.time;
                    }

                    referenceLookAtTarget = danPoints.danEnd;
                	lastDan101TargetDistance = Vector3.Distance(referenceLookAtTarget.position, danPoints.danStart.position);
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

                    for (int index = 0; index < frontHPointsList.Length; index++)
                        frontHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(frontHPointsList[index])).FirstOrDefault());

                    for (int index = 0; index < backHPointsList.Length; index++)
                        backHPoints.Add(female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(backHPointsList[index])).FirstOrDefault());

                    hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(headHPoint)).FirstOrDefault();

                    bpKokanTarget = female.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(bp_kokan_target)).FirstOrDefault();
                    if (bpKokanTarget != null)
                    {
                        Console.WriteLine("BP Target Found " + bpKokanTarget.name);
                        frontHPoints[0] = bpKokanTarget;
                    }

                    if (frontHPoints.Count == frontHPointsList.Length && backHPoints.Count == backHPointsList.Length && hPointBackOfHead != null)
                    {
                        bHPointsFound = true;
                        constrainPoints = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                    }

	                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
	                    db.m_Colliders.Clear();

	                Console.WriteLine("bHPointsFound " + bHPointsFound);
	       /*         for (int i = 0; i < colliderList.Length; i++)
	                {
	                    DynamicBone db = female.GetComponentsInChildren<DynamicBone>().Where(x => x.m_Root.name.Equals(dynamicBonesList[i])).FirstOrDefault();
	                    DynamicBoneCollider dbc = female.GetComponentsInChildren<DynamicBoneCollider>().Where(x => x.name.Equals(colliderList[i])).FirstOrDefault();

	                    if (dbc == null)
	                    {
	                        Transform colliderTransform = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(colliderList[i])).FirstOrDefault();

	                        if (colliderTransform != null)
	                        {
	                            dbc = colliderTransform.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
	                            dbc.m_Bound = DynamicBoneColliderBase.Bound.Inside;
	                            dbc.m_Direction = DynamicBoneColliderBase.Direction.Y;
	                            dbc.m_Height = colliderHeightList[i];
	                            dbc.m_Radius = colliderRadiusList[i];
	                        }
	                    }

	                    if (db != null && dbc != null)
	                    {             
	                        db.m_Colliders.Add(dbc);

	                        Console.WriteLine(dbc.name + " collider radius " + dbc.m_Radius + ", height: " + dbc.m_Height);

	                        foreach (DynamicBoneColliderBase dbcb in db.m_Colliders)
	                            Console.WriteLine(db.m_Root.name + " collider " + dbcb.name);
	                    }
	                    else
	                    {
	                        if (db == null)
	                            Console.WriteLine(dynamicBonesList[i] + " bone not found for " + female.name);
	                        if (dbc == null)
	                            Console.WriteLine(colliderList[i] + " collider not found for " + female.name);
	                    }
	                }
           */
                    List<DynamicBone> dbList = new List<DynamicBone>();
                    foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                    {
                        if (db != null)
                        {
                            dbList.Add(db);
	                    }
					}
                    kokanBones = dbList;
                }
            }
            inHScene = true;
            Console.WriteLine("AddColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_ChangeAnimation()
        {
            if (!inHScene)
                return;

            changingAnimations = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void H_Lookat_dan_ChangeTarget(H_Lookat_dan __instance)
        {
            if (!inHScene || __instance == null || !bDansFound || !bHPointsFound)
                return;


            SetupNewDanTarget(__instance);
            SetDanTarget();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void H_Lookat_dan_LateUpdate(H_Lookat_dan __instance)
        {
            if (!inHScene || !bDansFound || !bHPointsFound)
                return;

            danPoints.danStart.localScale = new Vector3(_dan_girth.Value, _dan_girth.Value, 1);
            danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);

            if (changingAnimations && !hScene.NowChangeAnim)
                SetupNewDanTarget(__instance);
				
        	SetDanTarget();
       }

        private static void SetupNewDanTarget(H_Lookat_dan lookAtDan)
        {
            referenceLookAtTarget = danPoints.danEnd;
            lastDan109Position = danPoints.danEnd.position;
            lastDanRotation = danPoints.danEnd.rotation;
            lastDanVector = danPoints.danEnd.position - danPoints.danStart.position;
            lastDanLength = _dan_length.Value;
            lastAdjustTime = Time.time;
            bDanPenetration = false;
            changingAnimations = false;
            if (lookAtDan != null && lookAtDan.transLookAtNull != null && lookAtDan.strPlayMotion != null && lookAtDan.transLookAtNull.name != chest_target &&
                lookAtDan.strPlayMotion.Contains("Idle") == false && lookAtDan.strPlayMotion.Contains("OUT") == false && lookAtDan.strPlayMotion.Contains("Drop") == false)
            {
                bDanPenetration = true;
                referenceLookAtTarget = lookAtDan.transLookAtNull;
                if (referenceLookAtTarget.name == kokan_target)
                {
                    if (bpKokanTarget != null)
                    {
                        Console.WriteLine("SetupNewDanTarget using BP target");
                        referenceLookAtTarget = bpKokanTarget;
                    }
                }
            }

            if (danCollider != null)
            {
                if (referenceLookAtTarget.name != ana_target && referenceLookAtTarget.name != head_target && referenceLookAtTarget.name != chest_target)
                {
                    foreach (DynamicBone db in kokanBones)
                    {
                        if (db.m_Colliders.Contains(danCollider))
                        {
                            Console.WriteLine("Instance of " + danCollider.name + " already exists in list for DB " + db.m_Root.name);
                        }
                        else
                        {
                            db.m_Colliders.Add(danCollider);
                            Console.WriteLine(danCollider.name + " added for bone " + db.m_Root.name);
                        }
                    }
                }
                else
                {
                    foreach (DynamicBone db in kokanBones)
                    {
                        if (db.m_Colliders.Contains(danCollider))
                        {
                            Console.WriteLine("Removing instance of " + danCollider.name + " in list for DB " + db.m_Root.name);
                            db.m_Colliders.Remove(danCollider);
                        }
                    }
                }
            }
			
            lastDan101TargetDistance = Vector3.Distance(referenceLookAtTarget.position, danPoints.danStart.position);
        }

		private static void SetDanTarget()
        {
            Vector3 dan101_pos = danPoints.danStart.position;
            Vector3 lookTarget = referenceLookAtTarget.position;

            if (referenceLookAtTarget.name == kokan_target || referenceLookAtTarget.name == bp_kokan_target)
                lookTarget = lookTarget + (referenceLookAtTarget.forward * _kokanForwardOffset.Value) + (referenceLookAtTarget.up * _kokanUpOffset.Value);
            if (referenceLookAtTarget.name == head_target)
                lookTarget = lookTarget + (referenceLookAtTarget.forward * _headForwardOffset.Value) + (referenceLookAtTarget.up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            Vector3 danVector = Vector3.Normalize(lookTarget - dan101_pos);
            Vector3 dan109_pos = dan101_pos + danVector * _dan_length.Value;

            float adjustTime = Time.time;
            float timeSinceLastAdjust = adjustTime - lastAdjustTime;
            lastAdjustTime = adjustTime;

            if (timeSinceLastAdjust < 0.0001)
            {
                danPoints.danStart.rotation = lastDanRotation;
                danPoints.danEnd.SetPositionAndRotation(lastDan109Position, lastDanRotation);
                return;
            }

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

                    float danLength = _dan_length.Value;
                    Plane kokanPlane = new Plane(danPoints.danStart.forward, lookTarget);

                    if (_dan_length.Value > distDan101ToTarget)
                        danLength = _dan_length.Value - (_dan_length.Value - distDan101ToTarget) * _dan_softness.Value;

                    if (kokanPlane.GetSide(dan101_pos))
                        danLength = _dan_length.Value * (1 - _dan_softness.Value);

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

                    if (danCollider != null)
                    {
                        danCollider.m_Center = new Vector3(0, _dan_collider_verticalcenter.Value, danLength / 2);
                        danCollider.m_Height = danLength + (_dan_collider_headlength.Value * 2);
                    }
                }
                else if (referenceLookAtTarget.name == head_target)
                {
                    float danLength;
                    float max_dist;

                    if (Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position))
                    {
                        danLength = _dan_length.Value * (1 - _dan_softness.Value);
                        max_dist = Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position);
                    }
                    else
                    {
                        if (_dan_length.Value > distDan101ToTarget)
                            danLength = _dan_length.Value - (_dan_length.Value - distDan101ToTarget) * _dan_softness.Value;
                        else
                            danLength = _dan_length.Value;
                        max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position);
                    }

                    if (danLength > max_dist)
                        danLength = max_dist;

                    dan109_pos = dan101_pos + danVector * danLength;
                }
            }

            Vector3 danForwardVector = Vector3.Normalize(dan109_pos - dan101_pos);
            Quaternion danQuaternion = Quaternion.LookRotation(danForwardVector, Vector3.Cross(danForwardVector, danPoints.danTop.right));

            danPoints.danStart.rotation = danQuaternion;
            danPoints.danEnd.SetPositionAndRotation(dan109_pos, danQuaternion);

            lastDanVector = danForwardVector;
            lastDanLength = Vector3.Distance(dan101_pos, dan109_pos);
            lastDanRotation = danQuaternion;
            lastDan109Position = dan109_pos;
            lastDan101TargetDistance = distDan101ToTarget;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            Console.WriteLine("HScene::EndProc");

            inHScene = false;
            bDansFound = false;
            bDanPenetration = false;
            bHPointsFound = false;

            if (!inHScene)
            {
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
                Console.WriteLine("Clearing females list");
                Array.Clear(fem_list, 0, fem_list.Length);
                Console.WriteLine("Clearing males list");
                Array.Clear(male_list, 0, male_list.Length);
            }
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
                harmony.PatchAll(typeof(AI_BetterPenetration));
            else
                harmony.UnpatchAll(nameof(AI_BetterPenetration));
        }

    }
}