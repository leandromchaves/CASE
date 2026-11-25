using System.Collections.Generic;
using System.Data;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ApuracaoRepository
    {
        private const string Colunas =
            "id_apuracao,id_jornada,valor_informado,data_hora,id_usuario_responsavel,observacao,valor_dinheiro,valor_pix,valor_cartao";

        public int Cadastrar(Apuracao item, int? ultimaConferenciaEsperada = null)
        {
            // A conferência é sempre um novo registro; nunca substitui a anterior.
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarResponsavel(conexao, transacao, item);
                        int ultima = 0;
                        using (MySqlCommand consulta = new MySqlCommand(
                            "SELECT id_apuracao FROM vw_apuracao WHERE id_jornada=@jornada " +
                            "ORDER BY id_apuracao DESC LIMIT 1 FOR UPDATE", conexao, transacao))
                        {
                            consulta.Parameters.AddWithValue("@jornada", item.IdJornada);
                            object valor = consulta.ExecuteScalar();
                            if (valor != null)
                            {
                                ultima = Convert.ToInt32(valor);
                            }
                        }
                        if (!ultimaConferenciaEsperada.HasValue || ultima != ultimaConferenciaEsperada.Value)
                        {
                            throw new InvalidOperationException("Outra conferência foi registrada. Atualize a jornada antes de conferir novamente.");
                        }
                        using (MySqlCommand comando = new MySqlCommand(
                            "INSERT INTO apuracao (id_jornada,data_hora,id_usuario_responsavel," +
                            "observacao,valor_dinheiro,valor_pix,valor_cartao) " +
                            "VALUES (@jornada,NOW(),@usuario,@observacao,@dinheiro,@pix,@cartao)",
conexao,
transacao))
                        {
                            comando.Parameters.AddWithValue("@jornada", item.IdJornada);
                            comando.Parameters.AddWithValue("@usuario", item.IdUsuarioResponsavel);
                            AcessoDados.AdicionarTextoOpcional(comando, "@observacao", item.Observacao);
                            comando.Parameters.AddWithValue("@dinheiro", item.ValorDinheiro);
                            comando.Parameters.AddWithValue("@pix", item.ValorPix);
                            comando.Parameters.AddWithValue("@cartao", item.ValorCartao);
                            comando.ExecuteNonQuery();
                            item.IdApuracao = checked((int)comando.LastInsertedId);
                        }
                        transacao.Commit();
                        return item.IdApuracao;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void ValidarResponsavel(MySqlConnection conexao, MySqlTransaction transacao, Apuracao item)
        {
            Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
            if (sessao.IdUsuario != item.IdUsuarioResponsavel)
            {
                throw new UnauthorizedAccessException("A identidade da conferência não corresponde à sessão.");
            }
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ps.nome FROM usuario u JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE u.id_usuario=@usuario AND u.id_pessoa=@pessoa AND u.id_perfil_sistema=@perfil " +
                "AND u.situacao_conta='aprovado' FOR UPDATE",
conexao,
transacao))
            {
                comando.Parameters.AddWithValue("@usuario", sessao.IdUsuario);
                comando.Parameters.AddWithValue("@pessoa", sessao.IdPessoa);
                comando.Parameters.AddWithValue("@perfil", sessao.IdPerfilSistema);
                string perfil = Convert.ToString(comando.ExecuteScalar());
                if (!string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Sua conta não pode registrar conferências.");
                }
                comando.CommandText = "SELECT ev.id_responsavel FROM jornadasetor j " +
                    "JOIN setor s ON s.id_setor=j.id_setor JOIN evento ev ON ev.id_evento=s.id_evento " +
                    "WHERE j.id_jornada=@jornada FOR UPDATE";
                comando.Parameters.AddWithValue("@jornada", item.IdJornada);
                object responsavel = comando.ExecuteScalar();
                if (responsavel == null || string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase) &&
                    Convert.ToInt32(responsavel) != sessao.IdPessoa)
                {
                    throw new UnauthorizedAccessException("Você não pode conferir esta jornada.");
                }
            }
            if (!item.ValorDinheiro.HasValue || !item.ValorPix.HasValue || !item.ValorCartao.HasValue ||
                item.ValorDinheiro < 0 || item.ValorPix < 0 || item.ValorCartao < 0 ||
                item.ValorInformado != item.ValorDinheiro + item.ValorPix + item.ValorCartao ||
                item.ValorInformado > 99999999.99M ||
                decimal.Round(item.ValorDinheiro.Value, 2) != item.ValorDinheiro.Value ||
                decimal.Round(item.ValorPix.Value, 2) != item.ValorPix.Value ||
                decimal.Round(item.ValorCartao.Value, 2) != item.ValorCartao.Value)
            {
                throw new ArgumentException("Os valores da conferência são inválidos.");
            }
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT COUNT(*) FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                "JOIN registroitem ri ON ri.id_registro=r.id_registro " +
                "WHERE e.id_jornada=@jornada AND r.tipo='venda' AND " +
                "(ri.preco_unitario_praticado IS NULL OR r.forma_pagamento IS NULL " +
                "OR r.forma_pagamento NOT IN ('dinheiro','pix','cartao'))", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@jornada", item.IdJornada);
                if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
                {
                    throw new InvalidOperationException("Há vendas sem preço ou modalidade válida. Regularize os registros antes de conferir.");
                }
            }
        }

        public DataTable VendasPorJornada(int idJornada)
        {
            return ConsultarJornada("SELECT r.id_registro,r.data_hora,p.nome AS responsavel,s.nome AS setor," +
                "t.nome AS turno,ri.quantidade,ri.preco_unitario_praticado,r.forma_pagamento," +
                "ri.quantidade*ri.preco_unitario_praticado AS total " +
                "FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN setor s ON s.id_setor=j.id_setor " +
                "JOIN turno t ON t.id_turno=j.id_turno JOIN usuario u ON u.id_usuario=r.id_usuario_responsavel " +
                "JOIN pessoa p ON p.id_pessoa=u.id_pessoa JOIN registroitem ri ON ri.id_registro=r.id_registro " +
                "WHERE j.id_jornada=@jornada AND r.tipo='venda' ORDER BY r.data_hora,r.id_registro", idJornada);
        }

        public DataTable ResumoPorJornada(int idJornada)
        {
            return ConsultarJornada("SELECT r.forma_pagamento," +
                "SUM(ri.quantidade*ri.preco_unitario_praticado) AS total," +
                "SUM(ri.preco_unitario_praticado IS NULL) AS itens_sem_preco " +
                "FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                "JOIN registroitem ri ON ri.id_registro=r.id_registro " +
                "WHERE e.id_jornada=@jornada AND r.tipo='venda' GROUP BY r.forma_pagamento", idJornada);
        }

        private static DataTable ConsultarJornada(string sql, int idJornada)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@jornada", idJornada);
                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                {
                    DataTable tabela = new DataTable();
                    adaptador.Fill(tabela);
                    return tabela;
                }
            }
        }

        public List<Apuracao> ListarPorJornada(int idJornada)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM vw_apuracao WHERE id_jornada=@id ORDER BY id_apuracao", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idJornada);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Apuracao> resultadoBanco = new List<Apuracao>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Apuracao BuscarPorId(int idApuracao)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT " + Colunas + " FROM vw_apuracao WHERE id_apuracao=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idApuracao);
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

        private static Apuracao ConverterLinha(MySqlDataReader leitor)
        {
            return new Apuracao
            {
                IdApuracao = leitor.GetInt32("id_apuracao"),
                IdJornada = leitor.GetInt32("id_jornada"),
                ValorInformado = leitor.GetDecimal("valor_informado"),
                ValorDinheiro = AcessoDados.LerDecimalOpcional(leitor, "valor_dinheiro"),
                ValorPix = AcessoDados.LerDecimalOpcional(leitor, "valor_pix"),
                ValorCartao = AcessoDados.LerDecimalOpcional(leitor, "valor_cartao"),
                DataHora = leitor.GetDateTime("data_hora"),
                IdUsuarioResponsavel = leitor.GetInt32("id_usuario_responsavel"),
                Observacao = AcessoDados.LerTextoOpcional(leitor, "observacao")
            };
        }
    }
}
