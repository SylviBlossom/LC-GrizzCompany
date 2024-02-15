using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrizzCompany
{
	public class PrefabChildInjector : MonoBehaviour
	{
		public List<Object> childPrefabs = new();

		public void Awake()
		{
			foreach (var prefab in childPrefabs)
			{
				Object.Instantiate(prefab, transform);
			}
		}
	}
}
