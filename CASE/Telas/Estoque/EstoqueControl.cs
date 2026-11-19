using System;
using System.Windows.Forms;
using CASE.Utils;
using CASE.Models;
using CASE.Services;
using System.Data;

namespace CASE
{
    public partial class EstoqueControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private string filtroSelecionado = "Todos";
        private Panel areaPesquisa;

        public EstoqueControl() : this(null)
        {
        }

        public EstoqueControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            cmbCategoriaEstoque.Items.Clear();
            cmbStatusEstoque.Items.Clear();
            CASE.Utils.FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelCardEstoqueTotal, 12);
            panelCardEstoqueTotal.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardEstoqueTotal);
            VisualUtils.Arredondar(panelCardEstoqueBaixo, 12);
            panelCardEstoqueBaixo.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardEstoqueBaixo);
            VisualUtils.Arredondar(panelCardSemEstoque, 12);
            panelCardSemEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardSemEstoque);
            VisualUtils.Arredondar(panelCardValorEstoque, 12);
            panelCardValorEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardValorEstoque);
            VisualUtils.Arredondar(panelFiltrosEstoque, 12);
            panelFiltrosEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelFiltrosEstoque);
            VisualUtils.Arredondar(btnAtualizarEstoque, 8);
            btnAtualizarEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.Arredondar(panelListaEstoque, 12);
            panelListaEstoque.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelListaEstoque);
            panelCardEstoqueTotal.Visible = panelCardEstoqueBaixo.Visible = false;
            panelCardSemEstoque.Visible = panelCardValorEstoque.Visible = false;
            panelFiltrosEstoque.Top = 115;
            panelListaEstoque.Top = 210;
            panelListaEstoque.Height = 380;
            dgvEstoque.Height = 300;
            lblSubtituloEstoque.Text = "Controle o estoque dos produtos";
            txtPesquisarEstoque.Visible = cmbCategoriaEstoque.Visible = cmbStatusEstoque.Visible = false;
            btnAtualizarEstoque.Visible = false;
            dgvEstoqueColCodigo.Visible = dgvEstoqueColIdeal.Visible = false;
            string[] filtros = { "Todos", "Em estoque", "Estoque baixo", "Sem estoque" };
            var botoes = new System.Collections.Generic.List<Button>();
            for (int i = 0; i < filtros.Length; i++)
            {
                string filtro = filtros[i];
                var botao = new Button
                {
                    Name = "btnFiltroEstoque" + i,
                    Text = filtro,
                    Bounds = new System.Drawing.Rectangle(16 + i * 150, 18, 140, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = System.Drawing.Color.White,
                    ForeColor = FrontendTheme.Texto
                };
                botao.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(221, 230, 240);
                botoes.Add(botao);
                panelFiltrosEstoque.Controls.Add(botao);
                botao.Click += (s, e) =>
                {
                    foreach (var item in botoes)
                    {
                        item.BackColor = item == botao ? System.Drawing.Color.FromArgb(226, 238, 255) : System.Drawing.Color.White;
                        item.ForeColor = item == botao ? FrontendTheme.Azul : FrontendTheme.Texto;
                    }
                    filtroSelecionado = filtro;
                    AplicarFiltros();
                };
            }
            FrontendTheme.Aplicar(panelFiltrosEstoque);
            botoes[0].PerformClick();
            txtPesquisarEstoque.Visible = true;
            txtPesquisarEstoque.SetBounds(635, 22, 220, txtPesquisarEstoque.PreferredHeight);
            VisualUtils.DefinirPlaceholder(txtPesquisarEstoque, "Produto ou local");
            txtPesquisarEstoque.TextChanged += (s, e) => AplicarFiltros();
            areaPesquisa = VisualUtils.EstilizarPesquisa(txtPesquisarEstoque, "Pesquisar produto ou local");
            btnAtualizarEstoque.Visible = true;
            btnAtualizarEstoque.SetBounds(870, 18, 100, 36);
            btnAtualizarEstoque.Click += AtualizarSaldos;
            dgvEstoqueColCategoria.HeaderText = "Local";
            dgvEstoqueColIdeal.Visible = true;
            panelFiltrosEstoque.MinimumSize = System.Drawing.Size.Empty;
            panelListaEstoque.MinimumSize = System.Drawing.Size.Empty;
            panelFiltrosEstoque.Anchor = panelListaEstoque.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnAtualizarEstoque.Anchor = areaPesquisa.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            SizeChanged += (s, e) => AjustarLayoutEstoque();
            AjustarLayoutEstoque();
            AtualizarSaldos(this, EventArgs.Empty);
        }

        private void AjustarLayoutEstoque()
        {
            int largura = Math.Max(680, ClientSize.Width) - 60;
            bool duasLinhas = largura < 960;
            panelFiltrosEstoque.SetBounds(30, 115, largura, duasLinhas ? 124 : 75);
            int larguraFiltro = Math.Min(140, (largura - 62) / 4);
            for (int i = 0; i < 4; i++)
                panelFiltrosEstoque.Controls["btnFiltroEstoque" + i].SetBounds(
                    16 + i * (larguraFiltro + 10), 18, larguraFiltro, 36);
            int topo = duasLinhas ? 68 : 18;
            btnAtualizarEstoque.SetBounds(largura - 116, topo, 100, 40);
            int esquerda = duasLinhas ? 16 : 620;
            areaPesquisa.SetBounds(esquerda, topo, btnAtualizarEstoque.Left - esquerda - 12, 40);
            int topoLista = panelFiltrosEstoque.Bottom + 20;
            panelListaEstoque.SetBounds(30, topoLista, largura, Math.Max(300, ClientSize.Height - topoLista - 25));
            lblListaEstoque.Width = largura - 40;
            dgvEstoque.SetBounds(20, 60, largura - 40, panelListaEstoque.Height - 80);
            lblSubtituloEstoque.Width = largura - 50;
            AutoScrollMinSize = new System.Drawing.Size(680, panelListaEstoque.Bottom + 25);
        }

        private void AtualizarSaldos(object sender, EventArgs e)
        {
            dgvEstoque.Rows.Clear();
            if (usuarioAutenticado == null)
            {
                lblListaEstoque.Text = "Autentique-se para consultar os saldos.";
                btnAtualizarEstoque.Enabled = false;
                return;
            }
            try
            {
                DataTable saldos = new ConsultaOperacionalService().Estoques(usuarioAutenticado);
                foreach (DataRow saldo in saldos.Rows)
                {
                    decimal atual = Convert.ToDecimal(saldo["quantidade_atual"]);
                    decimal minimo = Convert.ToDecimal(saldo["quantidade_minima"]);
                    string situacao = "Em estoque";
                    if (atual == 0)
                    {
                        situacao = "Sem estoque";
                    }
                    else if (atual <= minimo)
                    {
                        situacao = "Estoque baixo";
                    }
                    dgvEstoque.Rows.Add(saldo["id_produto"], saldo["produto"], saldo["local"],
                        atual, minimo, saldo["quantidade_ideal"], situacao);
                }
                AplicarFiltros();
                lblListaEstoque.Text = saldos.Rows.Count + " saldo(s) autorizado(s) por produto e local";
                VisualUtils.ExibirEstadoVazio(dgvEstoque, "Nenhum saldo autorizado encontrado.");
            }
            catch (Exception erro)
            {
                lblListaEstoque.Text = "Não foi possível consultar os saldos autorizados.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ResumoEstoque: " + erro.StackTrace);
            }
        }

        private void AplicarFiltros()
        {
            dgvEstoque.CurrentCell = null;
            foreach (DataGridViewRow linha in dgvEstoque.Rows)
            {
                string descricao = Convert.ToString(linha.Cells[dgvEstoqueColProduto.Index].Value) + " " +
                    Convert.ToString(linha.Cells[dgvEstoqueColCategoria.Index].Value);
                linha.Visible = descricao.IndexOf(txtPesquisarEstoque.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    (filtroSelecionado == "Todos" ||
                    Convert.ToString(linha.Cells[dgvEstoqueColStatus.Index].Value) == filtroSelecionado);
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

