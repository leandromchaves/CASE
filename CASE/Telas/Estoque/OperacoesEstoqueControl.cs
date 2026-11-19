using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class OperacoesEstoqueControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private bool administrador;
        private readonly LocalEstoqueService localEstoqueService = new LocalEstoqueService();
        private readonly EstoqueService estoqueService = new EstoqueService();
        private readonly ProdutoService produtoService = new ProdutoService();
        private readonly Dictionary<int, Produto> produtosPorId = new Dictionary<int, Produto>();
        private TextBox pesquisa;
        private MovimentacoesControl historicoMercadorias;
        private bool exibindoHistorico;

        public event EventHandler FornecedoresSolicitados;
        public event EventHandler SolicitacoesSolicitadas;

        public OperacoesEstoqueControl() : this(null)
        {
        }

        public OperacoesEstoqueControl(Usuario usuario)
        {
            InitializeComponent();
            btnFornecedores.Visible = false;
            btnFornecedores.TabStop = false;
            usuarioAutenticado = usuario;
            lblTitulo.Text = "Estoque";
            lblSubtitulo.Text = "Consulte os saldos ou acompanhe o histórico de mercadorias.";
            btnAbaSaldo.Text = "Saldo disponível";
            btnAbaHistorico.Text = "Histórico de mercadorias";
            btnAbaHistorico.Width = 230;
            lblModoApresentacao.Visible = false;

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            cmbLocal.DisplayMember = "Descricao";
            cmbLocal.SelectedIndexChanged += cmbLocal_SelectedIndexChanged;
            btnEntrada.Click += RegistrarEntrada;
            btnTransferir.Click += TransferirEstoque;
            btnTransferir.CausesValidation = false;
            btnTransferir.BackColor = VisualUtils.Azul;
            btnTransferir.ForeColor = System.Drawing.Color.White;
            btnTransferir.Width = 220;
            dgvSaldos.CellDoubleClick += ConfigurarLimitesEstoque;
            ContextMenuStrip menuSaldos = new ContextMenuStrip();
            ToolStripMenuItem resumoSaldos = new ToolStripMenuItem("Visão geral dos saldos autorizados");
            resumoSaldos.Click += (s, e) =>
            {
                using (Form janela = new Form())
                {
                    janela.Text = "Visão geral do estoque";
                    janela.Size = new System.Drawing.Size(1100, 760);
                    janela.StartPosition = FormStartPosition.CenterParent;
                    janela.Controls.Add(new EstoqueControl(usuarioAutenticado) { Dock = DockStyle.Fill });
                    IconoAplicacao.Aplicar(janela);
                    janela.ShowDialog(this);
                }
            };
            menuSaldos.Items.Add(resumoSaldos);
            dgvSaldos.ContextMenuStrip = menuSaldos;
            Disposed += (s, e) => menuSaldos.Dispose();
            btnFornecedores.Click += (s, e) => FornecedoresSolicitados?.Invoke(this, EventArgs.Empty);
            FrontendTheme.Aplicar(this);
            pesquisa = VisualUtils.AdicionarPesquisa(this, "txtPesquisarLocalEstoque", "Pesquisar neste estoque",
                texto => VisualUtils.FiltrarGrades(this, texto));
            AutoScrollMinSize = new System.Drawing.Size(760, 740);
            panelAcoes.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelSaldos.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelHistorico.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelSaldos.Padding = panelHistorico.Padding = new Padding(24, 20, 24, 20);
            dgvSaldos.Dock = dgvHistorico.Dock = DockStyle.Fill;
            VisualUtils.ExibirEstadoVazio(dgvHistorico, "O histórico consolidado ainda não está disponível.");
            dgvSaldos.Columns[0].FillWeight = 180;
            dgvSaldos.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSaldos.Columns[1].FillWeight = 90;
            dgvSaldos.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSaldos.Columns[2].FillWeight = 70;
            dgvSaldos.Columns[3].FillWeight = 100;
            dgvSaldos.Columns[4].FillWeight = 55;
            dgvHistorico.Columns[4].FillWeight = 180;
            VisualUtils.AplicarIcone(btnEntrada, Properties.Resources.LucideAcaoEntrada);
            VisualUtils.AplicarIcone(btnTransferir, Properties.Resources.LucideAcaoTransferencia);
            VisualUtils.AplicarIcone(btnFornecedores, Properties.Resources.LucideAcaoFornecedores);
            btnAbaSaldo.Click += (s, e) => SelecionarAba(false);
            btnAbaHistorico.Click += (s, e) => SelecionarAba(true);
            SizeChanged += (s, e) => Organizar();
            Organizar();
            SelecionarAba(false);

            if (usuarioAutenticado == null)
            {
                lblMensagem.Text = "Autentique-se para consultar os estoques.";
                btnEntrada.Enabled = false;
                btnTransferir.Enabled = false;
                return;
            }

            CarregarLocais();
            try
            {
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                administrador = perfil == "Administrador";
                btnEntrada.Enabled = perfil == "Administrador" && cmbLocal.Items.Count > 0;
                btnTransferir.Enabled = (perfil == "Administrador" || perfil == "Gerente") && cmbLocal.Items.Count > 0;
                btnFornecedores.Enabled = perfil == "Administrador" || perfil == "Gerente";
                AtualizarAcaoTransferencia();
            }
            catch (Exception)
            {
                btnEntrada.Enabled = false;
                btnTransferir.Enabled = false;
                btnFornecedores.Enabled = false;
            }
        }

        private void ConfigurarLimitesEstoque(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || usuarioAutenticado == null)
            {
                return;
            }
            Estoque estoque = dgvSaldos.Rows[e.RowIndex].Tag as Estoque;
            if (estoque == null)
            {
                return;
            }
            try
            {
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
                if (perfil == "Membro" || (local != null && local.EhCentral && perfil != "Administrador"))
                {
                    lblMensagem.Text = "Este local está disponível somente para consulta no seu perfil.";
                    return;
                }
            }
            catch (Exception)
            {
                lblMensagem.Text = "Sua sessão não permite configurar este estoque.";
                return;
            }
            using (Form janela = new Form())
            using (NumericUpDown minima = new NumericUpDown())
            using (NumericUpDown ideal = new NumericUpDown())
            using (Button salvar = new Button())
            {
                IconoAplicacao.Aplicar(janela);
                janela.Text = "Mínimo e ideal do produto neste local";
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.ClientSize = new System.Drawing.Size(430, 200);
                janela.FormBorderStyle = FormBorderStyle.FixedDialog;
                janela.MaximizeBox = false;
                minima.Maximum = 9999999;
                ideal.Maximum = 9999999;
                minima.Value = estoque.QuantidadeMinima;
                ideal.Value = estoque.QuantidadeIdeal;
                minima.SetBounds(200, 30, 200, 28);
                ideal.SetBounds(200, 80, 200, 28);
                salvar.Text = "Salvar limites";
                salvar.SetBounds(220, 140, 180, 36);
                janela.Controls.Add(FrontendTheme.Legenda("Quantidade mínima", new System.Drawing.Rectangle(20, 32, 180, 24)));
                janela.Controls.Add(FrontendTheme.Legenda("Quantidade ideal", new System.Drawing.Rectangle(20, 82, 180, 24)));
                janela.Controls.Add(minima);
                janela.Controls.Add(ideal);
                janela.Controls.Add(salvar);
                salvar.Click += (s, argumentos) =>
                {
                    try
                    {
                        estoqueService.ConfigurarQuantidadesAlvo(usuarioAutenticado,
                            estoque.IdEstoque, minima.Value, ideal.Value);
                        janela.DialogResult = DialogResult.OK;
                        janela.Close();
                    }
                    catch (Exception erro)
                    {
                        string mensagem = "Não foi possível salvar os limites.";
                        if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
                        {
                            mensagem = CASE.Utils.FeedbackUsuario.MensagemErro(erro, mensagem);
                        }
                        MessageBox.Show(janela, mensagem, "Estoque", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                FrontendTheme.Aplicar(janela);
                if (janela.ShowDialog(this) == DialogResult.OK)
                {
                    Atualizar();
                    lblMensagem.Text = "Limites salvos. O saldo atual não foi alterado.";
                }
            }
        }

        private void CarregarLocais()
        {
            try
            {
                List<LocalEstoque> locais = localEstoqueService.Listar(usuarioAutenticado);
                cmbLocal.Items.Clear();

                foreach (LocalEstoque local in locais)
                {
                    cmbLocal.Items.Add(local);
                }

                if (cmbLocal.Items.Count > 0)
                {
                    cmbLocal.SelectedIndex = 0;
                    lblMensagem.Text = string.Empty;
                }
                else
                {
                    btnEntrada.Enabled = false;
                    btnTransferir.Enabled = false;
                    lblMensagem.Text = "Não há locais de estoque disponíveis para esta conta.";
                }
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível carregar os locais de estoque.";
                MessageBox.Show(
                    this,
                    "Não foi possível consultar os locais autorizados.",
                    "Estoque",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void RegistrarEntrada(object sender, EventArgs e)
        {

            using (var formulario = new EntradaMercadoriaForm(usuarioAutenticado))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    Atualizar();
                    lblMensagem.Text = "Entrada registrada. O saldo foi atualizado pelo sistema.";
                    CASE.Utils.FeedbackUsuario.Exibir(this, "Entrada registrada com sucesso. O saldo foi atualizado.", CASE.Utils.TipoFeedback.Sucesso);
                }
            }
        }

        private void TransferirEstoque(object sender, EventArgs e)
        {
            try
            {
                new AutorizacaoService().ExigirAdministradorOuGerente(usuarioAutenticado);
                LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
                if (local == null)
                {
                    lblMensagem.Text = "Selecione o local de estoque antes de continuar.";
                    return;
                }
                if (local.EhCentral)
                {
                    if (administrador)
                    {
                        using (var formulario = new TransferenciaEstoqueForm(
                            usuarioAutenticado,
                            local.IdLocalEstoque,
                            true))
                        {
                            if (formulario.ShowDialog(this) == DialogResult.OK)
                            {
                                Atualizar();
                                lblMensagem.Text = "Transferência para o estoque geral registrada.";
                                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Sucesso);
                            }
                        }
                        return;
                    }

                    if (SolicitacoesSolicitadas != null)
                        SolicitacoesSolicitadas(this, EventArgs.Empty);
                    else
                        lblMensagem.Text = "Abra Solicitações para aprovar e atender os pedidos de liberação ou devolução do central.";
                    return;
                }
                using (var formulario = new TransferenciaEstoqueForm(usuarioAutenticado, local.IdLocalEstoque))
                {
                    if (formulario.ShowDialog(this) == DialogResult.OK)
                    {
                        Atualizar();
                        lblMensagem.Text = "Transferência registrada. Os saldos foram atualizados pelo sistema.";
                        CASE.Utils.FeedbackUsuario.Exibir(this, "Transferência registrada com sucesso. Os saldos foram atualizados.", CASE.Utils.TipoFeedback.Sucesso);
                    }
                }
            }
            catch (Exception erro)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível abrir a transferência. Confira os dados e tente novamente.", lblMensagem);
            }
        }

        private void AtualizarAcaoTransferencia()
        {
            LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
            btnTransferir.Text = local != null && local.EhCentral
                ? (administrador ? "Transferir para evento" : "Solicitações do central")
                : "Transferir entre estoques";
            btnTransferir.AccessibleDescription = local != null && local.EhCentral
                ? (administrador
                    ? "Transfere mercadorias do estoque central diretamente para o estoque geral de um evento."
                    : "Abre os pedidos de liberação e devolução do estoque central. O atendimento exige autorização.")
                : "Abre a transferência entre locais do mesmo evento, com a origem selecionada.";
        }

        private void Organizar()
        {
            int altura = Math.Max(740, ClientSize.Height);
            int largura = Math.Max(760, ClientSize.Width);
            int deslocamentoX = AutoScrollPosition.X;
            int deslocamentoY = AutoScrollPosition.Y;
            bool compacto = largura < 1050;
            int alturaAcoes = 96;
            if (compacto)
            {
                alturaAcoes = 136;
                btnEntrada.SetBounds(24, 84, btnEntrada.Width, 40);
                btnTransferir.SetBounds(216, 84, btnTransferir.Width, 40);
                btnFornecedores.SetBounds(408, 84, btnFornecedores.Width, 40);
            }
            else
            {
                btnEntrada.SetBounds(378, 40, btnEntrada.Width, 40);
                btnTransferir.SetBounds(570, 40, btnTransferir.Width, 40);
                btnFornecedores.SetBounds(762, 40, btnFornecedores.Width, 40);
            }
            panelAcoes.SetBounds(24 + deslocamentoX, 112 + deslocamentoY, largura - 48, alturaAcoes);
            // As ações e o local selecionado permanecem visíveis nas duas abas.
            int topoAbas = 112 + alturaAcoes + 16;
            btnAbaSaldo.Top = btnAbaHistorico.Top = topoAbas + deslocamentoY;
            panelSaldos.Left = panelHistorico.Left = 24 + deslocamentoX;
            panelSaldos.Width = panelHistorico.Width = largura - 48;
            panelSaldos.Top = panelHistorico.Top = topoAbas + 56 + deslocamentoY;
            panelSaldos.Height = panelHistorico.Height = altura - topoAbas - 110;
            lblMensagem.Top = altura - 40 + deslocamentoY;
        }

        private void SelecionarAba(bool historico)
        {
            exibindoHistorico = historico;
            panelAcoes.Visible = true;
            lblResumo.Visible = true;
            if (pesquisa != null && pesquisa.Parent != this)
                pesquisa.Parent.Visible = true;
            panelSaldos.Visible = !historico;
            panelHistorico.Visible = historico;
            if (historico)
            {
                if (historicoMercadorias == null)
                {
                    historicoMercadorias = new MovimentacoesControl(usuarioAutenticado) { Dock = DockStyle.Fill };
                    historicoMercadorias.IncorporarAoEstoque();
                    historicoMercadorias.OperacaoCorrigida += (s, e) => Atualizar();
                    dgvHistorico.Visible = false;
                    panelHistorico.Controls.Add(historicoMercadorias);
                    historicoMercadorias.BringToFront();
                }
                else
                    historicoMercadorias.AtualizarHistorico();
                btnAbaSaldo.BackColor = System.Drawing.Color.White;
                btnAbaSaldo.ForeColor = VisualUtils.Texto;
                btnAbaHistorico.BackColor = VisualUtils.Azul;
                btnAbaHistorico.ForeColor = System.Drawing.Color.White;
                btnAbaSaldo.FlatAppearance.MouseOverBackColor = VisualUtils.Fundo;
                btnAbaHistorico.FlatAppearance.MouseOverBackColor = VisualUtils.Azul;
                btnAbaSaldo.AccessibleDescription = "Mostrar saldo";
                btnAbaHistorico.AccessibleDescription = "Histórico selecionado";
            }
            else
            {
                btnAbaSaldo.BackColor = VisualUtils.Azul;
                btnAbaSaldo.ForeColor = System.Drawing.Color.White;
                btnAbaHistorico.BackColor = System.Drawing.Color.White;
                btnAbaHistorico.ForeColor = VisualUtils.Texto;
                btnAbaSaldo.FlatAppearance.MouseOverBackColor = VisualUtils.Azul;
                btnAbaHistorico.FlatAppearance.MouseOverBackColor = VisualUtils.Fundo;
                btnAbaSaldo.AccessibleDescription = "Saldo selecionado";
                btnAbaHistorico.AccessibleDescription = "Mostrar histórico";
            }
            Organizar();
        }

        private void cmbLocal_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarAcaoTransferencia();
            Atualizar();
        }

        private void Atualizar()
        {

            dgvSaldos.Rows.Clear();
            dgvHistorico.Rows.Clear();
            produtosPorId.Clear();

            LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;

            if (local == null || usuarioAutenticado == null)
            {
                return;
            }

            try
            {
                System.Data.DataTable produtos = new ConsultaOperacionalService().Estoques(usuarioAutenticado);
                foreach (System.Data.DataRow linhaProduto in produtos.Rows)
                {
                    Produto produto = new Produto();
                    produto.IdProduto = Convert.ToInt32(linhaProduto["id_produto"]);
                    produto.Nome = Convert.ToString(linhaProduto["produto"]);
                    produto.UnidadeMedida = Convert.ToString(linhaProduto["unidade_medida"]);
                    produtosPorId[produto.IdProduto] = produto;
                }

                List<Estoque> saldos = estoqueService.ListarPorLocal(
                    usuarioAutenticado,
                    local.IdLocalEstoque);
                int produtosComSaldo = 0;
                int estoquesBaixos = 0;
                int semEstoque = 0;

                foreach (Estoque estoque in saldos)
                {
                    Produto produto;
                    string nomeProduto = "Produto " + estoque.IdProduto;
                    string unidade = string.Empty;

                    if (produtosPorId.TryGetValue(estoque.IdProduto, out produto))
                    {
                        nomeProduto = produto.Nome;
                        unidade = produto.UnidadeMedida;
                    }

                    string situacao = estoque.QuantidadeAtual <= 0
                        ? "Sem estoque"
                        : estoque.QuantidadeAtual <= estoque.QuantidadeMinima
                            ? "Estoque baixo"
                            : "Normal";
                    int linha = dgvSaldos.Rows.Add(
                        nomeProduto,
                        estoque.QuantidadeAtual.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")),
                        estoque.QuantidadeMinima.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")),
                        situacao,
                        unidade);

                    dgvSaldos.Rows[linha].Tag = estoque;

                    if (estoque.QuantidadeAtual > 0)
                    {
                        produtosComSaldo++;
                    }
                    if (situacao == "Estoque baixo")
                    {
                        estoquesBaixos++;
                    }
                    else if (situacao == "Sem estoque")
                    {
                        semEstoque++;
                    }

                    dgvSaldos.Rows[linha].Cells[3].Style.ForeColor = situacao == "Normal"
                        ? System.Drawing.Color.FromArgb(5, 150, 105)
                        : System.Drawing.Color.FromArgb(185, 28, 28);
                }

                dgvSaldos.ClearSelection();
                System.Data.DataTable movimentos = new ConsultaOperacionalService().Movimentacoes(
                    usuarioAutenticado, DateTime.Today.AddDays(-30), DateTime.Today);
                foreach (System.Data.DataRow movimento in movimentos.Rows)
                {
                    int origem = Convert.ToInt32(movimento["id_origem"]);
                    int destino = Convert.ToInt32(movimento["id_destino"]);
                    if (origem != local.IdLocalEstoque && destino != local.IdLocalEstoque)
                    {
                        continue;
                    }
                    decimal quantidade = Convert.ToDecimal(movimento["quantidade"]);
                    if (origem == local.IdLocalEstoque)
                    {
                        quantidade = -quantidade;
                    }
                    dgvHistorico.Rows.Add(movimento["data_hora"], movimento["produto"],
                        movimento["tipo"], quantidade,
                        movimento["origem"] + " → " + movimento["destino"] + " / " + movimento["responsavel"]);
                }
                VisualUtils.ExibirEstadoVazio(dgvHistorico, "Nenhuma movimentação neste local nos últimos 30 dias.");
                lblResumo.Text = produtosComSaldo + " com saldo · " +
                    estoquesBaixos + " abaixo do mínimo · " + semEstoque + " sem estoque";
                VisualUtils.FiltrarGrades(this, pesquisa.Text.Trim());
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Sua conta não tem acesso a este local.";
                dgvSaldos.Rows.Clear();
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível carregar os saldos deste local.";
                dgvSaldos.Rows.Clear();
            }
        }
    }
}
