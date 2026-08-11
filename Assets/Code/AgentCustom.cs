using System.Collections.Generic;
using UnityEngine;
using System.Timers;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Biocrowds.Core
{
    [System.Serializable]
    public class MemorizedInterestInfo
    {
        public string name;
        public JObject description;
        public string time;

    }

    [System.Serializable]
    public class AgentMemoryData
    {
        public string agentName;
        public List<MemorizedInterestInfo> visitedPOIs;
    }

    [System.Serializable]
    public class SimulationMemoryData
    {
        public List <AgentMemoryData> agents = new List<AgentMemoryData>();
    }

    
    public class AgentCustom : Agent
    {
        [Header("Interest Point Sensor")]
        [Tooltip("Layer mask used to search for interest points.")]
        public LayerMask interestPointLayer = ~0;
        [Tooltip("Optional tag filter for interest points. Leave blank to use any InterestPoint component.")]
        public string interestPointTag = "";
        [Tooltip("Sensor radius used to detect nearby interest point colliders.")]
        public float interestSensorRadius = 5f;
        [Tooltip("How often, in seconds, the agent checks for interest points.")]
        public float interestSensorInterval = 0.5f;
        private float _interestSensorTimer;

        [SerializeField]
        private List<MemorizedInterestInfo> _interestMemory = new List<MemorizedInterestInfo>();
        public IReadOnlyList<MemorizedInterestInfo> InterestMemory => _interestMemory;

        protected override void Start()
        {
            base.Start();
            Debug.Log($"AgentCustom inicializado: {name}");
        }

        public override void NavmeshStep(float _timeStep)
        {
            base.NavmeshStep(_timeStep);
            SenseInterestPoints(_timeStep);
        }

        void SenseInterestPoints(float timeStep)
        {
            _interestSensorTimer += timeStep;
            if (_interestSensorTimer < interestSensorInterval)
                return;

            _interestSensorTimer = 0f;
            Collider[] hits = Physics.OverlapSphere(transform.position, interestSensorRadius, interestPointLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!string.IsNullOrEmpty(interestPointTag) && !hits[i].CompareTag(interestPointTag))
                    continue;

                InterestPoint point = hits[i].GetComponentInParent<InterestPoint>();
                if (point == null)
                    point = hits[i].GetComponent<InterestPoint>();

                if (point == null || !point.remember)
                    continue;

                // Check if already memorized by name
                bool alreadyMemorized = false;
                foreach (MemorizedInterestInfo mem in _interestMemory)
                {
                    if (mem.name == point.pointName)
                    {
                        alreadyMemorized = true;
                        break;
                    }
                }

                if (!alreadyMemorized)
                {
                    MemorizedInterestInfo info = new MemorizedInterestInfo
                    {
                        name = point.pointName,
                        description = JObject.Parse(point.pointDescription),
                        time = DateTime.Now.ToString("HH:mm:ss")
                    };
                    _interestMemory.Add(info);
                }
            }
        }

        public void ClearInterestMemory()
        {
            _interestMemory.Clear();
        }



        void Update()
        {
            // Optional: add custom frame behavior here.
        }

        private void OnDestroy()
        {
            
        }

    }
}
