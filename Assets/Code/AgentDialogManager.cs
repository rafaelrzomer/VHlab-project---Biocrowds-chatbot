using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace Biocrowds.Core
{

    [System.Serializable]
     public class AgentChatRequest
     {
        public string agentName;
        public string message;
        public List<MemorizedInterestInfo> memory;
    }
        
    public class AgentDialogManager : MonoBehaviour
    {
        public static AgentDialogManager Instance { get; private set; }
        

        [Header("Dialog Panel")]
        public GameObject dialogPanel;
        public Text titleText;
        public Text bodyText;
        public TMP_Text titleTextTMP;
        public TMP_Text bodyTextTMP;
        public TMP_InputField inputField;
        public Button submitButton;
        public TMP_Text submitResultText;
        public Button closeButton;

        private string _lastSubmittedText;
        private Image _panelImage;
        private Agent _currentAgent;

        [Header("Python")]
        public string pythonUrl = "http://127.0.0.1:8000/chat";

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

        private void Start()
        {
            if (dialogPanel != null)
            {
                _panelImage = dialogPanel.GetComponent<Image>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HideDialog);
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitInput);
            }

            HideDialog();
        }

        public void ShowDialog(Agent agent)
        {
            if (dialogPanel == null)
            {
                Debug.LogWarning("AgentDialogManager: dialogPanel não está atribuído.");
                return;
            }

            if (agent == null)
            {
                Debug.LogWarning("AgentDialogManager: agente nulo em ShowDialog.");
                return;
            }

            _currentAgent = agent;

            dialogPanel.SetActive(true);

            if (_panelImage != null)
                _panelImage.raycastTarget = true;

            SetText(titleText, titleTextTMP, $"Agente: {agent.name}");
            SetText(bodyText, bodyTextTMP, GenerateAgentText(agent));

            if (inputField != null)
                inputField.text = string.Empty;

            if (submitResultText != null)
                submitResultText.text = string.Empty;

            Debug.Log($"AgentDialogManager: exibindo diálogo do agente {agent.name}");
        }

        private string ExtractResponse(string json)
{
        JObject result = JObject.Parse(json);
        return result["response"]?.ToString() ?? json;
}

        public void SubmitInput()
        {
            if (inputField == null)
            {
                Debug.LogWarning("AgentDialogManager: inputField não está atribuído.");
                return;
            }

            if (_currentAgent == null)
            {
                Debug.LogWarning("Nenhum agente selecionado.");
                return;
            }

            AgentCustom customAgent = _currentAgent as AgentCustom;

            if (customAgent == null)
            {
                Debug.LogWarning("O agente selecionado não é um AgentCustom.");
                return;
            }

            string message = inputField.text;

            AgentChatRequest request = new AgentChatRequest
            {
                agentName = customAgent.name,
                message = message,
                memory = new List<MemorizedInterestInfo>(customAgent.InterestMemory)
            };

            string json = JsonConvert.SerializeObject(
                request,
                Formatting.Indented
            );

            Debug.Log("JSON DA CONVERSA:");
            Debug.Log(json);

            StartCoroutine(SendToPython(json));
        }

        private IEnumerator SendToPython(string json)
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request =
                new UnityWebRequest(pythonUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader(
                    "Content-Type",
                    "application/json"
                );

                if (submitResultText != null)
                    submitResultText.text = "Aguardando resposta...";

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;

                    Debug.Log("Resposta do Python:");
                    Debug.Log(response);

                    if (submitResultText != null)
                        submitResultText.text = ExtractResponse(response);
                }
                else
                {
                    Debug.LogError(
                        $"Erro na comunicação com Python: {request.error}"
                    );

                    Debug.LogError(request.downloadHandler.text);

                    if (submitResultText != null)
                        submitResultText.text =
                            "Erro ao conectar com o servidor Python.";
                }
            }
        }

        private void SetText(Text text, TMP_Text tmpText, string message)
        {
            if (text != null)
                text.text = message;
            else if (tmpText != null)
                tmpText.text = message;
        }

        private string GenerateAgentText(Agent agent)
        {
            return $"Posição: {agent.transform.position}\n" +
                   $"Velocidade: {agent._velocity:F2}\n" +
                   $"Meta atual: {(agent.goalsList != null && agent.goalsList.Count > 0 && agent.GoalIndex < agent.goalsList.Count ? agent.goalsList[agent.GoalIndex].name : "Sem meta")}";
        }

        public void HideDialog()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
                
                if (_panelImage != null)
                    _panelImage.raycastTarget = false;
            }
        }
    }
}
