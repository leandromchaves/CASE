using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class SolicitacoesControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento eventoSelecionado;
        private readonly SolicitacaoService solicitacaoService = new SolicitacaoService();
        private bool gerenteAutorizado;
        private bool administrador;
        private bool exibindoPedidosCentrais;
        private bool mostrandoReposicoesAdministrativas;
        private TextBox pesquisa;
        private readonly Button btnAtualizarSolicitacoes = new Button();

        public event Action<int> AnalisarPedidoCentral;

        public SolicitacoesControl() : this(null, null)
        {
        }

        public SolicitacoesControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
            pesquisa = VisualUtils.AdicionarPesquisa(this, "txtPesquisarSolicitacao", "Pesquisar pedido ou produto",
                texto => AplicarFiltros());
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            cmbFiltroSolicitacoes.Items.Clear();
            cmbStatusListaSolicitacoes.Items.Clear();
            btnDetalhesSolicitacao.Text = "Detalhes / analisar";
            dgvSolicitacoesQuermesse.CellDoubleClick += dgvSolicitacoesQuermesse_CellDoubleClick;
            ConfigurarBotaoAtualizar();

            if (usuarioAutenticado == null)
            {
                btnNovaSolicitacao.Enabled = false;
                btnDetalhesSolicitacao.Enabled = false;
                labelAviso.Text = "Entre no sistema para consultar solicitações.";
                return;
            }

            try
            {
                string nomePerfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(
                    usuarioAutenticado,
                    out nomePerfil);
                gerenteAutorizado = string.Equals(
                    nomePerfil,
                    "Gerente",
                    StringComparison.OrdinalIgnoreCase);
                administrador = string.Equals(
                    nomePerfil,
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                gerenteAutorizado = false;
            }

            if (administrador || gerenteAutorizado)
            {
                Button alternar = new Button
                {
                    Name = administrador ? "btnAlternarSolicitacoesAdmin" : "btnAlternarSolicitacoesGerente",
                    Text = administrador ? "Reposições dos setores" : "Pedidos ao central",
                    Bounds = new Rectangle(btnNovaSolicitacao.Left - 175, btnNovaSolicitacao.Top, 160, 40),
                    BackColor = Color.White,
                    ForeColor = VisualUtils.Texto
                };
                btnNovaSolicitacao.Parent.Controls.Add(alternar);
                VisualUtils.EstilizarBotao(alternar);
                alternar.Click += (s, e) =>
                {
                    if (administrador)
                    {
                        mostrandoReposicoesAdministrativas = !mostrandoReposicoesAdministrativas;
                        alternar.Text = mostrandoReposicoesAdministrativas ? "Pedidos ao central" : "Reposições dos setores";
                    }
                    else
                    {
                        exibindoPedidosCentrais = !exibindoPedidosCentrais;
                        AtualizarTextoBotaoAlternar();
                    }
                    CarregarSolicitacoes();
                };
            }

            CarregarSolicitacoes();
        }

        public void ExibirPedidosCentrais()
        {
            if (!administrador && !gerenteAutorizado)
                throw new UnauthorizedAccessException("Sua conta não pode consultar pedidos ao estoque central.");
            exibindoPedidosCentrais = true;
            mostrandoReposicoesAdministrativas = false;
            Control[] alternar = Controls.Find(administrador ? "btnAlternarSolicitacoesAdmin" : "btnAlternarSolicitacoesGerente", true);
            if (alternar.Length > 0)
                alternar[0].Text = "Reposições dos setores";
            CarregarSolicitacoes();
        }

        private void btnNovaSolicitacao_Click(object sender, EventArgs e)
        {
            if (administrador && !mostrandoReposicoesAdministrativas)
            {
                CarregarSolicitacoes();
                return;
            }

            if (administrador)
            {
                using (SolicitacaoForm formulario = new SolicitacaoForm(usuarioAutenticado))
                {
                    if (formulario.ShowDialog(this) == DialogResult.OK)
                        CarregarSolicitacoes();
                }
                return;
            }

            if (gerenteAutorizado)
            {
                Evento eventoDoPedido = ObterEventoParaPedidoCentral();
                if (eventoDoPedido == null)
                {
                    return;
                }

                using (SolicitacaoEstoqueCentralForm formulario =
                    new SolicitacaoEstoqueCentralForm(usuarioAutenticado, eventoDoPedido))
                {
                    if (formulario.ShowDialog(this) == DialogResult.OK)
                    {
                        exibindoPedidosCentrais = true;
                        AtualizarTextoBotaoAlternar();
                        CarregarSolicitacoes();
                    }
                }

                return;
            }

            using (SolicitacaoForm formulario = new SolicitacaoForm(usuarioAutenticado))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarSolicitacoes();
                }
            }
        }

        private Evento ObterEventoParaPedidoCentral()
        {
            if (eventoSelecionado != null)
            {
                return eventoSelecionado;
            }

            List<Evento> eventos;
            try
            {
                eventos = new EventoService().Listar(usuarioAutenticado);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível consultar os eventos sob sua responsabilidade.";
                return null;
            }

            if (eventos.Count == 0)
            {
                labelAviso.Text = "Nenhum evento está atribuído à sua conta. Peça ao administrador para definir você como responsável.";
                return null;
            }

            if (eventos.Count == 1)
            {
                return eventos[0];
            }

            using (Form dialogo = new Form())
            using (ComboBox selecao = new ComboBox())
            using (Button confirmar = new Button())
            using (Button cancelar = new Button())
            {
                dialogo.Text = "Escolher evento";
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialogo.MinimizeBox = false;
                dialogo.MaximizeBox = false;
                dialogo.ClientSize = new Size(440, 155);
                dialogo.Font = new Font("Segoe UI", 9F);

                Label instrucao = new Label
                {
                    Text = "Selecione o evento que precisa de produtos do estoque central.",
                    AutoSize = false,
                    Bounds = new Rectangle(20, 18, 400, 38),
                    ForeColor = VisualUtils.Marinho
                };
                selecao.Name = "cmbEventoPedidoCentral";
                selecao.DropDownStyle = ComboBoxStyle.DropDownList;
                selecao.DisplayMember = "Nome";
                selecao.DataSource = eventos;
                selecao.SetBounds(20, 62, 400, 32);

                confirmar.Text = "Continuar";
                confirmar.DialogResult = DialogResult.OK;
                confirmar.SetBounds(238, 108, 88, 32);
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(332, 108, 88, 32);

                dialogo.Controls.Add(instrucao);
                dialogo.Controls.Add(selecao);
                dialogo.Controls.Add(confirmar);
                dialogo.Controls.Add(cancelar);
                FrontendTheme.Aplicar(dialogo);
                VisualUtils.EstilizarSelecao(selecao);
                VisualUtils.EstilizarBotao(confirmar);
                VisualUtils.EstilizarBotao(cancelar);
                dialogo.AcceptButton = confirmar;
                dialogo.CancelButton = cancelar;

                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    return selecao.SelectedItem as Evento;
                }
            }

            return null;
        }

        private void AtualizarTextoBotaoAlternar()
        {
            Control[] alternar = Controls.Find(
                "btnAlternarSolicitacoesGerente",
                true);
            if (alternar.Length > 0)
            {
                alternar[0].Text = exibindoPedidosCentrais
                    ? "Reposições dos setores"
                    : "Pedidos ao central";
            }
        }

        private void btnDetalhesSolicitacao_Click(object sender, EventArgs e)
        {
            AbrirSolicitacaoSelecionada();
        }

        private void ConfigurarBotaoAtualizar()
        {
            btnAtualizarSolicitacoes.Name = "btnAtualizarSolicitacoes";
            btnAtualizarSolicitacoes.Text = "Atualizar lista";
            btnAtualizarSolicitacoes.Bounds = new Rectangle(
                btnDetalhesSolicitacao.Left - 145,
                btnDetalhesSolicitacao.Top,
                130,
                btnDetalhesSolicitacao.Height);
            btnAtualizarSolicitacoes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAtualizarSolicitacoes.Enabled = usuarioAutenticado != null;
            btnAtualizarSolicitacoes.Click += (s, e) => CarregarSolicitacoes();
            panelListaSolicitacoes.Controls.Add(btnAtualizarSolicitacoes);
            lblAcompanhamentodospedidos.Width = btnAtualizarSolicitacoes.Left -
                lblAcompanhamentodospedidos.Left - 16;
            VisualUtils.EstilizarBotao(btnAtualizarSolicitacoes);
        }

        private void dgvSolicitacoesQuermesse_CellDoubleClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                AbrirSolicitacaoSelecionada();
            }
        }

        private void AbrirSolicitacaoSelecionada()
        {

            if (dgvSolicitacoesQuermesse.SelectedRows.Count == 0)
            {
                labelAviso.Text = "Selecione uma solicitação.";
                return;
            }

            if (exibindoPedidosCentrais)
            {
                SolicitacaoEstoqueCentral pedidoCentral =
                    dgvSolicitacoesQuermesse.SelectedRows[0].Tag as SolicitacaoEstoqueCentral;

                if (pedidoCentral != null)
                {
                    if (administrador && AnalisarPedidoCentral != null)
                    {
                        SolicitacaoEstoqueCentralService service = new SolicitacaoEstoqueCentralService();
                        if (pedidoCentral.Situacao == SituacoesSolicitacao.Pendente ||
                            (pedidoCentral.Situacao == SituacoesSolicitacao.Aprovada &&
                            !service.BuscarIdTransferenciaAtendimento(usuarioAutenticado,
                                pedidoCentral.IdSolicitacaoEstoqueCentral).HasValue))
                        {
                            AnalisarPedidoCentral(pedidoCentral.IdSolicitacaoEstoqueCentral);
                        }
                        else
                        {
                            MostrarDetalhesCentral(pedidoCentral.IdSolicitacaoEstoqueCentral);
                        }
                        CarregarSolicitacoes();
                    }
                    else
                    {
                        MostrarDetalhesCentral(pedidoCentral.IdSolicitacaoEstoqueCentral);
                    }
                }

                return;
            }

            Solicitacao solicitacao = dgvSolicitacoesQuermesse.SelectedRows[0].Tag as Solicitacao;

            if (solicitacao == null)
            {
                return;
            }

            if (string.Equals(
                solicitacao.Situacao,
                SituacoesSolicitacao.Pendente,
                StringComparison.OrdinalIgnoreCase))
            {
                if (gerenteAutorizado || administrador)
                {
                    DecidirSolicitacao(solicitacao);
                }
                else
                {
                    MostrarDetalhes(solicitacao.IdSolicitacao);
                }

                return;
            }

            if (string.Equals(
                solicitacao.Situacao,
                SituacoesSolicitacao.Aprovada,
                StringComparison.OrdinalIgnoreCase))
            {
                if (!gerenteAutorizado && !administrador)
                {
                    MostrarDetalhes(solicitacao.IdSolicitacao);
                    return;
                }

                try
                {
                    using (var formulario = new DetalhesReposicaoForm(usuarioAutenticado, solicitacao.IdSolicitacao, true))
                        if (formulario.ShowDialog(this) == DialogResult.Yes)
                            AtenderSolicitacao(solicitacao);
                }
                catch (Exception erro)
                {
                    FeedbackUsuario.ExibirErro(this, erro, "Não foi possível abrir os detalhes da reposição.", labelAviso);
                }

                return;
            }

            MostrarDetalhes(solicitacao.IdSolicitacao);
        }

        public void AbrirPedidoDeReposicao(int idSolicitacao)
        {
            exibindoPedidosCentrais = false;
            CarregarSolicitacoes();
            foreach (DataGridViewRow linha in dgvSolicitacoesQuermesse.Rows)
            {
                Solicitacao solicitacao = linha.Tag as Solicitacao;
                if (solicitacao != null && solicitacao.IdSolicitacao == idSolicitacao)
                {
                    dgvSolicitacoesQuermesse.ClearSelection();
                    linha.Selected = true;
                    dgvSolicitacoesQuermesse.CurrentCell = linha.Cells[0];
                    AbrirSolicitacaoSelecionada();
                    return;
                }
            }
            labelAviso.Text = "O pedido não está mais disponível. Atualize as pendências.";
        }

        private void DecidirSolicitacao(Solicitacao solicitacao)
        {
            try
            {
                DialogResult resposta;
                using (var formulario = new DetalhesReposicaoForm(usuarioAutenticado, solicitacao.IdSolicitacao, true))
                    resposta = formulario.ShowDialog(this);
                if (resposta == DialogResult.Yes)
                {
                    solicitacaoService.Aprovar(
                        usuarioAutenticado,
                        solicitacao.IdSolicitacao,
                        null);
                    CarregarSolicitacoes();
                    labelAviso.Text = "Solicitação aprovada. O estoque não foi movimentado.";
                    FeedbackUsuario.Exibir(this, "Solicitação aprovada. Abra o pedido para efetivar a transferência ao setor.", TipoFeedback.Sucesso);
                }
                else if (resposta == DialogResult.No)
                {
                    string justificativa = SolicitarJustificativa();

                    if (string.IsNullOrWhiteSpace(justificativa))
                    {
                        return;
                    }

                    solicitacaoService.Rejeitar(
                        usuarioAutenticado,
                        solicitacao.IdSolicitacao,
                        justificativa);
                    CarregarSolicitacoes();
                    labelAviso.Text = "Solicitação rejeitada.";
                    FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
                }
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não pode decidir esta solicitação.";
            }
            catch (InvalidOperationException ex)
            {
                CarregarSolicitacoes();
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível atualizar as solicitações.", labelAviso);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível registrar a decisão.";
            }
        }

        private string SolicitarJustificativa()
        {
            using (Form dialogo = new Form())
            using (TextBox campo = new TextBox())
            using (Button confirmar = new Button())
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

                confirmar.Text = "Rejeitar";
                Label erroMotivo = new Label { Name = "lblErroJustificativa", ForeColor = Color.Firebrick };
                erroMotivo.SetBounds(12, 85, 396, 17);
                dialogo.Controls.Add(erroMotivo);
                confirmar.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(campo.Text))
                    {
                        erroMotivo.Text = "Informe o motivo da rejeição.";
                        campo.Focus();
                        return;
                    }
                    dialogo.DialogResult = DialogResult.OK;
                };
                confirmar.SetBounds(222, 102, 88, 30);

                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.SetBounds(320, 102, 88, 30);

                dialogo.Controls.Add(campo);
                dialogo.Controls.Add(confirmar);
                dialogo.Controls.Add(cancelar);
                FrontendTheme.Aplicar(dialogo);
                dialogo.AcceptButton = confirmar;
                dialogo.CancelButton = cancelar;

                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    return campo.Text.Trim();
                }
            }

            return string.Empty;
        }

        private void AtenderSolicitacao(Solicitacao solicitacao)
        {
            try
            {
                new TransferenciaEstoqueService().AtenderSolicitacaoMembro(
                    usuarioAutenticado,
                    solicitacao.IdSolicitacao);
                CarregarSolicitacoes();
                labelAviso.Text = "Solicitação atendida integralmente por transferência.";
                FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não pode atender esta solicitação.";
            }
            catch (InvalidOperationException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível concluir a solicitação.", labelAviso);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível atender a solicitação.";
            }
        }

        private void MostrarDetalhes(int idSolicitacao)
        {
            try
            {
                using (var formulario = new DetalhesReposicaoForm(usuarioAutenticado, idSolicitacao, false))
                    formulario.ShowDialog(this);
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível abrir os detalhes da reposição.", labelAviso);
            }
        }

        private void Filtros_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        internal void AtualizarConsulta()
        {
            string setor = cmbFiltroSolicitacoes.Text;
            string situacao = cmbStatusListaSolicitacoes.Text;
            CarregarSolicitacoes();
            int indiceSetor = cmbFiltroSolicitacoes.FindStringExact(setor);
            int indiceSituacao = cmbStatusListaSolicitacoes.FindStringExact(situacao);
            if (indiceSetor >= 0) cmbFiltroSolicitacoes.SelectedIndex = indiceSetor;
            if (indiceSituacao >= 0) cmbStatusListaSolicitacoes.SelectedIndex = indiceSituacao;
        }

        private void CarregarSolicitacoes()
        {
            btnNovaSolicitacao.Enabled = false;

            if (administrador && !mostrandoReposicoesAdministrativas)
            {
                CarregarPedidosParaAdministrador();
                return;
            }

            if (administrador)
            {
                exibindoPedidosCentrais = false;
                btnNovaSolicitacao.Text = "+ Nova reposição";
                btnDetalhesSolicitacao.Text = "Analisar / atender";
                btnDetalhesSolicitacao.Enabled = true;
                lblSubtitulo.Text = "Analise, aprove ou rejeite reposições dos eventos e atenda os pedidos aprovados.";
                lblSetor.Text = "Setor";
                colSolicitacoesQuermesseSetor.HeaderText = "Evento / setor";
                colSolicitacoesQuermesseQtd.Visible = false;
            }

            if (gerenteAutorizado && eventoSelecionado == null && !exibindoPedidosCentrais)
            {
                btnNovaSolicitacao.Text = "+ Solicitar ao central";
                btnNovaSolicitacao.Enabled = true;
                btnDetalhesSolicitacao.Text = "Detalhes / analisar";
                lblSubtitulo.Text = "Acompanhe as reposições dos setores e peça produtos ao estoque central.";
                labelAviso.Text = "Selecione um evento para ver as reposições dos setores. O botão ao lado permite pedir produtos ao estoque central.";
                dgvSolicitacoesQuermesse.Rows.Clear();
                return;
            }

            if (gerenteAutorizado && exibindoPedidosCentrais)
            {
                CarregarPedidosCentrais();
                return;
            }

            try
            {
                lblSetor.Text = "Setor";
                colSolicitacoesQuermesseSetor.HeaderText = "Evento / setor";
                colSolicitacoesQuermesseQtd.Visible = false;
                List<Solicitacao> solicitacoes = ObterSolicitacoesPermitidas();
                var linhasCarregadas = new List<DataGridViewRow>();

                foreach (Solicitacao solicitacao in solicitacoes)
                {
                    SolicitacaoService.ResumoSolicitacao resumo = solicitacaoService.ObterResumo(
                        usuarioAutenticado,
                        solicitacao.IdSolicitacao);
                    var linhaCarregada = new DataGridViewRow();
                    linhaCarregada.CreateCells(dgvSolicitacoesQuermesse,
                        solicitacao.DataHora.ToString("dd/MM/yyyy HH:mm"),
                        resumo.Solicitante,
                        resumo.Evento + " · " + resumo.Setor,
                        CASE.Utils.TextoInterface.FormatarValorBanco(solicitacao.Tipo),
                        resumo.Produtos,
                        string.Empty,
                        CASE.Utils.TextoInterface.FormatarValorBanco(solicitacao.Situacao));
                    linhaCarregada.Tag = solicitacao;
                    linhasCarregadas.Add(linhaCarregada);
                }

                bool membroComEscala = PodeSolicitarComoMembro();
                dgvSolicitacoesQuermesse.Rows.Clear();
                dgvSolicitacoesQuermesse.Rows.AddRange(linhasCarregadas.ToArray());
                btnNovaSolicitacao.Enabled = administrador || gerenteAutorizado || membroComEscala;

                if (gerenteAutorizado)
                {
                    btnNovaSolicitacao.Text = "+ Solicitar ao central";
                    btnNovaSolicitacao.Enabled = true;
                    btnDetalhesSolicitacao.Text = "Detalhes / analisar";
                    lblSubtitulo.Text = "Acompanhe as reposições dos membros deste evento e peça produtos ao estoque central.";
                }
                else if (!administrador)
                {
                    btnNovaSolicitacao.Text = "+ Nova solicitação";
                    btnDetalhesSolicitacao.Text = "Consultar detalhes";
                }
                ConfigurarFiltros();
                AplicarFiltros();
                labelAviso.Text = !administrador && !gerenteAutorizado && !membroComEscala
                    ? "Sem escala atribuída. Se você já está vinculado ao evento, o estoque geral aparece em Estoque; para solicitar reposição de um setor, o gerente precisa definir seu setor, data e turno."
                    : solicitacoes.Count + " solicitação(ões) carregada(s).";
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não possui acesso a estas solicitações.";
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar as solicitações.";
            }
        }

        private void CarregarPedidosParaAdministrador()
        {
            exibindoPedidosCentrais = true;
            btnNovaSolicitacao.Text = "Atualizar pedidos";
            btnNovaSolicitacao.Enabled = true;
            btnDetalhesSolicitacao.Text = "Detalhes / analisar / atender";
            btnDetalhesSolicitacao.Enabled = false;
            lblSubtitulo.Text = "Liberações e devoluções do estoque central: aprovação, rejeição e atendimento integral";
            lblSetor.Text = "Evento";
            colSolicitacoesQuermesseSetor.HeaderText = "Evento";
            colSolicitacoesQuermesseQtd.Visible = true;
            colSolicitacoesQuermesseQtd.HeaderText = "Atendimento";

            try
            {
                SolicitacaoEstoqueCentralService service = new SolicitacaoEstoqueCentralService();
                List<SolicitacaoEstoqueCentral> pedidos = service.ListarParaAdministrador(usuarioAutenticado);
                if (eventoSelecionado != null)
                    pedidos = pedidos.FindAll(pedido => pedido.IdEvento == eventoSelecionado.IdEvento);

                Dictionary<int, string> solicitantes = new Dictionary<int, string>();
                foreach (Usuario usuario in new UsuarioService().ListarUsuariosAdministrativos(usuarioAutenticado))
                {
                    solicitantes[usuario.IdUsuario] = usuario.Login;
                }
                Dictionary<int, string> eventos = new Dictionary<int, string>();
                foreach (Evento evento in new EventoService().Listar(usuarioAutenticado))
                {
                    eventos[evento.IdEvento] = evento.Nome;
                }

                var linhasCarregadas = new List<DataGridViewRow>();
                foreach (SolicitacaoEstoqueCentral pedido in pedidos)
                {
                    string solicitante;
                    string evento;
                    solicitantes.TryGetValue(pedido.IdUsuarioSolicitante, out solicitante);
                    eventos.TryGetValue(pedido.IdEvento, out evento);
                    var linhaCarregada = new DataGridViewRow();
                    linhaCarregada.CreateCells(dgvSolicitacoesQuermesse,
                        pedido.DataHora.ToString("dd/MM/yyyy HH:mm"),
                        string.IsNullOrWhiteSpace(solicitante) ? "Usuário indisponível" : solicitante,
                        evento ?? "Evento " + pedido.IdEvento,
                        CASE.Utils.TextoInterface.FormatarValorBanco(pedido.Tipo),
                        MontarResumoItensCentrais(pedido),
                        service.BuscarIdTransferenciaAtendimento(usuarioAutenticado,
                            pedido.IdSolicitacaoEstoqueCentral).HasValue ? "Concluído" : "—",
                        CASE.Utils.TextoInterface.FormatarValorBanco(pedido.Situacao));
                    linhaCarregada.Tag = pedido;
                    linhasCarregadas.Add(linhaCarregada);
                }

                dgvSolicitacoesQuermesse.Rows.Clear();
                dgvSolicitacoesQuermesse.Rows.AddRange(linhasCarregadas.ToArray());
                ConfigurarFiltros();
                cmbFiltroSolicitacoes.Items[0] = "Todos os eventos";
                AplicarFiltros();
                btnDetalhesSolicitacao.Enabled = pedidos.Count > 0;
                labelAviso.Text = pedidos.Count == 0
                    ? "Nenhum pedido ao estoque central. Use Atualizar pedidos para consultar novamente."
                    : "Todos os pedidos autorizados, incluindo o histórico. Selecione uma linha para consultar, analisar ou atender.";
            }
            catch (UnauthorizedAccessException)
            {
                btnNovaSolicitacao.Enabled = false;
                labelAviso.Text = "Somente o administrador pode analisar os pedidos ao estoque central.";
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar os pedidos ao estoque central. Tente atualizar novamente.";
            }
        }

        private void CarregarPedidosCentrais()
        {
            try
            {
                List<SolicitacaoEstoqueCentral> pedidos =
                    new SolicitacaoEstoqueCentralService().ListarDoGerente(
                        usuarioAutenticado);
                List<Evento> eventos = new EventoService().Listar(usuarioAutenticado);
                Dictionary<int, string> nomesEventos = new Dictionary<int, string>();
                foreach (Evento evento in eventos)
                {
                    nomesEventos[evento.IdEvento] = evento.Nome;
                }

                if (eventoSelecionado != null)
                {
                    pedidos = pedidos.FindAll(pedido =>
                        pedido.IdEvento == eventoSelecionado.IdEvento);
                }

                var linhasCarregadas = new List<DataGridViewRow>();
                foreach (SolicitacaoEstoqueCentral pedido in pedidos)
                {
                    string resumo = MontarResumoItensCentrais(pedido);
                    string nomeEvento;
                    if (!nomesEventos.TryGetValue(pedido.IdEvento, out nomeEvento))
                    {
                        nomeEvento = "Evento " + pedido.IdEvento;
                    }
                    var linhaCarregada = new DataGridViewRow();
                    linhaCarregada.CreateCells(dgvSolicitacoesQuermesse,
                        pedido.DataHora.ToString("dd/MM/yyyy HH:mm"),
                        new SolicitacaoEstoqueCentralService().ConsultarLoginSolicitante(usuarioAutenticado, pedido.IdSolicitacaoEstoqueCentral),
                        nomeEvento,
                        CASE.Utils.TextoInterface.FormatarValorBanco(pedido.Tipo),
                        resumo,
                        string.Empty,
                        CASE.Utils.TextoInterface.FormatarValorBanco(pedido.Situacao));
                    linhaCarregada.Tag = pedido;
                    linhasCarregadas.Add(linhaCarregada);
                }

                dgvSolicitacoesQuermesse.Rows.Clear();
                dgvSolicitacoesQuermesse.Rows.AddRange(linhasCarregadas.ToArray());
                exibindoPedidosCentrais = true;
                btnNovaSolicitacao.Text = "+ Solicitar ao central";
                btnNovaSolicitacao.Enabled = true;
                btnDetalhesSolicitacao.Text = "Consultar detalhes";
                lblSetor.Text = "Evento";
                lblSubtitulo.Text = "Acompanhe os pedidos de liberação ou devolução ao estoque central.";
                colSolicitacoesQuermesseSetor.HeaderText = "Evento";
                colSolicitacoesQuermesseQtd.Visible = false;
                ConfigurarFiltros();
                if (cmbFiltroSolicitacoes.Items.Count > 0)
                {
                    cmbFiltroSolicitacoes.Items[0] = "Todos os eventos";
                }
                AplicarFiltros();
                labelAviso.Text = pedidos.Count + " pedido(s) ao estoque central.";
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Somente o gerente responsável pode consultar seus pedidos ao central.";
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar os pedidos ao estoque central.";
            }
        }

        private string MontarResumoItensCentrais(SolicitacaoEstoqueCentral pedido)
        {
            SolicitacaoEstoqueCentralService service = new SolicitacaoEstoqueCentralService();
            List<SolicitacaoEstoqueCentralItem> itens = service.ListarItens(
                usuarioAutenticado,
                pedido.IdSolicitacaoEstoqueCentral);
            List<string> resumoItens = new List<string>();

            foreach (SolicitacaoEstoqueCentralItem item in itens)
            {
                Produto produto = new ProdutoService().BuscarPorId(
                    usuarioAutenticado,
                    item.IdProduto);
                string nomeProduto = "Produto " + item.IdProduto;

                if (produto != null)
                {
                    nomeProduto = produto.Nome;
                }

                resumoItens.Add(nomeProduto + " (" + item.Quantidade + ")");
            }

            return string.Join(", ", resumoItens);
        }

        private void MostrarDetalhesCentral(int idSolicitacao)
        {
            try
            {
                using (var formulario = new DetalhesPedidoCentralForm(usuarioAutenticado, idSolicitacao, false))
                    formulario.ShowDialog(this);
            }
            catch (Exception erro)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar os detalhes do pedido.", labelAviso);
            }
        }
        private List<Solicitacao> ObterSolicitacoesPermitidas()
        {
            if (administrador && eventoSelecionado == null)
            {
                List<Solicitacao> solicitacoes = new List<Solicitacao>();
                foreach (Evento evento in new EventoService().Listar(usuarioAutenticado))
                {
                    solicitacoes.AddRange(solicitacaoService.ListarDoEvento(usuarioAutenticado, evento.IdEvento));
                }
                return solicitacoes;
            }

            if (eventoSelecionado != null)
            {
                try
                {
                    return solicitacaoService.ListarDoEvento(
                        usuarioAutenticado,
                        eventoSelecionado.IdEvento);
                }
                catch (UnauthorizedAccessException)
                {
                    return solicitacaoService.ListarMinhas(usuarioAutenticado);
                }
            }

            return solicitacaoService.ListarMinhas(usuarioAutenticado);
        }

        private bool PodeSolicitarComoMembro()
        {
            try
            {
                List<Escala> escalas = new EscalaService().ListarMinhas(usuarioAutenticado);
                foreach (Escala escala in escalas)
                {
                    if (!string.Equals(
                        escala.Situacao,
                        SituacoesEscala.Cancelada,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void ConfigurarFiltros()
        {
            string setorSelecionado = cmbFiltroSolicitacoes.Text;
            cmbFiltroSolicitacoes.Items.Clear();
            cmbFiltroSolicitacoes.Items.Add("Todos os setores");

            foreach (DataGridViewRow linha in dgvSolicitacoesQuermesse.Rows)
            {
                string setor = Convert.ToString(linha.Cells[2].Value);

                if (!cmbFiltroSolicitacoes.Items.Contains(setor))
                {
                    cmbFiltroSolicitacoes.Items.Add(setor);
                }
            }

            int indiceSetor = cmbFiltroSolicitacoes.Items.IndexOf(setorSelecionado);

            if (indiceSetor < 0)
            {
                indiceSetor = 0;
            }

            cmbFiltroSolicitacoes.SelectedIndex = indiceSetor;

            cmbStatusListaSolicitacoes.Items.Clear();
            cmbStatusListaSolicitacoes.Items.Add("Todas");
            cmbStatusListaSolicitacoes.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(SituacoesSolicitacao.Pendente));
            cmbStatusListaSolicitacoes.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(SituacoesSolicitacao.Aprovada));
            cmbStatusListaSolicitacoes.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(SituacoesSolicitacao.Atendida));
            cmbStatusListaSolicitacoes.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(SituacoesSolicitacao.Rejeitada));
            cmbStatusListaSolicitacoes.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(SituacoesSolicitacao.Cancelada));
            cmbStatusListaSolicitacoes.SelectedIndex = 0;
        }

        private void AplicarFiltros()
        {
            if (cmbFiltroSolicitacoes.SelectedIndex < 0 ||
                cmbStatusListaSolicitacoes.SelectedIndex < 0)
            {
                return;
            }

            foreach (DataGridViewRow linha in dgvSolicitacoesQuermesse.Rows)
            {
                bool setorCorresponde = cmbFiltroSolicitacoes.SelectedIndex == 0 ||
                    Convert.ToString(linha.Cells[2].Value) == cmbFiltroSolicitacoes.Text;
                bool statusCorresponde = cmbStatusListaSolicitacoes.SelectedIndex == 0 ||
                    string.Equals(
                        Convert.ToString(linha.Cells[6].Value),
                        cmbStatusListaSolicitacoes.Text,
                        StringComparison.OrdinalIgnoreCase);
                linha.Visible = setorCorresponde && statusCorresponde &&
                    VisualUtils.LinhaCorrespondePesquisa(linha, pesquisa.Text.Trim());
            }

            dgvSolicitacoesQuermesse.ClearSelection();
        }
    }
}
