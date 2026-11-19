using System.Collections.Generic;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

using CASE.Data;

namespace CASE.Repositories
{
    public class UsuarioRepository
    {
        private const string Colunas = "id_usuario, id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta, solicitado_em";
        private const string ColunasSemHash = "id_usuario, id_pessoa, id_perfil_sistema, login, situacao_conta, solicitado_em";

        internal bool AlterarSenhaDaSessao(string hashAtual, string novoHash)
        {
            var sessao = CASE.Services.SessaoUsuario.ObterUsuarioAtual();
            // A condição no hash impede sobrescrever outra troca de senha concorrente.
            const string sql = "UPDATE usuario SET senha_hash=@novoHash " +
                "WHERE id_usuario=@usuario AND id_pessoa=@pessoa AND id_perfil_sistema=@perfil " +
                "AND LOWER(situacao_conta)='aprovado' AND BINARY senha_hash=BINARY @hashAtual " +
                "AND EXISTS (SELECT 1 FROM perfilsistema p WHERE p.id_perfil_sistema=@perfil " +
                "AND LOWER(p.nome) IN ('administrador','gerente','membro'))";
            return AcessoDados.Executar(sql, comando =>
            {
                AcessoDados.AdicionarParametro(comando, "@usuario", sessao.IdUsuario);
                AcessoDados.AdicionarParametro(comando, "@pessoa", sessao.IdPessoa);
                AcessoDados.AdicionarParametro(comando, "@perfil", sessao.IdPerfilSistema);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@hashAtual", hashAtual);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@novoHash", novoHash);
            }) == 1;
        }

        public List<Usuario> Listar()
        {
            string sql = "SELECT " + ColunasSemHash + " FROM usuario ORDER BY id_usuario";
            return AcessoDados.ConsultarLista(sql, ConverterLinhaSemHash);
        }

        public Usuario BuscarPorId(int id)
        {
            string sql = "SELECT " + ColunasSemHash + " FROM usuario WHERE id_usuario = @id";
            return AcessoDados.ConsultarUnico(
                sql,
                ConverterLinhaSemHash,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id));
        }

        public Usuario BuscarPorLogin(string login)
        {
            string sql = "SELECT " + Colunas + " FROM usuario WHERE login = @login";
            return AcessoDados.ConsultarUnico(
                sql,
                ConverterLinha,
                comando => AcessoDados.AdicionarTextoObrigatorio(comando, "@login", login));
        }

        public string BuscarLoginPorPessoa(int idPessoa)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                string sql = "SELECT login FROM usuario WHERE id_pessoa = @pessoa";

                using (var comando = new MySqlCommand(sql, conexao))
                {
                    AcessoDados.AdicionarParametro(comando, "@pessoa", idPessoa);

                    object resultado = comando.ExecuteScalar();

                    if (resultado == null || resultado == System.DBNull.Value)
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
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
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
                            CASE.Services.SituacoesUsuario.Aprovado,
                            System.StringComparison.OrdinalIgnoreCase))
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

                        using (var comando = new MySqlCommand(sqlAtualizacao, conexao, transacao))
                        {
                            AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", situacao);
                            AcessoDados.AdicionarParametro(comando, "@id", idUsuario);

                            if (string.Equals(
                                situacao,
                                CASE.Services.SituacoesUsuario.Aprovado,
                                System.StringComparison.OrdinalIgnoreCase))
                            {
                                AcessoDados.AdicionarParametro(comando, "@perfil", idPerfilSistema);
                            }

                            if (comando.ExecuteNonQuery() != 1)
                            {
                                throw new System.InvalidOperationException(
                                    "O cadastro foi analisado por outra pessoa. Atualize a lista.");
                            }
                        }

                        using (var comando = new MySqlCommand(
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
                throw new System.UnauthorizedAccessException("O responsável deve corresponder à sessão autenticada.");
            using (var comando = new MySqlCommand(
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
                    throw new System.UnauthorizedAccessException(
                        "A decisão exige uma conta de administrador aprovada.");
                }
            }
        }

        public void AlterarPerfilAdministrativo(int idUsuario, int idAnalisador,
            int idPerfilSistema, int idPerfilAnterior, string justificativa)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAdministrador(conexao, transacao, idAnalisador);
                        if (idUsuario == idAnalisador)
                            throw new System.InvalidOperationException("Use outra conta de administrador para alterar seu perfil.");
                        string nomeAnterior;
                        using (var comando = new MySqlCommand(
                            "SELECT u.id_perfil_sistema,u.situacao_conta,p.nome FROM usuario u " +
                            "LEFT JOIN perfilsistema p ON p.id_perfil_sistema=u.id_perfil_sistema " +
                            "WHERE u.id_usuario=@usuario FOR UPDATE", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            using (var leitor = comando.ExecuteReader())
                            {
                                if (!leitor.Read() || leitor.IsDBNull(0) ||
                                    !string.Equals(leitor.GetString(1), SituacoesUsuario.Aprovado, System.StringComparison.OrdinalIgnoreCase))
                                    throw new System.InvalidOperationException("Somente contas aprovadas podem ter o perfil alterado.");
                                if (leitor.GetInt32(0) != idPerfilAnterior)
                                    throw new System.InvalidOperationException("O perfil foi alterado por outra pessoa. Atualize a lista.");
                                nomeAnterior = leitor.GetString(2);
                            }
                        }
                        string novoNome;
                        using (var comando = new MySqlCommand(
                            "SELECT nome FROM perfilsistema WHERE id_perfil_sistema=@perfil", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@perfil", idPerfilSistema);
                            novoNome = System.Convert.ToString(comando.ExecuteScalar());
                        }
                        if (novoNome != "Administrador" && novoNome != "Gerente" && novoNome != "Membro")
                            throw new System.ArgumentException("O perfil selecionado não é válido.");
                        if (idPerfilSistema == idPerfilAnterior)
                            throw new System.ArgumentException("Escolha um perfil diferente do atual.");
                        if (justificativa != null && justificativa.Trim().Length > 500)
                            throw new System.ArgumentException("A justificativa deve ter até 500 caracteres.");
                        using (var comando = new MySqlCommand(
                            "UPDATE usuario SET id_perfil_sistema=@perfil WHERE id_usuario=@usuario " +
                            "AND id_perfil_sistema=@anterior AND LOWER(situacao_conta)='aprovado'", conexao, transacao))
                        {
                            comando.Parameters.AddWithValue("@perfil", idPerfilSistema);
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            comando.Parameters.AddWithValue("@anterior", idPerfilAnterior);
                            if (comando.ExecuteNonQuery() != 1)
                                throw new System.InvalidOperationException("A conta foi alterada. Atualize a lista.");
                        }
                        using (var comando = new MySqlCommand(
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
            using (var comando = new MySqlCommand(
                "SELECT id_usuario FROM usuario " +
                "WHERE id_usuario=@id AND LOWER(situacao_conta)='pendente' FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", idUsuario);

                if (comando.ExecuteScalar() == null)
                {
                    throw new System.InvalidOperationException(
                        "Somente cadastros pendentes podem receber uma decisão.");
                }
            }
        }

        public int Cadastrar(Usuario usuario)
        {
            string sql = "INSERT INTO usuario (id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta) " +
                "VALUES (@pessoa, @perfil, @login, @hash, @situacao)";

            return AcessoDados.Inserir(sql, comando =>
            {
                AcessoDados.AdicionarParametro(comando, "@pessoa", usuario.IdPessoa);
                AcessoDados.AdicionarParametro(comando, "@perfil", usuario.IdPerfilSistema);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@login", usuario.Login);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@hash", usuario.SenhaHash);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", usuario.SituacaoConta);
            });
        }

        public int CadastrarSolicitacao(Pessoa pessoa, Usuario usuario)
        {
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string sqlPessoa = "INSERT INTO pessoa " +
                            "(nome, telefone, email, foto_perfil) " +
                            "VALUES (@nome, @telefone, @email, @fotoPerfil)";

                        int idPessoa = AcessoDados.Inserir(
                            conexao,
                            transacao,
                            sqlPessoa,
                            comando =>
                            {
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", pessoa.Nome);
                                AcessoDados.AdicionarTextoOpcional(comando, "@telefone", pessoa.Telefone);
                                AcessoDados.AdicionarTextoOpcional(comando, "@email", pessoa.Email);

                                MySqlParameter parametroFoto = comando.Parameters.Add(
                                    "@fotoPerfil",
                                    MySqlDbType.MediumBlob);

                                if (pessoa.FotoPerfil == null)
                                {
                                    parametroFoto.Value = System.DBNull.Value;
                                }
                                else
                                {
                                    parametroFoto.Value = pessoa.FotoPerfil;
                                }
                            });

                        string sqlUsuario = "INSERT INTO usuario " +
                            "(id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta) " +
                            "VALUES (@pessoa, @perfil, @login, @hash, @situacao)";

                        int idUsuario = AcessoDados.Inserir(
                            conexao,
                            transacao,
                            sqlUsuario,
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@pessoa", idPessoa);
                                AcessoDados.AdicionarParametro(comando, "@perfil", usuario.IdPerfilSistema);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@login", usuario.Login);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@hash", usuario.SenhaHash);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", usuario.SituacaoConta);
                            });

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

        public int CriarPrimeiroAdministrador(Pessoa pessoa, string login, string senhaHash)
        {
            if (pessoa == null)
            {
                throw new System.ArgumentNullException(nameof(pessoa));
            }

            using (var conexao = CASE.Data.Conexao.CriarConexao())
            {
                conexao.Open();
                ValidarBancoLocalDoCase(conexao);

                bool bloqueioAdquirido = false;

                try
                {
                    using (var comandoBloqueio = new MySqlCommand(
                        "SELECT GET_LOCK('case_bootstrap_primeiro_administrador', 10)",
                        conexao))
                    {
                        object resultadoBloqueio = comandoBloqueio.ExecuteScalar();

                        if (resultadoBloqueio == null ||
                            resultadoBloqueio == System.DBNull.Value ||
                            System.Convert.ToInt32(resultadoBloqueio) != 1)
                        {
                            throw new System.InvalidOperationException(
                                "Não foi possível obter o bloqueio para inicializar o administrador.");
                        }

                        bloqueioAdquirido = true;
                    }

                    using (var comandoIsolamento = new MySqlCommand(
                        "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE",
                        conexao))
                    {
                        comandoIsolamento.ExecuteNonQuery();
                    }

                    using (var transacao = conexao.BeginTransaction())
                    {
                        try
                        {
                            int idPerfilAdministrador = BuscarIdPerfilAdministrador(
                                conexao,
                                transacao);

                            ValidarQueNaoExisteContaComPerfil(
                                conexao,
                                transacao);

                            int idPessoa = AcessoDados.Inserir(
                                conexao,
                                transacao,
                                "INSERT INTO pessoa (nome, telefone, email) " +
                                "VALUES (@nome, @telefone, @email)",
                                comando =>
                                {
                                    AcessoDados.AdicionarTextoObrigatorio(
                                        comando,
                                        "@nome",
                                        pessoa.Nome);
                                    AcessoDados.AdicionarTextoOpcional(
                                        comando,
                                        "@telefone",
                                        pessoa.Telefone);
                                    AcessoDados.AdicionarTextoOpcional(
                                        comando,
                                        "@email",
                                        pessoa.Email);
                                });

                            int idUsuario = AcessoDados.Inserir(
                                conexao,
                                transacao,
                                "INSERT INTO usuario " +
                                "(id_pessoa, id_perfil_sistema, login, senha_hash, situacao_conta) " +
                                "VALUES (@pessoa, @perfil, @login, @hash, @situacao)",
                                comando =>
                                {
                                    AcessoDados.AdicionarParametro(comando, "@pessoa", idPessoa);
                                    AcessoDados.AdicionarParametro(
                                        comando,
                                        "@perfil",
                                        idPerfilAdministrador);
                                    AcessoDados.AdicionarTextoObrigatorio(comando, "@login", login);
                                    AcessoDados.AdicionarTextoObrigatorio(comando, "@hash", senhaHash);
                                    AcessoDados.AdicionarTextoObrigatorio(
                                        comando,
                                        "@situacao",
                                        CASE.Services.SituacoesUsuario.Aprovado);
                                });

                            transacao.Commit();
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
                finally
                {
                    if (bloqueioAdquirido)
                    {
                        using (var comandoDesbloqueio = new MySqlCommand(
                            "SELECT RELEASE_LOCK('case_bootstrap_primeiro_administrador')",
                            conexao))
                        {
                            comandoDesbloqueio.ExecuteScalar();
                        }
                    }
                }
            }
        }

        private static int BuscarIdPerfilAdministrador(
            MySqlConnection conexao,
            MySqlTransaction transacao)
        {
            using (var comando = new MySqlCommand(
                "SELECT id_perfil_sistema FROM perfilsistema " +
                "WHERE LOWER(TRIM(nome)) = 'administrador'",
                conexao,
                transacao))
            using (var leitor = comando.ExecuteReader())
            {
                if (!leitor.Read())
                {
                    throw new System.InvalidOperationException(
                        "O perfil Administrador não existe no banco de dados.");
                }

                int idPerfilAdministrador = leitor.GetInt32("id_perfil_sistema");

                if (leitor.Read())
                {
                    throw new System.InvalidOperationException(
                        "Há mais de um perfil Administrador no banco de dados.");
                }

                return idPerfilAdministrador;
            }
        }

        private static void ValidarBancoLocalDoCase(MySqlConnection conexao)
        {
            if (!string.Equals(
                conexao.Database,
                "case_commercial",
                System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "O bootstrap exige o schema local case_commercial.");
            }

            string servidor = conexao.DataSource;

            if (!string.Equals(servidor, "localhost", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(servidor, "127.0.0.1", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(servidor, "::1", System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "O bootstrap só pode ser executado contra um servidor MySQL local.");
            }
        }

        private static void ValidarQueNaoExisteContaComPerfil(
            MySqlConnection conexao,
            MySqlTransaction transacao)
        {
            using (var comando = new MySqlCommand(
                "SELECT id_usuario, id_perfil_sistema " +
                "FROM usuario ORDER BY id_usuario FOR UPDATE",
                conexao,
                transacao))
            using (var leitor = comando.ExecuteReader())
            {
                while (leitor.Read())
                {
                    int? idPerfil = AcessoDados.LerInteiroOpcional(
                        leitor,
                        "id_perfil_sistema");

                    if (idPerfil.HasValue)
                    {
                        throw new System.InvalidOperationException(
                            "Já existe uma conta com perfil atribuído. " +
                            "A inicialização não pode ser repetida.");
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

            return AcessoDados.Executar(sql, comando =>
            {
                AcessoDados.AdicionarParametro(comando, "@id", usuario.IdUsuario);
                AcessoDados.AdicionarParametro(comando, "@pessoa", usuario.IdPessoa);
                AcessoDados.AdicionarParametro(comando, "@perfil", usuario.IdPerfilSistema);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@login", usuario.Login);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@hash", usuario.SenhaHash);
                AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", usuario.SituacaoConta);
            }) > 0;
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
