namespace CASE.Models
{
    public class LocalEstoque
    {
        public int IdLocalEstoque { get; set; }
        public int IdInstituicao { get; set; }
        public int? IdEvento { get; set; }
        public string Nome { get; set; }
        // Identifica o evento na exibição, sem alterar o nome salvo.
        public string Descricao
        {
            get
            {
                return IdEvento.HasValue ? Nome + " · evento " + IdEvento.Value : Nome;
            }
        }
        public bool Ativo { get; set; }
        public bool EhCentral { get; set; }
        public bool EhGeralEvento { get; set; }
    }
}
