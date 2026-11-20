using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Repositories;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class EquipeSetoresControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento eventoSelecionado;
        private readonly SetorService setorService = new SetorService();
        private readonly EquipeEventoService equipeService = new EquipeEventoService();
        private readonly EscalaService escalaService = new EscalaService();
        private readonly JornadaSetorService jornadaService = new JornadaSetorService();
        private readonly TurnoRepository turnoRepository = new TurnoRepository();
        private bool podeAdministrarEvento;
        private Setor setorEmEdicao;
        private TextBox pesquisa;
        private readonly Button btnProdutosPrecosSetor = new Button();
        private readonly Label lblAjudaProdutosPrecos = new Label();

        public EquipeSetoresControl() : this(null, null)
        {
        }

        public EquipeSetoresControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
            VisualUtils.ExibirEstadoVazio(dgvSetores, "Este evento ainda não tem setores. Cadastre o primeiro setor nesta tela.");
            VisualUtils.ExibirEstadoVazio(dgvEquipe, "Este evento ainda não tem membros vinculados.");
            ConfigurarAcaoProdutosPrecos();
            pesquisa = VisualUtils.AdicionarPesquisa(this, "txtPesquisarEquipe", "Pesquisar equipe ou setor",
                texto => VisualUtils.FiltrarGrades(panelCorpo, texto));
            // O painel superior não pode crescer sobre a seção de setores ao redimensionar.
            panelEquipe.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelSetores.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            SizeChanged += AjustarSecoes;
            AjustarSecoes(this, EventArgs.Empty);
            lblSetoresbarracas.Width = 250;
            txtNovoSetor.MaxLength = 100;
            txtNovoSetor.Enabled = false;
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            lblSubtitulo.Text = eventoSelecionado == null
                ? "Selecione um evento na tela Eventos para consultar a equipe e os setores."
                : "Evento: " + eventoSelecionado.Nome + " · Defina a equipe por setor, turno e perfil de acesso.";

            if (usuarioAutenticado == null)
            {
                labelAviso.Text = "Entre no sistema para consultar a equipe do evento.";
                btnAdicionarSetor.Enabled = false;
                btnNovoFuncionario.Enabled = false;
                btnEditarFuncionario.Enabled = false;
                return;
            }

            if (eventoSelecionado == null)
            {
                labelAviso.Text = "Selecione um evento na tela Eventos para consultar seus setores.";
                btnAdicionarSetor.Enabled = false;
                btnNovoFuncionario.Enabled = false;
                btnEditarFuncionario.Enabled = false;
                return;
            }

            try
            {
                string nomePerfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(
                    usuarioAutenticado,
                    out nomePerfil);
                podeAdministrarEvento = string.Equals(nomePerfil, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(
                    nomePerfil,
                    "Gerente",
                    StringComparison.OrdinalIgnoreCase) &&
                    eventoSelecionado.IdResponsavel == usuarioAutenticado.IdPessoa);
            }
            catch (Exception)
            {
                podeAdministrarEvento = false;
            }

            btnAdicionarSetor.Enabled = podeAdministrarEvento;
            btnNovoFuncionario.Enabled = podeAdministrarEvento;
            btnEditarFuncionario.Enabled = podeAdministrarEvento;
            txtNovoSetor.Enabled = podeAdministrarEvento;
            AtualizarAcaoProdutosPrecos();

            CarregarSetores();
            CarregarEquipe();
            ContextMenuStrip menuSetor = new ContextMenuStrip();
            ToolStripMenuItem renomear = new ToolStripMenuItem("Renomear setor selecionado");
            renomear.Enabled = podeAdministrarEvento;
            renomear.Click += (s, e) =>
            {
                if (dgvSetores.SelectedRows.Count == 0)
                {
                    labelAviso.Text = "Selecione o setor que deseja renomear.";
                    return;
                }
                setorEmEdicao = dgvSetores.SelectedRows[0].Tag as Setor;
                if (setorEmEdicao != null)
                {
                    txtNovoSetor.Text = setorEmEdicao.Nome;
                    btnAdicionarSetor.Text = "Salvar setor";
                    labelAviso.Text = "Edite o nome e salve. Pressione Esc no campo para cancelar.";
                    txtNovoSetor.Focus();
                    txtNovoSetor.SelectAll();
                }
            };
            menuSetor.Items.Add(renomear);
            Disposed += (s, e) => menuSetor.Dispose();
            txtNovoSetor.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    setorEmEdicao = null;
                    txtNovoSetor.Clear();
                    btnAdicionarSetor.Text = "Adicionar setor";
                    e.SuppressKeyPress = true;
                }
            };
            dgvSetores.ContextMenuStrip = menuSetor;
            dgvSetores.SelectionChanged += (s, e) => AtualizarAcaoProdutosPrecos();
            Disposed += (s, e) => menuSetor.Dispose();

            if (!podeAdministrarEvento)
            {
                labelAviso.Text = "Consulta administrativa. Equipes, setores e preços são gerenciados pelo gerente responsável pelo evento.";
            }
        }

        private void ConfigurarAcaoProdutosPrecos()
        {
            btnProdutosPrecosSetor.Name = "btnProdutosPrecosSetor";
            btnProdutosPrecosSetor.Text = "Produtos e preços";
            btnProdutosPrecosSetor.SetBounds(24, 150, 235, 38);
            btnProdutosPrecosSetor.FlatStyle = FlatStyle.Flat;
            btnProdutosPrecosSetor.BackColor = System.Drawing.Color.White;
            btnProdutosPrecosSetor.ForeColor = VisualUtils.Texto;
            btnProdutosPrecosSetor.Enabled = false;
            btnProdutosPrecosSetor.CausesValidation = false;
            btnProdutosPrecosSetor.AccessibleDescription =
                "Configure quais produtos são vendidos no setor e o preço padrão de cada um.";
            VisualUtils.EstilizarBotao(btnProdutosPrecosSetor);
            VisualUtils.Arredondar(btnProdutosPrecosSetor, 8);
            btnProdutosPrecosSetor.Click += (s, e) => ConfigurarProdutoDoSetor(s, null);
            panelSetores.Controls.Add(btnProdutosPrecosSetor);

            lblAjudaProdutosPrecos.Name = "lblAjudaProdutosPrecosSetor";
            lblAjudaProdutosPrecos.Text = "Selecione um setor para configurar os produtos vendidos e seus preços.";
            lblAjudaProdutosPrecos.SetBounds(24, 192, 235, 34);
            lblAjudaProdutosPrecos.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lblAjudaProdutosPrecos.ForeColor = VisualUtils.TextoSuave;
            lblAjudaProdutosPrecos.AutoEllipsis = true;
            panelSetores.Controls.Add(lblAjudaProdutosPrecos);
        }

        private void AtualizarAcaoProdutosPrecos()
        {
            if (btnProdutosPrecosSetor == null || dgvSetores == null)
                return;

            bool setorSelecionado = dgvSetores.SelectedRows.Count > 0;
            btnProdutosPrecosSetor.Enabled = podeAdministrarEvento && setorSelecionado;
            lblAjudaProdutosPrecos.Text = !podeAdministrarEvento
                ? "Os preços são configurados pelo administrador ou gerente responsável."
                : setorSelecionado
                    ? "Defina os produtos disponíveis e o preço padrão de venda deste setor."
                    : "Selecione um setor para configurar os produtos vendidos e seus preços.";
        }

        private void btnNovoFuncionario_Click(object sender, EventArgs e)
        {

            AbrirFormularioMembro(null);
        }

        private void btnEditarFuncionario_Click(object sender, EventArgs e)
        {

            EquipeEvento vinculo = ObterVinculoSelecionado();

            if (vinculo != null)
            {
                AbrirFormularioMembro(vinculo);
            }
        }

        private void dgvEquipe_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0)
            {
                return;
            }

            EquipeEvento vinculo = dgvEquipe.Rows[e.RowIndex].Tag as EquipeEvento;

            if (vinculo != null)
            {
                AbrirFormularioMembro(vinculo);
            }
        }

        private EquipeEvento ObterVinculoSelecionado()
        {
            if (dgvEquipe.SelectedRows.Count == 0)
            {
                labelAviso.Text = "Selecione um membro da equipe.";
                return null;
            }

            return dgvEquipe.SelectedRows[0].Tag as EquipeEvento;
        }

        private void AbrirFormularioMembro(EquipeEvento vinculo)
        {
            using (PessoaSetorForm formulario = new PessoaSetorForm(
                usuarioAutenticado,
                eventoSelecionado,
                vinculo))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarEquipe();
                    labelAviso.Text = "Equipe atualizada.";
                    CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Sucesso);
                }
            }
        }

        private void btnAdicionarSetor_Click(object sender, EventArgs e)
        {

            if (eventoSelecionado == null || usuarioAutenticado == null)
            {
                labelAviso.Text = "Selecione um evento e autentique-se antes de criar um setor.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNovoSetor.Text))
            {
                labelAviso.Text = "Informe o nome do setor.";
                txtNovoSetor.Focus();
                return;
            }

            Setor setor = new Setor();
            setor.IdEvento = eventoSelecionado.IdEvento;
            setor.Nome = txtNovoSetor.Text.Trim();

            try
            {
                if (setorEmEdicao == null)
                {
                    setorService.Criar(usuarioAutenticado, setor);
                    labelAviso.Text = "Setor cadastrado e local de estoque criado.";
                    CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Sucesso);
                }
                else
                {
                    setorService.Renomear(usuarioAutenticado, setorEmEdicao.IdSetor,
                        setor.Nome, setorEmEdicao.Nome);
                    labelAviso.Text = "Nome do setor e do local atualizado. Saldos e vínculos preservados.";
                    CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Sucesso);
                }
                setorEmEdicao = null;
                btnAdicionarSetor.Text = "Adicionar setor";
                txtNovoSetor.Clear();
                CarregarSetores();
                CarregarEquipe();
            }
            catch (ArgumentException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível atualizar a equipe e os setores.", labelAviso);
            }
            catch (InvalidOperationException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Não foi possível atualizar a equipe e os setores.", labelAviso);
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não pode criar setores neste evento.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível cadastrar o setor.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private void CarregarSetores()
        {
            try
            {
                List<Setor> setores = setorService.ListarPorEvento(
                    usuarioAutenticado,
                    eventoSelecionado.IdEvento);
                ConsultaOperacionalService consultas = new ConsultaOperacionalService();
                System.Data.DataTable escalas = consultas.Escalas(usuarioAutenticado, eventoSelecionado.IdEvento);
                System.Data.DataTable registros = consultas.Registros(usuarioAutenticado, eventoSelecionado.IdEvento,
                    eventoSelecionado.DataInicio, eventoSelecionado.DataFim);

                List<object[]> linhas = new List<object[]>();
                foreach (Setor setor in setores)
                {
                    HashSet<int> pessoas = new HashSet<int>();
                    HashSet<int> operacoes = new HashSet<int>();
                    foreach (System.Data.DataRow escala in escalas.Rows)
                    {
                        if (Convert.ToInt32(escala["id_setor"]) == setor.IdSetor &&
                            Convert.ToBoolean(escala["ativo"]) &&
                            Convert.ToString(escala["situacao"]) != SituacoesEscala.Cancelada)
                        {
                            pessoas.Add(Convert.ToInt32(escala["id_pessoa"]));
                        }
                    }
                    foreach (System.Data.DataRow registro in registros.Rows)
                    {
                        if (Convert.ToInt32(registro["id_setor"]) == setor.IdSetor)
                        {
                            operacoes.Add(Convert.ToInt32(registro["id_registro"]));
                        }
                    }
                    linhas.Add(new object[] { setor, setor.Nome, pessoas.Count, operacoes.Count });
                }

                dgvSetores.Rows.Clear();
                foreach (object[] linha in linhas)
                {
                    Setor setor = (Setor)linha[0];
                    int indiceLinha = dgvSetores.Rows.Add(linha[1], linha[2], linha[3]);
                    dgvSetores.Rows[indiceLinha].Tag = setor;
                }

                dgvSetores.ClearSelection();
                VisualUtils.FiltrarGrades(panelCorpo, pesquisa.Text.Trim());
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não pode consultar os setores deste evento.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar os setores do evento.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private void ConfigurarProdutoDoSetor(object sender, DataGridViewCellEventArgs e)
        {
            if (!podeAdministrarEvento || dgvSetores.SelectedRows.Count == 0 ||
                (e != null && e.RowIndex < 0))
            {
                return;
            }
            Setor setor = dgvSetores.SelectedRows[0].Tag as Setor;
            if (setor == null)
            {
                return;
            }
            try
            {
                ProdutoService produtos = new ProdutoService();
                var configuracoes = produtos.ListarConfiguracoesDoSetor(usuarioAutenticado, setor.IdSetor);
                using (Form dialogo = new Form())
                using (ComboBox produto = new ComboBox())
                using (NumericUpDown preco = new NumericUpDown())
                using (Button salvar = new Button())
                {
                    IconoAplicacao.Aplicar(dialogo);
                    dialogo.Text = "Produtos e preços — " + setor.Nome;
                    dialogo.Name = "ProdutoSetorForm";
                    produto.Name = "cmbProdutoSetor";
                    preco.Name = "numPrecoSetor";
                    salvar.Name = "btnSalvarPrecoSetor";
                    dialogo.ClientSize = new System.Drawing.Size(480, 190);
                    dialogo.StartPosition = FormStartPosition.CenterParent;
                    dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialogo.MaximizeBox = false;
                    produto.DisplayMember = "Nome";
                    produto.DropDownStyle = ComboBoxStyle.DropDownList;
                    produto.SetBounds(20, 42, 440, 30);
                    foreach (Produto item in produtos.Listar(usuarioAutenticado))
                    {
                        if (item.Ativo)
                        {
                            produto.Items.Add(item);
                        }
                    }
                    preco.DecimalPlaces = 2;
                    preco.Maximum = 99999999.99M;
                    preco.SetBounds(20, 113, 180, 28);
                    produto.SelectedIndexChanged += (origem, argumentos) =>
                    {
                        preco.Value = 0;
                        Produto selecionado = produto.SelectedItem as Produto;
                        foreach (ProdutoSetor configuracao in configuracoes)
                        {
                            if (selecionado != null && configuracao.IdProduto == selecionado.IdProduto)
                            {
                                preco.Value = configuracao.PrecoVenda;
                                break;
                            }
                        }
                    };
                    salvar.Text = "Salvar preço";
                    salvar.SetBounds(280, 110, 180, 36);
                    salvar.Click += (origem, argumentos) =>
                    {
                        Produto selecionado = produto.SelectedItem as Produto;
                        if (selecionado == null)
                        {
                            return;
                        }
                        try
                        {
                            produtos.ConfigurarNoSetor(usuarioAutenticado, setor.IdSetor,
                                selecionado.IdProduto, preco.Value);
                            dialogo.DialogResult = DialogResult.OK;
                            dialogo.Close();
                        }
                        catch (Exception erro)
                        {
                            string mensagem = "Não foi possível configurar o produto.";
                            if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
                            {
                                mensagem = CASE.Utils.FeedbackUsuario.MensagemErro(erro, mensagem);
                            }
                            CASE.Utils.FeedbackUsuario.ExibirErro(dialogo, erro, mensagem);
                        }
                    };
                    dialogo.Controls.Add(FrontendTheme.Legenda("Produto", new System.Drawing.Rectangle(20, 15, 400, 22)));
                    dialogo.Controls.Add(FrontendTheme.Legenda("Preço padrão de venda (R$)", new System.Drawing.Rectangle(20, 84, 400, 22)));
                    dialogo.Controls.Add(produto);
                    dialogo.Controls.Add(preco);
                    dialogo.Controls.Add(salvar);
                    FrontendTheme.Aplicar(dialogo);
                    if (produto.Items.Count > 0)
                    {
                        produto.SelectedIndex = 0;
                    }
                    if (dialogo.ShowDialog(this) == DialogResult.OK)
                    {
                        labelAviso.Text = "Produto vinculado ao setor e preço padrão salvo. Vendas anteriores foram preservadas.";
                        CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Sucesso);
                    }
                }
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível carregar os produtos autorizados.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ConfigurarProdutoDoSetor");
            }
        }

        private void AjustarSecoes(object sender, EventArgs e)
        {
            panelCorpo.Height = Math.Max(590, ClientSize.Height - 170);
            panelSetores.Height = Math.Max(230, panelCorpo.Height - panelSetores.Top);
            labelAviso.Top = panelCorpo.Bottom + 12;
            AutoScrollMinSize = new System.Drawing.Size(970, labelAviso.Bottom + 16);
        }

        private void CarregarEquipe()
        {
            try
            {
                List<EquipeEvento> equipe = equipeService.ListarPorEvento(
                    usuarioAutenticado,
                    eventoSelecionado.IdEvento);
                List<Perfil> perfis = equipeService.ListarPerfis(
                    usuarioAutenticado,
                    eventoSelecionado.IdEvento);

                List<object[]> linhas = new List<object[]>();
                foreach (EquipeEvento membro in equipe)
                {
                    Pessoa pessoa = equipeService.BuscarPessoaVinculada(
                        usuarioAutenticado,
                        eventoSelecionado.IdEvento,
                        membro.IdEquipe);
                    string nomePessoa = "Pessoa " + membro.IdPessoa;
                    string login = string.Empty;

                    if (pessoa != null)
                    {
                        nomePessoa = pessoa.Nome;
                        login = equipeService.BuscarLoginDaPessoaVinculada(
                            usuarioAutenticado,
                            eventoSelecionado.IdEvento,
                            membro.IdEquipe);
                    }

                    string nomePerfil = "—";

                    foreach (Perfil perfil in perfis)
                    {
                        if (perfil.IdPerfil == membro.IdPerfil)
                        {
                            nomePerfil = perfil.Nome;
                            break;
                        }
                    }

                    string resumoSetores;
                    string resumoTurnos;
                    MontarResumoEscala(membro, out resumoSetores, out resumoTurnos);
                    string situacao = "Inativo";

                    if (membro.Ativo)
                    {
                        situacao = "Ativo";
                    }

                    linhas.Add(new object[] { membro, nomePessoa, login, nomePerfil, resumoSetores, resumoTurnos, situacao });
                }

                dgvEquipe.Rows.Clear();
                foreach (object[] linha in linhas)
                {
                    int indiceLinha = dgvEquipe.Rows.Add(linha[1], linha[2], linha[3], linha[4], linha[5], linha[6]);
                    dgvEquipe.Rows[indiceLinha].Tag = linha[0];
                }

                dgvEquipe.ClearSelection();
                VisualUtils.FiltrarGrades(panelCorpo, pesquisa.Text.Trim());
            }
            catch (UnauthorizedAccessException)
            {
                labelAviso.Text = "Sua conta não pode consultar a equipe deste evento.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
            catch (Exception)
            {
                labelAviso.Text = "Não foi possível carregar a equipe do evento.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Erro);
            }
        }

        private void MontarResumoEscala(
            EquipeEvento membro,
            out string resumoSetores,
            out string resumoTurnos)
        {
            List<Escala> escalas = escalaService.ListarPorEquipe(
                usuarioAutenticado,
                membro.IdEquipe);
            List<string> nomesSetores = new List<string>();
            List<string> nomesTurnos = new List<string>();

            if (escalas.Count == 0)
            {
                resumoSetores = "Sem escala";
                resumoTurnos = "Sem escala";
                return;
            }

            foreach (Escala escala in escalas)
            {
                JornadaSetor jornada = jornadaService.BuscarPorId(
                    usuarioAutenticado,
                    escala.IdJornada);

                if (jornada == null)
                {
                    continue;
                }

                string nomeSetor = "Setor " + jornada.IdSetor;
                List<Setor> setores = setorService.ListarPorEvento(
                    usuarioAutenticado,
                    eventoSelecionado.IdEvento);

                foreach (Setor setor in setores)
                {
                    if (setor.IdSetor == jornada.IdSetor)
                    {
                        nomeSetor = setor.Nome;
                        break;
                    }
                }

                string nomeTurno = "Turno " + jornada.IdTurno;
                List<Turno> turnos = turnoRepository.Listar();

                foreach (Turno turno in turnos)
                {
                    if (turno.IdTurno == jornada.IdTurno)
                    {
                        nomeTurno = turno.Nome;
                        break;
                    }
                }

                AdicionarSeAusente(nomesSetores, nomeSetor);
                AdicionarSeAusente(nomesTurnos, nomeTurno);
            }

            resumoSetores = string.Join(", ", nomesSetores);
            resumoTurnos = string.Join(", ", nomesTurnos);

            if (string.IsNullOrWhiteSpace(resumoSetores))
            {
                resumoSetores = "Escala cadastrada";
            }

            if (string.IsNullOrWhiteSpace(resumoTurnos))
            {
                resumoTurnos = "Escala cadastrada";
            }
        }

        private static void AdicionarSeAusente(List<string> valores, string valor)
        {
            if (!valores.Contains(valor))
            {
                valores.Add(valor);
            }
        }
    }
}
