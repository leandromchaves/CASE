using System;
using System.Windows.Forms;
using CASE.Utils;
using System.Data;
using System.IO;
using System.Text;
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
                "Movimentações", "Estoque", "Vendas e perdas", "Solicitações de reposição" });
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
                bool utilizaPeriodo = cmbRelatorioCategoria.Text != "Estoque";
                dtpRelatorioInicio.Enabled = utilizaPeriodo;
                dtpRelatorioFim.Enabled = utilizaPeriodo;
                dadosRelatorio = null;
                gradeRelatorio.DataSource = null;
                btnExportarCsv.Enabled = false;
                resumoRelatorio.Text = "Clique em Gerar para consultar o relatório selecionado.";
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
                if (cmbRelatorioCategoria.Text == "Estoque")
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
                resumoRelatorio.Text += Environment.NewLine + Environment.NewLine +
                    dadosRelatorio.Rows.Count + " linha(s) autorizada(s).";
                btnExportarCsv.Enabled = dadosRelatorio.Rows.Count > 0;
            }
            catch (Exception erro)
            {
                if (erro is ArgumentException || erro is UnauthorizedAccessException)
                {
                    resumoRelatorio.Text = erro.Message;
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
                if (nome == "quantidade" || nome == "quantidade_atual" || nome == "quantidade_minima" ||
                    nome == "quantidade_ideal")
                {
                    coluna.DefaultCellStyle.Format = "N0";
                }
                if (nome == "valor" || nome == "total" || nome == "esperado_atual" || nome.StartsWith("valor_") ||
                    nome.StartsWith("preco_") || nome.StartsWith("diferenca_"))
                {
                    coluna.DefaultCellStyle.Format = "C2";
                }
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
                        dtpRelatorioFim.Value, dialogo.FileName, formato);
                    resumoRelatorio.Text = "Relatório exportado para " + formato.ToUpperInvariant() + ".";
                }
                catch (Exception erro)
                {
                    resumoRelatorio.Text = erro is UnauthorizedAccessException || erro is ArgumentException
                        ? erro.Message : "Não foi possível salvar o relatório. Verifique a pasta e se o arquivo está aberto.";
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
    }
}

