using System;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    internal static class AcessoDados
    {
        internal static void AdicionarParametro(MySqlCommand comando, string nome, object valor)
        {
            object valorParametro = valor;
            if (valorParametro == null)
            {
                valorParametro = DBNull.Value;
            }

            comando.Parameters.AddWithValue(nome, valorParametro);
        }

        internal static void AdicionarTextoObrigatorio(MySqlCommand comando, string nome, string valor)
        {
            if (valor == null)
            {
                throw new ArgumentNullException(nome);
            }

            comando.Parameters.AddWithValue(nome, valor);
        }

        // Salva texto vazio como NULL nos campos opcionais.
        internal static void AdicionarTextoOpcional(MySqlCommand comando, string nome, string valor)
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
