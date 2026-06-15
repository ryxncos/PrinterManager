using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PrintManager.Models;

namespace PrintManager.Data
{
    public class JsonRepository
    {
        private readonly string _filePath;

        public JsonRepository()
        {
            _filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "impressoras.json"
            );
        }

        public List<Impressora> CarregarTodas()
        {
            if (!File.Exists(_filePath))
                return new List<Impressora>();
            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<Impressora>>(json)
                       ?? new List<Impressora>();
            }
            catch { return new List<Impressora>(); }
        }

        public void SalvarTodas(List<Impressora> impressoras)
        {
            var json = JsonConvert.SerializeObject(impressoras, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Salvar(Impressora impressora, List<Impressora> lista)
        {
            var idx = lista.FindIndex(i => i.Id == impressora.Id);
            if (idx >= 0) lista[idx] = impressora;
            else lista.Add(impressora);
            SalvarTodas(lista);
        }

        public void Remover(Guid id, List<Impressora> lista)
        {
            lista.RemoveAll(i => i.Id == id);
            SalvarTodas(lista);
        }
    }
}
