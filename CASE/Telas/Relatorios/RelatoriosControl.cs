using System;
using System.Windows.Forms;
using CASE.Utils;
using System.Data;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using CASE.Models;
using CASE.Services;

namespace CASE
{
    public partial class RelatoriosControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly DataGridView gradeRelatorio = new DataGridView();
        private readonly Label resumoRelatorio = new Label();
        private DataTable dadosRelatorio;
        private readonly ContextMenuStrip menuExportacoes = new ContextMenuStrip();
        private readonly CheckBox chkIncluirGrafico = new CheckBox();
        private readonly ComboBox cmbGraficoRelatorio = new ComboBox();
        private readonly Panel opcoesGrafico = new Panel();
        private Chart graficoRelatorio;
        private readonly FlowLayoutPanel perfisRelatorio = new FlowLayoutPanel();

        public RelatoriosControl() : this(null)
        {
        }

        public RelatoriosControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            Disposed += (s, e) => menuExportacoes.Dispose();
            CASE.Utils.FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelAcoesRelatorios, 12);
            panelAcoesRelatorios.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelAcoesRelatorios);
            VisualUtils.Arredondar(btnExportarPdf, 8);
            btnExportarPdf.SizeChanged += AjustarArredondamento;
            VisualUtils.Arredondar(btnExportarCsv, 8);
            btnExportarCsv.SizeChanged += AjustarArredondamento;
            VisualUtils.Arredondar(panelCardRelBaixo, 12);
            panelCardRelBaixo.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardRelBaixo);
            VisualUtils.Arredondar(panelCardRelSemEstoque, 12);
            panelCardRelSemEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardRelSemEstoque);
            VisualUtils.Arredondar(panelCardRelValor, 12);
            panelCardRelValor.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardRelValor);
            VisualUtils.Arredondar(panelCardRelSugestao, 12);
            panelCardRelSugestao.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardRelSugestao);
            VisualUtils.Arredondar(panelGraficoEsquerdo, 12);
            panelGraficoEsquerdo.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelGraficoEsquerdo);
            VisualUtils.Arredondar(panelGraficoDireito, 12);
            panelGraficoDireito.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelGraficoDireito);
            lblSubtituloRelatorios.Text = "Acompanhe o desempenho do seu estoque";
            lblGraficoEsquerdo.Text = "Entradas e saídas por dia";
            lblGraficoDireito.Text = "Distribuição por categoria";
            panelGraficoEsquerdoArea.BackColor = System.Drawing.Color.White;
            panelGraficoDireitoArea.BackColor = System.Drawing.Color.White;
            gradeRelatorio.Dock = DockStyle.Fill;
            gradeRelatorio.Name = "dgvRelatorio";
            gradeRelatorio.CellFormatting += FormatarValorExibido;
            gradeRelatorio.BackgroundColor = System.Drawing.Color.White;
            gradeRelatorio.RowHeadersVisible = false;
            gradeRelatorio.EnableHeadersVisualStyles = false;
            gradeRelatorio.ReadOnly = true;
            gradeRelatorio.AllowUserToAddRows = false;
            gradeRelatorio.AllowUserToDeleteRows = false;
            gradeRelatorio.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            panelGraficoEsquerdoArea.Controls.Add(gradeRelatorio);
            FrontendTheme.Aplicar(panelGraficoEsquerdoArea);
            resumoRelatorio.Dock = DockStyle.Fill;
            resumoRelatorio.Name = "lblResumoRelatorio";
            resumoRelatorio.AutoEllipsis = true;
            panelGraficoDireitoArea.Controls.Add(resumoRelatorio);
            lblGraficoEsquerdo.Text = "Dados autorizados";
            lblGraficoDireito.Text = "Resumo da consulta";
            btnExportarPdf.Text = "Gerar";
            btnExportarCsv.Visible = true;
            btnExportarCsv.Text = "Exportar...";
            btnExportarCsv.Enabled = false;
            btnExportarPdf.Click += GerarRelatorio;
            btnExportarCsv.Click += MostrarExportacoes;
            panelCardRelBaixo.Visible = panelCardRelSemEstoque.Visible = panelCardRelValor.Visible = panelCardRelSugestao.Visible = false;
            panelGraficoEsquerdo.Top = panelGraficoDireito.Top = 215;
            panelGraficoEsquerdo.Height = panelGraficoDireito.Height = 330;
            panelGraficoEsquerdoArea.Height = panelGraficoDireitoArea.Height = 250;
            cmbRelatorioCategoria.Items.Clear();
            cmbRelatorioCategoria.Items.AddRange(new object[] {
                "Movimentações", "Estoque", "Estoque crítico", "Vendas e perdas", "Solicitações de reposição" });
            if (usuario != null)
            {
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuario, out perfil);
                    if (perfil != "Membro")
                    {
                        cmbRelatorioCategoria.Items.AddRange(new object[] {
                            "Eventos", "Solicitações ao central", "Despesas", "Apurações" });
                    }
                }
                catch (Exception erro)
                {
                    System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ConfigurarRelatorios");
                }
            }
            cmbRelatorioCategoria.SelectedIndex = 0;
            cmbRelatorioCategoria.SelectedIndexChanged += (s, e) =>
            {
                AtualizarPerfisRelatorio();
                bool utilizaPeriodo = cmbRelatorioCategoria.Text != "Estoque" && cmbRelatorioCategoria.Text != "Estoque crítico";
                dtpRelatorioInicio.Enabled = utilizaPeriodo;
                dtpRelatorioFim.Enabled = utilizaPeriodo;
                dadosRelatorio = null;
                gradeRelatorio.DataSource = null;
                btnExportarCsv.Enabled = false;
                resumoRelatorio.Text = "Clique em Gerar para consultar o relatório selecionado.";
                AtualizarTiposGrafico();
                AtualizarGraficoRelatorio();
            };
            dtpRelatorioInicio.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpRelatorioFim.Value = DateTime.Today;
            foreach (Control campo in new Control[] { cmbRelatorioCategoria, dtpRelatorioInicio, dtpRelatorioFim })
            {
                campo.Top = 30;
                panelAcoesRelatorios.Controls.Add(FrontendTheme.Legenda(
                    campo == cmbRelatorioCategoria
                        ? "Tipo de relatório"
                        : campo == dtpRelatorioInicio
                            ? "Data inicial"
                            : "Data final",
                    new System.Drawing.Rectangle(campo.Left, 6, campo.Width, 20)));
            }
            perfisRelatorio.Name = "perfisRelatorio";
            perfisRelatorio.SetBounds(20, 76, 680, 36);
            perfisRelatorio.BackColor = Color.White;
            perfisRelatorio.WrapContents = false;
            panelAcoesRelatorios.Controls.Add(perfisRelatorio);
            panelAcoesRelatorios.Height = 124;
            AtualizarPerfisRelatorio();
            ConfigurarGraficoRelatorio();
            dtpRelatorioInicio.ValueChanged += InvalidarConsulta;
            dtpRelatorioFim.ValueChanged += InvalidarConsulta;
            if (usuario != null)
            {
                GerarRelatorio(this, EventArgs.Empty);
            }
            else
            {
                resumoRelatorio.Text = "Autentique-se para consultar relatórios.";
                btnExportarPdf.Enabled = false;
            }
        }

        private void AtualizarPerfisRelatorio()
        {
            perfisRelatorio.SuspendLayout();
            while (perfisRelatorio.Controls.Count > 0)
                perfisRelatorio.Controls[0].Dispose();
            perfisRelatorio.Controls.Add(new Label
            {
                Text = "Disponível para", AutoSize = false, Size = new Size(105, 30),
                TextAlign = ContentAlignment.MiddleLeft, ForeColor = VisualUtils.TextoSuave,
                Font = new Font("Segoe UI", 9), Margin = new Padding(0, 0, 10, 0)
            });
            foreach (string perfil in ConsultaOperacionalService.PerfisPermitidosNoRelatorio(cmbRelatorioCategoria.Text))
            {
                Color cor = perfil == "Membro" ? Color.FromArgb(29, 125, 96) :
                    perfil == "Gerente" ? VisualUtils.Azul : Color.FromArgb(113, 73, 181);
                Color fundo = perfil == "Membro" ? Color.FromArgb(233, 247, 241) :
                    perfil == "Gerente" ? Color.FromArgb(234, 242, 254) : Color.FromArgb(243, 238, 255);
                var selo = new Label
                {
                    Text = perfil, AccessibleName = "Relatório disponível para " + perfil,
                    Size = new Size(perfil == "Administrador" ? 130 : 92, 30),
                    TextAlign = ContentAlignment.MiddleCenter, ForeColor = cor, BackColor = fundo,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 8, 0)
                };
                perfisRelatorio.Controls.Add(selo);
                VisualUtils.Arredondar(selo, 8);
            }
            perfisRelatorio.ResumeLayout(true);
        }

        private void GerarRelatorio(object sender, EventArgs e)
        {

            btnExportarCsv.Enabled = false;
            gradeRelatorio.DataSource = null;
            dadosRelatorio = null;
            try
            {
                ConsultaOperacionalService consulta = new ConsultaOperacionalService();
                dadosRelatorio = consulta.Relatorio(usuarioAutenticado, cmbRelatorioCategoria.Text,
                    dtpRelatorioInicio.Value, dtpRelatorioFim.Value);
                if (cmbRelatorioCategoria.Text == "Estoque crítico")
                {
                    resumoRelatorio.Text = "Produtos sem estoque ou com saldo abaixo do mínimo, por local autorizado. " +
                        "Falta para o mínimo indica a quantidade necessária para atingir o limite cadastrado. " +
                        "Saldo atual: o filtro de datas não se aplica. A consulta não realiza reposições.";
                }
                else if (cmbRelatorioCategoria.Text == "Estoque")
                {
                    resumoRelatorio.Text = "Saldo atual por produto e local. O filtro de datas não se aplica ao saldo atual.";
                }
                else if (cmbRelatorioCategoria.Text == "Apurações")
                {
                    resumoRelatorio.Text = "Conferências preservadas. Diferença atual = informado − vendas atuais da jornada, incluindo correções. " +
                        "Não some conferências sucessivas como receitas. Campos sem dados válidos permanecem vazios.";
                }
                else
                {
                    resumoRelatorio.Text = "Registros autorizados do período selecionado. Valores históricos preservados.";
                }
                gradeRelatorio.DataSource = dadosRelatorio;
                FormatarColunasRelatorio();
                VisualUtils.ExibirEstadoVazio(gradeRelatorio, cmbRelatorioCategoria.Text == "Estoque crítico"
                    ? "Nenhum produto sem estoque ou abaixo do mínimo nos locais autorizados."
                    : "Nenhum registro encontrado para esta consulta.");
                resumoRelatorio.Text += Environment.NewLine + Environment.NewLine +
                    dadosRelatorio.Rows.Count + " linha(s) autorizada(s).";
                btnExportarCsv.Enabled = dadosRelatorio.Rows.Count > 0;
                AtualizarGraficoRelatorio();
            }
            catch (Exception erro)
            {
                AtualizarGraficoRelatorio();
                if (erro is ArgumentException || erro is UnauthorizedAccessException)
                {
                    CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar os dados do relatório.", resumoRelatorio);
                }
                else
                {
                    resumoRelatorio.Text = "Não foi possível consultar os dados do relatório.";
                    System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em RelatoriosControl: " + erro.StackTrace);
                }
            }
        }

        private void FormatarColunasRelatorio()
        {
            bool estoqueCritico = cmbRelatorioCategoria.Text == "Estoque crítico";
            gradeRelatorio.AutoSizeColumnsMode = estoqueCritico
                ? DataGridViewAutoSizeColumnsMode.Fill : DataGridViewAutoSizeColumnsMode.DisplayedCells;
            foreach (DataGridViewColumn coluna in gradeRelatorio.Columns)
            {
                string nome = coluna.Name;
                coluna.Visible = !nome.StartsWith("id_", StringComparison.OrdinalIgnoreCase) && nome != "entidade";
                coluna.HeaderText = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(nome.Replace('_', ' '));
                if (nome == "data_hora")
                {
                    coluna.HeaderText = "Data / hora";
                    coluna.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    coluna.MinimumWidth = 145;
                }
                else if (nome == "preco_unitario_praticado")
                {
                    coluna.HeaderText = "Preço praticado";
                }
                else if (nome == "forma_pagamento")
                {
                    coluna.HeaderText = "Pagamento";
                }
                else if (nome == "responsavel")
                {
                    coluna.HeaderText = "Responsável";
                }
                else if (nome == "descricao")
                {
                    coluna.HeaderText = "Descrição";
                }
                else if (nome == "reposicao_minima")
                {
                    coluna.HeaderText = "Falta para o mínimo";
                    coluna.DefaultCellStyle.Format = "0.###";
                }
                if (nome == "quantidade" || nome == "quantidade_atual" || nome == "quantidade_minima" ||
                    nome == "quantidade_ideal")
                {
                    coluna.DefaultCellStyle.Format = "0.###";
                }
                if (nome == "valor" || nome == "total" || nome == "esperado_atual" || nome.StartsWith("valor_") ||
                    nome.StartsWith("preco_") || nome.StartsWith("diferenca_"))
                {
                    coluna.DefaultCellStyle.Format = "C2";
                }
                if (estoqueCritico)
                {
                    if (nome == "quantidade_ideal") coluna.Visible = false;
                    if (nome == "quantidade_atual") coluna.HeaderText = "Saldo atual";
                    if (nome == "quantidade_minima") coluna.HeaderText = "Mínimo";
                    if (nome == "unidade_medida") coluna.HeaderText = "Unidade";
                    coluna.FillWeight = nome == "local" || nome == "produto" ? 155 :
                        nome == "situacao" ? 125 : nome == "reposicao_minima" ? 90 : 65;
                }
            }
            if (estoqueCritico)
                gradeRelatorio.Columns["reposicao_minima"].DisplayIndex = gradeRelatorio.Columns["situacao"].DisplayIndex;
        }

        private void FormatarValorExibido(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value == null || e.Value == DBNull.Value)
                return;

            string nomeColuna = gradeRelatorio.Columns[e.ColumnIndex].Name;
            if (nomeColuna == "situacao" || nomeColuna == "status" || nomeColuna == "tipo" ||
                nomeColuna == "forma_pagamento")
            {
                e.Value = TextoInterface.FormatarColunaBanco(nomeColuna, e.Value);
                e.FormattingApplied = true;
            }
        }

        private void ExportarCsv(object sender, EventArgs e)
        {
            ExportarFormato("csv");
        }

        private void ExportarPdf(object sender, EventArgs e)
        {
            ExportarFormato("pdf");
        }

        private void ExportarExcel()
        {
            ExportarFormato("xlsx");
        }

        private void MostrarExportacoes(object sender, EventArgs e)
        {
            if (menuExportacoes.Items.Count == 0)
            {
                var csv = new ToolStripMenuItem("CSV compatível com Excel");
                var excel = new ToolStripMenuItem("Excel (.xlsx)");
                var pdf = new ToolStripMenuItem("PDF");
                csv.Click += ExportarCsv;
                excel.Click += (s, args) => ExportarExcel();
                pdf.Click += ExportarPdf;
                menuExportacoes.Items.AddRange(new ToolStripItem[] { csv, excel, pdf });
            }
            menuExportacoes.Show(btnExportarCsv, new System.Drawing.Point(0, btnExportarCsv.Height));
        }

        private void ExportarFormato(string formato)
        {
            using (var dialogo = new SaveFileDialog())
            {
                dialogo.Filter = formato == "xlsx" ? "Planilha Excel (*.xlsx)|*.xlsx" :
                    formato == "pdf" ? "Documento PDF (*.pdf)|*.pdf" : "CSV compatível com Excel (*.csv)|*.csv";
                dialogo.DefaultExt = formato;
                dialogo.AddExtension = true;
                dialogo.FileName = "CASE_relatorio_" + DateTime.Today.ToString("yyyyMMdd") + "." + formato;
                if (dialogo.ShowDialog(this) != DialogResult.OK)
                    return;
                btnExportarCsv.Enabled = btnExportarPdf.Enabled = false;
                UseWaitCursor = true;
                try
                {
                    // Consulta novamente e revalida a sessão depois do diálogo de salvar.
                    new ConsultaOperacionalService().ExportarRelatorio(usuarioAutenticado,
                        cmbRelatorioCategoria.Text, dtpRelatorioInicio.Value,
                        dtpRelatorioFim.Value, dialogo.FileName, formato,
                        chkIncluirGrafico.Checked ? cmbGraficoRelatorio.Text : null);
                    resumoRelatorio.Text = "Relatório exportado para " + formato.ToUpperInvariant() + ".";
                }
                catch (Exception erro)
                {
                    CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível salvar o relatório. Confira a pasta escolhida e tente novamente.", resumoRelatorio);
                    System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ExportarRelatorio");
                }
                finally
                {
                    UseWaitCursor = false;
                    btnExportarPdf.Enabled = usuarioAutenticado != null;
                    btnExportarCsv.Enabled = dadosRelatorio != null && dadosRelatorio.Rows.Count > 0;
                }
            }
        }
        private void AjustarArredondamento(object sender, EventArgs e)
        {
            Control controle = (Control)sender;
            VisualUtils.Arredondar(controle, controle is Panel ? 12 : 8);
            controle.Invalidate();
        }

        private void ConfigurarGraficoRelatorio()
        {
            chkIncluirGrafico.Name = "chkIncluirGrafico";
            chkIncluirGrafico.Text = "Incluir gráfico";
            chkIncluirGrafico.Checked = true;
            chkIncluirGrafico.SetBounds(0, 8, 155, 28);
            cmbGraficoRelatorio.Name = "cmbGraficoRelatorio";
            cmbGraficoRelatorio.AccessibleName = "Tipo de gráfico do relatório";
            cmbGraficoRelatorio.DropDownStyle = ComboBoxStyle.DropDownList;
            var painel = new Panel { Name = "panelTipoGraficoRelatorio", BackColor = VisualUtils.Fundo,
                Padding = new Padding(8, 0, 8, 0) };
            painel.SetBounds(160, 2, 175, 40);
            painel.Controls.Add(cmbGraficoRelatorio);
            VisualUtils.EstilizarSelecao(cmbGraficoRelatorio);
            cmbGraficoRelatorio.FlatStyle = FlatStyle.Flat;
            cmbGraficoRelatorio.BackColor = painel.BackColor;
            VisualUtils.Arredondar(painel, 10);
            VisualUtils.AplicarBorda(painel);
            var orientacao = new Label { Text = "Disponível também no PDF e no Excel. CSV mantém somente os dados.",
                AutoSize = false, ForeColor = VisualUtils.TextoSuave };
            orientacao.SetBounds(355, 12, 530, 26);
            opcoesGrafico.Controls.AddRange(new Control[] { chkIncluirGrafico, painel, orientacao });
            Controls.Add(opcoesGrafico);
            resumoRelatorio.Parent = this;
            resumoRelatorio.Dock = DockStyle.None;
            resumoRelatorio.ForeColor = VisualUtils.TextoSuave;
            resumoRelatorio.Font = new Font("Segoe UI", 9F);
            lblGraficoDireito.Text = "Gráfico do relatório";
            lblGraficoDireito.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            AtualizarTiposGrafico();
            chkIncluirGrafico.CheckedChanged += (s, e) =>
            {
                cmbGraficoRelatorio.Enabled = chkIncluirGrafico.Checked;
                AtualizarGraficoRelatorio();
                AjustarLayoutRelatorio();
            };
            cmbGraficoRelatorio.SelectedIndexChanged += (s, e) => AtualizarGraficoRelatorio();
            SizeChanged += (s, e) => AjustarLayoutRelatorio();
            AjustarLayoutRelatorio();
        }

        private void AtualizarTiposGrafico()
        {
            string anterior = cmbGraficoRelatorio.Text;
            cmbGraficoRelatorio.Items.Clear();
            cmbGraficoRelatorio.Items.AddRange(GraficoRelatorio.TiposDisponiveis(cmbRelatorioCategoria.Text));
            cmbGraficoRelatorio.SelectedItem = cmbGraficoRelatorio.Items.Contains(anterior) ? anterior : "Rosca";
        }

        private void InvalidarConsulta(object sender, EventArgs e)
        {
            dadosRelatorio = null;
            gradeRelatorio.DataSource = null;
            btnExportarCsv.Enabled = false;
            resumoRelatorio.Text = "Filtros alterados. Clique em Gerar para atualizar os dados e o gráfico.";
            AtualizarGraficoRelatorio();
        }

        private void AtualizarGraficoRelatorio()
        {
            if (graficoRelatorio != null)
            {
                graficoRelatorio.Dispose();
                graficoRelatorio = null;
            }
            panelGraficoDireitoArea.Controls.Clear();
            if (!chkIncluirGrafico.Checked || dadosRelatorio == null)
                return;
            graficoRelatorio = GraficoRelatorio.Criar(dadosRelatorio,
                cmbRelatorioCategoria.Text, cmbGraficoRelatorio.Text);
            graficoRelatorio.Name = "graficoRelatorio";
            graficoRelatorio.Dock = DockStyle.Fill;
            panelGraficoDireitoArea.Controls.Add(graficoRelatorio);
        }

        private void AjustarLayoutRelatorio()
        {
            int largura = Math.Max(970, ClientSize.Width) - 60;
            int altura = Math.Max(330, ClientSize.Height - 410);
            opcoesGrafico.SetBounds(30, 250, largura, 44);
            bool incluir = chkIncluirGrafico.Checked;
            int esquerda = incluir ? (largura - 16) * 55 / 100 : largura;
            panelGraficoEsquerdo.SetBounds(30, 308, esquerda, altura);
            panelGraficoDireito.Visible = incluir;
            panelGraficoDireito.SetBounds(30 + esquerda + 16, 308, Math.Max(1, largura - esquerda - 16), altura);
            panelGraficoEsquerdoArea.SetBounds(16, 60, esquerda - 32, altura - 76);
            panelGraficoDireitoArea.SetBounds(16, 60, Math.Max(1, panelGraficoDireito.Width - 32), altura - 76);
            resumoRelatorio.SetBounds(30, panelGraficoEsquerdo.Bottom + 10, largura, 65);
            AutoScrollMinSize = new Size(970, resumoRelatorio.Bottom + 12);
        }
    }
}

