using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EventoRepository
    {
        private const string Colunas = "id_evento,id_instituicao,id_tipo_evento,nome,`local`,data_inicio,data_fim,id_responsavel,situacao,observacao,id_usuario_criacao,criado_em,versao";

        public List<Evento> ListarTodos()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM evento ORDER BY data_inicio,id_evento", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Evento> resultadoBanco = new List<Evento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<Evento> ListarPorInstituicao(int idInstituicao)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM evento WHERE id_instituicao=@id ORDER BY data_inicio,id_evento", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idInstituicao);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Evento> resultadoBanco = new List<Evento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<Evento> ListarPorResponsavel(int idPessoaResponsavel)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM evento WHERE id_responsavel=@responsavel ORDER BY data_inicio,id_evento", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(
                    comandoBanco,
                    "@responsavel",
                    idPessoaResponsavel);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Evento> resultadoBanco = new List<Evento>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Evento BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM evento WHERE id_evento=@id", conexaoBanco))
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

        public int Cadastrar(Evento item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        int resultadoBanco4;
                        using (MySqlCommand comandoBanco4 = new MySqlCommand("INSERT INTO evento (id_instituicao,id_tipo_evento,nome,`local`,data_inicio,data_fim,id_responsavel,situacao,observacao,id_usuario_criacao,criado_em) " +
                            "VALUES (@instituicao,@tipo,@nome,@local,@inicio,@fim,@responsavel,@situacao,@observacao,@usuarioCriacao,@criadoEm)", conexao, transacao))
                        {
                            Vincular(comandoBanco4, item);
                            AcessoDados.AdicionarParametro(comandoBanco4, "@usuarioCriacao", item.IdUsuarioCriacao);
                            AcessoDados.AdicionarParametro(comandoBanco4, "@criadoEm", item.CriadoEm);
                            comandoBanco4.ExecuteNonQuery();
                            resultadoBanco4 = Convert.ToInt32(comandoBanco4.LastInsertedId);
                        }
                        int idEvento = resultadoBanco4;

                        using (MySqlCommand comandoBanco5 = new MySqlCommand("INSERT INTO localestoque (id_instituicao_central,id_evento,nome,ativo,eh_geral_evento) " +
                            "VALUES (NULL,@evento,@nome,1,1)", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco5, "@instituicao", item.IdInstituicao);
                            AcessoDados.AdicionarParametro(comandoBanco5, "@evento", idEvento);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco5, "@nome", "Estoque geral");
                            comandoBanco5.ExecuteNonQuery();
                        }

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
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE evento SET id_instituicao=@instituicao,id_tipo_evento=@tipo,nome=@nome,`local`=@local,data_inicio=@inicio,data_fim=@fim,id_responsavel=@responsavel,situacao=@situacao,observacao=@observacao,versao=versao+1 WHERE id_evento=@id AND versao=@versao", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdEvento);
                    AcessoDados.AdicionarParametro(comandoBanco, "@versao", item.Versao);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        public bool PossuiVendasOuPerdas(int idEvento)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                return PossuiVendasOuPerdas(conexao, null, idEvento);
            }
        }

        private static bool PossuiVendasOuPerdas(MySqlConnection conexao, MySqlTransaction transacao, int idEvento)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT EXISTS(SELECT 1 FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                "JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                "WHERE eq.id_evento=@evento AND r.tipo IN ('venda','perda'))", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@evento", idEvento);
                return Convert.ToBoolean(comando.ExecuteScalar());
            }
        }

        public void AtualizarSituacao(
            int idEvento,
            uint versao,
            string situacao,
            int idUsuario,
            string situacaoAnterior,
            bool devolverSaldosSemLancamentos = false)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string situacaoAtual;
                        uint versaoAtual;

                        using (MySqlCommand comando = new MySqlCommand(
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
                            if (devolverSaldosSemLancamentos)
                            {
                                if (PossuiVendasOuPerdas(conexao, transacao, idEvento))
                                    throw new InvalidOperationException(
                                        "Foram encontrados registros de venda ou perda. Confira os lançamentos antes de encerrar o evento.");
                                DevolverSaldosAoCentral(conexao, transacao, idEvento, idUsuario);
                            }
                            using (MySqlCommand comando = new MySqlCommand(
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

                        using (MySqlCommand comando = new MySqlCommand(
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

                        using (MySqlCommand comando = new MySqlCommand(
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

        private static void DevolverSaldosAoCentral(MySqlConnection conexao, MySqlTransaction transacao,
            int idEvento, int idUsuario)
        {
            List<Tuple<int, int, decimal>> saldos = new List<Tuple<int, int, decimal>>();
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT e.id_local_estoque,e.id_produto,e.quantidade_atual FROM estoque e " +
                "JOIN vw_localestoque l ON l.id_local_estoque=e.id_local_estoque " +
                "WHERE l.id_evento=@evento AND e.quantidade_atual<>0 " +
                "ORDER BY e.id_local_estoque,e.id_produto FOR UPDATE", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@evento", idEvento);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                    while (leitor.Read())
                    {
                        decimal quantidade = leitor.GetDecimal("quantidade_atual");
                        if (quantidade < 0)
                            throw new InvalidOperationException("Regularize os saldos negativos antes de encerrar o evento.");
                        saldos.Add(Tuple.Create(leitor.GetInt32("id_local_estoque"),
                            leitor.GetInt32("id_produto"), quantidade));
                    }
            }
            if (saldos.Count == 0)
                return;

            int idCentral = 0;
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT l.id_local_estoque FROM vw_localestoque l " +
                "JOIN evento ev ON ev.id_instituicao=l.id_instituicao " +
                "WHERE ev.id_evento=@evento AND l.eh_central=1 AND l.ativo=1 FOR UPDATE", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@evento", idEvento);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                    while (leitor.Read())
                    {
                        if (idCentral != 0)
                            throw new InvalidOperationException("Confira o cadastro do estoque central da instituição antes de continuar.");
                        idCentral = leitor.GetInt32(0);
                    }
            }
            if (idCentral == 0)
                throw new InvalidOperationException("Inicialize o estoque central da instituição antes de encerrar o evento.");

            Dictionary<int, int> transferencias = new Dictionary<int, int>();
            List<Tuple<int, int, decimal>> ajustes = new List<Tuple<int, int, decimal>>();
            foreach (var saldo in saldos)
            {
                int idTransferencia;
                if (!transferencias.TryGetValue(saldo.Item1, out idTransferencia))
                {
                    int resultadoBanco7;
                    using (MySqlCommand comandoBanco7 = new MySqlCommand("INSERT INTO transferenciaestoque " +
                        "(id_local_origem,id_local_destino,id_usuario_responsavel,data_hora,observacao,chave_operacao,criado_em) " +
                        "VALUES (@origem,@destino,@usuario,NOW(),@observacao,@chave,NOW())", conexao, transacao))
                    {
                        comandoBanco7.Parameters.AddWithValue("@origem", saldo.Item1);
                        comandoBanco7.Parameters.AddWithValue("@destino", idCentral);
                        comandoBanco7.Parameters.AddWithValue("@usuario", idUsuario);
                        comandoBanco7.Parameters.AddWithValue("@observacao", "Devolução dos saldos ao encerrar evento sem vendas ou perdas registradas.");
                        comandoBanco7.Parameters.AddWithValue("@chave", Guid.NewGuid().ToString());
                        comandoBanco7.ExecuteNonQuery();
                        resultadoBanco7 = Convert.ToInt32(comandoBanco7.LastInsertedId);
                    }
                    idTransferencia = resultadoBanco7;
                    transferencias.Add(saldo.Item1, idTransferencia);
                }
                using (MySqlCommand comandoBanco8 = new MySqlCommand("INSERT INTO transferenciaitem (id_transferencia,id_produto,quantidade) VALUES (@transferencia,@produto,@quantidade)", conexao, transacao))
                {
                    comandoBanco8.Parameters.AddWithValue("@transferencia", idTransferencia);
                    comandoBanco8.Parameters.AddWithValue("@produto", saldo.Item2);
                    comandoBanco8.Parameters.AddWithValue("@quantidade", saldo.Item3);
                    comandoBanco8.ExecuteNonQuery();
                }

                ajustes.Add(Tuple.Create(saldo.Item1, saldo.Item2, -saldo.Item3));
                ajustes.Add(Tuple.Create(idCentral, saldo.Item2, saldo.Item3));
            }
            ControleSaldoEstoque.AplicarAjustes(conexao, transacao, ajustes);
        }

        public void ExcluirCancelado(int idEvento, uint versao)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand comando = new MySqlCommand(
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

                        using (MySqlCommand comando = new MySqlCommand(
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
            using (MySqlCommand comando = new MySqlCommand(sql, conexao, transacao))
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
