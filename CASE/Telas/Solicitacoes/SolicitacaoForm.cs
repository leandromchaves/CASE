using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class SolicitacaoForm : Form
    {
        private class EscalaOpcao
        {
            public Escala Escala { get; set; }
            public string Descricao { get; set; }
        }

        private readonly Usuario usuarioAutenticado;
        private readonly int? idSolicitacaoDetalhe;
        private readonly SolicitacaoService solicitacaoService = new SolicitacaoService();
        private readonly EscalaService escalaService = new EscalaService();
        private readonly EventoService eventoService = new EventoService();
        private readonly ComboBox comboProdutos = new ComboBox();

        public SolicitacaoForm() : this(null, null)
        {
        }

        public SolicitacaoForm(bool detalhe) : this(null, null)
        {
            if (detalhe)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Entre no sistema para consultar os detalhes.";
            }
        }

        public SolicitacaoForm(Usuario usuario) : this(usuario, null)
        {
        }

        public SolicitacaoForm(Usuario usuario, int? idSolicitacao)
        {
            InitializeComponent();
            IconoAplicacao.Aplicar(this);
            FrontendTheme.Aplicar(this);
            usuarioAutenticado = usuario;
            idSolicitacaoDetalhe = idSolicitacao;

            ConfigurarComboProdutos();
            cmbTipoSolicitacao.Items.Clear();
            cmbTipoSolicitacao.Items.Add("Reposição");
            cmbTipoSolicitacao.SelectedIndex = 0;
            cmbStatusSolicitacao.Visible = false;
            cmbStatusSolicitacao.Items.Clear();
            lblSituacao.Visible = false;
            if (!idSolicitacaoDetalhe.HasValue) OrganizarNovaSolicitacao();

            if (usuarioAutenticado == null)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Entre no sistema para registrar uma solicitação.";
                return;
            }

            if (idSolicitacaoDetalhe.HasValue)
            {
                CarregarDetalhes();
            }
            else
            {
                PrepararNovaSolicitacao();
            }
        }

        private void ConfigurarComboProdutos()
        {
            comboProdutos.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProdutos.Font = txtProdutoSolicitacao.Font;
            comboProdutos.Name = "cmbProdutoSolicitacao";
            comboProdutos.BackColor = txtProdutoSolicitacao.BackColor;
            comboProdutos.ForeColor = txtProdutoSolicitacao.ForeColor;
            Control areaProduto = txtProdutoSolicitacao.Parent != panelFormulario
                ? txtProdutoSolicitacao.Parent : (Control)txtProdutoSolicitacao;
            comboProdutos.Bounds = areaProduto.Bounds;
            comboProdutos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboProdutos.DisplayMember = "Nome";
            comboProdutos.Visible = false;
            comboProdutos.SelectedIndexChanged += comboProdutos_SelectedIndexChanged;
            panelFormulario.Controls.Add(comboProdutos);
            comboProdutos.BringToFront();
            txtProdutoSolicitacao.Visible = false;
            if (areaProduto != txtProdutoSolicitacao) areaProduto.Visible = false;
            VisualUtils.EstilizarSelecao(comboProdutos);
            lblProduto.Text = "Produto disponível no setor";
            lblSetor.Text = "Evento, setor e escala";
            cmbSetorSolicitacao.Items.Clear();
            cmbSetorSolicitacao.SelectedIndexChanged += cmbSetorSolicitacao_SelectedIndexChanged;
        }

        private void OrganizarNovaSolicitacao()
        {
            Text = lblTitulo.Text = "Solicitar reposição";
            ClientSize = new System.Drawing.Size(640, 650);
            panelFormulario.Height = 618;
            cmbTipoSolicitacao.Visible = lblTipo.Visible = false;
            var orientacao = new Label
            {
                Text = "Peça produtos do estoque geral para abastecer seu setor.",
                ForeColor = VisualUtils.TextoSuave,
                Font = new System.Drawing.Font("Segoe UI", 10),
                Bounds = new System.Drawing.Rectangle(28, 70, 550, 30)
            };
            panelFormulario.Controls.Add(orientacao);
            lblSetor.Text = "Evento, setor e escala *";
            lblSetor.Top = 108;
            cmbSetorSolicitacao.SetBounds(28, 132, 550, 30);
            lblSolicitante.Text = "Solicitante · preenchido automaticamente";
            lblSolicitante.Top = 177;
            PosicionarCampo(txtSolicitante, 201, 38);
            lblProduto.Text = "Produto para reposição *";
            lblProduto.Top = 251;
            comboProdutos.SetBounds(28, 275, 550, 30);
            lblQuantidade.Text = "Quantidade solicitada *";
            lblQuantidade.Top = 317;
            PosicionarCampo(txtQuantidadeSolicitacao, 341, 38);
            lblObservacao.Text = "Observação (opcional)";
            lblObservacao.Top = 391;
            PosicionarCampo(txtObservacaoSolicitacao, 415, 38);
            VisualUtils.DefinirPlaceholder(txtQuantidadeSolicitacao, "Ex.: 10");
            VisualUtils.DefinirPlaceholder(txtObservacaoSolicitacao, "Informe algum detalhe que ajude a avaliar o pedido");
            labelAvisoFormulario.SetBounds(28, 477, 550, 58);
            btnCancelar.Top = btnSalvarVisual.Top = 558;
            btnSalvarVisual.Text = "Enviar pedido";
            comboProdutos.TabIndex = 3;
            txtQuantidadeSolicitacao.TabIndex = 4;
            txtObservacaoSolicitacao.TabIndex = 5;
        }

        private void PosicionarCampo(TextBox campo, int topo, int altura)
        {
            Control contorno = campo.Parent != panelFormulario ? campo.Parent : (Control)campo;
            contorno.SetBounds(28, topo, 550, altura);
            VisualUtils.EstilizarCampo(campo);
        }

        private void PrepararNovaSolicitacao()
        {
            txtSolicitante.ReadOnly = true;
            txtSolicitante.Text = usuarioAutenticado.Login;
            txtSolicitante.TabStop = false;
            txtSolicitante.AccessibleDescription = "Usuário conectado que está criando a solicitação. Preenchido automaticamente e não editável.";
            cmbStatusSolicitacao.Visible = false;
            lblSituacao.Visible = false;
            txtObservacaoSolicitacao.Text = string.Empty;
            txtQuantidadeSolicitacao.Text = string.Empty;

            try
            {
                string perfil;
                Usuario contaAtual = new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                txtSolicitante.Text = contaAtual.Login;
                List<Escala> escalas = escalaService.ListarDisponiveisParaSolicitacao(usuarioAutenticado);

                foreach (Escala escala in escalas)
                {
                    if (string.Equals(escala.Situacao, SituacoesEscala.Cancelada, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    JornadaSetor jornada = new JornadaSetorService().BuscarPorId(
                        usuarioAutenticado,
                        escala.IdJornada);

                    if (jornada == null)
                    {
                        continue;
                    }

                    Setor setor = new SetorService().BuscarPorId(
                        usuarioAutenticado,
                        jornada.IdSetor);
                    Turno turno = new CASE.Repositories.TurnoRepository().BuscarPorId(
                        jornada.IdTurno);

                    if (setor == null)
                    {
                        continue;
                    }

                    Evento evento = eventoService.BuscarPorId(
                        usuarioAutenticado,
                        setor.IdEvento);
                    if (evento == null)
                    {
                        continue;
                    }

                    string nomeTurno = "Turno " + jornada.IdTurno;

                    if (turno != null)
                    {
                        nomeTurno = turno.Nome;
                    }

                    EscalaOpcao opcao = new EscalaOpcao();
                    opcao.Escala = escala;
                    opcao.Descricao = evento.Nome + " · " + setor.Nome + " · " +
                        jornada.Data.ToString("dd/MM/yyyy") + " · " + nomeTurno;
                    cmbSetorSolicitacao.Items.Add(opcao);
                }

                cmbSetorSolicitacao.DisplayMember = "Descricao";

                if (cmbSetorSolicitacao.Items.Count == 0)
                {
                    btnSalvarVisual.Enabled = false;
                    labelAvisoFormulario.Text = "Para solicitar reposição, o gerente precisa vincular você ao evento e definir seu setor, data e turno em Escala de trabalho.";
                    return;
                }

                cmbSetorSolicitacao.SelectedIndex = 0;
                CarregarProdutosDaEscalaSelecionada();
                Text = lblTitulo.Text = "Solicitar reposição";
            }
            catch (UnauthorizedAccessException)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Somente membros vinculados podem solicitar reposição.";
            }
            catch (Exception)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Não foi possível carregar suas escalas.";
            }
        }

        private void cmbSetorSolicitacao_SelectedIndexChanged(object sender, EventArgs e)
        {

            CarregarProdutosDaEscalaSelecionada();
        }

        private void CarregarProdutosDaEscalaSelecionada()
        {
            comboProdutos.Items.Clear();
            comboProdutos.Visible = false;
            btnSalvarVisual.Enabled = false;

            EscalaOpcao opcao = cmbSetorSolicitacao.SelectedItem as EscalaOpcao;

            if (opcao == null)
            {
                return;
            }

            try
            {
                List<SolicitacaoService.ProdutoSolicitacaoOpcao> produtos =
                    solicitacaoService.ListarProdutosDaEscala(
                        usuarioAutenticado,
                        opcao.Escala.IdEscala);

                foreach (SolicitacaoService.ProdutoSolicitacaoOpcao produto in produtos)
                {
                    comboProdutos.Items.Add(produto);
                }

                if (comboProdutos.Items.Count > 0)
                {
                    comboProdutos.SelectedIndex = 0;
                    comboProdutos.Visible = true;
                    btnSalvarVisual.Enabled = true;
                    AtualizarResumoEstoqueDoProduto();
                }
                else
                {
                    comboProdutos.Visible = false;
                    btnSalvarVisual.Enabled = false;
                    labelAvisoFormulario.Text = "Este setor ainda não tem produtos configurados. O responsável pode incluí-los em Equipes e setores > Produtos e preços.";
                }
            }
            catch (Exception)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Não foi possível carregar os produtos do setor.";
            }
        }

        private void comboProdutos_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarResumoEstoqueDoProduto();
        }

        private void AtualizarResumoEstoqueDoProduto()
        {
            SolicitacaoService.ProdutoSolicitacaoOpcao produto =
                comboProdutos.SelectedItem as SolicitacaoService.ProdutoSolicitacaoOpcao;

            if (produto == null)
            {
                return;
            }

            string unidade = string.IsNullOrWhiteSpace(produto.UnidadeMedida)
                ? "un"
                : produto.UnidadeMedida;

            if (produto.QuantidadeNoEstoqueGeral <= 0)
            {
                labelAvisoFormulario.Text = "Saldo do setor: " + FormatarQuantidade(produto.QuantidadeNoSetor) +
                    " " + unidade + ". Estoque geral do evento: 0 " + unidade +
                    ". Você ainda pode enviar o pedido para avaliação.";
                return;
            }

            labelAvisoFormulario.Text = "No setor: " +
                FormatarQuantidade(produto.QuantidadeNoSetor) + " " + unidade +
                " · disponível no estoque geral do evento: " +
                FormatarQuantidade(produto.QuantidadeNoEstoqueGeral) + " " + unidade + ".";
        }

        private static string FormatarQuantidade(decimal quantidade)
        {
            return quantidade.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));
        }

        private void CarregarDetalhes()
        {
            try
            {
                Solicitacao solicitacao = solicitacaoService.BuscarPorId(
                    usuarioAutenticado,
                    idSolicitacaoDetalhe.Value);
                SolicitacaoService.ResumoSolicitacao resumo = solicitacaoService.ObterResumo(
                    usuarioAutenticado,
                    solicitacao.IdSolicitacao);

                Text = lblTitulo.Text = "Detalhes da solicitação";
                txtSolicitante.Text = resumo.Solicitante;
                txtSolicitante.ReadOnly = true;
                txtProdutoSolicitacao.Text = resumo.Produtos;
                txtProdutoSolicitacao.Visible = true;
                if (txtProdutoSolicitacao.Parent != panelFormulario) txtProdutoSolicitacao.Parent.Visible = true;
                comboProdutos.Visible = false;
                txtProdutoSolicitacao.ReadOnly = true;
                txtQuantidadeSolicitacao.Text = string.Empty;
                txtQuantidadeSolicitacao.ReadOnly = true;
                txtObservacaoSolicitacao.Text = solicitacao.Descricao;
                txtObservacaoSolicitacao.ReadOnly = true;
                cmbSetorSolicitacao.Items.Clear();
                cmbSetorSolicitacao.Items.Add(resumo.Setor);
                cmbSetorSolicitacao.SelectedIndex = 0;
                cmbSetorSolicitacao.Enabled = false;
                cmbTipoSolicitacao.Items.Clear();
                cmbTipoSolicitacao.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(solicitacao.Tipo));
                cmbTipoSolicitacao.SelectedIndex = 0;
                cmbTipoSolicitacao.Enabled = false;
                cmbStatusSolicitacao.Items.Clear();
                cmbStatusSolicitacao.Items.Add(CASE.Utils.TextoInterface.FormatarValorBanco(solicitacao.Situacao));
                cmbStatusSolicitacao.SelectedIndex = 0;
                cmbStatusSolicitacao.Visible = true;
                lblSituacao.Visible = true;
                btnSalvarVisual.Visible = false;
                labelAvisoFormulario.Text = "Consulta da solicitação. A situação e os itens refletem o pedido registrado.";
            }
            catch (UnauthorizedAccessException)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Sua conta não pode consultar esta solicitação.";
            }
            catch (Exception)
            {
                btnSalvarVisual.Enabled = false;
                labelAvisoFormulario.Text = "Não foi possível carregar os detalhes da solicitação.";
            }
        }

        private void btnSalvarVisual_Click(object sender, EventArgs e)
        {

            EscalaOpcao escalaSelecionada = cmbSetorSolicitacao.SelectedItem as EscalaOpcao;
            SolicitacaoService.ProdutoSolicitacaoOpcao produtoSelecionado =
                comboProdutos.SelectedItem as SolicitacaoService.ProdutoSolicitacaoOpcao;
            int quantidade;

            if (escalaSelecionada == null || produtoSelecionado == null)
            {
                labelAvisoFormulario.Text = "Selecione uma escala e um produto.";
                return;
            }

            if (!int.TryParse(txtQuantidadeSolicitacao.Text.Trim(), out quantidade) || quantidade <= 0)
            {
                labelAvisoFormulario.Text = "Informe uma quantidade inteira maior que zero.";
                txtQuantidadeSolicitacao.Focus();
                return;
            }

            Solicitacao solicitacao = new Solicitacao();
            solicitacao.IdEscala = escalaSelecionada.Escala.IdEscala;
            solicitacao.Tipo = "reposicao";
            solicitacao.Descricao = txtObservacaoSolicitacao.Text.Trim();

            List<SolicitacaoItem> itens = new List<SolicitacaoItem>();
            SolicitacaoItem item = new SolicitacaoItem();
            item.IdProdutoSetor = produtoSelecionado.IdProdutoSetor;
            item.Quantidade = quantidade;
            itens.Add(item);

            try
            {
                solicitacaoService.Criar(usuarioAutenticado, solicitacao, itens);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira os dados da solicitação e tente novamente.", labelAvisoFormulario);
            }
            catch (UnauthorizedAccessException)
            {
                labelAvisoFormulario.Text = "Sua conta não pode solicitar produtos nesta escala.";
            }
            catch (Exception)
            {
                labelAvisoFormulario.Text = "Não foi possível registrar a solicitação.";
            }
        }
    }
}
