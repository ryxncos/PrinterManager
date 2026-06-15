using System;

namespace PrintManager.Models
{
    public class Peca
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public DateTime? UltimaSubstituicao { get; set; }
        public string Observacoes { get; set; }

        public bool MaisAntigaQue(Peca outra)
        {
            if (UltimaSubstituicao == null) return true;
            if (outra.UltimaSubstituicao == null) return false;
            return UltimaSubstituicao < outra.UltimaSubstituicao;
        }
    }
}
