using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EntradaMercadoriaRepository
    {
        private const string Colunas = "id_entrada,id_fornecedor,id_local_destino,id_usuario_responsavel,tipo_entrada,data_hora,numero_documento,observacao,chave_operacao,criado_em,id_usuario_alteracao,atualizado_em,versao";

        public List<EntradaMercadoria> ListarPorLocalDestino(int idLocal)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM entradamercadoria WHERE id_local_destino=@id ORDER BY data_hora DESC,id_entrada DESC", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idLocal);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<EntradaMercadoria> resultadoBanco = new List<EntradaMercadoria>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public EntradaMercadoria BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM entradamercadoria WHERE id_entrada=@id", conexaoBanco))
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

        public int CadastrarComItens(EntradaMercadoria entrada, IList<EntradaItem> itens)
        {
            if (entrada == null)
            {
                throw new ArgumentNullException(nameof(entrada));
            }

            if (itens == null)
            {
                throw new ArgumentNullException(nameof(itens));
            }

            if (itens.Count == 0)
            {
                throw new ArgumentException("A entrada deve possuir pelo menos um item.", nameof(itens));
            }

            if (string.IsNullOrWhiteSpace(entrada.ChaveOperacao))
            {
                entrada.ChaveOperacao = Guid.NewGuid().ToString();
            }

            if (!entrada.CriadoEm.HasValue)
            {
                entrada.CriadoEm = DateTime.Now;
            }

            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarAdministradorEEstoqueCentral(
                            conexao,
                            transacao,
                            entrada.IdUsuarioResponsavel,
                            entrada.IdLocalDestino);

                        int resultadoBanco2;
                        using (MySqlCommand comandoBanco2 = new MySqlCommand("INSERT INTO entradamercadoria (id_fornecedor,id_local_destino,id_usuario_responsavel,tipo_entrada,data_hora,numero_documento,observacao,chave_operacao,criado_em) VALUES (@fornecedor,@local,@usuario,@tipo,@data,@documento,@observacao,@chave,@criadoEm)", conexao, transacao))
                        {
                            Vincular(comandoBanco2, entrada);
                            comandoBanco2.ExecuteNonQuery();
                            resultadoBanco2 = Convert.ToInt32(comandoBanco2.LastInsertedId);
                        }
                        int id = resultadoBanco2;
                        List<Tuple<int, int, decimal>> ajustesSaldo =
                            new List<Tuple<int, int, decimal>>();

                        foreach (EntradaItem item in itens)
                        {
                            if (item == null)
                            {
                                throw new ArgumentException("A lista contem um item nulo.", nameof(itens));
                            }

                            ValidarQuantidade(item.Quantidade);

                            using (MySqlCommand comandoBanco3 = new MySqlCommand("INSERT INTO entradaitem (id_entrada,id_produto,quantidade,custo_unitario) VALUES (@entrada,@produto,@quantidade,@custo)", conexao, transacao))
                            {
                                AcessoDados.AdicionarParametro(comandoBanco3, "@entrada", id);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@produto", item.IdProduto);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@quantidade", item.Quantidade);
                                AcessoDados.AdicionarParametro(comandoBanco3, "@custo", item.CustoUnitario);
                                comandoBanco3.ExecuteNonQuery();
                            }

                            ajustesSaldo.Add(Tuple.Create(
                                entrada.IdLocalDestino,
                                item.IdProduto,
                                item.Quantidade));

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
            EntradaMercadoria entrada,
            IList<EntradaItem> itens,
            int idUsuarioAlteracao,
            string justificativa)
        {
            if (entrada == null || itens == null || itens.Count == 0)
            {
                throw new ArgumentException("Informe a entrada e seus itens para corrigir.");
            }
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        string perfil = ControleCorrecaoOperacao.ValidarEditor(conexao, transacao, idUsuarioAlteracao);
                        if (perfil != "Administrador")
                        {
                            throw new UnauthorizedAccessException("Somente administradores podem corrigir entradas do central.");
                        }
                        EntradaMercadoria atual = BuscarParaAtualizacao(conexao, transacao, entrada.IdEntrada);
                        if (atual == null)
                        {
                            throw new InvalidOperationException("A entrada não foi encontrada.");
                        }
                        ControleCorrecaoOperacao.ValidarVersao(atual.Versao, entrada.Versao, justificativa);
                        if (atual.IdLocalDestino != entrada.IdLocalDestino || atual.IdFornecedor != entrada.IdFornecedor ||
                            atual.TipoEntrada != entrada.TipoEntrada)
                        {
                            throw new ArgumentException("A correção deve manter local, fornecedor e tipo originais da entrada.");
                        }
                        if (entrada.NumeroDocumento != null && entrada.NumeroDocumento.Length > 50)
                        {
                            throw new ArgumentException("O número do documento pode ter até 50 caracteres.");
                        }
                        ValidarAdministradorEEstoqueCentral(conexao, transacao, idUsuarioAlteracao, atual.IdLocalDestino);
                        string anterior = CriarSnapshot(conexao, transacao, atual.IdEntrada);
                        Dictionary<int, int> originais = ControleCorrecaoOperacao.LerItens(
conexao,
transacao,
                            "SELECT id_entrada_item AS id_item,id_produto FROM entradaitem WHERE id_entrada=@id FOR UPDATE",
                            atual.IdEntrada,
itens.Count);
                        HashSet<int> informados = new HashSet<int>();
                        List<Tuple<int, int, decimal>> ajustes = CarregarAjustesAnteriores(conexao, transacao, atual.IdEntrada);
                        foreach (EntradaItem item in itens)
                        {
                            if (item == null)
                            {
                                throw new ArgumentException("A correção contém um item nulo.");
                            }
                            ControleCorrecaoOperacao.ValidarItem(originais, informados, item.IdEntradaItem,
                                item.IdProduto, item.Quantidade, item.CustoUnitario, false);
                            ajustes.Add(Tuple.Create(atual.IdLocalDestino, item.IdProduto, item.Quantidade));
                            using (MySqlCommand comandoBanco4 = new MySqlCommand("UPDATE entradaitem SET quantidade=@quantidade,custo_unitario=@custo " +
                                "WHERE id_entrada_item=@item AND id_entrada=@entrada", conexao, transacao))
                            {
                                comandoBanco4.Parameters.AddWithValue("@quantidade", item.Quantidade);
                                AcessoDados.AdicionarParametro(comandoBanco4, "@custo", item.CustoUnitario);
                                comandoBanco4.Parameters.AddWithValue("@item", item.IdEntradaItem);
                                comandoBanco4.Parameters.AddWithValue("@entrada", atual.IdEntrada);
                                comandoBanco4.ExecuteNonQuery();
                            }

                        }
                        ControleSaldoEstoque.AplicarAjustes(conexao, transacao, ajustes, true);
                        using (MySqlCommand comandoBanco5 = new MySqlCommand("UPDATE entradamercadoria SET observacao=@observacao,numero_documento=@documento," +
                            "id_usuario_alteracao=@usuario,atualizado_em=NOW(),versao=versao+1 WHERE id_entrada=@id", conexao, transacao))
                        {
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@observacao", entrada.Observacao);
                            AcessoDados.AdicionarTextoOpcional(comandoBanco5, "@documento", entrada.NumeroDocumento);
                            comandoBanco5.Parameters.AddWithValue("@usuario", idUsuarioAlteracao);
                            comandoBanco5.Parameters.AddWithValue("@id", atual.IdEntrada);
                            comandoBanco5.ExecuteNonQuery();
                        }

                        RegistrarHistorico(
conexao,
transacao,
atual.IdEntrada,
idUsuarioAlteracao,
                            justificativa.Trim(),
anterior,
CriarSnapshot(conexao, transacao, atual.IdEntrada));
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

        private static EntradaMercadoria BuscarParaAtualizacao(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEntrada)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT " + Colunas + " FROM entradamercadoria WHERE id_entrada=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEntrada);

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

        private static List<Tuple<int, int, decimal>> CarregarAjustesAnteriores(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEntrada)
        {
            int idLocal;
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_local_destino FROM entradamercadoria WHERE id_entrada=@id",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEntrada);
                idLocal = Convert.ToInt32(comando.ExecuteScalar());
            }

            List<Tuple<int, int, decimal>> ajustes = new List<Tuple<int, int, decimal>>();

            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_produto,quantidade FROM entradaitem WHERE id_entrada=@id FOR UPDATE",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEntrada);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        ajustes.Add(Tuple.Create(
                            idLocal,
                            leitor.GetInt32("id_produto"),
                            -leitor.GetDecimal("quantidade")));
                    }
                }
            }

            return ajustes;
        }

        private static string CriarSnapshot(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEntrada)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT CAST(JSON_OBJECT('id_fornecedor',em.id_fornecedor," +
                "'id_local_destino',em.id_local_destino,'id_usuario_responsavel',em.id_usuario_responsavel," +
                "'tipo_entrada',em.tipo_entrada,'data_hora',em.data_hora,'numero_documento',em.numero_documento," +
                "'observacao',em.observacao,'versao',em.versao,'id_usuario_alteracao',em.id_usuario_alteracao," +
                "'atualizado_em',em.atualizado_em,'itens',(SELECT JSON_ARRAYAGG(JSON_OBJECT(" +
                "'id_item',ei.id_entrada_item,'id_produto',ei.id_produto,'quantidade',ei.quantidade,'custo_unitario',ei.custo_unitario)) " +
                "FROM entradaitem ei WHERE ei.id_entrada=em.id_entrada)) AS CHAR) " +
                "FROM entradamercadoria em WHERE em.id_entrada=@id",
                conexao,
                transacao))
            {
                comando.Parameters.AddWithValue("@id", idEntrada);
                return Convert.ToString(comando.ExecuteScalar());
            }
        }

        private static void RegistrarHistorico(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEntrada,
            int idUsuario,
            string justificativa,
            string dadosAnteriores,
            string dadosNovos)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "INSERT INTO historicoalteracaooperacao " +
                "(entidade,id_entidade,id_usuario,justificativa,dados_anteriores,dados_novos) " +
                "VALUES ('entradamercadoria',@id,@usuario,@justificativa,CAST(@anterior AS JSON),CAST(@novo AS JSON))",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", idEntrada);
                AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);
                AcessoDados.AdicionarTextoOpcional(comando, "@justificativa", justificativa);
                comando.Parameters.AddWithValue("@anterior", dadosAnteriores);
                comando.Parameters.AddWithValue("@novo", dadosNovos);
                comando.ExecuteNonQuery();
            }
        }

        private static void Vincular(MySqlCommand comando, EntradaMercadoria item)
        {
            AcessoDados.AdicionarParametro(comando, "@fornecedor", item.IdFornecedor);
            AcessoDados.AdicionarParametro(comando, "@local", item.IdLocalDestino);
            AcessoDados.AdicionarParametro(comando, "@usuario", item.IdUsuarioResponsavel);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@tipo", item.TipoEntrada);
            AcessoDados.AdicionarParametro(comando, "@data", item.DataHora);
            AcessoDados.AdicionarTextoOpcional(comando, "@documento", item.NumeroDocumento);
            AcessoDados.AdicionarTextoOpcional(comando, "@observacao", item.Observacao);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@chave", item.ChaveOperacao);
            AcessoDados.AdicionarParametro(comando, "@criadoEm", item.CriadoEm);
        }

        private static void ValidarAdministradorEEstoqueCentral(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idUsuario,
            int idLocal)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT le.id_local_estoque FROM vw_localestoque le " +
                "JOIN usuario u ON u.id_usuario=@usuario " +
                "JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE le.id_local_estoque=@local AND le.eh_central=1 " +
                "AND le.eh_geral_evento=0 AND le.id_evento IS NULL AND le.ativo=1 " +
                "AND u.situacao_conta='aprovado' AND ps.nome='Administrador' " +
                "FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);
                AcessoDados.AdicionarParametro(comando, "@local", idLocal);

                if (comando.ExecuteScalar() == null)
                {
                    throw new UnauthorizedAccessException(
                        "A entrada no estoque central exige administrador aprovado e local central ativo.");
                }
            }
        }

        private static void ValidarQuantidade(decimal quantidade)
        {
            if (quantidade <= 0 || quantidade != decimal.Truncate(quantidade))
            {
                throw new ArgumentException(
                    "A quantidade da entrada deve ser um número inteiro maior que zero.");
            }
        }

        private static EntradaMercadoria ConverterLinha(MySqlDataReader leitor)
        {
            return new EntradaMercadoria
            {
                IdEntrada = leitor.GetInt32("id_entrada"),
                IdFornecedor = AcessoDados.LerInteiroOpcional(leitor, "id_fornecedor"),
                IdLocalDestino = leitor.GetInt32("id_local_destino"),
                IdUsuarioResponsavel = leitor.GetInt32("id_usuario_responsavel"),
                TipoEntrada = leitor.GetString("tipo_entrada"),
                DataHora = leitor.GetDateTime("data_hora"),
                NumeroDocumento = AcessoDados.LerTextoOpcional(leitor, "numero_documento"),
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
