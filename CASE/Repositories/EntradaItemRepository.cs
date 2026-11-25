using System.Collections.Generic;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EntradaItemRepository
    {
        public List<EntradaItem> ListarPorEntrada(int idEntrada)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_entrada_item,id_entrada,id_produto,quantidade,custo_unitario FROM entradaitem WHERE id_entrada=@id ORDER BY id_entrada_item", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEntrada);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<EntradaItem> resultadoBanco = new List<EntradaItem>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        private static EntradaItem ConverterLinha(MySqlDataReader leitor)
        {
            return new EntradaItem
            {
                IdEntradaItem = leitor.GetInt32("id_entrada_item"),
                IdEntrada = leitor.GetInt32("id_entrada"),
                IdProduto = leitor.GetInt32("id_produto"),
                Quantidade = leitor.GetDecimal("quantidade"),
                CustoUnitario = AcessoDados.LerDecimalOpcional(leitor, "custo_unitario")
            };
        }
    }
}
