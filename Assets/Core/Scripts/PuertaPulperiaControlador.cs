using UnityEngine;

namespace Trucardo.Core
{
    public class PuertaPulperiaControlador : MonoBehaviour
    {
        public enum Estado
        {
            Cerrada,
            Entreabierta,
            Abierta
        }

        [SerializeField] private Transform hojaPuerta;
        [SerializeField] private Estado estadoInicial = Estado.Abierta;
        [SerializeField] private float anguloEntreabierta = 35f;
        [SerializeField] private float anguloAbierta = 100f;

        private void Start()
        {
            AplicarEstado(estadoInicial);
        }

        public void Configurar(Transform hoja)
        {
            hojaPuerta = hoja;
            estadoInicial = Estado.Abierta;
            AplicarEstado(Estado.Abierta);
        }

        public void Cerrar()
        {
            AplicarEstado(Estado.Cerrada);
        }

        public void Entreabrir()
        {
            AplicarEstado(Estado.Entreabierta);
        }

        public void Abrir()
        {
            AplicarEstado(Estado.Abierta);
        }

        public void AplicarEstado(Estado estado)
        {
            if (hojaPuerta == null)
                return;

            float angulo = estado == Estado.Abierta
                ? anguloAbierta
                : estado == Estado.Entreabierta ? anguloEntreabierta : 0f;

            hojaPuerta.localRotation = Quaternion.Euler(0f, angulo, 0f);
        }
    }
}