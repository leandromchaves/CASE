using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ProdutoSetorRepository
    {
        private const string Colunas = "id_produto_setor,id_setor,id_produto,preco_venda";

        public List<ProdutoSetor> ListarPorSetor(int idSetor)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM produtosetor WHERE id_setor=@id ORDER BY id_produto_setor", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idSetor);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<ProdutoSetor> resultadoBanco = new List<ProdutoSetor>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public ProdutoSetor BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM produtosetor WHERE id_produto_setor=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", id);
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

        public int Cadastrar(ProdutoSetor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO produtosetor (id_setor,id_produto,preco_venda) VALUES (@setor,@produto,@preco)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public int SalvarPreco(ProdutoSetor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO produtosetor (id_setor,id_produto,preco_venda) VALUES (@setor,@produto,@preco) " +
                "ON DUPLICATE KEY UPDATE id_produto_setor=LAST_INSERT_ID(id_produto_setor),preco_venda=@preco", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(ProdutoSetor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE produtosetor SET id_setor=@setor,id_produto=@produto,preco_venda=@preco WHERE id_produto_setor=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdProdutoSetor);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static void Vincular(MySqlCommand comando, ProdutoSetor item)
        {
            AcessoDados.AdicionarParametro(comando, "@setor", item.IdSetor);
            AcessoDados.AdicionarParametro(comando, "@produto", item.IdProduto);
            AcessoDados.AdicionarParametro(comando, "@preco", item.PrecoVenda);
        }

        private static ProdutoSetor ConverterLinha(MySqlDataReader leitor)
        {
            return new ProdutoSetor
            {
                IdProdutoSetor = leitor.GetInt32("id_produto_setor"),
                IdSetor = leitor.GetInt32("id_setor"),
                IdProduto = leitor.GetInt32("id_produto"),
                PrecoVenda = leitor.GetDecimal("preco_venda")
            };
        }
    }
}
