using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class QuermessesControl : UserControl
    {
        public event Action<Evento> EventoSelecionado;

        private readonly Usuario usuarioAutenticado;
        private readonly EventoService eventoService = new EventoService();
        private readonly TipoEventoService tipoEventoService = new TipoEventoService();
        private readonly Dictionary<int, string> nomesTiposEvento = new Dictionary<int, string>();
        private readonly Dictionary<int, string> nomesResponsaveis = new Dictionary<int, string>();
        private List<Evento> eventosCarregados = new List<Evento>();
        private string pesquisaAtual = string.Empty;

        public QuermessesControl() : this((Usuario)null)
        {
        }

        public QuermessesControl(Usuario usuario)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
            usuarioAutenticado = usuario;
            btnNovaQuermesse.Enabled = false;
            if (usuario != null)
            {
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuario, out perfil);
                    btnNovaQuermesse.Enabled = perfil == "Gerente" || perfil == "Administrador";
                    if (perfil == "Administrador")
                    {
                        lblSubtitulo.Text = "Cadastre eventos, defina o gerente responsável e acompanhe a organização.";
                    }
                }
                catch (Exception)
                {
                    btnNovaQuermesse.Enabled = false;
                }
            }
            dgvQuermesses.CellDoubleClick += dgvQuermesses_CellDoubleClick;
            dgvQuermesses.SelectionChanged += dgvQuermesses_SelectionChanged;
            ConfigurarTransicoesEvento();
            OrganizarListagemEventos();
            SizeChanged += (s, e) => OrganizarListagemEventos();

            VisualUtils.AdicionarPesquisa(this, "txtPesquisarEvento", "Pesquisar nome, local ou situação", Pesquisar);

            if (System.ComponentModel.LicenseManager.UsageMode !=
                System.ComponentModel.LicenseUsageMode.Designtime &&
                usuarioAutenticado != null)
            {
                CarregarEventos();
            }
        }

        private void ConfigurarTransicoesEvento()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem iniciar = new ToolStripMenuItem("Iniciar evento");
            ToolStripMenuItem finalizar = new ToolStripMenuItem("Finalizar evento");
            ToolStripMenuItem cancelar = new ToolStripMenuItem("Cancelar evento");
            iniciar.Click += (s, e) => AlterarSituacaoSelecionada(SituacoesEvento.EmAndamento);
            finalizar.Click += (s, e) => AlterarSituacaoSelecionada(SituacoesEvento.Finalizado);
            cancelar.Click += (s, e) => AlterarSituacaoSelecionada(SituacoesEvento.Cancelado);
            menu.Items.Add(iniciar);
            menu.Items.Add(finalizar);
            menu.Items.Add(cancelar);
            ToolStripMenuItem resumo = new ToolStripMenuItem("Consultar resumo do evento");
            resumo.Click += AbrirResumoEvento;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(resumo);
            menu.Opening += (s, e) =>
            {
                Evento evento = ObterEventoSelecionado();
                resumo.Enabled = evento != null;
                bool podeAlterarSituacao = false;
                if (usuarioAutenticado != null && evento != null)
                {
                    try
                    {
                        string perfil;
                        new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                        podeAlterarSituacao = perfil == "Administrador" ||
                            perfil == "Gerente" && evento.IdResponsavel == usuarioAutenticado.IdPessoa;
                    }
                    catch (Exception)
                    {
                        podeAlterarSituacao = false;
                    }
                }
                iniciar.Enabled = podeAlterarSituacao && evento.Situacao == SituacoesEvento.Planejado;
                finalizar.Enabled = podeAlterarSituacao && evento.Situacao == SituacoesEvento.EmAndamento;
                cancelar.Enabled = podeAlterarSituacao && (evento.Situacao == SituacoesEvento.Planejado ||
                    evento.Situacao == SituacoesEvento.EmAndamento);
            };
            dgvQuermesses.ContextMenuStrip = menu;
            dgvQuermesses.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvQuermesses.CurrentCell = dgvQuermesses.Rows[e.RowIndex].Cells[0];
                    dgvQuermesses.Rows[e.RowIndex].Selected = true;
                }
            };
            Button situacao = new Button();
            situacao.Name = "btnSituacaoEvento";
            situacao.Text = "Situação do evento";
            situacao.SetBounds(520, 12, 210, 36);
            situacao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            situacao.Click += (s, e) => menu.Show(situacao, new System.Drawing.Point(0, situacao.Height));
            panelListaQuermesses.Controls.Add(situacao);
            lblEventoscadastrados.Width = 480;
            VisualUtils.EstilizarBotao(situacao);
            Disposed += (s, e) => menu.Dispose();
        }

        private void OrganizarListagemEventos()
        {
            panelEventoResumo.Visible = false;
            pictureIconeTela.Image = Properties.Resources.LucideAzulEscala;
            if (btnNovaQuermesse.Parent != panelListaQuermesses)
            {
                panelListaQuermesses.Controls.Add(btnNovaQuermesse);
            }

            int largura = Math.Max(760, ClientSize.Width - 60);
            int altura = Math.Max(300, ClientSize.Height - 170);
            panelCorpo.SetBounds(30, 112, largura, altura);
            panelListaQuermesses.SetBounds(0, 0, largura, altura);
            Control situacao = panelListaQuermesses.Controls["btnSituacaoEvento"];
            situacao.SetBounds(largura - 204, 18, 180, 40);
            btnEditarQuermesse.Visible = false;
            btnEditarQuermesse.TabStop = false;
            btnNovaQuermesse.SetBounds(situacao.Left - 182, 18, 170, 40);
            btnNovaQuermesse.TabIndex = 0;
            situacao.TabIndex = 1;
            lblEventoscadastrados.SetBounds(18, 25, Math.Max(140, btnNovaQuermesse.Left - 36), 28);
            dgvQuermesses.SetBounds(12, 76, largura - 24, altura - 90);
            dgvQuermesses.TabIndex = 3;
            labelAviso.SetBounds(30, panelCorpo.Bottom + 12, largura, 28);
            AutoScrollMinSize = new System.Drawing.Size(820, labelAviso.Bottom + 12);
        }

        private void AlterarSituacaoSelecionada(string situacao)
        {

            Evento evento = ObterEventoSelecionado();
            if (evento == null || usuarioAutenticado == null)
            {
                labelAviso.Text = "Selecione um evento autorizado.";
                return;
            }
            if (MessageBox.Show(this, "Confirma alterar a situação para " + situacao + "?",
                "Situação do evento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                eventoService.AlterarSituacao(usuarioAutenticado, evento.IdEvento, situacao);
                CarregarEventos();
                labelAviso.Text = "Situação atualizada.";
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível atualizar a situação do evento.";
                if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
                {
                    CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível atualizar a situação do evento.", labelAviso);
                }
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em AlterarSituacaoSelecionada");
            }
        }

        private void AbrirResumoEvento(object sender, EventArgs e)
        {
            Evento selecionado = ObterEventoSelecionado();
            if (selecionado == null)
            {
                labelAviso.Text = "Selecione um evento para consultar o resumo.";
                return;
            }
            using (Form janela = new Form())
            {
                janela.Text = "Resumo do evento";
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.Size = new System.Drawing.Size(1100, 760);
                janela.MinimizeBox = false;
                janela.Controls.Add(new QuermesseDashboardControl(usuarioAutenticado, selecionado)
                {
                    Dock = DockStyle.Fill
                });
                IconoAplicacao.Aplicar(janela);
                janela.ShowDialog(this);
            }
        }

        private void btnNovaQuermesse_Click(object sender, EventArgs e)
        {

            if (!VerificarSessao())
            {
                return;
            }

            using (var formulario = new QuermesseForm(usuarioAutenticado, null))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarEventos(formulario.IdEventoSalvo);
                }
            }
        }

        private void btnEditarQuermesse_Click(object sender, EventArgs e)
        {
            AbrirEventoSelecionado();
        }

        private void dgvQuermesses_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                AbrirEventoSelecionado();
            }
        }

        private void dgvQuermesses_SelectionChanged(object sender, EventArgs e)
        {
            Evento evento = null;

            if (dgvQuermesses.CurrentRow != null)
            {
                evento = dgvQuermesses.CurrentRow.Tag as Evento;
            }

            lblQuermessedeSaoJoao2026.Text = evento == null ? "Nenhum evento selecionado" : evento.Nome;
            lblPatiodacomunidadeEmandamento.Text = evento == null ? string.Empty :
                evento.Local + " · " + evento.Situacao;

            Action<Evento> eventoSelecionado = EventoSelecionado;

            if (eventoSelecionado != null)
            {
                eventoSelecionado(evento);
            }
        }

        public Evento ObterEventoSelecionado()
        {
            if (dgvQuermesses.CurrentRow == null)
            {
                return null;
            }

            return dgvQuermesses.CurrentRow.Tag as Evento;
        }

        private void AbrirEventoSelecionado()
        {

            if (!VerificarSessao())
            {
                return;
            }

            if (dgvQuermesses.CurrentRow == null ||
                !(dgvQuermesses.CurrentRow.Tag is Evento))
            {
                labelAviso.Text = "Selecione um evento na lista.";
                return;
            }

            Evento evento = (Evento)dgvQuermesses.CurrentRow.Tag;

            using (var formulario = new QuermesseForm(usuarioAutenticado, evento))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarEventos();
                }
            }
        }

        private bool VerificarSessao()
        {
            if (usuarioAutenticado != null)
            {
                return true;
            }

            MessageBox.Show(
                this,
                "Entre no sistema com uma conta autorizada para usar esta função.",
                "Eventos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void CarregarEventos(int? idEventoSelecionado = null)
        {
            try
            {
                List<TipoEvento> tipos = tipoEventoService.Listar(usuarioAutenticado);
                Dictionary<int, string> novosNomesTipos = new Dictionary<int, string>();

                foreach (TipoEvento tipo in tipos)
                {
                    novosNomesTipos[tipo.IdTipoEvento] = tipo.Nome;
                }

                List<Evento> eventos = eventoService.Listar(usuarioAutenticado);
                Dictionary<int, string> novosNomesResponsaveis = new Dictionary<int, string>();
                foreach (Evento evento in eventos)
                {
                    if (!novosNomesResponsaveis.ContainsKey(evento.IdResponsavel))
                    {
                        novosNomesResponsaveis[evento.IdResponsavel] = eventoService.ConsultarNomeResponsavel(
                            usuarioAutenticado, evento.IdEvento);
                    }
                }

                eventosCarregados = eventos;
                nomesTiposEvento.Clear();
                foreach (KeyValuePair<int, string> par in novosNomesTipos)
                    nomesTiposEvento[par.Key] = par.Value;
                nomesResponsaveis.Clear();
                foreach (KeyValuePair<int, string> par in novosNomesResponsaveis)
                    nomesResponsaveis[par.Key] = par.Value;
                Pesquisar(pesquisaAtual);
                if (idEventoSelecionado.HasValue)
                {
                    SelecionarEvento(idEventoSelecionado.Value);
                }

                if (eventos.Count == 0)
                {
                    labelAviso.Text = "Nenhum evento cadastrado.";
                }
                else
                {
                    labelAviso.Text = eventos.Count + " evento(s) carregado(s).";
                }

                if (eventos.Count > 0)
                {
                    dgvQuermesses_SelectionChanged(this, EventArgs.Empty);
                }
                else
                {
                    lblQuermessedeSaoJoao2026.Text = "Nenhum evento cadastrado";
                    lblPatiodacomunidadeEmandamento.Text = string.Empty;
                }
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não tem permissão para consultar eventos.";
                MessageBox.Show(
                    this,
                    "Sua conta não tem permissão para consultar os eventos.",
                    "Eventos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar os eventos.";
                MessageBox.Show(
                    this,
                    "Não foi possível consultar os eventos. Tente novamente ou confira se sua conta tem acesso a esta área.",
                    "Eventos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void SelecionarEvento(int idEvento)
        {
            foreach (DataGridViewRow linha in dgvQuermesses.Rows)
            {
                Evento evento = linha.Tag as Evento;
                if (evento == null || evento.IdEvento != idEvento)
                    continue;

                dgvQuermesses.ClearSelection();
                dgvQuermesses.CurrentCell = linha.Cells[0];
                linha.Selected = true;
                dgvQuermesses_SelectionChanged(dgvQuermesses, EventArgs.Empty);
                break;
            }
        }

        public void Pesquisar(string texto)
        {
            Evento anterior = ObterEventoSelecionado();
            pesquisaAtual = texto ?? string.Empty;
            dgvQuermesses.Rows.Clear();
            string termo = texto;
            if (termo == null)
            {
                termo = string.Empty;
            }

            foreach (Evento evento in eventosCarregados)
            {
                string conteudo = evento.Nome + " " + evento.Local + " " + evento.Situacao;
                if (conteudo.IndexOf(termo, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    continue;
                }

                string nomeTipo = "Tipo " + evento.IdTipoEvento;
                if (nomesTiposEvento.ContainsKey(evento.IdTipoEvento))
                {
                    nomeTipo = nomesTiposEvento[evento.IdTipoEvento];
                }

                int indiceLinha = dgvQuermesses.Rows.Add(
                    evento.Nome,
                    nomeTipo,
                    evento.Local,
                    evento.DataInicio.ToString("dd/MM/yyyy"),
                    evento.DataFim.ToString("dd/MM/yyyy"),
                    nomesResponsaveis.ContainsKey(evento.IdResponsavel)
                        ? nomesResponsaveis[evento.IdResponsavel]
                        : "Responsável não encontrado",
                    evento.Situacao);
                dgvQuermesses.Rows[indiceLinha].Tag = evento;
            }

            dgvQuermesses.ClearSelection();
            SelecionarPrimeiroEventoSeDisponivel();
            foreach (DataGridViewRow linha in dgvQuermesses.Rows)
            {
                Evento evento = linha.Tag as Evento;
                if (anterior != null && evento != null && evento.IdEvento == anterior.IdEvento)
                {
                    dgvQuermesses.ClearSelection();
                    dgvQuermesses.CurrentCell = linha.Cells[0];
                    linha.Selected = true;
                    break;
                }
            }
            dgvQuermesses_SelectionChanged(this, EventArgs.Empty);

            if (dgvQuermesses.Rows.Count == 0)
            {
                labelAviso.Text = string.IsNullOrWhiteSpace(termo)
                    ? "Nenhum evento cadastrado."
                    : "Nenhum evento corresponde à busca.";
            }
            else
            {
                labelAviso.Text = dgvQuermesses.Rows.Count + " evento(s) encontrado(s).";
            }
        }

        private void SelecionarPrimeiroEventoSeDisponivel()
        {
            if (dgvQuermesses.Rows.Count == 0 || dgvQuermesses.Columns.Count == 0)
            {
                return;
            }

            dgvQuermesses.CurrentCell = dgvQuermesses.Rows[0].Cells[0];
            dgvQuermesses.Rows[0].Selected = true;
        }
    }
}
