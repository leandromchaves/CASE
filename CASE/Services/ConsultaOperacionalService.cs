using System;
using System.Collections.Generic;
using System.Data;
using CASE.Models;
using CASE.Repositories;

namespace CASE.Services
{
    public class ConsultaOperacionalService
    {
        private readonly AutorizacaoService autorizacao = new AutorizacaoService();
        private readonly ConsultaOperacionalRepository repository = new ConsultaOperacionalRepository();
        private readonly EventoRepository eventoRepository = new EventoRepository();

        public DataTable Escalas(Usuario usuario, int idEvento)
        {
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            return repository.Escalas(perfil, atual.IdPessoa, idEvento);
        }

        public DataTable ProdutosDaEscala(Usuario usuario, int idEscala)
        {
            if (new EscalaService().BuscarPorId(usuario, idEscala) == null)
            {
                throw new InvalidOperationException("A escala não foi encontrada.");
            }
            return repository.ProdutosDaEscala(idEscala);
        }

        public DataTable Registros(Usuario usuario, int evento, DateTime inicio, DateTime fim)
        {
            ValidarPeriodo(inicio, fim);
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            return repository.Registros(perfil, atual.IdPessoa, evento, inicio, fim);
        }

        public DataTable Estoques(Usuario usuario)
        {
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            return repository.Estoques(perfil, atual.IdPessoa);
        }

        public DataTable Movimentacoes(Usuario usuario, DateTime inicio, DateTime fim)
        {
            ValidarPeriodo(inicio, fim);
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            return repository.Movimentacoes(perfil, atual.IdPessoa, inicio, fim);
        }

        public DataTable MovimentacoesDoDashboard(Usuario usuario, DateTime inicio, DateTime fim)
        {
            ValidarPeriodo(inicio, fim);
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            DataTable consultadas = repository.Movimentacoes(
                perfil, atual.IdPessoa, inicio, fim, true);
            HashSet<int> eventosConsiderados = new HashSet<int>();

            foreach (Evento evento in eventoRepository.ListarTodos())
            {
                if (evento != null &&
                    !string.Equals(evento.Situacao, SituacoesEvento.Cancelado, StringComparison.OrdinalIgnoreCase) &&
                    autorizacao.PodeAcessarEvento(atual, perfil, evento))
                {
                    eventosConsiderados.Add(evento.IdEvento);
                }
            }

            DataTable resultado = consultadas.Clone();
            foreach (DataRow movimento in consultadas.Rows)
            {
                if (movimento["id_evento"] == DBNull.Value ||
                    eventosConsiderados.Contains(Convert.ToInt32(movimento["id_evento"])))
                {
                    resultado.ImportRow(movimento);
                }
            }

            resultado.Columns.Remove("id_evento");
            return resultado;
        }

        public void ExportarPdf(Usuario usuario, bool estoqueAtual, DateTime inicio, DateTime fim, string caminho)
        {
            ExportarRelatorio(usuario, estoqueAtual ? "Estoque" : "Movimentações", inicio, fim, caminho, "pdf");
        }

        public DataTable Relatorio(Usuario usuario, string tipo, DateTime inicio, DateTime fim)
        {
            ValidarPeriodo(inicio, fim);
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            switch (tipo)
            {
                case "Movimentações":
                    return repository.Movimentacoes(perfil, atual.IdPessoa, inicio, fim);
                case "Estoque":
                    return repository.Estoques(perfil, atual.IdPessoa);
                case "Estoque crítico":
                    return EstoqueCritico(perfil, atual.IdPessoa);
                case "Vendas e perdas":
                    return repository.Registros(perfil, atual.IdPessoa, 0, inicio, fim);
                case "Solicitações de reposição":
                    return repository.Relatorio(tipo, perfil, atual.IdPessoa, inicio, fim);
                case "Eventos":
                case "Solicitações ao central":
                case "Despesas":
                case "Apurações":
                    if (perfil == "Membro")
                    {
                        throw new UnauthorizedAccessException("Seu perfil não pode consultar este relatório.");
                    }
                    return repository.Relatorio(tipo, perfil, atual.IdPessoa, inicio, fim);
                default:
                    throw new ArgumentException("Selecione um tipo de relatório válido.");
            }
        }

        private DataTable EstoqueCritico(string perfil, int idPessoa)
        {
            // Filtra apenas os saldos que a consulta de estoque já autoriza para esta conta.
            DataTable estoques = repository.Estoques(perfil, idPessoa);
            DataTable criticos = estoques.Clone();
            criticos.Columns.Add("situacao", typeof(string));
            criticos.Columns.Add("reposicao_minima", typeof(decimal));
            foreach (DataRow estoque in estoques.Rows)
            {
                decimal atual = Convert.ToDecimal(estoque["quantidade_atual"]);
                decimal minimo = Convert.ToDecimal(estoque["quantidade_minima"]);
                if (atual > 0 && atual >= minimo)
                    continue;
                DataRow linha = criticos.NewRow();
                foreach (DataColumn coluna in estoques.Columns)
                    linha[coluna.ColumnName] = estoque[coluna];
                linha["situacao"] = atual <= 0 ? "Sem estoque" : "Abaixo do mínimo";
                linha["reposicao_minima"] = Math.Max(0, minimo - atual);
                criticos.Rows.Add(linha);
            }
            return criticos;
        }

        public DataTable ItensParaCorrecao(Usuario usuario, string entidade, int id)
        {
            ExigirAcessoOperacao(usuario, entidade, id);
            return repository.ItensParaCorrecao(entidade, id);
        }

        public DataTable HistoricoDaOperacao(Usuario usuario, string entidade, int id)
        {
            ExigirAcessoOperacao(usuario, entidade, id);
            return repository.HistoricoDaOperacao(entidade, id);
        }

        private static void ExigirAcessoOperacao(Usuario usuario, string entidade, int id)
        {
            object operacao;
            switch (entidade)
            {
                case "registro":
                    operacao = new RegistroService().BuscarPorId(usuario, id);
                    break;
                case "entradamercadoria":
                    operacao = new EntradaMercadoriaService().BuscarPorId(usuario, id);
                    break;
                case "transferenciaestoque":
                    operacao = new TransferenciaEstoqueService().BuscarPorId(usuario, id);
                    break;
                default:
                    throw new ArgumentException("Tipo de operação inválido.");
            }
            if (operacao == null)
            {
                throw new InvalidOperationException("A operação não foi encontrada.");
            }
        }

        public void ExportarRelatorio(Usuario usuario, string tipo, DateTime inicio, DateTime fim,
            string caminho, bool pdf)
        {
            ExportarRelatorio(usuario, tipo, inicio, fim, caminho, pdf ? "pdf" : "csv");
        }

        public void ExportarRelatorio(Usuario usuario, string tipo, DateTime inicio, DateTime fim,
            string caminho, string formato)
        {
            ExportarRelatorio(usuario, tipo, inicio, fim, caminho, formato, null);
        }

        public void ExportarRelatorio(Usuario usuario, string tipo, DateTime inicio, DateTime fim,
            string caminho, string formato, string tipoGrafico)
        {
            DataTable dados = Relatorio(usuario, tipo, inicio, fim);
            string perfil;
            Usuario atual = autorizacao.ValidarUsuarioEObterPerfil(usuario, out perfil);
            var contexto = new CASE.Utils.ContextoRelatorio
            {
                Titulo = tipo,
                TipoGrafico = tipoGrafico,
                Instituicao = repository.InstituicoesDoRelatorio(perfil, atual.IdPessoa),
                Filtros = tipo == "Estoque crítico" ? "Saldo atual — sem estoque ou abaixo do mínimo, somente nos locais autorizados. " +
                    "Reposição mínima = mínimo − saldo, limitada a zero; não movimenta mercadorias." :
                    tipo == "Estoque" ? "Saldo atual — locais autorizados" :
                    "Período: " + inicio.ToString("dd/MM/yyyy") + " a " + fim.ToString("dd/MM/yyyy") +
                    " — escopo autorizado da conta"
            };
            if (formato == "pdf")
            {
                CASE.Utils.ExportadorRelatorio.ExportarPdf(dados, caminho, contexto);
            }
            else if (formato == "xlsx")
            {
                CASE.Utils.ExportadorRelatorio.ExportarExcel(dados, caminho, contexto);
            }
            else if (formato == "csv")
            {
                CASE.Utils.ExportadorRelatorio.ExportarCsv(dados, caminho);
            }
            else throw new ArgumentException("Selecione CSV, Excel ou PDF.");
        }

        public void ExportarCsv(Usuario usuario, bool estoqueAtual, DateTime inicio, DateTime fim, string caminho)
        {
            DataTable dados;
            if (estoqueAtual)
            {
                dados = Estoques(usuario);
            }
            else
            {
                dados = Movimentacoes(usuario, inicio, fim);
            }
            CASE.Utils.ExportadorRelatorio.ExportarCsv(dados, caminho);
        }

        private static void ValidarPeriodo(DateTime inicio, DateTime fim)
        {
            if (inicio.Date > fim.Date || fim.Date == DateTime.MaxValue.Date)
            {
                throw new ArgumentException("Informe um período válido.");
            }
        }
    }
}
