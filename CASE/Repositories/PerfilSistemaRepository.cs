using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class PerfilSistemaRepository
    {
        public List<PerfilSistema> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_perfil_sistema,nome FROM perfilsistema ORDER BY id_perfil_sistema", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<PerfilSistema> resultadoBanco = new List<PerfilSistema>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public PerfilSistema BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_perfil_sistema,nome FROM perfilsistema WHERE id_perfil_sistema=@id", conexaoBanco))
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

        public int Cadastrar(PerfilSistema item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO perfilsistema (nome) VALUES (@nome)", conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(PerfilSistema item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE perfilsistema SET nome=@nome WHERE id_perfil_sistema=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdPerfilSistema);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static PerfilSistema ConverterLinha(MySqlDataReader leitor)
        {
            return new PerfilSistema
            {
                IdPerfilSistema = leitor.GetInt32("id_perfil_sistema"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
