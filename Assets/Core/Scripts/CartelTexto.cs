using TMPro;
using UnityEngine;

namespace Trucardo.Core
{
    public class CartelTexto : MonoBehaviour
    {
        [SerializeField] private TMP_Text texto3D;
        [SerializeField, TextArea] private string contenido = "PULPERIA";

        private void OnValidate()
        {
            ActualizarTexto();
        }

        public void Configurar(TMP_Text texto, string valorInicial)
        {
            texto3D = texto;
            contenido = valorInicial;
            ActualizarTexto();
        }

        private void ActualizarTexto()
        {
            if (texto3D != null)
                texto3D.text = contenido;
        }
    }
}