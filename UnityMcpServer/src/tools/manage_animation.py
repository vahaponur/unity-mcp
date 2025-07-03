from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional, List
from unity_connection import get_unity_connection

def register_manage_animation_tools(mcp: FastMCP):
    """Register all animation management tools with the MCP server."""

    @mcp.tool()
    def manage_animation(
        ctx: Context,
        action: str,
        name: Optional[str] = None,
        path: Optional[str] = None,
        target: Optional[str] = None,
        frameRate: Optional[float] = None,
        duration: Optional[float] = None,
        loop: Optional[bool] = None,
        speed: Optional[float] = None,
        amplitude: Optional[float] = None,
        stepHeight: Optional[float] = None,
        swayAmount: Optional[float] = None,
        controllerPath: Optional[str] = None,
        clips: Optional[List[str]] = None,
        curves: Optional[List[Dict[str, Any]]] = None,
    ) -> Dict[str, Any]:
        """Manages Unity animations (create AnimationClips, modify, etc.).

        Args:
            action: Operation type ('create_clip', 'create_idle_animation', 'create_walk_animation', 
                   'add_animator', 'create_animator_controller').
            name: Name of the animation clip or controller.
            path: Asset path for saving (default: "Assets/Animations/").
            target: Target GameObject name for adding Animator component.
            frameRate: Frame rate for animation clip (default: 30).
            duration: Duration of the animation in seconds.
            loop: Whether the animation should loop (default: true).
            speed: Speed multiplier for preset animations (default: 1).
            amplitude: Amplitude for idle animation breathing effect (default: 0.05).
            stepHeight: Height of steps for walk animation (default: 0.1).
            swayAmount: Amount of body sway for walk animation (default: 5).
            controllerPath: Path to AnimatorController to assign.
            clips: List of AnimationClip paths for controller creation.
            curves: Custom curves data for create_clip action.

        Returns:
            Dictionary with results ('success', 'message', 'data').
            
        Examples:
            # Create idle animation
            manage_animation(action="create_idle_animation", name="PlayerIdle", target="Player")
            
            # Create walk animation
            manage_animation(action="create_walk_animation", name="PlayerWalk", speed=1.5)
            
            # Create custom animation clip
            manage_animation(
                action="create_clip",
                name="CustomAnimation",
                curves=[{
                    "targetPath": "",
                    "property": "localPosition.y",
                    "componentType": "Transform",
                    "keyframes": [
                        {"time": 0, "value": 0},
                        {"time": 0.5, "value": 1},
                        {"time": 1, "value": 0}
                    ]
                }]
            )
        """
        try:
            # Build parameters dictionary
            params = {
                "action": action
            }
            
            # Add optional parameters if provided
            if name is not None:
                params["name"] = name
            if path is not None:
                params["path"] = path
            if target is not None:
                params["target"] = target
            if frameRate is not None:
                params["frameRate"] = frameRate
            if duration is not None:
                params["duration"] = duration
            if loop is not None:
                params["loop"] = loop
            if speed is not None:
                params["speed"] = speed
            if amplitude is not None:
                params["amplitude"] = amplitude
            if stepHeight is not None:
                params["stepHeight"] = stepHeight
            if swayAmount is not None:
                params["swayAmount"] = swayAmount
            if controllerPath is not None:
                params["controllerPath"] = controllerPath
            if clips is not None:
                params["clips"] = clips
            if curves is not None:
                params["curves"] = curves
            
            # Send command to Unity
            response = get_unity_connection().send_command("manage_animation", params)

            # Process response
            if response.get("success"):
                return {
                    "success": True, 
                    "message": response.get("message", "Animation operation successful."), 
                    "data": response.get("data")
                }
            else:
                return {
                    "success": False, 
                    "message": response.get("error", "An unknown error occurred during animation management.")
                }

        except Exception as e:
            return {"success": False, "message": f"Python error managing animation: {str(e)}"}
