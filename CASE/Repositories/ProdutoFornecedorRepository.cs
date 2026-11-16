using System.Collections.Generic;
using CASE.Models;
using System;
using CASE.Data;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ProdutoFornecedorRepository
    {

        public List<ProdutoFornecedor> ListarPorProduto(int idProduto)
        {
            ExigirConsulta();
            return AcessoDados.ConsultarLista(
                "SELECT id_produto,id_fornecedor,codigo_no_fornecedor FROM produtofornecedor WHERE id_produto=@id ORDER BY id_fornecedor",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", idProduto)
            );
        }

        public ProdutoFornecedor BuscarPorChave(int idProduto, int idFornecedor)
        {
            ExigirConsulta();
            return AcessoDados.ConsultarUnico(
                "SELECT id_produto,id_fornecedor,codigo_no_fornecedor FROM produtofornecedor WHERE id_produto=@produto AND id_fornecedor=@fornecedor",
                ConverterLinha,
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@produto", idProduto);
                    AcessoDados.AdicionarParametro(comando, "@fornecedor", idFornecedor);
                });

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
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        FornecedorRepository.ValidarAcesso(conexao, transacao, true);
                        if (produto <= 0 || fornecedor <= 0 || codigo != null && codigo.Length > 50)
                        {
                            throw new ArgumentException("Selecione produto e fornecedor válidos e um código com até 50 caracteres.");
                        }
                        int resultado = AcessoDados.Executar(conexao, transacao, sql, comando =>
                        {
                            comando.Parameters.AddWithValue("@produto", produto);
                            comando.Parameters.AddWithValue("@fornecedor", fornecedor);
                            AcessoDados.AdicionarTextoOpcional(comando, "@codigo", codigo);
                        });
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
