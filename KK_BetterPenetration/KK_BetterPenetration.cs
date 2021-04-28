using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Core_BetterPenetration;


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

        public const string VERSION = "3.1.3.1";
        private const int MaleLimit = 2;
        private const int FemaleLimit = 2;
        private const bool _useSelfColliders = false;

        private static readonly List<float> frontOffsets = new List<float> { -0.04f, 0.04f, 0.06f };
        private static readonly List<float> backOffsets = new List<float> { -0.04f, 0.04f, 0f };
        private static readonly List<bool> frontPointsInward = new List<bool> { false, false, false, };
        private static readonly List<bool> backPointsInward = new List<bool> { false, true, true };

        private static readonly ConfigEntry<float>[] _danColliderHeadLength = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danColliderRadius = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danColliderVerticalCenter = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danLengthSquishFactor = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danGirthSquishFactor = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<float>[] _danSquishThreshold = new ConfigEntry<float>[MaleLimit];
        private static readonly ConfigEntry<bool>[] _danSquishOralGirth = new ConfigEntry<bool>[MaleLimit];
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
        private static Traverse hSceneProcTraverse;
        private static bool hSceneStarted = false;
        private static bool inHScene = false;
        private static readonly bool loadingCharacter = false;
        private static bool twoDans = false;
        private static Type _uncensorSelectorType;

        private void Awake()
        {
            instance = this;

            for (int maleNum = 0; maleNum < _danColliderHeadLength.Length; maleNum++)
            {
                (_danColliderHeadLength[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Length of Head", 0.008f, "Distance from the center of the head bone to the tip, used for collision purposes.")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danColliderRadius[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Radius of Shaft", 0.024f, "Radius of the shaft collider.")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danColliderVerticalCenter[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis Collider: Vertical Center", 0.0f, "Vertical Center of the shaft collider")).SettingChanged += (s, e) =>
                { UpdateDanColliders(); };
                (_danLengthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Length Factor", 0.6f, new ConfigDescription("How much the length of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danGirthSquishFactor[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Girth Factor", 0.15f, new ConfigDescription("How much the girth of the penis squishes after it has passed the squish threshold", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danSquishThreshold[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Threshold", 0.2f, new ConfigDescription("Allows the penis to begin squishing (shorten length increase girth) after this amount of the penis has penetrated.", new AcceptableValueRange<float>(0, 1)))).SettingChanged += (s, e) =>
                { UpdateDanOptions(); };
                (_danSquishOralGirth[maleNum] = Config.Bind("Male " + (maleNum + 1) + " Options", "Penis: Squish Oral Girth", false, "Allows the penis to squish (increase girth) during oral.")).SettingChanged += (s, e) =>
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
            harmony.PatchAll(typeof(KK_BetterPenetration));

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
            
            harmony.Patch(uncensorSelectorReloadCharacterBody, postfix: new HarmonyMethod(typeof(KK_BetterPenetration), nameof(UncensorSelector_ReloadCharacterBody_Postfix), new[] { typeof(object) }));
            Debug.Log("KK_BetterPenetration: UncensorSelector patched ReloadCharacterBody correctly");
        }

        private static void UpdateDanColliders()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanCollider(index, _danColliderRadius[index].Value, _danColliderHeadLength[index].Value, _danColliderVerticalCenter[index].Value);
        }

        private static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            for (int index = 0; index < MaleLimit; index++)
                CoreGame.UpdateDanOptions(index, _danLengthSquishFactor[index].Value, _danGirthSquishFactor[index].Value,
                    _danSquishThreshold[index].Value, _danSquishOralGirth[index].Value, false,
                    _simplifyPenetration[index].Value, _simplifyOral[index].Value);
        }

        private static void UpdateCollisionOptions()
        {
            if (!inHScene)
                return;

            List<CollisionOptions> collisionOptions = PopulateCollisionOptionsList();
            for (int index = 0; index < FemaleLimit; index++)
                CoreGame.UpdateCollisionOptions(index, collisionOptions[index]);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        public static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            CoreGame.RemoveCollidersFromCoordinate(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
        public static void HScene_PostStart(HSceneProc __instance)
        {
            hSceneProc = __instance;
            hSceneProcTraverse = Traverse.Create(__instance);

            hSceneStarted = true;
            inHScene = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "EndProc")]
        internal static void HSceneProc_EndProc_Patch()
        {
            CoreGame.SetAgentsBPBoneWeights(0f);
            CoreGame.OnEndScene();

            inHScene = false;

            if (hSceneProc == null)
                return;

            if (hSceneProc.lookDan != null)
                hSceneProc.lookDan.transLookAtNull = null;

            hSceneProcTraverse = null;
            hSceneProc = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Update")]
        public static void HScene_PostUpdate()
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

        private static List<DanOptions> PopulateDanOptionsList()
        {
            List<DanOptions> danOptions = new List<DanOptions>();

            for (int maleNum = 0; maleNum < MaleLimit; maleNum++)
            {
                danOptions.Add(new DanOptions(_danColliderVerticalCenter[maleNum].Value, _danColliderRadius[maleNum].Value, _danColliderHeadLength[maleNum].Value,
                    _danLengthSquishFactor[maleNum].Value, _danGirthSquishFactor[maleNum].Value, _danSquishThreshold[maleNum].Value, _danSquishOralGirth[maleNum].Value,
                    0, 0, false, _simplifyPenetration[maleNum].Value, _simplifyOral[maleNum].Value));
            }

            return danOptions;
        }

        private static List<CollisionOptions> PopulateCollisionOptionsList()
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
                collisionOptions.Add(new CollisionOptions(_useSelfColliders, _kokanOffsetForward.Value, _kokanOffsetUp.Value, _headOffsetForward.Value, _headOffsetUp.Value,
                    false, 0, 0, 0, _clippingDepth.Value, frontInfo, backInfo));
            }

            return collisionOptions;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "ChangeAnimator")]
        private static void HSceneProc_PreChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo)
        {
            if (!inHScene || _nextAinmInfo == null || _nextAinmInfo.pathFemaleBase.file == null)
                return;

            CoreGame.OnChangeAnimation(_nextAinmInfo.pathFemaleBase.file);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "SetInfo")]
        private static void H_Lookat_dan_PostSetInfo(Lookat_dan __instance, ChaControl ___male)
        {

            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 0)
                maleNum = 1;

            twoDans = false;
            //       if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
            //           twoDans = true;

            CoreGame.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, maleNum, __instance.numFemale, twoDans);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "LateUpdate")]
        public static void Lookat_dan_PostLateUpdate(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;

            if (___male.chaID != 0)
            {
                if (!twoDans)
                    return;
                maleNum = 1;
            }

            CoreGame.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, false, maleNum, __instance.numFemale);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Lookat_dan), "Release")]
        private static void H_Lookat_dan_PostRelease(Lookat_dan __instance, ChaControl ___male)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || ___male == null)
                return;

            int maleNum = 0;
            if (___male != null && ___male.chaID != 0)
                maleNum = 1;

            twoDans = false;
            //       if (___assetName != null && ___assetName.Length != 0 && ___assetName.ToString().Contains("m2f"))
            //           twoDans = true;

            CoreGame.LookAtDanRelease(maleNum, __instance.numFemale, twoDans);
        }

        private static void UncensorSelector_ReloadCharacterBody_Postfix(object __instance)
        {
            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null)
                return;
            CoreGame.SetBPBoneWeights(chaControl, inHScene ? 1f : 0f);
        }

    }
}