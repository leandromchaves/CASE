using System.Collections.Generic;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class CategoriaRepository
    {

        public List<Categoria> Listar()
        {
            return AcessoDados.ConsultarLista(
                "SELECT id_categoria,nome FROM categoria ORDER BY nome,id_categoria",
                ConverterLinha
            );
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
            return AcessoDados.Executar(
                "UPDATE categoria SET nome=@nome WHERE id_categoria=@id",
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@id", item.IdCategoria);
                    AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                }) > 0;

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
