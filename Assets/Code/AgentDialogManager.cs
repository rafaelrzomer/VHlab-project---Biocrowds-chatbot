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
        public string agentId;
        public string agentName;
        public string message;
        public List<MemorizedInterestInfo> memory;
    }

    [System.Serializable]
    public class AgentChatMessage
    {
        public string sender;
        public string message;
        public bool isUser;

        public AgentChatMessage(string sender, string message, bool isUser)
        {
            this.sender = sender;
            this.message = message;
            this.isUser = isUser;
        }
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

        [Header("Chat History")]
        public Transform messagesContent;
        public GameObject chatMessagePrefab;

        private readonly Dictionary<string, List<AgentChatMessage>> agentChatHistories =
            new Dictionary<string, List<AgentChatMessage>>();
        private string currentAgentId;
        private Coroutine pendingChatScroll;
        private float previousTimeScale = 1f;
        private bool dialogPausedSimulation;

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

            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(HandleInputSubmit);
                inputField.onSubmit.AddListener(HandleInputSubmit);
            }

            HideDialog();
        }

    private void HandleInputSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        SubmitInput();
    }

    private void AddMessage(string sender, string message, bool isUser)
    {
        if (string.IsNullOrEmpty(currentAgentId))
        {
            Debug.LogWarning("AgentDialogManager: não há agente ativo para salvar a mensagem.");
            return;
        }

        if (!agentChatHistories.TryGetValue(currentAgentId, out List<AgentChatMessage> history))
        {
            history = new List<AgentChatMessage>();
            agentChatHistories[currentAgentId] = history;
        }

        history.Add(new AgentChatMessage(sender, message, isUser));
        CreateMessageRow(sender, message, isUser);
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
            currentAgentId = agent.GetInstanceID().ToString();

            PauseSimulationForDialog();

            if (messagesContent != null)
            {
                foreach (Transform child in messagesContent)
                    Destroy(child.gameObject);
            }

            if (!agentChatHistories.TryGetValue(currentAgentId, out List<AgentChatMessage> history))
            {
                history = new List<AgentChatMessage>();
                agentChatHistories[currentAgentId] = history;
            }

            foreach (AgentChatMessage savedMessage in history)
                CreateMessageBubble(savedMessage.sender, savedMessage.message, savedMessage.isUser);

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

        private void CreateMessageBubble(string sender, string message, bool isUser)
        {
            // Reuse rendering logic without appending a duplicate history entry.
            CreateMessageRow(sender, message, isUser);
        }

        private void CreateMessageRow(string sender, string message, bool isUser)
        {
            GameObject row = Instantiate(chatMessagePrefab, messagesContent);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
                layout.childAlignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            RectTransform rowRect = row.GetComponent<RectTransform>();
            Transform bubble = row.transform.Find("Bubble");
            if (bubble == null)
            {
                Debug.LogWarning("AgentDialogManager: o prefab de mensagem não contém 'Bubble'.");
                Destroy(row);
                return;
            }

            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            TMP_Text text = bubbleRect.GetComponentInChildren<TMP_Text>();
            if (rowRect == null || bubbleRect == null || text == null)
            {
                Debug.LogWarning("AgentDialogManager: estrutura inválida no prefab de mensagem.");
                Destroy(row);
                return;
            }

            text.text = sender + "\n" + message;
            text.color = new Color32(236, 236, 236, 255);
            text.ForceMeshUpdate();
            float textWidth = text.rectTransform.rect.width;
            if (textWidth <= 0)
                textWidth = 226;
            float textHeight = text.GetPreferredValues(text.text, textWidth, 10000).y;
            // Include the sender line and leave room for the prefab's 8 px top/bottom inset.
            float bubbleHeight = Mathf.Max(60, textHeight + 24);
            bubbleRect.anchorMin = new Vector2(isUser ? 1 : 0, 1);
            bubbleRect.anchorMax = new Vector2(isUser ? 1 : 0, 1);
            bubbleRect.pivot = new Vector2(isUser ? 1 : 0, 1);
            bubbleRect.anchoredPosition = Vector2.zero;
            bubbleRect.sizeDelta = new Vector2(250, bubbleHeight);
            rowRect.sizeDelta = new Vector2(0, bubbleHeight);

            Image background = bubbleRect.GetComponent<Image>();
            if (background != null)
                background.color = isUser
                    ? new Color32(52, 101, 142, 255)
                    : new Color32(55, 55, 55, 255);

            Canvas.ForceUpdateCanvases();
            RectTransform contentRect = messagesContent as RectTransform;
            if (contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            ScrollRect scrollRect = messagesContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null && contentRect != null)
            {
                if (pendingChatScroll != null)
                    StopCoroutine(pendingChatScroll);
                pendingChatScroll = StartCoroutine(ScrollToLatestMessage(scrollRect, contentRect));
            }
        }

        private IEnumerator ScrollToLatestMessage(ScrollRect scrollRect, RectTransform contentRect)
        {
            // Let the layout group and content size fitter finish updating first.
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            scrollRect.verticalNormalizedPosition = 0f;
            pendingChatScroll = null;
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
            if (string.IsNullOrWhiteSpace(message))
                return;

            AddMessage("Você", message, true);
            inputField.text = string.Empty;

            AgentChatRequest request = new AgentChatRequest
            {
                agentId = customAgent.GetInstanceID().ToString(),
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

            string respondingAgentId = currentAgentId;
            StartCoroutine(SendToPython(json, respondingAgentId, customAgent.name));
        }

    private IEnumerator SendToPython(string json, string respondingAgentId, string respondingAgentName)
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

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                Debug.Log("Resposta do Python:");
                Debug.Log(response);

                string answer = ExtractResponse(response);

                AddMessageForAgent(respondingAgentId, respondingAgentName, answer);
            }
            else
            {
                Debug.LogError(
                    $"Erro na comunicação com Python: {request.error}"
                );

                Debug.LogError(request.downloadHandler.text);

                AddMessageForAgent(respondingAgentId, "Sistema", "Erro ao conectar com o servidor Python.");
            }
        }
    }

    private void AddMessageForAgent(string agentId, string sender, string message)
    {
        if (string.IsNullOrEmpty(agentId))
            return;

        if (!agentChatHistories.TryGetValue(agentId, out List<AgentChatMessage> history))
        {
            history = new List<AgentChatMessage>();
            agentChatHistories[agentId] = history;
        }

        history.Add(new AgentChatMessage(sender, message, false));
        if (currentAgentId == agentId && messagesContent != null)
            CreateMessageRow(sender, message, false);
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

            ResumeSimulationAfterDialog();
        }

        private void PauseSimulationForDialog()
        {
            if (dialogPausedSimulation)
                return;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            dialogPausedSimulation = true;
        }

        private void ResumeSimulationAfterDialog()
        {
            if (!dialogPausedSimulation)
                return;

            Time.timeScale = previousTimeScale;
            dialogPausedSimulation = false;
        }
    }
}
