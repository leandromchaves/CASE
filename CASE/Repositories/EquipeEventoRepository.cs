using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EquipeEventoRepository
    {
        private const string Colunas = "id_equipe,id_evento,id_pessoa,id_perfil,ativo,id_usuario_criacao,criado_em,id_usuario_alteracao,atualizado_em,versao";

        public List<EquipeEvento> ListarPorEvento(int idEvento)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM equipeevento WHERE id_evento=@id ORDER BY id_equipe", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<EquipeEvento> resultadoBanco = new List<EquipeEvento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<EquipeEvento> ListarPorPessoa(int idPessoa)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM equipeevento WHERE id_pessoa=@pessoa AND ativo=1 ORDER BY id_evento,id_equipe", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@pessoa", idPessoa);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<EquipeEvento> resultadoBanco = new List<EquipeEvento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<Pessoa> ListarPessoasDisponiveis(int idEvento)
        {
            const string sql = @"SELECT p.id_pessoa,p.nome,p.telefone,p.email
                FROM pessoa p
                INNER JOIN usuario u ON u.id_pessoa=p.id_pessoa
                INNER JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema
                WHERE LOWER(u.situacao_conta)='aprovado'
                  AND LOWER(ps.nome)='membro'
                  AND NOT EXISTS (
                      SELECT 1 FROM equipeevento ee
                      WHERE ee.id_evento=@evento AND ee.id_pessoa=p.id_pessoa AND ee.ativo=1)
                ORDER BY p.nome";

            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Pessoa> resultadoBanco = new List<Pessoa>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterPessoa(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Pessoa BuscarPessoaDisponivelPorLogin(int idEvento, string login)
        {
            const string sql = @"SELECT p.id_pessoa,p.nome,p.telefone,p.email
                FROM pessoa p
                INNER JOIN usuario u ON u.id_pessoa=p.id_pessoa
                INNER JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema
                WHERE u.login=@login
                  AND LOWER(u.situacao_conta)='aprovado'
                  AND LOWER(ps.nome)='membro'
                  AND NOT EXISTS (
                      SELECT 1 FROM equipeevento ee
                      WHERE ee.id_evento=@evento AND ee.id_pessoa=p.id_pessoa AND ee.ativo=1)";

            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand(sql, conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@login", login);
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        if (leitorBanco.Read())
                        {
                            return ConverterPessoa(leitorBanco);
                        }
                        return null;
                    }
                }
            }
        }

        public Pessoa BuscarPessoaPorId(int idPessoa)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_pessoa,nome,telefone,email FROM pessoa WHERE id_pessoa=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idPessoa);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        if (leitorBanco.Read())
                        {
                            return ConverterPessoa(leitorBanco);
                        }
                        return null;
                    }
                }
            }
        }

        public EquipeEvento BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM equipeevento WHERE id_equipe=@id", conexaoBanco))
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

        public int Cadastrar(EquipeEvento item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO equipeevento (id_evento,id_pessoa,id_perfil,ativo,id_usuario_criacao,criado_em) VALUES (@evento,@pessoa,@perfil,@ativo,@usuarioCriacao,@criadoEm)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    AcessoDados.AdicionarParametro(comandoBanco, "@usuarioCriacao", item.IdUsuarioCriacao);
                    AcessoDados.AdicionarParametro(comandoBanco, "@criadoEm", item.CriadoEm);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(EquipeEvento item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE equipeevento SET id_evento=@evento,id_pessoa=@pessoa,id_perfil=@perfil,ativo=@ativo,id_usuario_alteracao=@usuarioAlteracao,atualizado_em=NOW(),versao=versao+1 WHERE id_equipe=@id AND versao=@versao", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdEquipe);
                    AcessoDados.AdicionarParametro(comandoBanco, "@versao", item.Versao);
                    AcessoDados.AdicionarParametro(comandoBanco, "@usuarioAlteracao", item.IdUsuarioAlteracao);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static void Vincular(MySqlCommand comando, EquipeEvento item)
        {
            AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
            AcessoDados.AdicionarParametro(comando, "@pessoa", item.IdPessoa);
            AcessoDados.AdicionarParametro(comando, "@perfil", item.IdPerfil);
            AcessoDados.AdicionarParametro(comando, "@ativo", item.Ativo);
        }

        private static EquipeEvento ConverterLinha(MySqlDataReader leitor)
        {
            return new EquipeEvento
            {
                IdEquipe = leitor.GetInt32("id_equipe"),
                IdEvento = leitor.GetInt32("id_evento"),
                IdPessoa = leitor.GetInt32("id_pessoa"),
                IdPerfil = leitor.GetInt32("id_perfil"),
                Ativo = leitor.GetBoolean("ativo"),
                IdUsuarioCriacao = AcessoDados.LerInteiroOpcional(leitor, "id_usuario_criacao"),
                CriadoEm = AcessoDados.LerDataHoraOpcional(leitor, "criado_em"),
                IdUsuarioAlteracao = AcessoDados.LerInteiroOpcional(leitor, "id_usuario_alteracao"),
                AtualizadoEm = AcessoDados.LerDataHoraOpcional(leitor, "atualizado_em"),
                Versao = leitor.GetUInt32("versao")
            };
        }

        private static Pessoa ConverterPessoa(MySqlDataReader leitor)
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
