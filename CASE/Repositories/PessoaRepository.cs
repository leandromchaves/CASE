using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class PessoaRepository
    {
        private const string Colunas = "id_pessoa, nome, telefone, email";

        public List<Pessoa> Listar()
        {
            string sql = "SELECT " + Colunas + " FROM pessoa ORDER BY id_pessoa";
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Pessoa> resultadoBanco = new List<Pessoa>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Pessoa BuscarPorId(int id)
        {
            string sql = "SELECT " + Colunas + " FROM pessoa WHERE id_pessoa = @id";
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
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

        public int Cadastrar(Pessoa pessoa)
        {
            string sql = "INSERT INTO pessoa (nome, telefone, email) VALUES (@nome, @telefone, @email)";
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", pessoa.Nome);
                                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@telefone", pessoa.Telefone);
                                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@email", pessoa.Email);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(Pessoa pessoa)
        {
            string sql = "UPDATE pessoa SET nome = @nome, telefone = @telefone, email = @email WHERE id_pessoa = @id";
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", pessoa.IdPessoa);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", pessoa.Nome);
                                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@telefone", pessoa.Telefone);
                                    AcessoDados.AdicionarTextoOpcional(comandoBanco, "@email", pessoa.Email);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;
        }

        public byte[] BuscarFotoPerfil(int idPessoa)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                string sql = "SELECT foto_perfil FROM pessoa WHERE id_pessoa = @idPessoa";

                using (MySqlCommand comando = new MySqlCommand(sql, conexao))
                {
                    AcessoDados.AdicionarParametro(comando, "@idPessoa", idPessoa);

                    object resultado = comando.ExecuteScalar();

                    if (resultado == null || resultado == DBNull.Value)
                    {
                        return null;
                    }

                    return (byte[])resultado;
                }
            }
        }

        public void AtualizarFotoPerfil(int idPessoa, byte[] fotoPerfil)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                string sql = "UPDATE pessoa SET foto_perfil = @fotoPerfil " +
                    "WHERE id_pessoa = @idPessoa";

                using (MySqlCommand comando = new MySqlCommand(sql, conexao))
                {
                    AcessoDados.AdicionarParametro(comando, "@idPessoa", idPessoa);

                    MySqlParameter parametroFoto = comando.Parameters.Add(
                        "@fotoPerfil",
                        MySqlDbType.MediumBlob);

                    if (fotoPerfil == null)
                    {
                        parametroFoto.Value = DBNull.Value;
                    }
                    else
                    {
                        parametroFoto.Value = fotoPerfil;
                    }

                    comando.ExecuteNonQuery();
                }
            }
        }

        private static Pessoa ConverterLinha(MySqlDataReader leitor)
        {
            return new Pessoa
            {
                IdPessoa = leitor.GetInt32("id_pessoa"),
                Nome = leitor.GetString("nome"),
                Telefone = AcessoDados.LerTextoOpcional(leitor, "telefone"),
                Email = AcessoDados.LerTextoOpcional(leitor, "email")
            };
        }
    }
}
