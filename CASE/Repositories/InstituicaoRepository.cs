using System.Collections.Generic;
using System.Data;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class InstituicaoRepository
    {
        public int SalvarUnica(Instituicao item)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (var transacao = conexao.BeginTransaction(IsolationLevel.Serializable))
                {
                    int existente = 0;
                    using (MySqlCommand comando = new MySqlCommand("SELECT id_instituicao FROM instituicao FOR UPDATE", conexao, transacao))
                    using (MySqlDataReader leitor = comando.ExecuteReader())
                    {
                        while (leitor.Read())
                        {
                            if (existente != 0)
                            {
                                throw new InvalidOperationException("Há mais de uma instituição cadastrada. Revise a configuração antes de continuar.");
                            }
                            existente = leitor.GetInt32(0);
                        }
                    }
                    if (existente != item.IdInstituicao)
                    {
                        throw new InvalidOperationException("A configuração institucional mudou. Reabra a tela antes de salvar.");
                    }
                    string sql = "INSERT INTO instituicao(nome,documento,telefone,email) VALUES (@nome,@documento,@telefone,@email)";
                    if (existente > 0)
                    {
                        sql = "UPDATE instituicao SET nome=@nome,documento=@documento,telefone=@telefone,email=@email WHERE id_instituicao=@id";
                    }
                    using (MySqlCommand comando = new MySqlCommand(sql, conexao, transacao))
                    {
                        comando.Parameters.AddWithValue("@id", existente);
                        AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                        AcessoDados.AdicionarTextoOpcional(comando, "@documento", item.Documento);
                        AcessoDados.AdicionarTextoOpcional(comando, "@telefone", item.Telefone);
                        AcessoDados.AdicionarTextoOpcional(comando, "@email", item.Email);
                        comando.ExecuteNonQuery();
                        if (existente == 0)
                        {
                            existente = checked((int)comando.LastInsertedId);
                        }
                    }
                    transacao.Commit();
                    return existente;
                }
            }
        }

        public List<Instituicao> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_instituicao,nome,documento,telefone,email FROM instituicao ORDER BY id_instituicao", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Instituicao> resultadoBanco = new List<Instituicao>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Instituicao BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_instituicao,nome,documento,telefone,email FROM instituicao WHERE id_instituicao=@id", conexaoBanco))
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

        public int Cadastrar(Instituicao item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO instituicao (nome,documento,telefone,email) VALUES (@nome,@documento,@telefone,@email)", conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@documento", item.Documento);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@telefone", item.Telefone);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@email", item.Email);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;

        }

        public bool Atualizar(Instituicao item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE instituicao SET nome=@nome,documento=@documento,telefone=@telefone,email=@email WHERE id_instituicao=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdInstituicao);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@documento", item.Documento);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@telefone", item.Telefone);
                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@email", item.Email);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static Instituicao ConverterLinha(MySqlDataReader leitor)
        {
            return new Instituicao
            {
                IdInstituicao = leitor.GetInt32("id_instituicao"),
                Nome = leitor.GetString("nome"),
                Documento = AcessoDados.LerTextoOpcional(leitor, "documento"),
                Telefone = AcessoDados.LerTextoOpcional(leitor, "telefone"),
                Email = AcessoDados.LerTextoOpcional(leitor, "email")
            };
        }
    }
}
