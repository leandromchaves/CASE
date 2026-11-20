using System;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

using System.Windows.Forms;
using System.Collections.Generic;
using CASE.Utils;
using CASE.Quermesse;
using CASE.Models;

namespace CASE
{
    public partial class PrincipalForm : Form
    {
        private readonly Usuario usuarioAutenticado;
        private Evento eventoSelecionado;
        private Button menuAtual;
        private bool ajustandoMenu;
        private Button buttonFornecedores;
        private Button buttonAcessos;
        private readonly ComboBox cmbEventoContexto = new ComboBox();
        private readonly Label lblEventoContexto = new Label();
        private bool sincronizandoContextoEvento;
        private readonly ToolTip dicaNotificacoes = new ToolTip();
        private readonly ToolTip dicaNomeUsuario = new ToolTip();
        private readonly Label indicadorNotificacoes = new Label();
        private readonly ContextMenuStrip menuFotoPerfil = new ContextMenuStrip();
        private readonly ContextMenuStrip menuNotificacoes = new ContextMenuStrip();
        private readonly CASE.Services.FotoPerfilService fotoPerfilService =
            new CASE.Services.FotoPerfilService();
        private Image fotoPerfilPersonalizada;


        public PrincipalForm() : this(null)
        {
        }

        internal PrincipalForm(Usuario usuario)
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;
            IconoAplicacao.Aplicar(this);

            if (usuario == null)
            {
                throw new InvalidOperationException("É necessário autenticar uma conta autorizada.");
            }

            CASE.Services.SessaoUsuario.ExigirUsuario(usuario);

            if (!usuario.IdPerfilSistema.HasValue)
            {
                throw new InvalidOperationException("A conta não possui um perfil de acesso.");
            }

            if (!string.Equals(
                usuario.SituacaoConta,
                "Aprovado",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A conta ainda não foi autorizada.");
            }

            usuarioAutenticado = usuario;
            ConfigurarFotoPerfil();
            ConfigurarNovosCadastros();
            InicializarEventoSelecionado();
            ConfigurarSeletorEventoContexto();
            navegacao.Layout += (s, e) => AjustarLarguraMenu();
            navegacao.ClientSizeChanged += (s, e) => AjustarLarguraMenu();
            AjustarLarguraMenu();
            AplicarAcabamento();
            ConfigurarNotificacoes();

            VisualUtils.Arredondar(buttonDashboard, 8);
            VisualUtils.Arredondar(buttonEstoque, 8);
            VisualUtils.Arredondar(buttonRelatorios, 8);
            VisualUtils.Arredondar(buttonProdutos, 8);
            VisualUtils.Arredondar(buttonMovimentacoes, 8);

            VisualUtils.Arredondar(buttonConfiguracoes, 8);
            VisualUtils.Arredondar(buttonSair, 8);

            panelPesquisa.Visible = false;
            textPesquisa.TabStop = false;
            Rectangle areaDisponivel = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(1100, areaDisponivel.Width), Math.Min(680, areaDisponivel.Height));
            Size = new Size(Math.Min(1320, areaDisponivel.Width), Math.Min(850, areaDisponivel.Height));
            buttonDashboard.Click += (s, e) => AbrirTela(buttonDashboard, () => new DashboardControl(usuarioAutenticado));
            buttonProdutos.Click += (s, e) => AbrirTela(buttonProdutos, () => new ProdutosControl(usuarioAutenticado));
            buttonEstoque.Click += (s, e) => AbrirTela(buttonEstoque, CriarOperacoesEstoque);
            buttonMovimentacoes.Click += (s, e) => AbrirTela(
                buttonMovimentacoes,
                () => new RegistrosControl(usuarioAutenticado, eventoSelecionado));
            buttonRelatorios.Text = "Relatórios";
            buttonRelatorios.Click += (s, e) => AbrirTela(buttonRelatorios, () => new RelatoriosControl(usuarioAutenticado));
            buttonConfiguracoes.Click += (s, e) => AbrirTela(buttonConfiguracoes, CriarConfiguracoes);
            buttonSair.Click += (s, e) => Close();
            ConfigurarQuermesse();
        }

        private void ConfigurarQuermesse()
        {
            buttonEquipeSetores.Click += (s, e) => AbrirTela(
                buttonEquipeSetores,
                () => new EquipeSetoresControl(usuarioAutenticado, eventoSelecionado));
            buttonFinanceiro.Click += (s, e) => AbrirTela(buttonFinanceiro, () => new FinanceiroControl(usuarioAutenticado, eventoSelecionado));
            buttonQuermesses.Click += (s, e) => AbrirTela(buttonQuermesses, CriarTelaEventos);
            buttonEscala.Click += (s, e) => AbrirTela(
                buttonEscala,
                () => new EscalaControl(usuarioAutenticado, eventoSelecionado));
            buttonSolicitacoes.Click += (s, e) => AbrirTela(
                buttonSolicitacoes,
                CriarSolicitacoes);
            foreach (var botao in new[] { buttonEquipeSetores, buttonFinanceiro, buttonQuermesses, buttonEscala, buttonSolicitacoes })
            {
                VisualUtils.Arredondar(botao, 8);
                botao.SizeChanged += (s, e) => VisualUtils.Arredondar(botao, 8);
            }
            usuarioPreview.Visible = false;
            labelPreviewUsuario.Visible = false;
            AtualizarCabecalho();
            AbrirTela(buttonDashboard, () => new DashboardControl(usuarioAutenticado));
        }

        private UserControl CriarTelaEventos()
        {
            QuermessesControl tela = new QuermessesControl(usuarioAutenticado);
            tela.EventoSelecionado += AtualizarEventoSelecionado;
            AtualizarEventoSelecionado(tela.ObterEventoSelecionado());
            return tela;
        }

        private UserControl CriarSolicitacoes()
        {
            SolicitacoesControl tela = new SolicitacoesControl(usuarioAutenticado, eventoSelecionado);
            tela.AnalisarPedidoCentral += DecidirSolicitacaoCentral;
            return tela;
        }

        private void AtualizarEventoSelecionado(Evento evento)
        {
            int? idAnterior = eventoSelecionado == null ? (int?)null : eventoSelecionado.IdEvento;
            eventoSelecionado = evento;
            int? idNovo = evento == null ? (int?)null : evento.IdEvento;
            if (idAnterior == idNovo)
                return;
            SincronizarSeletorEventoContexto();
        }

        private void ConfigurarSeletorEventoContexto()
        {
            lblEventoContexto.Name = "lblEventoContexto";
            lblEventoContexto.Text = "Evento em consulta";
            lblEventoContexto.SetBounds(18, 5, 280, 18);
            lblEventoContexto.Font = new Font("Segoe UI", 8.5F);
            lblEventoContexto.ForeColor = VisualUtils.TextoSuave;
            cmbEventoContexto.Name = "cmbEventoContexto";
            cmbEventoContexto.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEventoContexto.DisplayMember = "Nome";
            cmbEventoContexto.SetBounds(16, 25, 360, 34);
            cmbEventoContexto.Font = new Font("Segoe UI", 9.5F);
            cmbEventoContexto.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            VisualUtils.EstilizarSelecao(cmbEventoContexto);
            cmbEventoContexto.SelectedIndexChanged += cmbEventoContexto_SelectedIndexChanged;
            panelCabecalho.Controls.Add(cmbEventoContexto);
            panelCabecalho.Controls.Add(lblEventoContexto);
            SincronizarSeletorEventoContexto();
            cmbEventoContexto.Visible = false;
            lblEventoContexto.Visible = false;
        }

        private void SincronizarSeletorEventoContexto()
        {
            sincronizandoContextoEvento = true;
            try
            {
                List<Evento> eventos = new CASE.Services.EventoService().Listar(usuarioAutenticado);
                cmbEventoContexto.BeginUpdate();
                cmbEventoContexto.Items.Clear();
                foreach (Evento evento in eventos)
                    cmbEventoContexto.Items.Add(evento);

                int indice = -1;
                for (int i = 0; i < cmbEventoContexto.Items.Count; i++)
                {
                    Evento item = cmbEventoContexto.Items[i] as Evento;
                    if (item != null && eventoSelecionado != null && item.IdEvento == eventoSelecionado.IdEvento)
                    {
                        indice = i;
                        break;
                    }
                }
                if (indice < 0 && cmbEventoContexto.Items.Count > 0)
                    indice = 0;
                cmbEventoContexto.SelectedIndex = indice;
                if (eventoSelecionado == null && indice >= 0)
                    eventoSelecionado = cmbEventoContexto.Items[indice] as Evento;
                cmbEventoContexto.Enabled = cmbEventoContexto.Items.Count > 1;
                cmbEventoContexto.EndUpdate();
            }
            catch (Exception erro)
            {
                cmbEventoContexto.Enabled = false;
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " ao atualizar o evento em consulta.");
            }
            finally
            {
                sincronizandoContextoEvento = false;
            }
        }

        private bool TelaDependeDeEvento(Button menu)
        {
            return menu == buttonEquipeSetores || menu == buttonFinanceiro || menu == buttonEscala ||
                menu == buttonSolicitacoes || menu == buttonMovimentacoes;
        }

        private void cmbEventoContexto_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sincronizandoContextoEvento)
                return;
            Evento selecionado = cmbEventoContexto.SelectedItem as Evento;
            if (selecionado == null || (eventoSelecionado != null && selecionado.IdEvento == eventoSelecionado.IdEvento))
                return;

            eventoSelecionado = selecionado;
            if (TelaDependeDeEvento(menuAtual))
                ReabrirTelaDependenteDeEvento(menuAtual);
        }

        private void ReabrirTelaDependenteDeEvento(Button menu)
        {
            if (menu == buttonEquipeSetores)
                AbrirTela(menu, () => new EquipeSetoresControl(usuarioAutenticado, eventoSelecionado));
            else if (menu == buttonFinanceiro)
                AbrirTela(menu, () => new FinanceiroControl(usuarioAutenticado, eventoSelecionado));
            else if (menu == buttonEscala)
                AbrirTela(menu, () => new EscalaControl(usuarioAutenticado, eventoSelecionado));
            else if (menu == buttonSolicitacoes)
                AbrirTela(menu, CriarSolicitacoes);
            else if (menu == buttonMovimentacoes)
                AbrirTela(menu, () => new RegistrosControl(usuarioAutenticado, eventoSelecionado));
        }

        private void InicializarEventoSelecionado()
        {
            try
            {
                string perfil;
                new CASE.Services.AutorizacaoService().ValidarUsuarioEObterPerfil(
                    usuarioAutenticado,
                    out perfil);

                if (string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase))
                {
                    eventoSelecionado = new CASE.Services.EventoService()
                        .Listar(usuarioAutenticado)
                        .FirstOrDefault();
                }
            }
            catch (Exception erro)
            {
                eventoSelecionado = null;
                System.Diagnostics.Trace.WriteLine(
                    erro.GetType().Name + " ao selecionar o evento inicial.");
            }
        }

        private void ConfigurarNovosCadastros()
        {
            buttonFornecedores = CriarBotaoMenu("buttonFornecedores", "Fornecedores");
            buttonAcessos = CriarBotaoMenu("buttonAcessos", "Usuários e acessos");
            AdicionarIconeMenu(buttonFornecedores, Properties.Resources.LucideMenuFornecedores);
            AdicionarIconeMenu(buttonAcessos, Properties.Resources.LucideMenuAcessos);
            buttonAcessos.Visible = false;
            buttonFornecedores.Click += (s, e) => AbrirTela(buttonFornecedores, () => new FornecedoresControl(usuarioAutenticado));
            buttonAcessos.Click += (s, e) => AbrirTela(buttonAcessos, CriarGestaoAcessos);
            Button historico = CriarBotaoMenu("buttonHistorico", "Movimentações");
            historico.Visible = false;
            historico.TabStop = false;
            AdicionarIconeMenu(historico, Properties.Resources.LucideAzulRegistros);
            historico.Click += (s, e) => AbrirTela(buttonEstoque, CriarOperacoesEstoque);
        }

        private Button CriarBotaoMenu(string nome, string texto)
        {
            var botao = new Button
            {
                Name = nome,
                Text = texto,
                Size = buttonEstoque.Size,
                Margin = buttonEstoque.Margin,
                Padding = buttonEstoque.Padding,
                TextAlign = buttonEstoque.TextAlign,
                FlatStyle = FlatStyle.Flat,
                BackColor = VisualUtils.Marinho,
                ForeColor = Color.White
            };
            navegacao.Controls.Add(botao);
            VisualUtils.Arredondar(botao, 8);
            return botao;
        }

        private void AdicionarIconeMenu(Button botao, Image imagem)
        {
            var icone = new PictureBox
            {
                Name = "icone" + botao.Name,
                Image = imagem,
                Bounds = new Rectangle(12, 9, 20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Enabled = false,
                TabStop = false
            };
            botao.Controls.Add(icone);
        }

        private UserControl CriarOperacoesEstoque()
        {
            var tela = new OperacoesEstoqueControl(usuarioAutenticado);
            tela.FornecedoresSolicitados += (s, e) => AbrirTela(buttonFornecedores, () => new FornecedoresControl(usuarioAutenticado));
            tela.SolicitacoesSolicitadas += (s, e) => AbrirTela(buttonSolicitacoes, () =>
            {
                var pedidos = (SolicitacoesControl)CriarSolicitacoes();
                pedidos.ExibirPedidosCentrais();
                return pedidos;
            });
            return tela;
        }

        private UserControl CriarGestaoAcessos()
        {
            var tela = new AcessosControl(usuarioAutenticado);
            tela.AcessosAlterados += (s, e) =>
            {
                AtualizarCabecalho();
            };
            return tela;
        }

        private void ConfigurarNotificacoes()
        {
            FormClosing += (s, e) => menuNotificacoes.Close();
            Disposed += (s, e) => menuNotificacoes.Dispose();
            pictureNotificacoes.Cursor = Cursors.Hand;
            pictureNotificacoes.Click += pictureNotificacoes_Click;
            indicadorNotificacoes.Name = "indicadorNotificacoes";
            indicadorNotificacoes.Size = new Size(18, 18);
            indicadorNotificacoes.Location = new Point(
                pictureNotificacoes.Right - 7,
                pictureNotificacoes.Top - 6);
            indicadorNotificacoes.TextAlign = ContentAlignment.MiddleCenter;
            indicadorNotificacoes.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            indicadorNotificacoes.ForeColor = Color.White;
            indicadorNotificacoes.BackColor = Color.FromArgb(220, 53, 69);
            indicadorNotificacoes.Visible = false;
            indicadorNotificacoes.Cursor = Cursors.Hand;
            indicadorNotificacoes.Click += pictureNotificacoes_Click;
            panelPerfil.Controls.Add(indicadorNotificacoes);
            indicadorNotificacoes.BringToFront();
            VisualUtils.Arredondar(indicadorNotificacoes, 9);
            AtualizarContagemNotificacoes();
        }

        private void AtualizarContagemNotificacoes()
        {
            try
            {
                List<CASE.Services.NotificacaoPendente> pendencias =
                    new CASE.Services.NotificacaoService().ListarPendencias(usuarioAutenticado);
                indicadorNotificacoes.Visible = pendencias.Count > 0;

                if (pendencias.Count > 9)
                {
                    indicadorNotificacoes.Text = "9+";
                }
                else
                {
                    indicadorNotificacoes.Text = pendencias.Count.ToString();
                }

                dicaNotificacoes.SetToolTip(
                    pictureNotificacoes,
                    pendencias.Count + " pendência(s) que exigem atenção.");
            }
            catch (Exception)
            {
                indicadorNotificacoes.Visible = false;
                dicaNotificacoes.SetToolTip(
                    pictureNotificacoes,
                    "Não foi possível consultar as pendências.");
            }
        }

        private void pictureNotificacoes_Click(object sender, EventArgs e)
        {
            ContextMenuStrip menu = menuNotificacoes;
            menu.Close();
            while (menu.Items.Count > 0)
            {
                ToolStripItem itemAnterior = menu.Items[0];
                menu.Items.RemoveAt(0);
                itemAnterior.Dispose();
            }

            try
            {
                List<CASE.Services.NotificacaoPendente> pendencias =
                    new CASE.Services.NotificacaoService().ListarPendencias(usuarioAutenticado);

                if (pendencias.Count == 0)
                {
                    ToolStripMenuItem vazio = new ToolStripMenuItem("Nenhuma pendência no momento");
                    vazio.Enabled = false;
                    menu.Items.Add(vazio);
                }
                else
                {
                    foreach (CASE.Services.NotificacaoPendente pendencia in pendencias)
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(
                            pendencia.Titulo + " — " + pendencia.Descricao);
                        item.Tag = pendencia;
                        item.Click += NotificacaoSelecionada_Click;
                        menu.Items.Add(item);
                    }
                }

                // O mesmo menu permanece vivo durante as mensagens de foco do Windows.
                menu.Show(pictureNotificacoes, new Point(0, pictureNotificacoes.Height));
                AtualizarContagemNotificacoes();
            }
            catch (Exception erro)
            {
                menu.Close();
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em Notificacoes: " + erro.StackTrace);
                MessageBox.Show(
                    "Não foi possível carregar as pendências.",
                    "Notificações",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void NotificacaoSelecionada_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item == null)
            {
                return;
            }

            CASE.Services.NotificacaoPendente pendencia =
                item.Tag as CASE.Services.NotificacaoPendente;

            if (pendencia == null)
            {
                return;
            }

            if (pendencia.Tipo == "cadastro_usuario")
            {
                AbrirTela(buttonAcessos, CriarGestaoAcessos);
            }
            else if (pendencia.Tipo == "solicitacao_membro")
            {
                try
                {
                    eventoSelecionado = new CASE.Services.EventoService().BuscarPorId(
                        usuarioAutenticado,
                        pendencia.IdEvento);
                    AbrirTela(
                        buttonSolicitacoes,
                        () => new SolicitacoesControl(usuarioAutenticado, eventoSelecionado));
                    if (panelConteudo.Controls.Count > 0)
                    {
                        SolicitacoesControl tela = panelConteudo.Controls[0] as SolicitacoesControl;
                        if (tela != null)
                        {
                            tela.AbrirPedidoDeReposicao(pendencia.IdReferencia);
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        "Não foi possível abrir a solicitação deste evento.",
                        "Notificações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else if (pendencia.Tipo == "solicitacao_central" ||
                pendencia.Tipo == "solicitacao_central_aprovada")
            {
                DecidirSolicitacaoCentral(pendencia.IdReferencia);
            }

            AtualizarContagemNotificacoes();
        }

        private void DecidirSolicitacaoCentral(int idSolicitacao)
        {
            CASE.Services.SolicitacaoEstoqueCentralService service =
                new CASE.Services.SolicitacaoEstoqueCentralService();

            try
            {
                CASE.Models.SolicitacaoEstoqueCentral solicitacao = service.BuscarPorId(
                    usuarioAutenticado,
                    idSolicitacao);
                List<CASE.Models.SolicitacaoEstoqueCentralItem> itens = service.ListarItens(
                    usuarioAutenticado,
                    idSolicitacao);
                System.Text.StringBuilder detalhes = new System.Text.StringBuilder();
                detalhes.AppendLine("Pedido " + solicitacao.Tipo + " do evento " + solicitacao.IdEvento);
                detalhes.AppendLine();

                foreach (CASE.Models.SolicitacaoEstoqueCentralItem solicitacaoItem in itens)
                {
                    CASE.Models.Produto produto = new CASE.Services.ProdutoService().BuscarPorId(
                        usuarioAutenticado,
                        solicitacaoItem.IdProduto);
                    string nomeProduto = "Produto " + solicitacaoItem.IdProduto;

                    if (produto != null)
                    {
                        nomeProduto = produto.Nome;
                    }

                    detalhes.AppendLine(nomeProduto + " — " + solicitacaoItem.Quantidade);
                }

                if (string.Equals(
                    solicitacao.Situacao,
                    CASE.Services.SituacoesSolicitacao.Aprovada,
                    StringComparison.OrdinalIgnoreCase))
                {
                    DialogResult atender = MessageBox.Show(
                        detalhes.ToString() +
                        "\nO pedido já foi aprovado. Deseja efetivar a transferência agora?",
                        "Atender pedido aprovado",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (atender == DialogResult.Yes)
                    {
                        service.AtenderComTransferencia(
                            usuarioAutenticado,
                            solicitacao.IdSolicitacaoEstoqueCentral);
                    }

                    AtualizarContagemNotificacoes();
                    return;
                }

                DialogResult decisao = MessageBox.Show(
                    detalhes.ToString() + "\nSim: aprovar integralmente. Não: rejeitar. Cancelar: voltar.",
                    "Solicitação ao estoque central",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (decisao == DialogResult.Yes)
                {
                    service.Aprovar(
                        usuarioAutenticado,
                        solicitacao.IdSolicitacaoEstoqueCentral,
                        solicitacao.Versao);

                    DialogResult liberarAgora = MessageBox.Show(
                        "Pedido aprovado sem movimentar saldo. Deseja efetivar a transferência agora?",
                        "Efetivar atendimento",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (liberarAgora == DialogResult.Yes)
                    {
                        service.AtenderComTransferencia(
                            usuarioAutenticado,
                            solicitacao.IdSolicitacaoEstoqueCentral);
                    }
                }
                else if (decisao == DialogResult.No)
                {
                    string justificativa = SolicitarMotivoRejeicao();

                    if (string.IsNullOrWhiteSpace(justificativa))
                    {
                        return;
                    }

                    service.Rejeitar(
                        usuarioAutenticado,
                        solicitacao.IdSolicitacaoEstoqueCentral,
                        solicitacao.Versao,
                        justificativa);
                }

                AtualizarContagemNotificacoes();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Sua conta não pode decidir este pedido.",
                    "Solicitação ao estoque central",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    CASE.Utils.FeedbackUsuario.MensagemErro(ex, "Não foi possível processar o pedido. Atualize a tela e tente novamente."),
                    "Solicitação ao estoque central",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                AtualizarContagemNotificacoes();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível processar o pedido.",
                    "Solicitação ao estoque central",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string SolicitarMotivoRejeicao()
        {
            using (Form dialogo = new Form())
            using (TextBox campo = new TextBox())
            using (Button rejeitar = new Button())
            using (Button cancelar = new Button())
            {
                IconoAplicacao.Aplicar(dialogo);
                dialogo.Text = "Motivo da rejeição";
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.ClientSize = new Size(420, 150);
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                campo.Multiline = true;
                campo.Name = "txtJustificativaRejeicao";
                campo.MaxLength = 500;
                campo.SetBounds(12, 12, 396, 72);
                rejeitar.Text = "Rejeitar";
                Label erroMotivo = new Label { Name = "lblErroJustificativa", ForeColor = Color.Firebrick };
                erroMotivo.SetBounds(12, 85, 396, 17);
                dialogo.Controls.Add(erroMotivo);
                rejeitar.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(campo.Text))
                    {
                        erroMotivo.Text = "Informe o motivo da rejeição.";
                        campo.Focus();
                        return;
                    }
                    dialogo.DialogResult = DialogResult.OK;
                };
                rejeitar.SetBounds(222, 102, 88, 30);
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(320, 102, 88, 30);
                dialogo.Controls.Add(campo);
                dialogo.Controls.Add(rejeitar);
                dialogo.Controls.Add(cancelar);
                FrontendTheme.Aplicar(dialogo);
                dialogo.AcceptButton = rejeitar;
                dialogo.CancelButton = cancelar;

                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    return campo.Text.Trim();
                }
            }

            return string.Empty;
        }

        private void AplicarAcabamento()
        {
            BackColor = VisualUtils.Fundo;
            panelAreaPrincipal.BackColor = panelConteudo.BackColor = VisualUtils.Fundo;
            panelMenu.BackColor = panelRodapeMenu.BackColor = navegacao.BackColor = VisualUtils.Marinho;
            panelCabecalho.BackColor = Color.White;
            panelCabecalho.Paint += (s, e) =>
            {
                using (var linha = new Pen(VisualUtils.Borda))
                    e.Graphics.DrawLine(linha, 0, panelCabecalho.Height - 1, panelCabecalho.Width, panelCabecalho.Height - 1);
            };
            labelNomeUsuario.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            labelNomeUsuario.ForeColor = VisualUtils.Texto;
            labelTipoUsuario.Font = new Font("Segoe UI", 8.5F);
            labelTipoUsuario.ForeColor = VisualUtils.TextoSuave;
            panelPerfil.Width = 260;
            panelPesquisa.BackColor = textPesquisa.BackColor = Color.FromArgb(247, 249, 252);
            VisualUtils.AplicarBorda(panelPesquisa);
            foreach (var botao in navegacao.Controls.OfType<Button>().Concat(new[] { buttonConfiguracoes, buttonSair }))
            {
                botao.BackColor = VisualUtils.Marinho;
                botao.ForeColor = Color.FromArgb(213, 225, 239);
                botao.Font = new Font("Segoe UI", 9.5F);
                botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 61, 98);
                botao.FlatAppearance.BorderSize = 0;
                botao.Cursor = Cursors.Hand;
                botao.Paint += (s, e) =>
                {
                    if (botao.BackColor != Color.FromArgb(27, 75, 125))
                        return;
                    using (var pincel = new SolidBrush(Color.FromArgb(89, 163, 255)))
                        e.Graphics.FillRectangle(pincel, 0, 10, 3, Math.Max(1, botao.Height - 20));
                };
            }
        }

        private void ConfigurarFotoPerfil()
        {
            ToolStripMenuItem itemUsuario = new ToolStripMenuItem("Nome de usuário: " + usuarioAutenticado.Login)
            {
                Enabled = false
            };
            ToolStripMenuItem itemAlterarFoto = new ToolStripMenuItem("Alterar foto");
            itemAlterarFoto.Click += AlterarFotoPerfil_Click;

            ToolStripMenuItem itemRemoverFoto = new ToolStripMenuItem("Remover foto");
            itemRemoverFoto.Click += RemoverFotoPerfil_Click;

            menuFotoPerfil.Items.Add(itemUsuario);
            menuFotoPerfil.Items.Add(new ToolStripSeparator());
            menuFotoPerfil.Items.Add(itemAlterarFoto);
            menuFotoPerfil.Items.Add(itemRemoverFoto);
            menuFotoPerfil.Opening += MenuFotoPerfil_Opening;

            pictureUsuario.ContextMenuStrip = menuFotoPerfil;
            pictureUsuario.Cursor = Cursors.Hand;
            pictureUsuario.BackColor = Color.Transparent;
            pictureUsuario.Click += pictureUsuario_Click;
            pictureUsuario.Paint += PictureUsuario_Paint;
            FormClosed += PrincipalForm_FormClosed;

            try
            {
                byte[] foto = fotoPerfilService.BuscarFotoPerfil(usuarioAutenticado);

                if (foto != null && foto.Length > 0)
                {
                    AplicarFotoPerfil(foto);
                }
            }
            catch (Exception)
            {
                RestaurarFotoPadrao();
            }
        }

        private void pictureUsuario_Click(object sender, EventArgs e)
        {
            menuFotoPerfil.Show(
                pictureUsuario,
                new Point(0, pictureUsuario.Height));
        }

        private void PictureUsuario_Paint(object sender, PaintEventArgs e)
        {
            if (fotoPerfilPersonalizada == null)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var contorno = new Pen(Color.FromArgb(203, 213, 225), 1.5F))
            {
                Rectangle area = pictureUsuario.ClientRectangle;
                area.Inflate(-2, -2);
                e.Graphics.DrawEllipse(contorno, area);
            }
        }

        private void MenuFotoPerfil_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (menuFotoPerfil.Items.Count > 3)
            {
                menuFotoPerfil.Items[3].Enabled = fotoPerfilPersonalizada != null;
            }
        }

        private void AlterarFotoPerfil_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog seletorArquivo = new OpenFileDialog())
            {
                seletorArquivo.Title = "Selecionar foto de perfil";
                seletorArquivo.Filter = "Imagens JPG ou PNG|*.jpg;*.jpeg;*.png";
                seletorArquivo.CheckFileExists = true;
                seletorArquivo.Multiselect = false;

                if (seletorArquivo.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    FileInfo arquivo = new FileInfo(seletorArquivo.FileName);

                    if (arquivo.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show(
                            this,
                            "A imagem não pode ultrapassar 2 MB.",
                            "Foto de perfil",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    byte[] arquivoImagem = File.ReadAllBytes(seletorArquivo.FileName);
                    byte[] fotoSalva = fotoPerfilService.SalvarFotoPerfil(
                        usuarioAutenticado,
                        arquivoImagem);

                    AplicarFotoPerfil(fotoSalva);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show(
                        this,
                        "O arquivo não é uma imagem JPG ou PNG válida.",
                        "Foto de perfil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (InvalidOperationException erro)
                {
                    MessageBox.Show(
                        this,
                        CASE.Utils.FeedbackUsuario.MensagemErro(erro, "A imagem de perfil selecionada não pôde ser utilizada."),
                        "Foto de perfil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        this,
                        "Não foi possível salvar a foto de perfil.",
                        "Foto de perfil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void RemoverFotoPerfil_Click(object sender, EventArgs e)
        {
            try
            {
                fotoPerfilService.RemoverFotoPerfil(usuarioAutenticado);
                RestaurarFotoPadrao();
            }
            catch (InvalidOperationException erro)
            {
                MessageBox.Show(
                    this,
                    CASE.Utils.FeedbackUsuario.MensagemErro(erro, "A imagem de perfil selecionada não pôde ser utilizada."),
                    "Foto de perfil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "Não foi possível remover a foto de perfil.",
                    "Foto de perfil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AplicarFotoPerfil(byte[] dadosFoto)
        {
            using (MemoryStream fluxoImagem = new MemoryStream(dadosFoto))
            using (Image imagemCarregada = Image.FromStream(fluxoImagem, true, true))
            {
                Image copiaImagem = CriarFotoCircular(imagemCarregada);
                LiberarFotoPersonalizada();
                fotoPerfilPersonalizada = copiaImagem;
                pictureUsuario.Image = fotoPerfilPersonalizada;
                pictureUsuario.Invalidate();
            }
        }

        private static Bitmap CriarFotoCircular(Image imagem)
        {
            const int tamanho = 256;
            const int margem = 10;
            var circular = new Bitmap(
                tamanho,
                tamanho,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics grafico = Graphics.FromImage(circular))
            using (var recorte = new GraphicsPath())
            {
                grafico.Clear(Color.Transparent);
                grafico.SmoothingMode = SmoothingMode.AntiAlias;
                grafico.InterpolationMode = InterpolationMode.HighQualityBicubic;
                grafico.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var circulo = new Rectangle(
                    margem,
                    margem,
                    tamanho - margem * 2,
                    tamanho - margem * 2);
                recorte.AddEllipse(circulo);
                grafico.SetClip(recorte);

                int ladoOrigem = Math.Min(imagem.Width, imagem.Height);
                var origem = new Rectangle(
                    (imagem.Width - ladoOrigem) / 2,
                    (imagem.Height - ladoOrigem) / 2,
                    ladoOrigem,
                    ladoOrigem);
                grafico.DrawImage(imagem, circulo, origem, GraphicsUnit.Pixel);
                grafico.ResetClip();
            }

            return circular;
        }

        private void RestaurarFotoPadrao()
        {
            pictureUsuario.Image = Properties.Resources.LucideUsuario;
            LiberarFotoPersonalizada();
            pictureUsuario.Invalidate();
        }

        private void LiberarFotoPersonalizada()
        {
            if (fotoPerfilPersonalizada != null)
            {
                fotoPerfilPersonalizada.Dispose();
                fotoPerfilPersonalizada = null;
            }
        }

        private void PrincipalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            RestaurarFotoPadrao();
            menuFotoPerfil.Dispose();
        }

        private void AtualizarCabecalho()
        {
            AtualizarNomeExibido();
            labelTipoUsuario.Text = "Conta autenticada";
            buttonAcessos.Visible = false;

            try
            {
                string nomePerfil;
                new CASE.Services.AutorizacaoService().ValidarUsuarioEObterPerfil(
                    usuarioAutenticado,
                    out nomePerfil);
                buttonAcessos.Visible = string.Equals(
                    nomePerfil,
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase);
                labelTipoUsuario.Text = nomePerfil;
                bool membro = string.Equals(nomePerfil, "Membro", StringComparison.OrdinalIgnoreCase);
                buttonProdutos.Visible = !membro;
                buttonFornecedores.Visible = !membro;
                buttonQuermesses.Visible = !membro;
                buttonEquipeSetores.Visible = !membro;
                buttonFinanceiro.Visible = !membro;
                buttonRelatorios.Visible = true;
                OrganizarMenuPorPerfil(nomePerfil);
            }
            catch (Exception)
            {
                buttonAcessos.Visible = false;
            }

            labelNomeUsuario.AutoSize = false;
            labelNomeUsuario.Size = new Size(110, 20);
            labelNomeUsuario.AutoEllipsis = true;
            dicaNomeUsuario.SetToolTip(labelNomeUsuario, "Nome de usuário: " + usuarioAutenticado.Login);
            buttonMovimentacoes.Text = "Vendas e perdas";
        }

        private void AtualizarNomeExibido()
        {
            try
            {
                Pessoa pessoa = new CASE.Services.PessoaService().BuscarMeusDados(usuarioAutenticado);
                labelNomeUsuario.Text = string.IsNullOrWhiteSpace(pessoa?.Nome)
                    ? usuarioAutenticado.Login
                    : pessoa.Nome;
            }
            catch (Exception)
            {
                labelNomeUsuario.Text = usuarioAutenticado.Login;
            }
        }

        private UserControl CriarConfiguracoes()
        {
            var controle = new ConfiguracoesControl(usuarioAutenticado);
            controle.GerenciarAcessosSolicitado += (s, e) => AbrirTela(buttonAcessos, CriarGestaoAcessos);
            controle.PerfilAtualizado += (s, e) => AtualizarNomeExibido();
            controle.SenhaAlterada += (s, e) => Close();
            return controle;
        }

        private void OrganizarMenuPorPerfil(string perfil)
        {
            string[] ordem;
            if (string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                ordem = new[] { "buttonDashboard",
                    "buttonFornecedores",
"buttonProdutos",
"buttonQuermesses",
"buttonEquipeSetores",
                    "buttonEscala",
"buttonEstoque",
"buttonSolicitacoes",
"buttonMovimentacoes",
                    "buttonFinanceiro",
"buttonHistorico",
"buttonRelatorios",
"buttonConfiguracoes",
"buttonAcessos" };
            }
            else if (string.Equals(perfil, "Gerente", StringComparison.OrdinalIgnoreCase))
            {
                ordem = new[] { "buttonDashboard",
"buttonFornecedores",
"buttonProdutos",
                    "buttonQuermesses",
"buttonEquipeSetores",
"buttonEscala",
"buttonEstoque",
                    "buttonSolicitacoes",
"buttonMovimentacoes",
"buttonFinanceiro",
"buttonHistorico",
                    "buttonRelatorios",
"buttonConfiguracoes" };
            }
            else
            {
                ordem = new[] { "buttonDashboard",
"buttonEscala",
"buttonEstoque",
                    "buttonMovimentacoes",
"buttonSolicitacoes",
"buttonHistorico",
"buttonRelatorios",
                    "buttonConfiguracoes" };
            }

            navegacao.SuspendLayout();
            try
            {
                navegacao.Controls.Add(buttonConfiguracoes);
                buttonConfiguracoes.Margin = buttonDashboard.Margin;
                buttonConfiguracoes.Size = buttonDashboard.Size;
                for (int indice = 0; indice < ordem.Length; indice++)
                {
                    Control controle = navegacao.Controls[ordem[indice]];
                    if (controle != null)
                    {
                        navegacao.Controls.SetChildIndex(controle, indice);
                        controle.TabIndex = indice;
                    }
                }
            }
            finally
            {
                navegacao.ResumeLayout(true);
                AjustarLarguraMenu();
            }
        }

        private void SelecionarMenu(Button ativo)
        {
            foreach (Button botao in navegacao.Controls.OfType<Button>().Concat(new[] { buttonConfiguracoes }))
            {
                if (botao == ativo)
                {
                    botao.BackColor = Color.FromArgb(27, 75, 125);
                    botao.ForeColor = Color.White;
                }
                else
                {
                    botao.BackColor = panelMenu.BackColor;
                    botao.ForeColor = Color.FromArgb(213, 225, 239);
                }

                botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 61, 98);
            }
        }

        private void AbrirTela(Button menu, Func<UserControl> criar)
        {
            UserControl tela = null;
            try
            {
                // Mantém a tela atual se a abertura da próxima falhar.
                bool dependeDeEvento = TelaDependeDeEvento(menu);
                bool possuiEventosNoContexto = cmbEventoContexto.Items.Count > 0;
                cmbEventoContexto.Visible = dependeDeEvento && possuiEventosNoContexto;
                lblEventoContexto.Visible = dependeDeEvento && possuiEventosNoContexto;

                if (dependeDeEvento && eventoSelecionado == null)
                {
                    InicializarEventoSelecionado();
                }

                tela = criar();
                tela.Dock = DockStyle.Fill;
                panelConteudo.SuspendLayout();
                while (panelConteudo.Controls.Count > 0)
                {
                    panelConteudo.Controls[0].Dispose();
                }
                panelConteudo.Controls.Add(tela);
                menuAtual = menu;
                SelecionarMenu(menu);
            }
            catch (Exception erro)
            {
                if (tela != null && tela.Parent == null)
                {
                    tela.Dispose();
                }
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em AbrirTela: " + erro.StackTrace);
                MessageBox.Show(this, "Não foi possível abrir esta tela. Confira se sua conta tem acesso e tente novamente.",
                    "CASE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                panelConteudo.ResumeLayout(true);
            }
        }

        private void AjustarLarguraMenu()
        {
            if (ajustandoMenu)
            {
                return;
            }
            ajustandoMenu = true;
            try
            {
                int largura = navegacao.ClientSize.Width - navegacao.Padding.Horizontal;
                foreach (Control controle in navegacao.Controls)
                {
                    Button botao = controle as Button;
                    if (botao != null)
                    {
                        int desejada = Math.Max(140, largura - botao.Margin.Horizontal - 2);
                        if (botao.Width != desejada)
                        {
                            botao.Width = desejada;
                        }
                    }
                }
            }
            finally
            {
                ajustandoMenu = false;
            }
        }

        private void textPesquisa_Leave(object sender, EventArgs e)
        {
            if (textPesquisa.Text == string.Empty)
            {
                textPesquisa.Text = "Pesquisar...";
                textPesquisa.ForeColor = Color.FromArgb(148, 163, 184);
            }
        }

        private void textPesquisa_Enter(object sender, EventArgs e)
        {
            if (textPesquisa.Text == "Pesquisar...")
            {
                textPesquisa.Text = string.Empty;
                textPesquisa.ForeColor = Color.FromArgb(18, 47, 80);
            }
        }

        private void PrincipalForm_Shown(object sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

    }
}
