using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class RegistrosControl : UserControl
    {
        private Usuario usuarioAutenticado;
        private Evento eventoSelecionado;
        private ConsultaOperacionalService consultas = new ConsultaOperacionalService();
        private TextBox pesquisa;
        private NumericUpDown precoPraticado = new NumericUpDown();
        private ComboBox formaPagamento = new ComboBox();
        private Label legendaPreco;
        private bool ajustandoLayout;
        private bool carregando = true;
        private bool registrando;
        private bool eventoEmAndamentoAtual;
        private string chaveOperacao = Guid.NewGuid().ToString();
        private Label resumoRegistros = new Label();
        private ComboBox filtroEvento = new ComboBox();
        private Label legendaEvento = new Label();
        private DataTable escalasDisponiveis;
        private sealed class OpcaoFiltro
        {
            public int Id;
            public string Nome;
            public override string ToString()
            {
                return Nome;
            }
        }

        public RegistrosControl() : this(null, null)
        {
        }

        public RegistrosControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            FrontendTheme.Aplicar(this);
            lblTitulo.Text = "Vendas e perdas";
            lblSubtitulo.Text = "Selecione uma escala. Vendas e perdas ficam disponíveis quando o evento estiver em andamento.";
            Button abrirCorrecao = new Button();
            abrirCorrecao.Name = "btnCorrigirRegistro";
            abrirCorrecao.Text = "Revisar lançamento";
            abrirCorrecao.SetBounds(Width - 250, 25, 220, 36);
            abrirCorrecao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            abrirCorrecao.Enabled = false;
            abrirCorrecao.Click += CorrigirRegistro;
            VisualUtils.EstilizarBotao(abrirCorrecao);
            Controls.Add(abrirCorrecao);
            lblTitulo.Width = Math.Max(280, Width - 380);
            lblTitulo.AutoEllipsis = true;
            abrirCorrecao.BringToFront();
            dgvRegistrosEquipe.SelectionChanged += Tratar_dgvRegistrosEquipe_SelectionChanged;
            lblContextoRegistro.Text = "Selecione uma escala de trabalho";
            panelNovoRegistro.Visible = false;
            lblObservacao.Width = 120;
            Panel painelObservacao = txtObservacaoRegistro.Parent as Panel;
            if (painelObservacao != null && painelObservacao.Parent == panelNovoRegistro)
            {
                painelObservacao.Left = 150;
                painelObservacao.BringToFront();
            }

            VisualUtils.DefinirPlaceholder(txtObservacaoRegistro, "Detalhes ou justificativa da perda");
            lblSetor.Text = "3. Escala de trabalho";
            lblFuncionario.Text = "1. Pessoa escalada";
            lblFuncionario.Visible = true;
            cmbPessoaRegistro.Visible = true;
            cmbPessoaRegistro.Items.Clear();
            cmbPessoaRegistro.SelectedIndexChanged -= Filtros_SelectedIndexChanged;
            cmbPessoaRegistro.SelectedIndexChanged += PessoaFiltroAlterada;
            filtroEvento.Name = "cmbEventoRegistros";
            filtroEvento.DropDownStyle = ComboBoxStyle.DropDownList;
            filtroEvento.Font = cmbPessoaRegistro.Font;
            legendaEvento.Text = "2. Evento";
            legendaEvento.ForeColor = VisualUtils.TextoSuave;
            panelFiltrosRegistros.Controls.Add(legendaEvento);
            panelFiltrosRegistros.Controls.Add(filtroEvento);
            VisualUtils.EstilizarSelecao(filtroEvento);
            filtroEvento.SelectedIndexChanged += filtroEvento_SelectedIndexChanged;
            cmbSetorRegistro.Width = 900;
            cmbSetorRegistro.Items.Clear();
            cmbProdutoRegistro.Items.Clear();
            cmbTipoRegistro.Items.Clear();
            cmbTipoRegistro.Items.AddRange(new object[] { "venda", "perda" });
            cmbTipoRegistro.SelectedIndex = 0;
            precoPraticado.Name = "precoPraticado";
            precoPraticado.DecimalPlaces = 2;
            precoPraticado.Maximum = 99999999.99M;
            precoPraticado.SetBounds(735, 73, 120, 28);
            panelNovoRegistro.Controls.Add(precoPraticado);
            legendaPreco = FrontendTheme.Legenda("Preço unitário (R$)", new Rectangle(735, 47, 200, 22));
            panelNovoRegistro.Controls.Add(legendaPreco);
            formaPagamento.Name = "formaPagamento";
            formaPagamento.DropDownStyle = ComboBoxStyle.DropDownList;
            formaPagamento.Items.AddRange(new object[] { "dinheiro", "pix", "cartao" });
            formaPagamento.SelectedIndex = 0;
            formaPagamento.SetBounds(150, 160, 215, 28);
            VisualUtils.EstilizarSelecao(formaPagamento);
            panelNovoRegistro.Controls.Add(formaPagamento);
            panelNovoRegistro.Controls.Add(FrontendTheme.Legenda("Pagamento", new Rectangle(18, 163, 130, 22)));
            panelNovoRegistro.Height = 215;
            panelListaRegistros.Top = 335;
            panelListaRegistros.Height = 260;
            panelListaRegistros.Padding = new Padding(12, 70, 12, 12);
            resumoRegistros.Name = "lblResumoRegistros";
            resumoRegistros.SetBounds(18, 20, 600, 30);
            resumoRegistros.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            resumoRegistros.ForeColor = VisualUtils.Marinho;
            panelListaRegistros.Controls.Add(resumoRegistros);
            pesquisa = VisualUtils.AdicionarPesquisa(panelListaRegistros, "txtPesquisarRegistro", "Pesquisar nos registros", Preparar_AdicionarPesquisa);
            dgvRegistrosEquipe.Dock = DockStyle.Fill;
            panelCorpo.Height = panelListaRegistros.Bottom;
            labelAviso.Top = panelCorpo.Bottom + 12;
            labelAviso.Height = 42;
            AutoScrollMinSize = new Size(760, labelAviso.Bottom + 12);
            SizeChanged += Tela_SizeChanged;
            panelNovoRegistro.VisibleChanged += panelNovoRegistro_VisibleChanged;
            AjustarCamposAoEspaco();
            cmbProdutoRegistro.DisplayMember = "nome";
            cmbSetorRegistro.DisplayMember = "descricao_curta";
            cmbSetorRegistro.FormattingEnabled = true;
            cmbSetorRegistro.Format += cmbSetorRegistro_Format;
            cmbProdutoRegistro.SelectedIndexChanged += ProdutoAlterado;
            cmbTipoRegistro.SelectedIndexChanged += TipoAlterado;
            TipoAlterado(this, EventArgs.Empty);
            dgvRegistrosEquipe.Columns.Add("precoPraticado", "Preço praticado");
            dgvRegistrosEquipe.Columns.Add("pagamento", "Pagamento");
            dgvRegistrosEquipe.Columns.Add("numeroRegistro", "Registro");
            dgvRegistrosEquipe.Columns["numeroRegistro"].DisplayIndex = 0;
            dgvRegistrosEquipe.Columns["numeroRegistro"].MinimumWidth = 75;
            dgvRegistrosEquipe.Columns.Add("totalRegistroItem", "Total do item");
            dgvRegistrosEquipe.Columns["totalRegistroItem"].MinimumWidth = 110;
            dgvRegistrosEquipe.Columns["totalRegistroItem"].DefaultCellStyle.Format = "C2";
            dgvRegistrosEquipe.Columns[0].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            dgvRegistrosEquipe.Columns[8].DefaultCellStyle.Format = "C2";
            dgvRegistrosEquipe.Columns[8].DefaultCellStyle.NullValue = "—";
            dgvRegistrosEquipe.Columns["totalRegistroItem"].DefaultCellStyle.NullValue = "—";
            int[] largurasMinimas =
            {
                145,
                140,
                140,
                120,
                85,
                180,
                65,
                180,
                110,
                90
            };
            for (int coluna = 0; coluna < largurasMinimas.Length; coluna++)
            {
                dgvRegistrosEquipe.Columns[coluna].MinimumWidth = largurasMinimas[coluna];
            }

            dgvRegistrosEquipe.Columns[6].DefaultCellStyle.Format = "0.###";
            // A escala selecionada já identifica o funcionário e o setor.
            dgvRegistrosEquipe.Columns[1].Visible = false;
            dgvRegistrosEquipe.Columns[2].Visible = false;
            labelAviso.Text = "Autentique-se para registrar operações.";
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem corrigir = new ToolStripMenuItem("Revisar lançamento e histórico de correções");
            corrigir.Name = "menuCorrigirRegistro";
            corrigir.Click += CorrigirRegistro;
            menu.Items.Add(corrigir);
            dgvRegistrosEquipe.ContextMenuStrip = menu;
            menu.Opening += menu_Opening;
            dgvRegistrosEquipe.CellMouseDown += dgvRegistrosEquipe_CellMouseDown;
            Disposed += Tratar_Disposed;
            btnRegistrar.Enabled = false;
            if (usuarioAutenticado == null)
            {
                carregando = false;
                return;
            }

            try
            {
                escalasDisponiveis = consultas.Escalas(usuarioAutenticado, IdEvento());
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                bool membro = perfil == "Membro";
                if (!membro)
                    cmbPessoaRegistro.Items.Add(new OpcaoFiltro { Id = 0, Nome = "Todas as pessoas · consulta" });
                var pessoas = new HashSet<int>();
                foreach (DataRow linha in escalasDisponiveis.Rows)
                {
                    int id = Convert.ToInt32(linha["id_pessoa"]);
                    if (pessoas.Add(id))
                        cmbPessoaRegistro.Items.Add(new OpcaoFiltro { Id = id, Nome = Convert.ToString(linha["pessoa_nome"]) });
                }

                if (membro && cmbPessoaRegistro.Items.Count == 0)
                    cmbPessoaRegistro.Items.Add(new OpcaoFiltro { Id = usuario.IdPessoa, Nome = "Minha conta · sem escala" });
                cmbPessoaRegistro.Enabled = !membro;
                cmbPessoaRegistro.SelectedIndex = 0;
                carregando = false;
                PessoaFiltroAlterada(this, EventArgs.Empty);
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                carregando = false;
            }

            void Tratar_dgvRegistrosEquipe_SelectionChanged(object s, EventArgs e)
            {
                abrirCorrecao.Enabled = usuarioAutenticado != null && dgvRegistrosEquipe.SelectedRows.Count > 0 && dgvRegistrosEquipe.SelectedRows[0].Visible;
            }

            void Tratar_Disposed(object s, EventArgs e)
            {
                menu.Dispose();
            }
        }

        private bool EscalaDaPessoa(DataRow linha)
        {
            var pessoa = cmbPessoaRegistro.SelectedItem as OpcaoFiltro;
            return pessoa != null && (pessoa.Id == 0 || pessoa.Id == Convert.ToInt32(linha["id_pessoa"]));
        }

        private void PessoaFiltroAlterada(object sender, EventArgs e)
        {
            if (carregando || escalasDisponiveis == null)
                return;
            carregando = true;
            try
            {
                filtroEvento.Items.Clear();
                if (eventoSelecionado == null)
                    filtroEvento.Items.Add(new OpcaoFiltro { Id = 0, Nome = "Todos os eventos · consulta" });
                var eventos = new HashSet<int>();
                foreach (DataRow linha in escalasDisponiveis.Rows)
                {
                    int id = Convert.ToInt32(linha["id_evento"]);
                    if (EscalaDaPessoa(linha) && eventos.Add(id))
                        filtroEvento.Items.Add(new OpcaoFiltro { Id = id, Nome = Convert.ToString(linha["evento_nome"]) });
                }

                if (eventoSelecionado != null && filtroEvento.Items.Count == 0)
                    filtroEvento.Items.Add(new OpcaoFiltro { Id = eventoSelecionado.IdEvento, Nome = eventoSelecionado.Nome });
                if (filtroEvento.Items.Count > 0)
                    filtroEvento.SelectedIndex = filtroEvento.Items.Count == 2 && eventoSelecionado == null ? 1 : 0;
                filtroEvento.Enabled = eventoSelecionado == null && filtroEvento.Items.Count > 1;
            }
            finally
            {
                carregando = false;
            }

            PreencherEscalasFiltradas();
        }

        private void PreencherEscalasFiltradas()
        {
            carregando = true;
            try
            {
                cmbSetorRegistro.Items.Clear();
                cmbSetorRegistro.Items.Add("Todas as escalas · somente consulta");
                var evento = filtroEvento.SelectedItem as OpcaoFiltro;
                foreach (DataRowView linha in escalasDisponiveis.DefaultView)
                    if (EscalaDaPessoa(linha.Row) && evento != null && (evento.Id == 0 || evento.Id == Convert.ToInt32(linha["id_evento"])))
                        cmbSetorRegistro.Items.Add(linha);
                cmbSetorRegistro.SelectedIndex = cmbSetorRegistro.Items.Count == 2 ? 1 : 0;
                cmbSetorRegistro.Enabled = cmbSetorRegistro.Items.Count > 1;
            }
            finally
            {
                carregando = false;
            }

            Filtros_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private int IdEvento()
        {
            if (eventoSelecionado == null)
            {
                return 0;
            }

            return eventoSelecionado.IdEvento;
        }

        private void Filtros_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (carregando || usuarioAutenticado == null)
            {
                return;
            }

            try
            {
                DataRowView escala = cmbSetorRegistro.SelectedItem as DataRowView;
                cmbProdutoRegistro.Items.Clear();
                if (escala == null)
                {
                    panelNovoRegistro.Visible = false;
                    eventoEmAndamentoAtual = false;
                    btnRegistrar.Enabled = false;
                    labelAviso.Text = cmbSetorRegistro.Items.Count <= 1 ? "Nenhuma escala disponível para esta pessoa e evento. Defina a equipe e a escala de trabalho primeiro." : "Consulta de todas as escalas filtradas. Escolha uma escala específica para registrar uma venda ou perda.";
                    AtualizarRegistros();
                    AjustarCamposAoEspaco();
                    return;
                }

                DataTable produtos = consultas.ProdutosDaEscala(usuarioAutenticado, Convert.ToInt32(escala["id_escala"]));
                foreach (DataRowView produto in produtos.DefaultView)
                {
                    cmbProdutoRegistro.Items.Add(produto);
                }

                if (cmbProdutoRegistro.Items.Count > 0)
                {
                    cmbProdutoRegistro.SelectedIndex = 0;
                }

                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                bool podeRegistrar = string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase) || string.Equals(perfil, "Membro", StringComparison.OrdinalIgnoreCase);
                panelNovoRegistro.Visible = podeRegistrar;
                AtualizarEstadoLancamento(escala, podeRegistrar, produtos.Rows.Count);
                lblTitulo.Text = podeRegistrar ? "Registrar vendas e perdas" : "Consulta de vendas e perdas";
                lblSubtitulo.Text = podeRegistrar ? "Confira a pessoa, o evento e a escala antes de lançar. O evento precisa estar em andamento." : "Consulte os lançamentos da escala selecionada. Revisar lançamento permite corrigir erros com justificativa.";
                if (!podeRegistrar)
                {
                    labelAviso.Text = "Os membros registram as vendas e perdas. Aqui você consulta ou corrige lançamentos; o Financeiro confere os recebimentos.";
                }

                AtualizarRegistros();
                AjustarCamposAoEspaco();
            }
            catch (Exception erro)
            {
                btnRegistrar.Enabled = false;
                ExibirErro(erro);
            }
        }

        private void AtualizarEstadoLancamento(DataRowView escala, bool podeRegistrar, int quantidadeProdutos)
        {
            string situacaoEvento = escala != null && escala.DataView.Table.Columns.Contains("situacao_evento") ? Convert.ToString(escala["situacao_evento"]) : string.Empty;
            eventoEmAndamentoAtual = string.Equals(situacaoEvento, SituacoesEvento.EmAndamento, StringComparison.OrdinalIgnoreCase) && escala != null && Convert.ToBoolean(escala["ativo"]) && !string.Equals(Convert.ToString(escala["situacao"]), SituacoesEscala.Cancelada, StringComparison.OrdinalIgnoreCase);
            bool lancamentoPermitido = podeRegistrar && eventoEmAndamentoAtual && quantidadeProdutos > 0;
            btnRegistrar.Enabled = lancamentoPermitido && !registrando;
            cmbTipoRegistro.Enabled = lancamentoPermitido;
            cmbProdutoRegistro.Enabled = lancamentoPermitido;
            numQuantidadeRegistro.Enabled = lancamentoPermitido;
            precoPraticado.Enabled = lancamentoPermitido && cmbTipoRegistro.Text == "venda";
            formaPagamento.Enabled = lancamentoPermitido && cmbTipoRegistro.Text == "venda";
            txtObservacaoRegistro.Enabled = lancamentoPermitido;
            Panel painelObservacao = txtObservacaoRegistro.Parent as Panel;
            if (painelObservacao != null && painelObservacao.Parent == panelNovoRegistro)
                painelObservacao.Enabled = lancamentoPermitido;
            if (!podeRegistrar)
                return;
            lblContextoRegistro.BorderStyle = BorderStyle.FixedSingle;
            lblContextoRegistro.TextAlign = ContentAlignment.MiddleLeft;
            if (eventoEmAndamentoAtual)
            {
                lblContextoRegistro.Text = quantidadeProdutos > 0 ? "Evento em andamento — vendas e perdas liberadas para esta escala." : "Evento em andamento — nenhum produto está vinculado a esta escala.";
                lblContextoRegistro.BackColor = Color.FromArgb(232, 247, 239);
                lblContextoRegistro.ForeColor = Color.FromArgb(24, 116, 75);
                labelAviso.Text = quantidadeProdutos > 0 ? "O lançamento atualiza o estoque do setor. Perdas precisam de justificativa; vendas informam preço e forma de pagamento." : "Vincule produtos ao setor desta escala para habilitar os lançamentos.";
            }
            else if (string.Equals(situacaoEvento, SituacoesEvento.EmAndamento, StringComparison.OrdinalIgnoreCase))
            {
                lblContextoRegistro.Text = "Escala cancelada ou vínculo inativo — novos lançamentos estão bloqueados.";
                lblContextoRegistro.BackColor = Color.FromArgb(255, 246, 222);
                lblContextoRegistro.ForeColor = Color.FromArgb(143, 91, 0);
                labelAviso.Text = "Escolha uma escala ativa. Os registros existentes continuam disponíveis para consulta.";
            }
            else if (string.Equals(situacaoEvento, SituacoesEvento.Planejado, StringComparison.OrdinalIgnoreCase))
            {
                lblContextoRegistro.Text = "Evento ainda não iniciado — inicie em Eventos > Situação do evento para liberar vendas e perdas.";
                lblContextoRegistro.BackColor = Color.FromArgb(255, 246, 222);
                lblContextoRegistro.ForeColor = Color.FromArgb(143, 91, 0);
                labelAviso.Text = "Para registrar vendas ou perdas, primeiro inicie este evento na tela Eventos, em Situação do evento > Iniciar evento.";
            }
            else if (string.Equals(situacaoEvento, SituacoesEvento.Cancelado, StringComparison.OrdinalIgnoreCase))
            {
                lblContextoRegistro.Text = "Evento cancelado — novos lançamentos estão bloqueados; os registros existentes continuam disponíveis.";
                lblContextoRegistro.BackColor = Color.FromArgb(253, 234, 234);
                lblContextoRegistro.ForeColor = Color.FromArgb(160, 45, 45);
                labelAviso.Text = "Este evento foi cancelado. Consulte os lançamentos existentes ou escolha uma escala de outro evento.";
            }
            else if (string.Equals(situacaoEvento, SituacoesEvento.Finalizado, StringComparison.OrdinalIgnoreCase))
            {
                lblContextoRegistro.Text = "Evento finalizado — novos lançamentos estão bloqueados; os registros existentes continuam disponíveis.";
                lblContextoRegistro.BackColor = Color.FromArgb(237, 240, 244);
                lblContextoRegistro.ForeColor = Color.FromArgb(82, 98, 117);
                labelAviso.Text = "Este evento já foi finalizado. Escolha uma escala de um evento em andamento para registrar vendas ou perdas.";
            }
            else
            {
                lblContextoRegistro.Text = "Não foi possível confirmar a situação do evento. Os lançamentos estão bloqueados.";
                lblContextoRegistro.BackColor = Color.FromArgb(253, 234, 234);
                lblContextoRegistro.ForeColor = Color.FromArgb(160, 45, 45);
                labelAviso.Text = "Atualize a tela e confira a situação do evento antes de registrar.";
            }
        }

        private void ProdutoAlterado(object sender, EventArgs e)
        {
            DataRowView produto = cmbProdutoRegistro.SelectedItem as DataRowView;
            if (produto != null)
            {
                precoPraticado.Value = Convert.ToDecimal(produto["preco_venda"]);
            }
        }

        private void TipoAlterado(object sender, EventArgs e)
        {
            bool venda = cmbTipoRegistro.Text == "venda";
            bool lancamentoPermitido = panelNovoRegistro.Visible && eventoEmAndamentoAtual && cmbProdutoRegistro.Items.Count > 0;
            precoPraticado.Enabled = venda && lancamentoPermitido;
            formaPagamento.Enabled = venda && lancamentoPermitido;
            btnRegistrar.Text = venda ? "Registrar venda" : "Registrar perda";
            lblObservacao.Text = venda ? "Observação" : "Motivo da perda *";
        }

        private void AtualizarRegistros()
        {
            try
            {
                DataTable dados = consultas.Registros(usuarioAutenticado, IdEvento(), new DateTime(2000, 1, 1), DateTime.Today);
                DataRowView escala = cmbSetorRegistro.SelectedItem as DataRowView;
                HashSet<int> permitidos = new HashSet<int>();
                if (escala != null)
                    foreach (Registro registro in new RegistroService().ListarPorEscala(usuarioAutenticado, Convert.ToInt32(escala["id_escala"])))
                        permitidos.Add(registro.IdRegistro);
                else
                    foreach (object item in cmbSetorRegistro.Items)
                    {
                        var escalaFiltrada = item as DataRowView;
                        if (escalaFiltrada == null)
                            continue;
                        foreach (Registro registro in new RegistroService().ListarPorEscala(usuarioAutenticado, Convert.ToInt32(escalaFiltrada["id_escala"])))
                            permitidos.Add(registro.IdRegistro);
                    }

                dgvRegistrosEquipe.Columns[1].Visible = escala == null;
                dgvRegistrosEquipe.Columns[2].Visible = escala == null;
                HashSet<int> lancamentos = new HashSet<int>();
                List<object[]> linhas = new List<object[]>();
                decimal totalVendas = 0;
                bool totalCompleto = true;
                foreach (DataRow linha in dados.Rows)
                {
                    int id = Convert.ToInt32(linha["id_registro"]);
                    if (!permitidos.Contains(id))
                        continue;
                    lancamentos.Add(id);
                    bool venda = Convert.ToString(linha["tipo"]) == "venda";
                    if (venda)
                    {
                        if (linha.IsNull("total"))
                            totalCompleto = false;
                        else
                            totalVendas += Convert.ToDecimal(linha["total"]);
                    }

                    string pagamento = Convert.ToString(linha["forma_pagamento"]);
                    linhas.Add(new object[] { id, linha["data_hora"], linha["responsavel"], linha["setor"], linha["turno"], venda ? "Venda" : "Perda", linha["produto"], linha["quantidade"], linha["observacao"], venda ? linha["preco_unitario_praticado"] : DBNull.Value, !venda ? "—" : TextoInterface.FormatarColunaBanco("forma_pagamento", pagamento), venda ? linha["total"] : DBNull.Value });
                }

                dgvRegistrosEquipe.Rows.Clear();
                foreach (object[] linha in linhas)
                {
                    int indice = dgvRegistrosEquipe.Rows.Add(linha[1], linha[2], linha[3], linha[4], linha[5], linha[6], linha[7], linha[8], linha[9], linha[10], linha[0], linha[11]);
                    dgvRegistrosEquipe.Rows[indice].Tag = linha[0];
                }

                resumoRegistros.Text = (escala == null ? "Nas escalas filtradas: " : "Na escala: ") + lancamentos.Count + " lançamento(s) • " + dgvRegistrosEquipe.Rows.Count + " item(ns) • Vendas: " + (totalCompleto ? totalVendas.ToString("C2") : "preços incompletos");
                VisualUtils.ExibirEstadoVazio(dgvRegistrosEquipe, "Nenhuma venda ou perda encontrada para os filtros selecionados.");
                VisualUtils.FiltrarGrades(panelListaRegistros, pesquisa.Text.Trim());
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível atualizar os lançamentos. Seus registros continuam salvos.";
                FeedbackUsuario.ExibirErro(this, erro, labelAviso.Text, labelAviso);
            }
        }

        private void CorrigirRegistro(object sender, EventArgs e)
        {
            if (dgvRegistrosEquipe.SelectedRows.Count == 0 || !dgvRegistrosEquipe.SelectedRows[0].Visible || usuarioAutenticado == null)
            {
                labelAviso.Text = "Selecione um registro autorizado.";
                return;
            }

            int id = Convert.ToInt32(dgvRegistrosEquipe.SelectedRows[0].Tag);
            using (CorrecaoOperacaoForm formulario = new CorrecaoOperacaoForm(usuarioAutenticado, "registro", id))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    AtualizarRegistros();
                    labelAviso.Text = "Correção confirmada, estoque atualizado e histórico preservado. Confira novamente a apuração, se necessário.";
                }
            }
        }

        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (registrando || !btnRegistrar.Enabled)
            {
                return;
            }

            DataRowView escala = cmbSetorRegistro.SelectedItem as DataRowView;
            DataRowView produto = cmbProdutoRegistro.SelectedItem as DataRowView;
            if (escala == null || produto == null || usuarioAutenticado == null)
            {
                labelAviso.Text = "Selecione uma escala e um produto autorizados.";
                return;
            }

            registrando = true;
            btnRegistrar.Enabled = false;
            try
            {
                Registro registro = new Registro();
                registro.IdEscala = Convert.ToInt32(escala["id_escala"]);
                registro.Tipo = cmbTipoRegistro.Text;
                registro.FormaPagamento = formaPagamento.Text;
                registro.Observacao = txtObservacaoRegistro.Text;
                registro.ChaveOperacao = chaveOperacao;
                RegistroItem item = new RegistroItem();
                item.IdProdutoSetor = Convert.ToInt32(produto["id_produto_setor"]);
                item.Quantidade = numQuantidadeRegistro.Value;
                item.PrecoUnitarioPraticado = precoPraticado.Value;
                int id = new RegistroService().Registrar(usuarioAutenticado, registro, new List<RegistroItem> { item });
                chaveOperacao = Guid.NewGuid().ToString();
                txtObservacaoRegistro.Clear();
                numQuantidadeRegistro.Value = 1;
                labelAviso.Text = "Registro " + id + " confirmado. O estoque foi atualizado.";
                FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
                AtualizarRegistros();
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                // Drena cliques já enfileirados antes de habilitar uma nova operação.
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(btnRegistrar_Click_BeginInvoke));
                }
                else
                {
                    registrando = false;
                }
            }
        }

        private void AjustarCamposAoEspaco()
        {
            if (ajustandoLayout || legendaPreco == null)
                return;
            ajustandoLayout = true;
            SuspendLayout();
            try
            {
                panelCorpo.Width = Math.Max(700, ClientSize.Width - 60);
                panelFiltrosRegistros.Width = panelNovoRegistro.Width = panelListaRegistros.Width = panelCorpo.ClientSize.Width;
                panelFiltrosRegistros.Height = 104;
                int colunaFiltro = (panelFiltrosRegistros.ClientSize.Width - 68) / 3;
                lblFuncionario.SetBounds(18, 14, colunaFiltro, 22);
                cmbPessoaRegistro.SetBounds(18, 44, colunaFiltro, 30);
                legendaEvento.SetBounds(34 + colunaFiltro, 14, colunaFiltro, 22);
                filtroEvento.SetBounds(34 + colunaFiltro, 44, colunaFiltro, 30);
                lblSetor.SetBounds(50 + colunaFiltro * 2, 14, colunaFiltro, 22);
                cmbSetorRegistro.SetBounds(50 + colunaFiltro * 2, 44, colunaFiltro, 30);
                cmbSetorRegistro.DropDownWidth = Math.Max(540, colunaFiltro);
                panelNovoRegistro.Top = panelFiltrosRegistros.Bottom + 16;
                lblContextoRegistro.Width = Math.Max(300, panelNovoRegistro.ClientSize.Width - 36);
                lblContextoRegistro.AutoEllipsis = true;
                lblSubtitulo.Width = Math.Max(300, ClientSize.Width - lblSubtitulo.Left - 24);
                lblSubtitulo.AutoEllipsis = true;
                labelAviso.Width = Math.Max(300, ClientSize.Width - labelAviso.Left - 24);
                Control corrigir = Controls["btnCorrigirRegistro"];
                if (corrigir != null)
                    corrigir.Left = Math.Max(360, ClientSize.Width - corrigir.Width - 24);
                lblTitulo.Width = Math.Max(240, ClientSize.Width - 350);
                panelListaRegistros.Top = panelNovoRegistro.Visible ? panelNovoRegistro.Bottom + 16 : panelFiltrosRegistros.Bottom + 16;
                panelListaRegistros.Height = Math.Max(260, ClientSize.Height - panelCorpo.Top - panelListaRegistros.Top - 72);
                panelCorpo.Height = panelListaRegistros.Bottom;
                labelAviso.Top = panelCorpo.Bottom + 10;
                AutoScrollMinSize = new Size(1020, labelAviso.Bottom + 12);
                resumoRegistros.Width = Math.Max(220, panelListaRegistros.ClientSize.Width - 370);
                bool compacto = panelNovoRegistro.ClientSize.Width < 990;
                int produtoEsquerda = compacto ? 164 : 255;
                int produtoLargura = compacto ? Math.Max(180, panelNovoRegistro.ClientSize.Width - 464) : 300;
                int quantidadeEsquerda = compacto ? produtoEsquerda + produtoLargura + 16 : 575;
                int precoEsquerda = compacto ? quantidadeEsquerda + 146 : 735;
                cmbTipoRegistro.Width = compacto ? 130 : 215;
                lblTipo.Width = compacto ? 130 : 180;
                lblProduto.Left = cmbProdutoRegistro.Left = produtoEsquerda;
                lblProduto.Width = cmbProdutoRegistro.Width = produtoLargura;
                lblQuantidade.Left = numQuantidadeRegistro.Left = quantidadeEsquerda;
                lblQuantidade.Width = numQuantidadeRegistro.Width = compacto ? 130 : 135;
                legendaPreco.Left = precoPraticado.Left = precoEsquerda;
                legendaPreco.Width = compacto ? 130 : 200;
                btnRegistrar.Width = compacto ? 185 : 220;
                btnRegistrar.Left = compacto ? panelNovoRegistro.ClientSize.Width - btnRegistrar.Width - 18 : 748;
                Panel painelObservacao = txtObservacaoRegistro.Parent as Panel;
                int observacaoEsquerda = painelObservacao == null ? txtObservacaoRegistro.Left : painelObservacao.Left;
                int observacaoLargura = compacto ? Math.Max(140, btnRegistrar.Left - observacaoEsquerda - 16) : 560;
                if (painelObservacao != null && painelObservacao.Parent == panelNovoRegistro)
                    painelObservacao.Width = observacaoLargura;
                else
                    txtObservacaoRegistro.Width = observacaoLargura;
            }
            finally
            {
                ResumeLayout(false);
                ajustandoLayout = false;
            }
        }

        private void ExibirErro(Exception erro)
        {
            if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível concluir a operação. Confira os dados e tente novamente.", labelAviso);
            }
            else
            {
                FeedbackUsuario.ExibirErro(this, erro, "Não foi possível concluir a operação. Tente novamente.", labelAviso);
                Trace.WriteLine(erro.GetType().Name + " em RegistrosControl: " + erro.StackTrace);
            }
        }

        private void filtroEvento_SelectedIndexChanged(object s, EventArgs e)
        {
            if (!carregando)
                PreencherEscalasFiltradas();
        }

        private void Preparar_AdicionarPesquisa(string texto)
        {
            VisualUtils.FiltrarGrades(panelListaRegistros, texto);
            foreach (DataGridViewRow linha in dgvRegistrosEquipe.SelectedRows)
                if (!linha.Visible)
                    linha.Selected = false;
        }

        private void Tela_SizeChanged(object s, EventArgs e)
        {
            AjustarCamposAoEspaco();
        }

        private void panelNovoRegistro_VisibleChanged(object s, EventArgs e)
        {
            AjustarCamposAoEspaco();
        }

        private void cmbSetorRegistro_Format(object s, ListControlConvertEventArgs e)
        {
            var escala = e.ListItem as DataRowView;
            if (escala == null)
                return;
            var pessoa = cmbPessoaRegistro.SelectedItem as OpcaoFiltro;
            var eventoFiltro = filtroEvento.SelectedItem as OpcaoFiltro;
            e.Value = (eventoFiltro != null && eventoFiltro.Id == 0 ? Convert.ToString(escala["evento_nome"]) + " · " : "") + (pessoa != null && pessoa.Id == 0 ? Convert.ToString(escala["pessoa_nome"]) + " · " : "") + Convert.ToString(escala["descricao_curta"]);
        }

        private void menu_Opening(object s, CancelEventArgs e)
        {
            e.Cancel = dgvRegistrosEquipe.SelectedRows.Count == 0 || !dgvRegistrosEquipe.SelectedRows[0].Visible;
        }

        private void dgvRegistrosEquipe_CellMouseDown(object s, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvRegistrosEquipe.CurrentCell = dgvRegistrosEquipe.Rows[e.RowIndex].Cells[0];
                dgvRegistrosEquipe.Rows[e.RowIndex].Selected = true;
            }
            else if (e.Button == MouseButtons.Right)
                dgvRegistrosEquipe.ClearSelection();
        }

        private void btnRegistrar_Click_BeginInvoke()
        {
            if (!IsDisposed)
            {
                registrando = false;
                btnRegistrar.Enabled = panelNovoRegistro.Visible && eventoEmAndamentoAtual && cmbProdutoRegistro.Items.Count > 0;
            }
        }
    }
}
