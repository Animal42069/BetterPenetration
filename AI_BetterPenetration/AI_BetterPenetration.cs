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
    [BepInDependency("com.rclcircuit.bepinex.modboneimplantor", "1.1.1")]
    [BepInProcess("AI-Syoujyo")]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        internal const string VERSION = "5.0.0.0";
        internal const int MaleLimit = 1;
        internal const int FemaleLimit = 2;

        internal static readonly List<float> frontOffsets = new List<float> { -0.35f, 0.2f };
        internal static readonly List<float> backOffsets = new List<float> { -0.05f, 0.05f };
        internal static readonly List<bool> frontPointsInward = new List<bool> { false, false };
        internal static readonly List<bool> backPointsInward = new List<bool> { false, true };

        internal static ConfigEntry<float> _danColliderLengthScale;
        internal static ConfigEntry<float> _danColliderRadiusScale;
        internal static ConfigEntry<float> _danLengthSquishFactor;
        internal static ConfigEntry<float> _danGirthSquishFactor;
        internal static ConfigEntry<float> _danSquishThreshold;
        internal static ConfigEntry<bool> _danSquishOralGirth;
        internal static ConfigEntry<bool> _simplifyVaginal;
        internal static ConfigEntry<bool> _simplifyOral;
        internal static ConfigEntry<bool> _simplifyAnal;
        internal static ConfigEntry<bool> _rotateTamaWithShaft;
        internal static ConfigEntry<float> _maxCorrection;
        internal static ConfigEntry<bool> _limitCorrection;

        internal static ConfigEntry<KeyboardShortcut> _toggleMaleCollidersKey;
        internal static ConfigEntry<float> _clippingDepth;
        internal static ConfigEntry<float> _kokanOffset;
        internal static ConfigEntry<float> _innerKokanOffset;
        internal static ConfigEntry<float> _mouthOffset;
        internal static ConfigEntry<float> _innerMouthOffset;
        internal static ConfigEntry<bool> _useKokanFix;
        internal static ConfigEntry<float> _kokanFixPositionY;
        internal static ConfigEntry<float> _kokanFixPositionZ;
        internal static ConfigEntry<float> _kokanFixRotationX;
        internal static ConfigEntry<bool> _enableKokanPushPull;
        internal static ConfigEntry<float> _maxKokanPush;
        internal static ConfigEntry<float> _maxKokanPull;
        internal static ConfigEntry<float> _kokanPullRate;
        internal static ConfigEntry<float> _kokanReturnRate;
        internal static ConfigEntry<bool> _enableOralPushPull;
        internal static ConfigEntry<float> _maxOralPush;
        internal static ConfigEntry<float> _maxOralPull;
        internal static ConfigEntry<float> _oralPullRate;
        internal static ConfigEntry<float> _oralReturnRate;
        internal static ConfigEntry<bool> _useAnaAdjustment;
        internal static ConfigEntry<Vector3> _anaAdjustPosition;
        internal static ConfigEntry<Vector3> _anaAdjustRotation;
        internal static ConfigEntry<bool> _enableAnaPushPull;
        internal static ConfigEntry<float> _maxAnaPush;
        internal static ConfigEntry<float> _maxAnaPull;
        internal static ConfigEntry<float> _anaPullRate;
        internal static ConfigEntry<float> _anaReturnRate;
        internal static ConfigEntry<CollisionOptions.TargetType> _outerTarget;
        internal static ConfigEntry<CollisionOptions.TargetType> _innerTarget;
        internal static ConfigEntry<float> _bellyBulgeScale;
        internal static ConfigEntry<bool> _bellyBulgeEnable;

        internal static readonly ConfigEntry<float>[] _frontCollisionOffset = new ConfigEntry<float>[frontOffsets.Count];
        internal static readonly ConfigEntry<float>[] _backCollisionOffset = new ConfigEntry<float>[backOffsets.Count];

        internal static Harmony harmony;
        internal static HScene hScene;
        internal static bool inHScene = false;
        internal static bool loadingCharacter = false;
        internal static bool changeAnimation = false;

        internal void Awake()
        {
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
            (_simplifyVaginal = Config.Bind("Male Options", "Simplify Penetration Calculation", false, "Simplifys penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_simplifyOral = Config.Bind("Male Options", "Simplify Oral Calculation", false, "Simplifys oral penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_simplifyAnal = Config.Bind("Male Options", "Simplify Anal Calculation", true, "Simplifys anal penetration calclation by always having it target the same internal point.  Only valid for BP penis uncensors.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };    
            (_rotateTamaWithShaft = Config.Bind("Male Options", "Rotate Balls with Shaft", true, "If enabled, the base of the balls will be locked to the base of the shaft")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_limitCorrection = Config.Bind("Male Options", "Limit Penis Movement", true, "Limit the penis from moving laterally too much from frame to frame.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };
            (_maxCorrection = Config.Bind("Male Options", "Limit Penis Amount", 10.0f, "Amount of movement to limit the penis to.  Smaller values result in smoother animations, but can cause clipping.")).SettingChanged += (s, e) =>
            { UpdateDanOptions(); };

            (_clippingDepth = Config.Bind("Advanced", "Clipping Depth", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            for (int offset = 0; offset < frontOffsets.Count; offset++)
                (_frontCollisionOffset[offset] = Config.Bind("Advanced", "Clipping Offset: Front Collision " + offset, frontOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            for (int offset = 0; offset < backOffsets.Count; offset++)
                (_backCollisionOffset[offset] = Config.Bind("Advanced", "Clipping Offset: Back Collision " + offset, backOffsets[offset], "Individual offset on colision point, to improve clipping")).SettingChanged += (s, e) =>
                { UpdateCollisionOptions(); };
            (_kokanOffset = Config.Bind("Advanced", "Vagina Offset: Outer Target", 0.0f, "Vertical offset of the vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerKokanOffset = Config.Bind("Advanced", "Vagina Offset: Inner Target", 0.0f, "Vertical offset of the simplified inner vagina target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_mouthOffset = Config.Bind("Advanced", "Mouth Offset: Outer Target", 0.025f, "Vertical offset of the mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerMouthOffset = Config.Bind("Advanced", "Mouth Offset: Inner Target", 0.0f, "Vertical offset of the simplified inner mouth target")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_useKokanFix = Config.Bind("Joint Adjustment", "Missionary Correction", false, "NOTE: There is an Illusion bug that causes the vagina to appear sunken in certain missionary positions.  It is best to use Advanced Bonemod and adjust your female character's cf_J_Kokan Offset Y to 0.001.  If you don't do that, enabling this option will attempt to fix the problem by guessing where the bone should be")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixPositionY = Config.Bind("Joint Adjustment", "Missionary Position Y", -0.1f, "Amount to adjust the Vagina bone position Y for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixPositionZ = Config.Bind("Joint Adjustment", "Missionary Position Z", 0.1f, "Amount to adjust the Vagina bone position Z for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanFixRotationX = Config.Bind("Joint Adjustment", "Missionary Rotation X", 15.0f, "Amount to adjust the Vagina bone rotation X for certain Missionary positions to correct its appearance")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_useAnaAdjustment = Config.Bind("Joint Adjustment", "Anal Correction", true, "Enable adjustment of butt bones during Anal positions.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_anaAdjustPosition = Config.Bind("Joint Adjustment", "Anal Position", new Vector3(0.3f, 0f, 0f), "Amount to adjust the butt bones position for Anal positions")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_anaAdjustRotation = Config.Bind("Joint Adjustment", "Anal Rotation", new Vector3(0f, 10f, 20f), "Amount to adjust the butt bones rotation for Anal positions")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_enableKokanPushPull = Config.Bind("Push/Pull", "Vaginal Enable", true, "Enable vaginal push/pull during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxKokanPush = Config.Bind("Push/Pull", "Vaginal Max Push", 0.075f, "Maximum amount to push the vagina inwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxKokanPull = Config.Bind("Push/Pull", "Vaginal Max Pull", 0.15f, "Maximum amount to pull the vagina outwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanPullRate = Config.Bind("Push/Pull", "Vaginal Push/Pull Rate", 36.0f, "How quickly to push or pull the vagina during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_kokanReturnRate = Config.Bind("Push/Pull", "Vaginal Return Rate", 0.3f, "How quickly the vagina returns to its original shape when there is no penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_enableOralPushPull = Config.Bind("Push/Pull", "Oral Enable", true, "Enable mouth push/pull during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxOralPush = Config.Bind("Push/Pull", "Oral Max Push", 0.02f, "Maximum amount to push the mouth inwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxOralPull = Config.Bind("Push/Pull", "Oral Max Pull", 0.1f, "Maximum amount to pull the mouth outwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_oralPullRate = Config.Bind("Push/Pull", "Oral Push/Pull Rate", 18.0f, "How quickly to push or pull the mouth during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_oralReturnRate = Config.Bind("Push/Pull", "Oral Return Rate", 0.3f, "How quickly the mouth returns to its original shape when there is no penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_enableAnaPushPull = Config.Bind("Push/Pull", "Anal Enable", true, "Enable anus push/pull during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxAnaPush = Config.Bind("Push/Pull", "Anal Max Push", 0.0f, "Maximum amount to push the anus inwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_maxAnaPull = Config.Bind("Push/Pull", "Anal Max Pull", 0.2f, "Maximum amount to pull the anus outwards during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_anaPullRate = Config.Bind("Push/Pull", "Anal Push/Pull Rate", 36.0f, "How quickly to push or pull the anus during penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_anaReturnRate = Config.Bind("Push/Pull", "Anal Return Rate", 0.3f, "How quickly the anus returns to its original shape when there is no penetration")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };

            _toggleMaleCollidersKey = Config.Bind("Advanced", "Toggle Male Colliders", new KeyboardShortcut(KeyCode.P, KeyCode.LeftAlt), "Shortcut key to turn male colliders on or off");

            (_outerTarget = Config.Bind("Advanced", "Outer Target", CollisionOptions.TargetType.Average)).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_innerTarget = Config.Bind("Advanced", "Inner Target", CollisionOptions.TargetType.Inside)).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };


            (_bellyBulgeScale = Config.Bind("Belly Bulge", "Scale", 1.0f, new ConfigDescription("How much to scale belly colliders", new AcceptableValueRange<float>(0.1f, 3.0f)))).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            (_bellyBulgeEnable = Config.Bind("Belly Bulge", "Enable", true, "Allows the belly to deform during certain vaginal positions.")).SettingChanged += (s, e) =>
            { UpdateCollisionOptions(); };
            harmony = new Harmony("AI_BetterPenetration");
            harmony.PatchAll(typeof(AI_BetterPenetration));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "UpdateAccessoryMoveFromInfo")]
        internal static void ChaControl_UpdateAccessoryMoveFromInfo(ChaControl __instance)
        {
            Tools.RemoveCollidersFromCoordinate(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "UpdateSiru")]
        internal static void ChaControl_UpdateSiru(ChaControl __instance, bool forceChange)
        {
            if (!forceChange)
                return;

            Tools.RemoveCollidersFromCoordinate(__instance);
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

            CoreGame.ClearItemColliders();
            CoreGame.OnChangeAnimation(_info.fileFemale);
            changeAnimation = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "SetMovePositionPoint")]
        internal static void HScene_SetMovePositionPoint()
        {
            changeAnimation = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "setPlay")]
        internal static void ChaControl_PostSetPlay()
        {
            changeAnimation = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "Release")]
        internal static void H_Lookat_dan_PostRelease(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null || __instance.male == null)
                return;

            int maleNum = 0;
            if (__instance.male.chaID != 99)
                maleNum = 1;

            CoreGame.LookAtDanRelease(maleNum, __instance.numFemale, false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        internal static void H_Lookat_dan_PostSetInfo(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || __instance.strPlayMotion == null)
                return;

            CoreGame.LookAtDanSetup(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, 0, 0, false, true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        internal static void H_Lookat_dan_PostLateUpdate(H_Lookat_dan __instance)
        {
            if (!inHScene || loadingCharacter || hScene == null || __instance.strPlayMotion == null)
                return;

            if (changeAnimation && !hScene.NowChangeAnim)
            {
                CoreGame.ResetParticles();
                CoreGame.SetupFingerColliders(hScene.ctrlFlag.nowAnimationInfo.fileFemale);
                CoreGame.SetupItemColliders(hScene.ctrlFlag.nowAnimationInfo.fileFemale);
                changeAnimation = false;
            }

            if (_toggleMaleCollidersKey.Value.IsDown())
                CoreGame.ToggleMaleColliders();

            CoreGame.LookAtDanUpdate(__instance.transLookAtNull, __instance.strPlayMotion, __instance.bTopStick, hScene.NowChangeAnim, 0, 0, false, true);
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

        internal static void UpdateDanOptions()
        {
            if (!inHScene)
                return;

            CoreGame.UpdateDanOptions(0, _danLengthSquishFactor.Value, _danGirthSquishFactor.Value, 
				_danSquishThreshold.Value, _danSquishOralGirth.Value, 
				_simplifyVaginal.Value, _simplifyOral.Value, _rotateTamaWithShaft.Value,
				_limitCorrection.Value, _maxCorrection.Value);
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
                _simplifyVaginal.Value, _simplifyOral.Value, _simplifyAnal.Value, _rotateTamaWithShaft.Value,
                _limitCorrection.Value, _maxCorrection.Value)
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
                collisionOptions.Add(new CollisionOptions(_kokanOffset.Value, _innerKokanOffset.Value, _mouthOffset.Value, _innerMouthOffset.Value, 
                    _useKokanFix.Value, _kokanFixPositionZ.Value, _kokanFixPositionY.Value, _kokanFixRotationX.Value,
                    _useAnaAdjustment.Value, _anaAdjustPosition.Value, _anaAdjustRotation.Value,
                    _clippingDepth.Value, frontInfo, backInfo,
                    _enableKokanPushPull.Value, _maxKokanPush.Value, _maxKokanPull.Value, _kokanPullRate.Value, _kokanReturnRate.Value,
                    _enableOralPushPull.Value, _maxOralPush.Value, _maxOralPull.Value, _oralPullRate.Value, _oralReturnRate.Value,
                    _enableAnaPushPull.Value, _maxAnaPush.Value, _maxAnaPull.Value, _anaPullRate.Value, _anaReturnRate.Value, _outerTarget.Value, _innerTarget.Value,
                    _bellyBulgeEnable.Value, _bellyBulgeScale.Value));
            }

            return collisionOptions;
        }


        internal void Update()
        {
            var isHScene = HSceneManager.isHScene;
            
            if (!isHScene && inHScene)
                HScene_sceneUnloaded();
        }

        internal static void HScene_sceneUnloaded()
        {
            CoreGame.OnEndScene();

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