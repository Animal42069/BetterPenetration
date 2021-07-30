using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;
using AIChara;
using Core_BetterPenetration;
using UnityEngine;

namespace HS2_BetterPenetration
{
    [BepInPlugin("animal42069.HS2betterpenetration", "HS2 Better Penetration", VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.3")]
    [BepInProcess("HoneySelect2")]
    [BepInProcess("HoneySelect2VR")]
    public class HS2_BetterPenetration : BaseUnityPlugin
    {
        internal const string VERSION = "4.2.1.0";
        internal const int MaleLimit = 2;
        internal const int FemaleLimit = 2;

        internal static readonly List<float> frontOffsets = new List<float> { -0.35f, 0f };
        internal static readonly List<float> backOffsets = new List<float> { -0.05f, 0.05f };
        internal static readonly List<bool> frontPointsInward = new List<bool> { false, false };
        internal static readonly List<bool> backPointsInward = new List<bool> { false, true };
        
        internal static readonly ConfigEntry<float>[] _danColliderLengthScale = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danColliderRadiusScale = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _fingerColliderLength = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _fingerColliderRadius = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danLengthSquishFactor = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danGirthSquishFactor = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<float>[] _danSquishThreshold = new ConfigEntry<float>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _danSquishOralGirth = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _useFingerColliders = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _simplifyPenetration = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _simplifyOral = new ConfigEntry<bool>[MaleLimit];
        internal static readonly ConfigEntry<bool>[] _rotateTamaWithShaft = new ConfigEntry<bool>[MaleLimit];

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
        internal static bool twoDans;
        internal static bool resetParticles = false;

        internal void Awake()
        {
            for (int maleNum = 0; maleNum < MaleLimit; maleNum++)
            {
                (_fingerColliderLength[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Length", 0.18f, "Lenght of the finger colliders.")).SettingChanged += (s, e) =>
                { UpdateFingerColliders(); };
                (_fingerColliderRadius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Radius", 0.06f, "Radius of the finger colliders.")).SettingChanged += (s, e) =>
                { UpdateFingerColliders(); };
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
                (_useFingerColliders[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Enable", true, "Use finger colliders")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_simplifyPenetration[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Simplify Penetration Calculation", false, "Simplifys penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_simplifyOral[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Simplify Oral Calculation", false, "Simplifys oral penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_rotateTamaWithShaft[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Rotate Balls with Shaft", true, "If enabled, the base of the balls will be locked to the base of the shaft")).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
            }

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

            harmony = new Harmony("HS2_BetterPenetration");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
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
        internal static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance, System.Text.StringBuilder ___assetName, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 99)
                maleNum = 1;

            twoDans = false;
            if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
                twoDans = true;

            CoreGame.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, maleNum, __instance.numFemale, twoDans);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        internal static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            if (resetParticles && !hScene.NowChangeAnim)
            {
                CoreGame.ResetParticles();
                resetParticles = false;
            }

            int maleNum = 0;

            if (___male.chaID != 99)
            {
                if (!twoDans)
                    return;
                maleNum = 1;
            }

            CoreGame.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, hScene.NowChangeAnim, maleNum, __instance.numFemale);
        }
		
        internal static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanCollider(index, _danColliderRadiusScale[index].Value, _danColliderLengthScale[index].Value);
        }

        internal static void UpdateFingerColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateFingerColliders(index, _fingerColliderRadius[index].Value, _fingerColliderLength[index].Value);
        }

        internal static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanOptions(index, _danLengthSquishFactor[index].Value, _danGirthSquishFactor[index].Value, 
                    _danSquishThreshold[index].Value, _danSquishOralGirth[index].Value, _useFingerColliders[index].Value, 
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
                    _fingerColliderRadius[maleNum].Value, _fingerColliderLength[maleNum].Value, _useFingerColliders[maleNum].Value, 
                    _simplifyPenetration[maleNum].Value, _simplifyOral[maleNum].Value, _rotateTamaWithShaft[maleNum].Value));
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
                collisionOptions.Add(new CollisionOptions(_kokanOffset.Value, _innerKokanOffset.Value, _mouthOffset.Value, _innerMouthOffset.Value, _useKokanFix.Value,
                    _kokanFixPositionZ.Value, _kokanFixPositionY.Value, _kokanFixRotationX.Value, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }

        internal static void BeforeCharacterReload(object __instance)
        { 
            if (!inHScene)
                return;

            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null || chaControl.sex == 0)
                return;

            loadingCharacter = true;
            CoreGame.SetDansHaveNewTarget(true);
        }

        internal static void AfterCharacterReload(object __instance)
        {
            if (!inHScene || hScene == null)
                return;

            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null || chaControl.sex == 0)
                return;

            ChaControl[] femaleArray = hScene.GetFemales();
            List<ChaControl> femaleList = new List<ChaControl>();

            foreach (var character in femaleArray)
            {
                if (character == null)
                    continue;
                femaleList.Add(character);
            }

            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();
            CoreGame.InitializeCollisionAgents(femaleList, collisionOptions);
            loadingCharacter = false;
        }

        internal static void BeforeDanCharacterReload(object __instance)
        {
            if (!inHScene)
                return;

            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null || (chaControl.sex != 0 && !chaControl.fileParam.futanari))
                return;

            loadingCharacter = true;
            CoreGame.SetDansHaveNewTarget(true);
            CoreGame.ClearDanAgents();
        }

        internal static void AfterDanCharacterReload(object __instance)
        {
            if (!inHScene || hScene == null)
                return;

            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null || (chaControl.sex != 0 && !chaControl.fileParam.futanari))
                return;

            List<DanOptions> danOptions = PopulateDanOptionsList();

            ChaControl[] maleArray = hScene.GetMales();
            List<ChaControl> maleList = new List<ChaControl>();
            foreach (var character in maleArray)
            {
                if (character == null)
                    continue;
                maleList.Add(character);
            }

            CoreGame.InitializeDanAgents(maleList, danOptions);
            loadingCharacter = false;
        }


        internal static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            if (UnityEngine.Application.productName == "HoneySelect2VR") {
                if (scene.name == "Init" || scene.name == "VRTitle" || scene.name == "VRLogo" || scene.name == "VRSelect") // for the official HoneySelect2VR, the LoadSceneMode can be Multiple, and the scene name is equal to the map name, not "HScene"
                    return;
            } else {
                if (lsm != LoadSceneMode.Single || patched || scene.name != "HScene")
                    return;
            }

            harmony.PatchAll(typeof(HS2_BetterPenetration));
            patched = true;

            Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo pluginInfo);
            if (pluginInfo != null && pluginInfo.Instance != null)
            {
                Type nestedType = pluginInfo.Instance.GetType().GetNestedType("UncensorSelectorController", AccessTools.all);
                if (nestedType != null)
                {
                    MethodInfo methodInfo = AccessTools.Method(nestedType, "ReloadCharacterBody", null, null);
                    if (methodInfo != null)
                    {
                        harmony.Patch(methodInfo, new HarmonyMethod(typeof(HS2_BetterPenetration), "BeforeCharacterReload"), new HarmonyMethod(typeof(HS2_BetterPenetration), "AfterCharacterReload"), null, null);
                        Debug.Log("HS2_BetterPenetration: patched UncensorSelector::ReloadCharacterBody correctly");
                    }

                    methodInfo = AccessTools.Method(nestedType, "ReloadCharacterPenis", null, null);
                    if (methodInfo != null)
                    {
                        harmony.Patch(methodInfo, new HarmonyMethod(typeof(HS2_BetterPenetration), "BeforeDanCharacterReload"), new HarmonyMethod(typeof(HS2_BetterPenetration), "AfterDanCharacterReload"), null, null);
                        Debug.Log("HS2_BetterPenetration: patched UncensorSelector::ReloadCharacterPenis patched");
                    }
                }
            }
        }

        internal static void SceneManager_sceneUnloaded(Scene scene)
        {
            if(UnityEngine.Application.productName == "HoneySelect2VR") {
                if (scene.name == "Init" || scene.name == "VRTitle" || scene.name == "VRLogo" || scene.name == "VRSelect")
                    return;
            } else {
                if (!patched || scene.name != "HScene")
                    return;
			}

            CoreGame.OnEndScene();

            harmony.UnpatchAll(nameof(HS2_BetterPenetration));
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