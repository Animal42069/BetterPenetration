using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Chara;
using UniRx;
using System;
using System.Linq;
using System.Reflection;
using AIChara;
using Core_BetterPenetration;

namespace AI_Studio_BetterPenetration
{
    [BepInPlugin(GUID, PluginName, VERSION)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", "3.10")]
    [BepInDependency("com.rclcircuit.bepinex.modboneimplantor", "1.1.1")]
    [BepInDependency("com.joan6694.illusionplugins.nodesconstraints")]
    [BepInProcess("StudioNEOV2")]
    public class AI_Studio_BetterPenetration : BaseUnityPlugin
    {
        internal const string GUID = "com.animal42069.studiobetterpenetration";
        internal const string PluginName = "AI Studio Better Penetration";
        internal const string VERSION = "4.4.0.0";
        internal const string BEHAVIOR = "BetterPenetrationController";
        internal const string StudioCategoryName = "Better Penetration";
        internal static Harmony harmony;
        internal static BaseUnityPlugin nodeConstraintPlugin;
		internal static bool reloadConstraints = false;
        internal static int updateCount = 0;
        internal static int resetDelay = 0;

        internal void Main()
        {
            CharacterApi.RegisterExtraBehaviour<BetterPenetrationController>(BEHAVIOR);

            harmony = new Harmony("AI_Studio_BetterPenetration");
            harmony.PatchAll(GetType());

            Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo pluginInfo);
            if (pluginInfo == null || pluginInfo.Instance == null)
                return;

            Type nestedType = pluginInfo.Instance.GetType().GetNestedType("UncensorSelectorController", AccessTools.all);
            if (nestedType == null)
                return;

            MethodInfo methodInfo = AccessTools.Method(nestedType, "ReloadCharacterBody", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, prefix: new HarmonyMethod(GetType(), "BeforeCharacterReload"));
            UnityEngine.Debug.Log("Studio_BetterPenetration: patched UncensorSelector::ReloadCharacterBody correctly");

            methodInfo = AccessTools.Method(nestedType, "ReloadCharacterBalls", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), "AfterTamaCharacterReload"));
            UnityEngine.Debug.Log("Studio_BetterPenetration: patched UncensorSelectorController::ReloadCharacterBalls correctly");

            Chainloader.PluginInfos.TryGetValue("com.joan6694.illusionplugins.nodesconstraints", out pluginInfo);
            if (pluginInfo == null || pluginInfo.Instance == null)
                return;

            nodeConstraintPlugin = pluginInfo.Instance;
            Type nodeConstraintType = nodeConstraintPlugin.GetType();
            if (nodeConstraintType == null)
                return;

            methodInfo = AccessTools.Method(nodeConstraintType, "AddConstraint", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(AfterAddConstraint)));
            UnityEngine.Debug.Log("Studio_BetterPenetration: patched NodeConstraints::AddConstraint correctly");

            methodInfo = AccessTools.Method(nodeConstraintType, "ApplyNodesConstraints", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(AfterApplyNodesConstraints)));
            UnityEngine.Debug.Log("Studio_BetterPenetration: patched NodeConstraints::ApplyNodesConstraints correctly");

            methodInfo = AccessTools.Method(nodeConstraintType, "ApplyConstraints", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(AfterApplyConstraints)));
            UnityEngine.Debug.Log("Studio_BetterPenetration: patched NodeConstraints::ApplyConstraints correctly");

            RegisterStudioControllerBasic();
        }

        public static void RegisterStudioControllerBasic()
        {
            if (!StudioAPI.InsideStudio)
                return;

            var enablePushPull = new CurrentStateCategorySwitch("Push/Pull Target", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().EnablePushPull);
            enablePushPull.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.EnablePushPull = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(enablePushPull);

            var bpEnable = new CurrentStateCategorySwitch("Enable BP Controller", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().enabled);
            bpEnable.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                {
                    if (value == false)
                    {
                        controller.ClearDanAgent();
                        controller.enabled = false;
                    }
                    else
                    {
                        controller.enabled = true;
                        controller.InitializeDanAgent();
                        controller.AddDanConstraints(nodeConstraintPlugin);
                    }
                }
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(bpEnable);

            var colliderRadiusScale = new CurrentStateCategorySlider("Collilder Radius Scale", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanColliderRadiusScale, 0.5f, 1.5f);
            colliderRadiusScale.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanColliderRadiusScale = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(colliderRadiusScale);

            var colliderLengthScale = new CurrentStateCategorySlider("Collilder Length Scale", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanColliderLengthScale, 0.5f, 1.5f);
            colliderLengthScale.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanColliderLengthScale = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(colliderLengthScale);

            var lengthSlider = new CurrentStateCategorySlider("Length Squish", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanLengthSquish, 0f, 1f);
            lengthSlider.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanLengthSquish = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(lengthSlider);

            var girthSlider = new CurrentStateCategorySlider("Girth Squish", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanGirthSquish, 0f, 2f);
            girthSlider.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanGirthSquish = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(girthSlider);

            var thresholdSlider = new CurrentStateCategorySlider("Squish Threshold", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanSquishThreshold, 0f, 1f);
            thresholdSlider.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanSquishThreshold = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(thresholdSlider);
        }

        public static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio)
                return;

            var autoTargeter = new CurrentStateCategoryDropdown("Auto-Target", new string[] { "Off", "Vaginal", "Anal", "Oral" }, c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanAutoTarget);
            autoTargeter.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanAutoTarget = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(autoTargeter);

            var maxPush = new CurrentStateCategorySlider("Max Push", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().MaxPush, 0f, 0.2f);
            maxPush.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.MaxPush = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(maxPush);

            var maxPull = new CurrentStateCategorySlider("Max Pull", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().MaxPull, 0f, 0.2f);
            maxPull.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.MaxPull = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(maxPull);

            var pullRate = new CurrentStateCategorySlider("Pull Rate", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().PullRate, 0f, 50f);
            pullRate.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.PullRate = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(pullRate);

            var returnRate = new CurrentStateCategorySlider("Return Rate", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().ReturnRate, 0f, 1f);
            returnRate.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.ReturnRate = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(returnRate);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
        public static void ChaControl_LoadCharaFbxDataAsync(ChaControl __instance)
        {
            var dynamicBones = __instance.GetComponentsInChildren<DynamicBone>(true);

            if (dynamicBones == null)
                return;

            foreach (var dynamicBone in dynamicBones)
            {
                if (dynamicBone == null ||
                    dynamicBone.m_Colliders == null ||
                    (dynamicBone.name != null && (dynamicBone.name.Contains("Vagina") || dynamicBone.name.Contains("cm_J_dan"))) ||
                    __instance != dynamicBone.GetComponentInParent<ChaControl>())
                    continue;

                for (int collider = 0; collider < dynamicBone.m_Colliders.Count; collider++)
                {
                    if (dynamicBone.m_Colliders[collider] != null &&
                        dynamicBone.m_Colliders[collider].name != null &&
                        (dynamicBone.m_Colliders[collider].name.Contains("Vagina") || dynamicBone.m_Colliders[collider].name.Contains("cm_J_dan")))
                        dynamicBone.m_Colliders.RemoveAt(collider);
                }
            }
        }

        internal static void BeforeCharacterReload()
        {
            var bpControllers = FindObjectsOfType<BetterPenetrationController>();
            if (bpControllers == null)
                return;

            foreach (var controller in bpControllers)
            { 
                if (controller == null)
                    continue;

                controller.ClearDanAgent();
            }
        }

        internal static void AfterTamaCharacterReload()
        {
            reloadConstraints = true;
        }

        internal static void AfterAddConstraint(bool enabled, Transform parentTransform, Transform childTransform,
            bool linkPosition, Vector3 positionOffset, bool linkRotation, Quaternion rotationOffset, bool linkScale,
            Vector3 scaleOffset, string alias)
        {
            if (childTransform.name != BoneNames.BPDanEntryTarget && childTransform.name != BoneNames.BPDanEndTarget)
                return;

            var controller = childTransform.GetComponentInParent<BetterPenetrationController>();
            if (controller == null)
                return;

            var constrainParams = new object[] { enabled, parentTransform.name, childTransform, linkPosition, positionOffset,
                                                 linkRotation, rotationOffset, linkScale, scaleOffset, alias};

            controller.SaveConstraintParams(childTransform.name == BoneNames.BPDanEntryTarget, constrainParams);

            if (childTransform.name != BoneNames.BPDanEntryTarget)
                return;

            var targetChaControl = parentTransform.GetComponentInParent<ChaControl>();
            if (targetChaControl == null)
                return;

            controller.SetCollisionAgent(targetChaControl, parentTransform.name == BoneNames.BPKokanTarget);
        }

        internal static void AfterApplyConstraints()
        {
            if (!reloadConstraints)
                return;

            resetDelay = 60;
            reloadConstraints = false;
        }

        internal static void AfterApplyNodesConstraints()
        {
            if (!reloadConstraints)
                return;

            resetDelay = 60;
            reloadConstraints = false;
        }

        internal static void ReinitializeControllers()
        {
            if (nodeConstraintPlugin == null)
                return;

            var bpControllers = FindObjectsOfType<BetterPenetrationController>();
            if (bpControllers == null)
                return;

            foreach (var controller in bpControllers)
            {
                if (controller == null)
                    continue;

                controller.InitializeDanAgent();
                controller.AddDanConstraints(nodeConstraintPlugin);
            }
        }

        internal void Update()
        {
            if (nodeConstraintPlugin == null)
                return;

            if (resetDelay > 0 && --resetDelay <= 0)
                ReinitializeControllers();

            if (++updateCount < 60)
                return;

            updateCount = 0;

            var bpControllers = FindObjectsOfType<BetterPenetrationController>();
            if (bpControllers == null)
                return;

            foreach (var controller in bpControllers)
            {
                if (controller == null)
                    continue;

                controller.CheckAutoTarget(nodeConstraintPlugin);
            }           
        }
    }
}
