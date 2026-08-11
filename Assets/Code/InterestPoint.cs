using UnityEngine;

namespace Biocrowds.Core
{
    [System.Serializable]
    public class InterestPointData
    {
       
       
    }

    [DisallowMultipleComponent]
    public class InterestPoint : MonoBehaviour
    {
        [Tooltip("Nome opcional do ponto de interesse.")]
        public string pointName = "Interest Point";

        [TextArea(3, 5)]
        [Tooltip("Descrição em formato JSON do ponto de interesse.")]
        public string pointDescription = "{}";

        [Tooltip("Quando ativado, agentes podem memorizar este ponto de interesse.")]
        public bool remember = true;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }

       
    }
}
