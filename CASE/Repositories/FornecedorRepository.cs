
using System;
using System.Collections.Generic;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class FornecedorRepository
    {
        public List<Fornecedor> Listar()
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                ValidarAcesso(conexao, null, false);

                string sql = "SELECT id_fornecedor, nome, documento, telefone, email FROM fornecedor";

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    using (var leitor = comando.ExecuteReader())
                    {
                        List<Fornecedor> fornecedores = new List<Fornecedor>();

                        while (leitor.Read())
                        {
                            Fornecedor fornecedor = new Fornecedor();

                            fornecedor.IdFornecedor = Convert.ToInt32(leitor["id_fornecedor"]);
                            fornecedor.Nome = leitor["nome"].ToString();
                            fornecedor.Documento = AcessoDados.LerTextoOpcional(leitor, "documento");
                            fornecedor.Telefone = AcessoDados.LerTextoOpcional(leitor, "telefone");
                            fornecedor.Email = AcessoDados.LerTextoOpcional(leitor, "email");

                            fornecedores.Add(fornecedor);
                        }

                        return fornecedores;
                    }
                }
            }
        }

        public void Cadastrar(Fornecedor fornecedor)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAcesso(conexao, transacao, false);
                        using (var comando = new MySqlCommand(
                            "INSERT INTO fornecedor (nome, documento, telefone, email) VALUES (@nome, @documento, @telefone, @email)",
conexao,
transacao))
                        {
                            AdicionarCampos(comando, fornecedor);
                            comando.ExecuteNonQuery();
                            fornecedor.IdFornecedor = checked((int)comando.LastInsertedId);
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

        public Fornecedor BuscarPorId(int id)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                ValidarAcesso(conexao, null, false);

                string sql = "SELECT id_fornecedor, nome, documento, telefone, email FROM fornecedor WHERE id_fornecedor = @id";

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    comando.Parameters.AddWithValue("@id", id);

                    using (var leitor = comando.ExecuteReader())
                    {
                        if (leitor.Read())
                        {
                            Fornecedor fornecedor = new Fornecedor();

                            fornecedor.IdFornecedor = Convert.ToInt32(leitor["id_fornecedor"]);
                            fornecedor.Nome = leitor["nome"].ToString();
                            fornecedor.Documento = AcessoDados.LerTextoOpcional(leitor, "documento");
                            fornecedor.Telefone = AcessoDados.LerTextoOpcional(leitor, "telefone");
                            fornecedor.Email = AcessoDados.LerTextoOpcional(leitor, "email");

                            return fornecedor;
                        }

                        return null;
                    }
                }
            }
        }

        public void Atualizar(Fornecedor fornecedor)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAcesso(conexao, transacao, false);
                        using (var comando = new MySqlCommand("SELECT id_fornecedor FROM fornecedor WHERE id_fornecedor=@id FOR UPDATE", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@id", fornecedor.IdFornecedor);
                            if (comando.ExecuteScalar() == null)
                            {
                                throw new InvalidOperationException("O fornecedor não existe mais. Atualize a lista antes de continuar.");
                            }
                            comando.CommandText = "UPDATE fornecedor SET nome=@nome,documento=@documento,telefone=@telefone,email=@email WHERE id_fornecedor=@id";
                            AdicionarCampos(comando, fornecedor);
                            comando.ExecuteNonQuery();
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

        public bool Excluir(int id)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAcesso(conexao, transacao, true);
                        int linhasAfetadas;
                        using (var comando = new MySqlCommand("DELETE FROM fornecedor WHERE id_fornecedor=@id", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@id", id);
                            linhasAfetadas = comando.ExecuteNonQuery();
                        }
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

        private static void AdicionarCampos(MySqlCommand comando, Fornecedor fornecedor)
        {
            AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", fornecedor.Nome);
            AcessoDados.AdicionarTextoOpcional(comando, "@documento", fornecedor.Documento);
            AcessoDados.AdicionarTextoOpcional(comando, "@telefone", fornecedor.Telefone);
            AcessoDados.AdicionarTextoOpcional(comando, "@email", fornecedor.Email);
        }

        internal static void ValidarAcesso(MySqlConnection conexao, MySqlTransaction transacao, bool somenteAdministrador)
        {
            Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
            string sql = "SELECT p.nome FROM usuario u JOIN perfilsistema p ON p.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE u.id_usuario=@usuario AND u.id_pessoa=@pessoa AND u.id_perfil_sistema=@perfil AND u.situacao_conta='aprovado'";
            if (transacao != null)
            {
                sql += " FOR UPDATE";
            }
            using (var comando = new MySqlCommand(sql, conexao, transacao))
            {
                comando.Parameters.AddWithValue("@usuario", sessao.IdUsuario);
                comando.Parameters.AddWithValue("@pessoa", sessao.IdPessoa);
                comando.Parameters.AddWithValue("@perfil", sessao.IdPerfilSistema);
                string perfil = Convert.ToString(comando.ExecuteScalar());
                if (string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (!somenteAdministrador && string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                throw new UnauthorizedAccessException("Sua conta não está autorizada a executar esta operação com fornecedores.");
            }
        }
    }
}
