using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class OperacoesEstoqueControl : UserControl
    {
        private Usuario usuarioAutenticado;
        private Evento eventoContexto;
        private bool filtrarPorEvento;
        private bool administrador;
        private bool gerente;
        private LocalEstoqueService localEstoqueService = new LocalEstoqueService();
        private EstoqueService estoqueService = new EstoqueService();
        private ProdutoService produtoService = new ProdutoService();
        private Dictionary<int, Produto> produtosPorId = new Dictionary<int, Produto>();
        private Button btnConfigurarLimites = new Button();
        private TextBox pesquisa;
        private MovimentacoesControl historicoMercadorias;
        private bool exibindoHistorico;
        public event EventHandler FornecedoresSolicitados;
        public event EventHandler SolicitacoesSolicitadas;
        public OperacoesEstoqueControl() : this(null, null, false)
        {
        }

        public OperacoesEstoqueControl(Usuario usuario) : this(usuario, null, false)
        {
        }

        public OperacoesEstoqueControl(Usuario usuario, Evento evento, bool filtrarEvento)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            btnFornecedores.Visible = false;
            btnFornecedores.TabStop = false;
            usuarioAutenticado = usuario;
            eventoContexto = evento;
            filtrarPorEvento = filtrarEvento;
            lblTitulo.Text = "Estoque";
            lblSubtitulo.Text = "Consulte saldos e situações. Selecione um produto para ajustar o mínimo e o ideal.";
            btnAbaSaldo.Text = "Saldo disponível";
            btnAbaHistorico.Text = "Histórico de mercadorias";
            btnAbaHistorico.Width = 230;
            lblModoApresentacao.Visible = false;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            cmbLocal.DisplayMember = "Descricao";
            cmbLocal.SelectedIndexChanged += cmbLocal_SelectedIndexChanged;
            btnEntrada.Click += RegistrarEntrada;
            btnTransferir.Click += TransferirEstoque;
            btnTransferir.CausesValidation = false;
            btnTransferir.BackColor = VisualUtils.Azul;
            btnTransferir.ForeColor = Color.White;
            btnTransferir.Width = 220;
            btnConfigurarLimites.Name = "btnConfigurarLimitesEstoque";
            btnConfigurarLimites.Text = "Definir mínimo e ideal";
            btnConfigurarLimites.Size = new Size(190, 40);
            btnConfigurarLimites.Visible = false;
            btnConfigurarLimites.CausesValidation = false;
            btnConfigurarLimites.Enabled = false;
            btnConfigurarLimites.Click += btnConfigurarLimites_Click;
            panelAcoes.Controls.Add(btnConfigurarLimites);
            dgvSaldos.CellDoubleClick += ConfigurarLimitesEstoque;
            dgvSaldos.SelectionChanged += dgvSaldos_SelectionChanged;
            ContextMenuStrip menuSaldos = new ContextMenuStrip();
            ToolStripMenuItem resumoSaldos = new ToolStripMenuItem("Visão geral dos saldos autorizados");
            resumoSaldos.Click += resumoSaldos_Click;
            menuSaldos.Items.Add(resumoSaldos);
            dgvSaldos.ContextMenuStrip = menuSaldos;
            Disposed += Tratar_Disposed;
            btnFornecedores.Click += btnFornecedores_Click;
            FrontendTheme.Aplicar(this);
            pesquisa = VisualUtils.AdicionarPesquisa(this, "txtPesquisarLocalEstoque", "Pesquisar neste estoque", Preparar_AdicionarPesquisa);
            AutoScrollMinSize = new Size(760, 740);
            panelAcoes.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelSaldos.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelHistorico.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelSaldos.Padding = panelHistorico.Padding = new Padding(24, 20, 24, 20);
            dgvSaldos.Dock = dgvHistorico.Dock = DockStyle.Fill;
            VisualUtils.ExibirEstadoVazio(dgvSaldos, "Nenhum produto disponível neste local. Os produtos aparecem após o recebimento de mercadorias.");
            VisualUtils.ExibirEstadoVazio(dgvHistorico, "O histórico consolidado ainda não está disponível.");
            dgvSaldos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvSaldos.Columns[0].FillWeight = 160;
            dgvSaldos.Columns[0].MinimumWidth = 200;
            dgvSaldos.Columns[1].HeaderText = "Disponível";
            dgvSaldos.Columns[1].FillWeight = 90;
            dgvSaldos.Columns[2].HeaderText = "Estoque mínimo";
            dgvSaldos.Columns[2].FillWeight = 90;
            dgvSaldos.Columns[3].FillWeight = 110;
            dgvSaldos.Columns[4].FillWeight = 50;
            foreach (int indice in new[]
            {
                1,
                2,
                4
            }

            )
            {
                DataGridViewColumn coluna = dgvSaldos.Columns[indice];
                coluna.MinimumWidth = indice == 4 ? 75 : 120;
                coluna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                coluna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                coluna.DefaultCellStyle.Padding = Padding.Empty;
                coluna.HeaderCell.Style.Padding = Padding.Empty;
            }

            dgvSaldos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistorico.Columns[4].FillWeight = 180;
            VisualUtils.AplicarIcone(btnEntrada, Properties.Resources.IconeAcaoEntrada);
            VisualUtils.AplicarIcone(btnTransferir, Properties.Resources.IconeAcaoTransferencia);
            VisualUtils.EstilizarBotao(btnConfigurarLimites);
            VisualUtils.Arredondar(btnConfigurarLimites, 8);
            VisualUtils.AplicarIcone(btnFornecedores, Properties.Resources.IconeAcaoFornecedores);
            btnAbaSaldo.Click += btnAbaSaldo_Click;
            btnAbaHistorico.Click += btnAbaHistorico_Click;
            SizeChanged += Tela_SizeChanged;
            Organizar();
            SelecionarAba(false);
            if (usuarioAutenticado == null)
            {
                lblMensagem.Text = "Autentique-se para consultar os estoques.";
                btnEntrada.Enabled = false;
                btnTransferir.Enabled = false;
                btnConfigurarLimites.Visible = false;
                return;
            }

            CarregarLocais();
            try
            {
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                administrador = perfil == "Administrador";
                gerente = perfil == "Gerente";
                btnConfigurarLimites.Visible = administrador || gerente;
                btnEntrada.Enabled = perfil == "Administrador" && cmbLocal.Items.Count > 0;
                btnTransferir.Enabled = (perfil == "Administrador" || perfil == "Gerente") && cmbLocal.Items.Count > 0;
                btnFornecedores.Enabled = perfil == "Administrador" || perfil == "Gerente";
                AtualizarAcaoTransferencia();
                AtualizarAcaoLimites();
            }
            catch (Exception)
            {
                btnEntrada.Enabled = false;
                btnTransferir.Enabled = false;
                btnFornecedores.Enabled = false;
                btnConfigurarLimites.Visible = false;
            }

            void Tratar_Disposed(object s, EventArgs e)
            {
                menuSaldos.Dispose();
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
                janela.ClientSize = new Size(430, 200);
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
                janela.Controls.Add(FrontendTheme.Legenda("Quantidade mínima", new Rectangle(20, 32, 180, 24)));
                janela.Controls.Add(FrontendTheme.Legenda("Quantidade ideal", new Rectangle(20, 82, 180, 24)));
                janela.Controls.Add(minima);
                janela.Controls.Add(ideal);
                janela.Controls.Add(salvar);
                salvar.Click += Tratar_salvar_Click;
                FrontendTheme.Aplicar(janela);
                if (janela.ShowDialog(this) == DialogResult.OK)
                {
                    Atualizar();
                    lblMensagem.Text = "Limites salvos. O saldo atual não foi alterado.";
                }

                void Tratar_salvar_Click(object s, EventArgs argumentos)
                {
                    try
                    {
                        estoqueService.ConfigurarQuantidadesAlvo(usuarioAutenticado, estoque.IdEstoque, minima.Value, ideal.Value);
                        janela.DialogResult = DialogResult.OK;
                        janela.Close();
                    }
                    catch (Exception erro)
                    {
                        string mensagem = "Não foi possível salvar os limites.";
                        if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
                        {
                            mensagem = FeedbackUsuario.MensagemErro(erro, mensagem);
                        }

                        MessageBox.Show(janela, mensagem, "Estoque", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        internal void AtualizarConsulta()
        {
            CarregarLocais();
        }

        private void CarregarLocais()
        {
            try
            {
                List<LocalEstoque> locais = localEstoqueService.ListarOperacionais(usuarioAutenticado);
                LocalEstoque anterior = cmbLocal.SelectedItem as LocalEstoque;
                int indiceSelecionado = -1;
                cmbLocal.Items.Clear();
                foreach (LocalEstoque local in locais)
                {
                    if (filtrarPorEvento && !local.EhCentral && (!eventoContexto.IdEvento.Equals(local.IdEvento) || local.IdInstituicao != eventoContexto.IdInstituicao))
                    {
                        continue;
                    }

                    int indice = cmbLocal.Items.Add(local);
                    if (anterior != null && anterior.IdLocalEstoque == local.IdLocalEstoque)
                        indiceSelecionado = indice;
                }

                if (cmbLocal.Items.Count > 0)
                {
                    cmbLocal.SelectedIndex = indiceSelecionado >= 0 ? indiceSelecionado : 0;
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
                MessageBox.Show(this, "Não foi possível consultar os locais autorizados.", "Estoque", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    FeedbackUsuario.Exibir(this, "Entrada registrada com sucesso. O saldo foi atualizado.", TipoFeedback.Sucesso);
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
                        using (var formulario = new TransferenciaEstoqueForm(usuarioAutenticado, local.IdLocalEstoque, true, filtrarPorEvento ? (int? )eventoContexto.IdEvento : null))
                        {
                            if (formulario.ShowDialog(this) == DialogResult.OK)
                            {
                                Atualizar();
                                lblMensagem.Text = "Transferência para o estoque geral registrada.";
                                FeedbackUsuario.Exibir(this, lblMensagem.Text, TipoFeedback.Sucesso);
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

                using (var formulario = new TransferenciaEstoqueForm(usuarioAutenticado, local.IdLocalEstoque, false, filtrarPorEvento ? (int? )eventoContexto.IdEvento : null))
                {
                    if (formulario.ShowDialog(this) == DialogResult.OK)
                    {
                        Atualizar();
                        lblMensagem.Text = "Transferência registrada. Os saldos foram atualizados pelo sistema.";
                        FeedbackUsuario.Exibir(this, "Transferência registrada com sucesso. Os saldos foram atualizados.", TipoFeedback.Sucesso);
                    }
                }
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível abrir a transferência. Confira os dados e tente novamente.", lblMensagem);
            }
        }

        private void AtualizarAcaoTransferencia()
        {
            LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
            btnTransferir.Text = local != null && local.EhCentral ? (administrador ? "Transferir para evento" : "Solicitações do central") : "Transferir entre estoques";
            btnTransferir.AccessibleDescription = local != null && local.EhCentral ? (administrador ? "Transfere mercadorias do estoque central diretamente para o estoque geral de um evento." : "Abre os pedidos de liberação e devolução do estoque central. O atendimento exige autorização.") : "Abre a transferência entre locais do mesmo evento, com a origem selecionada.";
        }

        private void AtualizarAcaoLimites()
        {
            LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
            bool localPermitido = local != null && (!local.EhCentral || administrador);
            btnConfigurarLimites.Enabled = (administrador || gerente) && localPermitido && dgvSaldos.SelectedRows.Count > 0;
            btnConfigurarLimites.AccessibleDescription = "Define o estoque mínimo e ideal do produto selecionado neste local. " + "O saldo é considerado baixo quando fica igual ou abaixo do mínimo.";
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
                btnConfigurarLimites.SetBounds(450, 84, btnConfigurarLimites.Width, 40);
            }
            else
            {
                btnEntrada.SetBounds(378, 40, btnEntrada.Width, 40);
                btnTransferir.SetBounds(570, 40, btnTransferir.Width, 40);
                btnFornecedores.SetBounds(762, 40, btnFornecedores.Width, 40);
                btnConfigurarLimites.SetBounds(800, 40, btnConfigurarLimites.Width, 40);
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
                    historicoMercadorias = new MovimentacoesControl(usuarioAutenticado, eventoContexto, filtrarPorEvento)
                    {
                        Dock = DockStyle.Fill
                    };
                    historicoMercadorias.IncorporarAoEstoque();
                    historicoMercadorias.OperacaoCorrigida += historicoMercadorias_OperacaoCorrigida;
                    dgvHistorico.Visible = false;
                    panelHistorico.Controls.Add(historicoMercadorias);
                    historicoMercadorias.BringToFront();
                }
                else
                    historicoMercadorias.AtualizarHistorico();
                historicoMercadorias.DefinirLocalConsultado(cmbLocal.SelectedItem as LocalEstoque);
                historicoMercadorias.DefinirPesquisaExterna(pesquisa.Text);
                btnAbaSaldo.BackColor = Color.White;
                btnAbaSaldo.ForeColor = VisualUtils.Texto;
                btnAbaHistorico.BackColor = VisualUtils.Azul;
                btnAbaHistorico.ForeColor = Color.White;
                btnAbaSaldo.FlatAppearance.MouseOverBackColor = VisualUtils.Fundo;
                btnAbaHistorico.FlatAppearance.MouseOverBackColor = VisualUtils.Azul;
                btnAbaSaldo.AccessibleDescription = "Mostrar saldo";
                btnAbaHistorico.AccessibleDescription = "Histórico selecionado";
            }
            else
            {
                btnAbaSaldo.BackColor = VisualUtils.Azul;
                btnAbaSaldo.ForeColor = Color.White;
                btnAbaHistorico.BackColor = Color.White;
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
            AtualizarAcaoLimites();
        }

        private void Atualizar()
        {
            AtualizarAcaoLimites();
            LocalEstoque local = cmbLocal.SelectedItem as LocalEstoque;
            if (historicoMercadorias != null)
                historicoMercadorias.DefinirLocalConsultado(local);
            if (local == null || usuarioAutenticado == null)
            {
                dgvSaldos.Rows.Clear();
                dgvHistorico.Rows.Clear();
                produtosPorId.Clear();
                return;
            }

            try
            {
                DataTable produtos = new ConsultaOperacionalService().Estoques(usuarioAutenticado);
                Dictionary<int, Produto> produtosCarregados = new Dictionary<int, Produto>();
                foreach (DataRow linhaProduto in produtos.Rows)
                {
                    Produto produto = new Produto();
                    produto.IdProduto = Convert.ToInt32(linhaProduto["id_produto"]);
                    produto.Nome = Convert.ToString(linhaProduto["produto"]);
                    produto.UnidadeMedida = Convert.ToString(linhaProduto["unidade_medida"]);
                    produtosCarregados[produto.IdProduto] = produto;
                }

                List<Estoque> saldos = estoqueService.ListarPorLocal(usuarioAutenticado, local.IdLocalEstoque, somenteDisponiveis: !local.EhCentral);
                DataTable movimentos = new ConsultaOperacionalService().Movimentacoes(usuarioAutenticado, DateTime.Today.AddDays(-30), DateTime.Today);
                List<object[]> linhasSaldo = new List<object[]>();
                List<Estoque> saldosDasLinhas = new List<Estoque>();
                List<object[]> linhasHistorico = new List<object[]>();
                int produtosComSaldo = 0;
                int estoquesBaixos = 0;
                int semEstoque = 0;
                foreach (Estoque estoque in saldos)
                {
                    Produto produto;
                    string nomeProduto = "Produto " + estoque.IdProduto;
                    string unidade = string.Empty;
                    if (produtosCarregados.TryGetValue(estoque.IdProduto, out produto))
                    {
                        nomeProduto = produto.Nome;
                        unidade = produto.UnidadeMedida;
                    }

                    string situacao = estoque.QuantidadeAtual <= 0 ? "Sem estoque" : estoque.QuantidadeAtual <= estoque.QuantidadeMinima ? "Estoque baixo" : "Normal";
                    linhasSaldo.Add(new object[] { nomeProduto, estoque.QuantidadeAtual.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")), estoque.QuantidadeMinima.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")), situacao, unidade });
                    saldosDasLinhas.Add(estoque);
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
                }

                foreach (DataRow movimento in movimentos.Rows)
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

                    linhasHistorico.Add(new object[] { movimento["data_hora"], movimento["produto"], TextoInterface.FormatarValorBanco(movimento["tipo"]), quantidade, movimento["origem"] + " → " + movimento["destino"] + " / " + movimento["responsavel"] });
                }

                dgvSaldos.Rows.Clear();
                dgvHistorico.Rows.Clear();
                produtosPorId.Clear();
                foreach (KeyValuePair<int, Produto> par in produtosCarregados)
                    produtosPorId[par.Key] = par.Value;
                for (int i = 0; i < linhasSaldo.Count; i++)
                {
                    int linha = dgvSaldos.Rows.Add(linhasSaldo[i]);
                    dgvSaldos.Rows[linha].Tag = saldosDasLinhas[i];
                    string situacao = Convert.ToString(linhasSaldo[i][3]);
                    dgvSaldos.Rows[linha].Cells[3].Style.ForeColor = situacao == "Normal" ? Color.FromArgb(5, 150, 105) : Color.FromArgb(185, 28, 28);
                }

                foreach (object[] linha in linhasHistorico)
                    dgvHistorico.Rows.Add(linha);
                dgvSaldos.ClearSelection();
                VisualUtils.ExibirEstadoVazio(dgvHistorico, "Nenhuma movimentação neste local nos últimos 30 dias.");
                lblResumo.Text = produtosComSaldo + " com saldo neste local · " + estoquesBaixos + " abaixo do mínimo" + (local.EhCentral ? " · " + semEstoque + " sem estoque" : string.Empty);
                if (exibindoHistorico && historicoMercadorias != null)
                    historicoMercadorias.AtualizarHistorico();
                AplicarPesquisa(pesquisa.Text.Trim());
                AtualizarAcaoLimites();
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Sua conta não tem acesso a este local.";
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível carregar os saldos deste local.";
            }
        }

        private void AplicarPesquisa(string texto)
        {
            VisualUtils.FiltrarGrades(panelSaldos, texto);
            if (historicoMercadorias != null)
                historicoMercadorias.DefinirPesquisaExterna(texto);
        }

        private void btnConfigurarLimites_Click(object s, EventArgs e)
        {
            if (dgvSaldos.SelectedRows.Count == 0)
            {
                lblMensagem.Text = "Selecione um produto para definir o mínimo e o ideal.";
                return;
            }

            ConfigurarLimitesEstoque(this, new DataGridViewCellEventArgs(0, dgvSaldos.SelectedRows[0].Index));
        }

        private void dgvSaldos_SelectionChanged(object s, EventArgs e)
        {
            AtualizarAcaoLimites();
        }

        private void resumoSaldos_Click(object s, EventArgs e)
        {
            using (Form janela = new Form())
            {
                janela.Text = "Visão geral do estoque";
                janela.Size = new Size(1100, 760);
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.Controls.Add(new EstoqueControl(usuarioAutenticado) { Dock = DockStyle.Fill });
                IconoAplicacao.Aplicar(janela);
                janela.ShowDialog(this);
            }
        }

        private void btnFornecedores_Click(object s, EventArgs e)
        {
            FornecedoresSolicitados?.Invoke(this, EventArgs.Empty);
        }

        private void Preparar_AdicionarPesquisa(string texto)
        {
            AplicarPesquisa(texto);
        }

        private void btnAbaSaldo_Click(object s, EventArgs e)
        {
            SelecionarAba(false);
        }

        private void btnAbaHistorico_Click(object s, EventArgs e)
        {
            SelecionarAba(true);
        }

        private void Tela_SizeChanged(object s, EventArgs e)
        {
            Organizar();
        }

        private void historicoMercadorias_OperacaoCorrigida(object s, EventArgs e)
        {
            Atualizar();
        }
    }
}
