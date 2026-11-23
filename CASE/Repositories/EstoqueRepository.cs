using System.Collections.Generic;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class EstoqueRepository
    {

        private const string Colunas = "id_estoque,id_local_estoque,id_produto,quantidade_minima,quantidade_ideal,quantidade_atual";

        public List<Estoque> ListarPorLocal(int idLocalEstoque, bool somenteDisponiveis = false)
        {
            string sql = "SELECT " + Colunas + " FROM estoque WHERE id_local_estoque=@id ORDER BY id_produto";
            if (somenteDisponiveis)
            {
                sql = "SELECT e.* FROM estoque e LEFT JOIN (" +
                    "SELECT id_produto,MIN(ordem) AS primeira_chegada FROM (" +
                    "SELECT i.id_produto,CONCAT(DATE_FORMAT(en.data_hora,'%Y%m%d%H%i%s')," +
                    "LPAD(i.id_entrada_item,20,'0')) AS ordem " +
                    "FROM entradamercadoria en JOIN entradaitem i ON i.id_entrada=en.id_entrada " +
                    "WHERE en.id_local_destino=@id UNION ALL " +
                    "SELECT i.id_produto,CONCAT(DATE_FORMAT(tr.data_hora,'%Y%m%d%H%i%s')," +
                    "LPAD(i.id_transferencia_item,20,'0')) " +
                    "FROM transferenciaestoque tr JOIN transferenciaitem i ON i.id_transferencia=tr.id_transferencia " +
                    "WHERE tr.id_local_destino=@id) recebimentos GROUP BY id_produto) r " +
                    "ON r.id_produto=e.id_produto WHERE e.id_local_estoque=@id AND e.quantidade_atual>0 " +
                    "ORDER BY r.primeira_chegada IS NULL,r.primeira_chegada,e.id_estoque";
            }
            return AcessoDados.ConsultarLista(
                sql,
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", idLocalEstoque)
            );
        }

        public Estoque BuscarPorId(int id)
        {
            return AcessoDados.ConsultarUnico(
                "SELECT " + Colunas + " FROM estoque WHERE id_estoque=@id",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)
            );
        }

        public Estoque BuscarPorLocalEProduto(int idLocal, int idProduto)
        {
            return AcessoDados.ConsultarUnico(
                "SELECT " + Colunas + " FROM estoque WHERE id_local_estoque=@local AND id_produto=@produto",
                ConverterLinha,
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@local", idLocal);
                    AcessoDados.AdicionarParametro(comando, "@produto", idProduto);
                }
            );
        }

        public int Cadastrar(Estoque item)
        {
            return AcessoDados.Inserir(
                "INSERT INTO estoque (id_local_estoque,id_produto,quantidade_minima,quantidade_ideal) VALUES (@local,@produto,@minima,@ideal)",
                comando => Vincular(comando, item)
            );
        }

        public bool AtualizarQuantidadesAlvo(Estoque item)
        {
            return AcessoDados.Executar(
                "UPDATE estoque SET quantidade_minima=@minima,quantidade_ideal=@ideal WHERE id_estoque=@id",
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@id", item.IdEstoque);
                    AcessoDados.AdicionarParametro(comando, "@minima", item.QuantidadeMinima);
                    AcessoDados.AdicionarParametro(comando, "@ideal", item.QuantidadeIdeal);
                }) > 0;

        }

        private static void Vincular(MySqlCommand comando, Estoque item)
        {
            AcessoDados.AdicionarParametro(comando, "@local", item.IdLocalEstoque);
            AcessoDados.AdicionarParametro(comando, "@produto", item.IdProduto);
            AcessoDados.AdicionarParametro(comando, "@minima", item.QuantidadeMinima);
            AcessoDados.AdicionarParametro(comando, "@ideal", item.QuantidadeIdeal);
        }

        private static Estoque ConverterLinha(MySqlDataReader leitor)
        {
            return new Estoque
            {
                IdEstoque = leitor.GetInt32("id_estoque"),
                IdLocalEstoque = leitor.GetInt32("id_local_estoque"),
                IdProduto = leitor.GetInt32("id_produto"),
                QuantidadeMinima = leitor.GetDecimal("quantidade_minima"),
                QuantidadeIdeal = leitor.GetDecimal("quantidade_ideal"),
                QuantidadeAtual = leitor.GetDecimal("quantidade_atual")
            };
        }
    }
}
