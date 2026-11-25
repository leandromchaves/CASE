using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class PerfilRepository
    {
        public List<Perfil> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_perfil,nome FROM perfil ORDER BY id_perfil", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Perfil> resultadoBanco = new List<Perfil>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Perfil BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_perfil,nome FROM perfil WHERE id_perfil=@id", conexaoBanco))
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

        public int Cadastrar(Perfil item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO perfil (nome) VALUES (@nome)", conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(Perfil item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE perfil SET nome=@nome WHERE id_perfil=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdPerfil);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        public bool Excluir(int id)
        {
            new AutorizacaoService().ExigirAdministrador(SessaoUsuario.ObterUsuarioAtual());
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("DELETE FROM perfil WHERE id_perfil=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", id);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco == 1;
        }

        private static Perfil ConverterLinha(MySqlDataReader leitor)
        {
            return new Perfil
            {
                IdPerfil = leitor.GetInt32("id_perfil"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
