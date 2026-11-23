using System;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    internal sealed class DetalhesReposicaoForm : Form
    {
        public DetalhesReposicaoForm(Usuario usuario, int id, bool analisar)
        {
            var service = new SolicitacaoService();
            Solicitacao pedido = service.BuscarPorId(usuario, id);
            var resumo = service.ObterResumo(usuario, id);
            Text = "Reposição do setor #" + id;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(840, 650);
            MinimumSize = new Size(740, 580);
            ShowInTaskbar = false;
            MinimizeBox = false;
            IconoAplicacao.Aplicar(this);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(24), BackColor = VisualUtils.Fundo };
            foreach (int altura in new[] { 44, 42, 94 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, altura));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.Controls.Add(new Label { Text = Text, Dock = DockStyle.Fill, ForeColor = VisualUtils.Marinho, Font = new Font("Segoe UI", 22, FontStyle.Bold) }, 0, 0);
            var situacao = new Label
            {
                Text = TextoInterface.FormatarValorBanco(pedido.Situacao), Size = new Size(180, 30),
                TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(234, 242, 254), ForeColor = VisualUtils.Azul
            };
            layout.Controls.Add(situacao, 0, 1);
            VisualUtils.Arredondar(situacao, 8);
            layout.Controls.Add(new Label
            {
                Text = resumo.Evento + " · " + resumo.Setor + Environment.NewLine + "Solicitante: " + resumo.Solicitante +
                    Environment.NewLine + "Solicitado em " + pedido.DataHora.ToString("dd/MM/yyyy HH:mm"),
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = VisualUtils.TextoSuave
            }, 0, 2);
            var grade = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
            };
            grade.Columns.Add("produto", "Produto solicitado");
            grade.Columns.Add("quantidade", "Quantidade");
            grade.Columns.Add("unidade", "Unidade");
            grade.Columns[0].FillWeight = 220;
            foreach (var item in resumo.Itens) grade.Rows.Add(item.Produto, item.Quantidade.ToString("0.###"), item.Unidade);
            layout.Controls.Add(grade, 0, 3);
            string orientacao = pedido.Situacao == SituacoesSolicitacao.Pendente
                ? "Aprovar autoriza o pedido. Os produtos só saem do estoque geral ao efetivar a transferência."
                : pedido.Situacao == SituacoesSolicitacao.Aprovada
                    ? "Pronto para atender: a transferência retira os produtos do estoque geral e abastece o setor."
                    : "Consulte os produtos e a situação do pedido.";
            layout.Controls.Add(new TextBox
            {
                Name = "txtObservacoesReposicao", Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
                Text = orientacao + Environment.NewLine + (string.IsNullOrWhiteSpace(pedido.Descricao) ? "" : "Observação: " + pedido.Descricao) +
                    (string.IsNullOrWhiteSpace(pedido.JustificativaRejeicao) ? "" : Environment.NewLine + "Motivo da rejeição: " + pedido.JustificativaRejeicao)
            }, 0, 4);
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
            if (analisar && (pedido.Situacao == SituacoesSolicitacao.Pendente || pedido.Situacao == SituacoesSolicitacao.Aprovada))
            {
                acoes.Controls.Add(new Button
                {
                    Text = pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Efetivar transferência" : "Aprovar pedido",
                    DialogResult = DialogResult.Yes, Size = new Size(195, 38), BackColor = VisualUtils.Azul, ForeColor = Color.White
                });
                if (pedido.Situacao == SituacoesSolicitacao.Pendente)
                    acoes.Controls.Add(new Button { Text = "Rejeitar pedido", DialogResult = DialogResult.No, Size = new Size(140, 38) });
            }
            var fechar = new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Size = new Size(100, 38) };
            acoes.Controls.Add(fechar);
            CancelButton = fechar;
            layout.Controls.Add(acoes, 0, 5);
            Controls.Add(layout);
            FrontendTheme.Aplicar(this);
            VisualUtils.ExibirEstadoVazio(grade, "Nenhum item encontrado neste pedido.");
        }
    }
}
