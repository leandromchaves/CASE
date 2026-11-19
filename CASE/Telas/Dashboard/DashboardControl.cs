using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CASE.Models;
using CASE.Services;

namespace CASE
{
    public partial class DashboardControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Dictionary<DateTime, decimal> quantidadesPorDia = new Dictionary<DateTime, decimal>();
        private ComboBox cmbPeriodoGrafico;
        private ComboBox cmbTipoMovimento;
        private ComboBox cmbTipoGrafico;
        private Chart grafico;
        private ListBox movimentacoesRecentes;
        private readonly ToolTip dicaIndicadorEntradas = new ToolTip();
        private DataTable movimentosConsultados;
        private DateTime inicioGrafico;
        private Label avisoGrafico;

        public DashboardControl() : this(null)
        {
        }

        public DashboardControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            CASE.Utils.FrontendTheme.Aplicar(this);

            CASE.Utils.VisualUtils.Arredondar(panelTotalProdutos, 12);
            CASE.Utils.VisualUtils.Arredondar(panelEstoqueBaixo, 12);
            CASE.Utils.VisualUtils.Arredondar(panelIconeProdutos, 10);
            CASE.Utils.VisualUtils.Arredondar(panelIconeEstoqueBaixo, 10);
            CASE.Utils.VisualUtils.Arredondar(panelEntradasMes, 12);
            CASE.Utils.VisualUtils.Arredondar(panelSaidasMes, 12);
            CASE.Utils.VisualUtils.Arredondar(panelIconeEntradas, 10);
            CASE.Utils.VisualUtils.Arredondar(panelIconeSaidas, 10);
            CASE.Utils.VisualUtils.Arredondar(panelGraficoMovimentacoes, 12);
            CASE.Utils.VisualUtils.Arredondar(panelMovimentacoesRecentes, 12);
            BackColor = CASE.Utils.FrontendTheme.Fundo;
            AutoScroll = true;
            AutoScrollMinSize = new Size(970, 620);
            lblSubtituloDashboard.Text = "Resumo dos dados disponíveis";
            lblEntradasMesTitulo.Text = "Unidades recebidas no mês";
            dicaIndicadorEntradas.SetToolTip(
                lblEntradasMesTitulo,
                "Soma das quantidades registradas como entrada. Transferências entre estoques não são incluídas.");
            dicaIndicadorEntradas.SetToolTip(
                lblEntradasMesValor,
                "Soma das quantidades registradas como entrada. Transferências entre estoques não são incluídas.");
            lblTituloDashboard.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            lblTituloDashboard.SetBounds(80, 25, 400, 40);
            lblSubtituloDashboard.Location = new Point(82, 68);
            panelIconeProdutos.BackColor = Color.FromArgb(224, 237, 255);
            panelIconeEstoqueBaixo.BackColor = Color.FromArgb(255, 244, 219);
            panelIconeEntradas.BackColor = Color.FromArgb(220, 247, 237);
            panelIconeSaidas.BackColor = Color.FromArgb(255, 229, 233);
            CarregarTotalDeProdutos();
            CarregarEstoqueBaixo();
            lblEntradasMesValor.Text = "—";
            lblSaidasMesValor.Text = "—";
            lblTituloGrafico.Text = "Movimentações sem dados consolidados";
            lblTituloMovimentacoes.Text = "Sem dados disponíveis";
            lblTituloMovimentacoes.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ConfigurarGrafico();
            CarregarMovimentosReais();
            EventHandler ajustar = (s, e) =>
            {
                int largura = Math.Max(970, ClientSize.Width) - 60;
                Panel[] cards = { panelTotalProdutos, panelEstoqueBaixo, panelEntradasMes, panelSaidasMes };
                Panel[] icones = { panelIconeProdutos, panelIconeEstoqueBaixo, panelIconeEntradas, panelIconeSaidas };
                for (int i = 0; i < 4; i++)
                {
                    cards[i].SetBounds(30 + i * (largura + 16) / 4, 112, (largura - 48) / 4, 112);
                    icones[i].Location = new Point(cards[i].Width - 54, 16);
                    CASE.Utils.VisualUtils.Arredondar(cards[i], 12);
                }
                int direita = Math.Max(360, largura * 38 / 100);
                int altura = Math.Max(330, ClientSize.Height - 278);
                panelGraficoMovimentacoes.SetBounds(30, 244, largura - direita - 16, altura);
                panelMovimentacoesRecentes.SetBounds(30 + largura - direita, 244, direita, altura);
                CASE.Utils.VisualUtils.Arredondar(panelGraficoMovimentacoes, 12);
                CASE.Utils.VisualUtils.Arredondar(panelMovimentacoesRecentes, 12);
                AjustarGrafico();
            };
            SizeChanged += ajustar;
            ajustar(this, EventArgs.Empty);
        }

        private void CarregarTotalDeProdutos()
        {
            try
            {
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                if (perfil == "Membro")
                {
                    HashSet<int> produtos = new HashSet<int>();
                    foreach (DataRow linha in new ConsultaOperacionalService().Estoques(usuarioAutenticado).Rows)
                    {
                        produtos.Add(Convert.ToInt32(linha["id_produto"]));
                    }
                    lblTotalProdutosValor.Text = produtos.Count.ToString();
                    return;
                }
                ProdutoService produtoService = new ProdutoService();
                int totalProdutos = produtoService.Contar(usuarioAutenticado);
                lblTotalProdutosValor.Text = totalProdutos.ToString();
            }
            catch (Exception)
            {
                lblTotalProdutosValor.Text = "—";
                lblSubtituloDashboard.Text = "Não foi possível atualizar os indicadores. Tente novamente.";
            }
        }

        private void CarregarEstoqueBaixo()
        {
            try
            {
                LocalEstoqueService localService = new LocalEstoqueService();
                EstoqueService estoqueService = new EstoqueService();
                List<CASE.Models.LocalEstoque> locais = localService.Listar(
                    usuarioAutenticado);
                int quantidadeEstoqueBaixo = 0;

                foreach (CASE.Models.LocalEstoque local in locais)
                {
                    List<CASE.Models.Estoque> saldos = estoqueService.ListarPorLocal(
                        usuarioAutenticado,
                        local.IdLocalEstoque);

                    foreach (CASE.Models.Estoque estoque in saldos)
                    {
                        if (estoque.QuantidadeAtual <= 0 ||
                            estoque.QuantidadeAtual < estoque.QuantidadeMinima)
                        {
                            quantidadeEstoqueBaixo++;
                        }
                    }
                }

                lblEstoqueBaixoValor.Text = quantidadeEstoqueBaixo.ToString();
            }
            catch (Exception)
            {
                lblEstoqueBaixoValor.Text = "—";
            }
        }

        private void ConfigurarGrafico()
        {
            cmbPeriodoGrafico = CriarFiltro("cmbPeriodoGrafico", "Período",
                new[] { "Este mês", "Últimos 7 dias", "Últimos 30 dias" }, 20);
            cmbTipoMovimento = CriarFiltro("cmbTipoMovimento", "Movimentações",
                new[] { "Todas", "Entradas", "Transferências", "Vendas", "Perdas" }, 180);
            cmbTipoGrafico = CriarFiltro("cmbTipoGrafico", "Tipo de gráfico",
                new[] { "Colunas", "Barras", "Linhas", "Área", "Pizza", "Rosca" }, 340);
            cmbTipoGrafico.SelectedIndex = 5;

            grafico = new Chart { Name = "graficoMovimentacoes", BackColor = Color.White };
            var area = new ChartArea("Movimentos");
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 9F);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9F);
            area.AxisY.LabelStyle.Format = "0.###";
            area.AxisX.LabelStyle.ForeColor = area.AxisY.LabelStyle.ForeColor = CASE.Utils.VisualUtils.TextoSuave;
            area.AxisX.LineWidth = area.AxisY.LineWidth = 0;
            area.AxisY.Minimum = 0;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(232, 238, 246);
            area.AxisX.LineColor = area.AxisY.LineColor = CASE.Utils.VisualUtils.Borda;
            area.AxisX.MajorTickMark.Enabled = area.AxisY.MajorTickMark.Enabled = false;
            grafico.ChartAreas.Add(area);
            grafico.AntiAliasing = AntiAliasingStyles.All;
            grafico.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
            grafico.Palette = ChartColorPalette.None;
            grafico.PaletteCustomColors = new[]
            {
                CASE.Utils.VisualUtils.Azul, Color.FromArgb(30, 163, 147),
                Color.FromArgb(242, 174, 65), Color.FromArgb(136, 103, 201),
                Color.FromArgb(75, 159, 210), Color.FromArgb(207, 119, 141)
            };
            grafico.Legends.Add(new Legend("Produtos")
            {
                Docking = Docking.Bottom, Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 9F), ForeColor = CASE.Utils.VisualUtils.Texto,
                MaximumAutoSize = 28, Enabled = false
            });
            var serie = new Series("Unidades")
            {
                ChartType = SeriesChartType.Column,
                Color = CASE.Utils.VisualUtils.Azul,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                IsXValueIndexed = true,
                ToolTip = "#AXISLABEL: #VALY{0.###} unidades"
            };
            serie["PointWidth"] = "0.6";
            grafico.Series.Add(serie);
            panelGraficoMovimentacoes.Controls.Add(grafico);
            avisoGrafico = new Label
            {
                Name = "lblAvisoGrafico", Text = "Sem movimentações no período e filtro selecionados.",
                AutoSize = false, ForeColor = CASE.Utils.VisualUtils.TextoSuave,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White
            };
            panelGraficoMovimentacoes.Controls.Add(avisoGrafico);
            movimentacoesRecentes = new ListBox
            {
                Name = "lstMovimentacoesRecentes", BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F), ItemHeight = 25,
                ForeColor = CASE.Utils.VisualUtils.Texto, HorizontalScrollbar = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            movimentacoesRecentes.SetBounds(20, 62,
                panelMovimentacoesRecentes.Width - 40, panelMovimentacoesRecentes.Height - 82);
            panelMovimentacoesRecentes.Controls.Add(movimentacoesRecentes);
            cmbPeriodoGrafico.SelectedIndexChanged += (s, e) => CarregarMovimentosReais();
            cmbTipoMovimento.SelectedIndexChanged += (s, e) => AtualizarGrafico();
            cmbTipoGrafico.SelectedIndexChanged += (s, e) => AtualizarGrafico();
        }

        private ComboBox CriarFiltro(string nome, string legenda, string[] opcoes, int esquerda)
        {
            var label = new Label
            {
                Name = "lbl_" + nome, Text = legenda,
                ForeColor = CASE.Utils.VisualUtils.TextoSuave, AutoSize = true
            };
            label.SetBounds(esquerda, 51, 148, 18);
            var combo = new ComboBox
            {
                Name = nome, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F), AccessibleName = legenda
            };
            var painel = new Panel
            {
                Name = "panelFiltro_" + nome, BackColor = CASE.Utils.VisualUtils.Fundo,
                Padding = new Padding(8, 0, 8, 0)
            };
            painel.SetBounds(esquerda, 73, 148, 40);
            combo.Items.AddRange(opcoes);
            combo.SelectedIndex = 0;
            panelGraficoMovimentacoes.Controls.Add(label);
            painel.Controls.Add(combo);
            panelGraficoMovimentacoes.Controls.Add(painel);
            CASE.Utils.VisualUtils.EstilizarSelecao(combo);
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = painel.BackColor;
            combo.Font = new Font("Segoe UI", 10F);
            CASE.Utils.VisualUtils.Arredondar(painel, 10);
            painel.SizeChanged += (s, e) => CASE.Utils.VisualUtils.Arredondar(painel, 10);
            painel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var borda = new Pen(combo.ContainsFocus ? CASE.Utils.VisualUtils.Azul : CASE.Utils.VisualUtils.Borda))
                using (var contorno = CASE.Utils.VisualUtils.CriarRetanguloArredondado(
                    new Rectangle(0, 0, painel.Width - 1, painel.Height - 1), 10))
                    e.Graphics.DrawPath(borda, contorno);
            };
            combo.GotFocus += (s, e) => painel.Invalidate();
            combo.LostFocus += (s, e) => painel.Invalidate();
            return combo;
        }

        private void AjustarGrafico()
        {
            int largura = (panelGraficoMovimentacoes.Width - 64) / 3;
            ComboBox[] filtros = { cmbPeriodoGrafico, cmbTipoMovimento, cmbTipoGrafico };
            for (int i = 0; i < filtros.Length; i++)
            {
                filtros[i].Parent.SetBounds(20 + i * (largura + 12), 73, largura, 40);
                panelGraficoMovimentacoes.Controls["lbl_" + filtros[i].Name].Left = filtros[i].Parent.Left;
            }
            grafico.SetBounds(12, 130, panelGraficoMovimentacoes.Width - 24,
                Math.Max(160, panelGraficoMovimentacoes.Height - 142));
            avisoGrafico.Bounds = grafico.Bounds;
        }

        private void CarregarMovimentosReais()
        {
            try
            {
                DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                inicioGrafico = cmbPeriodoGrafico.SelectedIndex == 1 ? DateTime.Today.AddDays(-6) :
                    cmbPeriodoGrafico.SelectedIndex == 2 ? DateTime.Today.AddDays(-29) : inicioMes;
                var consulta = new ConsultaOperacionalService();
                DataTable movimentosMes = consulta.Movimentacoes(usuarioAutenticado, inicioMes, DateTime.Today);
                movimentosConsultados = inicioGrafico == inicioMes ? movimentosMes :
                    consulta.Movimentacoes(usuarioAutenticado, inicioGrafico, DateTime.Today);
                decimal entradas = 0;
                decimal saidas = 0;
                foreach (DataRow movimento in movimentosMes.Rows)
                {
                    decimal quantidade = Convert.ToDecimal(movimento["quantidade"]);
                    string tipo = Convert.ToString(movimento["tipo"]);
                    if (tipo == "entrada")
                        entradas += quantidade;
                    else if (tipo == "venda" || tipo == "perda")
                        saidas += quantidade;
                }
                lblEntradasMesValor.Text = entradas.ToString("N0");
                lblSaidasMesValor.Text = saidas.ToString("N0");
                lblSubtituloDashboard.Text = "Indicadores do mês. Os filtros abaixo alteram o gráfico e as movimentações recentes.";
                lblTituloGrafico.Text = "Unidades movimentadas por dia";
                lblTituloMovimentacoes.Text = "Movimentações recentes";
                AtualizarGrafico();
            }
            catch (Exception erro)
            {
                movimentosConsultados = null;
                grafico.Series[0].Points.Clear();
                movimentacoesRecentes.Items.Clear();
                avisoGrafico.Text = "Não foi possível carregar as movimentações. Troque o período para tentar novamente.";
                avisoGrafico.Visible = true;
                avisoGrafico.BringToFront();
                lblEntradasMesValor.Text = "—";
                lblSaidasMesValor.Text = "—";
                lblTituloMovimentacoes.Text = "Não foi possível consultar o histórico";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em DashboardControl");
            }
        }

        private void AtualizarGrafico()
        {
            if (movimentosConsultados == null)
                return;
            quantidadesPorDia.Clear();
            movimentacoesRecentes.Items.Clear();
            string[] tipos = { "", "entrada", "transferencia", "venda", "perda" };
            string filtro = tipos[cmbTipoMovimento.SelectedIndex];
            var movimentos = movimentosConsultados.Rows.Cast<DataRow>()
                .Where(r => filtro == "" || Convert.ToString(r["tipo"]) == filtro)
                .OrderByDescending(r => Convert.ToDateTime(r["data_hora"])).ToList();
            foreach (DataRow movimento in movimentos)
            {
                DateTime dia = Convert.ToDateTime(movimento["data_hora"]).Date;
                decimal quantidade = Convert.ToDecimal(movimento["quantidade"]);
                if (!quantidadesPorDia.ContainsKey(dia))
                    quantidadesPorDia.Add(dia, 0);
                quantidadesPorDia[dia] += quantidade;
                if (movimentacoesRecentes.Items.Count < 20)
                    movimentacoesRecentes.Items.Add(dia.ToString("dd/MM") + " · " +
                        NomeMovimento(Convert.ToString(movimento["tipo"])) + " · " +
                        movimento["produto"] + " · " + quantidade.ToString("0.###"));
            }
            Series serie = grafico.Series[0];
            serie.Points.Clear();
            SeriesChartType[] formatos =
            {
                SeriesChartType.Column, SeriesChartType.Bar, SeriesChartType.Line,
                SeriesChartType.Area, SeriesChartType.Pie, SeriesChartType.Doughnut
            };
            serie.ChartType = formatos[cmbTipoGrafico.SelectedIndex];
            bool circular = serie.ChartType == SeriesChartType.Pie || serie.ChartType == SeriesChartType.Doughnut;
            bool linhas = serie.ChartType == SeriesChartType.Line;
            serie.MarkerStyle = linhas ? MarkerStyle.Circle : MarkerStyle.None;
            serie.MarkerSize = 8;
            serie.MarkerColor = Color.White;
            serie.MarkerBorderColor = CASE.Utils.VisualUtils.Azul;
            serie.MarkerBorderWidth = 2;
            serie.BorderWidth = 3;
            serie.Color = circular ? Color.Empty : CASE.Utils.VisualUtils.Azul;
            serie.BackSecondaryColor = Color.FromArgb(230, 240, 255);
            serie.BackGradientStyle = serie.ChartType == SeriesChartType.Area ? GradientStyle.TopBottom : GradientStyle.None;
            serie.BorderColor = circular ? Color.White : CASE.Utils.VisualUtils.Azul;
            serie.IsVisibleInLegend = circular;
            serie.Label = circular ? "#PERCENT{P0}" : "";
            serie.LabelForeColor = Color.White;
            serie.LegendText = "#AXISLABEL";
            serie["PieLabelStyle"] = "Inside";
            serie["DoughnutRadius"] = "65";
            if (circular)
            {
                var produtos = movimentos.GroupBy(r => Convert.ToString(r["produto"]))
                    .Select(g => new { Nome = g.Key, Quantidade = g.Sum(r => Convert.ToDecimal(r["quantidade"])) })
                    .OrderByDescending(p => p.Quantidade).ToList();
                foreach (var produto in produtos.Take(5))
                    serie.Points.AddXY(produto.Nome, produto.Quantidade);
                if (produtos.Count > 5)
                    serie.Points.AddXY("Outros produtos", produtos.Skip(5).Sum(p => p.Quantidade));
            }
            else
            {
                for (DateTime dia = inicioGrafico; dia <= DateTime.Today; dia = dia.AddDays(1))
                {
                    decimal quantidade;
                    bool temMovimento = quantidadesPorDia.TryGetValue(dia, out quantidade);
                    if (linhas && !temMovimento)
                        continue;
                    int ponto = serie.Points.AddXY(dia.ToString("dd/MM"), quantidade);
                    if (linhas && quantidade == 0)
                        serie.Points[ponto].MarkerStyle = MarkerStyle.None;
                }
            }
            var area = grafico.ChartAreas[0];
            area.AxisX.Enabled = area.AxisY.Enabled = circular ? AxisEnabled.False : AxisEnabled.True;
            grafico.Legends[0].Enabled = circular;
            lblTituloGrafico.Text = circular ? "Unidades movimentadas por produto" :
                linhas ? "Unidades por dia com movimentação" : "Unidades movimentadas por dia";
            lblTituloGrafico.AutoSize = false;
            lblTituloGrafico.Width = panelGraficoMovimentacoes.Width - 40;
            lblTituloGrafico.AutoEllipsis = true;
            area.AxisX.LabelStyle.Interval = Math.Max(1, (int)Math.Ceiling(serie.Points.Count / 10.0));
            area.AxisY.Maximum = double.NaN;
            grafico.ResetAutoValues();
            avisoGrafico.Text = "Sem movimentações no período e filtro selecionados.";
            avisoGrafico.Visible = movimentos.Count == 0;
            if (avisoGrafico.Visible)
            {
                avisoGrafico.BringToFront();
                movimentacoesRecentes.Items.Add("Nenhuma movimentação neste período e filtro.");
            }
            grafico.Invalidate();
        }

        private static string NomeMovimento(string tipo)
        {
            switch (tipo)
            {
                case "entrada": return "Entrada";
                case "transferencia": return "Transferência";
                case "venda": return "Venda";
                case "perda": return "Perda";
                default: return tipo;
            }
        }
    }
}
