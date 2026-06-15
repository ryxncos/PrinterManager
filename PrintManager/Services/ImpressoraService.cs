using System;
using System.Collections.Generic;
using System.Linq;
using PrintManager.Data;
using PrintManager.Models;

namespace PrintManager.Services
{
    public class ImpressoraService
    {
        private readonly JsonRepository _repo;
        private List<Impressora> _cache;

        public ImpressoraService()
        {
            _repo  = new JsonRepository();
            _cache = _repo.CarregarTodas();
        }

        public List<Impressora> ListarTodas() => _cache;

        public List<Impressora> Filtrar(string busca)
        {
            if (string.IsNullOrWhiteSpace(busca)) return _cache;
            busca = busca.ToLower();
            return _cache.Where(i =>
                (i.Nome?.ToLower().Contains(busca) ?? false) ||
                (i.Setor?.ToLower().Contains(busca) ?? false) ||
                (i.EnderecoIP?.Contains(busca) ?? false) ||
                i.Tipo.ToString().ToLower().Contains(busca)
            ).ToList();
        }

        public void Salvar(Impressora impressora)
        {
            impressora.RegistrarModificacao();
            _repo.Salvar(impressora, _cache);
        }

        public void Remover(Guid id) => _repo.Remover(id, _cache);

        public Impressora BuscarPorId(Guid id) =>
            _cache.FirstOrDefault(i => i.Id == id);

        public Dictionary<string, (Impressora Impressora, Peca Peca)> ResumoMaisAntigas()
        {
            var resultado = new Dictionary<string, (Impressora, Peca)>();
            var nomes = new[]
            {
                "Cabeca de Impressao", "Roletes de Pressao",
                "Recartilhado", "Correias", "Sensores de Midia"
            };

            foreach (var nome in nomes)
            {
                Impressora piorImp = null;
                Peca piorPeca = null;

                foreach (var imp in _cache)
                {
                    var peca = ObterPeca(imp, nome);
                    if (peca == null) continue;
                    if (piorPeca == null || peca.MaisAntigaQue(piorPeca))
                    {
                        piorPeca = peca;
                        piorImp  = imp;
                    }
                }

                if (piorImp != null)
                    resultado[nome] = (piorImp, piorPeca);
            }

            return resultado;
        }

        private Peca ObterPeca(Impressora imp, string nome) => nome switch
        {
            "Cabeca de Impressao" => imp.CabecaImpressao,
            "Roletes de Pressao"  => imp.RoletesPressao,
            "Recartilhado"        => imp.Recartilhado,
            "Correias"            => imp.Correias,
            "Sensores de Midia"   => imp.SensoresMidia,
            _ => null
        };
    }
}
