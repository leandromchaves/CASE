using System.Collections.Generic;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class SolicitacaoItemRepository
    {
        public List<SolicitacaoItem> ListarPorSolicitacao(int idSolicitacao)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_solicitacao_item,id_solicitacao,id_produto_setor,quantidade FROM solicitacaoitem WHERE id_solicitacao=@id ORDER BY id_solicitacao_item", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idSolicitacao);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<SolicitacaoItem> resultadoBanco = new List<SolicitacaoItem>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        private static SolicitacaoItem ConverterLinha(MySqlDataReader leitor)
        {
            return new SolicitacaoItem
            {
                IdSolicitacaoItem = leitor.GetInt32("id_solicitacao_item"),
                IdSolicitacao = leitor.GetInt32("id_solicitacao"),
                IdProdutoSetor = leitor.GetInt32("id_produto_setor"),
                Quantidade = leitor.GetDecimal("quantidade")
            };
        }
    }
}
