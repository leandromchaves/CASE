using System.Collections.Generic;
using System;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class SetorRepository
    {

        public List<Setor> ListarPorEvento(int idEvento)
        {
            return AcessoDados.ConsultarLista(
                "SELECT id_setor,id_evento,nome FROM setor WHERE id_evento=@id ORDER BY nome,id_setor",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", idEvento)
            );
        }

        public Setor BuscarPorId(int id)
        {
            return AcessoDados.ConsultarUnico(
                "SELECT id_setor,id_evento,nome FROM setor WHERE id_setor=@id",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)
            );
        }

        public int CadastrarComEstoqueDeBarraca(Setor item, int idUsuarioResponsavel)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            using (var conexao = CASE.Data.Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarGerenteResponsavel(
                            conexao,
                            transacao,
                            item.IdEvento,
                            idUsuarioResponsavel);

                        int idSetor = AcessoDados.Inserir(
                            conexao,
                            transacao,
                            "INSERT INTO setor (id_evento,nome) VALUES (@evento,@nome)",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                            });

                        int idInstituicao = BuscarInstituicaoDoEvento(
                            conexao,
                            transacao,
                            item.IdEvento);

                        int idLocal = AcessoDados.Inserir(
                            conexao,
                            transacao,
                            "INSERT INTO localestoque " +
                            "(id_instituicao_central,id_evento,nome,ativo,eh_geral_evento) " +
                            "VALUES (NULL,@evento,@nome,1,0)",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@instituicao", idInstituicao);
                                AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                            });

                        AcessoDados.Executar(
                            conexao,
                            transacao,
                            "INSERT INTO estoquesetor (id_setor,id_local_estoque,id_evento) " +
                            "VALUES (@setor,@local,@evento)",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@setor", idSetor);
                                AcessoDados.AdicionarParametro(comando, "@local", idLocal);
                                AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
                            });

                        transacao.Commit();
                        return idSetor;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool Atualizar(Setor item)
        {
            return AcessoDados.Executar(
                "UPDATE setor SET id_evento=@evento,nome=@nome WHERE id_setor=@id",
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@id", item.IdSetor);
                    AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
                    AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                }) > 0;

        }

        public void RenomearComLocal(Setor setor, int idUsuario, string nomeAnterior)
        {
            using (var conexao = CASE.Data.Conexao.CriarConexao())
            {
                conexao.Open();
                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarGerenteResponsavel(conexao, transacao, setor.IdEvento, idUsuario);
                        int alterados = AcessoDados.Executar(conexao, transacao,
                            "UPDATE setor SET nome=@nome WHERE id_setor=@setor AND id_evento=@evento " +
                            "AND BINARY nome=BINARY @anterior",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@setor", setor.IdSetor);
                                AcessoDados.AdicionarParametro(comando, "@evento", setor.IdEvento);
                                AcessoDados.AdicionarParametro(comando, "@nome", setor.Nome);
                                AcessoDados.AdicionarParametro(comando, "@anterior", nomeAnterior);
                            });
                        if (alterados != 1)
                        {
                            throw new InvalidOperationException("O setor foi alterado. Atualize a lista antes de editar.");
                        }
                        int locais = AcessoDados.Executar(conexao, transacao,
                            "UPDATE localestoque l JOIN estoquesetor es ON es.id_local_estoque=l.id_local_estoque " +
                            "SET l.nome=@nome WHERE es.id_setor=@setor AND es.id_evento=@evento " +
                            "AND l.id_evento=@evento AND l.id_instituicao_central IS NULL AND l.eh_geral_evento=0",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@setor", setor.IdSetor);
                                AcessoDados.AdicionarParametro(comando, "@evento", setor.IdEvento);
                                AcessoDados.AdicionarParametro(comando, "@nome", setor.Nome);
                            });
                        if (locais != 1)
                        {
                            throw new InvalidOperationException("O local de estoque do setor não está configurado corretamente.");
                        }
                        transacao.Commit();
                    }
                    catch (MySqlException erro)
                    {
                        transacao.Rollback();
                        if (erro.Number == 1062)
                        {
                            throw new InvalidOperationException("Já existe um setor ou local com esse nome no evento.", erro);
                        }
                        throw;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void ValidarGerenteResponsavel(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEvento,
            int idUsuario)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ev.id_evento FROM evento ev " +
                "JOIN usuario u ON u.id_usuario=@usuario " +
                "JOIN pessoa p ON p.id_pessoa=u.id_pessoa " +
                "JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE ev.id_evento=@evento AND (ps.nome='Administrador' OR " +
                "(ps.nome='Gerente' AND ev.id_responsavel=p.id_pessoa)) " +
                "AND ev.situacao IN ('planejado','em_andamento') " +
                "AND u.situacao_conta='aprovado' FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@evento", idEvento);
                AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);

                if (comando.ExecuteScalar() == null)
                {
                    throw new UnauthorizedAccessException(
                        "Somente o administrador ou o gerente responsável por um evento operacional pode alterar setores.");
                }
            }
        }

        private static int BuscarInstituicaoDoEvento(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEvento)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_instituicao FROM evento WHERE id_evento=@evento FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@evento", idEvento);
                object resultado = comando.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException("O evento não foi encontrado.");
                }

                return Convert.ToInt32(resultado);
            }
        }

        private static Setor ConverterLinha(MySqlDataReader leitor)
        {
            return new Setor
            {
                IdSetor = leitor.GetInt32("id_setor"),
                IdEvento = leitor.GetInt32("id_evento"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
