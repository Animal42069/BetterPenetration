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
    [BepInDependency("com.joan6694.illusionplugins.bonesframework", "1.4.3")]
    [BepInDependency("com.joan6694.illusionplugins.nodesconstraints")]
    [BepInProcess("StudioNEOV2")]
    public class AI_Studio_BetterPenetration : BaseUnityPlugin
    {
        internal const string GUID = "com.animal42069.studiobetterpenetration";
        internal const string PluginName = "AI Studio Better Penetration";
        internal const string VERSION = "2.0.1.0";
        internal const string BEHAVIOR = "BetterPenetrationController";
        internal const string StudioCategoryName = "Better Penetration";
        internal static Harmony harmony;
        internal static BaseUnityPlugin nodeConstraintPlugin;

        internal void Main()
        {
            CharacterApi.RegisterExtraBehaviour<BetterPenetrationController>(BEHAVIOR);

            harmony = new Harmony("AI_Studio_BetterPenetration");
            harmony.PatchAll(typeof(AI_Studio_BetterPenetration));

            Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo pluginInfo);
            if (pluginInfo == null || pluginInfo.Instance == null)
                return;

            Type nestedType = pluginInfo.Instance.GetType().GetNestedType("UncensorSelectorController", AccessTools.all);
            if (nestedType == null)
                return;

            MethodInfo methodInfo = AccessTools.Method(nestedType, "ReloadCharacterPenis", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, prefix: new HarmonyMethod(typeof(AI_Studio_BetterPenetration), "BeforeDanCharacterReload"),
                                      postfix: new HarmonyMethod(typeof(AI_Studio_BetterPenetration), "AfterDanCharacterReload"));
            Debug.Log("Studio_BetterPenetration: patched UncensorSelector::ReloadCharacterPenis correctly");

            methodInfo = AccessTools.Method(nestedType, "ReloadCharacterBalls", null, null);
            if (methodInfo == null)
                return;

            harmony.Patch(methodInfo, prefix: new HarmonyMethod(typeof(AI_Studio_BetterPenetration), "BeforeTamaCharacterReload"),
                                      postfix: new HarmonyMethod(typeof(AI_Studio_BetterPenetration), "AfterTamaCharacterReload"));
            Debug.Log("Studio_BetterPenetration: patched UncensorSelectorController::ReloadCharacterBalls correctly");

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

            harmony.Patch(methodInfo, postfix: new HarmonyMethod(typeof(AI_Studio_BetterPenetration), "AfterAddConstraint"));
            Debug.Log("Studio_BetterPenetration: patched NodeConstraints::AddConstraint correctly");
        }

        public static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio)
                return;

            var bpEnable = new CurrentStateCategorySwitch("Enable BP Controller", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().enabled);
            bpEnable.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.enabled = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(bpEnable);

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

            var colliderRadius = new CurrentStateCategorySlider("Collilder Radius", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanColliderRadius, 0f, 1f);
            colliderRadius.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanColliderRadius = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(colliderRadius);

            var colliderLength = new CurrentStateCategorySlider("Collilder Length", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanColliderLength, 0f, 1f);
            colliderLength.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanColliderLength = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(colliderLength);

            var colliderVertical = new CurrentStateCategorySlider("Collilder Vertical", c => StudioAPI.GetSelectedControllers<BetterPenetrationController>().First().DanColliderVertical, -0.1f, 0.1f);
            colliderVertical.Value.Subscribe(value =>
            {
                foreach (var controller in StudioAPI.GetSelectedControllers<BetterPenetrationController>())
                    controller.DanColliderVertical = value;
            });
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(colliderVertical);
        }

        internal static void BeforeDanCharacterReload()
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

        internal static void AfterDanCharacterReload()
        {
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

        internal static void BeforeTamaCharacterReload()
        {
            var bpControllers = FindObjectsOfType<BetterPenetrationController>();
            if (bpControllers == null)
                return;

            foreach (var controller in bpControllers)
            {
                if (controller == null)
                    continue;

                controller.ClearTama();
            }
        }

        internal static void AfterTamaCharacterReload()
        {
            var bpControllers = FindObjectsOfType<BetterPenetrationController>();
            if (bpControllers == null)
                return;

            foreach (var controller in bpControllers)
            {
                if (controller == null)
                    continue;

                controller.InitializeTama();
            }
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
    }
}
