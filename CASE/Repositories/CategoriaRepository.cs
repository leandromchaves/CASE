using System.Collections.Generic;
using System;
using System.Data;
using CASE.Data;
using CASE.Services;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class CategoriaRepository
    {

        public List<Categoria> Listar()
        {
            using (var conexao = Conexao.CriarConexao())
            using (var adaptador = new MySqlDataAdapter(
                "SELECT id_categoria,nome FROM categoria ORDER BY nome,id_categoria", conexao))
            using (var dados = new DataSet("Catalogo"))
            {
                adaptador.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adaptador.Fill(dados, "Categorias");
                var categorias = new List<Categoria>();
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
            return AcessoDados.ConsultarUnico(
                "SELECT id_categoria,nome FROM categoria WHERE id_categoria=@id",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)
            );
        }

        public int Cadastrar(Categoria item)
        {
            return AcessoDados.Inserir(
                "INSERT INTO categoria (nome) VALUES (@nome)",
                comando => AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome)
            );
        }

        public bool Atualizar(Categoria item)
        {
            new AutorizacaoService().ExigirAdministrador(SessaoUsuario.ObterUsuarioAtual());
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (var conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (var transacao = conexao.BeginTransaction())
                using (var comando = new MySqlCommand(
                    "SELECT id_categoria,nome FROM categoria WHERE id_categoria=@id", conexao, transacao))
                using (var adaptador = new MySqlDataAdapter(comando))
                using (var dados = new DataSet("Catalogo"))
                using (var gerador = new MySqlCommandBuilder(adaptador))
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
            new CASE.Services.AutorizacaoService().ExigirAdministrador(CASE.Services.SessaoUsuario.ObterUsuarioAtual());
            return AcessoDados.Executar("DELETE FROM categoria WHERE id_categoria=@id",
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)) == 1;
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
