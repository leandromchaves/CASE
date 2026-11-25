using System;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public sealed partial class DetalhesPedidoCentralForm : Form
    {
        public DetalhesPedidoCentralForm()
        {
            InitializeComponent();
        }

        public DetalhesPedidoCentralForm(Usuario usuario, int idPedido, bool permitirDecisao) : this()
        {
            var service = new SolicitacaoEstoqueCentralService();
            SolicitacaoEstoqueCentral pedido = service.BuscarPorId(usuario, idPedido);
            var itens = service.ListarItens(usuario, idPedido);
            Evento evento = new EventoService().BuscarPorId(usuario, pedido.IdEvento);
            int? transferencia = service.BuscarIdTransferenciaAtendimento(usuario, idPedido);
            Text = lblTitulo.Text = "Solicitação #" + idPedido;
            lblSituacao.Text = transferencia.HasValue ? "Atendida · transferência registrada" : TextoInterface.FormatarValorBanco(pedido.Situacao);
            string origem = "—", destino = "—";
            foreach (LocalEstoque local in new LocalEstoqueService().Listar(usuario))
            {
                if (local.IdLocalEstoque == pedido.IdLocalOrigem) origem = local.Nome;
                if (local.IdLocalEstoque == pedido.IdLocalDestino) destino = local.Nome;
            }
            lblResumo.Text = (evento == null ? "Evento indisponível" : evento.Nome) + " · " + TextoInterface.FormatarValorBanco(pedido.Tipo) +
                Environment.NewLine + "Solicitante: " + service.ConsultarLoginSolicitante(usuario, idPedido) +
                " · " + pedido.DataHora.ToString("dd/MM/yyyy HH:mm") + Environment.NewLine + "Origem: " + origem + "  →  Destino: " + destino;
            foreach (SolicitacaoEstoqueCentralItem item in itens)
            {
                Produto produto = new ProdutoService().BuscarPorId(usuario, item.IdProduto);
                grade.Rows.Add(produto == null ? "Produto indisponível" : produto.Nome,
                    item.Quantidade.ToString("0.###"), produto == null ? "—" : produto.UnidadeMedida);
            }
            string orientacao = transferencia.HasValue ? "Atendimento concluído. A transferência #" + transferencia.Value + " atualizou os estoques de origem e destino."
                : pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Pedido aprovado. O estoque será atualizado ao efetivar a transferência."
                : pedido.Situacao == SituacoesSolicitacao.Pendente ? "Aguardando análise. Aprovar o pedido ainda não transfere os produtos."
                : "Consulte os itens e a situação deste pedido.";
            if (!string.IsNullOrWhiteSpace(pedido.JustificativaRejeicao)) orientacao = "Motivo da rejeição: " + pedido.JustificativaRejeicao;
            lblOrientacao.Text = orientacao;
            bool podeDecidir = permitirDecisao && !transferencia.HasValue &&
                (pedido.Situacao == SituacoesSolicitacao.Pendente || pedido.Situacao == SituacoesSolicitacao.Aprovada);
            btnAprovar.Visible = podeDecidir;
            btnAprovar.Text = pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Efetivar transferência" : "Aprovar pedido";
            btnRejeitar.Visible = podeDecidir && pedido.Situacao == SituacoesSolicitacao.Pendente;
            IconoAplicacao.Aplicar(this);
            VisualUtils.Arredondar(lblSituacao, 8);
            FrontendTheme.Aplicar(this);
            VisualUtils.ExibirEstadoVazio(grade, "Nenhum item encontrado neste pedido.");
        }
    }
}
