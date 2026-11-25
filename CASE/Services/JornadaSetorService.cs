using System;
using System.Collections.Generic;
using CASE.Models;
using CASE.Repositories;

namespace CASE.Services
{
    public class JornadaSetorService
    {
        private JornadaSetorRepository jornadaSetorRepository = new JornadaSetorRepository();
        private AutorizacaoService autorizacaoService = new AutorizacaoService();
        private SetorRepository setorRepository = new SetorRepository();
        private EventoRepository eventoRepository = new EventoRepository();
        private TurnoRepository turnoRepository = new TurnoRepository();

        public List<Turno> ListarTurnos(Usuario usuario, int idEvento)
        {
            ValidarGerenteDoEvento(usuario, idEvento);
            return turnoRepository.Listar();
        }

        public JornadaSetor ObterOuCriar(
            Usuario usuario,
            int idEvento,
            int idSetor,
            DateTime data,
            int idTurno)
        {
            ValidarGerenteDoEvento(usuario, idEvento);

            if (idSetor <= 0 || idTurno <= 0 || data == DateTime.MinValue)
            {
                throw new ArgumentException("Informe setor, data e turno válidos.");
            }
            Evento evento = eventoRepository.BuscarPorId(idEvento);
            if (data.Date < evento.DataInicio.Date || data.Date > evento.DataFim.Date)
            {
                throw new ArgumentException("A jornada deve ocorrer entre as datas de início e fim do evento.");
            }

            Setor setor = setorRepository.BuscarPorId(idSetor);

            if (setor == null || setor.IdEvento != idEvento)
            {
                throw new UnauthorizedAccessException(
                    "O setor não pertence ao evento selecionado.");
            }

            if (turnoRepository.BuscarPorId(idTurno) == null)
            {
                throw new ArgumentException("O turno informado não existe.", nameof(idTurno));
            }

            JornadaSetor jornada = jornadaSetorRepository.BuscarPorSetorDataTurno(
                idSetor,
                data,
                idTurno);

            if (jornada != null)
            {
                return jornada;
            }

            JornadaSetor novaJornada = new JornadaSetor();
            novaJornada.IdSetor = idSetor;
            novaJornada.Data = data.Date;
            novaJornada.IdTurno = idTurno;
            novaJornada.IdJornada = jornadaSetorRepository.Cadastrar(novaJornada);
            return novaJornada;
        }

        private Usuario ValidarGerenteDoEvento(Usuario usuario, int idEvento)
        {
            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);

            Evento evento = eventoRepository.BuscarPorId(idEvento);

            bool administrador = string.Equals(nomePerfil, "Administrador", StringComparison.OrdinalIgnoreCase);
            if (evento == null || (!administrador &&
                (!string.Equals(nomePerfil, "Gerente", StringComparison.OrdinalIgnoreCase) ||
                evento.IdResponsavel != usuarioAtual.IdPessoa)))
            {
                throw new UnauthorizedAccessException(
                    "Somente o administrador ou o gerente responsável pode organizar jornadas deste evento.");
            }

            SituacoesEvento.ExigirEventoAbertoParaAlteracoes(evento.Situacao);

            return usuarioAtual;
        }

        public List<JornadaSetor> ListarPorSetor(Usuario usuario, int idSetor)
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

            List<JornadaSetor> jornadas = jornadaSetorRepository.ListarPorSetor(idSetor);
            List<JornadaSetor> jornadasPermitidas = new List<JornadaSetor>();

            foreach (JornadaSetor jornada in jornadas)
            {
                if (autorizacaoService.PodeAcessarJornada(usuarioAtual, nomePerfil, jornada))
                {
                    jornadasPermitidas.Add(jornada);
                }
            }

            return jornadasPermitidas;
        }

        public JornadaSetor BuscarPorId(Usuario usuario, int idJornada)
        {
            if (idJornada <= 0)
            {
                throw new ArgumentException(
                    "O identificador da jornada deve ser maior que zero.",
                    nameof(idJornada));
            }

            string nomePerfil;
            Usuario usuarioAtual = autorizacaoService.ValidarUsuarioEObterPerfil(
                usuario,
                out nomePerfil);
            JornadaSetor jornada = jornadaSetorRepository.BuscarPorId(idJornada);

            if (jornada != null && !autorizacaoService.PodeAcessarJornada(
                usuarioAtual,
                nomePerfil,
                jornada))
            {
                throw new UnauthorizedAccessException("Você não pode acessar esta jornada.");
            }

            return jornada;
        }
    }
}
