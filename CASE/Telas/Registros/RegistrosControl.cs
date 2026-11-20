using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class RegistrosControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento eventoSelecionado;
        private readonly ConsultaOperacionalService consultas = new ConsultaOperacionalService();
        private TextBox pesquisa;
        private readonly NumericUpDown precoPraticado = new NumericUpDown();
        private readonly ComboBox formaPagamento = new ComboBox();
        private Label legendaPreco;
        private bool ajustandoLayout;
        private bool carregando = true;
        private bool registrando;
        private string chaveOperacao = Guid.NewGuid().ToString();
        private readonly Label resumoRegistros = new Label();

        public RegistrosControl() : this(null, null)
        {
        }

        public RegistrosControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            FrontendTheme.Aplicar(this);
            lblTitulo.Text = "Vendas e perdas";
            lblSubtitulo.Text = "Consulte os lançamentos da escala selecionada. Revisar lançamento permite corrigir erros com justificativa.";
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
            dgvRegistrosEquipe.SelectionChanged += (s, e) =>
                abrirCorrecao.Enabled = usuarioAutenticado != null && dgvRegistrosEquipe.SelectedRows.Count > 0 &&
                    dgvRegistrosEquipe.SelectedRows[0].Visible;
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
            lblSetor.Text = "Escala de trabalho";
            lblFuncionario.Visible = false;
            cmbPessoaRegistro.Visible = false;
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
            panelNovoRegistro.Controls.Add(FrontendTheme.Legenda("Pagamento",
                new Rectangle(18, 163, 130, 22)));
            panelNovoRegistro.Height = 215;
            panelListaRegistros.Top = 335;
            panelListaRegistros.Height = 260;
            panelListaRegistros.Padding = new Padding(12, 70, 12, 12);
            resumoRegistros.Name = "lblResumoRegistros";
            resumoRegistros.SetBounds(18, 20, 600, 30);
            resumoRegistros.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            resumoRegistros.ForeColor = VisualUtils.Marinho;
            panelListaRegistros.Controls.Add(resumoRegistros);
            pesquisa = VisualUtils.AdicionarPesquisa(panelListaRegistros, "txtPesquisarRegistro", "Pesquisar nos registros",
                texto =>
                {
                    VisualUtils.FiltrarGrades(panelListaRegistros, texto);
                    foreach (DataGridViewRow linha in dgvRegistrosEquipe.SelectedRows)
                        if (!linha.Visible)
                            linha.Selected = false;
                });
            dgvRegistrosEquipe.Dock = DockStyle.Fill;
            panelCorpo.Height = panelListaRegistros.Bottom;
            labelAviso.Top = panelCorpo.Bottom + 12;
            labelAviso.Height = 42;
            AutoScrollMinSize = new Size(760, labelAviso.Bottom + 12);
            SizeChanged += (s, e) => AjustarCamposAoEspaco();
            panelNovoRegistro.VisibleChanged += (s, e) => AjustarCamposAoEspaco();
            AjustarCamposAoEspaco();
            cmbProdutoRegistro.DisplayMember = "nome";
            cmbSetorRegistro.DisplayMember = "descricao";
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
            int[] largurasMinimas = { 145, 140, 140, 120, 85, 180, 65, 180, 110, 90 };
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
            menu.Opening += (s, e) => e.Cancel = dgvRegistrosEquipe.SelectedRows.Count == 0 ||
                !dgvRegistrosEquipe.SelectedRows[0].Visible;
            dgvRegistrosEquipe.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvRegistrosEquipe.CurrentCell = dgvRegistrosEquipe.Rows[e.RowIndex].Cells[0];
                    dgvRegistrosEquipe.Rows[e.RowIndex].Selected = true;
                }
                else if (e.Button == MouseButtons.Right)
                    dgvRegistrosEquipe.ClearSelection();
            };
            Disposed += (s, e) => menu.Dispose();
            btnRegistrar.Enabled = false;
            if (usuarioAutenticado == null)
            {
                carregando = false;
                return;
            }
            try
            {
                DataTable escalas = consultas.Escalas(usuarioAutenticado, IdEvento());
                foreach (DataRowView linha in escalas.DefaultView)
                {
                    cmbSetorRegistro.Items.Add(linha);
                }
                carregando = false;
                if (cmbSetorRegistro.Items.Count > 0)
                {
                    cmbSetorRegistro.SelectedIndex = 0;
                }
                else
                {
                    labelAviso.Text = "Não há escalas autorizadas. Cadastre equipe, jornada e escala primeiro.";
                }
                if (cmbSetorRegistro.Items.Count == 0)
                    AtualizarRegistros();
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                carregando = false;
            }
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
                    btnRegistrar.Enabled = false;
                    AtualizarRegistros();
                    AjustarCamposAoEspaco();
                    return;
                }
                DataTable produtos = consultas.ProdutosDaEscala(usuarioAutenticado,
                    Convert.ToInt32(escala["id_escala"]));
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
                bool administrador = string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase);
                bool podeRegistrar = administrador || string.Equals(perfil, "Membro", StringComparison.OrdinalIgnoreCase);
                panelNovoRegistro.Visible = podeRegistrar;
                btnRegistrar.Enabled = podeRegistrar && produtos.Rows.Count > 0;
                lblContextoRegistro.Text = "Novo lançamento na escala selecionada";
                lblTitulo.Text = podeRegistrar ? "Registrar vendas e perdas" : "Consulta de vendas e perdas";
                lblSubtitulo.Text = podeRegistrar
                    ? "Escolha sua escala, registre a venda ou perda e acompanhe seus lançamentos abaixo."
                    : "Consulte os lançamentos da escala selecionada. Revisar lançamento permite corrigir erros com justificativa.";
                labelAviso.Text = administrador
                    ? "Como administrador, selecione uma escala para registrar a venda ou perda. O lançamento ficará associado à sua conta."
                    : perfil == "Membro"
                    ? "Cada lançamento reduz o estoque do setor. Perdas exigem justificativa; vendas informam preço e pagamento."
                    : "Os membros registram as vendas e perdas. Aqui você consulta ou corrige lançamentos; o Financeiro confere os recebimentos.";
                AtualizarRegistros();
                AjustarCamposAoEspaco();
            }
            catch (Exception erro)
            {
                btnRegistrar.Enabled = false;
                ExibirErro(erro);
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
            precoPraticado.Enabled = venda;
            formaPagamento.Enabled = venda;
            btnRegistrar.Text = venda ? "Registrar venda" : "Registrar perda";
            lblObservacao.Text = venda ? "Observação" : "Motivo da perda *";
        }

        private void AtualizarRegistros()
        {
            try
            {
                DataTable dados = consultas.Registros(usuarioAutenticado, IdEvento(),
                    new DateTime(2000, 1, 1), DateTime.Today);
                DataRowView escala = cmbSetorRegistro.SelectedItem as DataRowView;
                HashSet<int> permitidos = new HashSet<int>();
                if (escala != null)
                    foreach (Registro registro in new RegistroService().ListarPorEscala(usuarioAutenticado, Convert.ToInt32(escala["id_escala"])))
                        permitidos.Add(registro.IdRegistro);
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
                        if (linha.IsNull("total")) totalCompleto = false;
                        else totalVendas += Convert.ToDecimal(linha["total"]);
                    }
                    string pagamento = Convert.ToString(linha["forma_pagamento"]);
                    linhas.Add(new object[] { id, linha["data_hora"], linha["responsavel"], linha["setor"],
                        linha["turno"], venda ? "Venda" : "Perda", linha["produto"], linha["quantidade"],
                        linha["observacao"], venda ? linha["preco_unitario_praticado"] : DBNull.Value,
                        !venda ? "—" : pagamento == "cartao" ? "Cartão" : pagamento == "pix" ? "Pix" : pagamento == "dinheiro" ? "Dinheiro" : "Não informado",
                        venda ? linha["total"] : DBNull.Value });
                }
                dgvRegistrosEquipe.Rows.Clear();
                foreach (object[] linha in linhas)
                {
                    int indice = dgvRegistrosEquipe.Rows.Add(linha[1], linha[2], linha[3], linha[4], linha[5],
                        linha[6], linha[7], linha[8], linha[9], linha[10], linha[0], linha[11]);
                    dgvRegistrosEquipe.Rows[indice].Tag = linha[0];
                }
                resumoRegistros.Text = "Na escala: " + lancamentos.Count + " lançamento(s) • " + dgvRegistrosEquipe.Rows.Count +
                    " item(ns) • Vendas: " + (totalCompleto ? totalVendas.ToString("C2") : "preços incompletos");
                VisualUtils.ExibirEstadoVazio(dgvRegistrosEquipe, escala == null ? "Selecione uma escala para consultar os lançamentos." : "Nenhuma venda ou perda registrada nesta escala.");
                VisualUtils.FiltrarGrades(panelListaRegistros, pesquisa.Text.Trim());
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível atualizar os lançamentos. Seus registros continuam salvos.";
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, labelAviso.Text, labelAviso);
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
                int id = new RegistroService().Registrar(usuarioAutenticado, registro,
                    new List<RegistroItem> { item });
                chaveOperacao = Guid.NewGuid().ToString();
                txtObservacaoRegistro.Clear();
                numQuantidadeRegistro.Value = 1;
                labelAviso.Text = "Registro " + id + " confirmado. O estoque foi atualizado.";
                CASE.Utils.FeedbackUsuario.Exibir(this, labelAviso.Text, CASE.Utils.TipoFeedback.Sucesso);
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
                    BeginInvoke(new Action(() =>
                    {
                        if (!IsDisposed)
                        {
                            registrando = false;
                            btnRegistrar.Enabled = true;
                        }
                    }));
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
                cmbSetorRegistro.Width = Math.Max(300, panelFiltrosRegistros.ClientSize.Width - 36);
                lblContextoRegistro.Width = Math.Max(300, panelNovoRegistro.ClientSize.Width - 36);
                lblContextoRegistro.AutoEllipsis = true;
                lblSubtitulo.Width = Math.Max(300, ClientSize.Width - lblSubtitulo.Left - 24);
                lblSubtitulo.AutoEllipsis = true;
                labelAviso.Width = Math.Max(300, ClientSize.Width - labelAviso.Left - 24);
                Control corrigir = Controls["btnCorrigirRegistro"];
                if (corrigir != null)
                    corrigir.Left = Math.Max(360, ClientSize.Width - corrigir.Width - 24);
                lblTitulo.Width = Math.Max(240, ClientSize.Width - 350);
                panelListaRegistros.Top = panelNovoRegistro.Visible ? 335 : 100;
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
                int observacaoLargura = compacto
                    ? Math.Max(140, btnRegistrar.Left - observacaoEsquerda - 16)
                    : 560;
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
            if (erro is ArgumentException || erro is InvalidOperationException ||
                erro is UnauthorizedAccessException)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível concluir a operação. Confira os dados e tente novamente.", labelAviso);
            }
            else
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível concluir a operação. Tente novamente.", labelAviso);
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em RegistrosControl: " + erro.StackTrace);
            }
        }
    }
}
