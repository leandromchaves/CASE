using System;
using System.Collections.Generic;
using CASE.Data;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    internal static class AcessoDados
    {

        internal static List<T> ConsultarLista<T>(
            string sql,
            Func<MySqlDataReader, T> converterLinha,
            Action<MySqlCommand> preencherParametros = null)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    if (preencherParametros != null)
                    {
                        preencherParametros(comando);
                    }

                    using (var leitor = comando.ExecuteReader())
                    {
                        List<T> resultados = new List<T>();

                        while (leitor.Read())
                        {
                            resultados.Add(converterLinha(leitor));
                        }

                        return resultados;
                    }
                }
            }
        }


        internal static T ConsultarUnico<T>(
            string sql,
            Func<MySqlDataReader, T> converterLinha,
            Action<MySqlCommand> preencherParametros = null) where T : class
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    if (preencherParametros != null)
                    {
                        preencherParametros(comando);
                    }

                    using (var leitor = comando.ExecuteReader())
                    {
                        if (leitor.Read())
                        {
                            return converterLinha(leitor);
                        }

                        return null;
                    }
                }
            }
        }


        internal static int Inserir(string sql, Action<MySqlCommand> preencherParametros)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    preencherParametros(comando);
                    comando.ExecuteNonQuery();
                }

                using (var comandoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexao))
                {
                    return Convert.ToInt32(comandoId.ExecuteScalar());
                }
            }
        }

        // Usa a transação aberta pela operação principal.
        internal static int Inserir(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            string sql,
            Action<MySqlCommand> preencherParametros)
        {
            using (var comando = new MySqlCommand(sql, conexao, transacao))
            {
                preencherParametros(comando);
                comando.ExecuteNonQuery();
            }

            using (var comandoId = new MySqlCommand("SELECT LAST_INSERT_ID()", conexao, transacao))
            {
                return Convert.ToInt32(comandoId.ExecuteScalar());
            }
        }


        internal static int Executar(string sql, Action<MySqlCommand> preencherParametros)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    if (preencherParametros != null)
                    {
                        preencherParametros(comando);
                    }

                    return comando.ExecuteNonQuery();
                }
            }
        }

        // Mantém o comando na transação da operação principal.
        internal static int Executar(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            string sql,
            Action<MySqlCommand> preencherParametros)
        {
            using (var comando = new MySqlCommand(sql, conexao, transacao))
            {
                preencherParametros(comando);
                return comando.ExecuteNonQuery();
            }
        }

        // Envia os valores por parâmetros, sem concatená-los ao SQL.

        internal static void AdicionarParametro(MySqlCommand comando, string nome, object valor)
        {
            object valorParametro = valor;

            if (valorParametro == null)
            {
                valorParametro = DBNull.Value;
            }

            comando.Parameters.AddWithValue(nome, valorParametro);
        }

        internal static void AdicionarTextoObrigatorio(
            MySqlCommand comando,
            string nome,
            string valor)
        {
            if (valor == null)
            {
                throw new ArgumentNullException(nome);
            }

            comando.Parameters.AddWithValue(nome, valor);
        }

        // Salva texto vazio como NULL nos campos opcionais.
        internal static void AdicionarTextoOpcional(
            MySqlCommand comando,
            string nome,
            string valor)
        {
            object valorParametro = valor;

            if (string.IsNullOrWhiteSpace(valor))
            {
                valorParametro = DBNull.Value;
            }

            comando.Parameters.AddWithValue(nome, valorParametro);
        }

        internal static string LerTextoOpcional(MySqlDataReader leitor, string coluna)
        {
            int indiceColuna = leitor.GetOrdinal(coluna);

            if (leitor.IsDBNull(indiceColuna))
            {
                return null;
            }

            // MySql.Data pode materializar CHAR(36) como Guid, inclusive chaves de operação.
            return Convert.ToString(leitor.GetValue(indiceColuna));
        }

        internal static int? LerInteiroOpcional(MySqlDataReader leitor, string coluna)
        {
            int indiceColuna = leitor.GetOrdinal(coluna);

            if (leitor.IsDBNull(indiceColuna))
            {
                return null;
            }

            return leitor.GetInt32(indiceColuna);
        }

        internal static decimal? LerDecimalOpcional(MySqlDataReader leitor, string coluna)
        {
            int indiceColuna = leitor.GetOrdinal(coluna);

            if (leitor.IsDBNull(indiceColuna))
            {
                return null;
            }

            return leitor.GetDecimal(indiceColuna);
        }

        internal static DateTime? LerDataHoraOpcional(MySqlDataReader leitor, string coluna)
        {
            int indiceColuna = leitor.GetOrdinal(coluna);

            if (leitor.IsDBNull(indiceColuna))
            {
                return null;
            }

            return leitor.GetDateTime(indiceColuna);
        }

        internal static byte[] LerBytesOpcional(MySqlDataReader leitor, string coluna)
        {
            int indiceColuna = leitor.GetOrdinal(coluna);

            if (leitor.IsDBNull(indiceColuna))
            {
                return null;
            }

            return (byte[])leitor.GetValue(indiceColuna);
        }
    }
}
