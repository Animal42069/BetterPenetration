using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Manager;
using System.Reflection;
using AIChara;
using Core_BetterPenetration;
using System.Linq;

namespace AI_BetterPenetration
{
    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.1")]
    [BepInProcess("AI-Syoujyo")]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "3.0.0.0";
        private const int MaleLimit = 1;
        private const int FemaleLimit = 2;
        private const bool _useSelfColliders = false;

        private static readonly List<float> frontOffsets = new List<float> { -0.35f, 0.25f, 0f, -0.65f };
        private static readonly List<float> backOffsets = new List<float> { -0.05f, 0.25f, 0.05f, 0.05f };
        private static readonly List<bool> frontPointsInward = new List<bool> { false, false, false, false };
        private static readonly List<bool> backPointsInward = new List<bool> { false, false, true, true };

        private static ConfigEntry<float> _danColliderHeadLength;
        private static ConfigEntry<float> _danColliderRadius;
        private static ConfigEntry<float> _danColliderVerticalCenter;
        private static ConfigEntry<float> _fingerColliderLength;
        private static ConfigEntry<float> _fingerColliderRadius;
        private static ConfigEntry<float> _danLengthSquishFactor;
        private static ConfigEntry<float> _danGirthSquishFactor;
        private static ConfigEntry<float> _danSquishThreshold;
        private static ConfigEntry<bool> _useFingerColliders;
        private static ConfigEntry<bool> _simplifyPenetration;

        private static ConfigEntry<float> _clippingDepth;
        private static ConfigEntry<float> _kokanOffsetForward;
        private static ConfigEntry<float> _kokanOffsetUp;
        private static ConfigEntry<float> _headOffsetForward;
        private static ConfigEntry<float> _headOffsetUp;
        private static ConfigEntry<bool> _useKokanFix;
        private static ConfigEntry<float> _kokanFixPositionY;
        private static ConfigEntry<float> _kokanFixPositionZ;
        private static ConfigEntry<float> _kokanFixRotationX;
        private static readonly ConfigEntry<float>[] _frontCollisionOffset = new ConfigEntry<float>[frontOffsets.Count];
        private static readonly ConfigEntry<float>[] _backCollisionOffset = new ConfigEntry<float>[backOffsets.Count];

        private static Harmony harmony;
        private static HScene hScene;
        private static bool patched = false;
        private static bool inHScene = false;
        private static bool loadingCharacter = false;

        private void Awake()
        {
            (_fingerColliderLength = Config.Bind("Male Options", "Finger Collider: Length", 0.6f, "Lenght of the finger colliders.")).SettingChanged += (s, e) =>
            { UpdateFingerColliders(); };
            (_fingerColliderRadius = Config.Bind("Male Options", "Finger Collider: Radius", 0.2f, "Radius of the finger colliders.")).SettingChanged += (s, e) =>
            { UpdateFingerColliders(); };
            (_danColliderHeadLength = Config.Bind("Male Options", "Penis Collider: Length of Head", 0.35f, "Distance from the center of the head bone to the tip, used for collision purposes.")).SettingChanged += (s, e) =>
            { UpdateDanColliders(); };
            (_danColliderRadius = Config.Bind("Male Options", "Penis Collider: Radius of Shaft", 0.32f, "Radius of the shaft collider.")).SettingChanged += (s, e) =>
            { UpdateDanColliders(); };
            (_danColliderVerticalCenter = Config.Bind("Male Options", "Penis Collider: Vertical Center", -0.03f, "Vertical Center of the shaft collider")).SettingChanged += (s, e) =>
            { UpdateDanColliders(); };
            (_danLengthSquishFactor = Config.Bind("Male Options", "Penis: Squish Length Factor", 0.6f, new ConfigDescription("How much the length of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_danGirthSquishFactor = Config.Bind("Male Options", "Penis: Squish Girth Factor", 0.25f, new ConfigDescription("How much the girth of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_danSquishThreshold = Config.Bind("Male Options", "Penis: Squish Threshold", 0.2f, new ConfigDescription("Allows the penis to begin squishing (shorten length increase girth) after this amount of the penis has penetrated.", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_useFingerColliders = Config.Bind("Male Options", "Finger Collider: Enable", true, "Use finger colliders")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_simplifyPenetration = Config.Bind("Male Options", "Simplify Penetration Calculation", false, "Simplifys penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };

            (_clippingDepth = Config.Bind("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            for (int offset = 0; offset < frontOffsets.Count; offset++)
                (_frontCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Front Collision " + offset, frontOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            for (int offset = 0; offset < backOffsets.Count; offset++)
                (_backCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Back Collision " + offset, backOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            (_kokanOffsetForward = Config.Bind("Female Options", "Target Offset: Vagina Vertical", -0.15f, "Vertical offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanOffsetUp = Config.Bind("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_headOffsetForward = Config.Bind("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_headOffsetUp = Config.Bind("Female Options", "Target Offset: Mouth Vertical", 0.03f, "Vertical offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_useKokanFix = Config.Bind("Female Options", "Joint Adjustment: Missionary Correction", false, "NOTE: There is an Illusion bug that causes the vagina to appear sunken in certain missionary positions.  It is best to use Advanced Bonemod and adjust your female character's cf_J_Kokan Offset Y to 0.001.  If you don't do that, enabling this option will attempt to fix the problem by guessing where the bone should be")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixPositionY = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Y", -0.075f, "Amount to adjust the Vagina bone position Y for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixPositionZ = Config.Bind("Female Options", "Joint Adjustment: Missionary Position Z", 0.0625f, "Amount to adjust the Vagina bone position Z for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixRotationX = Config.Bind("Female Options", "Joint Adjustment: Missionary Rotation X", 10.0f, "Amount to adjust the Vagina bone rotation X for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
			
			harmony = new Harmony("AI_BetterPenetration");
        }

        private static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            Core.UpdateDanCollider(0, _danColliderRadius.Value, _danColliderHeadLength.Value, _danColliderVerticalCenter.Value);
        }

        private static void UpdateFingerColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                Core.UpdateFingerColliders(0, _fingerColliderRadius.Value, _fingerColliderLength.Value);
        }

        private static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            Core.UpdateDanOptions(0, _danLengthSquishFactor.Value, _danGirthSquishFactor.Value, _danSquishThreshold.Value, _useFingerColliders.Value, _simplifyPenetration.Value);
        }

        private static void UpdateCollisionOptions()
        {
            if (!inHScene)
                return;

            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();
            for (int index = 0; index < MaleLimit; index++)
                Core.UpdateCollisionOptions(index, collisionOptions[index]);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        public static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            Core.RemovePCollidersFromCoordinate(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void HScene_PostSetStartVoice(HScene __instance)
        {
            hScene = __instance;

            List<DanOptions> danOptions = PopulateDanOptionsList();
            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();

            ChaControl[] femaleArray = hScene.GetFemales();
            List<ChaControl> femaleList = new List<ChaControl>();
            foreach (var character in femaleArray)
            {
                if (character == null)
                    continue;
                femaleList.Add(character);
            }

            ChaControl[] maleArray = hScene.GetMales();
            List<ChaControl> maleList = new List<ChaControl>();
            foreach (var character in maleArray)
            {
                if (character == null)
                    continue;
                maleList.Add(character);
            }

            Core.InitializeAgents(maleList, femaleList, danOptions, collisionOptions);
            inHScene = true;
        }

        private static List<DanOptions> PopulateDanOptionsList()
        {
            List<DanOptions> danOptions = new List<DanOptions>
            {
                new DanOptions(_danColliderVerticalCenter.Value, _danColliderRadius.Value, _danColliderHeadLength.Value,
                 _danLengthSquishFactor.Value, _danGirthSquishFactor.Value, _danSquishThreshold.Value,
                _fingerColliderRadius.Value, _fingerColliderLength.Value, _useFingerColliders.Value, _simplifyPenetration.Value)
            };

            return danOptions;
        }

        private static List<CollisionOptions> PopulateCollisionOptionsList()
        {
            List<CollisionOptions> collisionOptions = new List<CollisionOptions>();

            List<CollidonPointInfo> frontInfo = new List<CollidonPointInfo>();
            for (int info = 0; info < BoneNames.frontCollisionList.Count; info++)
                frontInfo.Add(new CollidonPointInfo(BoneNames.frontCollisionList[info], _frontCollisionOffset[info].Value, frontPointsInward[info]));

            List<CollidonPointInfo> backInfo = new List<CollidonPointInfo>();
            for (int info = 0; info < BoneNames.backCollisionList.Count; info++)
                backInfo.Add(new CollidonPointInfo(BoneNames.backCollisionList[info], _backCollisionOffset[info].Value, backPointsInward[info]));

            for (int femaleNum = 0; femaleNum < FemaleLimit; femaleNum++)
            {
                collisionOptions.Add(new CollisionOptions(_useSelfColliders, _kokanOffsetForward.Value, _kokanOffsetUp.Value, _headOffsetForward.Value, _headOffsetUp.Value, _useKokanFix.Value,
                    _kokanFixPositionZ.Value, _kokanFixPositionY.Value, _kokanFixRotationX.Value, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_PreChangeAnimation(HScene.AnimationListInfo _info)
        {
            if (!inHScene || _info == null || _info.fileFemale == null)
                return;

            Core.OnChangeAnimation(_info.fileFemale);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null)
                return;

            Core.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, 0, 0, false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || hScene == null || __instance.strPlayMotion == null)
                return;

            Core.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, hScene.NowChangeAnim, 0, 0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            HScene_sceneUnloaded();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProcADV")]
        public static void HScene_EndProcADV_Patch()
        {
            HScene_sceneUnloaded();
        }

        private void Update()
        {
            var isHScene = HSceneManager.isHScene;
            
            if (isHScene && !patched)
                HScene_sceneLoaded();
            else if (!isHScene && patched)
                HScene_sceneUnloaded();
        }

        private static void HScene_sceneLoaded()
        {
            if (patched)
                return;

            harmony.PatchAll(typeof(AI_BetterPenetration));
            patched = true;
        }

        private static void HScene_sceneUnloaded()
        {
            if (!patched)
                return;

            Core.OnEndScene();

            harmony.UnpatchAll(nameof(AI_BetterPenetration));
            patched = false;
            inHScene = false;
            loadingCharacter = false;

            if (hScene == null)
                return;

            foreach (var lookat in hScene.ctrlLookAts)
            {
                if (lookat == null)
                    continue;

                lookat.transLookAtNull = null;
            }

            hScene = null;
        }
    }
}