using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;
using MySql.Data.MySqlClient;

namespace CASE
{
    public partial class LoginForm : Form
    {
        private readonly AutenticacaoService autenticacaoService = new AutenticacaoService();
        private bool autenticando;

        public LoginForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            IconoAplicacao.Aplicar(this);

            VisualUtils.Arredondar(panelLogin, 15);
            btnEntrar.Click += BtnEntrar_Click;
            btnEntrar.CausesValidation = false;
            AcceptButton = btnEntrar;
            VisualUtils.Arredondar(btnEntrar, 7);
            VisualUtils.Arredondar(panelUsuario, 7);
            VisualUtils.Arredondar(panelSenha, 7);
            VisualUtils.AplicarBorda(panelUsuario);
            VisualUtils.AplicarBorda(panelSenha);

            PrepararCampoLogin(textUsuario, panelUsuario, "Seu nome de usuário");
            PrepararCampoLogin(textSenha, panelSenha, "Sua senha");
            textSenha.UseSystemPasswordChar = true;
            textUsuario.AccessibleName = "Usuário";
            textUsuario.AccessibleDescription = "Nome de usuário da conta CASE";
            textSenha.AccessibleName = "Senha";
            textUsuario.TabIndex = 0;
            textSenha.TabIndex = 1;
            panelUsuario.TabIndex = 0;
            panelSenha.TabIndex = 1;
            btnEntrar.TabIndex = 2;
            linkLabel1.TabIndex = 3;
            lblMensagem.SetBounds(60, 354, 300, 52);
            linkLabel1.Top = 418;
            Shown += (s, e) => textUsuario.Focus();
        }

        private static void PrepararCampoLogin(TextBox campo, Panel painel, string placeholder)
        {
            painel.Padding = new Padding(12, 2, 12, 2);
            painel.Cursor = Cursors.IBeam;
            campo.Dock = DockStyle.Fill;
            campo.Margin = Padding.Empty;
            campo.Font = new Font("Segoe UI", 10F);
            VisualUtils.EstilizarCampo(campo);
            VisualUtils.DefinirPlaceholder(campo, placeholder);
        }

        private async void BtnEntrar_Click(object sender, EventArgs e)
        {
            if (autenticando)
            {
                return;
            }
            DefinirMensagem(string.Empty);

            if (string.IsNullOrWhiteSpace(textUsuario.Text))
            {
                DefinirMensagem("Informe o login.");
                textUsuario.Focus();
                return;
            }

            if (string.IsNullOrEmpty(textSenha.Text))
            {
                DefinirMensagem("Informe a senha.");
                textSenha.Focus();
                return;
            }

            string login = textUsuario.Text;
            string senha = textSenha.Text;
            DefinirEstadoLogin(true);
            DefinirMensagem("Verificando acesso…", true);
            try
            {
                Usuario usuario = await Task.Run(() => autenticacaoService.Autenticar(login, senha));
                senha = null;

                if (IsDisposed)
                {
                    autenticacaoService.EncerrarSessao();
                    return;
                }

                if (usuario == null)
                {
                    DefinirMensagem("Não foi possível autenticar com os dados informados.");
                    textSenha.Clear();
                    return;
                }

                textSenha.Clear();
                DefinirMensagem("Carregando o sistema…", true);
                btnEntrar.Text = "Abrindo…";
                btnEntrar.Refresh();

                try
                {
                    using (PrincipalForm formularioPrincipal = new PrincipalForm(usuario))
                    {
                        Hide();
                        UseWaitCursor = false;
                        formularioPrincipal.ShowDialog(this);
                    }
                }
                finally
                {
                    autenticacaoService.EncerrarSessao();
                    if (!IsDisposed)
                    {
                        Show();
                        DefinirMensagem(string.Empty);
                        textSenha.Clear();
                    }
                }
            }
            catch (MySqlException erro)
            {
                RegistrarErroDeDesenvolvimento(erro);
                if (!IsDisposed)
                {
                    DefinirMensagem("Não foi possível entrar agora. Tente novamente em alguns instantes.");
                    textSenha.Clear();
                }
            }
            catch (Exception erro)
            {
                RegistrarErroDeDesenvolvimento(erro);
                if (!IsDisposed)
                {
                    DefinirMensagem("Não foi possível concluir o login.");
                    textSenha.Clear();
                }
            }
            finally
            {
                senha = null;
                autenticando = false;
                if (!IsDisposed)
                {
                    DefinirEstadoLogin(false);
                    if (string.IsNullOrEmpty(textSenha.Text))
                        textSenha.Focus();
                }
            }
        }

        private void DefinirEstadoLogin(bool emAndamento)
        {
            autenticando = emAndamento;
            btnEntrar.Enabled = !emAndamento;
            textUsuario.Enabled = textSenha.Enabled = !emAndamento;
            linkLabel1.Enabled = !emAndamento;
            btnEntrar.Text = emAndamento ? "Entrando…" : "Entrar";
            UseWaitCursor = emAndamento;
        }

        [Conditional("DEBUG")]
        private static void RegistrarErroDeDesenvolvimento(Exception erro)
        {
            if (erro == null)
            {
                return;
            }

            Debug.WriteLine("Falha no login. Tipo: " + erro.GetType().FullName);
            Debug.WriteLine(erro.StackTrace);

            if (erro.InnerException != null)
            {
                Debug.WriteLine("Falha interna. Tipo: " + erro.InnerException.GetType().FullName);
                Debug.WriteLine(erro.InnerException.StackTrace);
            }
        }

        private void DefinirMensagem(string mensagem, bool informativa = false)
        {
            lblMensagem.Text = mensagem;
            lblMensagem.ForeColor = informativa ? VisualUtils.TextoSuave : Color.Firebrick;

            if (string.IsNullOrEmpty(mensagem))
            {
                lblMensagem.Visible = false;
            }
            else
            {
                lblMensagem.Visible = true;
                lblMensagem.BringToFront();
                if (!informativa)
                    CASE.Utils.FeedbackUsuario.Exibir(this, mensagem, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private void panelLogin_Paint(object sender, PaintEventArgs e)
        {
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (CadastroForm formularioCadastro = new CadastroForm())
            {
                formularioCadastro.ShowDialog(this);
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
        }
    }
}
