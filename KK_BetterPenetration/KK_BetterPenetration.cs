using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Core_BetterPenetration;
using UnityEngine;

namespace KK_BetterPenetration
{
    [BepInPlugin("animal42069.KKbetterpenetration", "KK Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.11.1")]
    [BepInDependency("com.rclcircuit.bepinex.modboneimplantor", "1.0")]
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]

    public class KK_BetterPenetration : BaseUnityPlugin
    {
        public static KK_BetterPenetration instance;

        public const string VERSION = "4.2.0.0";
        internal const int MaleLimit = 2;
        internal const int FemaleLimit = 2;

        internal static readonly List<float> frontOffsets = new List<float> { -0.04f, 0.04f, 0.06f };
        internal static readonly List<float> backOffsets = new List<float> { -0.04f, 0.02f, 0f };
        internal static readonly List<bool> frontPointsInward = new List<bool> { false, false, false, };
        internal static readonly List<bool> backPointsInward = new List<bool> { false, true, true };

        internal static readonly ConfigEntry<float>[] _danColliderLengthScale = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danColliderRadiusScale = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danLengthSquishFactor = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danGirthSquishFactor = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danSquishThreshold = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _danSquishOralGirth = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _simplifyPenetration = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _simplifyOral = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _rotateTamaWithShaft = new ConfigEntry<bool>[MaleLimit];

        internal static ConfigEntry<float> _clippingDepth;
        internal static ConfigEntry<Vector3> _kokanOffset;
        internal static ConfigEntry<Vector3> _innerKokanOffset;
        internal static ConfigEntry<Vector3> _mouthOffset;
        internal static ConfigEntry<Vector3> _innerMouthOffset;
        internal static readonly ConfigEntry<float>[] _frontCollisionOffset = new ConfigEntry<float>[frontOffsets.Count];
        internal static readonly ConfigEntry<float>[] _backCollisionOffset = new ConfigEntry<float>[backOffsets.Count];

        internal static Harmony harmony;
		
        //In VR, type "VRHScene" is used instead of "HSceneProc". Use type "BaseLoader" as the type for "hSceneProc" since it's inherited by both "HSceneProc" and "VRHScene".
        internal static BaseLoader hSceneProc;
        internal static Traverse hSceneProcTraverse;
        internal static bool hSceneStarted = false;
        internal static bool inHScene = false;
        internal static readonly bool loadingCharacter = false;
        internal static bool twoDans = false;
        internal static Type _uncensorSelectorType;
        internal static bool resetParticlesStep1 = false;
        internal static bool resetParticlesStep2 = false;
        internal static int resetParticlesCount = 0;

        internal void Awake()
        {
            instance = this;

            for (int maleNum = 0; maleNum < MaleLimit; maleNum++)
            {
                (_danColliderLengthScale[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Length Scale", 1.0f, new ConfigDescription("How much to scale collider length", new AcceptableValueRange<float>(0.5f, 1.5f)))).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danColliderRadiusScale[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Radius Scale", 1.0f, new ConfigDescription("How much to scale collider radius", new AcceptableValueRange<float>(0.5f, 1.5f)))).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danLengthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Length Factor", 0.6f, new ConfigDescription("How much the length of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danGirthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Girth Factor", 0.2f, new ConfigDescription("How much the girth of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danSquishThreshold[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Threshold", 0.2f, new ConfigDescription("Allows the penis to begin squishing (shorten length increase girth) after this amount of the penis has penetrated.", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danSquishOralGirth[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Oral Girth", false, "Allows the penis to squish (increase girth) during oral.")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_simplifyPenetration[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Simplify Penetration Calculation", false, "Simplifys penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_simplifyOral[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Simplify Oral Calculation", false, "Simplifys oral penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_rotateTamaWithShaft[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Rotate Balls with Shaft", true, "If enabled, the base of the balls will be locked to the base of the shaft")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
            }

            (_clippingDepth = Config.Bind("Female Options", "Clipping Depth", 0.02f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.")).SettingChanged += (s, e) =>
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
            (_mouthOffset = Config.Bind("Female Options", "Target Offset: Mouth Target", Vector3.zero, "Offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerMouthOffset = Config.Bind("Female Options", "Target Offset: Inner Mouth Target", Vector3.zero, "Offset of the simplified inner mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };

            harmony = new Harmony("KK_BetterPenetration");
            harmony.PatchAll(GetType());

            Type VRHSceneType = Type.GetType("VRHScene, Assembly-CSharp");
            if (VRHSceneType != null)
            {
                harmony.Patch(VRHSceneType.GetMethod("Start", AccessTools.all), postfix: new HarmonyMethod(GetType().GetMethod(nameof(HScene_PostStart), AccessTools.all)));
                harmony.Patch(VRHSceneType.GetMethod("EndProc", AccessTools.all), prefix: new HarmonyMethod(GetType().GetMethod(nameof(HSceneProc_EndProc), AccessTools.all)));
                harmony.Patch(VRHSceneType.GetMethod("Update", AccessTools.all), postfix: new HarmonyMethod(GetType().GetMethod(nameof(HScene_PostUpdate), AccessTools.all)));
                harmony.Patch(VRHSceneType.GetMethod("ChangeAnimator", AccessTools.all), prefix: new HarmonyMethod(GetType().GetMethod(nameof(HSceneProc_PreChangeAnimator), AccessTools.all)));
            }

            Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo info);
            if (info == null || info.Instance == null)
                return;

            _uncensorSelectorType = info.Instance.GetType();
            Type uncensorSelectorControllerType = _uncensorSelectorType.GetNestedType("UncensorSelectorController", AccessTools.all);
            if (uncensorSelectorControllerType == null)
                return;

            MethodInfo uncensorSelectorReloadCharacterBody = AccessTools.Method(uncensorSelectorControllerType, "ReloadCharacterBody");
            if (uncensorSelectorReloadCharacterBody == null)
                return;
            
            harmony.Patch(uncensorSelectorReloadCharacterBody, postfix: new HarmonyMethod(GetType(), nameof(UncensorSelector_ReloadCharacterBody_Postfix), new[] { typeof(object) }));
            Debug.Log("KK_BetterPenetration: UncensorSelector patched ReloadCharacterBody correctly");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        internal static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            CoreGame.RemoveCollidersFromCoordinate(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
        internal static void HScene_PostStart(BaseLoader __instance)
        {
            hSceneProc = __instance;
            hSceneProcTraverse = Traverse.Create(__instance);

            hSceneStarted = true;
            inHScene = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
        internal static void HSceneProc_EndProc()
        {
            CoreGame.SetAgentsBPBoneWeights(0f);
            CoreGame.OnEndScene();

            inHScene = false;

            if (hSceneProc == null)
                return;

            var lookDan = hSceneProcTraverse.Field<Lookat_dan>("lookDan").Value;
            if (lookDan != null)
                lookDan.transLookAtNull = null;

            hSceneProcTraverse = null;
            hSceneProc = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Update")]
        internal static void HScene_PostUpdate()
        {
            if (!hSceneStarted || inHScene || hSceneProc == null || !hSceneProc.enabled || hSceneProcTraverse == null)
                return;

            List<DanOptions> danOptions = PopulateDanOptionsList();
            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();

            var lstFemale = hSceneProcTraverse.Field("lstFemale").GetValue<List<ChaControl>>();
            if (lstFemale == null || lstFemale.Count == 0)
                return;

            List<ChaControl> femaleList = new List<ChaControl>();
            foreach (var female in lstFemale)
                if (female != null)
                    femaleList.Add(female);

            List<ChaControl> maleList = new List<ChaControl>();
            var male = hSceneProcTraverse.Field("male").GetValue<ChaControl>();
            if (male != null)
                maleList.Add(male);

            var male1 = hSceneProcTraverse.Field("male1").GetValue<ChaControl>();
            if (male1 != null)
                maleList.Add(male1);

            if (maleList.IsNullOrEmpty())
                return;

            CoreGame.InitializeAgents(maleList, femaleList, danOptions, collisionOptions);
            CoreGame.SetAgentsBPBoneWeights(1f);
            inHScene = true;
            hSceneStarted = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "ChangeAnimator")]
        internal static void HSceneProc_PreChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo)
        {
            if (!inHScene || _nextAinmInfo == null || _nextAinmInfo.pathFemaleBase.file == null)
                return;

            CoreGame.OnChangeAnimation(_nextAinmInfo.pathFemaleBase.file);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "ChangeAnimator")]
        internal static void HSceneProc_PostChangeAnimator()
        {
            if (!inHScene)
                return;

            resetParticlesStep1 = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "setPlay")]
        internal static void ChaControl_PostSetPlay()
        {
            resetParticlesStep1 = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "SetInfo")]
        internal static void H_Lookat_dan_PostSetInfo(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 0)
                maleNum = 1;

            twoDans = false;

            CoreGame.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, maleNum, __instance.numFemale, twoDans);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "LateUpdate")]
        public static void Lookat_dan_PostLateUpdate(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            if (resetParticlesStep1)
            {
                CoreGame.ResetParticles();
                CoreGame.EnableParticles(false);
                resetParticlesStep1 = false;
                resetParticlesStep2 = true;
                resetParticlesCount = 0;
            }

            if (resetParticlesStep2 && ++resetParticlesCount > 3)
            {
                CoreGame.EnableParticles(true);
                resetParticlesStep2 = false;
            }

            int maleNum = 0;

            if (___male.chaID != 0)
            {
                if (!twoDans)
                    return;
                maleNum = 1;
            }

            CoreGame.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, false, maleNum, __instance.numFemale);

            var lstFemale = hSceneProcTraverse.Field("lstFemale").GetValue<List<ChaControl>>();
            if (lstFemale == null || lstFemale.Count == 0)
                return;

            List<ChaControl> femaleList = new List<ChaControl>();
            foreach (var female in lstFemale)
                if (female != null)
                    femaleList.Add(female);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "Release")]
        internal static void H_Lookat_dan_PostRelease(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 0)
                maleNum = 1;

            twoDans = false;

            CoreGame.LookAtDanRelease(maleNum, __instance.numFemale, twoDans);
        }

        internal static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanCollider(index, _danColliderRadiusScale[index].Value, _danColliderLengthScale[index].Value);
        }

        internal static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanOptions(index, _danLengthSquishFactor[index].Value, _danGirthSquishFactor[index].Value,
                    _danSquishThreshold[index].Value, _danSquishOralGirth[index].Value, false,
                    _simplifyPenetration[index].Value, _simplifyOral[index].Value, _rotateTamaWithShaft[index].Value);
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
            List<DanOptions> danOptions = new List<DanOptions>();

            for (int maleNum = 0; maleNum < MaleLimit; maleNum++)
            {
                danOptions.Add(new DanOptions(_danColliderRadiusScale[maleNum].Value, _danColliderLengthScale[maleNum].Value,
                    _danLengthSquishFactor[maleNum].Value, _danGirthSquishFactor[maleNum].Value, _danSquishThreshold[maleNum].Value, _danSquishOralGirth[maleNum].Value,
                    0, 0, false, _simplifyPenetration[maleNum].Value, _simplifyOral[maleNum].Value, _rotateTamaWithShaft[maleNum].Value));
            }

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
                collisionOptions.Add(new CollisionOptions(_kokanOffset.Value, _innerKokanOffset.Value, _mouthOffset.Value, _innerMouthOffset.Value,
                    false, 0, 0, 0, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }

        internal static void UncensorSelector_ReloadCharacterBody_Postfix(object __instance)
        {
            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null)
                return;
            CoreGame.SetBPBoneWeights(chaControl, inHScene ? 1f : 0f);
        }

    }
}