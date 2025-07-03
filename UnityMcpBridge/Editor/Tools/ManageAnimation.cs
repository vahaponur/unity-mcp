using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageAnimation
    {
        /// <summary>
        /// Manages Unity animations (create AnimationClips, modify, etc.)
        /// </summary>
        public static object HandleCommand(JObject @params)
        {
            try
            {
                string action = @params["action"]?.ToString()?.ToLower();
                
                if (string.IsNullOrEmpty(action))
                {
                    return Response.Error("Action parameter is required.");
                }

                switch (action)
                {
                    case "create_clip":
                        return CreateAnimationClip(@params);
                    case "create_idle_animation":
                        return CreateIdleAnimation(@params);
                    case "create_walk_animation":
                        return CreateWalkAnimation(@params);
                    case "add_animator":
                        return AddAnimatorToGameObject(@params);
                    case "create_animator_controller":
                        return CreateAnimatorController(@params);
                    default:
                        return Response.Error($"Unknown action: {action}");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error in ManageAnimation: {e.Message}");
            }
        }

        /// <summary>
        /// Creates a custom AnimationClip with specified curves
        /// </summary>
        private static object CreateAnimationClip(JObject @params)
        {
            string clipName = @params["name"]?.ToString();
            if (string.IsNullOrEmpty(clipName))
            {
                return Response.Error("Animation clip name is required.");
            }

            string savePath = @params["path"]?.ToString() ?? "Assets/Animations/";
            float frameRate = @params["frameRate"]?.ToObject<float>() ?? 30f;
            float duration = @params["duration"]?.ToObject<float>() ?? 1f;
            bool loop = @params["loop"]?.ToObject<bool>() ?? true;

            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(savePath.TrimEnd('/')))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                // Create new AnimationClip
                AnimationClip clip = new AnimationClip();
                clip.frameRate = frameRate;

                // Set loop settings
                AnimationClipSettings settings = new AnimationClipSettings
                {
                    loopTime = loop,
                    loopBlend = loop
                };
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Add curves from parameters
                if (@params["curves"] is JArray curves)
                {
                    foreach (JObject curveData in curves)
                    {
                        string targetPath = curveData["targetPath"]?.ToString() ?? "";
                        string property = curveData["property"]?.ToString();
                        string componentType = curveData["componentType"]?.ToString() ?? "Transform";
                        
                        if (string.IsNullOrEmpty(property))
                        {
                            continue;
                        }

                        // Create AnimationCurve
                        AnimationCurve curve = new AnimationCurve();
                        
                        // Add keyframes
                        if (curveData["keyframes"] is JArray keyframes)
                        {
                            foreach (JObject kf in keyframes)
                            {
                                float time = kf["time"]?.ToObject<float>() ?? 0f;
                                float value = kf["value"]?.ToObject<float>() ?? 0f;
                                
                                // Add keyframe first
                                int keyIndex = curve.AddKey(time, value);
                                
                                // Set tangent modes if specified
                                string tangentMode = kf["tangentMode"]?.ToString()?.ToLower();
                                if (tangentMode == "smooth" || tangentMode == "auto")
                                {
                                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Auto);
                                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Auto);
                                }
                                else
                                {
                                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                                }
                            }
                        }

                        // Create binding based on component type
                        Type compType = GetComponentType(componentType);
                        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                            targetPath,
                            compType,
                            property
                        );
                        
                        // Set the curve
                        AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }
                }

                // Save as asset
                string fullPath = savePath + clipName + ".anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success(
                    $"AnimationClip '{clipName}' created successfully.",
                    new { path = fullPath, duration = duration, frameRate = frameRate }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create animation clip: {e.Message}");
            }
        }

        /// <summary>
        /// Creates a simple idle animation (breathing effect)
        /// </summary>
        private static object CreateIdleAnimation(JObject @params)
        {
            string targetGameObject = @params["target"]?.ToString();
            string clipName = @params["name"]?.ToString() ?? "IdleAnimation";
            string savePath = @params["path"]?.ToString() ?? "Assets/Animations/";
            float speed = @params["speed"]?.ToObject<float>() ?? 1f;
            float amplitude = @params["amplitude"]?.ToObject<float>() ?? 0.05f;

            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(savePath.TrimEnd('/')))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                // Create AnimationClip
                AnimationClip clip = new AnimationClip();
                clip.frameRate = 30f;

                // Set loop
                AnimationClipSettings settings = new AnimationClipSettings
                {
                    loopTime = true,
                    loopBlend = true
                };
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Create breathing effect on Y scale
                AnimationCurve scaleYCurve = new AnimationCurve();
                float duration = 2f / speed;
                
                // Keyframes for smooth breathing
                scaleYCurve.AddKey(0f, 1f);
                scaleYCurve.AddKey(duration * 0.25f, 1f + amplitude);
                scaleYCurve.AddKey(duration * 0.5f, 1f);
                scaleYCurve.AddKey(duration * 0.75f, 1f - amplitude * 0.5f);
                scaleYCurve.AddKey(duration, 1f);

                // Smooth the curve
                for (int i = 0; i < scaleYCurve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(scaleYCurve, i, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(scaleYCurve, i, AnimationUtility.TangentMode.Auto);
                }

                // Add slight rotation for more natural idle
                AnimationCurve rotationZCurve = new AnimationCurve();
                rotationZCurve.AddKey(0f, 0f);
                rotationZCurve.AddKey(duration * 0.33f, 2f);
                rotationZCurve.AddKey(duration * 0.66f, -2f);
                rotationZCurve.AddKey(duration, 0f);

                // Smooth rotation curve
                for (int i = 0; i < rotationZCurve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(rotationZCurve, i, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(rotationZCurve, i, AnimationUtility.TangentMode.Auto);
                }

                // Create bindings
                EditorCurveBinding scaleYBinding = EditorCurveBinding.FloatCurve(
                    "",
                    typeof(Transform),
                    "localScale.y"
                );
                
                EditorCurveBinding rotationZBinding = EditorCurveBinding.FloatCurve(
                    "",
                    typeof(Transform),
                    "localEulerAngles.z"
                );

                // Set curves
                AnimationUtility.SetEditorCurve(clip, scaleYBinding, scaleYCurve);
                AnimationUtility.SetEditorCurve(clip, rotationZBinding, rotationZCurve);

                // Save animation
                string fullPath = savePath + clipName + ".anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // If target GameObject specified, add Animator component
                if (!string.IsNullOrEmpty(targetGameObject))
                {
                    GameObject target = GameObject.Find(targetGameObject);
                    if (target != null)
                    {
                        Animator animator = target.GetComponent<Animator>();
                        if (animator == null)
                        {
                            animator = target.AddComponent<Animator>();
                        }
                        
                        // Create simple AnimatorController if needed
                        string controllerPath = savePath + clipName + "_Controller.controller";
                        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(
                            controllerPath,
                            clip
                        );
                        animator.runtimeAnimatorController = controller;
                    }
                }

                return Response.Success(
                    $"Idle animation '{clipName}' created successfully.",
                    new { path = fullPath, duration = duration, amplitude = amplitude }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create idle animation: {e.Message}");
            }
        }

        /// <summary>
        /// Creates a simple walk animation
        /// </summary>
        private static object CreateWalkAnimation(JObject @params)
        {
            string clipName = @params["name"]?.ToString() ?? "WalkAnimation";
            string savePath = @params["path"]?.ToString() ?? "Assets/Animations/";
            float speed = @params["speed"]?.ToObject<float>() ?? 1f;
            float stepHeight = @params["stepHeight"]?.ToObject<float>() ?? 0.1f;
            float swayAmount = @params["swayAmount"]?.ToObject<float>() ?? 5f;

            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(savePath.TrimEnd('/')))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                // Create AnimationClip
                AnimationClip clip = new AnimationClip();
                clip.frameRate = 30f;

                // Set loop
                AnimationClipSettings settings = new AnimationClipSettings
                {
                    loopTime = true,
                    loopBlend = true
                };
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                float duration = 1f / speed;

                // Y position curve (bobbing)
                AnimationCurve posYCurve = new AnimationCurve();
                posYCurve.AddKey(0f, 0f);
                posYCurve.AddKey(duration * 0.25f, stepHeight);
                posYCurve.AddKey(duration * 0.5f, 0f);
                posYCurve.AddKey(duration * 0.75f, stepHeight);
                posYCurve.AddKey(duration, 0f);

                // Z rotation curve (body sway)
                AnimationCurve rotZCurve = new AnimationCurve();
                rotZCurve.AddKey(0f, 0f);
                rotZCurve.AddKey(duration * 0.25f, -swayAmount);
                rotZCurve.AddKey(duration * 0.5f, 0f);
                rotZCurve.AddKey(duration * 0.75f, swayAmount);
                rotZCurve.AddKey(duration, 0f);

                // Smooth all curves
                SmoothCurve(posYCurve);
                SmoothCurve(rotZCurve);

                // Create bindings
                EditorCurveBinding posYBinding = EditorCurveBinding.FloatCurve(
                    "",
                    typeof(Transform),
                    "localPosition.y"
                );
                
                EditorCurveBinding rotZBinding = EditorCurveBinding.FloatCurve(
                    "",
                    typeof(Transform),
                    "localEulerAngles.z"
                );

                // Set curves
                AnimationUtility.SetEditorCurve(clip, posYBinding, posYCurve);
                AnimationUtility.SetEditorCurve(clip, rotZBinding, rotZCurve);

                // Save animation
                string fullPath = savePath + clipName + ".anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success(
                    $"Walk animation '{clipName}' created successfully.",
                    new { path = fullPath, duration = duration, stepHeight = stepHeight, swayAmount = swayAmount }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create walk animation: {e.Message}");
            }
        }

        /// <summary>
        /// Adds an Animator component to a GameObject
        /// </summary>
        private static object AddAnimatorToGameObject(JObject @params)
        {
            string targetName = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(targetName))
            {
                return Response.Error("Target GameObject name is required.");
            }

            try
            {
                GameObject target = GameObject.Find(targetName);
                if (target == null)
                {
                    return Response.Error($"GameObject '{targetName}' not found.");
                }

                Animator animator = target.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<Animator>(target);
                }

                // Set controller if specified
                string controllerPath = @params["controllerPath"]?.ToString();
                if (!string.IsNullOrEmpty(controllerPath))
                {
                    var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                }

                return Response.Success(
                    $"Animator added to '{targetName}'.",
                    new { hasController = animator.runtimeAnimatorController != null }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add Animator: {e.Message}");
            }
        }

        /// <summary>
        /// Creates an AnimatorController asset
        /// </summary>
        private static object CreateAnimatorController(JObject @params)
        {
            string controllerName = @params["name"]?.ToString();
            if (string.IsNullOrEmpty(controllerName))
            {
                return Response.Error("Controller name is required.");
            }

            string savePath = @params["path"]?.ToString() ?? "Assets/Animations/";
            
            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(savePath.TrimEnd('/')))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                string fullPath = savePath + controllerName + ".controller";
                
                // Get animation clips if specified
                JArray clipPaths = @params["clips"] as JArray;
                AnimationClip defaultClip = null;
                
                if (clipPaths != null && clipPaths.Count > 0)
                {
                    string firstClipPath = clipPaths[0].ToString();
                    defaultClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(firstClipPath);
                }

                // Create controller
                UnityEditor.Animations.AnimatorController controller;
                
                if (defaultClip != null)
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(
                        fullPath,
                        defaultClip
                    );
                }
                else
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(fullPath);
                }

                // Add additional clips as states
                if (clipPaths != null && clipPaths.Count > 1)
                {
                    var rootStateMachine = controller.layers[0].stateMachine;
                    
                    for (int i = 1; i < clipPaths.Count; i++)
                    {
                        string clipPath = clipPaths[i].ToString();
                        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                        
                        if (clip != null)
                        {
                            var state = rootStateMachine.AddState(clip.name);
                            state.motion = clip;
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success(
                    $"AnimatorController '{controllerName}' created successfully.",
                    new { path = fullPath, clipCount = clipPaths?.Count ?? 0 }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create AnimatorController: {e.Message}");
            }
        }

        /// <summary>
        /// Helper to smooth animation curves
        /// </summary>
        private static void SmoothCurve(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
        }

        /// <summary>
        /// Helper to get component type from string
        /// </summary>
        private static Type GetComponentType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "transform":
                    return typeof(Transform);
                case "meshrenderer":
                    return typeof(MeshRenderer);
                case "light":
                    return typeof(Light);
                case "camera":
                    return typeof(Camera);
                default:
                    // Try to find the type
                    Type type = Type.GetType(typeName);
                    if (type == null)
                    {
                        type = Type.GetType("UnityEngine." + typeName + ", UnityEngine");
                    }
                    return type ?? typeof(Transform);
            }
        }
    }
}
