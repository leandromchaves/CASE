using System;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    internal sealed class DetalhesPedidoCentralForm : Form
    {
        public DetalhesPedidoCentralForm(Usuario usuario, int idPedido, bool permitirDecisao)
        {
            var service = new SolicitacaoEstoqueCentralService();
            SolicitacaoEstoqueCentral pedido = service.BuscarPorId(usuario, idPedido);
            var itens = service.ListarItens(usuario, idPedido);
            Evento evento = new EventoService().BuscarPorId(usuario, pedido.IdEvento);
            int? transferencia = service.BuscarIdTransferenciaAtendimento(usuario, idPedido);
            Text = "Solicitação #" + idPedido;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(840, 620);
            MinimumSize = new Size(740, 540);
            ShowInTaskbar = false;
            MinimizeBox = false;
            IconoAplicacao.Aplicar(this);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
                Padding = new Padding(24), BackColor = VisualUtils.Fundo
            };
            foreach (int altura in new[] { 44, 42, 100 })
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, altura));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.Controls.Add(new Label
            {
                Text = "Solicitação #" + idPedido, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = VisualUtils.Marinho
            }, 0, 0);
            var status = new Label
            {
                Text = transferencia.HasValue ? "Atendida · transferência registrada" : TextoInterface.FormatarValorBanco(pedido.Situacao),
                AutoSize = false, Width = 330, Height = 30,
                TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(234, 242, 254), ForeColor = VisualUtils.Azul
            };
            layout.Controls.Add(status, 0, 1);
            VisualUtils.Arredondar(status, 8);
            string origem = "—", destino = "—";
            foreach (LocalEstoque local in new LocalEstoqueService().Listar(usuario))
            {
                if (local.IdLocalEstoque == pedido.IdLocalOrigem) origem = local.Nome;
                if (local.IdLocalEstoque == pedido.IdLocalDestino) destino = local.Nome;
            }
            layout.Controls.Add(new Label
            {
                Text = (evento == null ? "Evento indisponível" : evento.Nome) + " · " + TextoInterface.FormatarValorBanco(pedido.Tipo) +
                    Environment.NewLine + "Solicitante: " + service.ConsultarLoginSolicitante(usuario, idPedido) +
                    " · " + pedido.DataHora.ToString("dd/MM/yyyy HH:mm") +
                    Environment.NewLine + "Origem: " + origem + "  →  Destino: " + destino,
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = VisualUtils.TextoSuave,
                Padding = new Padding(0, 8, 0, 8)
            }, 0, 2);
            var grade = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White
            };
            grade.Columns.Add("produto", "Produto solicitado");
            grade.Columns.Add("quantidade", "Quantidade");
            grade.Columns.Add("unidade", "Unidade");
            grade.Columns[0].FillWeight = 220;
            foreach (SolicitacaoEstoqueCentralItem item in itens)
            {
                Produto produto = new ProdutoService().BuscarPorId(usuario, item.IdProduto);
                grade.Rows.Add(produto == null ? "Produto indisponível" : produto.Nome,
                    item.Quantidade.ToString("0.###"), produto == null ? "—" : produto.UnidadeMedida);
            }
            layout.Controls.Add(grade, 0, 3);
            string orientacao = transferencia.HasValue ? "Atendimento concluído. A transferência #" + transferencia.Value + " atualizou os estoques de origem e destino."
                : pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Pedido aprovado. O estoque será atualizado ao efetivar a transferência."
                : pedido.Situacao == SituacoesSolicitacao.Pendente ? "Aguardando análise. Aprovar o pedido ainda não transfere os produtos."
                : "Consulte os itens e a situação deste pedido.";
            if (!string.IsNullOrWhiteSpace(pedido.JustificativaRejeicao)) orientacao = "Motivo da rejeição: " + pedido.JustificativaRejeicao;
            layout.Controls.Add(new Label
            {
                Text = orientacao, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10),
                ForeColor = VisualUtils.TextoSuave, Padding = new Padding(0, 14, 0, 0)
            }, 0, 4);
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var fechar = new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Size = new Size(100, 38) };
            CancelButton = fechar;
            if (permitirDecisao && !transferencia.HasValue &&
                (pedido.Situacao == SituacoesSolicitacao.Pendente || pedido.Situacao == SituacoesSolicitacao.Aprovada))
            {
                acoes.Controls.Add(new Button
                {
                    Text = pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Efetivar transferência" : "Aprovar pedido",
                    DialogResult = DialogResult.Yes, Size = new Size(195, 38), BackColor = VisualUtils.Azul, ForeColor = Color.White
                });
                if (pedido.Situacao == SituacoesSolicitacao.Pendente)
                    acoes.Controls.Add(new Button { Text = "Rejeitar pedido", DialogResult = DialogResult.No, Size = new Size(140, 38) });
            }
            acoes.Controls.Add(fechar);
            layout.Controls.Add(acoes, 0, 5);
            Controls.Add(layout);
            FrontendTheme.Aplicar(this);
            VisualUtils.ExibirEstadoVazio(grade, "Nenhum item encontrado neste pedido.");
        }
    }
}
