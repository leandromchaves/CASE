using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class TipoEventoRepository
    {
        public List<TipoEvento> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_tipo_evento,nome FROM tipoevento ORDER BY id_tipo_evento", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<TipoEvento> resultadoBanco = new List<TipoEvento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public TipoEvento BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_tipo_evento,nome FROM tipoevento WHERE id_tipo_evento=@id", conexaoBanco))
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

        public int Cadastrar(TipoEvento item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO tipoevento (nome) VALUES (@nome)", conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(TipoEvento item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE tipoevento SET nome=@nome WHERE id_tipo_evento=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdTipoEvento);
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
                using (MySqlCommand comandoBanco = new MySqlCommand("DELETE FROM tipoevento WHERE id_tipo_evento=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", id);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco == 1;
        }

        private static TipoEvento ConverterLinha(MySqlDataReader leitor)
        {
            return new TipoEvento
            {
                IdTipoEvento = leitor.GetInt32("id_tipo_evento"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
