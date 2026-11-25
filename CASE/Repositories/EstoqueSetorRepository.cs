using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EstoqueSetorRepository
    {
        public List<EstoqueSetor> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_setor,id_local_estoque,id_evento FROM estoquesetor ORDER BY id_setor", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<EstoqueSetor> resultadoBanco = new List<EstoqueSetor>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public EstoqueSetor BuscarPorSetor(int idSetor)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_setor,id_local_estoque,id_evento FROM estoquesetor WHERE id_setor=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idSetor);
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

        public void Associar(EstoqueSetor item)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarLocalDeBarraca(conexao, transacao, item);
                        int resultadoBanco;
                        using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO estoquesetor (id_setor,id_local_estoque,id_evento) " +
                            "VALUES (@setor,@local,@evento)", conexao, transacao))
                        {
                            Vincular(comandoBanco, item);
                            resultadoBanco = comandoBanco.ExecuteNonQuery();
                        }

                        transacao.Commit();
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool AtualizarLocal(EstoqueSetor item)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarLocalDeBarraca(conexao, transacao, item);
                        int resultadoBanco;
                        using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE estoquesetor SET id_local_estoque=@local,id_evento=@evento " +
                            "WHERE id_setor=@setor", conexao, transacao))
                        {
                            Vincular(comandoBanco, item);
                            resultadoBanco = comandoBanco.ExecuteNonQuery();
                        }
                        int linhasAfetadas = resultadoBanco;
                        transacao.Commit();
                        return linhasAfetadas > 0;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }

        }

        private static void Vincular(MySqlCommand comando, EstoqueSetor item)
        {
            AcessoDados.AdicionarParametro(comando, "@setor", item.IdSetor);
            AcessoDados.AdicionarParametro(comando, "@local", item.IdLocalEstoque);
            AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
        }

        private static void ValidarLocalDeBarraca(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            EstoqueSetor item)
        {
            if (item == null || item.IdSetor <= 0 || item.IdLocalEstoque <= 0 || item.IdEvento <= 0)
            {
                throw new ArgumentException("Os dados da associação do estoque são inválidos.", nameof(item));
            }

            using (MySqlCommand comando = new MySqlCommand(
                "SELECT s.id_setor FROM setor s " +
                "JOIN evento ev ON ev.id_evento=s.id_evento " +
                "JOIN vw_localestoque le ON le.id_local_estoque=@local " +
                "WHERE s.id_setor=@setor AND s.id_evento=@evento " +
                "AND ev.id_evento=@evento AND ev.id_instituicao=le.id_instituicao " +
                "AND le.id_evento=@evento AND le.eh_central=0 " +
                "AND le.eh_geral_evento=0 AND le.ativo=1 FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@setor", item.IdSetor);
                AcessoDados.AdicionarParametro(comando, "@local", item.IdLocalEstoque);
                AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);

                if (comando.ExecuteScalar() == null)
                {
                    throw new InvalidOperationException(
                        "O estoque associado precisa ser uma barraca ativa do mesmo evento e instituição.");
                }
            }
        }

        private static EstoqueSetor ConverterLinha(MySqlDataReader leitor)
        {
            return new EstoqueSetor
            {
                IdSetor = leitor.GetInt32("id_setor"),
                IdLocalEstoque = leitor.GetInt32("id_local_estoque"),
                IdEvento = leitor.GetInt32("id_evento")
            };
        }
    }
}

