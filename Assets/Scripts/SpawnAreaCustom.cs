using System.Collections.Generic;
using UnityEngine;
using Biocrowds.Core;

public class SpawnAreaCustom : SpawnArea
{
    [Header("Custom Agent Prefabs")]
    [Tooltip("Prefab list used by this spawn area when spawning AgentCustom instances.")]
    public List<AgentCustom> customAgentPrefabs = new List<AgentCustom>();

    [Tooltip("If true, this spawn area will spawn AgentCustom prefabs instead of the default Agent prefabs.")]
    public bool spawnCustomAgents = true;
}
