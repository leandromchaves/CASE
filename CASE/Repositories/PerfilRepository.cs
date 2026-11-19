using System.Collections.Generic;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class PerfilRepository
    {

        public List<Perfil> Listar()
        {
            return AcessoDados.ConsultarLista(
                "SELECT id_perfil,nome FROM perfil ORDER BY id_perfil",
                ConverterLinha
            );
        }

        public Perfil BuscarPorId(int id)
        {
            return AcessoDados.ConsultarUnico(
                "SELECT id_perfil,nome FROM perfil WHERE id_perfil=@id",
                ConverterLinha,
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)
            );
        }

        public int Cadastrar(Perfil item)
        {
            return AcessoDados.Inserir(
                "INSERT INTO perfil (nome) VALUES (@nome)",
                comando => AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome)
            );
        }

        public bool Atualizar(Perfil item)
        {
            return AcessoDados.Executar(
                "UPDATE perfil SET nome=@nome WHERE id_perfil=@id",
                comando =>
                {
                    AcessoDados.AdicionarParametro(comando, "@id", item.IdPerfil);
                    AcessoDados.AdicionarTextoObrigatorio(comando, "@nome", item.Nome);
                }) > 0;

        }

        public bool Excluir(int id)
        {
            new CASE.Services.AutorizacaoService().ExigirAdministrador(CASE.Services.SessaoUsuario.ObterUsuarioAtual());
            return AcessoDados.Executar("DELETE FROM perfil WHERE id_perfil=@id",
                comando => AcessoDados.AdicionarParametro(comando, "@id", id)) == 1;
        }

        private static Perfil ConverterLinha(MySqlDataReader leitor)
        {
            return new Perfil
            {
                IdPerfil = leitor.GetInt32("id_perfil"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
