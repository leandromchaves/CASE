using System;
using System.Data;
using CASE.Data;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class ConsultaOperacionalRepository
    {
        // O escopo acompanha o evento; para membros, exige a escala da própria pessoa.
        private const string Escopo = "(@perfil='Administrador' OR " +
            "(@perfil='Gerente' AND ev.id_responsavel=@pessoa) OR " +
            "(@perfil='Membro' AND eq.id_pessoa=@pessoa AND eq.ativo=1))";

        public DataTable Escalas(string perfil, int pessoa, int evento)
        {
            string sql = "SELECT e.id_escala,eq.id_pessoa,eq.ativo,e.situacao,j.id_jornada,j.data AS data_jornada,s.id_setor, " +
                "CONCAT(ev.nome,' / ',s.nome,' / ',DATE_FORMAT(j.data,'%d/%m/%Y')," +
                "' / ',p.nome,' / ',e.entrada_prevista,' - ',e.saida_prevista) AS descricao," +
                "CONCAT(s.nome,' / ',DATE_FORMAT(j.data,'%d/%m/%Y'),' / ',t.nome) AS descricao_jornada " +
                "FROM escala e JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                "JOIN pessoa p ON p.id_pessoa=eq.id_pessoa " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN turno t ON t.id_turno=j.id_turno " +
                "JOIN setor s ON s.id_setor=j.id_setor JOIN evento ev ON ev.id_evento=s.id_evento " +
                "WHERE eq.id_evento=ev.id_evento AND " + Escopo +
                " AND (@perfil<>'Membro' OR LOWER(e.situacao)<>'cancelada') " +
                "AND (@evento=0 OR ev.id_evento=@evento) ORDER BY j.data,s.nome,p.nome";
            return Consultar(sql, perfil, pessoa, evento, DateTime.Today, DateTime.Today);
        }

        public DataTable ProdutosDaEscala(int escala)
        {
            return Consultar("SELECT ps.id_produto_setor,p.nome,ps.preco_venda " +
                "FROM escala e JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                "JOIN produtosetor ps ON ps.id_setor=j.id_setor " +
                "JOIN produto p ON p.id_produto=ps.id_produto " +
                "WHERE e.id_escala=@evento AND p.ativo=1 ORDER BY p.nome",
                string.Empty, 0, escala, DateTime.Today, DateTime.Today);
        }

        public DataTable Registros(string perfil, int pessoa, int evento, DateTime inicio, DateTime fim)
        {
            string sql = "SELECT r.id_registro,s.id_setor,j.id_jornada,r.data_hora,p.nome AS responsavel,s.nome AS setor," +
                "t.nome AS turno,r.tipo,pr.nome AS produto,ri.quantidade," +
                "ri.preco_unitario_praticado,r.forma_pagamento,r.observacao," +
                "ri.quantidade*ri.preco_unitario_praticado AS total " +
                "FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                "JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                "JOIN usuario u ON u.id_usuario=r.id_usuario_responsavel " +
                "JOIN pessoa p ON p.id_pessoa=u.id_pessoa " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada JOIN turno t ON t.id_turno=j.id_turno " +
                "JOIN setor s ON s.id_setor=j.id_setor JOIN evento ev ON ev.id_evento=s.id_evento " +
                "JOIN registroitem ri ON ri.id_registro=r.id_registro " +
                "JOIN produtosetor ps ON ps.id_produto_setor=ri.id_produto_setor " +
                "JOIN produto pr ON pr.id_produto=ps.id_produto WHERE eq.id_evento=ev.id_evento " +
                "AND ps.id_setor=s.id_setor AND " + Escopo +
                " AND (@perfil<>'Membro' OR u.id_pessoa=@pessoa)" +
                " AND (@evento=0 OR ev.id_evento=@evento) AND r.data_hora>=@inicio " +
                "AND r.data_hora<@fim ORDER BY r.data_hora DESC,r.id_registro DESC";
            return Consultar(sql, perfil, pessoa, evento, inicio, fim);
        }

        public DataTable Estoques(string perfil, int pessoa)
        {
            string sql = "SELECT l.id_local_estoque,l.nome AS local,p.id_produto,p.nome AS produto,p.unidade_medida," +
                "es.quantidade_atual,es.quantidade_minima,es.quantidade_ideal " +
                "FROM estoque es JOIN vw_localestoque l ON l.id_local_estoque=es.id_local_estoque " +
                "JOIN produto p ON p.id_produto=es.id_produto " +
                "LEFT JOIN evento ev ON ev.id_evento=l.id_evento WHERE " +
                "(@perfil='Administrador' OR (@perfil='Gerente' AND (ev.id_responsavel=@pessoa OR " +
                "(l.eh_central=1 AND EXISTS (SELECT 1 FROM evento ce WHERE ce.id_responsavel=@pessoa " +
                "AND ce.id_instituicao=l.id_instituicao)))) " +
                "OR (@perfil='Membro' AND EXISTS (SELECT 1 FROM estoquesetor xs " +
                "JOIN jornadasetor j ON j.id_setor=xs.id_setor " +
                "JOIN escala e ON e.id_jornada=j.id_jornada " +
                "JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                "WHERE xs.id_local_estoque=l.id_local_estoque AND eq.id_evento=xs.id_evento " +
                "AND eq.ativo=1 AND eq.id_pessoa=@pessoa AND LOWER(e.situacao)<>'cancelada'))) " +
                "ORDER BY l.nome,p.nome";
            return Consultar(sql, perfil, pessoa, 0, DateTime.Today, DateTime.Today);
        }

        public DataTable Movimentacoes(string perfil, int pessoa, DateTime inicio, DateTime fim)
        {
            // Transferências aparecem como uma operação com origem e destino, sem duplicar o total institucional.
            string sql = "SELECT m.data_hora,m.tipo,m.produto,m.quantidade,m.origem,m.destino,m.responsavel,m.id_origem,m.id_destino,m.id_operacao,m.entidade " +
                "FROM (SELECT en.data_hora,'entrada' AS tipo,p.nome AS produto,i.quantidade," +
                "'' AS origem,l.nome AS destino,u.login AS responsavel,NULL AS evento,0 AS id_origem,l.id_local_estoque AS id_destino," +
                "en.id_entrada AS id_operacao,'entradamercadoria' AS entidade " +
                "FROM entradamercadoria en JOIN entradaitem i ON i.id_entrada=en.id_entrada " +
                "JOIN produto p ON p.id_produto=i.id_produto " +
                "JOIN vw_localestoque l ON l.id_local_estoque=en.id_local_destino " +
                "JOIN usuario u ON u.id_usuario=en.id_usuario_responsavel WHERE @perfil='Administrador' " +
                "UNION ALL SELECT tr.data_hora,'transferencia',p.nome,i.quantidade,o.nome,d.nome,u.login," +
                "COALESCE(o.id_evento,d.id_evento),o.id_local_estoque,d.id_local_estoque,tr.id_transferencia,'transferenciaestoque' FROM transferenciaestoque tr " +
                "JOIN transferenciaitem i ON i.id_transferencia=tr.id_transferencia " +
                "JOIN produto p ON p.id_produto=i.id_produto " +
                "JOIN vw_localestoque o ON o.id_local_estoque=tr.id_local_origem " +
                "JOIN vw_localestoque d ON d.id_local_estoque=tr.id_local_destino " +
                "JOIN usuario u ON u.id_usuario=tr.id_usuario_responsavel " +
                "WHERE @perfil<>'Membro' UNION ALL SELECT r.data_hora,r.tipo,p.nome,i.quantidade," +
                "s.nome,'',u.login,ev.id_evento,es.id_local_estoque,0,r.id_registro,'registro' FROM registro r " +
                "JOIN registroitem i ON i.id_registro=r.id_registro " +
                "JOIN produtosetor ps ON ps.id_produto_setor=i.id_produto_setor " +
                "JOIN produto p ON p.id_produto=ps.id_produto " +
                "JOIN escala e ON e.id_escala=r.id_escala JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                "JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                "JOIN setor s ON s.id_setor=j.id_setor JOIN evento ev ON ev.id_evento=s.id_evento " +
                "JOIN estoquesetor es ON es.id_setor=s.id_setor " +
                "JOIN usuario u ON u.id_usuario=r.id_usuario_responsavel " +
                "WHERE eq.id_evento=ev.id_evento AND ps.id_setor=s.id_setor AND " + Escopo +
                " AND (@perfil<>'Membro' OR u.id_pessoa=@pessoa)" +
                ") m LEFT JOIN evento ev ON ev.id_evento=m.evento " +
                "WHERE m.data_hora>=@inicio AND m.data_hora<@fim AND " +
                "(@perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa) " +
                "OR (@perfil='Membro' AND m.tipo IN ('venda','perda'))) ORDER BY m.data_hora DESC";
            return Consultar(sql, perfil, pessoa, 0, inicio, fim);
        }

        internal DataTable ItensParaCorrecao(string entidade, int id)
        {
            string sql;
            switch (entidade)
            {
                case "registro":
                    sql = "SELECT ri.id_registro_item AS id_item,ri.id_produto_setor AS id_produto,p.nome AS produto," +
                        "ri.quantidade,ri.preco_unitario_praticado AS valor_unitario FROM registroitem ri " +
                        "JOIN produtosetor ps ON ps.id_produto_setor=ri.id_produto_setor JOIN produto p ON p.id_produto=ps.id_produto " +
                        "WHERE ri.id_registro=@evento ORDER BY ri.id_registro_item";
                    break;
                case "entradamercadoria":
                    sql = "SELECT ei.id_entrada_item AS id_item,ei.id_produto,p.nome AS produto,ei.quantidade," +
                        "ei.custo_unitario AS valor_unitario FROM entradaitem ei JOIN produto p ON p.id_produto=ei.id_produto " +
                        "WHERE ei.id_entrada=@evento ORDER BY ei.id_entrada_item";
                    break;
                case "transferenciaestoque":
                    sql = "SELECT ti.id_transferencia_item AS id_item,ti.id_produto,p.nome AS produto,ti.quantidade," +
                        "NULL AS valor_unitario FROM transferenciaitem ti JOIN produto p ON p.id_produto=ti.id_produto " +
                        "WHERE ti.id_transferencia=@evento ORDER BY ti.id_transferencia_item";
                    break;
                default:
                    throw new ArgumentException("Tipo de operação inválido.");
            }
            return Consultar(sql, string.Empty, 0, id, DateTime.Today, DateTime.Today);
        }

        internal DataTable HistoricoDaOperacao(string entidade, int id)
        {
            const string sql = "SELECT h.data_hora,u.login AS responsavel,h.justificativa," +
                "JSON_EXTRACT(h.dados_anteriores,'$.versao') AS versao_anterior," +
                "JSON_EXTRACT(h.dados_novos,'$.versao') AS versao_nova " +
                "FROM historicoalteracaooperacao h JOIN usuario u ON u.id_usuario=h.id_usuario " +
                "WHERE h.entidade=@perfil AND h.id_entidade=@evento ORDER BY h.id_historico DESC";
            return Consultar(sql, entidade, 0, id, DateTime.Today, DateTime.Today);
        }

        public DataTable Relatorio(string tipo, string perfil, int pessoa, DateTime inicio, DateTime fim)
        {
            string sql;
            switch (tipo)
            {
                case "Eventos":
                    sql = "SELECT ev.nome,ev.local,ev.data_inicio,ev.data_fim,ev.situacao,p.nome AS responsavel " +
                        "FROM evento ev JOIN pessoa p ON p.id_pessoa=ev.id_responsavel " +
                        "WHERE (@perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa)) " +
                        "AND ev.data_inicio<@fim AND ev.data_fim>=@inicio ORDER BY ev.data_inicio,ev.nome";
                    break;
                case "Solicitações de reposição":
                    sql = "SELECT so.id_solicitacao,so.data_hora,ev.nome AS evento,s.nome AS setor," +
                        "p.nome AS solicitante,so.situacao,pr.nome AS produto,si.quantidade," +
                        "d.login AS decisor,so.decidida_em,so.justificativa_rejeicao " +
                        "FROM solicitacao so JOIN escala e ON e.id_escala=so.id_escala " +
                        "JOIN equipeevento eq ON eq.id_equipe=e.id_equipe " +
                        "JOIN pessoa p ON p.id_pessoa=eq.id_pessoa " +
                        "JOIN jornadasetor j ON j.id_jornada=e.id_jornada " +
                        "JOIN setor s ON s.id_setor=j.id_setor JOIN evento ev ON ev.id_evento=s.id_evento " +
                        "JOIN solicitacaoitem si ON si.id_solicitacao=so.id_solicitacao " +
                        "JOIN produtosetor ps ON ps.id_produto_setor=si.id_produto_setor " +
                        "JOIN produto pr ON pr.id_produto=ps.id_produto " +
                        "LEFT JOIN usuario d ON d.id_usuario=so.id_usuario_decisor " +
                        "WHERE eq.id_evento=ev.id_evento AND ps.id_setor=s.id_setor AND " + Escopo +
                        " AND so.data_hora>=@inicio AND so.data_hora<@fim ORDER BY so.data_hora DESC";
                    break;
                case "Solicitações ao central":
                    sql = "SELECT so.id_solicitacao_estoque_central,so.tipo,so.data_hora,ev.nome AS evento," +
                        "u.login AS solicitante,so.situacao,p.nome AS produto,si.quantidade," +
                        "d.login AS decisor,so.decidida_em,so.justificativa_rejeicao,tr.id_transferencia " +
                        "FROM vw_solicitacaoestoquecentral so JOIN evento ev ON ev.id_evento=so.id_evento " +
                        "JOIN usuario u ON u.id_usuario=so.id_usuario_solicitante " +
                        "JOIN solicitacaoestoquecentralitem si ON si.id_solicitacao_estoque_central=so.id_solicitacao_estoque_central " +
                        "JOIN produto p ON p.id_produto=si.id_produto " +
                        "LEFT JOIN usuario d ON d.id_usuario=so.id_usuario_decisor " +
                        "LEFT JOIN transferenciaestoque tr ON tr.id_solicitacao_estoque_central=so.id_solicitacao_estoque_central " +
                        "WHERE (@perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa " +
                        "AND u.id_pessoa=@pessoa)) AND so.data_hora>=@inicio AND so.data_hora<@fim " +
                        "ORDER BY so.data_hora DESC";
                    break;
                case "Despesas":
                    sql = "SELECT de.id_despesa,ev.nome AS evento,de.data,de.descricao,de.valor," +
                        "f.nome AS fornecedor,u.login AS responsavel FROM despesa de " +
                        "JOIN evento ev ON ev.id_evento=de.id_evento " +
                        "LEFT JOIN fornecedor f ON f.id_fornecedor=de.id_fornecedor " +
                        "JOIN usuario u ON u.id_usuario=de.id_usuario_responsavel " +
                        "WHERE (@perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa)) " +
                        "AND de.data>=@inicio AND de.data<@fim ORDER BY de.data DESC";
                    break;
                case "Apurações":
                    sql = "SELECT a.id_apuracao,ev.nome AS evento,s.nome AS setor,j.data AS data_jornada," +
                        "t.nome AS turno,a.data_hora,a.valor_informado,a.valor_dinheiro,a.valor_pix," +
                        "a.valor_cartao,u.login AS responsavel,a.observacao," +
                        "CASE WHEN COALESCE(v.pendentes,0)>0 THEN NULL ELSE COALESCE(v.total,0) END AS esperado_atual," +
                        "CASE WHEN COALESCE(v.pendentes,0)>0 THEN NULL ELSE a.valor_informado-COALESCE(v.total,0) END AS diferenca_atual," +
                        "CASE WHEN COALESCE(v.pendentes,0)>0 THEN NULL ELSE a.valor_dinheiro-COALESCE(v.dinheiro,0) END AS diferenca_dinheiro_atual," +
                        "CASE WHEN COALESCE(v.pendentes,0)>0 THEN NULL ELSE a.valor_pix-COALESCE(v.pix,0) END AS diferenca_pix_atual," +
                        "CASE WHEN COALESCE(v.pendentes,0)>0 THEN NULL ELSE a.valor_cartao-COALESCE(v.cartao,0) END AS diferenca_cartao_atual " +
                        "FROM vw_apuracao a " +
                        "JOIN jornadasetor j ON j.id_jornada=a.id_jornada " +
                        "JOIN turno t ON t.id_turno=j.id_turno JOIN setor s ON s.id_setor=j.id_setor " +
                        "JOIN evento ev ON ev.id_evento=s.id_evento " +
                        "JOIN usuario u ON u.id_usuario=a.id_usuario_responsavel " +
                        "LEFT JOIN (SELECT e.id_jornada,SUM(ri.quantidade*ri.preco_unitario_praticado) AS total," +
                        "SUM(CASE WHEN r.forma_pagamento='dinheiro' THEN ri.quantidade*ri.preco_unitario_praticado ELSE 0 END) AS dinheiro," +
                        "SUM(CASE WHEN r.forma_pagamento='pix' THEN ri.quantidade*ri.preco_unitario_praticado ELSE 0 END) AS pix," +
                        "SUM(CASE WHEN r.forma_pagamento='cartao' THEN ri.quantidade*ri.preco_unitario_praticado ELSE 0 END) AS cartao," +
                        "SUM(ri.preco_unitario_praticado IS NULL OR r.forma_pagamento IS NULL OR " +
                        "r.forma_pagamento NOT IN ('dinheiro','pix','cartao')) AS pendentes " +
                        "FROM registro r JOIN escala e ON e.id_escala=r.id_escala " +
                        "JOIN registroitem ri ON ri.id_registro=r.id_registro WHERE r.tipo='venda' GROUP BY e.id_jornada) v " +
                        "ON v.id_jornada=a.id_jornada " +
                        "WHERE (@perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa)) " +
                        "AND j.data>=@inicio AND j.data<@fim ORDER BY j.data DESC,a.data_hora DESC";
                    break;
                default:
                    throw new ArgumentException("Selecione um tipo de relatório válido.");
            }
            return Consultar(sql, perfil, pessoa, 0, inicio, fim);
        }

        public string InstituicoesDoRelatorio(string perfil, int pessoa)
        {
            const string sql = "SELECT DISTINCT i.nome FROM instituicao i " +
                "LEFT JOIN evento ev ON ev.id_instituicao=i.id_instituicao " +
                "WHERE @perfil='Administrador' OR (@perfil='Gerente' AND ev.id_responsavel=@pessoa) " +
                "OR (@perfil='Membro' AND EXISTS (SELECT 1 FROM equipeevento eq " +
                "WHERE eq.id_evento=ev.id_evento AND eq.id_pessoa=@pessoa AND eq.ativo=1)) ORDER BY i.nome";
            DataTable dados = Consultar(sql, perfil, pessoa, 0, DateTime.Today, DateTime.Today);
            if (dados.Rows.Count == 1)
                return Convert.ToString(dados.Rows[0][0]);
            return dados.Rows.Count > 1 ? "Instituições autorizadas" : null;
        }

        private static DataTable Consultar(string sql, string perfil, int pessoa, int evento,
            DateTime inicio, DateTime fim)
        {
            using (var conexao = Conexao.CriarConexao())
            using (var comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@perfil", perfil);
                comando.Parameters.AddWithValue("@pessoa", pessoa);
                comando.Parameters.AddWithValue("@evento", evento);
                comando.Parameters.AddWithValue("@inicio", inicio.Date);
                comando.Parameters.AddWithValue("@fim", fim.Date.AddDays(1));
                conexao.Open();
                using (var leitor = comando.ExecuteReader())
                {
                    DataTable dados = new DataTable();
                    dados.Load(leitor);
                    return dados;
                }
            }
        }
    }
}
