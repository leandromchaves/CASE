using System;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public sealed class TodosEventosControl : UserControl
    {
        public event Action<Evento> EventoSolicitado;

        public TodosEventosControl(Usuario usuario, string titulo)
        {
            BackColor = VisualUtils.Fundo;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                Padding = new Padding(24), BackColor = VisualUtils.Fundo
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label
            {
                Text = titulo, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = VisualUtils.Marinho
            }, 0, 0);
            layout.Controls.Add(new Label
            {
                Text = "Escolha um evento para consultar os dados e acessar as ações disponíveis para sua conta.",
                Dock = DockStyle.Fill, ForeColor = VisualUtils.TextoSuave,
                Font = new Font("Segoe UI", 10), AutoEllipsis = true
            }, 0, 1);
            var eventos = new EventoService().ListarEmAberto(usuario);
            var lista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true,
                Padding = new Padding(0, 0, 12, 12), BackColor = VisualUtils.Fundo
            };
            foreach (Evento evento in eventos)
            {
                var card = new Panel
                {
                    BackColor = Color.White, Size = new Size(360, 228),
                    Margin = new Padding(0, 0, 16, 16), Padding = new Padding(20)
                };
                var nome = new Label
                {
                    Text = evento.Nome, Font = new Font("Segoe UI", 13, FontStyle.Bold),
                    ForeColor = VisualUtils.Marinho, AutoEllipsis = true
                };
                nome.SetBounds(20, 18, 320, 34);
                var situacao = new Label
                {
                    Text = TextoInterface.FormatarValorBanco(evento.Situacao),
                    ForeColor = VisualUtils.Azul, BackColor = Color.FromArgb(234, 242, 254),
                    TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9)
                };
                situacao.SetBounds(20, 58, 135, 28);
                var detalhes = new Label
                {
                    Text = evento.Local + Environment.NewLine +
                        evento.DataInicio.ToString("dd/MM/yyyy") + " a " + evento.DataFim.ToString("dd/MM/yyyy"),
                    ForeColor = VisualUtils.TextoSuave, Font = new Font("Segoe UI", 10), AutoEllipsis = true
                };
                detalhes.SetBounds(20, 100, 320, 52);
                var abrir = new Button
                {
                    Text = "Abrir " + titulo.ToLowerInvariant(), BackColor = VisualUtils.Azul,
                    ForeColor = Color.White, AccessibleName = "Abrir " + titulo + " de " + evento.Nome
                };
                abrir.SetBounds(20, 170, 320, 38);
                abrir.Click += (s, e) => EventoSolicitado?.Invoke(evento);
                card.Controls.AddRange(new Control[] { nome, situacao, detalhes, abrir });
                lista.Controls.Add(card);
            }
            if (eventos.Count == 0)
                lista.Controls.Add(new Label
                {
                    Text = "Nenhum evento em aberto disponível para sua conta.", AutoSize = true,
                    ForeColor = VisualUtils.TextoSuave, Padding = new Padding(16)
                });
            layout.Controls.Add(lista, 0, 2);
            Controls.Add(layout);
            FrontendTheme.Aplicar(this);
        }
    }
}
