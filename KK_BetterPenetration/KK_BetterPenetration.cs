using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Core_BetterPenetration;

namespace KK_BetterPenetration
{
    [BepInPlugin("animal42069.KKbetterpenetration", "KK Better Penetration", VERSION)]
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    public class KK_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "3.0.0.0";
        private const int MaleLimit = 2;
        private const int FemaleLimit = 2;
        private const bool _useSelfColliders = false;

        private static readonly List<float> frontOffsets = new List<float> { -0.035f, -0.04f, -0.02f};
        private static readonly List<float> backOffsets = new List<float> { -0.005f, 0.01f, 0.01f};
        private static readonly List<bool> frontPointsInward = new List<bool> { false, false, false,};
        private static readonly List<bool> backPointsInward = new List<bool> { false, true, true};

        private static readonly ConfigEntry<float>[] _danColliderHeadLength = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danColliderRadius = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danColliderVerticalCenter = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _fingerColliderLength = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _fingerColliderRadius = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danLengthSquishFactor = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danGirthSquishFactor = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danSquishThreshold = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<bool>[] _danSquishOralGirth = new ConfigEntry<bool>[MaleLimit];
        private static readonly ConfigEntry<bool>[] _useFingerColliders = new ConfigEntry<bool>[MaleLimit];
        private static readonly ConfigEntry<bool>[] _simplifyPenetration = new ConfigEntry<bool>[MaleLimit];
        private static readonly ConfigEntry<bool>[] _simplifyOral = new ConfigEntry<bool>[MaleLimit];

        private static ConfigEntry<float> _clippingDepth;
        private static ConfigEntry<float> _kokanOffsetForward;
        private static ConfigEntry<float> _kokanOffsetUp;
        private static ConfigEntry<float> _headOffsetForward;
        private static ConfigEntry<float> _headOffsetUp;
        private static readonly ConfigEntry<float>[] _frontCollisionOffset = new ConfigEntry<float>[frontOffsets.Count];
        private static readonly ConfigEntry<float>[] _backCollisionOffset = new ConfigEntry<float>[backOffsets.Count];

        private static Harmony harmony;
        private static HSceneProc hSceneProc;
        private static bool patched = false;
        private static bool hSceneStarted = false;
        private static bool inHScene = false;
        private static readonly bool loadingCharacter = false;
        private static bool twoDans = false;

        private void Awake()
        {
            for (int maleNum = 0; maleNum < _danColliderHeadLength.Length; maleNum++)
            {
                (_fingerColliderLength[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Length", 0.06f, "Lenght of the finger colliders.")).SettingChanged += (s, e) =>
                { UpdateFingerColliders(); };
                (_fingerColliderRadius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Finger Collider: Radius", 0.02f, "Radius of the finger colliders.")).SettingChanged += (s, e) =>
                { UpdateFingerColliders(); };
                (_danColliderHeadLength[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Length of Head", 0.035f, "Distance from the center of the head bone to the tip, used for collision purposes.")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danColliderRadius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Radius of Shaft", 0.032f, "Radius of the shaft collider.")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danColliderVerticalCenter[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Vertical Center", -0.003f, "Vertical Center of the shaft collider")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danLengthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Length Factor", 0.5f, new ConfigDescription("How much the length of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danGirthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Girth Factor", 1.1f, new ConfigDescription("How much the girth of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(1, 2)))).SettingChanged += (s, e) =>
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
            }

            (_clippingDepth = Config.Bind("Female Options", "Clipping Depth", 0.02f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            for (int offset = 0; offset < frontOffsets.Count; offset++)
                (_frontCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Front Collision " + offset, frontOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            for (int offset = 0; offset < backOffsets.Count; offset++)
                (_backCollisionOffset[offset] = Config.Bind("Female Options", "Clipping Offset: Back Collision " + offset, backOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            (_kokanOffsetForward = Config.Bind("Female Options", "Target Offset: Vagina Vertical", -0.007f, "Vertical offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanOffsetUp = Config.Bind("Female Options", "Target Offset: Vagina Depth", 0.0f, "Depth offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_headOffsetForward = Config.Bind("Female Options", "Target Offset: Mouth Depth", 0.0f, "Depth offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_headOffsetUp = Config.Bind("Female Options", "Target Offset: Mouth Vertical", 0.00f, "Vertical offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };

            harmony = new Harmony("KK_BetterPenetration");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                Core.UpdateDanCollider(index, _danColliderRadius[index].Value, _danColliderHeadLength[index].Value, _danColliderVerticalCenter[index].Value);
        }

        private static void UpdateFingerColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                Core.UpdateFingerColliders(index, _fingerColliderRadius[index].Value, _fingerColliderLength[index].Value);
        }

        private static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                Core.UpdateDanOptions(index, _danLengthSquishFactor[index].Value, _danGirthSquishFactor[index].Value,
                    _danSquishThreshold[index].Value, _danSquishOralGirth[index].Value, _useFingerColliders[index].Value,
                    _simplifyPenetration[index].Value, _simplifyOral[index].Value);
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

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
        public static void HScene_PostStart()
        {
            Console.WriteLine("HSceneProc Start");

            hSceneStarted = true;
            inHScene = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Update")]
        public static void HScene_PostUpdate(HSceneProc __instance, List<ChaControl> ___lstFemale, ChaControl ___male, ChaControl ___male1)
        {
            if (!hSceneStarted || !__instance.enabled || inHScene)
                return;

            hSceneProc = __instance;

            List<DanOptions> danOptions = PopulateDanOptionsList();
            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();

            if (___lstFemale == null || ___lstFemale.Count == 0)
                return;

            List<ChaControl> femaleList = new List<ChaControl>();

            foreach (var female in ___lstFemale)
                if (female != null)
                    femaleList.Add(female);
            
            List<ChaControl> maleList = new List<ChaControl>();
            if (___male != null)
                maleList.Add(___male);

            if (___male1 != null)
                maleList.Add(___male1);

            Core.InitializeAgents(maleList, femaleList, danOptions, collisionOptions);
            inHScene = true;
            hSceneStarted = false;
        }

        private static List<DanOptions> PopulateDanOptionsList()
        {
            List<DanOptions> danOptions = new List<DanOptions>();

            for (int maleNum = 0; maleNum < MaleLimit; maleNum++)
            {
                danOptions.Add(new DanOptions(_danColliderVerticalCenter[maleNum].Value, _danColliderRadius[maleNum].Value, _danColliderHeadLength[maleNum].Value,
                    _danLengthSquishFactor[maleNum].Value, _danGirthSquishFactor[maleNum].Value, _danSquishThreshold[maleNum].Value, _danSquishOralGirth[maleNum].Value,
                    _fingerColliderRadius[maleNum].Value, _fingerColliderLength[maleNum].Value, _useFingerColliders[maleNum].Value,
                    _simplifyPenetration[maleNum].Value, _simplifyOral[maleNum].Value));
            }

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
                collisionOptions.Add(new CollisionOptions(_useSelfColliders, _kokanOffsetForward.Value, _kokanOffsetUp.Value, _headOffsetForward.Value, _headOffsetUp.Value,
                    false, 0, 0, 0, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "ChangeAnimator")]
        private static void HSceneProc_PreChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo)
        {
            Console.WriteLine("HSceneProc ChangeAnimator");

            if (!inHScene || _nextAinmInfo == null || _nextAinmInfo.pathFemaleBase.file == null)
                return;

            Console.WriteLine($"_nextAinmInfo {_nextAinmInfo.pathFemaleBase.file}");

            Core.OnChangeAnimation(_nextAinmInfo.pathFemaleBase.file);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "SetInfo")]
        private static void H_Lookat_dan_PostSetInfo(Lookat_dan __instance, ChaControl ___male)
        {
 
            if (!inHScene || loadingCharacter || __instance == null || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 0)
                maleNum = 1;

            twoDans = false;
     //       if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
     //           twoDans = true;

            Core.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, maleNum, __instance.numFemale, twoDans);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "LateUpdate")]
        public static void Lookat_dan_PostLateUpdate(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance == null || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;

            if (___male.chaID != 0)
            {
                if (!twoDans)
                    return;
                maleNum = 1;
            }

            Core.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, false, maleNum, __instance.numFemale);
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            Console.WriteLine($"SceneManager_sceneLoaded {scene.name}");

            if (patched || scene.name != "HProc")
                return;

            harmony.PatchAll(typeof(KK_BetterPenetration));
            patched = true;
        }

        private static void SceneManager_sceneUnloaded(Scene scene)
        {
            Console.WriteLine($"SceneManager_sceneUnloaded {scene.name}");

            if (!patched || scene.name != "HProc")
                return;

            Core.OnEndScene();

            harmony.UnpatchAll(nameof(KK_BetterPenetration));
            patched = false;
            inHScene = false;

            if (hSceneProc == null)
                return;

            if (hSceneProc.lookDan != null)
                hSceneProc.lookDan.transLookAtNull = null;

            if (hSceneProc.lookDan1 != null)
                hSceneProc.lookDan1.transLookAtNull = null;

            hSceneProc = null;
        }
    }
}