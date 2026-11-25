using System.Collections.Generic;
using System.Data;
using System;
using CASE.Data;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class CategoriaRepository
    {
        public List<Categoria> Listar()
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(
                "SELECT id_categoria,nome FROM categoria ORDER BY nome,id_categoria", conexao))
            using (DataSet dados = new DataSet("Catalogo"))
            {
                adaptador.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adaptador.Fill(dados, "Categorias");
                List<Categoria> categorias = new List<Categoria>();
                foreach (DataRow linha in dados.Tables["Categorias"].Rows)
                    categorias.Add(new Categoria
                    {
                        IdCategoria = Convert.ToInt32(linha["id_categoria"]),
                        Nome = Convert.ToString(linha["nome"])
                    });
                return categorias;
            }
        }

        public Categoria BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_categoria,nome FROM categoria WHERE id_categoria=@id", conexaoBanco))
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

        public int Cadastrar(Categoria item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO categoria (nome) VALUES (@nome)", conexaoBanco))
                {
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(Categoria item)
        {
            new AutorizacaoService().ExigirAdministrador(SessaoUsuario.ObterUsuarioAtual());
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                using (MySqlCommand comando = new MySqlCommand(
                    "SELECT id_categoria,nome FROM categoria WHERE id_categoria=@id", conexao, transacao))
                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                using (DataSet dados = new DataSet("Catalogo"))
                using (MySqlCommandBuilder gerador = new MySqlCommandBuilder(adaptador))
                {
                    try
                    {
                        comando.Parameters.AddWithValue("@id", item.IdCategoria);
                        adaptador.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                        adaptador.Fill(dados, "Categorias");
                        if (dados.Tables["Categorias"].Rows.Count == 0)
                        {
                            transacao.Commit();
                            return false;
                        }
                        DataRow categoria = dados.Tables["Categorias"].Rows[0];
                        if (string.Equals(Convert.ToString(categoria["nome"]), item.Nome, StringComparison.Ordinal))
                        {
                            transacao.Commit();
                            return false;
                        }
                        categoria["nome"] = item.Nome;
                        gerador.QuotePrefix = "`";
                        gerador.QuoteSuffix = "`";
                        adaptador.UpdateCommand = gerador.GetUpdateCommand();
                        adaptador.UpdateCommand.Transaction = transacao;
                        adaptador.AcceptChangesDuringUpdate = false;
                        int alteradas = adaptador.Update(dados, "Categorias");
                        transacao.Commit();
                        dados.AcceptChanges();
                        return alteradas == 1;
                    }
                    catch (DBConcurrencyException erro)
                    {
                        transacao.Rollback();
                        throw new InvalidOperationException(
                            "A categoria foi alterada ou removida durante a operação. Atualize a lista e tente novamente.", erro);
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool Excluir(int id)
        {
            new AutorizacaoService().ExigirAdministrador(SessaoUsuario.ObterUsuarioAtual());
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("DELETE FROM categoria WHERE id_categoria=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", id);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco == 1;
        }

        private static Categoria ConverterLinha(MySqlDataReader leitor)
        {
            return new Categoria
            {
                IdCategoria = leitor.GetInt32("id_categoria"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
