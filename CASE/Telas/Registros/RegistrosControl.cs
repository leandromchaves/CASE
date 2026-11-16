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
        private readonly NumericUpDown precoPraticado = new NumericUpDown();
        private readonly ComboBox formaPagamento = new ComboBox();
        private Label legendaPreco;
        private bool ajustandoLayout;
        private bool carregando = true;
        private bool registrando;
        private string chaveOperacao = Guid.NewGuid().ToString();

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
            lblSubtitulo.Text = "Selecione uma linha e use Corrigir / histórico. O botão direito também está disponível.";
            Button abrirCorrecao = new Button();
            abrirCorrecao.Name = "btnCorrigirRegistro";
            abrirCorrecao.Text = "Corrigir / histórico";
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
                abrirCorrecao.Enabled = usuarioAutenticado != null && dgvRegistrosEquipe.SelectedRows.Count > 0;
            lblContextoRegistro.Text = "Selecione uma escala de trabalho";
            panelNovoRegistro.Visible = false;
            lblObservacao.Width = 120;
            txtObservacaoRegistro.Left = 150;
            txtObservacaoRegistro.AutoSize = true;
            txtObservacaoRegistro.BorderStyle = BorderStyle.FixedSingle;
            txtObservacaoRegistro.BringToFront();
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
            panelListaRegistros.Padding = new Padding(12);
            dgvRegistrosEquipe.Dock = DockStyle.Fill;
            panelCorpo.Height = panelListaRegistros.Bottom;
            labelAviso.Top = panelCorpo.Bottom + 12;
            labelAviso.Height = 42;
            AutoScrollMinSize = new Size(760, labelAviso.Bottom + 12);
            SizeChanged += (s, e) => AjustarCamposAoEspaco();
            AjustarCamposAoEspaco();
            cmbProdutoRegistro.DisplayMember = "nome";
            cmbSetorRegistro.DisplayMember = "descricao";
            cmbProdutoRegistro.SelectedIndexChanged += ProdutoAlterado;
            cmbTipoRegistro.SelectedIndexChanged += TipoAlterado;
            dgvRegistrosEquipe.Columns.Add("precoPraticado", "Preço praticado");
            dgvRegistrosEquipe.Columns.Add("pagamento", "Pagamento");
            int[] largurasMinimas = { 145, 140, 140, 120, 85, 180, 65, 180, 110, 90 };
            for (int coluna = 0; coluna < largurasMinimas.Length; coluna++)
            {
                dgvRegistrosEquipe.Columns[coluna].MinimumWidth = largurasMinimas[coluna];
            }
            dgvRegistrosEquipe.Columns[6].DefaultCellStyle.Format = "N0";
            labelAviso.Text = "Autentique-se para registrar operações.";
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem corrigir = new ToolStripMenuItem("Corrigir registro / consultar histórico");
            corrigir.Name = "menuCorrigirRegistro";
            corrigir.Click += CorrigirRegistro;
            menu.Items.Add(corrigir);
            dgvRegistrosEquipe.ContextMenuStrip = menu;
            dgvRegistrosEquipe.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvRegistrosEquipe.CurrentCell = dgvRegistrosEquipe.Rows[e.RowIndex].Cells[0];
                    dgvRegistrosEquipe.Rows[e.RowIndex].Selected = true;
                }
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
                panelNovoRegistro.Visible = perfil == "Membro";
                btnRegistrar.Enabled = perfil == "Membro" && produtos.Rows.Count > 0;
                lblContextoRegistro.Text = Convert.ToString(escala["descricao"]);
                labelAviso.Text = "Vendas e perdas são permitidas durante eventos em andamento.";
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
        }

        private void AtualizarRegistros()
        {

            DataTable dados = consultas.Registros(usuarioAutenticado, IdEvento(),
                new DateTime(2000, 1, 1), DateTime.Today);
            dgvRegistrosEquipe.Rows.Clear();
            foreach (DataRow linha in dados.Rows)
            {
                int indice = dgvRegistrosEquipe.Rows.Add(linha["data_hora"], linha["responsavel"], linha["setor"],
                    linha["turno"], linha["tipo"], linha["produto"], linha["quantidade"],
                    linha["observacao"], linha["preco_unitario_praticado"], linha["forma_pagamento"]);
                dgvRegistrosEquipe.Rows[indice].Tag = Convert.ToInt32(linha["id_registro"]);
            }
            VisualUtils.ExibirEstadoVazio(dgvRegistrosEquipe, "Nenhum registro autorizado encontrado.");
        }

        private void CorrigirRegistro(object sender, EventArgs e)
        {
            if (dgvRegistrosEquipe.SelectedRows.Count == 0 || usuarioAutenticado == null)
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
                txtObservacaoRegistro.Width = compacto ? Math.Max(140, btnRegistrar.Left - txtObservacaoRegistro.Left - 16) : 560;
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
                labelAviso.Text = erro.Message;
            }
            else
            {
                labelAviso.Text = "Não foi possível concluir a operação. Confira a conexão e tente novamente.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em RegistrosControl: " + erro.StackTrace);
            }
        }
    }
}
