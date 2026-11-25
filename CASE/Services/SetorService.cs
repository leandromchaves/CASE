using System;
using System.Collections.Generic;
using CASE.Models;
using CASE.Repositories;

namespace CASE.Services
{
    public class SetorService
    {
        private SetorRepository setorRepository = new SetorRepository();
        private EventoRepository eventoRepository = new EventoRepository();
        private AutorizacaoService autorizacaoService = new AutorizacaoService();

        public int Criar(Usuario usuario, Setor setor)
        {
            if (setor == null)
            {
                throw new ArgumentNullException(nameof(setor));
            }

            if (setor.IdEvento <= 0 || string.IsNullOrWhiteSpace(setor.Nome) || setor.Nome.Trim().Length > 100)
            {
                throw new ArgumentException(
                    "Informe um evento e o nome do setor.",
                    nameof(setor));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);

            Evento evento = eventoRepository.BuscarPorId(setor.IdEvento);

            bool administrador = string.Equals(nomePerfil, "Administrador", StringComparison.OrdinalIgnoreCase);
            if (evento == null || (!administrador &&
                (!string.Equals(nomePerfil, "Gerente", StringComparison.OrdinalIgnoreCase) ||
                evento.IdResponsavel != usuarioAtual.IdPessoa)))
            {
                throw new UnauthorizedAccessException(
                    "Somente o administrador ou o gerente responsável pode criar setores neste evento.");
            }

            SituacoesEvento.ExigirEventoAbertoParaAlteracoes(evento.Situacao);

            setor.Nome = setor.Nome.Trim();

            return setorRepository.CadastrarComEstoqueDeBarraca(
                setor,
                usuarioAtual.IdUsuario);
        }

        public List<Setor> ListarPorEvento(Usuario usuario, int idEvento)
        {
            if (idEvento <= 0)
            {
                throw new ArgumentException(
                    "O identificador do evento deve ser maior que zero.",
                    nameof(idEvento));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);

            List<Setor> setores = setorRepository.ListarPorEvento(idEvento);
            List<Setor> setoresPermitidos = new List<Setor>();

            foreach (Setor setor in setores)
            {
                if (autorizacaoService.PodeAcessarSetor(usuarioAtual, nomePerfil, setor))
                {
                    setoresPermitidos.Add(setor);
                }
            }

            return setoresPermitidos;
        }

        public void Renomear(Usuario usuario, int idSetor, string nome, string nomeAnterior)
        {
            if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 100)
            {
                throw new ArgumentException("Informe um nome de setor com até 100 caracteres.");
            }
            string perfil;
            Usuario atual = autorizacaoService.ValidarUsuarioEObterPerfil(usuario, out perfil);
            Setor setor = BuscarPorId(usuario, idSetor);
            if (setor == null)
            {
                throw new InvalidOperationException("O setor não foi encontrado.");
            }
            Evento evento = eventoRepository.BuscarPorId(setor.IdEvento);
            if (evento == null || (perfil != "Administrador" &&
                (perfil != "Gerente" || evento.IdResponsavel != atual.IdPessoa)))
            {
                throw new UnauthorizedAccessException("Somente o administrador ou o gerente responsável pode renomear o setor.");
            }
            SituacoesEvento.ExigirEventoAbertoParaAlteracoes(evento.Situacao);
            if (!string.Equals(setor.Nome, nomeAnterior, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("O setor foi alterado. Atualize a lista antes de editar.");
            }
            nome = nome.Trim();
            if (nome == setor.Nome)
            {
                return;
            }
            setor.Nome = nome;
            setorRepository.RenomearComLocal(setor, atual.IdUsuario, nomeAnterior);
        }

        public void Excluir(Usuario usuario, int idSetor)
        {
            if (idSetor <= 0)
            {
                throw new ArgumentException("Selecione um setor válido.", nameof(idSetor));
            }

            string perfil;
            Usuario atual = autorizacaoService.ValidarUsuarioEObterPerfil(usuario, out perfil);
            if (!string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "Somente o administrador ou o gerente responsável pode remover setores.");
            }

            Setor setor = BuscarPorId(usuario, idSetor);
            if (setor == null)
            {
                throw new InvalidOperationException("O setor não foi encontrado.");
            }

            Evento evento = eventoRepository.BuscarPorId(setor.IdEvento);
            bool administrador = string.Equals(
                perfil,
                "Administrador",
                StringComparison.OrdinalIgnoreCase);
            if (evento == null || (!administrador && evento.IdResponsavel != atual.IdPessoa))
            {
                throw new UnauthorizedAccessException(
                    "Somente o administrador ou o gerente responsável pode remover setores deste evento.");
            }

            SituacoesEvento.ExigirEventoAbertoParaAlteracoes(evento.Situacao);

            setorRepository.ExcluirSeVazio(setor, atual.IdUsuario);
        }

        public Setor BuscarPorId(Usuario usuario, int idSetor)
        {
            if (idSetor <= 0)
            {
                throw new ArgumentException(
                    "O identificador do setor deve ser maior que zero.",
                    nameof(idSetor));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);
            Setor setor = setorRepository.BuscarPorId(idSetor);

            if (setor != null && !autorizacaoService.PodeAcessarSetor(
                usuarioAtual,
                nomePerfil,
                setor))
            {
                throw new UnauthorizedAccessException("Você não pode acessar este setor.");
            }

            return setor;
        }
    }
}
