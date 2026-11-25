using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ProdutoFornecedorRepository
    {
        public List<ProdutoFornecedor> ListarPorProduto(int idProduto)
        {
            ExigirConsulta();
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_produto,id_fornecedor,codigo_no_fornecedor FROM produtofornecedor WHERE id_produto=@id ORDER BY id_fornecedor", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idProduto);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<ProdutoFornecedor> resultadoBanco = new List<ProdutoFornecedor>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public ProdutoFornecedor BuscarPorChave(int idProduto, int idFornecedor)
        {
            ExigirConsulta();
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_produto,id_fornecedor,codigo_no_fornecedor FROM produtofornecedor WHERE id_produto=@produto AND id_fornecedor=@fornecedor", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@produto", idProduto);
                    AcessoDados.AdicionarParametro(comandoBanco, "@fornecedor", idFornecedor);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        if (leitorBanco.Read())
                        {
                            return ConverterLinha(leitorBanco);
                        }
                        return null;
                    }
                }
            }

        }

        public void Vincular(ProdutoFornecedor item)
        {
            ExecutarAdministrativo(
                "INSERT INTO produtofornecedor (id_produto,id_fornecedor,codigo_no_fornecedor) VALUES (@produto,@fornecedor,@codigo)",
                item.IdProduto,
item.IdFornecedor,
item.CodigoNoFornecedor);

        }

        public void SalvarVinculo(ProdutoFornecedor item)
        {
            ExecutarAdministrativo(
                "INSERT INTO produtofornecedor (id_produto,id_fornecedor,codigo_no_fornecedor) " +
                "VALUES (@produto,@fornecedor,@codigo) ON DUPLICATE KEY UPDATE codigo_no_fornecedor=@codigo",
                item.IdProduto, item.IdFornecedor, item.CodigoNoFornecedor);
        }

        public bool AtualizarCodigo(int idProduto, int idFornecedor, string codigo)
        {
            return ExecutarAdministrativo(
                "UPDATE produtofornecedor SET codigo_no_fornecedor=@codigo WHERE id_produto=@produto AND id_fornecedor=@fornecedor",
                idProduto,
idFornecedor,
codigo) > 0;

        }

        public bool Desvincular(int idProduto, int idFornecedor)
        {
            return ExecutarAdministrativo(
                "DELETE FROM produtofornecedor WHERE id_produto=@produto AND id_fornecedor=@fornecedor",
                idProduto, idFornecedor, null) > 0;

        }

        private static void ExigirConsulta()
        {
            new AutorizacaoService().ExigirAdministradorOuGerente(SessaoUsuario.ObterUsuarioAtual());
        }

        private static int ExecutarAdministrativo(string sql, int produto, int fornecedor, string codigo)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        FornecedorRepository.ValidarAcesso(conexao, transacao, true);
                        if (produto <= 0 || fornecedor <= 0 || codigo != null && codigo.Length > 50)
                        {
                            throw new ArgumentException("Selecione produto e fornecedor válidos e um código com até 50 caracteres.");
                        }
                        int resultadoBanco;
                        using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexao, transacao))
                        {
                            comandoBanco.Parameters.AddWithValue("@produto", produto);
                            comandoBanco.Parameters.AddWithValue("@fornecedor", fornecedor);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco, "@codigo", codigo);
                            resultadoBanco = comandoBanco.ExecuteNonQuery();
                        }
                        int resultado = resultadoBanco;
                        transacao.Commit();
                        return resultado;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        private static ProdutoFornecedor ConverterLinha(MySqlDataReader leitor)
        {
            return new ProdutoFornecedor
            {
                IdProduto = leitor.GetInt32("id_produto"),
                IdFornecedor = leitor.GetInt32("id_fornecedor"),
                CodigoNoFornecedor = AcessoDados.LerTextoOpcional(leitor, "codigo_no_fornecedor")
            };
        }
    }
}
