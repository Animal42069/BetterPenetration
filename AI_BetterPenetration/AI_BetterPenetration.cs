using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace AI_BetterPenetration
{

    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "2.0.0.0";

        private static ConfigEntry<float> _dan_length;
        private static ConfigEntry<float> _dan_girth;
        private static ConfigEntry<float> _dan_sack_size;
        private static ConfigEntry<float> _dan_softness;
        private static ConfigEntry<float> _dan_collider_headlength;
        private static ConfigEntry<float> _dan_collider_radius;

        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanForwardOffset;
        private static ConfigEntry<float> _kokanUpOffset;
        private static ConfigEntry<float> _headForwardOffset;
        private static ConfigEntry<float> _headUpOffset;
        private static List<ConfigEntry<float>> _front_collision_point_offset = new List<ConfigEntry<float>>();
        private static List<ConfigEntry<float>> _back_collision_point_offset = new List<ConfigEntry<float>>();

        private static bool inHScene = false;

        public static AIChara.ChaControl[] fem_list;
        public static AIChara.ChaControl[] male_list;
        public static List<DynamicBone> kokanBones = new List<DynamicBone>();
        public static DynamicBoneCollider danCollider = new DynamicBoneCollider();

        private static bool bDansFound = false;
        private static DanPoints danPoints;
        private static bool bDanPenetration = false;
        private static Transform referenceLookAtTarget;

        private static bool bHPointsFound = false;
        private static ConstrainPoints constrainPoints;
        private static H_Lookat_dan lookat_Dan;

        private static readonly float[] frontOffsets = { -0.08f, -0.15f, -0.08f, -0.65f };
        private static readonly float[] backOffsets = { 0.1f, 0.05f, 0.0f, 0.0f };
        private static readonly string[] frontHPointsList = { "cf_J_sk_00_02", "k_f_kosi03_03", "N_Waist_f", "k_f_spine03_03" };
        private static readonly string[] backHPointsList = { "cf_J_sk_04_02", "cf_J_sk_04_01", "N_Waist_b", "N_Back" };
        private static readonly bool[] frontHPointsInward = { false, false, false, false };
        private static readonly bool[] backHPointsInward = { false, false, true, true };

        private void Awake()
        {

            _dan_length = Config.Bind<float>("Male Options", "Length of Penis", 1.8f, "Set the length of the penis.  Apparent Length is about 0.2 larget than this, depending on uncensor.  2.0 is about 8 inches or 20 cm.");
            _dan_collider_headlength = Config.Bind<float>("Male Options", "Length of the Collider Head", 0.2f, "Distance from the center of the head bone to the tip, used for collision purposes.");
            _dan_collider_radius = Config.Bind<float>("Male Options", "Radius of the Collider", 0.2f, "Rasius of the shaft collider.");
            _dan_girth = Config.Bind<float>("Male Options", "Girth of Penis", 1.0f, "Set the scale of the circumference of the penis.");
            _dan_sack_size = Config.Bind<float>("Male Options", "Scale of the sack", 1.0f, "Set the scale (size) of the sack");
            _dan_softness = Config.Bind<float>("Male Options", "Softness of the penis", 0.1f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to begin to telescope after penetration.  A small value can make it appear there is friction during penetration.");

            _clipping_depth = Config.Bind<float>("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");
            _kokanForwardOffset = Config.Bind<float>("Female Options", "Vagina Target Forward Offset", -0.035f, "Forward offset of the vagina target");
            _kokanUpOffset = Config.Bind<float>("Female Options", "Vagina Target Vertical Offset", 0.0f, "Vertical offset of the vagina target");
            _headForwardOffset = Config.Bind<float>("Female Options", "Mouth Target Forward Offset", 0.0f, "Forward offset of the vagina target");
            _headUpOffset = Config.Bind<float>("Female Options", "Mouth Target Vertical Offset", 0.025f, "Vertical offset of the mouth target");
            for (int index = 0; index < frontOffsets.Length; index++)
                _front_collision_point_offset.Add(Config.Bind<float>("Female Options", "Front Collision Offset " + index, frontOffsets[index], "Individual offset on colision point, to improve clipping"));
            for (int index = 0; index < backOffsets.Length; index++)
                _back_collision_point_offset.Add(Config.Bind<float>("Female Options", "Back Collision Offset " + index, backOffsets[index], "Individual offset on colision point, to improve clipping"));

            _dan_length.SettingChanged += delegate
            {
                if (inHScene)
                {
                    danCollider.m_Center = new Vector3(0, 0, _dan_length.Value / 2);
                    danCollider.m_Height = _dan_length.Value + (_dan_collider_headlength.Value * 2);
                }
            };

            _dan_girth.SettingChanged += delegate
            {
                if (inHScene && bDansFound)
                {
                    danPoints.danStart.localScale = new Vector3(_dan_girth.Value, _dan_girth.Value, 1);
                }
            };

            _dan_sack_size.SettingChanged += delegate
            {
                if (inHScene && danPoints.danTop != null)
                {
                    danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);
                }
            };

            _dan_collider_radius.SettingChanged += delegate
            {
                if (inHScene)
                {
                    danCollider.m_Radius = _dan_collider_radius.Value;
                }
            };

            _dan_collider_headlength.SettingChanged += delegate
            {
                if (inHScene)
                {
                    danCollider.m_Height = _dan_length.Value + (_dan_collider_headlength.Value * 2);
                }
            };

            var harmony = new Harmony("AI_BetterPenetration");
            HarmonyWrapper.PatchAll(typeof(AI_BetterPenetration), harmony);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_ChangeAnimation(HScene __instance, HScene.AnimationListInfo _info)
        {
            if (!inHScene)
                return;

            bDanPenetration = false;
            referenceLookAtTarget = null;

            if (lookat_Dan != null)
            {
                lookat_Dan.transLookAtNull = null;
                lookat_Dan.dan_Info.SetTargetTransform(null);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void HScene_ChangeMotion(H_Lookat_dan __instance)
        {
            if (!inHScene || __instance == null || !bDansFound)
                return;

            if (lookat_Dan == null)
                lookat_Dan = __instance;

            referenceLookAtTarget = danPoints.danEnd;
            bDanPenetration = false;
            if (__instance.transLookAtNull != null && __instance.transLookAtNull.name != "k_f_spine03_00" && __instance.strPlayMotion.Contains("Idle") == false && __instance.strPlayMotion.Contains("OUT") == false)
            {
                bDanPenetration = true;
                referenceLookAtTarget = __instance.transLookAtNull;
            }

            SetDanTarget(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void OffsetPenisTarget(H_Lookat_dan __instance)
        {
            if (!inHScene || !bDansFound || !bHPointsFound)
                return;

            danPoints.danStart.localScale = new Vector3(_dan_girth.Value, _dan_girth.Value, 1);
            danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);

            SetDanTarget(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            inHScene = true;
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
                    Transform dan101;
                    Transform dan109;
                    Transform danTop;

                    dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan101_00")).FirstOrDefault();
                    dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan109_00")).FirstOrDefault();
                    danTop = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan_f_top")).FirstOrDefault();

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
                        danCollider.m_Center = new Vector3(0, 0, _dan_length.Value / 2);
                        danCollider.m_Bound = DynamicBoneColliderBase.Bound.Outside;
                        danCollider.m_Radius = _dan_collider_radius.Value;
                        danCollider.m_Height = _dan_length.Value + (_dan_collider_headlength.Value * 2);
                        danPoints.danTop.localScale = new Vector3(_dan_sack_size.Value, _dan_sack_size.Value, _dan_sack_size.Value);
                    }

                    referenceLookAtTarget = dan101;
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

                    hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_head_03")).FirstOrDefault();

                    if (frontHPoints.Count == frontHPointsList.Length && backHPoints.Count == backHPointsList.Length && hPointBackOfHead != null)
                    {
                        bHPointsFound = true;
                        constrainPoints = new ConstrainPoints(frontHPoints, backHPoints, hPointBackOfHead);
                    }

                    Console.WriteLine("bHPointsFound " + bHPointsFound);

                    List<DynamicBone> dbList = new List<DynamicBone>();

                    foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                    {
                        if (db != null)
                        {
                            Console.WriteLine(db.m_Root.name + " found, adding collilders");

                            dbList.Add(db);


                            if (db.m_Colliders.Contains(danCollider))
                            {
                                Console.WriteLine("Instance of " + danCollider.name + " already exists in list for DB " + db.name);
                            }
                            else
                            {
                                db.m_Colliders.Add(danCollider);
                                Console.WriteLine(danCollider.name + " added to " + female.name + " for bone " + db.name);
                            }
                        }
                    }

                    kokanBones = dbList;
                }
            }
            Console.WriteLine("AddColliders done.");
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

        private static void SetDanTarget(H_Lookat_dan __instance)
        {
            if (!bDansFound || !bHPointsFound || referenceLookAtTarget == null)
                return;

            if (referenceLookAtTarget == null)
                referenceLookAtTarget = danPoints.danEnd;

            Vector3 dan101_pos = danPoints.danStart.position;
            Vector3 lookTarget = referenceLookAtTarget.position;

            if (referenceLookAtTarget.name == "k_f_kokan_00")
                lookTarget = lookTarget + (referenceLookAtTarget.forward * _kokanForwardOffset.Value) + (referenceLookAtTarget.up * _kokanUpOffset.Value);
            if (referenceLookAtTarget.name == "k_f_head_00")
                lookTarget = lookTarget + (referenceLookAtTarget.forward * _headForwardOffset.Value) + (referenceLookAtTarget.up * _headUpOffset.Value);

            float distDan101ToTarget = Vector3.Distance(dan101_pos, lookTarget);
            if (distDan101ToTarget == 0)
                return;

            float tDan101ToTarget = _dan_length.Value / distDan101ToTarget;
            Vector3 dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);

            if (bDanPenetration)
            {
                if (referenceLookAtTarget.name == "k_f_kokan_00" || referenceLookAtTarget.name == "k_f_ana_00")
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

                    tDan101ToTarget = danLength / distDan101ToTarget;
                    dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);

                    Vector3 adjustedDanPos = dan109_pos;

                    bool bConstrainPastNearSide = false;
                    for (int index = 1; index < constrainPoints.frontConstrainPoints.Count; index++)
                    {
                        Plane hPlane = new Plane(Vector3.Normalize(Vector3.Cross(constrainPoints.frontConstrainPoints[index - 1].right, frontHitPoints[index] - frontHitPoints[index - 1])), Vector3.Lerp(frontHitPoints[index - 1], frontHitPoints[index], 0.5f));
                        if (frontHPointsInward[index - 1])
                            hPlane.Flip();

                        if (index == 1)
                            bConstrainPastNearSide = hPlane.GetSide(adjustedDanPos);

                        Vector3 constrainedDanPos = Geometry.ConstrainLineToHitPlane(dan101_pos, adjustedDanPos, danLength, frontHitPoints[index - 1], frontHitPoints[index], hPlane, ref bConstrainPastNearSide, out float constainedAngle);

                        if (constainedAngle > 0)
                            adjustedDanPos = constrainedDanPos;
                    }

                    for (int index = 1; index < constrainPoints.backConstrainPoints.Count; index++)
                    {
                        Plane hPlane = new Plane(Vector3.Normalize(Vector3.Cross(-constrainPoints.backConstrainPoints[index - 1].right, backHitPoints[index] - backHitPoints[index - 1])), Vector3.Lerp(backHitPoints[index - 1], backHitPoints[index], 0.5f));
                        if (backHPointsInward[index - 1])
                            hPlane.Flip();

                        if (index == 1)
                            bConstrainPastNearSide = hPlane.GetSide(adjustedDanPos);

                        Vector3 constrainedDanPos = Geometry.ConstrainLineToHitPlane(dan101_pos, adjustedDanPos, danLength, backHitPoints[index - 1], backHitPoints[index], hPlane, ref bConstrainPastNearSide, out float constainedAngle);

                        if (constainedAngle > 0)
                            adjustedDanPos = constrainedDanPos;
                    }

                    dan109_pos = adjustedDanPos;
                }
                else if (referenceLookAtTarget.name == "k_f_head_00")
                {
                    float danLength = _dan_length.Value;

                    if (Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position) < Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position))
                    {
                        danLength = _dan_length.Value * (1 - _dan_softness.Value);
                        tDan101ToTarget = danLength / distDan101ToTarget;

                        float max_dist = Vector3.Distance(dan101_pos, constrainPoints.headConstrainPoint.position);

                        if (danLength > max_dist)
                            tDan101ToTarget = max_dist / distDan101ToTarget;

                        dan109_pos = Vector3.LerpUnclamped(lookTarget, dan101_pos, tDan101ToTarget);
                    }
                    else
                    {
                        if (_dan_length.Value > distDan101ToTarget)
                            danLength = _dan_length.Value - (_dan_length.Value - distDan101ToTarget) * _dan_softness.Value;
                        tDan101ToTarget = danLength / distDan101ToTarget;

                        float max_dist = distDan101ToTarget + Vector3.Distance(lookTarget, constrainPoints.headConstrainPoint.position);

                        if (danLength > max_dist)
                            tDan101ToTarget = max_dist / distDan101ToTarget;

                        dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tDan101ToTarget);
                    }
                }
            }

            Vector3 danForwardVector = dan109_pos - danPoints.danStart.position;
            Vector3 danRightVector = danPoints.danTop.right;
            Vector3 danUpVector = Vector3.Normalize(Vector3.Cross(danForwardVector, danRightVector));

            danPoints.danStart.rotation = Quaternion.LookRotation(danForwardVector, danUpVector);
            danPoints.danEnd.SetPositionAndRotation(dan109_pos, Quaternion.LookRotation(danForwardVector, danUpVector));
        }
    }
}