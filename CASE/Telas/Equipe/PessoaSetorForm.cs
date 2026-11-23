using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class PessoaSetorForm : Form
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento evento;
        private readonly EquipeEvento vinculo;
        private readonly EquipeEventoService equipeService = new EquipeEventoService();
        private ComboBox cmbMembroAprovado;
        private readonly Label avisoListaVazia = new Label();

        public PessoaSetorForm() : this(null, null, null)
        {
        }

        public PessoaSetorForm(Usuario usuario, Evento evento, EquipeEvento item)
        {
            InitializeComponent();
            IconoAplicacao.Aplicar(this);
            FrontendTheme.Aplicar(this);

            usuarioAutenticado = usuario;
            this.evento = evento;
            vinculo = item;
            labelAvisoFormulario.Text = string.Empty;
            txtNomeFuncionario.ReadOnly = true;

            lblPerfildeacesso.Text = "Perfil neste evento";
            lblSetorbarraca.Text = "Setor (definido nas escalas)";
            lblTurno.Text = "Turno (definido nas escalas)";
            cmbSetorFuncionario.Enabled = false;
            cmbTurnoFuncionario.Enabled = false;
            cmbSetorFuncionario.Items.Clear();
            cmbTurnoFuncionario.Items.Clear();
            cmbPerfilFuncionario.Items.Clear();
            cmbSetorFuncionario.Visible = lblSetorbarraca.Visible = false;
            cmbTurnoFuncionario.Visible = lblTurno.Visible = false;
            lblUsuario.Top = 140;
            lblUsuario.Text = "Usuário (preenchido automaticamente)";
            Panel painelUsuario = txtLoginFuncionario.Parent as Panel;
            if (painelUsuario == null || painelUsuario.Controls.Count != 1)
                painelUsuario = new Panel();
            painelUsuario.Name = "panelUsuarioFuncionario";
            painelUsuario.BackColor = VisualUtils.Fundo;
            painelUsuario.SetBounds(28, 164, 550, 38);
            if (painelUsuario.Parent != null)
                painelUsuario.Parent.Controls.Remove(painelUsuario);
            painelUsuario.Controls.Remove(txtLoginFuncionario);
            painelUsuario.Controls.Add(txtLoginFuncionario);
            panelFormulario.Controls.Add(painelUsuario);
            txtLoginFuncionario.BorderStyle = BorderStyle.None;
            txtLoginFuncionario.BackColor = painelUsuario.BackColor;
            txtLoginFuncionario.SetBounds(12, 8, 526, 23);
            txtLoginFuncionario.TabStop = false;
            VisualUtils.EstilizarCampo(txtLoginFuncionario);
            VisualUtils.Arredondar(painelUsuario, 8);
            VisualUtils.AplicarBorda(painelUsuario);
            lblPerfildeacesso.Top = 210;
            cmbPerfilFuncionario.Top = 234;
            chkFuncionarioAtivo.Top = 360;
            var orientacao = new Label
            {
                Name = "lblOrientacaoEscalas", Text = "O vínculo inclui a pessoa na equipe do evento.\n" +
                    "Depois, use Escala de trabalho para definir setor, data e turno.",
                ForeColor = VisualUtils.TextoSuave, BackColor = VisualUtils.Fundo,
                Padding = new Padding(10), Font = new System.Drawing.Font("Segoe UI", 9F)
            };
            orientacao.SetBounds(28, 272, 550, 70);
            panelFormulario.Controls.Add(orientacao);
            VisualUtils.Arredondar(orientacao, 8);

            if (usuarioAutenticado == null || this.evento == null)
            {
                btnSalvarFuncionario.Enabled = false;
                labelAvisoFormulario.Text = "Selecione um evento e entre com uma conta autorizada.";
                return;
            }

            CarregarPerfis();

            if (vinculo == null)
            {
                Text = lblTitulo.Text = "Vincular membro ao evento";
                ConfigurarMembrosAprovados();
                txtLoginFuncionario.Leave += txtLoginFuncionario_Leave;
            }
            else
            {
                CarregarVinculo();
            }
        }

        private void ConfigurarMembrosAprovados()
        {
            lblNomecompleto.Text = "Membro aprovado para adicionar à equipe";
            txtNomeFuncionario.Visible = false;
            Panel contornoNome = txtNomeFuncionario.Parent as Panel;
            if (contornoNome != null && contornoNome.Controls.Count == 1 &&
                contornoNome.Controls.Contains(txtNomeFuncionario))
            {
                contornoNome.Visible = false;
            }
            txtLoginFuncionario.ReadOnly = true;
            chkFuncionarioAtivo.Checked = true;
            chkFuncionarioAtivo.Enabled = false;
            chkFuncionarioAtivo.Text = "O vínculo será criado como ativo";
            cmbMembroAprovado = new ComboBox
            {
                Name = "cmbMembroAprovado",
                DropDownStyle = ComboBoxStyle.DropDownList,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                FormattingEnabled = true,
                AccessibleName = "Membros aprovados disponíveis",
                TabIndex = txtNomeFuncionario.TabIndex
            };
            cmbMembroAprovado.Format += (s, e) =>
            {
                Pessoa pessoa = e.ListItem as Pessoa;
                e.Value = pessoa == null ? Convert.ToString(e.ListItem) : pessoa.Nome;
            };
            cmbMembroAprovado.SetBounds(28, 94, 440, 38);
            panelFormulario.Controls.Add(cmbMembroAprovado);
            VisualUtils.EstilizarSelecao(cmbMembroAprovado);
            cmbMembroAprovado.FlatStyle = FlatStyle.Standard;
            cmbMembroAprovado.BackColor = System.Drawing.Color.White;
            avisoListaVazia.Name = "lblSemMembrosDisponiveis";
            avisoListaVazia.Text = "Nenhum membro disponível para adicionar";
            avisoListaVazia.BackColor = panelFormulario.BackColor;
            avisoListaVazia.ForeColor = VisualUtils.TextoSuave;
            avisoListaVazia.Font = new System.Drawing.Font("Segoe UI", 9F);
            avisoListaVazia.SetBounds(28, 94, 440, 38);
            avisoListaVazia.Visible = false;
            panelFormulario.Controls.Add(avisoListaVazia);
            var atualizar = new Button
            {
                Name = "btnAtualizarMembrosDisponiveis", Text = "Atualizar",
                BackColor = System.Drawing.Color.White, ForeColor = VisualUtils.Azul
            };
            atualizar.SetBounds(478, 94, 100, 38);
            panelFormulario.Controls.Add(atualizar);
            VisualUtils.EstilizarBotao(atualizar);
            VisualUtils.Arredondar(atualizar, 8);
            atualizar.Click += (s, e) => CarregarMembrosAprovados();
            cmbMembroAprovado.SelectedIndexChanged += (s, e) =>
            {
                txtNomeFuncionario.Clear();
                txtLoginFuncionario.Clear();
                btnSalvarFuncionario.Enabled = false;
                Pessoa pessoa = cmbMembroAprovado.SelectedItem as Pessoa;
                if (pessoa == null)
                    return;
                try
                {
                    txtLoginFuncionario.Text = equipeService.BuscarLoginDaPessoaDisponivel(
                        usuarioAutenticado, evento.IdEvento, pessoa.IdPessoa);
                    txtNomeFuncionario.Text = pessoa.Nome;
                    labelAvisoFormulario.Text = "Membro selecionado. Confira o perfil e clique em Salvar vínculo.";
                    btnSalvarFuncionario.Enabled = cmbPerfilFuncionario.SelectedItem != null;
                }
                catch (Exception erro)
                {
                    btnSalvarFuncionario.Enabled = false;
                    CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar o membro selecionado.", labelAvisoFormulario);
                }
            };
            CarregarMembrosAprovados();
        }

        private void CarregarMembrosAprovados()
        {
            cmbMembroAprovado.Items.Clear();
            cmbMembroAprovado.Items.Add("Selecione um membro aprovado...");
            txtNomeFuncionario.Clear();
            txtLoginFuncionario.Clear();
            btnSalvarFuncionario.Enabled = false;
            cmbMembroAprovado.Enabled = false;
            cmbMembroAprovado.Visible = false;
            avisoListaVazia.Visible = true;
            avisoListaVazia.Text = "Nenhum membro disponível para adicionar";
            try
            {
                foreach (Pessoa pessoa in equipeService.ListarPessoasDisponiveis(usuarioAutenticado, evento.IdEvento))
                    cmbMembroAprovado.Items.Add(pessoa);
                bool disponiveis = cmbMembroAprovado.Items.Count > 1;
                cmbMembroAprovado.Enabled = disponiveis;
                cmbMembroAprovado.Visible = disponiveis;
                avisoListaVazia.Visible = !disponiveis;
                avisoListaVazia.BringToFront();
                if (disponiveis)
                {
                    cmbMembroAprovado.SelectedIndex = 0;
                    cmbMembroAprovado.BringToFront();
                }
                labelAvisoFormulario.Text = !disponiveis
                    ? "Nenhum novo membro disponível. Quem já está na equipe deve ser alterado em Editar vínculo. " +
                        "Novas contas precisam estar aprovadas com perfil Membro."
                    : "Selecione um membro aprovado. O nome e o usuário serão preenchidos automaticamente.";
            }
            catch (Exception erro)
            {
                avisoListaVazia.Text = "Lista indisponível. Clique em Atualizar.";
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível carregar os membros aprovados. Clique em Atualizar para tentar novamente.", labelAvisoFormulario);
            }
        }

        private void CarregarPerfis()
        {
            try
            {
                List<Perfil> perfis = equipeService.ListarPerfis(
                    usuarioAutenticado,
                    evento.IdEvento);

                foreach (Perfil perfil in perfis)
                {
                    cmbPerfilFuncionario.Items.Add(perfil);
                }

                cmbPerfilFuncionario.DisplayMember = "Nome";
                if (cmbPerfilFuncionario.Items.Count > 0)
                    cmbPerfilFuncionario.SelectedIndex = 0;

                if (cmbPerfilFuncionario.Items.Count == 0)
                {
                    btnSalvarFuncionario.Enabled = false;
                    labelAvisoFormulario.Text = "Não há perfis disponíveis para este evento.";
                }
            }
            catch (Exception)
            {
                btnSalvarFuncionario.Enabled = false;
                labelAvisoFormulario.Text = "Não foi possível carregar os perfis do evento.";
            }
        }

        private void CarregarVinculo()
        {
            try
            {
                Pessoa pessoa = equipeService.BuscarPessoaVinculada(
                    usuarioAutenticado,
                    evento.IdEvento,
                    vinculo.IdEquipe);

                if (pessoa == null)
                {
                    btnSalvarFuncionario.Enabled = false;
                    labelAvisoFormulario.Text = "O membro vinculado não foi encontrado.";
                    return;
                }

                txtNomeFuncionario.Text = pessoa.Nome;
                txtLoginFuncionario.Text = equipeService.BuscarLoginDaPessoaVinculada(
                    usuarioAutenticado,
                    evento.IdEvento,
                    vinculo.IdEquipe);
                txtLoginFuncionario.ReadOnly = true;
                chkFuncionarioAtivo.Checked = vinculo.Ativo;

                foreach (Perfil perfil in cmbPerfilFuncionario.Items)
                {
                    if (perfil.IdPerfil == vinculo.IdPerfil)
                    {
                        cmbPerfilFuncionario.SelectedItem = perfil;
                        break;
                    }
                }

                Text = lblTitulo.Text = "Editar vínculo de equipe";
            }
            catch (Exception)
            {
                btnSalvarFuncionario.Enabled = false;
                labelAvisoFormulario.Text = "Não foi possível carregar o vínculo selecionado.";
            }
        }

        private void txtLoginFuncionario_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLoginFuncionario.Text))
            {
                txtNomeFuncionario.Clear();
                return;
            }

            try
            {
                Pessoa pessoa = equipeService.BuscarPessoaDisponivelPorLogin(
                    usuarioAutenticado,
                    evento.IdEvento,
                    txtLoginFuncionario.Text.Trim());

                if (pessoa == null)
                {
                    txtNomeFuncionario.Clear();
                    labelAvisoFormulario.Text = "Não foi encontrado membro aprovado e disponível com esse login.";
                    return;
                }

                txtNomeFuncionario.Text = pessoa.Nome;
                labelAvisoFormulario.Text = "Membro localizado. Escolha o perfil e salve o vínculo.";
            }
            catch (Exception)
            {
                txtNomeFuncionario.Clear();
                labelAvisoFormulario.Text = "Não foi possível consultar esse membro.";
            }
        }

        private void btnSalvarFuncionario_Click(object sender, EventArgs e)
        {

            if (cmbPerfilFuncionario.SelectedItem == null)
            {
                labelAvisoFormulario.Text = "Selecione o perfil do membro neste evento.";
                cmbPerfilFuncionario.Focus();
                return;
            }

            Perfil perfil = (Perfil)cmbPerfilFuncionario.SelectedItem;

            try
            {
                if (vinculo == null)
                {
                    if (string.IsNullOrWhiteSpace(txtLoginFuncionario.Text))
                    {
                        labelAvisoFormulario.Text = "Informe o login de um membro aprovado e disponível.";
                        txtLoginFuncionario.Focus();
                        return;
                    }
                    // Reconsulta o membro caso Salvar seja acionado sem sair do login.
                    txtLoginFuncionario_Leave(txtLoginFuncionario, EventArgs.Empty);
                    if (string.IsNullOrWhiteSpace(txtNomeFuncionario.Text))
                    {
                        return;
                    }

                    equipeService.VincularMembro(
                        usuarioAutenticado,
                        evento.IdEvento,
                        txtLoginFuncionario.Text.Trim(),
                        perfil.IdPerfil);
                }
                else
                {
                    vinculo.IdPerfil = perfil.IdPerfil;
                    vinculo.Ativo = chkFuncionarioAtivo.Checked;
                    equipeService.AtualizarVinculo(usuarioAutenticado, vinculo);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira os dados do vínculo e tente novamente.", labelAvisoFormulario);
            }
            catch (InvalidOperationException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira os dados do vínculo e tente novamente.", labelAvisoFormulario);
            }
            catch (UnauthorizedAccessException)
            {
                labelAvisoFormulario.Text = "Sua conta não pode alterar a equipe deste evento.";
            }
            catch (Exception)
            {
                labelAvisoFormulario.Text = "Não foi possível salvar o vínculo.";
            }
        }
    }
}
