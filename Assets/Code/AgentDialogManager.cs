using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Biocrowds.Core
{
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

        public void SubmitInput()
        {
            if (inputField == null)
            {
                Debug.LogWarning("AgentDialogManager: inputField não está atribuído.");
                return;
            }

            _lastSubmittedText = inputField.text;
            Debug.Log($"AgentDialogManager: texto enviado -> {_lastSubmittedText}");

            if (submitResultText != null)
                submitResultText.text = $"Enviado: {_lastSubmittedText}";
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
