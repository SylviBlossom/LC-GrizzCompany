using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GrizzCompany
{
	public class GrillerAI : EnemyAI
	{
		public const int WANDERING = 0;
		public const int CHASING = 1;

		public static float TargetPrecision = 0.025f;

		public AISearchRoutine searchForPlayer;
		public LineRenderer laserLine;
		public Transform laserPos;
		public Transform turnBody;

		private Random enemyRandom;
		private GameObject[] grillerNodes;
		private bool followingPath;

		public override void Start()
		{
			base.Start();
			laserLine.gameObject.SetActive(false);

			enemyRandom = new Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);

			grillerNodes = Object.FindObjectsOfType<GrillerAINode>().Select(node => node.gameObject).ToArray();

			Plugin.Logger.LogInfo($"FOUND {grillerNodes.Length} GRILLER AI NODES");
		}

		public override void DoAIInterval()
		{
			base.DoAIInterval();

			if (isEnemyDead)
			{
				return;
			}

			switch (currentBehaviourStateIndex)
			{
				case WANDERING:
					targetPlayer = null;
					movingTowardsTargetPlayer = false;

					if (!searchForPlayer.inProgress)
					{
						StartSearch(transform.position, searchForPlayer);
					}

					var foundPlayer = CheckLineOfSightForPlayer(65f);

					if (foundPlayer != null)
					{
						// TODO: Multiplayer
						GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);

						StopSearch(searchForPlayer);

						SetMovingTowardsTargetPlayer(foundPlayer);
						SwitchToBehaviourState(CHASING);

						ChangeOwnershipOfEnemy(foundPlayer.actualClientId);
					}

					break;

				case CHASING:
					if (searchForPlayer.inProgress)
					{
						StopSearch(searchForPlayer);
					}
					break;
			}
		}

		public override void Update()
		{
			base.Update();

			if (targetPlayer != null && !PlayerIsTargetable(targetPlayer))
			{
				targetPlayer = null;

				if (IsOwner)
				{
					SwitchToBehaviourState(WANDERING);
				}
			}

			var laserActive = targetPlayer != null;

			if (laserLine.gameObject.activeSelf != laserActive)
			{
				laserLine.gameObject.SetActive(false);
			}

			if (laserActive)
			{
				laserLine.SetPosition(0, laserPos.position);
				laserLine.SetPosition(1, targetPlayer.transform.position + (Vector3.up * 0.5f));
			}
		}

		public void SelectRandomNode()
		{
			var unselectedNodes = new List<GameObject>(grillerNodes.Length > 0 ? grillerNodes : allAINodes);

			while (unselectedNodes.Count > 0)
			{
				var node = unselectedNodes[unselectedNodes.Count];

				unselectedNodes.Remove(node);

				if (SetDestinationToPosition(node.transform.position, true))
				{
					break;
				}
			}
		}
	}
}