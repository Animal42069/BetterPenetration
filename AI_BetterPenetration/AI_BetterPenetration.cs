using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using Manager;
using AIChara;
using UnityEngine;
using Core_BetterPenetration;

namespace AI_BetterPenetration
{
    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.2")]
    [BepInProcess("AI-Syoujyo")]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        internal const string VERSION = "4.2.1.0";
        internal const int MaleLimit = 1;
        internal const int FemaleLimit = 2;

        internal static readonly List<float> frontOffsets = new List<float> { -0.35f, 0f };
        internal static readonly List<float> backOffsets = new List<float> { -0.05f, 0.05f };
        internal static readonly List<bool> frontPointsInward = new List<bool> { false, false };
        internal static readonly List<bool> backPointsInward = new List<bool> { false, true };

        internal static ConfigEntry<float> _danColliderLengthScale;
        internal static ConfigEntry<float> _danColliderRadiusScale;
        internal static ConfigEntry<float> _fingerColliderLength;
        internal static ConfigEntry<float> _fingerColliderRadius;
        internal static ConfigEntry<float> _danLengthSquishFactor;
        internal static ConfigEntry<float> _danGirthSquishFactor;
        internal static ConfigEntry<float> _danSquishThreshold;
        internal static ConfigEntry<bool> _danSquishOralGirth;
        internal static ConfigEntry<bool> _useFingerColliders;
        internal static ConfigEntry<bool> _simplifyPenetration;
        internal static ConfigEntry<bool> _simplifyOral;
        internal static ConfigEntry<bool> _rotateTamaWithShaft;

        internal static ConfigEntry<float> _clippingDepth;
        internal static ConfigEntry<Vector3> _kokanOffset;
        internal static ConfigEntry<Vector3> _innerKokanOffset;
        internal static ConfigEntry<Vector3> _mouthOffset;
        internal static ConfigEntry<Vector3> _innerMouthOffset;
        internal static ConfigEntry<bool> _useKokanFix;
        internal static ConfigEntry<float> _kokanFixPositionY;
        internal static ConfigEntry<float> _kokanFixPositionZ;
        internal static ConfigEntry<float> _kokanFixRotationX;
        internal static readonly ConfigEntry<float>[] _frontCollisionOffset = new ConfigEntry<float>[frontOffsets.Count];
        internal static readonly ConfigEntry<float>[] _backCollisionOffset = new ConfigEntry<float>[backOffsets.Count];

        internal static Harmony harmony;
        internal static HScene hScene;
        internal static bool patched = false;
        internal static bool inHScene = false;
        internal static bool loadingCharacter = false;
        internal static bool resetParticles = false;

        internal void Awake()
        {
            (_fingerColliderLength = Config.Bind("Male Options", "Finger Collider: Length", 0.18f, "Lenght of the finger colliders.")).SettingChanged += (s, e) =>
            { UpdateFingerColliders(); };
            (_fingerColliderRadius = Config.Bind("Male Options", "Finger Collider: Radius", 0.06f, "Radius of the finger colliders.")).SettingChanged += (s, e) =>
            { UpdateFingerColliders(); };
            (_danColliderLengthScale = Config.Bind("Male Options", "Penis Collider: Length Scale", 1.0f, new ConfigDescription("How much to scale collider length", new AcceptableValueRange<float>(0.5f, 1.5f)))).SettingChanged += (s, e) =>
            { UpdateDanColliders(); };
            (_danColliderRadiusScale = Config.Bind("Male Options", "Penis Collider: Radius Scale", 1.0f, new ConfigDescription("How much to scale collider radius", new AcceptableValueRange<float>(0.5f, 1.5f)))).SettingChanged += (s, e) =>
            { UpdateDanColliders(); };
            (_danLengthSquishFactor = Config.Bind("Male Options", "Penis: Squish Length Factor", 0.6f, new ConfigDescription("How much the length of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_danGirthSquishFactor = Config.Bind("Male Options", "Penis: Squish Girth Factor", 0.2f, new ConfigDescription("How much the girth of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_danSquishThreshold = Config.Bind("Male Options", "Penis: Squish Threshold", 0.2f, new ConfigDescription("Allows the penis to begin squishing (shorten length increase girth) after this amount of the penis has penetrated.", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_danSquishOralGirth = Config.Bind("Male Options", "Penis: Squish Oral Girth", false, "Allows the penis to squish (increase girth) during oral.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_useFingerColliders = Config.Bind("Male Options", "Finger Collider: Enable", true, "Use finger colliders")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_simplifyPenetration = Config.Bind("Male Options", "Simplify Penetration Calculation", false, "Simplifys penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_simplifyOral = Config.Bind("Male Options", "Simplify Oral Calculation", false, "Simplifys oral penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_rotateTamaWithShaft = Config.Bind("Male Options", "Rotate Balls with Shaft", true, "If enabled, the base of the balls will be locked to the base of the shaft")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };

            (_clippingDepth = Config.Bind("Female Options", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            for (int offset = 0; offset < frontOffsets.Count; offset++)
                (_frontCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Front Collision " + offset, frontOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            for (int offset = 0; offset < backOffsets.Count; offset++)
                (_backCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Back Collision " + offset, backOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            (_kokanOffset = Config.Bind("Female Options", "Target Offset: Vagina Target", Vector3.zero, "Offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerKokanOffset = Config.Bind("Female Options", "Target Offset: Inner Vagina Target", Vector3.zero, "Offset of the simplified inner vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_mouthOffset = Config.Bind("Female Options", "Target Offset: Mouth Target", new Vector3(0, 0.025f, 0), "Offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerMouthOffset = Config.Bind("Female Options", "Target Offset: Inner Mouth Target", Vector3.zero, "Offset of the simplified inner mouth target")).SettingChanged += (s, e) =>
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


        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        internal static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            CoreGame.RemoveCollidersFromCoordinate(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        internal static void HScene_PostSetStartVoice(HScene __instance)
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

            CoreGame.InitializeAgents(maleList, femaleList, danOptions, collisionOptions);
            inHScene = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        internal static void HScene_PreChangeAnimation(HScene.AnimationListInfo _info)
        {
            if (!inHScene || _info == null || _info.fileFemale == null)
                return;

            CoreGame.OnChangeAnimation(_info.fileFemale);
            resetParticles = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "SetMovePositionPoint")]
        internal static void HScene_SetMovePositionPoint()
        {
            resetParticles = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "setPlay")]
        internal static void ChaControl_PostSetPlay()
        {
            resetParticles = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        internal static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null)
                return;

            CoreGame.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, 0, 0, false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        internal static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || hScene == null || __instance.strPlayMotion == null)
                return;

            if (resetParticles && !hScene.NowChangeAnim)
            {
                CoreGame.ResetParticles();
                resetParticles = false;
            }

            CoreGame.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, hScene.NowChangeAnim, 0, 0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        internal static void HScene_EndProc_Patch()
        {
            HScene_sceneUnloaded();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProcADV")]
        internal static void HScene_EndProcADV_Patch()
        {
            HScene_sceneUnloaded();
        }
		
        internal static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            CoreGame.UpdateDanCollider(0, _danColliderRadiusScale.Value, _danColliderLengthScale.Value);
        }

        internal static void UpdateFingerColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateFingerColliders(0, _fingerColliderRadius.Value, _fingerColliderLength.Value);
        }

        internal static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            CoreGame.UpdateDanOptions(0, _danLengthSquishFactor.Value, _danGirthSquishFactor.Value, _danSquishThreshold.Value, _danSquishOralGirth.Value, _useFingerColliders.Value, _simplifyPenetration.Value, _simplifyOral.Value, _rotateTamaWithShaft.Value);
        }

        internal static void UpdateCollisionOptions()
        {
            if (!inHScene)
                return;

            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();
            for (int index = 0; index < FemaleLimit; index++)
                CoreGame.UpdateCollisionOptions(index, collisionOptions[index]);
        }

        internal static List<DanOptions> PopulateDanOptionsList()
        {
            List<DanOptions> danOptions = new List<DanOptions>
            {
                new DanOptions(_danColliderRadiusScale.Value, _danColliderLengthScale.Value,
                 _danLengthSquishFactor.Value, _danGirthSquishFactor.Value, _danSquishThreshold.Value, _danSquishOralGirth.Value,
                _fingerColliderRadius.Value, _fingerColliderLength.Value, _useFingerColliders.Value, 
                _simplifyPenetration.Value, _simplifyOral.Value, _rotateTamaWithShaft.Value)
            };

            return danOptions;
        }

        internal static List<CollisionOptions> PopulateCollisionOptionsList()
        {
            List<CollisionOptions> collisionOptions = new List<CollisionOptions>();

            List<CollisionPointInfo> frontInfo = new List<CollisionPointInfo>();
            for (int info = 0; info < BoneNames.frontCollisionList.Count; info++)
                frontInfo.Add(new CollisionPointInfo(BoneNames.frontCollisionList[info], _frontCollisionOffset[info].Value, frontPointsInward[info]));

            List<CollisionPointInfo> backInfo = new List<CollisionPointInfo>();
            for (int info = 0; info < BoneNames.backCollisionList.Count; info++)
                backInfo.Add(new CollisionPointInfo(BoneNames.backCollisionList[info], _backCollisionOffset[info].Value, backPointsInward[info]));

            for (int femaleNum = 0; femaleNum < FemaleLimit; femaleNum++)
            {
                collisionOptions.Add(new CollisionOptions(_kokanOffset.Value, _innerKokanOffset.Value, _mouthOffset.Value, _innerMouthOffset.Value, _useKokanFix.Value,
                    _kokanFixPositionZ.Value, _kokanFixPositionY.Value, _kokanFixRotationX.Value, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }


        internal void Update()
        {
            var isHScene = HSceneManager.isHScene;
            
            if (isHScene && !patched)
                HScene_sceneLoaded();
            else if (!isHScene && patched)
                HScene_sceneUnloaded();
        }

        internal static void HScene_sceneLoaded()
        {
            if (patched)
                return;
            
            harmony.PatchAll(typeof(AI_BetterPenetration));
            patched = true;
        }

        internal static void HScene_sceneUnloaded()
        {
            if (!patched)
                return;

            CoreGame.OnEndScene();

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