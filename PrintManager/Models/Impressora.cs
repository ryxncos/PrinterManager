using System;
using System.Collections.Generic;

namespace PrintManager.Models
{
    public class Impressora
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nome { get; set; }
        public string EnderecoIP { get; set; }
        public string Setor { get; set; }
        public TipoImpressora Tipo { get; set; }
        public string NumeroSerie { get; set; }
        public string Localizacao { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public DateTime? UltimaModificacaoGeral { get; set; }

        public Peca CabecaImpressao { get; set; } = new Peca { Nome = "Cabeca de Impressao" };
        public Peca RoletesPressao  { get; set; } = new Peca { Nome = "Roletes de Pressao" };
        public Peca Recartilhado    { get; set; } = new Peca { Nome = "Recartilhado" };
        public Peca Correias        { get; set; } = new Peca { Nome = "Correias" };
        public Peca SensoresMidia   { get; set; } = new Peca { Nome = "Sensores de Midia" };

        public Peca PecaMaisAntiga()
        {
            var pecas = new List<Peca>
            {
                CabecaImpressao, RoletesPressao,
                Recartilhado, Correias, SensoresMidia
            };
            Peca maisAntiga = null;
            foreach (var p in pecas)
                if (maisAntiga == null || p.MaisAntigaQue(maisAntiga))
                    maisAntiga = p;
            return maisAntiga;
        }

        public void RegistrarModificacao()
        {
            UltimaModificacaoGeral = DateTime.Now;
        }
    }
}
