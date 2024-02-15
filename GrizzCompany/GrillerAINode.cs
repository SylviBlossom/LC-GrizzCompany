using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrizzCompany
{
	public class GrillerAINode : MonoBehaviour
	{
	}

	[CreateAssetMenu(menuName = "ScriptableObjects/GrizzCompany/GrillerAINodeInjector", order = 100)]
	public class GrillerAINodeInjector : ScriptableObject
	{
		public List<GrillerAINodeInjectorElement> elements;
	}

	[Serializable]
	public class GrillerAINodeInjectorElement
	{
		public GameObject targetPrefab;
		public GameObject nodesPrefab;
	}
}
