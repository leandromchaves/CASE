using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EscalaRepository
    {
        private const string Colunas = "id_escala,id_equipe,id_jornada,entrada_prevista,saida_prevista,situacao";

        public List<Escala> ListarPorJornada(int idJornada)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM escala WHERE id_jornada=@id ORDER BY entrada_prevista,id_escala", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idJornada);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Escala> resultadoBanco = new List<Escala>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<Escala> ListarPorEquipe(int idEquipe)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM escala WHERE id_equipe=@id ORDER BY id_jornada,entrada_prevista", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEquipe);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Escala> resultadoBanco = new List<Escala>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<Escala> ListarPorEvento(int idEvento)
        {
            const string sql = @"SELECT e.id_escala,e.id_equipe,e.id_jornada,
                    e.entrada_prevista,e.saida_prevista,e.situacao
                FROM escala e
                INNER JOIN equipeevento ee ON ee.id_equipe=e.id_equipe
                INNER JOIN jornadasetor js ON js.id_jornada=e.id_jornada
                INNER JOIN setor s ON s.id_setor=js.id_setor
                WHERE ee.id_evento=@evento AND s.id_evento=@evento
                ORDER BY js.`data`,e.entrada_prevista,e.id_escala";

            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Escala> resultadoBanco = new List<Escala>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Escala BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM escala WHERE id_escala=@id", conexaoBanco))
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

        public int Cadastrar(Escala item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO escala (id_equipe,id_jornada,entrada_prevista,saida_prevista,situacao) VALUES (@equipe,@jornada,@entrada,@saida,@situacao)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(Escala item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE escala SET id_equipe=@equipe,id_jornada=@jornada,entrada_prevista=@entrada,saida_prevista=@saida,situacao=@situacao WHERE id_escala=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdEscala);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        public bool RemoverSeSemHistorico(int idEscala, out bool possuiHistorico)
        {
            possuiHistorico = false;
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT id_escala FROM escala WHERE id_escala=@id FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEscala);
                            if (comando.ExecuteScalar() == null)
                            {
                                transacao.Commit();
                                return false;
                            }
                        }

                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT EXISTS(SELECT 1 FROM registro WHERE id_escala=@id) " +
                            "OR EXISTS(SELECT 1 FROM solicitacao WHERE id_escala=@id)",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEscala);
                            possuiHistorico = Convert.ToInt32(comando.ExecuteScalar()) > 0;
                        }

                        if (possuiHistorico)
                        {
                            transacao.Commit();
                            return false;
                        }

                        using (MySqlCommand comando = new MySqlCommand(
                            "DELETE FROM escala WHERE id_escala=@id",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEscala);
                            bool removida = comando.ExecuteNonQuery() > 0;
                            transacao.Commit();
                            return removida;
                        }
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public int SalvarValidandoConflito(
            Escala item,
            int idPessoa,
            int? idEscalaExistente)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        int idPessoaAnterior = idPessoa;

                        if (idEscalaExistente.HasValue)
                        {
                            idPessoaAnterior = BuscarPessoaDaEscala(
                                conexao,
                                transacao,
                                idEscalaExistente.Value);
                        }

                        int primeiroId = idPessoaAnterior;
                        int segundoId = idPessoa;

                        if (primeiroId > segundoId)
                        {
                            primeiroId = idPessoa;
                            segundoId = idPessoaAnterior;
                        }

                        BloquearPessoa(conexao, transacao, primeiroId);

                        if (segundoId != primeiroId)
                        {
                            BloquearPessoa(conexao, transacao, segundoId);
                        }

                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT id_escala FROM escala WHERE id_equipe=@equipe " +
                            "AND id_jornada=@jornada AND (@id IS NULL OR id_escala<>@id) FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@equipe", item.IdEquipe);
                            comando.Parameters.AddWithValue("@jornada", item.IdJornada);
                            object idIgnorado = DBNull.Value;
                            if (idEscalaExistente.HasValue)
                            {
                                idIgnorado = idEscalaExistente.Value;
                            }
                            comando.Parameters.AddWithValue("@id", idIgnorado);
                            if (comando.ExecuteScalar() != null)
                            {
                                throw new InvalidOperationException(
                                    "O membro já possui escala neste setor, dia e turno. " +
                                    "Edite a escala existente ou selecione outro turno.");
                            }
                        }

                        if (ExisteConflito(conexao, transacao, item, idPessoa, idEscalaExistente))
                        {
                            throw new InvalidOperationException(
                                "O membro já possui uma escala sobreposta neste dia.");
                        }

                        int idSalvo;

                        if (idEscalaExistente.HasValue)
                        {
                            int resultadoBanco6;
                            using (MySqlCommand comandoBanco6 = new MySqlCommand("UPDATE escala SET id_equipe=@equipe,id_jornada=@jornada,entrada_prevista=@entrada,saida_prevista=@saida,situacao=@situacao WHERE id_escala=@id", conexao, transacao))
                            {
                                AcessoDados.AdicionarParametro(comandoBanco6, "@id", idEscalaExistente.Value);
                                Vincular(comandoBanco6, item);
                                resultadoBanco6 = comandoBanco6.ExecuteNonQuery();
                            }
                            int linhasAfetadas = resultadoBanco6;

                            if (linhasAfetadas == 0)
                            {
                                throw new InvalidOperationException(
                                    "A escala não foi atualizada. Atualize a lista e tente novamente.");
                            }

                            idSalvo = idEscalaExistente.Value;
                        }
                        else
                        {
                            int resultadoBanco7;
                            using (MySqlCommand comandoBanco7 = new MySqlCommand("INSERT INTO escala (id_equipe,id_jornada,entrada_prevista,saida_prevista,situacao) VALUES (@equipe,@jornada,@entrada,@saida,@situacao)", conexao, transacao))
                            {
                                Vincular(comandoBanco7, item);
                                comandoBanco7.ExecuteNonQuery();
                                resultadoBanco7 = Convert.ToInt32(comandoBanco7.LastInsertedId);
                            }
                            idSalvo = resultadoBanco7;
                        }

                        transacao.Commit();
                        return idSalvo;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        private static int BuscarPessoaDaEscala(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEscala)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ee.id_pessoa FROM escala e INNER JOIN equipeevento ee ON ee.id_equipe=e.id_equipe WHERE e.id_escala=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEscala);
                object resultado = comando.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException("A escala não foi encontrada.");
                }

                return Convert.ToInt32(resultado);
            }
        }

        private static void BloquearPessoa(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idPessoa)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_pessoa FROM pessoa WHERE id_pessoa=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idPessoa);
                object resultado = comando.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException("O membro da escala não foi encontrado.");
                }
            }
        }

        private static bool ExisteConflito(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            Escala item,
            int idPessoa,
            int? idEscalaExistente)
        {
            const string sql = @"SELECT COUNT(*)
                FROM escala e
                INNER JOIN equipeevento ee ON ee.id_equipe=e.id_equipe
                INNER JOIN jornadasetor js ON js.id_jornada=e.id_jornada
                WHERE ee.id_pessoa=@pessoa
                  AND js.`data`=(SELECT `data` FROM jornadasetor WHERE id_jornada=@jornada)
                  AND LOWER(e.situacao)<>'cancelada'
                  AND e.entrada_prevista<@saida
                  AND e.saida_prevista>@entrada
                  AND (@escala IS NULL OR e.id_escala<>@escala)";

            using (MySqlCommand comando = new MySqlCommand(sql, conexao, transacao))
            {
                comando.Parameters.AddWithValue("@pessoa", idPessoa);
                comando.Parameters.AddWithValue("@jornada", item.IdJornada);
                comando.Parameters.AddWithValue("@entrada", item.EntradaPrevista);
                comando.Parameters.AddWithValue("@saida", item.SaidaPrevista);

                if (idEscalaExistente.HasValue)
                {
                    comando.Parameters.AddWithValue("@escala", idEscalaExistente.Value);
                }
                else
                {
                    comando.Parameters.AddWithValue("@escala", DBNull.Value);
                }

                return Convert.ToInt32(comando.ExecuteScalar()) > 0;
            }
        }

        private static void Vincular(MySqlCommand comando, Escala item)
        {
            AcessoDados.AdicionarParametro(comando, "@equipe", item.IdEquipe);
            AcessoDados.AdicionarParametro(comando, "@jornada", item.IdJornada);
            AcessoDados.AdicionarParametro(comando, "@entrada", item.EntradaPrevista);
            AcessoDados.AdicionarParametro(comando, "@saida", item.SaidaPrevista);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", item.Situacao);
        }

        private static Escala ConverterLinha(MySqlDataReader leitor)
        {
            return new Escala
            {
                IdEscala = leitor.GetInt32("id_escala"),
                IdEquipe = leitor.GetInt32("id_equipe"),
                IdJornada = leitor.GetInt32("id_jornada"),
                EntradaPrevista = (TimeSpan)leitor.GetValue(leitor.GetOrdinal("entrada_prevista")),
                SaidaPrevista = (TimeSpan)leitor.GetValue(leitor.GetOrdinal("saida_prevista")),
                Situacao = leitor.GetString("situacao")
            };
        }
    }
}
