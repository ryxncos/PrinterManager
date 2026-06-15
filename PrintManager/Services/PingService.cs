using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace PrintManager.Services
{
    public class PingService
    {
        public async Task<bool> TestarConexaoAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout: 2000);
                return reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        public async Task<PingResultado> TestarDetalhesAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return new PingResultado { Online = false, Mensagem = "IP nao informado" };
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout: 2000);
                if (reply.Status == IPStatus.Success)
                    return new PingResultado
                    {
                        Online = true,
                        Latencia = reply.RoundtripTime,
                        Mensagem = $"Online - {reply.RoundtripTime} ms"
                    };
                return new PingResultado { Online = false, Mensagem = "Sem resposta" };
            }
            catch
            {
                return new PingResultado { Online = false, Mensagem = "Erro ao conectar" };
            }
        }
    }

    public class PingResultado
    {
        public bool Online { get; set; }
        public long Latencia { get; set; }
        public string Mensagem { get; set; }
    }
}
