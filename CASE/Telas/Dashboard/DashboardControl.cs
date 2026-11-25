using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class DashboardControl : UserControl
    {
        private Usuario usuarioAutenticado;
        private Dictionary<DateTime, decimal> quantidadesPorDia = new Dictionary<DateTime, decimal>();
        private ComboBox cmbPeriodoGrafico;
        private ComboBox cmbTipoMovimento;
        private ComboBox cmbTipoGrafico;
        private Chart grafico;
        private ListBox movimentacoesRecentes;
        private ToolTip dicaIndicadorEntradas = new ToolTip();
        private DataTable movimentosConsultados;
        private DateTime inicioGrafico;
        private Label avisoGrafico;
        public DashboardControl() : this(null)
        {
        }

        public DashboardControl(Usuario usuario)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            usuarioAutenticado = usuario;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelTotalProdutos, 12);
            VisualUtils.Arredondar(panelEstoqueBaixo, 12);
            VisualUtils.Arredondar(panelIconeProdutos, 10);
            VisualUtils.Arredondar(panelIconeEstoqueBaixo, 10);
            VisualUtils.Arredondar(panelEntradasMes, 12);
            VisualUtils.Arredondar(panelSaidasMes, 12);
            VisualUtils.Arredondar(panelIconeEntradas, 10);
            VisualUtils.Arredondar(panelIconeSaidas, 10);
            VisualUtils.Arredondar(panelGraficoMovimentacoes, 12);
            VisualUtils.Arredondar(panelMovimentacoesRecentes, 12);
            BackColor = FrontendTheme.Fundo;
            AutoScroll = true;
            AutoScrollMinSize = new Size(970, 620);
            lblSubtituloDashboard.Text = "Resumo dos dados disponíveis";
            lblEntradasMesTitulo.Text = "Unidades recebidas no mês";
            dicaIndicadorEntradas.SetToolTip(lblEntradasMesTitulo, "Soma das unidades recebidas no estoque central neste mês. Confira em Estoque > Histórico de mercadorias, com o tipo Entrada e o local Estoque central. Transferências não entram nesta soma.");
            dicaIndicadorEntradas.SetToolTip(lblEntradasMesValor, "Soma das unidades recebidas no estoque central neste mês. Confira em Estoque > Histórico de mercadorias, com o tipo Entrada e o local Estoque central. Transferências não entram nesta soma.");
            lblTituloDashboard.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            lblTituloDashboard.SetBounds(80, 25, 400, 40);
            lblSubtituloDashboard.Location = new Point(82, 68);
            panelIconeProdutos.BackColor = Color.FromArgb(224, 237, 255);
            panelIconeEstoqueBaixo.BackColor = Color.FromArgb(255, 244, 219);
            panelIconeEntradas.BackColor = Color.FromArgb(220, 247, 237);
            panelIconeSaidas.BackColor = Color.FromArgb(255, 229, 233);
            CarregarTotalDeProdutos();
            CarregarEstoqueBaixo();
            ConfigurarDetalhesEstoqueBaixo(panelEstoqueBaixo);
            var verEstoques = new Button
            {
                Text = "Ver itens →",
                BackColor = Color.White,
                ForeColor = VisualUtils.Azul,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                AccessibleName = "Consultar produtos com estoque baixo"
            };
            verEstoques.FlatAppearance.BorderSize = 0;
            verEstoques.SetBounds(panelEstoqueBaixo.Width - 110, 76, 100, 26);
            verEstoques.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            verEstoques.Click += AbrirEstoquesBaixos;
            panelEstoqueBaixo.Controls.Add(verEstoques);
            ConfigurarConsultaCard(panelTotalProdutos, AbrirProdutos);
            ConfigurarConsultaCard(panelEntradasMes, Preparar_ConfigurarConsultaCard);
            ConfigurarConsultaCard(panelSaidasMes, Preparar_ConfigurarConsultaCard2);
            lblEntradasMesValor.Text = "—";
            lblSaidasMesValor.Text = "—";
            lblTituloGrafico.Text = "Movimentações sem dados consolidados";
            lblTituloMovimentacoes.Text = "Sem dados disponíveis";
            lblTituloMovimentacoes.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ConfigurarGrafico();
            CarregarMovimentosReais();
            EventHandler ajustar = Preparar_Executar;
            SizeChanged += ajustar;
            ajustar(this, EventArgs.Empty);
        }

        private void ConfigurarDetalhesEstoqueBaixo(Control controle)
        {
            controle.Cursor = Cursors.Hand;
            controle.Click += AbrirEstoquesBaixos;
            foreach (Control filho in controle.Controls)
                ConfigurarDetalhesEstoqueBaixo(filho);
        }

        private void ConfigurarConsultaCard(Panel card, EventHandler abrir)
        {
            AssociarConsultaCard(card, abrir);
            var consultar = new Button
            {
                Text = "Ver itens →",
                BackColor = Color.White,
                ForeColor = VisualUtils.Azul,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                AccessibleName = "Consultar " + card.Name
            };
            consultar.FlatAppearance.BorderSize = 0;
            consultar.SetBounds(card.Width - 110, 76, 100, 26);
            consultar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            consultar.Click += abrir;
            card.Controls.Add(consultar);
        }

        private void AssociarConsultaCard(Control controle, EventHandler abrir)
        {
            controle.Cursor = Cursors.Hand;
            controle.Click += abrir;
            foreach (Control filho in controle.Controls)
                AssociarConsultaCard(filho, abrir);
        }

        private void AbrirProdutos(object sender, EventArgs e)
        {
            try
            {
                var dados = new DataTable();
                dados.Columns.Add("Produto");
                dados.Columns.Add("Unidade");
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                if (perfil == "Membro")
                {
                    var incluidos = new HashSet<int>();
                    foreach (DataRow saldo in new ConsultaOperacionalService().Estoques(usuarioAutenticado).Rows)
                        if (incluidos.Add(Convert.ToInt32(saldo["id_produto"])))
                            dados.Rows.Add(saldo["produto"], saldo["unidade_medida"]);
                }
                else
                {
                    dados.Columns.Add("Situação");
                    foreach (Produto produto in new ProdutoService().Listar(usuarioAutenticado))
                        dados.Rows.Add(produto.Nome, produto.UnidadeMedida, produto.Ativo ? "Ativo" : "Inativo");
                }

                lblTotalProdutosValor.Text = dados.Rows.Count.ToString();
                ExibirConsultaIndicador("Produtos cadastrados", perfil == "Membro" ? "Produtos dos estoques disponíveis para sua conta, sem repetir o mesmo produto por local." : "Catálogo completo de produtos, incluindo os inativos.", dados.Rows.Count + " produto(s)", dados);
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar os produtos. Tente novamente.");
            }
        }

        private void AbrirMovimentosDoMes(bool recebimentos)
        {
            try
            {
                DateTime hoje = DateTime.Today;
                DateTime inicio = new DateTime(hoje.Year, hoje.Month, 1);
                DataTable movimentos = new ConsultaOperacionalService().MovimentacoesDoDashboard(usuarioAutenticado, inicio, hoje);
                var dados = new DataTable();
                dados.Columns.Add("Data / hora", typeof(DateTime));
                dados.Columns.Add("Tipo");
                dados.Columns.Add("Produto");
                dados.Columns.Add("Quantidade", typeof(decimal));
                dados.Columns.Add("Local");
                dados.Columns.Add("Responsável");
                decimal total = 0;
                foreach (DataRow movimento in movimentos.Rows)
                {
                    string tipo = Convert.ToString(movimento["tipo"]);
                    if (recebimentos ? tipo != "entrada" : tipo != "venda" && tipo != "perda")
                        continue;
                    decimal quantidade = Convert.ToDecimal(movimento["quantidade"]);
                    total += quantidade;
                    dados.Rows.Add(movimento["data_hora"], TextoInterface.FormatarValorBanco(tipo), movimento["produto"], quantidade, movimento[recebimentos ? "destino" : "origem"], movimento["responsavel"]);
                }

                if (recebimentos)
                    lblEntradasMesValor.Text = total.ToString("0.###");
                else
                    lblSaidasMesValor.Text = total.ToString("0.###");
                ExibirConsultaIndicador(recebimentos ? "Unidades recebidas no mês" : "Vendas e perdas no mês", inicio.ToString("dd/MM/yyyy") + " a " + hoje.ToString("dd/MM/yyyy") + " · " + (recebimentos ? "Recebimentos no estoque central. Transferências não entram no total." : "Unidades que saíram por venda ou perda. Eventos cancelados não entram no total."), total.ToString("0.###") + " unidade(s) · " + dados.Rows.Count + " item(ns) de movimentação", dados);
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar as movimentações do mês. Tente novamente.");
            }
        }

        private void ExibirConsultaIndicador(string titulo, string explicacao, string total, DataTable dados)
        {
            using (var janela = new Form
            {
                Text = titulo,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(980, 620),
                MinimumSize = new Size(740, 440),
                ShowInTaskbar = false,
                MinimizeBox = false
            }

            )
            {
                IconoAplicacao.Aplicar(janela);
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(24),
                    ColumnCount = 1,
                    RowCount = 6
                };
                foreach (int altura in new[]
                {
                    44,
                    56,
                    36,
                    50
                }

                )
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, altura));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                layout.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = VisualUtils.Marinho }, 0, 0);
                layout.Controls.Add(new Label { Text = explicacao, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = VisualUtils.TextoSuave }, 0, 1);
                layout.Controls.Add(new Label { Text = total, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = VisualUtils.Marinho }, 0, 2);
                var pesquisa = new TextBox
                {
                    Name = "txtPesquisaIndicador",
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(pesquisa, 0, 3);
                var grade = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    DataSource = dados.DefaultView,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    BackgroundColor = Color.White,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false
                };
                layout.Controls.Add(grade, 0, 4);
                pesquisa.TextChanged += Tratar_pesquisa_TextChanged;
                var fechar = new Button
                {
                    Text = "Fechar",
                    Dock = DockStyle.Right,
                    Width = 110,
                    DialogResult = DialogResult.Cancel
                };
                layout.Controls.Add(fechar, 0, 5);
                janela.CancelButton = fechar;
                janela.Controls.Add(layout);
                VisualUtils.EstilizarPesquisa(pesquisa, "Pesquisar nesta consulta");
                FrontendTheme.Aplicar(janela);
                foreach (DataGridViewColumn coluna in grade.Columns)
                {
                    if (coluna.ValueType == typeof(DateTime))
                        coluna.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    if (coluna.ValueType == typeof(decimal))
                        coluna.DefaultCellStyle.Format = "0.###";
                }

                VisualUtils.ExibirEstadoVazio(grade, "Nenhum item encontrado nesta consulta.");
                janela.ShowDialog(this);
                void Tratar_pesquisa_TextChanged(object s, EventArgs e)
                {
                    string termo = pesquisa.Text.Trim();
                    string literal = termo.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                    dados.DefaultView.RowFilter = string.IsNullOrEmpty(termo) ? string.Empty : string.Join(" OR ", dados.Columns.Cast<DataColumn>().Where(Tratar_ExibirConsultaIndicador_Where).Select(Tratar_ExibirConsultaIndicador_Select).ToArray());
                    bool Tratar_ExibirConsultaIndicador_Where(DataColumn coluna)
                    {
                        return coluna.DataType == typeof(string);
                    }

                    string Tratar_ExibirConsultaIndicador_Select(DataColumn coluna)
                    {
                        return "[" + coluna.ColumnName + "] LIKE '%" + literal + "%'";
                    }
                }
            }
        }

        private void AbrirEstoquesBaixos(object sender, EventArgs e)
        {
            try
            {
                var locais = new LocalEstoqueService().ListarOperacionais(usuarioAutenticado);
                var ids = new HashSet<int>(locais.Select(AbrirEstoquesBaixos_Select));
                DataTable saldos = new ConsultaOperacionalService().Estoques(usuarioAutenticado);
                using (var janela = new Form
                {
                    Text = "Produtos que precisam de reposição",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(920, 580),
                    MinimumSize = new Size(720, 420),
                    ShowInTaskbar = false,
                    MinimizeBox = false
                }

                )
                {
                    IconoAplicacao.Aplicar(janela);
                    var layout = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        Padding = new Padding(24),
                        ColumnCount = 1,
                        RowCount = 5
                    };
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
                    layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
                    layout.Controls.Add(new Label { Text = "Atenção ao estoque", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = VisualUtils.Marinho }, 0, 0);
                    layout.Controls.Add(new Label { Text = "Produtos no mínimo ou abaixo dele, incluindo os sem estoque. A lista considera os locais ativos disponíveis para sua conta.", Dock = DockStyle.Fill, ForeColor = VisualUtils.TextoSuave, Font = new Font("Segoe UI", 10) }, 0, 1);
                    var resumo = new Label
                    {
                        Dock = DockStyle.Fill,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        ForeColor = VisualUtils.Marinho
                    };
                    layout.Controls.Add(resumo, 0, 2);
                    var grade = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        MultiSelect = false
                    };
                    grade.Columns.Add("produto", "Produto");
                    grade.Columns.Add("local", "Local / evento");
                    grade.Columns.Add("saldo", "Disponível");
                    grade.Columns.Add("minimo", "Mínimo");
                    grade.Columns.Add("situacao", "Situação");
                    grade.Columns[1].FillWeight = 160;
                    int zerados = 0;
                    foreach (DataRow saldo in saldos.Rows)
                    {
                        if (!ids.Contains(Convert.ToInt32(saldo["id_local_estoque"])))
                            continue;
                        decimal atual = Convert.ToDecimal(saldo["quantidade_atual"]);
                        decimal minimo = Convert.ToDecimal(saldo["quantidade_minima"]);
                        if (atual > 0 && atual > minimo)
                            continue;
                        if (atual <= 0)
                            zerados++;
                        LocalEstoque local = locais.First(Tratar_AbrirEstoquesBaixos_First);
                        string unidade = Convert.ToString(saldo["unidade_medida"]);
                        grade.Rows.Add(saldo["produto"], local.Descricao, atual.ToString("0.###") + " " + unidade, minimo.ToString("0.###") + " " + unidade, atual <= 0 ? "Sem estoque" : "Estoque baixo");
                        bool Tratar_AbrirEstoquesBaixos_First(LocalEstoque item)
                        {
                            return item.IdLocalEstoque == Convert.ToInt32(saldo["id_local_estoque"]);
                        }
                    }

                    resumo.Text = grade.Rows.Count + " produto(s) por local precisam de atenção · " + zerados + " sem estoque";
                    layout.Controls.Add(grade, 0, 3);
                    var fechar = new Button
                    {
                        Text = "Fechar",
                        Dock = DockStyle.Right,
                        Width = 110,
                        DialogResult = DialogResult.Cancel
                    };
                    layout.Controls.Add(fechar, 0, 4);
                    janela.CancelButton = fechar;
                    janela.Controls.Add(layout);
                    FrontendTheme.Aplicar(janela);
                    VisualUtils.ExibirEstadoVazio(grade, "Tudo em dia! Nenhum produto precisa de reposição nos locais consultados.");
                    janela.ShowDialog(this);
                }

                CarregarEstoqueBaixo();
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar os produtos para reposição. Tente novamente.");
            }
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
                List<LocalEstoque> locais = localService.ListarOperacionais(usuarioAutenticado);
                int quantidadeEstoqueBaixo = 0;
                foreach (LocalEstoque local in locais)
                {
                    List<Estoque> saldos = estoqueService.ListarPorLocal(usuarioAutenticado, local.IdLocalEstoque);
                    foreach (Estoque estoque in saldos)
                    {
                        if (estoque.QuantidadeAtual <= 0 || estoque.QuantidadeAtual <= estoque.QuantidadeMinima)
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
            cmbPeriodoGrafico = CriarFiltro("cmbPeriodoGrafico", "Período", new[] { "Este mês", "Últimos 7 dias", "Últimos 30 dias" }, 20);
            cmbTipoMovimento = CriarFiltro("cmbTipoMovimento", "Movimentações", new[] { "Todas", "Entradas", "Transferências", "Vendas", "Perdas" }, 180);
            cmbTipoGrafico = CriarFiltro("cmbTipoGrafico", "Tipo de gráfico", new[] { "Colunas", "Barras", "Linhas", "Área", "Pizza", "Rosca" }, 340);
            cmbTipoGrafico.SelectedIndex = 5;
            grafico = new Chart
            {
                Name = "graficoMovimentacoes",
                BackColor = Color.White
            };
            var area = new ChartArea("Movimentos");
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 9F);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9F);
            area.AxisY.LabelStyle.Format = "0.###";
            area.AxisX.LabelStyle.ForeColor = area.AxisY.LabelStyle.ForeColor = VisualUtils.TextoSuave;
            area.AxisX.LineWidth = area.AxisY.LineWidth = 0;
            area.AxisY.Minimum = 0;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(232, 238, 246);
            area.AxisX.LineColor = area.AxisY.LineColor = VisualUtils.Borda;
            area.AxisX.MajorTickMark.Enabled = area.AxisY.MajorTickMark.Enabled = false;
            grafico.ChartAreas.Add(area);
            grafico.AntiAliasing = AntiAliasingStyles.All;
            grafico.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
            grafico.Palette = ChartColorPalette.None;
            grafico.PaletteCustomColors = new[]
            {
                VisualUtils.Azul,
                Color.FromArgb(30, 163, 147),
                Color.FromArgb(242, 174, 65),
                Color.FromArgb(136, 103, 201),
                Color.FromArgb(75, 159, 210),
                Color.FromArgb(207, 119, 141)
            };
            grafico.Legends.Add(new Legend("Produtos") { Docking = Docking.Bottom, Alignment = StringAlignment.Center, Font = new Font("Segoe UI", 9F), ForeColor = VisualUtils.Texto, MaximumAutoSize = 28, Enabled = false });
            var serie = new Series("Unidades")
            {
                ChartType = SeriesChartType.Column,
                Color = VisualUtils.Azul,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                IsXValueIndexed = true,
                ToolTip = "#AXISLABEL: #VALY{0.###} unidades"
            };
            serie["PointWidth"] = "0.6";
            grafico.Series.Add(serie);
            panelGraficoMovimentacoes.Controls.Add(grafico);
            avisoGrafico = new Label
            {
                Name = "lblAvisoGrafico",
                Text = "Sem movimentações no período e filtro selecionados.",
                AutoSize = false,
                ForeColor = VisualUtils.TextoSuave,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };
            panelGraficoMovimentacoes.Controls.Add(avisoGrafico);
            movimentacoesRecentes = new ListBox
            {
                Name = "lstMovimentacoesRecentes",
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                ItemHeight = 25,
                ForeColor = VisualUtils.Texto,
                HorizontalScrollbar = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            movimentacoesRecentes.SetBounds(20, 62, panelMovimentacoesRecentes.Width - 40, panelMovimentacoesRecentes.Height - 82);
            panelMovimentacoesRecentes.Controls.Add(movimentacoesRecentes);
            cmbPeriodoGrafico.SelectedIndexChanged += cmbPeriodoGrafico_SelectedIndexChanged;
            cmbTipoMovimento.SelectedIndexChanged += cmbTipoMovimento_SelectedIndexChanged;
            cmbTipoGrafico.SelectedIndexChanged += cmbTipoGrafico_SelectedIndexChanged;
        }

        private ComboBox CriarFiltro(string nome, string legenda, string[] opcoes, int esquerda)
        {
            var label = new Label
            {
                Name = "lbl_" + nome,
                Text = legenda,
                ForeColor = VisualUtils.TextoSuave,
                AutoSize = true
            };
            label.SetBounds(esquerda, 51, 148, 18);
            var combo = new ComboBox
            {
                Name = nome,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                AccessibleName = legenda
            };
            var painel = new Panel
            {
                Name = "panelFiltro_" + nome,
                BackColor = VisualUtils.Fundo,
                Padding = new Padding(8, 0, 8, 0)
            };
            painel.SetBounds(esquerda, 73, 148, 40);
            combo.Items.AddRange(opcoes);
            combo.SelectedIndex = 0;
            panelGraficoMovimentacoes.Controls.Add(label);
            painel.Controls.Add(combo);
            panelGraficoMovimentacoes.Controls.Add(painel);
            VisualUtils.EstilizarSelecao(combo);
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = painel.BackColor;
            combo.Font = new Font("Segoe UI", 10F);
            VisualUtils.Arredondar(painel, 10);
            painel.SizeChanged += Tratar_painel_SizeChanged;
            painel.Paint += Tratar_painel_Paint;
            combo.GotFocus += Tratar_combo_GotFocus;
            combo.LostFocus += Tratar_combo_LostFocus;
            return combo;
            void Tratar_painel_SizeChanged(object s, EventArgs e)
            {
                VisualUtils.Arredondar(painel, 10);
            }

            void Tratar_painel_Paint(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var borda = new Pen(combo.ContainsFocus ? VisualUtils.Azul : VisualUtils.Borda))
                using (var contorno = VisualUtils.CriarRetanguloArredondado(new Rectangle(0, 0, painel.Width - 1, painel.Height - 1), 10))
                    e.Graphics.DrawPath(borda, contorno);
            }

            void Tratar_combo_GotFocus(object s, EventArgs e)
            {
                painel.Invalidate();
            }

            void Tratar_combo_LostFocus(object s, EventArgs e)
            {
                painel.Invalidate();
            }
        }

        private void AjustarGrafico()
        {
            int largura = (panelGraficoMovimentacoes.Width - 64) / 3;
            ComboBox[] filtros =
            {
                cmbPeriodoGrafico,
                cmbTipoMovimento,
                cmbTipoGrafico
            };
            for (int i = 0; i < filtros.Length; i++)
            {
                filtros[i].Parent.SetBounds(20 + i * (largura + 12), 73, largura, 40);
                panelGraficoMovimentacoes.Controls["lbl_" + filtros[i].Name].Left = filtros[i].Parent.Left;
            }

            grafico.SetBounds(12, 130, panelGraficoMovimentacoes.Width - 24, Math.Max(160, panelGraficoMovimentacoes.Height - 142));
            avisoGrafico.Bounds = grafico.Bounds;
        }

        private void CarregarMovimentosReais()
        {
            try
            {
                DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                inicioGrafico = cmbPeriodoGrafico.SelectedIndex == 1 ? DateTime.Today.AddDays(-6) : cmbPeriodoGrafico.SelectedIndex == 2 ? DateTime.Today.AddDays(-29) : inicioMes;
                var consulta = new ConsultaOperacionalService();
                DataTable movimentosMes = consulta.MovimentacoesDoDashboard(usuarioAutenticado, inicioMes, DateTime.Today);
                movimentosConsultados = inicioGrafico == inicioMes ? movimentosMes : consulta.MovimentacoesDoDashboard(usuarioAutenticado, inicioGrafico, DateTime.Today);
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

                lblEntradasMesValor.Text = entradas.ToString("#,##0.###");
                lblSaidasMesValor.Text = saidas.ToString("#,##0.###");
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
                Trace.WriteLine(erro.GetType().Name + " em DashboardControl");
            }
        }

        private void AtualizarGrafico()
        {
            if (movimentosConsultados == null)
                return;
            quantidadesPorDia.Clear();
            movimentacoesRecentes.Items.Clear();
            string[] tipos =
            {
                "",
                "entrada",
                "transferencia",
                "venda",
                "perda"
            };
            string filtro = tipos[cmbTipoMovimento.SelectedIndex];
            var movimentos = movimentosConsultados.Rows.Cast<DataRow>().Where(Tratar_AtualizarGrafico_Where).OrderByDescending(AtualizarGrafico_OrderByDescending).ToList();
            foreach (DataRow movimento in movimentos)
            {
                DateTime dia = Convert.ToDateTime(movimento["data_hora"]).Date;
                decimal quantidade = Convert.ToDecimal(movimento["quantidade"]);
                if (!quantidadesPorDia.ContainsKey(dia))
                    quantidadesPorDia.Add(dia, 0);
                quantidadesPorDia[dia] += quantidade;
                if (movimentacoesRecentes.Items.Count < 20)
                    movimentacoesRecentes.Items.Add(dia.ToString("dd/MM") + " · " + NomeMovimento(Convert.ToString(movimento["tipo"])) + " · " + movimento["produto"] + " · " + quantidade.ToString("0.###"));
            }

            Series serie = grafico.Series[0];
            serie.Points.Clear();
            SeriesChartType[] formatos =
            {
                SeriesChartType.Column,
                SeriesChartType.Bar,
                SeriesChartType.Line,
                SeriesChartType.Area,
                SeriesChartType.Pie,
                SeriesChartType.Doughnut
            };
            serie.ChartType = formatos[cmbTipoGrafico.SelectedIndex];
            bool circular = serie.ChartType == SeriesChartType.Pie || serie.ChartType == SeriesChartType.Doughnut;
            bool linhas = serie.ChartType == SeriesChartType.Line;
            serie.MarkerStyle = linhas ? MarkerStyle.Circle : MarkerStyle.None;
            serie.MarkerSize = 8;
            serie.MarkerColor = Color.White;
            serie.MarkerBorderColor = VisualUtils.Azul;
            serie.MarkerBorderWidth = 2;
            serie.BorderWidth = 3;
            serie.Color = circular ? Color.Empty : VisualUtils.Azul;
            serie.BackSecondaryColor = Color.FromArgb(230, 240, 255);
            serie.BackGradientStyle = serie.ChartType == SeriesChartType.Area ? GradientStyle.TopBottom : GradientStyle.None;
            serie.BorderColor = circular ? Color.White : VisualUtils.Azul;
            serie.IsVisibleInLegend = circular;
            serie.Label = circular ? "#PERCENT{P0}" : "";
            serie.LabelForeColor = Color.White;
            serie.LegendText = "#AXISLABEL";
            serie["PieLabelStyle"] = "Inside";
            serie["DoughnutRadius"] = "65";
            if (circular)
            {
                Dictionary<string, decimal> totais = new Dictionary<string, decimal>();
                foreach (DataRow movimento in movimentos)
                {
                    string nome = Convert.ToString(movimento["produto"]);
                    decimal acumulado;
                    totais.TryGetValue(nome, out acumulado);
                    totais[nome] = acumulado + Convert.ToDecimal(movimento["quantidade"]);
                }

                List<TotalProduto> produtos = new List<TotalProduto>();
                foreach (KeyValuePair<string, decimal> total in totais)
                    produtos.Add(new TotalProduto { Nome = total.Key, Quantidade = total.Value, Ordem = produtos.Count });
                produtos.Sort(CompararTotaisProdutos);
                foreach (var produto in produtos.Take(5))
                    serie.Points.AddXY(produto.Nome, produto.Quantidade);
                if (produtos.Count > 5)
                    serie.Points.AddXY("Outros produtos", SomarOutrosProdutos(produtos));
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
            lblTituloGrafico.Text = circular ? "Unidades movimentadas por produto" : linhas ? "Unidades por dia com movimentação" : "Unidades movimentadas por dia";
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
            bool Tratar_AtualizarGrafico_Where(DataRow r)
            {
                return filtro == "" || Convert.ToString(r["tipo"]) == filtro;
            }
        }

        private static string NomeMovimento(string tipo)
        {
            switch (tipo)
            {
                case "entrada":
                    return "Entrada";
                case "transferencia":
                    return "Transferência";
                case "venda":
                    return "Venda";
                case "perda":
                    return "Perda";
                default:
                    return tipo;
            }
        }

        private void Preparar_ConfigurarConsultaCard(object s, EventArgs e)
        {
            AbrirMovimentosDoMes(true);
        }

        private void Preparar_ConfigurarConsultaCard2(object s, EventArgs e)
        {
            AbrirMovimentosDoMes(false);
        }

        private void Preparar_Executar(object s, EventArgs e)
        {
            int largura = Math.Max(970, ClientSize.Width) - 60;
            Panel[] cards =
            {
                panelTotalProdutos,
                panelEstoqueBaixo,
                panelEntradasMes,
                panelSaidasMes
            };
            Panel[] icones =
            {
                panelIconeProdutos,
                panelIconeEstoqueBaixo,
                panelIconeEntradas,
                panelIconeSaidas
            };
            for (int i = 0; i < 4; i++)
            {
                cards[i].SetBounds(30 + i * (largura + 16) / 4, 112, (largura - 48) / 4, 112);
                icones[i].Location = new Point(cards[i].Width - 54, 16);
                VisualUtils.Arredondar(cards[i], 12);
            }

            int direita = Math.Max(360, largura * 38 / 100);
            int altura = Math.Max(330, ClientSize.Height - 278);
            panelGraficoMovimentacoes.SetBounds(30, 244, largura - direita - 16, altura);
            panelMovimentacoesRecentes.SetBounds(30 + largura - direita, 244, direita, altura);
            VisualUtils.Arredondar(panelGraficoMovimentacoes, 12);
            VisualUtils.Arredondar(panelMovimentacoesRecentes, 12);
            AjustarGrafico();
        }

        private int AbrirEstoquesBaixos_Select(LocalEstoque local)
        {
            return local.IdLocalEstoque;
        }

        private void cmbPeriodoGrafico_SelectedIndexChanged(object s, EventArgs e)
        {
            CarregarMovimentosReais();
        }

        private void cmbTipoMovimento_SelectedIndexChanged(object s, EventArgs e)
        {
            AtualizarGrafico();
        }

        private void cmbTipoGrafico_SelectedIndexChanged(object s, EventArgs e)
        {
            AtualizarGrafico();
        }

        private DateTime AtualizarGrafico_OrderByDescending(DataRow r)
        {
            return Convert.ToDateTime(r["data_hora"]);
        }

        private string AtualizarGrafico_GroupBy(DataRow r)
        {
            return Convert.ToString(r["produto"]);
        }

        private class TotalProduto
        {
            public string Nome;
            public decimal Quantidade;
            public int Ordem;
        }

        private static int CompararTotaisProdutos(TotalProduto a, TotalProduto b)
        {
            int comparacao = b.Quantidade.CompareTo(a.Quantidade);
            return comparacao == 0 ? a.Ordem.CompareTo(b.Ordem) : comparacao;
        }

        private static decimal SomarOutrosProdutos(List<TotalProduto> produtos)
        {
            decimal total = 0;
            for (int i = 5; i < produtos.Count; i++)
                total += produtos[i].Quantidade;
            return total;
        }
    }
}
