using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class ProdutosControl : UserControl
    {
        private readonly ProdutoService produtoService = new ProdutoService();
        private readonly CategoriaService categoriaService = new CategoriaService();
        private readonly Usuario usuarioAutenticado;
        private List<Produto> produtos = new List<Produto>();
        private List<Categoria> categorias = new List<Categoria>();
        private readonly List<System.Drawing.Image> imagensDaGrade = new List<System.Drawing.Image>();

        public ProdutosControl() : this(null)
        {
        }

        public ProdutosControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            lblSubtituloProdutos.Text =
                "Gerencie o catálogo. Preços de venda são definidos por setor em Equipes e setores.";

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelAcoesProdutos, 12);
            VisualUtils.Arredondar(panelListaProdutos, 12);
            VisualUtils.Arredondar(btnNovoProduto, 8);
            VisualUtils.Arredondar(btnPaginaAnterior, 8);
            VisualUtils.Arredondar(btnProximaPagina, 8);
            VisualUtils.AplicarBorda(panelAcoesProdutos);
            VisualUtils.AplicarBorda(panelListaProdutos);

            panelAcoesProdutos.SizeChanged += AjustarArredondamento;
            panelListaProdutos.SizeChanged += AjustarArredondamento;

            colCodigo.Visible = false;
            colQuantidade.Visible = false;

            DataGridViewImageColumn colunaImagem = new DataGridViewImageColumn();
            colunaImagem.Name = "colImagemProduto";
            colunaImagem.HeaderText = "Imagem";
            colunaImagem.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colunaImagem.FillWeight = 8;
            colunaImagem.MinimumWidth = 76;
            colunaImagem.DefaultCellStyle.NullValue = null;
            colunaImagem.DefaultCellStyle.Padding = new Padding(6);
            colunaImagem.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvProdutos.Columns.Insert(0, colunaImagem);

            colAcoes.Text = "Editar";

            DataGridViewButtonColumn colunaExcluir = new DataGridViewButtonColumn();
            colunaExcluir.Name = "colExcluir";
            colunaExcluir.HeaderText = "";
            colunaExcluir.Text = "";
            colunaExcluir.UseColumnTextForButtonValue = false;
            colunaExcluir.FlatStyle = FlatStyle.Flat;
            colunaExcluir.FillWeight = 14;
            colunaExcluir.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(185, 48, 48);
            colunaExcluir.Visible = false;
            dgvProdutos.Columns.Add(colunaExcluir);
            ConfigurarApresentacaoDaGrade();

            btnNovoProduto.Click += BtnNovoProduto_Click;
            VisualUtils.EstilizarPesquisa(txtPesquisarProduto, "Pesquisar produtos");
            dgvProdutos.CellContentClick += DgvProdutos_CellContentClick;
            txtPesquisarProduto.TextChanged += Filtros_Changed;
            cmbFiltroCategoria.SelectedIndexChanged += Filtros_Changed;
            ContextMenuStrip menuProduto = new ContextMenuStrip();
            ToolStripMenuItem fornecedores = new ToolStripMenuItem("Fornecedores do produto selecionado");
            fornecedores.Click += AbrirFornecedoresDoProduto;
            menuProduto.Items.Add(fornecedores);
            dgvProdutos.ContextMenuStrip = menuProduto;
            dgvProdutos.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvProdutos.CurrentCell = dgvProdutos.Rows[e.RowIndex].Cells[0];
                    dgvProdutos.Rows[e.RowIndex].Selected = true;
                }
            };
            Disposed += (s, e) => menuProduto.Dispose();

            btnPaginaAnterior.Enabled = false;
            btnProximaPagina.Enabled = false;
            dgvProdutos.ClearSelection();

            VisualUtils.ExibirEstadoVazio(
                dgvProdutos,
                "Nenhum produto cadastrado.");

            if (usuarioAutenticado != null)
            {
                CarregarProdutos();
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                    btnNovoProduto.Enabled = perfil == "Administrador";
                    colunaExcluir.Visible = perfil == "Administrador";
                    if (perfil != "Administrador")
                    {
                        colAcoes.Text = "Consultar";
                    }
                }
                catch (Exception)
                {
                    btnNovoProduto.Enabled = false;
                }
            }
            else
            {
                btnNovoProduto.Enabled = false;
                lblPaginacao.Text = "Entre no sistema para consultar os produtos.";
            }
            Disposed += (s, e) => LiberarImagensDaGrade();
        }

        private void ConfigurarApresentacaoDaGrade()
        {
            // O Designer compartilhava este estilo com a coluna de ações. Separar as
            // instâncias impede que centralizar o botão "Editar" centralize também
            // todas as células das linhas alternadas.
            dgvProdutos.AlternatingRowsDefaultCellStyle =
                new DataGridViewCellStyle(dgvProdutos.AlternatingRowsDefaultCellStyle);
            colAcoes.DefaultCellStyle = new DataGridViewCellStyle(colAcoes.DefaultCellStyle);

            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProdutos.RowTemplate.Height = 68;
            dgvProdutos.ColumnHeadersHeight = 44;
            dgvProdutos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgvProdutos.AlternatingRowsDefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
            dgvProdutos.AlternatingRowsDefaultCellStyle.Padding = dgvProdutos.DefaultCellStyle.Padding;
            dgvProdutos.AlternatingRowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            colProduto.FillWeight = 30;
            colCategoria.FillWeight = 22;
            colPreco.FillWeight = 22;
            colAcoes.FillWeight = 14;

            foreach (DataGridViewColumn coluna in dgvProdutos.Columns)
            {
                coluna.DefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
                coluna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            colAcoes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colAcoes.DefaultCellStyle.Padding = new Padding(6, 8, 6, 8);
            dgvProdutos.RowTemplate.Height = 68;
        }

        private void AbrirFornecedoresDoProduto(object sender, EventArgs e)
        {
            if (dgvProdutos.SelectedRows.Count == 0 || usuarioAutenticado == null)
            {
                return;
            }
            Produto produto = dgvProdutos.SelectedRows[0].Tag as Produto;
            if (produto == null)
            {
                return;
            }
            GerenciarFornecedores(this, usuarioAutenticado, produto);
        }

        internal static void GerenciarFornecedores(IWin32Window proprietario, Usuario usuarioAutenticado, Produto produto)
        {
            ProdutoService produtoService = new ProdutoService();
            try
            {
                List<Fornecedor> fornecedores = new FornecedorService().Listar(usuarioAutenticado);
                List<ProdutoFornecedor> vinculos = produtoService.ListarFornecedores(usuarioAutenticado, produto.IdProduto);
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                using (Form janela = new Form())
                using (ComboBox fornecedor = new ComboBox())
                using (TextBox codigo = new TextBox())
                using (Button salvar = new Button())
                using (DataGridView grade = new DataGridView())
                {
                    janela.Text = "Fornecedores · " + produto.Nome;
                    janela.Name = "VinculosProdutoForm";
                    janela.ClientSize = new System.Drawing.Size(600, 430);
                    janela.MinimumSize = janela.Size;
                    janela.StartPosition = FormStartPosition.CenterParent;
                    IconoAplicacao.Aplicar(janela);
                    fornecedor.DisplayMember = "Nome";
                    fornecedor.Name = "cmbFornecedorVinculo";
                    fornecedor.DropDownStyle = ComboBoxStyle.DropDownList;
                    fornecedor.SetBounds(20, 48, 335, 28);
                    codigo.MaxLength = 50;
                    codigo.Name = "txtCodigoFornecedorVinculo";
                    codigo.SetBounds(375, 48, 205, 28);
                    codigo.ReadOnly = perfil != "Administrador";
                    salvar.Text = "Salvar vínculo";
                    salvar.BackColor = VisualUtils.Azul;
                    salvar.ForeColor = System.Drawing.Color.White;
                    salvar.Name = "btnSalvarVinculoFornecedor";
                    salvar.SetBounds(20, 93, 160, 36);
                    salvar.Enabled = perfil == "Administrador" && fornecedores.Count > 0;
                    Label mensagem = FrontendTheme.Legenda("Vínculos do catálogo. Não alteram preços de venda nem saldos.",
                        new System.Drawing.Rectangle(20, 145, 560, 42));
                    grade.SetBounds(20, 195, 560, 210);
                    grade.Name = "dgvVinculosFornecedor";
                    grade.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    grade.AllowUserToAddRows = false;
                    grade.AllowUserToDeleteRows = false;
                    grade.ReadOnly = true;
                    grade.RowHeadersVisible = false;
                    grade.BackgroundColor = System.Drawing.Color.White;
                    grade.BorderStyle = BorderStyle.None;
                    grade.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    grade.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    grade.MultiSelect = false;
                    grade.Columns.Add("fornecedor", "Fornecedor");
                    grade.Columns.Add("codigo", "Código no fornecedor");
                    janela.Controls.Add(FrontendTheme.Legenda("Fornecedor", new System.Drawing.Rectangle(20, 20, 335, 22)));
                    janela.Controls.Add(FrontendTheme.Legenda("Código opcional", new System.Drawing.Rectangle(375, 20, 205, 22)));
                    janela.Controls.Add(fornecedor);
                    janela.Controls.Add(codigo);
                    janela.Controls.Add(salvar);
                    janela.Controls.Add(mensagem);
                    janela.Controls.Add(grade);
                    Action sincronizarSelecaoGrade = () =>
                    {
                        grade.ClearSelection();
                        grade.CurrentCell = null;
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        if (selecionado == null)
                        {
                            return;
                        }

                        foreach (DataGridViewRow linha in grade.Rows)
                        {
                            if (linha.Tag != null && Convert.ToInt32(linha.Tag) == selecionado.IdFornecedor)
                            {
                                linha.Selected = true;
                                grade.CurrentCell = linha.Cells[0];
                                return;
                            }
                        }
                    };
                    Action atualizar = () =>
                    {
                        vinculos = produtoService.ListarFornecedores(usuarioAutenticado, produto.IdProduto);
                        grade.Rows.Clear();
                        foreach (ProdutoFornecedor vinculo in vinculos)
                        {
                            foreach (Fornecedor item in fornecedores)
                            {
                                if (item.IdFornecedor == vinculo.IdFornecedor)
                                {
                                    int indice = grade.Rows.Add(item.Nome, vinculo.CodigoNoFornecedor);
                                    grade.Rows[indice].Tag = item.IdFornecedor;
                                    break;
                                }
                            }
                        }
                        VisualUtils.ExibirEstadoVazio(grade, "Nenhum fornecedor vinculado ao produto.");
                        sincronizarSelecaoGrade();
                    };
                    fornecedor.SelectedIndexChanged += (s, argumentos) =>
                    {
                        codigo.Clear();
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        foreach (ProdutoFornecedor vinculo in vinculos)
                        {
                            if (selecionado != null && vinculo.IdFornecedor == selecionado.IdFornecedor)
                            {
                                codigo.Text = vinculo.CodigoNoFornecedor;
                                break;
                            }
                        }
                        sincronizarSelecaoGrade();
                    };
                    salvar.Click += (s, argumentos) =>
                    {
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        if (selecionado == null)
                        {
                            mensagem.Text = "Selecione um fornecedor cadastrado.";
                            return;
                        }
                        try
                        {
                            produtoService.VincularFornecedor(usuarioAutenticado, produto.IdProduto,
                                selecionado.IdFornecedor, codigo.Text.Trim());
                            atualizar();
                            mensagem.Text = "Fornecedor vinculado ao produto.";
                            CASE.Utils.FeedbackUsuario.Exibir(janela, mensagem.Text, CASE.Utils.TipoFeedback.Sucesso);
                        }
                        catch (Exception erro)
                        {
                            mensagem.Text = "Não foi possível salvar o vínculo.";
                            if (erro is ArgumentException || erro is UnauthorizedAccessException)
                            {
                                mensagem.Text = CASE.Utils.FeedbackUsuario.MensagemErro(erro, "Não foi possível salvar o vínculo.");
                            }
                            CASE.Utils.FeedbackUsuario.Exibir(janela, mensagem.Text, CASE.Utils.TipoFeedback.Erro);
                            System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em VincularFornecedor");
                        }
                    };
                    foreach (Fornecedor item in fornecedores)
                    {
                        fornecedor.Items.Add(item);
                    }
                    if (fornecedor.Items.Count > 0)
                    {
                        fornecedor.SelectedIndex = 0;
                    }
                    else
                    {
                        mensagem.Text = "Cadastre um fornecedor antes de vincular ao produto.";
                    }
                    FrontendTheme.Aplicar(janela);
                    atualizar();
                    janela.ShowDialog(proprietario);
                }
            }
            catch (Exception erro)
            {
                MessageBox.Show(proprietario, "Não foi possível consultar os fornecedores autorizados.", "Produtos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em FornecedoresDoProduto");
            }
        }

        private void CarregarProdutos()
        {

            try
            {
                List<Produto> novosProdutos = produtoService.Listar(usuarioAutenticado);
                List<Categoria> novasCategorias = categoriaService.Listar(usuarioAutenticado);
                produtos = novosProdutos;
                categorias = novasCategorias;
                CarregarFiltroDeCategorias();
                ExibirProdutos();
            }
            catch (Exception)
            {
                lblPaginacao.Text = produtos.Count == 0
                    ? "Não foi possível carregar os produtos. Tente novamente."
                    : produtos.Count + " produtos em exibição. A atualização falhou; tente novamente.";
            }
        }

        private void CarregarFiltroDeCategorias()
        {
            cmbFiltroCategoria.Items.Clear();
            cmbFiltroCategoria.Items.Add("Todas as categorias");

            foreach (Categoria categoria in categorias)
            {
                cmbFiltroCategoria.Items.Add(categoria.Nome);
            }

            cmbFiltroCategoria.SelectedIndex = 0;
        }

        private void ExibirProdutos()
        {
            dgvProdutos.Rows.Clear();
            LiberarImagensDaGrade();

            foreach (Produto produto in produtos)
            {
                string nomeCategoria = ObterNomeCategoria(produto.IdCategoria);

                object[] valores = new object[]
                {
                    CriarMiniatura(produto),
                    produto.IdProduto,
                    produto.Nome,
                    nomeCategoria,
                    null,
                    "Definido por setor"
                };

                int indiceLinha = dgvProdutos.Rows.Add(valores);
                dgvProdutos.Rows[indiceLinha].Tag = produto;
                dgvProdutos.Rows[indiceLinha].Cells["colExcluir"].Value = produto.Ativo ? "Remover" : "Reativar";
                if (!produto.Ativo)
                {
                    dgvProdutos.Rows[indiceLinha].DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
                }
            }

            AplicarFiltros();
        }

        private System.Drawing.Image CriarMiniatura(Produto produto)
        {
            if (produto.Imagem == null || produto.Imagem.Length == 0)
            {
                return null;
            }
            try
            {
                using (var memoria = new System.IO.MemoryStream(produto.Imagem))
                using (var original = System.Drawing.Image.FromStream(memoria))
                {
                    const int tamanhoMiniatura = 56;
                    const int margemImagem = 7;
                    var miniatura = new System.Drawing.Bitmap(tamanhoMiniatura, tamanhoMiniatura,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var desenho = System.Drawing.Graphics.FromImage(miniatura))
                    using (var fundo = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    using (var borda = new System.Drawing.Pen(System.Drawing.Color.FromArgb(226, 232, 240)))
                    using (var contorno = VisualUtils.CriarRetanguloArredondado(
                        new System.Drawing.Rectangle(0, 0, tamanhoMiniatura - 1, tamanhoMiniatura - 1), 9))
                    {
                        desenho.Clear(System.Drawing.Color.Transparent);
                        desenho.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        desenho.FillPath(fundo, contorno);
                        desenho.SetClip(contorno);

                        double escala = Math.Min(
                            (double)(tamanhoMiniatura - margemImagem * 2) / original.Width,
                            (double)(tamanhoMiniatura - margemImagem * 2) / original.Height);
                        int largura = Math.Max(1, (int)Math.Round(original.Width * escala));
                        int altura = Math.Max(1, (int)Math.Round(original.Height * escala));
                        int x = (tamanhoMiniatura - largura) / 2;
                        int y = (tamanhoMiniatura - altura) / 2;

                        desenho.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        desenho.DrawImage(original, new System.Drawing.Rectangle(x, y, largura, altura));
                        desenho.ResetClip();
                        desenho.DrawPath(borda, contorno);
                    }
                    imagensDaGrade.Add(miniatura);
                    return miniatura;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private void LiberarImagensDaGrade()
        {
            foreach (System.Drawing.Image imagem in imagensDaGrade)
            {
                imagem.Dispose();
            }
            imagensDaGrade.Clear();
        }

        private string ObterNomeCategoria(int idCategoria)
        {
            foreach (Categoria categoria in categorias)
            {
                if (categoria.IdCategoria == idCategoria)
                {
                    return categoria.Nome;
                }
            }

            return "Categoria não encontrada";
        }

        private void AplicarFiltros()
        {
            dgvProdutos.CurrentCell = null;

            int quantidadeVisivel = 0;
            string textoBusca = txtPesquisarProduto.Text.Trim();

            foreach (DataGridViewRow linha in dgvProdutos.Rows)
            {
                Produto produto = linha.Tag as Produto;

                if (produto == null)
                {
                    linha.Visible = false;
                    continue;
                }

                string nomeCategoria = Convert.ToString(linha.Cells[colCategoria.Index].Value);
                bool nomeCorresponde = produto.Nome.IndexOf(
                    textoBusca,
                    StringComparison.CurrentCultureIgnoreCase) >= 0;
                bool categoriaCorresponde = cmbFiltroCategoria.SelectedIndex <= 0 ||
                    string.Equals(
                        nomeCategoria,
                        cmbFiltroCategoria.Text,
                        StringComparison.CurrentCultureIgnoreCase);

                linha.Visible = nomeCorresponde && categoriaCorresponde;

                if (linha.Visible)
                {
                    quantidadeVisivel++;
                }
            }

            lblPaginacao.Text = quantidadeVisivel + " de " + produtos.Count + " produtos";
            dgvProdutos.ClearSelection();
        }

        private void AbrirFormularioDeProduto(Produto produto)
        {

            using (ProdutoForm formulario = new ProdutoForm(usuarioAutenticado, produto))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarProdutos();
                }
            }
        }

        private void BtnNovoProduto_Click(object sender, EventArgs e)
        {

            using (ProdutoForm formulario = new ProdutoForm(usuarioAutenticado, null))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarProdutos();
                }
            }
        }

        private void DgvProdutos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            Produto produto = dgvProdutos.Rows[e.RowIndex].Tag as Produto;

            if (produto == null)
            {
                return;
            }

            if (dgvProdutos.Columns[e.ColumnIndex] == colAcoes)
            {
                AbrirFormularioDeProduto(produto);
                return;
            }

            if (dgvProdutos.Columns[e.ColumnIndex].Name == "colExcluir")
            {
                bool ativar = !produto.Ativo;
                string acao = ativar ? "reativar" : "remover";
                string explicacao = ativar
                    ? "O produto voltará a aparecer para novas operações."
                    : "O produto deixará de aparecer para novas operações. O histórico e os registros existentes serão preservados.";
                if (MessageBox.Show(this,
                    "Deseja " + acao + " “" + produto.Nome + "”?\n\n" + explicacao,
                    "Produtos", MessageBoxButtons.YesNo,
                    ativar ? MessageBoxIcon.Question : MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
                try
                {
                    produtoService.AlterarAtivo(usuarioAutenticado, produto.IdProduto, ativar);
                    CarregarProdutos();
                    lblPaginacao.Text = ativar
                        ? "Produto reativado. Ele já está disponível para novas operações."
                        : "Produto removido do catálogo ativo. O histórico foi preservado.";
                }
                catch (Exception erro)
                {
                    FeedbackUsuario.ExibirErro(this, erro,
                        "Não foi possível atualizar o produto. Tente novamente.", lblPaginacao);
                }
            }
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        public void Pesquisar(string texto)
        {
            if (texto == null)
            {
                texto = string.Empty;
            }

            txtPesquisarProduto.Text = texto;
        }

        private void AjustarArredondamento(object sender, EventArgs e)
        {
            Control controle = (Control)sender;
            VisualUtils.Arredondar(controle, 12);
            controle.Invalidate();
        }
    }
}
