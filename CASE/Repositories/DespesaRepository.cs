using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class DespesaRepository
    {
        private const string Colunas = "id_despesa,id_evento,id_fornecedor,descricao,`data`,valor,id_usuario_responsavel";

        public List<Despesa> ListarPorEvento(int idEvento)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM despesa WHERE id_evento=@id ORDER BY `data` DESC,id_despesa DESC", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Despesa> resultadoBanco = new List<Despesa>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Despesa BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM despesa WHERE id_despesa=@id", conexaoBanco))
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

        public int Cadastrar(Despesa item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO despesa (id_evento,id_fornecedor,descricao,`data`,valor,id_usuario_responsavel) VALUES (@evento,@fornecedor,@descricao,@data,@valor,@usuario)", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", item.IdEvento);
                    AcessoDados.AdicionarParametro(comandoBanco, "@fornecedor", item.IdFornecedor);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@descricao", item.Descricao);
                    AcessoDados.AdicionarParametro(comandoBanco, "@data", item.Data);
                    AcessoDados.AdicionarParametro(comandoBanco, "@valor", item.Valor);
                    AcessoDados.AdicionarParametro(comandoBanco, "@usuario", item.IdUsuarioResponsavel);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;

        }

        private static Despesa ConverterLinha(MySqlDataReader leitor)
        {
            return new Despesa
            {
                IdDespesa = leitor.GetInt32("id_despesa"),
                IdEvento = leitor.GetInt32("id_evento"),
                IdFornecedor = AcessoDados.LerInteiroOpcional(leitor, "id_fornecedor"),
                Descricao = leitor.GetString("descricao"),
                Data = leitor.GetDateTime("data"),
                Valor = leitor.GetDecimal("valor"),
                IdUsuarioResponsavel = leitor.GetInt32("id_usuario_responsavel")
            };
        }
    }
}
