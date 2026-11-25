using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class AnaliseUsuarioRepository
    {
        public List<AnaliseUsuario> ListarPorUsuario(int idUsuario)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_analise,id_usuario,id_analisador,decisao,data_hora,justificativa FROM analiseusuario WHERE id_usuario=@id ORDER BY data_hora,id_analise", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idUsuario);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<AnaliseUsuario> resultadoBanco = new List<AnaliseUsuario>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public int Registrar(AnaliseUsuario analise)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO analiseusuario (id_usuario,id_analisador,decisao,data_hora,justificativa) VALUES (@usuario,@analisador,@decisao,@data,@justificativa)", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@usuario", analise.IdUsuario);
                    AcessoDados.AdicionarParametro(comandoBanco, "@analisador", analise.IdAnalisador);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@decisao", analise.Decisao);
                    AcessoDados.AdicionarParametro(comandoBanco, "@data", analise.DataHora);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@justificativa", analise.Justificativa);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;

        }

        private static AnaliseUsuario ConverterLinha(MySqlDataReader leitor)
        {
            return new AnaliseUsuario
            {
                IdAnalise = leitor.GetInt32("id_analise"),
                IdUsuario = leitor.GetInt32("id_usuario"),
                IdAnalisador = leitor.GetInt32("id_analisador"),
                Decisao = leitor.GetString("decisao"),
                DataHora = leitor.GetDateTime("data_hora"),
                Justificativa = AcessoDados.LerTextoOpcional(leitor, "justificativa")
            };
        }
    }
}
