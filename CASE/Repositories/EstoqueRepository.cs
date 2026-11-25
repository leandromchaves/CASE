using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EstoqueRepository
    {
        private const string Colunas = "id_estoque,id_local_estoque,id_produto,quantidade_minima,quantidade_ideal,quantidade_atual";

        public List<Estoque> ListarPorLocal(int idLocalEstoque, bool somenteDisponiveis = false)
        {
            string sql = "SELECT " + Colunas + " FROM estoque WHERE id_local_estoque=@id ORDER BY id_produto";
            if (somenteDisponiveis)
            {
                sql = "SELECT e.* FROM estoque e LEFT JOIN (" +
                    "SELECT id_produto,MIN(ordem) AS primeira_chegada FROM (" +
                    "SELECT i.id_produto,CONCAT(DATE_FORMAT(en.data_hora,'%Y%m%d%H%i%s')," +
                    "LPAD(i.id_entrada_item,20,'0')) AS ordem " +
                    "FROM entradamercadoria en JOIN entradaitem i ON i.id_entrada=en.id_entrada " +
                    "WHERE en.id_local_destino=@id UNION ALL " +
                    "SELECT i.id_produto,CONCAT(DATE_FORMAT(tr.data_hora,'%Y%m%d%H%i%s')," +
                    "LPAD(i.id_transferencia_item,20,'0')) " +
                    "FROM transferenciaestoque tr JOIN transferenciaitem i ON i.id_transferencia=tr.id_transferencia " +
                    "WHERE tr.id_local_destino=@id) recebimentos GROUP BY id_produto) r " +
                    "ON r.id_produto=e.id_produto WHERE e.id_local_estoque=@id AND e.quantidade_atual>0 " +
                    "ORDER BY r.primeira_chegada IS NULL,r.primeira_chegada,e.id_estoque";
            }
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idLocalEstoque);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Estoque> resultadoBanco = new List<Estoque>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Estoque BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM estoque WHERE id_estoque=@id", conexaoBanco))
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

        public Estoque BuscarPorLocalEProduto(int idLocal, int idProduto)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM estoque WHERE id_local_estoque=@local AND id_produto=@produto", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@local", idLocal);
                    AcessoDados.AdicionarParametro(comandoBanco, "@produto", idProduto);
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

        public int Cadastrar(Estoque item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO estoque (id_local_estoque,id_produto,quantidade_minima,quantidade_ideal) VALUES (@local,@produto,@minima,@ideal)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool AtualizarQuantidadesAlvo(Estoque item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE estoque SET quantidade_minima=@minima,quantidade_ideal=@ideal WHERE id_estoque=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdEstoque);
                    AcessoDados.AdicionarParametro(comandoBanco, "@minima", item.QuantidadeMinima);
                    AcessoDados.AdicionarParametro(comandoBanco, "@ideal", item.QuantidadeIdeal);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static void Vincular(MySqlCommand comando, Estoque item)
        {
            AcessoDados.AdicionarParametro(comando, "@local", item.IdLocalEstoque);
            AcessoDados.AdicionarParametro(comando, "@produto", item.IdProduto);
            AcessoDados.AdicionarParametro(comando, "@minima", item.QuantidadeMinima);
            AcessoDados.AdicionarParametro(comando, "@ideal", item.QuantidadeIdeal);
        }

        private static Estoque ConverterLinha(MySqlDataReader leitor)
        {
            return new Estoque
            {
                IdEstoque = leitor.GetInt32("id_estoque"),
                IdLocalEstoque = leitor.GetInt32("id_local_estoque"),
                IdProduto = leitor.GetInt32("id_produto"),
                QuantidadeMinima = leitor.GetDecimal("quantidade_minima"),
                QuantidadeIdeal = leitor.GetDecimal("quantidade_ideal"),
                QuantidadeAtual = leitor.GetDecimal("quantidade_atual")
            };
        }
    }
}
