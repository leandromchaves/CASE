using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class RegistroRepository
    {
        private const string Colunas = "id_registro,id_escala,id_usuario_responsavel,tipo,forma_pagamento,data_hora,observacao,chave_operacao,criado_em,id_usuario_alteracao,atualizado_em,versao";

        public List<Registro> ListarPorEscala(int idEscala)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM registro WHERE id_escala=@id ORDER BY data_hora DESC,id_registro DESC", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEscala);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Registro> resultadoBanco = new List<Registro>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Registro BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM registro WHERE id_registro=@id", conexaoBanco))
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

        public int RegistrarComItens(Registro registro, IList<RegistroItem> itens)
        {
            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            if (itens == null)
            {
                throw new ArgumentNullException(nameof(itens));
            }

            if (itens.Count == 0)
            {
                throw new ArgumentException("O registro deve possuir pelo menos um item.", nameof(itens));
            }

            if (string.IsNullOrWhiteSpace(registro.ChaveOperacao))
            {
                registro.ChaveOperacao = Guid.NewGuid().ToString();
            }

            if (!registro.CriadoEm.HasValue)
            {
                registro.CriadoEm = DateTime.Now;
            }

            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAutorNaTransacao(conexao, transacao, registro);
                        int? registroExistente = BuscarRepeticao(conexao, transacao, registro, itens);
                        if (registroExistente.HasValue)
                        {
                            transacao.Commit();
                            return registroExistente.Value;
                        }
                        int idSetor;
                        int idLocalEstoque;
                        BuscarSetorELocalDaEscala(
                            conexao,
                            transacao,
                            registro.IdEscala,
                            out idSetor,
                            out idLocalEstoque);

                        VerificarEventoEmAndamento(
                            conexao,
                            transacao,
                            registro.IdEscala);

                        int resultadoBanco2;
                        using (MySqlCommand comandoBanco2 = new MySqlCommand("INSERT INTO registro (id_escala,id_usuario_responsavel,tipo,forma_pagamento,data_hora,observacao,chave_operacao,criado_em) VALUES (@escala,@usuario,@tipo,@pagamento,@data,@observacao,@chave,@criadoEm)", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco2, "@escala", registro.IdEscala);
                            AcessoDados.AdicionarParametro(comandoBanco2, "@usuario", registro.IdUsuarioResponsavel);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco2, "@tipo", registro.Tipo);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco2, "@pagamento", registro.FormaPagamento);
                            AcessoDados.AdicionarParametro(comandoBanco2, "@data", registro.DataHora);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco2, "@observacao", registro.Observacao);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco2, "@chave", registro.ChaveOperacao);
                            AcessoDados.AdicionarParametro(comandoBanco2, "@criadoEm", registro.CriadoEm);
                            comandoBanco2.ExecuteNonQuery();
                            resultadoBanco2 = Convert.ToInt32(comandoBanco2.LastInsertedId);
                        }
                        int id = resultadoBanco2;

                        List<Tuple<int, int, decimal>> ajustesSaldo =
                            new List<Tuple<int, int, decimal>>();

                        foreach (RegistroItem item in itens)
                        {
                            if (item == null)
                            {
                                throw new ArgumentException("A lista contem um item nulo.", nameof(itens));
                            }

                            ValidarQuantidade(item.Quantidade);
                            int idProduto = BuscarProdutoDoSetor(
                                conexao,
                                transacao,
                                item.IdProdutoSetor,
                                idSetor);

                            using (MySqlCommand comandoBanco3 = new MySqlCommand("INSERT INTO registroitem (id_registro,id_produto_setor,quantidade,preco_unitario_praticado) VALUES (@registro,@produtoSetor,@quantidade,@preco)", conexao, transacao))
                            {
                                AcessoDados.AdicionarParametro(comandoBanco3, "@registro", id);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@produtoSetor", item.IdProdutoSetor);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@quantidade", item.Quantidade);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@preco", item.PrecoUnitarioPraticado);
                                comandoBanco3.ExecuteNonQuery();
                            }

                            ajustesSaldo.Add(Tuple.Create(
                                idLocalEstoque,
                                idProduto,
                                -item.Quantidade));

                        }

                        ControleSaldoEstoque.AplicarAjustes(
                            conexao,
                            transacao,
                            ajustesSaldo);

                        transacao.Commit();
                        return id;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AtualizarComItens(
            Registro registro,
            IList<RegistroItem> itens,
            int idUsuarioAlteracao,
            string justificativa)
        {
            if (registro == null || itens == null || itens.Count == 0)
            {
                throw new ArgumentException("Informe o registro e seus itens para corrigir.");
            }
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string perfil = ControleCorrecaoOperacao.ValidarEditor(conexao, transacao, idUsuarioAlteracao);
                        Registro atual = BuscarParaAtualizacao(conexao, transacao, registro.IdRegistro);
                        if (atual == null)
                        {
                            throw new InvalidOperationException("O registro não foi encontrado.");
                        }
                        ControleCorrecaoOperacao.ValidarVersao(atual.Versao, registro.Versao, justificativa);
                        if (atual.IdEscala != registro.IdEscala)
                        {
                            throw new ArgumentException("A correção não pode mudar a escala original.");
                        }
                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT s.id_evento FROM escala e JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                            "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                            "WHERE e.id_escala=@escala AND eq.id_evento=s.id_evento " +
                            "AND (@perfil<>'Membro' OR (eq.ativo=1 AND eq.id_pessoa=@pessoa AND e.situacao<>'cancelada')) FOR UPDATE",
                            conexao,
transacao))
                        {
                            comando.Parameters.AddWithValue("@escala", atual.IdEscala);
                            comando.Parameters.AddWithValue("@perfil", perfil);
                            comando.Parameters.AddWithValue("@pessoa", SessaoUsuario.ObterUsuarioAtual().IdPessoa);
                            object evento = comando.ExecuteScalar();
                            if (evento == null)
                            {
                                throw new UnauthorizedAccessException("A escala não está autorizada para esta correção.");
                            }
                            ControleCorrecaoOperacao.ValidarEvento(conexao, transacao, Convert.ToInt32(evento),
                                perfil, true, atual.IdUsuarioResponsavel);
                        }
                        registro.Tipo = TiposRegistro.Normalizar(registro.Tipo);
                        if (registro.Tipo == TiposRegistro.Venda)
                        {
                            if (registro.FormaPagamento != "dinheiro" && registro.FormaPagamento != "pix" &&
                                registro.FormaPagamento != "cartao")
                            {
                                throw new ArgumentException("Selecione a forma de pagamento da venda.");
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(registro.Observacao))
                            {
                                throw new ArgumentException("Informe a justificativa da perda.");
                            }
                            registro.FormaPagamento = null;
                        }
                        string anterior = CriarSnapshot(conexao, transacao, atual.IdRegistro);
                        Dictionary<int, int> originais = ControleCorrecaoOperacao.LerItens(
conexao,
transacao,
                            "SELECT id_registro_item AS id_item,id_produto_setor AS id_produto FROM registroitem " +
                            "WHERE id_registro=@id FOR UPDATE",
atual.IdRegistro,
itens.Count);
                        HashSet<int> informados = new HashSet<int>();
                        List<Tuple<int, int, decimal>> ajustes = CarregarAjustesAnteriores(conexao, transacao, atual.IdRegistro);
                        int idSetor;
                        int idLocal;
                        BuscarSetorELocalDaEscala(conexao, transacao, atual.IdEscala, out idSetor, out idLocal);
                        foreach (RegistroItem item in itens)
                        {
                            if (item == null)
                            {
                                throw new ArgumentException("A correção contém um item nulo.");
                            }
                            if (registro.Tipo == TiposRegistro.Perda)
                            {
                                item.PrecoUnitarioPraticado = null;
                            }
                            ControleCorrecaoOperacao.ValidarItem(
originais,
informados,
item.IdRegistroItem,
                                item.IdProdutoSetor,
item.Quantidade,
item.PrecoUnitarioPraticado,
registro.Tipo == TiposRegistro.Venda,
true);
                            int produto = BuscarProdutoDoSetor(conexao, transacao, item.IdProdutoSetor, idSetor);
                            if (originais[item.IdRegistroItem] != item.IdProdutoSetor)
                            {
                                using (MySqlCommand comando = new MySqlCommand("SELECT ativo FROM produto WHERE id_produto=@id FOR UPDATE", conexao, transacao))
                                {
                                    comando.Parameters.AddWithValue("@id", produto);
                                    if (!Convert.ToBoolean(comando.ExecuteScalar()))
                                    {
                                        throw new InvalidOperationException("Selecione um produto ativo do mesmo setor para corrigir o registro.");
                                    }
                                }
                            }
                            ajustes.Add(Tuple.Create(idLocal, produto, -item.Quantidade));
                            using (MySqlCommand comandoBanco4 = new MySqlCommand("UPDATE registroitem SET id_produto_setor=@produto,quantidade=@quantidade,preco_unitario_praticado=@preco " +
                                "WHERE id_registro_item=@item AND id_registro=@registro", conexao, transacao))
                            {
                                comandoBanco4.Parameters.AddWithValue("@produto", item.IdProdutoSetor);
                                comandoBanco4.Parameters.AddWithValue("@quantidade", item.Quantidade);
                                AcessoDados.AdicionarParametro(comandoBanco4, "@preco", item.PrecoUnitarioPraticado);
                                comandoBanco4.Parameters.AddWithValue("@item", item.IdRegistroItem);
                                comandoBanco4.Parameters.AddWithValue("@registro", atual.IdRegistro);
                                comandoBanco4.ExecuteNonQuery();
                            }

                        }
                        ControleSaldoEstoque.AplicarAjustes(conexao, transacao, ajustes, true);
                        using (MySqlCommand comandoBanco5 = new MySqlCommand("UPDATE registro SET tipo=@tipo,forma_pagamento=@pagamento,observacao=@observacao," +
                            "id_usuario_alteracao=@usuario,atualizado_em=NOW(),versao=versao+1 WHERE id_registro=@id", conexao, transacao))
                        {
                            comandoBanco5.Parameters.AddWithValue("@tipo", registro.Tipo);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@pagamento", registro.FormaPagamento);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@observacao", registro.Observacao);
                            comandoBanco5.Parameters.AddWithValue("@usuario", idUsuarioAlteracao);
                            comandoBanco5.Parameters.AddWithValue("@id", atual.IdRegistro);
                            comandoBanco5.ExecuteNonQuery();
                        }

                        RegistrarHistorico(
conexao,
transacao,
atual.IdRegistro,
idUsuarioAlteracao,
                            justificativa.Trim(),
anterior,
CriarSnapshot(conexao, transacao, atual.IdRegistro));
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

        private static void ValidarAutorNaTransacao(MySqlConnection conexao,
            MySqlTransaction transacao, Registro registro)
        {
            Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
            if (sessao.IdUsuario != registro.IdUsuarioResponsavel)
            {
                throw new UnauthorizedAccessException("O responsável deve corresponder à sessão autenticada.");
            }
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT u.id_usuario FROM usuario u " +
                "JOIN perfilsistema pf ON pf.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE u.id_usuario=@usuario AND u.situacao_conta='aprovado' " +
                "AND (pf.nome='Administrador' OR (pf.nome='Membro' AND EXISTS (" +
                "SELECT 1 FROM equipeevento eq " +
                "JOIN escala e ON e.id_equipe=eq.id_equipe " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                "JOIN setor s ON s.id_setor=j.id_setor " +
                "WHERE eq.id_pessoa=u.id_pessoa AND eq.ativo=1 AND e.id_escala=@escala " +
                "AND s.id_evento=eq.id_evento AND e.situacao<>'cancelada'))) FOR UPDATE",
                conexao, transacao))
            {
                comando.Parameters.AddWithValue("@usuario", registro.IdUsuarioResponsavel);
                comando.Parameters.AddWithValue("@escala", registro.IdEscala);
                if (comando.ExecuteScalar() == null)
                {
                    throw new UnauthorizedAccessException("A conta ou a escala não está autorizada para registrar esta operação.");
                }
            }
        }

        private static int? BuscarRepeticao(MySqlConnection conexao, MySqlTransaction transacao,
            Registro registro, IList<RegistroItem> itens)
        {
            int id;
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_registro,id_escala,id_usuario_responsavel,tipo,forma_pagamento,observacao " +
                "FROM registro WHERE chave_operacao=@chave FOR UPDATE", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@chave", registro.ChaveOperacao);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (!leitor.Read())
                    {
                        return null;
                    }
                    id = leitor.GetInt32("id_registro");
                    if (leitor.GetInt32("id_escala") != registro.IdEscala ||
                        leitor.GetInt32("id_usuario_responsavel") != registro.IdUsuarioResponsavel ||
                        leitor.GetString("tipo") != registro.Tipo ||
                        Convert.ToString(leitor["forma_pagamento"]) != Convert.ToString(registro.FormaPagamento) ||
                        Convert.ToString(leitor["observacao"]).Trim() != Convert.ToString((object)registro.Observacao).Trim())
                    {
                        throw new InvalidOperationException("A chave já foi usada em outra operação. Atualize a tela.");
                    }
                }
            }
            int encontrados = 0;
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_produto_setor,quantidade,preco_unitario_praticado FROM registroitem " +
                "WHERE id_registro=@id", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        bool corresponde = false;
                        foreach (RegistroItem item in itens)
                        {
                            if (item.IdProdutoSetor == leitor.GetInt32("id_produto_setor") &&
                                item.Quantidade == leitor.GetDecimal("quantidade") &&
                                item.PrecoUnitarioPraticado == AcessoDados.LerDecimalOpcional(leitor, "preco_unitario_praticado"))
                            {
                                corresponde = true;
                                break;
                            }
                        }
                        if (!corresponde)
                        {
                            throw new InvalidOperationException("Os itens diferem da operação já confirmada.");
                        }
                        encontrados++;
                    }
                }
            }
            if (encontrados != itens.Count)
            {
                throw new InvalidOperationException("Os itens diferem da operação já confirmada.");
            }
            return id;
        }

        private static Registro BuscarParaAtualizacao(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idRegistro)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_registro,id_escala,id_usuario_responsavel,tipo,forma_pagamento,data_hora,observacao," +
                "chave_operacao,criado_em,id_usuario_alteracao,atualizado_em,versao " +
                "FROM registro WHERE id_registro=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idRegistro);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (!leitor.Read())
                    {
                        return null;
                    }

                    return ConverterLinha(leitor);
                }
            }
        }

        private static void VerificarEventoEmAndamento(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEscala)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ev.situacao FROM escala e " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                "JOIN setor s ON s.id_setor=j.id_setor " +
                "JOIN evento ev ON ev.id_evento=s.id_evento " +
                "WHERE e.id_escala=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEscala);
                object situacao = comando.ExecuteScalar();

                if (situacao == null || situacao == DBNull.Value ||
                    !string.Equals(
                        Convert.ToString(situacao),
                        SituacoesEvento.EmAndamento,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Só é possível editar registros enquanto o evento está em andamento.");
                }
            }
        }

        private static List<Tuple<int, int, decimal>> CarregarAjustesAnteriores(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idRegistro)
        {
            int idEscala;
            int idSetor;
            int idLocalEstoque;

            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_escala FROM registro WHERE id_registro=@id",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idRegistro);
                idEscala = Convert.ToInt32(comando.ExecuteScalar());
            }

            BuscarSetorELocalDaEscala(
                conexao,
                transacao,
                idEscala,
                out idSetor,
                out idLocalEstoque);

            List<Tuple<int, int, decimal>> ajustes = new List<Tuple<int, int, decimal>>();

            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ps.id_produto,ri.quantidade " +
                "FROM registroitem ri " +
                "JOIN produtosetor ps ON ps.id_produto_setor=ri.id_produto_setor " +
                "WHERE ri.id_registro=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idRegistro);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        ajustes.Add(Tuple.Create(
                            idLocalEstoque,
                            leitor.GetInt32("id_produto"),
                            leitor.GetDecimal("quantidade")));
                    }
                }
            }

            return ajustes;
        }

        private static string CriarSnapshot(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idRegistro)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT CAST(JSON_OBJECT(" +
                "'id_escala',r.id_escala,'id_usuario_responsavel',r.id_usuario_responsavel," +
                "'tipo',r.tipo,'forma_pagamento',r.forma_pagamento,'data_hora',r.data_hora,'observacao',r.observacao," +
                "'versao',r.versao,'id_usuario_alteracao',r.id_usuario_alteracao,'atualizado_em',r.atualizado_em," +
                "'itens',(SELECT JSON_ARRAYAGG(JSON_OBJECT(" +
                "'id_item',ri.id_registro_item,'id_produto_setor',ri.id_produto_setor,'quantidade',ri.quantidade," +
                "'preco',ri.preco_unitario_praticado)) FROM registroitem ri " +
                "WHERE ri.id_registro=r.id_registro)) AS CHAR) " +
                "FROM registro r WHERE r.id_registro=@id",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idRegistro);
                return Convert.ToString(comando.ExecuteScalar());
            }
        }

        private static void RegistrarHistorico(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idRegistro,
            int idUsuario,
            string justificativa,
            string dadosAnteriores,
            string dadosNovos)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "INSERT INTO historicoalteracaooperacao " +
                "(entidade,id_entidade,id_usuario,justificativa,dados_anteriores,dados_novos) " +
                "VALUES ('registro',@id,@usuario,@justificativa,CAST(@anterior AS JSON),CAST(@novo AS JSON))",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", idRegistro);
                AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);
                AcessoDados.AdicionarTextoOpcional(comando, "@justificativa", justificativa);
                comando.Parameters.AddWithValue("@anterior", dadosAnteriores);
                comando.Parameters.AddWithValue("@novo", dadosNovos);
                comando.ExecuteNonQuery();
            }
        }

        private static void BuscarSetorELocalDaEscala(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEscala,
            out int idSetor,
            out int idLocalEstoque)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT j.id_setor,es.id_local_estoque " +
                "FROM escala e " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                "JOIN estoquesetor es ON es.id_setor=j.id_setor " +
                "WHERE e.id_escala=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEscala);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (!leitor.Read())
                    {
                        throw new InvalidOperationException(
                            "A escala não possui um setor ligado a um local de estoque.");
                    }

                    idSetor = leitor.GetInt32("id_setor");
                    idLocalEstoque = leitor.GetInt32("id_local_estoque");
                }
            }
        }

        private static int BuscarProdutoDoSetor(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idProdutoSetor,
            int idSetorEsperado)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_produto,id_setor FROM produtosetor " +
                "WHERE id_produto_setor=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idProdutoSetor);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (!leitor.Read())
                    {
                        throw new InvalidOperationException("O produto do registro não foi encontrado.");
                    }

                    if (leitor.GetInt32("id_setor") != idSetorEsperado)
                    {
                        throw new InvalidOperationException(
                            "O produto informado não pertence ao setor da escala.");
                    }

                    return leitor.GetInt32("id_produto");
                }
            }
        }

        private static void ValidarQuantidade(decimal quantidade)
        {
            if (quantidade <= 0 || quantidade != decimal.Truncate(quantidade))
            {
                throw new ArgumentException(
                    "A quantidade registrada deve ser um número inteiro maior que zero.");
            }
        }

        private static Registro ConverterLinha(MySqlDataReader leitor)
        {
            return new Registro
            {
                IdRegistro = leitor.GetInt32("id_registro"),
                IdEscala = leitor.GetInt32("id_escala"),
                IdUsuarioResponsavel = leitor.GetInt32("id_usuario_responsavel"),
                Tipo = leitor.GetString("tipo"),
                FormaPagamento = AcessoDados.LerTextoOpcional(leitor, "forma_pagamento"),
                DataHora = leitor.GetDateTime("data_hora"),
                Observacao = AcessoDados.LerTextoOpcional(leitor, "observacao"),
                ChaveOperacao = AcessoDados.LerTextoOpcional(leitor, "chave_operacao"),
                CriadoEm = AcessoDados.LerDataHoraOpcional(leitor, "criado_em"),
                IdUsuarioAlteracao = AcessoDados.LerInteiroOpcional(leitor, "id_usuario_alteracao"),
                AtualizadoEm = AcessoDados.LerDataHoraOpcional(leitor, "atualizado_em"),
                Versao = leitor.GetUInt32("versao")
            };
        }
    }
}
