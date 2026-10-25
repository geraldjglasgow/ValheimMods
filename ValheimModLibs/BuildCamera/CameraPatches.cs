using System;
using HarmonyLib;
using UnityEngine;

namespace BuildCamera;

/// <summary>
/// The view and the body, by name. <c>GameCamera.GetCameraPosition</c> (private, called from its LateUpdate for a living
/// player without a seat camera): one frame of flying, and the camera's pose instead of the third-person one, with the
/// near clip plane the game uses close to walls. <c>GameCamera.UpdateListner</c> (private): the sound listener stays on
/// the camera (the game puts it on the player's eyes), so you hear what you see. <c>LightLod.GetLightReferencePoint</c>
/// (private static): lights fade by their distance to the camera, as in the game's own free fly. <c>Player.SetMouseLook</c>:
/// the game's look input (fed by <c>PlayerController.LateUpdate</c> with sensitivity, inversion and gamepad handling
/// applied) turns the camera, not the body. <c>Player.SetControls</c>: the body is held at rest (no walking, jumping,
/// crouching, running, autorun, dodging, blocking or attacking; the game's place mode reads its build keys itself, so
/// building still works; a seated player stays seated). <c>Player.Update</c> (postfix): every way a trip ends by itself.
/// </summary>
internal static class CameraPatches
{
	public static void Install(Harmony harmony)
	{
		Patch(harmony, typeof(GameCamera), "GetCameraPosition", nameof(View), false);
		Patch(harmony, typeof(GameCamera), "UpdateListner", nameof(Listener), true);
		Patch(harmony, typeof(LightLod), "GetLightReferencePoint", nameof(LightPoint), false);
		Patch(harmony, typeof(Player), "SetMouseLook", nameof(Look), false);
		Patch(harmony, typeof(Player), "SetControls", nameof(AtRest), false);
		Patch(harmony, typeof(Player), "Update", nameof(AfterUpdate), true);
	}

	private static void Patch(Harmony harmony, Type type, string method, string patch, bool postfix)
	{
		HarmonyMethod ours = new(typeof(CameraPatches), patch);
		harmony.Patch(AccessTools.Method(type, method), prefix: postfix ? null : ours, postfix: postfix ? ours : null);
	}

	private static bool View(GameCamera __instance, float dt, ref Vector3 pos, ref Quaternion rot)
	{
		CameraFlight? flight = CameraTrip.Current;
		if (flight == null)
		{
			return true;
		}
		CameraMotion.Step(Player.m_localPlayer, flight, dt);
		pos = CameraPose.Position;
		rot = CameraPose.Rotation;
		Camera? view = GameAccess.View(__instance);
		if (view != null)
		{
			view.nearClipPlane = __instance.m_nearClipPlaneMin;
		}
		return false;
	}

	private static void Listener(GameCamera __instance)
	{
		AudioListener? listener = CameraTrip.Current != null ? GameAccess.Listener(__instance) : null;
		if (listener != null)
		{
			listener.transform.localPosition = Vector3.zero;
		}
	}

	private static bool LightPoint(ref Vector3 __result)
	{
		if (CameraTrip.Current == null)
		{
			return true;
		}
		__result = CameraPose.Position;
		return false;
	}

	private static void Look(Player __instance, ref Vector2 mouseLook)
	{
		if (CameraTrip.IsOutFor(__instance))
		{
			CameraPose.Look(mouseLook);
			mouseLook = Vector2.zero;
		}
	}

	private static void AtRest(Player __instance, ref Vector3 movedir, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
		ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump, ref bool crouch, ref bool run, ref bool autoRun, ref bool dodge)
	{
		if (!CameraTrip.IsOutFor(__instance))
		{
			return;
		}
		movedir = Vector3.zero;
		attack = attackHold = secondaryAttack = secondaryAttackHold = false;
		block = blockHold = jump = crouch = run = autoRun = dodge = false;
	}

	private static void AfterUpdate(Player __instance)
	{
		if (__instance == Player.m_localPlayer)
		{
			CameraTrip.Tick(__instance);
		}
	}
}
