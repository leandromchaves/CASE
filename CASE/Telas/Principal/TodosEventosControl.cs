using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public sealed class TodosEventosControl : UserControl
    {
        public event Action<Evento> EventoSolicitado;
        private readonly Dictionary<string, DataGridView> tabelas = new Dictionary<string, DataGridView>();
        private readonly TabControl abas = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(148, 34),
            Padding = new Point(12, 5),
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9F)
        };
        private readonly Dictionary<string, decimal> totais = new Dictionary<string, decimal>();

        public TodosEventosControl(Usuario usuario, string titulo, Func<Evento, UserControl> criar)
        {
            BackColor = VisualUtils.Fundo;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1,
                Padding = new Padding(24), BackColor = VisualUtils.Fundo
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = titulo + " · Todos os eventos em aberto", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = VisualUtils.Marinho }, 0, 0);
            var ferramentas = new Panel { Dock = DockStyle.Fill, BackColor = VisualUtils.Fundo };
            ferramentas.Controls.Add(new Label { Text = "Cada linha mostra de qual evento vêm os dados. Dê dois cliques para abrir esse evento.",
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = VisualUtils.TextoSuave, Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent });
            var pesquisa = new TextBox { Name = "txtPesquisarTodosEventos", Dock = DockStyle.Right, Width = 300, Height = 38 };
            ferramentas.Controls.Add(pesquisa);
            pesquisa.BringToFront();
            pesquisa.TextChanged += (s, e) => VisualUtils.FiltrarGrades(abas, pesquisa.Text.Trim());
            VisualUtils.EstilizarPesquisa(pesquisa, "Pesquisar evento, produto ou pessoa");
            layout.Controls.Add(ferramentas, 0, 1);
            var resumo = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = VisualUtils.TextoSuave, Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent, Padding = new Padding(4, 0, 0, 0)
            };
            layout.Controls.Add(resumo, 0, 2);
            layout.Controls.Add(abas, 0, 3);
            Controls.Add(layout);
            List<Evento> eventos = new EventoService().ListarEmAberto(usuario);
            foreach (Evento evento in eventos)
            {
                using (UserControl tela = criar(evento))
                {
                    // O histórico de vendas e perdas é filtrado por escala; reunimos todas as escalas do evento.
                    ComboBox escalas = Encontrar(tela, "cmbSetorRegistro") as ComboBox;
                    if (escalas != null && escalas.Items.Count > 0)
                    {
                        for (int i = 0; i < escalas.Items.Count; i++)
                        {
                            escalas.SelectedIndex = i;
                            CopiarTabelas(tela, evento);
                        }
                    }
                    else CopiarTabelas(tela, evento);
                    foreach (string nome in new[] { "lblValorFinanceiroResumo0", "lblValorFinanceiroResumo1", "lblValorFinanceiroResumo2" })
                    {
                        Control valor = Encontrar(tela, nome);
                        decimal numero;
                        if (valor != null)
                        {
                            if (!decimal.TryParse(valor.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out numero))
                                throw new InvalidOperationException("Há valores financeiros incompletos. Consulte cada evento para conferir os dados.");
                            if (!totais.ContainsKey(nome)) totais[nome] = 0;
                            totais[nome] += numero;
                        }
                    }
                }
            }
            resumo.Text = eventos.Count + " evento(s) autorizado(s).";
            if (totais.Count == 3)
                resumo.Text += " Hoje: vendas " + totais["lblValorFinanceiroResumo0"].ToString("C2") +
                    " · Despesas " + totais["lblValorFinanceiroResumo1"].ToString("C2") +
                    " · Resultado parcial " + totais["lblValorFinanceiroResumo2"].ToString("C2");
            FrontendTheme.Aplicar(this);
        }

        private static Control Encontrar(Control raiz, string nome)
        {
            Control[] encontrados = raiz.Controls.Find(nome, true);
            return encontrados.Length == 0 ? null : encontrados[0];
        }

        private void CopiarTabelas(Control raiz, Evento evento)
        {
            foreach (Control controle in raiz.Controls)
            {
                DataGridView origem = controle as DataGridView;
                if (origem == null) { CopiarTabelas(controle, evento); continue; }
                DataGridView destino;
                if (!tabelas.TryGetValue(origem.Name, out destino))
                {
                    destino = new DataGridView { Name = "todos_" + origem.Name, Dock = DockStyle.Fill,
                        ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                        RowHeadersVisible = false, BackgroundColor = Color.White,
                        BorderStyle = BorderStyle.None,
                        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                        EnableHeadersVisualStyles = false,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        MultiSelect = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        RowTemplate = { Height = 50 },
                        ColumnHeadersHeight = 42,
                        GridColor = Color.FromArgb(240, 243, 248) };
                    destino.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "evento", HeaderText = "Evento", ReadOnly = true,
                        FillWeight = 22, MinimumWidth = 170,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                            ForeColor = VisualUtils.Marinho,
                            BackColor = Color.FromArgb(244, 248, 255),
                            SelectionBackColor = Color.FromArgb(220, 234, 255),
                            SelectionForeColor = VisualUtils.Marinho,
                            Padding = new Padding(12, 0, 8, 0)
                        }
                    });
                    foreach (DataGridViewColumn coluna in origem.Columns)
                    {
                        var copia = new DataGridViewTextBoxColumn { Name = coluna.Name, HeaderText = coluna.HeaderText,
                            Visible = coluna.Visible, DefaultCellStyle = coluna.DefaultCellStyle.Clone(),
                            FillWeight = coluna.FillWeight };
                        destino.Columns.Add(copia);
                    }
                    destino.CellDoubleClick += (s, e) =>
                    {
                        if (e.RowIndex >= 0) EventoSolicitado?.Invoke((Evento)destino.Rows[e.RowIndex].Tag);
                    };
                    var aba = new TabPage(TituloTabela(origem.Name))
                    {
                        BackColor = Color.White,
                        Padding = new Padding(8)
                    };
                    aba.Controls.Add(destino);
                    abas.TabPages.Add(aba);
                    tabelas.Add(origem.Name, destino);
                    VisualUtils.ExibirEstadoVazio(destino, "Nenhum registro encontrado nos eventos autorizados.");
                }
                foreach (DataGridViewRow linha in origem.Rows)
                {
                    if (linha.IsNewRow || !linha.Visible) continue;
                    var valores = new object[origem.Columns.Count + 1];
                    valores[0] = evento.Nome;
                    for (int i = 0; i < origem.Columns.Count; i++) valores[i + 1] = linha.Cells[i].Value;
                    int indice = destino.Rows.Add(valores);
                    destino.Rows[indice].Tag = evento;
                    destino.Rows[indice].Cells[0].ToolTipText = evento.Nome;
                }
            }
        }

        private static string TituloTabela(string nome)
        {
            switch (nome)
            {
                case "dgvEquipe": return "Equipe";
                case "dgvSetores": return "Setores";
                case "dgvEscala": return "Escalas";
                case "dgvCustos": return "Despesas de hoje";
                case "dgvConferenciasDia": return "Recebimentos de hoje";
                case "dgvRegistrosEquipe": return "Vendas e perdas";
                default: return "Solicitações";
            }
        }
    }
}
