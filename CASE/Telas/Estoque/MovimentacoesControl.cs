using System;
using System.Windows.Forms;
using CASE.Utils;
using CASE.Models;
using CASE.Services;
using System.Data;

namespace CASE
{
    public partial class MovimentacoesControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;

        public MovimentacoesControl() : this(null)
        {
        }

        public MovimentacoesControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            CASE.Utils.FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelCardEntradasMes, 12);
            panelCardEntradasMes.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardEntradasMes);
            VisualUtils.Arredondar(panelCardSaidasMes, 12);
            panelCardSaidasMes.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelCardSaidasMes);
            VisualUtils.Arredondar(panelFiltrosMovimentacoes, 12);
            panelFiltrosMovimentacoes.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelFiltrosMovimentacoes);
            VisualUtils.Arredondar(btnNovaMovimentacao, 8);
            btnNovaMovimentacao.SizeChanged += AjustarArredondamento;
            VisualUtils.Arredondar(panelListaMovimentacoes, 12);
            panelListaMovimentacoes.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelListaMovimentacoes);
            lblSubtituloMovimentacoes.Text = "Selecione uma linha e use Corrigir / histórico. O botão direito também está disponível.";
            Button abrirCorrecao = new Button();
            abrirCorrecao.Name = "btnCorrigirMovimentacao";
            abrirCorrecao.Text = "Corrigir / histórico";
            abrirCorrecao.SetBounds(Width - 250, 25, 220, 36);
            abrirCorrecao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            abrirCorrecao.Enabled = false;
            abrirCorrecao.Click += CorrigirMovimentacao;
            VisualUtils.EstilizarBotao(abrirCorrecao);
            Controls.Add(abrirCorrecao);
            abrirCorrecao.BringToFront();
            dgvMovimentacoes.SelectionChanged += (s, e) =>
                abrirCorrecao.Enabled = usuarioAutenticado != null && dgvMovimentacoes.SelectedRows.Count > 0;
            panelCardEntradasMes.Visible = panelCardSaidasMes.Visible = false;
            panelFiltrosMovimentacoes.Top = 115;
            panelListaMovimentacoes.Top = 225;
            panelListaMovimentacoes.Height = 340;
            dgvMovimentacoes.Height = 260;
            btnNovaMovimentacao.Text = "Filtrar";
            dgvMovimentacoesColMotivo.Visible = false;
            dtpDataInicial.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpDataFinal.Value = DateTime.Today;
            cmbTipoMovimentacao.Items.Clear();
            cmbTipoMovimentacao.Items.AddRange(new object[] { "Todos", "entrada", "transferencia", "venda", "perda" });
            cmbTipoMovimentacao.SelectedIndex = 0;
            foreach (Control campo in new Control[] { cmbTipoMovimentacao, txtProdutoMovimentacao, dtpDataInicial, dtpDataFinal })
            {
                string titulo = campo == cmbTipoMovimentacao
                    ? "Tipo"
                    : campo == txtProdutoMovimentacao
                        ? "Produto"
                        : campo == dtpDataInicial
                            ? "Data inicial"
                            : "Data final";
                panelFiltrosMovimentacoes.Controls.Add(FrontendTheme.Legenda(titulo,
                    new System.Drawing.Rectangle(campo.Left, 7, campo.Width, 20)));
            }
            btnNovaMovimentacao.Click += (s, e) =>
            {
                CarregarMovimentacoes();
                dgvMovimentacoes.ClearSelection();
            };
            txtProdutoMovimentacao.TextChanged += (s, e) => FiltrarProduto();
            VisualUtils.EstilizarPesquisa(txtProdutoMovimentacao, "Pesquisar produto");
            cmbTipoMovimentacao.SelectedIndexChanged += (s, e) => FiltrarProduto();
            dgvMovimentacoes.Columns[0].MinimumWidth = 145;
            dgvMovimentacoes.Columns[0].HeaderText = "Data / hora";
            dgvMovimentacoes.Columns[1].MinimumWidth = 180;
            dgvMovimentacoes.Columns[3].DefaultCellStyle.Format = "N0";
            dgvMovimentacoes.Columns[5].MinimumWidth = 240;
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem corrigir = new ToolStripMenuItem("Corrigir operação / consultar histórico");
            corrigir.Name = "menuCorrigirMovimentacao";
            corrigir.Click += CorrigirMovimentacao;
            menu.Items.Add(corrigir);
            dgvMovimentacoes.ContextMenuStrip = menu;
            dgvMovimentacoes.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvMovimentacoes.CurrentCell = dgvMovimentacoes.Rows[e.RowIndex].Cells[0];
                    dgvMovimentacoes.Rows[e.RowIndex].Selected = true;
                }
            };
            Disposed += (s, e) => menu.Dispose();
            CarregarMovimentacoes();
        }

        private void CarregarMovimentacoes()
        {

            dgvMovimentacoes.Rows.Clear();
            if (usuarioAutenticado == null)
            {
                lblListaMovimentacoes.Text = "Autentique-se para consultar o histórico.";
                btnNovaMovimentacao.Enabled = false;
                return;
            }
            try
            {
                DataTable movimentos = new ConsultaOperacionalService().Movimentacoes(
                    usuarioAutenticado, dtpDataInicial.Value, dtpDataFinal.Value);
                foreach (DataRow movimento in movimentos.Rows)
                {
                    int indice = dgvMovimentacoes.Rows.Add(
Convert.ToDateTime(movimento["data_hora"]).ToString("dd/MM/yyyy HH:mm"),
                        movimento["produto"],
movimento["tipo"],
movimento["quantidade"],
movimento["responsavel"],
                        movimento["origem"] + " → " + movimento["destino"]);
                    dgvMovimentacoes.Rows[indice].Tag = movimento;
                }
                dgvMovimentacoesColMotivo.Visible = true;
                dgvMovimentacoesColMotivo.HeaderText = "Origem → destino";
                lblListaMovimentacoes.Text = movimentos.Rows.Count + " movimentação(ões) autorizada(s) no período";
                FiltrarProduto();
            }
            catch (Exception erro)
            {
                lblListaMovimentacoes.Text = "Não foi possível consultar o histórico.";
                if (erro is ArgumentException || erro is UnauthorizedAccessException)
                {
                    lblListaMovimentacoes.Text = erro.Message;
                }
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em MovimentacoesControl: " + erro.StackTrace);
            }
        }

        private void CorrigirMovimentacao(object sender, EventArgs e)
        {
            if (dgvMovimentacoes.SelectedRows.Count == 0 || usuarioAutenticado == null)
            {
                lblListaMovimentacoes.Text = "Selecione uma operação autorizada.";
                return;
            }
            DataRow operacao = dgvMovimentacoes.SelectedRows[0].Tag as DataRow;
            if (operacao == null)
            {
                return;
            }
            using (CorrecaoOperacaoForm formulario = new CorrecaoOperacaoForm(usuarioAutenticado,
                Convert.ToString(operacao["entidade"]), Convert.ToInt32(operacao["id_operacao"])))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarMovimentacoes();
                    lblListaMovimentacoes.Text = "Correção confirmada. Saldos e histórico atualizados.";
                }
            }
        }

        private void FiltrarProduto()
        {
            foreach (DataGridViewRow linha in dgvMovimentacoes.Rows)
            {
                linha.Visible = Convert.ToString(linha.Cells[1].Value).IndexOf(
                    txtProdutoMovimentacao.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    (cmbTipoMovimentacao.SelectedIndex <= 0 || Convert.ToString(linha.Cells[2].Value) == cmbTipoMovimentacao.Text);
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

