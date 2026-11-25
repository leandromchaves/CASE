using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class LocalEstoqueRepository
    {
        public int GarantirCentral(int idInstituicao)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    using (MySqlCommand comando = new MySqlCommand(
                        "SELECT id_instituicao FROM instituicao WHERE id_instituicao=@id FOR UPDATE",
conexao,
transacao))
                    {
                        comando.Parameters.AddWithValue("@id", idInstituicao);
                        if (comando.ExecuteScalar() == null)
                        {
                            throw new InvalidOperationException("Cadastre a instituição primeiro.");
                        }
                    }
                    int idCentral = 0;
                    using (MySqlCommand comando = new MySqlCommand(
                        "SELECT id_local_estoque,ativo FROM vw_localestoque WHERE id_instituicao=@id AND eh_central=1 FOR UPDATE",
conexao,
transacao))
                    {
                        comando.Parameters.AddWithValue("@id", idInstituicao);
                        using (MySqlDataReader leitor = comando.ExecuteReader())
                        {
                            while (leitor.Read())
                            {
                                if (idCentral > 0 || !leitor.GetBoolean("ativo"))
                                {
                                    throw new InvalidOperationException("O estoque central precisa ser revisado: duplicado ou inativo.");
                                }
                                idCentral = leitor.GetInt32("id_local_estoque");
                            }
                        }
                    }
                    if (idCentral == 0)
                    {
                        using (MySqlCommand comando = new MySqlCommand(
                            "INSERT INTO localestoque(id_instituicao_central,nome,ativo,eh_geral_evento,id_evento) " +
                            "VALUES (@id,'Estoque central',1,0,NULL)",
conexao,
transacao))
                        {
                            comando.Parameters.AddWithValue("@id", idInstituicao);
                            comando.ExecuteNonQuery();
                            idCentral = checked((int)comando.LastInsertedId);
                        }
                    }
                    transacao.Commit();
                    return idCentral;
                }
            }
        }

        public List<LocalEstoque> Listar()
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT le.id_local_estoque,le.id_instituicao,le.id_evento,le.nome,le.ativo,le.eh_central,le.eh_geral_evento,ev.nome AS nome_evento " +
                "FROM vw_localestoque le LEFT JOIN evento ev ON ev.id_evento=le.id_evento ORDER BY le.id_local_estoque", conexaoBanco))
                {
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<LocalEstoque> resultadoBanco = new List<LocalEstoque>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<LocalEstoque> ListarCentrais(int idInstituicao)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT le.id_local_estoque,le.id_instituicao,le.id_evento,le.nome,le.ativo,le.eh_central,le.eh_geral_evento,ev.nome AS nome_evento " +
                "FROM vw_localestoque le LEFT JOIN evento ev ON ev.id_evento=le.id_evento " +
                "WHERE le.id_instituicao=@instituicao AND le.eh_central=1 ORDER BY le.id_local_estoque", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@instituicao", idInstituicao);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<LocalEstoque> resultadoBanco = new List<LocalEstoque>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<LocalEstoque> ListarPorEvento(int idEvento)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT le.id_local_estoque,le.id_instituicao,le.id_evento,le.nome,le.ativo,le.eh_central,le.eh_geral_evento,ev.nome AS nome_evento " +
                "FROM vw_localestoque le LEFT JOIN evento ev ON ev.id_evento=le.id_evento " +
                "WHERE le.id_evento=@evento ORDER BY le.id_local_estoque", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<LocalEstoque> resultadoBanco = new List<LocalEstoque>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public List<LocalEstoque> ListarGeraisPorEvento(int idEvento)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT le.id_local_estoque,le.id_instituicao,le.id_evento,le.nome,le.ativo,le.eh_central,le.eh_geral_evento,ev.nome AS nome_evento " +
                "FROM vw_localestoque le LEFT JOIN evento ev ON ev.id_evento=le.id_evento " +
                "WHERE le.id_evento=@evento AND le.eh_geral_evento=1 " +
                "ORDER BY le.id_local_estoque", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<LocalEstoque> resultadoBanco = new List<LocalEstoque>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public LocalEstoque BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT le.id_local_estoque,le.id_instituicao,le.id_evento,le.nome,le.ativo,le.eh_central,le.eh_geral_evento,ev.nome AS nome_evento " +
                "FROM vw_localestoque le LEFT JOIN evento ev ON ev.id_evento=le.id_evento " +
                "WHERE le.id_local_estoque=@id", conexaoBanco))
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

        public int Cadastrar(LocalEstoque item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO localestoque (id_instituicao_central,id_evento,nome,ativo,eh_geral_evento) VALUES (@instituicao,@evento,@nome,@ativo,@geralEvento)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(LocalEstoque item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE localestoque SET nome=@nome,ativo=@ativo WHERE id_local_estoque=@id AND id_instituicao_central <=> @instituicao AND id_evento <=> @evento AND eh_geral_evento=@geralEvento", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdLocalEstoque);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static void Vincular(MySqlCommand comando, LocalEstoque item)
        {
            AcessoDados.AdicionarParametro(comando, "@instituicao", item.EhCentral ? (int?)item.IdInstituicao : null);
            AcessoDados.AdicionarParametro(comando, "@evento", item.IdEvento);
            AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
            AcessoDados.AdicionarParametro(comando, "@ativo", item.Ativo);
            AcessoDados.AdicionarParametro(comando, "@geralEvento", item.EhGeralEvento);
        }

        private static LocalEstoque ConverterLinha(MySqlDataReader leitor)
        {
            return new LocalEstoque
            {
                IdLocalEstoque = leitor.GetInt32("id_local_estoque"),
                IdInstituicao = leitor.GetInt32("id_instituicao"),
                IdEvento = AcessoDados.LerInteiroOpcional(leitor, "id_evento"),
                Nome = leitor.GetString("nome"),
                EventoNome = leitor.IsDBNull(leitor.GetOrdinal("nome_evento"))
                    ? string.Empty
                    : leitor.GetString("nome_evento"),
                Ativo = leitor.GetBoolean("ativo"),
                EhCentral = leitor.GetBoolean("eh_central"),
                EhGeralEvento = leitor.GetBoolean("eh_geral_evento")
            };
        }
    }
}
