using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Repositories;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class EscalaControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento eventoSelecionado;
        private readonly EscalaService escalaService = new EscalaService();
        private readonly EquipeEventoService equipeService = new EquipeEventoService();
        private readonly JornadaSetorService jornadaService = new JornadaSetorService();
        private readonly SetorService setorService = new SetorService();
        private readonly TurnoRepository turnoRepository = new TurnoRepository();
        private readonly Button btnRemoverEscala = new Button();
        private bool podeAdministrarEscalas;
        private bool usuarioEhMembro;
        private TextBox pesquisa;

        public EscalaControl() : this(null, null)
        {
        }

        public EscalaControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
            ConfigurarAcaoRemoverEscala();
            pesquisa = VisualUtils.AdicionarPesquisa(this, "txtPesquisarEscala", "Pesquisar escala ou funcionário",
                texto => AplicarFiltros());
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            cmbFiltroEscala.Items.Clear();
            cmbFiltroTurnoEscala.Items.Clear();
            dgvEscala.CellDoubleClick += dgvEscala_CellDoubleClick;
            dgvEscala.SelectionChanged += (s, e) => AtualizarAcaoRemoverEscala();
            panelFiltrosEscala.SizeChanged += (s, e) => AjustarAcoesEscala();
            AjustarAcoesEscala();

            if (usuarioAutenticado == null)
            {
                btnNovaAlocacao.Enabled = false;
                labelAviso.Text = "Entre no sistema para consultar as escalas.";
                return;
            }

            try
            {
                string nomePerfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(
                    usuarioAutenticado,
                    out nomePerfil);
                usuarioEhMembro = string.Equals(
                    nomePerfil,
                    "Membro",
                    StringComparison.OrdinalIgnoreCase);
                bool administrador = string.Equals(nomePerfil, "Administrador", StringComparison.OrdinalIgnoreCase);
                podeAdministrarEscalas = administrador || string.Equals(
                    nomePerfil,
                    "Gerente",
                    StringComparison.OrdinalIgnoreCase);

                if (podeAdministrarEscalas && eventoSelecionado != null)
                {
                    Evento eventoAutorizado = new EventoService().BuscarPorId(
                        usuarioAutenticado,
                        eventoSelecionado.IdEvento);

                    podeAdministrarEscalas = eventoAutorizado != null &&
                        (administrador || eventoAutorizado.IdResponsavel == usuarioAutenticado.IdPessoa);
                }
            }
            catch (Exception)
            {
                podeAdministrarEscalas = false;
            }

            btnNovaAlocacao.Enabled = podeAdministrarEscalas && EventoPermiteAlteracoes();
            btnRemoverEscala.Visible = podeAdministrarEscalas && eventoSelecionado != null;
            AtualizarAcaoRemoverEscala();

            if (eventoSelecionado == null && !usuarioEhMembro)
            {
                labelAviso.Text = "Abra Eventos e selecione um evento antes de alocar funcionários.";
                return;
            }

            if (eventoSelecionado != null && !EventoPermiteAlteracoes())
            {
                labelAviso.Text = "Este evento está encerrado. As escalas existentes podem ser consultadas, mas não alteradas.";
            }

            CarregarEscalas();
        }

        private void ConfigurarAcaoRemoverEscala()
        {
            btnRemoverEscala.Name = "btnRemoverEscalaSelecionada";
            btnRemoverEscala.Text = "Remover escala";
            btnRemoverEscala.Size = new System.Drawing.Size(175, 40);
            btnRemoverEscala.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            btnRemoverEscala.BackColor = System.Drawing.Color.White;
            btnRemoverEscala.ForeColor = System.Drawing.Color.FromArgb(180, 55, 65);
            btnRemoverEscala.FlatStyle = FlatStyle.Flat;
            btnRemoverEscala.FlatAppearance.BorderSize = 1;
            btnRemoverEscala.FlatAppearance.BorderColor = VisualUtils.Borda;
            btnRemoverEscala.Cursor = Cursors.Hand;
            btnRemoverEscala.Enabled = false;
            btnRemoverEscala.Visible = false;
            btnRemoverEscala.Click += RemoverEscalaSelecionada;
            VisualUtils.EstilizarBotao(btnRemoverEscala);
            VisualUtils.Arredondar(btnRemoverEscala, 8);
            panelFiltrosEscala.Controls.Add(btnRemoverEscala);
        }

        private void AjustarAcoesEscala()
        {
            if (panelFiltrosEscala == null || btnNovaAlocacao == null || btnRemoverEscala == null)
                return;

            int direita = panelFiltrosEscala.ClientSize.Width - 20;
            btnNovaAlocacao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNovaAlocacao.SetBounds(direita - 235, 34, 235, 40);
            btnRemoverEscala.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemoverEscala.SetBounds(btnNovaAlocacao.Left - 187, 34, 175, 40);
        }

        private bool EventoPermiteAlteracoes()
        {
            return eventoSelecionado != null &&
                (string.Equals(eventoSelecionado.Situacao, SituacoesEvento.Planejado, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventoSelecionado.Situacao, SituacoesEvento.EmAndamento, StringComparison.OrdinalIgnoreCase));
        }

        private void AtualizarAcaoRemoverEscala()
        {
            if (btnRemoverEscala == null || dgvEscala == null)
                return;

            btnRemoverEscala.Enabled = podeAdministrarEscalas && EventoPermiteAlteracoes() &&
                dgvEscala.SelectedRows.Count > 0;
        }

        private void RemoverEscalaSelecionada(object sender, EventArgs e)
        {
            if (dgvEscala.SelectedRows.Count == 0)
            {
                labelAviso.Text = "Selecione a escala que deseja remover.";
                return;
            }

            Escala escala = dgvEscala.SelectedRows[0].Tag as Escala;
            if (escala == null)
            {
                labelAviso.Text = "Não foi possível identificar a escala selecionada.";
                return;
            }

            string funcionario = Convert.ToString(dgvEscala.SelectedRows[0].Cells[0].Value);
            string setor = Convert.ToString(dgvEscala.SelectedRows[0].Cells[1].Value);
            string data = Convert.ToString(dgvEscala.SelectedRows[0].Cells[2].Value);
            DialogResult confirmar = MessageBox.Show(
                this,
                "Remover a escala de " + funcionario + " em " + setor + " no dia " + data + "?\n\n" +
                "Escalas com vendas, perdas ou solicitações vinculadas não podem ser removidas, para preservar o histórico.",
                "Remover escala",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmar != DialogResult.Yes)
                return;

            try
            {
                escalaService.Remover(usuarioAutenticado, escala.IdEscala);
                CarregarEscalas();
                labelAviso.Text = "Escala removida.";
                FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(
                    this,
                    erro,
                    "Não foi possível remover a escala. Atualize a lista e tente novamente.",
                    labelAviso);
            }
        }

        private void btnNovaAlocacao_Click(object sender, EventArgs e)
        {

            if (!podeAdministrarEscalas || !EventoPermiteAlteracoes())
            {
                labelAviso.Text = eventoSelecionado == null
                    ? "Selecione um evento e entre como administrador ou gerente responsável para alocar funcionários."
                    : "Este evento está encerrado. Não é possível criar ou alterar escalas.";
                return;
            }

            AbrirFormulario(null);
        }

        private void dgvEscala_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || !podeAdministrarEscalas || !EventoPermiteAlteracoes())
            {
                return;
            }

            Escala escala = dgvEscala.Rows[e.RowIndex].Tag as Escala;

            if (escala != null)
            {
                AbrirFormulario(escala);
            }
        }

        private void AbrirFormulario(Escala escala)
        {
            using (EscalaForm formulario = new EscalaForm(
                usuarioAutenticado,
                eventoSelecionado,
                escala))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarEscalas();
                }
            }
        }

        private void Filtros_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        internal void AtualizarConsulta()
        {
            string setor = cmbFiltroEscala.Text;
            string turno = cmbFiltroTurnoEscala.Text;
            CarregarEscalas();
            int indiceSetor = cmbFiltroEscala.FindStringExact(setor);
            int indiceTurno = cmbFiltroTurnoEscala.FindStringExact(turno);
            if (indiceSetor >= 0) cmbFiltroEscala.SelectedIndex = indiceSetor;
            if (indiceTurno >= 0) cmbFiltroTurnoEscala.SelectedIndex = indiceTurno;
        }

        private void CarregarEscalas()
        {
            if (eventoSelecionado == null)
            {
                if (usuarioEhMembro)
                {
                    CarregarMinhasEscalas();
                }
                else
                {
                    labelAviso.Text = "Selecione um evento para consultar as escalas.";
                }

                return;
            }

            try
            {
                List<Escala> escalas = escalaService.ListarPorEvento(
                    usuarioAutenticado,
                    eventoSelecionado.IdEvento);

                SubstituirEscalas(escalas);

                PreencherFiltros();
                AplicarFiltros();
                AtualizarAcaoRemoverEscala();
                labelAviso.Text = EventoPermiteAlteracoes()
                    ? escalas.Count + " escala(s) carregada(s)."
                    : "Este evento está encerrado. As escalas existentes podem ser consultadas, mas não alteradas.";
                if (!podeAdministrarEscalas && EventoPermiteAlteracoes())
                {
                    labelAviso.Text += " A alocação é feita pelo gerente responsável pelo evento.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                btnNovaAlocacao.Enabled = false;
                btnRemoverEscala.Enabled = false;
                labelAviso.Text = "Sua conta não pode consultar as escalas deste evento.";
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar as escalas do evento.";
            }
        }

        private void CarregarMinhasEscalas()
        {
            try
            {
                List<Escala> escalas = escalaService.ListarMinhas(usuarioAutenticado);

                SubstituirEscalas(escalas);

                PreencherFiltros();
                AplicarFiltros();
                btnNovaAlocacao.Enabled = false;
                btnRemoverEscala.Enabled = false;
                labelAviso.Text = escalas.Count + " escala(s) vinculada(s) à sua conta.";
            }
            catch (UnauthorizedAccessException)
            {
                btnNovaAlocacao.Enabled = false;
                labelAviso.Text = "Selecione um evento ou consulte suas escalas com uma conta de membro.";
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar as escalas.";
            }
        }

        private void SubstituirEscalas(List<Escala> escalas)
        {
            var linhas = new List<DataGridViewRow>();
            foreach (Escala escala in escalas)
            {
                DataGridViewRow linha = CriarLinha(escala);
                if (linha != null)
                    linhas.Add(linha);
            }
            dgvEscala.Rows.Clear();
            dgvEscala.Rows.AddRange(linhas.ToArray());
        }

        private DataGridViewRow CriarLinha(Escala escala)
        {
            EquipeEvento equipe = equipeService.BuscarPorId(
                usuarioAutenticado,
                escala.IdEquipe);
            JornadaSetor jornada = jornadaService.BuscarPorId(
                usuarioAutenticado,
                escala.IdJornada);

            if (equipe == null || jornada == null)
            {
                return null;
            }

            Pessoa pessoa = equipeService.BuscarPessoaVinculada(
                usuarioAutenticado,
                equipe.IdEvento,
                equipe.IdEquipe);
            Setor setor = setorService.BuscarPorId(
                usuarioAutenticado,
                jornada.IdSetor);
            Turno turno = turnoRepository.BuscarPorId(jornada.IdTurno);

            string nomePessoa = "Pessoa " + equipe.IdPessoa;
            string nomeSetor = "Setor " + jornada.IdSetor;
            string nomeTurno = "Turno " + jornada.IdTurno;

            if (pessoa != null)
            {
                nomePessoa = pessoa.Nome;
            }

            if (setor != null)
            {
                nomeSetor = setor.Nome;
            }

            if (turno != null)
            {
                nomeTurno = turno.Nome;
            }

            var linha = new DataGridViewRow();
            linha.CreateCells(dgvEscala,
                nomePessoa,
                nomeSetor,
                jornada.Data.ToString("dd/MM/yyyy"),
                nomeTurno,
                escala.EntradaPrevista.ToString(@"hh\:mm"),
                escala.SaidaPrevista.ToString(@"hh\:mm"),
                CASE.Utils.TextoInterface.FormatarValorBanco(escala.Situacao));
            linha.Height = 50;
            linha.Tag = escala;
            return linha;
        }

        private void PreencherFiltros()
        {
            string setorSelecionado = cmbFiltroEscala.Text;
            string turnoSelecionado = cmbFiltroTurnoEscala.Text;

            cmbFiltroEscala.Items.Clear();
            cmbFiltroTurnoEscala.Items.Clear();
            cmbFiltroEscala.Items.Add("Todos os setores");
            cmbFiltroTurnoEscala.Items.Add("Todos os turnos");

            foreach (DataGridViewRow linha in dgvEscala.Rows)
            {
                AdicionarFiltroSeAusente(cmbFiltroEscala, Convert.ToString(linha.Cells[1].Value));
                AdicionarFiltroSeAusente(cmbFiltroTurnoEscala, Convert.ToString(linha.Cells[3].Value));
            }

            SelecionarFiltro(cmbFiltroEscala, setorSelecionado, "Todos os setores");
            SelecionarFiltro(cmbFiltroTurnoEscala, turnoSelecionado, "Todos os turnos");
        }

        private static void AdicionarFiltroSeAusente(ComboBox combo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return;
            }

            if (!combo.Items.Contains(valor))
            {
                combo.Items.Add(valor);
            }
        }

        private static void SelecionarFiltro(ComboBox combo, string valor, string padrao)
        {
            int indice = combo.Items.IndexOf(valor);

            if (indice < 0)
            {
                indice = combo.Items.IndexOf(padrao);
            }

            if (indice >= 0)
            {
                combo.SelectedIndex = indice;
            }
        }

        private void AplicarFiltros()
        {
            if (cmbFiltroEscala.SelectedIndex < 0 || cmbFiltroTurnoEscala.SelectedIndex < 0)
            {
                return;
            }

            foreach (DataGridViewRow linha in dgvEscala.Rows)
            {
                bool setorCorresponde = cmbFiltroEscala.SelectedIndex == 0 ||
                    Convert.ToString(linha.Cells[1].Value) == cmbFiltroEscala.Text;
                bool turnoCorresponde = cmbFiltroTurnoEscala.SelectedIndex == 0 ||
                    Convert.ToString(linha.Cells[3].Value) == cmbFiltroTurnoEscala.Text;
                linha.Visible = setorCorresponde && turnoCorresponde &&
                    VisualUtils.LinhaCorrespondePesquisa(linha, pesquisa.Text.Trim());
            }

            dgvEscala.ClearSelection();
        }
    }
}
