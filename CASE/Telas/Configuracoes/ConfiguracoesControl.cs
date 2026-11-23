using System;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class ConfiguracoesControl : UserControl
    {
        public event EventHandler GerenciarAcessosSolicitado;
        public event EventHandler SenhaAlterada;
        public event EventHandler PerfilAtualizado;
        private readonly Usuario usuarioAutenticado;
        private readonly PessoaService pessoaService = new PessoaService();
        private readonly Label lblMensagemPerfil = new Label();
        private bool administrador;
        private FlowLayoutPanel menuConfiguracoes;
        private Panel[] paginasConfiguracao;
        private Button[] botoesConfiguracao;

        public ConfiguracoesControl() : this(null)
        {
        }

        public ConfiguracoesControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            if (usuario != null)
            {
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuario, out perfil);
                    administrador = perfil == "Administrador";
                }
                catch (Exception)
                {
                    administrador = false;
                }
            }
            CASE.Utils.FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelPerfilUsuario, 12);
            panelPerfilUsuario.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelPerfilUsuario);
            VisualUtils.Arredondar(btnSalvarPerfil, 8);
            btnSalvarPerfil.SizeChanged += AjustarArredondamento;
            panelPreferencias.Visible = false;
            VisualUtils.Arredondar(panelSolicitacoesPendentes, 12);
            panelSolicitacoesPendentes.SizeChanged += AjustarArredondamento;
            VisualUtils.AplicarBorda(panelSolicitacoesPendentes);
            VisualUtils.Arredondar(btnAprovarSolicitacao, 8);
            btnAprovarSolicitacao.SizeChanged += AjustarArredondamento;
            VisualUtils.Arredondar(btnRejeitarSolicitacao, 8);
            btnRejeitarSolicitacao.SizeChanged += AjustarArredondamento;
            dgvSolicitacoes.ClearSelection();
            lblSubtituloConfiguracoes.Text = "Gerencie sua conta e os cadastros da instituição";
            txtPerfilNome.Clear();
            txtPerfilUsuario.Clear();
            lblPerfilEmail.Visible = false;
            txtPerfilEmail.Visible = false;
            txtPerfilEmail.TabStop = false;
            chkAlertasEmail.Visible = false;
            chkAlertasEmail.Enabled = false;
            menuConfiguracoes = new FlowLayoutPanel
            {
                Name = "panelMenuConfiguracoes",
                Bounds = new System.Drawing.Rectangle(30, 115, 180, 320),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 4),
                BackColor = BackColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(menuConfiguracoes);
            panelPerfilUsuario.SetBounds(210, 115, 710, 360);
            panelSolicitacoesPendentes.MinimumSize = System.Drawing.Size.Empty;
            panelSolicitacoesPendentes.SetBounds(210, 115, 710, 500);
            dgvSolicitacoes.Width = 670;
            btnAprovarSolicitacao.Left = 460;
            btnRejeitarSolicitacao.Left = 575;
            Label[] titulos = { lblPerfilNome, lblPerfilUsuario };
            TextBox[] campos = { txtPerfilNome, txtPerfilUsuario };
            for (int i = 0; i < campos.Length; i++)
            {
                titulos[i].SetBounds(24, 52 + i * 75, 620, 23);
                campos[i].SetBounds(24, 77 + i * 75, 660, 28);
            }
            btnSalvarPerfil.SetBounds(514, 304, 170, 36);
            txtPerfilUsuario.ReadOnly = true;
            txtPerfilUsuario.TabStop = false;

            lblMensagemPerfil.Name = "lblMensagemPerfil";
            lblMensagemPerfil.SetBounds(24, 342, 660, 22);
            lblMensagemPerfil.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lblMensagemPerfil.ForeColor = VisualUtils.TextoSuave;
            lblMensagemPerfil.Visible = false;
            panelPerfilUsuario.Controls.Add(lblMensagemPerfil);

            btnSalvarPerfil.Click += BtnSalvarPerfil_Click;

            CarregarMeusDados();
            var categorias = new Panel
            {
                Name = "panelCategorias",
                BackColor = System.Drawing.Color.White,
                Bounds = panelPerfilUsuario.Bounds,
                Anchor = panelPerfilUsuario.Anchor
            };
            categorias.Controls.Add(FrontendTheme.Legenda("Categorias de produtos", new System.Drawing.Rectangle(24, 20, 400, 30)));
            var lista = new ListBox
            {
                Name = "listCategorias",
                BorderStyle = BorderStyle.None,
                BackColor = System.Drawing.Color.FromArgb(248, 250, 253),
                ForeColor = VisualUtils.Texto,
                Bounds = new System.Drawing.Rectangle(24, 66, 650, 220),
                Font = new System.Drawing.Font("Segoe UI", 11),
                ItemHeight = 40,
                IntegralHeight = false
            };
            if (administrador)
            {
                CarregarCategorias(lista);
            }
            categorias.Controls.Add(lista);
            ConfigurarCadastroCategorias(categorias, lista);
            Controls.Add(categorias);
            Panel tipos = CriarCadastroTiposEvento(categorias.Bounds);
            Controls.Add(tipos);
            Panel instituicao = CriarConfiguracaoInstituicao(categorias.Bounds);
            Panel turnos = CriarCadastroApoio(categorias.Bounds, true);
            Panel funcoes = CriarCadastroApoio(categorias.Bounds, false);
            Controls.Add(instituicao);
            Controls.Add(turnos);
            Controls.Add(funcoes);
            paginasConfiguracao = new Panel[] { panelPerfilUsuario,
instituicao,
categorias,
tipos,
turnos,
funcoes,
panelSolicitacoesPendentes };
            string[] nomes = { "Perfil",
"Instituição",
"Categorias",
"Tipos de evento",
"Turnos",
"Funções na equipe",
"Usuários" };
            var botoes = new System.Collections.Generic.List<Button>();
            for (int i = 0; i < nomes.Length; i++)
            {
                Panel pagina = paginasConfiguracao[i];
                var botao = new Button
                {
                    Name = "btnConfig" + i,
                    Text = nomes[i],
                    Width = 174,
                    Height = 42,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = FrontendTheme.Texto,
                    BackColor = System.Drawing.Color.Transparent,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    Padding = new Padding(10, 0, 0, 0),
                    UseVisualStyleBackColor = false
                };
                botao.FlatAppearance.BorderSize = 0;
                botao.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 241, 253);
                botao.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(215, 233, 255);
                if (!administrador && pagina != panelPerfilUsuario)
                {
                    botao.Visible = false;
                }
                menuConfiguracoes.Controls.Add(botao);
                botoes.Add(botao);
                botao.Click += (s, e) =>
                {
                    if (pagina == panelSolicitacoesPendentes)
                    {
                        GerenciarAcessosSolicitado?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                    foreach (Panel item in paginasConfiguracao)
                        item.Visible = item == pagina;
                    foreach (Button item in botoes)
                    {
                        if (item == botao)
                        {
                            item.BackColor = System.Drawing.Color.FromArgb(215, 233, 255);
                            item.ForeColor = VisualUtils.Azul;
                            item.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                        }
                        else
                        {
                            item.BackColor = System.Drawing.Color.Transparent;
                            item.ForeColor = FrontendTheme.Texto;
                            item.Font = new System.Drawing.Font("Segoe UI", 9.5F);
                        }
                    }
                };
            }
            botoesConfiguracao = botoes.ToArray();
            botoes[0].PerformClick();
            SizeChanged += (s, e) => OrganizarConfiguracoes();
            OrganizarConfiguracoes();
            PrepararAlteracaoSenha();
            EnvolverCampoArredondado(panelPerfilUsuario, txtPerfilNome, "panelCampoPerfilNome");
            EnvolverCampoArredondado(panelPerfilUsuario, txtPerfilUsuario, "panelCampoPerfilUsuario");
            OrganizarCamposPerfil();
        }

        private void OrganizarConfiguracoes()
        {
            if (menuConfiguracoes == null || paginasConfiguracao == null)
            {
                return;
            }

            int largura = Math.Max(640, ClientSize.Width - 236);
            int alturaVisivel = Math.Max(420, ClientSize.Height - 132);
            menuConfiguracoes.SetBounds(14, 115, 180, alturaVisivel);

            foreach (Panel pagina in paginasConfiguracao)
            {
                int altura = pagina == panelPerfilUsuario ? 700 :
                    pagina == panelSolicitacoesPendentes ? 500 : 420;
                pagina.SetBounds(210, 115, largura, altura);
                pagina.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }

            OrganizarCamposPerfil();

            dgvSolicitacoes.Width = largura - 40;
            btnRejeitarSolicitacao.Left = largura - 135;
            btnAprovarSolicitacao.Left = largura - 250;
            AutoScrollMinSize = new System.Drawing.Size(920, Math.Max(680, 115 + 700 + 24));
        }

        private void OrganizarCamposPerfil()
        {
            if (panelPerfilUsuario == null || txtPerfilNome == null ||
                txtPerfilUsuario == null || btnSalvarPerfil == null)
            {
                return;
            }

            int larguraInterna = Math.Max(300, panelPerfilUsuario.ClientSize.Width - 48);
            lblTituloPerfilUsuario.SetBounds(24, 18, larguraInterna, 32);
            lblPerfilNome.SetBounds(24, 62, larguraInterna, 22);
            DefinirLimitesCampoPerfil(txtPerfilNome, new System.Drawing.Rectangle(24, 88, larguraInterna, 38));
            lblPerfilUsuario.SetBounds(24, 132, larguraInterna, 22);
            DefinirLimitesCampoPerfil(txtPerfilUsuario, new System.Drawing.Rectangle(24, 158, larguraInterna, 38));
            btnSalvarPerfil.SetBounds(larguraInterna - 170 + 24, 205, 170, 40);
            lblMensagemPerfil.SetBounds(24, 250, larguraInterna, 30);

            foreach (Control controle in panelPerfilUsuario.Controls)
            {
                if (controle.Name == "lblOrientacaoAlteracaoSenha" ||
                    controle.Name == "lblTituloAlteracaoSenha" ||
                    controle.Name == "lblMensagemSenha")
                {
                    controle.Width = larguraInterna;
                }
                else if (controle.Name.StartsWith("paneltxtSenha", StringComparison.Ordinal))
                {
                    controle.Width = larguraInterna;
                }
                else if (controle.Name == "btnAlterarSenha")
                {
                    controle.Left = larguraInterna - controle.Width + 24;
                }
                else if (controle.Name.StartsWith("lbltxtSenha", StringComparison.Ordinal))
                {
                    controle.Width = larguraInterna;
                }
            }
        }

        private void DefinirLimitesCampoPerfil(TextBox campo, System.Drawing.Rectangle limites)
        {
            string nomePainel = campo == txtPerfilNome ? "panelCampoPerfilNome" : "panelCampoPerfilUsuario";
            Control destino = panelPerfilUsuario.Controls[nomePainel];
            if (destino == null)
            {
                campo.SetBounds(limites.X, limites.Y, limites.Width, 34);
                return;
            }

            destino.SetBounds(limites.X, limites.Y, limites.Width, limites.Height);
        }

        private Panel EnvolverCampoArredondado(Panel contenedor, TextBox campo, string nomePainel)
        {
            if (contenedor == null || campo == null)
                return null;

            Panel existente = contenedor.Controls[nomePainel] as Panel;
            if (existente != null)
                return existente;

            Panel painelCriadoPeloTema = campo.Parent as Panel;
            if (painelCriadoPeloTema != null && painelCriadoPeloTema.Parent == contenedor &&
                painelCriadoPeloTema.Controls.Count == 1 && painelCriadoPeloTema.Controls[0] == campo)
            {
                painelCriadoPeloTema.Name = nomePainel;
                return painelCriadoPeloTema;
            }

            System.Drawing.Rectangle limites = campo.Bounds;
            AnchorStyles ancora = campo.Anchor;
            int indice = contenedor.Controls.GetChildIndex(campo);
            contenedor.Controls.Remove(campo);
            Panel painelCampo = new Panel
            {
                Name = nomePainel,
                Bounds = limites,
                Anchor = ancora,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(10, 2, 10, 2)
            };
            campo.Dock = DockStyle.Fill;
            painelCampo.Controls.Add(campo);
            contenedor.Controls.Add(painelCampo);
            contenedor.Controls.SetChildIndex(painelCampo, Math.Min(indice, contenedor.Controls.Count - 1));
            campo.AutoSize = false;
            campo.BorderStyle = BorderStyle.None;
            campo.BackColor = painelCampo.BackColor;
            campo.ForeColor = VisualUtils.Texto;
            campo.Cursor = Cursors.IBeam;
            campo.Dock = DockStyle.Fill;
            campo.Margin = Padding.Empty;
            // Alguns campos do Designer já receberam o estilo padrão antes de serem
            // envolvidos. Reaplica aqui o estilo interno do campo arredondado.
            campo.BringToFront();
            painelCampo.MouseDown += (s, e) => campo.Focus();
            VisualUtils.Arredondar(painelCampo, 7);
            VisualUtils.AplicarBorda(painelCampo);
            painelCampo.Paint += (s, e) =>
            {
                if (!campo.Focused || painelCampo.ClientSize.Width < 3 || painelCampo.ClientSize.Height < 3)
                    return;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (System.Drawing.Drawing2D.GraphicsPath caminho = VisualUtils.CriarRetanguloArredondado(
                    new System.Drawing.Rectangle(1, 1, painelCampo.ClientSize.Width - 2,
                        painelCampo.ClientSize.Height - 2), 7))
                using (System.Drawing.Pen caneta = new System.Drawing.Pen(VisualUtils.Azul, 1.5F))
                {
                    e.Graphics.DrawPath(caneta, caminho);
                }
            };
            campo.Enter += (s, e) => painelCampo.Invalidate();
            campo.Leave += (s, e) => painelCampo.Invalidate();
            painelCampo.SizeChanged += AjustarArredondamento;
            return painelCampo;
        }

        private void PrepararAlteracaoSenha()
        {
            AutoScroll = true;
            panelPerfilUsuario.Height = 700;
            int larguraCampo = panelPerfilUsuario.Width - 48;
            lblTituloPerfilUsuario.SetBounds(24, 18, larguraCampo, 32);
            lblPerfilNome.SetBounds(24, 62, larguraCampo, 22);
            txtPerfilNome.SetBounds(24, 88, larguraCampo, 34);
            lblPerfilUsuario.SetBounds(24, 132, larguraCampo, 22);
            txtPerfilUsuario.SetBounds(24, 158, larguraCampo, 34);
            btnSalvarPerfil.SetBounds(larguraCampo - 170 + 24, 205, 170, 40);
            lblMensagemPerfil.SetBounds(24, 250, larguraCampo, 30);
            lblMensagemPerfil.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Label tituloSenha = FrontendTheme.Legenda(
                "Alterar senha", new System.Drawing.Rectangle(24, 296, larguraCampo, 30));
            tituloSenha.Name = "lblTituloAlteracaoSenha";
            tituloSenha.Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold);
            panelPerfilUsuario.Controls.Add(tituloSenha);
            Label orientacaoSenha = FrontendTheme.Legenda(
                "Confirme sua senha atual para cadastrar uma nova.",
                new System.Drawing.Rectangle(24, 330, larguraCampo, 24));
            orientacaoSenha.Name = "lblOrientacaoAlteracaoSenha";
            panelPerfilUsuario.Controls.Add(orientacaoSenha);

            string[] nomes = { "Senha atual", "Nova senha", "Confirmar nova senha" };
            string[] identificadores = { "txtSenhaAtual", "txtNovaSenha", "txtConfirmarNovaSenha" };
            TextBox[] senhas = new TextBox[3];
            for (int i = 0; i < senhas.Length; i++)
            {
                int topo = 362 + i * 70;
                Label legendaSenha = FrontendTheme.Legenda(nomes[i],
                    new System.Drawing.Rectangle(24, topo, larguraCampo, 22));
                legendaSenha.Name = "lbl" + identificadores[i];
                panelPerfilUsuario.Controls.Add(legendaSenha);
                senhas[i] = new TextBox
                {
                    Name = identificadores[i],
                    UseSystemPasswordChar = true,
                    TabIndex = 3 + i,
                    AccessibleName = nomes[i],
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                Panel campoArredondado = new Panel
                {
                    Name = "panel" + identificadores[i],
                    BackColor = System.Drawing.Color.White,
                    Bounds = new System.Drawing.Rectangle(24, topo + 26, larguraCampo, 38),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Padding = new Padding(10, 2, 10, 2)
                };
                senhas[i].Dock = DockStyle.Fill;
                campoArredondado.Controls.Add(senhas[i]);
                panelPerfilUsuario.Controls.Add(campoArredondado);
                VisualUtils.EstilizarCampo(senhas[i]);
                VisualUtils.Arredondar(campoArredondado, 7);
                VisualUtils.AplicarBorda(campoArredondado);
                campoArredondado.SizeChanged += AjustarArredondamento;
            }

            Button alterar = new Button
            {
                Name = "btnAlterarSenha", Text = "Alterar senha", TabIndex = 6,
                BackColor = VisualUtils.Azul, ForeColor = System.Drawing.Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = usuarioAutenticado != null
            };
            alterar.SetBounds(larguraCampo - 170 + 24, 600, 170, 40);
            VisualUtils.EstilizarBotao(alterar);
            panelPerfilUsuario.Controls.Add(alterar);
            Label mensagem = new Label
            {
                Name = "lblMensagemSenha", AutoSize = false,
                ForeColor = VisualUtils.TextoSuave,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Após alterar a senha, entre novamente no CASE."
            };
            mensagem.SetBounds(24, 650, larguraCampo, 28);
            panelPerfilUsuario.Controls.Add(mensagem);
            bool alterando = false;
            alterar.Click += (s, e) =>
            {
                if (alterando) return;
                alterando = true;
                alterar.Enabled = false;
                bool sucesso = false;
                try
                {
                    new UsuarioService().AlterarMinhaSenha(senhas[0].Text, senhas[1].Text, senhas[2].Text);
                    sucesso = true;
                }
                catch (ArgumentException erro) { mensagem.Text = CASE.Utils.FeedbackUsuario.MensagemErro(erro, "Confira os dados da senha e tente novamente."); }
                catch (UnauthorizedAccessException) { mensagem.Text = "Sua sessão não permite alterar a senha. Entre novamente."; }
                catch (InvalidOperationException erro) { mensagem.Text = CASE.Utils.FeedbackUsuario.MensagemErro(erro, "Não foi possível alterar a senha. Confira os dados e tente novamente."); }
                catch (Exception) { mensagem.Text = "Não foi possível alterar a senha. Tente novamente."; }
                finally
                {
                    foreach (TextBox campo in senhas) campo.Clear();
                    alterando = false;
                    alterar.Enabled = !sucesso && usuarioAutenticado != null;
                }
                if (!sucesso && !string.IsNullOrWhiteSpace(mensagem.Text))
                    CASE.Utils.FeedbackUsuario.Exibir(this, mensagem.Text, CASE.Utils.TipoFeedback.Erro);
                if (sucesso)
                {
                    MessageBox.Show(this, "Senha alterada. Entre novamente com sua nova senha.",
                        "Alterar senha", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SenhaAlterada?.Invoke(this, EventArgs.Empty);
                    FindForm()?.Close();
                }
            };
        }

        private void CarregarMeusDados()
        {
            if (usuarioAutenticado == null)
            {
                txtPerfilNome.Enabled = false;
                txtPerfilUsuario.Enabled = false;
                btnSalvarPerfil.Enabled = false;
                ExibirMensagemPerfil(
                    "Entre no sistema para consultar e alterar seus dados pessoais.",
                    true);
                return;
            }

            try
            {
                Pessoa pessoa = pessoaService.BuscarMeusDados(usuarioAutenticado);
                txtPerfilNome.Text = pessoa.Nome;
                txtPerfilUsuario.Text = usuarioAutenticado.Login;
                ExibirMensagemPerfil(string.Empty, false);
            }
            catch (UnauthorizedAccessException)
            {
                btnSalvarPerfil.Enabled = false;
                ExibirMensagemPerfil("Sua sessão não permite consultar estes dados.", true);
            }
            catch (Exception)
            {
                btnSalvarPerfil.Enabled = false;
                ExibirMensagemPerfil("Não foi possível carregar os dados do perfil.", true);
            }
        }

        private void BtnSalvarPerfil_Click(object sender, EventArgs e)
        {
            try
            {
                pessoaService.AtualizarMeusDados(
                    usuarioAutenticado,
                    txtPerfilNome.Text);

                ExibirMensagemPerfil("Seus dados foram atualizados.", false);
                PerfilAtualizado?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(
                    this,
                    "Seus dados pessoais foram atualizados.",
                    "Perfil",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (ArgumentException erro)
            {
                ExibirMensagemPerfil(CASE.Utils.FeedbackUsuario.MensagemErro(erro, "Confira os dados do perfil e tente novamente."), true);
            }
            catch (UnauthorizedAccessException)
            {
                ExibirMensagemPerfil("Sua sessão não permite alterar estes dados.", true);
            }
            catch (Exception)
            {
                ExibirMensagemPerfil("Não foi possível salvar os dados do perfil.", true);
            }
        }

        private void ExibirMensagemPerfil(string mensagem, bool erro)
        {
            lblMensagemPerfil.Text = mensagem;
            lblMensagemPerfil.ForeColor = erro
                ? System.Drawing.Color.FromArgb(190, 55, 65)
                : System.Drawing.Color.FromArgb(43, 133, 104);
            lblMensagemPerfil.Visible = !string.IsNullOrWhiteSpace(mensagem);
            if (!string.IsNullOrWhiteSpace(mensagem))
                CASE.Utils.FeedbackUsuario.Exibir(this, mensagem,
                    erro ? CASE.Utils.TipoFeedback.Erro : CASE.Utils.TipoFeedback.Sucesso);
        }

        private void AjustarArredondamento(object sender, EventArgs e)
        {
            Control controle = (Control)sender;
            int arredondamento;

            if (controle is Panel)
            {
                arredondamento = 12;
            }
            else
            {
                arredondamento = 8;
            }

            VisualUtils.Arredondar(controle, arredondamento);
            controle.Invalidate();
        }

        private void CarregarCategorias(ListBox lista)
        {
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            try
            {
                lista.DisplayMember = "Nome";
                CategoriaService categoriaService = new CategoriaService();
                var categorias = categoriaService.Listar(usuarioAutenticado);

                lista.Items.Clear();
                foreach (Categoria categoria in categorias)
                {
                    lista.Items.Add(categoria);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "Não foi possível carregar as categorias.",
                    "Configurações",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ConfigurarCadastroCategorias(Panel painel, ListBox lista)
        {
            Label mensagem = new Label();
            TextBox nome = new TextBox();
            nome.Name = "txtNomeCategoria";
            nome.MaxLength = 100;
            nome.SetBounds(24, 290, 400, 28);
            Button novo = new Button();
            novo.Text = "Nova";
            novo.SetBounds(434, 286, 100, 36);
            Button salvar = new Button();
            salvar.Text = "Salvar";
            salvar.SetBounds(544, 286, 130, 36);
            novo.Click += (s, e) =>
            {
                lista.ClearSelected();
                nome.Clear();
                nome.Focus();
            };
            lista.SelectedIndexChanged += (s, e) =>
            {
                Categoria categoria = lista.SelectedItem as Categoria;
                if (categoria != null)
                {
                    nome.Text = categoria.Nome;
                }
            };
            salvar.Click += (s, e) =>
            {
                try
                {
                    Categoria anterior = lista.SelectedItem as Categoria;
                    bool existente = anterior != null;
                    Categoria categoria = new Categoria { Nome = nome.Text.Trim() };
                    if (existente)
                        categoria.IdCategoria = anterior.IdCategoria;
                    CategoriaService service = new CategoriaService();
                    if (existente)
                    {
                        if (!service.Atualizar(usuarioAutenticado, categoria))
                            throw new InvalidOperationException("A categoria não foi encontrada. Atualize a lista.");
                    }
                    else
                    {
                        service.Cadastrar(usuarioAutenticado, categoria);
                    }
                    CarregarCategorias(lista);
                    SelecionarCadastroPorNome(lista, categoria.Nome);
                    ExibirResultadoCadastro(mensagem, existente ? "Alterações salvas com sucesso." : "Categoria adicionada com sucesso.");
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro, mensagem);
                }
            };
            painel.Controls.Add(nome);
            painel.Controls.Add(novo);
            painel.Controls.Add(salvar);
            PrepararEditorCadastro(painel, lista, nome, novo, salvar, mensagem, () =>
            {
                Categoria categoria = (Categoria)lista.SelectedItem;
                if (!new CategoriaService().Excluir(usuarioAutenticado, categoria.IdCategoria))
                    throw new InvalidOperationException("A categoria não foi encontrada. Atualize a lista.");
                CarregarCategorias(lista);
            });
            FrontendTheme.Aplicar(painel);
        }

        private Panel CriarCadastroTiposEvento(System.Drawing.Rectangle area)
        {
            Label mensagem = new Label();
            Panel painel = new Panel();
            painel.Name = "panelTiposEvento";
            painel.Bounds = area;
            painel.BackColor = System.Drawing.Color.White;
            ListBox lista = new ListBox();
            lista.DisplayMember = "Nome";
            lista.BackColor = System.Drawing.Color.FromArgb(248, 250, 253);
            lista.ForeColor = VisualUtils.Texto;
            lista.IntegralHeight = false;
            lista.SetBounds(24, 56, 650, 220);
            TextBox nome = new TextBox();
            nome.MaxLength = 100;
            nome.SetBounds(24, 290, 400, 28);
            Button novo = new Button();
            novo.Text = "Novo";
            novo.SetBounds(434, 286, 100, 36);
            Button salvar = new Button();
            salvar.Text = "Salvar";
            salvar.SetBounds(544, 286, 130, 36);
            TipoEventoService service = new TipoEventoService();
            EventHandler carregar = (s, e) =>
            {
                var tipos = service.Listar(usuarioAutenticado);
                lista.Items.Clear();
                foreach (TipoEvento tipo in tipos)
                {
                    lista.Items.Add(tipo);
                }
            };
            lista.SelectedIndexChanged += (s, e) =>
            {
                TipoEvento tipo = lista.SelectedItem as TipoEvento;
                if (tipo != null)
                {
                    nome.Text = tipo.Nome;
                }
            };
            novo.Click += (s, e) =>
            {
                lista.ClearSelected();
                nome.Clear();
                nome.Focus();
            };
            salvar.Click += (s, e) =>
            {
                try
                {
                    TipoEvento anterior = lista.SelectedItem as TipoEvento;
                    bool existente = anterior != null;
                    TipoEvento tipo = new TipoEvento { Nome = nome.Text.Trim() };
                    if (existente)
                        tipo.IdTipoEvento = anterior.IdTipoEvento;
                    if (existente)
                    {
                        if (!service.Atualizar(usuarioAutenticado, tipo))
                            throw new InvalidOperationException("O tipo de evento não foi encontrado. Atualize a lista.");
                    }
                    else
                    {
                        service.Cadastrar(usuarioAutenticado, tipo);
                    }
                    carregar(this, EventArgs.Empty);
                    SelecionarCadastroPorNome(lista, tipo.Nome);
                    ExibirResultadoCadastro(mensagem, existente ? "Alterações salvas com sucesso." : "Tipo de evento adicionado com sucesso.");
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro, mensagem);
                }
            };
            painel.Controls.Add(FrontendTheme.Legenda("Tipos de evento", new System.Drawing.Rectangle(24, 20, 500, 30)));
            painel.Controls.Add(lista);
            painel.Controls.Add(nome);
            painel.Controls.Add(novo);
            painel.Controls.Add(salvar);
            PrepararEditorCadastro(painel, lista, nome, novo, salvar, mensagem, () =>
            {
                TipoEvento tipo = (TipoEvento)lista.SelectedItem;
                if (!service.Excluir(usuarioAutenticado, tipo.IdTipoEvento))
                    throw new InvalidOperationException("O tipo de evento não foi encontrado. Atualize a lista.");
                carregar(this, EventArgs.Empty);
            });
            FrontendTheme.Aplicar(painel);
            if (administrador)
            {
                try
                {
                    carregar(this, EventArgs.Empty);
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro);
                }
            }
            return painel;
        }

        private Panel CriarConfiguracaoInstituicao(System.Drawing.Rectangle area)
        {
            Panel painel = new Panel();
            painel.Name = "panelInstituicao";
            painel.Bounds = area;
            painel.BackColor = System.Drawing.Color.White;
            painel.Controls.Add(FrontendTheme.Legenda("1. Instituição e estoque central", new System.Drawing.Rectangle(24, 16, 600, 30)));
            string[] titulos = { "Nome da instituição *", "CPF/CNPJ (opcional)", "Telefone (opcional)", "E-mail (opcional)" };
            int[] limites = { 150, 18, 20, 150 };
            TextBox[] campos = new TextBox[4];
            for (int i = 0; i < campos.Length; i++)
            {
                painel.Controls.Add(FrontendTheme.Legenda(titulos[i], new System.Drawing.Rectangle(24, 48 + i * 56, 600, 22)));
                campos[i] = new TextBox();
                campos[i].MaxLength = limites[i];
                campos[i].SetBounds(24, 71 + i * 56, 650, 25);
                campos[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                painel.Controls.Add(campos[i]);
                EnvolverCampoArredondado(painel, campos[i], "panelCampoInstituicao" + i);
            }
            Button salvar = new Button();
            salvar.Text = "Salvar instituição";
            salvar.SetBounds(24, 292, 200, 36);
            Button central = new Button();
            central.Text = "Inicializar estoque central";
            central.SetBounds(240, 292, 270, 36);
            central.Visible = false;
            var situacaoCentral = new Label
            {
                Name = "lblSituacaoEstoqueCentral",
                Bounds = new System.Drawing.Rectangle(240, 292, 320, 36),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(29, 125, 96),
                BackColor = System.Drawing.Color.FromArgb(233, 247, 241),
                Visible = false
            };
            Label mensagem = FrontendTheme.Legenda("Salve a instituição e inicialize seu estoque central uma única vez.",
                new System.Drawing.Rectangle(24, 334, 650, 23));
            Instituicao atual = null;
            ConfiguracaoInstitucionalService service = new ConfiguracaoInstitucionalService();
            Action atualizarCentral = () =>
            {
                central.Visible = false;
                situacaoCentral.Visible = false;
                if (!administrador) return;
                try
                {
                    bool configurado = service.EstoqueCentralConfigurado(usuarioAutenticado);
                    situacaoCentral.Text = "Estoque central configurado ✓";
                    situacaoCentral.Visible = configurado;
                    central.Visible = !configurado;
                    central.Enabled = !configurado && atual != null;
                    mensagem.Text = configurado
                        ? "Para adicionar produtos e quantidades, acesse Estoque > Registrar entrada."
                        : atual == null ? "Salve a instituição para habilitar a criação do estoque central."
                            : "Crie o estoque central uma única vez. Depois, registre as entradas em Estoque.";
                }
                catch (Exception erro)
                {
                    mensagem.Text = "Não foi possível confirmar a configuração do estoque central. Abra esta aba novamente para tentar.";
                    System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " ao consultar a configuração do estoque central.");
                }
            };
            if (administrador)
            {
                try
                {
                    atual = service.ObterInstituicao(usuarioAutenticado);
                    if (atual != null)
                    {
                        campos[0].Text = atual.Nome;
                        campos[1].Text = atual.Documento;
                        campos[2].Text = atual.Telefone;
                        campos[3].Text = atual.Email;
                    }
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro);
                }
            }
            salvar.Enabled = administrador;
            central.Enabled = administrador;
            salvar.Click += (s, e) =>
            {
                try
                {
                    Instituicao item = new Instituicao();
                    if (atual != null)
                    {
                        item.IdInstituicao = atual.IdInstituicao;
                    }
                    item.Nome = campos[0].Text;
                    item.Documento = campos[1].Text;
                    item.Telefone = campos[2].Text;
                    item.Email = campos[3].Text;
                    item.IdInstituicao = service.SalvarInstituicao(usuarioAutenticado, item);
                    atual = item;
                    atualizarCentral();
                    FeedbackUsuario.Exibir(this, "Dados da instituição salvos.", TipoFeedback.Sucesso);
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro);
                }
            };
            central.Click += (s, e) =>
            {
                try
                {
                    service.InicializarCentral(usuarioAutenticado);
                    atualizarCentral();
                    FeedbackUsuario.Exibir(this, "Estoque central configurado. Você já pode registrar entradas em Estoque.", TipoFeedback.Sucesso);
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro);
                }
            };
            painel.Controls.Add(salvar);
            painel.Controls.Add(central);
            painel.Controls.Add(situacaoCentral);
            painel.Controls.Add(mensagem);
            VisualUtils.Arredondar(situacaoCentral, 8);
            atualizarCentral();
            painel.SizeChanged += (s, e) =>
            {
                int largura = painel.ClientSize.Width - 48;
                foreach (TextBox campo in campos)
                    campo.Width = largura;
                salvar.Top = central.Top = 292;
                mensagem.Top = 334;
                salvar.Left = 24;
                central.Left = 240;
                mensagem.Width = largura;
            };
            FrontendTheme.Aplicar(painel);
            return painel;
        }

        private Panel CriarCadastroApoio(System.Drawing.Rectangle area, bool ehTurno)
        {
            Label mensagem = new Label();
            Panel painel = new Panel();
            painel.Name = ehTurno ? "panelTurnos" : "panelFuncoesEquipe";
            painel.Bounds = area;
            painel.BackColor = System.Drawing.Color.White;
            string titulo = "Funções de participação na equipe (não são perfis de acesso)";
            if (ehTurno)
            {
                titulo = "Turnos utilizados nas jornadas e escalas";
            }
            painel.Controls.Add(FrontendTheme.Legenda(titulo, new System.Drawing.Rectangle(24, 16, 650, 30)));
            ListBox lista = new ListBox();
            lista.DisplayMember = "Nome";
            lista.BackColor = System.Drawing.Color.FromArgb(248, 250, 253);
            lista.ForeColor = VisualUtils.Texto;
            lista.IntegralHeight = false;
            lista.SetBounds(24, 56, 650, 220);
            TextBox nome = new TextBox();
            nome.MaxLength = 100;
            if (ehTurno)
            {
                nome.MaxLength = 50;
            }
            nome.SetBounds(24, 290, 400, 28);
            Button novo = new Button();
            novo.Text = "Novo";
            novo.SetBounds(434, 286, 100, 36);
            Button salvar = new Button();
            salvar.Text = "Salvar";
            salvar.SetBounds(544, 286, 130, 36);
            salvar.Enabled = administrador;
            ConfiguracaoInstitucionalService service = new ConfiguracaoInstitucionalService();
            EventHandler carregar = (s, e) =>
            {
                var turnos = ehTurno ? service.ListarTurnos(usuarioAutenticado) : null;
                var funcoes = ehTurno ? null : service.ListarFuncoesEquipe(usuarioAutenticado);
                lista.Items.Clear();
                if (ehTurno)
                {
                    foreach (Turno item in turnos)
                    {
                        lista.Items.Add(item);
                    }
                }
                else
                {
                    foreach (Perfil item in funcoes)
                    {
                        lista.Items.Add(item);
                    }
                }
            };
            lista.SelectedIndexChanged += (s, e) => nome.Text = lista.GetItemText(lista.SelectedItem);
            novo.Click += (s, e) =>
            {
                lista.ClearSelected();
                nome.Clear();
                nome.Focus();
            };
            salvar.Click += (s, e) =>
            {
                try
                {
                    bool existente = lista.SelectedItem != null;
                    string nomeSalvo = nome.Text.Trim();
                    if (ehTurno)
                    {
                        Turno anterior = lista.SelectedItem as Turno;
                        Turno item = new Turno();
                        if (anterior != null)
                        {
                            item.IdTurno = anterior.IdTurno;
                        }
                        item.Nome = nome.Text.Trim();
                        service.SalvarTurno(usuarioAutenticado, item);
                    }
                    else
                    {
                        Perfil anterior = lista.SelectedItem as Perfil;
                        Perfil item = new Perfil();
                        if (anterior != null)
                        {
                            item.IdPerfil = anterior.IdPerfil;
                        }
                        item.Nome = nome.Text.Trim();
                        service.SalvarFuncaoEquipe(usuarioAutenticado, item);
                    }
                    carregar(this, EventArgs.Empty);
                    SelecionarCadastroPorNome(lista, nomeSalvo);
                    ExibirResultadoCadastro(mensagem, existente ? "Alterações salvas com sucesso." : "Cadastro adicionado com sucesso.");
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro, mensagem);
                }
            };
            painel.Controls.Add(lista);
            painel.Controls.Add(nome);
            painel.Controls.Add(novo);
            painel.Controls.Add(salvar);
            PrepararEditorCadastro(painel, lista, nome, novo, salvar, mensagem, () =>
            {
                bool excluiu = ehTurno
                    ? service.ExcluirTurno(usuarioAutenticado, ((Turno)lista.SelectedItem).IdTurno)
                    : service.ExcluirFuncaoEquipe(usuarioAutenticado, ((Perfil)lista.SelectedItem).IdPerfil);
                if (!excluiu)
                    throw new InvalidOperationException("O cadastro não foi encontrado. Atualize a lista.");
                carregar(this, EventArgs.Empty);
            });
            FrontendTheme.Aplicar(painel);
            if (administrador)
            {
                try
                {
                    carregar(this, EventArgs.Empty);
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro);
                }
            }
            return painel;
        }

        private void MostrarErroCadastro(Exception erro, Label resultado = null)
        {
            string mensagem = "Não foi possível salvar ou consultar este cadastro.";
            if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
            {
                mensagem = CASE.Utils.FeedbackUsuario.MensagemErro(erro, mensagem);
            }
            var erroBanco = erro as MySql.Data.MySqlClient.MySqlException;
            if (erroBanco != null && erroBanco.Number == 1062)
                mensagem = "Já existe um cadastro com esse nome. Escolha outro nome.";
            else if (erroBanco != null && erroBanco.Number == 1451)
                mensagem = "Este cadastro está em uso e não pode ser excluído. Os dados existentes foram preservados.";
            if (resultado != null)
            {
                resultado.ForeColor = System.Drawing.Color.Firebrick;
                resultado.Text = mensagem;
                CASE.Utils.FeedbackUsuario.Exibir(this, mensagem, CASE.Utils.TipoFeedback.Erro);
                return;
            }
            MessageBox.Show(this, mensagem, "Configurações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void SelecionarCadastroPorNome(ListBox lista, string nome)
        {
            foreach (object item in lista.Items)
                if (string.Equals(lista.GetItemText(item), nome, StringComparison.Ordinal))
                {
                    lista.SelectedItem = item;
                    return;
                }
        }

        private static void ExibirResultadoCadastro(Label mensagem, string texto)
        {
            mensagem.ForeColor = System.Drawing.Color.FromArgb(43, 133, 104);
            mensagem.Text = texto;
        }

        private void PrepararEditorCadastro(Panel painel, ListBox lista, TextBox nome,
            Button novo, Button salvar, Label mensagem, Action remover)
        {
            string chave = painel.Name.Replace("panel", string.Empty);
            painel.Height = 420;
            lista.Name = "list" + chave;
            lista.BorderStyle = BorderStyle.None;
            lista.Font = new System.Drawing.Font("Segoe UI", 10F);
            lista.SetBounds(24, 56, 650, 224);
            if (string.IsNullOrEmpty(nome.Name))
                nome.Name = "txtNome" + chave;
            nome.SetBounds(24, 316, 330, 38);
            Panel campoNome = EnvolverCampoArredondado(painel, nome, "panelCampo" + chave);
            Label legendaNome = FrontendTheme.Legenda("Nome *", new System.Drawing.Rectangle(24, 290, 330, 22));
            legendaNome.Name = "lblNomeCadastro" + chave;
            painel.Controls.Add(legendaNome);
            novo.Name = "btnNovo" + chave;
            novo.SetBounds(364, 312, 90, 36);
            salvar.Name = "btnSalvar" + chave;
            salvar.SetBounds(464, 312, 110, 36);
            salvar.BackColor = VisualUtils.Azul;
            salvar.ForeColor = System.Drawing.Color.White;
            novo.Enabled = administrador;
            var excluir = new Button
            {
                Name = "btnExcluir" + chave, Text = "Excluir",
                ForeColor = System.Drawing.Color.Firebrick, BackColor = System.Drawing.Color.White
            };
            excluir.SetBounds(584, 312, 90, 36);
            painel.Controls.Add(excluir);
            nome.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mensagem.Name = "lblResultado" + chave;
            mensagem.SetBounds(24, 362, 650, 44);
            mensagem.Font = new System.Drawing.Font("Segoe UI", 9F);
            mensagem.ForeColor = VisualUtils.TextoSuave;
            mensagem.Text = "Digite o nome e clique em Adicionar. Para editar ou excluir, selecione um item da lista.";
            painel.Controls.Add(mensagem);
            Action atualizarAcoes = () =>
            {
                bool selecionado = lista.SelectedItem != null;
                salvar.Text = selecionado ? "Salvar" : "Adicionar";
                salvar.Enabled = administrador && !string.IsNullOrWhiteSpace(nome.Text);
                excluir.Enabled = administrador && selecionado;
            };
            Action organizarEditor = () =>
            {
                int larguraPainel = painel.ClientSize.Width;
                int xNovo = larguraPainel - 24 - 282;
                lista.SetBounds(24, 56, larguraPainel - 48, 224);
                legendaNome.SetBounds(24, 290, Math.Max(200, xNovo - 36), 22);
                campoNome.SetBounds(24, 312, Math.Max(200, xNovo - 36), 38);
                novo.SetBounds(xNovo, 312, 90, 40);
                salvar.SetBounds(xNovo + 96, 312, 96, 40);
                excluir.SetBounds(xNovo + 198, 312, 90, 40);
                mensagem.SetBounds(24, 362, larguraPainel - 48, 44);
            };
            painel.SizeChanged += (s, e) => organizarEditor();
            organizarEditor();
            lista.SelectedIndexChanged += (s, e) => atualizarAcoes();
            nome.TextChanged += (s, e) => atualizarAcoes();
            novo.Click += (s, e) =>
            {
                mensagem.ForeColor = VisualUtils.TextoSuave;
                mensagem.Text = "Novo cadastro: informe o nome e clique em Adicionar.";
                atualizarAcoes();
            };
            excluir.Click += (s, e) =>
            {
                if (lista.SelectedItem == null)
                    return;
                if (MessageBox.Show(this, "Excluir o cadastro “" + lista.GetItemText(lista.SelectedItem) + "”?\n" +
                    "Cadastros utilizados por outros registros não podem ser excluídos.", "Excluir cadastro",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;
                excluir.Enabled = false;
                try
                {
                    remover();
                    nome.Clear();
                    ExibirResultadoCadastro(mensagem, "Cadastro excluído com sucesso.");
                }
                catch (Exception erro)
                {
                    MostrarErroCadastro(erro, mensagem);
                }
                finally
                {
                    atualizarAcoes();
                }
            };
            foreach (Button botao in new[] { novo, salvar, excluir })
            {
                botao.CausesValidation = false;
                VisualUtils.EstilizarBotao(botao);
            }
            VisualUtils.Arredondar(painel, 12);
            VisualUtils.AplicarBorda(painel);
            atualizarAcoes();
        }
    }
}

