using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public sealed partial class TodosEventosControl : UserControl
    {
        public event Action<Evento> EventoSolicitado;
        public TodosEventosControl()
        {
            InitializeComponent();
        }

        public TodosEventosControl(Usuario usuario, string titulo)
        {
            InitializeComponent();
            lblTitulo.Text = titulo;
            lista.Controls.Clear();
            var eventos = new EventoService().ListarEmAberto(usuario);
            foreach (Evento evento in eventos)
            {
                var card = new Panel
                {
                    BackColor = Color.White,
                    Size = new Size(360, 228),
                    Margin = new Padding(0, 0, 16, 16),
                    Padding = new Padding(20)
                };
                var nome = new Label
                {
                    Text = evento.Nome,
                    Font = new Font("Segoe UI", 13, FontStyle.Bold),
                    ForeColor = VisualUtils.Marinho,
                    AutoEllipsis = true
                };
                nome.SetBounds(20, 18, 320, 34);
                var situacao = new Label
                {
                    Text = TextoInterface.FormatarValorBanco(evento.Situacao),
                    ForeColor = VisualUtils.Azul,
                    BackColor = Color.FromArgb(234, 242, 254),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9)
                };
                situacao.SetBounds(20, 58, 135, 28);
                var detalhes = new Label
                {
                    Text = evento.Local + Environment.NewLine + evento.DataInicio.ToString("dd/MM/yyyy") + " a " + evento.DataFim.ToString("dd/MM/yyyy"),
                    ForeColor = VisualUtils.TextoSuave,
                    Font = new Font("Segoe UI", 10),
                    AutoEllipsis = true
                };
                detalhes.SetBounds(20, 100, 320, 52);
                var abrir = new Button
                {
                    Text = "Abrir " + titulo.ToLowerInvariant(),
                    BackColor = VisualUtils.Azul,
                    ForeColor = Color.White,
                    AccessibleName = "Abrir " + titulo + " de " + evento.Nome
                };
                abrir.SetBounds(20, 170, 320, 38);
                abrir.Click += Tratar_abrir_Click;
                card.Controls.AddRange(new Control[] { nome, situacao, detalhes, abrir });
                lista.Controls.Add(card);
                void Tratar_abrir_Click(object s, EventArgs e)
                {
                    EventoSolicitado?.Invoke(evento);
                }
            }

            if (eventos.Count == 0)
                lista.Controls.Add(new Label { Text = "Nenhum evento em aberto disponível para sua conta.", AutoSize = true, ForeColor = VisualUtils.TextoSuave, Padding = new Padding(16) });
            FrontendTheme.Aplicar(this);
        }
    }
}
