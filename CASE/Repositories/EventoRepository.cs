using System.Collections.Generic;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;
using System;

namespace CASE.Repositories
{
    public class EventoRepository
    {

        private const string Colunas = "id_evento,id_instituicao,id_tipo_evento,nome,`local`,data_inicio,data_fim,id_responsavel,situacao,observacao,id_usuario_criacao,criado_em,versao";

        public List<Evento> ListarTodos()
        {
            return AcessoDados.ConsultarLista(
                "SELECT " + Colunas + " FROM evento ORDER BY data_inicio,id_evento",
                ConverterLinha);
        }

        public List<Evento> ListarPorInstituicao(int idInstituicao)
        {
            return AcessoDados.ConsultarLista(
                "SELECT " + Colunas + " FROM evento WHERE id_instituicao=@id ORDER BY data_inicio,id_evento",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", idInstituicao)
            );
        }

        public List<Evento> ListarPorResponsavel(int idPessoaResponsavel)
        {
            return AcessoDados.ConsultarLista(
                "SELECT " + Colunas + " FROM evento WHERE id_responsavel=@responsavel ORDER BY data_inicio,id_evento",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(
                    comando,
                    "@responsavel",
                    idPessoaResponsavel)
            );
        }

        public Evento BuscarPorId(int id)
        {
            return AcessoDados.ConsultarUnico(
                "SELECT " + Colunas + " FROM evento WHERE id_evento=@id",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)
            );
        }

        public int Cadastrar(Evento item)
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
                        int idEvento = AcessoDados.Inserir(
                            conexao,
                            transacao,
                            "INSERT INTO evento (id_instituicao,id_tipo_evento,nome,`local`,data_inicio,data_fim,id_responsavel,situacao,observacao,id_usuario_criacao,criado_em) " +
                            "VALUES (@instituicao,@tipo,@nome,@local,@inicio,@fim,@responsavel,@situacao,@observacao,@usuarioCriacao,@criadoEm)",
                            comando =>
                            {
                                Vincular(comando, item);
                                AcessoDados.AdicionarParametro(comando, "@usuarioCriacao", item.IdUsuarioCriacao);
                                AcessoDados.AdicionarParametro(comando, "@criadoEm", item.CriadoEm);
                            });

                        AcessoDados.Executar(
                            conexao,
                            transacao,
                            "INSERT INTO localestoque (id_instituicao_central,id_evento,nome,ativo,eh_geral_evento) " +
                            "VALUES (NULL,@evento,@nome,1,1)",
                            comando =>
                            {
                                AcessoDados.AdicionarParametro(comando, "@instituicao", item.IdInstituicao);
                                AcessoDados.AdicionarParametro(comando, "@evento", idEvento);
                                AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", "Estoque geral");
                            });

                        transacao.Commit();
                        return idEvento;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool Atualizar(Evento item)
        {
            return AcessoDados.Executar(
                "UPDATE evento SET id_instituicao=@instituicao,id_tipo_evento=@tipo,nome=@nome,`local`=@local,data_inicio=@inicio,data_fim=@fim,id_responsavel=@responsavel,situacao=@situacao,observacao=@observacao,versao=versao+1 WHERE id_evento=@id AND versao=@versao",
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@id", item.IdEvento);
                    AcessoDados.AdicionarParametro(comando, "@versao", item.Versao);
                    Vincular(comando, item);
                }) > 0;

        }

        public void AtualizarSituacao(
            int idEvento,
            uint versao,
            string situacao,
            int idUsuario,
            string situacaoAnterior)
        {
            using (var conexao = CASE.Data.Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string situacaoAtual;
                        uint versaoAtual;

                        using (var comando = new MySqlCommand(
                            "SELECT situacao,versao FROM evento WHERE id_evento=@id FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEvento);

                            using (MySqlDataReader leitor = comando.ExecuteReader())
                            {
                                if (!leitor.Read())
                                {
                                    throw new InvalidOperationException("O evento não foi encontrado.");
                                }

                                situacaoAtual = leitor.GetString("situacao");
                                versaoAtual = leitor.GetUInt32("versao");
                            }
                        }

                        if (versaoAtual != versao ||
                            !string.Equals(
                                situacaoAtual,
                                situacaoAnterior,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "O evento foi alterado por outra pessoa. Atualize os dados e tente novamente.");
                        }

                        if (string.Equals(situacao, SituacoesEvento.Finalizado, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(situacao, SituacoesEvento.Cancelado, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var comando = new MySqlCommand(
                                "SELECT COUNT(*) FROM estoque e " +
                                "JOIN vw_localestoque le ON le.id_local_estoque=e.id_local_estoque " +
                                "WHERE le.id_evento=@evento AND e.quantidade_atual<>0",
                                conexao,
                                transacao))
                            {
                                comando.Parameters.AddWithValue("@evento", idEvento);
                                int saldosPendentes = Convert.ToInt32(comando.ExecuteScalar());

                                if (saldosPendentes > 0)
                                {
                                    throw new InvalidOperationException(
                                        "O evento possui saldo no estoque geral ou nas barracas e não pode ser finalizado.");
                                }
                            }
                        }

                        using (var comando = new MySqlCommand(
                            "UPDATE evento SET situacao=@situacao,versao=versao+1 " +
                            "WHERE id_evento=@id AND versao=@versao",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@situacao", situacao);
                            comando.Parameters.AddWithValue("@id", idEvento);
                            comando.Parameters.AddWithValue("@versao", versao);

                            if (comando.ExecuteNonQuery() != 1)
                            {
                                throw new InvalidOperationException(
                                    "O evento foi alterado por outra pessoa. Atualize os dados e tente novamente.");
                            }
                        }

                        using (var comando = new MySqlCommand(
                            "INSERT INTO historicoalteracaooperacao " +
                            "(entidade,id_entidade,id_usuario,justificativa,dados_anteriores,dados_novos) " +
                            "VALUES ('evento',@id,@usuario,'Alteração de situação'," +
                            "JSON_OBJECT('situacao',@anterior),JSON_OBJECT('situacao',@nova))",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEvento);
                            comando.Parameters.AddWithValue("@usuario", idUsuario);
                            comando.Parameters.AddWithValue("@anterior", situacaoAnterior);
                            comando.Parameters.AddWithValue("@nova", situacao);
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

        public void ExcluirCancelado(int idEvento, uint versao)
        {
            using (var conexao = CASE.Data.Conexao.CriarConexao())
            {
                conexao.Open();

                using (var transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        using (var comando = new MySqlCommand(
                            "SELECT situacao,versao FROM evento WHERE id_evento=@id FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEvento);
                            using (MySqlDataReader leitor = comando.ExecuteReader())
                            {
                                if (!leitor.Read())
                                    throw new InvalidOperationException("O evento não foi encontrado.");
                                if (!string.Equals(leitor.GetString("situacao"), SituacoesEvento.Cancelado,
                                    StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("Somente eventos cancelados podem ser excluídos.");
                                if (leitor.GetUInt32("versao") != versao)
                                    throw new InvalidOperationException("O evento foi alterado. Atualize a lista e tente novamente.");
                            }
                        }

                        // Limpa o histórico operacional do evento junto com os dados que deixarão de ser consultáveis.
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM historicoalteracaooperacao WHERE " +
                            "(entidade='evento' AND id_entidade=@evento) OR " +
                            "(entidade='equipeevento' AND id_entidade IN (SELECT id_equipe FROM equipeevento WHERE id_evento=@evento)) OR " +
                            "(entidade='registro' AND id_entidade IN (SELECT r.id_registro FROM registro r " +
                                "JOIN escala e ON e.id_escala=r.id_escala JOIN equipeevento q ON q.id_equipe=e.id_equipe " +
                                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                                "WHERE q.id_evento=@evento OR s.id_evento=@evento)) OR " +
                            "(entidade='solicitacao' AND id_entidade IN (SELECT so.id_solicitacao FROM solicitacao so " +
                                "JOIN escala e ON e.id_escala=so.id_escala JOIN equipeevento q ON q.id_equipe=e.id_equipe " +
                                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                                "WHERE q.id_evento=@evento OR s.id_evento=@evento)) OR " +
                            "(entidade='solicitacaoestoquecentral' AND id_entidade IN (SELECT sc.id_solicitacao_estoque_central " +
                                "FROM solicitacaoestoquecentral sc JOIN localestoque lo ON lo.id_evento=@evento " +
                                "WHERE sc.id_local_origem=lo.id_local_estoque OR sc.id_local_destino=lo.id_local_estoque)) OR " +
                            "(entidade='entradamercadoria' AND id_entidade IN (SELECT en.id_entrada FROM entradamercadoria en " +
                                "JOIN localestoque lo ON lo.id_local_estoque=en.id_local_destino WHERE lo.id_evento=@evento)) OR " +
                            "(entidade='transferenciaestoque' AND id_entidade IN (SELECT tr.id_transferencia FROM transferenciaestoque tr " +
                                "JOIN localestoque lo ON lo.id_evento=@evento " +
                                "WHERE tr.id_local_origem=lo.id_local_estoque OR tr.id_local_destino=lo.id_local_estoque))",
                            idEvento);

                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM registroitem WHERE id_registro IN (SELECT r.id_registro FROM registro r " +
                            "JOIN escala e ON e.id_escala=r.id_escala JOIN equipeevento q ON q.id_equipe=e.id_equipe " +
                            "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                            "WHERE q.id_evento=@evento OR s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM solicitacaoitem WHERE id_solicitacao IN (SELECT so.id_solicitacao FROM solicitacao so " +
                            "JOIN escala e ON e.id_escala=so.id_escala JOIN equipeevento q ON q.id_equipe=e.id_equipe " +
                            "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                            "WHERE q.id_evento=@evento OR s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM solicitacaoestoquecentralitem WHERE id_solicitacao_estoque_central IN " +
                            "(SELECT sc.id_solicitacao_estoque_central FROM solicitacaoestoquecentral sc " +
                            "JOIN localestoque lo ON lo.id_evento=@evento " +
                            "WHERE sc.id_local_origem=lo.id_local_estoque OR sc.id_local_destino=lo.id_local_estoque)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM transferenciaitem WHERE id_transferencia IN (SELECT tr.id_transferencia FROM transferenciaestoque tr " +
                            "JOIN localestoque lo ON lo.id_evento=@evento " +
                            "WHERE tr.id_local_origem=lo.id_local_estoque OR tr.id_local_destino=lo.id_local_estoque)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM entradaitem WHERE id_entrada IN (SELECT en.id_entrada FROM entradamercadoria en " +
                            "JOIN localestoque lo ON lo.id_local_estoque=en.id_local_destino WHERE lo.id_evento=@evento)", idEvento);

                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM transferenciaestoque WHERE id_local_origem IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento) " +
                            "OR id_local_destino IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM solicitacaoestoquecentral WHERE id_local_origem IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento) " +
                            "OR id_local_destino IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM solicitacao WHERE id_escala IN (SELECT e.id_escala FROM escala e " +
                            "JOIN equipeevento q ON q.id_equipe=e.id_equipe JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                            "JOIN setor s ON s.id_setor=j.id_setor WHERE q.id_evento=@evento OR s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM registro WHERE id_escala IN (SELECT e.id_escala FROM escala e " +
                            "JOIN equipeevento q ON q.id_equipe=e.id_equipe JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                            "JOIN setor s ON s.id_setor=j.id_setor WHERE q.id_evento=@evento OR s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM entradamercadoria WHERE id_local_destino IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM despesa WHERE id_evento=@evento", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM apuracaototallegado WHERE id_apuracao IN (SELECT a.id_apuracao FROM apuracao a " +
                            "JOIN jornadasetor j ON j.id_jornada=a.id_jornada JOIN setor s ON s.id_setor=j.id_setor WHERE s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM apuracao WHERE id_jornada IN (SELECT j.id_jornada FROM jornadasetor j " +
                            "JOIN setor s ON s.id_setor=j.id_setor WHERE s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM estoque WHERE id_local_estoque IN (SELECT id_local_estoque FROM localestoque WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM estoquesetor WHERE id_evento=@evento", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM produtosetor WHERE id_setor IN (SELECT id_setor FROM setor WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM escala WHERE id_equipe IN (SELECT id_equipe FROM equipeevento WHERE id_evento=@evento) " +
                            "OR id_jornada IN (SELECT j.id_jornada FROM jornadasetor j JOIN setor s ON s.id_setor=j.id_setor WHERE s.id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM equipeevento WHERE id_evento=@evento", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM jornadasetor WHERE id_setor IN (SELECT id_setor FROM setor WHERE id_evento=@evento)", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM setor WHERE id_evento=@evento", idEvento);
                        ExecutarExclusao(conexao, transacao,
                            "DELETE FROM localestoque WHERE id_evento=@evento", idEvento);

                        using (var comando = new MySqlCommand(
                            "DELETE FROM evento WHERE id_evento=@id AND situacao=@situacao AND versao=@versao",
                            conexao,
                            transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idEvento);
                            comando.Parameters.AddWithValue("@situacao", SituacoesEvento.Cancelado);
                            comando.Parameters.AddWithValue("@versao", versao);
                            if (comando.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("O evento foi alterado. Atualize a lista e tente novamente.");
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

        private static void ExecutarExclusao(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            string sql,
            int idEvento)
        {
            using (var comando = new MySqlCommand(sql, conexao, transacao))
            {
                comando.Parameters.AddWithValue("@evento", idEvento);
                comando.ExecuteNonQuery();
            }
        }

        private static void Vincular(MySqlCommand comando, Evento item)
        {
            AcessoDados.AdicionarParametro(comando, "@instituicao", item.IdInstituicao);
            AcessoDados.AdicionarParametro(comando, "@tipo", item.IdTipoEvento);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@local", item.Local);
            AcessoDados.AdicionarParametro(comando, "@inicio", item.DataInicio);
            AcessoDados.AdicionarParametro(comando, "@fim", item.DataFim);
            AcessoDados.AdicionarParametro(comando, "@responsavel", item.IdResponsavel);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@situacao", item.Situacao);
            AcessoDados.AdicionarTextoOpcional(comando, "@observacao", item.Observacao);
        }

        private static Evento ConverterLinha(MySqlDataReader leitor)
        {
            return new Evento
            {
                IdEvento = leitor.GetInt32("id_evento"),
                IdInstituicao = leitor.GetInt32("id_instituicao"),
                IdTipoEvento = leitor.GetInt32("id_tipo_evento"),
                Nome = leitor.GetString("nome"),
                Local = leitor.GetString("local"),
                DataInicio = leitor.GetDateTime("data_inicio"),
                DataFim = leitor.GetDateTime("data_fim"),
                IdResponsavel = leitor.GetInt32("id_responsavel"),
                Situacao = leitor.GetString("situacao"),
                Observacao = AcessoDados.LerTextoOpcional(leitor, "observacao"),
                IdUsuarioCriacao = AcessoDados.LerInteiroOpcional(leitor, "id_usuario_criacao"),
                CriadoEm = AcessoDados.LerDataHoraOpcional(leitor, "criado_em"),
                Versao = leitor.GetUInt32("versao")
            };
        }
    }
}
