using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class FornecedoresControl : UserControl
    {
        private FornecedorService fornecedorService = new FornecedorService();
        private List<Fornecedor> fornecedores = new List<Fornecedor>();
        private Fornecedor fornecedorSelecionado;
        private Usuario usuarioAutenticado;
        private ToolTip dicasCampos = new ToolTip();
        private bool salvando;
        private Form formularioFornecedor;
        private Button btnEditarFornecedor = new Button();
        public FornecedoresControl() : this(null)
        {
        }

        public FornecedoresControl(Usuario usuario)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            usuarioAutenticado = usuario;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            FrontendTheme.Aplicar(this);
            ConfigurarJanelaFornecedor();
            VisualUtils.ExibirEstadoVazio(dgvFornecedores, "Nenhum fornecedor encontrado. Altere a busca ou cadastre um novo.");
            dicasCampos.SetToolTip(txtNome, "Exemplo: Mercado São José ou João da Silva");
            dicasCampos.SetToolTip(txtDocumento, "Opcional. Exemplo: CPF ou CNPJ do fornecedor");
            dicasCampos.SetToolTip(txtTelefone, "Opcional. Exemplo: (11) 3333-4444");
            dicasCampos.SetToolTip(txtEmail, "Opcional. Exemplo: contato@fornecedor.com.br");
            Disposed += Tela_Disposed;
            txtNome.MaxLength = 150;
            txtDocumento.MaxLength = 18;
            txtTelefone.MaxLength = 20;
            txtEmail.MaxLength = 150;
            lblMensagem.AutoEllipsis = true;
            pictureIconeTela.AccessibleName = "Ícone de fornecedores";
            lblSubtitulo.Text = "Cadastro compartilhado da instituição. Use Novo fornecedor ou selecione uma linha e clique em Editar.";
            lblBusca.Text = "Buscar por nome, documento, e-mail ou telefone";
            txtBusca.TextChanged += TxtBusca_TextChanged;
            VisualUtils.EstilizarPesquisa(txtBusca, "Pesquisar nome, documento, e-mail ou telefone").Top = 28;
            lblBusca.Visible = false;
            btnNovo.Click += BtnNovo_Click;
            dgvFornecedores.SelectionChanged += DgvFornecedores_SelectionChanged;
            dgvFornecedores.CellDoubleClick += dgvFornecedores_CellDoubleClick;
            btnSalvar.Click += BtnSalvar_Click;
            ConfigurarExclusao();
            if (usuarioAutenticado == null)
            {
                btnNovo.Enabled = false;
                btnEditarFornecedor.Enabled = false;
                btnSalvar.Enabled = false;
                lblMensagem.Text = "Entre com uma conta autorizada para consultar fornecedores.";
                return;
            }

            CarregarFornecedores();
            try
            {
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                if (perfil != "Administrador" && perfil != "Gerente")
                {
                    btnNovo.Enabled = false;
                    btnEditarFornecedor.Enabled = false;
                    btnSalvar.Enabled = false;
                    txtNome.ReadOnly = true;
                    txtDocumento.ReadOnly = true;
                    txtTelefone.ReadOnly = true;
                    txtEmail.ReadOnly = true;
                    lblMensagem.Text = "Seu perfil não pode gerenciar fornecedores.";
                }
                else
                {
                    LimparCampos();
                    lblMensagem.Text = "Administrador e gerente podem cadastrar e editar. Exclusão sem vínculos: apenas administrador, pelo botão direito.";
                }
            }
            catch (Exception)
            {
                btnNovo.Enabled = false;
                btnEditarFornecedor.Enabled = false;
                btnSalvar.Enabled = false;
            }
        }

        private void ConfigurarJanelaFornecedor()
        {
            // Reutiliza os campos do Designer na janela de cadastro.
            panelEdicao.Visible = false;
            btnEditarFornecedor.Name = "btnEditarFornecedor";
            btnEditarFornecedor.Text = "Editar selecionado";
            btnEditarFornecedor.BackColor = btnNovo.BackColor;
            btnEditarFornecedor.ForeColor = btnNovo.ForeColor;
            btnEditarFornecedor.TabIndex = btnNovo.TabIndex + 1;
            btnEditarFornecedor.Click += btnEditarFornecedor_Click;
            panelBusca.Controls.Add(btnEditarFornecedor);
            FrontendTheme.Aplicar(panelBusca);
            SizeChanged += Tela_SizeChanged;
            panelBusca.SizeChanged += panelBusca_SizeChanged;
            AjustarListagemFornecedores();
        }

        private void AjustarListagemFornecedores()
        {
            btnEditarFornecedor.SetBounds(Math.Max(220, panelBusca.ClientSize.Width - 194), 28, 170, 40);
            btnNovo.SetBounds(btnEditarFornecedor.Left - 196, 28, 184, 40);
            Control areaBusca = txtBusca.Parent.Name == "panelPesquisa_" + txtBusca.Name ? txtBusca.Parent : txtBusca;
            areaBusca.Width = Math.Max(120, btnNovo.Left - areaBusca.Left - 20);
            lblBusca.Width = areaBusca.Width;
            panelLista.Height = Math.Max(150, ClientSize.Height - panelLista.Top - 72);
        }

        private void AbrirJanelaFornecedor(bool editar)
        {
            if (formularioFornecedor != null)
            {
                formularioFornecedor.Activate();
                return;
            }

            try
            {
                new AutorizacaoService().ExigirAdministradorOuGerente(usuarioAutenticado);
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Seu perfil não pode cadastrar ou editar fornecedores.";
                FeedbackUsuario.Exibir(this, lblMensagem.Text, TipoFeedback.Erro);
                return;
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível validar sua conta. Tente novamente.";
                return;
            }

            if (editar)
            {
                if (dgvFornecedores.SelectedRows.Count == 0)
                {
                    lblMensagem.Text = "Selecione o fornecedor que deseja editar.";
                    return;
                }

                SelecionarFornecedor();
                if (fornecedorSelecionado == null)
                    return;
            }
            else
                LimparCampos();
            Rectangle limitesPainel = panelEdicao.Bounds;
            Rectangle limitesMensagem = lblMensagem.Bounds;
            AnchorStyles ancoraPainel = panelEdicao.Anchor;
            AnchorStyles ancoraMensagem = lblMensagem.Anchor;
            using (Form janela = new Form())
            using (Button cancelar = new Button())
            {
                janela.Name = "FornecedorForm";
                janela.Text = editar ? "Editar fornecedor" : "Novo fornecedor";
                janela.Font = Font;
                janela.AutoScaleMode = AutoScaleMode.Font;
                janela.ClientSize = new Size(660, 460);
                janela.FormBorderStyle = FormBorderStyle.FixedDialog;
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.MaximizeBox = false;
                janela.MinimizeBox = false;
                janela.ShowInTaskbar = false;
                IconoAplicacao.Aplicar(janela);
                Label titulo = new Label
                {
                    Text = janela.Text,
                    Font = new Font("Segoe UI", 20, FontStyle.Bold),
                    AutoSize = true
                };
                titulo.SetBounds(24, 18, 600, 40);
                janela.Controls.Add(titulo);
                janela.Controls.Add(panelEdicao);
                panelEdicao.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                panelEdicao.SetBounds(24, 74, 612, 310);
                lblNome.SetBounds(24, 18, 564, 24);
                PrepararCampoFormulario(txtNome, new Rectangle(24, 44, 564, 38));
                lblDocumento.SetBounds(24, 92, 270, 24);
                lblTelefone.SetBounds(318, 92, 270, 24);
                PrepararCampoFormulario(txtDocumento, new Rectangle(24, 116, 270, 38));
                PrepararCampoFormulario(txtTelefone, new Rectangle(318, 116, 270, 38));
                lblEmail.SetBounds(24, 164, 564, 24);
                PrepararCampoFormulario(txtEmail, new Rectangle(24, 188, 564, 38));
                btnSalvar.SetBounds(438, 246, 150, 40);
                btnSalvar.Text = editar ? "Salvar alterações" : "Cadastrar";
                panelEdicao.Visible = true;
                janela.Controls.Add(lblMensagem);
                lblMensagem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                lblMensagem.SetBounds(24, 398, 612, 48);
                cancelar.Name = "btnCancelarFornecedor";
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(318, 246, 110, 40);
                panelEdicao.Controls.Add(cancelar);
                txtNome.TabIndex = 0;
                txtDocumento.TabIndex = 1;
                txtEmail.TabIndex = 2;
                txtTelefone.TabIndex = 3;
                btnSalvar.TabIndex = 4;
                cancelar.TabIndex = 5;
                janela.AcceptButton = btnSalvar;
                janela.CancelButton = cancelar;
                FrontendTheme.Aplicar(janela);
                VisualUtils.EstilizarCampo(txtNome);
                VisualUtils.EstilizarCampo(txtDocumento);
                VisualUtils.EstilizarCampo(txtTelefone);
                VisualUtils.EstilizarCampo(txtEmail);
                janela.Shown += janela_Shown;
                formularioFornecedor = janela;
                DialogResult resultado;
                try
                {
                    resultado = janela.ShowDialog(FindForm());
                }
                finally
                {
                    formularioFornecedor = null;
                    panelEdicao.Controls.Remove(cancelar);
                    Controls.Add(panelEdicao);
                    panelEdicao.Bounds = limitesPainel;
                    panelEdicao.Anchor = ancoraPainel;
                    panelEdicao.Visible = false;
                    Controls.Add(lblMensagem);
                    lblMensagem.Bounds = limitesMensagem;
                    lblMensagem.Anchor = ancoraMensagem;
                }

                if (resultado == DialogResult.OK)
                {
                    CarregarFornecedores();
                    lblMensagem.Text = "Fornecedor salvo com sucesso.";
                }
                else
                    lblMensagem.Text = "Edição encerrada. Nenhuma alteração foi salva.";
            }
        }

        private void PrepararCampoFormulario(TextBox campo, Rectangle limites)
        {
            Panel painel = campo.Parent as Panel;
            bool reutilizarPainel = painel != null && painel.Parent == panelEdicao;
            if (!reutilizarPainel)
            {
                Control paiAnterior = campo.Parent;
                if (paiAnterior != null)
                    paiAnterior.Controls.Remove(campo);
                painel = new Panel
                {
                    Name = "painel" + campo.Name,
                    BackColor = Color.White,
                    Cursor = Cursors.IBeam
                };
                painel.Controls.Add(campo);
                panelEdicao.Controls.Add(painel);
            }

            painel.Bounds = limites;
            painel.Padding = new Padding(12, 2, 12, 2);
            campo.Dock = DockStyle.Fill;
            campo.Margin = Padding.Empty;
            campo.Font = new Font("Segoe UI", 10F);
            VisualUtils.Arredondar(painel, 7);
            VisualUtils.AplicarBorda(painel);
            VisualUtils.EstilizarCampo(campo);
        }

        private void ConfigurarExclusao()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem excluir = new ToolStripMenuItem("Excluir fornecedor sem vínculos");
            excluir.Name = "menuExcluirFornecedor";
            excluir.Click += ExcluirFornecedor_Click;
            menu.Items.Add(excluir);
            menu.Opening += Tratar_menu_Opening;
            dgvFornecedores.ContextMenuStrip = menu;
            dgvFornecedores.CellMouseDown += dgvFornecedores_CellMouseDown;
            Disposed += Tratar_Disposed;
            void Tratar_menu_Opening(object s, CancelEventArgs e)
            {
                excluir.Enabled = false;
                if (usuarioAutenticado == null || dgvFornecedores.SelectedRows.Count == 0)
                {
                    return;
                }

                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                    excluir.Enabled = perfil == "Administrador";
                }
                catch (Exception erro)
                {
                    Trace.WriteLine(erro.GetType().Name + " em MenuExcluirFornecedor");
                }
            }

            void Tratar_Disposed(object s, EventArgs e)
            {
                menu.Dispose();
            }
        }

        private void ExcluirFornecedor_Click(object sender, EventArgs e)
        {
            if (dgvFornecedores.SelectedRows.Count == 0)
            {
                lblMensagem.Text = "Selecione um fornecedor para excluir.";
                return;
            }

            Fornecedor fornecedor = dgvFornecedores.SelectedRows[0].Tag as Fornecedor;
            if (fornecedor == null)
            {
                return;
            }

            if (MessageBox.Show(this, "Excluir definitivamente o fornecedor selecionado? A exclusão só é permitida sem vínculos.", "Excluir fornecedor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                bool excluido = fornecedorService.Excluir(usuarioAutenticado, fornecedor.IdFornecedor);
                CarregarFornecedores();
                LimparCampos();
                if (excluido)
                {
                    lblMensagem.Text = "Fornecedor excluído.";
                    FeedbackUsuario.Exibir(this, lblMensagem.Text, TipoFeedback.Sucesso);
                }
                else
                {
                    lblMensagem.Text = "O fornecedor não existe mais. A lista foi atualizada.";
                }
            }
            catch (Exception erro)
            {
                if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
                {
                    lblMensagem.Text = FeedbackUsuario.MensagemErro(erro, "Não foi possível excluir o fornecedor.");
                }
                else
                {
                    lblMensagem.Text = "Não foi possível excluir o fornecedor. Tente novamente.";
                    FeedbackUsuario.Exibir(this, lblMensagem.Text, TipoFeedback.Erro);
                }

                Trace.WriteLine(erro.GetType().Name + " em ExcluirFornecedor: " + erro.StackTrace);
            }
        }

        private void CarregarFornecedores()
        {
            try
            {
                fornecedores = fornecedorService.Listar(usuarioAutenticado);
                AtualizarLista();
            }
            catch (UnauthorizedAccessException)
            {
                btnNovo.Enabled = false;
                btnSalvar.Enabled = false;
                lblMensagem.Text = "Seu perfil não pode consultar fornecedores.";
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível carregar os fornecedores.";
            }
        }

        private void AtualizarLista()
        {
            dgvFornecedores.Rows.Clear();
            string textoBusca = txtBusca.Text.Trim();
            foreach (Fornecedor fornecedor in fornecedores)
            {
                string textoFornecedor = fornecedor.Nome;
                if (fornecedor.Documento != null)
                {
                    textoFornecedor += " " + fornecedor.Documento;
                }

                if (fornecedor.Email != null)
                {
                    textoFornecedor += " " + fornecedor.Email;
                }

                if (fornecedor.Telefone != null)
                {
                    textoFornecedor += " " + fornecedor.Telefone;
                }

                bool correspondeBusca = textoFornecedor.IndexOf(textoBusca, StringComparison.CurrentCultureIgnoreCase) >= 0;
                if (!correspondeBusca)
                {
                    continue;
                }

                int indiceLinha = dgvFornecedores.Rows.Add(fornecedor.Nome, fornecedor.Documento, fornecedor.Email, fornecedor.Telefone);
                dgvFornecedores.Rows[indiceLinha].Tag = fornecedor;
            }

            dgvFornecedores.ClearSelection();
        }

        private void LimparCampos()
        {
            fornecedorSelecionado = null;
            txtNome.Clear();
            txtDocumento.Clear();
            txtEmail.Clear();
            txtTelefone.Clear();
            dgvFornecedores.ClearSelection();
            lblMensagem.Text = "Novo fornecedor. Informe o nome e os contatos disponíveis.";
            btnSalvar.Text = "Cadastrar fornecedor";
        }

        private void SelecionarFornecedor()
        {
            if (dgvFornecedores.SelectedRows.Count == 0)
            {
                return;
            }

            fornecedorSelecionado = dgvFornecedores.SelectedRows[0].Tag as Fornecedor;
            if (fornecedorSelecionado == null)
            {
                return;
            }

            txtNome.Text = fornecedorSelecionado.Nome;
            txtDocumento.Text = fornecedorSelecionado.Documento;
            txtEmail.Text = fornecedorSelecionado.Email;
            txtTelefone.Text = fornecedorSelecionado.Telefone;
            lblMensagem.Text = "Editando " + fornecedorSelecionado.Nome;
            btnSalvar.Text = "Salvar alterações";
        }

        private void SalvarFornecedor()
        {
            if (salvando)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                lblMensagem.Text = "Informe o nome do fornecedor.";
                return;
            }

            Fornecedor fornecedor = new Fornecedor();
            bool novoFornecedor = fornecedorSelecionado == null;
            if (novoFornecedor)
            {
                fornecedor = new Fornecedor();
            }
            else
            {
                fornecedor.IdFornecedor = fornecedorSelecionado.IdFornecedor;
            }

            fornecedor.Nome = txtNome.Text.Trim();
            fornecedor.Documento = txtDocumento.Text.Trim();
            fornecedor.Email = txtEmail.Text.Trim();
            fornecedor.Telefone = txtTelefone.Text.Trim();
            try
            {
                salvando = true;
                btnSalvar.Enabled = false;
                if (novoFornecedor)
                {
                    fornecedorService.Cadastrar(usuarioAutenticado, fornecedor);
                }
                else
                {
                    fornecedorService.Atualizar(usuarioAutenticado, fornecedor);
                }

                lblMensagem.Text = "Fornecedor salvo com sucesso.";
                if (formularioFornecedor != null)
                    formularioFornecedor.DialogResult = DialogResult.OK;
            }
            catch (ArgumentException erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Confira os dados do fornecedor e tente novamente.", lblMensagem);
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Seu perfil não pode cadastrar ou editar fornecedores.";
            }
            catch (InvalidOperationException erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível salvar o fornecedor.", lblMensagem);
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível salvar o fornecedor.";
                FeedbackUsuario.Exibir(this, lblMensagem.Text, TipoFeedback.Erro);
            }
            finally
            {
                salvando = false;
                try
                {
                    new AutorizacaoService().ExigirAdministradorOuGerente(usuarioAutenticado);
                    btnSalvar.Enabled = true;
                }
                catch
                {
                    btnSalvar.Enabled = false;
                }
            }
        }

        private void TxtBusca_TextChanged(object sender, EventArgs e)
        {
            AtualizarLista();
        }

        public void Pesquisar(string texto)
        {
            if (texto == null)
            {
                texto = string.Empty;
            }

            txtBusca.Text = texto;
        }

        private void BtnNovo_Click(object sender, EventArgs e)
        {
            AbrirJanelaFornecedor(false);
        }

        private void DgvFornecedores_SelectionChanged(object sender, EventArgs e)
        {
            SelecionarFornecedor();
        }

        private void BtnSalvar_Click(object sender, EventArgs e)
        {
            SalvarFornecedor();
        }

        private void Tela_Disposed(object s, EventArgs e)
        {
            dicasCampos.Dispose();
        }

        private void dgvFornecedores_CellDoubleClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                AbrirJanelaFornecedor(true);
        }

        private void btnEditarFornecedor_Click(object s, EventArgs e)
        {
            AbrirJanelaFornecedor(true);
        }

        private void Tela_SizeChanged(object s, EventArgs e)
        {
            AjustarListagemFornecedores();
        }

        private void panelBusca_SizeChanged(object s, EventArgs e)
        {
            AjustarListagemFornecedores();
        }

        private void janela_Shown(object s, EventArgs e)
        {
            txtNome.Focus();
        }

        private void dgvFornecedores_CellMouseDown(object s, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvFornecedores.CurrentCell = dgvFornecedores.Rows[e.RowIndex].Cells[0];
                dgvFornecedores.Rows[e.RowIndex].Selected = true;
            }
        }
    }
}
