namespace CASE.Models
{
    public class LocalEstoque
    {
        public int IdLocalEstoque { get; set; }
        public int IdInstituicao { get; set; }
        public int? IdEvento { get; set; }
        public string Nome { get; set; }
        public string EventoNome { get; set; }

        // Identifica o evento na exibição, sem alterar o nome salvo.
        public string Descricao
        {
            get
            {
                if (!IdEvento.HasValue)
                {
                    return Nome;
                }

                string evento = string.IsNullOrWhiteSpace(EventoNome) ? "evento " + IdEvento.Value : EventoNome;
                return Nome + " · " + evento;
            }
        }

        public bool Ativo { get; set; }
        public bool EhCentral { get; set; }
        public bool EhGeralEvento { get; set; }
    }
}
