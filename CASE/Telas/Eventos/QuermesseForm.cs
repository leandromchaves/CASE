using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Repositories;
using CASE.Services;
using CASE.Utils;
using MySql.Data.MySqlClient;

namespace CASE.Quermesse
{
    public partial class QuermesseForm : Form
    {
        public int? IdEventoSalvo { get; private set; }

        private Usuario usuarioAutenticado;
        private Evento eventoEmEdicao;
        private EventoService eventoService = new EventoService();
        private TipoEventoService tipoEventoService = new TipoEventoService();
        private ComboBox gerenteResponsavel;
        private bool administrador;
        public QuermesseForm() : this(null, null)
        {
        }

        public QuermesseForm(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            IconoAplicacao.Aplicar(this);
            FrontendTheme.Aplicar(this);
            AlinharCamposEvento();
            usuarioAutenticado = usuario;
            eventoEmEdicao = evento;
            cmbTipoEvento.Items.Clear();
            cmbTipoEvento.DisplayMember = "Nome";
            cmbTipoEvento.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipoEvento.DrawMode = DrawMode.Normal;
            cmbTipoEvento.FlatStyle = FlatStyle.Standard;
            labelAvisoFormulario.Text = string.Empty;
            btnSalvarVisual.Enabled = false;
            // A situação é controlada pelas transições do Service, não pelo formulário.
            cmbSituacaoQuermesse.Items.Clear();
            cmbSituacaoQuermesse.Enabled = false;
            cmbSituacaoQuermesse.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSituacaoQuermesse.DrawMode = DrawMode.Normal;
            cmbSituacaoQuermesse.FlatStyle = FlatStyle.Standard;
            lblResponsavel.Text = "Gerente responsável (automático)";
            txtResponsavelQuermesse.ReadOnly = true;
            txtResponsavelQuermesse.TabStop = false;
            lblSituacao.Text = "Situação (automática)";
            lblObservacao.Top = 438;
            ControleVisual(txtObservacaoQuermesse).Top = 464;
            labelAvisoFormulario.Top = 499;
            txtNomeQuermesse.TabIndex = 0;
            txtNomeQuermesse.MaxLength = 150;
            txtLocalQuermesse.MaxLength = 200;
            cmbTipoEvento.TabIndex = 1;
            txtLocalQuermesse.TabIndex = 2;
            dtpInicioQuermesse.TabIndex = 3;
            dtpFimQuermesse.TabIndex = 4;
            txtResponsavelQuermesse.TabIndex = 5;
            txtObservacaoQuermesse.TabIndex = 6;
            cmbSituacaoQuermesse.TabStop = false;
            btnSalvarVisual.TabIndex = 7;
            btnCancelar.TabIndex = 8;
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && usuarioAutenticado != null)
            {
                CarregarTiposEvento();
                PrepararFormulario();
                if (btnSalvarVisual.Enabled)
                {
                    ExibirAviso("O responsável será definido pela conta autenticada.", false);
                }

                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                    administrador = perfil == "Administrador";
                    if (eventoEmEdicao != null)
                    {
                        txtResponsavelQuermesse.Text = eventoService.ConsultarNomeResponsavel(usuarioAutenticado, eventoEmEdicao.IdEvento);
                    }

                    if (administrador)
                    {
                        ConfigurarGerenteResponsavel(eventoEmEdicao == null ? (int? )null : eventoEmEdicao.IdResponsavel);
                    }
                    else if (perfil != "Gerente")
                    {
                        btnSalvarVisual.Enabled = false;
                        txtNomeQuermesse.ReadOnly = true;
                        txtLocalQuermesse.ReadOnly = true;
                        txtObservacaoQuermesse.ReadOnly = true;
                        cmbTipoEvento.Enabled = false;
                        dtpInicioQuermesse.Enabled = false;
                        dtpFimQuermesse.Enabled = false;
                        ExibirAviso("Detalhes do evento. A edição é exclusiva do gerente responsável.", false);
                    }
                }
                catch (Exception)
                {
                    btnSalvarVisual.Enabled = false;
                    ExibirAviso("Não foi possível confirmar a autorização e o responsável deste evento.", true);
                }
            }
            else
            {
                ExibirAviso("Entre com uma conta autorizada para cadastrar eventos.", true);
            }
        }

        private void AlinharCamposEvento()
        {
            Control[] campos =
            {
                ControleVisual(txtNomeQuermesse),
                cmbTipoEvento,
                ControleVisual(txtLocalQuermesse),
                dtpInicioQuermesse,
                dtpFimQuermesse,
                ControleVisual(txtResponsavelQuermesse),
                cmbSituacaoQuermesse,
                ControleVisual(txtObservacaoQuermesse)
            };
            Label[] rotulos =
            {
                lblNomedoevento,
                lblTipoEvento,
                lblLocal,
                lblDatainicial,
                lblDatafinal,
                lblResponsavel,
                lblSituacao,
                lblObservacao
            };
            for (int i = 0; i < campos.Length; i++)
            {
                campos[i].Left = 28;
                campos[i].Width = panelFormulario.ClientSize.Width - 56;
                campos[i].Top = 98 + i * 52;
                campos[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                rotulos[i].Left = 28;
                rotulos[i].Top = campos[i].Top - 24;
                rotulos[i].AutoSize = false;
                rotulos[i].Width = campos[i].Width;
                rotulos[i].Height = 21;
                rotulos[i].Anchor = campos[i].Anchor;
            }
        }

        private static Control ControleVisual(TextBox campo)
        {
            Panel painel = campo == null ? null : campo.Parent as Panel;
            return painel != null && painel.Controls.Count == 1 && painel.Controls[0] == campo ? (Control)painel : campo;
        }

        private void ConfigurarGerenteResponsavel(int? idResponsavelAtual)
        {
            lblResponsavel.Text = "Gerente responsável";
            Control contornoResponsavel = ControleVisual(txtResponsavelQuermesse);
            txtResponsavelQuermesse.Visible = false;
            contornoResponsavel.Visible = false;
            gerenteResponsavel = new ComboBox
            {
                Name = "cmbGerenteResponsavel",
                Bounds = contornoResponsavel.Bounds,
                Anchor = contornoResponsavel.Anchor,
                Font = txtResponsavelQuermesse.Font,
                DisplayMember = "Nome",
                DropDownStyle = ComboBoxStyle.DropDownList,
                TabIndex = 5
            };
            panelFormulario.Controls.Add(gerenteResponsavel);
            VisualUtils.EstilizarSelecao(gerenteResponsavel);
            List<Pessoa> gerentes = eventoService.ListarGerentesResponsaveis(usuarioAutenticado);
            if (idResponsavelAtual.HasValue && !gerentes.Exists(Tratar_ConfigurarGerenteResponsavel_Exists))
            {
                gerentes.Add(new Pessoa { IdPessoa = idResponsavelAtual.Value, Nome = txtResponsavelQuermesse.Text });
            }

            foreach (Pessoa gerente in gerentes)
            {
                gerenteResponsavel.Items.Add(gerente);
            }

            if (idResponsavelAtual.HasValue)
            {
                for (int indice = 0; indice < gerenteResponsavel.Items.Count; indice++)
                {
                    Pessoa gerente = gerenteResponsavel.Items[indice] as Pessoa;
                    if (gerente != null && gerente.IdPessoa == idResponsavelAtual.Value)
                    {
                        gerenteResponsavel.SelectedIndex = indice;
                        break;
                    }
                }
            }

            gerenteResponsavel.SelectedIndexChanged += gerenteResponsavel_SelectedIndexChanged;
            btnSalvarVisual.Enabled = cmbTipoEvento.Items.Count > 0 && gerenteResponsavel.Items.Count > 0;
            ExibirAviso(gerenteResponsavel.Items.Count == 0 ? "Aprove uma conta com perfil Gerente antes de cadastrar o evento." : "Selecione o gerente que ficará responsável pela organização do evento.", gerenteResponsavel.Items.Count == 0);
            bool Tratar_ConfigurarGerenteResponsavel_Exists(Pessoa pessoa)
            {
                return pessoa.IdPessoa == idResponsavelAtual.Value;
            }
        }

        private void PrepararFormulario()
        {
            if (eventoEmEdicao == null)
            {
                Text = lblTitulo.Text = "Novo evento";
                cmbSituacaoQuermesse.Items.Add(TextoInterface.FormatarValorBanco(SituacoesEvento.Planejado));
                cmbSituacaoQuermesse.SelectedIndex = 0;
                txtResponsavelQuermesse.Text = usuarioAutenticado.Login;
                dtpInicioQuermesse.Value = DateTime.Today;
                dtpFimQuermesse.Value = DateTime.Today;
                return;
            }

            Text = lblTitulo.Text = "Editar evento";
            txtNomeQuermesse.Text = eventoEmEdicao.Nome;
            txtLocalQuermesse.Text = eventoEmEdicao.Local;
            txtResponsavelQuermesse.Text = usuarioAutenticado.Login;
            txtObservacaoQuermesse.Text = eventoEmEdicao.Observacao;
            dtpInicioQuermesse.Value = eventoEmEdicao.DataInicio;
            dtpFimQuermesse.Value = eventoEmEdicao.DataFim;
            cmbSituacaoQuermesse.Items.Add(TextoInterface.FormatarValorBanco(eventoEmEdicao.Situacao));
            cmbSituacaoQuermesse.SelectedIndex = 0;
            SelecionarTipoEvento(eventoEmEdicao.IdTipoEvento);
        }

        private void btnSalvarVisual_Click(object sender, EventArgs e)
        {
            if (usuarioAutenticado == null)
            {
                ExibirAviso("Entre com uma conta autorizada para salvar eventos.", true);
                return;
            }

            TipoEvento tipoEvento = cmbTipoEvento.SelectedItem as TipoEvento;
            if (tipoEvento == null)
            {
                ExibirAviso("Selecione um tipo de evento cadastrado.", true);
                cmbTipoEvento.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNomeQuermesse.Text))
            {
                ExibirAviso("Informe o nome do evento.", true);
                txtNomeQuermesse.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLocalQuermesse.Text))
            {
                ExibirAviso("Informe o local do evento.", true);
                txtLocalQuermesse.Focus();
                return;
            }

            if (dtpFimQuermesse.Value.Date < dtpInicioQuermesse.Value.Date)
            {
                ExibirAviso("A data final não pode ser anterior à data inicial.", true);
                dtpFimQuermesse.Focus();
                return;
            }

            try
            {
                Evento evento;
                if (eventoEmEdicao == null)
                {
                    evento = new Evento();
                    evento.IdInstituicao = eventoService.ObterIdInstituicaoUnica(usuarioAutenticado);
                    if (gerenteResponsavel != null)
                    {
                        Pessoa gerente = gerenteResponsavel.SelectedItem as Pessoa;
                        if (gerente == null)
                        {
                            ExibirAviso("Selecione o gerente responsável pelo evento.", true);
                            gerenteResponsavel.Focus();
                            return;
                        }

                        evento.IdResponsavel = gerente.IdPessoa;
                    }
                }
                else
                {
                    evento = new Evento();
                    evento.IdEvento = eventoEmEdicao.IdEvento;
                    evento.IdInstituicao = eventoEmEdicao.IdInstituicao;
                    evento.IdResponsavel = eventoEmEdicao.IdResponsavel;
                    evento.Situacao = eventoEmEdicao.Situacao;
                    evento.Versao = eventoEmEdicao.Versao;
                    evento.IdUsuarioCriacao = eventoEmEdicao.IdUsuarioCriacao;
                    evento.CriadoEm = eventoEmEdicao.CriadoEm;
                    if (administrador && gerenteResponsavel != null)
                    {
                        Pessoa gerente = gerenteResponsavel.SelectedItem as Pessoa;
                        if (gerente == null)
                        {
                            ExibirAviso("Selecione o gerente responsável pelo evento.", true);
                            gerenteResponsavel.Focus();
                            return;
                        }

                        evento.IdResponsavel = gerente.IdPessoa;
                    }
                }

                evento.IdTipoEvento = tipoEvento.IdTipoEvento;
                evento.Nome = txtNomeQuermesse.Text.Trim();
                evento.Local = txtLocalQuermesse.Text.Trim();
                evento.DataInicio = dtpInicioQuermesse.Value.Date;
                evento.DataFim = dtpFimQuermesse.Value.Date;
                evento.Observacao = txtObservacaoQuermesse.Text.Trim();
                if (eventoEmEdicao == null)
                {
                    evento.IdEvento = eventoService.Criar(usuarioAutenticado, evento);
                }
                else
                {
                    eventoService.Atualizar(usuarioAutenticado, evento);
                }

                IdEventoSalvo = evento.IdEvento;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                ExibirAviso(FeedbackUsuario.MensagemErro(ex, "Confira os dados do evento e tente novamente."), true);
            }
            catch (InvalidOperationException ex)
            {
                ExibirAviso(FeedbackUsuario.MensagemErro(ex, "Confira os dados do evento e tente novamente."), true);
            }
            catch (UnauthorizedAccessException)
            {
                ExibirAviso("Sua conta não tem permissão para salvar este evento.", true);
            }
            catch (MySqlException)
            {
                ExibirAviso("Não foi possível salvar o evento. Confira os dados e tente novamente.", true);
            }
            catch (Exception)
            {
                ExibirAviso("Não foi possível salvar o evento. Tente novamente.", true);
            }
        }

        private void CarregarTiposEvento()
        {
            try
            {
                List<TipoEvento> tiposEvento = tipoEventoService.Listar(usuarioAutenticado);
                foreach (TipoEvento tipoEvento in tiposEvento)
                {
                    cmbTipoEvento.Items.Add(tipoEvento);
                }

                if (eventoEmEdicao != null)
                {
                    SelecionarTipoEvento(eventoEmEdicao.IdTipoEvento);
                }
                else if (cmbTipoEvento.Items.Count > 0)
                {
                    cmbTipoEvento.SelectedIndex = 0;
                }

                if (cmbTipoEvento.Items.Count == 0)
                {
                    btnSalvarVisual.Enabled = false;
                    ExibirAviso("Não há tipos de evento cadastrados. Peça a um administrador para cadastrar um tipo antes de continuar.", true);
                }
                else
                {
                    btnSalvarVisual.Enabled = true;
                }
            }
            catch (Exception)
            {
                ExibirAviso("Não foi possível carregar os tipos de evento. Atualize a tela e tente novamente.", true);
                btnSalvarVisual.Enabled = false;
            }
        }

        private void ExibirAviso(string mensagem, bool erro)
        {
            labelAvisoFormulario.Text = mensagem;
            labelAvisoFormulario.ForeColor = erro ? Color.FromArgb(190, 55, 65) : VisualUtils.TextoSuave;
            if (!string.IsNullOrWhiteSpace(mensagem))
                FeedbackUsuario.Exibir(this, mensagem, erro ? TipoFeedback.Erro : TipoFeedback.Sucesso);
        }

        private void SelecionarTipoEvento(int idTipoEvento)
        {
            for (int indice = 0; indice < cmbTipoEvento.Items.Count; indice++)
            {
                TipoEvento tipoEvento = cmbTipoEvento.Items[indice] as TipoEvento;
                if (tipoEvento != null && tipoEvento.IdTipoEvento == idTipoEvento)
                {
                    cmbTipoEvento.SelectedIndex = indice;
                    return;
                }
            }
        }

        private void gerenteResponsavel_SelectedIndexChanged(object s, EventArgs e)
        {
            if (gerenteResponsavel.SelectedItem != null)
            {
                ExibirAviso("O evento será vinculado ao gerente selecionado.", false);
            }
        }
    }
}
