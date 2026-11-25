using System;
using System.Collections.Generic;
using CASE.Models;
using CASE.Repositories;

namespace CASE.Services
{
    public class LocalEstoqueService
    {
        private LocalEstoqueRepository localEstoqueRepository = new LocalEstoqueRepository();
        private AutorizacaoService autorizacaoService = new AutorizacaoService();
        private EventoRepository eventoRepository = new EventoRepository();

        public List<LocalEstoque> Listar(Usuario usuario)
        {
            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);

            List<LocalEstoque> locais = localEstoqueRepository.Listar();
            List<LocalEstoque> locaisPermitidos = new List<LocalEstoque>();

            foreach (LocalEstoque local in locais)
            {
                if (autorizacaoService.PodeAcessarLocalEstoque(usuarioAtual, nomePerfil, local))
                {
                    locaisPermitidos.Add(local);
                }
            }

            return locaisPermitidos;
        }

        public List<LocalEstoque> ListarOperacionais(Usuario usuario)
        {
            List<LocalEstoque> autorizados = Listar(usuario);
            List<Evento> eventos = eventoRepository.ListarTodos();
            HashSet<int> eventosAbertos = new HashSet<int>();

            foreach (Evento evento in eventos)
            {
                if (evento != null &&
                    (string.Equals(evento.Situacao, SituacoesEvento.Planejado, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(evento.Situacao, SituacoesEvento.EmAndamento, StringComparison.OrdinalIgnoreCase)))
                {
                    eventosAbertos.Add(evento.IdEvento);
                }
            }

            List<LocalEstoque> locaisOperacionais = new List<LocalEstoque>();
            foreach (LocalEstoque local in autorizados)
            {
                if (local == null || !local.Ativo)
                    continue;

                if (local.EhCentral ||
                    (local.IdEvento.HasValue && eventosAbertos.Contains(local.IdEvento.Value)))
                {
                    locaisOperacionais.Add(local);
                }
            }

            return locaisOperacionais;
        }

        public LocalEstoque ObterCentral(Usuario usuario, int idInstituicao)
        {
            if (idInstituicao <= 0)
            {
                throw new ArgumentException(
                    "O identificador da instituição deve ser válido.",
                    nameof(idInstituicao));
            }

            autorizacaoService.ExigirAdministradorOuGerente(usuario);

            List<LocalEstoque> centrais =
                localEstoqueRepository.ListarCentrais(idInstituicao);

            if (centrais.Count == 0)
            {
                throw new InvalidOperationException(
                    "A instituição ainda não possui um estoque central cadastrado.");
            }

            if (centrais.Count > 1)
            {
                throw new InvalidOperationException(
                    "A instituição possui mais de um estoque central. Corrija o cadastro antes de operar.");
            }

            return centrais[0];
        }

        public LocalEstoque ObterGeralDoEvento(Usuario usuario, int idEvento)
        {
            if (idEvento <= 0)
            {
                throw new ArgumentException(
                    "O identificador do evento deve ser válido.",
                    nameof(idEvento));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);
            Evento evento = eventoRepository.BuscarPorId(idEvento);

            if (evento == null || !autorizacaoService.PodeAcessarEvento(
                usuarioAtual,
                nomePerfil,
                evento))
            {
                throw new UnauthorizedAccessException(
                    "Você não pode acessar os locais de estoque deste evento.");
            }

            List<LocalEstoque> locaisGerais =
                localEstoqueRepository.ListarGeraisPorEvento(idEvento);

            if (locaisGerais.Count != 1 || !locaisGerais[0].Ativo)
            {
                throw new InvalidOperationException(
                    "O evento precisa possuir exatamente um estoque geral ativo.");
            }

            return locaisGerais[0];
        }

        public LocalEstoque BuscarPorId(Usuario usuario, int idLocalEstoque)
        {
            if (idLocalEstoque <= 0)
            {
                throw new ArgumentException(
                    "O identificador do local de estoque deve ser maior que zero.",
                    nameof(idLocalEstoque));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);
            LocalEstoque local = localEstoqueRepository.BuscarPorId(idLocalEstoque);

            if (local != null && !autorizacaoService.PodeAcessarLocalEstoque(
                usuarioAtual,
                nomePerfil,
                local))
            {
                throw new UnauthorizedAccessException("Você não pode acessar este local de estoque.");
            }

            return local;
        }
    }
}
