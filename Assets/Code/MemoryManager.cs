using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biocrowds.Core;
using Newtonsoft.Json;
using UnityEngine;

public class MemoryManager : MonoBehaviour
{
    public static MemoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        
    }

    //salva os POIs visitados por cada agente em um JSON
    public void SaveInterestMemoryToJson(string agentName, List<MemorizedInterestInfo> interestMemory)
    {
        string path = @"F:\RafaelZomer\BioCrowds-GS-main\simulation_memory.json";

        SimulationMemoryData simulationData;

        if (File.Exists(path))
        {
            string existingJson = File.ReadAllText(path);

            simulationData = JsonConvert.DeserializeObject<SimulationMemoryData>(existingJson);

            if (simulationData == null)
                simulationData = new SimulationMemoryData();
        }
        else
        {
            simulationData = new SimulationMemoryData();
        }

        AgentMemoryData existingAgent = simulationData.agents.Find(
            a => a.agentName == agentName
        );

        if (existingAgent != null)
        {
            existingAgent.visitedPOIs = new List<MemorizedInterestInfo>(interestMemory);
        }
        else
        {
            AgentMemoryData newAgent = new AgentMemoryData
            {
                agentName = agentName,
                visitedPOIs = new List<MemorizedInterestInfo>(interestMemory)
            };

            simulationData.agents.Add(newAgent);
        }

        string json = JsonConvert.SerializeObject(
            simulationData,
            Formatting.Indented
        );

        File.WriteAllText(path, json);

    }

    // Salva os POIs visitados por todos os agentes em uma única operação
    public void SaveAllAgentsMemoryToJson(List<Agent> agents)
    {
        string path = @"F:\RafaelZomer\BioCrowds-GS-main\simulation_memory.json";

        SimulationMemoryData simulationData;

        if (File.Exists(path))
        {
            string existingJson = File.ReadAllText(path);
            simulationData = JsonConvert.DeserializeObject<SimulationMemoryData>(existingJson);

            if (simulationData == null)
                simulationData = new SimulationMemoryData();
        }
        else
        {
            simulationData = new SimulationMemoryData();
        }

        foreach (Agent agent in agents)
        {
            if (agent != null && agent is AgentCustom customAgent)
            {
                AgentMemoryData existingAgent = simulationData.agents.Find(a => a.agentName == customAgent.name);

                if (existingAgent != null)
                {
                    existingAgent.visitedPOIs = new List<MemorizedInterestInfo>(customAgent.InterestMemory);
                }
                else
                {
                    AgentMemoryData newAgent = new AgentMemoryData
                    {
                        agentName = customAgent.name,
                        visitedPOIs = new List<MemorizedInterestInfo>(customAgent.InterestMemory)
                    };
                    simulationData.agents.Add(newAgent);
                }
            }
        }

        string json = JsonConvert.SerializeObject(simulationData, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log("JSON com a memória de todos os agentes salvo no término do World em: " + path);
    }
}
