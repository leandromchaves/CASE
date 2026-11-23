using System;
using System.Collections.Generic;
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
        private readonly Evento eventoContexto;
        private readonly bool filtrarPorEvento;
        private readonly Dictionary<int, LocalEstoque> locaisOperacionaisPorId =
            new Dictionary<int, LocalEstoque>();
        private bool incorporado;
        private LocalEstoque localConsultado;
        private string pesquisaExterna = string.Empty;
        private readonly ComboBox filtroLocal = new ComboBox();
        private Panel painelPesquisaProduto;
        public event EventHandler OperacaoCorrigida;

        public MovimentacoesControl() : this(null, null, false)
        {
        }

        public MovimentacoesControl(Usuario usuario) : this(usuario, null, false)
        {
        }

        public MovimentacoesControl(Usuario usuario, Evento evento, bool filtrarEvento)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            eventoContexto = evento;
            filtrarPorEvento = filtrarEvento;
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
            lblTituloMovimentacoes.Text = "Histórico de mercadorias";
            lblSubtituloMovimentacoes.Text = "Veja o que entrou, saiu ou mudou de local. Valores em dinheiro ficam no Financeiro.";
            Button abrirCorrecao = new Button();
            abrirCorrecao.Name = "btnCorrigirMovimentacao";
            abrirCorrecao.Text = "Revisar operação";
            abrirCorrecao.SetBounds(Width - 250, 25, 220, 36);
            abrirCorrecao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            abrirCorrecao.Enabled = false;
            abrirCorrecao.Click += CorrigirMovimentacao;
            VisualUtils.EstilizarBotao(abrirCorrecao);
            Controls.Add(abrirCorrecao);
            abrirCorrecao.BringToFront();
            dgvMovimentacoes.SelectionChanged += (s, e) =>
                abrirCorrecao.Enabled = usuarioAutenticado != null && dgvMovimentacoes.SelectedRows.Count > 0 &&
                    dgvMovimentacoes.SelectedRows[0].Visible;
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
            cmbTipoMovimentacao.Items.AddRange(new object[] { "Todos", "Entrada", "Transferência", "Venda", "Perda" });
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
                Label legenda = FrontendTheme.Legenda(titulo,
                    new System.Drawing.Rectangle(campo.Left, 7, campo.Width, 20));
                legenda.Name = "lblFiltro_" + campo.Name;
                panelFiltrosMovimentacoes.Controls.Add(legenda);
            }
            btnNovaMovimentacao.Click += (s, e) =>
            {
                CarregarMovimentacoes();
                dgvMovimentacoes.ClearSelection();
            };
            txtProdutoMovimentacao.TextChanged += (s, e) => FiltrarProduto();
            painelPesquisaProduto = VisualUtils.EstilizarPesquisa(txtProdutoMovimentacao, "Pesquisar produto");
            cmbTipoMovimentacao.SelectedIndexChanged += (s, e) => FiltrarProduto();
            dgvMovimentacoes.Columns[0].MinimumWidth = 145;
            dgvMovimentacoes.Columns[0].HeaderText = "Data / hora";
            dgvMovimentacoes.Columns[1].MinimumWidth = 180;
            dgvMovimentacoes.Columns[3].DefaultCellStyle.Format = "0.###";
            dgvMovimentacoes.Columns[5].MinimumWidth = 240;
            filtroLocal.Name = "cmbLocalHistorico";
            filtroLocal.AccessibleName = "Filtrar histórico por local de estoque";
            filtroLocal.DropDownStyle = ComboBoxStyle.DropDownList;
            filtroLocal.DisplayMember = "Descricao";
            filtroLocal.Items.Add("Todos os locais");
            filtroLocal.SelectedIndex = 0;
            panelListaMovimentacoes.Controls.Add(filtroLocal);
            VisualUtils.EstilizarSelecao(filtroLocal);
            try
            {
                if (usuarioAutenticado != null)
                {
                    List<LocalEstoque> locais = new LocalEstoqueService().ListarOperacionais(usuarioAutenticado);
                    foreach (LocalEstoque local in locais)
                    {
                        if (filtrarPorEvento && !local.EhCentral &&
                            (!eventoContexto.IdEvento.Equals(local.IdEvento) ||
                             local.IdInstituicao != eventoContexto.IdInstituicao))
                        {
                            continue;
                        }
                        filtroLocal.Items.Add(local);
                    }
                }
            }
            catch (Exception)
            {
                filtroLocal.Enabled = false;
            }
            filtroLocal.SelectedIndexChanged += (s, e) => FiltrarProduto();
            dgvMovimentacoes.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 2)
                {
                    string valor = Convert.ToString(e.Value);
                    e.Value = valor == "transferencia" ? "Transferência" : valor == "entrada" ? "Entrada" :
                        valor == "venda" ? "Venda" : valor == "perda" ? "Perda" : valor;
                }
            };
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem corrigir = new ToolStripMenuItem("Revisar operação e histórico de correções");
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
            SizeChanged += (s, e) => AjustarLayoutHistorico();
            AjustarLayoutHistorico();
            CarregarMovimentacoes();
        }

        public void IncorporarAoEstoque()
        {
            incorporado = true;
            filtroLocal.Visible = false;
            lblTituloMovimentacoes.Visible = lblSubtituloMovimentacoes.Visible = pictureIconeTela.Visible = false;
            AjustarLayoutHistorico();
        }

        public void AtualizarHistorico()
        {
            CarregarMovimentacoes();
        }

        public void DefinirLocalConsultado(LocalEstoque local)
        {
            localConsultado = local;
            FiltrarProduto();
        }

        public void DefinirPesquisaExterna(string texto)
        {
            pesquisaExterna = (texto ?? string.Empty).Trim();
            FiltrarProduto();
        }

        private void AjustarLayoutHistorico()
        {
            int margem = incorporado ? 0 : 24;
            int topo = incorporado ? 0 : 112;
            int largura = Math.Max(920, ClientSize.Width - margem * 2);
            panelFiltrosMovimentacoes.SetBounds(margem, topo, largura, 90);
            panelListaMovimentacoes.SetBounds(margem, topo + 104, largura,
                Math.Max(260, ClientSize.Height - topo - 120));
            dgvMovimentacoes.SetBounds(16, 60, panelListaMovimentacoes.Width - 32, panelListaMovimentacoes.Height - 76);
            int larguraInterior = largura - 40;
            int larguraPesquisa = Math.Min(280, Math.Max(190, (int)(larguraInterior * 0.28)));
            int larguraTipo = 140;
            int larguraData = 140;
            int intervalo = 14;
            int x = 20;
            PosicionarLegenda("lblFiltro_txtProdutoMovimentacao", x, 7, larguraPesquisa);
            painelPesquisaProduto.SetBounds(x, 27, larguraPesquisa, 40);
            x += larguraPesquisa + intervalo;
            PosicionarLegenda("lblFiltro_cmbTipoMovimentacao", x, 7, larguraTipo);
            cmbTipoMovimentacao.SetBounds(x, 30, larguraTipo, 30);
            x += larguraTipo + intervalo;
            PosicionarLegenda("lblFiltro_dtpDataInicial", x, 7, larguraData);
            dtpDataInicial.SetBounds(x, 30, larguraData, 30);
            x += larguraData + intervalo;
            PosicionarLegenda("lblFiltro_dtpDataFinal", x, 7, larguraData);
            dtpDataFinal.SetBounds(x, 30, larguraData, 30);
            int larguraBotao = 160;
            btnNovaMovimentacao.SetBounds(largura - larguraBotao - 20, 25, larguraBotao, 40);
            filtroLocal.SetBounds(largura - 480, 16, 230, 28);
            lblListaMovimentacoes.Width = Math.Max(260, largura - 520);
            Control revisar = Controls["btnCorrigirMovimentacao"];
            if (revisar != null)
            {
                revisar.Parent = panelListaMovimentacoes;
                revisar.SetBounds(largura - 235, 12, 220, 36);
            }
            AutoScrollMinSize = new System.Drawing.Size(incorporado ? 920 : 970, panelListaMovimentacoes.Bottom + 16);
        }

        private void PosicionarLegenda(string nome, int esquerda, int topo, int largura)
        {
            Control legenda = panelFiltrosMovimentacoes.Controls[nome];
            if (legenda != null)
                legenda.SetBounds(esquerda, topo, largura, 20);
        }

        private void CarregarMovimentacoes()
        {

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
                locaisOperacionaisPorId.Clear();
                foreach (LocalEstoque localOperacional in new LocalEstoqueService().ListarOperacionais(usuarioAutenticado))
                    locaisOperacionaisPorId[localOperacional.IdLocalEstoque] = localOperacional;
                dgvMovimentacoes.Rows.Clear();
                foreach (DataRow movimento in movimentos.Rows)
                {
                    int indice = dgvMovimentacoes.Rows.Add(
Convert.ToDateTime(movimento["data_hora"]).ToString("dd/MM/yyyy HH:mm"),
                        movimento["produto"],
CASE.Utils.TextoInterface.FormatarValorBanco(movimento["tipo"]),
movimento["quantidade"],
movimento["responsavel"],
                        string.IsNullOrWhiteSpace(Convert.ToString(movimento["origem"])) ? "Recebimento em " + movimento["destino"] :
                        string.IsNullOrWhiteSpace(Convert.ToString(movimento["destino"])) ? "Saída de " + movimento["origem"] :
                        movimento["origem"] + " → " + movimento["destino"]);
                    dgvMovimentacoes.Rows[indice].Tag = movimento;
                }
                dgvMovimentacoesColMotivo.Visible = true;
                dgvMovimentacoesColMotivo.HeaderText = "Origem → destino";
                lblListaMovimentacoes.Text = movimentos.Rows.Count + " movimentação(ões) autorizada(s) no período";
                FiltrarProduto();
                VisualUtils.ExibirEstadoVazio(dgvMovimentacoes, "Nenhuma movimentação encontrada com estes filtros.");
            }
            catch (Exception erro)
            {
                lblListaMovimentacoes.Text = "Não foi possível consultar o histórico.";
                if (erro is ArgumentException || erro is UnauthorizedAccessException)
                {
                    CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar o histórico.", lblListaMovimentacoes);
                }
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em MovimentacoesControl: " + erro.StackTrace);
            }
        }

        private void CorrigirMovimentacao(object sender, EventArgs e)
        {
            if (dgvMovimentacoes.SelectedRows.Count == 0 || !dgvMovimentacoes.SelectedRows[0].Visible || usuarioAutenticado == null)
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
                    OperacaoCorrigida?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void FiltrarProduto()
        {
            string[] tipos = { "", "entrada", "transferencia", "venda", "perda" };
            LocalEstoque local = incorporado ? localConsultado : filtroLocal.SelectedItem as LocalEstoque;
            int visiveis = 0;
            foreach (DataGridViewRow linha in dgvMovimentacoes.Rows)
            {
                DataRow operacao = linha.Tag as DataRow;
                linha.Visible = Convert.ToString(linha.Cells[1].Value).IndexOf(
                    txtProdutoMovimentacao.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    Convert.ToString(linha.Cells[1].Value).IndexOf(pesquisaExterna,
                        StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    (!incorporado || local != null) &&
                    (cmbTipoMovimentacao.SelectedIndex <= 0 || operacao != null &&
                        Convert.ToString(operacao["tipo"]) == tipos[cmbTipoMovimentacao.SelectedIndex]) &&
                    MovimentoDoContexto(operacao) &&
                    (local == null || operacao != null && (LerIdLocal(operacao, "id_origem") == local.IdLocalEstoque ||
                        LerIdLocal(operacao, "id_destino") == local.IdLocalEstoque));
                if (!linha.Visible)
                    linha.Selected = false;
                else
                    visiveis++;
            }
            lblListaMovimentacoes.Text = visiveis + " movimentação(ões)";
        }

        private bool MovimentoDoContexto(DataRow operacao)
        {
            if (operacao == null)
                return false;

            int origem = LerIdLocal(operacao, "id_origem");
            int destino = LerIdLocal(operacao, "id_destino");
            if ((origem > 0 && !locaisOperacionaisPorId.ContainsKey(origem)) ||
                (destino > 0 && !locaisOperacionaisPorId.ContainsKey(destino)))
            {
                return false;
            }

            if (!filtrarPorEvento || eventoContexto == null)
                return true;

            string tipo = Convert.ToString(operacao["tipo"]);
            if (string.Equals(tipo, "transferencia", StringComparison.OrdinalIgnoreCase))
                return EhDoEvento(origem) || EhDoEvento(destino);
            if (string.Equals(tipo, "entrada", StringComparison.OrdinalIgnoreCase))
                return EhDoEvento(destino);
            return EhDoEvento(origem);
        }

        private bool EhDoEvento(int idLocal)
        {
            LocalEstoque local;
            return idLocal > 0 && locaisOperacionaisPorId.TryGetValue(idLocal, out local) &&
                local.IdEvento == eventoContexto.IdEvento;
        }

        private static int LerIdLocal(DataRow operacao, string coluna)
        {
            return operacao[coluna] == DBNull.Value ? 0 : Convert.ToInt32(operacao[coluna]);
        }

        private void AjustarArredondamento(object sender, EventArgs e)
        {
            Control controle = (Control)sender;
            VisualUtils.Arredondar(controle, controle is Panel ? 12 : 8);
            controle.Invalidate();
        }
    }
}

