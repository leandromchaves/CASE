using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ProdutoRepository
    {
        private const string Colunas = "id_produto,id_categoria,nome,descricao,unidade_medida,ativo,imagem";

        public List<Produto> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM produto ORDER BY nome,id_produto", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Produto> resultadoBanco = new List<Produto>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public int Contar()
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                string sql = "SELECT COUNT(*) FROM produto";

                using (MySqlCommand comando = new MySqlCommand(sql, conexao))
                {
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        public Produto BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM produto WHERE id_produto=@id", conexaoBanco))
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

        public int Cadastrar(Produto item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO produto (id_categoria,nome,descricao,unidade_medida,ativo,imagem) VALUES (@categoria,@nome,@descricao,@unidade,@ativo,@imagem)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(Produto item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE produto SET id_categoria=@categoria,nome=@nome,descricao=@descricao,unidade_medida=@unidade,ativo=@ativo,imagem=@imagem WHERE id_produto=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdProduto);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        public bool AlterarAtivo(int idProduto, bool ativo)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE produto SET ativo=@ativo WHERE id_produto=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idProduto);
                    AcessoDados.AdicionarParametro(comandoBanco, "@ativo", ativo);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;
        }

        private static void Vincular(MySqlCommand comando, Produto item)
        {
            AcessoDados.AdicionarParametro(comando, "@categoria", item.IdCategoria);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
            AcessoDados.AdicionarTextoOpcional(comando, "@descricao", item.Descricao);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@unidade", item.UnidadeMedida);
            AcessoDados.AdicionarParametro(comando, "@ativo", item.Ativo);
            AcessoDados.AdicionarParametro(comando, "@imagem", item.Imagem);
        }

        private static Produto ConverterLinha(MySqlDataReader leitor)
        {
            return new Produto
            {
                IdProduto = leitor.GetInt32("id_produto"),
                IdCategoria = leitor.GetInt32("id_categoria"),
                Nome = leitor.GetString("nome"),
                Descricao = AcessoDados.LerTextoOpcional(leitor, "descricao"),
                UnidadeMedida = leitor.GetString("unidade_medida"),
                Ativo = leitor.GetBoolean("ativo"),
                Imagem = AcessoDados.LerBytesOpcional(leitor, "imagem")
            };
        }
    }
}
