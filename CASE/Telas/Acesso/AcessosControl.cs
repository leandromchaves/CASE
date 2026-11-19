using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class AcessosControl : UserControl
    {
        public event EventHandler AcessosAlterados;

        private readonly Usuario usuarioAutenticado;
        private readonly UsuarioService usuarioService = new UsuarioService();
        private readonly PessoaService pessoaService = new PessoaService();
        private Usuario selecionado;
        private List<PerfilSistema> perfis = new List<PerfilSistema>();
        private readonly Label lblOrientacaoDecisao = new Label();

        public AcessosControl() : this(null)
        {
        }

        public AcessosControl(Usuario usuario)
        {
            InitializeComponent();

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            usuarioAutenticado = usuario;
            FrontendTheme.Aplicar(this);
            dgvContas.Columns.Remove("col2");
            lblPerfil.Text = "Perfil de acesso";
            lblMotivo.Text = "Justificativa (obrigatória para rejeitar)";
            btnAprovar.Text = "Aprovar cadastro";
            btnRejeitar.Text = "Rejeitar cadastro";
            btnPerfil.Text = "Salvar novo perfil";
            btnPerfil.AccessibleDescription =
                "Altera o perfil de uma conta aprovada. Para contas pendentes, selecione um perfil e use Aprovar cadastro.";
            btnSuspender.Visible = false;
            btnSuspender.TabStop = false;
            ConfigurarOrientacaoDecisao();
            lblModoApresentacao.Visible = false;
            VisualUtils.ExibirEstadoVazio(
                dgvContas,
                "Nenhuma conta encontrada para os filtros selecionados.");
            VisualUtils.AplicarIcone(btnPerfil, Properties.Resources.LucideAcaoAcessos);
            btnRejeitar.ForeColor = System.Drawing.Color.FromArgb(174, 55, 65);
            btnRejeitar.FlatAppearance.MouseOverBackColor =
                System.Drawing.Color.FromArgb(253, 238, 239);

            cmbSituacao.Items.Clear();
            cmbSituacao.Items.Add("Todas");
            cmbSituacao.Items.Add(SituacoesUsuario.Pendente);
            cmbSituacao.Items.Add(SituacoesUsuario.Aprovado);
            cmbSituacao.Items.Add(SituacoesUsuario.Rejeitado);
            cmbSituacao.SelectedIndex = 0;

            cmbPerfil.Items.Clear();
            cmbPerfil.DisplayMember = "Nome";
            cmbSituacao.SelectedIndexChanged += cmbSituacao_SelectedIndexChanged;
            txtBusca.TextChanged += txtBusca_TextChanged;
            VisualUtils.EstilizarPesquisa(txtBusca, "Pesquisar por nome ou login");
            dgvContas.SelectionChanged += dgvContas_SelectionChanged;
            btnAprovar.Click += btnAprovar_Click;
            btnRejeitar.Click += btnRejeitar_Click;
            btnPerfil.Click += btnPerfil_Click;
            cmbPerfil.SelectedIndexChanged += (s, e) => AtualizarBotoes();
            btnSuspender.Enabled = false;
            btnPerfil.Enabled = false;

            if (usuarioAutenticado == null)
            {
                HabilitarLeitura(false);
                lblMensagem.Text = "Entre com uma conta de administrador para gerenciar cadastros.";
                return;
            }

            CarregarDados();
        }

        private void HabilitarLeitura(bool habilitado)
        {
            cmbSituacao.Enabled = habilitado;
            txtBusca.Enabled = habilitado;
            cmbPerfil.Enabled = false;
            txtMotivo.Enabled = false;
            btnSuspender.Enabled = false;
            btnAprovar.Enabled = false;
            btnRejeitar.Enabled = false;
            btnPerfil.Enabled = false;
        }

        private void ConfigurarOrientacaoDecisao()
        {
            lblOrientacaoDecisao.Name = "lblOrientacaoDecisao";
            lblOrientacaoDecisao.SetBounds(24, 112, 936, 24);
            lblOrientacaoDecisao.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblOrientacaoDecisao.ForeColor = VisualUtils.TextoSuave;
            lblOrientacaoDecisao.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblOrientacaoDecisao.AutoEllipsis = true;
            panelDecisao.Controls.Add(lblOrientacaoDecisao);
            lblOrientacaoDecisao.BringToFront();
        }

        private void CarregarDados()
        {
            dgvContas.Rows.Clear();

            try
            {
                perfis = usuarioService.ListarPerfisDeSistema(usuarioAutenticado);
                cmbPerfil.Items.Clear();

                foreach (PerfilSistema perfil in perfis)
                {
                    cmbPerfil.Items.Add(perfil);
                }

                List<Usuario> usuarios = usuarioService.ListarUsuariosAdministrativos(
                    usuarioAutenticado);

                foreach (Usuario usuario in usuarios)
                {
                    Pessoa pessoa = pessoaService.BuscarPorId(
                        usuarioAutenticado,
                        usuario.IdPessoa);
                    string nome = "Pessoa " + usuario.IdPessoa;

                    if (pessoa != null)
                    {
                        nome = pessoa.Nome;
                    }

                    string nomePerfil = ObterNomePerfil(usuario.IdPerfilSistema);
                    int indice = dgvContas.Rows.Add(
                        nome,
                        usuario.Login,
                        usuario.SituacaoConta,
                        nomePerfil,
                        usuario.SolicitadoEm.ToString("dd/MM/yyyy"));
                    dgvContas.Rows[indice].Tag = usuario;
                }

                Selecionar();
                AtualizarBotoes();
                lblMensagem.Text = usuarios.Count + " conta(s) consultada(s).";
            }
            catch (UnauthorizedAccessException)
            {
                HabilitarLeitura(false);
                lblMensagem.Text = "Somente um administrador pode consultar contas.";
            }
            catch (Exception)
            {
                HabilitarLeitura(false);
                lblMensagem.Text = "Não foi possível carregar as contas.";
            }
        }

        private string ObterNomePerfil(int? idPerfilSistema)
        {
            if (!idPerfilSistema.HasValue)
            {
                return "Sem perfil";
            }

            foreach (PerfilSistema perfil in perfis)
            {
                if (perfil.IdPerfilSistema == idPerfilSistema.Value)
                {
                    return perfil.Nome;
                }
            }

            return "Perfil não encontrado";
        }

        private void cmbSituacao_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void txtBusca_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            if (cmbSituacao.SelectedIndex < 0)
            {
                return;
            }

            foreach (DataGridViewRow linha in dgvContas.Rows)
            {
                string situacao = Convert.ToString(linha.Cells["col3"].Value);
                string nome = Convert.ToString(linha.Cells[0].Value);
                string login = Convert.ToString(linha.Cells[1].Value);
                bool situacaoCorresponde = cmbSituacao.SelectedIndex == 0 ||
                    string.Equals(
                        situacao,
                        cmbSituacao.Text,
                        StringComparison.OrdinalIgnoreCase);
                bool buscaCorresponde = (nome + " " + login).IndexOf(
                    txtBusca.Text,
                    StringComparison.CurrentCultureIgnoreCase) >= 0;
                linha.Visible = situacaoCorresponde && buscaCorresponde;
            }

            dgvContas.ClearSelection();
            Selecionar();
        }

        private void dgvContas_SelectionChanged(object sender, EventArgs e)
        {
            Selecionar();
        }

        private void Selecionar()
        {

            selecionado = null;

            if (dgvContas.SelectedRows.Count > 0)
            {
                selecionado = dgvContas.SelectedRows[0].Tag as Usuario;
            }

            if (selecionado != null)
            {
                lblSelecionado.Text = Convert.ToString(
                    dgvContas.SelectedRows[0].Cells[0].Value) + " · " +
                    selecionado.SituacaoConta;
                txtMotivo.Text = string.Empty;
                SelecionarPerfil(selecionado.IdPerfilSistema);

                List<AnaliseUsuario> historico;
                try
                {
                    historico = usuarioService.ListarHistorico(usuarioAutenticado, selecionado.IdUsuario);
                }
                catch (Exception)
                {
                    selecionado = null;
                    HabilitarLeitura(false);
                    lblMensagem.Text = "Não foi possível validar o acesso à conta. Entre novamente para continuar.";
                    return;
                }

                if (historico.Count > 0 && !string.Equals(selecionado.SituacaoConta,
                    SituacoesUsuario.Aprovado, StringComparison.OrdinalIgnoreCase))
                {
                    AnaliseUsuario ultimaAnalise = historico[historico.Count - 1];
                    txtMotivo.Text = ultimaAnalise.Justificativa;
                }
            }
            else
            {
                lblSelecionado.Text = "Selecione uma conta para analisar.";
                txtMotivo.Clear();
                cmbPerfil.SelectedIndex = -1;
            }

            AtualizarBotoes();
        }

        private void SelecionarPerfil(int? idPerfilSistema)
        {
            cmbPerfil.SelectedIndex = -1;

            if (!idPerfilSistema.HasValue)
            {
                return;
            }

            for (int indice = 0; indice < cmbPerfil.Items.Count; indice++)
            {
                PerfilSistema perfil = cmbPerfil.Items[indice] as PerfilSistema;

                if (perfil != null && perfil.IdPerfilSistema == idPerfilSistema.Value)
                {
                    cmbPerfil.SelectedIndex = indice;
                    return;
                }
            }
        }

        private void AtualizarBotoes()
        {

            bool pendente = selecionado != null && string.Equals(
                selecionado.SituacaoConta,
                SituacoesUsuario.Pendente,
                StringComparison.OrdinalIgnoreCase);
            bool aprovado = selecionado != null && string.Equals(selecionado.SituacaoConta,
                SituacoesUsuario.Aprovado, StringComparison.OrdinalIgnoreCase);
            bool propriaConta = selecionado != null && usuarioAutenticado != null &&
                selecionado.IdUsuario == usuarioAutenticado.IdUsuario;
            PerfilSistema perfil = cmbPerfil.SelectedItem as PerfilSistema;
            lblPerfil.Text = pendente
                ? "Perfil de acesso (obrigatório para aprovar)"
                : "Perfil de acesso";
            cmbPerfil.Enabled = pendente || (aprovado && !propriaConta);
            txtMotivo.Enabled = pendente || (aprovado && !propriaConta);
            btnAprovar.Enabled = pendente && perfil != null;
            btnRejeitar.Enabled = pendente;
            btnSuspender.Enabled = false;
            btnPerfil.Enabled = aprovado && !propriaConta && perfil != null &&
                perfil.IdPerfilSistema != selecionado.IdPerfilSistema;

            if (selecionado == null)
            {
                lblOrientacaoDecisao.Text = "Selecione uma conta para ver as ações disponíveis.";
            }
            else if (pendente)
            {
                lblOrientacaoDecisao.Text = perfil == null
                    ? "Obrigatório: selecione o perfil que a conta receberá antes de aprovar."
                    : "Aprovar cadastro atribui o perfil selecionado à conta.";
            }
            else if (aprovado && propriaConta)
            {
                lblOrientacaoDecisao.Text = "O perfil da própria conta não pode ser alterado nesta tela.";
            }
            else if (aprovado)
            {
                lblOrientacaoDecisao.Text = btnPerfil.Enabled
                    ? "Salvar novo perfil registra a alteração; o usuário precisará entrar novamente."
                    : "Para alterar o acesso, selecione um perfil diferente e clique em Salvar novo perfil.";
            }
            else
            {
                lblOrientacaoDecisao.Text = "Aprovação e alteração de perfil ficam disponíveis conforme a situação da conta.";
            }
        }

        private void btnPerfil_Click(object sender, EventArgs e)
        {
            PerfilSistema perfil = cmbPerfil.SelectedItem as PerfilSistema;
            if (selecionado == null || perfil == null || !selecionado.IdPerfilSistema.HasValue)
            {
                lblMensagem.Text = "Selecione uma conta aprovada e o novo perfil.";
                return;
            }
            btnPerfil.Enabled = false;
            try
            {
                usuarioService.AlterarPerfilSistema(usuarioAutenticado, selecionado.IdUsuario,
                    perfil.IdPerfilSistema, selecionado.IdPerfilSistema.Value, txtMotivo.Text);
                AcessosAlterados?.Invoke(this, EventArgs.Empty);
                CarregarDados();
                lblMensagem.Text = "Perfil atualizado e alteração registrada. O usuário deverá entrar novamente.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Sucesso);
            }
            catch (Exception erro)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível alterar o perfil.", lblMensagem);
                AtualizarBotoes();
            }
        }

        private void btnAprovar_Click(object sender, EventArgs e)
        {

            if (selecionado == null || cmbPerfil.SelectedItem == null)
            {
                lblMensagem.Text = "Selecione a conta e o perfil de sistema que será atribuído.";
                return;
            }

            PerfilSistema perfil = (PerfilSistema)cmbPerfil.SelectedItem;
            string nome = NomeSelecionado();
            DialogResult confirmacao = MessageBox.Show(
                this,
                "Confirma a aprovação de \"" + nome + "\"?" + Environment.NewLine +
                Environment.NewLine + "O perfil \"" + perfil.Nome +
                "\" será atribuído e a conta ficará disponível para autenticação.",
                "Confirmar aprovação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirmacao != DialogResult.Yes)
            {
                return;
            }

            try
            {
                usuarioService.AprovarCadastro(
                    usuarioAutenticado,
                    selecionado.IdUsuario,
                    perfil.IdPerfilSistema);
                AcessosAlterados?.Invoke(this, EventArgs.Empty);
                CarregarDados();
                lblMensagem.Text = "Cadastro aprovado e decisão registrada.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Sucesso);
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Sua conta não pode aprovar cadastros.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Erro);
            }
            catch (InvalidOperationException ex)
            {
                CarregarDados();
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível aprovar o cadastro.", lblMensagem);
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível aprovar o cadastro.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private void btnRejeitar_Click(object sender, EventArgs e)
        {

            if (selecionado == null)
            {
                lblMensagem.Text = "Selecione uma conta pendente.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                lblMensagem.Text = "Informe a justificativa antes de rejeitar o cadastro.";
                txtMotivo.Focus();
                return;
            }

            string motivo = txtMotivo.Text.Trim();
            DialogResult confirmacao = MessageBox.Show(
                this,
                "Confirma a rejeição do cadastro de \"" + NomeSelecionado() + "\"?" +
                Environment.NewLine + Environment.NewLine + "Justificativa: " + motivo,
                "Confirmar rejeição",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmacao != DialogResult.Yes)
            {
                return;
            }

            try
            {
                usuarioService.RejeitarCadastro(
                    usuarioAutenticado,
                    selecionado.IdUsuario,
                    txtMotivo.Text);
                AcessosAlterados?.Invoke(this, EventArgs.Empty);
                CarregarDados();
                lblMensagem.Text = "Cadastro rejeitado e decisão registrada.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Sucesso);
            }
            catch (ArgumentException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira a justificativa e tente novamente.", lblMensagem);
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Sua conta não pode rejeitar cadastros.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Erro);
            }
            catch (InvalidOperationException ex)
            {
                CarregarDados();
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível rejeitar o cadastro.", lblMensagem);
            }
            catch (Exception)
            {
                lblMensagem.Text = "Não foi possível rejeitar o cadastro.";
                CASE.Utils.FeedbackUsuario.Exibir(this, lblMensagem.Text, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private string NomeSelecionado()
        {
            if (dgvContas.SelectedRows.Count > 0)
            {
                DataGridViewRow linha = dgvContas.SelectedRows[0];
                string nome = Convert.ToString(linha.Cells[0].Value);
                string login = Convert.ToString(linha.Cells[1].Value);
                return string.IsNullOrWhiteSpace(login)
                    ? nome
                    : nome + " (" + login + ")";
            }

            return selecionado == null ? "conta selecionada" : selecionado.Login;
        }
    }
}
