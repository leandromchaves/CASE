using System.Collections.Generic;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class RegistroItemRepository
    {
        public List<RegistroItem> ListarPorRegistro(int idRegistro)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_registro_item,id_registro,id_produto_setor,quantidade,preco_unitario_praticado FROM registroitem WHERE id_registro=@id ORDER BY id_registro_item", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idRegistro);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<RegistroItem> resultadoBanco = new List<RegistroItem>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        private static RegistroItem ConverterLinha(MySqlDataReader leitor)
        {
            return new RegistroItem
            {
                IdRegistroItem = leitor.GetInt32("id_registro_item"),
                IdRegistro = leitor.GetInt32("id_registro"),
                IdProdutoSetor = leitor.GetInt32("id_produto_setor"),
                Quantidade = leitor.GetDecimal("quantidade"),
                PrecoUnitarioPraticado = AcessoDados.LerDecimalOpcional(leitor, "preco_unitario_praticado")
            };
        }
    }
}
