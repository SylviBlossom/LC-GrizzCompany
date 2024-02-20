using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GrizzCompany.Items
{
	public class ScrapCannonItem : GrabbableObject
	{
		public static bool enableDebugLines = false;
		public static int maxEntranceCount = 4;
		public static float movementHinderance = 0.8f; // 0.8f

		public static float arcHeight = 30f;
		public static float launchSpeed = 30f;
		public static float launchDistance = 75f;

		public static float landingRadius = 8f;
		public static float landingRadiusShip = 6f;
		public static float landingRadiusEntrance = 3f;

		public static float landingRaycastUp = 50f;
		public static float landingRaycastUpShip = 5f;
		public static float landingRaycastUpEntrance = 20f;
		public static float landingRaycastDown = 100f;

		public static float navMeshCastDistance = 10f;

		public static float targetAngleSnap = 17.5f;
		public static float targetAngleSnapMinDistance = 20f;

		public InteractTrigger cannonTrigger;
		public Transform shootPoint;
		public AudioClip loadSFX;
		public AudioClip shootSFX;
		public LineRenderer[] debugLines;

		private Random cannonRandom = new();
		private bool interactWaiting;

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			Plugin.Logger.LogInfo("Scrap Cannon - Network spawned");
			cannonRandom = new Random(Mathf.Min(StartOfRound.Instance.randomMapSeed + (int)NetworkObjectId, 99999999));
		}

		public override void GrabItem()
		{
			base.GrabItem();

			cannonTrigger.interactable = false;
			cannonTrigger.disabledHoverTip = "";

			if (playerHeldBy != null)
			{
				playerHeldBy.isMovementHindered++;
				playerHeldBy.hinderedMultiplier *= movementHinderance;
			}
		}

		public override void DiscardItem()
		{
			if (playerHeldBy != null)
			{
				playerHeldBy.isMovementHindered = Mathf.Clamp(playerHeldBy.isMovementHindered - 1, 0, 100);
				playerHeldBy.hinderedMultiplier = Mathf.Clamp(playerHeldBy.hinderedMultiplier / movementHinderance, 1f, 100f);
			}

			base.DiscardItem();
		}

		public override void Update()
		{
			base.Update();

			if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
			{
				return;
			}

			if (playerHeldBy != null)
			{
				if (enableDebugLines)
				{
					DrawDebugLines();
				}
				return;
			}

			foreach (var line in debugLines)
			{
				line.enabled = false;
			}

			if (isInShipRoom || isInFactory || StartOfRound.Instance.inShipPhase)
			{
				cannonTrigger.disabledHoverTip = "Cannot use indoors";
				cannonTrigger.interactable = false;
				return;
			}

			cannonTrigger.hoverTip = $"Launch item : [{(StartOfRound.Instance.localPlayerUsingController ? "R-trigger" : "LMB")}]";
			cannonTrigger.disabledHoverTip = "Hold item to launch";
			cannonTrigger.interactable = !interactWaiting && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null;
		}

		private void DrawDebugLines()
		{
			if (isInShipRoom || isInFactory || StartOfRound.Instance.inShipPhase)
			{
				foreach (var line in debugLines)
				{
					line.enabled = false;
				}
				return;
			}

			var ship = StartOfRound.Instance.elevatorTransform;
			var shipDoorPos = ship.Find("PositionNodes/DoorPos");

			DrawDebugLine(debugLines[0], shipDoorPos);

			var entranceTeleports = GetEntranceTeleports();

			for (var i = 0; i < entranceTeleports.Length; i++)
			{
				if (entranceTeleports[i] == null)
				{
					debugLines[i + 1].enabled = false;
					continue;
				}

				DrawDebugLine(debugLines[i + 1], entranceTeleports[i].entrancePoint);
			}
		}

		private void DrawDebugLine(LineRenderer debugLine, Transform targetPos)
		{
			var cannonPos2D = new Vector3(shootPoint.position.x, 0f, shootPoint.position.z);
			var targetPos2D = new Vector3(targetPos.position.x, 0f, targetPos.position.z);

			var targetDistance = Vector3.Distance(cannonPos2D, targetPos2D);

			if (targetDistance < targetAngleSnapMinDistance)
			{
				debugLine.enabled = false;
				return;
			}
			else
			{
				debugLine.enabled = true;
			}

			var cannonFacing2D = new Vector3(shootPoint.forward.x, 0f, shootPoint.forward.z).normalized;

			var targetAngleFull = Quaternion.FromToRotation(cannonFacing2D, targetPos2D - cannonPos2D);
			var targetAngle = targetAngleFull.eulerAngles.y;

			//Plugin.Logger.LogInfo($"TARGET ANGLE: {targetAngle}");

			debugLine.SetPosition(0, new Vector3(Mathf.Sin((targetAngle - targetAngleSnap) * Mathf.Deg2Rad) * 3f, 0, Mathf.Cos((targetAngle - targetAngleSnap) * Mathf.Deg2Rad) * 3f));
			debugLine.SetPosition(1, new Vector3(Mathf.Sin((targetAngle + targetAngleSnap) * Mathf.Deg2Rad) * 3f, 0, Mathf.Cos((targetAngle + targetAngleSnap) * Mathf.Deg2Rad) * 3f));
		}

		private EntranceTeleport[] GetEntranceTeleports()
		{
			var entranceTeleports = new EntranceTeleport[maxEntranceCount];

			foreach (var entranceTeleport in Object.FindObjectsOfType<EntranceTeleport>())
			{
				if (entranceTeleport.isEntranceToBuilding && entranceTeleport.entranceId < entranceTeleports.Length)
				{
					entranceTeleports[entranceTeleport.entranceId] = entranceTeleport;
				}
			}

			return entranceTeleports;
		}

		public void CannonInteract(PlayerControllerB player)
		{
			if (interactWaiting || player.currentlyHeldObjectServer == null)
			{
				return;
			}

			var ship = StartOfRound.Instance.elevatorTransform;
			var shipDoorPos = ship.Find("PositionNodes/DoorPos");

			if (CanSnap(shipDoorPos.position, out var shipAccuracy, "Ship"))
			{
				ShootItemToShip(player, shipDoorPos, shipAccuracy);
				return;
			}

			var entranceTeleports = GetEntranceTeleports();

			for (var i = 0; i < entranceTeleports.Length; i++)
			{
				var entrance = entranceTeleports[i];

				if (entrance != null && CanSnap(entrance.entrancePoint.position, out _, $"Entrance {entrance.entranceId}"))
				{
					ShootItemToEntrance(player, entrance);
					return;
				}
			}

			ShootItemForwards(player, shipDoorPos);
		}

		private bool CanSnap(Vector3 targetPos, out float accuracy, string name = null)
		{
			accuracy = 0f;

			var cannonPos2D = new Vector3(shootPoint.position.x, 0f, shootPoint.position.z);
			var targetPos2D = new Vector3(targetPos.x, 0f, targetPos.z);

			var targetDistance = Vector3.Distance(cannonPos2D, targetPos2D);

			if (targetDistance < targetAngleSnapMinDistance)
			{
				return false;
			}

			var cannonFacing2D = new Vector3(shootPoint.forward.x, 0f, shootPoint.forward.z).normalized;

			var targetAngleFull = Quaternion.FromToRotation(cannonFacing2D, targetPos2D - cannonPos2D);
			var targetAngle = targetAngleFull.eulerAngles.y;

			if (name != null)
			{
				Plugin.Logger.LogInfo($"Scrap Cannon - Angle to {name}: {Mathf.DeltaAngle(0f, targetAngle)}");
			}

			var angle = Mathf.DeltaAngle(0f, targetAngle);

			accuracy = angle / targetAngleSnap;
			return Mathf.Abs(angle) < targetAngleSnap;
		}

		private void ShootItemToEntrance(PlayerControllerB player, EntranceTeleport entrance)
		{
			Plugin.Logger.LogInfo($"Scrap Cannon - Shooting to entrace {entrance.entranceId}");

			var heldObject = player.currentlyHeldObjectServer;

			var doorPos2D = new Vector3(entrance.transform.position.x, 0f, entrance.transform.position.z);
			var telePos2D = new Vector3(entrance.entrancePoint.position.x, 0f, entrance.entrancePoint.position.z);

			var doorDirection = (telePos2D - doorPos2D).normalized;

			var randOffset = (float)(cannonRandom.NextDouble() * landingRadiusEntrance);
			var randAngle = (float)(cannonRandom.NextDouble() * (360f * Mathf.Deg2Rad));

			var landingPos2D = telePos2D + (doorDirection * landingRadiusEntrance) + (new Vector3(Mathf.Sin(randAngle), 0f, Mathf.Cos(randAngle)) * randOffset);
			var landingPos = landingPos2D + (entrance.entrancePoint.position.y * Vector3.up);

			if (NavMesh.SamplePosition(landingPos, out var navMeshHit, navMeshCastDistance, NavMesh.AllAreas))
			{
				// Raycast again incase any mods hook GetItemFloorPosition
				var landingPosition = heldObject.GetItemFloorPosition(navMeshHit.position + Vector3.up);

				ShootItem(player, landingPosition);
				return;
			}

			var landingPosRay = landingPos + (landingRaycastUpEntrance * Vector3.up);

			if (Physics.Raycast(landingPosRay, -Vector3.up, out var raycastHit, landingRaycastUpEntrance + landingRaycastDown, 0b10000000000000000100100000001, QueryTriggerInteraction.Ignore))
			{
				// Raycast again incase any mods hook GetItemFloorPosition
				var landingPosition = heldObject.GetItemFloorPosition(raycastHit.point + Vector3.up);

				ShootItem(player, landingPosition);
			}
			else
			{
				ShootItem(player, landingPos);
			}
		}

		private void ShootItemToShip(PlayerControllerB player, Transform shipDoorPos, float accuracy)
		{
			Plugin.Logger.LogInfo($"Scrap Cannon - Shooting to ship");

			var heldObject = player.currentlyHeldObjectServer;

			var targetPos = shipDoorPos.transform.position
				+ (-shipDoorPos.right * (1 + landingRadiusShip))// - (accuracy * (landingRadiusShip / 2f)))) 
				- (shipDoorPos.transform.localPosition.y * Vector3.up)
				+ (-shipDoorPos.forward * accuracy * 5f);

			var targetPos2D = new Vector3(targetPos.x, 0f, targetPos.z);

			var randOffset = (float)(cannonRandom.NextDouble() * landingRadiusShip);
			var randAngle = (float)(cannonRandom.NextDouble() * (360f * Mathf.Deg2Rad));

			var landingPos2D = targetPos2D + new Vector3(Mathf.Sin(randAngle), 0f, Mathf.Cos(randAngle)) * randOffset;
			var landingPositionRay = landingPos2D + (targetPos.y + landingRaycastUpShip) * Vector3.up;

			if (Physics.Raycast(landingPositionRay, -Vector3.up, out var raycastHit, landingRaycastUpShip + landingRaycastDown, 0b10000000000000000100100000001, QueryTriggerInteraction.Ignore))
			{
				// Raycast again incase any mods hook GetItemFloorPosition
				var landingPosition = heldObject.GetItemFloorPosition(raycastHit.point + Vector3.up);

				ShootItem(player, landingPosition);
			}
			else
			{
				var landingPosition = landingPos2D + (targetPos.y * Vector3.up);

				ShootItem(player, landingPosition);
			}
		}

		private void ShootItemForwards(PlayerControllerB player, Transform shipDoorPos = null)
		{
			Plugin.Logger.LogInfo($"Scrap Cannon - Shooting forwards (untargeted)");

			var heldObject = player.currentlyHeldObjectServer;

			var cannonPos2D = new Vector3(shootPoint.position.x, 0f, shootPoint.position.z);
			var cannonFacing2D = new Vector3(shootPoint.forward.x, 0f, shootPoint.forward.z).normalized;

			var randOffset = (float)(cannonRandom.NextDouble() * landingRadius);
			var randAngle = (float)(cannonRandom.NextDouble() * (360f * Mathf.Deg2Rad));

			var distance = launchDistance;

			if (shipDoorPos != null)
			{
				var shipPos2D = new Vector3(shipDoorPos.position.x, 0f, shipDoorPos.position.y);

				var targetAngleFull = Quaternion.FromToRotation(cannonFacing2D, shipPos2D - cannonPos2D);
				var targetAngle = targetAngleFull.eulerAngles.y;

				if (Mathf.DeltaAngle(0f, targetAngle) < 90f)
				{
					distance = Mathf.Max(distance, Vector3.Distance(shipPos2D, cannonPos2D));
				}
			}

			var landingPos2D = cannonPos2D + (cannonFacing2D * distance) + new Vector3(Mathf.Sin(randAngle), 0f, Mathf.Cos(randAngle)) * randOffset;
			var landingPositionRay = landingPos2D + (transform.position.y + landingRaycastUp) * Vector3.up;

			if (Physics.Raycast(landingPositionRay, -Vector3.up, out var raycastHit, landingRaycastUp + landingRaycastDown, 0b10000000000000000100100000001, QueryTriggerInteraction.Ignore))
			{
				// Raycast again incase any mods hook GetItemFloorPosition
				var landingPosition = heldObject.GetItemFloorPosition(raycastHit.point + Vector3.up);

				ShootItem(player, landingPosition);
			}
			else
			{
				var landingPosition = landingPos2D + (transform.position.y * Vector3.up);

				ShootItem(player, landingPosition);
			}
		}

		public void ShootItem(PlayerControllerB player, Vector3 landingPosition)
		{
			var heldObject = player.currentlyHeldObjectServer;

			player.SetSpecialGrabAnimationBool(false, heldObject);
			player.playerBodyAnimator.SetBool("cancelHolding", true);
			player.playerBodyAnimator.SetTrigger("Throw");
			HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
			HUDManager.Instance.holdingTwoHandedItem.enabled = false;

			var localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);

			player.PlaceGrabbableObject(StartOfRound.Instance.propsContainer, localPos, false, heldObject);
			heldObject.DiscardItemOnClient();

			heldObject.fallTime = 1.1f;
			heldObject.hasHitGround = true;
			heldObject.EnablePhysics(false);
			heldObject.EnableItemMeshes(false);

			cannonTrigger.interactable = false;
			interactWaiting = true;

			if (loadSFX != null)
			{
				gameObject.GetComponent<AudioSource>().PlayOneShot(loadSFX);
			}

			ShootItemServerRPC((int)player.playerClientId, heldObject.gameObject.GetComponent<NetworkObject>(), landingPosition);
		}

		[ServerRpc(RequireOwnership = false)]
		public void ShootItemServerRPC(int playerId, NetworkObjectReference heldObjectRef, Vector3 landingPosition)
		{
			if (!heldObjectRef.TryGet(out _))
			{
				Plugin.Logger.LogError($"Scrap Cannon - ShootItemServerRPC - Held object could not be found: {heldObjectRef.NetworkObjectId}");
				return;
			}

			ShootItemClientRPC(playerId, heldObjectRef, landingPosition);
		}

		[ClientRpc]
		public void ShootItemClientRPC(int playerId, NetworkObjectReference heldObjectRef, Vector3 landingPosition)
		{
			var player = StartOfRound.Instance.allPlayerScripts[playerId];

			if (player == null)
			{
				Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Player could not be found: {playerId}");
				return;
			}

			if (!heldObjectRef.TryGet(out var heldObjectNetwork))
			{
				Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Held object could not be found: {heldObjectRef.NetworkObjectId}");
				return;
			}

			var heldObject = heldObjectNetwork.GetComponent<GrabbableObject>();

			if (!player.IsOwner)
			{
				var localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);

				player.PlaceGrabbableObject(StartOfRound.Instance.propsContainer, localPos, false, heldObject);

				heldObject.fallTime = 1.1f;
				heldObject.hasHitGround = true;
				heldObject.EnablePhysics(false);
				heldObject.EnableItemMeshes(false);

				if (loadSFX != null)
				{
					gameObject.GetComponent<AudioSource>().PlayOneShot(loadSFX);
				}

				// Don't shoot to same position on every client
				cannonRandom.NextDouble();
				cannonRandom.NextDouble();
			}

			if (!heldObject.itemProperties.syncDiscardFunction)
			{
				heldObject.playerHeldBy = null;
			}

			if (player.currentlyHeldObjectServer == heldObject)
			{
				player.currentlyHeldObjectServer = null;
			}
			else
			{
				Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Held object mismatch (found {player.currentlyHeldObjectServer?.gameObject.name ?? "null"}) on player {playerId}");
			}

			if (player.IsOwner)
			{
				player.throwingObject = false;
				HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
			}

			interactWaiting = false;

			StartCoroutine(ShootItemRoutine(heldObject, landingPosition));
		}

		private IEnumerator ShootItemRoutine(GrabbableObject heldObject, Vector3 landingPosition)
		{
			Plugin.Logger.LogInfo($"Scrap Cannon - Scrap landing at {landingPosition}");

			yield return new WaitForSeconds(0.5f);

			if (shootSFX != null)
			{
				gameObject.GetComponent<AudioSource>().PlayOneShot(shootSFX);
			}

			heldObject.EnableItemMeshes(true);

			var launchTotalDistance = Vector3.Distance(shootPoint.position, landingPosition);
			var launchProgress = 0f;

			while (launchProgress < launchTotalDistance)
			{
				var currentPosition = SampleParabola(shootPoint.position, landingPosition, arcHeight, launchProgress / launchTotalDistance);
				var localPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(currentPosition);

				heldObject.startFallingPosition = localPosition;
				heldObject.targetFloorPosition = localPosition;
				heldObject.transform.localPosition = localPosition;

				launchProgress += launchSpeed * Time.deltaTime;

				yield return null;
			}

			var finalLocalPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);

			heldObject.EnablePhysics(true);
			heldObject.startFallingPosition = finalLocalPosition;
			heldObject.targetFloorPosition = finalLocalPosition;
			heldObject.transform.localPosition = finalLocalPosition;
			heldObject.hasHitGround = false;
			heldObject.fallTime = 0f;
		}

		// https://forum.unity.com/threads/generating-dynamic-parabola.211681/#post-1426169
		private Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
		{
			float parabolicT = t * 2 - 1;
			if (Mathf.Abs(start.y - end.y) < 0.1f)
			{
				//start and end are roughly level, pretend they are - simpler solution with less steps
				Vector3 travelDirection = end - start;
				Vector3 result = start + t * travelDirection;
				result.y += (-parabolicT * parabolicT + 1) * height;
				return result;
			}
			else
			{
				//start and end are not level, gets more complicated
				Vector3 travelDirection = end - start;
				Vector3 levelDirecteion = end - new Vector3(start.x, end.y, start.z);
				Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
				Vector3 up = Vector3.Cross(right, travelDirection);
				if (end.y > start.y) up = -up;
				Vector3 result = start + t * travelDirection;
				result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
				return result;
			}
		}
	}
}
