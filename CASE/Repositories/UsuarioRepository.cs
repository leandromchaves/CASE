using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class UsuarioRepository
    {
        private const string Colunas = "id_usuario, id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta, solicitado_em";
        private const string ColunasSemHash = "id_usuario, id_pessoa, id_perfil_sistema, login, situacao_conta, solicitado_em";

        internal bool AlterarSenhaDaSessao(string hashAtual, string novoHash)
        {
            var sessao = SessaoUsuario.ObterUsuarioAtual();
            // A condição no hash impede sobrescrever outra troca de senha concorrente.
            const string sql = "UPDATE usuario SET senha_hash=@novoHash " +
                "WHERE id_usuario=@usuario AND id_pessoa=@pessoa AND id_perfil_sistema=@perfil " +
                "AND LOWER(situacao_conta)='aprovado' AND BINARY senha_hash=BINARY @hashAtual " +
                "AND EXISTS (SELECT 1 FROM perfilsistema p WHERE p.id_perfil_sistema=@perfil " +
                "AND LOWER(p.nome) IN ('administrador','gerente','membro'))";
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@usuario", sessao.IdUsuario);
                                    AcessoDados.AdicionarParametro(comandoBanco, "@pessoa", sessao.IdPessoa);
                                    AcessoDados.AdicionarParametro(comandoBanco, "@perfil", sessao.IdPerfilSistema);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@hashAtual", hashAtual);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@novoHash", novoHash);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco == 1;
        }

        public List<Usuario> Listar()
        {
            string sql = "SELECT " + ColunasSemHash + " FROM usuario ORDER BY id_usuario";
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Usuario> resultadoBanco = new List<Usuario>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinhaSemHash(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Usuario BuscarPorId(int id)
        {
            string sql = "SELECT " + ColunasSemHash + " FROM usuario WHERE id_usuario = @id";
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
                            return ConverterLinhaSemHash(leitorBanco);
                        }
                        return null;
                    }
                }
            }
        }

        public Usuario BuscarPorLogin(string login)
        {
            string sql = "SELECT " + Colunas + " FROM usuario WHERE login = @login";
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@login", login);
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

        public string BuscarLoginPorPessoa(int idPessoa)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                string sql = "SELECT login FROM usuario WHERE id_pessoa = @pessoa";

                using (MySqlCommand comando = new MySqlCommand(sql, conexao))
                {
                    AcessoDados.AdicionarParametro(comando, "@pessoa", idPessoa);

                    object resultado = comando.ExecuteScalar();

                    if (resultado == null || resultado == DBNull.Value)
                    {
                        return string.Empty;
                    }

                    return resultado.ToString();
                }
            }
        }

        public void RegistrarDecisaoCadastro(
            int idUsuario,
            int idAnalisador,
            int? idPerfilSistema,
            string situacao,
            string justificativa)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAdministrador(
                            conexao,
                            transacao,
                            idAnalisador);
                        ValidarCadastroPendente(
                            conexao,
                            transacao,
                            idUsuario);

                        string sqlAtualizacao;

                        if (string.Equals(
                            situacao,
                            SituacoesUsuario.Aprovado,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            sqlAtualizacao = "UPDATE usuario SET situacao_conta=@situacao, " +
                                "id_perfil_sistema=@perfil " +
                                "WHERE id_usuario=@id AND LOWER(situacao_conta)='pendente'";
                        }
                        else
                        {
                            sqlAtualizacao = "UPDATE usuario SET situacao_conta=@situacao " +
                                "WHERE id_usuario=@id AND LOWER(situacao_conta)='pendente'";
                        }

                        using (MySqlCommand comando = new MySqlCommand(sqlAtualizacao, conexao, transacao))
                        {
                            AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", situacao);
                            AcessoDados.AdicionarParametro(comando, "@id", idUsuario);

                            if (string.Equals(
                                situacao,
                                SituacoesUsuario.Aprovado,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                AcessoDados.AdicionarParametro(comando, "@perfil", idPerfilSistema);
                            }

                            if (comando.ExecuteNonQuery() != 1)
                            {
                                throw new InvalidOperationException(
                                    "O cadastro foi analisado por outra pessoa. Atualize a lista.");
                            }
                        }

                        using (MySqlCommand comando = new MySqlCommand(
                            "INSERT INTO analiseusuario " +
                            "(id_usuario,id_analisador,decisao,data_hora,justificativa) " +
                            "VALUES (@usuario,@analisador,@decisao,NOW(),@justificativa)",
                            conexao,
                            transacao))
                        {
                            AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);
                            AcessoDados.AdicionarParametro(comando, "@analisador", idAnalisador);
                            AcessoDados.AdicionarTextoObrigatorio(comando, "@decisao", situacao);
                            AcessoDados.AdicionarTextoOpcional(
                                comando,
                                "@justificativa",
                                justificativa);
                            comando.ExecuteNonQuery();
                        }

                        transacao.Commit();
                    }
                    catch
                    {
                        try
                        {
                            transacao.Rollback();
                        }
                        catch
                        {
                            // Preserva o erro original se o rollback também falhar.
                        }

                        throw;
                    }
                }
            }
        }

        private static void ValidarAdministrador(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idAnalisador)
        {
            Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
            if (sessao.IdUsuario != idAnalisador)
                throw new UnauthorizedAccessException("O responsável deve corresponder à sessão autenticada.");
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT u.id_usuario FROM usuario u " +
                "JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE u.id_usuario=@id AND LOWER(u.situacao_conta)='aprovado' " +
                "AND LOWER(ps.nome)='administrador' AND u.id_perfil_sistema=@perfilSessao " +
                "AND u.id_pessoa=@pessoaSessao FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", idAnalisador);
                AcessoDados.AdicionarParametro(comando, "@perfilSessao", sessao.IdPerfilSistema);
                AcessoDados.AdicionarParametro(comando, "@pessoaSessao", sessao.IdPessoa);

                if (comando.ExecuteScalar() == null)
                {
                    throw new UnauthorizedAccessException(
                        "A decisão exige uma conta de administrador aprovada.");
                }
            }
        }

        public void AlterarPerfilAdministrativo(int idUsuario, int idAnalisador,
            int idPerfilSistema, int idPerfilAnterior, string justificativa)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAdministrador(conexao, transacao, idAnalisador);
                        if (idUsuario == idAnalisador)
                            throw new InvalidOperationException("Use outra conta de administrador para alterar seu perfil.");
                        string nomeAnterior;
                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT u.id_perfil_sistema,u.situacao_conta,p.nome FROM usuario u " +
                            "LEFT JOIN perfilsistema p ON p.id_perfil_sistema=u.id_perfil_sistema " +
                            "WHERE u.id_usuario=@usuario FOR UPDATE", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            using (MySqlDataReader leitor = comando.ExecuteReader())
                            {
                                if (!leitor.Read() || leitor.IsDBNull(0) ||
                                    !string.Equals(leitor.GetString(1), SituacoesUsuario.Aprovado, StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("Somente contas aprovadas podem ter o perfil alterado.");
                                if (leitor.GetInt32(0) != idPerfilAnterior)
                                    throw new InvalidOperationException("O perfil foi alterado por outra pessoa. Atualize a lista.");
                                nomeAnterior = leitor.GetString(2);
                            }
                        }
                        string novoNome;
                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT nome FROM perfilsistema WHERE id_perfil_sistema=@perfil", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@perfil", idPerfilSistema);
                            novoNome = Convert.ToString(comando.ExecuteScalar());
                        }
                        if (novoNome != "Administrador" && novoNome != "Gerente" && novoNome != "Membro")
                            throw new ArgumentException("O perfil selecionado não é válido.");
                        if (idPerfilSistema == idPerfilAnterior)
                            throw new ArgumentException("Escolha um perfil diferente do atual.");
                        if (justificativa != null && justificativa.Trim().Length > 500)
                            throw new ArgumentException("A justificativa deve ter até 500 caracteres.");
                        using (MySqlCommand comando = new MySqlCommand(
                            "UPDATE usuario SET id_perfil_sistema=@perfil WHERE id_usuario=@usuario " +
                            "AND id_perfil_sistema=@anterior AND LOWER(situacao_conta)='aprovado'", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@perfil", idPerfilSistema);
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            comando.Parameters.AddWithValue("@anterior", idPerfilAnterior);
                            if (comando.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("A conta foi alterada. Atualize a lista.");
                        }
                        using (MySqlCommand comando = new MySqlCommand(
                            "INSERT INTO analiseusuario (id_usuario,id_analisador,decisao,data_hora,justificativa) " +
                            "VALUES (@usuario,@analisador,'perfil_alterado',NOW(),@motivo)", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            comando.Parameters.AddWithValue("@analisador", idAnalisador);
                            comando.Parameters.AddWithValue("@motivo", "Perfil geral alterado de " + nomeAnterior +
                                " para " + novoNome + ". " + (justificativa ?? string.Empty).Trim());
                            comando.ExecuteNonQuery();
                        }
                        transacao.Commit();
                    }
                    catch
                    {
                        try { transacao.Rollback(); }
                        catch { }
                        throw;
                    }
                }
            }
        }

        private static void ValidarCadastroPendente(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idUsuario)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_usuario FROM usuario " +
                "WHERE id_usuario=@id AND LOWER(situacao_conta)='pendente' FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", idUsuario);

                if (comando.ExecuteScalar() == null)
                {
                    throw new InvalidOperationException(
                        "Somente cadastros pendentes podem receber uma decisão.");
                }
            }
        }

        public int Cadastrar(Usuario usuario)
        {
            string sql = "INSERT INTO usuario (id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta) " +
                "VALUES (@pessoa, @perfil, @login, @hash, @situacao)";

            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@pessoa", usuario.IdPessoa);
                                    AcessoDados.AdicionarParametro(comandoBanco, "@perfil", usuario.IdPerfilSistema);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@login", usuario.Login);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@hash", usuario.SenhaHash);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@situacao", usuario.SituacaoConta);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public int CadastrarSolicitacao(Pessoa pessoa, Usuario usuario)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string sqlPessoa = "INSERT INTO pessoa " +
                            "(nome, telefone, email, foto_perfil) " +
                            "VALUES (@nome, @telefone, @email, @fotoPerfil)";

                        int resultadoBanco5;
                        using (MySqlCommand comandoBanco5 = new MySqlCommand(sqlPessoa, conexao, transacao))
                        {
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco5, "@nome", pessoa.Nome);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@telefone", pessoa.Telefone);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@email", pessoa.Email);

                            MySqlParameter parametroFoto = comandoBanco5.Parameters.Add(
                            "@fotoPerfil",
                            MySqlDbType.MediumBlob);

                            if (pessoa.FotoPerfil == null)
                            {
                                parametroFoto.Value = DBNull.Value;
                            }
                            else
                            {
                                parametroFoto.Value = pessoa.FotoPerfil;
                            }
                            comandoBanco5.ExecuteNonQuery();
                            resultadoBanco5 = Convert.ToInt32(comandoBanco5.LastInsertedId);
                        }
                        int idPessoa = resultadoBanco5;

                        string sqlUsuario = "INSERT INTO usuario " +
                            "(id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta) " +
                            "VALUES (@pessoa, @perfil, @login, @hash, @situacao)";

                        int resultadoBanco6;
                        using (MySqlCommand comandoBanco6 = new MySqlCommand(sqlUsuario, conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco6, "@pessoa", idPessoa);
                            AcessoDados.AdicionarParametro(comandoBanco6, "@perfil", usuario.IdPerfilSistema);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco6, "@login", usuario.Login);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco6, "@hash", usuario.SenhaHash);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco6, "@situacao", usuario.SituacaoConta);
                            comandoBanco6.ExecuteNonQuery();
                            resultadoBanco6 = Convert.ToInt32(comandoBanco6.LastInsertedId);
                        }
                        int idUsuario = resultadoBanco6;

                        transacao.Commit();

                        pessoa.IdPessoa = idPessoa;
                        usuario.IdPessoa = idPessoa;
                        usuario.IdUsuario = idUsuario;

                        return idUsuario;
                    }
                    catch
                    {
                        try
                        {
                            transacao.Rollback();
                        }
                        catch
                        {
                            // Preserva o erro original se o rollback também falhar.
                        }

                        throw;
                    }
                }
            }
        }

        public bool Atualizar(Usuario usuario)
        {
            string sql = "UPDATE usuario " +
                "SET id_pessoa = @pessoa, id_perfil_sistema = @perfil, login = @login, " +
                "senha_hash = @hash, situacao_conta = @situacao " +
                "WHERE id_usuario = @id";

            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", usuario.IdUsuario);
                                    AcessoDados.AdicionarParametro(comandoBanco, "@pessoa", usuario.IdPessoa);
                                    AcessoDados.AdicionarParametro(comandoBanco, "@perfil", usuario.IdPerfilSistema);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@login", usuario.Login);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@hash", usuario.SenhaHash);
                                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@situacao", usuario.SituacaoConta);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;
        }

        private static Usuario ConverterLinha(MySqlDataReader leitor)
        {
            return new Usuario
            {
                IdUsuario = leitor.GetInt32("id_usuario"),
                IdPessoa = leitor.GetInt32("id_pessoa"),
                IdPerfilSistema = AcessoDados.LerInteiroOpcional(leitor, "id_perfil_sistema"),
                Login = leitor.GetString("login"),
                SenhaHash = leitor.GetString("senha_hash"),
                SituacaoConta = leitor.GetString("situacao_conta"),
                SolicitadoEm = leitor.GetDateTime("solicitado_em")
            };
        }

        private static Usuario ConverterLinhaSemHash(MySqlDataReader leitor)
        {
            return new Usuario
            {
                IdUsuario = leitor.GetInt32("id_usuario"),
                IdPessoa = leitor.GetInt32("id_pessoa"),
                IdPerfilSistema = AcessoDados.LerInteiroOpcional(leitor, "id_perfil_sistema"),
                Login = leitor.GetString("login"),
                SituacaoConta = leitor.GetString("situacao_conta"),
                SolicitadoEm = leitor.GetDateTime("solicitado_em")
            };
        }
    }
}
