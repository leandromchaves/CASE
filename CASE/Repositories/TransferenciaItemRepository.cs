using System.Collections.Generic;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class TransferenciaItemRepository
    {
        public List<TransferenciaItem> ListarPorTransferencia(int idTransferencia)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_transferencia_item,id_transferencia,id_produto,quantidade FROM transferenciaitem WHERE id_transferencia=@id ORDER BY id_transferencia_item", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idTransferencia);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<TransferenciaItem> resultadoBanco = new List<TransferenciaItem>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        private static TransferenciaItem ConverterLinha(MySqlDataReader leitor)
        {
            return new TransferenciaItem
            {
                IdTransferenciaItem = leitor.GetInt32("id_transferencia_item"),
                IdTransferencia = leitor.GetInt32("id_transferencia"),
                IdProduto = leitor.GetInt32("id_produto"),
                Quantidade = leitor.GetDecimal("quantidade")
            };
        }
    }
}
